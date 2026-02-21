using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TailDocs.CLI.Configuration;

namespace TailDocs.CLI.Builder
{
    public class SearchIndexGenerator
    {
        private readonly List<SearchDocument> _documents = new List<SearchDocument>();

        public void AddDocument(string path, string title, string content)
        {
            // Simple text extraction (stripping HTML tags would be better, but basic for now)
            // Content passed here is raw markdown? Or Parsed HTML?
            // MarkdownParser returns ParsedDocument.

            // We should use text content.
            // For now, let's just index title and a snippet or full content if clean.

            _documents.Add(new SearchDocument
            {
                Id = path,
                Title = title,
                Content = content // TODO: Strip markdown/HTML
            });
        }

        public async Task WriteIndexAsync(string outputDir)
        {
            // MiniSearch expects a JSON array of documents
            // We can just dump the list as JSON and let the client load and index it.
            // "Automatically generate the required json and fetch it when searching for the first time."

            var json = JsonSerializer.Serialize(_documents, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(Path.Combine(outputDir, "search.json"), json);
        }
    }

    public class SearchDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
