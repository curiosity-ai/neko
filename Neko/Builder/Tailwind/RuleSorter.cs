using System;
using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 5: assigns each rule a deterministic, Tailwind-matching cascade
    /// position and sorts. The order is lexicographic over
    /// (media → variant → utility/property → value → selector):
    ///
    /// - <b>media</b>: non-responsive rules first, then each <c>@media</c> block
    ///   grouped <c>sm → 2xl</c> (then <c>max-*</c>), as Tailwind groups
    ///   responsive variants at the end of the layer.
    /// - <b>variant</b>: unvariant utilities before variant ones.
    /// - <b>utility</b>: the property order Tailwind's core plugins register in
    ///   (so <c>p-4</c> precedes <c>px-2</c> precedes <c>pl-1</c> and the longhand
    ///   wins, matching the CLI cascade).
    /// - <b>value/selector</b>: stable tie-breakers for full determinism.
    ///
    /// Determinism matters: rule generation iterates a hash set whose order is
    /// randomised per process, so a total sort key is the only thing keeping the
    /// emitted CSS — and thus same-specificity conflict resolution — stable across
    /// builds.
    /// </summary>
    internal static class RuleSorter
    {
        public static List<CssRule> Sort(List<CssRule> rules)
        {
            foreach (var r in rules)
                if (r.UtilityRank == 0) r.UtilityRank = RankFor(r);

            rules.Sort((a, b) =>
            {
                int c = a.MediaRank.CompareTo(b.MediaRank);
                if (c != 0) return c;
                c = a.VariantRank.CompareTo(b.VariantRank);
                if (c != 0) return c;
                c = a.UtilityRank.CompareTo(b.UtilityRank);
                if (c != 0) return c;
                c = a.ValueRank.CompareTo(b.ValueRank);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Selector, b.Selector);
            });
            return rules;
        }

        // The utility/property order the Tailwind v3 core plugins register in,
        // captured from the standalone CLI's output. A rule is ranked by its
        // "primary" declaration (first non custom-property declaration, else the
        // first), which maps cleanly onto this sequence.
        private static int RankFor(CssRule rule)
        {
            string primary = null;
            foreach (var (prop, _) in rule.Declarations)
            {
                if (!prop.StartsWith("--")) { primary = prop; break; }
            }
            if (primary == null && rule.Declarations.Count > 0)
                primary = rule.Declarations[0].Prop;
            if (primary != null && _propOrder.TryGetValue(primary, out var rank))
                return rank;
            return _propOrder.Count + 100; // unknown props sort last
        }

        private static readonly Dictionary<string, int> _propOrder = BuildOrder(new[]
        {
            "position", "pointer-events", "visibility", "inset", "top", "bottom", "left", "right",
            "isolation", "z-index", "order", "grid-column", "grid-row", "float", "clear",
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "box-sizing", "line-height-clamp", "overflow", "display", "aspect-ratio",
            "height", "max-height", "min-height", "width", "min-width", "max-width",
            "flex", "flex-shrink", "flex-grow", "flex-basis", "table-layout", "border-collapse",
            "transform-origin", "transform", "animation", "cursor", "touch-action",
            "-webkit-user-select", "resize", "scroll-margin-top", "scroll-margin-bottom",
            "list-style-position", "list-style-type", "-moz-columns", "grid-auto-flow",
            "grid-template-columns", "grid-template-rows", "flex-direction", "flex-wrap",
            "place-content", "place-items", "align-content", "align-items", "justify-content",
            "justify-items", "gap", "-moz-column-gap", "column-gap", "row-gap",
            "border-right-width", "border-top-width", "border-bottom-width", "border-left-width",
            "border-color", "overflow-x", "overflow-y", "scroll-behavior", "text-overflow",
            "white-space", "text-wrap", "word-break", "border-radius",
            "border-top-left-radius", "border-top-right-radius", "border-bottom-right-radius",
            "border-bottom-left-radius", "border-width", "border-style",
            "background-color", "--tw-bg-opacity", "background-image", "--tw-gradient-from",
            "--tw-gradient-via", "--tw-gradient-to", "background-size", "background-position",
            "background-repeat", "-o-object-fit", "object-fit", "-o-object-position",
            "object-position", "padding", "padding-top", "padding-right", "padding-bottom",
            "padding-left", "text-align", "vertical-align", "font-family", "font-size",
            "font-weight", "text-transform", "font-style", "font-variant-numeric",
            "line-height", "letter-spacing", "color", "-webkit-text-decoration-line",
            "text-decoration-line", "opacity", "box-shadow", "outline-style", "outline",
            "outline-width", "outline-offset", "outline-color", "--tw-ring-inset",
            "--tw-ring-offset-width", "--tw-ring-offset-color", "--tw-ring-color",
            "--tw-ring-opacity", "--tw-blur", "filter", "-webkit-backdrop-filter",
            "backdrop-filter", "transition-property", "transition-delay", "transition-duration",
            "transition-timing-function",
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
