using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class RegressionTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestButtonWithEmoji()
        {
            var markdown = "[!button text=\"Click\" icon=\":heart:\"]";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Button Emoji: {doc.Html}");
            // Should render inline
            // If it renders "heart " it failed.
            Assert.That(doc.Html, Does.Not.Contain("heart </a>"), "Should not render as stripped text 'heart '");
            Assert.That(doc.Html, Does.Not.Contain(":heart:"), "Should not render as literal ':heart:'");
        }

        [Test]
        public void TestButtonWithInlineIconInText()
        {
            var markdown = "[!button text=\"Click :icon-home:\"]";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Button Inline Icon: {doc.Html}");
            Assert.That(doc.Html, Contains.Substring("fi fi-rr-home"), "Button text should parse inline icons");
        }

        [Test]
        public void TestInlineIcon()
        {
            var markdown = "Check out this icon: :icon-home:";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Inline Icon: {doc.Html}");
            Assert.That(doc.Html, Contains.Substring("<i class=\"fi fi-rr-home align-middle\"></i>"));
        }

        [Test]
        public void TestPanelWithIconTitle()
        {
            var markdown = "=== :icon-home: Title\nContent\n===";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Panel Icon: {doc.Html}");
            Assert.That(doc.Html, Contains.Substring("<i class=\"fi fi-rr-home align-middle\"></i>")
                | Contains.Substring("fi-rr-home")); // Allow some variation but must be processed
        }

        [Test]
        public void TestColorChipPositional()
        {
            var markdown = "[!color-chip #ff0000]";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Color Chip: {doc.Html}");
            Assert.That(doc.Html, Contains.Substring("background-color: #ff0000"));
        }

        [Test]
        public void TestEmojiTable()
        {
            var markdown = "[!emoji-table]";
            var doc = _parser.Parse(markdown);

            TestContext.WriteLine($"Emoji Table: {doc.Html}");
            Assert.That(doc.Html, Contains.Substring("Emoji"));
            Assert.That(doc.Html, Contains.Substring("Shortcode"));
            // Check for some emoji
            Assert.That(doc.Html, Contains.Substring(":smile:"));
            Assert.That(doc.Html, Contains.Substring("😄") | Contains.Substring("&#x1F604;"));
        }
    }
}
