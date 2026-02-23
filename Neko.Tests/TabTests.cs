using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
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

        [Test]
        public void TestDuplicateTabs()
        {
            var markdown = "+++ Tab 1\nContent 1\n+++\n\n+++ Tab 1\nContent 2\n+++";
            var doc = _parser.Parse(markdown);

            // We expect TWO tab groups because there are two separate blocks.
            // Each block starts with +++ Title and ends with +++

            int count = System.Text.RegularExpressions.Regex.Matches(doc.Html, "class=\"my-4 border rounded-md dark:border-gray-700\"").Count;
            Assert.That(count, Is.EqualTo(2), $"Expected 2 tab groups, found {count}. HTML: {doc.Html}");
        }

        [Test]
        public void TestTabWithIcon()
        {
            var markdown = "+++ :icon-code-simple: Source\nCode\n+++";
            var doc = _parser.Parse(markdown);

            // Expect the icon class to be present
            Assert.That(doc.Html, Contains.Substring("fi-rr-code-simple"));
            Assert.That(doc.Html, Contains.Substring("Source"));
        }
    }
}
