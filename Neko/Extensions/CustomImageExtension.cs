using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;
using System.Linq;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class CustomImageRenderer : HtmlObjectRenderer<LinkInline>
    {
        private readonly HtmlObjectRenderer<LinkInline> _originalRenderer;

        public CustomImageRenderer(HtmlObjectRenderer<LinkInline> originalRenderer)
        {
            _originalRenderer = originalRenderer;
        }

        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            if (link.IsImage)
            {
                var attributes = link.GetAttributes();
                var properties = attributes.Properties?.ToList();
                string darkSrc = null;
                if (properties != null)
                {
                    var styleParts = new List<string>();
                    var propsToRemove = new List<KeyValuePair<string, string?>>();

                    foreach (var prop in properties)
                    {
                        if (prop.Key == "min-width" || prop.Key == "max-width" ||
                            prop.Key == "min-height" || prop.Key == "max-height")
                        {
                            styleParts.Add($"{prop.Key}: {prop.Value}");
                            propsToRemove.Add(prop);
                        }
                        else if (prop.Key == "src-dark" || prop.Key == "srcDark")
                        {
                            darkSrc = prop.Value;
                            propsToRemove.Add(prop);
                        }
                    }

                    foreach (var prop in propsToRemove)
                    {
                        attributes.Properties?.Remove(prop);
                    }

                    if (styleParts.Count > 0)
                    {
                        var existingStyle = properties.FirstOrDefault(p => p.Key == "style").Value;
                        var newStyle = string.Join("; ", styleParts);
                        if (!string.IsNullOrEmpty(existingStyle))
                        {
                            newStyle = existingStyle + (!existingStyle.EndsWith(";") ? "; " : " ") + newStyle;
                            attributes.Properties?.Remove(new KeyValuePair<string, string?>("style", existingStyle));
                        }
                        attributes.Properties?.Add(new KeyValuePair<string, string?>("style", newStyle));
                    }
                }

                if (!string.IsNullOrEmpty(darkSrc))
                {
                    // Emit two <img> tags so the active theme picks the right one.
                    // The light image keeps every original attribute (including any
                    // alignment classes from ImageAlignmentRenderer) and is hidden
                    // in dark mode; the dark image mirrors those classes but with
                    // the visibility swap so it inherits the same layout.
                    attributes.AddClass("dark:hidden");
                    _originalRenderer.Write(renderer, link);

                    var altText = ExtractAltText(link);
                    var titleAttr = string.IsNullOrEmpty(link.Title) ? "" : $" title=\"{System.Net.WebUtility.HtmlEncode(link.Title)}\"";
                    var sharedClasses = string.Join(" ", (attributes.Classes ?? new List<string>())
                        .Where(c => c != "dark:hidden"));
                    var darkClasses = string.IsNullOrEmpty(sharedClasses)
                        ? "hidden dark:block"
                        : sharedClasses + " hidden dark:block";
                    renderer.Write($"<img src=\"{System.Net.WebUtility.HtmlEncode(darkSrc)}\" alt=\"{System.Net.WebUtility.HtmlEncode(altText)}\"{titleAttr} class=\"{darkClasses}\" />");
                    return;
                }
            }

            _originalRenderer.Write(renderer, link);
        }

        private static string ExtractAltText(LinkInline link)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var child in link)
            {
                if (child is LiteralInline lit) sb.Append(lit.Content.ToString());
            }
            return sb.ToString();
        }
    }

    public class CustomImageExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var originalRenderer = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
                if (originalRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(originalRenderer);
                    htmlRenderer.ObjectRenderers.Insert(0, new CustomImageRenderer(originalRenderer));
                }
                else
                {
                    htmlRenderer.ObjectRenderers.Insert(0, new CustomImageRenderer(new LinkInlineRenderer()));
                }
            }
        }
    }
}
