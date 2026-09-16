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
    /// Every navigation in a Neko site is a full page load, so the sidebar is rebuilt
    /// from scratch on each click. These tests drive a real browser over a sidebar
    /// taller than the viewport and check the reader never has to hunt for their place:
    /// the entry for the page just opened is always on screen, and a sidebar that did
    /// not change keeps the exact offset it had on the previous page.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class SidebarScrollPlaywrightTests
    {
        [Test]
        public async Task ClickingASiblingEntry_KeepsTheSidebarWhereItWas()
        {
            await WithSiteAsync(async (page, baseUrl) =>
            {
                await GotoAsync(page, baseUrl + "/beta/page-10");

                // Put the active entry mid-pane, the way a reader scrolling the tree would.
                await page.EvaluateAsync("() => document.querySelector('#sidebar-list a.bg-primary-50').scrollIntoView({ block: 'center' })");
                await SettleAsync(page);
                var before = await ScrollTopAsync(page);
                var offsetBefore = await ActiveOffsetAsync(page);

                await page.ClickAsync("#sidebar-list a[href='/beta/page-11']");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await SettleAsync(page);

                Assert.That(await ActiveTextAsync(page), Is.EqualTo("Page 11"));
                Assert.That(await ScrollTopAsync(page), Is.EqualTo(before).Within(60),
                    "an unchanged sidebar should keep its scroll offset across a navigation");
                Assert.That(await ActiveOffsetAsync(page), Is.EqualTo(offsetBefore).Within(60),
                    "the entry for the new page should sit where its neighbour just was");
                Assert.That(await ActiveVisibleAsync(page), Is.True);
            });
        }

        [Test]
        public async Task StaleSavedPosition_StillShowsTheCurrentPagesEntry()
        {
            await WithSiteAsync(async (page, baseUrl) =>
            {
                // A position saved against some other sidebar (sub-projects share a
                // storage key when they share a branding title) must not be trusted.
                await GotoAsync(page, baseUrl + "/beta/page-10");
                await page.EvaluateAsync(@"() => {
                    const key = Object.keys(sessionStorage).find(k => k.endsWith('-sidebar-scroll'));
                    sessionStorage.setItem(key, JSON.stringify({ top: 4000, sig: 'a-different-sidebar' }));
                }");

                await GotoAsync(page, baseUrl + "/gamma/page-6");

                Assert.That(await ActiveTextAsync(page), Is.EqualTo("Page 6"));
                Assert.That(await ScrollTopAsync(page), Is.Not.EqualTo(4000).Within(1),
                    "a position from a sidebar that no longer matches should be discarded");
                Assert.That(await ActiveVisibleAsync(page), Is.True,
                    "and discarding it must not leave the reader at the top of the tree");
            });
        }

        [Test]
        public async Task ColdVisitToADeepPage_ScrollsItsEntryIntoView()
        {
            await WithSiteAsync(async (page, baseUrl) =>
            {
                // No saved position at all, as when arriving from search or an external link.
                await GotoAsync(page, baseUrl + "/gamma/page-18");

                Assert.That(await ActiveTextAsync(page), Is.EqualTo("Page 18"));
                Assert.That(await ScrollTopAsync(page), Is.GreaterThan(0),
                    "the sidebar should not sit at the top with the current entry far below");
                Assert.That(await ActiveVisibleAsync(page), Is.True);
            });
        }

        [Test]
        public async Task CollapsedSectionsAreAppliedBeforeTheSidebarIsPositioned()
        {
            await WithSiteAsync(async (page, baseUrl) =>
            {
                await GotoAsync(page, baseUrl + "/gamma/page-18");

                // Collapse every section. The saved state shrinks the tree on the next
                // load, so a position restored before the sections were applied would
                // be measured against a tree that no longer exists.
                await page.EvaluateAsync(@"() => {
                    const key = Object.keys(localStorage).find(k => k.endsWith('-sidebar-sections'));
                    const state = {};
                    document.querySelectorAll('details[data-section-key]').forEach(d => { state[d.getAttribute('data-section-key')] = false; });
                    localStorage.setItem(key, JSON.stringify(state));
                }");

                await GotoAsync(page, baseUrl + "/gamma/page-20");

                Assert.That(await ActiveTextAsync(page), Is.EqualTo("Page 20"));
                Assert.That(await ActiveVisibleAsync(page), Is.True,
                    "the current page's section is re-opened, and the sidebar is positioned on it");
            });
        }

        // The sidebar entry for the current page, as the highlighter marks it.
        private const string ActiveSelector = "#sidebar-list a.bg-primary-50";

        private static Task<string> ActiveTextAsync(IPage page) =>
            page.EvaluateAsync<string>($"() => {{ const a = document.querySelector('{ActiveSelector}'); return a ? a.textContent.trim() : null; }}");

        private static Task<double> ScrollTopAsync(IPage page) =>
            page.EvaluateAsync<double>("() => document.getElementById('sidebar').scrollTop");

        // Distance from the top of the sidebar to the active entry.
        private static Task<double> ActiveOffsetAsync(IPage page) =>
            page.EvaluateAsync<double>($@"() => {{
                const sb = document.getElementById('sidebar');
                const a = document.querySelector('{ActiveSelector}');
                return a.getBoundingClientRect().top - sb.getBoundingClientRect().top;
            }}");

        // Visible means fully inside the sidebar's scroll port *and* clear of the
        // filter box, which is sticky over the top of the list.
        private static Task<bool> ActiveVisibleAsync(IPage page) =>
            page.EvaluateAsync<bool>($@"() => {{
                const sb = document.getElementById('sidebar');
                const a = document.querySelector('{ActiveSelector}');
                if (!a) return false;
                const bar = document.getElementById('sidebar-filter-bar');
                const sbRect = sb.getBoundingClientRect();
                const top = bar ? bar.getBoundingClientRect().bottom : sbRect.top;
                const r = a.getBoundingClientRect();
                return r.top >= top && r.bottom <= sbRect.bottom;
            }}");

        private static async Task GotoAsync(IPage page, string url)
        {
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await SettleAsync(page);
        }

        private static async Task SettleAsync(IPage page)
        {
            await page.EvaluateAsync("() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))");
            await page.WaitForTimeoutAsync(120);
        }

        /// <summary>
        /// Builds a site whose sidebar is several screens tall (three sections of
        /// twenty-odd pages), serves it, and runs <paramref name="body"/> against it.
        /// </summary>
        private static async Task WithSiteAsync(Func<IPage, string, Task> body)
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-sbscroll-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-sbscroll-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Scroll Docs\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"), "# Home\n\nHome page.\n");

            foreach (var section in new[] { "alpha", "beta", "gamma" })
            {
                var dir = Path.Combine(inDir, section);
                Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(Path.Combine(dir, "index.yml"),
                    $"label: {char.ToUpper(section[0]) + section.Substring(1)}\n");
                for (int i = 1; i <= 24; i++)
                {
                    await File.WriteAllTextAsync(Path.Combine(dir, $"page-{i}.md"),
                        $"---\ntitle: Page {i}\n---\n\n# Page {i}\n\nBody of {section} page {i}.\n");
                }
            }

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

                await body(page, baseUrl);
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
