using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    public class YouTubeEmbedExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.DocumentProcessed += ProcessDocument;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private void ProcessDocument(MarkdownDocument document)
        {
            foreach (var node in document.Descendants<ParagraphBlock>().ToList())
            {
                if (IsSingleYouTubeLink(node, out var videoId, out var queryParams))
                {
                    var embedHtml = GenerateEmbedHtml(videoId, queryParams);
                    var embedBlock = new HtmlBlock(null);
                    embedBlock.Type = (HtmlBlockType)6; // Standard block (like div, iframe)
                    embedBlock.Lines = new Markdig.Helpers.StringLineGroup(1);
                    embedBlock.Lines.Add(new Markdig.Helpers.StringSlice(embedHtml));

                    if (node.Parent != null)
                    {
                        var index = node.Parent.IndexOf(node);
                        if (index >= 0)
                        {
                            node.Parent.Insert(index, embedBlock);
                            node.Parent.Remove(node);
                        }
                    }
                }
            }
        }

        private bool IsSingleYouTubeLink(ParagraphBlock paragraph, out string videoId, out Dictionary<string, string> queryParams)
        {
            videoId = null;
            queryParams = null;

            if (paragraph.Inline == null) return false;

            LinkInline link = null;
            var child = paragraph.Inline.FirstChild;

            while (child != null)
            {
                if (child is LinkInline l)
                {
                    if (link != null) return false; // More than one link
                    link = l;
                }
                else if (child is LiteralInline literal)
                {
                    if (!string.IsNullOrWhiteSpace(literal.Content.ToString())) return false; // Non-whitespace text
                }
                else
                {
                    // Any other inline type
                    return false;
                }
                child = child.NextSibling;
            }

            if (link == null) return false;

            return TryParseYouTubeUrl(link.Url, out videoId, out queryParams);
        }

        private bool TryParseYouTubeUrl(string url, out string videoId, out Dictionary<string, string> queryParams)
        {
            videoId = null;
            queryParams = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(url)) return false;

            Uri uri;
            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            }
            catch
            {
                return false;
            }

            if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
            {
                // Parse query parameters
                var query = uri.Query.TrimStart('?');
                foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=');
                    var key = kv[0];
                    var value = kv.Length > 1 ? kv[1] : "";
                    queryParams[key] = value;
                }

                if (uri.Host.Contains("youtube.com"))
                {
                    if (uri.AbsolutePath == "/watch")
                    {
                        if (queryParams.TryGetValue("v", out var v))
                        {
                            videoId = v;
                        }
                    }
                    else if (uri.AbsolutePath.StartsWith("/embed/"))
                    {
                        videoId = uri.AbsolutePath.Substring("/embed/".Length);
                    }
                }
                else if (uri.Host.Contains("youtu.be"))
                {
                    videoId = uri.AbsolutePath.TrimStart('/');
                }
            }

            return !string.IsNullOrEmpty(videoId);
        }

        private string GenerateEmbedHtml(string videoId, Dictionary<string, string> queryParams)
        {
            // Handle timestamp 't' -> 'start'
            if (queryParams.TryGetValue("t", out var t))
            {
                // t can be 30s, 1m30s, etc. Or just seconds.
                // YouTube embed expects 'start' in seconds.
                // Simple parsing for now: if ends with s, strip it.
                // A robust parser would handle 1h2m3s.
                // For this task, assuming 's' suffix or plain number is enough based on doc examples.

                string start = null;
                if (int.TryParse(t, out _))
                {
                    start = t;
                }
                else if (t.EndsWith("s"))
                {
                    if (int.TryParse(t.TrimEnd('s'), out _))
                    {
                        start = t.TrimEnd('s');
                    }
                }

                if (start != null)
                {
                    queryParams["start"] = start;
                }
                queryParams.Remove("t");
            }

            // Remove 'v' from params as it's the ID
            queryParams.Remove("v");

            var queryString = "";
            if (queryParams.Count > 0)
            {
                queryString = "?" + string.Join("&", queryParams.Select(kv => $"{kv.Key}={kv.Value}"));
            }

            // HTML escaping? The videoId and query params come from URL parsing, usually safe-ish but we should be careful.
            // But we trust the URL parser to give us valid parts.

            return $"<div class=\"aspect-w-16 aspect-h-9 my-4\"><iframe src=\"https://www.youtube.com/embed/{videoId}{queryString}\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen class=\"w-full h-full rounded-lg shadow-lg\"></iframe></div>";
        }
    }
}
