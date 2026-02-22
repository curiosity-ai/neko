using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neko.Configuration;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Neko.Builder
{
    public class NavigationGenerator
    {
        private readonly string _inputDirectory;
        private readonly List<(string FilePath, string RelativePath, ParsedDocument Doc)> _docs;
        private readonly IDeserializer _deserializer;

        public NavigationGenerator(string inputDirectory, List<(string FilePath, string RelativePath, ParsedDocument Doc)> docs)
        {
            _inputDirectory = inputDirectory;
            _docs = docs;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public List<LinkConfig> Generate()
        {
            return GetItemsInDirectory(_inputDirectory);
        }

        private List<LinkConfig> GetItemsInDirectory(string directory)
        {
            var items = new List<(LinkConfig Item, int Order)>();

            // Process Subdirectories
            var subDirs = Directory.GetDirectories(directory);
            foreach (var subDir in subDirs)
            {
                var dirName = Path.GetFileName(subDir);
                if (dirName.StartsWith(".")) continue;
                if (dirName == "assets" || dirName == "neko") continue;

                // Check if directory contains any relevant docs
                if (!_docs.Any(d => d.FilePath.StartsWith(subDir))) continue;

                var meta = GetFolderMeta(subDir);
                var folderItems = GetItemsInDirectory(subDir);

                // Check for index/README inside the folder to link the folder itself
                string link = null;
                var indexDoc = _docs.FirstOrDefault(d =>
                    Path.GetDirectoryName(d.FilePath) == subDir &&
                    (Path.GetFileName(d.FilePath).Equals("index.md", StringComparison.OrdinalIgnoreCase) ||
                     Path.GetFileName(d.FilePath).Equals("README.md", StringComparison.OrdinalIgnoreCase)));

                int order = 9999;

                if (meta != null && meta.Order.HasValue)
                {
                    order = meta.Order.Value;
                }
                else if (indexDoc.Doc != null && indexDoc.Doc.FrontMatter.Order.HasValue)
                {
                    order = indexDoc.Doc.FrontMatter.Order.Value;
                }

                if (indexDoc.Doc != null)
                {
                    // Use index/README as the link for the folder
                    link = indexDoc.RelativePath.Replace("\\", "/");
                    if (link.EndsWith(".md")) link = link.Substring(0, link.Length - 3);
                }

                var folderLink = new LinkConfig
                {
                    Text = meta?.Label ?? (indexDoc.Doc?.FrontMatter.Title) ?? ToTitleCase(dirName),
                    Icon = meta?.Icon ?? (indexDoc.Doc?.FrontMatter.Icon),
                    Items = folderItems,
                    Link = link
                };

                items.Add((folderLink, order));
            }

            // Process Files
            var files = _docs.Where(d => Path.GetDirectoryName(d.FilePath) == directory).ToList();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file.FilePath);

                // Skip index/README if not in root, as they are handled by folder logic
                if (directory != _inputDirectory && (fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase) || fileName.Equals("README.md", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var order = file.Doc.FrontMatter.Order ?? 9999;
                var link = file.RelativePath.Replace("\\", "/");
                if (link.EndsWith(".md")) link = link.Substring(0, link.Length - 3);

                var item = new LinkConfig
                {
                    Text = !string.IsNullOrEmpty(file.Doc.FrontMatter.Title) ? file.Doc.FrontMatter.Title : Path.GetFileNameWithoutExtension(fileName),
                    Icon = file.Doc.FrontMatter.Icon,
                    Link = link
                };

                items.Add((item, order));
            }

            return items.OrderBy(x => x.Order).ThenBy(x => x.Item.Text).Select(x => x.Item).ToList();
        }

        private FolderMeta GetFolderMeta(string directory)
        {
            var metaPath = Path.Combine(directory, "meta.yml");
            if (!File.Exists(metaPath)) return null;

            try
            {
                var yaml = File.ReadAllText(metaPath);
                return _deserializer.Deserialize<FolderMeta>(yaml);
            }
            catch
            {
                return null;
            }
        }

        private string ToTitleCase(string str)
        {
             return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.Replace("-", " "));
        }
    }
}
