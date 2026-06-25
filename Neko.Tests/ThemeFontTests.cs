using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    // Covers `theme.font` — the configurable base font. Neko itself pins no
    // typeface: with nothing configured it loads no web font and emits no
    // `font-family` rule, leaving the CSS preflight's system stack in place. A
    // site (e.g. the curiosity.ai blog) opts in via `theme.font` to set its brand
    // font and supply a stylesheet URL.
    public class ThemeFontTests
    {
        private static ParsedDocument Doc() => new ParsedDocument
        {
            Html = "<p>Content</p>",
            FrontMatter = new FrontMatter { Title = "Home" }
        };

        [Test]
        public void DefaultFont_PinsNothing()
        {
            var html = new HtmlGenerator(new NekoConfig()).Generate(Doc());

            // No bundled/default web font, and no base `:root` font-family rule
            // emitted by the engine — fonts are the content repo's choice, not Neko's.
            Assert.That(html, Does.Not.Contain("https://rsms.me/inter/inter.css"));
            Assert.That(html, Does.Not.Contain(":root { font-family:"));
        }

        [Test]
        public void BlogMode_DoesNotPinHeaderFont()
        {
            var config = new NekoConfig { Mode = "blog" };
            config.Theme.Font = new FontConfig
            {
                Family = "Plus Jakarta Sans",
                Url = "/assets/fonts/fonts.css"
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            // Blog mode used to pin the header to Inter from the engine; that now
            // lives in the content repo's own stylesheet. The engine emits only the
            // configured base font.
            Assert.That(html, Does.Not.Contain("https://rsms.me/inter/inter.css"));
            Assert.That(html, Does.Not.Contain("header { font-family"));
            Assert.That(html, Contains.Substring("font-family: 'Plus Jakarta Sans', sans-serif"));
        }

        [Test]
        public void ConfiguredFont_LoadsUrlAndSetsFamily_NotInter()
        {
            var config = new NekoConfig();
            config.Theme.Font = new FontConfig
            {
                Family = "Plus Jakarta Sans",
                Url = "https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans&display=swap"
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            // The href is HTML-attribute escaped (& -> &amp;).
            Assert.That(html, Contains.Substring("href=\"https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans&amp;display=swap\""));
            Assert.That(html, Contains.Substring("font-family: 'Plus Jakarta Sans', sans-serif"));
            // The default Inter font is not loaded when a font is configured.
            Assert.That(html, Does.Not.Contain("https://rsms.me/inter/inter.css"));
        }

        [Test]
        public void ConfiguredFont_WithoutUrl_OnlySetsFamily()
        {
            var config = new NekoConfig();
            config.Theme.Font = new FontConfig { Family = "Georgia" };

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("font-family: 'Georgia', sans-serif"));
            Assert.That(html, Does.Not.Contain("<link rel=\"stylesheet\" href=\"\">"));
            Assert.That(html, Does.Not.Contain("https://rsms.me/inter/inter.css"));
        }

        [Test]
        public void FontStackWithComma_IsNotDoubleQuoted()
        {
            var config = new NekoConfig();
            config.Theme.Font = new FontConfig { Family = "Inter, Helvetica, Arial" };

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("font-family: Inter, Helvetica, Arial, sans-serif"));
        }

        [Test]
        public void Font_IsInheritedFromParentWhenChildLeavesItUnset()
        {
            var parent = new NekoConfig();
            parent.Theme.Font = new FontConfig { Family = "Plus Jakarta Sans", Url = "https://example.com/f.css" };
            var child = new NekoConfig();

            child.MergeWith(parent);

            Assert.That(child.Theme.Font.Family, Is.EqualTo("Plus Jakarta Sans"));
            Assert.That(child.Theme.Font.Url, Is.EqualTo("https://example.com/f.css"));
        }
    }
}
