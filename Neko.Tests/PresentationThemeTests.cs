using System;
using System.Linq;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Deck themes and the slide components added with the curiosity theme: the
    /// `--theme` override, the theme's stylesheet and typefaces, slide grounds and
    /// generated art, and the steps / stats / agenda / timeline / quote / compare
    /// containers.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class PresentationThemeTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        [TearDown]
        public void ResetOverride() => PresentationOptions.ThemeOverride = null;

        private static string Render(string markdown)
        {
            var doc = NewParser().Parse(markdown);
            Assert.That(doc.IsPresentation, Is.True);
            return new HtmlGenerator(new NekoConfig()).GeneratePresentation(doc);
        }

        [Test]
        public void BuiltInThemes_AreMidnightDaylightAndCuriosity()
        {
            Assert.That(PresentationOptions.BuiltInThemes, Is.EquivalentTo(new[] { "midnight", "daylight", "curiosity" }));
            Assert.That(PresentationOptions.IsBuiltInTheme("Curiosity"), Is.True);
            Assert.That(PresentationOptions.IsBuiltInTheme("nope"), Is.False);
        }

        [Test]
        public void ThemeOverride_ReplacesTheFrontMatterTheme()
        {
            PresentationOptions.ThemeOverride = "curiosity";
            var doc = NewParser().Parse("---\ntitle: T\npresentation:\n  theme: daylight\n---\n\n# One\n");
            Assert.That(doc.Presentation!.Theme, Is.EqualTo("curiosity"));

            PresentationOptions.ThemeOverride = null;
            doc = NewParser().Parse("---\ntitle: T\npresentation:\n  theme: daylight\n---\n\n# One\n");
            Assert.That(doc.Presentation!.Theme, Is.EqualTo("daylight"));
        }

        [Test]
        public void CuriosityTheme_LinksItsStylesheetAndTypefaces()
        {
            var html = Render("---\ntitle: T\npresentation:\n  theme: curiosity\n---\n\n# One\n");
            Assert.That(html, Does.Contain("data-deck-theme=\"curiosity\""));
            Assert.That(html, Does.Contain("/assets/presentation-curiosity.css"));
            // The theme's typefaces ship with Neko: no font host by default.
            Assert.That(html, Does.Contain("/assets/deckfonts/deck-fonts.css"));
            Assert.That(html, Does.Not.Contain("fonts.googleapis.com"));
            Assert.That(html, Does.Not.Contain("family=Archivo"));
        }

        [Test]
        public void CuriosityTheme_FontsGoogle_PullsTheTypefacesFromGoogleFonts()
        {
            var html = Render("---\ntitle: T\npresentation:\n  theme: curiosity\n  fonts: google\n---\n\n# One\n");
            Assert.That(html, Does.Contain("family=Schibsted+Grotesk"));
            Assert.That(html, Does.Contain("family=Geist+Mono"));
            Assert.That(html, Does.Not.Contain("deckfonts/deck-fonts.css"));
        }

        [Test]
        public void CuriosityTheme_FontsNone_LinksNoTypefaces()
        {
            var html = Render("---\ntitle: T\npresentation:\n  theme: curiosity\n  fonts: none\n---\n\n# One\n");
            Assert.That(html, Does.Not.Contain("deckfonts/deck-fonts.css"));
            Assert.That(html, Does.Not.Contain("fonts.googleapis.com"));
        }

        [Test]
        public void MidnightTheme_KeepsItsOwnTypefaces_AndNoThemeStylesheet()
        {
            var html = Render("---\ntitle: T\npresentation: true\n---\n\n# One\n");
            Assert.That(html, Does.Not.Contain("presentation-curiosity.css"));
            Assert.That(html, Does.Contain("family=Archivo"));
        }

        [Test]
        public void SlideGroundAndArt_AreRendered_AndEverySlideHasAFoot()
        {
            var html = Render("---\ntitle: T\npresentation: true\n---\n\n--- {layout=\"cover\" art=\"field\" ground=\"ink\"}\n\n# One\n\n---\n\n## Two\n");
            Assert.That(html, Does.Contain("data-ground=\"ink\""));
            Assert.That(html, Does.Contain("<div class=\"deck-art\" data-art=\"field\""));
            Assert.That(html, Does.Contain("class=\"art-sig\""), "the field carries its signal square");
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "class=\"deck-slide-foot\"").Count, Is.EqualTo(2));
            Assert.That(html, Does.Contain("<span class=\"deck-slide-num\">02</span>"));
        }

        [TestCase("field")]
        [TestCase("bars")]
        [TestCase("squares")]
        [TestCase("glyph")]
        [TestCase("stair")]
        public void DeckArt_RendersEveryKind_Deterministically(string kind)
        {
            var a = DeckArt.Render(kind, 960, 540, 3, "graph");
            var b = DeckArt.Render(kind, 960, 540, 3, "graph");
            Assert.That(a, Does.StartWith("<svg class=\"deck-art-svg\""));
            Assert.That(a, Is.EqualTo(b), "the same inputs draw the same art");
        }

        [Test]
        public void DeckArt_Glyph_ReadsNamesAndCells()
        {
            Assert.That(DeckArt.ResolveGlyph("graph"), Is.EqualTo(DeckArt.Glyphs["graph"]));
            Assert.That(DeckArt.ResolveGlyph("kkkk............"), Is.EqualTo("kkkk............"));
            Assert.That(DeckArt.ResolveGlyph("not-a-glyph"), Is.EqualTo(DeckArt.Glyphs["curious"]));
            var svg = DeckArt.Render("glyph", glyph: "k..o............");
            Assert.That(svg.Split("<rect").Length - 1, Is.EqualTo(2));
            Assert.That(svg, Does.Contain("class=\"art-sig\""));
            Assert.That(DeckArt.Render("nope"), Is.Null);
        }

        [Test]
        public void DeckContainers_RenderTheirStructure()
        {
            var html = Render(@"---
title: T
presentation: true
---

## Components

:::: steps {style=""stairs""}
::: step {label=""Describe"" art=""glyph"" glyph=""graph""}
### Step one
One line
:::
::::

:::: stats {style=""cards""}
::: stat {value=""30TB+"" label=""Data connected"" ground=""deep""}
Context
:::
::::

::: agenda
1. **Where we are** A line
:::

:::: timeline
::: milestone {when=""Q3 2026"" state=""now""}
**Milestone**
:::
::::

::: quote {by=""Company"" context=""Team"" art=""squares""}
Their words.
:::

::: compare {highlight=""3""}
| A | B | C |
| --- | --- | --- |
| 1 | 2 | 3 |
:::

::: box {title=""T"" label=""01 · Topic"" ground=""ink"" art=""bars""}
Body
:::

[!tag text=""Presenter"" tone=""solid""]
");
            Assert.That(html, Does.Contain("<div class=\"deck-steps\" data-style=\"stairs\">"));
            Assert.That(html, Does.Contain("<div class=\"deck-step\"><div class=\"deck-art-block\" data-art=\"glyph\""));
            Assert.That(html, Does.Contain("<span class=\"deck-step-name\">Describe</span>"));
            Assert.That(html, Does.Contain("<div class=\"deck-stats\" data-style=\"cards\">"));
            Assert.That(html, Does.Contain("<div class=\"deck-stat\" data-ground=\"deep\"><p class=\"deck-stat-value\">30TB+</p><p class=\"deck-stat-label\">Data connected</p>"));
            Assert.That(html, Does.Contain("<div class=\"deck-agenda\">"));
            Assert.That(html, Does.Contain("<div class=\"deck-milestone\" data-state=\"now\">"));
            Assert.That(html, Does.Contain("<p class=\"deck-milestone-when\">Q3 2026</p>"));
            Assert.That(html, Does.Contain("<figcaption>Company &#183; Team</figcaption>"));
            Assert.That(html, Does.Contain("<div class=\"deck-compare\" data-highlight=\"3\">"));
            Assert.That(html, Does.Contain("<div class=\"deck-box\" data-tone=\"default\" data-ground=\"ink\"><div class=\"deck-art-block\" data-art=\"bars\""));
            Assert.That(html, Does.Contain("<p class=\"deck-box-label\">01 &#183; Topic</p>"));
            Assert.That(html, Does.Contain("<span class=\"deck-tag\" data-tone=\"solid\">Presenter</span>"));
        }

        [Test]
        public void DeckContainers_StayGenericOutsideADeck()
        {
            var doc = NewParser().Parse("# Page\n\n::: stats\nhello\n:::\n");
            Assert.That(doc.Html, Does.Not.Contain("deck-stats"));
        }
    }
}
