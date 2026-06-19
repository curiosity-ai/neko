using System;
using System.Collections.Generic;
using System.Text;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 4: turns a resolved utility <see cref="UtilityPart"/> into a final
    /// <see cref="CssRule"/> by applying the candidate's variants — pseudo-classes,
    /// pseudo-elements, <c>dark</c>, <c>group-*</c>/<c>peer-*</c> combinators,
    /// responsive <c>@media</c> wrappers and <c>supports-[…]</c>. Selector forms
    /// match the Tailwind v3 standalone CLI output.
    /// </summary>
    internal sealed class VariantEngine
    {
        private readonly TailwindTheme _t;

        public VariantEngine(TailwindTheme theme) { _t = theme; }

        /// <summary>Returns the rule, or null if a variant is unknown.</summary>
        public CssRule Build(Candidate c, string escapedClass, UtilityPart part)
        {
            // Accumulators.
            var prefixes = new List<string>();      // combinators, prepended (outermost first)
            var atRules = new List<string>();
            var suffixes = new List<(int Prio, string Text)>();
            var extraLeadingDecls = new List<(string, string)>();
            long bp = 0;                             // breakpoint sort weight

            foreach (var variant in c.Variants)
            {
                if (!ApplyVariant(variant, prefixes, atRules, suffixes, extraLeadingDecls, ref bp))
                    return null;
            }

            // Stable sort suffixes by priority (pseudo-class < pseudo-element < dark).
            suffixes.Sort((a, b) => a.Prio.CompareTo(b.Prio));

            var sb = new StringBuilder();
            foreach (var p in prefixes) sb.Append(p);
            sb.Append('.').Append(escapedClass);
            foreach (var s in suffixes) sb.Append(s.Text);
            sb.Append(part.SelectorSuffix);

            var rule = new CssRule(sb.ToString()) { Token = c.Raw };
            rule.AtRules.AddRange(atRules);
            foreach (var d in extraLeadingDecls) rule.Declarations.Add(d);
            foreach (var d in part.Declarations) rule.Declarations.Add(d);

            if (c.Important)
            {
                for (int i = 0; i < rule.Declarations.Count; i++)
                {
                    var (prop, val) = rule.Declarations[i];
                    if (!val.EndsWith("!important")) rule.Declarations[i] = (prop, val + " !important");
                }
            }

            // Responsive rules sort after non-responsive, grouped by breakpoint.
            rule.SortKey = bp;
            return rule;
        }

        private bool ApplyVariant(
            string variant,
            List<string> prefixes,
            List<string> atRules,
            List<(int, string)> suffixes,
            List<(string, string)> extraLeadingDecls,
            ref long bp)
        {
            // Responsive (min-width) and max-* (max-width).
            if (_t.Screens.TryGetValue(variant, out var screen))
            {
                atRules.Add($"@media (min-width: {screen})");
                bp = BreakpointWeight(variant, false);
                return true;
            }
            if (variant.StartsWith("max-") && _t.Screens.TryGetValue(variant.Substring(4), out var maxScreen))
            {
                atRules.Add($"@media not all and (min-width: {maxScreen})");
                bp = BreakpointWeight(variant.Substring(4), true);
                return true;
            }

            // dark (class strategy → :is(.dark *)).
            if (variant == "dark")
            {
                suffixes.Add((PrioDark, ":is(.dark *)"));
                return true;
            }

            // supports-[...]
            if (variant.StartsWith("supports-[") && variant.EndsWith("]"))
            {
                var feature = variant.Substring("supports-[".Length, variant.Length - "supports-[".Length - 1).Replace('_', ' ');
                if (feature == "backdrop-filter")
                    atRules.Add("@supports ((-webkit-backdrop-filter: var(--tw)) or (backdrop-filter: var(--tw)))");
                else if (feature.Contains(":"))
                    atRules.Add($"@supports ({feature})");
                else
                    atRules.Add($"@supports ({feature}: var(--tw))");
                return true;
            }

            // Typography plugin element variants (prose-a:, prose-headings:, …).
            // Generates the same scoped descendant selector the plugin emits, so
            // these utilities work against the shipped static typography layer.
            if (variant.StartsWith("prose-"))
            {
                var elements = ProseElementSelectors(variant.Substring("prose-".Length));
                if (elements == null) return false;
                suffixes.Add((PrioProse, $" :is(:where({elements}):not(:where([class~=\"not-prose\"],[class~=\"not-prose\"] *)))"));
                return true;
            }

            // group-* / peer-* (optionally named: group-hover/name).
            if (variant.StartsWith("group-") || variant.StartsWith("peer-"))
            {
                bool isPeer = variant.StartsWith("peer-");
                var rest = variant.Substring(isPeer ? 5 : 6);
                string name = null;
                int slash = rest.IndexOf('/');
                if (slash >= 0) { name = rest.Substring(slash + 1); rest = rest.Substring(0, slash); }
                var state = PseudoClass(rest);
                if (state == null) return false;
                var baseClass = isPeer ? "peer" : "group";
                var escapedBase = name != null ? $"{baseClass}\\/{name}" : baseClass;
                prefixes.Add(isPeer ? $".{escapedBase}{state} ~ " : $".{escapedBase}{state} ");
                return true;
            }

            // Pseudo-elements.
            var pe = PseudoElement(variant, out bool addsContent);
            if (pe != null)
            {
                if (addsContent) extraLeadingDecls.Add(("content", "var(--tw-content)"));
                suffixes.Add((PrioPseudoElement, pe));
                return true;
            }

            // Pseudo-classes.
            var pc = PseudoClass(variant);
            if (pc != null)
            {
                suffixes.Add((PrioPseudoClass, pc));
                return true;
            }

            return false;
        }

        private const int PrioPseudoClass = 1;
        private const int PrioPseudoElement = 2;
        private const int PrioProse = 4;
        private const int PrioDark = 3;

        // The element groups the @tailwindcss/typography plugin scopes prose-*
        // variants to. Mirrors the plugin's selector lists.
        private static string ProseElementSelectors(string element) => element switch
        {
            "headings" => "h1, h2, h3, h4, h5, h6, th",
            "h1" => "h1",
            "h2" => "h2",
            "h3" => "h3",
            "h4" => "h4",
            "a" => "a",
            "p" => "p",
            "blockquote" => "blockquote",
            "strong" => "strong",
            "em" => "em",
            "code" => "code",
            "pre" => "pre",
            "ol" => "ol",
            "ul" => "ul",
            "li" => "li",
            "img" => "img",
            "hr" => "hr",
            "table" => "table",
            "th" => "thead th",
            "td" => "tbody td, tfoot td",
            _ => null
        };

        private static long BreakpointWeight(string screen, bool max)
        {
            long baseW = screen switch
            {
                "sm" => 1, "md" => 2, "lg" => 3, "xl" => 4, "2xl" => 5, _ => 9
            };
            // max-* media sort after min-* media.
            return (max ? 100 : 0) + baseW;
        }

        private static string PseudoClass(string v) => v switch
        {
            "hover" => ":hover",
            "focus" => ":focus",
            "focus-within" => ":focus-within",
            "focus-visible" => ":focus-visible",
            "active" => ":active",
            "visited" => ":visited",
            "target" => ":target",
            "disabled" => ":disabled",
            "enabled" => ":enabled",
            "checked" => ":checked",
            "required" => ":required",
            "valid" => ":valid",
            "invalid" => ":invalid",
            "default" => ":default",
            "indeterminate" => ":indeterminate",
            "read-only" => ":read-only",
            "empty" => ":empty",
            "first" => ":first-child",
            "last" => ":last-child",
            "only" => ":only-child",
            "odd" => ":nth-child(odd)",
            "even" => ":nth-child(even)",
            "first-of-type" => ":first-of-type",
            "last-of-type" => ":last-of-type",
            _ => null
        };

        private static string PseudoElement(string v, out bool addsContent)
        {
            addsContent = false;
            switch (v)
            {
                case "before": addsContent = true; return "::before";
                case "after": addsContent = true; return "::after";
                case "placeholder": return "::placeholder";
                case "marker": return "::marker";
                case "selection": return "::selection";
                case "first-line": return "::first-line";
                case "first-letter": return "::first-letter";
                case "file": return "::file-selector-button";
                case "backdrop": return "::backdrop";
                default: return null;
            }
        }

        /// <summary>
        /// Escapes a class token for use in a CSS selector, matching Tailwind:
        /// every char outside <c>[A-Za-z0-9_-]</c> is backslash-escaped, and a
        /// leading digit is hex-escaped.
        /// </summary>
        public static string EscapeClassName(string token)
        {
            var sb = new StringBuilder(token.Length + 8);
            for (int i = 0; i < token.Length; i++)
            {
                char ch = token[i];
                bool safe = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_';
                // Leading digit, non-ASCII/control, and comma are hex-escaped
                // (`\NN`), matching CSS.escape; other specials get a backslash.
                if (i == 0 && ch >= '0' && ch <= '9')
                {
                    sb.Append('\\').Append(((int)ch).ToString("x")).Append(' ');
                }
                else if (safe)
                {
                    sb.Append(ch);
                }
                else if (ch == ',' || ch < 0x20 || ch > 0x7e)
                {
                    sb.Append('\\').Append(((int)ch).ToString("x"));
                    // A trailing space terminates the hex escape only when the next
                    // character would otherwise extend it (hex digit or space).
                    char next = i + 1 < token.Length ? token[i + 1] : '\0';
                    if (IsHex(next) || next == ' ') sb.Append(' ');
                }
                else
                {
                    sb.Append('\\').Append(ch);
                }
            }
            return sb.ToString();
        }

        private static bool IsHex(char c) =>
            (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    }
}
