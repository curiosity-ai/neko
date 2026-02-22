using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    public class PanelBlock : ContainerBlock
    {
        public string Title { get; set; }
        public bool IsCollapsed { get; set; }

        public PanelBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class PanelParser : BlockParser
    {
        public PanelParser()
        {
            OpeningCharacters = new[] { '=' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            // Match === (expanded) or ==- (collapsed)
            bool isCollapsed = false;

            if (slice.Match("==="))
            {
                isCollapsed = false;
            }
            else if (slice.Match("==-"))
            {
                isCollapsed = true;
            }
            else
            {
                return BlockState.None;
            }

            // Move past marker
            slice.Start += 3;

            // Parse title
            string title = null;
            slice.TrimStart();
            if (!slice.IsEmpty)
            {
                title = slice.ToString();
            }

            var currentContainer = processor.CurrentContainer;

            // If we are in a PanelBlock, close it?
            if (currentContainer is PanelBlock)
            {
                processor.Close(currentContainer);
                // currentContainer becomes Parent (likely Document or CustomContainer)
            }

            // If title is empty, it might be a closing fence for a panel
            if (string.IsNullOrEmpty(title))
            {
                // If we match === alone on a line, it might be H1 (Setext).
                // However, since we inserted this parser at index 0, we take precedence.
                // Standard Setext requires previous line to be text.
                // If this is start of block, it can't be Setext.
                // If previous block was text (Paragraph), Setext parser would have handled it?
                // Actually Setext H1 is parsed by HeadingParser which looks for === on NEXT line.
                // If we consume === here, HeadingParser won't see it?
                // This is risky. We might break H1.

                // Setext Heading:
                // Title
                // ===

                // Our parser runs on the line `===`.
                // Processor gives us the line.
                // If we return BlockState.BreakDiscard, we consume the line?

                // If we are NOT closing a panel (no active panel), and we see `===`, and previous block was Paragraph...
                // We should probably NOT consume it if it looks like Setext.
                // But Markdig's BlockProcessor logic is complex.

                // Assuming we only support === as Panel delimiter if it has title OR if it closes an active Panel.
                // If there is no active Panel, and no title, we should probably ignore it (return None) to let HeadingParser handle it.

                if (!(currentContainer is PanelBlock))
                {
                    // Check if previous line was non-empty (Setext candidate)?
                    // But we don't have easy access to previous block type here easily unless we check LastBlock.

                    // Safe bet: If no title and not in Panel, return None.
                    return BlockState.None;
                }

                return BlockState.BreakDiscard;
            }

            // Open new PanelBlock
            var panelBlock = new PanelBlock(this)
            {
                Title = title,
                IsCollapsed = isCollapsed,
                Column = processor.Column,
                Span = new SourceSpan(processor.Start, slice.End)
            };
            processor.NewBlocks.Push(panelBlock);

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.Continue;
            }

            // If we are in a PanelBlock, we check for === or ==- to close/start new.
            if (block is PanelBlock)
            {
                var slice = processor.Line;
                if (slice.Match("===") || slice.Match("==-"))
                {
                    // Let TryOpen handle it (it will close current and open new)
                    return BlockState.None;
                }
                return BlockState.Continue;
            }

            return BlockState.Continue;
        }
    }

    public class PanelRenderer : HtmlObjectRenderer<PanelBlock>
    {
        protected override void Write(HtmlRenderer renderer, PanelBlock obj)
        {
            var openAttr = obj.IsCollapsed ? "" : " open";
            renderer.Write($"<details class=\"group panel my-4 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-800 shadow-sm\"{openAttr}>");

            // Render Summary (Title)
            renderer.Write("<summary class=\"flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-800/50 cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors list-none\">");

            renderer.Write("<span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\">");

            // Process title for icons
            var title = obj.Title;
            // Regex for :icon-name:
            title = Regex.Replace(title, @":icon-([a-zA-Z0-9-]+):", m => $"<i class=\"fi fi-rr-{m.Groups[1].Value} align-middle\"></i>");

            renderer.Write(title);
            renderer.Write("</span>");

            renderer.Write("<i class=\"fi fi-rr-angle-small-down text-gray-400 transition-transform duration-200 ease-in-out group-open:rotate-180\"></i>");

            renderer.Write("</summary>");

            // Render Content
            renderer.Write("<div class=\"p-4 border-t border-gray-200 dark:border-gray-700 prose dark:prose-invert max-w-none\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            renderer.Write("</details>");
        }
    }

    public class PanelExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<PanelParser>())
            {
                pipeline.BlockParsers.Insert(0, new PanelParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<PanelRenderer>();
            }
        }
    }
}
