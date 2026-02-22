using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class PanelBlock : ContainerBlock
    {
        public string Title { get; set; }
        public bool IsExpanded { get; set; } = true;

        public PanelBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class PanelGroupBlock : ContainerBlock
    {
        public PanelGroupBlock(BlockParser parser) : base(parser)
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
            bool isExpanded = false;

            // Check for === or ==-
            if (slice.Match("==="))
            {
                isExpanded = true;
            }
            else if (slice.Match("==-"))
            {
                isExpanded = false;
            }
            else
            {
                return BlockState.None;
            }

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

            // If we are in a PanelBlock, close it.
            if (currentContainer is PanelBlock)
            {
                processor.Close(currentContainer);
                currentContainer = currentContainer.Parent;
            }

            // If we are in a PanelGroupBlock (after closing PanelBlock if any)
            if (currentContainer is PanelGroupBlock)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    // Opening next panel
                    var panelBlock = new PanelBlock(this)
                    {
                        Title = title,
                        IsExpanded = isExpanded,
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    processor.NewBlocks.Push(panelBlock);
                    return BlockState.ContinueDiscard;
                }
                else
                {
                    // Closing group (=== or ==- without title)
                    processor.Close(currentContainer);
                    return BlockState.BreakDiscard;
                }
            }

            // Not in PanelGroup. Start one only if we have a title.
            if (!string.IsNullOrEmpty(title))
            {
                var panelGroup = new PanelGroupBlock(this)
                {
                    Column = processor.Column,
                    Span = new SourceSpan(processor.Start, slice.End)
                };
                var panelBlock = new PanelBlock(this)
                {
                    Title = title,
                    IsExpanded = isExpanded,
                    Column = processor.Column,
                    Span = new SourceSpan(processor.Start, slice.End)
                };
                processor.NewBlocks.Push(panelBlock);
                processor.NewBlocks.Push(panelGroup);

                return BlockState.ContinueDiscard;
            }

            return BlockState.None;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.Continue;
            }

            // If we are in a PanelBlock, we continue unless we see "===" or "==-"
            if (block is PanelBlock)
            {
                var slice = processor.Line;
                if (slice.Match("===") || slice.Match("==-"))
                {
                    // Close the current PanelBlock
                    return BlockState.None;
                }
                return BlockState.Continue;
            }

            if (block is PanelGroupBlock group)
            {
                // If the last child is an open PanelBlock, we must continue (delegate to child)
                if (group.LastChild is PanelBlock lastPanel && lastPanel.IsOpen)
                {
                    return BlockState.Continue;
                }

                var slice = processor.Line;
                if (slice.Match("===") || slice.Match("==-"))
                {
                    // Let TryOpen handle it
                    return BlockState.Continue;
                }

                // If line is blank, continue (ignore).
                if (processor.IsBlankLine) return BlockState.Continue;

                // If we have a panel, we accept content into the group
                if (group.LastChild is PanelBlock || (group.LastChild != null && group.Count > 0))
                {
                     return BlockState.Continue;
                }

                // Close the group if we encounter non-panel content
                return BlockState.None;
            }

            return BlockState.Continue;
        }
    }

    public class PanelRenderer : HtmlObjectRenderer<PanelGroupBlock>
    {
        private readonly MarkdownPipeline _pipeline;

        public PanelRenderer(MarkdownPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, PanelGroupBlock obj)
        {
            renderer.Write("<div class=\"panel-group my-4\">");

            PanelBlock currentPanel = null;
            bool pendingDetailsClose = false;

            foreach (var child in obj)
            {
                if (child is PanelBlock panel)
                {
                    if (pendingDetailsClose)
                    {
                        renderer.Write("</div>"); // Close previous content div
                        renderer.Write("</details>");
                        pendingDetailsClose = false;
                    }

                    currentPanel = panel;
                    var openAttr = panel.IsExpanded ? " open" : "";
                    renderer.Write($"<details class=\"bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-sm mb-2 overflow-hidden\"{openAttr}>");

                    renderer.Write("<summary class=\"px-4 py-2 bg-gray-50 dark:bg-gray-900 font-semibold cursor-pointer list-none flex items-center select-none\">");

                    // Render title as inline markdown
                    if (!string.IsNullOrEmpty(panel.Title))
                    {
                         // Parse as document but only render inlines of first paragraph
                         var doc = Markdig.Markdown.Parse(panel.Title, _pipeline);
                         foreach (var block in doc)
                         {
                             if (block is Markdig.Syntax.ParagraphBlock p)
                             {
                                 renderer.Write(p.Inline);
                             }
                             else
                             {
                                 renderer.Render(block);
                             }
                         }
                    }

                    renderer.Write("</summary>");

                    renderer.Write("<div class=\"p-4 border-t border-gray-200 dark:border-gray-700\">");

                    // Render PanelBlock's own children (if any)
                    renderer.WriteChildren(panel);

                    pendingDetailsClose = true;
                }
                else if (currentPanel != null)
                {
                    // Render orphan content as part of current panel
                    renderer.Render(child);
                }
            }

            if (pendingDetailsClose)
            {
                renderer.Write("</div>"); // Close last content div
                renderer.Write("</details>");
            }

            renderer.Write("</div>");
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
                if (!htmlRenderer.ObjectRenderers.Contains<PanelRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Add(new PanelRenderer(pipeline));
                }
            }
        }
    }
}
