using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class BadgeTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
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

            Assert.That(doc.Html, Contains.Substring("bg-primary-100"));
        }

        [Test]
        public void TestBadgeCorners()
        {
            var markdown = "[!badge corners=\"pill\" text=\"Pill\"]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("rounded-full"));
        }

        [Test]
        public void TestBadgeWithIconInside()
        {
            // Note: The icon syntax is :icon-home: as per memory.
            // Also need to check if the icon is rendered as <i> tag with correct class.
            var markdown = "[!badge :icon-home: Home]";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("fi-rr-home"));
        }

        [Test]
        public void TestBadgeWithEmojiInside()
        {
            // Note: UseEmojiAndSmiley renders unicode by default unless image url configured.
            // Assuming default Markdig behavior.
            var markdown = "[!badge :smile: Smile]";
            var doc = _parser.Parse(markdown);

            // Check if smiley is rendered (either unicode or image, checking both or unicode char)
            // Markdig default emoji for :smile: is 😄 (\u1f604)
            // But if it fails to parse, it remains ":smile:"
            // So assert it DOES NOT contain ":smile:"
            Assert.That(doc.Html, Does.Not.Contain(":smile:"));
        }

        [Test]
        public void TestMissingEmojiInIconProperty()
        {
            var markdown = "[!badge icon=\":heart:\" text=\"Like\"]";
            var doc = _parser.Parse(markdown);

            // Expected: Should not render 'heart' text literally.
            Assert.That(doc.Html, Does.Not.Contain("heart Like"), "Should not render 'heart' text literally");

            // Should contain span with mr-1 for the icon.
            Assert.That(doc.Html, Contains.Substring("class=\"mr-1\""));
        }

        [Test]
        public void TestIconAlignRight()
        {
            var markdown = "[!badge icon=\"paper-plane\" iconAlign=\"right\" text=\"Send\"]";
            var doc = _parser.Parse(markdown);

            // Expected: Text first, then Icon.
            // Icon should appear after "Send".
            // Using regex to check order.

            // Check for "Send" appearing before "fi-rr-paper-plane"
            Assert.That(doc.Html, Does.Match("Send.*fi-rr-paper-plane"), "Icon should be after text");
            Assert.That(doc.Html, Contains.Substring("ml-1"), "Icon should have left margin (ml-1)");
        }
        [Test]
        public void Badge_Consecutive_AreNotNested()
        {
            var markdown = "[!badge variant=\"info\" icon=\"user\" text=\"User\" margin=\"0 8 0 0\"]\n[!badge variant=\"primary\" icon=\"paper-plane\" iconAlign=\"right\" text=\"Send\"]";
            var doc = _parser.Parse(markdown);

            // Assert that there are 2 badges present, not nested. We can check the count of <span class="inline-flex...
            // that is an immediate child of <p>. Since _parser returns HTML, let's just count occurrences.
            // Two badges should be produced.

            var matchCount = System.Text.RegularExpressions.Regex.Matches(doc.Html, "<span class=\"inline-flex").Count;
            // The outer span contains inline-flex, but maybe not the inner span.
            // Let's just check the outer ones. We expect 2 badges.
            Assert.That(matchCount, Is.GreaterThanOrEqualTo(2), "Expected at least 2 inline-flex spans");

            // The actual bug caused the second badge to be inside the first badge's span.
            // When badges are correctly not nested, they should appear as separate siblings.
            // In HTML, we should see `</span></span>\n<span` (with newlines/spaces) instead of `</span></span></span>`

            var nestedCount = System.Text.RegularExpressions.Regex.Matches(doc.Html, "class=\"inline-flex.*?class=\"inline-flex", System.Text.RegularExpressions.RegexOptions.Singleline).Count;
            // If one is inside the other, we might see it without a closing tag in between.

            // We can just assert that it produces two distinct badges by looking for 2 occurrences of `class="inline-flex items-center font-medium`
            var outerMatchCount = System.Text.RegularExpressions.Regex.Matches(doc.Html, "class=\"inline-flex items-center font-medium").Count;
            Assert.That(outerMatchCount, Is.EqualTo(2), "Expected exactly 2 outer badge spans");

            // Check they aren't nested - if they were nested, the second one would be parsed as text or inside the first.
            // With the fix, they are adjacent.
            Assert.That(doc.Html, Does.Not.Contain("</span></span></span>"));
        }
    }
}
