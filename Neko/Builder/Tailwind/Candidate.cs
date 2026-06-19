using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// A parsed utility token, e.g. <c>dark:hover:-bg-primary-500/50!</c> split
    /// into its component parts. Produced by <see cref="CandidateParser"/> and
    /// consumed by the <see cref="UtilityRegistry"/> + <see cref="VariantEngine"/>.
    ///
    /// Fine-grained value parsing (opacity <c>/50</c>, fractions <c>1/2</c>,
    /// arbitrary <c>[...]</c>) is left to the individual utility handlers, since
    /// the meaning of <c>/</c> and <c>[]</c> depends on the utility family.
    /// </summary>
    internal sealed class Candidate
    {
        /// <summary>The full original token, used (escaped) as the class selector.</summary>
        public string Raw { get; set; }

        /// <summary>Ordered variant list, leftmost first (e.g. <c>dark</c>, <c>hover</c>).</summary>
        public List<string> Variants { get; } = new();

        /// <summary>
        /// The utility plus its value, with variants / leading <c>-</c> / trailing
        /// <c>!</c> removed (e.g. <c>bg-primary-500/50</c>, <c>min-h-[inherit]</c>,
        /// <c>w-1/2</c>). Handlers prefix-match and parse the value from this.
        /// </summary>
        public string Core { get; set; }

        /// <summary>True when the token carried a leading or trailing <c>!</c>.</summary>
        public bool Important { get; set; }

        /// <summary>True when the utility started with <c>-</c> (negative value).</summary>
        public bool Negative { get; set; }
    }
}
