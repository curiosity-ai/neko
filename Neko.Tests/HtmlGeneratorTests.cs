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

            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            Assert.That(html, Contains.Substring("<title>Test Docs - Page Title</title>"));
            // Updated assertion for branding in header
            Assert.That(html, Contains.Substring("<a href=\"/index\" class=\"font-bold text-xl hover:text-blue-600 transition-colors\">Test Docs</a>"));
            // Updated assertion for link with new classes
            Assert.That(html, Contains.Substring("<a href=\"/\" class=\"block py-1 px-2 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2 text-[13px] whitespace-nowrap text-gray-700 dark:text-gray-300\"><i class=\"fi fi-rr-circle opacity-0\"></i> Home</a>"));
            Assert.That(html, Contains.Substring("<p>Content</p>"));
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

            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            // Check item with icon
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-home\"></i> With Icon"));

            // Check item without icon (should have invisible circle)
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-circle opacity-0\"></i> Without Icon"));
        }
    }
}
