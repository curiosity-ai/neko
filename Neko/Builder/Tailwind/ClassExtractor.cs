using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neko.Builder.Tailwind
{
    /// <summary>
    /// Stage 1: harvest candidate class tokens from content files. Mirrors
    /// Tailwind's deliberately permissive extractor — it grabs any substring that
    /// could plausibly be a class, then later stages discard the ones that don't
    /// map to a known utility.
    /// </summary>
    internal static class ClassExtractor
    {
        // Characters that can appear inside a Tailwind class token. Everything
        // else (quotes, whitespace, &lt;&gt;, backticks, =, etc.) is a boundary.
        private static bool IsTokenChar(char ch)
        {
            // Mirror the character classes Tailwind's extractor permits inside a
            // candidate so we harvest (and reject) the same tokens it does. In
            // particular '|' is part of a token — so a pipe-delimited string in an
            // inlined script (e.g. "list-item|line|line-through") is one (invalid)
            // token rather than several valid utilities.
            return (ch >= 'A' && ch <= 'Z')
                || (ch >= 'a' && ch <= 'z')
                || (ch >= '0' && ch <= '9')
                || ch == '_' || ch == '-' || ch == ':' || ch == '/'
                || ch == '[' || ch == ']' || ch == '.' || ch == '!'
                || ch == '#' || ch == '%' || ch == '(' || ch == ')'
                || ch == ',' || ch == '+' || ch == '*' || ch == '@'
                || ch == '|' || ch == '&' || ch == '~'
                || ch == '?' || ch == ';' || ch > 0x7f;
        }

        /// <summary>Harvest the deduplicated token set from the given file contents.</summary>
        public static HashSet<string> Extract(IEnumerable<string> fileContents)
        {
            var tokens = new HashSet<string>();
            foreach (var content in fileContents)
            {
                if (string.IsNullOrEmpty(content)) continue;
                ExtractInto(content, tokens);
            }
            return tokens;
        }

        private static void ExtractInto(string content, HashSet<string> tokens)
        {
            int n = content.Length;
            var sb = new StringBuilder();
            for (int i = 0; i <= n; i++)
            {
                char ch = i < n ? content[i] : '\0';
                if (i < n && IsTokenChar(ch))
                {
                    sb.Append(ch);
                }
                else if (sb.Length > 0)
                {
                    AddToken(sb.ToString(), tokens);
                    sb.Clear();
                }
            }
        }

        private static void AddToken(string token, HashSet<string> tokens)
        {
            // Trim leading/trailing punctuation that is never the start/end of a
            // real class but is commonly adjacent in markup. A leading ':' is also
            // trimmed so that the tail of an escaped CSS selector — e.g.
            // `.dark\:bg-gray-900` in an inlined <style>, which splits on the
            // backslash into `:bg-gray-900` — recovers the bare utility, exactly
            // as Tailwind's extractor does.
            int start = 0, end = token.Length;
            while (start < end && (token[start] == ',' || token[start] == '.' || token[start] == '(' || token[start] == ')' || token[start] == '+' || token[start] == '*' || token[start] == ':'))
                start++;
            while (end > start && (token[end - 1] == ',' || token[end - 1] == '(' || token[end - 1] == ')' || token[end - 1] == '+' || token[end - 1] == '*'))
                end--;
            if (end <= start) return;

            var t = token.Substring(start, end - start);
            tokens.Add(t);
        }

        /// <summary>
        /// Reads the same content globs the build scans: emitted <c>.html</c> plus
        /// non-minified <c>.js</c> (so JS-toggled classes present as string
        /// literals are picked up), excluding <c>*.min.js</c>.
        /// </summary>
        public static IEnumerable<string> ReadContentFiles(string outputDir)
        {
            foreach (var file in Directory.EnumerateFiles(outputDir, "*.html", SearchOption.AllDirectories))
                yield return SafeRead(file);

            foreach (var file in Directory.EnumerateFiles(outputDir, "*.js", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".min.js", System.StringComparison.OrdinalIgnoreCase)) continue;
                yield return SafeRead(file);
            }
        }

        private static string SafeRead(string path)
        {
            try { return File.ReadAllText(path); }
            catch { return string.Empty; }
        }
    }
}
