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
            var parser = new Neko.Builder.MarkdownParser(new Neko.Configuration.NekoConfig());
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

            var parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            var doc = parser.Parse(markdown, filePath, _testDir);
            var parsedDocs = new System.Collections.Generic.List<(string FilePath, string RelativePath, Neko.Builder.ParsedDocument Doc, string Markdown)>
            {
                (filePath, "components.md", doc, markdown)
            };

            var generator = new SidebarGenerator(_testDir, parsedDocs);

            // Act
            var links = generator.Generate();

            // Assert
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links.First().Text, Is.EqualTo("Title Components"));
        }

        [Test]
        public void SidebarGenerator_RootIndex_IsRenamedToHome_OnTrueSiteRoot()
        {
            // Arrange — index.md at the true site root (no route prefix)
            var filePath = Path.Combine(_testDir, "index.md");
            var markdown = "---\r\nlabel: \"Introduction\"\r\nicon: rocket\r\norder: 1\r\n---\r\n# Get Started\r\nSome text.";
            File.WriteAllText(filePath, markdown);

            var parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            var doc = parser.Parse(markdown, filePath, _testDir);
            var parsedDocs = new System.Collections.Generic.List<(string FilePath, string RelativePath, Neko.Builder.ParsedDocument Doc, string Markdown)>
            {
                (filePath, "index.md", doc, markdown)
            };

            var generator = new SidebarGenerator(_testDir, parsedDocs);

            // Act
            var links = generator.Generate();

            // Assert
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links.First().Text, Is.EqualTo("Home"));
        }

        [Test]
        public void SidebarGenerator_SubSiteRootIndex_IsOmittedFromSidebar()
        {
            // Arrange — a sub-site (mounted under a route prefix) with a root
            // index.md plus a real content page.
            var indexPath = Path.Combine(_testDir, "index.md");
            var indexMd = "---\r\nlabel: \"Introduction\"\r\nicon: rocket\r\norder: 1\r\n---\r\n# Build\r\nSome text.";
            File.WriteAllText(indexPath, indexMd);

            var pagePath = Path.Combine(_testDir, "setup.md");
            var pageMd = "---\r\nlabel: \"Setup\"\r\norder: 2\r\n---\r\n# Setup\r\nSome text.";
            File.WriteAllText(pagePath, pageMd);

            var parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            var parsedDocs = new System.Collections.Generic.List<(string FilePath, string RelativePath, Neko.Builder.ParsedDocument Doc, string Markdown)>
            {
                (indexPath, "index.md", parser.Parse(indexMd, indexPath, _testDir), indexMd),
                (pagePath, "setup.md", parser.Parse(pageMd, pagePath, _testDir), pageMd)
            };

            var generator = new SidebarGenerator(_testDir, parsedDocs, routePrefix: "/workspace-build");

            // Act
            var links = generator.Generate();

            // Assert — the sub-site root is reachable via the header pivot, so it
            // is not repeated as a sidebar entry; sibling pages still appear.
            Assert.That(links, Has.Count.EqualTo(1));
            Assert.That(links.First().Text, Is.EqualTo("Setup"));
        }
    }
}
