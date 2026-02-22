using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace TailDocs.CLI.Extensions
{
    public class IconInline : Inline
    {
        public string Name { get; set; }
    }

    public class IconParser : InlineParser
    {
        public IconParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            // Check for starting :
            if (slice.CurrentChar != ':') return false;
            slice.NextChar();

            // Check for icon- prefix
            // We expect "icon-" followed by something.
            // Check manually chars
            if (slice.CurrentChar == 'i' &&
                slice.PeekChar(1) == 'c' &&
                slice.PeekChar(2) == 'o' &&
                slice.PeekChar(3) == 'n' &&
                slice.PeekChar(4) == '-')
            {
                slice.NextChar(); // i
                slice.NextChar(); // c
                slice.NextChar(); // o
                slice.NextChar(); // n
                slice.NextChar(); // -
            }
            else
            {
                slice = saved;
                return false;
            }

            // Parse name (e.g. home, user, etc.)
            var nameStart = slice.Start;
            while (slice.CurrentChar.IsAlphaNumeric() || slice.CurrentChar == '-')
            {
                slice.NextChar();
            }
            var nameLength = slice.Start - nameStart;

            if (nameLength == 0)
            {
                slice = saved;
                return false;
            }

            var name = slice.Text.Substring(nameStart, nameLength).ToLower();

            // Check for closing :
            if (slice.CurrentChar != ':')
            {
                slice = saved;
                return false;
            }
            slice.NextChar(); // Skip closing :

            processor.Inline = new IconInline { Name = name };
            return true;
        }
    }

    public class IconRenderer : HtmlObjectRenderer<IconInline>
    {
        protected override void Write(HtmlRenderer renderer, IconInline obj)
        {
            renderer.Write($"<i class=\"fi fi-rr-{obj.Name} align-middle\"></i>");
        }
    }

    public class IconExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<IconParser>())
            {
                pipeline.InlineParsers.Insert(0, new IconParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<IconRenderer>();
            }
        }
    }
}
