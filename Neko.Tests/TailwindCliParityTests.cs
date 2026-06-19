using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Neko.Builder.Tailwind;
using Neko.Configuration;

namespace Neko.Tests
{
    /// <summary>
    /// Live parity test: runs the real Tailwind v3 standalone CLI over the
    /// documentation project's class vocabulary and asserts the pure-C#
    /// generator produces the same <c>@layer utilities</c> output. This guards
    /// against future drift — if a registry change (or a newer Tailwind release)
    /// diverges from the CLI, this test catches it.
    ///
    /// The CLI is located via the <c>NEKO_TAILWIND_CLI</c> environment variable or
    /// <c>tailwindcss</c> on <c>PATH</c>. When no CLI is available (e.g. CI without
    /// the binary) the test is skipped rather than failed, so the offline
    /// golden-master test in <see cref="TailwindParityTests"/> remains the gate.
    /// </summary>
    [TestFixture]
    public class TailwindCliParityTests
    {
        private static string FixtureDir =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "tailwind");

        [Test]
        public void Cli_And_CSharp_Produce_Same_Utilities_For_Docs_Vocabulary()
        {
            var cli = ResolveCli();
            if (cli == null)
                Assert.Ignore("No Tailwind CLI found (set NEKO_TAILWIND_CLI or put `tailwindcss` on PATH). " +
                              "The offline golden-master test still enforces parity.");

            var tokens = File.ReadAllLines(Path.Combine(FixtureDir, "tokens_docs.txt"))
                             .Where(l => !string.IsNullOrWhiteSpace(l))
                             .ToList();

            // CLI output for the same vocabulary (utilities layer only).
            var cliCss = RunCli(cli!, tokens);
            var cliRules = ParseRules(cliCss);

            // Guard against a vacuous pass: if the CLI produced nothing (broken
            // binary, bad config, network), every assertion below would trivially
            // hold. The docs vocabulary must yield hundreds of utility rules.
            Assert.That(cliRules.Count, Is.GreaterThan(100),
                $"The Tailwind CLI produced only {cliRules.Count} utility rules — it likely failed to run. " +
                $"CLI: {cli}");

            // Our generator's utilities for the same tokens.
            var theme = new TailwindTheme(new NekoConfig());
            var ourCss = TailwindGenerator.GenerateUtilitiesCss(tokens, theme, minify: false);
            var ourRules = ParseRules(ourCss);

            var missing = new List<string>();
            var mismatched = new List<string>();
            int matched = 0;
            foreach (var kv in cliRules)
            {
                if (!ourRules.TryGetValue(kv.Key, out var ours))
                {
                    missing.Add($"{kv.Key.Item1}|{kv.Key.Item2}");
                    continue;
                }
                if (!ours.SequenceEqual(kv.Value))
                    mismatched.Add($"{kv.Key.Item2}\n   CLI: {string.Join("; ", kv.Value)}\n   OUR: {string.Join("; ", ours)}");
                else
                    matched++;
            }

            TestContext.WriteLine($"CLI utility rules: {cliRules.Count}; reproduced {matched}; " +
                                  $"missing {missing.Count}; mismatched {mismatched.Count}.");
            if (missing.Count > 0) TestContext.WriteLine("MISSING:\n  " + string.Join("\n  ", missing.Take(40)));
            if (mismatched.Count > 0) TestContext.WriteLine("MISMATCHED:\n  " + string.Join("\n  ", mismatched.Take(40)));

            Assert.That(matched, Is.EqualTo(cliRules.Count),
                "The C# generator's utilities diverged from the live Tailwind CLI output.");
        }

        // --- CLI invocation ------------------------------------------------

        private static string ResolveCli()
        {
            var env = Environment.GetEnvironmentVariable("NEKO_TAILWIND_CLI");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            var exe = OperatingSystem.IsWindows() ? "tailwindcss.exe" : "tailwindcss";
            var path = Environment.GetEnvironmentVariable("PATH");
            if (path == null) return null;
            foreach (var dir in path.Split(Path.PathSeparator))
            {
                try
                {
                    var candidate = Path.Combine(dir.Trim(), exe);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* malformed PATH entry */ }
            }
            return null;
        }

