using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using System.Linq;
using System.Net;

namespace Neko.Extensions
{
    public class CustomContainerRenderer : HtmlObjectRenderer<CustomContainer>
    {
        private readonly HtmlObjectRenderer<CustomContainer> _originalRenderer;
        private readonly Configuration.NekoConfig _config;

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

            // Apply styles based on container type
            // Note: UseAddClass helper from Markdig handles appending to existing classes.
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
                var classes = "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 my-8";
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

            // Gradient attributes
            var gradientModeAttr = GetAttribute(attributes, "gradient-mode");
            var gradient = GetAttribute(attributes, "gradient") == "true" || gradientModeAttr != null || !string.IsNullOrEmpty(_config?.Theme?.Gradient?.Mode);
            var gradientMode = gradientModeAttr ?? _config?.Theme?.Gradient?.Mode;
            var gradientNoise = GetAttribute(attributes, "gradient-noise") ?? _config?.Theme?.Gradient?.Noise;
            var gradientSpeed = GetAttribute(attributes, "gradient-speed") ?? _config?.Theme?.Gradient?.Speed;
            var gradientColors = GetAttribute(attributes, "gradient-colors") ?? _config?.Theme?.Gradient?.Colors;

            if (variant == "horizontal")
            {
                RenderHorizontalCard(renderer, obj, image, title, link, seeMore, tags, icon, gradient, gradientMode, gradientNoise, gradientSpeed, gradientColors);
            }
            else if (variant == "grid")
            {
                RenderGridCard(renderer, obj, image, title, link, seeMore, tags, icon, gradient, gradientMode, gradientNoise, gradientSpeed, gradientColors);
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
                RenderStackedCard(renderer, obj, image, title, link, seeMore, tags, icon, gradient, gradientMode, gradientNoise, gradientSpeed, gradientColors);
            }
        }

        private void RenderExample(HtmlRenderer renderer, CustomContainer obj)
        {
            // The "example" component encompasses a markdown section for the left side
            // and a code section for the right side.
            renderer.Write("<div class=\"grid grid-cols-1 lg:grid-cols-2 gap-8 items-start my-12\">");

            // Left side: Non-code blocks
            renderer.Write("<div class=\"prose dark:prose-invert max-w-none\">");
            foreach (var block in obj)
            {
                if (!(block is Markdig.Syntax.FencedCodeBlock))
                {
                    renderer.Render(block);
                }
            }
            renderer.Write("</div>");

            // Right side: Code blocks
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
            var baseClasses = "flex flex-col h-full p-6 rounded-lg transition-all duration-300 hover:-translate-y-1";

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

        private void RenderGridCard(HtmlRenderer renderer, CustomContainer obj, string? image, string? title, string? link, string? seeMore, string? tags, string? icon, bool gradient, string? gradientMode, string? gradientNoise, string? gradientSpeed, string? gradientColors)
        {
            var classes = "flex flex-col h-full rounded-lg border border-gray-200 bg-white shadow-md dark:border-gray-700 dark:bg-gray-800 overflow-hidden transition-all duration-300 hover:-translate-y-1 hover:border-gray-300 dark:hover:border-gray-600";
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

                renderer.Write("<div class=\"flex h-48 w-full items-center justify-center bg-gray-50 dark:bg-white p-6 relative\">");

                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"flex h-full w-full items-center justify-center absolute inset-0 z-10\"></a>");
                }

