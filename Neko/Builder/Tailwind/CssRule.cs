using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// A single generated CSS rule: one selector, an ordered list of
    /// declarations, and an optional list of wrapping at-rules (e.g.
    /// <c>@media (min-width: 1024px)</c>). The <see cref="SortKey"/> places the
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
        /// Cascade ordering. Lower sorts earlier. Responsive rules (with an
        /// <c>@media min-width</c>) sort after non-responsive ones, grouped by
        /// breakpoint, matching Tailwind's output.
        /// </summary>
        public long SortKey { get; set; }

        public CssRule(string selector)
        {
            Selector = selector;
        }

        public void Add(string prop, string val) => Declarations.Add((prop, val));
    }
}
