using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TailDocs.CLI.Configuration;

namespace TailDocs.CLI.Builder
{
    public class SiteBuilder
    {
        private readonly string _inputDirectory;
        private readonly string? _outputDirectoryOverride;
        private TailDocsConfig _config;

        public string OutputDirectory { get; private set; }

        public SiteBuilder(string inputDirectory, string? outputDirectory = null)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
            _outputDirectoryOverride = outputDirectory;
        }

        public async Task BuildAsync()
        {
            Console.WriteLine($"Building documentation from {_inputDirectory}...");

            // 1. Read Configuration
            var configPath = Path.Combine(_inputDirectory, "taildocs.yml");
            try
            {
                _config = ConfigParser.Parse(configPath);
            }
            catch
            {
                // Fallback if config is missing or invalid?
                // ConfigParser usually handles missing file by returning default config.
                _config = new TailDocsConfig();
            }

            // Resolve output directory
            if (!string.IsNullOrEmpty(_outputDirectoryOverride))
            {
                // CLI arg is relative to current working directory
                OutputDirectory = Path.IsPathRooted(_outputDirectoryOverride)
                    ? _outputDirectoryOverride
                    : Path.GetFullPath(_outputDirectoryOverride);
            }
            else
            {
                // Default to temp folder as requested
                var projectName = new DirectoryInfo(_inputDirectory).Name;
                OutputDirectory = Path.Combine(Path.GetTempPath(), "taildocs", projectName);
            }

            Console.WriteLine($"Output directory: {OutputDirectory}");

            // Clear Output Directory
            if (Directory.Exists(OutputDirectory))
            {
                try
                {
                    Directory.Delete(OutputDirectory, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not clear output directory: {ex.Message}");
                }
            }
            Directory.CreateDirectory(OutputDirectory);

            // 2. Scan Files
            var scanner = new FileScanner(_inputDirectory, OutputDirectory);
            var files = scanner.Scan();

            // 3. Prepare Pipeline
            var parser = new MarkdownParser();
            var generator = new HtmlGenerator(_config);
            var searchIndexer = new SearchIndexGenerator();

            // 4. Process Files
            var parsedDocs = new List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)>();

            // Generate Sidebar
            var sidebarGenerator = new SidebarGenerator(_inputDirectory);
            var sidebarLinks = sidebarGenerator.Generate();

            // Build Navigation Map
            var navigationMap = BuildNavigationMap(sidebarLinks);

            // Pass 1: Parse all files
            foreach (var file in files)
            {
                Console.WriteLine($"Parsing {Path.GetFileName(file)}...");
                var markdown = await File.ReadAllTextAsync(file);
                var doc = parser.Parse(markdown);
                var relativePath = Path.GetRelativePath(_inputDirectory, file);
                parsedDocs.Add((file, relativePath, doc, markdown));
            }

            // Pass 2: Build Backlinks Map
            var backlinkMap = new Dictionary<string, List<(string Url, string Title)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parsedDocs)
            {
                var sourceRelativePath = item.RelativePath;
                var sourceTitle = !string.IsNullOrEmpty(item.Doc.FrontMatter.Title)
                    ? item.Doc.FrontMatter.Title
                    : Path.GetFileNameWithoutExtension(item.FilePath);

                // Source URL for the backlink (relative to root, no extension)
                var sourceUrl = "/" + sourceRelativePath.Replace("\\", "/");
                if (sourceUrl.EndsWith(".md")) sourceUrl = sourceUrl.Substring(0, sourceUrl.Length - 3);

                foreach (var link in item.Doc.OutgoingLinks)
                {
                    try
                    {
                        var sourceDir = Path.GetDirectoryName(item.FilePath) ?? "";

                        // Handle links starting with / as relative to root
                        string targetFullPath;
                        if (link.StartsWith("/"))
                        {
                            targetFullPath = Path.GetFullPath(Path.Combine(_inputDirectory, link.TrimStart('/')));
                        }
                        else
                        {
                            targetFullPath = Path.GetFullPath(Path.Combine(sourceDir, link));
                        }

                        // Normalize extension
                        if (!File.Exists(targetFullPath) && !targetFullPath.EndsWith(".md"))
                        {
                            if (File.Exists(targetFullPath + ".md"))
                                targetFullPath += ".md";
                        }

                        if (targetFullPath.StartsWith(_inputDirectory))
                        {
                            var targetRelativePath = Path.GetRelativePath(_inputDirectory, targetFullPath);

                            if (!backlinkMap.ContainsKey(targetRelativePath))
                            {
                                backlinkMap[targetRelativePath] = new List<(string, string)>();
                            }

                            if (!backlinkMap[targetRelativePath].Any(b => b.Url == sourceUrl))
                            {
                                backlinkMap[targetRelativePath].Add((sourceUrl, sourceTitle));
                            }
                        }
                    }
                    catch
                    {
                        // Ignore invalid paths
                    }
                }
            }

