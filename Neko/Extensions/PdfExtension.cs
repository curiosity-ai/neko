using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;
using System.Linq;
using System.Collections.Generic;
using System.Web;

namespace Neko.Extensions
{
    public class PdfRenderer : HtmlObjectRenderer<LinkInline>
    {
        private readonly HtmlObjectRenderer<LinkInline> _originalRenderer;

        public PdfRenderer(HtmlObjectRenderer<LinkInline> originalRenderer)
        {
            _originalRenderer = originalRenderer;
        }

        protected override void Write(HtmlRenderer renderer, LinkInline link)
        {
            if (link.IsImage && link.Url != null && link.Url.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
            {
                var attributes = link.GetAttributes();
                var style = "width:100%; height:800px;";
                var id = "";
                var cssClass = "";

                if (attributes != null)
                {
                    if (!string.IsNullOrEmpty(attributes.Id))
                    {
                        id = $" id=\"{attributes.Id}\"";
                    }

                    if (attributes.Classes != null && attributes.Classes.Count > 0)
                    {
                        cssClass = $" class=\"{string.Join(" ", attributes.Classes)}\"";
                    }

                    if (attributes.Properties != null)
                    {
                        var propStyle = attributes.Properties.FirstOrDefault(p => p.Key == "style").Value;
                        if (!string.IsNullOrEmpty(propStyle))
                        {
                            style = propStyle;
                        }

                        var width = attributes.Properties.FirstOrDefault(p => p.Key == "width").Value;
                        var height = attributes.Properties.FirstOrDefault(p => p.Key == "height").Value;

                        if (!string.IsNullOrEmpty(width)) style += $"width: {width}{(width.All(char.IsDigit) ? "px" : "")}; ";
                        if (!string.IsNullOrEmpty(height)) style += $"height: {height}{(height.All(char.IsDigit) ? "px" : "")}; ";
                    }
                }

                renderer.Write($"<iframe src=\"https://mozilla.github.io/pdf.js/web/viewer.html?file={HttpUtility.UrlEncode(link.Url)}\"{id}{cssClass} style=\"{style}\" frameborder=\"0\"></iframe>");
            }
            else
            {
                _originalRenderer.Write(renderer, link);
            }
        }
    }

    public class PdfExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var customRenderer = htmlRenderer.ObjectRenderers.OfType<CustomImageRenderer>().FirstOrDefault();
                if (customRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.InsertBefore<CustomImageRenderer>(new PdfRenderer(customRenderer));
                    htmlRenderer.ObjectRenderers.Remove(customRenderer);
                }
                else
                {
                    var originalRenderer = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
                    if (originalRenderer != null)
                    {
                        htmlRenderer.ObjectRenderers.Remove(originalRenderer);
                        htmlRenderer.ObjectRenderers.Insert(0, new PdfRenderer(originalRenderer));
                    }
                    else
                    {
                        htmlRenderer.ObjectRenderers.Insert(0, new PdfRenderer(new LinkInlineRenderer()));
                    }
                }
            }
        }
    }
}
