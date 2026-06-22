using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ApiDocsSyncTests
    {
        private string _docs = "";
        private string _src = "";

        [SetUp]
        public void Setup()
        {
            var root = Path.Combine(Path.GetTempPath(), "neko-apidocs-" + Path.GetRandomFileName());
            _docs = Path.Combine(root, "docs");
            _src = Path.Combine(root, "src");
            Directory.CreateDirectory(_docs);
            Directory.CreateDirectory(_src);

            File.WriteAllText(Path.Combine(_src, "Foo.cs"), @"
namespace Demo
{
    /// <summary>A widget.</summary>
    public class Foo
    {
        /// <summary>Does the thing with <paramref name=""x""/>.</summary>
        public int Bar(int x)
        {
            return x + Secret();
        }

        private int Secret()
        {
            return 41;
        }

        internal void Hidden()
        {
        }
    }
}");
        }

        private IDictionary<string, string> Roots() =>
            new Dictionary<string, string> { ["demo"] = _src };

        private string Md(string body) => $"# Page\n\n{body}\n";

        [Test]
        public void RegeneratesPublicSurfaceWithoutBodiesOrPrivateMembers()
        {
            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            var result = ApiDocsSync.Run(_docs, Roots());
            var text = File.ReadAllText(page);

            Assert.That(result.FilesUpdated, Is.EqualTo(1));
            Assert.That(text, Contains.Substring("```csharp-docs"));
            // Public signature is kept, body stripped.
            Assert.That(text, Contains.Substring("public int Bar(int x);"));
            Assert.That(text, Does.Not.Contain("return x + Secret()"));
            // Doc comment rides along.
            Assert.That(text, Contains.Substring("Does the thing"));
            // Private / internal members are gone.
            Assert.That(text, Does.Not.Contain("Secret"));
            Assert.That(text, Does.Not.Contain("Hidden"));
        }

        [Test]
        public void LeavesBlockIntactWhenRootMissing()
        {
            var page = Path.Combine(_docs, "skip.md");
            var original = Md(
                "<!-- api:source start repo=\"unknown\" file=\"Foo.cs\" type=\"Foo\" -->\nKEEP ME\n<!-- api:source end -->");
            File.WriteAllText(page, original);

            var result = ApiDocsSync.Run(_docs, Roots());

            Assert.That(result.Skipped, Is.EqualTo(1));
            Assert.That(result.FilesUpdated, Is.EqualTo(0));
            Assert.That(File.ReadAllText(page), Is.EqualTo(original));
        }

        [Test]
        public void IgnoresMarkersInsideFencedCodeBlocks()
        {
            var page = Path.Combine(_docs, "doc.md");
            var original = "# Docs\n\nExample:\n\n````markdown\n" +
                           "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n" +
                           "<!-- api:source end -->\n````\n";
            File.WriteAllText(page, original);

            var result = ApiDocsSync.Run(_docs, Roots());

            Assert.That(result.FilesUpdated, Is.EqualTo(0));
            Assert.That(result.Skipped, Is.EqualTo(0));
            Assert.That(File.ReadAllText(page), Is.EqualTo(original));
        }

        [Test]
        public void IsIdempotent()
        {
            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            ApiDocsSync.Run(_docs, Roots());
            var afterFirst = File.ReadAllText(page);
            var second = ApiDocsSync.Run(_docs, Roots());

            Assert.That(second.FilesUpdated, Is.EqualTo(0));
            Assert.That(File.ReadAllText(page), Is.EqualTo(afterFirst));
        }
    }
}
