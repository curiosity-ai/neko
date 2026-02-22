using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Neko.Extensions
{
    public class CustomContainerRenderer : HtmlObjectRenderer<CustomContainer>
    {
        private readonly HtmlObjectRenderer<CustomContainer> _originalRenderer;

        public CustomContainerRenderer(HtmlObjectRenderer<CustomContainer> originalRenderer)
        {
            _originalRenderer = originalRenderer;
        }

        protected override void Write(HtmlRenderer renderer, CustomContainer obj)
        {
            var type = obj.Info;

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

            _originalRenderer.Write(renderer, obj);
        }
    }

    public class CustomContainerExtension : IMarkdownExtension
    {
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
                    htmlRenderer.ObjectRenderers.Add(new CustomContainerRenderer(originalRenderer));
                }
            }
        }
    }
}
