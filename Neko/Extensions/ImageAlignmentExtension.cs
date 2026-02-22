using Markdig;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;

namespace Neko.Extensions
{
    public class ImageAlignmentRenderer : HtmlObjectRenderer<ParagraphBlock>
    {
        private readonly HtmlObjectRenderer<ParagraphBlock> _originalRenderer;

        public ImageAlignmentRenderer(HtmlObjectRenderer<ParagraphBlock> originalRenderer)
        {
            _originalRenderer = originalRenderer;
        }

        protected override void Write(HtmlRenderer renderer, ParagraphBlock obj)
        {
            if (obj.Inline == null)
            {
                _originalRenderer.Write(renderer, obj);
                return;
            }

            var inlines = obj.Inline.ToList();
            var meaningfulInlines = inlines.Where(x => !(x is LiteralInline l && string.IsNullOrWhiteSpace(l.Content.ToString()))).ToList();

            if (meaningfulInlines.Count == 0 || meaningfulInlines.Count > 3)
            {
                _originalRenderer.Write(renderer, obj);
                return;
            }

            LinkInline image = null;
            LiteralInline leading = null;
            LiteralInline trailing = null;

            if (meaningfulInlines.Count == 1)
            {
                if (meaningfulInlines[0] is LinkInline l && l.IsImage)
                {
                    image = l;
                }
            }
            else if (meaningfulInlines.Count == 2)
            {
                if (meaningfulInlines[0] is LiteralInline l1 && meaningfulInlines[1] is LinkInline l2 && l2.IsImage)
                {
                    leading = l1;
                    image = l2;
                }
                else if (meaningfulInlines[0] is LinkInline l3 && l3.IsImage && meaningfulInlines[1] is LiteralInline l4)
                {
                    image = l3;
                    trailing = l4;
                }
            }
            else if (meaningfulInlines.Count == 3)
            {
                if (meaningfulInlines[0] is LiteralInline l1 &&
                    meaningfulInlines[1] is LinkInline l2 && l2.IsImage &&
                    meaningfulInlines[2] is LiteralInline l3)
                {
                    leading = l1;
                    image = l2;
                    trailing = l3;
                }
            }

            if (image == null)
            {
                _originalRenderer.Write(renderer, obj);
                return;
            }

            // Check dashes
            int leadingDashes = 0;
            int trailingDashes = 0;

            if (leading != null)
            {
                var text = leading.Content.ToString();
                if (!text.All(c => c == '-'))
                {
                    _originalRenderer.Write(renderer, obj);
                    return;
                }
                leadingDashes = text.Length;
            }

            if (trailing != null)
            {
                var text = trailing.Content.ToString();
                if (!text.All(c => c == '-'))
                {
                    _originalRenderer.Write(renderer, obj);
                    return;
                }
                trailingDashes = text.Length;
            }

            // Determine classes
            var classes = "";

            if (leadingDashes == 0 && trailingDashes == 0)
            {
                 // Center (default for standalone image)
                 classes = "mx-auto block";
            }
            else if (leadingDashes == 1 && trailingDashes == 0)
            {
                classes = "float-left mr-4 mb-2";
            }
            else if (leadingDashes == 2 && trailingDashes == 0)
            {
                classes = "float-left -ml-8 mr-4 mb-2";
            }
            else if (leadingDashes == 0 && trailingDashes == 1)
            {
                classes = "float-right ml-4 mb-2";
            }
            else if (leadingDashes == 0 && trailingDashes == 2)
            {
                classes = "float-right -mr-8 ml-4 mb-2";
            }
            else if (leadingDashes == 2 && trailingDashes == 2)
            {
                classes = "mx-auto block -mx-8";
            }
            else
            {
                // Unknown pattern, fallback
                _originalRenderer.Write(renderer, obj);
                return;
            }

            // Add class to image
            image.GetAttributes().AddClass(classes);

            // Render ONLY the image, ignoring paragraph tags and dash literals
            renderer.Write(image);
        }
    }

    public class ImageAlignmentExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var originalRenderer = htmlRenderer.ObjectRenderers.FindExact<ParagraphRenderer>();
                if (originalRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(originalRenderer);
                    htmlRenderer.ObjectRenderers.Add(new ImageAlignmentRenderer(originalRenderer));
                }
                else
                {
                    // If not found, add ours (should be rare)
                    // We need a dummy renderer if original is missing? Or create one.
                    htmlRenderer.ObjectRenderers.Add(new ImageAlignmentRenderer(new ParagraphRenderer()));
                }
            }
        }
    }
}
