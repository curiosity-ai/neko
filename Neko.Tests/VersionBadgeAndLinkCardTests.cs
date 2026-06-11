using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class VersionBadgeAndLinkCardTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        // ---- version-badge ----

        [Test]
        public void VersionBadge_RendersTextAndVersion()
        {
            var doc = _parser.Parse("[!version-badge text=\"Curiosity.FrontEnd\" version=\"v26.6.1753\"]");

            Assert.That(doc.Html, Contains.Substring("neko-version-badge"));
            Assert.That(doc.Html, Contains.Substring("Curiosity.FrontEnd"));
            Assert.That(doc.Html, Contains.Substring("v26.6.1753"));
        }

        [Test]
        public void VersionBadge_LinksToUrl()
        {
            var doc = _parser.Parse("[!version-badge text=\"Pkg\" version=\"v1.0\" url=\"https://example.com/pkg\"]");

            Assert.That(doc.Html, Contains.Substring("href=\"https://example.com/pkg\""));
        }

        [Test]
        public void VersionBadge_HasCopyButtonDefaultingToVersion()
        {
            var doc = _parser.Parse("[!version-badge text=\"Pkg\" version=\"v1.2.3\"]");

            Assert.That(doc.Html, Contains.Substring("neko-copy-btn"));
            Assert.That(doc.Html, Contains.Substring("data-copy=\"v1.2.3\""));
        }

        [Test]
        public void VersionBadge_CopyOverrideIsHonoured()
        {
            var doc = _parser.Parse("[!version-badge text=\"Docker\" version=\"img:1\" copy=\"curiosityai/curiosity:67298\"]");

            Assert.That(doc.Html, Contains.Substring("data-copy=\"curiosityai/curiosity:67298\""));
        }

        [Test]
        public void VersionBadge_Consecutive_RenderAsSeparatePills()
        {
            var doc = _parser.Parse("[!version-badge text=\"A\" version=\"v1\"] [!version-badge text=\"B\" version=\"v2\"]");

            var count = System.Text.RegularExpressions.Regex.Matches(doc.Html, "neko-version-badge").Count;
            Assert.That(count, Is.EqualTo(2), "Expected two separate version badges");
            // inline-flex keeps them on the same line / wrapping.
            Assert.That(doc.Html, Contains.Substring("inline-flex"));
        }

        // ---- link-card (```links) ----

        [Test]
        public void LinkCard_RendersTitleIconAndRows()
        {
            var md = "```links title=\"Current packages\" icon=\"box\"\n"
                   + "Workspace · Docker | https://example.com/docker | curiosityai/curiosity:67298\n"
                   + "Curiosity.FrontEnd | https://example.com/fe | v26.6.1753\n"
                   + "```";
            var doc = _parser.Parse(md);

            Assert.That(doc.Html, Contains.Substring("neko-link-card"));
            Assert.That(doc.Html, Contains.Substring("Current packages"));
            Assert.That(doc.Html, Contains.Substring("fi-rr-box"));
            Assert.That(doc.Html, Contains.Substring("href=\"https://example.com/docker\""));
            Assert.That(doc.Html, Contains.Substring("curiosityai/curiosity:67298"));
            Assert.That(doc.Html, Contains.Substring("v26.6.1753"));
        }

        [Test]
        public void LinkCard_RowWithoutUrl_RendersLabelWithoutLink()
        {
            var md = "```links title=\"Versions\"\nEngine | | v26.6\n```";
            var doc = _parser.Parse(md);

            Assert.That(doc.Html, Contains.Substring("Engine"));
            Assert.That(doc.Html, Contains.Substring("v26.6"));
            // No anchor should be produced for a row without a url.
            Assert.That(doc.Html, Does.Not.Contain("<a href=\"\""));
        }

        // ---- change container (changelog entry) ----

        [Test]
        public void ChangeContainer_RendersBadgeTitleAndBody()
        {
            var md = "::: change {badge=\"New\" title=\"Shiny feature\"}\nIt does great things.\n:::";
            var doc = _parser.Parse(md);

            Assert.That(doc.Html, Contains.Substring("neko-change"));
            Assert.That(doc.Html, Contains.Substring("New"));
            Assert.That(doc.Html, Contains.Substring("Shiny feature"));
            Assert.That(doc.Html, Contains.Substring("It does great things."));
            // "New" maps to the primary palette.
            Assert.That(doc.Html, Contains.Substring("bg-primary-100"));
        }

        [Test]
        public void ChangeContainer_FixedBadgeUsesGreenPalette()
        {
            var md = "::: change {badge=\"Fixed\" title=\"A fix\"}\nResolved.\n:::";
            var doc = _parser.Parse(md);

            Assert.That(doc.Html, Contains.Substring("bg-green-100"));
        }
    }
}
