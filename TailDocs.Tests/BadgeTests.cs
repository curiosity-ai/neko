using NUnit.Framework;
using TailDocs.CLI.Builder;

namespace TailDocs.Tests
{
    public class BadgeTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestBadgeDefault()
        {
            var markdown = "[!badge text=\"Default\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Default"));
            Assert.That(doc.Html, Contains.Substring("bg-gray-100")); // Base variant
        }

        [Test]
        public void TestBadgeVariant()
        {
            var markdown = "[!badge variant=\"primary\" text=\"Primary\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("bg-blue-100"));
        }

        [Test]
        public void TestBadgeCorners()
        {
            var markdown = "[!badge corners=\"pill\" text=\"Pill\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("rounded-full"));
        }
    }
}
