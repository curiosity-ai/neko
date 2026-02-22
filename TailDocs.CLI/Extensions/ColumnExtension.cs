using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Collections.Generic;

namespace TailDocs.CLI.Extensions
{
    public class ColumnBlock : ContainerBlock
    {
        public string Title { get; set; }

        public ColumnBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class ColumnGroupBlock : ContainerBlock
    {
        public ColumnGroupBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class ColumnParser : BlockParser
    {
        public ColumnParser()
        {
            OpeningCharacters = new[] { '|' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            // Check for ||| at start
            if (slice.Match("|||"))
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

                var currentContainer = processor.CurrentContainer;

                // If we are in a ColumnBlock, close it.
                if (currentContainer is ColumnBlock)
                {
                    processor.Close(currentContainer);
                    currentContainer = currentContainer.Parent;
                }

                // If we are in a ColumnGroupBlock (after closing ColumnBlock if any)
                if (currentContainer is ColumnGroupBlock)
                {
                    if (!string.IsNullOrEmpty(title))
                    {
                        // Opening next column
                        var columnBlock = new ColumnBlock(this)
                        {
                            Title = title,
                            Column = processor.Column,
                            Span = new SourceSpan(processor.Start, slice.End)
                        };
                        processor.NewBlocks.Push(columnBlock);
                        return BlockState.ContinueDiscard;
                    }
                    else
                    {
                        // Closing group (||| without title)
                        processor.Close(currentContainer);
                        return BlockState.BreakDiscard;
                    }
                }

                // Not in ColumnGroup. Start one.
                if (!string.IsNullOrEmpty(title))
                {
                    var groupBlock = new ColumnGroupBlock(this)
                    {
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    var columnBlock = new ColumnBlock(this)
                    {
                        Title = title,
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    processor.NewBlocks.Push(columnBlock);
                    processor.NewBlocks.Push(groupBlock);

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

            // If we are in a ColumnBlock, we continue unless we see "|||"
            if (block is ColumnBlock)
            {
                var slice = processor.Line;
                if (slice.Match("|||"))
                {
                    // Close the current ColumnBlock
                    return BlockState.None;
                }
                return BlockState.Continue;
            }

            if (block is ColumnGroupBlock group)
            {
                // If the last child is an open ColumnBlock, we must continue (delegate to child)
                if (group.LastChild is ColumnBlock lastColumn && lastColumn.IsOpen)
                {
                    return BlockState.Continue;
                }

                var slice = processor.Line;
                if (slice.Match("|||"))
                {
                    // Let TryOpen handle it
                    return BlockState.Continue;
                }

                // If line is blank, continue (ignore).
                if (processor.IsBlankLine) return BlockState.Continue;

                // If we have a column, we accept content into the group (to be associated with the column later)
                if (group.LastChild is ColumnBlock || (group.LastChild != null && group.Count > 0))
                {
                     return BlockState.Continue;
                }

                return BlockState.None;
            }

            return BlockState.Continue;
        }
    }

    public class ColumnRenderer : HtmlObjectRenderer<ColumnGroupBlock>
    {
        protected override void Write(HtmlRenderer renderer, ColumnGroupBlock obj)
        {
            renderer.Write("<div class=\"flex flex-col md:flex-row gap-4 my-4\">");

            // Collect columns and content
            var columns = new List<(ColumnBlock Column, List<Block> Content)>();
            ColumnBlock currentColumn = null;
            List<Block> currentContent = null;

            void CollectColumns(ColumnGroupBlock group)
            {
                foreach (var child in group)
                {
                    if (child is ColumnBlock col)
                    {
                        currentColumn = col;
                        currentContent = new List<Block>();
                        columns.Add((currentColumn, currentContent));
                        if (col.Count > 0)
                        {
                            foreach (var colChild in col)
                            {
                                currentContent.Add(colChild);
                            }
                        }
                    }
                    else if (child is ColumnGroupBlock nestedGroup)
                    {
                        CollectColumns(nestedGroup);
                    }
                    else if (currentColumn != null)
                    {
                        currentContent.Add(child);
                    }
                }
            }

            CollectColumns(obj);

            foreach (var item in columns)
            {
                var col = item.Column;
                renderer.Write("<div class=\"flex-1 min-w-0\">");

                // Render Title
                if (!string.IsNullOrEmpty(col.Title))
                {
                     renderer.Write("<div class=\"font-bold mb-2\">");
                     renderer.WriteEscape(col.Title);
                     renderer.Write("</div>");
                }

                // Render Content
                foreach (var block in item.Content)
                {
                    renderer.Render(block);
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>");
        }
    }

    public class ColumnExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<ColumnParser>())
            {
                pipeline.BlockParsers.Insert(0, new ColumnParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<ColumnRenderer>();
            }
        }
    }
}
