using NUnit.Framework;
using Neko.Builder;
using System.IO;
using System.Linq;

namespace Neko.Tests
{
    [TestFixture]
    public class SidebarLabelTests
    {
        private string _testDir;

        [SetUp]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "SidebarLabelTests");
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public void SidebarGenerator_UsesLabelFromFrontMatter_WhenAvailable()
        {
            // Arrange
            var filePath = Path.Combine(_testDir, "components.md");
            var markdown = "---\r\nlabel: \"UI Components\"\r\ntitle: \"Title\"\r\nicon: stack\r\norder: 50\r\n---\r\n## Components\r\nSome text.";
            File.WriteAllText(filePath, markdown);

            var parsedDocs = new System.Collections.Generic.List<(string FilePath, string RelativePath, Neko.Builder.ParsedDocument Doc, string Markdown)>();
            var parser = new Neko.Builder.MarkdownParser();
            var doc = parser.Parse(System.IO.File.ReadAllText(filePath), filePath);
            parsedDocs.Add((filePath, "components.md", doc, System.IO.File.ReadAllText(filePath)));
            var generator = new SidebarGenerator(_testDir, parsedDocs);

            // Act
            var links = generator.Generate();

            // Assert
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links.First().Text, Is.EqualTo("UI Components"));
        }

        [Test]
        public void SidebarGenerator_UsesTitleFromFrontMatter_WhenLabelNotAvailable()
        {
            // Arrange
            var filePath = Path.Combine(_testDir, "components.md");
            var markdown = "---\r\ntitle: \"Title Components\"\r\nicon: stack\r\norder: 50\r\n---\r\n## Components\r\nSome text.";
            File.WriteAllText(filePath, markdown);

            var parser = new MarkdownParser();
            var doc = parser.Parse(markdown, filePath, _testDir);
            var parsedDocs = new System.Collections.Generic.List<(string, string, Neko.Builder.ParsedDocument, string)>
            {
                (filePath, "components.md", doc, markdown)
            };

            var parsedDocs = new System.Collections.Generic.List<(string FilePath, string RelativePath, Neko.Builder.ParsedDocument Doc, string Markdown)>();
            var parser = new Neko.Builder.MarkdownParser();
            var doc = parser.Parse(System.IO.File.ReadAllText(filePath), filePath);
            parsedDocs.Add((filePath, "components.md", doc, System.IO.File.ReadAllText(filePath)));
            var generator = new SidebarGenerator(_testDir, parsedDocs);

            // Act
            var links = generator.Generate();

            // Assert
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links.First().Text, Is.EqualTo("Title Components"));
        }
    }
}
