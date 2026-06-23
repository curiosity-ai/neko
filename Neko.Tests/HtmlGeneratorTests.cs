using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    public class HtmlGeneratorTests
    {
        private HtmlGenerator _generator;
        private NekoConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Test Docs" },
                Links = new System.Collections.Generic.List<LinkConfig>
                {
                    new LinkConfig { Text = "Home", Link = "/" }
                }
            };
            _generator = new HtmlGenerator(_config);
        }

        [Test]
        public void TestGenerate()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var sidebar = new System.Collections.Generic.List<LinkConfig>
            {
                new LinkConfig { Text = "Home", Link = "/" }
            };

            // Icons are opted into via nav.icons.mode; "all" reserves the spacer.
            _config.Nav.Icons.Mode = "all";

            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            Assert.That(html, Contains.Substring("<title>Test Docs - Page Title</title>"));
            // Updated assertion for branding in header
            Assert.That(html, Contains.Substring("<a href=\"/index\" class=\"font-bold text-xl hover:text-primary-600 transition-colors\">Test Docs</a>"));
            // Updated assertion for link with new classes
            Assert.That(html, Contains.Substring("<a href=\"/\" class=\"block py-1 px-2 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2 text-[13px] text-gray-700 dark:text-gray-300 truncate\"><i class=\"fi fi-rr-circle opacity-0\"></i> <span class=\"truncate\">Home</span></a>"));
            Assert.That(html, Contains.Substring("<p>Content</p>"));
        }

        [Test]
        public void TestPasswordProtectedPageHidesPromptByDefault()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Secret content</p>",
                FrontMatter = new FrontMatter { Title = "Secret", Password = "letmein" }
            };

            var html = _generator.Generate(doc, currentUrl: "/secret");

            // The plaintext must not ship in the page; only the encrypted payload.
            Assert.That(html, Does.Not.Contain("<p>Secret content</p>"));
            Assert.That(html, Contains.Substring("id=\"encrypted-data\""));
            Assert.That(html, Contains.Substring("/assets/password.js"));

            // The prompt is hidden by default so navigating with a cached session
            // key never flashes it; password.js reveals it only on a cache miss.
            Assert.That(html, Contains.Substring("id=\"password-form-container\" class=\"hidden "));
            // The old global-password flag is gone (key caching is keyed by salt).
            Assert.That(html, Does.Not.Contain("nekoIsGlobalPassword"));
        }

        [Test]
        public void TestPageLinksRenderedAboveToc()
        {
            _config.Url = "docs.example.com";
            _config.PageLinks = new System.Collections.Generic.List<PageLinkConfig>
            {
                new PageLinkConfig
                {
                    Label = "Report an issue",
                    Icon = "bug",
                    Url = "https://github.com/curiosity-ai/neko/issues/new?title=Issue%20on%20page%20${url}",
                    Target = "blank"
                }
            };
            var generator = new HtmlGenerator(_config);

            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" },
                Toc = new System.Collections.Generic.List<TocItem>
                {
                    new TocItem { Id = "section", Title = "Section", Level = 2 }
                }
            };

            var html = generator.Generate(doc, currentUrl: "/guides/install");

            Assert.That(html, Contains.Substring("class=\"neko-page-links"));
            Assert.That(html, Contains.Substring("neko-page-link"));
            Assert.That(html, Contains.Substring("fi-rr-bug"));
            Assert.That(html, Contains.Substring(">Report an issue<"));
            // ${url} substituted with URL-encoded absolute URL in the fallback href.
            Assert.That(html, Contains.Substring("https%3A%2F%2Fdocs.example.com%2Fguides%2Finstall"));
            // Template preserved verbatim for click-time resolution.
            Assert.That(html, Contains.Substring("data-neko-link-template=\"https://github.com/curiosity-ai/neko/issues/new?title=Issue%20on%20page%20${url}\""));
            Assert.That(html, Contains.Substring("target=\"_blank\""));
            // Page-link list should sit above the "On this page" heading.
            var pageLinksIdx = html.IndexOf("neko-page-links", System.StringComparison.Ordinal);
            var headingIdx = html.IndexOf("On this page", System.StringComparison.Ordinal);
            Assert.That(pageLinksIdx, Is.GreaterThan(-1));
            Assert.That(headingIdx, Is.GreaterThan(pageLinksIdx));
        }

        [Test]
        public void TestPageLinksHiddenWithoutToc()
        {
            _config.PageLinks = new System.Collections.Generic.List<PageLinkConfig>
            {
                new PageLinkConfig { Label = "Report an issue", Url = "https://example.com" }
            };
            var generator = new HtmlGenerator(_config);

            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = generator.Generate(doc, currentUrl: "/no-toc");

            Assert.That(html, Does.Not.Contain("neko-page-links"));
        }

        [Test]
        public void TestPageLinksConfigParsing()
        {
            var yaml = @"
pageLinks:
  - label: Report an issue
    icon: bug
    url: ""https://github.com/o/r/issues/new?title=${page}&body=${selection}&page=${url}""
    target: blank
";
            var tempFile = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, yaml);
            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.PageLinks, Has.Count.EqualTo(1));
                Assert.That(config.PageLinks[0].Label, Is.EqualTo("Report an issue"));
                Assert.That(config.PageLinks[0].Icon, Is.EqualTo("bug"));
                Assert.That(config.PageLinks[0].Url, Contains.Substring("${page}"));
                Assert.That(config.PageLinks[0].Url, Contains.Substring("${selection}"));
                Assert.That(config.PageLinks[0].Url, Contains.Substring("${url}"));
                Assert.That(config.PageLinks[0].Target, Is.EqualTo("blank"));
            }
            finally
            {
                System.IO.File.Delete(tempFile);
            }
        }

        [Test]
        public void TestSidebarIcons()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var sidebar = new System.Collections.Generic.List<LinkConfig>
            {
                new LinkConfig { Text = "With Icon", Link = "/with-icon", Icon = "home" },
                new LinkConfig { Text = "Without Icon", Link = "/without-icon" }
            };

            // Icons are opt-in: with nav.icons.mode = all both render (icon + spacer).
            _config.Nav.Icons.Mode = "all";

            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            // Check item with icon
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-home\"></i> <span class=\"truncate\">With Icon</span>"));

            // Check item without icon (should have invisible circle)
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-circle opacity-0\"></i> <span class=\"truncate\">Without Icon</span>"));
        }

        [Test]
        public void TestSidebarIconsHiddenByDefault()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var sidebar = new System.Collections.Generic.List<LinkConfig>
            {
                new LinkConfig { Text = "With Icon", Link = "/with-icon", Icon = "home" },
                new LinkConfig { Text = "Without Icon", Link = "/without-icon" }
            };

            // Default nav.icons.mode is "none": no icons and no reserved spacer.
            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            Assert.That(html, Does.Not.Contain("<i class=\"fi fi-rr-home\"></i> <span class=\"truncate\">With Icon</span>"));
            Assert.That(html, Does.Not.Contain("<i class=\"fi fi-rr-circle opacity-0\"></i> <span class=\"truncate\">Without Icon</span>"));
            // The link itself still renders, just without an icon element.
            Assert.That(html, Contains.Substring("<span class=\"truncate\">With Icon</span>"));
        }
    }
}
