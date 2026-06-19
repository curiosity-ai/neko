using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Neko.Builder.Tailwind;
using Neko.Configuration;

namespace Neko.Tests
{
    /// <summary>
    /// Golden-master parity tests for the pure-C# Tailwind generator. The fixtures
    /// under <c>Fixtures/tailwind/</c> were produced by the official Tailwind v3
    /// standalone CLI over the real <c>.template</c> and <c>Neko.Documentation</c>
    /// sites: <c>tokens_*.txt</c> is the class vocabulary scanned from those sites,
    /// and <c>utilities_*.jsonl</c> is every rule the CLI emitted in
    /// <c>@layer utilities</c> for them.
    ///
    /// The generator must reproduce each CLI utility rule — same selector, same
    /// at-rule context, equivalent declaration block — for the build to match what
    /// the CLI produced. This is the "CLI output == our output" contract.
    /// </summary>
    [TestFixture]
    public class TailwindParityTests
    {
        private static string FixtureDir =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "tailwind");

        private sealed record GoldenRule(string Ctx, string Sel, List<string> Decls);

        private static List<GoldenRule> LoadGolden(string name)
        {
            var rules = new List<GoldenRule>();
            foreach (var line in File.ReadAllLines(Path.Combine(FixtureDir, name)))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                using var doc = JsonDocument.Parse(line);
                var ctx = string.Join(" ", doc.RootElement.GetProperty("ctx").EnumerateArray().Select(e => e.GetString()));
                var sel = doc.RootElement.GetProperty("sel").GetString();
                var decls = doc.RootElement.GetProperty("decls").EnumerateArray().Select(e => e.GetString()).ToList();
                rules.Add(new GoldenRule(ctx, sel!, decls!));
            }
            return rules;
        }

        private static List<string> LoadTokens(string name) =>
            File.ReadAllLines(Path.Combine(FixtureDir, name))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

        private static string NormDecl(string decl)
        {
            // collapse whitespace runs to single space, trim.
            var parts = decl.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts).TrimEnd(';');
        }

        private static List<string> NormSet(IEnumerable<string> decls) =>
            decls.Select(NormDecl).OrderBy(s => s, System.StringComparer.Ordinal).ToList();

        // Builds the generator's rule lookup: (ctx, selector) -> sorted decl set.
        private static Dictionary<(string, string), List<string>> Generate(IReadOnlyList<string> tokens)
        {
            var theme = new TailwindTheme(new NekoConfig());
            var rules = TailwindGenerator.GenerateRules(tokens, theme);
            var map = new Dictionary<(string, string), List<string>>();
            foreach (var r in rules)
            {
                var ctx = r.AtRules.Count > 0 ? string.Join(" ", r.AtRules) : "";
                var decls = NormSet(r.Declarations.Select(d => $"{d.Prop}: {d.Val}"));
                map[(ctx, r.Selector)] = decls;
            }
            return map;
        }

        private void AssertParity(string tokensFile, string goldenFile)
        {
            var tokens = LoadTokens(tokensFile);
            var golden = LoadGolden(goldenFile);
            var generated = Generate(tokens);

            var missing = new List<string>();
            var mismatched = new List<string>();
            int matched = 0;

            foreach (var g in golden)
            {
                if (!generated.TryGetValue((g.Ctx, g.Sel), out var genDecls))
                {
                    missing.Add($"{g.Ctx}|{g.Sel}");
                    continue;
                }
                var want = NormSet(g.Decls);
                if (!want.SequenceEqual(genDecls))
                    mismatched.Add($"{g.Sel}\n   CLI: {string.Join("; ", want)}\n   OUR: {string.Join("; ", genDecls)}");
                else
                    matched++;
            }

            double coverage = golden.Count == 0 ? 1.0 : (double)matched / golden.Count;
            TestContext.WriteLine($"{goldenFile}: {matched}/{golden.Count} rules reproduced ({coverage:P1}); " +
                                  $"{missing.Count} missing, {mismatched.Count} mismatched.");
            if (missing.Count > 0)
                TestContext.WriteLine("MISSING (first 40):\n  " + string.Join("\n  ", missing.Take(40)));
            if (mismatched.Count > 0)
                TestContext.WriteLine("MISMATCHED (first 40):\n  " + string.Join("\n  ", mismatched.Take(40)));

            Assert.That(matched, Is.EqualTo(golden.Count),
                $"Generator did not reproduce every CLI utility rule for {goldenFile}.");
        }

        [Test]
        public void Template_Site_Matches_Cli_Utilities() =>
            AssertParity("tokens_template.txt", "utilities_template.jsonl");

        [Test]
        public void Documentation_Site_Matches_Cli_Utilities() =>
            AssertParity("tokens_docs.txt", "utilities_docs.jsonl");
    }
}
