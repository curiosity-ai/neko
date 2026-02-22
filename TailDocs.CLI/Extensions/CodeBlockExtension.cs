using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Text.RegularExpressions;

namespace TailDocs.CLI.Extensions
{
    public class CodeBlockRenderer : Markdig.Renderers.Html.CodeBlockRenderer
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            // Only enhance FencedCodeBlocks (```)
            if (obj is not FencedCodeBlock fencedBlock)
            {
                base.Write(renderer, obj);
                return;
            }

            // Handle Mermaid
            if ((fencedBlock.Info ?? "").ToLower() == "mermaid")
            {
                renderer.Write("<div class=\"mermaid flex justify-center bg-gray-50 dark:bg-gray-800 p-4 rounded-md border border-gray-200 dark:border-gray-700 overflow-x-auto my-6\">");
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var slices = leafBlock.Lines;
                    for (int i = 0; i < slices.Count; i++)
                    {
                        var slice = slices.Lines[i].Slice;
                        if (slice.Text == null) continue;
                        renderer.WriteEscape(slice.ToString());
                        renderer.Write("\n");
                    }
                }
                renderer.Write("</div>");
                return;
            }

            var args = fencedBlock.Arguments ?? "";
            string title = null;
            string highlight = null;

            // 1. Extract Highlight (#1-5,7) or {1-5,7}
            var highlightMatch = Regex.Match(args, @"#([\d,-]+)");
            if (highlightMatch.Success)
            {
                highlight = highlightMatch.Groups[1].Value;
                args = args.Replace(highlightMatch.Value, "").Trim();
            }
            else
            {
                var curlyHighlightMatch = Regex.Match(args, @"\{([\d,-]+)\}");
                if (curlyHighlightMatch.Success)
                {
                    highlight = curlyHighlightMatch.Groups[1].Value;
                    args = args.Replace(curlyHighlightMatch.Value, "").Trim();
                }
            }

            // 2. Extract Title (remaining string or title="...")
            var titleMatch = Regex.Match(args, "title=\"([^\"]+)\"");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
            }
            else if (!string.IsNullOrWhiteSpace(args))
            {
                title = args.Trim();
            }

            // Wrapper
            renderer.Write("<div class=\"relative group my-6 bg-gray-50 dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700 not-prose [&>pre]:!m-0 [&>pre]:!p-0 [&>pre]:!bg-transparent [&>pre]:!rounded-none\">");

            // Header (if title exists)
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<div class=\"flex items-center justify-between px-4 py-2 border-b border-gray-200 dark:border-gray-700 bg-gray-100 dark:bg-gray-800 rounded-t-md\">");
                renderer.Write($"<span class=\"font-mono text-sm text-gray-700 dark:text-gray-300\">{title}</span>");

                // Copy Button in Header
                renderer.Write("<button class=\"copy-btn p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-colors rounded hover:bg-gray-200 dark:hover:bg-gray-700\" title=\"Copy to clipboard\">");
                renderer.Write("<i class=\"fi fi-rr-copy\"></i>");
                renderer.Write("</button>");

                renderer.Write("</div>");

                // Adjust code block styles to not have rounded top if header exists
                // We'll wrap the code in a div with proper padding
            }
            else
            {
                // Copy Button Overlay (no header)
                renderer.Write("<button class=\"copy-btn absolute top-2 right-2 p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 bg-white dark:bg-gray-900 bg-opacity-80 dark:bg-opacity-80 backdrop-blur opacity-0 group-hover:opacity-100 transition-opacity rounded border border-gray-200 dark:border-gray-700 shadow-sm\" title=\"Copy to clipboard\">");
                renderer.Write("<i class=\"fi fi-rr-copy\"></i>");
                renderer.Write("</button>");
            }

            // Code Content Wrapper
            // We strip the default <pre> styles Markdig might add if we rely on our wrapper.
            // Markdig's base.Write adds <pre><code ...
            // We want to add attributes to <pre> or <code>?
            // Actually, base.Write adds attributes from obj.GetAttributes() to the <pre> tag.

            var attributes = obj.GetAttributes();

            // Add custom class for styling the inner pre/code
            attributes.AddClass("!my-0 !rounded-none !bg-transparent !border-0 overflow-x-auto p-4 font-mono text-sm"); // Override prose defaults

            // Store highlight info in data attribute
            if (!string.IsNullOrEmpty(highlight))
            {
                attributes.AddProperty("data-highlight", highlight);
            }

            // Write the actual code
            // Note: base.Write writes <pre><code>...</code></pre>
            base.Write(renderer, obj);

            renderer.Write("</div>"); // End Wrapper
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
                var codeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<Markdig.Renderers.Html.CodeBlockRenderer>();
                if (codeBlockRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(codeBlockRenderer);
                    htmlRenderer.ObjectRenderers.Add(new CodeBlockRenderer());
                }
                else
                {
                     htmlRenderer.ObjectRenderers.Add(new CodeBlockRenderer());
                }
            }
        }
    }
}
