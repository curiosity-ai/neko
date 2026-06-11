using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Net;

namespace Neko.Extensions
{
    /// <summary>
    /// Inline component that renders a compact, theme-agnostic "package pill":
    /// a muted label, a bold monospace version, an optional link on the label/version,
    /// and a copy-to-clipboard button. Several badges placed next to each other flow
    /// inline and wrap onto the same line.
    /// </summary>
    public class VersionBadge : Inline
    {
        public string Text { get; set; }
        public string Version { get; set; }
        public string Url { get; set; }
        public string Copy { get; set; }
        public string Icon { get; set; }
    }

    public class VersionBadgeParser : InlineParser
    {
        public VersionBadgeParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            if (slice.CurrentChar != '!')
            {
                slice = saved;
                return false;
            }
            slice.NextChar();

            if (!slice.Match("version-badge"))
            {
                slice = saved;
                return false;
            }
            slice.Start += "version-badge".Length;

            if (slice.CurrentChar != ' ' && slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }
            if (slice.CurrentChar == ' ') slice.NextChar();

            var contentStart = slice.Start;
            while (slice.CurrentChar != ']' && !slice.IsEmpty)
            {
                slice.NextChar();
            }

            if (slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }

            var content = slice.Text.Substring(contentStart, slice.Start - contentStart).Trim();
            slice.NextChar();

            var badge = new VersionBadge();
            ParseAttributes(content, badge);

            processor.Inline = badge;
            return true;
        }

        // Parses `key="value"` pairs. Unknown keys are ignored.
        private static void ParseAttributes(string content, VersionBadge badge)
        {
            int pos = 0;
            while (pos < content.Length)
            {
                while (pos < content.Length && content[pos] == ' ') pos++;
                if (pos >= content.Length) break;

                int keyStart = pos;
                while (pos < content.Length && content[pos] != '=' && content[pos] != ' ') pos++;
                if (pos >= content.Length || content[pos] != '=') break;

                var key = content.Substring(keyStart, pos - keyStart).Trim();
                pos++; // skip '='

                if (pos < content.Length && content[pos] == '"')
                {
                    pos++;
                    int valStart = pos;
                    while (pos < content.Length && content[pos] != '"') pos++;
                    var val = content.Substring(valStart, System.Math.Max(0, pos - valStart));
                    if (pos < content.Length) pos++; // skip closing quote

                    switch (key.ToLowerInvariant())
                    {
                        case "text": badge.Text = val; break;
                        case "version": badge.Version = val; break;
                        case "url": badge.Url = val; break;
                        case "copy": badge.Copy = val; break;
                        case "icon": badge.Icon = val; break;
                    }
                }
            }
        }
    }

    public class VersionBadgeRenderer : HtmlObjectRenderer<VersionBadge>
    {
        protected override void Write(HtmlRenderer renderer, VersionBadge obj)
        {
            var text = obj.Text;
            var version = obj.Version;
            var url = obj.Url;
            // What the copy button puts on the clipboard: explicit `copy`, else the
            // version, else the label.
            var copy = !string.IsNullOrEmpty(obj.Copy) ? obj.Copy
                     : !string.IsNullOrEmpty(version) ? version
                     : text;

            renderer.Write("<span class=\"neko-version-badge inline-flex items-center gap-1.5 align-middle mr-2 mb-2 rounded-lg border border-gray-200 dark:border-white/10 bg-gray-50 dark:bg-white/5 pl-2.5 pr-1.5 py-1 text-sm leading-none\">");

            if (!string.IsNullOrEmpty(obj.Icon))
            {
                renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(obj.Icon)} text-gray-400 dark:text-gray-500 text-xs\"></i>");
            }

            var hasLink = !string.IsNullOrEmpty(url);
            if (hasLink)
            {
                renderer.Write($"<a href=\"{WebUtility.HtmlEncode(url)}\" class=\"inline-flex items-center gap-1.5 no-underline group/vb\">");
            }

            if (!string.IsNullOrEmpty(text))
            {
                var textClasses = hasLink
                    ? "text-gray-500 dark:text-gray-400 group-hover/vb:text-gray-700 dark:group-hover/vb:text-gray-200 transition-colors"
                    : "text-gray-500 dark:text-gray-400";
                renderer.Write($"<span class=\"{textClasses}\">{WebUtility.HtmlEncode(text)}</span>");
            }

            if (!string.IsNullOrEmpty(version))
            {
                renderer.Write($"<span class=\"font-mono font-semibold text-gray-800 dark:text-gray-100\">{WebUtility.HtmlEncode(version)}</span>");
            }

            if (hasLink)
            {
                renderer.Write("</a>");
            }

            renderer.Write($"<button type=\"button\" class=\"neko-copy-btn inline-flex items-center justify-center p-1 rounded text-gray-400 hover:text-gray-700 dark:text-gray-500 dark:hover:text-gray-200 hover:bg-gray-200/70 dark:hover:bg-white/10 transition-colors\" data-copy=\"{WebUtility.HtmlEncode(copy ?? string.Empty)}\" title=\"Copy\" aria-label=\"Copy\"><i class=\"fi fi-rr-copy text-xs\"></i></button>");

            renderer.Write("</span>");
        }
    }

    public class VersionBadgeExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<VersionBadgeParser>())
            {
                // Insert ahead of BadgeParser so `[!version-badge ...]` is matched
                // before the more general `[!badge ...]` parser sees it.
                pipeline.InlineParsers.Insert(0, new VersionBadgeParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<VersionBadgeRenderer>();
            }
        }
    }
}
