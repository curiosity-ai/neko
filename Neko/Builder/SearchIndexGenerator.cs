using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _routePrefix;

        // The friendly name of the (sub-)project every document belongs to, shown
        // as the leading search-breadcrumb crumb. Settable because it is resolved
        // from the finalized navbar, which isn't built until after construction.
        public string ProjectName { get; set; }

        public SearchIndexGenerator(string routePrefix = null, string projectName = null)
        {
            _routePrefix = NormalizeRoutePrefix(routePrefix);
            ProjectName = projectName;
        }

        // The leading breadcrumb(s) naming the project. Prefer the resolved
        // friendly name; fall back to the raw route-prefix segments so a result
        // still names its project even when no friendly name was resolved.
        private string[] ProjectCrumbs()
        {
            if (!string.IsNullOrWhiteSpace(ProjectName)) return new[] { ProjectName.Trim() };
            if (!string.IsNullOrEmpty(_routePrefix)) return _routePrefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return Array.Empty<string>();
        }

        // Prefix becomes part of the document id (e.g. `workspace/foo.html`) so
        // every entry survives being merged into a single aggregated index at
        // the root, and so links resolve correctly regardless of which sub-site
        // surfaced the result. We strip any leading/trailing slashes and a
        // ".html" suffix so it composes cleanly with file paths.
        private static string NormalizeRoutePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;
            var p = prefix.Replace('\\', '/').Trim();
            while (p.StartsWith("/")) p = p.Substring(1);
            while (p.EndsWith("/")) p = p.Substring(0, p.Length - 1);
            return p;
        }

        private string ApplyPrefix(string path)
        {
            if (string.IsNullOrEmpty(_routePrefix)) return path;
            if (string.IsNullOrEmpty(path)) return _routePrefix;
            var normalized = path.Replace('\\', '/').TrimStart('/');
            return _routePrefix + "/" + normalized;
        }

        public void AddDocument(string path, string title, string html, string description = null, string[] tags = null, string[] breadcrumbs = null)
        {
            // Breadcrumbs are the page's ancestor group titles from the site
            // navigation ("Guides", "Core concepts", …). They're stored on every
            // document — sections inherit the page's trail — so the search UI can
            // show a nav-derived breadcrumb instead of the raw file path.
            //
            // In multi-repo mode the supplied trail comes from the sub-project's
            // own sidebar/navbar and therefore starts *inside* the sub-project
            // (e.g. "Advanced"). Prepend the project crumb — its friendly name
            // (e.g. "Connect & Ingest") or, failing that, its mount path — so a
            // result in the aggregated root index always names the project it
            // belongs to as the first crumb. This restores the project segment
            // that the path fallback in search.js only adds when the trail is
            // empty.
            var crumbList = new List<string>(ProjectCrumbs());
            if (breadcrumbs != null)
            {
                crumbList.AddRange(breadcrumbs.Where(b => !string.IsNullOrWhiteSpace(b)));
            }
            var crumbs = crumbList.Count > 0 ? crumbList.ToArray() : null;

            var sections = ExtractSections(html);

            var headingsText = new StringBuilder();
            foreach (var section in sections)
            {
                if (headingsText.Length > 0) headingsText.Append(' ');
                headingsText.Append(HtmlToText(section.TitleHtml));
            }

            var prefixedPath = ApplyPrefix(path);
            var slug = BuildSlug(prefixedPath);
            var fullText = HtmlToText(html);

            // Tags are kept as a discrete field (in addition to being folded into
            // the searchable content below) so the search UI can surface them as
            // chips next to a result — used by the blog's inline search.
            var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
            if (tagList != null && tagList.Length == 0) tagList = null;

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
                Id = prefixedPath,
                Title = title,
                Content = pageContent.ToString().Trim(),
                Headings = headingsText.Length > 0 ? headingsText.ToString() : null,
                Slug = slug,
                Type = "page",
                Tags = tagList,
                Breadcrumbs = crumbs
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
                    Id = $"{prefixedPath}#{section.Anchor}",
                    Title = sectionTitle,
                    Content = sectionText,
                    Slug = slug,
                    ParentTitle = title,
                    ParentId = prefixedPath,
                    Type = "section",
                    Tags = tagList,
                    Breadcrumbs = crumbs
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

        // Reads every `search.json` produced by a multi-repo build (one per
        // sub-project), concatenates the entries, and writes a single merged
        // `search.json` to the root output. The client always fetches the root
        // copy so a search from any sub-site can find pages in any other one.
        // Sub-projects' own search.json files are left in place — they remain
        // valid stand-alone for downstream tooling, but the client ignores them.
        public static async Task AggregateAsync(string rootOutputDir, IEnumerable<string> subProjectOutputDirs)
        {
            var merged = new List<JsonElement>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var dir in subProjectOutputDirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                var path = Path.Combine(dir, "search.json");
                if (!File.Exists(path)) continue;

                using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                if (doc.RootElement.ValueKind != JsonValueKind.Array) continue;
                foreach (var entry in doc.RootElement.EnumerateArray())
                {
                    var id = entry.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    if (id != null && !seenIds.Add(id)) continue;
                    merged.Add(entry.Clone());
                }
            }

            Directory.CreateDirectory(rootOutputDir);
            var outputPath = Path.Combine(rootOutputDir, "search.json");
            await using var stream = File.Create(outputPath);
            await JsonSerializer.SerializeAsync(stream, merged, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
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

        // Turns "blog/post-1.html" into "blog post-1", giving each path segment its
        // own search term. A trailing "index" segment is dropped: "blog/index.html"
        // becomes "blog" (and a root "index.html" becomes ""), so queries like
        // "index" don't surface every section's landing page via the slug. Pages
        // remain findable via title and content.
        public static string BuildSlug(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var s = path;
            if (s.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) s = s.Substring(0, s.Length - 5);
            s = s.Replace('\\', '/');
            var segments = s.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 && segments[^1].Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                segments = segments[..^1];
            }
            return string.Join(' ', segments);
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
        public string[] Tags { get; set; }
        public string ParentTitle { get; set; }
        public string ParentId { get; set; }
        public string[] Breadcrumbs { get; set; }
    }
}
