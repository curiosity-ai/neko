using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    public class RedirectSlugTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RedirectSlugSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\n");
            File.WriteAllText(Path.Combine(_sampleDir, "about.md"),
                "---\nredirectSlug: a\n---\n# About\n");
            Directory.CreateDirectory(Path.Combine(_sampleDir, "docs"));
            File.WriteAllText(Path.Combine(_sampleDir, "docs", "index.md"),
                "---\nredirectSlug: docs-home\n---\n# Docs Home\n");
        }

        [Test]
        public async Task RedirectSlug_WritesRedirectHtmlForPage()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var redirectPath = Path.Combine(builder.OutputDirectory, "redirect", "a.html");
            Assert.That(File.Exists(redirectPath), Is.True, "redirect/a.html should exist");

            var html = await File.ReadAllTextAsync(redirectPath);
            Assert.That(html, Does.Contain("<meta http-equiv=\"refresh\" content=\"0; url=/about\">"));
            Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"/about\">"));
            Assert.That(html, Does.Contain("window.location.replace(\"/about\")"));
            Assert.That(html, Does.Contain("<a href=\"/about\">"));
        }

        [Test]
        public async Task RedirectSlug_CollapsesIndexToFolderUrl()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var redirectPath = Path.Combine(builder.OutputDirectory, "redirect", "docs-home.html");
            Assert.That(File.Exists(redirectPath), Is.True, "redirect/docs-home.html should exist");

            var html = await File.ReadAllTextAsync(redirectPath);
            // /docs/index → /docs/ (trailing slash stripped)
            Assert.That(html, Does.Contain("url=/docs/"));
        }

        [Test]
        public async Task RedirectSlug_AppliesRoutePrefix()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RedirectSlugOut_prefix");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, false, "/workspace");
            await builder.BuildAsync();

            var redirectPath = Path.Combine(outDir, "redirect", "a.html");
            Assert.That(File.Exists(redirectPath), Is.True);

            var html = await File.ReadAllTextAsync(redirectPath);
            Assert.That(html, Does.Contain("url=/workspace/about"));
        }

        [Test]
        public async Task RedirectSlug_SkippedWhenAbsent()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            // index.md has no redirectSlug — no redirect/index.html should exist
            var unwanted = Path.Combine(builder.OutputDirectory, "redirect", "index.html");
            Assert.That(File.Exists(unwanted), Is.False);
        }

        [Test]
        public async Task RedirectSlug_RejectsSlugContainingSlash()
        {
            var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RedirectSlashSample");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "neko.yml"), "url: https://example.com\n");
            File.WriteAllText(Path.Combine(dir, "page.md"),
                "---\nredirectSlug: nested/path\n---\n# Page\n");

            var builder = new SiteBuilder(dir);
            await builder.BuildAsync();

            var redirectDir = Path.Combine(builder.OutputDirectory, "redirect");
            // The nested-path slug is rejected, so no file is emitted.
            Assert.That(!Directory.Exists(redirectDir) || Directory.GetFiles(redirectDir).Length == 0,
                "Slugs containing '/' should be rejected.");
        }
    }
}
