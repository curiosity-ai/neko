using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// A single generated CSS rule: one selector, an ordered list of
    /// declarations, and an optional list of wrapping at-rules (e.g.
    /// <c>@media (min-width: 1024px)</c>). The rank fields place the
    /// rule in Tailwind's cascade order when the final stylesheet is assembled.
    /// </summary>
    internal sealed class CssRule
    {
        public string Selector { get; set; }
        public List<(string Prop, string Val)> Declarations { get; } = new();

        /// <summary>Wrapping at-rules, outermost first (e.g. a single <c>@media</c>).</summary>
        public List<string> AtRules { get; } = new();

        /// <summary>The originating utility token (for de-duplication / debugging).</summary>
        public string Token { get; set; }

        /// <summary>
        /// Cascade ordering components, applied lexicographically by
        /// <see cref="RuleSorter"/> to reproduce Tailwind's deterministic order:
        /// media group (responsive last, by breakpoint) → variant group
        /// (unvariant first) → utility/property order → value order. A fully
        /// ordered key is essential — without it, conflicting same-specificity
        /// utilities (e.g. <c>p-4</c> vs <c>px-2</c>) would resolve by source
        /// order, which must be stable and correct, not hash-dependent.
        /// </summary>
        public int MediaRank { get; set; }
        public int VariantRank { get; set; }
        public int UtilityRank { get; set; }
        public int ValueRank { get; set; }

        public CssRule(string selector)
        {
            Selector = selector;
        }

        public void Add(string prop, string val) => Declarations.Add((prop, val));
    }
}
