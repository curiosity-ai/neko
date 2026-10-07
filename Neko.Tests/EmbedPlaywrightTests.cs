using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Builds a page using the documented <c>[!embed](url)</c> forms and opens it in a real browser:
    /// each one must produce a visible frame of the requested size, load the embedded page, and show
    /// its caption. The URL in parentheses used to be ignored, so every embed rendered nothing.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class EmbedPlaywrightTests
    {
        [Test]
        public async Task DocumentedEmbedForms_RenderVisibleFrames()
        {
            var inDir  = Path.Combine(Path.GetTempPath(), "neko-embed-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-embed-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(inDir, "assets"));

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Embed Docs\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "assets", "report.html"),
                "<!doctype html><html><body><h1 id=\"marker\">Embedded report</h1></body></html>");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"),
                "# Embeds\n\n" +
                "[!embed height=\"500\" text=\"The report, with a caption\"](/assets/report.html)\n\n" +
                "[!embed aspect=\"4:3\" width=\"400\"](/assets/report.html)\n\n" +
                "[!embed](/assets/report.html)\n");

            await new SiteBuilder(inDir, outDir).BuildAsync();

            using var server = new StaticServer(outDir);
            var baseUrl = server.Start();

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

            try
            {
                var ctx  = await browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
                var page = await ctx.NewPageAsync();

                await page.RouteAsync("**/*", async route =>
                {
                    var u = route.Request.Url;
                    if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:"))
                        await route.ContinueAsync();
                    else
                        await route.AbortAsync();
                });

                try { await page.GotoAsync(baseUrl + "/index.html", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
                catch { /* network idle best-effort */ }

                var frames = page.Locator("figure.neko-embed iframe");
                Assert.That(await frames.CountAsync(), Is.EqualTo(3), "Every [!embed](url) renders an iframe.");

                // An explicit height is honoured.
                var first = await frames.Nth(0).BoundingBoxAsync();
                Assert.That(first!.Height, Is.EqualTo(500).Within(2), "height=\"500\" gives a 500px frame.");

                // aspect + width size the frame to 400 x 300.
                var second = await frames.Nth(1).BoundingBoxAsync();
                Assert.That(second!.Width,  Is.EqualTo(400).Within(2), "width=\"400\" gives a 400px frame.");
                Assert.That(second.Height,  Is.EqualTo(300).Within(3), "aspect=\"4:3\" at 400px wide is 300px tall.");

                // With neither, the frame defaults to 16:9 and is not collapsed.
                var third = await frames.Nth(2).BoundingBoxAsync();
                Assert.That(third!.Height, Is.GreaterThan(100), "The default embed has a visible height.");
                Assert.That(third.Width / third.Height, Is.EqualTo(16.0 / 9.0).Within(0.05), "The default aspect ratio is 16:9.");

                // The embedded page actually loads.
                var frame = page.FrameLocator("figure.neko-embed iframe").First;
                Assert.That(await frame.Locator("#marker").TextContentAsync(), Is.EqualTo("Embedded report"));

                // text= becomes a caption.
                Assert.That(await page.Locator("figure.neko-embed figcaption").First.TextContentAsync(), Is.EqualTo("The report, with a caption"));
            }
            finally
            {
                if (browser != null) await browser.CloseAsync();
                pw?.Dispose();
                try { Directory.Delete(inDir, true); } catch { }
                try { Directory.Delete(outDir, true); } catch { }
            }
        }

        private sealed class StaticServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private readonly string _root;
            public StaticServer(string root) { _root = root; }

            public string Start()
            {
                int port = 9100 + new Random().Next(800);
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
                        if (Directory.Exists(file)) file = Path.Combine(file, "index.html");
                        if (!File.Exists(file) && File.Exists(file + ".html")) file += ".html";

                        if (File.Exists(file))
                        {
                            ctx.Response.ContentType = file.EndsWith(".css") ? "text/css"
                                : file.EndsWith(".js") ? "text/javascript"
                                : file.EndsWith(".json") ? "application/json"
                                : file.EndsWith(".html") ? "text/html" : "application/octet-stream";
                            var bytes = await File.ReadAllBytesAsync(file);
                            await ctx.Response.OutputStream.WriteAsync(bytes);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
                    }
                    catch { /* best-effort static server */ }
                    finally { try { ctx.Response.Close(); } catch { } }
                }
            }

            public void Dispose()
            {
                try { _listener.Stop(); } catch { }
                try { _listener.Close(); } catch { }
            }
        }
    }
}
