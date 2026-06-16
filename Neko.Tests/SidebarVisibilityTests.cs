using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neko.Tests
{
    [TestFixture]
    public class SidebarVisibilityTests
    {
        private string _testDir;
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "SidebarVisibilityTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
        }

        private (string FilePath, string RelativePath, ParsedDocument Doc, string Markdown) Page(string relativePath, string frontMatter, string body = "# Heading\nText.")
        {
            var filePath = Path.Combine(_testDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            var markdown = string.IsNullOrEmpty(frontMatter) ? body : $"---\n{frontMatter}\n---\n{body}";
            File.WriteAllText(filePath, markdown);
            var doc = _parser.Parse(markdown, filePath);
            return (filePath, relativePath, doc, markdown);
        }

        private List<LinkConfig> Generate(params (string, string, ParsedDocument, string)[] pages)
            => new SidebarGenerator(_testDir, pages.ToList()).Generate();

        private static IEnumerable<string> Texts(IEnumerable<LinkConfig> links)
        {
            foreach (var l in links)
            {
                yield return l.Text;
                if (l.Items != null)
                    foreach (var t in Texts(l.Items)) yield return t;
            }
        }

        [Test]
        public void Page_VisibilityHidden_IsExcludedFromSidebar()
        {
            var visible = Page("visible.md", "label: \"Visible\"\norder: 1");
            var hidden = Page("secret.md", "label: \"Secret\"\nvisibility: hidden\norder: 2");

            var links = Generate(visible, hidden);

            Assert.That(Texts(links), Does.Contain("Visible"));
            Assert.That(Texts(links), Does.Not.Contain("Secret"));
        }

        [Test]
        public void Page_VisibilityPrivate_IsExcludedFromSidebar()
        {
            var visible = Page("visible.md", "label: \"Visible\"\norder: 1");
            var priv = Page("locked.md", "label: \"Locked\"\nvisibility: private\norder: 2");

            var links = Generate(visible, priv);

            Assert.That(Texts(links), Does.Contain("Visible"));
            Assert.That(Texts(links), Does.Not.Contain("Locked"));
        }

        [Test]
        public void Page_VisibilityProtected_StaysInSidebar()
        {
            // protected renders via the password flow and must remain visible.
            var prot = Page("members.md", "label: \"Members\"\nvisibility: protected\norder: 1");

            var links = Generate(prot);

            Assert.That(Texts(links), Does.Contain("Members"));
        }

        [Test]
        public void Folder_VisibilityHiddenInIndexYml_IsExcludedFromSidebar()
        {
            var visible = Page("visible.md", "label: \"Visible\"\norder: 1");
            var hiddenChild = Page("internal/page.md", "label: \"Internal Page\"\norder: 1");
            // Mark the folder hidden via its index.yml.
            File.WriteAllText(Path.Combine(_testDir, "internal", "index.yml"), "label: \"Internal\"\nvisibility: hidden\norder: 2\n");

            var links = Generate(visible, hiddenChild);

            Assert.That(Texts(links), Does.Contain("Visible"));
            Assert.That(Texts(links), Does.Not.Contain("Internal"));
            Assert.That(Texts(links), Does.Not.Contain("Internal Page"));
        }

        [Test]
        public void Folder_VisibilityHiddenInIndexMd_IsExcludedFromSidebar()
        {
            var visible = Page("visible.md", "label: \"Visible\"\norder: 1");
            // No index.yml — the folder's index.md frontmatter marks it hidden.
            var folderIndex = Page("drafts/index.md", "label: \"Drafts\"\nvisibility: hidden");
            var child = Page("drafts/wip.md", "label: \"WIP\"");

            var links = Generate(visible, folderIndex, child);

            Assert.That(Texts(links), Does.Contain("Visible"));
            Assert.That(Texts(links), Does.Not.Contain("Drafts"));
            Assert.That(Texts(links), Does.Not.Contain("WIP"));
        }
    }
}
