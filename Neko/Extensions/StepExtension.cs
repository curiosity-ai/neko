using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class StepBlock : ContainerBlock
    {
        public string Title { get; set; }

        public StepBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class StepGroupBlock : ContainerBlock
    {
        public StepGroupBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class StepParser : BlockParser
    {
        public StepParser()
        {
            OpeningCharacters = new[] { '>' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;

            // Check for >>>
            if (slice.Match(">>>"))
            {
                var c = slice.PeekChar(3);
                if (c != '\0' && !c.IsWhitespace())
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

                // If we are in a StepBlock, close it.
                if (currentContainer is StepBlock)
                {
                    processor.Close(currentContainer);
                    currentContainer = currentContainer.Parent;
                }

                // If we are in a StepGroupBlock (after closing StepBlock if any)
                if (currentContainer is StepGroupBlock)
                {
                    if (!string.IsNullOrEmpty(title))
                    {
                        // Opening next step
                        var stepBlock = new StepBlock(this)
                        {
                            Title = title,
                            Column = processor.Column,
                            Span = new SourceSpan(processor.Start, slice.End)
                        };
                        processor.NewBlocks.Push(stepBlock);
                        return BlockState.ContinueDiscard;
                    }
                    else
                    {
                        // Closing group (>>> without title)
                        processor.Close(currentContainer);
                        return BlockState.BreakDiscard;
                    }
                }

                // Not in StepGroup. Start one.
                if (!string.IsNullOrEmpty(title))
                {
                    var stepGroup = new StepGroupBlock(this)
                    {
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    var stepBlock = new StepBlock(this)
                    {
                        Title = title,
                        Column = processor.Column,
                        Span = new SourceSpan(processor.Start, slice.End)
                    };
                    processor.NewBlocks.Push(stepBlock);
                    processor.NewBlocks.Push(stepGroup);

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

            if (block is StepBlock)
            {
                var slice = processor.Line;
                if (slice.Match(">>>"))
                {
                    var c = slice.PeekChar(3);
                    if (c == '\0' || c.IsWhitespace())
                    {
                        return BlockState.None;
                    }
                }
                return BlockState.Continue;
            }

            if (block is StepGroupBlock group)
            {
                if (group.LastChild is StepBlock lastStep && lastStep.IsOpen)
                {
                    return BlockState.Continue;
                }

                var slice = processor.Line;
                if (slice.Match(">>>"))
                {
                    var c = slice.PeekChar(3);
                    if (c == '\0' || c.IsWhitespace())
                    {
                        return BlockState.Continue;
                    }
                }

                if (processor.IsBlankLine) return BlockState.Continue;

                if (group.LastChild is StepBlock || (group.LastChild != null && group.Count > 0))
                {
                     return BlockState.Continue;
                }

                return BlockState.None;
            }

            return BlockState.Continue;
        }
    }

    public class StepRenderer : HtmlObjectRenderer<StepGroupBlock>
    {
        private readonly MarkdownPipeline _pipeline;

        public StepRenderer(MarkdownPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, StepGroupBlock obj)
        {
            renderer.Write("<div class=\"steps my-8 ml-4 border-l border-gray-200 dark:border-gray-800\">");

            int index = 1;
            foreach (var child in obj)
            {
                if (child is StepBlock step)
                {
                    renderer.Write("<div class=\"step relative pl-8 pb-10 last:pb-4\">");

                    renderer.Write($"<div class=\"absolute -left-[17px] top-0 flex items-center justify-center w-8 h-8 rounded-full bg-primary-500 text-white font-bold text-sm ring-8 ring-white dark:ring-gray-900\">{index}</div>");

                    renderer.Write("<h3 class=\"text-lg font-bold text-gray-900 dark:text-white mt-0 mb-4 pt-1\">");
                    if (!string.IsNullOrEmpty(step.Title))
                    {
                        var doc = Markdig.Markdown.Parse(step.Title, _pipeline);
                        foreach (var docBlock in doc)
                        {
                            if (docBlock is Markdig.Syntax.ParagraphBlock p)
                            {
                                renderer.Write(p.Inline);
                            }
                            else
                            {
                                renderer.Render(docBlock);
                            }
                        }
                    }
                    renderer.Write("</h3>");

                    renderer.Write("<div class=\"step-content prose dark:prose-invert max-w-none text-gray-600 dark:text-gray-300\">");
                    renderer.WriteChildren(step);
                    renderer.Write("</div>");

                    renderer.Write("</div>");
                    index++;
                }
            }

            renderer.Write("</div>");
        }
    }

    public class StepExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<StepParser>())
            {
                pipeline.BlockParsers.Insert(0, new StepParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                if (!htmlRenderer.ObjectRenderers.Contains<StepRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Add(new StepRenderer(pipeline));
                }
            }
        }
    }
}
