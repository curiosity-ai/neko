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
    /// Drives presentation mode in a real browser: builds a small site with a public
    /// deck, a password-protected deck and a page that embeds one with
    /// <c>[!deck]</c>, then checks that only one slide is on screen at a time, that
    /// the keyboard and the control bar move between slides, that the back control
    /// returns to the page the reader came from, that an embedded deck scales itself
    /// down and drops its own back control, and that a locked deck ships nothing
    /// readable until it is unlocked.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class PresentationPlaywrightTests
    {
        private string _outDir;

        [SetUp]
        public void Setup()
        {
            var inputDir = Path.Combine(Path.GetTempPath(), "neko-deck-pw-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(inputDir, "decks"));

            File.WriteAllText(Path.Combine(inputDir, "neko.yml"), @"
url: https://example.com
branding:
  title: Demo
");

            File.WriteAllText(Path.Combine(inputDir, "index.md"), @"---
title: Home
---
# Home

[!deck link=""/decks/talk"" title=""The talk"" description=""Three slides.""]
");

            File.WriteAllText(Path.Combine(inputDir, "decks", "talk.md"), @"---
title: The talk
presentation:
  eyebrow: Demo deck
---

# Opening slide

The lead paragraph.

> The claim this deck makes.

--- {eyebrow=""The middle"" accent=""rose""}

## Second slide

Key
:   The value.

::: note {tone=""limit""}
A caveat.
:::

--- {eyebrow=""The end""}

## Third slide

:::: cols
::: box {title=""Left""}
- point
:::

::: box {title=""Right"" tone=""warn""}
- point
:::
::::
");

            File.WriteAllText(Path.Combine(inputDir, "decks", "locked.md"), @"---
title: Locked deck
password: hunter2
presentation: true
---

# A distinctive locked heading

Body of the locked deck.

---

## The second locked slide
");

            _outDir = Path.Combine(Path.GetTempPath(), "neko-deck-pw-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(inputDir, _outDir).BuildAsync().GetAwaiter().GetResult();
        }

        [Test]
        public async Task Deck_ShowsOneSlideAtATime_AndNavigatesByKeyboardAndControls()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
                await page.GotoAsync($"{baseUrl}/decks/talk", new() { WaitUntil = WaitUntilState.NetworkIdle });

                Assert.That(await page.Locator(".deck-slide").CountAsync(), Is.EqualTo(3));
                Assert.That(await page.Locator(".deck-slide:visible").CountAsync(), Is.EqualTo(1),
                    "exactly one slide is on screen");
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("1 / 3"));

                // The first slide opens on an H1, so it is laid out as a title slide.
                Assert.That(await page.Locator(".deck-slide.is-on").GetAttributeAsync("data-layout"), Is.EqualTo("title"));

                // The documentation shell is absent.
                Assert.That(await page.Locator("#sidebar-list").CountAsync(), Is.EqualTo(0));
                Assert.That(await page.Locator("#toc-list").CountAsync(), Is.EqualTo(0));

                await page.Keyboard.PressAsync("ArrowRight");
                await page.WaitForTimeoutAsync(300);
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("2 / 3"));
                Assert.That(await page.Locator(".deck-slide.is-on").GetAttributeAsync("data-accent"), Is.EqualTo("rose"),
                    "the per-slide accent is applied");
                Assert.That(await page.Locator(".deck-slide.is-on .deck-eyebrow").TextContentAsync(),
                    Does.Contain("The middle"));

                await page.ClickAsync("#deck-next");
                await page.WaitForTimeoutAsync(300);
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("3 / 3"));
                Assert.That(await page.Locator("#deck-next").IsDisabledAsync(), Is.True, "the last slide disables next");
                Assert.That(new Uri(page.Url).Fragment, Is.EqualTo("#slide-3"), "the current slide is mirrored into the URL");

                await page.ClickAsync("#deck-prev");
                await page.WaitForTimeoutAsync(300);
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("2 / 3"));

                await page.Keyboard.PressAsync("Home");
                await page.WaitForTimeoutAsync(300);
                Assert.That(await page.Locator("#deck-prev").IsDisabledAsync(), Is.True, "the first slide disables prev");
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        [TestCase("#slide-2")]
        [TestCase("#2")]
        public async Task Deck_OpensAtTheSlideNamedInTheFragment(string fragment)
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync();
                await page.GotoAsync($"{baseUrl}/decks/talk{fragment}", new() { WaitUntil = WaitUntilState.NetworkIdle });

                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("2 / 3"));
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task BackControl_ReturnsToThePageTheReaderCameFrom()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync();
                await page.GotoAsync($"{baseUrl}/index", new() { WaitUntil = WaitUntilState.NetworkIdle });

                // Arrive at the deck the way a reader would: through the card that
                // embeds it, so the browser records a referrer.
                await page.Locator(".neko-deck-card a[href$='/decks/talk']").First.ClickAsync();
                await page.WaitForSelectorAsync("#deck-back");

                var referrer = await page.EvaluateAsync<string>("() => document.referrer");
                Assert.That(referrer, Is.Not.Empty, "following the card link records a referrer");
                Assert.That(await page.Locator("#deck-back").GetAttributeAsync("href"),
                    Is.EqualTo(new Uri(referrer).AbsolutePath),
                    "the back control points at the referring page");

                await page.ClickAsync("#deck-back");
                await page.WaitForSelectorAsync("h1");
                Assert.That(await page.Locator("h1").First.TextContentAsync(), Is.EqualTo("Home"));
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task EmbeddedDeck_ScalesItselfDown_AndDropsItsOwnBackControl()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
                await page.GotoAsync($"{baseUrl}/", new() { WaitUntil = WaitUntilState.NetworkIdle });

                var card = page.Locator(".neko-deck-card");
                await Assertions.Expect(card).ToBeVisibleAsync();

                var stage = card.Locator("div.aspect-video");
                await Assertions.Expect(stage).ToBeVisibleAsync();

                // 16:9 — the aspect ratio of a standard presentation.
                var box = await stage.BoundingBoxAsync();
                Assert.That(box, Is.Not.Null);
                Assert.That(box!.Width / box.Height, Is.EqualTo(16.0 / 9.0).Within(0.02));

                var frame = page.FrameLocator(".neko-deck-card iframe");
                await Assertions.Expect(frame.Locator(".deck-slide.is-on")).ToBeVisibleAsync(new() { Timeout = 15000 });

                var body = frame.Locator("body");
                Assert.That(await body.GetAttributeAsync("data-deck-embedded"), Is.EqualTo("true"));

                Assert.That(await frame.Locator("#deck-back").IsVisibleAsync(), Is.False,
                    "the embedding page already offers the way out");

                var zoom = await frame.Locator("html").EvaluateAsync<string>("el => el.style.zoom");
                Assert.That(double.Parse(zoom, System.Globalization.CultureInfo.InvariantCulture),
                    Is.LessThan(1).And.GreaterThan(0),
                    "a preview renders the deck as a scaled-down miniature");

                // The whole slide fits inside the frame rather than being cropped.
                var slideFits = await frame.Locator(".deck-slide.is-on").EvaluateAsync<bool>(
                    "el => el.getBoundingClientRect().bottom <= window.innerHeight + 2");
                Assert.That(slideFits, Is.True);
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task LockedDeck_ShipsNothingReadable_AndRunsOnceUnlocked()
        {
            var raw = await File.ReadAllTextAsync(Path.Combine(_outDir, "decks", "locked.html"));
            Assert.That(raw, Does.Not.Contain("A distinctive locked heading"));
            Assert.That(raw, Does.Not.Contain("Body of the locked deck."));

            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
                await page.GotoAsync($"{baseUrl}/decks/locked", new() { WaitUntil = WaitUntilState.NetworkIdle });

                await Assertions.Expect(page.Locator("#password-input")).ToBeVisibleAsync();
                Assert.That(await page.Locator(".deck-slide").CountAsync(), Is.EqualTo(0));
                // The chrome that frames the prompt is outside the encrypted payload.
                await Assertions.Expect(page.Locator("#deck-back")).ToBeVisibleAsync();

                await page.FillAsync("#password-input", "hunter2");
                await page.ClickAsync("#password-submit");

                await Assertions.Expect(page.Locator(".deck-slide.is-on")).ToBeVisibleAsync(new() { Timeout = 15000 });
                Assert.That(await page.Locator(".deck-slide").CountAsync(), Is.EqualTo(2));
                Assert.That(await page.Locator(".deck-slide:visible").CountAsync(), Is.EqualTo(1),
                    "the deck runtime wires itself up over the decrypted slides");
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("1 / 2"));

                await page.Keyboard.PressAsync("ArrowRight");
                await page.WaitForTimeoutAsync(300);
                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("2 / 2"));

                Assert.That(await page.TitleAsync(), Does.Contain("A distinctive locked heading"),
                    "the real title is restored from the decrypted H1");
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task WithoutJavaScript_EverySlideIsStillReadable()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var context = await browser.NewContextAsync(new() { JavaScriptEnabled = false });
                var page = await context.NewPageAsync();
                await page.GotoAsync($"{baseUrl}/decks/talk", new() { WaitUntil = WaitUntilState.NetworkIdle });

                Assert.That(await page.Locator(".deck-slide:visible").CountAsync(), Is.EqualTo(3),
                    "with no runtime the deck falls back to a scroll of every slide");
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        private static async Task<(IPlaywright, IBrowser)> LaunchAsync()
        {
            try
            {
                var pw = await Playwright.CreateAsync();
                var browser = await pw.Chromium.LaunchAsync(new() { Headless = true });
                return (pw, browser);
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
                            ctx.Response.ContentType = Path.GetExtension(file) switch
                            {
                                ".html" => "text/html",
                                ".css" => "text/css",
                                ".js" => "text/javascript",
                                ".json" => "application/json",
                                ".woff2" => "font/woff2",
                                _ => "application/octet-stream",
                            };
                            var bytes = await File.ReadAllBytesAsync(file);
                            await ctx.Response.OutputStream.WriteAsync(bytes);
                        }
                        else
                        {
                            ctx.Response.StatusCode = 404;
                        }
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
