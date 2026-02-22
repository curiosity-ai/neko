using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace Neko.Extensions
{
    public class EmojiInline : Inline
    {
        public string Name { get; set; }
    }

    public class EmojiParser : InlineParser
    {
        public EmojiParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            // Check for starting :
            if (slice.CurrentChar != ':') return false;
            slice.NextChar();

            // Avoid conflict with IconParser which uses :icon-name:
            if (slice.CurrentChar == 'i' &&
                slice.PeekChar(1) == 'c' &&
                slice.PeekChar(2) == 'o' &&
                slice.PeekChar(3) == 'n' &&
                slice.PeekChar(4) == '-')
            {
                slice = saved;
                return false;
            }

            // We expect alphanumeric, _, -, +
            var nameStart = slice.Start;
            while (slice.CurrentChar.IsAlphaNumeric() || slice.CurrentChar == '-' || slice.CurrentChar == '_' || slice.CurrentChar == '+')
            {
                slice.NextChar();
            }
            var nameLength = slice.Start - nameStart;

            if (nameLength == 0)
            {
                slice = saved;
                return false;
            }

            var name = slice.Text.Substring(nameStart, nameLength);

            // Check for closing :
            if (slice.CurrentChar != ':')
            {
                slice = saved;
                return false;
            }
            slice.NextChar(); // Skip closing :

            processor.Inline = new EmojiInline { Name = name };
            return true;
        }
    }

    public class EmojiRenderer : HtmlObjectRenderer<EmojiInline>
    {
        protected override void Write(HtmlRenderer renderer, EmojiInline obj)
        {
            renderer.Write($"<i class=\"em em-{obj.Name}\"></i>");
        }
    }

    public class EmojiExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<EmojiParser>())
            {
                // Insert at 0 to be checked early, but it explicitly yields to IconParser via check
                pipeline.InlineParsers.Insert(0, new EmojiParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<EmojiRenderer>();
            }
        }
    }
}
