using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    // `neko watch --no-password` (SiteBuilder's disablePasswords ctor flag) is
    // meant purely as a local-authoring convenience: it must make every
    // password-protected page render straight through, whether the password
    // comes from the site-wide `password:` config key or a page's own
    // frontmatter, without touching a normal `build` (disablePasswords: false).
    public class WatchDisablePasswordsTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "WatchDisablePasswordsSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            // Site-wide password, plus one page that opts out (`password: none`)
            // and one page with its own distinct per-page password.
            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"),
                "url: https://example.com\npassword: site-secret\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home\nPublicly-inherited-password content.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "opted-out.md"),
                "---\npassword: none\n---\n# Opted Out\nAlways-public content.\n");
            File.WriteAllText(Path.Combine(_sampleDir, "own-password.md"),
                "---\npassword: page-secret\n---\n# Own Password\nPage-specific-password content.\n");
        }

        [Test]
        public async Task DisablePasswords_RendersSiteWidePasswordPageInPlaintext()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "WatchDisablePasswordsOut_site");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, isWatchMode: true, disablePasswords: true);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));
            Assert.That(html, Does.Contain("Publicly-inherited-password content."));
            Assert.That(html, Does.Not.Contain("encrypted-data"));
        }

        [Test]
        public async Task DisablePasswords_RendersPagePasswordInPlaintext()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "WatchDisablePasswordsOut_page");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, isWatchMode: true, disablePasswords: true);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "own-password.html"));
            Assert.That(html, Does.Contain("Page-specific-password content."));
            Assert.That(html, Does.Not.Contain("encrypted-data"));
        }

        [Test]
        public async Task WithoutFlag_PasswordProtectedPagesStayEncrypted()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "WatchDisablePasswordsOut_default");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            // Same site, but the flag is off (the `build` command's default) —
            // password protection must be completely unaffected.
            var builder = new SiteBuilder(_sampleDir, outDir, isWatchMode: true, disablePasswords: false);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));
            Assert.That(html, Does.Not.Contain("Publicly-inherited-password content."));
            Assert.That(html, Does.Contain("encrypted-data"));

            var ownPasswordHtml = await File.ReadAllTextAsync(Path.Combine(outDir, "own-password.html"));
            Assert.That(ownPasswordHtml, Does.Not.Contain("Page-specific-password content."));
            Assert.That(ownPasswordHtml, Does.Contain("encrypted-data"));
        }

        [Test]
        public async Task DisablePasswords_OptedOutPageIsUnaffected()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "WatchDisablePasswordsOut_optout");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, isWatchMode: true, disablePasswords: true);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "opted-out.html"));
            Assert.That(html, Does.Contain("Always-public content."));
            Assert.That(html, Does.Not.Contain("encrypted-data"));
        }
    }
}
