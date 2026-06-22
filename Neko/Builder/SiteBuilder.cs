using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Neko.Builder.Tailwind;
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

        // This sub-project's *own* identity (branding/breadcrumb label), captured
        // before it inherits the parent's branding. Used as a fallback for the
        // search breadcrumb when the project isn't named in the navbar. Null for
        // the root project (no route prefix).
        private string _projectOwnName;

        public string OutputDirectory { get; private set; }

        // State captured at the end of the last full BuildAsync so a subsequent
        // single-file change (watch mode) can regenerate just that page via
        // TryRebuildSinglePageAsync instead of rebuilding the whole project. Null
        // until the first full build completes.
        private List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)> _lastParsedDocs;
        private List<LinkConfig> _lastSidebarLinks;
        private List<(string Url, string Title, List<NavigationItem> Breadcrumbs)> _lastNavigationMap;
        private Dictionary<string, List<(string Url, string Title)>> _lastBacklinkMap;
        private List<(ParsedDocument Doc, string Url)> _lastBlogPosts;
        private HashSet<string> _lastSearchExcludedFolders;
        private Dictionary<string, List<string>> _lastSidebarBreadcrumbMap;
        private HashSet<string> _lastChangelogManagedFiles;
        private HtmlGenerator _lastGenerator;
        private string _lastProjectName;
        private (string FileName, string Title, string Content, string Description, string[] Tags, string[] Breadcrumbs)?[] _lastIndexRequests;
        private List<(string FileName, string Title, string Content, string Description, string[] Tags, string[] Breadcrumbs)> _lastChangelogIndexRequests;

        public SiteBuilder(string inputDirectory, string? outputDirectory = null, bool isWatchMode = false, string? routePrefix = null)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
            _outputDirectoryOverride = outputDirectory;
            _isWatchMode = isWatchMode;
            _routePrefix = routePrefix;
        }

        private static readonly SemaphoreSlim _singleBuild = new SemaphoreSlim(1, 1);

        public static string CurrentRoutePrefix { get; private set; }
        public async Task BuildAsync()
        {
            var curDir = Environment.CurrentDirectory;
            await _singleBuild.WaitAsync();
            CurrentRoutePrefix = _routePrefix;
            try
            {
                Console.WriteLine($"Building documentation from {_inputDirectory}...");
                // 1. Read Configuration
                var configPath = Path.Combine(_inputDirectory, "neko.yml");
                try
                {
                    _config = ConfigParser.Parse(configPath);

                    // Capture this project's own identity before the merge below
                    // pulls the parent's branding down (the parent's title/label
                    // would otherwise mask this sub-project's). Used as the search
                    // breadcrumb fallback when the navbar doesn't name the project.
                    if (!string.IsNullOrEmpty(_routePrefix))
                    {
                        _projectOwnName = ResolveOwnProjectName(_config);
                    }

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
                    // Default to the project's `.neko` build folder (honouring the
                    // neko.yml `output:` key), matching multi-repo mode instead of
                    // writing into the OS temp directory.
                    var outputName = string.IsNullOrWhiteSpace(_config.Output) ? ".neko" : _config.Output;
                    OutputDirectory = Path.GetFullPath(Path.Combine(_inputDirectory, outputName));
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

                Environment.CurrentDirectory = OutputDirectory;

                // Auto-detect favicon at the input root if not explicitly configured.
                if (string.IsNullOrEmpty(_config.Branding.Favicon))
                {
                    foreach (var candidate in new[] { "favicon.ico", "favicon.png" })
                    {
                        if (File.Exists(Path.Combine(_inputDirectory, candidate)))
                        {
                            _config.Branding.Favicon = "/" + candidate;
                            break;
                        }
                    }
                }

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
                var scanner = new FileScanner(_inputDirectory, OutputDirectory, excludedDirs.Count > 0 ? excludedDirs : null, _config.Ignore);
                var files = scanner.Scan();

                // 3. Prepare Pipeline
                var parser = new MarkdownParser(_config);

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
                var searchIndexer = new SearchIndexGenerator(_routePrefix);

                // Collect folders whose root yml opts the folder out of search indexing.
                var searchExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (Directory.Exists(_inputDirectory))
                {
                    foreach (var dir in Directory.GetDirectories(_inputDirectory, "*", SearchOption.AllDirectories))
                    {
                        var folderConfig = FolderConfig.LoadFromDirectory(dir);
                        if (folderConfig.SearchExclude || SidebarGenerator.IsHiddenVisibility(folderConfig.Visibility))
                        {
                            var dirPath = Path.GetFullPath(dir);
                            if (!dirPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                                dirPath += Path.DirectorySeparatorChar;
                            searchExcludedFolders.Add(dirPath);
                        }
                    }
                }

                // 4. Process Files
                var parsedDocs = new List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)>();

                // Pass 0: Warm the Tesserae compile cache in parallel. Compilation
                // otherwise happens inline during the (synchronous, sequential) parse
                // below — one sample at a time — because Markdig's renderer can't be
                // awaited. Compiling every sample up front turns each parse into a fast
                // cache hit and lets independent samples build concurrently.
                Builder.TesseraeCompiler.Configure(_config.Tesserae?.Version, _config.Tesserae?.MaxParallelism ?? 0);
                var tesseraeSamples = new List<(string Arguments, string Code)>();
                foreach (var file in files)
                {
                    try
                    {
                        var markdown = await File.ReadAllTextAsync(file);
                        tesseraeSamples.AddRange(parser.ExtractTesseraeSamples(markdown, file, _inputDirectory));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: failed to scan {Path.GetFileName(file)} for Tesserae samples: {ex.Message}");
                    }
                }
                if (tesseraeSamples.Count > 0)
                {
                    await Builder.TesseraeCompiler.WarmAsync(tesseraeSamples, Environment.CurrentDirectory);
                }

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

                // Resolve the friendly name shown as the leading search-breadcrumb
                // crumb for this sub-project. The navbar already maps each
                // sub-project's root link to a friendly label (e.g. "Connect &
                // Ingest"), so it is the single, preferred source; fall back to the
                // project's own branding, then a title-cased mount path.
                if (!string.IsNullOrEmpty(_routePrefix))
                {
                    searchIndexer.ProjectName = ResolveProjectBreadcrumbName(_config.Links, _projectOwnName, _routePrefix);
                }

                // Sidebar-derived breadcrumb trails for the search index. The sidebar
                // mirrors the folder hierarchy and covers every page, so it is the
                // primary source; the navbar `links:` config only nests pages that
                // sit under explicit dropdown groups.
                var sidebarBreadcrumbMap = BuildSidebarBreadcrumbMap(sidebarLinks);

                // Pass 2: Build Backlinks Map
                var backlinkMap = new Dictionary<string, List<(string Url, string Title)>>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in parsedDocs)
                {
                    var sourceRelativePath = item.RelativePath;
                    var sourceTitle = item.Doc.FrontMatter.Title;
                    if (string.IsNullOrEmpty(sourceTitle)) sourceTitle = item.Doc.FrontMatter.Label;
                    if (string.IsNullOrEmpty(sourceTitle)) sourceTitle = Path.GetFileNameWithoutExtension(item.FilePath);

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
                    .Select(p =>
                    {
                        var url = "/" + p.RelativePath.Replace("\\", "/");
                        if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
                        if (!string.IsNullOrEmpty(_routePrefix)) url = _routePrefix + url;
                        return (p.Doc, url);
                    })
                    .OrderByDescending(p => p.Doc.FrontMatter.Date ?? "")
                    .ToList();

                // Changelog folders: any folder whose index.yml / <foldername>.yml sets
                // `changelog: true`. Their version-named `.md` files are aggregated into a
                // single timeline page rendered at the folder URL (newest version first),
                // and are not emitted as standalone pages.
                var changelogFolders = DiscoverChangelogFolders(excludedDirs);
                var changelogManagedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var changelogEntriesByFolder = new Dictionary<string, List<(ParsedDocument Doc, string Url, string Version)>>(StringComparer.OrdinalIgnoreCase);

                if (changelogFolders.Count > 0)
                {
                    var versionedByFolder = new Dictionary<string, List<(ChangelogVersion Ver, ParsedDocument Doc)>>(StringComparer.OrdinalIgnoreCase);
                    var unversionedByFolder = new Dictionary<string, List<ParsedDocument>>(StringComparer.OrdinalIgnoreCase);

                    foreach (var p in parsedDocs)
                    {
                        var dir = Path.GetFullPath(Path.GetDirectoryName(p.FilePath) ?? string.Empty);
                        if (!changelogFolders.ContainsKey(dir)) continue;

                        // Every `.md` in a changelog folder is managed (not built as its own page).
                        changelogManagedFiles.Add(Path.GetFullPath(p.FilePath));

                        var fileName = Path.GetFileName(p.FilePath);
                        if (fileName.Equals("index.md", StringComparison.OrdinalIgnoreCase)
                            || fileName.Equals("readme.md", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // intro page, not a version entry
                        }

                        var nameNoExt = Path.GetFileNameWithoutExtension(p.FilePath);
                        if (ChangelogVersion.TryParse(nameNoExt, out var version))
                        {
                            if (!versionedByFolder.TryGetValue(dir, out var list))
                                versionedByFolder[dir] = list = new List<(ChangelogVersion, ParsedDocument)>();
                            list.Add((version, p.Doc));
                        }
                        else
                        {
                            if (!unversionedByFolder.TryGetValue(dir, out var list))
                                unversionedByFolder[dir] = list = new List<ParsedDocument>();
                            list.Add(p.Doc);
                        }
                    }

                    foreach (var dir in changelogFolders.Keys)
                    {
                        var entries = new List<(ParsedDocument Doc, string Url, string Version)>();

                        if (versionedByFolder.TryGetValue(dir, out var versioned))
                        {
                            foreach (var v in versioned.OrderByDescending(x => x.Ver))
                            {
                                entries.Add((v.Doc, "#" + v.Ver.Anchor, v.Ver.Display));
                            }
                        }

                        // Any non-version files fall back to date ordering, listed after versions.
                        if (unversionedByFolder.TryGetValue(dir, out var unversioned))
                        {
                            foreach (var doc in unversioned.OrderByDescending(d => d.FrontMatter.Date ?? string.Empty))
                            {
                                var label = !string.IsNullOrEmpty(doc.FrontMatter.Title) ? doc.FrontMatter.Title : doc.FrontMatter.Label;
                                entries.Add((doc, null, label ?? string.Empty));
                            }
                        }

                        changelogEntriesByFolder[dir] = entries;
                    }
                }

                // Pass 3: Generate HTML. Pages are independent, so page generation and
                // the file writes run in parallel. Search-index entries are collected
                // per page and added afterwards in source order so search.json stays
                // deterministic regardless of completion order.
                var indexRequests =
                    new (string FileName, string Title, string Content, string Description, string[] Tags, string[] Breadcrumbs)?[parsedDocs.Count];

                await Parallel.ForEachAsync(
                    Enumerable.Range(0, parsedDocs.Count),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (pageIndex, ct) =>
                {
                    indexRequests[pageIndex] = await RenderAndWritePageAsync(
                        parsedDocs[pageIndex], generator, sidebarLinks, navigationMap,
                        backlinkMap, blogPosts, searchExcludedFolders, sidebarBreadcrumbMap,
                        changelogManagedFiles, ct);
                });

                // Add the collected search documents in deterministic source order so
                // the generated search.json doesn't churn between builds.
                foreach (var request in indexRequests)
                {
                    if (request == null) continue;
                    var r = request.Value;
                    searchIndexer.AddDocument(r.FileName, r.Title, r.Content, r.Description, r.Tags, r.Breadcrumbs);
                }

                // Pass 4: Generate one aggregated timeline page per changelog folder.
                // Collect their search-index entries here (rather than adding them to
                // the indexer inline) so the cached state can rebuild search.json on an
                // incremental page rebuild.
                var changelogIndexRequests = new List<(string FileName, string Title, string Content, string Description, string[] Tags, string[] Breadcrumbs)>();
                foreach (var (folderFullPath, folderConfig) in changelogFolders)
                {
                    if (!changelogEntriesByFolder.TryGetValue(folderFullPath, out var entries)) continue;

                    var relativeFolder = Path.GetRelativePath(_inputDirectory, folderFullPath).Replace("\\", "/");
                    var folderUrl = "/" + relativeFolder;
                    if (!string.IsNullOrEmpty(_routePrefix)) folderUrl = _routePrefix + folderUrl;

                    // Use an existing index.md / readme.md as the intro, otherwise synthesize
                    // a heading + lead paragraph from the folder yml title/description.
                    var indexEntry = parsedDocs.FirstOrDefault(p =>
                        Path.GetFullPath(Path.GetDirectoryName(p.FilePath) ?? string.Empty).Equals(folderFullPath, StringComparison.OrdinalIgnoreCase)
                        && (Path.GetFileName(p.FilePath).Equals("index.md", StringComparison.OrdinalIgnoreCase)
                            || Path.GetFileName(p.FilePath).Equals("readme.md", StringComparison.OrdinalIgnoreCase)));

                    ParsedDocument pageDoc;
                    if (indexEntry.Doc != null)
                    {
                        pageDoc = indexEntry.Doc;
                        if (string.IsNullOrEmpty(pageDoc.FrontMatter.Title)) pageDoc.FrontMatter.Title = folderConfig.Title ?? folderConfig.Label;
                        if (string.IsNullOrEmpty(pageDoc.FrontMatter.Description)) pageDoc.FrontMatter.Description = folderConfig.Description;
                    }
                    else
                    {
                        var heading = folderConfig.Title ?? folderConfig.Label ?? "Changelog";
                        var intro = new System.Text.StringBuilder();
                        intro.AppendLine($"<h1>{System.Net.WebUtility.HtmlEncode(heading)}</h1>");
                        if (!string.IsNullOrEmpty(folderConfig.Description))
                        {
                            intro.AppendLine($"<p class=\"text-lg text-gray-600 dark:text-gray-400\">{System.Net.WebUtility.HtmlEncode(folderConfig.Description)}</p>");
                        }
                        pageDoc = new ParsedDocument
                        {
                            Html = intro.ToString(),
                            FrontMatter = new FrontMatter
                            {
                                Title = heading,
                                Description = folderConfig.Description,
                                Icon = folderConfig.Icon,
                            },
                        };
                    }
                    pageDoc.FrontMatter.Layout = "changelog";

                    // Navigation context (breadcrumbs / prev-next) for the folder URL.
                    var navContext = new NavigationContext();
                    var navIndex = navigationMap.FindIndex(n => n.Url.Equals(folderUrl, StringComparison.OrdinalIgnoreCase));
                    if (navIndex >= 0)
                    {
                        navContext.Breadcrumbs = navigationMap[navIndex].Breadcrumbs;
                        if (navIndex > 0) navContext.Prev = new NavigationItem { Title = navigationMap[navIndex - 1].Title, Url = navigationMap[navIndex - 1].Url };
                        if (navIndex < navigationMap.Count - 1) navContext.Next = new NavigationItem { Title = navigationMap[navIndex + 1].Title, Url = navigationMap[navIndex + 1].Url };
                    }

                    var changelogHtml = generator.Generate(pageDoc, null, navContext, sidebarLinks, blogPosts, entries, folderUrl);

                    var outputPath = Path.Combine(OutputDirectory, relativeFolder.Replace('/', Path.DirectorySeparatorChar), "index.html");
                    var outputDirForFile = Path.GetDirectoryName(outputPath);
                    if (outputDirForFile != null && !Directory.Exists(outputDirForFile)) Directory.CreateDirectory(outputDirForFile);
                    await File.WriteAllTextAsync(outputPath, changelogHtml);
                    Console.WriteLine($"Generated changelog {folderUrl}");

                    // Index the aggregated page (unless the folder opts out / is protected).
                    var folderSearchExcluded = folderConfig.SearchExclude
                        || SidebarGenerator.IsHiddenVisibility(folderConfig.Visibility)
                        || IsInDotOrUnderscoreFolder(relativeFolder + "/index.md");
                    var isProtectedSite = !string.IsNullOrEmpty(_config.Password);
                    if (!folderSearchExcluded && !isProtectedSite)
                    {
                        var indexable = generator.BuildIndexableContent(pageDoc, blogPosts, entries, folderUrl);
                        var indexTitle = !string.IsNullOrWhiteSpace(pageDoc.FrontMatter.Title)
                            ? pageDoc.FrontMatter.Title
                            : (folderConfig.Label ?? "Changelog");
                        changelogIndexRequests.Add((
                            relativeFolder + "/index.html",
                            indexTitle,
                            indexable,
                            pageDoc.FrontMatter.Description,
                            pageDoc.FrontMatter.Tags,
                            null));
                    }
                }

                foreach (var r in changelogIndexRequests)
                {
                    searchIndexer.AddDocument(r.FileName, r.Title, r.Content, r.Description, r.Tags, r.Breadcrumbs);
                }

                await searchIndexer.WriteIndexAsync(OutputDirectory);

                // Generate Redirect Pages
                await GenerateRedirectPagesAsync(parsedDocs);

                // Generate Sitemap (root project only; sub-projects share the output dir
                // and would otherwise clobber each other's sitemap.xml).
                if (_config.Sitemap && string.IsNullOrEmpty(_routePrefix))
                {
                    var rawUrl = _config.Url;
                    var isPlaceholderUrl = string.IsNullOrWhiteSpace(rawUrl)
                        || rawUrl.Equals("localhost", StringComparison.OrdinalIgnoreCase);

                    if (isPlaceholderUrl)
                    {
                        Console.WriteLine("Skipping sitemap.xml: `url` is not configured in neko.yml.");
                    }
                    else
                    {
                        Console.WriteLine("Generating sitemap.xml...");
                        var baseUrl = rawUrl;
                        if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
                        {
                            baseUrl = "https://" + baseUrl;
                        }
                        baseUrl = baseUrl.TrimEnd('/');

                        var sitemapContent = new System.Text.StringBuilder();
                        sitemapContent.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                        sitemapContent.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

                        foreach (var item in parsedDocs)
                        {
                            // Changelog version files are not standalone pages; the
                            // aggregated folder URL is added separately below.
                            if (changelogManagedFiles.Contains(Path.GetFullPath(item.FilePath))) continue;

                            // Skip password-protected pages — their URLs render an unlock prompt,
                            // so there's no reason to advertise them to crawlers.
                            var pagePassword = item.Doc.FrontMatter.Password;
                            var isPageOptedOut = !string.IsNullOrEmpty(pagePassword)
                                && pagePassword.Equals("none", StringComparison.OrdinalIgnoreCase);
                            var isProtected = !string.IsNullOrEmpty(pagePassword) && !isPageOptedOut
                                || string.IsNullOrEmpty(pagePassword) && !string.IsNullOrEmpty(_config.Password);
                            if (isProtected) continue;

                            // Build a clean, extensionless URL (matching the rest of the site).
                            var path = "/" + item.RelativePath.Replace("\\", "/");
                            if (path.EndsWith(".md")) path = path.Substring(0, path.Length - 3);
                            if (path.EndsWith("/index")) path = path.Substring(0, path.Length - "index".Length);

                            var loc = System.Security.SecurityElement.Escape(baseUrl + path);

                            string lastmod;
                            try
                            {
                                lastmod = File.GetLastWriteTimeUtc(item.FilePath).ToString("yyyy-MM-dd");
                            }
                            catch
                            {
                                lastmod = DateTime.UtcNow.ToString("yyyy-MM-dd");
                            }

                            sitemapContent.AppendLine("  <url>");
                            sitemapContent.AppendLine($"    <loc>{loc}</loc>");
                            sitemapContent.AppendLine($"    <lastmod>{lastmod}</lastmod>");
                            sitemapContent.AppendLine("  </url>");
                        }

                        // One sitemap entry per aggregated changelog page (clean folder URL).
                        if (!string.IsNullOrEmpty(_config.Password))
                        {
                            // whole-site password: changelog pages are gated, skip them
                        }
                        else
                        {
                            foreach (var folderFullPath in changelogFolders.Keys)
                            {
                                var relativeFolder = Path.GetRelativePath(_inputDirectory, folderFullPath).Replace("\\", "/");
                                var loc = System.Security.SecurityElement.Escape(baseUrl + "/" + relativeFolder);
                                sitemapContent.AppendLine("  <url>");
                                sitemapContent.AppendLine($"    <loc>{loc}</loc>");
                                sitemapContent.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                                sitemapContent.AppendLine("  </url>");
                            }
                        }

                        sitemapContent.AppendLine("</urlset>");
                        await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "sitemap.xml"), sitemapContent.ToString());
                    }
                }

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

                // Generate the static Tailwind stylesheet now that every page and
                // asset has been written — the generator scans the emitted HTML/JS
                // for the utility classes actually used and emits only those, plus
                // the shipped preflight + typography layers.
                await GenerateTailwindAsync(OutputDirectory);

                // Generate CNAME if applicable
                var customCnamePath = Path.Combine(_inputDirectory, "CNAME");
                if (!File.Exists(customCnamePath) && _config.Cname?.ToLower() != "false")
                {
                    string cnameValue = _config.Cname?.ToLower() == "true" || string.IsNullOrEmpty(_config.Cname)
                        ? _config.Url
                        : _config.Cname;

                    if (!string.IsNullOrEmpty(cnameValue))
                    {
                        // Clean URL to get just the domain
                        var domain = cnameValue.Replace("http://", "").Replace("https://", "");
                        int slashIndex = domain.IndexOf('/');
                        if (slashIndex >= 0)
                        {
                            domain = domain.Substring(0, slashIndex);
                        }
                        domain = domain.Trim();

                        if (!string.IsNullOrEmpty(domain) && domain != "localhost")
                        {
                            await File.WriteAllTextAsync(Path.Combine(OutputDirectory, "CNAME"), domain);
                        }
                    }
                }

                // Cache this build's cross-page state so a subsequent single-file
                // change can regenerate just that page (watch mode) instead of
                // rebuilding the whole project. See TryRebuildSinglePageAsync.
                _lastParsedDocs             = parsedDocs;
                _lastSidebarLinks           = sidebarLinks;
                _lastNavigationMap          = navigationMap;
                _lastBacklinkMap            = backlinkMap;
                _lastBlogPosts              = blogPosts;
                _lastSearchExcludedFolders  = searchExcludedFolders;
                _lastSidebarBreadcrumbMap   = sidebarBreadcrumbMap;
                _lastChangelogManagedFiles  = changelogManagedFiles;
                _lastGenerator              = generator;
                _lastProjectName            = searchIndexer.ProjectName;
                _lastIndexRequests          = indexRequests;
                _lastChangelogIndexRequests = changelogIndexRequests;

                Console.WriteLine($"Build complete. Output in {OutputDirectory}");
            }
            finally
            {
                _singleBuild.Release();
                Environment.CurrentDirectory = curDir;
            }
        }

        // Renders one page to HTML, writes it to the output directory, and returns
        // the page's search-index request (or null when the page is changelog-managed,
        // password-protected, or search-excluded). Shared by the full build (run in
        // parallel across all pages) and the watch-mode single-page fast path.
        private async Task<(string FileName, string Title, string Content, string Description, string[] Tags, string[] Breadcrumbs)?> RenderAndWritePageAsync(
            (string FilePath, string RelativePath, ParsedDocument Doc, string Markdown) item,
            HtmlGenerator generator,
            List<LinkConfig> sidebarLinks,
            List<(string Url, string Title, List<NavigationItem> Breadcrumbs)> navigationMap,
            Dictionary<string, List<(string Url, string Title)>> backlinkMap,
            List<(ParsedDocument Doc, string Url)> blogPosts,
            HashSet<string> searchExcludedFolders,
            Dictionary<string, List<string>> sidebarBreadcrumbMap,
            HashSet<string> changelogManagedFiles,
            CancellationToken ct)
        {
            // Files inside a changelog folder are aggregated into a single page
            // (generated separately), so they are not emitted individually here.
            if (changelogManagedFiles.Contains(Path.GetFullPath(item.FilePath)))
            {
                return null;
            }

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

            // Lesson-step navigation: if this page lives inside a [!lesson] folder,
            // expose prev/next siblings in the curriculum's own order.
            var (lessonPrev, lessonNext) = Neko.Extensions.LessonExtension.GetLessonStepNavigation(item.FilePath, _inputDirectory);
            if (lessonPrev != null || lessonNext != null)
            {
                navContext.IsLessonStep = true;
                if (lessonPrev != null) navContext.LessonPrev = new NavigationItem { Title = lessonPrev.Title, Url = lessonPrev.Url };
                if (lessonNext != null) navContext.LessonNext = new NavigationItem { Title = lessonNext.Title, Url = lessonNext.Url };
            }

            var html = generator.Generate(item.Doc, backlinks, navContext, sidebarLinks, blogPosts, null, relativeUrl);

            var htmlFileName = Path.ChangeExtension(item.RelativePath, ".html");
            var outputPath = Path.Combine(OutputDirectory, htmlFileName);

            var outputDirForFile = Path.GetDirectoryName(outputPath);
            if (outputDirForFile != null && !Directory.Exists(outputDirForFile))
            {
                Directory.CreateDirectory(outputDirForFile);
            }

            await File.WriteAllTextAsync(outputPath, html, ct);

            // Index only pages whose content is publicly visible. Pages with a
            // per-page password (or a site-wide password without `password: none`)
            // would otherwise leak plaintext into search.json.
            var pagePassword = item.Doc.FrontMatter.Password;
            var isPageOptedOut = !string.IsNullOrEmpty(pagePassword)
                && pagePassword.Equals("none", StringComparison.OrdinalIgnoreCase);
            var isProtected = !string.IsNullOrEmpty(pagePassword) && !isPageOptedOut
                || string.IsNullOrEmpty(pagePassword) && !string.IsNullOrEmpty(_config.Password);

            var isSearchExcluded = item.Doc.FrontMatter.SearchExclude
                || SidebarGenerator.IsHiddenVisibility(item.Doc.FrontMatter.Visibility)
                || IsInSearchExcludedFolder(item.FilePath, searchExcludedFolders)
                || IsInDotOrUnderscoreFolder(item.RelativePath);

            if (isProtected || isSearchExcluded)
            {
                return null;
            }

            var indexableContent = generator.BuildIndexableContent(item.Doc, blogPosts, null, relativeUrl);
            var indexTitle = item.Doc.FrontMatter.Title;
            if (string.IsNullOrWhiteSpace(indexTitle))
            {
                indexTitle = item.Doc.FrontMatter.Label;
            }
            if (string.IsNullOrWhiteSpace(indexTitle))
            {
                // Prefer an H1, but accept the first heading at any level — some pages
                // open with `## Title` (e.g. when the sidebar already shows the label)
                // and would otherwise fall through to the bare filename.
                indexTitle = item.Doc.Toc.FirstOrDefault(x => x.Level == 1)?.Title
                    ?? item.Doc.Toc.FirstOrDefault()?.Title;
            }
            if (string.IsNullOrWhiteSpace(indexTitle))
            {
                indexTitle = Path.GetFileNameWithoutExtension(item.FilePath);
            }
            // Ancestor group titles shown by the search UI as a breadcrumb trail
            // instead of the raw file path. Root-level pages have no sidebar
            // ancestors; for those, fall back to the navbar trail so pages nested
            // under navbar dropdown groups still get a crumb.
            var crumbSource = sidebarBreadcrumbMap.TryGetValue(relativeUrl, out var sidebarCrumbs) && sidebarCrumbs.Count > 0
                ? sidebarCrumbs
                : navContext.Breadcrumbs.Select(b => b.Title).ToList();
            var breadcrumbTitles = crumbSource
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();

            return (
                htmlFileName,
                indexTitle,
                indexableContent,
                item.Doc.FrontMatter.Description,
                item.Doc.FrontMatter.Tags,
                breadcrumbTitles.Length > 0 ? breadcrumbTitles : null);
        }

        /// <summary>
        /// Watch-mode fast path: regenerate just the single page whose source file
        /// changed, reusing the navigation/sidebar/backlink/search state captured by
        /// the last full <see cref="BuildAsync"/>. Returns <c>false</c> when the
        /// change can't be handled in isolation (no prior build, a new/deleted file,
        /// a non-Markdown file, a changelog-managed file, or a change to frontmatter /
        /// outgoing links that affects other pages) so the caller can fall back to a
        /// full rebuild.
        /// </summary>
        public async Task<bool> TryRebuildSinglePageAsync(string changedFullPath)
        {
            // No prior full build to reuse, or output was never produced.
            if (_lastParsedDocs == null || string.IsNullOrEmpty(OutputDirectory)) return false;

            // Only Markdown body edits qualify. yml/config/asset changes can move
            // pages, retitle the sidebar, or change the project layout.
            if (!changedFullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) return false;

            var fullPath = Path.GetFullPath(changedFullPath);

            // A file not present in the last build (newly created) or one that was
            // deleted is structural — fall back to a full rebuild.
            var idx = _lastParsedDocs.FindIndex(d =>
                string.Equals(Path.GetFullPath(d.FilePath), fullPath, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return false;
            if (!File.Exists(fullPath)) return false;
            if (_lastChangelogManagedFiles != null && _lastChangelogManagedFiles.Contains(fullPath)) return false;

            await _singleBuild.WaitAsync();
            var curDir = Environment.CurrentDirectory;
            CurrentRoutePrefix = _routePrefix;
            try
            {
                var old = _lastParsedDocs[idx];
                var markdown = await File.ReadAllTextAsync(fullPath);
                var parser = new MarkdownParser(_config);

                // Warm the Tesserae cache for any live samples on this page so the
                // (synchronous) parse below is a fast cache hit.
                Builder.TesseraeCompiler.Configure(_config.Tesserae?.Version, _config.Tesserae?.MaxParallelism ?? 0);
                var samples = parser.ExtractTesseraeSamples(markdown, fullPath, _inputDirectory);
                if (samples.Count > 0)
                {
                    await Builder.TesseraeCompiler.WarmAsync(samples, OutputDirectory);
                }

                var doc = parser.Parse(markdown, fullPath, _inputDirectory);

                // If anything other pages depend on changed (sidebar label, order,
                // the title shown in their prev/next, outgoing links feeding their
                // backlinks, search-affecting flags, …) a single-page rebuild would
                // leave the rest of the site stale — bail to a full rebuild.
                if (RequiresFullRebuild(old.Doc, doc)) return false;

                Environment.CurrentDirectory = OutputDirectory;

                var newItem = (old.FilePath, old.RelativePath, doc, markdown);
                var request = await RenderAndWritePageAsync(
                    newItem, _lastGenerator, _lastSidebarLinks, _lastNavigationMap,
                    _lastBacklinkMap, _lastBlogPosts, _lastSearchExcludedFolders,
                    _lastSidebarBreadcrumbMap, _lastChangelogManagedFiles, CancellationToken.None);

                // Keep the cached state consistent for the next incremental rebuild.
                _lastParsedDocs[idx]    = newItem;
                _lastIndexRequests[idx] = request;

                // search.json and the Tailwind stylesheet are whole-site artifacts;
                // refresh them from the (now-updated) cached state and emitted HTML.
                await RewriteSearchIndexAsync();
                await GenerateTailwindAsync(OutputDirectory);

                Console.WriteLine($"Incrementally rebuilt {old.RelativePath}.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Incremental rebuild failed ({ex.Message}); falling back to a full build.");
                return false;
            }
            finally
            {
                _singleBuild.Release();
                Environment.CurrentDirectory = curDir;
            }
        }

        // Frontmatter fields (and the outgoing-link set) that, when changed, affect
        // pages other than the one edited — sidebar entries, prev/next titles, blog
        // ordering, backlinks, and search-index membership. Body-only edits leave all
        // of these untouched, so the single-page fast path is safe; otherwise the
        // caller must do a full rebuild.
        private static bool RequiresFullRebuild(ParsedDocument oldDoc, ParsedDocument newDoc)
        {
            var a = oldDoc.FrontMatter;
            var b = newDoc.FrontMatter;

            if (!string.Equals(a.Title, b.Title, StringComparison.Ordinal)) return true;
            if (!string.Equals(a.Label, b.Label, StringComparison.Ordinal)) return true;
            if (!string.Equals(a.Icon, b.Icon, StringComparison.Ordinal)) return true;
            if (a.Order != b.Order) return true;
            if (!string.Equals(a.Visibility, b.Visibility, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(a.Layout, b.Layout, StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(a.RedirectSlug, b.RedirectSlug, StringComparison.Ordinal)) return true;
            if (!string.Equals(a.Password, b.Password, StringComparison.Ordinal)) return true;
            if (a.SearchExclude != b.SearchExclude) return true;
            // Date drives blog/changelog ordering on other pages.
            if (!string.Equals(a.Date, b.Date, StringComparison.Ordinal)) return true;
            if (!StringArraysEqual(a.Tags, b.Tags)) return true;
            // Outgoing links feed other pages' backlink lists.
            if (!StringListsEqual(oldDoc.OutgoingLinks, newDoc.OutgoingLinks)) return true;

            return false;
        }

        private static bool StringArraysEqual(string[] a, string[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            var la = a?.Length ?? 0;
            var lb = b?.Length ?? 0;
            if (la != lb) return false;
            for (int i = 0; i < la; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            return true;
        }

        private static bool StringListsEqual(List<string> a, List<string> b)
        {
            var ca = a?.Count ?? 0;
            var cb = b?.Count ?? 0;
            if (ca != cb) return false;
            for (int i = 0; i < ca; i++)
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
            return true;
        }

        // Rebuilds search.json from the cached per-page and changelog index requests
        // (with the changed page's entry already updated). Reproduces the exact same
        // document set and order a full build would, without re-parsing every page.
        private async Task RewriteSearchIndexAsync()
        {
            var indexer = new SearchIndexGenerator(_routePrefix) { ProjectName = _lastProjectName };

            if (_lastIndexRequests != null)
            {
                foreach (var request in _lastIndexRequests)
                {
                    if (request == null) continue;
                    var r = request.Value;
                    indexer.AddDocument(r.FileName, r.Title, r.Content, r.Description, r.Tags, r.Breadcrumbs);
                }
            }

            if (_lastChangelogIndexRequests != null)
            {
                foreach (var r in _lastChangelogIndexRequests)
                {
                    indexer.AddDocument(r.FileName, r.Title, r.Content, r.Description, r.Tags, r.Breadcrumbs);
                }
            }

            await indexer.WriteIndexAsync(OutputDirectory);
        }

        private async Task GenerateRedirectPagesAsync(List<(string FilePath, string RelativePath, ParsedDocument Doc, string Markdown)> parsedDocs)
        {
            var redirectDir = Path.Combine(OutputDirectory, "redirect");
            var seenSlugs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parsedDocs)
            {
                var rawSlug = item.Doc.FrontMatter.RedirectSlug;
                if (string.IsNullOrWhiteSpace(rawSlug)) continue;

                var slug = NormalizeRedirectSlug(rawSlug);
                if (string.IsNullOrEmpty(slug))
                {
                    Console.WriteLine($"Warning: redirectSlug '{rawSlug}' in {item.RelativePath} is empty after normalization; skipping.");
                    continue;
                }

                if (seenSlugs.TryGetValue(slug, out var existingPath))
                {
                    Console.WriteLine($"Warning: redirectSlug '{slug}' in {item.RelativePath} conflicts with {existingPath}; skipping.");
                    continue;
                }
                seenSlugs[slug] = item.RelativePath;

                var targetUrl = "/" + item.RelativePath.Replace("\\", "/");
                if (targetUrl.EndsWith(".md")) targetUrl = targetUrl.Substring(0, targetUrl.Length - 3);
                if (targetUrl.EndsWith("/index")) targetUrl = targetUrl.Substring(0, targetUrl.Length - "index".Length);
                if (!string.IsNullOrEmpty(_routePrefix)) targetUrl = _routePrefix + targetUrl;

                if (!Directory.Exists(redirectDir)) Directory.CreateDirectory(redirectDir);

                var html = BuildRedirectHtml(targetUrl);
                var outputPath = Path.Combine(redirectDir, slug + ".html");
                await File.WriteAllTextAsync(outputPath, html);
                Console.WriteLine($"Generated redirect /redirect/{slug} -> {targetUrl}");
            }
        }

        private static string NormalizeRedirectSlug(string raw)
        {
            var trimmed = raw.Trim().Trim('/');
            // Disallow path separators inside a slug — `/redirect/{slug}` is a flat namespace.
            if (trimmed.Contains('/') || trimmed.Contains('\\')) return string.Empty;
            return trimmed;
        }

        private static string BuildRedirectHtml(string targetUrl)
        {
            var escapedHtml = System.Net.WebUtility.HtmlEncode(targetUrl);
            var escapedJs = System.Web.HttpUtility.JavaScriptStringEncode(targetUrl);
            return "<!DOCTYPE html>\n"
                 + "<html lang=\"en\">\n"
                 + "<head>\n"
                 + "  <meta charset=\"utf-8\">\n"
                 + $"  <meta http-equiv=\"refresh\" content=\"0; url={escapedHtml}\">\n"
                 + $"  <link rel=\"canonical\" href=\"{escapedHtml}\">\n"
                 + "  <meta name=\"robots\" content=\"noindex\">\n"
                 + "  <title>Redirecting…</title>\n"
                 + $"  <script>window.location.replace(\"{escapedJs}\");</script>\n"
                 + "</head>\n"
                 + "<body>\n"
                 + $"  <p>Redirecting to <a href=\"{escapedHtml}\">{escapedHtml}</a>…</p>\n"
                 + "</body>\n"
                 + "</html>\n";
        }

        // Find every folder under the input root whose folder config (index.yml /
        // <foldername>.yml) sets `changelog: true`. Keyed by normalized full path.
        // <paramref name="excludedDirs"/> holds the roots of sub-projects (folders
        // with their own neko.yml); folders living under those are skipped because
        // the sub-project builds its own changelog. Without this the root build emits
        // an empty duplicate page that shadows the real one in the dev server.
        private Dictionary<string, FolderConfig> DiscoverChangelogFolders(HashSet<string> excludedDirs)
        {
            var result = new Dictionary<string, FolderConfig>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(_inputDirectory)) return result;

            foreach (var dir in Directory.GetDirectories(_inputDirectory, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(dir);
                if (name.StartsWith(".") || name.StartsWith("_")) continue;

                if (IsInExcludedSubProject(dir, excludedDirs)) continue;

                var config = FolderConfig.LoadFromDirectory(dir);
                if (config.Changelog)
                {
                    result[Path.GetFullPath(dir)] = config;
                }
            }

            return result;
        }

        // True when <paramref name="dir"/> lives inside one of the sub-project roots
        // in <paramref name="excludedDirs"/>. The excluded roots are stored with a
        // trailing separator; a separator is appended to the candidate too so that
        // a sibling like `workspace-api` is not matched by the `workspace` prefix.
        private static bool IsInExcludedSubProject(string dir, HashSet<string> excludedDirs)
        {
            if (excludedDirs == null || excludedDirs.Count == 0) return false;
            var fullPath = Path.GetFullPath(dir);
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                fullPath += Path.DirectorySeparatorChar;
            foreach (var excluded in excludedDirs)
            {
                if (fullPath.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool IsInSearchExcludedFolder(string filePath, HashSet<string> excludedFolders)
        {
            if (excludedFolders.Count == 0) return false;
            var fullPath = Path.GetFullPath(filePath);
            foreach (var folder in excludedFolders)
            {
                if (fullPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // Pages living under a folder whose name starts with "." or "_" (e.g.
        // `_helpers`, `_reference-material`, `.template`) are treated as private
        // scaffolding and kept out of the search index. The check looks at every
        // directory segment of the page's path relative to the input root.
        private static bool IsInDotOrUnderscoreFolder(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return false;
            var segments = relativePath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            // The final segment is the file itself; only directory segments count.
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var s = segments[i];
                if (s.Length > 0 && (s[0] == '.' || s[0] == '_')) return true;
            }
            return false;
        }

        // Maps a page's root-relative URL (no extension, leading slash) to the
        // titles of its sidebar ancestor groups, e.g. "/guides/core/page" →
        // ["Guides", "Core"]. Sidebar links already carry the route prefix, so
        // unlike BuildNavigationMap no prefixing is applied here.
        private static Dictionary<string, List<string>> BuildSidebarBreadcrumbMap(List<LinkConfig> links)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            void Walk(List<LinkConfig> items, List<string> trail)
            {
                if (items == null) return;
                foreach (var link in items)
                {
                    if (!string.IsNullOrEmpty(link.Link) && link.Link != "#" && !link.Link.Contains("://"))
                    {
                        var url = link.Link.Replace('\\', '/');
                        if (url.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) url = url.Substring(0, url.Length - 3);
                        if (url.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) url = url.Substring(0, url.Length - 5);
                        if (!url.StartsWith("/")) url = "/" + url;
                        map[url] = trail;
                    }

                    if (link.Items != null && link.Items.Count > 0)
                    {
                        var next = trail;
                        if (!string.IsNullOrWhiteSpace(link.Text))
                        {
                            next = new List<string>(trail) { link.Text };
                        }
                        Walk(link.Items, next);
                    }
                }
            }

            Walk(links, new List<string>());
            return map;
        }

        // The single, unified label for a sub-project in the search breadcrumb.
        // The navbar already names every sub-project (its root link's text), so it
        // is the preferred source; fall back to the project's own branding, then a
        // title-cased version of the last route-prefix segment.
        private static string ResolveProjectBreadcrumbName(List<LinkConfig> links, string ownName, string routePrefix)
        {
            var navbarLabel = FindNavbarLabel(links, routePrefix);
            if (!string.IsNullOrWhiteSpace(navbarLabel)) return navbarLabel.Trim();

            if (!string.IsNullOrWhiteSpace(ownName)) return ownName.Trim();

            var segment = (routePrefix ?? string.Empty)
                .Replace('\\', '/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            return string.IsNullOrEmpty(segment) ? null : ToFriendlyName(segment);
        }

        // A sub-project's own identity, from its own config: an explicit
        // `breadcrumb.label`, else `branding.label`, else `branding.title`
        // (ignoring the default "Neko"). Read before the parent merge so an
        // inherited parent title/label doesn't masquerade as this project's name.
        private static string ResolveOwnProjectName(NekoConfig config)
        {
            var explicitLabel = config?.Breadcrumb?.Label;
            if (!string.IsNullOrWhiteSpace(explicitLabel)) return explicitLabel.Trim();

            var brandingLabel = config?.Branding?.Label;
            if (!string.IsNullOrWhiteSpace(brandingLabel)) return brandingLabel.Trim();

            var brandingTitle = config?.Branding?.Title;
            if (!string.IsNullOrWhiteSpace(brandingTitle)
                && !string.Equals(brandingTitle, "Neko", StringComparison.Ordinal))
            {
                return brandingTitle.Trim();
            }

            return null;
        }

        // Finds the navbar link (at any nesting depth) whose target is this
        // sub-project's root, and returns its text. Matching canonicalises both
        // sides: drop scheme-less leading slash, a `.md`/`.html` suffix, a
        // trailing `/index`, and surrounding slashes.
        private static string FindNavbarLabel(List<LinkConfig> links, string routePrefix)
        {
            var target = CanonicalizeLinkPath(routePrefix);
            if (string.IsNullOrEmpty(target)) return null;

            string Walk(List<LinkConfig> items)
            {
                if (items == null) return null;
                foreach (var link in items)
                {
                    if (!string.IsNullOrWhiteSpace(link.Text)
                        && !string.IsNullOrEmpty(link.Link)
                        && !link.Link.Contains("://")
                        && string.Equals(CanonicalizeLinkPath(link.Link), target, StringComparison.OrdinalIgnoreCase))
                    {
                        return link.Text;
                    }

                    var nested = Walk(link.Items);
                    if (nested != null) return nested;
                }
                return null;
            }

            return Walk(links);
        }

        private static string CanonicalizeLinkPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var p = path.Replace('\\', '/').Trim();
            if (p.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) p = p.Substring(0, p.Length - 3);
            else if (p.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) p = p.Substring(0, p.Length - 5);
            p = p.Trim('/');
            if (p.EndsWith("/index", StringComparison.OrdinalIgnoreCase)) p = p.Substring(0, p.Length - "/index".Length);
            else if (p.Equals("index", StringComparison.OrdinalIgnoreCase)) p = string.Empty;
            return p;
        }

        // "landlock-sharp" → "Landlock-Sharp": capitalise the first letter of each
        // word while leaving separators (-, _, spaces) intact.
        private static string ToFriendlyName(string slug)
        {
            var chars = slug.ToCharArray();
            var atWordStart = true;
            for (var i = 0; i < chars.Length; i++)
            {
                if (char.IsLetterOrDigit(chars[i]))
                {
                    if (atWordStart) chars[i] = char.ToUpperInvariant(chars[i]);
                    atWordStart = false;
                }
                else
                {
                    atWordStart = true;
                }
            }
            return new string(chars);
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
                if (resourceName.EndsWith(".js") || resourceName.EndsWith(".css") || resourceName.EndsWith(".woff2") || resourceName.EndsWith(".json"))
                {
                    // The Tailwind base/components layers are inputs to the
                    // generator, not standalone assets — never copy them out.
                    if (resourceName.StartsWith("Neko.Resources.tailwind.")) continue;

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

            // Copy a root-level favicon if present, so the default auto-detected
            // path resolves in the generated site.
            foreach (var candidate in new[] { "favicon.ico", "favicon.png" })
            {
                var src = Path.Combine(_inputDirectory, candidate);
                if (File.Exists(src))
                {
                    try
                    {
                        File.Copy(src, Path.Combine(outputDir, candidate), true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Error copying {candidate}: {ex.Message}");
                    }
                }
            }
        }

        // Scans the emitted site for used Tailwind classes and writes a static
        // assets/tailwind.css with the pure-C# generator. Per-site (multi-repo
        // sub-projects each call this for their own OutputDirectory), so the
        // used-class set and palette are correct for each.
        private async Task GenerateTailwindAsync(string outputDir)
        {
            try
            {
                var contents = ClassExtractor.ReadContentFiles(outputDir).ToList();
                // Non-minified so the output matches the Tailwind CLI's
                // (non-minified) output byte-for-byte. Minification (matching
                // cssnano) can be layered on later without affecting correctness.
                var css = TailwindGenerator.Generate(contents, _config, minify: false);

                var assetsDir = Path.Combine(outputDir, "assets");
                Directory.CreateDirectory(assetsDir);
                var cssOut = Path.Combine(assetsDir, "tailwind.css");
                await File.WriteAllTextAsync(cssOut, css);
                Console.WriteLine($"Generated static Tailwind stylesheet: {Path.GetRelativePath(outputDir, cssOut)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to generate the Tailwind stylesheet: {ex.Message}");
            }
        }
    }
}
