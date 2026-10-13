using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// The voice-over in the preview, in a real browser: the timing panel (scenes,
    /// animation and voice-over lanes) opens with <kbd>T</kbd> or <c>#timing</c>, its
    /// playhead follows the playhead, clicking a sentence seeks to it, the subtitle shows
    /// the sentence being spoken, and render mode films none of it. A take on disk turns
    /// its sentence from an estimate into audio the preview plays.
    ///
    /// Skipped when a Playwright browser is unavailable; <c>NEKO_TEST_CHROMIUM</c> points
    /// at a preinstalled Chromium.
    /// </summary>
    [TestFixture]
    public class VideoNarrationPlaywrightTests
    {
        private string _inputDir;
        private string _outDir;

        private const string Film = @"---
title: Narrated film
video:
  size: 640x360
  fps: 12
  narration:
    voice: ""A calm narrator.""
---

--- {duration=""3""}

# Hello *there*

--- {duration=""3"" transition=""cut""}

## The second scene

--- {narration}

The first line is spoken here.

{at=""scene-2""}
The second scene has a line too.
";

        [OneTimeSetUp]
        public void Setup()
        {
            _inputDir = Path.Combine(Path.GetTempPath(), "neko-narration-pw-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_inputDir, "videos"));
            File.WriteAllText(Path.Combine(_inputDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
            File.WriteAllText(Path.Combine(_inputDir, "index.md"), "# Home\n");
            var source = Path.Combine(_inputDir, "videos", "film.md");
            File.WriteAllText(source, Film);

            // A take for the first sentence, as `neko voiceover` would leave it.
            VideoNarration.PrintWarnings = false;
            var doc = new MarkdownParser(new Neko.Configuration.NekoConfig()).Parse(Film, source, _inputDir);
            var tone = Enumerable.Range(0, 24000 * 2).Select(i => 0.3f * MathF.Sin(i * 2 * MathF.PI * 220 / 24000)).ToArray();
            VoiceOverCommand.WriteTake(doc.Narration!.Sentences.First().File, tone, 24000);
            VideoNarration.PrintWarnings = true;

            _outDir = Path.Combine(Path.GetTempPath(), "neko-narration-pw-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(_inputDir, _outDir).BuildAsync().GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            try { Directory.Delete(_inputDir, true); } catch (IOException) { }
            try { Directory.Delete(_outDir, true); } catch (IOException) { }
        }

        private string PageUrl(string fragment = "") => new Uri(Path.Combine(_outDir, "videos", "film.html")).AbsoluteUri + fragment;

        private static async Task<(IPlaywright, IBrowser)> LaunchAsync()
        {
            try
            {
                var pw = await Playwright.CreateAsync();
                var launch = new BrowserTypeLaunchOptions { Headless = true, Args = new[] { "--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE localhost" } };
                var exe = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM");
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) launch.ExecutablePath = exe;
                return (pw, await pw.Chromium.LaunchAsync(launch));
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Playwright browser unavailable: {ex.Message}");
                throw;
            }
        }

        private static async Task<IPage> OpenReadyAsync(IBrowser browser, string url, int width = 1280, int height = 800)
        {
            var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
            await page.GotoAsync(url);
            await page.WaitForFunctionAsync("() => window.nekoVideo && window.nekoVideo.isReady", null, new() { PollingInterval = 50, Timeout = 30000 });
            return page;
        }

        [Test]
        public void Build_CopiesTheTakes_WithTheSite()
        {
            var takes = Directory.GetFiles(Path.Combine(_outDir, "videos", "assets", "voiceover", "film"), "*.ogg");
            Assert.That(takes, Has.Length.EqualTo(1));
        }

        [Test]
        public async Task TimingPanel_ShowsTheLanes_FollowsThePlayhead_AndSeeks()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl());
                var panel = page.Locator("#nv-timing");
                Assert.That(await panel.IsVisibleAsync(), Is.False, "the panel starts closed");

                var stageBefore = await page.Locator("#nv-stage").BoundingBoxAsync();
                await page.Keyboard.PressAsync("t");
                Assert.That(await panel.IsVisibleAsync(), Is.True, "T opens the timing panel");
                Assert.That(await page.Locator("#nv-timing-toggle").GetAttributeAsync("aria-pressed"), Is.EqualTo("true"));
                var stageAfter = await page.Locator("#nv-stage").BoundingBoxAsync();
                Assert.That(stageAfter!.Height, Is.LessThan(stageBefore!.Height), "the canvas makes room for the panel");

                Assert.That(await page.Locator(".nv-seg").CountAsync(), Is.EqualTo(2));
                Assert.That(await page.Locator(".nv-ev").CountAsync(), Is.GreaterThanOrEqualTo(2));
                Assert.That(await page.Locator(".nv-sent").CountAsync(), Is.EqualTo(2));
                Assert.That(await page.Locator(".nv-sent.is-estimated").CountAsync(), Is.EqualTo(1), "only the sentence with no take is an estimate");

                // Clicking the second sentence seeks to it; the playhead and the subtitle follow.
                var second = page.Locator(".nv-sent").Nth(1);
                var at = double.Parse((await second.GetAttributeAsync("data-t"))!, System.Globalization.CultureInfo.InvariantCulture);
                await second.ClickAsync();
                Assert.That(await page.EvaluateAsync<double>("() => window.nekoVideo.time()"), Is.EqualTo(at).Within(1e-3));
                Assert.That(await page.Locator(".nv-sent.is-active").CountAsync(), Is.EqualTo(1));
                Assert.That(await page.Locator("#nv-subtitle").TextContentAsync(), Is.EqualTo("The second scene has a line too."));
                var f = await page.EvaluateAsync<double>("() => parseFloat(getComputedStyle(document.getElementById('nv-playhead')).getPropertyValue('--f'))");
                Assert.That(f, Is.EqualTo(at / await page.EvaluateAsync<double>("() => window.nekoVideo.duration")).Within(1e-3));

                // Between the lines there is no subtitle.
                await page.EvaluateAsync("() => window.nekoVideo.seek(0.1)");
                Assert.That(await page.Locator("#nv-subtitle.is-on").CountAsync(), Is.EqualTo(0));

                await page.Keyboard.PressAsync("t");
                Assert.That(await panel.IsVisibleAsync(), Is.False, "T closes it again");
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task TimingFragment_OpensThePanel_AndTheTakeIsAnAudioTrack()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl("#timing"));
                Assert.That(await page.Locator("#nv-timing").IsVisibleAsync(), Is.True);
                var audio = page.Locator("audio.nv-audio[data-kind=\"narration\"]");
                Assert.That(await audio.CountAsync(), Is.EqualTo(1));
                Assert.That(await audio.GetAttributeAsync("src"), Does.EndWith(".ogg"));
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task RenderMode_FilmsNeitherThePanelNorTheSubtitle()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl("#render&timing"), 640, 360);
                await page.EvaluateAsync("() => window.nekoVideo.seek(1)");
                Assert.That(await page.Locator("#nv-timing").IsVisibleAsync(), Is.False);
                Assert.That(await page.Locator("#nv-subtitle").IsVisibleAsync(), Is.False);
                var box = await page.Locator("#nv-stage").BoundingBoxAsync();
                Assert.That((box!.Width, box.Height), Is.EqualTo((640f, 360f)));
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task NekoRender_Preview_PrintsTheSheet_WithoutABrowser()
        {
            var output = new StringWriter();
            var previous = Console.Out;
            Console.SetOut(output);
            try
            {
                var exit = await VideoRenderer.RunAsync(new VideoRenderSettings { Input = Path.Combine(_inputDir, "videos", "film.md"), Preview = true });
                Assert.That(exit, Is.EqualTo(0));
            }
            finally
            {
                Console.SetOut(previous);
            }
            var text = output.ToString();
            Assert.That(text, Does.Contain("2 voice-over lines in 2 sentences (1 estimated)"));
            Assert.That(text, Does.Contain("● The first line is spoken here."));
            Assert.That(text, Does.Contain("~ The second scene has a line too."));
            Assert.That(text, Does.Not.Contain("[render] ffmpeg"), "a preview needs neither ffmpeg nor a browser");
        }

        [Test]
        public async Task NekoRender_MixesTheVoiceOver_IntoTheMp4()
        {
            if (FfmpegLocator.Find() == null) Assert.Ignore("ffmpeg is not installed.");
            var (pw, browser) = await LaunchAsync();
            await browser.CloseAsync();
            pw.Dispose();

            var mp4 = Path.Combine(_outDir, "film.mp4");
            var exit = await VideoRenderer.RunAsync(new VideoRenderSettings
            {
                Input = Path.Combine(_inputDir, "videos", "film.md"),
                Output = mp4,
                Quality = "draft",
                Workers = 2,
                NoVoiceOver = true, // the second sentence stays silent: no model in a test
                Browser = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM"),
            });
            Assert.That(exit, Is.EqualTo(0));
            var bytes = File.ReadAllBytes(mp4);
            Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 4, 4), Is.EqualTo("ftyp"));
            Assert.That(System.Text.Encoding.ASCII.GetString(bytes).Contains("soun"), Is.True, "the MP4 has an audio track");
        }
    }
}
