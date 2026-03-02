using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Neko.Configuration;

namespace Neko.Builder
{
    public class SiteBuilder
    {
        private readonly string _inputDirectory;
        private readonly string? _outputDirectoryOverride;
        private readonly bool _isWatchMode;
        private readonly string? _routePrefix;
        private NekoConfig _config;

        public string OutputDirectory { get; private set; }

        public SiteBuilder(string inputDirectory, string? outputDirectory = null, bool isWatchMode = false, string? routePrefix = null)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
            _outputDirectoryOverride = outputDirectory;
            _isWatchMode = isWatchMode;
            _routePrefix = routePrefix;
        }

        public async Task BuildAsync()
        {
            Console.WriteLine($"Building documentation from {_inputDirectory}...");

            // 1. Read Configuration
            var configPath = Path.Combine(_inputDirectory, "neko.yml");
            try
            {
                _config = ConfigParser.Parse(configPath);

                // If we are a sub-project (indicated by having a routePrefix), attempt to inherit from root config
                if (!string.IsNullOrEmpty(_routePrefix))
                {
                    // Find root directory by walking up until we find the highest neko.yml, or simply use the project root if known.
                    // Wait, _routePrefix tells us we're a subproject.
                    // Let's look for a root neko.yml in the parent directories up to the closest parent without a neko.yml.
                    // Or simpler: walk up the directory tree until we find the root config.
                    string currentDir = Path.GetDirectoryName(_inputDirectory);
                    while (currentDir != null)
                    {
                        var parentConfigPath = Path.Combine(currentDir, "neko.yml");
                        if (File.Exists(parentConfigPath))
                        {
                            try
                            {
                                var parentConfig = ConfigParser.Parse(parentConfigPath);
                                _config.MergeWith(parentConfig);
                            }
                            catch { }
                            break; // only merge with nearest parent config
                        }
                        currentDir = Path.GetDirectoryName(currentDir);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR parsing config: {ex}");
                // Fallback if config is missing or invalid?
                // ConfigParser usually handles missing file by returning default config.
                _config = new NekoConfig();
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
                OutputDirectory = Path.Combine(Path.GetTempPath(), "neko", projectName);
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

            var excludedDirs = new HashSet<string>();
            if (Directory.Exists(_inputDirectory))
            {
                var allConfigs = Directory.GetFiles(_inputDirectory, "neko.yml", SearchOption.AllDirectories);
                // Exclude any subdirectory that contains its own neko.yml (so it's handled as a separate project)
                foreach (var cfg in allConfigs)
                {
                    var dir = Path.GetDirectoryName(cfg);
                    if (dir != null && Path.GetFullPath(dir) != Path.GetFullPath(_inputDirectory))
                    {
                        var dirPath = Path.GetFullPath(dir);
                        if (!dirPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            dirPath += Path.DirectorySeparatorChar;
                        excludedDirs.Add(dirPath);
                    }
                }
            }

            // 2. Scan Files
            var scanner = new FileScanner(_inputDirectory, OutputDirectory, excludedDirs.Count > 0 ? excludedDirs : null);
            var files = scanner.Scan();

            // 3. Prepare Pipeline
            var parser = new MarkdownParser();

            string headIncludes = null;
            var headIncludesPath = Path.Combine(_inputDirectory, "_includes", "head.html");
            if (File.Exists(headIncludesPath))
            {
                try
                {
                    headIncludes = await File.ReadAllTextAsync(headIncludesPath);
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"Warning: Failed to read _includes/head.html: {ex.Message}");
                }
            }

            var generator = new HtmlGenerator(_config, _isWatchMode, headIncludes);
            var searchIndexer = new SearchIndexGenerator();

            // 4. Process Files
            var parsedDocs = new List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)>();

            // Pass 1: Parse all files
            foreach (var file in files)
            {
                Console.WriteLine($"Parsing {Path.GetFileName(file)}...");
                var markdown = await File.ReadAllTextAsync(file);
                var doc = parser.Parse(markdown, file, _inputDirectory);
                var relativePath = Path.GetRelativePath(_inputDirectory, file);
                parsedDocs.Add((file, relativePath, doc, markdown));
            }

            // Generate Sidebar
            var sidebarGenerator = new SidebarGenerator(_inputDirectory, parsedDocs, excludedDirs.Count > 0 ? excludedDirs : null, _routePrefix);
            var sidebarLinks = sidebarGenerator.Generate();

            // Auto-generate Navigation if empty
            if (_config.Links == null || _config.Links.Count == 0)
            {
                Console.WriteLine("Generating navigation from file system...");
                var navGenerator = new NavigationGenerator(_inputDirectory, parsedDocs.Select(p => (p.FilePath, p.RelativePath, p.Doc)).ToList());
                _config.Links = navGenerator.Generate();
            }
            else
            {
                // Enrich existing links with icons from frontmatter
                EnrichLinks(_config.Links, parsedDocs);
            }

            // Build Navigation Map
            var navigationMap = BuildNavigationMap(_config.Links);

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

                if (!string.IsNullOrEmpty(_routePrefix))
                {
                    sourceUrl = _routePrefix + sourceUrl;
                }

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

            // Identify Blog Posts & Changelog
            var blogPosts = parsedDocs
                .Where(p => p.RelativePath.Replace("\\", "/").StartsWith("blog/") && !p.RelativePath.EndsWith("index.md"))
                .Select(p => {
                    var url = "/" + p.RelativePath.Replace("\\", "/");
                    if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
                    if (!string.IsNullOrEmpty(_routePrefix)) url = _routePrefix + url;
                    return (p.Doc, url);
                })
                .OrderByDescending(p => p.Doc.FrontMatter.Date ?? "")
                .ToList();

            var changelogEntries = parsedDocs
                .Where(p => p.RelativePath.Replace("\\", "/").StartsWith("changelog/") && !p.RelativePath.EndsWith("index.md"))
                .Select(p => {
                    var url = "/" + p.RelativePath.Replace("\\", "/");
                    if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
                    if (!string.IsNullOrEmpty(_routePrefix)) url = _routePrefix + url;
                    return (p.Doc, url);
                })
                .OrderByDescending(p => p.Doc.FrontMatter.Date ?? "")
                .ToList();

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

                if (!string.IsNullOrEmpty(_routePrefix))
                {
                    relativeUrl = _routePrefix + relativeUrl;
                }

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

                var html = generator.Generate(item.Doc, backlinks, navContext, sidebarLinks, blogPosts, changelogEntries, relativeUrl);

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

            // Generate 404 Page
            var notFoundConfigPath = Path.Combine(_inputDirectory, "404.yml");
            NotFoundConfig notFoundConfig;
            if (File.Exists(notFoundConfigPath))
            {
                try
                {
                    notFoundConfig = ConfigParser.Parse<NotFoundConfig>(notFoundConfigPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to parse 404.yml: {ex.Message}. Using defaults.");
                    notFoundConfig = new NotFoundConfig();
                }
            }
            else
            {
                notFoundConfig = new NotFoundConfig();
            }

            var notFoundHtml = generator.GenerateNotFound(notFoundConfig);
            await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "404.html"), notFoundHtml);
            Console.WriteLine("Generated 404.html");

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

        private void EnrichLinks(List<LinkConfig> links, List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)> docs)
        {
            if (links == null) return;

            foreach (var link in links)
            {
                if (!string.IsNullOrEmpty(link.Link))
                {
                    // Find doc
                    var relativeUrl = link.Link;
                    if (!string.IsNullOrEmpty(_routePrefix) && relativeUrl.StartsWith(_routePrefix + "/"))
                    {
                        relativeUrl = relativeUrl.Substring(_routePrefix.Length);
                    }
                    if (relativeUrl.StartsWith("/")) relativeUrl = relativeUrl.Substring(1);
                    if (relativeUrl.EndsWith(".html")) relativeUrl = relativeUrl.Substring(0, relativeUrl.Length - 5);
                    if (relativeUrl.EndsWith(".md")) relativeUrl = relativeUrl.Substring(0, relativeUrl.Length - 3);

                    var docItem = docs.FirstOrDefault(d =>
                    {
                        var docUrl = d.RelativePath.Replace("\\", "/");
                        if (docUrl.EndsWith(".md")) docUrl = docUrl.Substring(0, docUrl.Length - 3);
                        return docUrl.Equals(relativeUrl, StringComparison.OrdinalIgnoreCase);
                    });

                    if (docItem.Doc != null)
                    {
                        if (string.IsNullOrEmpty(link.Icon) && !string.IsNullOrEmpty(docItem.Doc.FrontMatter.Icon))
                        {
                            link.Icon = docItem.Doc.FrontMatter.Icon;
                        }
                    }
                }

                if (link.Items != null && link.Items.Count > 0)
                {
                    EnrichLinks(link.Items, docs);
                }
            }
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

                    if (!string.IsNullOrEmpty(_routePrefix) && !url.Contains("://"))
                    {
                        if (url.StartsWith("/")) url = _routePrefix + url;
                        else url = _routePrefix + "/" + url;
                    }

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
                    // Resource name format: Neko.Resources.filename.ext
                    // We need to map it to assets/filename.ext

                    var relativeName = resourceName.Replace("Neko.Resources.", "");
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

            // Copy User Assets
            try
            {
                var directories = Directory.GetDirectories(_inputDirectory, "assets", SearchOption.AllDirectories);
                foreach (var dir in directories)
                {
                    // Avoid copying assets from the output directory itself if it's nested
                    // Also avoid .git and other common hidden folders if possible, but "assets" is specific.
                    // Checking if the assets folder is within the output directory to avoid infinite loops or conflicts
                    if (Path.GetFullPath(dir).StartsWith(Path.GetFullPath(outputDir), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Calculate relative path
                    var relativePath = Path.GetRelativePath(_inputDirectory, dir);
                    var targetDir = Path.Combine(outputDir, relativePath);

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        var fileRelativePath = Path.GetRelativePath(dir, file);
                        var targetFile = Path.Combine(targetDir, fileRelativePath);

                        var targetFileDir = Path.GetDirectoryName(targetFile);
                        if (!Directory.Exists(targetFileDir))
                        {
                            Directory.CreateDirectory(targetFileDir!);
                        }

                        File.Copy(file, targetFile, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error copying user assets: {ex.Message}");
            }
        }
    }
}
