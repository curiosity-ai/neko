using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Linq;

namespace Neko.Extensions
{
    /// <summary>
    /// In a pipe table, <c>\|</c> inside a code span stands for a literal pipe (GFM spec, example 200):
    /// <c>`a \| b`</c> renders as <c>a | b</c>. Markdig keeps the backslash, so take it out.
    /// </summary>
    public class TableCodePipeExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed += document =>
            {
                foreach (var cell in document.Descendants<TableCell>())
                    foreach (var code in cell.Descendants<CodeInline>().Where(c => c.Content.Contains("\\|")))
                        code.Content = code.Content.Replace("\\|", "|");
            };
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }
}
