using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Neko.Tests
{
    /// <summary>
    /// Renders real documentation pages with the pure-C# generated stylesheet and
    /// with the official Tailwind CLI's stylesheet, and asserts the two render
    /// pixel-for-pixel the same. This is the strongest "the output looks the same"
    /// guarantee: even where the CSS source differs (rule order, formatting), the
    /// computed styles — and therefore the rendered pixels — must match.
    ///
    /// Covers the index plus five different page types, in light and dark mode.
    /// Skipped when the Tailwind CLI or a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class TailwindVisualParityTests
    {
        private static readonly string[] Pages =
        {
            "/index.html",
            "/components/code-block.html",
            "/components/alert.html",
            "/components/table.html",
            "/components/mermaid.html",
            "/changelog/index.html",
        };

        [Test]
        public async Task CSharp_And_Cli_Render_Identically()
        {
            var docsDir = LocateDocs();
            if (docsDir == null) Assert.Ignore("Neko.Documentation not found next to the test assembly.");

            var cli = TailwindCliParityTests_ResolveCli();
            if (cli == null) Assert.Ignore("No Tailwind CLI (set NEKO_TAILWIND_CLI or PATH) — visual parity needs the oracle.");

            // 1. Build the docs with our generator into a temp dir.
            var outDir = Path.Combine(Path.GetTempPath(), "neko-vis-" + Guid.NewGuid().ToString("N"));
            await new SiteBuilder(docsDir!, outDir).BuildAsync();
            var ourCss = Path.Combine(outDir, "assets", "tailwind.css");
            Assert.That(File.Exists(ourCss), "our tailwind.css should have been generated");
            var ourCssText = await File.ReadAllTextAsync(ourCss);

            // 2. Generate the CLI's stylesheet for the same built site.
            var cliCssText = RunCliFull(cli!, outDir);
            if (string.IsNullOrWhiteSpace(cliCssText) || cliCssText.Length < 5000)
                Assert.Ignore($"Tailwind CLI produced no usable stylesheet ({cliCssText?.Length ?? 0} bytes).");

            // 3. Serve the built site over HTTP.
            using var server = new StaticServer(outDir);
            var baseUrl = server.Start();

            // 4. Launch a browser (skip if unavailable).
            IPlaywright pw = null;
            IBrowser browser = null;
            try
            {
                pw = await Playwright.CreateAsync();
                browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
            }
            catch (Exception ex)
            {
                pw?.Dispose();
                Assert.Ignore($"Playwright browser unavailable: {ex.Message}");
            }

            var failures = new List<string>();
            try
            {
                foreach (var dark in new[] { false, true })
                foreach (var page in Pages)
                {
                    // Our render.
                    await File.WriteAllTextAsync(ourCss, ourCssText);
                    var ours = await Shoot(browser!, baseUrl + page, dark);
                    // CLI render.
                    await File.WriteAllTextAsync(ourCss, cliCssText);
                    var theirs = await Shoot(browser!, baseUrl + page, dark);

                    var diff = PixelDiffFraction(ours, theirs, out var note);
                    var label = $"{page} ({(dark ? "dark" : "light")})";
                    TestContext.WriteLine($"{label}: diff={diff:P3} {note}");
                    if (diff > 0.001) // allow 0.1% for anti-aliasing jitter
                        failures.Add($"{label}: {diff:P3} differ {note}");
                }
            }
            finally
            {
                await File.WriteAllTextAsync(ourCss, ourCssText);
                await browser!.CloseAsync();
                pw!.Dispose();
            }

            Assert.That(failures, Is.Empty,
                "Pages rendered differently between the C# generator and the Tailwind CLI:\n" + string.Join("\n", failures));
        }

        private static async Task<byte[]> Shoot(IBrowser browser, string url, bool dark)
        {
            var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1440, Height = 1600 },
                ColorScheme = dark ? ColorScheme.Dark : ColorScheme.Light,
            });
            if (dark)
                await ctx.AddInitScriptAsync("try{localStorage.setItem('theme','dark');localStorage.setItem('neko-theme','dark');document.documentElement.classList.add('dark');}catch(e){}");
            else
                await ctx.AddInitScriptAsync("try{localStorage.setItem('theme','light');document.documentElement.classList.remove('dark');}catch(e){}");

            var page = await ctx.NewPageAsync();
            // Block external requests (fonts/CDN) so renders are offline-stable and
            // depend only on the local stylesheet under test.
            await page.RouteAsync("**/*", async route =>
            {
                var u = route.Request.Url;
                if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:"))
                    await route.ContinueAsync();
                else
                    await route.AbortAsync();
            });
            try { await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
            catch { /* network idle best-effort */ }
            await page.WaitForTimeoutAsync(200);
            var bytes = await page.ScreenshotAsync(new() { FullPage = false });
            await ctx.CloseAsync();
            return bytes;
        }

        private static double PixelDiffFraction(byte[] a, byte[] b, out string note)
        {
            using var ia = Image.Load<Rgba32>(a);
            using var ib = Image.Load<Rgba32>(b);
            if (ia.Width != ib.Width || ia.Height != ib.Height)
            {
                note = $"(size {ia.Width}x{ia.Height} vs {ib.Width}x{ib.Height})";
                return 1.0;
            }
            long differing = 0;
            long total = (long)ia.Width * ia.Height;
            for (int y = 0; y < ia.Height; y++)
            for (int x = 0; x < ia.Width; x++)
            {
                var pa = ia[x, y];
                var pb = ib[x, y];
                int d = Math.Abs(pa.R - pb.R) + Math.Abs(pa.G - pb.G) + Math.Abs(pa.B - pb.B) + Math.Abs(pa.A - pb.A);
                if (d > 16) differing++; // tolerate minor anti-aliasing
            }
            note = "";
            return (double)differing / total;
        }

        // --- helpers --------------------------------------------------------

        private static string LocateDocs()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "Neko.Documentation");
                if (File.Exists(Path.Combine(candidate, "neko.yml"))) return candidate;
            }
            return null;
        }

        private static string TailwindCliParityTests_ResolveCli()
        {
            var env = Environment.GetEnvironmentVariable("NEKO_TAILWIND_CLI");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;
            var exe = OperatingSystem.IsWindows() ? "tailwindcss.exe" : "tailwindcss";
            var path = Environment.GetEnvironmentVariable("PATH");
            if (path == null) return null;
            foreach (var d in path.Split(Path.PathSeparator))
                try { var c = Path.Combine(d.Trim(), exe); if (File.Exists(c)) return c; } catch { }
            return null;
        }

        private static string RunCliFull(string cli, string siteDir)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "neko-vis-cli-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            try
            {
                var (primary, accent) = Neko.Configuration.ThemeDefinitions.ResolvePalettes(new Neko.Configuration.NekoConfig());
                string Json(Dictionary<string, string> d) => "{" + string.Join(",", d.Select(k => $"\"{k.Key}\":\"{k.Value}\"")) + "}";
                var sd = siteDir.Replace("\\", "/");
                File.WriteAllText(Path.Combine(tmp, "tw.config.js"),
                    "module.exports={content:['" + sd + "/**/*.html','" + sd + "/**/*.js','!" + sd + "/**/*.min.js'],darkMode:'class'," +
                    "theme:{extend:{colors:{primary:" + Json(primary) + ",accent:" + Json(accent) + "}}}," +
                    "plugins:[require('@tailwindcss/typography')]};");
                File.WriteAllText(Path.Combine(tmp, "in.css"), "@tailwind base;\n@tailwind components;\n@tailwind utilities;\n");
                var outCss = Path.Combine(tmp, "out.css");
                var psi = new ProcessStartInfo { FileName = cli, WorkingDirectory = tmp, RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
                psi.ArgumentList.Add("-c"); psi.ArgumentList.Add(Path.Combine(tmp, "tw.config.js"));
                psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(Path.Combine(tmp, "in.css"));
                psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(outCss);
                psi.ArgumentList.Add("--minify");
                using var p = Process.Start(psi);
                p!.StandardError.ReadToEnd(); p.StandardOutput.ReadToEnd(); p.WaitForExit();
                return File.Exists(outCss) ? File.ReadAllText(outCss) : "";
            }
            finally { try { Directory.Delete(tmp, true); } catch { } }
        }

        /// <summary>Minimal static file server for the built site.</summary>
        private sealed class StaticServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private readonly string _root;
            public StaticServer(string root) { _root = root; }

            public string Start()
            {
                int port = 8100 + new Random().Next(800);
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                _ = Task.Run(Loop);
                return $"http://localhost:{port}";
            }

            private async Task Loop()
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = await _listener.GetContextAsync(); } catch { break; }
                    try
                    {
                        var rel = Uri.UnescapeDataString(ctx.Request.Url!.AbsolutePath.TrimStart('/'));
                        if (rel.Length == 0) rel = "index.html";
                        var file = Path.Combine(_root, rel.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(file))
                        {
                            ctx.Response.ContentType = file.EndsWith(".css") ? "text/css"
                                : file.EndsWith(".js") ? "text/javascript"
                                : file.EndsWith(".html") ? "text/html" : "application/octet-stream";
                            var b = File.ReadAllBytes(file);
                            ctx.Response.OutputStream.Write(b, 0, b.Length);
                        }
                        else ctx.Response.StatusCode = 404;
                    }
                    catch { try { ctx.Response.StatusCode = 500; } catch { } }
                    finally { try { ctx.Response.Close(); } catch { } }
                }
            }

            public void Dispose() { try { _listener.Stop(); } catch { } }
        }
    }
}
