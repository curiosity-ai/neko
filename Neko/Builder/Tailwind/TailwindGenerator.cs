using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Neko.Configuration;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Pure-C# replacement for the Tailwind standalone CLI. Scans the generated
    /// content, generates only the <c>@layer utilities</c> rules it finds, and
    /// concatenates them with the shipped <c>base</c> (preflight) and
    /// <c>components</c> (typography) layers captured from the official CLI.
    ///
    /// Produces the same <c>assets/tailwind.css</c> the CLI path produced, with no
    /// Node/npm/binary download — honouring Neko's embedded-resources rule.
    /// </summary>
    public static class TailwindGenerator
    {
        /// <summary>
        /// Generates the full stylesheet for a site: preflight + typography +
        /// generated utilities. <paramref name="contents"/> are the raw file
        /// contents to scan (HTML/JS).
        /// </summary>
        public static string Generate(IEnumerable<string> contents, NekoConfig config, bool minify = true)
        {
            var theme = new TailwindTheme(config);
            var tokens = ClassExtractor.Extract(contents);
            var utilities = GenerateUtilitiesCss(tokens, theme, minify);

            var sb = new StringBuilder();
            sb.Append(LoadLayer("preflight.css", minify));
            if (sb.Length > 0 && !minify) sb.AppendLine();
            sb.Append(LoadLayer("typography.css", minify));
            if (!minify) sb.AppendLine();
            sb.Append(utilities);
            return sb.ToString();
        }

        /// <summary>Builds, dedupes and sorts the utility rules for a token set.</summary>
        internal static List<CssRule> GenerateRules(IEnumerable<string> tokens, TailwindTheme theme)
        {
            var registry = new UtilityRegistry(theme);
            var variants = new VariantEngine(theme);
            var rules = new List<CssRule>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var token in tokens)
            {
                var candidate = CandidateParser.Parse(token);
                if (candidate == null) continue;

                UtilityResult result;
                try { result = registry.Resolve(candidate); }
                catch { continue; }
                if (result == null || result.Parts.Count == 0) continue;

                var escaped = VariantEngine.EscapeClassName(token);
                foreach (var part in result.Parts)
                {
                    if (part.Declarations.Count == 0) continue;
                    var rule = variants.Build(candidate, escaped, part);
                    if (rule == null) continue;

                    var key = DedupeKey(rule);
                    if (seen.Add(key)) rules.Add(rule);
                }
            }

            return RuleSorter.Sort(rules);
        }

        internal static string GenerateUtilitiesCss(IEnumerable<string> tokens, TailwindTheme theme, bool minify)
        {
            var rules = GenerateRules(tokens, theme);
            return Serialize(rules, minify);
        }

        private static string DedupeKey(CssRule rule)
        {
            var sb = new StringBuilder();
            foreach (var at in rule.AtRules) sb.Append(at).Append('{');
            sb.Append(rule.Selector).Append('{');
            foreach (var (prop, val) in rule.Declarations) sb.Append(prop).Append(':').Append(val).Append(';');
            return sb.ToString();
        }

        // Serializes rules, grouping consecutive rules that share the same
        // at-rule wrapper into a single block (Tailwind groups responsive rules).
        private static string Serialize(List<CssRule> rules, bool minify)
        {
            var sb = new StringBuilder();
            string currentAt = null;

            foreach (var rule in rules)
            {
                var at = rule.AtRules.Count > 0 ? string.Join(" ", rule.AtRules) : null;
                if (at != currentAt)
                {
                    if (currentAt != null) sb.Append(minify ? "}" : "}\n");
                    if (at != null) sb.Append(at).Append(minify ? "{" : " {\n");
                    currentAt = at;
                }
                WriteRule(sb, rule, minify, indent: currentAt != null && !minify);
            }
            if (currentAt != null) sb.Append(minify ? "}" : "}\n");
            return sb.ToString();
        }

        private static void WriteRule(StringBuilder sb, CssRule rule, bool minify, bool indent)
        {
            string pad = indent ? "  " : "";
            if (minify)
            {
                sb.Append(rule.Selector).Append('{');
                for (int i = 0; i < rule.Declarations.Count; i++)
                {
                    var (prop, val) = rule.Declarations[i];
                    sb.Append(prop).Append(':').Append(val);
                    if (i < rule.Declarations.Count - 1) sb.Append(';');
                }
                sb.Append('}');
            }
            else
            {
                sb.Append(pad).Append(rule.Selector).Append(" {\n");
                foreach (var (prop, val) in rule.Declarations)
                    sb.Append(pad).Append("  ").Append(prop).Append(": ").Append(val).Append(";\n");
                sb.Append(pad).Append("}\n");
            }
        }

        private static string LoadLayer(string name, bool minify)
        {
            var asm = Assembly.GetExecutingAssembly();
            var resource = "Neko.Resources.tailwind." + name;
            using var stream = asm.GetManifestResourceStream(resource);
            if (stream == null) return string.Empty;
            using var reader = new StreamReader(stream);
            var css = reader.ReadToEnd();
            return minify ? CssMinifier.Minify(css) : css;
        }
    }
}
