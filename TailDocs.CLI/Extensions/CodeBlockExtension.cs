using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace TailDocs.CLI.Extensions
{
    public class CodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
    {
        private readonly HtmlObjectRenderer<CodeBlock> _originalRenderer;

        public CodeBlockRenderer(HtmlObjectRenderer<CodeBlock> originalRenderer)
        {
            _originalRenderer = originalRenderer;
        }

        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            string title = null;

            if (obj is FencedCodeBlock fencedBlock)
            {
                var args = fencedBlock.Arguments;
                if (!string.IsNullOrEmpty(args))
                {
                    // Simple regex for title="filename.cs"
                    var titleMatch = System.Text.RegularExpressions.Regex.Match(args, "title=\"([^\"]+)\"");
                    if (titleMatch.Success)
                    {
                        title = titleMatch.Groups[1].Value;
                    }
                }
            }

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write($"<div class=\"flex items-center gap-2 px-4 py-2 bg-gray-100 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 rounded-t-md text-sm font-mono text-gray-700 dark:text-gray-300\">");
                renderer.Write($"<i class=\"fi fi-rr-file-code\"></i> {title}");
                renderer.Write("</div>");

                // Adjust original attributes to remove rounded top
                // Markdig's HtmlObjectRenderer.WriteAttributes handles GetAttributes().
                obj.GetAttributes().AddClass("rounded-t-none");
                obj.GetAttributes().AddClass("mt-0");
            }

            _originalRenderer.Write(renderer, obj);
        }
    }

    public class CodeBlockExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // Find existing renderer for CodeBlock (which handles FencedCodeBlock too usually)
                var codeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<Markdig.Renderers.Html.CodeBlockRenderer>();
                if (codeBlockRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(codeBlockRenderer);
                    htmlRenderer.ObjectRenderers.Add(new CodeBlockRenderer(codeBlockRenderer));
                }
            }
        }
    }
}
