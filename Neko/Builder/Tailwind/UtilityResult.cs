using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// The output of resolving one utility token: one or more "parts", each a set
    /// of declarations plus an optional selector suffix appended after the
    /// (variant-decorated) class selector. Most utilities yield a single part with
    /// an empty suffix; <c>space-*</c>/<c>divide-*</c> add a child-combinator
    /// suffix, and <c>placeholder-*</c> yields two pseudo-element parts.
    /// </summary>
    internal sealed class UtilityResult
    {
        public List<UtilityPart> Parts { get; } = new();

        public static UtilityResult Of(params (string Prop, string Val)[] decls)
        {
            var r = new UtilityResult();
            var p = new UtilityPart();
            p.Declarations.AddRange(decls);
            r.Parts.Add(p);
            return r;
        }

        public UtilityResult Add(UtilityPart part)
        {
            Parts.Add(part);
            return this;
        }
    }

    internal sealed class UtilityPart
    {
        /// <summary>Appended verbatim after the class selector + variant suffixes (e.g. <c>::placeholder</c>).</summary>
        public string SelectorSuffix { get; set; } = "";
        public List<(string Prop, string Val)> Declarations { get; } = new();
    }
}
