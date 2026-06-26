using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    /// <summary>
    /// Renders a blog-mode index page in a real browser and asserts the post
    /// cards match the curiosity.ai/resources/blog design: a dark (ink) rounded
    /// 14px tile, fixed 244px tall with 22px padding, the cover image faded
    /// behind the content, the title + a small right arrow on top, and the author
    /// (left) / date (right) in a bottom row split by a hairline rule.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class BlogCardVisualTests
    {
        [Test]
        public async Task BlogCards_MatchCuriosityReferenceDesign()
        {
            // 1. Author a minimal blog-mode site: index (layout: blog) + 2 posts.
            var inDir = Path.Combine(Path.GetTempPath(), "neko-blogcard-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-blogcard-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);
            Directory.CreateDirectory(Path.Combine(inDir, "blog"));

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nmode: blog\nbranding:\n  title: Curiosity\n");

            await File.WriteAllTextAsync(Path.Combine(inDir, "blog", "index.md"),
                "---\ntitle: Blog\nlayout: blog\n---\n\nOur writing.\n");

            await File.WriteAllTextAsync(Path.Combine(inDir, "blog", "first-post.md"),
                "---\ntitle: Curiosity and Capgemini partner to scale industrial AI\nauthor: Curiosity\ndate: May 7, 2026\ncover: /assets/cover.png\n---\n\nBody.\n");

            await File.WriteAllTextAsync(Path.Combine(inDir, "blog", "second-post.md"),
                "---\ntitle: Notes on how we build AI products\nauthor: Curiosity\ndate: April 1, 2026\ncover: /assets/cover.png\n---\n\nBody.\n");

            // 2. Build with the real pipeline (also generates assets/tailwind.css).
            await new SiteBuilder(inDir, outDir).BuildAsync();
            var indexHtml = Path.Combine(outDir, "blog", "index.html");
            Assert.That(File.Exists(indexHtml), "blog/index.html should have been emitted");

            // The arbitrary utilities the new card relies on must be in the sheet.
            // Tailwind escapes brackets/dots in selectors (e.g. `.rounded-\[14px\]`),
            // so strip the backslashes before matching the plain class token.
            var css = (await File.ReadAllTextAsync(Path.Combine(outDir, "assets", "tailwind.css"))).Replace("\\", "");
            foreach (var cls in new[] { ".rounded-[14px]", ".h-[244px]", ".p-[22px]", ".gap-[44px]", ".leading-[1.4]", ".text-[20px]" })
                Assert.That(css, Contains.Substring(cls), $"tailwind.css is missing a rule for {cls}");

            // 3. Serve and open in a headless browser (skip if none available).
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
                var ctx = await browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 1400 } });
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
                try { await page.GotoAsync(baseUrl + "/blog/index.html", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
                catch { /* network idle best-effort */ }
                await page.WaitForTimeoutAsync(200);

                // The blog grid is uniquely identified by its gap-[44px] class.
                var grid = page.Locator("div[class*='gap-[44px]']");
                var cardCount = await grid.Locator("> a").CountAsync();
                Assert.That(cardCount, Is.EqualTo(2), "one card per blog post");

                var card = grid.Locator("> a").First;

                // --- Card frame: dark ink tile, 14px radius, 244px tall, 22px pad. ---
                async Task<string> S(ILocator l, string prop) =>
                    await l.EvaluateAsync<string>($"(el) => getComputedStyle(el).{prop}");

                Assert.That(await S(card, "display"), Is.EqualTo("flex"));
                Assert.That(await S(card, "flexDirection"), Is.EqualTo("column"));
                Assert.That(await S(card, "justifyContent"), Is.EqualTo("space-between"));
                Assert.That(await S(card, "position"), Is.EqualTo("relative"));
                Assert.That(await S(card, "overflow"), Does.Contain("hidden"));
                Assert.That(await S(card, "borderTopLeftRadius"), Is.EqualTo("14px"));
                Assert.That(await S(card, "height"), Is.EqualTo("244px"));
                Assert.That(await S(card, "paddingTop"), Is.EqualTo("22px"));
                Assert.That(await S(card, "paddingLeft"), Is.EqualTo("22px"));
                // Ink background = curiosity #1f1f1f.
                Assert.That(await S(card, "backgroundColor"), Is.EqualTo("rgb(31, 31, 31)"));
                Assert.That(await S(card, "textDecorationLine"), Is.EqualTo("none"));

                // --- Cover image: faded (0.3), cover-fit, absolutely behind content. ---
                var img = card.Locator("img").First;
                Assert.That(await img.CountAsync(), Is.EqualTo(1), "card has a cover image");
                Assert.That(await S(img, "position"), Is.EqualTo("absolute"));
                Assert.That(await S(img, "objectFit"), Is.EqualTo("cover"));
                Assert.That(double.Parse(await S(img, "opacity"), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(0.3).Within(0.001));

                // --- Title: light (#f1f1f1), 16px, medium weight, 1.4 line-height. ---
                var title = card.Locator("h3").First;
                Assert.That((await title.InnerTextAsync()).Trim(), Is.EqualTo("Curiosity and Capgemini partner to scale industrial AI"));
                Assert.That(await S(title, "color"), Is.EqualTo("rgb(241, 241, 241)"));
                Assert.That(await S(title, "fontSize"), Is.EqualTo("16px"));
                Assert.That(await S(title, "fontWeight"), Is.EqualTo("500"));

                // --- Small right arrow icon, top-right, same light colour (rotates to
                // up-right on hover). ---
                var arrow = card.Locator("i.fi-rr-arrow-small-right").First;
                Assert.That(await arrow.CountAsync(), Is.EqualTo(1), "card has a small right arrow icon");
                Assert.That(await S(arrow, "color"), Is.EqualTo("rgb(241, 241, 241)"));

                // --- Footer row: hairline top rule, author left / date right. ---
                var footer = card.Locator("h3").Locator("xpath=../following-sibling::div[1]");
                Assert.That(await S(footer, "justifyContent"), Is.EqualTo("space-between"));
                Assert.That(await S(footer, "borderTopWidth"), Is.EqualTo("1px"));
                Assert.That(await S(footer, "borderTopColor"), Is.EqualTo("rgb(241, 241, 241)"));
                var spans = footer.Locator("span");
                Assert.That(await spans.CountAsync(), Is.EqualTo(2), "footer shows author + date");
                Assert.That((await spans.Nth(0).InnerTextAsync()).Trim(), Is.EqualTo("Curiosity"));
                Assert.That((await spans.Nth(1).InnerTextAsync()).Trim(), Is.EqualTo("May 7, 2026"));

                await ctx.CloseAsync();
            }
            finally
            {
                await browser!.CloseAsync();
                pw!.Dispose();
                try { Directory.Delete(inDir, true); } catch { }
                try { Directory.Delete(outDir, true); } catch { }
            }
        }

        /// <summary>Minimal static file server for the built site.</summary>
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
