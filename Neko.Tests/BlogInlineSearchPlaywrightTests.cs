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
    /// Drives the blog-mode <b>inline search</b> in a real browser: builds a small
    /// `mode: blog` site, serves it, and verifies that typing into the in-content
    /// search box filters the index live (hiding the post grid), surfaces matching
    /// posts together with their tag chips (highlighting the matched tag), and that
    /// clearing the box restores the grid.
    ///
    /// Skipped when a Playwright browser is unavailable (e.g. no download).
    /// </summary>
    [TestFixture]
    public class BlogInlineSearchPlaywrightTests
    {
        private string _outDir;

        [SetUp]
        public void Setup()
        {
            var inputDir = Path.Combine(Path.GetTempPath(), "neko-blog-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(inputDir, "blog"));

            File.WriteAllText(Path.Combine(inputDir, "neko.yml"), @"
url: https://example.com
mode: blog
branding:
  title: Demo Blog
layout:
  sidebar: false
  toc: false
");
            File.WriteAllText(Path.Combine(inputDir, "blog", "index.md"), @"---
title: Blog
layout: blog
---
# Blog
");
            // One post tagged so we can prove tag-surfacing; its tags don't appear in
            // any other post's title/description, so a tag query isolates it.
            File.WriteAllText(Path.Combine(inputDir, "blog", "alpha.md"), @"---
title: Alpha Announcement
date: 2026-06-01
description: The first thing we shipped.
cover: /assets/alpha.png
tags: [photography, milestone]
---
# Alpha Announcement
Body of the alpha post.
");
            // A tiny real PNG so the cover thumbnail actually loads (and onerror
            // doesn't hide it) when the result renders.
            Directory.CreateDirectory(Path.Combine(inputDir, "assets"));
            File.WriteAllBytes(Path.Combine(inputDir, "assets", "alpha.png"), Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="));
            File.WriteAllText(Path.Combine(inputDir, "blog", "beta.md"), @"---
title: Beta Guide
date: 2026-06-02
description: How to use the beta.
tags: [cooking]
---
# Beta Guide
Body of the beta post.
");

            _outDir = Path.Combine(Path.GetTempPath(), "neko-blog-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(inputDir, _outDir).BuildAsync().GetAwaiter().GetResult();
        }

        [Test]
        public async Task InlineSearch_FiltersLivePosts_SurfacesTagChips_AndRestoresGrid()
        {
            Assert.That(File.Exists(Path.Combine(_outDir, "search.json")), Is.True, "search.json should be built");

            using var server = new StaticServer(_outDir);
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
                var page = await browser!.NewPageAsync();
                // Keep the render offline-stable: allow only the local site, abort any
                // external (font/CDN) request.
                await page.RouteAsync("**/*", async route =>
                {
                    var u = route.Request.Url;
                    if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:"))
                        await route.ContinueAsync();
                    else
                        await route.AbortAsync();
                });

                await page.GotoAsync(baseUrl + "/blog/index.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 15000 });

                var input = page.Locator("#neko-inline-search");
                var results = page.Locator("#neko-inline-search-results");
                var grid = page.Locator("#neko-blog-grid");

                // Baseline: the box and grid render; results are hidden until a query.
                await input.WaitForAsync(new() { Timeout = 10000 });
                Assert.That(await grid.IsVisibleAsync(), Is.True, "post grid should be visible before searching");
                Assert.That(await results.IsHiddenAsync(), Is.True, "results container starts hidden");

                // Type a tag that only the alpha post carries.
                await input.FillAsync("photography");

                // The index loads async, then renders — wait for the first result link.
                await results.Locator("a").First.WaitForAsync(new() { Timeout = 10000 });

                Assert.That(await results.IsVisibleAsync(), Is.True, "results show once a query matches");
                Assert.That(await grid.IsHiddenAsync(), Is.True, "the post grid is hidden while showing inline results");

                var resultsText = await results.InnerTextAsync();
                Assert.That(resultsText, Does.Contain("Alpha Announcement"),
                    "the matching post should appear in the inline results");
                Assert.That(resultsText, Does.Not.Contain("Beta Guide"),
                    "a non-matching post should not appear");

                // The tag is surfaced as a chip, and the matched tag is highlighted.
                Assert.That(resultsText, Does.Contain("photography"),
                    "the page's tags should be surfaced as chips in the result");
                var highlightedTag = results.Locator("mark", new() { HasTextString = "photography" });
                Assert.That(await highlightedTag.CountAsync(), Is.GreaterThan(0),
                    "the tag matching the query should be highlighted");

                // The post's cover image is shown as a thumbnail in the result, and it
                // actually loads (naturalWidth > 0 → onerror didn't hide it).
                var cover = results.Locator("img");
                Assert.That(await cover.CountAsync(), Is.GreaterThan(0),
                    "a post with a cover should show a thumbnail in the result");
                Assert.That(await cover.First.GetAttributeAsync("src"), Does.Contain("/assets/alpha.png"));
                // Wait for the (lazy) image to finish loading, then confirm it decoded.
                await cover.First.EvaluateAsync("img => img.complete ? Promise.resolve() : new Promise(r => { img.onload = r; img.onerror = r; })");
                var natWidth = await cover.First.EvaluateAsync<int>("img => img.naturalWidth");
                Assert.That(natWidth, Is.GreaterThan(0), "the cover thumbnail should load, not be hidden by onerror");

                // Clearing the box restores the grid and hides the results.
                await input.FillAsync("");
                await grid.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
                Assert.That(await grid.IsVisibleAsync(), Is.True, "clearing the query restores the post grid");
                Assert.That(await results.IsHiddenAsync(), Is.True, "clearing the query hides the results");
            }
            finally
            {
                await browser!.CloseAsync();
                pw!.Dispose();
            }
        }

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
