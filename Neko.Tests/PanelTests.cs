using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class PanelTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestExpandedPanel()
        {
            var markdown = "=== My Panel\nThis is a Panel.\n===";
            var doc = _parser.Parse(markdown);

            // Check wrapper
            Assert.That(doc.Html, Contains.Substring("class=\"panel-group my-4\""));

            // Check details open
            Assert.That(doc.Html, Contains.Substring("<details class=\"bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-sm mb-2 overflow-hidden\" open"));

            // Check summary
            Assert.That(doc.Html, Contains.Substring("<summary class=\"px-4 py-2 bg-gray-50 dark:bg-gray-900 font-semibold cursor-pointer list-none flex items-center select-none\">"));
            Assert.That(doc.Html, Contains.Substring("My Panel"));

            // Check content
            Assert.That(doc.Html, Contains.Substring("This is a Panel."));
        }

        [Test]
        public void TestCollapsedPanel()
        {
            var markdown = "==- My Collapsed Panel\nHidden content.\n===";
            var doc = _parser.Parse(markdown);

            // Check details (no open attribute)
            Assert.That(doc.Html, Does.Not.Contain("<details class=\"bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-sm mb-2 overflow-hidden\" open"));
            Assert.That(doc.Html, Contains.Substring("<details class=\"bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-sm mb-2 overflow-hidden\""));

            Assert.That(doc.Html, Contains.Substring("My Collapsed Panel"));
            Assert.That(doc.Html, Contains.Substring("Hidden content."));
        }

        [Test]
        public void TestStackedPanels()
        {
            var markdown = "=== Panel 1\nContent 1\n=== Panel 2\nContent 2\n===";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Panel 1"));
            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("Panel 2"));
            Assert.That(doc.Html, Contains.Substring("Content 2"));

            // Should be in one group
            int groupCount = System.Text.RegularExpressions.Regex.Matches(doc.Html, "class=\"panel-group my-4\"").Count;
            Assert.That(groupCount, Is.EqualTo(1));

            // Should have 2 details
            int detailsCount = System.Text.RegularExpressions.Regex.Matches(doc.Html, "<details").Count;
            Assert.That(detailsCount, Is.EqualTo(2));
        }

        [Test]
        public void TestStackedMixedPanels()
        {
            var markdown = "=== Expanded\nVisible\n==- Collapsed\nHidden\n===";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Expanded"));
            Assert.That(doc.Html, Contains.Substring("Collapsed"));

            // First open, second closed
            Assert.That(doc.Html, Contains.Substring("open"));
        }

        [Test]
        public void TestClosingWithCollapsedMarker()
        {
             var markdown = "=== Panel\nContent\n==-";
             var doc = _parser.Parse(markdown);

             Assert.That(doc.Html, Contains.Substring("Panel"));
             Assert.That(doc.Html, Contains.Substring("Content"));
        }
    }
}
