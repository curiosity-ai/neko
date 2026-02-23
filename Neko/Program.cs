using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Neko.Builder;
using Neko.Server;
using System.IO;
using System;
using Neko.Configuration;

namespace Neko
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("Neko CLI - Static Site Generator");

            // Build Command
            var buildCommand = new Command("build", "Build the documentation");

            var inputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var outputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            buildCommand.AddOption(inputOption);
            buildCommand.AddOption(outputOption);

            buildCommand.SetHandler(async (string input, string? output) =>
            {
                var builder = new SiteBuilder(input, output);
                await builder.BuildAsync();
            }, inputOption, outputOption);

            // Watch Command
            var watchCommand = new Command("watch", "Watch for changes and rebuild");
            var watchInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var watchOutputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            watchCommand.AddOption(watchInputOption);
            watchCommand.AddOption(watchOutputOption);

            watchCommand.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(watchInputOption) ?? ".";
                var output = context.ParseResult.GetValueForOption(watchOutputOption);
                var token = context.GetCancellationToken();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Extra safety: Hook Console.CancelKeyPress manually

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true; // Prevent immediate termination
                    Console.WriteLine("Ctrl+C detected, shutting down...");
                    cts.Cancel();
                };

                Console.WriteLine($"Watching {input}...");

                // Initial build
                var builder = new SiteBuilder(input, output, true);
                await builder.BuildAsync();

                // Get the output directory from the builder
                var outputDir = builder.OutputDirectory;

                // Start Server in background
                var server = new DevServer(outputDir, input);
                var serverTask = server.StartAsync(cts.Token);

                // Watch file changes
                using var watcher = new FileSystemWatcher(input);
                watcher.IncludeSubdirectories = true;
                watcher.Filters.Add("*.md");
                watcher.Filters.Add("neko.yml");

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
                        // Re-instantiate builder to reload config?
                        // Or just call BuildAsync again.
                        // Ideally config reload should be handled inside BuildAsync if needed, or we recreate builder.
                        // Let's recreate builder to be safe if config changed.
                        builder = new SiteBuilder(input, output, true);
                        await builder.BuildAsync();

                        await server.NotifyChange();
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

                try
                {
                    await serverTask; // Wait for server (which runs until cancelled)
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Shutdown complete.");
                }
            });

            rootCommand.AddCommand(buildCommand);
            rootCommand.AddCommand(watchCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
