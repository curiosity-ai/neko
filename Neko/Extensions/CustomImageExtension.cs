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
                    }

                    if (styleParts.Count > 0)
                    {
                        foreach (var prop in propsToRemove)
                        {
                            attributes.Properties?.Remove(prop);
                        }
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
            }

            _originalRenderer.Write(renderer, link);
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
