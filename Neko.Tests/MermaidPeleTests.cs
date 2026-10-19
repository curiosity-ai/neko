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
    /// ```mermaid blocks are drawn by Pele, which Neko embeds: a site page loads assets/pele.min.js (only when it
    /// has a diagram) and nothing comes from a CDN. A diagram is drawn once and follows the page's light/dark mode
    /// through CSS variables set from the site's own theme variables.
    /// </summary>
    [TestFixture]
    public class MermaidPeleTests
    {
        private const string Diagram = "```mermaid\nflowchart LR\n  A[Draft] --> B{Review}\n  B -- ok --> C([Published])\n```\n";

        private static async Task<string> BuildSiteAsync()
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-mermaid-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-mermaid-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"), "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Diagram Docs\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"), "# With a diagram\n\n" + Diagram);
            await File.WriteAllTextAsync(Path.Combine(inDir, "plain.md"), "# Without a diagram\n\nJust text.\n");

            await new SiteBuilder(inDir, outDir).BuildAsync();
            return outDir;
        }

        [Test]
        public async Task Site_EmbedsPele_AndLoadsNoMermaidFromACdn()
        {
            var outDir = await BuildSiteAsync();
            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));

            Assert.That(html, Does.Not.Contain("cdn.jsdelivr.net/npm/mermaid"), "Mermaid is not loaded from a CDN");
            Assert.That(html, Does.Not.Contain("unpkg.com/panzoom"), "panzoom is not loaded from a CDN");
            Assert.That(html, Does.Contain("assets/pele.min.js"), "the page knows where its embedded Pele is");
            Assert.That(html, Does.Contain("--neko-primary-500"), "the site's palette is exposed as CSS variables");
            Assert.That(html, Does.Contain("--pele-line: var(--neko-primary-500"), "Pele's variables are mapped onto the site's theme variables");

            var pele = Path.Combine(outDir, "assets", "pele.min.js");
            Assert.That(File.Exists(pele), "Pele is copied into the site's assets");
            Assert.That(File.ReadAllText(pele), Does.StartWith("/*! Pele "));
            Assert.That(File.Exists(Path.Combine(outDir, "assets", "mermaid.min.js")), Is.False);
            Assert.That(File.Exists(Path.Combine(outDir, "assets", "panzoom.min.js")), Is.False);
        }

        [Test]
        public async Task Diagram_IsDrawnOnce_AndFollowsTheColourModeWithoutRedrawing()
        {
            var outDir = await BuildSiteAsync();

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
                var ctx = await browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1100, Height = 800 } });
                var page = await ctx.NewPageAsync();

                var external = new List<string>();
                var peleRequests = new List<string>();
                var errors = new List<string>();
                page.Request += (_, r) =>
                {
                    if (r.Url.Contains("pele.min.js")) peleRequests.Add(r.Url);
                    if (!r.Url.StartsWith(baseUrl) && !r.Url.StartsWith("data:")) external.Add(r.Url);
                };
                page.PageError += (_, e) => errors.Add(e);

                await page.RouteAsync("**/*", async route =>
                {
                    var u = route.Request.Url;
                    if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:")) await route.ContinueAsync();
                    else await route.AbortAsync();
                });

                // A page without a diagram never fetches Pele.
                await page.GotoAsync(baseUrl + "/plain.html");
                await page.WaitForTimeoutAsync(500);
                Assert.That(peleRequests, Is.Empty, "a page with no diagram does not load Pele");

                await page.GotoAsync(baseUrl + "/index.html");
                await page.WaitForSelectorAsync(".mermaid svg.pele", new() { Timeout = 15000 });
                Assert.That(peleRequests, Has.Count.EqualTo(1), "Pele is loaded once, from the site's own assets");
                Assert.That(peleRequests[0], Does.StartWith(baseUrl));
                Assert.That(await page.Locator(".mermaid svg.pele").CountAsync(), Is.EqualTo(1));

                // Tag the drawn SVG, then switch to dark mode: the same SVG must stay, with new colours.
                await page.EvaluateAsync("() => { document.documentElement.classList.remove('dark'); document.querySelector('.mermaid svg.pele').dataset.drawnOnce = '1'; }");
                var light = await page.EvaluateAsync<string[]>("() => { const m = document.querySelector('.mermaid'); const cs = getComputedStyle(m); return [cs.getPropertyValue('--pele-bg').trim(), cs.getPropertyValue('--pele-line').trim(), cs.backgroundColor]; }");
                await page.EvaluateAsync("() => document.documentElement.classList.add('dark')");
                var dark = await page.EvaluateAsync<string[]>("() => { const m = document.querySelector('.mermaid'); const cs = getComputedStyle(m); return [cs.getPropertyValue('--pele-bg').trim(), cs.getPropertyValue('--pele-line').trim(), cs.backgroundColor]; }");

                Assert.That(await page.Locator(".mermaid svg.pele[data-drawn-once='1']").CountAsync(), Is.EqualTo(1), "the diagram is not drawn again for dark mode");
                Assert.That(dark[0], Is.Not.EqualTo(light[0]), "the surface follows the colour mode");
                Assert.That(dark[1], Is.Not.EqualTo(light[1]), "the lines follow the colour mode");
                Assert.That(light[2], Is.EqualTo(await page.EvaluateAsync<string>("() => { document.documentElement.classList.remove('dark'); return getComputedStyle(document.querySelector('.mermaid')).backgroundColor; }")), "the box paints the colour Pele uses behind its labels");

                // (KaTeX and highlight.js still come from their CDNs; the diagram library does not.)
                Assert.That(external.FindAll(u => u.Contains("mermaid") || u.Contains("panzoom") || u.Contains("pele")), Is.Empty, "no diagram library is requested from outside the site");
                // The page's KaTeX and highlight.js CDN scripts are blocked here, which only they complain about.
                Assert.That(errors.FindAll(e => !System.Text.RegularExpressions.Regex.IsMatch(e, "renderMathInElement|katex|hljs", System.Text.RegularExpressions.RegexOptions.IgnoreCase)), Is.Empty);
            }
            finally
            {
                if (browser != null) await browser.CloseAsync();
                pw?.Dispose();
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
