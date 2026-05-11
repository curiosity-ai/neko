using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using System;
using System.Linq;
using System.Net;

namespace Neko.Extensions
{
    public class CustomContainerRenderer : HtmlObjectRenderer<CustomContainer>
    {
        private readonly HtmlObjectRenderer<CustomContainer> _originalRenderer;
        private readonly Configuration.NekoConfig _config;

        // Curated icon color palettes for variant="grid" cards (curiosity-style).
        private static readonly (string Bg, string Ring, string Text)[] IconPalettes = new[]
        {
            ("bg-blue-500/15",    "ring-blue-500/30",    "text-blue-300"),
            ("bg-violet-500/15",  "ring-violet-500/30",  "text-violet-300"),
            ("bg-emerald-500/15", "ring-emerald-500/30", "text-emerald-300"),
            ("bg-amber-500/15",   "ring-amber-500/30",   "text-amber-300"),
            ("bg-rose-500/15",    "ring-rose-500/30",    "text-rose-300"),
            ("bg-sky-500/15",     "ring-sky-500/30",     "text-sky-300"),
            ("bg-fuchsia-500/15", "ring-fuchsia-500/30", "text-fuchsia-300"),
            ("bg-orange-500/15",  "ring-orange-500/30",  "text-orange-300"),
        };

        public CustomContainerRenderer(HtmlObjectRenderer<CustomContainer> originalRenderer, Configuration.NekoConfig config)
        {
            _originalRenderer = originalRenderer;
            _config = config;
        }

        protected override void Write(HtmlRenderer renderer, CustomContainer obj)
        {
            var type = obj.Info;

            if (type == "card")
            {
                RenderCard(renderer, obj);
                return;
            }

            if (type == "example")
            {
                RenderExample(renderer, obj);
                return;
            }

            if (type == "panel")
            {
                var classes = "bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm p-6 my-4";
                obj.GetAttributes().AddClass(classes);
            }
            else if (type == "column")
            {
                var classes = "flex-1 min-w-0";
                obj.GetAttributes().AddClass(classes);
            }
            else if (type == "columns")
            {
                var classes = "flex flex-col md:flex-row gap-4 my-4";
                obj.GetAttributes().AddClass(classes);
            }
            else if (type == "card-grid")
            {
                var classes = "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 my-8 not-prose";
                obj.GetAttributes().AddClass(classes);
            }

            _originalRenderer.Write(renderer, obj);
        }

        private void RenderCard(HtmlRenderer renderer, CustomContainer obj)
        {
            var attributes = obj.GetAttributes();
            var image = GetAttribute(attributes, "image");
            var title = GetAttribute(attributes, "title");
            var link = GetAttribute(attributes, "link");
            var seeMore = GetAttribute(attributes, "see-more");
            var tags = GetAttribute(attributes, "tags");
            var icon = GetAttribute(attributes, "icon");
            var variant = GetAttribute(attributes, "variant") ?? "stacked";
            var palette = GetAttribute(attributes, "palette");

            if (variant == "horizontal")
            {
                RenderHorizontalCard(renderer, obj, image, title, link, seeMore, tags, icon);
            }
            else if (variant == "grid")
            {
                RenderGridCard(renderer, obj, title, link, icon, palette, seeMore, tags);
            }
            else if (variant == "link")
            {
                var linkText = GetAttribute(attributes, "link-text");
                var theme = GetAttribute(attributes, "theme");
                var arrow = GetAttribute(attributes, "arrow") == "true";
                RenderLinkCard(renderer, obj, title, link, linkText, theme, arrow);
            }
            else
            {
                RenderStackedCard(renderer, obj, image, title, link, seeMore, tags, icon);
            }
        }

        private void RenderExample(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write("<div class=\"grid grid-cols-1 lg:grid-cols-2 gap-8 items-start my-12\">");

            renderer.Write("<div class=\"prose dark:prose-invert max-w-none\">");
            foreach (var block in obj)
            {
                if (!(block is Markdig.Syntax.FencedCodeBlock))
                {
                    renderer.Render(block);
                }
            }
            renderer.Write("</div>");

            renderer.Write("<div class=\"sticky top-8\">");
            foreach (var block in obj)
            {
                if (block is Markdig.Syntax.FencedCodeBlock)
                {
                    renderer.Render(block);
                }
            }
            renderer.Write("</div>");

            renderer.Write("</div>");
        }

