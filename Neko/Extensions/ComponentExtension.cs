using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class ComponentInline : Inline
    {
        public string Name { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<string> Arguments { get; set; } = new List<string>();

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

                // Parse key="value" or positional value
                var start = slice.Start;
                while (slice.CurrentChar != '=' && slice.CurrentChar != ']' && !slice.IsEmpty && slice.CurrentChar != ' ')
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar == '=')
                {
                    var key = slice.Text.Substring(start, slice.Start - start).Trim();
                    slice.NextChar(); // Skip =

                    // Parse value
                    if (slice.CurrentChar == '"')
                    {
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
                    else
                    {
                        // Unquoted value until space or ]
                        var valStart = slice.Start;
                        while (slice.CurrentChar != ' ' && slice.CurrentChar != ']' && !slice.IsEmpty)
                        {
                            slice.NextChar();
                        }
                        var val = slice.Text.Substring(valStart, slice.Start - valStart);
                        component.Attributes[key.ToLower()] = val;
                    }
                }
                else
                {
                    // Positional argument
                    var val = slice.Text.Substring(start, slice.Start - start).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        component.Arguments.Add(val);
                    }

                    if (slice.CurrentChar == ']') break;
                }
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
        private readonly MarkdownPipeline _pipeline;

        public ComponentRenderer(MarkdownPipeline pipeline)
        {
            _pipeline = pipeline;
        }

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
                case "emoji-table":
                    RenderEmojiTable(renderer, obj);
                    break;
                case "hero":
                    RenderHero(renderer, obj);
                    break;
                default:
                    // Fallback or ignore
                    renderer.Write($"<span class=\"text-red-500\">Unknown component: {obj.Name}</span>");
                    break;
            }
        }

        private void RenderHero(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");
            var badgeText = obj.GetAttribute("badge-text");
            var badgeLink = obj.GetAttribute("badge-link");
            var cta1Text = obj.GetAttribute("cta1-text");
            var cta1Link = obj.GetAttribute("cta1-link");
            var cta2Text = obj.GetAttribute("cta2-text");
            var cta2Link = obj.GetAttribute("cta2-link");
            var image = obj.GetAttribute("image");
            var align = obj.GetAttribute("align", "center");

            // Container
            renderer.Write("<div class=\"not-prose relative isolate overflow-hidden bg-gray-900 py-24 sm:py-32 rounded-3xl my-8\">");

            // Background Image/Gradient
            if (!string.IsNullOrEmpty(image))
            {
                renderer.Write("<img src=\"");
                renderer.WriteEscapeUrl(image);
                renderer.Write("\" alt=\"\" class=\"absolute inset-0 -z-10 h-full w-full object-cover object-right md:object-center opacity-20\">");
            }
            else
            {
                // Default Dark Gradient
                renderer.Write("<div class=\"hidden sm:absolute sm:-top-10 sm:right-1/2 sm:-z-10 sm:mr-10 sm:block sm:transform-gpu sm:blur-3xl\" aria-hidden=\"true\">");
                renderer.Write("<div class=\"aspect-[1097/845] w-[68.5625rem] bg-gradient-to-tr from-[#ff4694] to-[#776fff] opacity-20\" style=\"clip-path: polygon(74.1% 44.1%, 100% 61.6%, 97.5% 26.9%, 85.5% 0.1%, 80.7% 2%, 72.5% 32.5%, 60.2% 62.4%, 52.4% 68.1%, 47.5% 58.3%, 45.2% 34.5%, 27.5% 76.7%, 0.1% 64.9%, 17.9% 100%, 27.6% 76.8%, 76.1% 97.7%, 74.1% 44.1%)\"></div>");
                renderer.Write("</div>");
                renderer.Write("<div class=\"absolute -top-52 left-1/2 -z-10 -translate-x-1/2 transform-gpu blur-3xl sm:top-[-28rem] sm:ml-16 sm:translate-x-0 sm:transform-gpu\" aria-hidden=\"true\">");
                renderer.Write("<div class=\"aspect-[1097/845] w-[68.5625rem] bg-gradient-to-tr from-[#ff4694] to-[#776fff] opacity-20\" style=\"clip-path: polygon(74.1% 44.1%, 100% 61.6%, 97.5% 26.9%, 85.5% 0.1%, 80.7% 2%, 72.5% 32.5%, 60.2% 62.4%, 52.4% 68.1%, 47.5% 58.3%, 45.2% 34.5%, 27.5% 76.7%, 0.1% 64.9%, 17.9% 100%, 27.6% 76.8%, 76.1% 97.7%, 74.1% 44.1%)\"></div>");
                renderer.Write("</div>");
            }

            // Content Wrapper
            var alignClass = align == "left" ? "text-left" : "text-center";
            var maxWClass = align == "left" ? "max-w-2xl" : "mx-auto max-w-2xl";

            renderer.Write($"<div class=\"{maxWClass} {alignClass} px-6 lg:px-8\">");

            // Badge
            if (!string.IsNullOrEmpty(badgeText))
            {
                var badgeAlign = align == "left" ? "justify-start" : "justify-center";
                renderer.Write($"<div class=\"hidden sm:mb-8 sm:flex {badgeAlign}\">");
                renderer.Write("<div class=\"relative rounded-full px-3 py-1 text-sm leading-6 text-gray-400 ring-1 ring-white/10 hover:ring-white/20\">");

                if (!string.IsNullOrEmpty(badgeLink))
                {
                    renderer.WriteEscape(badgeText);
                    renderer.Write(" <a href=\"");
                    renderer.WriteEscapeUrl(badgeLink);
                    renderer.Write("\" class=\"font-semibold text-white\"><span class=\"absolute inset-0\" aria-hidden=\"true\"></span>Read more <span aria-hidden=\"true\">&rarr;</span></a>");
                }
                else
                {
                    renderer.WriteEscape(badgeText);
                }
                renderer.Write("</div></div>");
            }

            // Title
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h2 class=\"text-4xl font-bold tracking-tight text-white sm:text-6xl\">");
                renderer.WriteEscape(title);
                renderer.Write("</h2>");
            }

            // Subtitle
            if (!string.IsNullOrEmpty(subtitle))
            {
                renderer.Write("<p class=\"mt-6 text-lg leading-8 text-gray-300\">");
                renderer.WriteEscape(subtitle);
                renderer.Write("</p>");
            }

            // Buttons
            if (!string.IsNullOrEmpty(cta1Text) || !string.IsNullOrEmpty(cta2Text))
            {
                var justifyClass = align == "left" ? "justify-start" : "justify-center";
                renderer.Write($"<div class=\"mt-10 flex items-center {justifyClass} gap-x-6\">");

                if (!string.IsNullOrEmpty(cta1Text))
                {
                     var link = cta1Link ?? "#";
                     renderer.Write("<a href=\"");
                     renderer.WriteEscapeUrl(link);
                     renderer.Write("\" class=\"rounded-md bg-indigo-500 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400 no-underline\">");
                     renderer.WriteEscape(cta1Text);
                     renderer.Write("</a>");
                }

                if (!string.IsNullOrEmpty(cta2Text))
                {
                     var link = cta2Link ?? "#";
                     renderer.Write("<a href=\"");
                     renderer.WriteEscapeUrl(link);
                     renderer.Write("\" class=\"text-sm font-semibold leading-6 text-white no-underline\">");
                     renderer.WriteEscape(cta2Text);
                     renderer.Write(" <span aria-hidden=\"true\">→</span></a>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>"); // End Content Wrapper
            renderer.Write("</div>"); // End Container
        }

        private void RenderButton(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text");
            var link = obj.GetAttribute("link", "#");
            var variant = obj.GetAttribute("variant", "primary");
            var corners = obj.GetAttribute("corners", "round");
            var size = obj.GetAttribute("size", "m");
            var icon = obj.GetAttribute("icon");
            var target = obj.GetAttribute("target");

            // 1. Variant Styles
            var bgClass = "bg-blue-600 hover:bg-blue-700 text-white dark:bg-blue-600 dark:hover:bg-blue-500"; // default primary
            switch (variant.ToLower())
            {
                case "base":
                    bgClass = "bg-gray-100 text-gray-800 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-100 dark:hover:bg-gray-600";
                    break;
                case "primary":
                    bgClass = "bg-blue-600 text-white hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-500";
                    break;
                case "secondary":
                    bgClass = "bg-gray-600 text-white hover:bg-gray-700 dark:bg-gray-600 dark:hover:bg-gray-500";
                    break;
                case "success":
                    bgClass = "bg-green-600 text-white hover:bg-green-700 dark:bg-green-600 dark:hover:bg-green-500";
                    break;
                case "danger":
                    bgClass = "bg-red-600 text-white hover:bg-red-700 dark:bg-red-600 dark:hover:bg-red-500";
                    break;
                case "warning":
                    bgClass = "bg-yellow-500 text-white hover:bg-yellow-600 dark:bg-yellow-500 dark:hover:bg-yellow-400";
                    break;
                case "info":
                    bgClass = "bg-sky-500 text-white hover:bg-sky-600 dark:bg-sky-500 dark:hover:bg-sky-400";
                    break;
                case "light":
                    bgClass = "bg-white text-gray-800 border border-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:hover:bg-gray-700";
                    break;
                case "dark":
                    bgClass = "bg-gray-800 text-white hover:bg-gray-900 dark:bg-gray-50 dark:text-gray-900 dark:hover:bg-gray-200";
                    break;
                case "ghost":
                    bgClass = "bg-transparent text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800";
                    break;
                case "contrast":
                    bgClass = "bg-white text-black hover:bg-gray-100 dark:bg-black dark:text-white dark:hover:bg-gray-900 border border-gray-300 dark:border-gray-700";
                    break;
                case "outline":
                    bgClass = "bg-transparent border border-blue-600 text-blue-600 hover:bg-blue-50 dark:border-blue-400 dark:text-blue-400 dark:hover:bg-blue-900/20";
                    break;
            }

            // 2. Corners
            var roundedClass = "rounded-md";
            switch (corners.ToLower())
            {
                case "round": roundedClass = "rounded-md"; break;
                case "square": roundedClass = "rounded-none"; break;
                case "pill": roundedClass = "rounded-full"; break;
            }

            // 3. Size
            var sizeClass = "px-4 py-2 text-sm";
            switch (size.ToLower())
            {
                case "xs": sizeClass = "px-2.5 py-1.5 text-xs"; break;
                case "s": sizeClass = "px-3 py-2 text-sm"; break;
                case "m": sizeClass = "px-4 py-2 text-sm"; break;
                case "l": sizeClass = "px-4 py-2 text-base"; break;
                case "xl": sizeClass = "px-6 py-3 text-base"; break;
                case "2xl": sizeClass = "px-6 py-3.5 text-xl"; break;
                case "3xl": sizeClass = "px-7 py-4 text-2xl"; break;
            }

            // Target handling
            var targetAttr = "";
            if (!string.IsNullOrEmpty(target))
            {
                if (target.Equals("blank", System.StringComparison.OrdinalIgnoreCase)) target = "_blank";
                targetAttr = $" target=\"{target}\"";
            }

            renderer.Write($"<a href=\"{link}\"{targetAttr} class=\"inline-flex items-center border border-transparent font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 mr-2 no-underline {bgClass} {roundedClass} {sizeClass}\">");

            if (!string.IsNullOrEmpty(icon))
            {
                if (icon.Trim().StartsWith("<svg", System.StringComparison.OrdinalIgnoreCase))
                {
                     renderer.Write(System.Net.WebUtility.HtmlDecode(icon));
                     renderer.Write(" ");
                }
                else if (icon.StartsWith(":"))
                {
                     // Try to parse inline markdown for emoji or icon shortcode
                     RenderInline(renderer, icon);
                     renderer.Write(" ");
                }
                else
                {
                     renderer.Write($"<i class=\"fi fi-rr-{icon} mr-2\"></i>");
                }
            }

            RenderInline(renderer, text);
            renderer.Write("</a>");
        }

        private void RenderColorChip(HtmlRenderer renderer, ComponentInline obj)
        {
            var color = obj.GetAttribute("color", "#000000");

            // Priority to positional arg
            if (obj.Arguments.Count > 0)
            {
                color = obj.Arguments[0];
            }

            var text = obj.GetAttribute("text", color);

            renderer.Write($"<span class=\"inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300 mr-2 border border-gray-200 dark:border-gray-600\">");
            renderer.Write($"<span class=\"w-3 h-3 rounded-full mr-1.5\" style=\"background-color: {color};\"></span>");
            renderer.Write(text);
            renderer.Write("</span>");
        }

        private void RenderEmojiTable(HtmlRenderer renderer, ComponentInline obj)
        {
            renderer.Write("<div class=\"overflow-x-auto my-4\">");
            renderer.Write("<table class=\"min-w-full divide-y divide-gray-200 dark:divide-gray-700\">");
            renderer.Write("<thead class=\"bg-gray-50 dark:bg-gray-800\">");
            renderer.Write("<tr>");
            renderer.Write("<th class=\"px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider\">Emoji</th>");
            renderer.Write("<th class=\"px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider\">Shortcode</th>");
            renderer.Write("</tr>");
            renderer.Write("</thead>");
            renderer.Write("<tbody class=\"bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700\">");

            // Markdig Emoji Mapping
            foreach (var kvp in Markdig.Extensions.Emoji.EmojiMapping.GetDefaultEmojiShortcodeToUnicode())
            {
                var shortcode = kvp.Key;
                var unicode = kvp.Value;

                renderer.Write("<tr>");
                renderer.Write($"<td class=\"px-6 py-4 whitespace-nowrap text-2xl\">{unicode}</td>");
                renderer.Write($"<td class=\"px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400\"><code>{shortcode}</code></td>");
                renderer.Write("</tr>");
            }

            renderer.Write("</tbody>");
            renderer.Write("</table>");
            renderer.Write("</div>");
        }

        private void RenderInline(HtmlRenderer renderer, string text)
        {
             if (string.IsNullOrEmpty(text)) return;
             // We need a pipeline to parse.
             if (_pipeline != null)
             {
                 // Parse as document but only render inlines of first paragraph
                 var doc = Markdig.Markdown.Parse(text, _pipeline);
                 foreach (var block in doc)
                 {
                     if (block is Markdig.Syntax.ParagraphBlock p)
                     {
                         renderer.Write(p.Inline);
                     }
                     else
                     {
                         renderer.Render(block);
                     }
                 }
                 return;
             }
             renderer.Write(text);
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
                // We need to pass the pipeline to the renderer so it can parse nested markdown (e.g. in button text)
                if (!htmlRenderer.ObjectRenderers.Contains<ComponentRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Add(new ComponentRenderer(pipeline));
                }
            }
        }
    }
}
