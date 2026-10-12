using Microsoft.Playwright;
using Neko.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Builder
{
    /// <summary>What <c>neko render</c> was asked to do.</summary>
    public sealed class VideoRenderSettings
    {
        /// <summary>A video page (a Markdown file with <c>video:</c> front matter), or a folder of them.</summary>
        public string Input { get; set; }

        /// <summary>The MP4 (or WebM) to write; a folder when the input is one. Defaults to beside the source.</summary>
        public string Output { get; set; }

        public int Workers { get; set; } = 4;

        /// <summary>draft (fast, large CRF), standard, high.</summary>
        public string Quality { get; set; } = "standard";

        /// <summary>mp4 (H.264 + AAC) or webm (VP9 + Opus).</summary>
        public string Format { get; set; } = "mp4";

        public double? From { get; set; }
        public double? To { get; set; }

        /// <summary>Capture frames as JPEG (faster, slightly softer) instead of PNG.</summary>
        public bool Jpeg { get; set; }

        public bool KeepFrames { get; set; }

        /// <summary>When set, write a single PNG of this moment instead of a video.</summary>
        public double? Still { get; set; }

        public bool NoAudio { get; set; }

        /// <summary>A Chromium executable to use instead of Playwright's own.</summary>
        public string Browser { get; set; }

        public string Ffmpeg { get; set; }
    }

    /// <summary>
    /// <c>neko render</c>: films a video page. The page is built exactly as
    /// <c>neko build</c> would build it, loaded into headless Chromium (Playwright) at
    /// the video's own size, and driven one frame at a time through
    /// <c>window.nekoVideo.seek(t)</c>; each frame is a screenshot. ffmpeg (through
    /// Xabe.FFmpeg) then encodes the frames and mixes the soundtrack — the music bed
    /// and each scene's voice line, placed at the times the timeline gave them.
    ///
    /// Nothing about the page depends on the wall clock — every frame is a pure
    /// function of its time — so the frames can be captured by several pages in
    /// parallel, each taking a slice of the timeline.
    /// </summary>
    public static class VideoRenderer
    {
        private const string Origin = "http://neko.render";

        private sealed class Job
        {
            public string Source;
            public string Root;
            public ParsedDocument Doc;
            public string Html;
            public string Output;
        }

        public static async Task<int> RunAsync(VideoRenderSettings settings)
        {
            var input = Path.GetFullPath(settings.Input ?? ".");
            var sources = new List<string>();
            if (Directory.Exists(input))
            {
                sources.AddRange(Directory.GetFiles(input, "*.md", SearchOption.AllDirectories)
                    .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(seg => seg.StartsWith(".") && seg.Length > 1)));
            }
            else if (File.Exists(input))
            {
                sources.Add(input);
            }
            else
            {
                Console.Error.WriteLine($"[render] Not found: {input}");
                return 1;
            }

            var jobs = new List<Job>();
            foreach (var source in sources.OrderBy(s => s, StringComparer.Ordinal))
            {
                var job = Prepare(source, settings, isBatch: sources.Count > 1 || Directory.Exists(input));
                if (job != null) jobs.Add(job);
            }

            if (jobs.Count == 0)
            {
                Console.Error.WriteLine("[render] No video pages found (a video page has `video:` in its front matter).");
                return 1;
            }

            string ffmpeg = null;
            if (settings.Still == null)
            {
                ffmpeg = await FfmpegLocator.EnsureAsync(settings.Ffmpeg);
                Console.WriteLine($"[render] ffmpeg: {ffmpeg}");
            }

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await LaunchAsync(playwright, settings.Browser);

            foreach (var job in jobs)
            {
                if (settings.Still is double still)
                {
                    await StillAsync(browser, job, still);
                }
                else
                {
                    await RenderAsync(browser, job, settings, ffmpeg);
                }
            }
            return 0;
        }

        // -----------------------------------------------------------------
        // building the page
        // -----------------------------------------------------------------

        private static Job Prepare(string source, VideoRenderSettings settings, bool isBatch)
        {
            var root = FindProjectRoot(source);
            var config = ConfigParser.Parse(Path.Combine(root, "neko.yml"));
            var parser = new MarkdownParser(config);
            var markdown = File.ReadAllText(source);
            var doc = parser.Parse(markdown, source, root);
            if (!doc.IsVideo)
            {
                if (!isBatch) Console.Error.WriteLine($"[render] {source} is not a video page: add `video: true` to its front matter.");
                return null;
            }
            if (doc.Scenes == null || doc.Scenes.Count == 0)
            {
                Console.Error.WriteLine($"[render] {source} has no scenes.");
                return null;
            }

            // A render films the content, never a password prompt.
            doc.FrontMatter.Password = "none";

            string headIncludes = null;
            var headPath = Path.Combine(root, "_includes", "head.html");
            if (File.Exists(headPath)) headIncludes = File.ReadAllText(headPath);
            var generator = new HtmlGenerator(config, false, headIncludes, editorEnabled: false) { InputDirectory = root };
            var html = generator.GenerateVideo(doc, source);

            var extension = settings.Still != null ? ".png" : (settings.Format == "webm" ? ".webm" : ".mp4");
            string output;
            if (!string.IsNullOrEmpty(settings.Output) && !isBatch)
            {
                output = Path.GetFullPath(settings.Output);
                if (Directory.Exists(output)) output = Path.Combine(output, Path.GetFileNameWithoutExtension(source) + extension);
            }
            else if (!string.IsNullOrEmpty(settings.Output))
            {
                output = Path.Combine(Path.GetFullPath(settings.Output), Path.GetFileNameWithoutExtension(source) + extension);
            }
            else
            {
                output = Path.ChangeExtension(source, extension);
            }

            return new Job { Source = source, Root = root, Doc = doc, Html = html, Output = output };
        }

        /// <summary>The folder holding the nearest <c>neko.yml</c> above the file, or the file's own folder.</summary>
        public static string FindProjectRoot(string file)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(file));
            for (var d = dir; d != null; d = Path.GetDirectoryName(d))
            {
                if (File.Exists(Path.Combine(d, "neko.yml"))) return d;
            }
            return dir;
        }

        // -----------------------------------------------------------------
        // the browser
        // -----------------------------------------------------------------

        private static readonly string[] ChromiumArgs =
        {
            "--force-device-scale-factor=1",
            "--force-color-profile=srgb",
            "--font-render-hinting=none",
            "--disable-background-timer-throttling",
            "--disable-renderer-backgrounding",
            "--disable-backgrounding-occluded-windows",
            "--autoplay-policy=no-user-gesture-required",
            "--hide-scrollbars",
            "--mute-audio",
        };

        private static async Task<IBrowser> LaunchAsync(IPlaywright playwright, string explicitBrowser)
        {
            var options = new BrowserTypeLaunchOptions { Headless = true, Args = ChromiumArgs };
            if (!string.IsNullOrEmpty(explicitBrowser))
            {
                options.ExecutablePath = explicitBrowser;
                return await playwright.Chromium.LaunchAsync(options);
            }

            try
            {
                return await playwright.Chromium.LaunchAsync(options);
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                // Playwright's own build is not installed. A Chromium Playwright installed
                // for another version works just as well for screenshots.
                var existing = FindInstalledChromium();
                if (existing != null)
                {
                    Console.WriteLine($"[render] Using installed Chromium: {existing}");
                    options.ExecutablePath = existing;
                    return await playwright.Chromium.LaunchAsync(options);
                }

                Console.WriteLine("[render] Installing Chromium for Playwright (once)…");
                var exit = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                if (exit != 0) throw new InvalidOperationException("Chromium could not be installed. Pass --browser <path to chrome>.");
                return await playwright.Chromium.LaunchAsync(options);
            }
        }

        private static string FindInstalledChromium()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var roots = new[]
            {
                Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH"),
                Path.Combine(home, ".cache", "ms-playwright"),
                Path.Combine(home, "Library", "Caches", "ms-playwright"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ms-playwright"),
            };
            var names = new[] { "chrome", "chrome.exe", "Chromium", "Google Chrome for Testing" };
            foreach (var root in roots.Where(r => !string.IsNullOrEmpty(r) && Directory.Exists(r)))
            {
                foreach (var dir in Directory.GetDirectories(root, "chromium-*").OrderByDescending(d => d, StringComparer.Ordinal))
                {
                    var exe = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                        .FirstOrDefault(f => names.Contains(Path.GetFileName(f)));
                    if (exe != null) return exe;
                }
            }
            return null;
        }

        private static async Task<IPage> OpenAsync(IBrowser browser, Job job)
        {
            var video = job.Doc.Video;
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = video.Width, Height = video.Height },
                DeviceScaleFactor = 1,
                ColorScheme = ColorScheme.Light,
                ReducedMotion = ReducedMotion.NoPreference,
            });
            var page = await context.NewPageAsync();

            // The page is served from memory, and every other request (a clip, an image
            // the page did not inline) from the project folder — no web server, and the
            // page's site-root-relative URLs resolve as they do on the built site.
            await page.RouteAsync(Origin + "/**", async route =>
            {
                var path = Uri.UnescapeDataString(new Uri(route.Request.Url).AbsolutePath);
                if (path == "/__neko_video.html")
                {
                    await route.FulfillAsync(new RouteFulfillOptions { Status = 200, ContentType = "text/html; charset=utf-8", Body = job.Html });
                    return;
                }
                var file = ResolveLocalAsset(path, job.Source, job.Root);
                if (file == null)
                {
                    await route.FulfillAsync(new RouteFulfillOptions { Status = 404, Body = string.Empty });
                    return;
                }
                await route.FulfillAsync(new RouteFulfillOptions { Status = 200, Path = file });
            });

            page.Console += (_, message) =>
            {
                if (message.Type == "error") Console.WriteLine($"[render] page: {message.Text}");
            };

            await page.GotoAsync(Origin + "/__neko_video.html#render", new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 120000 });
            await page.WaitForFunctionAsync("() => window.nekoVideo && window.nekoVideo.isReady", null,
                new PageWaitForFunctionOptions { PollingInterval = 100, Timeout = 120000 });
            await page.EvaluateAsync("() => window.nekoVideo.renderMode()");
            return page;
        }

        /// <summary>A URL path the page asked for, as a file: under the project root, or next to the source.</summary>
        internal static string ResolveLocalAsset(string url, string source, string root)
        {
            if (string.IsNullOrEmpty(url) || url.Contains("://")) return null;
            var clean = url.Split('?', '#')[0].Replace('\\', '/');
            var candidates = new List<string>();
            if (clean.StartsWith("/"))
            {
                candidates.Add(Path.Combine(root, clean.TrimStart('/')));
            }
            else
            {
                candidates.Add(Path.Combine(Path.GetDirectoryName(source) ?? root, clean));
                candidates.Add(Path.Combine(root, clean));
            }
            foreach (var candidate in candidates)
            {
                var full = Path.GetFullPath(candidate);
                // Never serve anything outside the project.
                if (!full.StartsWith(Path.GetFullPath(root), StringComparison.Ordinal)) continue;
                if (File.Exists(full)) return full;
            }
            return null;
        }

        // -----------------------------------------------------------------
        // capturing
        // -----------------------------------------------------------------

        private static async Task StillAsync(IBrowser browser, Job job, double time)
        {
            var page = await OpenAsync(browser, job);
            await page.EvaluateAsync("t => window.nekoVideo.seek(t)", time);
            Directory.CreateDirectory(Path.GetDirectoryName(job.Output)!);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = job.Output, Type = ScreenshotType.Png });
            await page.Context.CloseAsync();
            Console.WriteLine($"[render] {Path.GetFileName(job.Source)} @ {time:0.###}s → {job.Output}");
        }

        private static async Task RenderAsync(IBrowser browser, Job job, VideoRenderSettings settings, string ffmpeg)
        {
            var video = job.Doc.Video;
            var fps = video.Fps;
            var from = Math.Max(0, settings.From ?? 0);
            var to = Math.Min(job.Doc.VideoDuration, settings.To ?? job.Doc.VideoDuration);
            var frames = Math.Max(1, (int)Math.Ceiling((to - from) * fps - 1e-6));
            var workers = Math.Clamp(settings.Workers, 1, Math.Max(1, frames / 15));
            var extension = settings.Jpeg ? "jpg" : "png";

            var work = Path.Combine(Path.GetTempPath(), "neko-render-" + Guid.NewGuid().ToString("N")[..10]);
            var framesDir = Path.Combine(work, "frames");
            Directory.CreateDirectory(framesDir);

            Console.WriteLine($"[render] {Path.GetFileName(job.Source)}: {job.Doc.Scenes.Count} scenes, {to - from:0.##} s, {video.Width}x{video.Height} @ {fps} fps → {frames} frames, {workers} worker(s)");
            var clock = Stopwatch.StartNew();

            try
            {
                var done = 0;
                var chunk = (int)Math.Ceiling(frames / (double)workers);
                var tasks = Enumerable.Range(0, workers).Select(async w =>
                {
                    var first = w * chunk;
                    var last = Math.Min(frames, first + chunk);
                    if (first >= last) return;
                    var page = await OpenAsync(browser, job);
                    for (int i = first; i < last; i++)
                    {
                        // The frame's time, quantized to the frame grid.
                        var t = from + i / (double)fps;
                        await page.EvaluateAsync("t => window.nekoVideo.seek(t)", t);
                        var path = Path.Combine(framesDir, $"f_{i + 1:000000}.{extension}");
                        await page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            Path = path,
                            Type = settings.Jpeg ? ScreenshotType.Jpeg : ScreenshotType.Png,
                            Quality = settings.Jpeg ? 92 : null,
                            Animations = ScreenshotAnimations.Allow,
                            Caret = ScreenshotCaret.Hide,
                        });
                        var n = Interlocked.Increment(ref done);
                        if (n % Math.Max(1, fps * 2) == 0 || n == frames)
                        {
                            Console.WriteLine($"[render]   {n}/{frames} frames ({n / Math.Max(0.001, clock.Elapsed.TotalSeconds):0.0} fps)");
                        }
                    }
                    await page.Context.CloseAsync();
                }).ToList();
                await Task.WhenAll(tasks);

                Console.WriteLine($"[render] Captured {frames} frames in {clock.Elapsed.TotalSeconds:0.0} s; encoding…");

                Directory.CreateDirectory(Path.GetDirectoryName(job.Output)!);
                var audio = settings.NoAudio ? new List<AudioClip>() : CollectAudio(job, from, to);
                var arguments = BuildFfmpegArguments(Path.Combine(framesDir, "f_%06d." + extension), fps, to - from, audio, settings, job.Output);
                await Xabe.FFmpeg.FFmpeg.Conversions.New().Start(arguments);

                var size = new FileInfo(job.Output).Length / (1024.0 * 1024.0);
                Console.WriteLine($"[render] Wrote {job.Output} ({size:0.0} MB) in {clock.Elapsed.TotalSeconds:0.0} s");
            }
            finally
            {
                if (settings.KeepFrames)
                {
                    Console.WriteLine($"[render] Frames kept in {framesDir}");
                }
                else
                {
                    try { Directory.Delete(work, true); } catch (IOException) { /* a frame still open: leave the temp folder */ }
                }
            }
        }

        // -----------------------------------------------------------------
        // the soundtrack and the encode
        // -----------------------------------------------------------------

        /// <summary>One audio file on the timeline: where it starts, how loud, and whether it is the music bed.</summary>
        public sealed record AudioClip(string Path, double Start, double Volume, bool IsMusic);

        private static List<AudioClip> CollectAudio(Job job, double from, double to)
        {
            var clips = new List<AudioClip>();
            var video = job.Doc.Video;
            if (!string.IsNullOrEmpty(video.Music))
            {
                var file = ResolveLocalAsset(video.Music, job.Source, job.Root);
                if (file != null) clips.Add(new AudioClip(file, -from, video.MusicVolume, true));
                else Console.WriteLine($"[render] Music not found: {video.Music}");
            }
            foreach (var scene in job.Doc.Scenes)
            {
                if (string.IsNullOrEmpty(scene.Voice)) continue;
                var file = ResolveLocalAsset(scene.Voice, job.Source, job.Root);
                if (file == null) { Console.WriteLine($"[render] Voice not found: {scene.Voice}"); continue; }
                var offset = VideoOptions.TryDouble(scene.Get("voice-at"), out var at) ? at : scene.TransitionDuration;
                var volume = VideoOptions.TryDouble(scene.Get("voice-volume"), out var v) ? v : 1;
                var start = scene.Start + offset - from;
                if (start < to - from) clips.Add(new AudioClip(file, start, volume, false));
            }
            return clips;
        }

        /// <summary>
        /// The ffmpeg command line: the frame sequence in, H.264 (or VP9) out in BT.709,
        /// and — when there is a soundtrack — a filter graph that delays each clip to
        /// its place, sets its gain, fades the music bed in and out, and mixes them.
        /// </summary>
        public static string BuildFfmpegArguments(string framePattern, int fps, double duration, IReadOnlyList<AudioClip> audio, VideoRenderSettings settings, string output)
        {
            string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.Append($"-y -framerate {fps} -i \"{framePattern}\"");
            foreach (var clip in audio)
            {
                // A clip that started before the rendered range is trimmed at its head.
                if (clip.Start < 0) sb.Append($" -ss {N(-clip.Start)}");
                sb.Append($" -i \"{clip.Path}\"");
            }

            if (audio.Count > 0)
            {
                var graph = new StringBuilder();
                for (int i = 0; i < audio.Count; i++)
                {
                    var clip = audio[i];
                    var delayMs = (int)Math.Round(Math.Max(0, clip.Start) * 1000);
                    graph.Append($"[{i + 1}:a]aresample=48000,volume={N(clip.Volume)}");
                    if (clip.IsMusic)
                    {
                        var fadeOut = Math.Min(2.0, duration / 4);
                        graph.Append($",afade=t=in:st=0:d=0.6,afade=t=out:st={N(Math.Max(0, duration - fadeOut))}:d={N(fadeOut)}");
                    }
                    graph.Append($",adelay={delayMs}|{delayMs}[a{i}];");
                }
                for (int i = 0; i < audio.Count; i++) graph.Append($"[a{i}]");
                graph.Append($"amix=inputs={audio.Count}:duration=longest:dropout_transition=0:normalize=0,atrim=0:{N(duration)}[aout]");
                sb.Append($" -filter_complex \"{graph}\" -map 0:v -map \"[aout]\"");
            }

            // Screenshots are sRGB; convert to video range BT.709 explicitly and tag it,
            // or players read the colours with the BT.601 matrix and shift every hue.
            sb.Append(" -vf \"scale=out_color_matrix=bt709:out_range=tv,format=yuv420p\"");
            sb.Append(" -colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv");

            var (preset, crf) = (settings.Quality ?? "standard").ToLowerInvariant() switch
            {
                "draft" => ("ultrafast", 28),
                "high" => ("slow", 15),
                _ => ("medium", 18),
            };

            if (string.Equals(settings.Format, "webm", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($" -c:v libvpx-vp9 -b:v 0 -crf {crf + 14} -row-mt 1 -deadline good");
                if (audio.Count > 0) sb.Append(" -c:a libopus -b:a 128k");
            }
            else
            {
                sb.Append($" -c:v libx264 -preset {preset} -crf {crf} -movflags +faststart");
                if (audio.Count > 0) sb.Append(" -c:a aac -b:a 192k");
            }
            sb.Append($" -r {fps} -t {N(duration)} \"{output}\"");
            return sb.ToString();
        }
    }
}