            // Pass 3: Generate HTML
            foreach (var item in parsedDocs)
            {
                Console.WriteLine($"Generating {Path.GetFileName(item.FilePath)}...");

                // Get backlinks
                List<(string Url, string Title)> backlinks = null;
                if (backlinkMap.TryGetValue(item.RelativePath, out var links))
                {
                    backlinks = links;
                }

                // Determine Navigation Context
                var relativeUrl = "/" + item.RelativePath.Replace("\\", "/");
                if (relativeUrl.EndsWith(".md")) relativeUrl = relativeUrl.Substring(0, relativeUrl.Length - 3);
                if (relativeUrl.EndsWith(".html")) relativeUrl = relativeUrl.Substring(0, relativeUrl.Length - 5);

                var navContext = new NavigationContext();

                // Find in navigation map
                var index = navigationMap.FindIndex(n => n.Url.Equals(relativeUrl, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    var entry = navigationMap[index];
                    navContext.Breadcrumbs = entry.Breadcrumbs;

                    if (index > 0)
                    {
                        var prev = navigationMap[index - 1];
                        navContext.Prev = new NavigationItem { Title = prev.Title, Url = prev.Url };
                    }

                    if (index < navigationMap.Count - 1)
                    {
                        var next = navigationMap[index + 1];
                        navContext.Next = new NavigationItem { Title = next.Title, Url = next.Url };
                    }
                }

                var html = generator.Generate(item.Doc, backlinks, navContext, sidebarLinks);

                var htmlFileName = Path.ChangeExtension(item.RelativePath, ".html");
                var outputPath = Path.Combine(OutputDirectory, htmlFileName);

                var outputDirForFile = Path.GetDirectoryName(outputPath);
                if (outputDirForFile != null && !Directory.Exists(outputDirForFile))
                {
                    Directory.CreateDirectory(outputDirForFile);
                }

                await File.WriteAllTextAsync(outputPath, html);

                searchIndexer.AddDocument(htmlFileName, item.Doc.FrontMatter.Title ?? Path.GetFileNameWithoutExtension(item.FilePath), item.Markdown);
            }

            await searchIndexer.WriteIndexAsync(OutputDirectory);

            // Copy Assets
            await CopyAssetsAsync(OutputDirectory);

            Console.WriteLine($"Build complete. Output in {OutputDirectory}");
        }

        private List<(string Url, string Title, List<NavigationItem> Breadcrumbs)> BuildNavigationMap(List<LinkConfig> links)
        {
            var map = new List<(string Url, string Title, List<NavigationItem> Breadcrumbs)>();
            BuildNavigationMapRecursive(links, map, new List<NavigationItem>());
            return map;
        }

        private void BuildNavigationMapRecursive(List<LinkConfig> links, List<(string Url, string Title, List<NavigationItem> Breadcrumbs)> map, List<NavigationItem> breadcrumbs)
        {
            if (links == null) return;

            foreach (var link in links)
            {
                var currentBreadcrumbs = new List<NavigationItem>(breadcrumbs);
                if (link.Items != null && link.Items.Count > 0)
                {
                    currentBreadcrumbs.Add(new NavigationItem { Title = link.Text, Url = null });
                }

                if (!string.IsNullOrEmpty(link.Link) && link.Link != "#")
                {
                    var url = link.Link;
                    if (!url.StartsWith("/") && !url.Contains("://"))
                    {
                        url = "/" + url;
                    }
                    if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
                    if (url.EndsWith(".html")) url = url.Substring(0, url.Length - 5);

                    map.Add((url, link.Text, new List<NavigationItem>(breadcrumbs))); // Add parent breadcrumbs to this item
                }

                if (link.Items != null && link.Items.Count > 0)
                {
                    BuildNavigationMapRecursive(link.Items, map, currentBreadcrumbs);
                }
            }
        }

        private async Task CopyAssetsAsync(string outputDir)
        {
            var assetsDir = Path.Combine(outputDir, "assets");
            if (!Directory.Exists(assetsDir))
            {
                Directory.CreateDirectory(assetsDir);
            }

            // Extract embedded resources
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (resourceName.EndsWith(".js") || resourceName.EndsWith(".css") || resourceName.EndsWith(".woff2"))
                {
                    // Resource name format: TailDocs.CLI.Resources.filename.ext
                    // We need to map it to assets/filename.ext

                    var relativeName = resourceName.Replace("TailDocs.CLI.Resources.", "");
                    string outputPath;

                    // Handle 'highlight' folder specifically
                    if (relativeName.StartsWith("highlight."))
                    {
                        var highlightDir = Path.Combine(assetsDir, "highlight");
                        if (!Directory.Exists(highlightDir)) Directory.CreateDirectory(highlightDir);

                        var fileName = relativeName.Substring("highlight.".Length);
                        outputPath = Path.Combine(highlightDir, fileName);
                    }
                    else
                    {
                        outputPath = Path.Combine(assetsDir, relativeName);
                    }

                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream != null)
                    {
                        using var fileStream = File.Create(outputPath);
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
        }
    }
}
