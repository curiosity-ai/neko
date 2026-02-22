using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class CardTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestStackedCard()
        {
            var markdown = "::: card {image=\"img.jpg\" title=\"Title\" tags=\"tag1,tag2\"}\nDescription\n:::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("<div class=\"max-w-sm rounded overflow-hidden shadow-lg"));
            Assert.That(doc, Contains.Substring("<img class=\"w-full object-cover\" src=\"img.jpg\" alt=\"Title\">"));
            Assert.That(doc, Contains.Substring("<div class=\"font-bold text-xl mb-2 text-gray-900 dark:text-white\">Title</div>"));
            Assert.That(doc, Contains.Substring("Description"));
            Assert.That(doc, Contains.Substring("#tag1"));
            Assert.That(doc, Contains.Substring("#tag2"));
        }

        [Test]
        public void TestHorizontalCard()
        {
            var markdown = "::: card {variant=\"horizontal\" image=\"img.jpg\" title=\"Title\" see-more=\"#\"}\nDescription\n:::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("lg:flex"));
            Assert.That(doc, Contains.Substring("style=\"background-image: url('img.jpg')\""));
            Assert.That(doc, Contains.Substring("See more"));
        }

        [Test]
        public void TestCardWithExtraClasses()
        {
            var markdown = "::: card {.my-class title=\"Title\"}\nContent\n:::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("my-class"));
        }
    }
}
