using System;
using System.IO;
using System.Linq;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Unit coverage for presentation mode: the front-matter marker, the slide
    /// splitter, per-slide attributes, and the generated deck document.
    /// </summary>
    [TestFixture]
    public class PresentationTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        [Test]
        public void PresentationMarker_BareTrue_EnablesDeckWithDefaults()
        {
            var doc = NewParser().Parse("---\ntitle: T\npresentation: true\n---\n\n# One\n");

            Assert.That(doc.IsPresentation, Is.True);
            Assert.That(doc.Presentation!.Theme, Is.EqualTo("midnight"));
            Assert.That(doc.Presentation.Accent, Is.EqualTo("cyan"));
            Assert.That(doc.Presentation.Grid, Is.True);
        }

        [Test]
        public void PresentationMarker_Mapping_ReadsDeckOptions()
        {
            var doc = NewParser().Parse(
                "---\ntitle: T\npresentation:\n  eyebrow: Curiosity\n  theme: daylight\n  accent: rose\n  grid: false\n  counter: no\n  back: /talks\n---\n\n# One\n");

            Assert.That(doc.IsPresentation, Is.True);
            Assert.That(doc.Presentation!.Eyebrow, Is.EqualTo("Curiosity"));
            Assert.That(doc.Presentation.Theme, Is.EqualTo("daylight"));
            Assert.That(doc.Presentation.Accent, Is.EqualTo("rose"));
            Assert.That(doc.Presentation.Grid, Is.False);
            Assert.That(doc.Presentation.Counter, Is.False);
            Assert.That(doc.Presentation.Back, Is.EqualTo("/talks"));
        }

        [TestCase("false")]
        [TestCase("no")]
        [TestCase("off")]
        [TestCase("none")]
        public void PresentationMarker_Falsey_BuildsAnOrdinaryPage(string value)
        {
            var doc = NewParser().Parse($"---\ntitle: T\npresentation: {value}\n---\n\n# One\n");

            Assert.That(doc.IsPresentation, Is.False);
            Assert.That(doc.Html, Does.Contain("<h1"));
        }

        [Test]
        public void Split_SeparatesSlidesOnHorizontalRules()
        {
            var slides = PresentationParser.Split("# One\n\nLead.\n\n---\n\n## Two\n\nLead.\n\n---\n\n## Three\n");

            Assert.That(slides.Count, Is.EqualTo(3));
            Assert.That(slides[0].Markdown, Does.StartWith("# One"));
            Assert.That(slides[1].Markdown, Does.StartWith("## Two"));
            Assert.That(slides[2].Markdown, Does.StartWith("## Three"));
        }

        [Test]
        public void Split_ReadsAttributesFromTheSeparator()
        {
            var slides = PresentationParser.Split("# One\n\n--- {eyebrow=\"The problem\" accent=rose layout=\"center\"}\n\n## Two\n");

            Assert.That(slides.Count, Is.EqualTo(2));
            Assert.That(slides[1].Get("eyebrow"), Is.EqualTo("The problem"));
            Assert.That(slides[1].Get("accent"), Is.EqualTo("rose"));
            Assert.That(slides[1].Get("layout"), Is.EqualTo("center"));
        }

        [Test]
        public void Split_LeadingSeparatorGivesTheFirstSlideItsAttributes()
        {
            var slides = PresentationParser.Split("\n--- {eyebrow=\"Where we are\"}\n\n# One\n\n---\n\n## Two\n");

            Assert.That(slides.Count, Is.EqualTo(2), "a leading separator must not open an empty slide");
            Assert.That(slides[0].Get("eyebrow"), Is.EqualTo("Where we are"));
            Assert.That(slides[0].Markdown, Does.StartWith("# One"));
        }

        [Test]
        public void Split_IgnoresSeparatorsInsideFencedBlocks()
        {
            var body = "# One\n\n```embed\n<svg>\n---\n</svg>\n```\n\n---\n\n## Two\n";
            var slides = PresentationParser.Split(body);

            Assert.That(slides.Count, Is.EqualTo(2));
            Assert.That(slides[0].Markdown, Does.Contain("---"), "the fenced rule stays in slide one");
        }

        [Test]
        public void Split_KeepsSetextHeadingsIntact()
        {
            // `Title` followed by `---` with no blank line between is a setext H2,
            // not a slide break.
            var slides = PresentationParser.Split("# One\n\nA setext heading\n---\n\nBody.\n");

            Assert.That(slides.Count, Is.EqualTo(1));
        }

        [Test]
        public void StripFrontMatter_RemovesOnlyTheLeadingBlock()
        {
            var body = PresentationParser.StripFrontMatter("---\ntitle: T\npresentation: true\n---\n\n# One\n\n---\n\n## Two\n");

            Assert.That(body, Does.StartWith("\n# One"));
            Assert.That(body, Does.Contain("## Two"));
        }

        [Test]
        public void Parse_RendersEachSlideThroughTheFullPipeline()
        {
            var markdown = "---\npresentation: true\n---\n\n# One\n\n--- {eyebrow=\"Two up\"}\n\n:::: cols\n::: box {title=\"Left\" tone=\"warn\"}\n- point\n:::\n::::\n\nTerm\n:   Definition.\n\n> A claim.\n\n::: note {tone=\"limit\"}\nA caveat.\n:::\n";
            var doc = NewParser().Parse(markdown);

            Assert.That(doc.Slides, Is.Not.Null);
            Assert.That(doc.Slides!.Count, Is.EqualTo(2));

            var second = doc.Slides[1].Html!;
            Assert.That(second, Does.Contain("deck-cols"));
            Assert.That(second, Does.Contain("deck-box"));
            Assert.That(second, Does.Contain("data-tone=\"warn\""));
            Assert.That(second, Does.Contain("<dl>"), "definition lists carry the key/value rows");
            Assert.That(second, Does.Contain("<blockquote>"), "a blockquote is the slide's claim");
            Assert.That(second, Does.Contain("deck-note"));
            Assert.That(second, Does.Contain("data-tone=\"limit\""));
        }

        [Test]
        public void DeckContainers_StayGenericOutsideAPresentation()
        {
            // `::: name` is Neko's generic container everywhere else, so presentation
            // mode must not claim ordinary words like `note` and `box` site-wide.
            var html = NewParser().Parse("::: note\nAn ordinary container.\n:::\n\n::: box\nAnother one.\n:::\n").Html!;

            Assert.That(html, Does.Not.Contain("deck-note"));
            Assert.That(html, Does.Not.Contain("deck-box"));
            Assert.That(html, Does.Contain("class=\"note\""));
            Assert.That(html, Does.Contain("class=\"box\""));
        }

        [Test]
        public void EmbedFence_EmitsRawMarkupVerbatim()
        {
            var doc = NewParser().Parse("---\npresentation: true\n---\n\n# One\n\n```embed\n<svg viewBox=\"0 0 10 10\">\n\n  <rect x=\"1\"/>\n</svg>\n```\n");

            var html = doc.Slides![0].Html!;
            Assert.That(html, Does.Contain("<svg viewBox=\"0 0 10 10\">"));
            Assert.That(html, Does.Contain("<rect x=\"1\"/>"));
            Assert.That(html, Does.Not.Contain("&lt;svg"), "an embed fence must not be escaped");
        }

        [Test]
        public void Generate_ProducesAFullScreenDeckWithoutTheDocumentationShell()
        {
            var config = new NekoConfig();
            config.Branding.Title = "Demo";
            var doc = NewParser().Parse("---\ntitle: My deck\npresentation:\n  eyebrow: Demo\n---\n\n# One\n\n---\n\n## Two\n");
            var html = new HtmlGenerator(config).GeneratePresentation(doc);

            Assert.That(html, Does.Contain("neko-deck-html"));
            Assert.That(html, Does.Contain("/assets/presentation.css"));
            Assert.That(html, Does.Contain("/assets/presentation.js"));
            Assert.That(html, Does.Contain("<title>My deck</title>"));

            Assert.That(html, Does.Contain("id=\"slide-1\""));
            Assert.That(html, Does.Contain("id=\"slide-2\""));
            Assert.That(html, Does.Contain("data-layout=\"title\""), "a slide opening on an H1 is a title slide");
            Assert.That(html, Does.Contain("deck-eyebrow"));
            Assert.That(html, Does.Contain("id=\"deck-back\""), "a deck must draw its own way out");

            Assert.That(html, Does.Not.Contain("id=\"sidebar-list\""));
            Assert.That(html, Does.Not.Contain("id=\"toc-list\""));
        }

        [Test]
        public void Generate_DeckWithAPassword_ShipsOnlyAnEncryptedPayload()
        {
            var config = new NekoConfig();
            config.Branding.Title = "Demo";
            var doc = NewParser().Parse("---\ntitle: Secret deck\npassword: hunter2\npresentation: true\n---\n\n# The secret heading\n\nSecret body.\n");
            var html = new HtmlGenerator(config).GeneratePresentation(doc);

            Assert.That(html, Does.Contain("id=\"encrypted-data\""));
            Assert.That(html, Does.Contain("id=\"password-input\""));
            Assert.That(html, Does.Not.Contain("The secret heading"));
            Assert.That(html, Does.Not.Contain("Secret body."));
            Assert.That(html, Does.Contain("<title>Demo</title>"), "the real title would give the deck away");
            // The chrome stays outside the payload so it frames the unlock prompt.
            Assert.That(html, Does.Contain("id=\"deck-back\""));
        }

        [Test]
        public void DeckComponent_RendersAFramedIframePreview()
        {
            var doc = NewParser().Parse("[!deck link=\"decks/foo.md\" title=\"A deck\" description=\"Caption.\" slide=\"3\" ratio=\"4:3\"]");
            var html = doc.Html!;

            Assert.That(html, Does.Contain("neko-deck-card"));
            Assert.That(html, Does.Contain("<iframe"));
            Assert.That(html, Does.Contain("src=\"decks/foo#slide-3\""), "the .md suffix is stripped and the slide anchor applied");
            Assert.That(html, Does.Contain("aspect-[4/3]"));
            Assert.That(html, Does.Contain("A deck"));
            Assert.That(html, Does.Contain("Caption."));
            Assert.That(html, Does.Contain("loading=\"lazy\""));
        }

        [Test]
        public void DeckComponent_DefaultsToSixteenByNineWithMacOsChrome()
        {
            var html = NewParser().Parse("[!deck link=\"/decks/foo\"]").Html!;

            Assert.That(html, Does.Contain("aspect-video"));
            Assert.That(html, Does.Contain("rounded-full bg-red-400"), "the macOS traffic lights");

            var bare = NewParser().Parse("[!deck link=\"/decks/foo\" chrome=\"none\"]").Html!;
            Assert.That(bare, Does.Not.Contain("rounded-full bg-red-400"));
        }

        [Test]
        public void TagComponent_RendersATonedChip()
        {
            var html = NewParser().Parse("[!tag text=\"reject\" tone=\"stop\"]").Html!;

            Assert.That(html, Does.Contain("deck-tag"));
            Assert.That(html, Does.Contain("data-tone=\"stop\""));
            Assert.That(html, Does.Contain("reject"));
        }

        [Test]
        public void Build_KeepsDecksOutOfSearchAndTheSidebar()
        {
            var inputDir = Path.Combine(Path.GetTempPath(), "neko-deck-in-" + Guid.NewGuid().ToString("N"));
            var outDir = Path.Combine(Path.GetTempPath(), "neko-deck-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(inputDir);

            try
            {
                File.WriteAllText(Path.Combine(inputDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
                File.WriteAllText(Path.Combine(inputDir, "index.md"), "---\ntitle: Home\n---\n# Home\n\n[!deck link=\"/talk\" title=\"The talk\"]\n");
                File.WriteAllText(Path.Combine(inputDir, "talk.md"),
                    "---\ntitle: The talk\npresentation: true\n---\n\n# A distinctive deck heading\n\nBody.\n");

                new SiteBuilder(inputDir, outDir).BuildAsync().GetAwaiter().GetResult();

                Assert.That(File.Exists(Path.Combine(outDir, "talk.html")), Is.True, "the deck page is still built");

                var search = File.ReadAllText(Path.Combine(outDir, "search.json"));
                Assert.That(search, Does.Not.Contain("A distinctive deck heading"));
                Assert.That(search, Does.Not.Contain("talk.html"));

                var home = File.ReadAllText(Path.Combine(outDir, "index.html"));
                var sidebarStart = home.IndexOf("id=\"sidebar-list\"", StringComparison.Ordinal);
                Assert.That(sidebarStart, Is.GreaterThan(-1));
                var sidebarEnd = home.IndexOf("</aside>", sidebarStart, StringComparison.Ordinal);
                Assert.That(sidebarEnd, Is.GreaterThan(sidebarStart));
                var sidebar = home.Substring(sidebarStart, sidebarEnd - sidebarStart);
                Assert.That(sidebar, Does.Not.Contain("href=\"/talk\""), "a deck never joins the sidebar");

                // ...but the page that embeds it still links it.
                Assert.That(home, Does.Contain("neko-deck-card"));

                // The deck's own assets are emitted.
                Assert.That(File.Exists(Path.Combine(outDir, "assets", "presentation.css")), Is.True);
                Assert.That(File.Exists(Path.Combine(outDir, "assets", "presentation.js")), Is.True);
            }
            finally
            {
                try { Directory.Delete(inputDir, true); } catch { }
                try { Directory.Delete(outDir, true); } catch { }
            }
        }
    }
}
