using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Linq;
using TailDocs.CLI.Extensions;

namespace TailDocs.CLI.Builder
{
    public class ParsedDocument
    {
        public string Html { get; set; }
        public FrontMatter FrontMatter { get; set; } = new FrontMatter();
    }

    public class FrontMatter
    {
        [YamlMember(Alias = "title")]
        public string Title { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "order")]
        public int Order { get; set; }

        [YamlMember(Alias = "tags")]
        public string[] Tags { get; set; }
    }

    public class MarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownParser()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .Use<BadgeExtension>()
                .Use<AlertExtension>()
                .Use<TabExtension>()
                .Build();
        }

        public ParsedDocument Parse(string markdown)
        {
            var document = Markdig.Markdown.Parse(markdown, _pipeline);

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

            var html = document.ToHtml(_pipeline);

            return new ParsedDocument
            {
                Html = html,
                FrontMatter = frontMatter
            };
        }
    }
}
