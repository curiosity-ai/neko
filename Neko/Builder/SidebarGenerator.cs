using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neko.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Neko.Builder
{
    public class FolderConfig
    {
        [YamlMember(Alias = "order")]
        public int? Order { get; set; }

        [YamlMember(Alias = "label")]
        public string Label { get; set; }

        [YamlMember(Alias = "icon")]
        public string Icon { get; set; }

        [YamlMember(Alias = "expanded")]
        public bool Expanded { get; set; } = false;
    }

    public class SidebarGenerator
    {
        private readonly string _inputDirectory;
        private readonly MarkdownParser _parser;

        public SidebarGenerator(string inputDirectory)
        {
            _inputDirectory = inputDirectory;
            _parser = new MarkdownParser();
        }

        public List<LinkConfig> Generate()
        {
            return GenerateRecursive(_inputDirectory);
        }

        private List<LinkConfig> GenerateRecursive(string directory)
        {
            var items = new List<(LinkConfig Item, int Order, string Title)>();

            // 1. Process Directories
            var subDirectories = Directory.GetDirectories(directory);
            foreach (var subDir in subDirectories)
            {
                var dirName = Path.GetFileName(subDir);
                // Skip hidden folders and output folder
                if (dirName.StartsWith(".") || dirName.StartsWith("_") || dirName == "bin" || dirName == "obj") continue;

                var folderConfig = GetFolderConfig(subDir);
                var subItems = GenerateRecursive(subDir);

                if (subItems.Count > 0)
                {
                    var title = !string.IsNullOrEmpty(folderConfig.Label) ? folderConfig.Label : ToTitleCase(dirName);
                    var linkConfig = new LinkConfig
                    {
                        Text = title,
                        Items = subItems,
                        Icon = folderConfig.Icon
                    };

                    items.Add((linkConfig, folderConfig.Order ?? int.MaxValue, title));
                }
            }

            // 2. Process Files
            var files = Directory.GetFiles(directory, "*.md");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                // Skip hidden files
                if (fileName.StartsWith(".")) continue;

                var markdown = File.ReadAllText(file);
                var doc = _parser.Parse(markdown);

                string title = doc.FrontMatter.Label;

                if (string.IsNullOrEmpty(title))
                {
                    title = doc.FrontMatter.Title;
                }

                if (string.IsNullOrEmpty(title))
                {
                    var h1 = doc.Toc.FirstOrDefault(x => x.Level == 1);
                    if (h1 != null)
                    {
                        title = h1.Title;
                    }
                    else
                    {
                        title = ToTitleCase(Path.GetFileNameWithoutExtension(file));
                    }
                }

                var order = doc.FrontMatter.Order ?? int.MaxValue;

                // Special handling for index.md
                if (fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase))
                {
                    if (Path.GetFullPath(directory) == Path.GetFullPath(_inputDirectory))
                    {
                        title = "Home"; // Root index is Home
                    }
                    else
                    {
                         // If we fell back to filename "Index", change to "Overview"
                         if (title == "Index") title = "Overview";
                    }
                }

                var relativePath = Path.GetRelativePath(_inputDirectory, file).Replace("\\", "/");
                // remove extension
                if (relativePath.EndsWith(".md")) relativePath = relativePath.Substring(0, relativePath.Length - 3);

                var linkConfig = new LinkConfig
                {
                    Text = title,
                    Link = relativePath,
                    Icon = doc.FrontMatter.Icon
                };

                items.Add((linkConfig, order, title));
            }

            // 3. Sort
            var sortedItems = items
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Title)
                .Select(x => x.Item)
                .ToList();

            return sortedItems;
        }

        private FolderConfig GetFolderConfig(string directory)
        {
            var dirName = Path.GetFileName(directory);
            var possibleFiles = new[] { Path.Combine(directory, "index.yml"), Path.Combine(directory, $"{dirName}.yml") };

            foreach (var file in possibleFiles)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        var yaml = File.ReadAllText(file);
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .IgnoreUnmatchedProperties()
                            .Build();
                        return deserializer.Deserialize<FolderConfig>(yaml) ?? new FolderConfig();
                    }
                    catch
                    {
                        // ignore error
                    }
                }
            }

            return new FolderConfig();
        }

        private string ToTitleCase(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            var text = str.Replace("-", " ").Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }
    }
}
