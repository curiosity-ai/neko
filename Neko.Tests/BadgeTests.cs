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
    }
}
