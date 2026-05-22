using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neko.Builder
{
    public class SearchIndexGenerator
    {
        private static readonly Regex ScriptOrStyleRegex = new Regex(
            @"<(script|style)\b[^>]*>.*?</\1>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HtmlCommentRegex = new Regex(
            @"<!--.*?-->",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex TagRegex = new Regex(
            @"<[^>]+>",
            RegexOptions.Compiled);

        private static readonly Regex WhitespaceRegex = new Regex(
            @"\s+",
            RegexOptions.Compiled);

        // Matches any opening heading tag and captures its level, attributes, and inner text up to (but not
        // including) the next heading or end of string. Attribute order is not assumed.
        private static readonly Regex HeadingOpenRegex = new Regex(
            @"<h([1-6])\b([^>]*)>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex IdAttrRegex = new Regex(
            @"\bid\s*=\s*""([^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<SearchDocument> _documents = new List<SearchDocument>();

        public void AddDocument(string path, string title, string html, string description = null, string[] tags = null)
        {
            var sections = ExtractSections(html);

            var headingsText = new StringBuilder();
            foreach (var section in sections)
            {
                if (headingsText.Length > 0) headingsText.Append(' ');
                headingsText.Append(HtmlToText(section.TitleHtml));
            }

            var slug = BuildSlug(path);
            var fullText = HtmlToText(html);

            var pageContent = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(description))
            {
                pageContent.Append(description).Append(' ');
            }
            if (tags != null && tags.Length > 0)
            {
                pageContent.Append(string.Join(' ', tags)).Append(' ');
            }
            pageContent.Append(fullText);

            _documents.Add(new SearchDocument
            {
                Id = path,
                Title = title,
                Content = pageContent.ToString().Trim(),
                Headings = headingsText.Length > 0 ? headingsText.ToString() : null,
                Slug = slug,
                Type = "page"
            });

            // Emit one document per H2/H3 with an id attribute so users can deep-link
            // directly to a section. H1 is treated as the page-level title and skipped.
            foreach (var section in sections)
            {
                if (section.Level < 2 || section.Level > 3) continue;
                if (string.IsNullOrEmpty(section.Anchor)) continue;

                var sectionText = HtmlToText(section.BodyHtml);
                if (string.IsNullOrWhiteSpace(sectionText)) continue;

                var sectionTitle = HtmlToText(section.TitleHtml);

                _documents.Add(new SearchDocument
                {
                    Id = $"{path}#{section.Anchor}",
                    Title = sectionTitle,
                    Content = sectionText,
                    Slug = slug,
                    ParentTitle = title,
                    ParentId = path,
                    Type = "section"
                });
            }
        }

        public async Task WriteIndexAsync(string outputDir)
        {
            var json = JsonSerializer.Serialize(_documents, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            await File.WriteAllTextAsync(Path.Combine(outputDir, "search.json"), json);
        }

        public static string HtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var stripped = ScriptOrStyleRegex.Replace(html, " ");
            stripped = HtmlCommentRegex.Replace(stripped, " ");
            stripped = TagRegex.Replace(stripped, " ");
            stripped = WebUtility.HtmlDecode(stripped);
            stripped = WhitespaceRegex.Replace(stripped, " ");
            return stripped.Trim();
        }

        // Turns "blog/index.html" into "blog index", giving each path segment its own
        // search term so queries like "index" still match files named index.md.
        public static string BuildSlug(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var s = path;
            if (s.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 5);
            s = s.Replace('\\', '/').Replace('/', ' ');
            return WhitespaceRegex.Replace(s, " ").Trim();
        }

        public class ExtractedSection
        {
            public int Level;
            public string Anchor;
            public string TitleHtml;
            public string BodyHtml;
        }

        // Walks the rendered HTML, slicing it at each heading tag. The body of a section
        // runs from the close of its heading tag to the start of the next heading of any
        // level — H2 sections therefore contain their nested H3s, which we accept.
        public static List<ExtractedSection> ExtractSections(string html)
        {
            var result = new List<ExtractedSection>();
            if (string.IsNullOrWhiteSpace(html)) return result;

            var matches = HeadingOpenRegex.Matches(html);
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                if (!int.TryParse(m.Groups[1].Value, out var level)) continue;
                var attrs = m.Groups[2].Value;
                var idMatch = IdAttrRegex.Match(attrs);
                var anchor = idMatch.Success ? idMatch.Groups[1].Value : null;

                // Title HTML: from end of opening tag to start of matching closing tag.
                var closeTag = "</h" + level + ">";
                var titleStart = m.Index + m.Length;
                var closeIdx = html.IndexOf(closeTag, titleStart, StringComparison.OrdinalIgnoreCase);
                if (closeIdx < 0) continue;
                var titleHtml = html.Substring(titleStart, closeIdx - titleStart);

                // Body: from after the closing heading tag to the start of the next heading.
                var bodyStart = closeIdx + closeTag.Length;
                int bodyEnd;
                if (i + 1 < matches.Count) bodyEnd = matches[i + 1].Index;
                else bodyEnd = html.Length;
                if (bodyEnd < bodyStart) bodyEnd = bodyStart;
                var bodyHtml = html.Substring(bodyStart, bodyEnd - bodyStart);

                result.Add(new ExtractedSection
                {
                    Level = level,
                    Anchor = anchor,
                    TitleHtml = titleHtml,
                    BodyHtml = bodyHtml
                });
            }

            return result;
        }
    }

    public class SearchDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Headings { get; set; }
        public string Slug { get; set; }
        public string Type { get; set; }
        public string ParentTitle { get; set; }
        public string ParentId { get; set; }
    }
}
