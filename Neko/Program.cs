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

            var inputOption  = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var outputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            buildCommand.AddOption(inputOption);
            buildCommand.AddOption(outputOption);

            buildCommand.SetHandler(async (string input, string? output) =>
            {
                var inputFullPath = Path.GetFullPath(input);
                var configFiles = Directory.Exists(inputFullPath)
                    ? Directory.GetFiles(inputFullPath, "neko.yml", SearchOption.AllDirectories)
                    : Array.Empty<string>();

                var isMultiRepo = configFiles.Length > 1 || (configFiles.Length == 1 && Path.GetDirectoryName(configFiles[0]) != inputFullPath);

                if (isMultiRepo)
                {
                    Console.WriteLine($"Building in Multi-Repo Mode...");
                    foreach (var configFile in configFiles)
                    {
                        var subDir = Path.GetDirectoryName(configFile);
                        if (subDir == null) continue;

                        var subDirRelative = Path.GetRelativePath(inputFullPath, subDir).Replace("\\", "/");
                        var isRoot = subDir == inputFullPath;
                        var routePrefix = isRoot ? "" : "/" + subDirRelative;
                        var siteOutput = output != null
                            ? (isRoot ? output : Path.Combine(output, subDirRelative))
                            : Path.Combine(subDir, ".neko");

                        var builder = new SiteBuilder(subDir, siteOutput, false, isRoot ? null : routePrefix);
                        await builder.BuildAsync();
                    }
                }
                else
                {
                    var builder = new SiteBuilder(input, output);
                    await builder.BuildAsync();
                }
            }, inputOption, outputOption);

            // Watch Command
            var watchCommand = new Command("watch", "Watch for changes and rebuild");
            var portOption   = new Option<int?>(new[] { "--port", "-p" }, "Port to use (default: 5000)");
            var watchInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var watchOutputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            watchCommand.AddOption(watchInputOption);
            watchCommand.AddOption(portOption);
            watchCommand.AddOption(watchOutputOption);

            watchCommand.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(watchInputOption) ?? ".";
                var output = context.ParseResult.GetValueForOption(watchOutputOption);
                var port = context.ParseResult.GetValueForOption(portOption) ?? 5000;
                var token = context.GetCancellationToken();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);

                // Extra safety: Hook Console.CancelKeyPress manually

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true; // Prevent immediate termination
                    Console.WriteLine("Ctrl+C detected, shutting down...");
                    cts.Cancel();
                };

                var inputFullPath = Path.GetFullPath(input);
                var configFiles = Directory.Exists(inputFullPath)
                    ? Directory.GetFiles(inputFullPath, "neko.yml", SearchOption.AllDirectories)
                    : Array.Empty<string>();

                var isMultiRepo = configFiles.Length > 1 || (configFiles.Length == 1 && Path.GetDirectoryName(configFiles[0]) != inputFullPath);

                Console.WriteLine($"Watching {input}{(isMultiRepo ? " (Multi-Repo Mode)" : "")}...");

                var sites = new System.Collections.Generic.List<Neko.Server.SiteInfo>();
                var builders = new System.Collections.Generic.Dictionary<string, SiteBuilder>();

                if (isMultiRepo)
                {
                    if (Directory.Exists(inputFullPath))
                    {
                        foreach (var configFile in configFiles)
                        {
                            var subDir = Path.GetDirectoryName(configFile);
                            if (subDir == null) continue;

                            var subDirRelative = Path.GetRelativePath(inputFullPath, subDir).Replace("\\", "/");
                            var isRoot = subDir == inputFullPath;
                            var routePrefix = isRoot ? "" : "/" + subDirRelative;
                            var siteOutput = output != null
                                ? (isRoot ? output : Path.Combine(output, subDirRelative))
                                : Path.Combine(subDir, ".neko");

                            var builder = new SiteBuilder(subDir, siteOutput, true, isRoot ? null : routePrefix);
                            await builder.BuildAsync();

                            var siteInfo = new Neko.Server.SiteInfo
                            {
                                RoutePrefix = routePrefix,
                                InputPath = subDir,
                                OutputPath = builder.OutputDirectory
                            };

                            sites.Add(siteInfo);
                            builders[subDir] = builder;
                        }
                    }

                    if (sites.Count == 0)
                    {
                        Console.WriteLine("Warning: Multi-repo mode detected but no directories with neko.yml found.");
                    }
                }
                else
                {
                    var builder = new SiteBuilder(input, output, true, null);
                    await builder.BuildAsync();

                    sites.Add(new Neko.Server.SiteInfo
                    {
                        RoutePrefix = "",
                        InputPath = input,
                        OutputPath = builder.OutputDirectory
                    });
                    builders[Path.GetFullPath(input)] = builder;
                }

                // Start Server in background
                var server = new DevServer(sites, port);
                var serverTask = server.StartAsync(cts.Token);

                // Watch file changes
                var watchers = new System.Collections.Generic.List<FileSystemWatcher>();
                DateTime lastBuild = DateTime.MinValue;

                foreach (var site in sites)
                {
                    var watcher = new FileSystemWatcher(site.InputPath);
                    watcher.IncludeSubdirectories = true;
                    watcher.Filters.Add("*.md");
                    watcher.Filters.Add("neko.yml");

                    FileSystemEventHandler onChanged = async (sender, e) =>
                    {
                        var fullOutput = Path.GetFullPath(site.OutputPath);
                        if (e.FullPath.Contains(fullOutput)) return;

                        if ((DateTime.Now - lastBuild).TotalMilliseconds < 500) return;
                        lastBuild = DateTime.Now;

                        Console.WriteLine($"Change detected in {site.RoutePrefix}: {e.Name}. Rebuilding...");
                        try
                        {
                            var b = new SiteBuilder(site.InputPath, site.OutputPath, true);
                            await b.BuildAsync();
                            builders[site.InputPath] = b;

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

                    watchers.Add(watcher);
                }

                Console.WriteLine("Press Ctrl+C to exit.");

                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Shutdown complete.");
                }
                finally
                {
                    foreach (var w in watchers)
                    {
                        w.Dispose();
                    }
                }
            });

            rootCommand.AddCommand(buildCommand);
            rootCommand.AddCommand(watchCommand);

            return await rootCommand.InvokeAsync(args);
        }
    }
}
