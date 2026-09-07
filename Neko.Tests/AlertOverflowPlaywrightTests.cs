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
    /// Renders a note (alert) whose content cannot fit the column — a long code
    /// line, an unbreakable token, a long inline path and a wide table — in a real
    /// browser. None of it may widen the note or the page: prose wraps, and what
    /// cannot wrap scrolls inside its own box.
    ///
    /// Skipped when a Playwright browser is unavailable.
    /// </summary>
    [TestFixture]
    public class AlertOverflowPlaywrightTests
    {
        [Test]
        public async Task NoteWithUnwrappableContent_StaysInsideThePage()
        {
            var inDir = Path.Combine(Path.GetTempPath(), "neko-alert-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-alert-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inDir);

            await File.WriteAllTextAsync(Path.Combine(inDir, "neko.yml"),
                "input: ./\noutput: .neko\nurl: example.com\nbranding:\n  title: Alert Docs\n");
            await File.WriteAllTextAsync(Path.Combine(inDir, "index.md"),
                "# Notes\n\n" +
                "!!! warning The install folder is written to once\n" +
                "The first start downloads the English language models (`Catalyst.Models.English.dll` and " +
                "`Catalyst.ConceptNet.English.dll`) into the install folder. Either grant the service account " +
                "Modify on `E:\\Curiosity\\CuriosityWorkspace`, or download the models once as an administrator:\n\n" +
                "```powershell\n" +
                "& \"E:\\Curiosity\\CuriosityWorkspace\\curiosity.exe\" download-default-languages   # or download-all-languages\n" +
                "```\n" +
                "!!!\n\n" +
                "!!! info Unbreakable content\n" +
                new string('a', 160) + " and `E:\\Curiosity\\CuriosityWorkspace\\some\\extremely\\long\\path\\that\\never\\ends.dll`\n\n" +
                "| A rather long column header | Another long column header here | And a third one as well | Plus a fourth |\n" +
                "| --- | --- | --- | --- |\n" +
                "| `E:\\Curiosity\\CuriosityWorkspaceData` | Full control, full control, full control | Modify | Read |\n" +
                "!!!\n");

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
                var ctx = await browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
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

                try { await page.GotoAsync(baseUrl + "/index.html", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 15000 }); }
                catch { /* network idle best-effort */ }

                // The content column never scrolls sideways.
                var main = await page.EvaluateAsync<int[]>(
                    "() => { const m = document.querySelector('#main-scroll'); return [m.scrollWidth, m.clientWidth]; }");
                Assert.That(main[0], Is.LessThanOrEqualTo(main[1] + 1),
                    "A note's content must not widen the page horizontally.");

                // Nothing inside a note paints past the note's own right edge.
                var overflowing = await page.EvaluateAsync<int>(@"() => {
                    let n = 0;
                    for (const note of document.querySelectorAll('.neko-alert-body')) {
                        const edge = note.getBoundingClientRect().right;
                        for (const el of note.querySelectorAll('p, pre, table, h5')) {
                            if (el.getBoundingClientRect().right > edge + 1) n++;
                        }
                    }
                    return n;
                }");
                Assert.That(overflowing, Is.Zero, "Content inside a note must stay within the note.");

                // The code line that cannot wrap is still reachable: its box scrolls.
                var scrollable = await page.EvaluateAsync<bool>(@"() => {
                    const box = document.querySelector('.neko-alert-body .neko-code-block pre code')
                             ?? document.querySelector('.neko-alert-body .neko-code-block pre');
                    if (!box || box.scrollWidth <= box.clientWidth) return false;
                    box.scrollLeft = 50;
                    return box.scrollLeft > 0;
                }");
                Assert.That(scrollable, Is.True, "A wide code block inside a note scrolls horizontally.");

                // A table in a note is a scroll container of its own, so one too
                // wide to compress scrolls instead of pushing the note open.
                var tableScrolls = await page.EvaluateAsync<bool>(@"() => {
                    const t = document.querySelector('.neko-alert-body table');
                    if (!t) return false;
                    const cs = getComputedStyle(t);
                    return cs.overflowX === 'auto' && cs.display === 'block';
                }");
                Assert.That(tableScrolls, Is.True, "A table inside a note scrolls rather than widening it.");
            }
            finally
            {
                if (browser != null) await browser.CloseAsync();
                pw?.Dispose();
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
