using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 5: orders generated utility rules for the cascade. Non-responsive
    /// rules come first (in stable generation order), then each responsive
    /// <c>@media</c> block grouped <c>sm → 2xl</c> (and <c>max-*</c> after),
    /// matching how Tailwind groups responsive variants at the end of the layer.
    /// </summary>
    internal static class RuleSorter
    {
        public static List<CssRule> Sort(List<CssRule> rules)
        {
            // Stable sort by (SortKey, original index).
            var indexed = new List<(CssRule Rule, int Index)>(rules.Count);
            for (int i = 0; i < rules.Count; i++) indexed.Add((rules[i], i));
            indexed.Sort((a, b) =>
            {
                int c = a.Rule.SortKey.CompareTo(b.Rule.SortKey);
                return c != 0 ? c : a.Index.CompareTo(b.Index);
            });
            var result = new List<CssRule>(rules.Count);
            foreach (var (rule, _) in indexed) result.Add(rule);
            return result;
        }
    }
}
