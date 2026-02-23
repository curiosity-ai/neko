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
        }
    }
}
