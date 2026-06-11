using Markdig.Renderers;
using Markdig.Syntax;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    /// <summary>
    /// Renders a "card of links": a titled, theme-agnostic card where each row is a
    /// labelled link on the left and a version pill on the right. Authored as a
    /// fenced <c>```links</c> block (dispatched from <see cref="CodeBlockRenderer"/>),
    /// one row per line in the form <c>text | url | version</c> (url and version
    /// optional).
    /// </summary>
    public static class LinkCardComponent
    {
        public static void Write(HtmlRenderer renderer, FencedCodeBlock block)
        {
            var args = block.Arguments ?? string.Empty;
            var title = ExtractArg(args, "title");
            var icon = ExtractArg(args, "icon") ?? "box";

            renderer.Write("<div class=\"neko-link-card not-prose my-6 rounded-2xl border border-gray-200 dark:border-white/10 bg-gray-50 dark:bg-white/[0.03] p-5\">");

            // Header (icon + title)
            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(icon))
            {
                renderer.Write("<div class=\"flex items-center gap-2 mb-3 text-gray-900 dark:text-gray-100\">");
                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)} text-gray-500 dark:text-gray-400\"></i>");
                }
                if (!string.IsNullOrEmpty(title))
                {
                    renderer.Write($"<span class=\"text-lg font-semibold tracking-tight\">{WebUtility.HtmlEncode(title)}</span>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"divide-y divide-gray-200 dark:divide-white/10\">");

            foreach (var (text, url, version) in ParseRows(block))
            {
                renderer.Write("<div class=\"flex items-center justify-between gap-4 py-2.5\">");

                // Left: label (linked when a url is given)
                if (!string.IsNullOrEmpty(url))
                {
                    renderer.Write($"<a href=\"{WebUtility.HtmlEncode(url)}\" class=\"min-w-0 truncate no-underline text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 transition-colors\">{WebUtility.HtmlEncode(text)}</a>");
                }
                else
                {
                    renderer.Write($"<span class=\"min-w-0 truncate text-gray-500 dark:text-gray-400\">{WebUtility.HtmlEncode(text)}</span>");
                }

                // Right: version pill
                if (!string.IsNullOrEmpty(version))
                {
                    renderer.Write($"<span class=\"shrink-0 font-mono text-sm font-semibold rounded-md border border-gray-200 dark:border-white/10 bg-white dark:bg-white/[0.06] px-2 py-0.5 text-gray-800 dark:text-gray-100\">{WebUtility.HtmlEncode(version)}</span>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>");
            renderer.Write("</div>");
        }

        private static IEnumerable<(string Text, string Url, string Version)> ParseRows(FencedCodeBlock block)
        {
            var rows = new List<(string, string, string)>();
            var lines = block.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines.Lines[i].Slice.ToString();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('|');
                var text = parts.Length > 0 ? parts[0].Trim() : string.Empty;
                var url = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                var version = parts.Length > 2 ? parts[2].Trim() : string.Empty;

                if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(version)) continue;
                rows.Add((text, url, version));
            }
            return rows;
        }

        private static string ExtractArg(string args, string key)
        {
            var m = Regex.Match(args ?? string.Empty, key + "=\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
