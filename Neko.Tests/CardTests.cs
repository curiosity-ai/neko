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

        [Test]
        public void TestGridCard()
        {
            var markdown = "::: card {variant=\"grid\" image=\"img.jpg\" title=\"Title\" link=\"#\"}\nContent\n:::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("flex flex-col h-full rounded-lg"));
            Assert.That(doc, Contains.Substring("bg-gray-50 dark:bg-white")); // Image container
            Assert.That(doc, Contains.Substring("object-contain")); // Image fit
            Assert.That(doc, Contains.Substring("Title"));
            Assert.That(doc, Contains.Substring("Content"));
        }

        [Test]
        public void TestCardGridContainer()
        {
            var markdown = "::: card-grid\n::: card {variant=\"grid\"}\nC1\n:::\n::: card {variant=\"grid\"}\nC2\n:::\n:::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 my-8"));
            Assert.That(doc, Contains.Substring("C1"));
            Assert.That(doc, Contains.Substring("C2"));
        }
    }
}
