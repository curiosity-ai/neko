using NUnit.Framework;
using Neko.Builder;
using System.Linq;

namespace Neko.Tests
{
    public class MarkdownFeatureTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestCodeBlockWithTitleAndHighlight()
        {
            var markdown = @"```csharp title=""Program.cs"" #1-3
public class Program {}
```";
            var doc = _parser.Parse(markdown);

            // Assert
            Assert.That(doc.Html, Contains.Substring("Program.cs"), "Title should be rendered");
            Assert.That(doc.Html, Contains.Substring("data-highlight=\"1-3\""), "Highlight data attribute should be present");
            Assert.That(doc.Html, Contains.Substring("copy-btn"), "Copy button should be present");
        }

        [Test]
        public void TestCodeBlockWithHighlightOnly()
        {
            var markdown = @"```csharp #5,7-9
// Code
```";
            var doc = _parser.Parse(markdown);

            // Assert
            Assert.That(doc.Html, Contains.Substring("data-highlight=\"5,7-9\""), "Highlight data attribute should be present");
            Assert.That(doc.Html, Contains.Substring("copy-btn"), "Copy button should be present");
        }

        [Test]
        public void TestTocGeneration()
        {
            var markdown = @"# Introduction
## Setup
### Installation
## Usage
";
            var doc = _parser.Parse(markdown);

            // Assert
            Assert.That(doc.Toc, Is.Not.Null, "TOC should not be null");
            Assert.That(doc.Toc.Count, Is.EqualTo(4), "TOC count incorrect");

            Assert.That(doc.Toc[0].Title, Is.EqualTo("Introduction"));
            Assert.That(doc.Toc[0].Level, Is.EqualTo(1));
            Assert.That(doc.Toc[0].Id, Is.EqualTo("introduction"));

            Assert.That(doc.Toc[1].Title, Is.EqualTo("Setup"));
            Assert.That(doc.Toc[1].Level, Is.EqualTo(2));
            Assert.That(doc.Toc[1].Id, Is.EqualTo("setup"));

            Assert.That(doc.Toc[2].Title, Is.EqualTo("Installation"));
            Assert.That(doc.Toc[2].Level, Is.EqualTo(3));
            Assert.That(doc.Toc[2].Id, Is.EqualTo("installation"));

            Assert.That(doc.Toc[3].Title, Is.EqualTo("Usage"));
            Assert.That(doc.Toc[3].Level, Is.EqualTo(2));
            Assert.That(doc.Toc[3].Id, Is.EqualTo("usage"));
        }

        [Test]
        public void TestTocWithFormatting()
        {
            var markdown = @"# **Bold** Title";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Toc.Count, Is.EqualTo(1));
            // Assuming we render HTML for the title in TOC
            Assert.That(doc.Toc[0].Title, Does.Contain("<strong>Bold</strong>").Or.Contain("<b>Bold</b>"), "TOC title should preserve HTML formatting");
            // AutoIdentifiers usually sluggify based on text content, so "bold-title"
            Assert.That(doc.Toc[0].Id, Is.EqualTo("bold-title"));
        }
    }
}
