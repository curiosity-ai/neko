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
    /// Opens a site rebuilt with <c>neko watch --focus</c> in a real browser: the
    /// focused page must show its new content, and a page the focused build skipped
    /// must still render as a complete, navigable page (its HTML is the one the
    /// previous build wrote, never regenerated).
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class FocusModePlaywrightTests
    {
        [Test]
        public async Task FocusedRebuild_ServesFreshFocusedPageAndIntactReusedPage()
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-focus-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-focus-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(inDir, "guides"));
            Directory.CreateDirectory(Path.Combine(inDir, "reference"));

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Focus Docs\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"), "# Home\n\nHome page.\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "guides", "install.md"),
                "---\ntitle: Install\n---\n\n# Install\n\nOriginal install text.\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "reference", "api.md"),
                "---\ntitle: API\n---\n\n# API\n\nOriginal reference text.\n");

            // Full build first — focus mode reuses what this produces.
            await new SiteBuilder(inDir, outDir).BuildAsync();

            // Edit both pages, then rebuild with the focus on guides/ only.
            await File.WriteAllTextAsync(Path.Combine(inDir, "guides", "install.md"),
                "---\ntitle: Install\n---\n\n# Install\n\nRebuilt install text.\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "reference", "api.md"),
                "---\ntitle: API\n---\n\n# API\n\nEdited outside the focus path.\n");

            await new SiteBuilder(inDir, outDir, focus: FocusScope.Resolve(
                inDir, Path.Combine(inDir, "guides"), new[] { inDir })).BuildAsync();

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
                var ctx = await browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
                var page = await ctx.NewPageAsync();

                // Keep the render offline-stable: allow only local + data URIs.
                await page.RouteAsync("**/*", async route =>
                {
                    var u = route.Request.Url;
                    if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:"))
                        await route.ContinueAsync();
                    else
                        await route.AbortAsync();
                });

                // The focused page serves its new body.
                try { await page.GotoAsync(baseUrl + "/guides/install.html", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
                catch { /* network idle best-effort */ }
                Assert.That(await page.Locator("h1").First.InnerTextAsync(), Is.EqualTo("Install"));
                Assert.That(await page.ContentAsync(), Does.Contain("Rebuilt install text."));

                // The skipped page still renders — complete page, working sidebar,
                // and the body the previous build wrote (not the later edit).
                try { await page.GotoAsync(baseUrl + "/reference/api.html", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
                catch { /* network idle best-effort */ }

                Assert.That(await page.Locator("h1").First.InnerTextAsync(), Is.EqualTo("API"));

                var content = await page.ContentAsync();
                Assert.That(content, Does.Contain("Original reference text."),
                    "A page outside the focus path keeps the previous build's body.");
                Assert.That(content, Does.Not.Contain("Edited outside the focus path."),
                    "Edits outside the focus path are not published by a focused build.");

                // Navigation still works from the reused page into the rebuilt one.
                var link = page.Locator("a[href='/guides/install']").First;
                Assert.That(await link.CountAsync(), Is.GreaterThan(0),
                    "The reused page's sidebar still links to the rest of the site.");
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
