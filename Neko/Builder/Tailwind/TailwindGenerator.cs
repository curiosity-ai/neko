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

        // Serializes rules. The non-minified form matches the Tailwind CLI's
        // non-minified output byte-for-byte: rules separated by a blank line, the
        // last declaration with no trailing semicolon, responsive rules grouped
        // into indented @media blocks. The minified form is compact.
        private static string Serialize(List<CssRule> rules, bool minify)
        {
            // Partition into top-level rules (in order) and @media groups
            // (preserving order; consecutive same-at rules form one block).
            var blocks = new List<string>();
            var emittedKeyframes = new HashSet<string>(StringComparer.Ordinal);
            int i = 0;
            while (i < rules.Count)
            {
                var at = rules[i].AtRules.Count > 0 ? string.Join(" ", rules[i].AtRules) : null;
                if (at == null)
                {
                    // Tailwind emits @keyframes immediately before the first
                    // animation utility that references them.
                    var kf = KeyframesFor(rules[i]);
                    if (kf != null && emittedKeyframes.Add(kf))
                        blocks.Add(Keyframes(kf, minify));
                    blocks.Add(RuleText(rules[i], minify, indent: false));
                    i++;
                }
                else
                {
                    int j = i;
                    var inner = new List<string>();
                    while (j < rules.Count && rules[j].AtRules.Count > 0 && string.Join(" ", rules[j].AtRules) == at)
                    {
                        inner.Add(RuleText(rules[j], minify, indent: !minify));
                        j++;
                    }
                    if (minify)
                        blocks.Add(at + "{" + string.Join("", inner) + "}");
                    else
                        blocks.Add(at + " {\n" + string.Join("\n\n", inner) + "\n}");
                    i = j;
                }
            }

            if (minify) return string.Concat(blocks);
            return blocks.Count == 0 ? "" : string.Join("\n\n", blocks) + "\n";
        }

        private static string RuleText(CssRule rule, bool minify, bool indent)
        {
            var sb = new StringBuilder();
            if (minify)
            {
                sb.Append(rule.Selector).Append('{');
                for (int k = 0; k < rule.Declarations.Count; k++)
                {
                    var (prop, val) = rule.Declarations[k];
                    sb.Append(prop).Append(':').Append(val);
                    if (k < rule.Declarations.Count - 1) sb.Append(';');
                }
                sb.Append('}');
            }
            else
            {
                string pad = indent ? "  " : "";
                var indents = AlignIndents(rule.Declarations);
                sb.Append(pad).Append(rule.Selector).Append(" {\n");
                for (int k = 0; k < rule.Declarations.Count; k++)
                {
                    var (prop, val) = rule.Declarations[k];
                    sb.Append(pad).Append(' ', indents[k]).Append(prop).Append(": ").Append(val);
                    // The CLI omits the trailing semicolon on the final declaration.
                    sb.Append(k < rule.Declarations.Count - 1 ? ";\n" : "\n");
                }
                sb.Append(pad).Append("}");
            }
            return sb.ToString();
        }

        // Reproduces autoprefixer's "visual cascade": a run of consecutive
        // declarations that are vendor-prefixed forms of the same base property
        // gets its property names right-aligned (padded with leading spaces) so the
        // values line up. Each entry is the leading-space count (>= 2).
        private static int[] AlignIndents(List<(string Prop, string Val)> decls)
        {
            var indents = new int[decls.Count];
            for (int i = 0; i < decls.Count; i++) indents[i] = 2;
            int g = 0;
            while (g < decls.Count)
            {
                var baseProp = BaseProperty(decls[g].Prop);
                int h = g + 1;
                while (h < decls.Count && BaseProperty(decls[h].Prop) == baseProp)
                    h++;
                // [g, h) share a base property. autoprefixer only right-aligns the
                // groups IT generates (it adds the prefixes); properties Tailwind
                // emits already-prefixed (e.g. backdrop-filter) are left untouched.
                if (h - g > 1 && _aligned.Contains(baseProp))
                {
                    int max = 0;
                    for (int k = g; k < h; k++) if (decls[k].Prop.Length > max) max = decls[k].Prop.Length;
                    // Only align when the names actually differ in length (true
                    // vendor-prefix groups), not e.g. repeated identical props.
                    bool differ = false;
                    for (int k = g; k < h; k++) if (decls[k].Prop.Length != max) { differ = true; break; }
                    if (differ)
                        for (int k = g; k < h; k++) indents[k] = 2 + (max - decls[k].Prop.Length);
                }
                g = h;
            }
            return indents;
        }

        // Base properties whose vendor prefixes autoprefixer adds (and aligns).
        private static readonly HashSet<string> _aligned = new(StringComparer.Ordinal)
        {
            "user-select", "column-gap", "columns", "object-fit", "object-position",
            "appearance", "backface-visibility", "hyphens", "tab-size",
        };

        private static string BaseProperty(string prop)
        {
            foreach (var p in new[] { "-webkit-", "-moz-", "-o-", "-ms-" })
                if (prop.StartsWith(p, StringComparison.Ordinal)) return prop.Substring(p.Length);
            return prop;
        }

        private static string KeyframesFor(CssRule rule)
        {
            foreach (var (prop, val) in rule.Declarations)
            {
                if (prop != "animation") continue;
                var name = val.Split(' ')[0];
                if (_keyframes.ContainsKey(name)) return name;
            }
            return null;
        }

        private static string Keyframes(string name, bool minify)
        {
            var frames = _keyframes[name];
            if (minify) return "@keyframes " + name + "{" + frames.Minified + "}";
            return "@keyframes " + name + " {\n" + frames.Pretty + "\n}";
        }

        // The keyframes Tailwind emits for its default animations. Pretty form is
        // indented to match the CLI's non-minified output byte-for-byte.
        private static readonly Dictionary<string, (string Pretty, string Minified)> _keyframes = new(StringComparer.Ordinal)
        {
            ["spin"] = (
                "  to {\n    transform: rotate(360deg)\n  }",
                "to{transform:rotate(360deg)}"),
            ["ping"] = (
                "  75%, 100% {\n    transform: scale(2);\n    opacity: 0\n  }",
                "75%,100%{transform:scale(2);opacity:0}"),
            ["pulse"] = (
                "  50% {\n    opacity: .5\n  }",
                "50%{opacity:.5}"),
            ["bounce"] = (
                "  0%, 100% {\n    transform: translateY(-25%);\n    animation-timing-function: cubic-bezier(0.8, 0, 1, 1)\n  }\n\n  50% {\n    transform: none;\n    animation-timing-function: cubic-bezier(0, 0, 0.2, 1)\n  }",
                "0%,100%{transform:translateY(-25%);animation-timing-function:cubic-bezier(0.8,0,1,1)}50%{transform:none;animation-timing-function:cubic-bezier(0,0,0.2,1)}"),
        };

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
