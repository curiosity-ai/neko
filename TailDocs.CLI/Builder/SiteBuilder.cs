using System;
using System.IO;
using System.Threading.Tasks;
using TailDocs.CLI.Configuration;

namespace TailDocs.CLI.Builder
{
    public class SiteBuilder
    {
        private readonly string _inputDirectory;
        private TailDocsConfig _config;

        public SiteBuilder(string inputDirectory)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
        }

        public async Task BuildAsync()
        {
            Console.WriteLine($"Building documentation from {_inputDirectory}...");

            // 1. Read Configuration
            var configPath = Path.Combine(_inputDirectory, "taildocs.yml");
            _config = ConfigParser.Parse(configPath);

            // Resolve output directory
            var outputDir = Path.IsPathRooted(_config.Output)
                ? _config.Output
                : Path.Combine(_inputDirectory, _config.Output);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 2. Scan Files
            var scanner = new FileScanner(_inputDirectory, outputDir);
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
                var outputPath = Path.Combine(outputDir, htmlFileName);

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

            await searchIndexer.WriteIndexAsync(outputDir);

            // Copy Assets
            await CopyAssetsAsync(outputDir);

            Console.WriteLine($"Build complete. Output in {outputDir}");
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
                    // But folder structure might be flattened or preserved with dots?
                    // "TailDocs.CLI.Resources.minisearch.min.js" -> "minisearch.min.js"

                    // Simple logic: take everything after "TailDocs.CLI.Resources."
                    var fileName = resourceName.Replace("TailDocs.CLI.Resources.", "");
                    var outputPath = Path.Combine(assetsDir, fileName);

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
