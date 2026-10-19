using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using Neko.Server;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// A site loads none of its libraries from a CDN: KaTeX (with its fonts), the line-numbers plugin of highlight.js,
    /// leader-line and the Twemoji emoji all come from the site's own assets, taken from what is embedded in Neko, and
    /// KaTeX is fetched only by a page that has math. The Monaco editor of `neko watch` is served from the embedded copy.
    /// </summary>
    [TestFixture]
    public class EmbeddedLibrariesTests
    {
        private static async Task<string> BuildSiteAsync()
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-libs-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-libs-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"), "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Library Docs\nsnippets:\n  lineNumbers:\n    - csharp\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"),
                "# Libraries\n\n" +
                "Some math: $$ E = mc^2 $$\n\n" +
                "```csharp\nvar a = 1;\nvar b = 2;\n```\n\n" +
                "[!button icon=\":rocket:\" text=\"Go\"](plain.md)\n\n" +
                "```workflow\na: First | book-alt\nb: Second | code-branch\na --> b\n```\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "plain.md"), "# Plain\n\nJust text.\n");

            await new SiteBuilder(inDir, outDir).BuildAsync();
            return outDir;
        }

        [Test]
        public async Task Site_LinksItsLibrariesFromItsOwnAssets_AndNoCdn()
        {
            var outDir = await BuildSiteAsync();
            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));

            foreach (var host in new[] { "cdn.jsdelivr.net", "unpkg.com", "cdnjs.cloudflare.com", "cdn.jsdelivr", "googleapis" })
            {
                Assert.That(html, Does.Not.Contain(host), $"the page does not reference {host}");
            }

            var assets = Path.Combine(outDir, "assets");
            foreach (var file in new[] { "katex.min.css", "katex.min.js", "auto-render.min.js", "highlightjs-line-numbers.min.js", "leader-line.min.js", "pele.min.js", "emoji.css", "KaTeX_Main-Regular.woff2" })
            {
                Assert.That(File.Exists(Path.Combine(assets, file)), $"assets/{file} is shipped with the site");
            }

            Assert.That(html, Does.Contain("assets/highlightjs-line-numbers.min.js"));
            Assert.That(html, Does.Contain("assets/leader-line.min.js"));

            // The emoji stylesheet holds only the emoji the site shows, as files in assets/emoji/.
            var emojiCss = await File.ReadAllTextAsync(Path.Combine(assets, "emoji.css"));
            Assert.That(emojiCss, Does.Not.Contain("cdn.jsdelivr.net"));
            Assert.That(emojiCss, Does.Contain("url(\"emoji/1f680.svg\")"));
            Assert.That(Directory.GetFiles(Path.Combine(assets, "emoji")).Select(Path.GetFileName), Is.EquivalentTo(new[] { "1f680.svg" }));
        }

        [Test]
        public async Task Page_FetchesKatexOnlyWhenItHasMath_AndNothingComesFromACdn()
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
                var requested = new List<string>();
                var failed = new List<string>();
                page.Request += (_, r) =>
                {
                    if (r.Url.StartsWith(baseUrl)) requested.Add(r.Url.Substring(baseUrl.Length));
                    else if (!r.Url.StartsWith("data:")) external.Add(r.Url);
                };
                page.Response += (_, r) => { if (r.Status >= 400 && r.Url.StartsWith(baseUrl)) failed.Add(r.Status + " " + r.Url); };
                var errors = new List<string>();
                page.PageError += (_, e) => errors.Add(e);

                await page.RouteAsync("**/*", async route =>
                {
                    var u = route.Request.Url;
                    if (u.StartsWith("http://localhost") || u.StartsWith("http://127.0.0.1") || u.StartsWith("data:")) await route.ContinueAsync();
                    else await route.AbortAsync();
                });

                // A page with no math never fetches KaTeX.
                await page.GotoAsync(baseUrl + "/plain.html");
                await page.WaitForTimeoutAsync(500);
                Assert.That(requested.Where(u => u.Contains("katex") || u.Contains("KaTeX") || u.Contains("auto-render")), Is.Empty, "a page without math does not load KaTeX");

                requested.Clear();
                await page.GotoAsync(baseUrl + "/index.html");
                await page.WaitForSelectorAsync(".katex", new() { Timeout = 15000 });
                await page.WaitForSelectorAsync(".leader-line", new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
                await page.WaitForTimeoutAsync(500);

                Assert.That(requested, Has.Some.Contain("assets/katex.min.js"));
                Assert.That(requested, Has.Some.Contain("assets/auto-render.min.js"));
                Assert.That(requested, Has.Some.Contain("assets/katex.min.css"));
                Assert.That(requested, Has.Some.Contain("assets/KaTeX_"), "the math fonts come from the site too");
                Assert.That(requested, Has.Some.Contain("assets/leader-line.min.js"));
                Assert.That(requested, Has.Some.Contain("assets/highlightjs-line-numbers.min.js"));
                Assert.That(requested, Has.Some.Contain("assets/emoji/1f680.svg"), "the emoji is a file of the site, not a CDN image");
                Assert.That(await page.Locator(".hljs-ln").CountAsync(), Is.GreaterThan(0), "the code block got its line numbers");
                Assert.That(external, Is.Empty, "nothing is requested from outside the site");
                Assert.That(failed, Is.Empty, "every asset the page links exists");
                Assert.That(errors, Is.Empty);
            }
            finally
            {
                if (browser != null) await browser.CloseAsync();
                pw?.Dispose();
            }
        }

        [Test]
        public void WatchEditor_IsServedFromTheEmbeddedMonaco()
        {
            Assert.That(MonacoAssets.TryGet("vs/loader.js", out var loader, out var loaderType), Is.True);
            Assert.That(loaderType, Does.StartWith("text/javascript"));
            Assert.That(loader.Length, Is.GreaterThan(10_000));

            foreach (var file in new[] { "vs/editor/editor.main.js", "vs/editor/editor.main.css", "vs/editor/editor.main.nls.js", "vs/base/worker/workerMain.js", "vs/basic-languages/markdown/markdown.js", "vs/basic-languages/yaml/yaml.js", "vs/base/browser/ui/codicons/codicon/codicon.ttf" })
            {
                Assert.That(MonacoAssets.TryGet(file, out _, out _), Is.True, file + " is embedded");
            }

            Assert.That(MonacoAssets.TryGet("vs/../../etc/passwd", out _, out _), Is.False);
            Assert.That(MonacoAssets.TryGet("vs/nope.js", out _, out _), Is.False);
            Assert.That(MonacoAssets.TryGet("", out _, out _), Is.False);
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
