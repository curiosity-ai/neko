using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    public class HtmlGeneratorIncludesTests
    {
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
        }

        [Test]
        public void TestGenerateWithHeadIncludes()
        {
            var headIncludes = "<script>console.log('test');</script>";
            var generator = new HtmlGenerator(_config, isWatchMode: false, headIncludes: headIncludes);

            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = generator.Generate(doc);

            Assert.That(html, Contains.Substring(headIncludes));
            Assert.That(html, Contains.Substring("</head>"));
            // Ensure includes are before closing head tag
            var includesIndex = html.IndexOf(headIncludes);
            var closingHeadIndex = html.IndexOf("</head>");
            Assert.That(includesIndex, Is.LessThan(closingHeadIndex));
        }

        [Test]
        public void TestGenerateWithoutHeadIncludes()
        {
            var generator = new HtmlGenerator(_config);

            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Not.Contain("<script>console.log('test');</script>"));
        }
    }
}
