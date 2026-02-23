using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ButtonFixTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestButtonWithPositionalText()
        {
            // Case: [!button Button](button.md)
            // The text "Button" is a positional argument.
            var markdown = "[!button Button](button.md)";
            var doc = _parser.Parse(markdown);

            // Expectation: The button should contain the text "Button"
            Assert.That(doc.Html, Contains.Substring("Button"));
            Assert.That(doc.Html, Contains.Substring("href=\"button.md\""));
        }

        [Test]
        public void TestButtonWithEmojiPadding()
        {
            // Case: [!button icon=":heart:" text="Love"]
            var markdown = "[!button icon=\":heart:\" text=\"Love\"]";
            var doc = _parser.Parse(markdown);

            // Expectation: The emoji should have some margin or padding.
            // Currently it just renders space. I plan to wrap it in a span with mr-2.
            Assert.That(doc.Html, Contains.Substring("<span class=\"mr-2\">"));
            Assert.That(doc.Html, Contains.Substring("Love"));
        }
    }
}
