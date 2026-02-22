using NUnit.Framework;
using Neko.Builder;
using System;

namespace Neko.Tests
{
    public class IconTests
    {
        [Test]
        public void TestInlineIconRendering()
        {
            var parser = new MarkdownParser();
            var markdown = @"Hello :icon-home: World";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            Assert.That(doc.Html, Contains.Substring("fi-rr-home"), "Icon should be rendered");
        }

        [Test]
        public void TestButtonIconRendering()
        {
            var parser = new MarkdownParser();
            var markdown = @"[!button text=""Click"" icon="":icon-home:""]";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            Assert.That(doc.Html, Contains.Substring("fi-rr-home"), "Button Icon should be rendered");
        }

        [Test]
        public void TestEmojiRendering()
        {
            var parser = new MarkdownParser();
            var markdown = @"Hello :smile: World";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            // Markdig renders :smile: as unicode or img depending on config.
            // By default UseEmojiAndSmiley renders unicode if possible.
            // Standard smile emoji is 😄 (\u1F604)
            Assert.That(doc.Html, Contains.Substring("😄").Or.Contains("smile"), "Emoji should be rendered");
        }
    }
}
