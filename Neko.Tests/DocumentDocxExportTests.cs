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
    /// Exports the Curiosity whitepaper sample
    /// (Neko.Documentation/documents/samples/modern-enterprise-search.md) and the plain
    /// neko-theme handbook to Word in a real browser, and reads the packages back:
    ///
    /// <list type="bullet">
    ///   <item>every part is well-formed XML and the file has one section per sheet;</item>
    ///   <item>the theme's typefaces are embedded as obfuscated TrueType (what Word reads),
    ///   under the names the fonts' own tables carry, and settings.xml asks Word to keep them;</item>
    ///   <item>the running head and foot are real headers and footers, the page number is a live
    ///   field, the ground and art are floating pictures behind the text;</item>
    ///   <item>the content is native Word: text runs in the embedded typefaces, tables, list numbering.</item>
    /// </list>
    ///
    /// Skipped when a Playwright browser is unavailable (NEKO_TEST_CHROMIUM points at a
    /// preinstalled Chromium).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class DocumentDocxExportTests
    {
        private string _out;
        private string _paper;
        private string _handbook;

        private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        [OneTimeSetUp]
        public void Build()
        {
            var input = Path.Combine(Path.GetTempPath(), "neko-docx-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(input, "docs", "assets"));
            File.WriteAllText(Path.Combine(input, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
            File.WriteAllText(Path.Combine(input, "index.md"), "# Home\n");
            var samples = Path.Combine(FindRoot(), "Neko.Documentation", "documents", "samples");
            File.Copy(Path.Combine(samples, "modern-enterprise-search.md"), Path.Combine(input, "docs", "paper.md"));
            File.Copy(Path.Combine(samples, "team-handbook.md"), Path.Combine(input, "docs", "handbook.md"));
            File.Copy(Path.Combine(samples, "assets", "airbus.svg"), Path.Combine(input, "docs", "assets", "airbus.svg"));
            _out = Path.Combine(Path.GetTempPath(), "neko-docx-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(input, _out).BuildAsync().GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            foreach (var f in new[] { _paper, _handbook }) if (f != null && File.Exists(f)) File.Delete(f);
        }

        private static string FindRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Neko.Documentation"))) dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "repository root not found");
            return dir!.FullName;
        }

        private static XDocument Part(ZipArchive zip, string name)
        {
            var entry = zip.GetEntry(name);
            Assert.That(entry, Is.Not.Null, name + " is in the package");
            using var reader = new StreamReader(entry!.Open());
            return XDocument.Parse(reader.ReadToEnd());
        }

        [Test]
        public void TheDocumentFonts_ShipWithTheSite()
        {
            var fonts = Path.Combine(_out, "assets", "deckfonts");
            foreach (var file in new[] { "Inter-Regular.ttf", "Inter-SemiBold.ttf", "SchibstedGrotesk-Regular.ttf", "GeistMono-Regular.ttf" })
            {
                Assert.That(File.ReadAllBytes(Path.Combine(fonts, file)).Take(4), Is.EqualTo(new byte[] { 0, 1, 0, 0 }), file + " is TrueType");
            }
            var html = File.ReadAllText(Path.Combine(_out, "docs", "paper.html"));
            Assert.That(html, Does.Contain("/*neko-font:Inter-Regular.ttf*/"), "the fonts are inlined");
            StandaloneMarkup.AssertSelfContained(html);
        }

        [Test]
        public async Task Whitepaper_ExportsASectionPerSheet_WithEmbeddedFonts_HeadersFootersAndFields()
        {
            _paper ??= await ExportAsync("docs/paper", "neko-paper-");
            using var zip = ZipFile.OpenRead(_paper);

            // Every part Word parses must be well-formed XML.
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".xml") || e.FullName.EndsWith(".rels")))
            {
                Assert.DoesNotThrow(() => XDocument.Parse(new StreamReader(entry.Open()).ReadToEnd()), entry.FullName + " is well-formed");
            }

            var document = Part(zip, "word/document.xml");

            // Word's schema requires a name on every picture (wp:docPr); LibreOffice does not care, Word calls the file unreadable.
            XNamespace wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
            foreach (var entry in zip.Entries.Where(e => Regex.IsMatch(e.FullName, @"^word/(document|header\d+|footer\d+)\.xml$")))
            {
                var pictures = Part(zip, entry.FullName).Descendants(wp + "docPr").ToList();
                Assert.That(pictures.All(d => !string.IsNullOrEmpty(d.Attribute("name")?.Value)), Is.True, entry.FullName + ": every picture is named");
            }
            var sections = document.Descendants(W + "sectPr").ToList();
            Assert.That(sections.Count, Is.EqualTo(21), "one Word section per sheet, like the 21 pages of the paper");
            Assert.That(sections.All(s => s.Element(W + "headerReference") != null && s.Element(W + "footerReference") != null), Is.True, "every sheet has its own header and footer");
            Assert.That(sections.All(s => s.Element(W + "pgNumType")?.Attribute(W + "fmt")?.Value == "decimalZero"), Is.True, "page numbers print as 01, 02, …");
            var size = sections[0].Element(W + "pgSz")!;
            Assert.That(size.Attribute(W + "w")!.Value, Is.EqualTo("11906"), "A4");
            Assert.That(size.Attribute(W + "h")!.Value, Is.EqualTo("16838"));

            // Fonts: obfuscated TrueType, named as the fonts name themselves, kept on re-save.
            var settings = Part(zip, "word/settings.xml");
            Assert.That(settings.Descendants(W + "embedTrueTypeFonts").Any(), Is.True, "Word keeps the fonts when a reader saves the file");
            var table = Part(zip, "word/fontTable.xml");
            var embedded = table.Descendants(W + "font").Where(f => f.Element(W + "embedRegular") != null).Select(f => f.Attribute(W + "name")!.Value).ToList();
            Assert.That(embedded, Does.Contain("Schibsted Grotesk"));
            Assert.That(embedded, Does.Contain("Schibsted Grotesk Medium"), "weight 500 is its own family to Word");
            Assert.That(embedded, Does.Contain("Geist Mono"));
            var fontParts = zip.Entries.Where(e => e.FullName.StartsWith("word/fonts/") && e.FullName.EndsWith(".odttf")).ToList();
            Assert.That(fontParts.Count, Is.EqualTo(embedded.Count), "one font file per embedded typeface");
            Assert.That(fontParts.All(f => f.Length > 20000), Is.True);
            Assert.That(zip.GetEntry("[Content_Types].xml"), Is.Not.Null);
            Assert.That(new StreamReader(zip.GetEntry("[Content_Types].xml")!.Open()).ReadToEnd(), Does.Contain("odttf"));

            // Every run is set in an embedded typeface — no fallback to a substitute.
            var runFonts = document.Descendants(W + "rFonts").Select(r => r.Attribute(W + "ascii")?.Value).Where(v => v != null).Distinct().ToList();
            Assert.That(runFonts, Is.SupersetOf(new[] { "Schibsted Grotesk", "Schibsted Grotesk Medium", "Geist Mono" }));
            Assert.That(runFonts.Except(embedded), Is.Empty, "no run names a font the file does not carry");

            // Content is native Word: text, tables, and the cover's two-line title.
            var text = string.Concat(document.Descendants(W + "t").Select(t => t.Value));
            Assert.That(text, Does.Contain("Modern enterprise search"));
            Assert.That(text, Does.Contain("Which layer finds what"));
            Assert.That(document.Descendants(W + "tbl").Count(), Is.GreaterThan(30), "grids, boxes and tables are Word tables");

            // The foot's page number is a live field; the ground and art float behind the text, in the headers.
            var footers = zip.Entries.Where(e => Regex.IsMatch(e.FullName, @"^word/footer\d+\.xml$")).ToList();
            Assert.That(footers.Count, Is.EqualTo(21));
            var footer = Part(zip, footers[3].FullName);
            Assert.That(footer.Descendants(W + "instrText").Any(i => i.Value.Contains("PAGE")) || footer.Descendants(W + "fldChar").Any() || footer.Descendants(W + "fldSimple").Any(), Is.True, "the page number is a field");
            var header = Part(zip, zip.Entries.First(e => e.FullName.StartsWith("word/header") && e.FullName.EndsWith(".xml")).FullName);
            var xml = header.ToString();
            Assert.That(xml, Does.Contain("behindDoc=\"1\""), "the ground floats behind the text");
            Assert.That(xml, Does.Contain("relativeFrom=\"page\""), "and is placed on the page, not in the margins");
        }

        [Test]
        public async Task Handbook_ExportsAFlowingDocument_WithListsAFlowingFooterAndInter()
        {
            _handbook ??= await ExportAsync("docs/handbook", "neko-handbook-");
            using var zip = ZipFile.OpenRead(_handbook);
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".xml") || e.FullName.EndsWith(".rels")))
            {
                Assert.DoesNotThrow(() => XDocument.Parse(new StreamReader(entry.Open()).ReadToEnd()), entry.FullName + " is well-formed");
            }

            var document = Part(zip, "word/document.xml");
            Assert.That(document.Descendants(W + "sectPr").Count(), Is.EqualTo(1), "no separators: one flowing section");
            var embedded = Part(zip, "word/fontTable.xml").Descendants(W + "font").Where(f => f.Element(W + "embedRegular") != null)
                .Select(f => f.Attribute(W + "name")!.Value).ToList();
            Assert.That(embedded, Does.Contain("Inter"));
            Assert.That(embedded, Does.Contain("Inter SemiBold"), "headings are the 600 instance");

            // Bullets and numbers are Word lists, not typed characters.
            var numbering = Part(zip, "word/numbering.xml");
            var formats = numbering.Descendants(W + "numFmt").Select(n => n.Attribute(W + "val")!.Value).ToList();
            Assert.That(formats, Does.Contain("bullet"));
            Assert.That(formats, Does.Contain("decimal"));
            Assert.That(document.Descendants(W + "numPr").Count(), Is.GreaterThanOrEqualTo(6));

            // The table is a table, with its header row repeating.
            Assert.That(document.Descendants(W + "tblHeader").Any(), Is.True);
            var text = string.Concat(document.Descendants(W + "t").Select(t => t.Value));
            Assert.That(text, Does.Contain("Freeze the branch"));
            Assert.That(text, Does.Contain("dotnet build Neko.sln"));
            Assert.That(text, Does.Contain("Hotfixes"));

            // The foot repeats on every page, with the page number as a field.
            var footer = string.Concat(Part(zip, "word/footer1.xml").Descendants(W + "t").Select(t => t.Value)) + Part(zip, "word/footer1.xml");
            Assert.That(footer, Does.Contain("Neko"));
            Assert.That(footer, Does.Contain("PAGE"));
        }

        private async Task<string> ExportAsync(string path, string prefix)
        {
            using var server = new StaticServer(_out);
            var baseUrl = server.Start();
            var (pw, browser) = await LaunchAsync();
            try
            {
                var context = await browser.NewContextAsync(new() { AcceptDownloads = true, ViewportSize = new() { Width = 1100, Height = 1200 } });
                var page = await context.NewPageAsync();
                var errors = new List<string>();
                // the CDN libraries (Mermaid, KaTeX, panzoom) are unreachable here by design
                page.PageError += (_, e) => { if (!Regex.IsMatch(e, "mermaid|renderMathInElement|panzoom|hljs|katex", RegexOptions.IgnoreCase)) errors.Add(e); };
                var response = await page.GotoAsync($"{baseUrl}/{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
                Assert.That(response!.Status, Is.EqualTo(200), $"{path} is served");
                await page.WaitForSelectorAsync("#doc-download", new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
                var download = await page.RunAndWaitForDownloadAsync(
                    () => page.EvaluateAsync("document.getElementById('doc-download').click()"), new() { Timeout = 120000 });
                var file = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N") + ".docx");
                await download.SaveAsAsync(file);
                Assert.That(errors, Is.Empty, "the page raised no script errors");
                return file;
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
                int port = 11700 + new Random().Next(800);
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
