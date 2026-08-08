using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using Neko.Extensions;
using NUnit.Framework;

namespace Neko.Tests
{
    // Covers `neko watch --no-tesserae` and `--no-snapframe`: the two switches that
    // take the external toolchains out of a watch session.
    public class ToolchainToggleTests
    {
        [TearDown]
        public void TearDown()
        {
            TesseraeCompiler.Disabled = false;
            SnapFrameExtension.Disabled = false;
        }

        [Test]
        public async Task NoTesserae_SkipsCompilationAndFallsBackToACodeBlock()
        {
            var dir = Path.Combine(TestContext.CurrentContext.TestDirectory, "NoTesseraeSample");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            File.WriteAllText(Path.Combine(dir, "neko.yml"), "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(dir, "index.md"), @"# Sample

```tesserae sample.js
UI.Button(""Click me"").Render();
```
");

            TesseraeCompiler.Disabled = true;

            var output = Path.Combine(TestContext.CurrentContext.TestDirectory, "NoTesseraeOut");
            if (Directory.Exists(output)) Directory.Delete(output, true);

            var builder = new SiteBuilder(dir, output);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(output, "index.html"));

            // No compile means no live preview: the sample renders as plain C#.
            Assert.That(html, Does.Not.Contain("Live Preview"));
            Assert.That(html, Does.Contain("language-csharp"));
            Assert.That(html, Does.Contain("Click me"), "The sample source is still shown.");
        }

        [Test]
        public async Task NoTesserae_CompileReturnsNullWithoutTouchingDisk()
        {
            TesseraeCompiler.Disabled = true;

            var siteRoot = Path.Combine(TestContext.CurrentContext.TestDirectory, "NoTesseraeCompileRoot");
            if (Directory.Exists(siteRoot)) Directory.Delete(siteRoot, true);

            var result = await TesseraeCompiler.CompileAsync("sample.js", "UI.Button(\"x\").Render();", siteRoot);

            Assert.That(result, Is.Null);
            Assert.That(Directory.Exists(siteRoot), Is.False,
                "A disabled compile must not create the site's Tesserae asset folder.");
        }

        [Test]
        public void NoSnapframe_ReportsTheToolAsUnavailable()
        {
            SnapFrameExtension.Disabled = true;

            // EnsureToolInstalled is the single gate every snapframe caller goes
            // through — including Tesserae height measurement — so a false here means
            // no browser is launched and nothing is installed.
            Assert.That(SnapFrameExtension.EnsureToolInstalled(), Is.False);
        }

        [Test]
        public void NoSnapframe_CaptureIsANoOp()
        {
            SnapFrameExtension.Disabled = true;

            var target = Path.Combine(TestContext.CurrentContext.TestDirectory, "NoSnapframeShot.png");
            if (File.Exists(target)) File.Delete(target);

            SnapFrameExtension.CaptureScreenshot("https://example.com", "", null, target);

            Assert.That(File.Exists(target), Is.False);
        }
    }
}
