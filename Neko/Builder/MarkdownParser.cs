using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Neko.Extensions;

namespace Neko.Builder
{
    public class TocItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int Level { get; set; }
    }

    public class ParsedDocument
    {
        public string Html { get; set; }
        public FrontMatter FrontMatter { get; set; } = new FrontMatter();
        public List<TocItem> Toc { get; set; } = new List<TocItem>();
        public List<string> OutgoingLinks { get; set; } = new List<string>();
    }

    public class FrontMatter
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "order")]
        public int? Order { get; set; }

        [YamlMember(Alias = "tags")]
        public string[] Tags { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }

        [YamlMember(Alias = "password")]
        public string Password { get; set; }

        [YamlMember(Alias = "author")]
        public string Author { get; set; }

        [YamlMember(Alias = "authorImage")]
        public string AuthorImage { get; set; }

        [YamlMember(Alias = "date")]
        public string Date { get; set; }

        [YamlMember(Alias = "cover")]
        public string Cover { get; set; }

        [YamlMember(Alias = "layout")]
        public string Layout { get; set; }

        [YamlMember(Alias = "searchExclude")]
        public bool SearchExclude { get; set; }

        // Sidebar/nav visibility. `hidden` keeps the page out of the sidebar
        // while still building it (crawlable, linkable). `protected` is handled
        // separately via `password`; `public` (default) shows normally.
        [YamlMember(Alias = "visibility")]
        public string Visibility { get; set; }

        [YamlMember(Alias = "redirectSlug")]
        public string RedirectSlug { get; set; }

        // Changelog version entries may point at a release/reference for the version
        // (e.g. the NuGet page, a GitHub release, or release notes). Rendered as a
        // linked version badge in the timeline header.
        [YamlMember(Alias = "link")]
        public string Link { get; set; }

        // Blog "Read next" related posts. Each entry is a link to another blog
        // post — a file path (`other-post.md`), a site URL (`/blog/other-post`),
        // or just the slug (`other-post`). Rendered (blog mode) as the card grid
        // in the "Read next" section at the foot of the post. When fewer than the
        // configured count are listed, the section auto-fills with recent posts.
        [YamlMember(Alias = "readNext")]
        public string[] ReadNext { get; set; }
    }

    public static class UrlHelper
    {
        // Internal page URLs are authored with the .md suffix (so editor link
        // completion and IDE navigation work) but generated pages are served
        // without one. Strip the suffix from any local URL, preserving the
        // anchor/query suffix. External URLs and non-.md links pass through.
        public static string StripMarkdownExtension(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (url.Contains("://")) return url;
            if (url.StartsWith("#") || url.StartsWith("mailto:")) return url;

            var splitIdx = url.IndexOfAny(new[] { '#', '?' });
            var path = splitIdx >= 0 ? url.Substring(0, splitIdx) : url;
            var suffix = splitIdx >= 0 ? url.Substring(splitIdx) : "";

            if (path.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
            {
                return path.Substring(0, path.Length - 3) + suffix;
            }

            return url;
        }
    }

    public class MarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownParser(Configuration.NekoConfig config)
        {
            var customContainerExtension = new CustomContainerExtension(config);
            var pipelineBuilder = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .UseMathematics()
                .UseDiagrams()
                .UseCustomContainers()
                .UseGenericAttributes() // Enables {width=500}
                .Use<IconExtension>()
                .Use<EmojiExtension>()
                .Use<BadgeExtension>()
                .Use<VersionBadgeExtension>()
                .Use<AlertExtension>()
                .Use<GitHubAlertExtension>()
                .Use<TabExtension>()
                .Use<PanelExtension>()
                .Use<ColumnExtension>()
                .Use<StepExtension>()
                .Use<ComponentExtension>()
                .Use<YouTubeEmbedExtension>()
                .Use<CodeBlockExtension>()
                .Use<CodeSnippetExtension>()
                .Use<ImageAlignmentExtension>()
                .Use<CustomImageExtension>()
                .Use<PdfExtension>()
                .Use<SnapFrameExtension>()
                .Use<ImageGenExtension>()
                .Use<LessonExtension>();

            pipelineBuilder.Extensions.Add(customContainerExtension);
            _pipeline = pipelineBuilder.Build();
        }

        private string PreProcessIncludes(string content, string currentFilePath, string rootDirectory, int depth = 0)
        {
            if (depth > 5) return content; // Recursion limit

            var currentDir = Path.GetDirectoryName(currentFilePath);

            // Regex to find {{ include "path" }}
            return Regex.Replace(content, @"\{\{\s*include\s+""([^""]+)""\s*\}\}", match =>
            {
                var relativePath = match.Groups[1].Value;

                // Ignore anchors for file resolution
                if (relativePath.Contains("#"))
                {
                    relativePath = relativePath.Split('#')[0];
                }

                if (string.IsNullOrEmpty(relativePath)) return match.Value;

                var targetPath = Path.Combine(currentDir, relativePath);

                if (!File.Exists(targetPath))
                {
                    // Try relative to root
                    var rootPath = Path.Combine(rootDirectory, relativePath.TrimStart('/', '\\'));
                    if (File.Exists(rootPath))
                    {
                        targetPath = rootPath;
                    }
                    else
                    {
                        return $"<!-- Include file not found: {relativePath} -->";
                    }
                }

                try
                {
                    var includedContent = File.ReadAllText(targetPath);
                    // Recursively process includes
                    return PreProcessIncludes(includedContent, targetPath, rootDirectory, depth + 1);
                }
                catch (System.Exception ex)
                {
                    return $"<!-- Error including file {relativePath}: {ex.Message} -->";
                }
            });
        }

        // Extract the C# code of every `tesserae` fenced block in a document, exactly
        // as the renderer would read it (post-include, line-by-line). Used to warm the
        // Tesserae compile cache in parallel before the synchronous render pass, so
        // each block becomes a cache hit instead of a serial compile.
        public List<(string Arguments, string Code)> ExtractTesseraeSamples(string markdown, string filePath = null, string rootDirectory = null)
        {
            var samples = new List<(string Arguments, string Code)>();
            if (string.IsNullOrEmpty(markdown)) return samples;

            // Cheap gate: skip the parse entirely when the file has no tesserae blocks.
            if (markdown.IndexOf("tesserae", System.StringComparison.OrdinalIgnoreCase) < 0) return samples;

            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(rootDirectory))
            {
                markdown = PreProcessIncludes(markdown, filePath, rootDirectory);
            }

            var document = Markdig.Markdown.Parse(markdown, _pipeline);
            foreach (var fenced in document.Descendants<FencedCodeBlock>())
            {
                if ((fenced.Info ?? "").ToLower() != "tesserae") continue;

                // Resolve the compiled source exactly as the render pass does (honouring
                // any // <overwrite-sample-code> region), so the warm pass compiles
                // byte-identical source and shares the cache key.
                var lines = fenced.Lines;
                var rawLines = new List<string>(lines.Count);
                for (int i = 0; i < lines.Count; i++)
                {
                    var slice = lines.Lines[i].Slice;
                    rawLines.Add(slice.Text == null ? null : slice.ToString());
                }

                var (code, _) = TesseraeCompiler.PartitionSampleSource(rawLines);
                samples.Add((fenced.Arguments, code));
            }

            return samples;
        }

        // Resolves a local (non-absolute, non-anchor) asset URL to a root-relative
        // path. If the file exists exactly where the URL points (relative to the
        // page), the URL is returned unchanged; otherwise the asset is searched for
        // up the folder tree (matching the authored sub-path, or an `assets/` folder
        // when the URL is a bare filename) and rewritten to `/<path-from-root>`.
        // Unresolvable, external (`://`) and anchor (`#`) URLs are returned as-is.
        private static string ResolveAssetUrl(string url, string currentDir, string rootDir)
        {
            if (string.IsNullOrEmpty(url) || url.Contains("://") || url.StartsWith("#")) return url;

            var trimmedUrl = url.TrimStart('/');
            var targetPath = Path.Combine(currentDir, trimmedUrl);

            // If file exists as is, no need to resolve.
            if (File.Exists(targetPath)) return url;

            // Try to resolve in folders up the tree.
            var fileName = Path.GetFileName(trimmedUrl);
            var urlDir = Path.GetDirectoryName(trimmedUrl)?.Replace('\\', '/');

            var searchDir = currentDir;
            while (searchDir.StartsWith(rootDir, System.StringComparison.OrdinalIgnoreCase))
            {
                string candidateDir = string.IsNullOrEmpty(urlDir) ? Path.Combine(searchDir, "assets") : Path.Combine(searchDir, urlDir);
                var assetPath = Path.GetFullPath(Path.Combine(candidateDir, fileName));

                if (File.Exists(assetPath))
                {
                    // Calculate relative path from rootDir to foundPath.
                    return "/" + Path.GetRelativePath(rootDir, assetPath).Replace("\\", "/");
                }

                var parent = Directory.GetParent(searchDir);
                if (parent == null) break;
                searchDir = parent.FullName;
            }

            return url;
        }

        public ParsedDocument Parse(string markdown, string filePath = null, string rootDirectory = null)
        {
            // Pre-process includes
            if (!string.IsNullOrEmpty(markdown) && !string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(rootDirectory))
            {
                markdown = PreProcessIncludes(markdown, filePath, rootDirectory);
            }

            LessonExtension.CurrentFilePath = filePath;
            LessonExtension.CurrentRootDirectory = rootDirectory;

            var document = Markdig.Markdown.Parse(markdown, _pipeline);

            // Asset Resolution
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(rootDirectory) && File.Exists(filePath))
            {
                var currentDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                var rootDir = Path.GetFullPath(rootDirectory);

                foreach (var link in document.Descendants<LinkInline>())
                {
                    // Process image URLs
                    if (link.IsImage && !string.IsNullOrEmpty(link.Url) && !link.Url.Contains("://") && !link.Url.StartsWith("#"))
                    {
                        link.Url = ResolveAssetUrl(link.Url, currentDir, rootDir);
                    }
                }
            }

            // Extract FrontMatter
            var frontMatterBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            var frontMatter = new FrontMatter();

            if (frontMatterBlock != null)
            {
                var yaml = frontMatterBlock.Lines.ToString();
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                frontMatter = deserializer.Deserialize<FrontMatter>(yaml) ?? new FrontMatter();
            }

            // Resolve image-bearing front-matter paths (cover, author image) the same
            // way body images are resolved above. These are emitted straight into the
            // HTML by the generator, so without this a relative `cover: assets/…` would
            // resolve against the page URL (e.g. `/blog/<post>/assets/…`) and 404 on
            // single post pages. Rewriting to a root-relative `/assets/…` fixes that.
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(rootDirectory) && File.Exists(filePath))
            {
                var currentDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                var rootDir = Path.GetFullPath(rootDirectory);
                frontMatter.Cover = ResolveAssetUrl(frontMatter.Cover, currentDir, rootDir);
                frontMatter.AuthorImage = ResolveAssetUrl(frontMatter.AuthorImage, currentDir, rootDir);
            }

            // Extract TOC
            var toc = new List<TocItem>();
            foreach (var heading in document.Descendants<HeadingBlock>())
            {
                var id = heading.GetAttributes().Id;
                if (string.IsNullOrEmpty(id)) continue;

                // Render inline content to HTML string for the TOC title
                using var writer = new StringWriter();
                var renderer = new HtmlRenderer(writer);
                _pipeline.Setup(renderer);
                renderer.Render(heading.Inline);
                var title = writer.ToString();

                toc.Add(new TocItem
                {
                    Id = id,
                    Title = title,
                    Level = heading.Level
                });
            }

            // Extract Links
            var outgoingLinks = new List<string>();
            foreach (var link in document.Descendants<LinkInline>())
            {
                if (!link.IsImage && !string.IsNullOrEmpty(link.Url) && !link.Url.Contains("://") && !link.Url.StartsWith("#") && !link.Url.StartsWith("mailto:"))
                {
                    link.Url = UrlHelper.StripMarkdownExtension(link.Url);
                    outgoingLinks.Add(link.Url);
                }
            }

            var html = document.ToHtml(_pipeline);

            return new ParsedDocument
            {
                Html = html,
                FrontMatter = frontMatter,
                Toc = toc,
                OutgoingLinks = outgoingLinks
            };
        }
    }
}
