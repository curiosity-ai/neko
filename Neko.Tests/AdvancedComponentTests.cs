using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class AdvancedComponentTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestCodeBlockWithTitle()
        {
            var markdown = "```csharp title=\"MyFile.cs\"\npublic class Foo {}\n```";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("MyFile.cs"));
            // New implementation does not use the icon
            // Assert.That(doc.Html, Contains.Substring("fi-rr-file-code"));
            Assert.That(doc.Html, Contains.Substring("!rounded-none"));
        }

        [Test]
        public void TestCodeSnippet()
        {
            var markdown = ":::code source=\"Example.cs\" title=\"Custom Title\" :::";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Custom Title"));
            Assert.That(doc.Html, Contains.Substring("View Source"));
            Assert.That(doc.Html, Contains.Substring("href=\"Example.cs\""));
            // Placeholder content check
            Assert.That(doc.Html, Contains.Substring("// Content of Example.cs"));
        }

        [Test]
        public void TestPanel()
        {
            var markdown = "::: panel\n# Header\nContent inside panel\n:::";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("shadow-sm")); // Panel class
            Assert.That(doc.Html, Contains.Substring("Content inside panel"));
        }

        [Test]
        public void TestColumns()
        {
            var markdown = "::: columns\n::: column\nLeft\n:::\n::: column\nRight\n:::\n:::";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("flex flex-col md:flex-row")); // Columns container
            Assert.That(doc.Html, Contains.Substring("flex-1 min-w-0")); // Column item
            Assert.That(doc.Html, Contains.Substring("Left"));
            Assert.That(doc.Html, Contains.Substring("Right"));
        }

        [Test]
        public void TestImageAttributes()
        {
            var markdown = "![Alt Text](image.png){width=500}";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("width=\"500\""));
            Assert.That(doc.Html, Contains.Substring("src=\"image.png\""));
        }
    }
}
