using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace TailDocs.CLI.Extensions
{
    public class CodeSnippetBlock : LeafBlock
    {
        public string Source { get; set; }
        public string Title { get; set; }

        public CodeSnippetBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class CodeSnippetParser : BlockParser
    {
        public CodeSnippetParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            // Match :::code (with space check or just start)
            if (!slice.Match(":::code"))
            {
                return BlockState.None;
            }

            var start = slice.Start;
            // Move past ":::code"
            slice.Start += 7;

            string source = null;
            string title = null;

            // Simple loop to parse attributes: source="val" title="val"
            // This is brittle and assumes strict key="val" format
            while (!slice.IsEmpty)
            {
                slice.TrimStart();

                // End of line check
                if (slice.Match(":::")) // Closer on same line? Or multiline? Usually inline block directive.
                {
                    // Found end marker, we are done
                    break;
                }

                // Parse key
                var keyStart = slice.Start;
                while (!slice.IsEmpty && slice.CurrentChar != '=' && slice.CurrentChar != ' ' && slice.CurrentChar != ':')
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar != '=')
                {
                    // Failed to find =, skip
                    if (slice.CurrentChar == ' ') { slice.NextChar(); continue; }
                    break;
                }

                var key = slice.Text.Substring(keyStart, slice.Start - keyStart).Trim();
                slice.NextChar(); // Skip =

                if (slice.CurrentChar != '"')
                {
                    // Expect quote
                    continue;
                }
                slice.NextChar(); // Skip "

                var valStart = slice.Start;
                while (!slice.IsEmpty && slice.CurrentChar != '"')
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar != '"') break; // unterminated string

                var val = slice.Text.Substring(valStart, slice.Start - valStart);
                slice.NextChar(); // Skip "

                if (key.ToLower() == "source") source = val;
                if (key.ToLower() == "title") title = val;
            }

            var block = new CodeSnippetBlock(this)
            {
                Source = source,
                Title = title,
                Span = new SourceSpan(processor.Start, slice.End)
            };
            processor.NewBlocks.Push(block);

            return BlockState.BreakDiscard;
        }
    }

    public class CodeSnippetRenderer : HtmlObjectRenderer<CodeSnippetBlock>
    {
        protected override void Write(HtmlRenderer renderer, CodeSnippetBlock obj)
        {
            var sourceLink = string.IsNullOrEmpty(obj.Source) ? "#" : obj.Source;
            var title = !string.IsNullOrEmpty(obj.Title) ? obj.Title : (!string.IsNullOrEmpty(obj.Source) ? System.IO.Path.GetFileName(obj.Source) : "Snippet");

            renderer.Write("<div class=\"my-4 border border-gray-200 dark:border-gray-700 rounded-md overflow-hidden\">");

            // Header
            renderer.Write("<div class=\"bg-gray-100 dark:bg-gray-800 px-4 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between\">");
            renderer.Write($"<span class=\"font-mono text-sm text-gray-700 dark:text-gray-300 flex items-center\"><i class=\"fi fi-rr-file-code mr-2\"></i>{title}</span>");
            renderer.Write($"<a href=\"{sourceLink}\" class=\"text-xs text-blue-600 dark:text-blue-400 hover:underline\">View Source</a>");
            renderer.Write("</div>");

            // Content
            renderer.Write("<div class=\"p-4 bg-gray-50 dark:bg-gray-900 overflow-x-auto\">");
            // Placeholder content since we can't easily read file here without IO context
            renderer.Write($"<pre><code class=\"language-csharp\">// Content of {sourceLink} would be displayed here.\n// (File reading not fully integrated)</code></pre>");
            renderer.Write("</div>");

            renderer.Write("</div>");
        }
    }

    public class CodeSnippetExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<CodeSnippetParser>())
            {
                pipeline.BlockParsers.Insert(0, new CodeSnippetParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<CodeSnippetRenderer>();
            }
        }
    }
}
