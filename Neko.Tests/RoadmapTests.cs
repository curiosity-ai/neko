using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class RoadmapTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestRoadmapContainer()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""Planned"" count=""1"" accent=""teal""}
::: roadmap-item {title=""Item A"" tag=""Feature"" tag-color=""emerald"" votes=""3""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            // Outer grid layout
            Assert.That(html, Contains.Substring("neko-roadmap"));
            Assert.That(html, Contains.Substring("grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4"));

            // Lane: header + accent bar
            Assert.That(html, Contains.Substring("neko-roadmap-lane"));
            Assert.That(html, Contains.Substring("bg-teal-500"));
            Assert.That(html, Contains.Substring("Planned"));

            // Item: title, tag, votes
            Assert.That(html, Contains.Substring("neko-roadmap-item"));
            Assert.That(html, Contains.Substring("Item A"));
            Assert.That(html, Contains.Substring("Feature"));
            // emerald tag uses the Neko badge-style tinted pill
            Assert.That(html, Contains.Substring("bg-emerald-100"));
            Assert.That(html, Contains.Substring("text-emerald-800"));
            Assert.That(html, Contains.Substring(">3<"));
        }

        [Test]
        public void TestRoadmapItemIsNestedInsideLane()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""Planned"" count=""1"" accent=""teal""}
::: roadmap-item {title=""Inside"" tag=""Feature""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            // The item div must appear AFTER the lane opening tag and BEFORE the lane's closing tag.
            var laneOpen = html.IndexOf("neko-roadmap-lane");
            var itemOpen = html.IndexOf("neko-roadmap-item");
            Assert.That(laneOpen, Is.GreaterThan(-1));
            Assert.That(itemOpen, Is.GreaterThan(laneOpen), "Item must be rendered AFTER lane open (nested inside)");

            // There must be exactly one lane and one item.
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "neko-roadmap-lane(?![-A-Za-z])").Count, Is.EqualTo(1));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "neko-roadmap-item(?![-A-Za-z])").Count, Is.EqualTo(1));
        }

        [Test]
        public void TestRoadmapLaneAccents()
        {
            string Build(string accent) => $@"::::: roadmap
:::: lane {{title=""L"" count=""1"" accent=""{accent}""}}
::: roadmap-item {{title=""I"" tag=""Feature""}}
:::
::::
:::::";

            // Each accent value should produce a solid accent bar of the matching color
            // plus a tinted count-badge using bg-{color}-500/15 + ring-1 ring-{color}-500/30.
            Assert.That(_parser.Parse(Build("gray")).Html,    Contains.Substring("bg-gray-400"));
            Assert.That(_parser.Parse(Build("gray")).Html,    Contains.Substring("ring-gray-500/30"));
            Assert.That(_parser.Parse(Build("teal")).Html,    Contains.Substring("bg-teal-500"));
            Assert.That(_parser.Parse(Build("teal")).Html,    Contains.Substring("ring-teal-500/30"));
            Assert.That(_parser.Parse(Build("amber")).Html,   Contains.Substring("bg-amber-500"));
            Assert.That(_parser.Parse(Build("amber")).Html,   Contains.Substring("ring-amber-500/30"));
            Assert.That(_parser.Parse(Build("sky")).Html,     Contains.Substring("bg-sky-500"));
            Assert.That(_parser.Parse(Build("sky")).Html,     Contains.Substring("ring-sky-500/30"));
            Assert.That(_parser.Parse(Build("blue")).Html,    Contains.Substring("bg-blue-500"));
            Assert.That(_parser.Parse(Build("violet")).Html,  Contains.Substring("bg-violet-500"));
            Assert.That(_parser.Parse(Build("emerald")).Html, Contains.Substring("bg-emerald-500"));
            Assert.That(_parser.Parse(Build("rose")).Html,    Contains.Substring("bg-rose-500"));
        }

        [Test]
        public void TestRoadmapLaneDefaultAccent()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""Default""}
::: roadmap-item {title=""Item"" tag=""Feature""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;
            // Default lane accent is gray
            Assert.That(html, Contains.Substring("bg-gray-400"));
        }

        [Test]
        public void TestRoadmapItemWithDateAndLink()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""In Progress"" count=""1"" accent=""amber""}
::: roadmap-item {title=""Zapier Integration"" tag=""Integrations"" tag-color=""amber"" date=""Sep, 21"" votes=""141"" link=""https://example.com""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            Assert.That(html, Contains.Substring("Zapier Integration"));
            Assert.That(html, Contains.Substring("Integrations"));
            Assert.That(html, Contains.Substring("Sep, 21"));
            Assert.That(html, Contains.Substring(">141<"));
            Assert.That(html, Contains.Substring("href=\"https://example.com\""));
            // Tag color for amber uses the Neko badge style
            Assert.That(html, Contains.Substring("bg-amber-100"));
            Assert.That(html, Contains.Substring("text-amber-800"));
        }

        [Test]
        public void TestRoadmapItemWithBodyDescription()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""Planned"" count=""1"" accent=""teal""}
::: roadmap-item {title=""Dark mode"" tag=""Feature"" tag-color=""emerald"" votes=""42""}
A polished dark mode that follows the system preference.
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            Assert.That(html, Contains.Substring("Dark mode"));
            Assert.That(html, Contains.Substring("neko-roadmap-item-body"));
            Assert.That(html, Contains.Substring("polished dark mode"));
        }

        [Test]
        public void TestRoadmapHtmlEncodesAttributes()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""<Lane>"" count=""1"" accent=""sky""}
::: roadmap-item {title=""<Item & Co>"" tag=""Feature""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            // Title text in titles must be HTML-escaped
            Assert.That(html, Contains.Substring("&lt;Lane&gt;"));
            Assert.That(html, Contains.Substring("&lt;Item &amp; Co&gt;"));
        }

        [Test]
        public void TestRoadmapMultipleLanes()
        {
            var markdown = @"::::: roadmap
:::: lane {title=""Under Consideration"" count=""2"" accent=""gray""}
::: roadmap-item {title=""A"" tag=""Feature""}
:::
::: roadmap-item {title=""B"" tag=""Feature""}
:::
::::
:::: lane {title=""Public Beta"" count=""1"" accent=""sky""}
::: roadmap-item {title=""C"" tag=""Feature""}
:::
::::
:::::";
            var html = _parser.Parse(markdown).Html;

            Assert.That(html, Contains.Substring("Under Consideration"));
            Assert.That(html, Contains.Substring("Public Beta"));

            // All three items are present and ordered.
            var aIdx = html.IndexOf("A<");
            var bIdx = html.IndexOf("B<");
            var cIdx = html.IndexOf("C<");
            Assert.That(aIdx, Is.GreaterThan(-1));
            Assert.That(bIdx, Is.GreaterThan(aIdx));
            Assert.That(cIdx, Is.GreaterThan(bIdx));

            // Two lanes total.
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "neko-roadmap-lane(?![-A-Za-z])").Count, Is.EqualTo(2));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "neko-roadmap-item(?![-A-Za-z])").Count, Is.EqualTo(3));
        }
    }
}
