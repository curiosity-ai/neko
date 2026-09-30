using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Renders the Curiosity slide templates deck
    /// (Neko.Documentation/presentations/decks/curiosity-templates.md), which uses
    /// every slide layout and component the curiosity theme brought, in a real
    /// browser — once in the curiosity theme and once re-themed to midnight with
    /// the `--theme` override — and walks every slide:
    ///
    /// <list type="bullet">
    ///   <item>curiosity: the slide is a 16:9 canvas that fits the window at several
    ///   sizes, and nothing on it (bar its generated art) spills past its edges;</item>
    ///   <item>midnight: nothing scrolls sideways, and the components still render;</item>
    ///   <item>both: the generated art draws, and no script error is raised.</item>
    /// </list>
    ///
    /// Skipped when a Playwright browser is unavailable (NEKO_TEST_CHROMIUM points
    /// at a preinstalled Chromium).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class PresentationThemePlaywrightTests
    {
        private readonly Dictionary<string, string> _out = new();

        [OneTimeSetUp]
        public void Build()
        {
            var deck = File.ReadAllText(FindDeck());
            foreach (var theme in new[] { "curiosity", "midnight" })
            {
                var input = Path.Combine(Path.GetTempPath(), "neko-theme-in-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path.Combine(input, "decks"));
                File.WriteAllText(Path.Combine(input, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
                File.WriteAllText(Path.Combine(input, "index.md"), "# Home\n");
                // fonts: none keeps the test off the network; the layout is what is checked.
                File.WriteAllText(Path.Combine(input, "decks", "templates.md"), deck.Replace("  theme: curiosity\n", "  theme: curiosity\n  fonts: none\n"));

                var output = Path.Combine(Path.GetTempPath(), "neko-theme-out-" + Guid.NewGuid().ToString("N"));
                PresentationOptions.ThemeOverride = theme == "midnight" ? "midnight" : null;
                try { new SiteBuilder(input, output).BuildAsync().GetAwaiter().GetResult(); }
                finally { PresentationOptions.ThemeOverride = null; }
                _out[theme] = output;
            }
        }

        private static string FindDeck()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Neko.Documentation"))) dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "repository root not found");
            return Path.Combine(dir!.FullName, "Neko.Documentation", "presentations", "decks", "curiosity-templates.md");
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        [TestCase(900, 1200)]
        public async Task Curiosity_EverySlideIsAFittedCanvas_AndNothingSpillsOffIt(int width, int height)
        {
            using var server = new StaticServer(_out["curiosity"]);
            var baseUrl = server.Start();
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
                var errors = new List<string>();
                page.PageError += (_, e) => { if (!IsOfflineLibrary(e)) errors.Add(e); };
                await page.GotoAsync($"{baseUrl}/decks/templates", new() { WaitUntil = WaitUntilState.NetworkIdle });

                Assert.That(await page.GetAttributeAsync("html", "data-deck-theme"), Is.EqualTo("curiosity"));
                var count = await page.Locator(".deck-slide").CountAsync();
                Assert.That(count, Is.GreaterThanOrEqualTo(20));

                for (int i = 1; i <= count; i++)
                {
                    await page.EvaluateAsync($"location.hash = '#slide-{i}'");
                    await page.WaitForTimeoutAsync(120);
                    var r = await page.EvaluateAsync<double[]>(@"() => {
                        const s = document.querySelector('.deck-slide.is-on');
                        const b = s.getBoundingClientRect();
                        let spill = 0;
                        for (const el of s.querySelectorAll('*')) {
                            if (el.closest('.deck-art')) continue;
                            const e = el.getBoundingClientRect();
                            if (!e.width || !e.height) continue;
                            if (e.right > b.right + 2 || e.bottom > b.bottom + 2 || e.left < b.left - 2 || e.top < b.top - 2) spill++;
                        }
                        return [b.width, b.height, b.left, b.top, spill, innerWidth, innerHeight];
                    }");
                    var (w, h, left, top, spill, vw, vh) = (r[0], r[1], r[2], r[3], r[4], r[5], r[6]);
                    Assert.That(w / h, Is.EqualTo(16.0 / 9.0).Within(0.02), $"slide {i} is 16:9");
                    Assert.That(w, Is.LessThanOrEqualTo(vw + 1), $"slide {i} fits the width");
                    Assert.That(h, Is.LessThanOrEqualTo(vh + 1), $"slide {i} fits the height");
                    Assert.That(Math.Abs(left - (vw - w) / 2), Is.LessThan(2), $"slide {i} is centred");
                    Assert.That(Math.Min(Math.Abs(w - vw), Math.Abs(h - vh)), Is.LessThan(2), $"slide {i} fills the window on one axis");
                    Assert.That(spill, Is.EqualTo(0), $"slide {i}: {spill} elements spill past the canvas");
                }

                Assert.That(errors, Is.Empty);
            }
            finally { await CloseAsync(pw, browser); }
        }

        [TestCase("curiosity")]
        [TestCase("midnight")]
        public async Task Components_RenderInTheme(string theme)
        {
            using var server = new StaticServer(_out[theme]);
            var baseUrl = server.Start();
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
                var errors = new List<string>();
                page.PageError += (_, e) => { if (!IsOfflineLibrary(e)) errors.Add(e); };
                await page.GotoAsync($"{baseUrl}/decks/templates", new() { WaitUntil = WaitUntilState.NetworkIdle });
                Assert.That(await page.GetAttributeAsync("html", "data-deck-theme"), Is.EqualTo(theme));
                Assert.That(await page.Locator("link[href$='presentation-curiosity.css']").CountAsync(), Is.EqualTo(theme == "curiosity" ? 1 : 0));

                // Each component shows on its slide, with a real size.
                var selectors = new[] { ".deck-art svg", ".deck-agenda li", ".deck-stat-value", ".deck-steps .deck-step",
                                        ".deck-art-block svg", ".deck-milestone", ".deck-quote blockquote", ".deck-compare table", ".deck-box-label" };
                foreach (var selector in selectors)
                {
                    var slide = await page.EvaluateAsync<int>($"() => [...document.querySelectorAll('.deck-slide')].findIndex(s => s.querySelector(\"{selector}\")) + 1");
                    Assert.That(slide, Is.GreaterThan(0), $"{selector} is in the deck");
                    await page.EvaluateAsync($"location.hash = '#slide-{slide}'");
                    await page.WaitForTimeoutAsync(150);
                    var box = await page.Locator($".deck-slide.is-on {selector}").First.BoundingBoxAsync();
                    Assert.That(box, Is.Not.Null, $"{selector} is visible on slide {slide}");
                    Assert.That(box!.Width * box.Height, Is.GreaterThan(0), $"{selector} has a size on slide {slide}");
                }

                if (theme == "midnight")
                {
                    var count = await page.Locator(".deck-slide").CountAsync();
                    for (int i = 1; i <= count; i++)
                    {
                        await page.EvaluateAsync($"location.hash = '#slide-{i}'");
                        await page.WaitForTimeoutAsync(80);
                        var sideways = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > innerWidth + 1");
                        Assert.That(sideways, Is.False, $"slide {i} scrolls sideways");
                    }
                }

                Assert.That(errors, Is.Empty);
            }
            finally { await CloseAsync(pw, browser); }
        }

        // Mermaid, KaTeX and highlight.js load from CDNs; a test machine without
        // the network raises their "not defined" errors, which are not the deck's.
        private static bool IsOfflineLibrary(string error)
            => error.Contains("mermaid") || error.Contains("katex") || error.Contains("hljs") || error.Contains("renderMathInElement");

        private static async Task<(IPlaywright, IBrowser)> LaunchAsync()
        {
            try
            {
                var pw = await Playwright.CreateAsync();
                var launch = new BrowserTypeLaunchOptions { Headless = true };
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

        private static async Task CloseAsync(IPlaywright pw, IBrowser browser)
        {
            if (browser != null) await browser.CloseAsync();
            pw?.Dispose();
        }

        private sealed class StaticServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private readonly string _root;
            public StaticServer(string root) { _root = root; }

            public string Start()
            {
                int port = 9900 + new Random().Next(800);
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
                            ctx.Response.ContentType = Path.GetExtension(file) switch
                            {
                                ".html" => "text/html", ".css" => "text/css", ".js" => "text/javascript",
                                ".json" => "application/json", ".woff2" => "font/woff2", ".svg" => "image/svg+xml",
                                _ => "application/octet-stream",
                            };
                            var bytes = await File.ReadAllBytesAsync(file);
                            await ctx.Response.OutputStream.WriteAsync(bytes);
                        }
                        else ctx.Response.StatusCode = 404;
                    }
                    catch { }
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
