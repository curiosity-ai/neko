using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Neko.Builder
{
    /// <summary>
    /// Updates the Neko-managed skills under a target project's
    /// <c>.claude/skills/</c> folder to match the versions embedded in this
    /// build of the Neko CLI. Skills that don't ship with Neko are left alone.
    /// </summary>
    public static class UpdateSkillsCommand
    {
        private const string SkillsPrefix = ".claude/skills/";

        public static int Run(string? path, bool dryRun)
        {
            var targetPath = string.IsNullOrWhiteSpace(path)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(path);

            if (!Directory.Exists(targetPath))
            {
                Console.Error.WriteLine($"Target directory does not exist: {targetPath}");
                return 1;
            }

            var claudeDir = Path.Combine(targetPath, ".claude");
            if (!Directory.Exists(claudeDir))
            {
                Console.Error.WriteLine($"No '.claude' folder found in: {targetPath}");
                Console.Error.WriteLine("Run `neko new` to scaffold a project, or create the folder first.");
                return 1;
            }

            var skillsDir = Path.Combine(claudeDir, "skills");

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(NewCommand.TemplateResourceName);
            if (stream == null)
            {
                Console.Error.WriteLine($"Embedded template resource '{NewCommand.TemplateResourceName}' was not found in the Neko assembly.");
                return 2;
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            // Collect the set of skill names shipped with this build of Neko.
            var nekoSkillNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/');
                if (!normalized.StartsWith(SkillsPrefix, StringComparison.Ordinal)) continue;

                var rest = normalized.Substring(SkillsPrefix.Length);
                var slash = rest.IndexOf('/');
                if (slash <= 0) continue;

                nekoSkillNames.Add(rest.Substring(0, slash));
            }

            if (nekoSkillNames.Count == 0)
            {
                Console.Error.WriteLine("The embedded template does not contain any skills under '.claude/skills/'.");
                return 3;
            }

            if (dryRun)
            {
                Console.WriteLine($"[dry-run] Would update {nekoSkillNames.Count} skill(s) under: {skillsDir}");
            }
            else
            {
                Directory.CreateDirectory(skillsDir);
            }

            var replaced = 0;
            var added = 0;
            var fileCount = 0;

            // Remove each Neko-managed skill folder before re-extracting it,
            // so files deleted upstream don't linger in the target.
            foreach (var skillName in nekoSkillNames)
            {
                var skillPath = Path.Combine(skillsDir, skillName);
                if (Directory.Exists(skillPath))
                {
                    replaced++;
                    if (!dryRun)
                    {
                        Directory.Delete(skillPath, recursive: true);
                    }
                }
                else
                {
                    added++;
                }
            }

            var targetFullPath = Path.GetFullPath(targetPath) + Path.DirectorySeparatorChar;

            foreach (var entry in archive.Entries)
            {
                var normalized = entry.FullName.Replace('\\', '/');
                if (!normalized.StartsWith(SkillsPrefix, StringComparison.Ordinal)) continue;

                // Skip the bare directory entry for skills/ itself.
                if (normalized.Equals(SkillsPrefix, StringComparison.Ordinal)) continue;

                var destination = Path.GetFullPath(Path.Combine(targetPath, entry.FullName));
                if (!destination.StartsWith(targetFullPath, StringComparison.Ordinal))
                {
                    throw new IOException($"Refusing to extract '{entry.FullName}' outside of '{targetPath}'.");
                }

                if (string.IsNullOrEmpty(entry.Name) && normalized.EndsWith("/", StringComparison.Ordinal))
                {
                    if (!dryRun) Directory.CreateDirectory(destination);
                    continue;
                }

                var destinationDir = Path.GetDirectoryName(destination);
                if (!dryRun && !string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                if (!dryRun)
                {
                    entry.ExtractToFile(destination, overwrite: true);
                }
                fileCount++;
            }

            var prefix = dryRun ? "[dry-run] " : "";
            Console.WriteLine($"{prefix}Updated skills in: {skillsDir}");
            Console.WriteLine($"  {nekoSkillNames.Count} skill(s) total — {added} added, {replaced} replaced.");
            Console.WriteLine($"  {fileCount} file(s) {(dryRun ? "would be written" : "written")}.");

            // Surface any non-Neko skills so the user knows they were preserved.
            if (Directory.Exists(skillsDir))
            {
                var otherSkills = Directory.EnumerateDirectories(skillsDir)
                    .Select(Path.GetFileName)
                    .Where(name => name != null && !nekoSkillNames.Contains(name!))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToList();

                if (otherSkills.Count > 0)
                {
                    Console.WriteLine($"  Preserved {otherSkills.Count} non-Neko skill(s): {string.Join(", ", otherSkills)}");
                }
            }

            return 0;
        }
    }
}
