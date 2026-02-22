using NUnit.Framework;
using TailDocs.CLI.Builder;

namespace TailDocs.Tests
{
    public class ColumnTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
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
        public void TestColumnTitleEscaping()
        {
            var markdown = "||| Column <script>alert(1)</script>\nContent\n|||";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Does.Not.Contain("<script>alert(1)</script>"));
            Assert.That(doc.Html, Contains.Substring("&lt;script&gt;alert(1)&lt;/script&gt;"));
        }
    }
}
