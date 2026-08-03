using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    // `branding.link` overrides where the navbar brand (logo and/or title)
    // points — by default the site homepage (`/index`).
    [TestFixture]
    public class NavbarBrandLinkTests
    {
        private static ParsedDocument Doc() => new ParsedDocument
        {
            Html = "<p>Content</p>",
            FrontMatter = new FrontMatter { Title = "Page" }
        };

        private static NekoConfig DocsConfig() => new NekoConfig
        {
            Branding = new BrandingConfig { Title = "Test Docs" }
        };

        private static NekoConfig BlogConfig() => new NekoConfig
        {
            Mode = "blog",
            Branding = new BrandingConfig { Title = "Curiosity", Logo = "/assets/logo.png" }
        };

        [Test]
        public void DocsMode_BrandLinksToHomepage_ByDefault()
        {
            var html = new HtmlGenerator(DocsConfig()).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"/index\" class=\"font-bold text-xl hover:text-primary-600 transition-colors\">Test Docs</a>"));
        }

        [Test]
        public void DocsMode_BrandingLink_OverridesBrandHref()
        {
            var config = DocsConfig();
            config.Branding.Link = "https://example.com";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"https://example.com\" class=\"font-bold text-xl hover:text-primary-600 transition-colors\">Test Docs</a>"));
            Assert.That(html, Does.Not.Contain("<a href=\"/index\" class=\"font-bold text-xl"));
        }

        [Test]
        public void BlogMode_BrandingLink_OverridesWordmarkHref()
        {
            var config = BlogConfig();
            config.Branding.Link = "https://curiosity.ai";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"https://curiosity.ai\" class=\"flex items-center shrink-0\" aria-label=\"Curiosity\">"));
        }

        [Test]
        public void BlogMode_BrandLinksToHomepage_ByDefault()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"/index\" class=\"flex items-center shrink-0\" aria-label=\"Curiosity\">"));
        }

        [Test]
        public void BrandingLinkTarget_IsNormalizedAndGetsRelForBlank()
        {
            var config = DocsConfig();
            config.Branding.Link = "https://example.com";
            config.Branding.LinkTarget = "blank";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"font-bold text-xl"));
        }

        [Test]
        public void BrandingLink_WithoutTarget_EmitsNoTargetAttribute()
        {
            var config = DocsConfig();
            config.Branding.Link = "/overview";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"/overview\" class=\"font-bold text-xl"));
        }

        [Test]
        public void BrandingLink_HashMakesBrandInert()
        {
            var config = DocsConfig();
            config.Branding.Link = "#";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("<a href=\"#\" class=\"font-bold text-xl"));
        }

        [Test]
        public void BrandingLink_StripsMarkdownExtension_OnNormalize()
        {
            var config = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Test Docs", Link = "docs/overview.md" }
            };
            config.NormalizeLinks();

            Assert.That(config.Branding.Link, Is.EqualTo("docs/overview"));

            var html = new HtmlGenerator(config).Generate(Doc());
            Assert.That(html, Contains.Substring("<a href=\"docs/overview\" class=\"font-bold text-xl"));
        }

        [Test]
        public void BrandingLink_IsInheritedFromParentConfig()
        {
            var parent = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Parent", Link = "https://example.com", LinkTarget = "blank" }
            };
            var child = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Child" },
                Links = new List<LinkConfig>()
            };

            child.MergeWith(parent);

            Assert.That(child.Branding.Link, Is.EqualTo("https://example.com"));
            Assert.That(child.Branding.LinkTarget, Is.EqualTo("blank"));
        }

        [Test]
        public void BrandingLink_ChildOverrideWins_OverParentConfig()
        {
            var parent = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Parent", Link = "https://example.com" }
            };
            var child = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Child", Link = "/child/index" }
            };

            child.MergeWith(parent);

            Assert.That(child.Branding.Link, Is.EqualTo("/child/index"));
        }

        [Test]
        public void BrandingLink_ParsesFromYaml()
        {
            var yaml = "branding:\n  title: Test Docs\n  link: https://example.com\n  linkTarget: blank\n";
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "neko-brand-link-" + System.Guid.NewGuid().ToString("N") + ".yml");
            System.IO.File.WriteAllText(path, yaml);
            try
            {
                var config = ConfigParser.Parse(path);

                Assert.That(config.Branding.Link, Is.EqualTo("https://example.com"));
                Assert.That(config.Branding.LinkTarget, Is.EqualTo("blank"));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
