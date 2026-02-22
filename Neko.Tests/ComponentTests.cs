using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ComponentTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestButton()
        {
            var markdown = "[!button text=\"Click Me\" link=\"/url\" variant=\"primary\" icon=\"home\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<a href=\"/url\""));
            Assert.That(doc.Html, Contains.Substring("bg-blue-600")); // primary variant
            Assert.That(doc.Html, Contains.Substring("Click Me"));
            Assert.That(doc.Html, Contains.Substring("fi fi-rr-home"));
        }

        [Test]
        public void TestColorChip()
        {
            var markdown = "[!color-chip color=\"#ff0000\" text=\"Red\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("background-color: #ff0000"));
            Assert.That(doc.Html, Contains.Substring("Red"));
            Assert.That(doc.Html, Contains.Substring("bg-gray-100"));
        }

        [Test]
        public void TestFile()
        {
            var markdown = "[!file text=\"Download Me\" link=\"file.zip\" size=\"1.2MB\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("href=\"file.zip\""));
            Assert.That(doc.Html, Contains.Substring("Download Me"));
            Assert.That(doc.Html, Contains.Substring("1.2MB"));
            Assert.That(doc.Html, Contains.Substring("fi fi-rr-document"));
        }

        [Test]
        public void TestIcon()
        {
            var markdown = "[!icon name=\"home\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("fi fi-rr-home"));
        }

        [Test]
        public void TestRef()
        {
            var markdown = "[!ref text=\"Reference Link\" link=\"/docs\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("href=\"/docs\""));
            Assert.That(doc.Html, Contains.Substring("Reference Link"));
            Assert.That(doc.Html, Contains.Substring("fi fi-rr-link-alt"));
        }

        [Test]
        public void TestYouTube()
        {
            var markdown = "[!youtube id=\"xyz123\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("src=\"https://www.youtube.com/embed/xyz123\""));
            Assert.That(doc.Html, Contains.Substring("iframe"));
            // Check for aspect-video or whatever we used (wait, I haven't changed the renderer yet, I kept aspect-w-16)
        }

        [Test]
        public void TestMath()
        {
            var markdown = "$$ E = mc^2 $$";
            var doc = _parser.Parse(markdown);

            // Markdig Math usually outputs correct MathML or KaTeX compatible output.
            // If UseMathematics is on, it often outputs <span class="math">...</span> or similar.
            // Let's just check if it parsed it differently than plain text.
            Assert.That(doc.Html, Contains.Substring("math")); // Often contains "math" class or tag
            Assert.That(doc.Html, Does.Not.Contain("$$")); // Should not contain raw delimiters
        }

        [Test]
        public void TestMermaid()
        {
            var markdown = "```mermaid\ngraph TD;\n    A-->B;\n```";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("mermaid"));
            Assert.That(doc.Html, Contains.Substring("graph TD"));
        }

        [Test]
        public void TestCustomContainer()
        {
            var markdown = "::: div\nContent\n:::";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<div class=\"div\">"));
            Assert.That(doc.Html, Contains.Substring("Content"));
        }
    }
}
