using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    // Covers SiteBuilder.TryRebuildSinglePageAsync — the watch-mode fast path that
    // regenerates only the changed page instead of rebuilding the whole project.
    public class IncrementalRebuildTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "IncrementalSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\n\nWelcome home.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "about.md"), "# About\n\nOriginal about text.\n");
        }

        [Test]
        public async Task TryRebuildSinglePage_BodyEdit_RegeneratesOnlyThatPage()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexHtml = Path.Combine(builder.OutputDirectory, "index.html");
            var aboutHtml = Path.Combine(builder.OutputDirectory, "about.html");

            // Capture the index.html so we can prove it isn't touched by the
            // single-page rebuild of about.md.
            var indexBefore = await File.ReadAllTextAsync(indexHtml);
            var indexWriteBefore = File.GetLastWriteTimeUtc(indexHtml);

            var aboutPath = Path.Combine(_sampleDir, "about.md");
            await File.WriteAllTextAsync(aboutPath, "# About\n\nUpdated about text appears here.\n");

            var handled = await builder.TryRebuildSinglePageAsync(aboutPath);

            Assert.That(handled, Is.True, "A body-only edit should be handled incrementally.");

            var aboutAfter = await File.ReadAllTextAsync(aboutHtml);
            Assert.That(aboutAfter, Does.Contain("Updated about text appears here."));

            // index.html must be byte-for-byte unchanged and not rewritten.
            Assert.That(await File.ReadAllTextAsync(indexHtml), Is.EqualTo(indexBefore),
                "Editing about.md must not change index.html.");
            Assert.That(File.GetLastWriteTimeUtc(indexHtml), Is.EqualTo(indexWriteBefore),
                "index.html should not be rewritten by an incremental rebuild.");

            // search.json should reflect the new content.
            var search = await File.ReadAllTextAsync(Path.Combine(builder.OutputDirectory, "search.json"));
            Assert.That(search, Does.Contain("Updated about text"));
        }

        [Test]
        public async Task TryRebuildSinglePage_StructuralFrontmatterChange_FallsBackToFullRebuild()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var aboutPath = Path.Combine(_sampleDir, "about.md");
            // Changing the title affects the sidebar / prev-next on other pages.
            await File.WriteAllTextAsync(aboutPath, "---\ntitle: Renamed\n---\n# About\n\nBody.\n");

            var handled = await builder.TryRebuildSinglePageAsync(aboutPath);

            Assert.That(handled, Is.False, "A title change should fall back to a full rebuild.");
        }

        [Test]
        public async Task TryRebuildSinglePage_OutgoingLinkChange_FallsBackToFullRebuild()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var aboutPath = Path.Combine(_sampleDir, "about.md");
            // A new outgoing link changes index.md's backlinks, so a single-page
            // rebuild would leave them stale.
            await File.WriteAllTextAsync(aboutPath, "# About\n\nSee [Home](index.md).\n");

            var handled = await builder.TryRebuildSinglePageAsync(aboutPath);

            Assert.That(handled, Is.False, "Adding an outgoing link should fall back to a full rebuild.");
        }

        [Test]
        public async Task TryRebuildSinglePage_NewFile_FallsBackToFullRebuild()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            // A file that didn't exist during the last build is structural.
            var newPath = Path.Combine(_sampleDir, "new-page.md");
            await File.WriteAllTextAsync(newPath, "# New\n");

            var handled = await builder.TryRebuildSinglePageAsync(newPath);

            Assert.That(handled, Is.False, "A brand-new page should fall back to a full rebuild.");
        }

        [Test]
        public async Task TryRebuildSinglePage_BeforeAnyBuild_ReturnsFalse()
        {
            var builder = new SiteBuilder(_sampleDir);

            var handled = await builder.TryRebuildSinglePageAsync(Path.Combine(_sampleDir, "about.md"));

            Assert.That(handled, Is.False, "Without a prior full build there is no cached state to reuse.");
        }
    }
}
