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
    }
}
