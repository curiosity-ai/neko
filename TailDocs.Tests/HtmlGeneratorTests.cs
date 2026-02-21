using NUnit.Framework;
using TailDocs.CLI.Builder;
using TailDocs.CLI.Configuration;

namespace TailDocs.Tests
{
    public class HtmlGeneratorTests
    {
        private HtmlGenerator _generator;
        private TailDocsConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new TailDocsConfig
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

            var html = _generator.Generate(doc);

            Assert.That(html, Contains.Substring("<title>Page Title - Test Docs</title>"));
            Assert.That(html, Contains.Substring("<div class=\"p-4 font-bold text-xl\">Test Docs</div>"));
            // Correct assertion for link without icon
            Assert.That(html, Contains.Substring("<a href=\"/\" class=\"block py-2 px-4 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2\"> Home</a>"));
            Assert.That(html, Contains.Substring("<p>Content</p>"));
        }
    }
}
