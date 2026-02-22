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

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "order")]
        public int? Order { get; set; }

        [YamlMember(Alias = "tags")]
        public string[] Tags { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; }
    }

    public class MarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownParser()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .UseMathematics()
                .UseDiagrams()
                .UseCustomContainers()
                .Use<CustomContainerExtension>()
                .UseGenericAttributes() // Enables {width=500}
                .Use<IconExtension>()
                .Use<EmojiExtension>()
                .Use<BadgeExtension>()
                .Use<AlertExtension>()
                .Use<GitHubAlertExtension>()
                .Use<TabExtension>()
                .Use<PanelExtension>()
                .Use<ColumnExtension>()
                .Use<ComponentExtension>()
                .Use<CodeBlockExtension>()
                .Use<CodeSnippetExtension>()
                .Use<ImageAlignmentExtension>()
                .Build();
        }

        public ParsedDocument Parse(string markdown, string filePath = null, string rootDirectory = null)
        {
            var document = Markdig.Markdown.Parse(markdown, _pipeline);

            // Asset Resolution
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(rootDirectory) && File.Exists(filePath))
            {
                var currentDir = Path.GetDirectoryName(Path.GetFullPath(filePath));
                var rootDir = Path.GetFullPath(rootDirectory);

                foreach (var link in document.Descendants<LinkInline>())
                {
                    // Only process relative image URLs
                    if (link.IsImage && !string.IsNullOrEmpty(link.Url) && !link.Url.Contains("://") && !link.Url.StartsWith("/") && !link.Url.StartsWith("#"))
                    {
                        var url = link.Url;
                        var targetPath = Path.Combine(currentDir, url);

                        // If file exists as is, no need to resolve
                        if (File.Exists(targetPath)) continue;

                        // Try to resolve in assets folders up the tree
                        var fileName = Path.GetFileName(url);
                        var searchDir = currentDir;
                        string foundPath = null;

                        while (searchDir.StartsWith(rootDir, System.StringComparison.OrdinalIgnoreCase))
                        {
                            var assetPath = Path.Combine(searchDir, "assets", fileName);
                            if (File.Exists(assetPath))
                            {
                                foundPath = assetPath;
                                break;
                            }

                            var parent = Directory.GetParent(searchDir);
                            if (parent == null) break;
                            searchDir = parent.FullName;
                        }

                        if (foundPath != null)
                        {
                            // Calculate relative path from currentDir to foundPath
                            var relativePath = Path.GetRelativePath(currentDir, foundPath).Replace("\\", "/");
                            link.Url = relativePath;
                        }
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
