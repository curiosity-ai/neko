using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    // The live preview's script/link order is not maintained in Neko: it is read out
    // of the index.html the Transpose compiler generates for the shared runtime build,
    // so a new runtime file — or a change in which file has to load first — needs no
    // change here. These tests pin the reading of that page.
    public class TesseraeRuntimeLayoutTests
    {
        // The shape Transpose's OutputBuilder emits for a Tesserae app.
        private const string GeneratedIndexHtml = @"<!doctype html>
<html lang=en>
<head>
    <meta charset=""utf-8"" />
    <title>App</title>
    <link rel=""stylesheet"" href=""assets/css/tss.css"">
    <script src=""tps.js"" defer></script>
    <script src=""tps.shim.js"" defer></script>
    <script src=""Transpose.Core.js"" defer></script>
    <script src=""tss.js"" defer></script>
    <script src=""assets/js/tss-dep.js"" defer></script>
    <script src=""app.js"" defer></script>
    <script src=""app.meta.js"" defer></script>
</head>
<body>
</body>
</html>";

        [Test]
        public void KeepsTheCompilersOwnScriptOrder()
        {
            var layout = TesseraeCompiler.ReadLayout(GeneratedIndexHtml);

            Assert.That(layout.Js, Is.EqualTo(new[]
            {
                "tps.js",
                "tps.shim.js",
                "Transpose.Core.js",
                "tss.js",
                "assets/js/tss-dep.js",
            }));
        }

        [Test]
        public void ReadsStylesheetsInOrder()
        {
            var layout = TesseraeCompiler.ReadLayout(GeneratedIndexHtml);
            Assert.That(layout.Css, Is.EqualTo(new[] { "assets/css/tss.css" }));
        }

        // The scaffold's own bundle is per-sample: the sample's JavaScript is inlined
        // into the preview document instead. Loading the scaffold's copy alongside it
        // would define the same `App` class twice.
        [Test]
        public void DropsTheScaffoldsOwnBundle()
        {
            var layout = TesseraeCompiler.ReadLayout(GeneratedIndexHtml);

            Assert.That(layout.Js, Does.Not.Contain("app.js"));
            Assert.That(layout.Js, Does.Not.Contain("app.meta.js"));
        }

        // A Release build links the pre-minified variants; they are still runtime
        // files, and the scaffold's minified bundle must still be dropped.
        [Test]
        public void HandlesMinifiedVariants()
        {
            var layout = TesseraeCompiler.ReadLayout(@"<html><head>
<script src=""tps.min.js""></script>
<script src=""app.min.js""></script>
</head></html>");

            Assert.That(layout.Js, Is.EqualTo(new[] { "tps.min.js" }));
        }

        [Test]
        public void HandlesAPageWithNoAssets()
        {
            var layout = TesseraeCompiler.ReadLayout("<html><head></head><body></body></html>");

            Assert.That(layout.Js, Is.Empty);
            Assert.That(layout.Css, Is.Empty);
        }
    }
}
