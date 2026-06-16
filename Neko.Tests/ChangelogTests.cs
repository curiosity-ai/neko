using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    public class ChangelogTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "ChangelogSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\n");

            var clDir = Path.Combine(_sampleDir, "changelog");
            Directory.CreateDirectory(clDir);
            File.WriteAllText(Path.Combine(clDir, "index.yml"),
                "changelog: true\ntitle: Changelog\ndescription: All notable changes.\nicon: memo\n");
            File.WriteAllText(Path.Combine(clDir, "v1.0.0.md"),
                "---\ntitle: First stable\ndate: 2024-06-18\n---\nThe **1.0.0** release notes.\n");
            File.WriteAllText(Path.Combine(clDir, "v1.2.0.md"),
                "---\ntitle: Newer\ndate: 2024-08-01\n---\nThe **1.2.0** release notes.\n");
            File.WriteAllText(Path.Combine(clDir, "v0.9.0.md"),
                "---\ndate: 2024-01-01\n---\nThe **0.9.0** release notes.\n");
        }

        [Test]
        public void ChangelogVersion_ParsesAndOrdersNewestFirst()
        {
            Assert.That(ChangelogVersion.TryParse("v1.2.0", out var a), Is.True);
            Assert.That(ChangelogVersion.TryParse("1.0.0", out var b), Is.True);
            Assert.That(ChangelogVersion.TryParse("v26.3.16", out var c), Is.True);
            Assert.That(ChangelogVersion.TryParse("v26.6", out var d), Is.True);

            // Display always gains a leading "v".
            Assert.That(a.Display, Is.EqualTo("v1.2.0"));
            Assert.That(b.Display, Is.EqualTo("v1.0.0"));

            // Numeric ordering: 1.2.0 > 1.0.0
            Assert.That(a.CompareTo(b), Is.GreaterThan(0));
            // Calendar versioning: 26.6 (June) is newer than 26.3.16 (March patch)
            Assert.That(d.CompareTo(c), Is.GreaterThan(0));

            // Anchor slug is id-safe.
            Assert.That(d.Anchor, Is.EqualTo("v26-6"));
        }

        [Test]
        public void ChangelogVersion_RejectsNonVersionNames()
        {
            Assert.That(ChangelogVersion.TryParse("index", out _), Is.False);
            Assert.That(ChangelogVersion.TryParse("readme", out _), Is.False);
            Assert.That(ChangelogVersion.TryParse("", out _), Is.False);
        }

        [Test]
        public async Task ChangelogFolder_AggregatesIntoSinglePageNewestFirst()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var page = Path.Combine(builder.OutputDirectory, "changelog", "index.html");
            Assert.That(File.Exists(page), Is.True, "changelog/index.html should be generated");

            var html = await File.ReadAllTextAsync(page);

            // Title + description from the folder yml.
            Assert.That(html, Does.Contain("<h1>Changelog</h1>"));
            Assert.That(html, Does.Contain("All notable changes."));

            // Version badges present, optional headlines present.
            Assert.That(html, Does.Contain(">v1.2.0<"));
            Assert.That(html, Does.Contain(">v1.0.0<"));
            Assert.That(html, Does.Contain(">v0.9.0<"));
            Assert.That(html, Does.Contain("First stable"));

            // Newest first: 1.2.0 appears before 1.0.0 which appears before 0.9.0.
            var i12 = html.IndexOf("v1.2.0");
            var i10 = html.IndexOf("v1.0.0");
            var i09 = html.IndexOf("v0.9.0");
            Assert.That(i12, Is.LessThan(i10));
            Assert.That(i10, Is.LessThan(i09));

            // Per-version anchors for deep linking.
            Assert.That(html, Does.Contain("id=\"v1-2-0\""));
        }

        [Test]
        public async Task ChangelogFolder_DoesNotEmitStandaloneVersionPages()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var clOut = Path.Combine(builder.OutputDirectory, "changelog");
            Assert.That(File.Exists(Path.Combine(clOut, "v1.0.0.html")), Is.False);
            Assert.That(File.Exists(Path.Combine(clOut, "v1.2.0.html")), Is.False);
            Assert.That(File.Exists(Path.Combine(clOut, "v0.9.0.html")), Is.False);
        }

        [Test]
        public async Task ChangelogFolder_IndexedAsSinglePageInSearch()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var searchJson = await File.ReadAllTextAsync(Path.Combine(builder.OutputDirectory, "search.json"));
            Assert.That(searchJson, Does.Contain("changelog/index.html"));
            // The version files are not indexed as their own documents.
            Assert.That(searchJson, Does.Not.Contain("changelog/v1.0.0"));
        }

        // A changelog living inside a sub-project (a folder with its own neko.yml)
        // belongs to that sub-project's build. The root build excludes the
        // sub-project's markdown from scanning, so it must NOT discover and emit an
        // (empty) aggregated page for that folder — otherwise the root site's
        // catch-all file server shadows the sub-project's real changelog at the
        // shared URL. See SiteBuilder.DiscoverChangelogFolders.
        [Test]
        public async Task RootBuild_DoesNotEmitChangelogForSubProjectFolder()
        {
            var rootDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "MultiProjectChangelogSample");
            if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true);
            Directory.CreateDirectory(rootDir);

            File.WriteAllText(Path.Combine(rootDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: Root\n");
            File.WriteAllText(Path.Combine(rootDir, "index.md"), "# Home\n");

            // A sub-project: its own neko.yml makes `sub` a separate project, so the
            // root build excludes it.
            var subDir = Path.Combine(rootDir, "sub");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "neko.yml"), "url: https://example.com/sub\nbranding:\n  title: Sub\n");
            File.WriteAllText(Path.Combine(subDir, "index.md"), "# Sub Home\n");

            var clDir = Path.Combine(subDir, "changelog");
            Directory.CreateDirectory(clDir);
            File.WriteAllText(Path.Combine(clDir, "index.yml"),
                "changelog: true\ntitle: Changelog\ndescription: Sub changes.\nicon: memo\n");
            File.WriteAllText(Path.Combine(clDir, "v1.0.0.md"),
                "---\ndate: 2024-06-18\n---\nThe **1.0.0** release notes.\n");

            // Build the ROOT project (mirrors `neko watch` building each project).
            var rootBuilder = new SiteBuilder(rootDir);
            await rootBuilder.BuildAsync();

            // The root build must not produce a (shadowing) changelog page for the
            // sub-project's folder.
            var shadowPage = Path.Combine(rootBuilder.OutputDirectory, "sub", "changelog", "index.html");
            Assert.That(File.Exists(shadowPage), Is.False,
                "root build must not emit a changelog page for a sub-project's folder");

            // Building the sub-project itself still produces the real changelog.
            var subBuilder = new SiteBuilder(subDir, routePrefix: "/sub");
            await subBuilder.BuildAsync();
            var realPage = Path.Combine(subBuilder.OutputDirectory, "changelog", "index.html");
            Assert.That(File.Exists(realPage), Is.True, "sub-project build should emit its changelog");
            var html = await File.ReadAllTextAsync(realPage);
            Assert.That(html, Does.Contain(">v1.0.0<"), "sub-project changelog should contain its version entry");
        }
    }
}
