using NUnit.Framework;
using Neko.Builder;
using System;

namespace Neko.Tests
{
    public class PanelTests
    {
        [Test]
        public void TestPanelRendering()
        {
            var parser = new MarkdownParser();
            var markdown = @"=== Test Panel
Content
===";
            var doc = parser.Parse(markdown);
            Console.WriteLine("Parsed HTML:");
            Console.WriteLine(doc.Html);

            Assert.That(doc.Html, Contains.Substring("<details"), "Panel should render as details element. Output: " + doc.Html);
            Assert.That(doc.Html, Contains.Substring("Test Panel"), "Panel title should be present");
        }
    }
}
