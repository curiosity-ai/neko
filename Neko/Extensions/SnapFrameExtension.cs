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
using System.IO.Compression;
using System.Text;
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
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<SnapFrameParser>())
            {
                pipeline.InlineParsers.Insert(0, new SnapFrameParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            // Do not render the SnapFrameInline itself, it's just metadata
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<SnapFrameRenderer>();
            }
        }

        public static bool EnsureToolInstalled()
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

        // Settle delays applied on top of whatever the underlying snapframe/Playwright
        // call waits for. Covers SPA rendering, web fonts, late images, and post-action
        // UI updates that `load` / `WaitForExit` does not.
        private const int PostNavigateSettleMs = 1000;
        private const int PostCommandSettleMs = 500;
        private const int PreCaptureSettleMs = 500;

        public static void CaptureScreenshot(string url, string options, List<string> commands, string targetPath)
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

                // Let SPA frameworks render, fonts load, etc., before we either issue
                // commands or take the screenshot.
                Thread.Sleep(PostNavigateSettleMs);

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

                            // Let the page react to the command before the next step.
                            Thread.Sleep(PostCommandSettleMs);
                        }
                    }
                }

                // Final settle so any in-flight rendering finishes before capture.
                Thread.Sleep(PreCaptureSettleMs);

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
                else if (!File.Exists(targetPath))
                {
                    Console.WriteLine($"[SnapFrame] Capture reported success but produced no file at {targetPath}.");
                }
                else if (IsBlankImage(targetPath, out var reason))
                {
                    // Drop the file so the next `neko snap` run will retry instead of
                    // leaving a broken screenshot in the repo.
                    try { File.Delete(targetPath); } catch { /* best-effort */ }
                    Console.WriteLine($"[SnapFrame] Captured image rejected as blank ({reason}); deleted {targetPath}.");
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

        /// <summary>
        /// Returns true when <paramref name="path"/> is empty, unreadable, or every
        /// decoded pixel has the same value (e.g. a full-white or full-transparent
        /// capture from a page that never finished rendering).
        /// </summary>
        public static bool IsBlankImage(string path, out string reason)
        {
            reason = "";
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0)
                {
                    reason = "file missing or empty";
                    return true;
                }

                using var stream = File.OpenRead(path);
                if (!IsPngSignature(stream))
                {
                    // Unknown format - leave it alone rather than deleting something
                    // we cannot understand.
                    return false;
                }

                return IsSingleColorPng(stream, out reason);
            }
            catch (Exception ex)
            {
                reason = $"decode error: {ex.Message}";
                // Don't delete on decode error: better to keep a possibly-broken
                // image than to lose a valid one.
                return false;
            }
        }

        private static bool IsPngSignature(Stream s)
        {
            Span<byte> sig = stackalloc byte[8];
            int read = 0;
            while (read < 8)
            {
                int n = s.Read(sig.Slice(read));
                if (n <= 0) return false;
                read += n;
            }
            return sig[0] == 0x89 && sig[1] == 0x50 && sig[2] == 0x4E && sig[3] == 0x47
                && sig[4] == 0x0D && sig[5] == 0x0A && sig[6] == 0x1A && sig[7] == 0x0A;
        }

        private static bool IsSingleColorPng(Stream png, out string reason)
        {
            reason = "";
            int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
            var idat = new MemoryStream();

            Span<byte> header = stackalloc byte[8];
            while (true)
            {
                int read = 0;
                while (read < 8)
                {
                    int n = png.Read(header.Slice(read));
                    if (n <= 0) { reason = "truncated PNG"; return false; }
                    read += n;
                }
                int length = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
                string type = Encoding.ASCII.GetString(header.Slice(4, 4));

                if (type == "IHDR")
                {
                    if (length < 13) { reason = "bad IHDR"; return false; }
                    var ihdr = new byte[length];
                    ReadExact(png, ihdr);
                    width = (ihdr[0] << 24) | (ihdr[1] << 16) | (ihdr[2] << 8) | ihdr[3];
                    height = (ihdr[4] << 24) | (ihdr[5] << 16) | (ihdr[6] << 8) | ihdr[7];
                    bitDepth = ihdr[8];
                    colorType = ihdr[9];
                    interlace = ihdr[12];
                }
                else if (type == "IDAT")
                {
                    var buf = new byte[length];
                    ReadExact(png, buf);
                    idat.Write(buf, 0, buf.Length);
                }
                else if (type == "IEND")
                {
                    break;
                }
                else
                {
                    // Skip chunk data
                    Skip(png, length);
                }
                // Skip CRC
                Skip(png, 4);
            }

            if (width <= 0 || height <= 0)
            {
                reason = "no IHDR";
                return false;
            }
            if (interlace != 0)
            {
                // Adam7 interlacing is rare for Playwright captures; if we hit it,
                // bail out rather than misjudge the image.
                return false;
            }

            int channels = colorType switch
            {
                0 => 1, // grayscale
                2 => 3, // RGB
                3 => 1, // palette index
                4 => 2, // grayscale + alpha
                6 => 4, // RGBA
                _ => 0,
            };
            if (channels == 0 || (bitDepth != 8 && bitDepth != 16))
            {
                // Sub-byte depths and palette images are uncommon for screenshots;
                // skip the check rather than risk a wrong verdict.
                return false;
            }

            int bytesPerPixel = channels * (bitDepth / 8);
            int scanlineBytes = bytesPerPixel * width;

            idat.Position = 0;
            using var zlib = new ZLibStream(idat, CompressionMode.Decompress);

            var prev = new byte[scanlineBytes];
            var curr = new byte[scanlineBytes];
            byte[]? firstPixel = null;

            for (int y = 0; y < height; y++)
            {
                int filter = zlib.ReadByte();
                if (filter < 0) { reason = "truncated IDAT"; return false; }
                ReadExact(zlib, curr);
                UnfilterScanline(filter, curr, prev, bytesPerPixel);

                if (firstPixel == null)
                {
                    firstPixel = new byte[bytesPerPixel];
                    Array.Copy(curr, firstPixel, bytesPerPixel);
                }

                for (int x = 0; x < scanlineBytes; x += bytesPerPixel)
                {
                    for (int b = 0; b < bytesPerPixel; b++)
                    {
                        if (curr[x + b] != firstPixel[b])
                        {
                            return false;
                        }
                    }
                }

                (prev, curr) = (curr, prev);
            }

            reason = firstPixel != null
                ? $"all pixels equal {FormatPixel(firstPixel, colorType)}"
                : "no pixels decoded";
            return true;
        }

        private static void UnfilterScanline(int filter, byte[] curr, byte[] prev, int bpp)
        {
            int len = curr.Length;
            switch (filter)
            {
                case 0: // None
                    break;
                case 1: // Sub
                    for (int i = bpp; i < len; i++) curr[i] = (byte)(curr[i] + curr[i - bpp]);
                    break;
                case 2: // Up
                    for (int i = 0; i < len; i++) curr[i] = (byte)(curr[i] + prev[i]);
                    break;
                case 3: // Average
                    for (int i = 0; i < len; i++)
                    {
                        byte left = i >= bpp ? curr[i - bpp] : (byte)0;
                        byte up = prev[i];
                        curr[i] = (byte)(curr[i] + (left + up) / 2);
                    }
                    break;
                case 4: // Paeth
                    for (int i = 0; i < len; i++)
                    {
                        byte a = i >= bpp ? curr[i - bpp] : (byte)0;
                        byte b = prev[i];
                        byte c = i >= bpp ? prev[i - bpp] : (byte)0;
                        int p = a + b - c;
                        int pa = Math.Abs(p - a);
                        int pb = Math.Abs(p - b);
                        int pc = Math.Abs(p - c);
                        byte pred = (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                        curr[i] = (byte)(curr[i] + pred);
                    }
                    break;
                default:
                    throw new InvalidDataException($"Unknown PNG filter type {filter}");
            }
        }

        private static void ReadExact(Stream s, byte[] buffer)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int n = s.Read(buffer, total, buffer.Length - total);
                if (n <= 0) throw new EndOfStreamException();
                total += n;
            }
        }

        private static void Skip(Stream s, int count)
        {
            if (s.CanSeek) { s.Seek(count, SeekOrigin.Current); return; }
            var buf = new byte[Math.Min(count, 4096)];
            while (count > 0)
            {
                int n = s.Read(buf, 0, Math.Min(buf.Length, count));
                if (n <= 0) throw new EndOfStreamException();
                count -= n;
            }
        }

        private static string FormatPixel(byte[] pixel, int colorType)
        {
            return colorType switch
            {
                0 => $"gray={pixel[0]}",
                2 when pixel.Length >= 3 => $"rgb({pixel[0]},{pixel[1]},{pixel[2]})",
                4 when pixel.Length >= 2 => $"gray={pixel[0]} a={pixel[1]}",
                6 when pixel.Length >= 4 => $"rgba({pixel[0]},{pixel[1]},{pixel[2]},{pixel[3]})",
                _ => Convert.ToHexString(pixel),
            };
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
