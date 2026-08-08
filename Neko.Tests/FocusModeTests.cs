using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    // Covers `neko watch --focus <path>`: only pages under the focus path are
    // regenerated, everything else keeps the output the previous build produced.
    public class FocusModeTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\n\nWelcome home.\n");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "guides"));
            File.WriteAllText(Path.Combine(_sampleDir, "guides", "install.md"), "# Install\n\nOriginal install text.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "guides", "upgrade.md"), "# Upgrade\n\nOriginal upgrade text.\n");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "reference"));
            File.WriteAllText(Path.Combine(_sampleDir, "reference", "api.md"), "# API\n\nA unique-reference-marker lives here.\n");
        }

        [Test]
        public async Task Focus_RebuildsOnlyPagesUnderThePath()
        {
            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusOut1");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await new SiteBuilder(_sampleDir, output).BuildAsync();

            var apiHtml = Path.Combine(output, "reference", "api.html");
            var apiBefore = await File.ReadAllTextAsync(apiHtml);
            var apiWriteBefore = File.GetLastWriteTimeUtc(apiHtml);

            // Edit one page inside the focus path and one outside it.
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "guides", "install.md"), "# Install\n\nUpdated install text.\n");
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "reference", "api.md"), "# API\n\nThis edit-outside-focus must not be published.\n");

            var focused = new SiteBuilder(_sampleDir, output, focus: FocusScope.Resolve(
                _sampleDir, Path.Combine(_sampleDir, "guides"), new[] { _sampleDir }));
            await focused.BuildAsync();

            Assert.That(await File.ReadAllTextAsync(Path.Combine(output, "guides", "install.html")),
                Does.Contain("Updated install text."), "Pages under the focus path are rebuilt.");

            Assert.That(await File.ReadAllTextAsync(apiHtml), Is.EqualTo(apiBefore),
                "A page outside the focus path keeps the HTML the previous build wrote.");
            Assert.That(File.GetLastWriteTimeUtc(apiHtml), Is.EqualTo(apiWriteBefore),
                "A page outside the focus path is not rewritten at all.");
        }

        [Test]
        public async Task Focus_KeepsSearchEntriesForPagesItSkipped()
        {
            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusOut2");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await new SiteBuilder(_sampleDir, output).BuildAsync();

            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "guides", "install.md"), "# Install\n\nA fresh-focus-marker was added.\n");

            var focused = new SiteBuilder(_sampleDir, output, focus: FocusScope.Resolve(
                _sampleDir, Path.Combine(_sampleDir, "guides"), new[] { _sampleDir }));
            await focused.BuildAsync();

            var search = await File.ReadAllTextAsync(Path.Combine(output, "search.json"));

            Assert.That(search, Does.Contain("fresh-focus-marker"),
                "The focused page's new content is indexed.");
            Assert.That(search, Does.Contain("unique-reference-marker"),
                "Pages outside the focus path keep their entries from the previous index.");
            Assert.That(search, Does.Contain("Welcome home"),
                "Root-level pages outside the focus path keep their entries too.");
        }

        [Test]
        public async Task Focus_OnASingleFile_RebuildsOnlyThatFile()
        {
            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusOut3");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await new SiteBuilder(_sampleDir, output).BuildAsync();

            var upgradeHtml = Path.Combine(output, "guides", "upgrade.html");
            var upgradeWriteBefore = File.GetLastWriteTimeUtc(upgradeHtml);

            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "guides", "install.md"), "# Install\n\nSingle-file focus edit.\n");

            var focused = new SiteBuilder(_sampleDir, output, focus: FocusScope.Resolve(
                _sampleDir, Path.Combine(_sampleDir, "guides", "install.md"), new[] { _sampleDir }));
            await focused.BuildAsync();

            Assert.That(await File.ReadAllTextAsync(Path.Combine(output, "guides", "install.html")),
                Does.Contain("Single-file focus edit."));
            Assert.That(File.GetLastWriteTimeUtc(upgradeHtml), Is.EqualTo(upgradeWriteBefore),
                "A sibling page in the same folder is not rebuilt when the focus is a single file.");
        }

        [Test]
        public async Task SkippedProject_IsNotBuiltAndKeepsItsOutput()
        {
            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusOut4");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await new SiteBuilder(_sampleDir, output).BuildAsync();

            var indexHtml = Path.Combine(output, "index.html");
            var indexBefore = await File.ReadAllTextAsync(indexHtml);

            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "index.md"), "# Home\n\nThis must not be published.\n");

            var skipped = new SiteBuilder(_sampleDir, output, focus: FocusScope.Skipped);
            await skipped.BuildAsync();

            Assert.That(skipped.OutputDirectory, Is.EqualTo(Path.GetFullPath(output)),
                "A skipped project still resolves its output directory so the dev server can serve it.");
            Assert.That(await File.ReadAllTextAsync(indexHtml), Is.EqualTo(indexBefore),
                "A skipped project's output is left untouched.");
        }

        [Test]
        public async Task Focus_DoesNotWipeTheOutputDirectory()
        {
            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "FocusOut5");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            await new SiteBuilder(_sampleDir, output).BuildAsync();

            // A file only the previous build knows about — a full build would wipe it.
            var keepMe = Path.Combine(output, "reference", "api.html");
            Assert.That(File.Exists(keepMe), Is.True);

            var focused = new SiteBuilder(_sampleDir, output, focus: FocusScope.Resolve(
                _sampleDir, Path.Combine(_sampleDir, "guides"), new[] { _sampleDir }));
            await focused.BuildAsync();

            Assert.That(File.Exists(keepMe), Is.True,
                "Focus mode must never clear the output directory — the reused pages live there.");
        }

        // --- FocusScope resolution ------------------------------------------------

        [Test]
        public void Resolve_AttributesTheFocusToTheMostSpecificProject()
        {
            var root = Path.Combine(_sampleDir, "site");
            var nested = Path.Combine(root, "api-docs");
            var sibling = Path.Combine(root, "guides-site");
            var all = new[] { root, nested, sibling };

            var focus = Path.Combine(nested, "endpoints");

            Assert.That(FocusScope.Resolve(nested, focus, all).Path, Is.EqualTo("endpoints"));
            Assert.That(FocusScope.Resolve(root, focus, all).SkipsProject, Is.True,
                "The parent project owns nothing under a focus path that lives in a nested project.");
            Assert.That(FocusScope.Resolve(sibling, focus, all).SkipsProject, Is.True);
        }

        [Test]
        public void Resolve_ProjectInsideTheFocusFolderBuildsInFull()
        {
            var root = Path.Combine(_sampleDir, "site");
            var nested = Path.Combine(root, "api-docs");
            var all = new[] { root, nested };

            var scope = FocusScope.Resolve(nested, root, all);

            Assert.That(scope.SkipsProject, Is.False);
            Assert.That(scope.Path, Is.Empty, "A project fully inside the focus folder rebuilds every page.");
            Assert.That(scope.Includes("anything/at/all.md"), Is.True);
        }

        [Test]
        public void Includes_MatchesTheTargetAndItsDescendantsOnly()
        {
            var root = Path.Combine(_sampleDir, "site");
            var scope = FocusScope.Resolve(root, Path.Combine(root, "guides"), new[] { root });

            Assert.That(scope.Includes("guides"), Is.True);
            Assert.That(scope.Includes("guides/install.md"), Is.True);
            Assert.That(scope.Includes("guides\\nested\\deep.md"), Is.True, "Windows separators are accepted.");
            Assert.That(scope.Includes("guides-extra/install.md"), Is.False, "A prefix match is not a path match.");
            Assert.That(scope.Includes("reference/api.md"), Is.False);
        }

        [Test]
        public void Intersects_MatchesAncestorsOfTheFocusToo()
        {
            var root = Path.Combine(_sampleDir, "site");
            var scope = FocusScope.Resolve(root, Path.Combine(root, "changelog", "v26.8.md"), new[] { root });

            Assert.That(scope.Intersects("changelog"), Is.True,
                "A folder that contains the focus target intersects it (aggregated pages must be rebuilt).");
            Assert.That(scope.Includes("changelog"), Is.False,
                "But the folder itself is not under the focus target.");
            Assert.That(scope.Intersects("guides"), Is.False);
        }
    }
}
