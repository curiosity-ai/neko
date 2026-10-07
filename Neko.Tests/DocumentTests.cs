using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Unit coverage for paged documents: the front-matter marker, the page splitter,
    /// the page components, the generated art, and the generated document page.
    /// </summary>
    [TestFixture]
    public class DocumentTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        private static string Render(string markdown)
        {
            var config = new NekoConfig();
            var doc = new MarkdownParser(config).Parse(markdown);
            return new HtmlGenerator(config).GenerateDocument(doc);
        }

        [Test]
        public void DocumentMarker_BareTrue_EnablesTheNekoTheme()
        {
            var doc = NewParser().Parse("---\ntitle: T\ndocument: true\n---\n\n# One\n");

            Assert.That(doc.IsPagedDocument, Is.True);
            Assert.That(doc.IsPresentation, Is.False);
            Assert.That(doc.PagedDocument!.Theme, Is.EqualTo("neko"));
            Assert.That(doc.PagedDocument.Size, Is.EqualTo("a4"));
            Assert.That(doc.PagedDocument.Download, Is.True);
            Assert.That(doc.Pages!.Count, Is.EqualTo(1));
        }

        [Test]
        public void DocumentMarker_Mapping_ReadsTheOptions()
        {
            var doc = NewParser().Parse(
                "---\ntitle: T\ndocument:\n  theme: curiosity\n  size: letter\n  running: A paper\n  numbers: no\n  docx: false\n  author: Ada\n---\n\n# One\n");

            var o = doc.PagedDocument!;
            Assert.That(o.Theme, Is.EqualTo("curiosity"));
            Assert.That(o.IsLetter, Is.True);
            Assert.That(o.Running, Is.EqualTo("A paper"));
            Assert.That(o.Numbers, Is.False);
            Assert.That(o.Download, Is.False);
            Assert.That(o.Author, Is.EqualTo("Ada"));
            Assert.That(o.ThemeStylesheet, Is.EqualTo("document-curiosity.css"));
        }

        [TestCase("false")]
        [TestCase("no")]
        [TestCase("off")]
        public void DocumentMarker_Falsey_BuildsAnOrdinaryPage(string value)
        {
            var doc = NewParser().Parse($"---\ntitle: T\ndocument: {value}\n---\n\n# One\n");

            Assert.That(doc.IsPagedDocument, Is.False);
            Assert.That(doc.Html, Does.Contain("<h1"));
        }

        [Test]
        public void ADeckWinsOverADocument()
        {
            var doc = NewParser().Parse("---\ntitle: T\npresentation: true\ndocument: true\n---\n\n# One\n");

            Assert.That(doc.IsPresentation, Is.True);
            Assert.That(doc.IsPagedDocument, Is.False);
        }

        [Test]
        public void Split_PagesFollowTheSeparatorRules_AndKeepTheirAttributes()
        {
            var pages = DocumentParser.Split("# One\n\n--- {layout=\"opener\" ground=\"ink\"}\n\n## Two\n\n---\n\n## Three\n");

            Assert.That(pages.Count, Is.EqualTo(3));
            Assert.That(pages[1].Get("layout"), Is.EqualTo("opener"));
            Assert.That(pages[1].Get("ground"), Is.EqualTo("ink"));
            Assert.That(pages[2].Markdown, Does.StartWith("## Three"));
        }

        [Test]
        public void ThemeOverride_AppliesToDocumentsOnlyWhenTheyHaveThatTheme()
        {
            try
            {
                PresentationOptions.ThemeOverride = "curiosity";
                Assert.That(NewParser().Parse("---\ndocument: true\n---\n\n# A\n").PagedDocument!.Theme, Is.EqualTo("curiosity"));

                PresentationOptions.ThemeOverride = "midnight";
                Assert.That(NewParser().Parse("---\ndocument: true\n---\n\n# A\n").PagedDocument!.Theme, Is.EqualTo("neko"));
            }
            finally { PresentationOptions.ThemeOverride = null; }
        }

        [Test]
        public void Components_OnlyClaimTheirNamesInsideADocument()
        {
            var inDoc = Render("---\ndocument: true\n---\n\n::: note {label=\"Why\"}\nBecause.\n:::\n");
            Assert.That(inDoc, Does.Contain("class=\"doc-note\""));
            Assert.That(inDoc, Does.Contain("<p class=\"doc-note-label\">Why</p>"));

            var plain = NewParser().Parse("---\ntitle: T\n---\n\n::: note {label=\"Why\"}\nBecause.\n:::\n");
            Assert.That(plain.Html, Does.Not.Contain("doc-note"));
        }

        [Test]
        public void Cols_NestAndCarryTheirCount()
        {
            var html = Render("---\ndocument: true\n---\n\n::::: cols {count=\"3\"}\n:::: col\nOne\n::::\n\n:::: col\nTwo\n::::\n:::::\n");

            Assert.That(html, Does.Contain("class=\"doc-cols\" data-cols=\"3\""));
            Assert.That(Regex.Matches(html, "class=\"doc-col\"").Count, Is.EqualTo(2));
        }

        [Test]
        public void Facts_BecomeCellsWithTheirLabels()
        {
            var html = Render("---\ndocument: true\n---\n\n::: facts\nTopic\n:   Search\n\nDate\n:   October 2026\n:::\n");

            Assert.That(Regex.Matches(html, "class=\"doc-fact\"").Count, Is.EqualTo(2));
            Assert.That(html, Does.Contain("<p class=\"doc-fact-label\">Topic</p>"));
            Assert.That(html, Does.Contain("October 2026"));
        }

        [Test]
        public void Flow_DrawsBoxesAndArrows_FromBullets()
        {
            var html = Render("---\ndocument: true\n---\n\n::: flow {caption=\"Fig. 1\"}\n- [mark] 01 · Keyword | Keyword | Exact terms || [solid] 02 · Agents | Agents | Search as a tool\n:::\n");

            Assert.That(html, Does.Contain("class=\"doc-flow-svg\" data-layout=\"row\""));
            Assert.That(Regex.Matches(html, "class=\"f-box\"").Count, Is.EqualTo(1), "one outlined box");
            Assert.That(Regex.Matches(html, "class=\"f-solid\"").Count, Is.EqualTo(1), "one inverted box");
            Assert.That(html, Does.Contain("class=\"art-sig\""), "the marked step carries the signal square");
            Assert.That(html, Does.Contain(">01 &#183; KEYWORD<"));
            Assert.That(html, Does.Contain("<figcaption class=\"doc-figure-caption\">Fig. 1</figcaption>"));
        }

        [Test]
        public void Flow_RowsOfTwoBoxesFork_ThePath()
        {
            var html = Render("---\ndocument: true\n---\n\n::: flow\n- A | One | a\n- B | Two | b || C | Three | c\n- D | Four | d\n:::\n");

            Assert.That(html, Does.Contain("data-layout=\"path\""));
            // an arrow into each of the two boxes, and one from each to the last box
            Assert.That(Regex.Matches(html, "<polyline").Count, Is.EqualTo(2 + 2));
        }

        [Test]
        public void Quote_DrawsALoneImageAsAMaskedMark()
        {
            var html = Render("---\ndocument: true\n---\n\n::: quote {by=\"Someone\"}\n![Acme](logo.svg)\n\nWords.\n:::\n");

            Assert.That(html, Does.Contain("class=\"doc-quote-logo\""));
            Assert.That(html, Does.Contain("mask-image:url('logo.svg')"));
            Assert.That(html, Does.Contain("<figcaption>Someone</figcaption>"));
            Assert.That(html, Does.Not.Contain("<img"));
        }

        [Test]
        public void Mark_RendersATableSquare()
        {
            var html = Render("---\ndocument: true\n---\n\n[!mark part] In part\n");

            Assert.That(html, Does.Contain("class=\"doc-mark\" data-kind=\"part\""));
        }

        [Test]
        public void Page_CarriesItsLayoutGroundArtHeadAndFoot()
        {
            var html = Render(
                "---\ntitle: T\ndocument:\n  theme: curiosity\n  running: A paper\n---\n\n" +
                "--- {layout=\"opener\" art=\"squares\" number=\"02\"}\n\n# Title\n\n--- {layout=\"back\" foot=\"hi@x.io|Street 1\"}\n\n# Bye\n");

            Assert.That(html, Does.Contain("data-doc-theme=\"curiosity\""));
            Assert.That(html, Does.Contain("data-layout=\"opener\""));
            Assert.That(html, Does.Contain("data-ground=\"ink\""), "an opener is on ink unless it says otherwise");
            Assert.That(html, Does.Contain("data-ground=\"deep\""), "a back cover is on the deep blue");
            Assert.That(html, Does.Contain("data-doc-bg=\"art\""));
            Assert.That(html, Does.Contain("<p class=\"doc-number\">02</p>"));
            Assert.That(html, Does.Contain("<span class=\"doc-head-left\">A paper</span>"));
            Assert.That(html, Does.Contain("data-doc-part=\"foot\""));
            Assert.That(html, Does.Contain("hi@x.io<br>Street 1"));
            Assert.That(html, Does.Contain("data-doc-field=\"page\" data-doc-format=\"zero\">01<"));
            Assert.That(html, Does.Contain("[data-doc-theme=\"curiosity\"]"), "the theme stylesheet is inlined");
            StandaloneMarkup.AssertSelfContained(html);
        }

        [Test]
        public void Page_Download_NamesTheFileAfterTheTitle()
        {
            var html = Render("---\ntitle: Modern enterprise search!\ndocument: true\n---\n\n# A\n");

            Assert.That(html, Does.Contain("id=\"doc-download\""));
            Assert.That(html, Does.Contain("data-doc-file=\"modern-enterprise-search.docx\""));

            var off = Render("---\ntitle: T\ndocument:\n  docx: false\n---\n\n# A\n");
            Assert.That(off, Does.Not.Contain("id=\"doc-download\""));
        }

        [Test]
        public void Page_Letter_IsAdvertisedOnTheRoot()
        {
            Assert.That(Render("---\ndocument:\n  size: letter\n---\n\n# A\n"), Does.Contain("data-doc-size=\"letter\""));
        }

        [Test]
        public void Art_IsDeterministic_AndKnowsItsKinds()
        {
            foreach (var kind in DocumentArt.Kinds)
            {
                var a = DocumentArt.Render(kind, 595, 260, .6, .5);
                var b = DocumentArt.Render(kind, 595, 260, .6, .5);
                Assert.That(a, Is.Not.Null, kind);
                Assert.That(a, Is.EqualTo(b), kind + " does not churn between builds");
                Assert.That(a, Does.Contain("class=\"art-sig\""), kind + " spends the page's one signal");
            }
            Assert.That(DocumentArt.Render("nope", 10, 10, .5, .5), Is.Null);
        }

        [Test]
        public void Lockup_IsOutlinedSoItNeedsNoFont()
        {
            var svg = DocumentArt.Lockup();

            Assert.That(svg, Does.Contain("<path"));
            Assert.That(svg, Does.Not.Contain("<text"));
            Assert.That(svg, Does.Contain("currentColor"));
        }

        [Test]
        public void Build_ShipsTheDocumentAssets_AndKeepsDocumentsOutOfTheNavigation()
        {
            var input = Path.Combine(Path.GetTempPath(), "neko-doc-in-" + Guid.NewGuid().ToString("N"));
            var output = Path.Combine(Path.GetTempPath(), "neko-doc-out-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(input);
                File.WriteAllText(Path.Combine(input, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
                File.WriteAllText(Path.Combine(input, "index.md"), "# Home\n");
                File.WriteAllText(Path.Combine(input, "paper.md"), "---\ntitle: A paper\ndocument: true\n---\n\n# A paper\n\nWords in the document.\n");

                new SiteBuilder(input, output).BuildAsync().GetAwaiter().GetResult();

                Assert.That(File.Exists(Path.Combine(output, "paper.html")), Is.True);
                foreach (var asset in new[] { "document.css", "document-curiosity.css", "document.js", "document-docx.js", "docx.bundle.js" })
                {
                    Assert.That(File.Exists(Path.Combine(output, "assets", asset)), Is.True, asset);
                }
                var home = File.ReadAllText(Path.Combine(output, "index.html"));
                Assert.That(home, Does.Not.Contain("paper.html"), "a document is not a sidebar entry");
                Assert.That(File.ReadAllText(Path.Combine(output, "search.json")), Does.Not.Contain("Words in the document"), "nor a search result");
            }
            finally
            {
                if (Directory.Exists(input)) Directory.Delete(input, true);
                if (Directory.Exists(output)) Directory.Delete(output, true);
            }
        }
    }
}
