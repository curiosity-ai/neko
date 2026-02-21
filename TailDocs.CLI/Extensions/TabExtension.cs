using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Collections.Generic;

namespace TailDocs.CLI.Extensions
{
    public class TabBlock : ContainerBlock
    {
        public string Title { get; set; }

        public TabBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class TabGroupBlock : ContainerBlock
    {
        public TabGroupBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class TabParser : BlockParser
    {
        public TabParser()
        {
            OpeningCharacters = new[] { '+' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            // Check for +++ at start
            if (slice.Match("+++"))
            {
                var start = slice.Start;
                slice.Start += 3;

                // Parse title
                string title = null;
                slice.TrimStart();
                if (!slice.IsEmpty)
                {
                    title = slice.ToString();
                }

                // If we are in a TabBlock, close it.
                if (processor.CurrentContainer is TabBlock)
                {
                    processor.Close(processor.CurrentContainer);
                }

                // If we are in a TabGroupBlock (after closing TabBlock if any)
                if (processor.CurrentContainer is TabGroupBlock)
                {
                    if (!string.IsNullOrEmpty(title))
                    {
                        // Opening next tab
                        var tabBlock = new TabBlock(this)
                        {
                            Title = title,
                            Column = processor.Column,
                            Span = new SourceSpan(processor.Start, slice.End)
                        };
                        processor.NewBlocks.Push(tabBlock);
                        return BlockState.ContinueDiscard;
                    }
                    else
                    {
                        // Closing group (+++ without title)
                        processor.Close(processor.CurrentContainer);
                        return BlockState.BreakDiscard;
                    }
                }

                // Not in TabGroup. Start one.
                if (!string.IsNullOrEmpty(title))
                {
                    var tabGroup = new TabGroupBlock(this)
                    {
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    var tabBlock = new TabBlock(this)
                    {
                        Title = title,
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    processor.NewBlocks.Push(tabBlock);
                    processor.NewBlocks.Push(tabGroup);

                    return BlockState.ContinueDiscard;
                }
            }

            return BlockState.None;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.Continue;
            }

            // If we are in a TabBlock, we continue unless we see "+++"
            if (block is TabBlock)
            {
                var slice = processor.Line;
                if (slice.Match("+++"))
                {
                    // Close the current TabBlock
                    return BlockState.None;
                }
                return BlockState.Continue;
            }

            if (block is TabGroupBlock group)
            {
                // If the last child is an open TabBlock, we must continue (delegate to child)
                if (group.LastChild is TabBlock lastTab && lastTab.IsOpen)
                {
                    return BlockState.Continue;
                }

                var slice = processor.Line;
                if (slice.Match("+++"))
                {
                    // Let TryOpen handle it
                    return BlockState.None;
                }

                // If line is blank, continue (ignore).
                if (processor.IsBlankLine) return BlockState.Continue;

                // If line is content (not +++), and we are in TabGroup (no active Tab), then syntax is invalid or group ended.
                // We should close the group.
                return BlockState.None;
            }

            return BlockState.Continue;
        }
    }

    public class TabRenderer : HtmlObjectRenderer<TabGroupBlock>
    {
        protected override void Write(HtmlRenderer renderer, TabGroupBlock obj)
        {
            var groupId = System.Guid.NewGuid().ToString("N");

            renderer.Write("<div class=\"my-4 border rounded-md dark:border-gray-700\">");

            // Tab Headers
            renderer.Write("<div class=\"flex border-b bg-gray-50 dark:bg-gray-800 dark:border-gray-700 overflow-x-auto\">");

            int index = 0;
            foreach (var child in obj)
            {
                if (child is TabBlock tab)
                {
                    var activeClass = index == 0 ? "border-blue-500 text-blue-600 dark:text-blue-400 font-medium" : "border-transparent hover:text-gray-700 dark:hover:text-gray-300 text-gray-500 dark:text-gray-400";
                    renderer.Write($"<button class=\"px-4 py-2 border-b-2 focus:outline-none whitespace-nowrap {activeClass}\" onclick=\"openTab(event, '{groupId}', 'tab-{groupId}-{index}')\">");
                    renderer.Write(tab.Title);
                    renderer.Write("</button>");
                    index++;
                }
            }
            renderer.Write("</div>");

            // Tab Contents
            renderer.Write("<div class=\"p-4\">");
            index = 0;
            foreach (var child in obj)
            {
                if (child is TabBlock tab)
                {
                    var hiddenClass = index == 0 ? "" : "hidden";
                    renderer.Write($"<div id=\"tab-{groupId}-{index}\" class=\"tab-content {hiddenClass}\">");
                    renderer.WriteChildren(tab);
                    renderer.Write("</div>");
                    index++;
                }
            }
            renderer.Write("</div>");
            renderer.Write("</div>");
        }
    }

    public class TabExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<TabParser>())
            {
                pipeline.BlockParsers.Insert(0, new TabParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<TabRenderer>();
            }
        }
    }
}
