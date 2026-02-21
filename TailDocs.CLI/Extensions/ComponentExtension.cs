using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;

namespace TailDocs.CLI.Extensions
{
    public class ComponentInline : Inline
    {
        public string Name { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();

        public string GetAttribute(string key, string defaultValue = "")
        {
            return Attributes.TryGetValue(key.ToLower(), out var value) ? value : defaultValue;
        }
    }

    public class ComponentParser : InlineParser
    {
        public ComponentParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            // Check for starting [
            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            // Check for !
            if (slice.CurrentChar != '!')
            {
                 slice = saved;
                 return false;
            }
            slice.NextChar();

            // Parse name (e.g. button, icon, etc.)
            // Name should be alphanumeric + dashes
            var nameStart = slice.Start;
            while (slice.CurrentChar.IsAlphaNumeric() || slice.CurrentChar == '-')
            {
                slice.NextChar();
            }
            var nameLength = slice.Start - nameStart;
            if (nameLength == 0)
            {
                slice = saved;
                return false;
            }

            var name = slice.Text.Substring(nameStart, nameLength).ToLower();

            // Avoid conflict with GitHub Alerts (which might be parsed as ComponentInline if we don't skip them)
            // GitHub alerts use > [!NOTE], which Markdig post-processes. If we parse [!NOTE] as a component,
            // Markdig's alert detection logic (likely inspecting inlines) fails.
            if (name == "note" || name == "tip" || name == "important" || name == "warning" || name == "caution")
            {
                slice = saved;
                return false;
            }

            // Avoid conflict with BadgeParser
            if (name == "badge")
            {
                slice = saved;
                return false;
            }

            // Skip space if any
            if (slice.CurrentChar == ' ')
            {
                slice.NextChar();
            }

            var component = new ComponentInline { Name = name };

            // Parse attributes loop
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
                var keyStart = slice.Start;
                while (slice.CurrentChar != '=' && slice.CurrentChar != ']' && !slice.IsEmpty && slice.CurrentChar != ' ')
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar != '=')
                {
                    // Maybe boolean attribute? Or broken syntax.
                    // For now, assume key=value format strictly as per spec.
                    // Or if we hit ']', we are done (no more attributes).
                    if (slice.CurrentChar == ']') break;

                    // If we hit space, it might be a boolean attribute, let's skip
                    if (slice.CurrentChar == ' ') continue;

                    // Fallback
                    break;
                }

                var key = slice.Text.Substring(keyStart, slice.Start - keyStart).Trim();
                slice.NextChar(); // Skip =

                // Parse value
                if (slice.CurrentChar != '"')
                {
                     // Maybe unquoted value? Let's skip for now to keep it simple.
                     break;
                }
                slice.NextChar(); // Skip "

                var valStart = slice.Start;
                while (slice.CurrentChar != '"' && !slice.IsEmpty)
                {
                    slice.NextChar(); // Just advance
                }

                if (slice.CurrentChar != '"') break; // Should end with "

                var val = slice.Text.Substring(valStart, slice.Start - valStart);
                slice.NextChar(); // Skip closing "

                component.Attributes[key.ToLower()] = val;
            }

            if (slice.CurrentChar == ']')
            {
                slice.NextChar();
                processor.Inline = component;
                return true;
            }

