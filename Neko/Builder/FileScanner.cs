using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neko.Builder
{
    public class FileScanner
    {
        private readonly string _inputDirectory;
        private readonly string _outputDirectory;

        public FileScanner(string inputDirectory, string outputDirectory)
        {
            _inputDirectory = Path.GetFullPath(inputDirectory);
            _outputDirectory = Path.GetFullPath(outputDirectory);

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
                var fileNameLower = parts[0].ToLowerInvariant();
                var excludedRootFiles = new[]
                {
                    "agents.md", "agent.md", "wip.md", "todo.md", "to-do.md",
                    "readme.md", "ai.md", "instructions.md", "prompt.md",
                    "rules.md", "context.md", ".cursorrules", ".windsurfrules"
                };

                if (excludedRootFiles.Contains(fileNameLower))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