                renderer.Write($"<img class=\"max-h-full max-w-full object-contain relative z-0\" src=\"{encodedImage}\" alt=\"{encodedTitle}\">");

                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                }

                renderer.Write("</div>");
            }
            else if (gradient)
            {
                var encodedTitle = WebUtility.HtmlEncode(title ?? "");
                renderer.Write("<div class=\"flex h-48 w-full items-center justify-center bg-gray-50 dark:bg-white relative\">");

                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"flex h-full w-full items-center justify-center absolute inset-0 z-10\" title=\"{encodedTitle}\"></a>");
                }

                renderer.Write("<div data-lumina-gradient");
                if (!string.IsNullOrEmpty(gradientMode)) renderer.Write($" data-mode=\"{WebUtility.HtmlEncode(gradientMode)}\"");
                if (!string.IsNullOrEmpty(gradientNoise)) renderer.Write($" data-noise=\"{WebUtility.HtmlEncode(gradientNoise)}\"");
                if (!string.IsNullOrEmpty(gradientSpeed)) renderer.Write($" data-speed=\"{WebUtility.HtmlEncode(gradientSpeed)}\"");
                if (!string.IsNullOrEmpty(gradientColors)) renderer.Write($" data-colors=\"{WebUtility.HtmlEncode(gradientColors).Replace("\"", "&quot;")}\"");
                renderer.Write(" style=\"width: 100%; height: 100%; position: absolute; inset: 0; z-index: 0;\"></div>");

                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"flex flex-1 flex-col p-6\">");

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h3 class=\"text-xl mt-1 mb-2 font-bold tracking-tight text-gray-900 dark:text-white\">");
                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"hover:underline\">");
                }
                renderer.Write(WebUtility.HtmlEncode(title));
                if (!string.IsNullOrEmpty(link)) renderer.Write("</a>");
                renderer.Write("</h3>");
            }

            renderer.Write("<div class=\"flex-1 text-gray-700 dark:text-gray-400\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(tags) || !string.IsNullOrEmpty(seeMore))
            {
                renderer.Write("<div class=\"mt-auto pt-4 flex items-center justify-between\">");

                if (!string.IsNullOrEmpty(tags))
                {
                    renderer.Write("<div class=\"flex flex-wrap gap-2\">");
                    foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        var t = WebUtility.HtmlEncode(tag.Trim());
                        renderer.Write($"<span class=\"inline-block rounded-full bg-gray-100 px-2.5 py-0.5 text-xs font-medium text-gray-800 dark:bg-gray-700 dark:text-gray-300\">#{t}</span>");
                    }
                    renderer.Write("</div>");
                }

                if (!string.IsNullOrEmpty(seeMore))
                {
                    var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                    renderer.Write($"<a href=\"{encodedSeeMore}\" class=\"inline-flex items-center text-sm font-medium text-primary-600 hover:underline dark:text-primary-500\">See more &rarr;</a>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>"); // End content div
            renderer.Write("</div>"); // End card div
        }

        private void RenderStackedCard(HtmlRenderer renderer, CustomContainer obj, string? image, string? title, string? link, string? seeMore, string? tags, string? icon, bool gradient, string? gradientMode, string? gradientNoise, string? gradientSpeed, string? gradientColors)
        {
            var classes = "max-w-sm rounded overflow-hidden shadow-lg bg-white dark:bg-gray-800 my-4 border border-gray-200 dark:border-gray-700 transition-all duration-300 hover:-translate-y-1 hover:border-gray-300 dark:hover:border-gray-600";
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
            else if (gradient)
            {
                var encodedTitle = WebUtility.HtmlEncode(title ?? "");
                renderer.Write("<div class=\"w-full h-48 relative\">");

                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"flex h-full w-full items-center justify-center absolute inset-0 z-10\" title=\"{encodedTitle}\"></a>");
                }

                renderer.Write("<div data-lumina-gradient");
                if (!string.IsNullOrEmpty(gradientMode)) renderer.Write($" data-mode=\"{WebUtility.HtmlEncode(gradientMode)}\"");
                if (!string.IsNullOrEmpty(gradientNoise)) renderer.Write($" data-noise=\"{WebUtility.HtmlEncode(gradientNoise)}\"");
                if (!string.IsNullOrEmpty(gradientSpeed)) renderer.Write($" data-speed=\"{WebUtility.HtmlEncode(gradientSpeed)}\"");
                if (!string.IsNullOrEmpty(gradientColors)) renderer.Write($" data-colors=\"{WebUtility.HtmlEncode(gradientColors).Replace("\"", "&quot;")}\"");
                renderer.Write(" style=\"width: 100%; height: 100%; position: absolute; inset: 0; z-index: 0;\"></div>");

                if (!string.IsNullOrEmpty(icon))
                {
                    renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"px-6 py-4\">");
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<div class=\"font-bold text-xl mt-1 mb-2 text-gray-900 dark:text-white\">");
                if (!string.IsNullOrEmpty(link))
                {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"hover:underline\">");
                }
                renderer.Write(WebUtility.HtmlEncode(title));
                if (!string.IsNullOrEmpty(link)) renderer.Write("</a>");
                renderer.Write("</div>");
            }

            renderer.Write("<div class=\"text-gray-700 dark:text-gray-300 text-base\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(seeMore))
            {
                var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                renderer.Write($"<div class=\"mt-4\"><a href=\"{encodedSeeMore}\" class=\"text-primary-600 dark:text-primary-400 hover:underline\">See more &rarr;</a></div>");
            }

            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(tags))
            {
                renderer.Write("<div class=\"px-6 pt-4 pb-2\">");
                foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = WebUtility.HtmlEncode(tag.Trim());
                    renderer.Write($"<span class=\"inline-block bg-gray-200 dark:bg-gray-700 rounded-full px-3 py-1 text-sm font-semibold text-gray-700 dark:text-gray-300 mr-2 mb-2\">#{t}</span>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("</div>");
        }

        private void RenderHorizontalCard(HtmlRenderer renderer, CustomContainer obj, string? image, string? title, string? link, string? seeMore, string? tags, string? icon, bool gradient, string? gradientMode, string? gradientNoise, string? gradientSpeed, string? gradientColors)
        {
             var classes = "max-w-sm w-full lg:max-w-full lg:flex my-4 shadow-lg rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 transition-all duration-300 hover:-translate-y-1 hover:border-gray-300 dark:hover:border-gray-600";
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
                 renderer.Write($"<div class=\"h-48 lg:h-auto lg:w-48 flex-none bg-cover rounded-t lg:rounded-t-none lg:rounded-l text-center overflow-hidden relative\" style=\"background-image: url('{encodedImage}')\" title=\"{encodedTitle}\">");

                 if (!string.IsNullOrEmpty(icon))
                 {
                     renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                 }

                 renderer.Write("</div>");
             }
             else if (gradient)
             {
                 var encodedTitle = WebUtility.HtmlEncode(title ?? "");
                 renderer.Write($"<div class=\"h-48 lg:h-auto lg:w-48 flex-none rounded-t lg:rounded-t-none lg:rounded-l text-center overflow-hidden relative\" title=\"{encodedTitle}\">");

                 renderer.Write("<div data-lumina-gradient");
                 if (!string.IsNullOrEmpty(gradientMode)) renderer.Write($" data-mode=\"{WebUtility.HtmlEncode(gradientMode)}\"");
                 if (!string.IsNullOrEmpty(gradientNoise)) renderer.Write($" data-noise=\"{WebUtility.HtmlEncode(gradientNoise)}\"");
                 if (!string.IsNullOrEmpty(gradientSpeed)) renderer.Write($" data-speed=\"{WebUtility.HtmlEncode(gradientSpeed)}\"");
                 if (!string.IsNullOrEmpty(gradientColors)) renderer.Write($" data-colors=\"{WebUtility.HtmlEncode(gradientColors).Replace("\"", "&quot;")}\"");
                 renderer.Write(" style=\"width: 100%; height: 100%; position: absolute; inset: 0; z-index: 0;\"></div>");

                 if (!string.IsNullOrEmpty(icon))
                 {
                     renderer.Write($"<div class=\"absolute inset-0 flex items-center justify-center pointer-events-none z-20\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(WebUtility.HtmlEncode(icon))} text-white text-6xl drop-shadow-md\"></i></div>");
                 }

                 renderer.Write("</div>");
             }

             renderer.Write("<div class=\"p-4 flex flex-col justify-between leading-normal w-full\">");

             renderer.Write("<div class=\"mb-8\">");

             if (!string.IsNullOrEmpty(title))
             {
                 renderer.Write("<div class=\"text-gray-900 dark:text-white font-bold text-xl mb-2\">");
                 if (!string.IsNullOrEmpty(link))
                 {
                    var encodedLink = WebUtility.HtmlEncode(link);
                    renderer.Write($"<a href=\"{encodedLink}\" class=\"hover:underline\">");
                 }
                 renderer.Write(WebUtility.HtmlEncode(title));
                 if (!string.IsNullOrEmpty(link)) renderer.Write("</a>");
                 renderer.Write("</div>");
             }

             renderer.Write("<div class=\"text-gray-700 dark:text-gray-300 text-base\">");
             renderer.WriteChildren(obj);
             renderer.Write("</div>");

             renderer.Write("</div>");

             if (!string.IsNullOrEmpty(tags) || !string.IsNullOrEmpty(seeMore))
             {
                 renderer.Write("<div class=\"flex items-center justify-between mt-auto\">");

                 if (!string.IsNullOrEmpty(tags))
                 {
                     renderer.Write("<div class=\"flex flex-wrap\">");
                     foreach (var tag in tags.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
                     {
                         var t = WebUtility.HtmlEncode(tag.Trim());
                         renderer.Write($"<span class=\"text-xs font-semibold inline-block py-1 px-2 uppercase rounded text-primary-600 bg-primary-200 dark:text-primary-400 dark:bg-primary-900 last:mr-0 mr-1\">{t}</span>");
                     }
                     renderer.Write("</div>");
                 }

                 if (!string.IsNullOrEmpty(seeMore))
                 {
                     var encodedSeeMore = WebUtility.HtmlEncode(seeMore);
                     renderer.Write($"<div class=\"text-sm\"><a href=\"{encodedSeeMore}\" class=\"text-primary-600 dark:text-primary-400 hover:underline\">See more</a></div>");
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
                // Find the default HtmlCustomContainerRenderer
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
