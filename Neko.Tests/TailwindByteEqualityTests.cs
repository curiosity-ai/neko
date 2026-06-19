using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neko.Builder.Tailwind;
using Neko.Configuration;

namespace Neko.Tests
{
    /// <summary>
    /// Proves the pure-C# generator's <c>@layer utilities</c> output is
    /// byte-for-byte identical to the Tailwind v3 CLI's non-minified output for
    /// the same class set.
    ///
    /// The golden file <c>golden_utilities_nonmin_docs.css</c> is the CLI's
    /// <c>tailwindcss -i '@tailwind utilities'</c> output (no <c>--minify</c>) over
    /// the documentation vocabulary in <c>tokens_docs.txt</c>. Feeding the same
    /// tokens to our generator must reproduce it exactly — same order, same
    /// formatting (blank lines, no trailing semicolons, autoprefixer alignment),
    /// same <c>@keyframes</c>, same declarations.
    ///
    /// Note: on a real site the CLI additionally emits a handful of rules from
    /// scanning non-class text (prose words like "ordinal", JS regex literals,
    /// escaped selectors in inlined &lt;style&gt;). Those are unused false
    /// positives with no rendering effect; see <see cref="TailwindParityTests"/>.
    /// Comparing a pure class list (as here) isolates the generator from that
    /// extraction noise.
    /// </summary>
    [TestFixture]
    public class TailwindByteEqualityTests
    {
        private static string FixtureDir =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "tailwind");

        [Test]
        public void Utilities_Are_Byte_Identical_To_Cli_NonMinified()
        {
            var tokens = File.ReadAllLines(Path.Combine(FixtureDir, "tokens_docs.txt"))
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .ToList();

            // Reproduce the golden's content path: the CLI scanned an HTML file
            // with every token as a class, so we scan the same shape through our
            // extractor (not the raw list) for an apples-to-apples comparison.
            var html = "<div class=\"" + string.Join(" ", tokens.Select(t => t.Replace("\"", "&quot;"))) + "\"></div>";
            var harvested = ClassExtractor.Extract(new[] { html });

            var theme = new TailwindTheme(new NekoConfig());
            var ours = TailwindGenerator.GenerateUtilitiesCss(harvested, theme, minify: false);
            var golden = File.ReadAllText(Path.Combine(FixtureDir, "golden_utilities_nonmin_docs.css"));

            // Normalise only line endings (git may rewrite them); everything else
            // must match exactly.
            ours = ours.Replace("\r\n", "\n");
            golden = golden.Replace("\r\n", "\n");

            if (ours != golden)
            {
                var ol = ours.Split('\n');
                var gl = golden.Split('\n');
                int firstDiff = -1;
                for (int i = 0; i < Math.Min(ol.Length, gl.Length); i++)
                    if (ol[i] != gl[i]) { firstDiff = i; break; }
                var msg = $"Generated utilities are not byte-identical to the CLI golden " +
                          $"(ours {ours.Length} bytes / {ol.Length} lines, golden {golden.Length} bytes / {gl.Length} lines).";
                if (firstDiff >= 0)
                {
                    msg += $"\nFirst difference at line {firstDiff + 1}:";
                    for (int i = Math.Max(0, firstDiff - 2); i <= firstDiff + 2 && i < Math.Max(ol.Length, gl.Length); i++)
                    {
                        msg += $"\n  [{i + 1}] OUR: {(i < ol.Length ? ol[i] : "<eof>")}";
                        msg += $"\n  [{i + 1}] CLI: {(i < gl.Length ? gl[i] : "<eof>")}";
                    }
                }
                Assert.Fail(msg);
            }
        }
    }
}
