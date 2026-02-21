using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;

namespace TailDocs.CLI.Extensions
{
    public class Badge : Inline
    {
        public string Text { get; set; }
        public string Variant { get; set; } = "base"; // base, primary, etc.
        public string Corners { get; set; } = "round"; // round, square, pill
        public string Size { get; set; } = "m"; // xs, s, m, l, xl
        public string Icon { get; set; }
        public string Link { get; set; }
    }

    public class BadgeParser : InlineParser
    {
        public BadgeParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            // Check for starting [
            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            // Check for !badge
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
            slice.Start += 5; // length of "badge"

            // Parse attributes
            // Expect space
            if (slice.CurrentChar != ' ')
            {
                // Maybe just [!badge] ? unlikely without text.
                // But let's assume attributes follow.
            }

            var badge = new Badge();

            // Simple attribute parsing loop
            while (slice.CurrentChar != ']')
            {
                if (slice.IsEmpty)
                {
                    slice = saved;
                    return false;
                }

                if (slice.CurrentChar == ' ')
                {
                    slice.NextChar();
                    continue;
                }

                // Parse key="value"
                // Find key
                var keyStart = slice.Start;
                while (slice.CurrentChar != '=' && slice.CurrentChar != ']' && !slice.IsEmpty)
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar != '=') break; // Should be =

                var key = slice.Text.Substring(keyStart, slice.Start - keyStart).Trim();
                slice.NextChar(); // Skip =

                // Parse value
                if (slice.CurrentChar != '"') break; // Should start with "
                slice.NextChar();

                var valStart = slice.Start;
                while (slice.CurrentChar != '"' && !slice.IsEmpty)
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar != '"') break; // Should end with "

                var val = slice.Text.Substring(valStart, slice.Start - valStart);
                slice.NextChar(); // Skip closing "

                // Assign to badge
                switch (key.ToLower())
                {
                    case "text": badge.Text = val; break;
                    case "variant": badge.Variant = val; break;
                    case "corners": badge.Corners = val; break;
                    case "size": badge.Size = val; break;
                    case "icon": badge.Icon = val; break;
                }
            }

            if (slice.CurrentChar == ']')
            {
                slice.NextChar();

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
                        slice.NextChar(); // consume closing )
                    }
                }

                processor.Inline = badge;
                return true;
            }

            slice = saved;
            return false;
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
                case "primary": bgClass = "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300"; break;
                case "success": bgClass = "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300"; break;
                case "danger": bgClass = "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300"; break;
                case "warning": bgClass = "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300"; break;
                case "info": bgClass = "bg-sky-100 text-sky-800 dark:bg-sky-900 dark:text-sky-300"; break;
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

            renderer.Write($"<span class=\"inline-flex items-center font-medium {bgClass} {roundedClass} {sizeClass} mr-2\">");

            if (!string.IsNullOrEmpty(obj.Icon))
            {
                // Simple icon handling
                if (obj.Icon.StartsWith(":"))
                {
                     // Emoji
                     renderer.Write(obj.Icon.Trim(':') + " ");
                }
                else
                {
                    // Assume Flaticon class or name
                     renderer.Write($"<i class=\"fi fi-rr-{obj.Icon} mr-1\"></i>");
                }
            }

            renderer.Write(obj.Text);
            renderer.Write("</span>");

            if (!string.IsNullOrEmpty(obj.Link))
            {
                renderer.Write("</a>");
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
