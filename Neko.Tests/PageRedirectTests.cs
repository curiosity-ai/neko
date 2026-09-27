using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    public class PageRedirectTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "PageRedirectSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\n");
            File.WriteAllText(Path.Combine(_sampleDir, "getting-started.md"), "# Getting started\n\nThe new page.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "setup.md"),
                "---\nlabel: Setup\nredirect: getting-started.md\n---\n# Setup\n\nOld content that must not be rendered.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "external.md"),
                "---\nredirect: https://example.org/docs\n---\n# External\n");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "guides"));
            File.WriteAllText(Path.Combine(_sampleDir, "guides", "old.md"),
                "---\nredirect: ../getting-started.md#install\n---\n# Old guide\n");
            File.WriteAllText(Path.Combine(_sampleDir, "guides", "rooted.md"),
                "---\nredirect: /somewhere/else/\n---\n# Rooted\n");
        }

        private static async Task<string> ReadOutputAsync(SiteBuilder builder, string relativePath)
        {
            var path = Path.Combine(builder.OutputDirectory, relativePath);
            Assert.That(File.Exists(path), Is.True, $"{relativePath} should exist");
            return await File.ReadAllTextAsync(path);
        }

        [Test]
        public async Task Redirect_ReplacesPageWithRedirectToRelativeTarget()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var html = await ReadOutputAsync(builder, "setup.html");
            Assert.That(html, Does.Contain("<meta http-equiv=\"refresh\" content=\"0; url=/getting-started\">"));
            Assert.That(html, Does.Contain("window.location.replace(\"/getting-started\")"));
            Assert.That(html, Does.Not.Contain("Old content that must not be rendered"));
        }

        [Test]
        public async Task Redirect_ResolvesParentPathAndKeepsAnchor()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var html = await ReadOutputAsync(builder, Path.Combine("guides", "old.html"));
            Assert.That(html, Does.Contain("url=/getting-started#install"));
        }

        [Test]
        public async Task Redirect_KeepsRootRelativeAndExternalTargets()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            Assert.That(await ReadOutputAsync(builder, Path.Combine("guides", "rooted.html")), Does.Contain("url=/somewhere/else/"));
            Assert.That(await ReadOutputAsync(builder, "external.html"), Does.Contain("url=https://example.org/docs"));
        }

        [Test]
        public async Task Redirect_AppliesRoutePrefixToRelativeTargets()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "PageRedirectOut_prefix");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, false, "/workspace");
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "setup.html"));
            Assert.That(html, Does.Contain("url=/workspace/getting-started"));

            // Root-relative targets are relative to the site root, not the sub-project.
            var rooted = await File.ReadAllTextAsync(Path.Combine(outDir, "guides", "rooted.html"));
            Assert.That(rooted, Does.Contain("url=/somewhere/else/"));
        }

        [Test]
        public async Task Redirect_PageIsLeftOutOfSidebarSearchAndSitemap()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var home = await ReadOutputAsync(builder, "index.html");
            Assert.That(home, Does.Contain("getting-started"));
            Assert.That(home, Does.Not.Contain("href=\"/setup\""));
            Assert.That(home, Does.Not.Contain(">Setup<"));

            var search = await ReadOutputAsync(builder, "search.json");
            Assert.That(search, Does.Not.Contain("setup.html"));
            Assert.That(search, Does.Not.Contain("old.html"));

            var sitemap = await ReadOutputAsync(builder, "sitemap.xml");
            Assert.That(sitemap, Does.Not.Contain("/setup<"));
            Assert.That(sitemap, Does.Not.Contain("/guides/old<"));
            Assert.That(sitemap, Does.Contain("/getting-started<"));
        }
    }
}
