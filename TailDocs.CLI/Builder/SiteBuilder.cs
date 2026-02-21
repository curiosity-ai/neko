using System;
using System.IO;
using System.Threading.Tasks;
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
            foreach (var file in files)
            {
                Console.WriteLine($"Processing {Path.GetFileName(file)}...");
                var markdown = await File.ReadAllTextAsync(file);
                var doc = parser.Parse(markdown);
                var html = generator.Generate(doc);

                var relativePath = Path.GetRelativePath(_inputDirectory, file);
                var htmlFileName = Path.ChangeExtension(relativePath, ".html");
                var outputPath = Path.Combine(OutputDirectory, htmlFileName);

                var outputDirForFile = Path.GetDirectoryName(outputPath);
                if (outputDirForFile != null && !Directory.Exists(outputDirForFile))
                {
                    Directory.CreateDirectory(outputDirForFile);
                }

                await File.WriteAllTextAsync(outputPath, html);

                // Add to search index
                // Note: stripping markdown for search content is better
                searchIndexer.AddDocument(htmlFileName, doc.FrontMatter.Title ?? Path.GetFileNameWithoutExtension(file), markdown);
            }

            await searchIndexer.WriteIndexAsync(OutputDirectory);

            // Copy Assets
            await CopyAssetsAsync(OutputDirectory);

            Console.WriteLine($"Build complete. Output in {OutputDirectory}");
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
