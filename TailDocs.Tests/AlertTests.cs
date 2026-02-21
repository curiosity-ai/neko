using NUnit.Framework;
using TailDocs.CLI.Builder;

namespace TailDocs.Tests
{
    public class AlertTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestAlertDefault()
        {
            var markdown = "!!!\nThis is an alert.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("This is an alert."));
            Assert.That(doc.Html, Contains.Substring("border-blue-500")); // Primary/Info default
        }

        [Test]
        public void TestAlertVariant()
        {
            var markdown = "!!!danger\nThis is dangerous.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("border-red-500"));
        }

        [Test]
        public void TestAlertTitle()
        {
            var markdown = "!!! warning My Title\nWarning content.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("My Title"));
            Assert.That(doc.Html, Contains.Substring("border-yellow-500"));
        }
    }
}
