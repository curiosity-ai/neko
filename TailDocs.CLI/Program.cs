using System.CommandLine;
using System.Threading.Tasks;
using TailDocs.CLI.Builder;
using TailDocs.CLI.Server;
using System.IO;
using System;
using TailDocs.CLI.Configuration;

namespace TailDocs.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("TailDocs CLI - Static Site Generator");

            // Build Command
            var buildCommand = new Command("build", "Build the documentation");

            var inputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            buildCommand.AddOption(inputOption);

            buildCommand.SetHandler(async (string input) =>
            {
                var builder = new SiteBuilder(input);
                await builder.BuildAsync();
            }, inputOption);

            // Watch Command
            var watchCommand = new Command("watch", "Watch for changes and rebuild");
            var watchInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            watchCommand.AddOption(watchInputOption);

            watchCommand.SetHandler(async (string input) =>
            {
                Console.WriteLine($"Watching {input}...");

                // Determine output directory to serve
                // We need to parse config to know where output is, or default to .taildocs
                var configPath = Path.Combine(input, "taildocs.yml");
                var config = ConfigParser.Parse(configPath);
                var outputDir = Path.IsPathRooted(config.Output)
                    ? config.Output
                    : Path.Combine(input, config.Output);

                // Initial build
                var builder = new SiteBuilder(input);
                await builder.BuildAsync();

                // Start Server in background
                var server = new DevServer(outputDir);
                var serverTask = server.StartAsync();

                // Watch file changes
                using var watcher = new FileSystemWatcher(input);
                watcher.IncludeSubdirectories = true;
                watcher.Filters.Add("*.md");
                watcher.Filters.Add("taildocs.yml");

                // Debounce logic
                DateTime lastBuild = DateTime.MinValue;

                FileSystemEventHandler onChanged = async (sender, e) =>
                {
                    // Ignore changes in output dir
                    if (e.FullPath.Contains(Path.GetFullPath(outputDir))) return;

                    if ((DateTime.Now - lastBuild).TotalMilliseconds < 500) return;
                    lastBuild = DateTime.Now;

                    Console.WriteLine($"Change detected: {e.Name}. Rebuilding...");
                    try
                    {
                        // Need to reload config in case it changed
                        // Builder re-parses config on BuildAsync so it's fine.
                        await builder.BuildAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Build failed: {ex.Message}");
                    }
                };

                watcher.Changed += onChanged;
                watcher.Created += onChanged;
                watcher.Deleted += onChanged;
                watcher.Renamed += new RenamedEventHandler(onChanged);

                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Press Ctrl+C to exit.");
                await serverTask; // Wait for server (which runs until cancelled)
            }, watchInputOption);

            rootCommand.AddCommand(buildCommand);
            rootCommand.AddCommand(watchCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
