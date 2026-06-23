using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Neko.Extensions;

namespace Neko.Builder
{
    // `neko gen-tesserae-heights` measures the rendered height of each `tesserae`
    // live sample and writes it back onto the block as a `height=<px>` argument, so
    // the build can pin the preview iframe to its content instead of a fixed
    // minimum. Scope it to a single file with `--file` and rerun it whenever you
    // change a sample — there is no hash cache, so the file you target is exactly
    // what gets regenerated.
    public class GenTesseraeHeightsCommand
    {
        private readonly string _inputDirectory;
        private readonly string _file;

        // Captures a ```tesserae … ``` fenced block: indent, the args after the
        // language token, and the body. Tolerant of CRLF.
        private static readonly Regex TesseraeBlock = new(
            @"(?<indent>[ \t]*)```tesserae(?<args>[^\r\n]*)\r?\n(?<body>.*?)\r?\n[ \t]*```",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public GenTesseraeHeightsCommand(string inputDirectory, string file)
        {
            _inputDirectory = inputDirectory;
            _file = file;
        }

        public void Run()
        {
            if (!Directory.Exists(_inputDirectory))
            {
                Console.WriteLine($"[heights] Input directory not found: {_inputDirectory}");
                return;
            }

            // Resolve which markdown files to process.
            List<string> files;
            if (!string.IsNullOrEmpty(_file))
            {
                var target = Path.IsPathRooted(_file) ? _file : Path.Combine(_inputDirectory, _file);
                if (!File.Exists(target))
                {
                    Console.WriteLine($"[heights] File not found: {target}");
                    return;
                }
                files = new List<string> { Path.GetFullPath(target) };
            }
            else
            {
                files = Directory.GetFiles(_inputDirectory, "*.md", SearchOption.AllDirectories).ToList();
            }

            // Keep the on-disk Tesserae compile cache in the project, like the build.
            TesseraeCompiler.SetCacheRoot(Path.Combine(_inputDirectory, ".neko-cache"));

            if (!SnapFrameExtension.EnsureToolInstalled())
            {
                Console.WriteLine("[heights] snapframe tool is required to measure heights and could not be installed. Aborting.");
                return;
            }

            // Compiled previews and their shared assets are written here, then served
            // so snapframe can load each sample from a real URL.
            var serveRoot = Path.Combine(Path.GetTempPath(), "neko-heights-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(serveRoot);

            HttpListener server = null;
            try
            {
                var port = StartServer(serveRoot, out server);
                var totalUpdated = 0;

                foreach (var file in files)
                {
                    string markdown;
                    try { markdown = File.ReadAllText(file); }
                    catch { continue; }

                    if (markdown.IndexOf("```tesserae", StringComparison.OrdinalIgnoreCase) < 0) continue;

                    var index = 0;
                    var updatedFile = false;

                    var rewritten = TesseraeBlock.Replace(markdown, match =>
                    {
                        var args = match.Groups["args"].Value;
                        var body = match.Groups["body"].Value;
                        var sampleIndex = index++;

                        var height = MeasureSample(body, serveRoot, port, $"{Path.GetFileNameWithoutExtension(file)}-{sampleIndex}");
                        if (height <= 0)
                        {
                            Console.WriteLine($"[heights] {Path.GetFileName(file)} sample #{sampleIndex}: could not measure, left unchanged.");
                            return match.Value;
                        }

                        var newArgs = SetHeightArgument(args, height);
                        if (newArgs == args) return match.Value;

                        updatedFile = true;
                        Console.WriteLine($"[heights] {Path.GetFileName(file)} sample #{sampleIndex}: height={height}");
                        return $"{match.Groups["indent"].Value}```tesserae{newArgs}\r\n{body}\r\n{match.Groups["indent"].Value}```"
                            .Replace("\r\n", DetectNewline(markdown));
                    });

                    if (updatedFile)
                    {
                        File.WriteAllText(file, rewritten);
                        totalUpdated++;
                    }
                }

                Console.WriteLine($"[heights] Done. Updated {totalUpdated} file(s).");
            }
            finally
            {
                try { server?.Stop(); server?.Close(); } catch { }
                try { Directory.Delete(serveRoot, recursive: true); } catch { }
            }
        }

        // Replace an existing `height=<n>` token in the block args, or append one.
        // Public + static so it can be unit-tested without a browser.
        public static string SetHeightArgument(string args, int height)
        {
            args ??= string.Empty;
            var existing = Regex.Match(args, @"\bheight\s*=\s*\d+");
            if (existing.Success)
            {
                return args.Substring(0, existing.Index) + $"height={height}" + args.Substring(existing.Index + existing.Length);
            }
            // Append, keeping a single leading space after the language token.
            var trimmed = args.TrimEnd();
            return (trimmed.Length == 0 ? " " : trimmed + " ") + $"height={height}";
        }

        private static string DetectNewline(string text) => text.Contains("\r\n") ? "\r\n" : "\n";

        // Compile the sample, write a standalone preview page into the served root,
        // and measure its content height with snapframe. Returns 0 on failure.
        private int MeasureSample(string body, string serveRoot, int port, string name)
        {
            try
            {
                var rawLines = body.Replace("\r\n", "\n").Split('\n').Select(l => (string)l).ToList();
                var (code, _) = TesseraeCompiler.PartitionSampleSource(rawLines);

                var result = TesseraeCompiler.CompileAsync($"height probe {name}", code, serveRoot).GetAwaiter().GetResult();
                if (result == null || string.IsNullOrEmpty(result.OutputHtml) || !result.OutputHtml.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                var rel = Path.Combine("_heights", name + ".html");
                var htmlPath = Path.Combine(serveRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(htmlPath));
                File.WriteAllText(htmlPath, result.OutputHtml);

                var url = $"http://localhost:{port}/{rel.Replace(Path.DirectorySeparatorChar, '/')}";
                return MeasureUrlHeight(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[heights] measure failed for {name}: {ex.Message}");
                return 0;
            }
        }

        // Navigate to the URL at a known viewport width and capture a full-page
        // screenshot; the PNG's pixel size (corrected for the device scale factor
        // derived from the captured width) gives the CSS content height.
        private const int ProbeWidth = 1000;

        private int MeasureUrlHeight(string url)
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            var exe = isWindows ? "snapframe.exe" : "snapframe";

            var nav = Run(exe, $"navigate-json \"{url}\" --size {ProbeWidth}x80");
            if (nav == null) return 0;
            var pageId = Regex.Match(nav, "\"PageId\"\\s*:\\s*\"([^\"]+)\"").Groups[1].Value;
            if (string.IsNullOrEmpty(pageId)) return 0;

            Thread.Sleep(1200); // let the sample render

            var png = Path.Combine(Path.GetTempPath(), "neko-height-" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                Run(exe, $"capture {pageId} \"{png}\" --full-page");
                Run(exe, $"close {pageId}");
                if (!File.Exists(png)) return 0;

                var (w, h) = ReadPngSize(png);
                if (w <= 0 || h <= 0) return 0;

                // Correct for the device scale factor: the captured width should be
                // ProbeWidth × scale, so scale = w / ProbeWidth.
                var scale = w / (double)ProbeWidth;
                if (scale <= 0) scale = 1;
                var cssHeight = (int)Math.Round(h / scale);
                return Math.Clamp(cssHeight, 80, 1600);
            }
            finally
            {
                try { if (File.Exists(png)) File.Delete(png); } catch { }
            }
        }

        private static string Run(string exe, string args)
        {
            try
            {
                var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 ? output : null;
            }
            catch
            {
                return null;
            }
        }

        private static (int Width, int Height) ReadPngSize(string path)
        {
            var b = File.ReadAllBytes(path);
            if (b.Length < 24) return (0, 0);
            int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
            int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
            return (w, h);
        }

        private static int StartServer(string root, out HttpListener listener)
        {
            var port = GetFreePort();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            var l = listener;
            var thread = new Thread(() =>
            {
                while (l.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = l.GetContext(); }
                    catch { break; }

                    try
                    {
                        var rel = Uri.UnescapeDataString(ctx.Request.Url.AbsolutePath.TrimStart('/'));
                        var path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(path))
                        {
                            ctx.Response.ContentType = ContentType(path);
                            var bytes = File.ReadAllBytes(path);
                            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
                    }
                    catch { }
                    finally { try { ctx.Response.OutputStream.Close(); } catch { } }
                }
            })
            { IsBackground = true };
            thread.Start();
            return port;
        }

        private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".html" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".woff2" => "font/woff2",
            ".woff" => "font/woff",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        private static int GetFreePort()
        {
            var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            l.Start();
            var port = ((System.Net.IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
    }
}
