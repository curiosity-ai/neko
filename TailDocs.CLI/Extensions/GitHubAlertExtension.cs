using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace TailDocs.CLI.Extensions
{
    public class GitHubAlertExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            // Nothing to setup for parsing, UseAlerts handles it.
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // Insert our renderer at the beginning to take precedence over Markdig's default renderer
                htmlRenderer.ObjectRenderers.Insert(0, new GitHubAlertRenderer());
            }
        }
    }
}
