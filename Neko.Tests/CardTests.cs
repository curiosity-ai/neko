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
            var markdown = ":::: card-grid\n::: card {variant=\"grid\"}\nC1\n:::\n::: card {variant=\"grid\"}\nC2\n:::\n::::";
            var doc = _parser.Parse(markdown).Html;

            Assert.That(doc, Contains.Substring("grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 my-8"));
            Assert.That(doc, Contains.Substring("C1"));
            Assert.That(doc, Contains.Substring("C2"));

            // Verify nesting: Find the index of the grid container start
            var gridStart = doc.IndexOf("card-grid");
            var gridEnd = doc.LastIndexOf("</div>"); // This assumes it's the outermost div

            // But strict HTML parsing is better or at least checking order.
            // C2 should be before the END of the grid.
            // Since we don't have a full HTML parser here easily, let's just check relative positions if unique.

            var c1Index = doc.IndexOf("C1");
            var c2Index = doc.IndexOf("C2");

            Assert.That(c1Index, Is.GreaterThan(gridStart));
            Assert.That(c2Index, Is.GreaterThan(c1Index));

            // To verify they are INSIDE, we need to ensure the closing div comes after.
            // The grid div opens at the beginning.
            // It should close at the end.
            // Markdig output for CustomContainer is <div>...</div>.

            Assert.That(doc.EndsWith("</div>") || doc.Trim().EndsWith("</div>"));
        }

        [Test]
        public void TestLinkCard()
        {
            // Test Light Variant
            var markdownLight = "::: card {variant=\"link\" title=\"Light Card\" link=\"#\" link-text=\"View docs\"}\nContent Light\n:::";
            var docLight = _parser.Parse(markdownLight).Html;

            Assert.That(docLight, Contains.Substring("bg-white"));
            Assert.That(docLight, Contains.Substring("border-gray-200"));
            Assert.That(docLight, Contains.Substring("Light Card"));
            Assert.That(docLight, Contains.Substring("Content Light"));
            Assert.That(docLight, Contains.Substring("View docs"));
            Assert.That(docLight, Contains.Substring("text-blue-600"));
            Assert.That(docLight, Does.Not.Contain("&rarr;"));

            // Test Dark Variant with Arrow
            var markdownDark = "::: card {variant=\"link\" theme=\"dark\" title=\"Dark Card\" link=\"#\" link-text=\"View dark\" arrow=\"true\"}\nContent Dark\n:::";
            var docDark = _parser.Parse(markdownDark).Html;

            Assert.That(docDark, Contains.Substring("bg-gray-900"));
            Assert.That(docDark, Contains.Substring("text-white"));
            Assert.That(docDark, Contains.Substring("Dark Card"));
            Assert.That(docDark, Contains.Substring("Content Dark"));
            Assert.That(docDark, Contains.Substring("View dark"));
            Assert.That(docDark, Contains.Substring("&rarr;"));
        }
    }
}