        private void RenderLinkCard(HtmlRenderer renderer, CustomContainer obj, string? title, string? link, string? linkText, string? theme, bool arrow)
        {
            var isDark = theme == "dark";
            var baseClasses = "flex flex-col h-full p-6 rounded-xl transition-all duration-300 hover:-translate-y-0.5 not-prose";

            if (isDark)
            {
                baseClasses += " bg-gray-900 text-white shadow-md";
            }
            else
            {
                baseClasses += " bg-white border border-gray-200 shadow-sm hover:shadow-md dark:bg-gray-800 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600";
            }

            var attrs = obj.GetAttributes();
            if (attrs.Classes != null)
            {
                var extraClasses = string.Join(" ", attrs.Classes.Where(c => c != "card"));
                if (!string.IsNullOrEmpty(extraClasses)) baseClasses += " " + extraClasses;
            }

            renderer.Write($"<div class=\"{baseClasses}\">");

            if (!string.IsNullOrEmpty(title))
            {
                var titleClass = isDark ? "text-white" : "text-gray-900 dark:text-white";
                renderer.Write($"<h3 class=\"mb-2 text-xl font-bold tracking-tight {titleClass}\">");
                renderer.Write(WebUtility.HtmlEncode(title));
                renderer.Write("</h3>");
            }

            var textClass = isDark ? "text-gray-300" : "text-gray-600 dark:text-gray-300";
            renderer.Write($"<div class=\"flex-1 mb-8 {textClass} text-sm leading-relaxed\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(linkText) || !string.IsNullOrEmpty(link))
            {
                var displayText = !string.IsNullOrEmpty(linkText) ? linkText : "View docs";
                var url = !string.IsNullOrEmpty(link) ? link : "#";

                var linkClass = isDark
                    ? "text-white hover:text-gray-200"
                    : "text-primary-600 hover:underline dark:text-primary-400";

                renderer.Write($"<a href=\"{WebUtility.HtmlEncode(url)}\" class=\"mt-auto inline-flex items-center font-medium text-sm {linkClass}\">");
                renderer.Write(WebUtility.HtmlEncode(displayText));

                if (arrow)
                {
                    renderer.Write(" <span class=\"ml-1\">&rarr;</span>");
                }
                renderer.Write("</a>");
            }

            renderer.Write("</div>");
        }

        private string? GetAttribute(HtmlAttributes attributes, string key)
        {
            if (attributes.Properties != null)
            {
                foreach (var prop in attributes.Properties)
                {
                    if (prop.Key == key) return prop.Value;
                }
            }
            return null;
        }

        private static (string Bg, string Ring, string Text) ResolvePalette(string? requested, string? title, string? icon)
        {
            if (!string.IsNullOrEmpty(requested))
            {
                var idx = requested.ToLowerInvariant() switch
                {
                    "blue"    => 0,
                    "violet"  => 1,
                    "emerald" => 2,
                    "amber"   => 3,
                    "rose"    => 4,
                    "sky"     => 5,
                    "fuchsia" => 6,
                    "orange"  => 7,
                    _ => -1
                };
                if (idx >= 0) return IconPalettes[idx];
            }

            // Deterministic palette from title/icon for visual variety.
            var seed = (title ?? icon ?? "");
            unchecked
            {
                int hash = 17;
                foreach (var c in seed) hash = hash * 31 + c;
                var i = Math.Abs(hash) % IconPalettes.Length;
                return IconPalettes[i];
            }
        }

        private void RenderGridCard(HtmlRenderer renderer, CustomContainer obj, string? title, string? link, string? icon, string? palette, string? seeMore, string? tags)
        {
            var classes = "group relative flex flex-col h-full rounded-2xl p-6 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:border-primary-400 dark:hover:border-primary-500 hover:shadow-lg overflow-hidden not-prose";
            var attrs = obj.GetAttributes();
            if (attrs.Classes != null)
            {
                var extraClasses = string.Join(" ", attrs.Classes.Where(c => c != "card"));
                if (!string.IsNullOrEmpty(extraClasses)) classes += " " + extraClasses;
            }

            renderer.Write($"<div class=\"{classes}\">");

            // Subtle radial glow background (curiosity-style)
            renderer.Write("<div aria-hidden=\"true\" class=\"pointer-events-none absolute -top-24 -right-24 h-48 w-48 rounded-full bg-primary-500/10 blur-3xl opacity-0 group-hover:opacity-100 transition-opacity duration-500\"></div>");

            if (!string.IsNullOrEmpty(link))
            {
                renderer.Write($"<a href=\"{WebUtility.HtmlEncode(link)}\" class=\"absolute inset-0 z-10\" aria-label=\"{WebUtility.HtmlEncode(title ?? "")}\"></a>");
            }

            // Icon badge
            var (bg, ring, text) = ResolvePalette(palette, title, icon);
            renderer.Write($"<div class=\"relative inline-flex h-11 w-11 items-center justify-center rounded-xl {bg} ring-1 {ring} {text} mb-5\">");
            if (!string.IsNullOrEmpty(icon))
            {
                renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-lg\"></i>");
            }
            else
            {
                renderer.Write("<i class=\"fi fi-rr-square text-lg\"></i>");
            }
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h3 class=\"relative text-lg font-semibold tracking-tight text-gray-900 dark:text-white mb-2\">");
                renderer.Write(WebUtility.HtmlEncode(title));
                renderer.Write("</h3>");
            }

