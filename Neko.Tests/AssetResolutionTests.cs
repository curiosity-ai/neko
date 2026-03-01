using NUnit.Framework;
using Neko.Builder;
using System.IO;

namespace Neko.Tests
{
    public class AssetResolutionTests
    {
        private MarkdownParser _parser;
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
            _tempDir = Path.Combine(Path.GetTempPath(), "NekoTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void TestResolveAssetInSameDirectory()
        {
            var filePath = Path.Combine(_tempDir, "page.md");
            var assetPath = Path.Combine(_tempDir, "image.png");
            File.WriteAllText(filePath, "test");
            File.WriteAllText(assetPath, "image data");

            var markdown = "![](image.png)";
            var doc = _parser.Parse(markdown, filePath, _tempDir);

            Assert.That(doc.Html, Contains.Substring("src=\"image.png\""));
        }

        [Test]
        public void TestResolveAssetInAssetsDirectory()
        {
            var filePath = Path.Combine(_tempDir, "page.md");
            var assetsDir = Path.Combine(_tempDir, "assets");
            Directory.CreateDirectory(assetsDir);
            var assetPath = Path.Combine(assetsDir, "image.png");
            File.WriteAllText(filePath, "test");
            File.WriteAllText(assetPath, "image data");

            var markdown = "![](image.png)";
            var doc = _parser.Parse(markdown, filePath, _tempDir);

            // Should resolve to /assets/image.png
            // Path.GetRelativePath uses platform separator. Replace to / for HTML check
            Assert.That(doc.Html.Replace("\\", "/"), Contains.Substring("src=\"/assets/image.png\""));
        }

        [Test]
        public void TestResolveAssetInParentAssetsDirectory()
        {
            // Structure:
            // /root
            //   /assets/image.png
            //   /sub
            //     /page.md

            var assetsDir = Path.Combine(_tempDir, "assets");
            Directory.CreateDirectory(assetsDir);
            File.WriteAllText(Path.Combine(assetsDir, "image.png"), "data");

            var subDir = Path.Combine(_tempDir, "sub");
            Directory.CreateDirectory(subDir);
            var filePath = Path.Combine(subDir, "page.md");
            File.WriteAllText(filePath, "test");

            var markdown = "![](image.png)";
            var doc = _parser.Parse(markdown, filePath, _tempDir);

            // Should resolve to /assets/image.png
            Assert.That(doc.Html.Replace("\\", "/"), Contains.Substring("src=\"/assets/image.png\""));
        }
    }
}
