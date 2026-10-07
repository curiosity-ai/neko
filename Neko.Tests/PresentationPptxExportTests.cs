using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Exports the Curiosity slide templates deck
    /// (Neko.Documentation/presentations/decks/curiosity-templates.md) to PowerPoint
    /// in a real browser, with the fonts Neko ships for the curiosity theme, and
    /// reads the package back:
    ///
    /// <list type="bullet">
    ///   <item>the deck loads its typefaces from the site (assets/deckfonts/), not a font host;</item>
    ///   <item>every typeface the slides use is embedded as Embedded OpenType, and a weight
    ///   PowerPoint has no flag for is named as the static instance's own family;</item>
    ///   <item>generated content (::before counters) and CSS-drawn marks are exported;</item>
    ///   <item>the theme's grid layouts survive: the cover's title sits left of its lead.</item>
    /// </list>
    ///
    /// Skipped when a Playwright browser is unavailable (NEKO_TEST_CHROMIUM points
    /// at a preinstalled Chromium).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class PresentationPptxExportTests
    {
        private string _out;
        private string _pptx;

        [OneTimeSetUp]
        public void Build()
        {
            var input = Path.Combine(Path.GetTempPath(), "neko-pptx-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(input, "decks"));
            File.WriteAllText(Path.Combine(input, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
            File.WriteAllText(Path.Combine(input, "index.md"), "# Home\n");
            File.Copy(FindDeck(), Path.Combine(input, "decks", "templates.md"));
            _out = Path.Combine(Path.GetTempPath(), "neko-pptx-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(input, _out).BuildAsync().GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (_pptx != null && File.Exists(_pptx)) File.Delete(_pptx);
        }

        private static string FindDeck()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Neko.Documentation"))) dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "repository root not found");
            return Path.Combine(dir!.FullName, "Neko.Documentation", "presentations", "decks", "curiosity-templates.md");
        }

        [Test]
        public void TheThemeFonts_ShipWithTheSite()
        {
            var fonts = Path.Combine(_out, "assets", "deckfonts");
            Assert.That(File.Exists(Path.Combine(fonts, "deck-fonts.css")), Is.True);
            Assert.That(File.Exists(Path.Combine(fonts, "deck-fonts.json")), Is.True);
            foreach (var file in new[] { "SchibstedGrotesk-Regular.ttf", "SchibstedGrotesk-Medium.ttf", "GeistMono-Regular.ttf", "GeistMono-Medium.ttf" })
            {
                var bytes = File.ReadAllBytes(Path.Combine(fonts, file));
                Assert.That(bytes.Take(4), Is.EqualTo(new byte[] { 0, 1, 0, 0 }), file + " is TrueType");
            }

            var html = File.ReadAllText(Path.Combine(_out, "decks", "templates.html"));
            Assert.That(html, Does.Contain("/*neko-font:SchibstedGrotesk-Regular.ttf*/"), "the fonts are inlined");
            StandaloneMarkup.AssertSelfContained(html);
        }

        [Test]
        public async Task Export_EmbedsTheFonts_AndKeepsWhatTheSlidesShow()
        {
            var pptx = await ExportAsync();
            using var zip = ZipFile.OpenRead(pptx);

            string Read(string name)
            {
                var entry = zip.GetEntry(name);
                Assert.That(entry, Is.Not.Null, name + " is in the package");
                using var reader = new StreamReader(entry!.Open());
                return reader.ReadToEnd();
            }

            // Every part PowerPoint parses must be well-formed XML (a duplicated
            // attribute once made LibreOffice refuse the whole file).
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".xml") || e.FullName.EndsWith(".rels")))
            {
                Assert.DoesNotThrow(() => XDocument.Parse(Read(entry.FullName)), entry.FullName + " is well-formed");
            }

            var presentation = Read("ppt/presentation.xml");
            Assert.That(presentation, Does.Contain("embedTrueTypeFonts=\"1\""));
            var embedded = Regex.Matches(presentation, "<p:font typeface=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
            Assert.That(embedded, Does.Contain("Schibsted Grotesk"));
            Assert.That(embedded, Does.Contain("Schibsted Grotesk Medium"), "weight 500 is its own family to PowerPoint");
            Assert.That(embedded, Does.Contain("Geist Mono"));

            var fonts = zip.Entries.Where(e => e.FullName.StartsWith("ppt/fonts/") && e.FullName.EndsWith(".fntdata")).ToList();
            Assert.That(fonts.Count, Is.EqualTo(embedded.Count), "one embedded font file per typeface slot");
            foreach (var font in fonts)
            {
                using var stream = font.Open();
                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                var eot = buffer.ToArray();
                Assert.That(BitConverter.ToUInt32(eot, 0), Is.EqualTo((uint)eot.Length), "EOTSize is the file size");
                Assert.That(BitConverter.ToUInt32(eot, 8), Is.EqualTo(0x00020001u), "Embedded OpenType 2.1");
                Assert.That(BitConverter.ToUInt16(eot, 34), Is.EqualTo(0x504C), "the EOT magic number");
                var fontData = BitConverter.ToUInt32(eot, 4);
                Assert.That(eot.Skip(eot.Length - (int)fontData).Take(4), Is.EqualTo(new byte[] { 0, 1, 0, 0 }), "the TrueType font follows the header");
            }

            var slideXml = Enumerable.Range(1, 25).Select(i => Read($"ppt/slides/slide{i}.xml")).ToArray();
            Assert.That(slideXml.Any(s => s.Contains("typeface=\"Schibsted Grotesk Medium\"")), Is.True, "headlines are set in the 500 instance");
            Assert.That(slideXml.Any(s => s.Contains("typeface=\"Geist Mono\"")), Is.True, "eyebrows and labels in the mono");
            Assert.That(slideXml.All(s => !s.Contains("typeface=\"Arial\"")), Is.True, "no run falls back to a substitute");

            // ::before counters: the agenda numbers and the steps' "01 · DESCRIBE".
            var agenda = slideXml[4];
            Assert.That(agenda, Does.Contain(">01<"), "agenda counters are exported");
            Assert.That(slideXml[10], Does.Contain("DESCRIBE"));
            Assert.That(slideXml[10], Does.Match(">01\\s*·?\\s*<|>01<"), "step counters are exported");

            // The cover keeps its grid: the title on the left, the lead to its right.
            var cover = slideXml[0];
            long XOf(string text)
            {
                var sp = Regex.Matches(cover, "<p:sp>.*?</p:sp>", RegexOptions.Singleline).Select(m => m.Value).First(v => v.Contains(text));
                return long.Parse(Regex.Match(sp, "<a:off x=\"(\\d+)\"").Groups[1].Value);
            }
            Assert.That(XOf("One sentence on what this deck is for"), Is.GreaterThan(XOf("Deck title")), "the cover's lead stays beside its title");

            // The Escape mark on the slide foot is drawn as native squares, not dropped.
            Assert.That(Regex.Matches(slideXml[4], "prstGeom prst=\"rect\"").Count, Is.GreaterThan(8));
        }

        private async Task<string> ExportAsync()
        {
            if (_pptx != null) return _pptx;
            using var server = new StaticServer(_out);
            var baseUrl = server.Start();
            var (pw, browser) = await LaunchAsync();
            try
            {
                var context = await browser.NewContextAsync(new() { AcceptDownloads = true, ViewportSize = new() { Width = 1280, Height = 720 } });
                var page = await context.NewPageAsync();
                await page.GotoAsync($"{baseUrl}/decks/templates", new() { WaitUntil = WaitUntilState.NetworkIdle });
                var download = await page.RunAndWaitForDownloadAsync(
                    () => page.EvaluateAsync("document.getElementById('deck-download').click()"), new() { Timeout = 120000 });
                _pptx = Path.Combine(Path.GetTempPath(), "neko-curiosity-" + Guid.NewGuid().ToString("N") + ".pptx");
                await download.SaveAsAsync(_pptx);
                return _pptx;
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
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

        private sealed class StaticServer : IDisposable
        {
            private readonly HttpListener _listener = new();
            private readonly string _root;
            public StaticServer(string root) { _root = root; }

            public string Start()
            {
                int port = 10800 + new Random().Next(800);
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
                                ".json" => "application/json", ".woff2" => "font/woff2", ".ttf" => "font/ttf",
                                ".svg" => "image/svg+xml", _ => "application/octet-stream",
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
