using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class Badge : ContainerInline
    {
        public string Variant { get; set; } = "base"; // base, primary, etc.
        public string Corners { get; set; } = "round"; // round, square, pill
        public string Size { get; set; } = "m"; // xs, s, m, l, xl
        public string Icon { get; set; }
        public string Link { get; set; }
        public string IconAlign { get; set; }
    }

    public class BadgeParser : InlineParser
    {
        internal static readonly System.Lazy<MarkdownPipeline> _innerPipeline = new System.Lazy<MarkdownPipeline>(() =>
            new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Use<IconExtension>()
            .Build());

        public BadgeParser()
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

            var match = slice.Match("badge");
            if (!match)
            {
                slice = saved;
                return false;
            }
            slice.Start += 5;

            if (slice.CurrentChar != ' ' && slice.CurrentChar != ']')
            {
                 slice = saved;
                 return false;
            }
            if (slice.CurrentChar == ' ') slice.NextChar();

            var badge = new Badge();
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

            string textContent = content;

            // Check for key="value" pattern (Legacy/Advanced support)
            if (content.Contains("=\""))
            {
                int pos = 0;
                while (pos < content.Length)
                {
                    while (pos < content.Length && content[pos] == ' ') pos++;
                    if (pos >= content.Length) break;

                    int keyStart = pos;
                    while (pos < content.Length && content[pos] != '=') pos++;
                    if (pos >= content.Length) break;

                    var key = content.Substring(keyStart, pos - keyStart).Trim();
                    pos++;

                    if (pos < content.Length && content[pos] == '"')
                    {
                        pos++;
                        int valStart = pos;
                        while (pos < content.Length && content[pos] != '"') pos++;
                        var val = "";
                        if (pos < content.Length)
                        {
                            val = content.Substring(valStart, pos - valStart);
                            pos++;
                        }

                        switch (key.ToLower())
                        {
                            case "text": textContent = val; break;
                            case "variant": badge.Variant = val; break;
                            case "corners": badge.Corners = val; break;
                            case "size": badge.Size = val; break;
                            case "icon": badge.Icon = val; break;
                            case "iconalign": badge.IconAlign = val; break;
                        }
                    }
                }
            }
            else
            {
                // Parse positional args: [!badge variant text] or [!badge text]
                var firstSpace = content.IndexOf(' ');
                string firstWord = null;
                string remainder = null;

                if (firstSpace > 0)
                {
                    firstWord = content.Substring(0, firstSpace);
                    remainder = content.Substring(firstSpace + 1).Trim();
                }
                else
                {
                    firstWord = content;
                    remainder = "";
                }

                if (IsKnownVariant(firstWord) && !string.IsNullOrEmpty(remainder))
                {
                    badge.Variant = firstWord;
                    textContent = remainder;
                }
                else
                {
                    badge.Variant = "base";
                    textContent = content;
                }
            }

            // Check for optional link (url)
            if (slice.CurrentChar == '(')
            {
                slice.NextChar();
                var linkStart = slice.Start;
                int parenCount = 1;
                while (parenCount > 0 && !slice.IsEmpty)
                {
                    if (slice.CurrentChar == '(') parenCount++;
                    else if (slice.CurrentChar == ')') parenCount--;

                    if (parenCount > 0) slice.NextChar();
                }

                if (parenCount == 0)
                {
                    var link = slice.Text.Substring(linkStart, slice.Start - linkStart);
                    badge.Link = link;
                    slice.NextChar();
                }
            }

            // Parse textContent as markdown inlines
            if (!string.IsNullOrEmpty(textContent))
            {
                var doc = Markdown.Parse(textContent, _innerPipeline.Value);
                foreach (var block in doc)
                {
                    if (block is ParagraphBlock p && p.Inline != null)
                    {
                        var child = p.Inline.FirstChild;
                        while (child != null)
                        {
                            var next = child.NextSibling;
                            child.Remove();
                            badge.AppendChild(child);
                            child = next;
                        }
                    }
                }

            }

            processor.Inline = badge;
            return true;
        }

        private bool IsKnownVariant(string v)
        {
             if (string.IsNullOrEmpty(v)) return false;
             var l = v.ToLower();
             return l == "primary" || l == "secondary" || l == "success" || l == "danger" || l == "warning" || l == "info" || l == "light" || l == "dark" ||
                    l == "red" || l == "blue" || l == "green" || l == "yellow" || l == "orange" || l == "purple" || l == "sky" || l == "ghost" || l == "contrast";
        }
    }

    public class BadgeRenderer : HtmlObjectRenderer<Badge>
    {
        protected override void Write(HtmlRenderer renderer, Badge obj)
        {
            if (!string.IsNullOrEmpty(obj.Link))
            {
                renderer.Write($"<a href=\"{obj.Link}\" class=\"no-underline hover:opacity-80 transition-opacity\">");
            }

            // Map variant to Tailwind classes
            var bgClass = "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300"; // base
            switch (obj.Variant.ToLower())
            {
                case "primary":
                case "blue":
                    bgClass = "bg-primary-100 text-primary-800 dark:bg-primary-900 dark:text-primary-300";
                    break;
                case "success":
                case "green":
                    bgClass = "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300";
                    break;
                case "danger":
                case "red":
                    bgClass = "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300";
                    break;
                case "warning":
                case "yellow":
                case "orange":
                    bgClass = "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300";
                    break;
                case "info":
                case "sky":
                    bgClass = "bg-sky-100 text-sky-800 dark:bg-sky-900 dark:text-sky-300";
                    break;
                case "purple":
                    bgClass = "bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-300";
                    break;
                case "light":
                case "secondary":
                    bgClass = "bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300";
                    break;
                case "dark":
                    bgClass = "bg-gray-800 text-gray-100 dark:bg-gray-100 dark:text-gray-900";
                    break;
            }

            var roundedClass = "rounded"; // square
            switch (obj.Corners.ToLower())
            {
                case "round": roundedClass = "rounded-md"; break;
                case "pill": roundedClass = "rounded-full"; break;
                case "square": roundedClass = "rounded-none"; break;
            }

            var sizeClass = "text-sm px-2 py-0.5";
            switch (obj.Size.ToLower())
            {
                 case "xs": sizeClass = "text-xs px-1.5 py-0.5"; break;
                 case "s": sizeClass = "text-xs px-2 py-0.5"; break;
                 case "m": sizeClass = "text-sm px-2.5 py-0.5"; break;
                 case "l": sizeClass = "text-base px-3 py-1"; break;
                 case "xl": sizeClass = "text-lg px-3.5 py-1"; break;
            }

            renderer.Write($"<span class=\"inline-flex items-center font-medium {bgClass} {roundedClass} {sizeClass} mr-2 mb-1\" style=\"white-space: nowrap; flex-shrink: 0;\">");

            bool iconRight = string.Equals(obj.IconAlign, "right", System.StringComparison.OrdinalIgnoreCase);

            if (!iconRight && !string.IsNullOrEmpty(obj.Icon))
            {
                WriteIcon(renderer, obj.Icon, true);
            }

            renderer.WriteChildren(obj);

            if (iconRight && !string.IsNullOrEmpty(obj.Icon))
            {
                WriteIcon(renderer, obj.Icon, false);
            }

            renderer.Write("</span>");

            if (!string.IsNullOrEmpty(obj.Link))
            {
                renderer.Write("</a>");
            }
        }

        private void WriteIcon(HtmlRenderer renderer, string icon, bool isLeft)
        {
            var marginClass = isLeft ? "mr-1" : "ml-1";

            if (icon.Trim().StartsWith("<svg", System.StringComparison.OrdinalIgnoreCase))
            {
                 var svg = System.Net.WebUtility.HtmlDecode(icon);
                 renderer.Write($"<span class=\"{marginClass} inline-flex\">{svg}</span>");
            }
            else if (icon.StartsWith(":"))
            {
                 // Render using pipeline to resolve shortcodes
                 var pipeline = BadgeParser._innerPipeline.Value;
                 var html = Markdown.ToHtml(icon, pipeline);
                 // Strip <p> tags
                 html = html.Replace("<p>", "").Replace("</p>", "").Trim();
                 renderer.Write($"<span class=\"{marginClass}\">{html}</span>");
            }
            else
            {
                 renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)} {marginClass}\"></i>");
            }
        }
    }

    public class BadgeExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<BadgeParser>())
            {
                pipeline.InlineParsers.Insert(0, new BadgeParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<BadgeRenderer>();
            }
        }
    }
}