            slice = saved;
            return false;
        }
    }

    public class ComponentRenderer : HtmlObjectRenderer<ComponentInline>
    {
        protected override void Write(HtmlRenderer renderer, ComponentInline obj)
        {
            switch (obj.Name)
            {
                case "button":
                    RenderButton(renderer, obj);
                    break;
                case "color-chip":
                    RenderColorChip(renderer, obj);
                    break;
                case "embed":
                    RenderEmbed(renderer, obj);
                    break;
                case "file":
                    RenderFile(renderer, obj);
                    break;
                case "icon":
                    RenderIcon(renderer, obj);
                    break;
                case "ref":
                    RenderRef(renderer, obj);
                    break;
                case "youtube":
                    RenderYouTube(renderer, obj);
                    break;
                default:
                    // Fallback or ignore
                    renderer.Write($"<span class=\"text-red-500\">Unknown component: {obj.Name}</span>");
                    break;
            }
        }

        private void RenderButton(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text");
            var link = obj.GetAttribute("link", "#");
            var variant = obj.GetAttribute("variant", "primary");
            var icon = obj.GetAttribute("icon");

            var bgClass = "bg-blue-600 hover:bg-blue-700 text-white";
            switch (variant)
            {
                case "secondary": bgClass = "bg-gray-600 hover:bg-gray-700 text-white"; break;
                case "outline": bgClass = "bg-transparent border border-blue-600 text-blue-600 hover:bg-blue-50"; break;
                case "danger": bgClass = "bg-red-600 hover:bg-red-700 text-white"; break;
            }

            renderer.Write($"<a href=\"{link}\" class=\"inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm {bgClass} focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 mr-2 no-underline\">");

            if (!string.IsNullOrEmpty(icon))
            {
                renderer.Write($"<i class=\"fi fi-rr-{icon} mr-2\"></i>");
            }

            renderer.Write(text);
            renderer.Write("</a>");
        }

        private void RenderColorChip(HtmlRenderer renderer, ComponentInline obj)
        {
            var color = obj.GetAttribute("color", "#000000");
            var text = obj.GetAttribute("text", color);

            renderer.Write($"<span class=\"inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300 mr-2 border border-gray-200 dark:border-gray-600\">");
            renderer.Write($"<span class=\"w-3 h-3 rounded-full mr-1.5\" style=\"background-color: {color};\"></span>");
            renderer.Write(text);
            renderer.Write("</span>");
        }

        private void RenderEmbed(HtmlRenderer renderer, ComponentInline obj)
        {
            var src = obj.GetAttribute("src");
            var height = obj.GetAttribute("height", "400"); // Default height

            if (!string.IsNullOrEmpty(src))
            {
                renderer.Write($"<div class=\"my-4 w-full rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 shadow-sm\">");
                renderer.Write($"<iframe src=\"{src}\" style=\"width: 100%; height: {height}px;\" frameborder=\"0\" allowfullscreen></iframe>");
                renderer.Write("</div>");
            }
        }

        private void RenderFile(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text", "Download");
            var link = obj.GetAttribute("link", "#");
            var size = obj.GetAttribute("size"); // Optional file size text

            renderer.Write($"<a href=\"{link}\" class=\"inline-flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors no-underline group my-2\">");
            renderer.Write($"<div class=\"p-2 bg-blue-50 dark:bg-blue-900 rounded-md mr-4 group-hover:bg-blue-100 dark:group-hover:bg-blue-800 transition-colors\">");
            renderer.Write($"<i class=\"fi fi-rr-document text-blue-600 dark:text-blue-400 text-xl\"></i>");
            renderer.Write("</div>");
            renderer.Write("<div>");
            renderer.Write($"<div class=\"font-medium text-gray-900 dark:text-white\">{text}</div>");
            if (!string.IsNullOrEmpty(size))
            {
                renderer.Write($"<div class=\"text-xs text-gray-500 dark:text-gray-400\">{size}</div>");
            }
            renderer.Write("</div>");
            renderer.Write("</a>");
        }

        private void RenderIcon(HtmlRenderer renderer, ComponentInline obj)
        {
            var name = obj.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
            {
                renderer.Write($"<i class=\"fi fi-rr-{name} align-middle\"></i>");
            }
        }

        private void RenderRef(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text");
            var link = obj.GetAttribute("link", "#");

            renderer.Write($"<a href=\"{link}\" class=\"inline-flex items-center text-blue-600 dark:text-blue-400 hover:underline no-underline\">");
            renderer.Write($"<i class=\"fi fi-rr-link-alt mr-1\"></i>");
            renderer.Write(text);
            renderer.Write("</a>");
        }

        private void RenderYouTube(HtmlRenderer renderer, ComponentInline obj)
        {
            var id = obj.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
            {
                renderer.Write($"<div class=\"aspect-w-16 aspect-h-9 my-4\">");
                renderer.Write($"<iframe src=\"https://www.youtube.com/embed/{id}\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen class=\"w-full h-full rounded-lg shadow-lg\"></iframe>");
                renderer.Write("</div>");
            }
        }
    }

    public class ComponentExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<ComponentParser>())
            {
                pipeline.InlineParsers.Insert(0, new ComponentParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<ComponentRenderer>();
            }
        }
    }
}
