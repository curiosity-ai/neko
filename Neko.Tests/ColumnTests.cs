using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ColumnTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestColumnGroup()
        {
            var markdown = "||| Column 1\nContent 1\n||| Column 2\nContent 2\n|||";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Column 1"));
            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("Column 2"));
            Assert.That(doc.Html, Contains.Substring("Content 2"));

            // Check structure
            Assert.That(doc.Html, Contains.Substring("flex flex-col md:flex-row gap-4 my-4"));
            Assert.That(doc.Html, Contains.Substring("flex-1 min-w-0"));
        }

        [Test]
        public void TestColumnGroupEnding()
        {
            var markdown = "||| Column 1\nContent 1\n|||\nAfter columns";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("After columns"));
            Assert.That(doc.Html, Contains.Substring("flex flex-col md:flex-row gap-4 my-4"));
        }

        [Test]
        public void TestColumnTitleMarkdown()
        {
            var markdown = "||| Column **Bold**\nContent\n|||";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<strong>Bold</strong>"));
        }

        [Test]
        public void TestColumnTitleIcon()
        {
            var markdown = "||| :icon-code-simple: Source\nContent\n|||";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("fi-rr-code-simple"));
        }
    }
}
