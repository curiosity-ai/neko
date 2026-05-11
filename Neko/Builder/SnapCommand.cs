using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Neko.Extensions;

namespace Neko.Builder
{
    public class SnapCommand
    {
        private readonly string _inputDirectory;
        private readonly bool _all;

        // Matches [!snapframe URL [options]\n[commands]\n]
        private static readonly Regex SnapRegex = new(
            @"\[!snapframe\s+([^\s\]]+)([^\]]*)\]\s*(?:\r?\n)?\s*!\[[^\]]*\]\(([^\)]+)\)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public SnapCommand(string inputDirectory, bool all)
        {
            _inputDirectory = inputDirectory;
            _all = all;
        }

        public void Run()
        {
            if (!Directory.Exists(_inputDirectory))
            {
                Console.WriteLine($"[snap] Input directory not found: {_inputDirectory}");
                return;
            }

            var markdownFiles = Directory.GetFiles(_inputDirectory, "*.md", SearchOption.AllDirectories);

            Console.WriteLine($"[snap] Scanning {markdownFiles.Length} markdown file(s) in {_inputDirectory}...");
            if (_all) Console.WriteLine("[snap] --all flag set: existing screenshots will be re-captured.");

            var toCapture = new List<(string Url, string Options, List<string> Commands, string TargetPath)>();

            foreach (var file in markdownFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch { continue; }

                var fileDir = Path.GetDirectoryName(file) ?? _inputDirectory;

                foreach (Match m in SnapRegex.Matches(content))
                {
                    var url = m.Groups[1].Value.Trim();
                    var optionsAndCommands = m.Groups[2].Value;
                    var imageUrl = m.Groups[3].Value.Trim();

                    if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://")) continue;

                    var (options, commands) = ParseOptionsAndCommands(optionsAndCommands);

                    var relative = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    var targetPath = imageUrl.StartsWith("/")
                        ? Path.Combine(_inputDirectory, relative)
                        : Path.Combine(fileDir, relative);

                    if (!_all && File.Exists(targetPath)) continue;

                    toCapture.Add((url, options, commands, targetPath));
                }
            }

            if (toCapture.Count == 0)
            {
                Console.WriteLine("[snap] No screenshots to capture.");
                return;
            }

            Console.WriteLine($"[snap] Capturing {toCapture.Count} screenshot(s)...");

            if (!SnapFrameExtension.EnsureToolInstalled())
            {
                Console.WriteLine("[snap] SnapFrame tool could not be installed. Aborting.");
                return;
            }

            foreach (var item in toCapture)
            {
                SnapFrameExtension.CaptureScreenshot(item.Url, item.Options, item.Commands, item.TargetPath);
            }

            Console.WriteLine("[snap] Done.");
        }

        private static (string Options, List<string> Commands) ParseOptionsAndCommands(string raw)
        {
            var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var commands = new List<string>();
            var options = "";

            if (lines.Length > 0)
            {
                options = lines[0].Trim();
                for (int i = 1; i < lines.Length; i++)
                {
                    var l = lines[i].Trim();
                    if (!string.IsNullOrEmpty(l)) commands.Add(l);
                }
            }

            return (options, commands);
        }
    }
}
