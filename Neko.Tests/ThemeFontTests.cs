using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    // Covers `theme.font` — the configurable base font. By default Neko loads its
    // bundled Inter; a site can override the family (and supply a stylesheet URL)
    // to match its brand, e.g. curiosity.ai's Plus Jakarta Sans.
    public class ThemeFontTests
    {
        private static ParsedDocument Doc() => new ParsedDocument
        {
            Html = "<p>Content</p>",
            FrontMatter = new FrontMatter { Title = "Home" }
        };

        [Test]
        public void DefaultFont_LoadsInter()
        {
            var html = new HtmlGenerator(new NekoConfig()).Generate(Doc());

            Assert.That(html, Contains.Substring("https://rsms.me/inter/inter.css"));
            Assert.That(html, Contains.Substring("font-family: 'Inter var', sans-serif"));
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
