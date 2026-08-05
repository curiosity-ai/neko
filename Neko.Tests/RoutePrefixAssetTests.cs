using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    // A sub-project (route prefix set — multi-repo `neko watch`, or any
    // deployment nesting projects under one domain) must request every one of
    // its own static assets under its own prefix. `tailwind.css` already did
    // this; the UIcons/emoji stylesheets and several head/auxiliary scripts
    // were hard-coded to the domain root instead, so a sub-project page asked
    // for the *root* project's copy — 404ing (and leaving icons unrendered)
    // whenever there was no root project sharing that path.
    public class RoutePrefixAssetTests
    {
        private string _sampleDir = null!;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), "# Home :icon-home:\n");
        }

        [Test]
        public async Task SubProject_PrefixesUIconsAndEmojiStylesheets()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetOut_uicons");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, false, "/workspace");
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));

            Assert.That(html, Does.Contain("href=\"/workspace/assets/uicons-regular-rounded.css\""));
            Assert.That(html, Does.Contain("href=\"/workspace/assets/uicons-brands.css\""));
            Assert.That(html, Does.Contain("href=\"/workspace/assets/emoji.css\""));

            // Never fall back to the un-prefixed, domain-root path.
            Assert.That(html, Does.Not.Contain("href=\"/assets/uicons-regular-rounded.css\""));
            Assert.That(html, Does.Not.Contain("href=\"/assets/uicons-brands.css\""));
            Assert.That(html, Does.Not.Contain("href=\"/assets/emoji.css\""));
        }

        [Test]
        public async Task SubProject_PrefixesAuxiliaryScriptsAndHighlightAssets()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetOut_scripts");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir, false, "/workspace");
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));

            Assert.That(html, Does.Contain("src=\"/workspace/assets/force-graph.min.js\""));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/minisearch.min.js\""));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/search.js\""));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/history.js\""));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/icons.js\""));
            Assert.That(html, Does.Contain("href=\"/workspace/assets/highlight/"));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/highlight/highlight.min.js\""));
        }

        [Test]
        public async Task SubProject_PrefixesPasswordScript()
        {
            var protectedDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetSample_protected");
            if (Directory.Exists(protectedDir)) Directory.Delete(protectedDir, true);
            Directory.CreateDirectory(protectedDir);
            File.WriteAllText(Path.Combine(protectedDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(protectedDir, "index.md"), "---\npassword: letmein\n---\n# Secret\n");

            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetOut_password");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(protectedDir, outDir, false, "/workspace");
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));
            Assert.That(html, Does.Contain("src=\"/workspace/assets/password.js\""));
        }

        [Test]
        public async Task RootProject_KeepsUnprefixedAssetPaths()
        {
            var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "RoutePrefixAssetOut_root");
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            var builder = new SiteBuilder(_sampleDir, outDir);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(outDir, "index.html"));

            Assert.That(html, Does.Contain("href=\"/assets/uicons-regular-rounded.css\""));
            Assert.That(html, Does.Contain("href=\"/assets/uicons-brands.css\""));
            Assert.That(html, Does.Contain("href=\"/assets/emoji.css\""));
        }
    }
}
