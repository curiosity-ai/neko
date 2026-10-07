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

            Directory.CreateDirectory(Path.Combine(inputDir, "assets"));
            // A tiny real PNG so the brand logo actually loads and contributes width.
            File.WriteAllBytes(Path.Combine(inputDir, "assets", "logo.png"), Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="));

            File.WriteAllText(Path.Combine(inputDir, "decks", "talk.md"), @"---
title: The talk
presentation:
  eyebrow: Demo deck
  logo: /assets/logo.png
  logoText: Built with Neko
  logoLink: https://neko.curiosity.ai
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

            File.WriteAllText(Path.Combine(inputDir, "decks", "locked-brand.md"), @"---
title: Locked but branded
password: hunter2
presentation:
  logoText: Built with Neko
---

# Hidden until unlocked
");

            File.WriteAllText(Path.Combine(inputDir, "decks", "offline.md"), @"---
title: Everything inline
presentation:
  logo: /assets/logo.png
---

# Everything inline :rocket:

---

## A diagram

```mermaid
graph LR
  A --> B
```

---

## Math and code

$$
\int_0^1 x^2 \, dx = \frac{1}{3}
$$

```csharp
var x = 1;
```
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
        public async Task BrandMark_SitsInTheCornerOnEverySlide_WithoutCoveringTheCounter()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
                await page.GotoAsync($"{baseUrl}/decks/talk", new() { WaitUntil = WaitUntilState.NetworkIdle });

                var brand = page.Locator(".deck-brand");
                await Assertions.Expect(brand).ToBeVisibleAsync();
                await Assertions.Expect(brand).ToHaveAttributeAsync("href", "https://neko.curiosity.ai");

                for (var slide = 1; slide <= 3; slide++)
                {
                    var brandBox = await brand.BoundingBoxAsync();
                    var countBox = await page.Locator("#deck-count").BoundingBoxAsync();
                    Assert.That(brandBox, Is.Not.Null);
                    Assert.That(countBox, Is.Not.Null);

                    // Pinned to the bottom-right corner…
                    Assert.That(brandBox!.X + brandBox.Width, Is.GreaterThan(1280 * 0.75),
                        $"slide {slide}: the mark hugs the right edge");
                    Assert.That(brandBox.Y, Is.GreaterThan(800 * 0.85),
                        $"slide {slide}: the mark hugs the bottom edge");

                    // …and the control bar reserves room, so the counter sits to its left.
                    Assert.That(countBox!.X + countBox.Width, Is.LessThanOrEqualTo(brandBox.X),
                        $"slide {slide}: the slide counter must not run under the brand mark");

                    if (slide < 3)
                    {
                        await page.Keyboard.PressAsync("ArrowRight");
                        await page.WaitForTimeoutAsync(300);
                    }
                }
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task BrandMark_ShowsOnALockedDeckBeforeItIsUnlocked()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
                await page.GotoAsync($"{baseUrl}/decks/locked-brand", new() { WaitUntil = WaitUntilState.NetworkIdle });

                await Assertions.Expect(page.Locator("#password-input")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator(".deck-brand")).ToBeVisibleAsync();
                await Assertions.Expect(page.Locator(".deck-brand-text")).ToHaveTextAsync("Built with Neko");
            }
            finally
            {
                await CloseAsync(pw, browser);
            }
        }

        [Test]
        public async Task Deck_OpensAsASingleFile_WithTheNetworkOff()
        {
            // The deck alone, away from the site: no assets folder, no CDN, no font host.
            var lone = Path.Combine(Path.GetTempPath(), "neko-deck-lone-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(lone);
            var file = Path.Combine(lone, "offline.html");
            File.Copy(Path.Combine(_outDir, "decks", "offline.html"), file);

            var (pw, browser) = await LaunchAsync();
            try
            {
                var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
                var requested = new System.Collections.Generic.List<string>();
                await context.RouteAsync("**/*", route =>
                {
                    var url = route.Request.Url;
                    if (url.StartsWith("file:") || url.StartsWith("data:") || url.StartsWith("blob:")) return route.ContinueAsync();
                    requested.Add(url);
                    return route.AbortAsync();
                });
                var page = await context.NewPageAsync();
                var errors = new System.Collections.Generic.List<string>();
                page.PageError += (_, e) => errors.Add(e);

                await page.GotoAsync(new Uri(file).AbsoluteUri);
                await page.WaitForSelectorAsync(".mermaid svg", new() { State = WaitForSelectorState.Attached, Timeout = 15000 });

                Assert.That(requested, Is.Empty, "the deck requests nothing");
                Assert.That(errors, Is.Empty);
                Assert.That(await page.Locator(".katex").CountAsync(), Is.GreaterThan(0), "KaTeX is inlined");
                var state = await page.EvaluateAsync<string>(@"async () => {
                    await document.fonts.ready;
                    const loaded = f => [...document.fonts].some(x => x.family.replace(/""/g, '') === f && x.status === 'loaded');
                    const emoji = getComputedStyle(document.querySelector('.em')).backgroundImage;
                    const logo = document.querySelector('.deck-brand img');
                    return [loaded('Archivo'), loaded('uicons-regular-rounded'), emoji.startsWith('url(""data:image/svg+xml'), logo.naturalWidth > 0].join(',');
                }");
                Assert.That(state, Is.EqualTo("true,true,true,true"), "theme font, icon font, emoji and logo all load from the file");
            }
            finally
            {
                await CloseAsync(pw, browser);
                try { Directory.Delete(lone, true); } catch { }
            }
        }

        [Test]
        public async Task DownloadButton_ExportsTheDeckAsAnEditablePptx()
        {
            Assert.That(File.Exists(Path.Combine(_outDir, "assets", "pptxgen.bundle.js")), Is.True,
                "PptxGenJS ships with the site");
            Assert.That(File.Exists(Path.Combine(_outDir, "assets", "presentation-pptx.js")), Is.True);

            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            var (pw, browser) = await LaunchAsync();
            try
            {
                var context = await browser.NewContextAsync(new() { AcceptDownloads = true, ViewportSize = new() { Width = 1280, Height = 800 } });
                var page = await context.NewPageAsync();
                var scripts = new System.Collections.Generic.List<string>();
                page.Request += (_, request) => { if (request.ResourceType == "script") scripts.Add(request.Url); };

                await page.GotoAsync($"{baseUrl}/decks/talk", new() { WaitUntil = WaitUntilState.NetworkIdle });
                Assert.That(scripts, Is.Empty, "the deck is self-contained: every script is inline");
                Assert.That(await page.EvaluateAsync<bool>("() => typeof window.nekoDeckExportPptx === 'undefined'"), Is.True,
                    "the exporter only runs on demand");

                // Export from the middle of the deck: every slide is exported, not
                // just the one on screen, and the reader is left where they were.
                await page.Keyboard.PressAsync("ArrowRight");
                await page.WaitForTimeoutAsync(300);

                var download = await page.RunAndWaitForDownloadAsync(() => page.ClickAsync("#deck-download"), new() { Timeout = 60000 });
                Assert.That(download.SuggestedFilename, Is.EqualTo("the-talk.pptx"));
                Assert.That(scripts, Is.Empty, "the bundle and exporter are inlined in the deck, not fetched");

                var path = Path.Combine(Path.GetTempPath(), "neko-deck-" + Guid.NewGuid().ToString("N") + ".pptx");
                await download.SaveAsAsync(path);

                using (var zip = System.IO.Compression.ZipFile.OpenRead(path))
                {
                    string Read(string name)
                    {
                        var entry = zip.GetEntry(name);
                        Assert.That(entry, Is.Not.Null, name + " is in the package");
                        using var reader = new StreamReader(entry!.Open());
                        return reader.ReadToEnd();
                    }

                    var presentation = Read("ppt/presentation.xml");
                    Assert.That(presentation, Does.Contain("cx=\"12192000\" cy=\"6858000\""), "16:9 widescreen slides");

                    Assert.That(zip.GetEntry("ppt/slides/slide3.xml"), Is.Not.Null);
                    Assert.That(zip.GetEntry("ppt/slides/slide4.xml"), Is.Null, "one PowerPoint slide per deck slide");

                    var first = Read("ppt/slides/slide1.xml");
                    Assert.That(first, Does.Contain("Opening slide"), "headings are exported as editable text");
                    Assert.That(first, Does.Contain("The claim this deck makes."));
                    Assert.That(first, Does.Contain("Demo deck"), "the eyebrow comes along");
                    Assert.That(first, Does.Contain("Built with Neko"), "the brand mark is on every slide");

                    var second = Read("ppt/slides/slide2.xml");
                    Assert.That(second, Does.Contain("Second slide"));
                    Assert.That(second, Does.Contain("The value."));

                    var third = Read("ppt/slides/slide3.xml");
                    Assert.That(third, Does.Contain("Left"));
                    Assert.That(third, Does.Contain("<a:buChar"), "list items keep their bullets");
                }
                File.Delete(path);

                Assert.That(await page.Locator("#deck-count").TextContentAsync(), Is.EqualTo("2 / 3"));
                Assert.That(await page.Locator("#deck-download").IsEnabledAsync(), Is.True, "the button is ready for another export");
                Assert.That(await page.Locator("iframe").CountAsync(), Is.EqualTo(0), "the off-screen layout frame is cleaned up");
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
                // The pages link CDN scripts (Mermaid, KaTeX, panzoom). Resolve nothing but localhost, so a
                // machine without internet access fails those requests at once instead of hanging the
                // page's parse until the test reads an empty body.
                var launch = new BrowserTypeLaunchOptions { Headless = true, Args = new[] { "--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE localhost" } };
                // Environments that ship a preinstalled Chromium (a different build than
                // the one this Playwright version downloads) point at it with this variable.
                var exe = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM");
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) launch.ExecutablePath = exe;
                var browser = await pw.Chromium.LaunchAsync(launch);
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
