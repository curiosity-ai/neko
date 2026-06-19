using System.Collections.Generic;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 2: turns a raw harvested string into a <see cref="Candidate"/> by
    /// peeling off variants, the <c>!important</c> marker, and a leading negative
    /// sign. Returns null for tokens that obviously cannot be a utility.
    /// </summary>
    internal static class CandidateParser
    {
        public static Candidate Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var c = new Candidate { Raw = token };

            // Split variants on ':' at bracket depth 0. The final segment is the
            // utility; everything before it is a variant.
            var segments = SplitTopLevel(token, ':');
            if (segments.Count == 0) return null;

            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].Length == 0) return null; // stray "::" etc.
                c.Variants.Add(segments[i]);
            }

            var util = segments[segments.Count - 1];
            if (util.Length == 0) return null;

            // Important: leading or trailing '!'.
            if (util.StartsWith("!"))
            {
                c.Important = true;
                util = util.Substring(1);
            }
            if (util.EndsWith("!"))
            {
                c.Important = true;
                util = util.Substring(0, util.Length - 1);
            }
            if (util.Length == 0) return null;

            // Negative: a leading '-'.
            if (util.StartsWith("-"))
            {
                c.Negative = true;
                util = util.Substring(1);
            }
            if (util.Length == 0) return null;

            c.Core = util;
            return c;
        }

        /// <summary>Splits on <paramref name="sep"/> only at bracket/paren depth 0.</summary>
        internal static List<string> SplitTopLevel(string s, char sep)
        {
            var parts = new List<string>();
            int depth = 0;
            int start = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch == '[' || ch == '(') depth++;
                else if (ch == ']' || ch == ')') { if (depth > 0) depth--; }
                else if (ch == sep && depth == 0)
                {
                    parts.Add(s.Substring(start, i - start));
                    start = i + 1;
                }
            }
            parts.Add(s.Substring(start));
            return parts;
        }
    }
}
