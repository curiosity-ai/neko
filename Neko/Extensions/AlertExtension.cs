using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;

namespace Neko.Extensions
{
    public class AlertBlock : ContainerBlock
    {
        public string Variant { get; set; } = "primary";
        public string Title { get; set; }

        public AlertBlock(BlockParser parser) : base(parser)
        {
        }
    }

    public class AlertParser : BlockParser
    {
        public AlertParser()
        {
            OpeningCharacters = new[] { '!' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            // Check for !!! at start
            if (slice.Match("!!!"))
            {
                var start = slice.Start;

                // Advance the slice past "!!!"
                slice.Start += 3;

                string variant = "primary";
                string title = null;

                // Skip spaces
                slice.TrimStart();

                var variantStart = slice.Start;

                // Read first word (potential variant)
                while (!slice.IsEmpty && !IsSpace(slice.CurrentChar))
                {
                    slice.NextChar();
                }

                string firstWord = null;
                if (slice.Start > variantStart)
                {
                    firstWord = slice.Text.Substring(variantStart, slice.Start - variantStart);
                }

                if (IsKnownVariant(firstWord))
                {
                    variant = firstWord;
                    // The rest is title
                    slice.TrimStart();
                    if (!slice.IsEmpty)
                    {
                        title = slice.ToString();
                    }
                }
                else
                {
                    variant = "primary";
                    // Fallback: title is everything after !!! (excluding initial whitespace)
                    // We need to reconstruct the title because we might have consumed part of it as "firstWord"

                    // Reset to variantStart (which is after initial spaces)
                    var lineEnd = slice.End;
                    if (variantStart <= lineEnd)
                    {
                        title = slice.Text.Substring(variantStart, lineEnd - variantStart + 1);
                    }
                }

                var block = new AlertBlock(this)
                {
                    Variant = variant,
                    Title = title,
                    Column = processor.Column,
                    Span = new SourceSpan(processor.Start, slice.End)
                };

                processor.NewBlocks.Push(block);

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

            var slice = processor.Line;

            if (slice.Match("!!!"))
            {
                processor.Close(block);
                return BlockState.BreakDiscard;
            }

            return BlockState.Continue;
        }

        private static bool IsSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private static bool IsKnownVariant(string v)
        {
            if (string.IsNullOrEmpty(v)) return false;
            var l = v.ToLower();
            return l == "primary" || l == "secondary" || l == "success" || l == "danger" || l == "warning" || l == "info" || l == "light" || l == "dark" || l == "tip" || l == "question" || l == "ghost" || l == "contrast" || l == "note" || l == "important" || l == "caution";
        }
    }

    public class AlertRenderer : HtmlObjectRenderer<AlertBlock>
    {
        protected override void Write(HtmlRenderer renderer, AlertBlock obj)
        {
            var variant = obj.Variant?.ToLower() ?? "primary";

            // Map variant to colors
            string borderClass = "border-l-4";
            string bgClass = "bg-blue-50 dark:bg-blue-900/20";
            string borderColor = "border-blue-500";
            string icon = "info";
            string titleColor = "text-blue-800 dark:text-blue-200";
            string iconColor = "text-blue-500";

            switch (variant)
            {
                case "primary":
                case "info":
                case "note":
                    bgClass = "bg-blue-50 dark:bg-blue-900/20";
                    borderColor = "border-blue-500";
                    titleColor = "text-blue-800 dark:text-blue-200";
                    iconColor = "text-blue-500";
                    icon = "info";
                    break;
                case "success":
                case "tip":
                    bgClass = "bg-green-50 dark:bg-green-900/20";
                    borderColor = "border-green-500";
                    titleColor = "text-green-800 dark:text-green-200";
                    iconColor = "text-green-500";
                    icon = "check-circle";
                    break;
                case "warning":
                    bgClass = "bg-yellow-50 dark:bg-yellow-900/20";
                    borderColor = "border-yellow-500";
                    titleColor = "text-yellow-800 dark:text-yellow-200";
                    iconColor = "text-yellow-500";
                    icon = "exclamation";
                    break;
                case "danger":
                case "caution":
                    bgClass = "bg-red-50 dark:bg-red-900/20";
                    borderColor = "border-red-500";
                    titleColor = "text-red-800 dark:text-red-200";
                    iconColor = "text-red-500";
                    icon = "cross-circle";
                    break;
                case "question":
                case "important":
                    bgClass = "bg-purple-50 dark:bg-purple-900/20";
                    borderColor = "border-purple-500";
                    titleColor = "text-purple-800 dark:text-purple-200";
                    iconColor = "text-purple-500";
                    icon = "question";
                    break;
                case "secondary":
                case "light":
                case "dark":
                case "ghost":
                case "contrast":
                    bgClass = "bg-gray-50 dark:bg-gray-800";
                    borderColor = "border-gray-500";
                    titleColor = "text-gray-900 dark:text-gray-100";
                    iconColor = "text-gray-500";
                    icon = "info";
                    break;
            }

            renderer.Write($"<div class=\"my-4 p-4 {borderClass} {borderColor} {bgClass} rounded-r shadow-sm\">");

            // Header with Icon and Title
            renderer.Write("<div class=\"flex items-start\">");

            renderer.Write($"<div class=\"flex-shrink-0 text-xl mr-3 {iconColor}\">");
            renderer.Write($"<i class=\"fi fi-rr-{icon}\"></i>");
            renderer.Write("</div>");

            renderer.Write("<div class=\"flex-1\">");

            if (!string.IsNullOrEmpty(obj.Title))
            {
                 renderer.Write($"<h5 class=\"font-bold mb-2 {titleColor}\">{obj.Title}</h5>");
            }

            renderer.Write("<div class=\"prose dark:prose-invert max-w-none\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");

            renderer.Write("</div>"); // flex-1
            renderer.Write("</div>"); // flex
            renderer.Write("</div>");
        }
    }

    public class AlertExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<AlertParser>())
            {
                pipeline.BlockParsers.Insert(0, new AlertParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<AlertRenderer>();
            }
        }
    }
}
