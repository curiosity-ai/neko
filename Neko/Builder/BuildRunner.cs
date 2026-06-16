using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Neko.Builder
{
    /// <summary>
    /// Shared one-shot build orchestration used by the <c>build</c> and
    /// <c>check-links</c> commands. Handles both single-repo and multi-repo
    /// layouts (the latter aggregating every sub-project's search index into a
    /// single root index) and returns the root output directory.
    /// </summary>
    public static class BuildRunner
    {
        /// <summary>
        /// Builds the project rooted at <paramref name="input"/> into
        /// <paramref name="output"/> (or each project's <c>.neko</c> folder when
        /// <paramref name="output"/> is null). Returns the root output directory.
        /// </summary>
        public static async Task<string> RunAsync(string input, string? output)
        {
            var inputFullPath = Path.GetFullPath(input);
            var configFiles = FindProjectConfigs(inputFullPath);

            var isMultiRepo = configFiles.Length > 1 || (configFiles.Length == 1 && Path.GetDirectoryName(configFiles[0]) != inputFullPath);

            if (isMultiRepo)
            {
                Console.WriteLine("Building in Multi-Repo Mode...");
                var subProjectOutputs = new List<string>();
                string? rootOutput = null;

                // Build the root project first so that, when a shared output is
                // used, clearing the root output directory doesn't wipe a
                // sub-project that was already written into it.
                foreach (var configFile in configFiles.OrderBy(c => c.Length))
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

                    if (isRoot) rootOutput = builder.OutputDirectory;
                    subProjectOutputs.Add(builder.OutputDirectory);
                }

                if (rootOutput != null)
                {
                    Console.WriteLine("Aggregating search indexes across sub-projects...");
                    await SearchIndexGenerator.AggregateAsync(rootOutput, subProjectOutputs);
                }

                return rootOutput
                    ?? (output != null ? Path.GetFullPath(output) : inputFullPath);
            }
            else
            {
                var builder = new SiteBuilder(input, output);
                await builder.BuildAsync();
                return builder.OutputDirectory;
            }
        }

        // Discovers all neko.yml project configs under <root>, skipping any that
        // live inside a hidden directory (a path segment starting with '.', e.g.
        // .git, .idea, the .neko build output, or .claude/worktrees). Without this,
        // multi-repo mode descends into git worktrees and rebuilds every project's
        // duplicate copy into the worktree.
        public static string[] FindProjectConfigs(string root)
        {
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            return Directory
                .GetFiles(root, "neko.yml", SearchOption.AllDirectories)
                .Where(cfg => !HasHiddenSegment(Path.GetRelativePath(root, Path.GetDirectoryName(cfg)!)))
                .ToArray();
        }

        private static bool HasHiddenSegment(string relativeDir)
        {
            foreach (var part in relativeDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (part.Length > 0 && part != "." && part.StartsWith('.'))
                    return true;
            }
            return false;
        }
    }
}
