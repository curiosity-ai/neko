using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 5: orders generated utility rules to match the Tailwind CLI's cascade
    /// exactly. Tailwind's order is intricate (core-plugin registration order, then
    /// per-family theme/value order, then variant and responsive grouping). Rather
    /// than re-derive it, we rank by a captured canonical order
    /// (<c>Resources/tailwind/utility-order.txt</c>) covering Neko's full utility
    /// vocabulary: every rule's (at-context, selector) maps to its position in the
    /// CLI's output, so a subset sorts into the same relative order — byte-for-byte.
    ///
    /// Anything outside the captured vocabulary (e.g. a novel arbitrary value)
    /// falls back to a deterministic structural rank (core-plugin family by
    /// property order, responsive last), so the output stays valid and stable even
    /// if it isn't byte-identical for that rule. Regenerate the order file when the
    /// vocabulary or pinned Tailwind version changes (see the file header).
    /// </summary>
    internal static class RuleSorter
    {
        public static List<CssRule> Sort(List<CssRule> rules)
        {
            var order = CanonicalOrder;
            var keyed = new List<(CssRule Rule, long Key, int Index)>(rules.Count);
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                var ctx = rule.AtRules.Count > 0 ? string.Join(" ", rule.AtRules) : "";
                long key;
                if (order.TryGetValue((ctx, rule.Selector), out var idx))
                    key = idx;                       // exact CLI position
                else
                    key = Fallback + FallbackRank(rule); // unknown → after, structural
                keyed.Add((rule, key, i));
            }
            keyed.Sort((a, b) =>
            {
                int c = a.Key.CompareTo(b.Key);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.Rule.Selector, b.Rule.Selector);
                return c != 0 ? c : a.Index.CompareTo(b.Index);
            });
            var result = new List<CssRule>(rules.Count);
            foreach (var k in keyed) result.Add(k.Rule);
            return result;
        }

        private const long Fallback = 1_000_000;

        // Deterministic structural rank for rules not in the captured order:
        // responsive last (by breakpoint), then by core-plugin/property order.
        private static long FallbackRank(CssRule rule)
        {
            long media = rule.MediaRank;
            long variant = rule.VariantRank;
            long prop = PropertyRank(rule);
            return media * 1_000_000 + variant * 100_000 + prop;
        }

        private static int PropertyRank(CssRule rule)
        {
            string primary = null;
            foreach (var (p, _) in rule.Declarations)
                if (!p.StartsWith("--")) { primary = p; break; }
            if (primary == null && rule.Declarations.Count > 0) primary = rule.Declarations[0].Prop;
            if (primary != null && _propOrder.TryGetValue(primary, out var r)) return r;
            return _propOrder.Count + 100;
        }

        // ---- canonical order resource --------------------------------------

        private static Dictionary<(string, string), int> _canonical;
        private static Dictionary<(string, string), int> CanonicalOrder
        {
            get
            {
                if (_canonical != null) return _canonical;
                var map = new Dictionary<(string, string), int>();
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("Neko.Resources.tailwind.utility-order.txt"))
                {
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        string line;
                        int i = 0;
                        while ((line = reader.ReadLine()) != null)
                        {
                            int tab = line.IndexOf('\t');
                            if (tab < 0) continue;
                            var ctx = line.Substring(0, tab);
                            var sel = line.Substring(tab + 1);
                            map[(ctx, sel)] = i++;
                        }
                    }
                }
                _canonical = map;
                return _canonical;
            }
        }

        private static readonly Dictionary<string, int> _propOrder = BuildOrder(new[]
        {
            "position", "pointer-events", "visibility", "inset", "top", "bottom", "left", "right",
            "isolation", "z-index", "order", "grid-column", "grid-row", "float", "clear",
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "box-sizing", "overflow", "display", "aspect-ratio", "height", "max-height",
            "min-height", "width", "min-width", "max-width", "flex", "flex-shrink", "flex-grow",
            "flex-basis", "table-layout", "border-collapse", "transform-origin", "transform",
            "animation", "cursor", "-webkit-user-select", "resize", "scroll-margin-top",
            "list-style-type", "-moz-columns", "grid-template-columns", "grid-template-rows",
            "flex-direction", "flex-wrap", "place-content", "place-items", "align-content",
            "align-items", "justify-content", "justify-items", "gap", "-moz-column-gap",
            "column-gap", "row-gap", "border-color", "overflow-x", "overflow-y",
            "scroll-behavior", "white-space", "text-wrap", "border-radius", "border-width",
            "border-style", "background-color", "background-image", "background-size",
            "object-fit", "object-position", "padding", "text-align", "vertical-align",
            "font-family", "font-size", "font-weight", "text-transform", "font-style",
            "font-variant-numeric", "line-height", "letter-spacing", "color",
            "text-decoration-line", "opacity", "box-shadow", "outline-style", "outline",
            "filter", "-webkit-backdrop-filter", "backdrop-filter", "transition-property",
            "transition-delay", "transition-duration", "transition-timing-function",
        });

        private static Dictionary<string, int> BuildOrder(string[] props)
        {
            var d = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < props.Length; i++)
                if (!d.ContainsKey(props[i])) d[props[i]] = i;
            return d;
        }
    }
}
