using System;
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

            // Roots are resolved solely from the root neko.yml's apiDocs.roots.
            File.WriteAllText(Path.Combine(_docs, "neko.yml"),
                $"apiDocs:\n  roots:\n    demo: {_src.Replace("\\", "/")}\n");

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

        private string Md(string body) => $"# Page\n\n{body}\n";

        [Test]
        public void RegeneratesPublicSurfaceWithoutBodiesOrPrivateMembers()
        {
            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            var result = ApiDocsSync.Run(_docs);
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

            var result = ApiDocsSync.Run(_docs);

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

            var result = ApiDocsSync.Run(_docs);

            Assert.That(result.FilesUpdated, Is.EqualTo(0));
            Assert.That(result.Skipped, Is.EqualTo(0));
            Assert.That(File.ReadAllText(page), Is.EqualTo(original));
        }

        [Test]
        public void ResolvesRootsFromRootConfigOnlyAndIgnoresNestedConfigs()
        {
            // Root neko.yml declares the real source root (relative to itself).
            File.WriteAllText(Path.Combine(_docs, "neko.yml"),
                "apiDocs:\n  roots:\n    demo: ../src\n");

            // A nested sub-project config points the same name at a bogus path; it
            // must be ignored now that only the root config is consulted.
            var nested = Path.Combine(_docs, "sub");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(nested, "neko.yml"),
                "apiDocs:\n  roots:\n    demo: ./does-not-exist\n");

            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            var result = ApiDocsSync.Run(_docs);

            Assert.That(result.FilesUpdated, Is.EqualTo(1));
            Assert.That(File.ReadAllText(page), Contains.Substring("public int Bar(int x);"));
        }

        [Test]
        public void LogsSyncedXmlDocCommentCount()
        {
            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            var original = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            try { ApiDocsSync.Run(_docs); }
            finally { Console.SetOut(original); }

            // Foo carries a <summary> on the class and on Bar(int); the private /
            // internal members are dropped, so two doc comments are synced.
            Assert.That(captured.ToString(),
                Does.Contain("synced 2 XML doc comment(s) from demo:Foo.cs"));
            Assert.That(captured.ToString(), Does.Contain("foo.md"));
        }

        [Test]
        public void IsIdempotent()
        {
            var page = Path.Combine(_docs, "foo.md");
            File.WriteAllText(page, Md(
                "<!-- api:source start repo=\"demo\" file=\"Foo.cs\" type=\"Foo\" -->\n<!-- api:source end -->"));

            ApiDocsSync.Run(_docs);
            var afterFirst = File.ReadAllText(page);
            var second = ApiDocsSync.Run(_docs);

            Assert.That(second.FilesUpdated, Is.EqualTo(0));
            Assert.That(File.ReadAllText(page), Is.EqualTo(afterFirst));
        }
    }
}
