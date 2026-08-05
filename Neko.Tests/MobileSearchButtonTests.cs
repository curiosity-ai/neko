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
    /// The documentation header's action row (search box, history, theme toggle) is
    /// <c>hidden md:flex</c>, so on a phone the "Search ⌘K" box disappears — and with
    /// it the only way to reach search, since ⌘K needs a keyboard. These tests drive a
    /// real browser at a phone viewport and check that the compact
    /// <c>#mobile-search-btn</c> icon is visible, opens the same search modal, and
    /// actually returns results — then flip to a desktop viewport and check the icon
    /// gives way to the full search box (no duplicated trigger).
    ///
    /// Skipped when a Playwright browser is unavailable (e.g. no download).
    /// </summary>
    [TestFixture]
    public class MobileSearchButtonTests
    {
        private string _outDir;

        [SetUp]
        public void Setup()
        {
            var inputDir = Path.Combine(Path.GetTempPath(), "neko-msearch-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inputDir);

            File.WriteAllText(Path.Combine(inputDir, "neko.yml"), @"
url: https://example.com
branding:
  title: Demo Docs
layout:
  sidebar: true
  toc: false
");
            File.WriteAllText(Path.Combine(inputDir, "index.md"), @"---
title: Home
---
# Home
Welcome to the demo documentation.
");
            // A page with a distinctive word so a query can prove the modal really
            // searched the index rather than just opening.
            File.WriteAllText(Path.Combine(inputDir, "widgets.md"), @"---
title: Widgets
---
# Widgets
Everything about the zanzibar widget subsystem.
");

            _outDir = Path.Combine(Path.GetTempPath(), "neko-msearch-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(inputDir, _outDir).BuildAsync().GetAwaiter().GetResult();
        }

        [Test]
        public async Task MobileSearchIcon_IsVisibleOnPhone_OpensModal_AndReturnsResults()
        {
            Assert.That(File.Exists(Path.Combine(_outDir, "search.json")), Is.True, "search.json should be built");

            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            IPlaywright pw = null;
            IBrowser browser = null;
            try
            {
                pw = await Playwright.CreateAsync();
                browser = await pw.Chromium.LaunchAsync(LaunchOptions());
            }
            catch (Exception ex)
            {
                pw?.Dispose();
                Assert.Ignore($"Playwright browser unavailable: {ex.Message}");
            }

            try
            {
                // iPhone-sized viewport: below Tailwind's `md` (768px) breakpoint.
                var page = await browser!.NewPageAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
                await BlockExternalRequests(page);

                await page.GotoAsync(baseUrl + "/index.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 15000 });

                var mobileBtn = page.Locator("#mobile-search-btn");
                var desktopBox = page.Locator("header button:has-text('Search')").First;

                Assert.That(await mobileBtn.CountAsync(), Is.EqualTo(1), "the mobile search button should be rendered");
                Assert.That(await mobileBtn.IsVisibleAsync(), Is.True, "the mobile search button should be visible on a phone viewport");
                Assert.That(await desktopBox.IsVisibleAsync(), Is.False, "the wide desktop search box stays hidden on a phone");

                // It sits inside the header, not off-screen, and is a comfortable tap
                // target that doesn't overflow the viewport.
                var box = await mobileBtn.BoundingBoxAsync();
                Assert.That(box, Is.Not.Null, "the mobile search button should have a layout box");
                Assert.That(box!.Width, Is.GreaterThanOrEqualTo(28), "tap target should be reasonably sized");
                Assert.That(box.Height, Is.GreaterThanOrEqualTo(28), "tap target should be reasonably sized");
                Assert.That(box.X + box.Width, Is.LessThanOrEqualTo(390), "the button must stay inside the viewport");
                Assert.That(box.Y, Is.LessThan(64), "the button sits in the header row");

                // Tapping it opens the same modal the desktop box opens.
                await mobileBtn.ClickAsync();
                var modal = page.Locator("#search-modal");
                await modal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                Assert.That(await modal.IsVisibleAsync(), Is.True, "tapping the icon opens the search modal");

                // The modal fits the phone: it doesn't run edge-to-edge and doesn't
                // overflow horizontally.
                var panel = modal.Locator("> div").Nth(1);
                var panelBox = await panel.BoundingBoxAsync();
                Assert.That(panelBox, Is.Not.Null);
                Assert.That(panelBox!.X, Is.GreaterThan(0), "the modal panel keeps side padding on a phone");
                Assert.That(panelBox.X + panelBox.Width, Is.LessThanOrEqualTo(390), "the modal panel must not overflow the viewport");
                var docOverflow = await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth + 1");
                Assert.That(docOverflow, Is.False, "opening search must not cause horizontal page overflow");

                // Typing runs a real search against the built index.
                var input = page.Locator("#search-input");
                Assert.That(await input.IsVisibleAsync(), Is.True, "the search input is focused/visible in the modal");
                await input.FillAsync("zanzibar");

                var results = page.Locator("#search-results");
                await results.Locator("a").First.WaitForAsync(new() { Timeout = 10000 });
                Assert.That(await results.InnerTextAsync(), Does.Contain("Widgets"),
                    "the matching page should be returned by the mobile search");

                // Escape closes it again.
                await page.Keyboard.PressAsync("Escape");
                await modal.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
                Assert.That(await modal.IsHiddenAsync(), Is.True, "Escape closes the modal");
            }
            finally
            {
                await browser!.CloseAsync();
                pw!.Dispose();
            }
        }

        [Test]
        public async Task MobileSearchIcon_IsHiddenOnDesktop_WhereTheFullSearchBoxShows()
        {
            using var server = new StaticServer(_outDir);
            var baseUrl = server.Start();

            IPlaywright pw = null;
            IBrowser browser = null;
            try
            {
                pw = await Playwright.CreateAsync();
                browser = await pw.Chromium.LaunchAsync(LaunchOptions());
            }
            catch (Exception ex)
            {
                pw?.Dispose();
                Assert.Ignore($"Playwright browser unavailable: {ex.Message}");
            }

            try
            {
                var page = await browser!.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
                await BlockExternalRequests(page);

                await page.GotoAsync(baseUrl + "/index.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 15000 });

                var mobileBtn = page.Locator("#mobile-search-btn");
                var desktopBox = page.Locator("header button:has-text('Search')").First;

                Assert.That(await mobileBtn.CountAsync(), Is.EqualTo(1), "the button is still in the DOM on desktop");
                Assert.That(await mobileBtn.IsVisibleAsync(), Is.False, "the compact icon is hidden from `md` up");
                Assert.That(await desktopBox.IsVisibleAsync(), Is.True, "the full search box takes over on desktop");
            }
            finally
            {
                await browser!.CloseAsync();
                pw!.Dispose();
            }
        }

        /// <summary>
        /// Headless Chromium launch options. Environments that ship a system Chromium
        /// instead of Playwright's own download can point the test at it with
        /// <c>NEKO_CHROMIUM_PATH</c> (same escape hatch as <c>NEKO_TAILWIND_CLI</c> in
        /// the Tailwind parity tests); otherwise the bundled browser is used.
        /// </summary>
        private static BrowserTypeLaunchOptions LaunchOptions()
        {
            var options = new BrowserTypeLaunchOptions { Headless = true };
            var exe = Environment.GetEnvironmentVariable("NEKO_CHROMIUM_PATH");
            if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) options.ExecutablePath = exe;
            return options;
        }

        /// <summary>Keeps the render offline-stable: only the local site may load.</summary>
        private static Task BlockExternalRequests(IPage page) =>
            page.RouteAsync("**/*", async route =>
            {
                var u = route.Request.Url;
                if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:"))
                    await route.ContinueAsync();
                else
                    await route.AbortAsync();
            });

        [TearDown]
        public void TearDown()
        {
            try { if (_outDir != null && Directory.Exists(_outDir)) Directory.Delete(_outDir, true); } catch { }
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
                                : file.EndsWith(".json") ? "application/json"
                                : file.EndsWith(".png") ? "image/png"
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