        private static string RunCli(string cli, IEnumerable<string> tokens)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "neko-tw-cli-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                // One HTML file containing every token as a class — the CLI's
                // scanner harvests them exactly like a real site.
                var html = "<div class=\"" + string.Join(" ", tokens).Replace("\"", "&quot;") + "\"></div>";
                File.WriteAllText(Path.Combine(tempDir, "content.html"), html);

                var (primary, accent) = ThemeDefinitions.ResolvePalettes(new NekoConfig());
                string Json(Dictionary<string, string> d) =>
                    "{" + string.Join(",", d.Select(k => $"\"{k.Key}\":\"{k.Value}\"")) + "}";

                var config =
                    "module.exports = {\n" +
                    $"  content: ['{tempDir.Replace("\\", "/")}/content.html'],\n" +
                    "  darkMode: 'class',\n" +
                    $"  theme: {{ extend: {{ colors: {{ primary: {Json(primary)}, accent: {Json(accent)} }} }} }},\n" +
                    "  plugins: [require('@tailwindcss/typography')],\n" +
                    "};\n";
                File.WriteAllText(Path.Combine(tempDir, "tailwind.config.js"), config);
                // Utilities layer only — base/components are shipped verbatim.
                File.WriteAllText(Path.Combine(tempDir, "input.css"), "@tailwind utilities;\n");

                var psi = new ProcessStartInfo
                {
                    FileName = cli,
                    WorkingDirectory = tempDir,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("-c"); psi.ArgumentList.Add(Path.Combine(tempDir, "tailwind.config.js"));
                psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(Path.Combine(tempDir, "input.css"));
                psi.ArgumentList.Add("-o"); psi.ArgumentList.Add(Path.Combine(tempDir, "out.css"));

                using var proc = Process.Start(psi);
                proc!.StandardError.ReadToEnd();
                proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return File.Exists(Path.Combine(tempDir, "out.css"))
                    ? File.ReadAllText(Path.Combine(tempDir, "out.css"))
                    : string.Empty;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
            }
        }

        // --- minimal CSS parser (paren-aware, one level of @media/@supports) ---

        private static Dictionary<(string, string), List<string>> ParseRules(string css)
        {
            var map = new Dictionary<(string, string), List<string>>();
            css = StripComments(css);
            ParseBlock(css, "", map);
            return map;
        }

        private static void ParseBlock(string s, string ctx, Dictionary<(string, string), List<string>> map)
        {
            int i = 0, n = s.Length;
            while (i < n)
            {
                int brace = s.IndexOf('{', i);
                if (brace < 0) break;
                var header = s.Substring(i, brace - i).Trim();
                int depth = 1, k = brace + 1;
                while (k < n && depth > 0)
                {
                    if (s[k] == '{') depth++;
                    else if (s[k] == '}') depth--;
                    k++;
                }
                var body = s.Substring(brace + 1, k - brace - 2);
                if (header.StartsWith("@media") || header.StartsWith("@supports"))
                {
                    ParseBlock(body, ctx.Length == 0 ? header : ctx + " " + header, map);
                }
                else if (!header.StartsWith("@"))
                {
                    var decls = body.Split(';')
                        .Select(d => NormDecl(d))
                        .Where(d => d.Length > 0)
                        .OrderBy(d => d, StringComparer.Ordinal)
                        .ToList();
                    foreach (var sel in SplitSelectors(header))
                        map[(ctx, sel.Trim())] = decls;
                }
                i = k;
            }
        }

        private static string NormDecl(string decl)
        {
            var parts = decl.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts).TrimEnd(';');
        }

        private static IEnumerable<string> SplitSelectors(string header)
        {
            var parts = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < header.Length; i++)
            {
                char c = header[i];
                if (c == '(' || c == '[') depth++;
                else if (c == ')' || c == ']') depth = Math.Max(0, depth - 1);
                else if (c == ',' && depth == 0) { parts.Add(header.Substring(start, i - start)); start = i + 1; }
            }
            parts.Add(header.Substring(start));
            return parts;
        }

        private static string StripComments(string css)
        {
            var sb = new StringBuilder(css.Length);
            for (int i = 0; i < css.Length; i++)
            {
                if (css[i] == '/' && i + 1 < css.Length && css[i + 1] == '*')
                {
                    int end = css.IndexOf("*/", i + 2);
                    if (end < 0) break;
                    i = end + 1;
                }
                else sb.Append(css[i]);
            }
            return sb.ToString();
        }
    }
}
