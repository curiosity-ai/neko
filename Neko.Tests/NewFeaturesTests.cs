using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    public class NewFeaturesTests
    {
        private HtmlGenerator _generator;
        private NekoConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new NekoConfig
            {
                Branding = new BrandingConfig
                {
                    Title = "Test Docs",
                    Favicon = "/favicon.ico",
                    Icon = "fi fi-rr-star"
                },
                Meta = new MetaConfig
                {
                    Description = "Test Description",
                    Keywords = "test, docs",
                    Author = "Test Author",
                    Image = "/logo.png",
                    Url = "https://example.com",
                    Type = "article",
                    TwitterCard = "summary_large_image",
                    TwitterSite = "@test",
                    TwitterCreator = "@author"
                }
            };
            _generator = new HtmlGenerator(_config);
        }

        [Test]
        public void TestFavicon()
        {
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };
            var html = _generator.Generate(doc);
            Assert.That(html, Contains.Substring("<link rel=\"icon\" href=\"/favicon.ico\">"));
        }

        [Test]
        public void TestFaviconHrefIsHtmlEscaped()
        {
            _config.Branding.Favicon = "/favicon.ico?v=1&cache=2";
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };
            var html = _generator.Generate(doc);
            Assert.That(html, Contains.Substring("<link rel=\"icon\" href=\"/favicon.ico?v=1&amp;cache=2\">"));
            Assert.That(html, Does.Not.Contain("/favicon.ico?v=1&cache=2\">"));
        }

        [Test]
        public void TestMetaTags()
        {
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };
            var html = _generator.Generate(doc);

            Assert.That(html, Contains.Substring("<meta name=\"description\" content=\"Test Description\">"));
            Assert.That(html, Contains.Substring("<meta name=\"keywords\" content=\"test, docs\">"));
            Assert.That(html, Contains.Substring("<meta name=\"author\" content=\"Test Author\">"));
            Assert.That(html, Contains.Substring("<meta property=\"og:image\" content=\"/logo.png\">"));
            Assert.That(html, Contains.Substring("<meta property=\"og:url\" content=\"https://example.com\">"));
            Assert.That(html, Contains.Substring("<meta property=\"og:type\" content=\"article\">"));
            Assert.That(html, Contains.Substring("<meta name=\"twitter:card\" content=\"summary_large_image\">"));
            Assert.That(html, Contains.Substring("<meta name=\"twitter:site\" content=\"@test\">"));
            Assert.That(html, Contains.Substring("<meta name=\"twitter:creator\" content=\"@author\">"));
        }

        [Test]
        public void TestNavbarIcon()
        {
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };
            var html = _generator.Generate(doc);

            // Should contain the icon class
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-star text-2xl text-primary-600 dark:text-primary-400\"></i>"));
        }

        [Test]
        public void TestNavbarLogoPrecedence()
        {
            _config.Branding.Logo = "/logo.png";
            // Re-init generator or just use modified config if passed by reference (it is)
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };
            var html = _generator.Generate(doc);

            // Should contain the logo image
            Assert.That(html, Contains.Substring("<img src=\"/logo.png\""));
            // Should NOT contain the icon class (because logo takes precedence in my implementation: if (Logo) ... else if (Icon) ...)
            Assert.That(html, Does.Not.Contain("<i class=\"fi fi-rr-star text-2xl"));
        }

        private static LinkConfig MakePivotGroup()
        {
            return new LinkConfig
            {
                Text = "Workspace",
                Items = new List<LinkConfig>
                {
                    new LinkConfig { Text = "Learn", Link = "/workspace/", Icon = "graduation-cap" },
                    new LinkConfig { Text = "Deploy", Link = "/workspace-deployment/" },
                    new LinkConfig { Text = "Build", Link = "/workspace-build/" },
                }
            };
        }

        [Test]
        public void TestPivotRendersForMatchingSection()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            // A page under /workspace-build should surface the pivot with all tabs.
            var html = _generator.Generate(doc, currentUrl: "/workspace-build/intro");

            Assert.That(html, Contains.Substring("aria-current=\"page\""));
            Assert.That(html, Contains.Substring(">Learn</a>"));
            Assert.That(html, Contains.Substring(">Deploy</a>"));
            Assert.That(html, Contains.Substring(">Build</a>"));
        }

        [Test]
        public void TestPivotHighlightsActiveItemWithoutPrefixCollision()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            var html = _generator.Generate(doc, currentUrl: "/workspace-build/intro");

            // /workspace-build must mark "Build" active, NOT "Learn" (/workspace),
            // even though "/workspace" is a string prefix of "/workspace-build".
            Assert.That(html, Contains.Substring("aria-current=\"page\" class=\"border-primary-600 text-primary-600 dark:text-primary-400 dark:border-primary-400 flex items-center gap-2 border-b-2 py-3 whitespace-nowrap transition-colors\">Build</a>"));
            Assert.That(html, Does.Not.Contain("aria-current=\"page\" class=\"border-primary-600 text-primary-600 dark:text-primary-400 dark:border-primary-400 flex items-center gap-2 border-b-2 py-3 whitespace-nowrap transition-colors\">Learn</a>"));
        }

        [Test]
        public void TestPivotHiddenOutsideSection()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            // A page outside any pivot section (e.g. the home page) shows no pivot bar.
            var html = _generator.Generate(doc, currentUrl: "/");

            Assert.That(html, Does.Not.Contain("aria-current=\"page\""));
        }

        [Test]
        public void TestNavIconsHiddenByDefault()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            var html = _generator.Generate(doc, currentUrl: "/workspace-build/intro");

            // "Learn" carries a graduation-cap icon; with icons off by default it
            // must not appear in the pivot bar or the dropdown.
            Assert.That(html, Does.Not.Contain("fi-rr-graduation-cap"));
        }

        [Test]
        public void TestPivotIconsEnabled()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            _config.Nav = new NavConfig { PivotIcons = true };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            var html = _generator.Generate(doc, currentUrl: "/workspace-build/intro");

            Assert.That(html, Contains.Substring("fi-rr-graduation-cap"));
        }

        [Test]
        public void TestDropdownIconsEnabled()
        {
            _config.Links = new List<LinkConfig> { MakePivotGroup() };
            _config.Nav = new NavConfig { DropdownIcons = true };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            // On a page outside the section the pivot is hidden, so the only place
            // the icon can come from is the dropdown flyout.
            var html = _generator.Generate(doc, currentUrl: "/");

            Assert.That(html, Contains.Substring("fi-rr-graduation-cap"));
        }

        [Test]
        public void TestHeaderIconsEnabled()
        {
            var group = MakePivotGroup();
            group.Icon = "building";
            _config.Links = new List<LinkConfig> { group };
            _config.Nav = new NavConfig { HeaderIcons = true };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            var html = _generator.Generate(doc, currentUrl: "/");

            // The dropdown trigger button shows its icon when header icons are on.
            Assert.That(html, Contains.Substring("fi-rr-building"));
        }

        [Test]
        public void TestPivotNotRenderedForGroupWithoutItems()
        {
            // A plain top-nav link (no items) is never a pivot section.
            _config.Links = new List<LinkConfig>
            {
                new LinkConfig { Text = "Home", Link = "/workspace-build/" }
            };
            var doc = new ParsedDocument { Html = "<p>Content</p>", FrontMatter = new FrontMatter { Title = "Page" } };

            var html = _generator.Generate(doc, currentUrl: "/workspace-build/intro");

            Assert.That(html, Does.Not.Contain("aria-current=\"page\""));
        }
    }
}