            renderer.Write("<div class=\"relative flex-1 text-sm leading-relaxed text-gray-600 dark:text-gray-400\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(tags) || !string.IsNullOrEmpty(seeMore))
            {
                renderer.Write("<div class=\"relative mt-5 pt-4 border-t border-gray-100 dark:border-gray-700/60 flex items-center justify-between text-xs text-gray-500 dark:text-gray-400\">");

                if (!string.IsNullOrEmpty(tags))
                {
                    renderer.Write("<div class=\"flex flex-wrap gap-1.5\">");
                    foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        var t = WebUtility.HtmlEncode(tag.Trim());
                        renderer.Write($"<span class=\"font-mono\">· {t}</span>");
                    }
                    renderer.Write("</div>");
                }

                if (!string.IsNullOrEmpty(seeMore))
                {
                    var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                    renderer.Write($"<a href=\"{encodedSeeMore}\" class=\"relative z-20 inline-flex items-center font-medium text-primary-600 hover:underline dark:text-primary-400\">More <span aria-hidden=\"true\" class=\"ml-1\">&rarr;</span></a>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>");
        }

        private void RenderStackedCard(HtmlRenderer renderer, CustomContainer obj, string? image, string? title, string? link, string? seeMore, string? tags, string? icon)
        {
            var classes = "max-w-sm rounded-2xl overflow-hidden shadow-sm bg-white dark:bg-gray-800 my-4 border border-gray-200 dark:border-gray-700 transition-all duration-300 hover:-translate-y-0.5 hover:border-primary-400 dark:hover:border-primary-500 hover:shadow-lg not-prose";
            var attrs = obj.GetAttributes();
            if (attrs.Classes != null)
            {
                var extraClasses = string.Join(" ", attrs.Classes.Where(c => c != "card"));
                if (!string.IsNullOrEmpty(extraClasses)) classes += " " + extraClasses;
            }

            renderer.Write($"<div class=\"{classes}\">");

            if (!string.IsNullOrEmpty(image))
            {
                var encodedImage = WebUtility.HtmlEncode(image);
                var encodedTitle = WebUtility.HtmlEncode(title ?? "");
                renderer.Write("<div class=\"w-full relative\">");

                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"absolute inset-0 z-10 w-full h-full block\"></a>");
                }
                renderer.Write($"<img class=\"mt-0 mb-0 w-full object-cover relative z-0\" src=\"{encodedImage}\" alt=\"{encodedTitle}\">");

                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"px-6 py-5\">");
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<div class=\"font-semibold text-lg mt-1 mb-2 text-gray-900 dark:text-white\">");
                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"hover:underline\">");
                }
                renderer.Write(WebUtility.HtmlEncode(title));
                if (!string.IsNullOrEmpty(link)) renderer.Write("</a>");
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"text-gray-600 dark:text-gray-400 text-sm\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(seeMore))
            {
                var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                renderer.Write($"<div class=\"mt-4\"><a href=\"{encodedSeeMore}\" class=\"text-primary-600 dark:text-primary-400 hover:underline text-sm font-medium\">See more &rarr;</a></div>");
            }

            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(tags))
            {
                renderer.Write("<div class=\"px-6 pb-4 -mt-2 flex flex-wrap gap-2\">");
                foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = WebUtility.HtmlEncode(tag.Trim());
                    renderer.Write($"<span class=\"inline-block bg-gray-100 dark:bg-gray-700/60 rounded-full px-2.5 py-0.5 text-xs font-medium text-gray-700 dark:text-gray-300\">#{t}</span>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("</div>");
        }

        private void RenderHorizontalCard(HtmlRenderer renderer, CustomContainer obj, string? image, string? title, string? link, string? seeMore, string? tags, string? icon)
        {
            var classes = "max-w-sm w-full lg:max-w-full lg:flex my-4 rounded-2xl overflow-hidden border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 transition-all duration-300 hover:-translate-y-0.5 hover:border-primary-400 dark:hover:border-primary-500 hover:shadow-lg not-prose";
            var attrs = obj.GetAttributes();
            if (attrs.Classes != null)
            {
                var extraClasses = string.Join(" ", attrs.Classes.Where(c => c != "card"));
                if (!string.IsNullOrEmpty(extraClasses)) classes += " " + extraClasses;
            }

            renderer.Write($"<div class=\"{classes}\">");

            if (!string.IsNullOrEmpty(image))
            {
                var encodedImage = WebUtility.HtmlEncode(image);
                var encodedTitle = WebUtility.HtmlEncode(title ?? "");
                renderer.Write($"<div class=\"h-48 lg:h-auto lg:w-48 flex-none bg-cover text-center overflow-hidden relative\" style=\"background-image: url('{encodedImage}')\" title=\"{encodedTitle}\">");

                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"p-5 flex flex-col justify-between leading-normal w-full\">");
            renderer.Write("<div class=\"mb-6\">");

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<div class=\"text-gray-900 dark:text-white font-semibold text-lg mb-2\">");
                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"hover:underline\">");
                }
                renderer.Write(WebUtility.HtmlEncode(title));
                if (!string.IsNullOrEmpty(link)) renderer.Write("</a>");
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"text-gray-600 dark:text-gray-400 text-sm\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(tags) || !string.IsNullOrEmpty(seeMore))
            {
                renderer.Write("<div class=\"flex items-center justify-between mt-auto\">");

                if (!string.IsNullOrEmpty(tags))
                {
                    renderer.Write("<div class=\"flex flex-wrap gap-2\">");
                    foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        var t = WebUtility.HtmlEncode(tag.Trim());
                        renderer.Write($"<span class=\"text-xs font-medium inline-block py-1 px-2 rounded-full text-primary-700 bg-primary-50 dark:text-primary-300 dark:bg-primary-900/30\">{t}</span>");
                    }
                    renderer.Write("</div>");
                }

                if (!string.IsNullOrEmpty(seeMore))
                {
                    var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                    renderer.Write($"<div class=\"text-sm\"><a href=\"{encodedSeeMore}\" class=\"text-primary-600 dark:text-primary-400 hover:underline font-medium\">See more</a></div>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>");
            renderer.Write("</div>");
        }
    }

    public class CustomContainerExtension : IMarkdownExtension
    {
        private readonly Configuration.NekoConfig _config;

        public CustomContainerExtension(Configuration.NekoConfig config = null)
        {
            _config = config;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var originalRenderer = htmlRenderer.ObjectRenderers.FindExact<HtmlCustomContainerRenderer>();
                if (originalRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(originalRenderer);
                    htmlRenderer.ObjectRenderers.Add(new CustomContainerRenderer(originalRenderer, _config));
                }
            }
        }
    }
}
