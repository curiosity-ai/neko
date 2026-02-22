using NUnit.Framework;
using Neko.Builder;
using System;

namespace Neko.Tests
{
    public class ColorChipTests
    {
        [Test]
        public void TestColorChipPositional()
        {
            var parser = new MarkdownParser();
            var markdown = @"[!color-chip #ff0000]";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            Assert.That(doc.Html, Contains.Substring("background-color: #ff0000"), "Color should be rendered from positional arg");
        }

        [Test]
        public void TestColorChipPositionalWithText()
        {
            var parser = new MarkdownParser();
            var markdown = @"[!color-chip #00ff00 Green]";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            Assert.That(doc.Html, Contains.Substring("background-color: #00ff00"), "Color should be rendered");
            Assert.That(doc.Html, Contains.Substring("Green"), "Text should be rendered");
        }
    }
}
