using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Neko.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    public class SnapFrameInline : Inline
    {
        public string Url { get; set; }
        public string Options { get; set; }
        public List<string> Commands { get; set; } = new List<string>();
    }

    public class SnapFrameParser : InlineParser
    {
        public SnapFrameParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            if (slice.CurrentChar != '!')
            {
                slice = saved;
                return false;
            }
            slice.NextChar();

            var match = slice.Match("snapframe");
            if (!match)
            {
                slice = saved;
                return false;
            }
            slice.Start += 9;

            if (slice.CurrentChar != ' ' && slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }
            if (slice.CurrentChar == ' ') slice.NextChar();

            var contentStart = slice.Start;

            while (slice.CurrentChar != ']' && !slice.IsEmpty)
            {
                slice.NextChar();
            }

            if (slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }

            var content = slice.Text.Substring(contentStart, slice.Start - contentStart).Trim();
            slice.NextChar();

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).ToList();
            if (lines.Count == 0)
            {
                processor.Inline = new SnapFrameInline { Url = "", Options = "" };
                return true;
            }

            var firstLine = lines[0];
            var firstSpace = firstLine.IndexOf(' ');
            string url = firstLine;
            string options = "";

            if (firstSpace > 0)
            {
                url = firstLine.Substring(0, firstSpace);
                options = firstLine.Substring(firstSpace + 1).Trim();
            }

            var commands = lines.Skip(1).ToList();

            processor.Inline = new SnapFrameInline { Url = url, Options = options, Commands = commands };
            return true;
        }
    }

    public class SnapFrameExtension : IMarkdownExtension
    {
        private string _inputDirectory;

        public SnapFrameExtension(string inputDirectory)
        {
            _inputDirectory = inputDirectory;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<SnapFrameParser>())
            {
                pipeline.InlineParsers.Insert(0, new SnapFrameParser());
            }

            // We will call ProcessDocument explicitly from MarkdownParser to guarantee execution
            // pipeline.DocumentProcessed += DocumentProcessed;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            // Do not render the SnapFrameInline itself, it's just metadata
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<SnapFrameRenderer>();
            }
        }

        public void ProcessDocument(MarkdownDocument document)
        {
            if (string.IsNullOrEmpty(_inputDirectory)) return;

            foreach (var node in document.Descendants())
            {
                if (node is SnapFrameInline snapFrame)
                {
                    // Find the next LinkInline that is an image
                    Inline current = snapFrame;
                    LinkInline targetImage = null;

                    // Search siblings
                    while (current.NextSibling != null)
                    {
                        current = current.NextSibling;
                        if (current is LinkInline link && link.IsImage)
                        {
                            targetImage = link;
                            break;
                        }
                    }

                    if (targetImage != null && !string.IsNullOrEmpty(targetImage.Url))
                    {
                        var url = targetImage.Url;
                        if (url.StartsWith("http://") || url.StartsWith("https://"))
                        {
                            continue;
                        }

                        // Remove starting slash if any for relative resolution
                        var relativePath = url.TrimStart('/');

                        // Wait, we need the exact output path to save the image to.
                        // We will save it in the input directory, so it's a permanent asset.
                        var targetPath = Path.Combine(_inputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

                        if (!File.Exists(targetPath))
                        {
                            CaptureScreenshot(snapFrame.Url, snapFrame.Options, snapFrame.Commands, targetPath);
                        }
                    }
                }
            }
        }

        private bool EnsureToolInstalled()
        {
            try
            {
                var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                var exeName = isWindows ? "snapframe.exe" : "snapframe";

                var testProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                testProcess.Start();
                testProcess.WaitForExit();
                return testProcess.ExitCode == 0;
            }
            catch
            {
                Console.WriteLine("[SnapFrame] Tool 'snapframe' not found. Installing...");
                try
                {
                    var installProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "tool install -g SnapFrame",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    installProcess.Start();
                    installProcess.WaitForExit();

                    if (installProcess.ExitCode == 0)
                    {
                        Console.WriteLine("[SnapFrame] Successfully installed SnapFrame tool. Initializing playwright...");
                        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                        var exeName = isWindows ? "snapframe.exe" : "snapframe";

                        var initProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = exeName,
                                Arguments = "install",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        initProcess.Start();
                        initProcess.WaitForExit();
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[SnapFrame] Failed to install tool: {installProcess.StandardError.ReadToEnd()}");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SnapFrame] Exception while installing tool: {ex.Message}");
                    return false;
                }
            }
        }

        private void CaptureScreenshot(string url, string options, List<string> commands, string targetPath)
        {
            try
            {
                if (!EnsureToolInstalled())
                {
                    Console.WriteLine("[SnapFrame] SnapFrame tool could not be executed or installed. Skipping screenshot capture.");
                    return;
                }

                var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
                var exeName = isWindows ? "snapframe.exe" : "snapframe";

                var directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Console.WriteLine($"[SnapFrame] Capturing {url} to {targetPath}...");

                // Navigate and get page ID
                var navigateProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = $"navigate-json \"{url}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                navigateProcess.Start();
                string output = navigateProcess.StandardOutput.ReadToEnd();
                navigateProcess.WaitForExit();

                if (navigateProcess.ExitCode != 0)
                {
                    Console.WriteLine($"[SnapFrame] Failed to navigate: {navigateProcess.StandardError.ReadToEnd()}");
                    return;
                }

                // Parse Page ID
                var match = Regex.Match(output, "\"PageId\"\\s*:\\s*\"([^\"]+)\"");
                if (!match.Success)
                {
                    Console.WriteLine("[SnapFrame] Could not parse PageId from output.");
                    return;
                }

                var pageId = match.Groups[1].Value;

                // Execute Commands
                if (commands != null)
                {
                    foreach (var commandLine in commands)
                    {
                        var firstSpace = commandLine.IndexOf(' ');
                        if (firstSpace < 0) continue;

                        var action = commandLine.Substring(0, firstSpace).Trim();
                        var commandArgs = commandLine.Substring(firstSpace + 1).Trim();
                        var finalArgs = "";

                        if (action.Equals("click", StringComparison.OrdinalIgnoreCase))
                        {
                            if (commandArgs.StartsWith("'") && commandArgs.EndsWith("'"))
                            {
                                finalArgs = $"click {pageId} --text \"{commandArgs.Substring(1, commandArgs.Length - 2)}\"";
                            }
                            else if (commandArgs.StartsWith("\"") && commandArgs.EndsWith("\""))
                            {
                                finalArgs = $"click {pageId} --text {commandArgs}";
                            }
                            else
                            {
                                var parts = commandArgs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length == 2 && int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _))
                                {
                                    finalArgs = $"click {pageId} --x {parts[0]} --y {parts[1]}";
                                }
                                else
                                {
                                    finalArgs = $"click {pageId} --text \"{commandArgs}\"";
                                }
                            }
                        }
                        else if (action.Equals("interact", StringComparison.OrdinalIgnoreCase))
                        {
                            var argsFirstSpace = commandArgs.IndexOf(' ');
                            if (argsFirstSpace > 0)
                            {
                                var elementId = commandArgs.Substring(0, argsFirstSpace).Trim();
                                var rest = commandArgs.Substring(argsFirstSpace + 1).Trim();

                                if (rest.StartsWith("value=", StringComparison.OrdinalIgnoreCase))
                                {
                                    var val = rest.Substring("value=".Length).Trim();
                                    if (val.StartsWith("'") && val.EndsWith("'")) val = val.Substring(1, val.Length - 2);
                                    else if (val.StartsWith("\"") && val.EndsWith("\"")) val = val.Substring(1, val.Length - 2);

                                    finalArgs = $"interact {pageId} {elementId} set-value --value \"{val}\"";
                                }
                                else if (rest.Equals("click", StringComparison.OrdinalIgnoreCase))
                                {
                                    finalArgs = $"interact {pageId} {elementId} click";
                                }
                                else if (rest.Equals("right-click", StringComparison.OrdinalIgnoreCase))
                                {
                                    finalArgs = $"interact {pageId} {elementId} right-click";
                                }
                                else
                                {
                                    Console.WriteLine($"[SnapFrame] Unrecognized interact action: {rest}");
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[SnapFrame] Invalid interact format: {commandArgs}");
                                continue;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[SnapFrame] Unrecognized action: {action}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(finalArgs))
                        {
                            Console.WriteLine($"[SnapFrame] Executing: {finalArgs}");
                            var commandProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = exeName,
                                    Arguments = finalArgs,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                }
                            };
                            commandProcess.Start();
                            commandProcess.WaitForExit();

                            if (commandProcess.ExitCode != 0)
                            {
                                Console.WriteLine($"[SnapFrame] Command failed: {commandProcess.StandardError.ReadToEnd()}");
                            }
                        }
                    }
                }

                // Capture
                var args = $"capture {pageId} \"{targetPath}\" {options}";
                var captureProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                captureProcess.Start();
                captureProcess.WaitForExit();

                if (captureProcess.ExitCode != 0)
                {
                    Console.WriteLine($"[SnapFrame] Capture failed: {captureProcess.StandardError.ReadToEnd()}");
                }
                else
                {
                    Console.WriteLine($"[SnapFrame] Captured successfully.");
                }

                // Close
                var closeProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exeName,
                        Arguments = $"close {pageId}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                closeProcess.Start();
                closeProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SnapFrame] Exception: {ex.Message}");
            }
        }
    }

    public class SnapFrameRenderer : HtmlObjectRenderer<SnapFrameInline>
    {
        protected override void Write(HtmlRenderer renderer, SnapFrameInline obj)
        {
            // Do nothing, it shouldn't render HTML
        }
    }
}
