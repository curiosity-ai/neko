using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
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

        private readonly List<SearchDocument> _documents = new List<SearchDocument>();

        public void AddDocument(string path, string title, string html, string description = null, string[] tags = null)
        {
            var text = HtmlToText(html);

            var combined = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(description))
            {
                combined.Append(description).Append(' ');
            }
            if (tags != null && tags.Length > 0)
            {
                combined.Append(string.Join(' ', tags)).Append(' ');
            }
            combined.Append(text);

            _documents.Add(new SearchDocument
            {
                Id = path,
                Title = title,
                Content = combined.ToString().Trim()
            });
        }

        public async Task WriteIndexAsync(string outputDir)
        {
            var json = JsonSerializer.Serialize(_documents, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
    }

    public class SearchDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
