using System.Text;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Conservative CSS minifier: strips comments and collapses insignificant
    /// whitespace while preserving string literals and significant spaces inside
    /// values (e.g. <c>rgb(0 0 0 / 0.5)</c>). Used to shrink the shipped static
    /// layers; the generated utilities are already emitted compactly.
    /// </summary>
    internal static class CssMinifier
    {
        public static string Minify(string css)
        {
            if (string.IsNullOrEmpty(css)) return css;

            // 1. Strip /* ... */ comments outside of strings.
            var noComments = StripComments(css);

            // 2. Collapse whitespace runs to a single space (outside strings).
            var sb = new StringBuilder(noComments.Length);
            char quote = '\0';
            bool pendingSpace = false;
            foreach (var ch in noComments)
            {
                if (quote != '\0')
                {
                    sb.Append(ch);
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch == '"' || ch == '\'')
                {
                    FlushSpace(sb, ref pendingSpace);
                    quote = ch;
                    sb.Append(ch);
                    continue;
                }
                if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r' || ch == '\f')
                {
                    pendingSpace = true;
                    continue;
                }
                // Drop spaces around structural punctuation.
                if (ch == '{' || ch == '}' || ch == ';' || ch == ',')
                {
                    pendingSpace = false;
                    TrimTrailingSpace(sb);
                    sb.Append(ch);
                    continue;
                }
                FlushSpace(sb, ref pendingSpace);
                sb.Append(ch);
            }

            // Remove empty rules and trailing semicolons before '}'.
            return sb.ToString().Replace(";}", "}");
        }

        private static void FlushSpace(StringBuilder sb, ref bool pending)
        {
            if (pending)
            {
                if (sb.Length > 0)
                {
                    char last = sb[sb.Length - 1];
                    if (last != '{' && last != '}' && last != ';' && last != ',' && last != ':')
                        sb.Append(' ');
                }
                pending = false;
            }
        }

        private static void TrimTrailingSpace(StringBuilder sb)
        {
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;
        }

        private static string StripComments(string css)
        {
            var sb = new StringBuilder(css.Length);
            char quote = '\0';
            for (int i = 0; i < css.Length; i++)
            {
                char ch = css[i];
                if (quote != '\0')
                {
                    sb.Append(ch);
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch == '"' || ch == '\'') { quote = ch; sb.Append(ch); continue; }
                if (ch == '/' && i + 1 < css.Length && css[i + 1] == '*')
                {
                    int end = css.IndexOf("*/", i + 2);
                    if (end < 0) break;
                    i = end + 1;
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
