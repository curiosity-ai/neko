using NUnit.Framework;
using TailDocs.CLI.Builder;

namespace TailDocs.Tests
{
    public class TabTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestTabGroup()
        {
            var markdown = "+++ Tab 1\nContent 1\n+++ Tab 2\nContent 2\n+++";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Tab 1"));
            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("Tab 2"));
            Assert.That(doc.Html, Contains.Substring("Content 2"));

            // Check structure
            Assert.That(doc.Html, Contains.Substring("tab-content"));
        }

        [Test]
        public void TestTabGroupEnding()
        {
            var markdown = "+++ Tab 1\nContent 1\n+++\nAfter tabs";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("After tabs"));
        }
    }
}
