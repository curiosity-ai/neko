using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Neko.Builder
{
    public class FileScanner
    {
        private static readonly HashSet<string> ExcludedRootFiles = new(StringComparer.OrdinalIgnoreCase)
        {
            "agents.md", "agent.md", "wip.md", "todo.md", "to-do.md",
            "readme.md", "ai.md", "instructions.md", "prompt.md",
            "rules.md", "context.md", ".cursorrules", ".windsurfrules"
        };

        private readonly string _inputDirectory;
        private readonly string _outputDirectory;
        private readonly HashSet<string>? _excludedSubDirectories;
        private readonly Matcher _ignoreMatcher;

        public FileScanner(string inputDirectory, string outputDirectory, HashSet<string>? excludedSubDirectories = null, string[]? ignorePatterns = null)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
            _outputDirectory = Path.GetFullPath(outputDirectory);
            _excludedSubDirectories = excludedSubDirectories;

            _ignoreMatcher = new Matcher();
            if (ignorePatterns != null)
            {
                foreach (var pattern in ignorePatterns)
                {
                    _ignoreMatcher.AddInclude(pattern);
                }
            }

            // Ensure output directory ends with separator for safer check
            if (!_outputDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                _outputDirectory += Path.DirectorySeparatorChar;
            }
        }

        public IEnumerable<string> Scan()
        {
            if (!Directory.Exists(_inputDirectory))
            {
                yield break;
            }

            // Enumerate all .md files
            var files = Directory.EnumerateFiles(_inputDirectory, "*.md", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Exclude output directory if it's inside input directory
                if (file.StartsWith(_outputDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (_excludedSubDirectories != null)
                {
                    var isExcluded = false;
                    foreach(var excludedDir in _excludedSubDirectories)
                    {
                        if (file.StartsWith(excludedDir, StringComparison.OrdinalIgnoreCase))
                        {
                            isExcluded = true;
                            break;
                        }
                    }
                    if (isExcluded) continue;
                }

                // Check against global ignore patterns
                var relativePath = Path.GetRelativePath(_inputDirectory, file);

                // Normalise separators for globbing
                var normalizedRelativePath = relativePath.Replace('\\', '/');

                if (_ignoreMatcher.Match(normalizedRelativePath).HasMatches)
                {
                    continue;
                }

                // Exclude .git and other hidden folders/files
                if (IsHiddenOrExcluded(file))
                {
                    continue;
                }

                yield return file;
            }
        }

        private bool IsHiddenOrExcluded(string filePath)
        {
            var relativePath = Path.GetRelativePath(_inputDirectory, filePath);
            var parts = relativePath.Split(Path.DirectorySeparatorChar);

            foreach (var part in parts)
            {
                // Check if any part of the path starts with '.' (hidden file or folder)
                // But ignore "." itself which might be returned by GetRelativePath if input is same
                if (part.StartsWith(".") && part != ".")
                {
                    return true;
                }
            }

            // Exclude common AI agent files and placeholders ONLY in the root folder
            if (parts.Length == 1)
            {
                if (ExcludedRootFiles.Contains(parts[0]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
