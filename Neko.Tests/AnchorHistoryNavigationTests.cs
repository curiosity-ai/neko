using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// The content pane (<c>#main-scroll</c>) is the scroller, not the document, so a
    /// browser back/forward step between two anchors of the same page has no document
    /// offset to restore and used to leave the reader wherever the previous anchor put
    /// them. These tests drive a real browser to check that traversing anchor history
    /// re-aligns the pane with the URL fragment.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class AnchorHistoryNavigationTests
    {
        [Test]
        public async Task BackAndForward_AcrossAnchors_ScrollsPaneToTheFragment()
        {
            await WithAnchorPage(async p =>
            {
                var thirdTop = await ExpectedTop(p, "section-3");
                var seventhTop = await ExpectedTop(p, "section-7");

                await ClickAnchor(p, "section-3");
                Assert.That(await PaneTop(p), Is.EqualTo(thirdTop).Within(40), "clicking an anchor should scroll the pane");

                await ClickAnchor(p, "section-7");
                Assert.That(await PaneTop(p), Is.EqualTo(seventhTop).Within(40), "second anchor click should scroll the pane");

                await p.GoBackAsync();
                await SettleAsync(p);
                Assert.That(p.Url, Does.EndWith("#section-3"));
                Assert.That(await PaneTop(p), Is.EqualTo(thirdTop).Within(40), "back should re-align the pane with #section-3");

                await p.GoForwardAsync();
                await SettleAsync(p);
                Assert.That(p.Url, Does.EndWith("#section-7"));
                Assert.That(await PaneTop(p), Is.EqualTo(seventhTop).Within(40), "forward should re-align the pane with #section-7");
            });
        }

        [Test]
        public async Task BackPastTheFirstAnchor_ReturnsToWhereTheReaderWas()
        {
            await WithAnchorPage(async p =>
            {
                // Read a little way down the page by hand, then jump to an anchor: the
                // fragment-less entry has no heading to align to, so stepping back to it
                // must restore the offset the reader had.
                await p.EvaluateAsync("() => document.getElementById('main-scroll').scrollTo({ top: 600 })");
                await SettleAsync(p);

                await ClickAnchor(p, "section-7");
                Assert.That(await PaneTop(p), Is.GreaterThan(1000), "the anchor click should move the pane well past 600");

                await p.GoBackAsync();
                await SettleAsync(p);
                Assert.That(p.Url, Does.Not.Contain("#"));
                Assert.That(await PaneTop(p), Is.EqualTo(600).Within(40), "back should return to the reader's own position");
            });
        }

        private static Task<double> PaneTop(IPage p) =>
            p.EvaluateAsync<double>("() => document.getElementById('main-scroll').scrollTop");

        // Where the pane ends up when the fragment is honoured: the heading's own
        // offset within the pane, clamped to the pane's maximum scroll.
        private static Task<double> ExpectedTop(IPage p, string id) => p.EvaluateAsync<double>(
            @"(id) => { const pane = document.getElementById('main-scroll');
                        const el = document.getElementById(id);
                        const top = el.getBoundingClientRect().top - pane.getBoundingClientRect().top + pane.scrollTop;
                        return Math.min(top, pane.scrollHeight - pane.clientHeight); }", id);

        // Click the hash link Neko prepends to every heading, the way a reader would,
        // so each jump lands its own history entry.
        private static async Task ClickAnchor(IPage p, string id)
        {
            await p.EvalOnSelectorAsync($"h2#{id} a[href='#{id}']", "a => a.click()");
            await SettleAsync(p);
        }

        // The pane scrolls smoothly, so give any animation time to finish before
        // reading scrollTop.
        private static Task SettleAsync(IPage p) => p.WaitForTimeoutAsync(900);

        /// <summary>
        /// Builds a single long page with anchored H2s, serves it, and hands the caller
        /// a browser page pointed at it.
        /// </summary>
        private static async Task WithAnchorPage(Func<IPage, Task> body)
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-anchor-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-anchor-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Anchor Docs\n");

            var markdown = new StringBuilder("# Anchors\n\nIntro paragraph.\n\n");
            for (int i = 1; i <= 14; i++)
            {
                markdown.AppendLine($"## Section {i}");
                markdown.AppendLine();
                for (int b = 0; b < 12; b++) markdown.AppendLine($"Body text {i}.{b} — filler to make the page scroll.\n");
            }
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"), markdown.ToString());

            await new SiteBuilder(inDir, outDir).BuildAsync();

            using var server = new StaticServer(outDir);
            var baseUrl = server.Start();

            IPlaywright pw = null;
            IBrowser browser = null;
            try
            {
                pw = await Playwright.CreateAsync();
                var launch = new BrowserTypeLaunchOptions { Headless = true };
                // Environments that ship a preinstalled Chromium (a different build than
                // the one this Playwright version downloads) point at it with this variable.
                var exe = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM");
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) launch.ExecutablePath = exe;
                browser = await pw.Chromium.LaunchAsync(launch);
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

                await page.GotoAsync($"{baseUrl}/index.html", new() { WaitUntil = WaitUntilState.Load });
                await body(page);
            }
            finally
            {
                await browser!.CloseAsync();
                pw!.Dispose();
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
                        if (File.Exists(file))
                        {
                            ctx.Response.ContentType = file.EndsWith(".css") ? "text/css"
                                : file.EndsWith(".js") ? "text/javascript"
                                : file.EndsWith(".json") ? "application/json"
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
