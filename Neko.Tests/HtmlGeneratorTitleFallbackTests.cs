using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    public class HtmlGeneratorTitleFallbackTests
    {
        private HtmlGenerator _generator;
        private NekoConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Test Docs" }
            };
            _generator = new HtmlGenerator(_config);
        }

        [Test]
        public void TestTitleFallback()
        {
            var doc = new ParsedDocument
            {
                Html = "<h1>Header Title</h1><p>Content</p>",
                FrontMatter = new FrontMatter { Title = null },
                Toc = new List<TocItem>
                {
                    new TocItem { Level = 1, Title = "Header Title", Id = "header-title" }
                }
            };

            var html = _generator.Generate(doc);

            Assert.That(html, Contains.Substring("<title>Test Docs - Header Title</title>"));
        }

        [Test]
        public void TestTitleFallback_StripHtml()
        {
            var doc = new ParsedDocument
            {
                Html = "<h1><i>Header</i> Title</h1><p>Content</p>",
                FrontMatter = new FrontMatter { Title = null },
                Toc = new List<TocItem>
                {
                    new TocItem { Level = 1, Title = "<i>Header</i> Title", Id = "header-title" }
                }
            };

            var html = _generator.Generate(doc);

            // Expect stripped HTML
            Assert.That(html, Contains.Substring("<title>Test Docs - Header Title</title>"));
        }

        [Test]
        public void TestTitleFallback_NoHeader()
        {
             var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = null },
                Toc = new List<TocItem>()
            };

            var html = _generator.Generate(doc);

            // Should just be Branding Title
            Assert.That(html, Contains.Substring("<title>Test Docs</title>"));
        }
    }
}
