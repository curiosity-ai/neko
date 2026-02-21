using System.Threading.Tasks;
using NUnit.Framework;
using TailDocs.CLI.Builder;
using TailDocs.CLI.Configuration;
using System.IO;

namespace TailDocs.Tests
{
    public class E2ETests
    {
        private string _outputDir;
        private string _sampleDir;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "E2ESample");
            _outputDir = Path.Combine(_sampleDir, ".taildocs");

            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            // Create sample config and files
            File.WriteAllText(Path.Combine(_sampleDir, "taildocs.yml"), @"
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

            var indexPath = Path.Combine(_outputDir, "index.html");
            var componentsPath = Path.Combine(_outputDir, "components.html");

            Assert.That(File.Exists(indexPath), Is.True, "Index.html should exist");
            Assert.That(File.Exists(componentsPath), Is.True, "Components.html should exist");

            // Verify Index
            var indexContent = await File.ReadAllTextAsync(indexPath);
            Assert.That(indexContent, Contains.Substring("<title>Hello E2E - E2E Docs</title>"));
            Assert.That(indexContent, Contains.Substring("Hello E2E"));
            Assert.That(indexContent, Contains.Substring("E2E Docs"));

            // Verify Components
            var componentsContent = await File.ReadAllTextAsync(componentsPath);
            Assert.That(componentsContent, Contains.Substring("BadgeText"));
            Assert.That(componentsContent, Contains.Substring("AlertContent"));
        }
    }
}
