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
                    return BlockState.Continue;
                }

                // If line is blank, continue (ignore).
                if (processor.IsBlankLine) return BlockState.Continue;

                // If we have a tab, we accept content into the group (to be associated with the tab later)
                if (group.LastChild is TabBlock || (group.LastChild != null && group.Count > 0)) // Check if any children exist (assuming first is TabBlock)
                {
                     // Actually we just need to know if we started a tab.
                     // LastChild might be Paragraph now.
                     // But we want to allow content.
                     return BlockState.Continue;
                }

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

            // Collect tabs and their contents
            var tabs = new List<(TabBlock Tab, List<Block> Content)>();
            TabBlock currentTab = null;
            List<Block> currentContent = null;

            void CollectTabs(TabGroupBlock group)
            {
                foreach (var child in group)
                {
                    if (child is TabBlock tab)
                    {
                        currentTab = tab;
                        currentContent = new List<Block>();
                        tabs.Add((currentTab, currentContent));
                        // If TabBlock managed to capture children before closing, treat them as content?
                        // Currently we assume it closed early.
                        if (tab.Count > 0)
                        {
                            foreach (var tabChild in tab)
                            {
                                currentContent.Add(tabChild);
                            }
                        }
                    }
                    else if (child is TabGroupBlock nestedGroup)
                    {
                        CollectTabs(nestedGroup);
                    }
                    else if (currentTab != null)
                    {
                        currentContent.Add(child);
                    }
                }
            }

            CollectTabs(obj);

            renderer.Write("<div class=\"my-4 border rounded-md dark:border-gray-700\">");

            // Tab Headers
            renderer.Write("<div class=\"flex border-b bg-gray-50 dark:bg-gray-800 dark:border-gray-700 overflow-x-auto\">");

            int index = 0;
            foreach (var item in tabs)
            {
                var tab = item.Tab;
                var activeClass = index == 0 ? "border-blue-500 text-blue-600 dark:text-blue-400 font-medium" : "border-transparent hover:text-gray-700 dark:hover:text-gray-300 text-gray-500 dark:text-gray-400";
                renderer.Write($"<button class=\"px-4 py-2 border-b-2 focus:outline-none whitespace-nowrap {activeClass}\" onclick=\"openTab(event, '{groupId}', 'tab-{groupId}-{index}')\">");
                renderer.Write(tab.Title);
                renderer.Write("</button>");
                index++;
            }
            renderer.Write("</div>");

            // Tab Contents
            renderer.Write("<div class=\"p-4\">");
            index = 0;
            foreach (var item in tabs)
            {
                var tab = item.Tab;
                var hiddenClass = index == 0 ? "" : "hidden";
                renderer.Write($"<div id=\"tab-{groupId}-{index}\" class=\"tab-content {hiddenClass}\">");

                // Render associated content blocks
                foreach (var block in item.Content)
                {
                    renderer.Render(block);
                }

                renderer.Write("</div>");
                index++;
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
