using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Neko.Builder;
using Neko.Server;
using System.IO;
using System;
using Neko.Configuration;
using System.Text;
using Microsoft.Build.Locator;
using System.Runtime.InteropServices;

namespace Neko
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            ForceInvariantCultureAndUTF8Output();
            InitializeMSBuild();

            var rootCommand = new RootCommand("Neko CLI - Static Site Generator");

            // Build Command
            var buildCommand = new Command("build", "Build the documentation");

            var inputOption  = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var outputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            buildCommand.AddOption(inputOption);
            buildCommand.AddOption(outputOption);

            buildCommand.SetHandler(async (string input, string? output) =>
            {
                await BuildRunner.RunAsync(input, output);
            }, inputOption, outputOption);

            // Snap Command
            var snapCommand = new Command("snap", "Capture screenshots referenced by [!snapframe ...] directives via Playwright");
            var snapInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var snapAllOption = new Option<bool>(new[] { "--all" }, () => false, "Re-capture all screenshots (overwrite existing images)");
            snapCommand.AddOption(snapInputOption);
            snapCommand.AddOption(snapAllOption);

            snapCommand.SetHandler((string input, bool all) =>
            {
                var inputFullPath = Path.GetFullPath(input);
                var snap = new Neko.Builder.SnapCommand(inputFullPath, all);
                snap.Run();
            }, snapInputOption, snapAllOption);

            // Watch Command
            var watchCommand = new Command("watch", "Watch for changes and rebuild");
            // macOS' AirPlay Receiver (ControlCenter) binds port 5000 and answers
            // with HTTP 403, so default to 5050 there to avoid the collision.
            var defaultPort  = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 5050 : 5000;
            var portOption   = new Option<int?>(new[] { "--port", "-p" }, $"Port to use (default: {defaultPort})");
            var watchInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var watchOutputOption = new Option<string?>(new[] { "--output", "-o" }, "Output directory path");

            watchCommand.AddOption(watchInputOption);
            watchCommand.AddOption(portOption);
            watchCommand.AddOption(watchOutputOption);

            watchCommand.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(watchInputOption) ?? ".";
                var output = context.ParseResult.GetValueForOption(watchOutputOption);
                var port = context.ParseResult.GetValueForOption(portOption) ?? defaultPort;
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
                var configFiles = BuildRunner.FindProjectConfigs(inputFullPath);

                // Keep all on-disk Tesserae build artifacts in the project's
                // .neko-cache folder rather than the OS temp directory.
                Neko.Builder.TesseraeCompiler.SetCacheRoot(Path.Combine(inputFullPath, ".neko-cache"));

                var isMultiRepo = configFiles.Length > 1 || (configFiles.Length == 1 && Path.GetDirectoryName(configFiles[0]) != inputFullPath);

                Console.WriteLine($"Watching {input}{(isMultiRepo ? " (Multi-Repo Mode)" : "")}...");

                var sites    = new List<SiteInfo>();
                var builders = new Dictionary<string, SiteBuilder>();

                // Resolve the (sub-)projects once and create a persistent SiteBuilder
                // for each. Builders are reused across rebuilds so each can cache its
                // last build state and regenerate a single changed page incrementally
                // (see SiteBuilder.TryRebuildSinglePageAsync) instead of rebuilding the
                // whole project on every change.
                var projects = new List<(string Dir, string? Output, string? RoutePrefix, bool IsRoot)>();
                if (isMultiRepo)
                {
                    foreach (var configFile in configFiles)
                    {
                        var subDir = Path.GetDirectoryName(configFile);
                        if (subDir == null) continue;

                        var subDirRelative = Path.GetRelativePath(inputFullPath, subDir).Replace("\\", "/");
                        var isRoot = subDir == inputFullPath;
                        var routePrefix = isRoot ? null : "/" + subDirRelative;
                        var siteOutput = output != null
                            ? (isRoot ? output : Path.Combine(output, subDirRelative))
                            : Path.Combine(subDir, ".neko");
                        projects.Add((Path.GetFullPath(subDir), siteOutput, routePrefix, isRoot));
                    }
                }
                else
                {
                    projects.Add((inputFullPath, output, null, true));
                }

                foreach (var p in projects)
                {
                    builders[p.Dir] = new SiteBuilder(p.Dir, p.Output, true, p.RoutePrefix);
                }

                string rootOutput = null;

                // Re-merge each sub-project's search.json into the root aggregated index.
                async Task ReaggregateSearchAsync()
                {
                    if (!isMultiRepo || rootOutput == null) return;
                    var subOutputs = sites.Select(s => s.OutputPath).Where(o => !string.IsNullOrEmpty(o)).ToList();
                    await Neko.Builder.SearchIndexGenerator.AggregateAsync(rootOutput, subOutputs);
                }

                // Full build of a single project, registering (or refreshing) its SiteInfo.
                async Task BuildProjectAsync((string Dir, string? Output, string? RoutePrefix, bool IsRoot) p)
                {
                    var builder = builders[p.Dir];
                    await builder.BuildAsync();

                    var existing = sites.FirstOrDefault(s =>
                        string.Equals(Path.GetFullPath(s.InputPath), p.Dir, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.OutputPath = builder.OutputDirectory;
                    }
                    else
                    {
                        sites.Add(new Neko.Server.SiteInfo
                        {
                            RoutePrefix = p.IsRoot ? "" : p.RoutePrefix,
                            InputPath = p.Dir,
                            OutputPath = builder.OutputDirectory
                        });
                    }

                    if (p.IsRoot) rootOutput = builder.OutputDirectory;
                }

                // Full build of every project (startup and structural-change fallback).
                async Task BuildAsync()
                {
                    if (isMultiRepo && !Directory.Exists(inputFullPath))
                    {
                        Console.WriteLine("Warning: Multi-repo mode detected but the input path does not exist.");
                        return;
                    }

                    foreach (var p in projects)
                    {
                        await BuildProjectAsync(p);
                    }

                    await ReaggregateSearchAsync();

                    if (sites.Count == 0)
                    {
                        Console.WriteLine("Warning: no documentation projects (neko.yml) found.");
                    }
                }

                await BuildAsync();

                // Start Server in background
                var server = new DevServer(sites, port);
                var serverTask = server.StartAsync(cts.Token);

                // Watch file changes
                var watchers = new System.Collections.Generic.List<FileSystemWatcher>();
                DateTime lastBuild = DateTime.MinValue;
                var rebuildLock = new object();

                foreach (var site in sites.ToList())
                {
                    var watcher = new FileSystemWatcher(site.InputPath);
                    watcher.IncludeSubdirectories = true;
                    watcher.Filters.Add("*.md");
                    watcher.Filters.Add("*.yml");

                    FileSystemEventHandler onChanged = async (sender, e) =>
                    {
                        var changed = Path.GetFullPath(e.FullPath);

                        // Never rebuild in response to our own build artifacts /
                        // the Tesserae cache being written.
                        foreach (var s in sites)
                        {
                            if (!string.IsNullOrEmpty(s.OutputPath)
                                && changed.StartsWith(Path.GetFullPath(s.OutputPath), StringComparison.OrdinalIgnoreCase))
                                return;
                        }
                        if (changed.Contains($"{Path.DirectorySeparatorChar}.neko-cache{Path.DirectorySeparatorChar}")) return;

                        lock (rebuildLock)
                        {
                            if ((DateTime.Now - lastBuild).TotalMilliseconds < 500) return;
                            lastBuild = DateTime.Now;
                        }

                        Console.WriteLine($"Change detected: {e.Name}. Rebuilding...");

                        try
                        {
                            // Attribute the change to the most specific project that owns it.
                            (string Dir, string? Output, string? RoutePrefix, bool IsRoot)? owner = null;
                            foreach (var p in projects)
                            {
                                var root = p.Dir.EndsWith(Path.DirectorySeparatorChar) ? p.Dir : p.Dir + Path.DirectorySeparatorChar;
                                if (changed.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                                    && (owner == null || p.Dir.Length > owner.Value.Dir.Length))
                                {
                                    owner = p;
                                }
                            }

                            if (owner != null)
                            {
                                // Fast path: regenerate just the changed page when possible;
                                // otherwise rebuild only the owning sub-project.
                                var builder = builders[owner.Value.Dir];
                                if (!await builder.TryRebuildSinglePageAsync(changed))
                                {
                                    await BuildProjectAsync(owner.Value);
                                }
                                await ReaggregateSearchAsync();
                            }
                            else
                            {
                                // Couldn't attribute the change to a project — full rebuild.
                                await BuildAsync();
                            }

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

            // Gen-Images Command — find every [!img-gen ...] directive, ask the
            // chosen LLM for a slug + alt text, generate the image via the
            // chosen image model, save the PNG into the page's assets/img-gen/
            // folder, and rewrite the directive to a real Markdown image with
            // the original directive preserved as an HTML comment.
            var genImagesCommand = new Command("gen-images", "Generate images for every [!img-gen ...] directive using OpenAI");
            var genInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var genApiKeyOption = new Option<string?>(new[] { "--api-key" }, "OpenAI API key (defaults to the OPENAI_API_KEY environment variable)");
            var genImageModelOption = new Option<string>(new[] { "--image-model" }, () => "gpt-image-1", "OpenAI image model to use");
            var genLlmModelOption = new Option<string>(new[] { "--llm-model" }, () => "gpt-4o-mini", "OpenAI chat model used to generate filename and alt text");
            genImagesCommand.AddOption(genInputOption);
            genImagesCommand.AddOption(genApiKeyOption);
            genImagesCommand.AddOption(genImageModelOption);
            genImagesCommand.AddOption(genLlmModelOption);

            genImagesCommand.SetHandler(async (string input, string? apiKey, string imageModel, string llmModel) =>
            {
                var inputFullPath = Path.GetFullPath(input);
                var key = apiKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                }
                var configPath = Path.Combine(inputFullPath, "neko.yml");
                var nekoConfig = File.Exists(configPath)
                    ? Neko.Configuration.ConfigParser.Parse(configPath)
                    : new Neko.Configuration.NekoConfig();
                var cmd = new Neko.Builder.ImageGenCommand(inputFullPath, key ?? "", imageModel, llmModel, nekoConfig.ImageGen);
                Environment.ExitCode = await cmd.RunAsync();
            }, genInputOption, genApiKeyOption, genImageModelOption, genLlmModelOption);

            // Gen-Dark-Images Command — walk every `assets/img-gen/*.png`
            // reference in the input tree and, for any image that doesn't yet
            // have a `src-dark="…"` companion attribute, generate a paired
            // dark-mode variant via the OpenAI image-edit endpoint and rewrite
            // the Markdown to point at both files.
            var darkImagesCommand = new Command("gen-dark-images", "Generate missing dark-mode variants for images previously created by [!img-gen]");
            var darkInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var darkApiKeyOption = new Option<string?>(new[] { "--api-key" }, "OpenAI API key (defaults to the OPENAI_API_KEY environment variable)");
            var darkImageModelOption = new Option<string>(new[] { "--image-model" }, () => "gpt-image-1", "OpenAI image model to use");
            darkImagesCommand.AddOption(darkInputOption);
            darkImagesCommand.AddOption(darkApiKeyOption);
            darkImagesCommand.AddOption(darkImageModelOption);

            darkImagesCommand.SetHandler(async (string input, string? apiKey, string imageModel) =>
            {
                var inputFullPath = Path.GetFullPath(input);
                var key = apiKey;
                if (string.IsNullOrWhiteSpace(key))
                {
                    key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                }
                var configPath = Path.Combine(inputFullPath, "neko.yml");
                var nekoConfig = File.Exists(configPath)
                    ? Neko.Configuration.ConfigParser.Parse(configPath)
                    : new Neko.Configuration.NekoConfig();
                var cmd = new Neko.Builder.ImageGenCommand(inputFullPath, key ?? "", imageModel, llmModel: "gpt-4o-mini", nekoConfig.ImageGen);
                Environment.ExitCode = await cmd.BackfillDarkImagesAsync();
            }, darkInputOption, darkApiKeyOption, darkImageModelOption);

            // Check-Links Command — build the site into a throwaway folder and
            // verify every internal page/asset link (and #anchor) resolves, plus
            // optionally probe external http(s) links. Exits non-zero when any
            // link is broken so it can gate CI.
            var checkLinksCommand = new Command("check-links", "Build the project and report any broken links in the generated site");
            var checkLinksInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            var checkLinksExternalOption = new Option<bool>(new[] { "--external" }, () => false, "Also verify external http(s) links over the network");
            var checkLinksNoAnchorsOption = new Option<bool>(new[] { "--no-anchors" }, () => false, "Skip validation of #fragment anchors");
            var checkLinksRedirectsOption = new Option<bool>(new[] { "--redirects" }, () => false, "Report external links that resolve via an HTTP redirect (implies --external)");
            checkLinksCommand.AddOption(checkLinksInputOption);
            checkLinksCommand.AddOption(checkLinksExternalOption);
            checkLinksCommand.AddOption(checkLinksNoAnchorsOption);
            checkLinksCommand.AddOption(checkLinksRedirectsOption);

            // Use the InvocationContext so the broken-link exit code propagates
            // as the process exit code (InvokeAsync's return value, which Main
            // returns, otherwise wins over Environment.ExitCode).
            checkLinksCommand.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(checkLinksInputOption) ?? ".";
                var external = context.ParseResult.GetValueForOption(checkLinksExternalOption);
                var noAnchors = context.ParseResult.GetValueForOption(checkLinksNoAnchorsOption);
                var redirects = context.ParseResult.GetValueForOption(checkLinksRedirectsOption);

                var cmd = new Neko.Builder.CheckLinksCommand(input, external, checkAnchors: !noAnchors, checkRedirects: redirects);
                context.ExitCode = await cmd.RunAsync();
            });

            // New Command — scaffold a new documentation project from the
            // embedded .template/ starter zip.
            var newCommand = new Command("new", "Initialize a new Neko documentation project from the built-in template");
            var newPathOption = new Option<string?>(new[] { "--path", "-p" }, "Target directory for the new project (default: current directory)");
            var newForceOption = new Option<bool>(new[] { "--force", "-f" }, () => false, "Overwrite existing files at the target path");
            newCommand.AddOption(newPathOption);
            newCommand.AddOption(newForceOption);

            newCommand.SetHandler((string? newPath, bool newForce) =>
            {
                var exitCode = Neko.Builder.NewCommand.Run(newPath, newForce);
                Environment.ExitCode = exitCode;
            }, newPathOption, newForceOption);

            // Update-Skills Command — refresh the Neko-managed skills under
            // an existing project's .claude/skills/ folder, leaving custom
            // (non-Neko) skills untouched.
            var updateSkillsCommand = new Command("update-skills", "Update Neko-managed skills under a target project's .claude/skills/ folder");
            var updateSkillsPathOption = new Option<string?>(new[] { "--path", "-p" }, "Target project directory (default: current directory)");
            var updateSkillsDryRunOption = new Option<bool>(new[] { "--dry-run" }, () => false, "List the changes that would be made without writing files");
            updateSkillsCommand.AddOption(updateSkillsPathOption);
            updateSkillsCommand.AddOption(updateSkillsDryRunOption);

            updateSkillsCommand.SetHandler((string? updatePath, bool dryRun) =>
            {
                var exitCode = Neko.Builder.UpdateSkillsCommand.Run(updatePath, dryRun);
                Environment.ExitCode = exitCode;
            }, updateSkillsPathOption, updateSkillsDryRunOption);

            // Gen-Tesserae-Heights Command — measure each `tesserae` live sample's
            // rendered height with a headless browser and bake a `height=NNN` token
            // into its fence, so normal builds size the preview iframe up front
            // without ever launching a browser.
            var tesseraeHeightsCommand = new Command("gen-tesserae-heights", "Measure tesserae live samples and bake iframe heights into their fences");
            var thInputOption = new Option<string>(new[] { "--input", "-i" }, () => ".", "Input directory path");
            tesseraeHeightsCommand.AddOption(thInputOption);
            tesseraeHeightsCommand.SetHandler(async (string input) =>
            {
                var cmd = new Neko.Builder.TesseraeHeightsCommand(input);
                Environment.ExitCode = await cmd.RunAsync();
            }, thInputOption);

            rootCommand.AddCommand(buildCommand);
            rootCommand.AddCommand(watchCommand);
            rootCommand.AddCommand(checkLinksCommand);
            rootCommand.AddCommand(tesseraeHeightsCommand);
            rootCommand.AddCommand(snapCommand);
            rootCommand.AddCommand(genImagesCommand);
            rootCommand.AddCommand(darkImagesCommand);
            rootCommand.AddCommand(newCommand);
            rootCommand.AddCommand(updateSkillsCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void InitializeMSBuild()
        {
            // Initialize MSBuild
            try
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                if (instances.Length > 0)
                {
                    var instance = instances.OrderByDescending(x => x.Version).First();
                    MSBuildLocator.RegisterInstance(instance);
                    Console.WriteLine($"Registered MSBuild instance: {instance.Name} {instance.Version} at {instance.MSBuildPath}");
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                    Console.WriteLine("Registered default MSBuild instance.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MSBuildLocator initialization warning: {ex.Message}");
            }
        }

        public static void ForceInvariantCultureAndUTF8Output()
        {
            var localTimeZone = TimeZoneInfo.Local;
            var localCulture = Thread.CurrentThread.CurrentUICulture;
            
            bool consoleAvailable;
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                consoleAvailable = true;
            }
            catch
            {
                //This might throw if not running on a console, ignore as we don't care in that case
                consoleAvailable = false;
            }

            if (consoleAvailable)
            {
                try
                {
                    Console.InputEncoding = Encoding.UTF8;
                }
                catch
                {
                    //This might throw if not running on a console that reads input, ignore as we don't care in that case
                }
            }

            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }
    }
}
