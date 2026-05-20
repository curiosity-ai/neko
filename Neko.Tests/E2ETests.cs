using System.Threading.Tasks;
using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.IO;

namespace Neko.Tests
{
    public class E2ETests
    {
        private string _sampleDir;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "E2ESample");

            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            // Create sample config and files
            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), @"
url: https://example.com
sitemap: true
branding:
  title: E2E Docs
links:
  - text: Home
    link: /
");
            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), @"---
title: Hello E2E
---
# Hello E2E
This is a test.");

            File.WriteAllText(Path.Combine(_sampleDir, "components.md"), @"
# Components
[!badge text=""BadgeText""]
!!!
AlertContent
!!!
[!command-example install=""test-install"" quickstart=""test-quickstart""]
");
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup if needed
        }

        [Test]
        public async Task TestGeneratedSite_TextPresence()
        {
            // Build the site
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var outputDir = builder.OutputDirectory;
            System.Console.WriteLine($"E2E Output Directory: {outputDir}");

            var indexPath = Path.Combine(outputDir, "index.html");
            var componentsPath = Path.Combine(outputDir, "components.html");

            Assert.That(File.Exists(indexPath), Is.True, "Index.html should exist");
            Assert.That(File.Exists(componentsPath), Is.True, "Components.html should exist");

            // Verify Index
            var indexContent = await File.ReadAllTextAsync(indexPath);
            Assert.That(indexContent, Contains.Substring("<title>E2E Docs - Hello E2E</title>"));
            Assert.That(indexContent, Contains.Substring("Hello E2E"));
            Assert.That(indexContent, Contains.Substring("E2E Docs"));

            // Verify Components
            var componentsContent = await File.ReadAllTextAsync(componentsPath);
            Assert.That(componentsContent, Contains.Substring("BadgeText"));
            Assert.That(componentsContent, Contains.Substring("AlertContent"));
            Assert.That(componentsContent, Contains.Substring("test-install"));
            Assert.That(componentsContent, Contains.Substring("test-quickstart"));

            // Verify Sitemap
            var sitemapPath = Path.Combine(outputDir, "sitemap.xml");
            Assert.That(File.Exists(sitemapPath), Is.True, "sitemap.xml should exist");
            var sitemapContent = await File.ReadAllTextAsync(sitemapPath);
            Assert.That(sitemapContent, Contains.Substring("<loc>https://example.com/</loc>"));
            Assert.That(sitemapContent, Contains.Substring("<loc>https://example.com/components</loc>"));
            Assert.That(sitemapContent, Contains.Substring("<lastmod>"));
        }

        [Test]
        public async Task TestFaviconAutoDetectedFromInputRoot()
        {
            // Drop a favicon.ico at the input root. No `branding.favicon` is set in neko.yml.
            File.WriteAllBytes(Path.Combine(_sampleDir, "favicon.ico"), new byte[] { 0x00, 0x00, 0x01, 0x00 });

            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var outputDir = builder.OutputDirectory;

            // File copied to output root.
            Assert.That(File.Exists(Path.Combine(outputDir, "favicon.ico")), Is.True, "favicon.ico should be copied to output");

            // Link injected into generated HTML.
            var indexContent = await File.ReadAllTextAsync(Path.Combine(outputDir, "index.html"));
            Assert.That(indexContent, Contains.Substring("<link rel=\"icon\" href=\"/favicon.ico\">"));
        }
    }
}
