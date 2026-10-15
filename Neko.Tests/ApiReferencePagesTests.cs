using System;
using System.IO;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ApiReferencePagesTests
    {
        private string _docs = "";
        private string _src = "";
        private string _typeDir = "";
        private string _page = "";

        private const string Markers =
            "<!-- api:type start repo=\"demo\" file=\"Widget.cs;Widget.Part.cs\" type=\"Widget\" namespaceLink=\"../\" " +
            "package=\"Demo.Widgets\" version=\"1.2.3\" source=\"https://example.com/blob/main/\" -->\n<!-- api:type end -->\n\n" +
            "## Examples\n\nHand-written.\n\n" +
            "<!-- api:members start -->\n<!-- api:members end -->\n";

        [SetUp]
        public void Setup()
        {
            var root = Path.Combine(Path.GetTempPath(), "neko-apiref-" + Path.GetRandomFileName());
            _docs = Path.Combine(root, "docs");
            _src = Path.Combine(root, "src");
            _typeDir = Path.Combine(_docs, "api", "widget");
            Directory.CreateDirectory(_typeDir);
            Directory.CreateDirectory(_src);

            File.WriteAllText(Path.Combine(_docs, "neko.yml"),
                $"apiDocs:\n  roots:\n    demo: {_src.Replace("\\", "/")}\n");

            File.WriteAllText(Path.Combine(_src, "Widget.cs"), @"using System;

namespace Demo
{
    /// <summary>A widget. See <see cref=""Render(string)""/>.</summary>
    public partial class Widget
    {
        /// <summary>Creates a widget.</summary>
        public Widget() { }

        /// <summary>The widget's name.</summary>
        /// <value>The name, or <see langword=""null""/>.</value>
        public string Name { get; private set; }

        /// <summary>Renders the widget.</summary>
        /// <param name=""format"">The output format.</param>
        /// <returns>The rendered text.</returns>
        /// <exception cref=""ArgumentNullException""><paramref name=""format""/> is null.</exception>
        /// <example>
        /// <code>
        /// var text = widget.Render(""html"");
        /// </code>
        /// </example>
        public string Render(string format) => format;

        /// <summary>Renders the widget with options.</summary>
        public string Render(string format, int width) => format;

        /// <summary>Copies <typeparamref name=""T""/>.</summary>
        /// <typeparam name=""T"">The target type.</typeparam>
        public T CopyTo<T>(T target) where T : class => target;

        private void Secret() { }

        internal void Hidden() { }
    }
}");

            // The modifiers and base list live on the other partial, as they often do.
            File.WriteAllText(Path.Combine(_src, "Widget.Part.cs"), @"using System;

namespace Demo
{
    public sealed partial class Widget : IDisposable
    {
        public void Dispose() { }
    }
}");

            _page = Path.Combine(_typeDir, "index.md");
            File.WriteAllText(_page, "# Widget Class\n\n" + Markers);
        }

        [Test]
        public void WritesDefinitionInheritanceAndMemberTablesOnTheTypePage()
        {
            ApiDocsSync.Run(_docs);
            var text = File.ReadAllText(_page);

            Assert.That(text, Does.Contain("## Definition"));
            Assert.That(text, Does.Contain("<p><span>Namespace:</span> <a href=\"/api/\">Demo</a></p>"));
            Assert.That(text, Does.Contain("https://www.nuget.org/packages/Demo.Widgets/1.2.3"));
            Assert.That(text, Does.Contain("public sealed class Widget : IDisposable"), "modifiers and bases from every partial");
            Assert.That(text, Does.Contain("<span>Inheritance</span>"));
            Assert.That(text, Does.Contain("## Constructors"));
            Assert.That(text, Does.Contain("## Properties"));
            Assert.That(text, Does.Contain("## Methods"));
            Assert.That(text, Does.Contain("Render(String, Int32)"), "one row per overload");
            Assert.That(text, Does.Contain("(Inherited from [Object]"));
            Assert.That(text, Does.Contain("## Applies to"));
            Assert.That(text, Does.Contain("## Examples\n\nHand-written."), "prose between the markers is kept");
            Assert.That(text, Does.Not.Contain("Secret").And.Not.Contain("Hidden"));
        }

        [Test]
        public void WritesOnePagePerMemberGroupWithOverloadsAndSourceLines()
        {
            ApiDocsSync.Run(_docs);

            var render = File.ReadAllText(Path.Combine(_typeDir, "methods", "render.md"));
            Assert.That(render, Does.StartWith("---\nlabel: \"Render\"\n---\n"));
            Assert.That(render, Does.Contain(ApiReferencePages.GeneratedNotice));
            Assert.That(render, Does.Contain("# Widget.Render Method"));
            Assert.That(render, Does.Contain("## Overloads"));
            Assert.That(render, Does.Contain("## Render(String) {#render-string}"));
            Assert.That(render, Does.Contain("## Render(String, Int32) {#render-string-int32}"));
            Assert.That(render, Does.Contain("https://example.com/blob/main/Widget.cs#L24"), "source link to the declaration line");
            Assert.That(render, Does.Contain("<code class=\"api-ref-name\">format</code>"));
            Assert.That(render, Does.Contain("learn.microsoft.com/dotnet/api/system.argumentnullexception"));
            Assert.That(render, Does.Contain("```csharp title=\"C#\"\nvar text = widget.Render(\"html\");\n```"));

            var copy = File.ReadAllText(Path.Combine(_typeDir, "methods", "copy-to.md"));
            Assert.That(copy, Does.Contain("# Widget.CopyTo&lt;T&gt;(T) Method"));
            Assert.That(copy, Does.Contain("Type Parameters"));

            var name = File.ReadAllText(Path.Combine(_typeDir, "properties", "name.md"));
            Assert.That(name, Does.Contain("public string Name { get; }"), "a private setter is not part of the surface");
            Assert.That(name, Does.Contain("Property Value"));

            Assert.That(File.Exists(Path.Combine(_typeDir, "constructors.md")));
            Assert.That(File.ReadAllText(Path.Combine(_typeDir, "methods", "index.yml")), Does.Contain("label: \"Methods\""));
        }

        [Test]
        public void ResolvesCrefsToRootRelativeLinks()
        {
            ApiDocsSync.Run(_docs);
            var text = File.ReadAllText(_page);
            Assert.That(text, Does.Contain("A widget. See [Render](/api/widget/methods/render)."));

            var render = File.ReadAllText(Path.Combine(_typeDir, "methods", "render.md"));
            Assert.That(render, Does.Contain("<a href=\"/api/\">Demo</a>"), "namespaceLink is written root-relative on every page");
        }

        [Test]
        public void RemovesStalePagesItGeneratedButNeverHandWrittenOnes()
        {
            ApiDocsSync.Run(_docs);
            var stale = Path.Combine(_typeDir, "methods", "gone.md");
            var handWritten = Path.Combine(_typeDir, "methods", "notes.md");
            File.WriteAllText(stale, "---\nlabel: \"Gone\"\n---\n\n" + ApiReferencePages.GeneratedNotice + "\n");
            File.WriteAllText(handWritten, "# Notes\n");

            ApiDocsSync.Run(_docs);

            Assert.That(File.Exists(stale), Is.False);
            Assert.That(File.Exists(handWritten), Is.True);
        }

        [Test]
        public void IsIdempotent()
        {
            ApiDocsSync.Run(_docs);
            var before = File.ReadAllText(_page);
            var second = ApiDocsSync.Run(_docs);

            Assert.That(second.FilesUpdated, Is.EqualTo(0));
            Assert.That(File.ReadAllText(_page), Is.EqualTo(before));
        }

        [Test]
        public void LeavesEverythingAsItIsWhenTheRootIsMissing()
        {
            var original = "# Widget Class\n\n" + Markers.Replace("repo=\"demo\"", "repo=\"unknown\"");
            File.WriteAllText(_page, original);

            var result = ApiDocsSync.Run(_docs);

            Assert.That(result.Skipped, Is.EqualTo(1));
            Assert.That(File.ReadAllText(_page), Is.EqualTo(original));
            Assert.That(Directory.Exists(Path.Combine(_typeDir, "methods")), Is.False);
        }

        [Test]
        public void ReportsAMissingRootOncePerRepoAndSaysWhy()
        {
            File.WriteAllText(_page, "# Widget Class\n\n" + Markers.Replace("repo=\"demo\"", "repo=\"unknown\""));
            var other = Path.Combine(_docs, "api", "gadget", "index.md");
            Directory.CreateDirectory(Path.GetDirectoryName(other)!);
            File.WriteAllText(other, "# Gadget Class\n\n" + Markers.Replace("repo=\"demo\"", "repo=\"unknown\"").Replace("Widget", "Gadget"));

            var original = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            try { ApiDocsSync.Run(_docs); }
            finally { Console.SetOut(original); }

            var log = captured.ToString();
            Assert.That(log, Does.Contain("no source for repo 'unknown' (the root neko.yml declares no apiDocs.roots.unknown): left 2 page(s) as committed."));
            Assert.That(log.Split("no source for repo").Length - 1, Is.EqualTo(1), "one line for the repo, not one per page");
        }

        [Test]
        public void PutsMemberPagesInAFolderNamedAfterAPageThatIsNotAnIndex()
        {
            var page = Path.Combine(_docs, "api", "widget.md");
            File.Delete(_page);
            File.WriteAllText(page, "# Widget Class\n\n" + Markers);

            ApiDocsSync.Run(_docs);

            Assert.That(File.Exists(Path.Combine(_docs, "api", "widget", "methods", "render.md")));
            Assert.That(File.ReadAllText(page), Does.Contain("](/api/widget/methods/render#render-string)"));
        }
    }
}
