using NUnit.Framework;
using Neko.Builder;
using System.IO;
using System.Linq;
using System;

namespace Neko.Tests
{
    public class FileScannerTests
    {
        private string _tempDir;
        private string _outputDir;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _outputDir = Path.Combine(_tempDir, ".neko");
            Directory.CreateDirectory(_outputDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Test]
        public void TestScanFiles()
        {
            File.WriteAllText(Path.Combine(_tempDir, "index.md"), "# Index");
            File.WriteAllText(Path.Combine(_tempDir, "about.md"), "# About");
            File.WriteAllText(Path.Combine(_tempDir, "other.txt"), "Not markdown");

            var scanner = new FileScanner(_tempDir, _outputDir);
            var files = scanner.Scan().ToList();

            Assert.That(files.Count, Is.EqualTo(2));
            Assert.That(files.Any(f => f.EndsWith("index.md")), Is.True);
            Assert.That(files.Any(f => f.EndsWith("about.md")), Is.True);
        }

        [Test]
        public void TestScanExcludesOutputDirectory()
        {
            File.WriteAllText(Path.Combine(_tempDir, "index.md"), "# Index");
            File.WriteAllText(Path.Combine(_outputDir, "generated.md"), "# Generated");

            var scanner = new FileScanner(_tempDir, _outputDir);
            var files = scanner.Scan().ToList();

            Assert.That(files.Count, Is.EqualTo(1));
            Assert.That(files.Any(f => f.EndsWith("index.md")), Is.True);
            Assert.That(files.Any(f => f.EndsWith("generated.md")), Is.False);
        }

        [Test]
        public void TestScanExcludesHiddenDirectories()
        {
            var hiddenDir = Path.Combine(_tempDir, ".hidden");
            Directory.CreateDirectory(hiddenDir);
            File.WriteAllText(Path.Combine(hiddenDir, "hidden.md"), "# Hidden");
            File.WriteAllText(Path.Combine(_tempDir, "visible.md"), "# Visible");

            var scanner = new FileScanner(_tempDir, _outputDir);
            var files = scanner.Scan().ToList();

            Assert.That(files.Count, Is.EqualTo(1));
            Assert.That(files.Any(f => f.EndsWith("visible.md")), Is.True);
        }
    }
}
