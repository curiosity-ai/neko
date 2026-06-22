using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neko.Builder
{
    /// <summary>
    /// Measures the rendered height of every <c>```tesserae</c> live sample with a
    /// headless browser and bakes the result back into each fence as a
    /// <c>height=NNN</c> token. A normal <c>build</c>/<c>watch</c> then reads that
    /// token and sizes the live-preview iframe up front, so the page reserves the
    /// right space and never reflows once the sample renders. Builds never measure;
    /// this command is the only place the browser runs (analogous to
    /// <c>gen-images</c> / <c>check-links</c>).
    ///
    /// Samples are compiled into a throwaway folder under <c>.neko-cache</c> (so the
    /// runtime assets exist for the headless render), each unique sample is measured
    /// once, and only files whose tokens actually change are rewritten.
    /// </summary>
    public class TesseraeHeightsCommand
    {
        private readonly string _input;

        // Opening fence of a tesserae block: optional indent, 3+ backticks, the
        // `tesserae` info word, then the rest of the info string (args).
        private static readonly Regex OpenFenceRegex = new(
            @"^(?<indent>[ \t]*)(?<fence>`{3,})tesserae(?<rest>.*)$",
            RegexOptions.Compiled);

        private static readonly Regex HeightTokenRegex = new(
            @"\s*\bheight\s*=\s*\d+\b", RegexOptions.Compiled);

        public TesseraeHeightsCommand(string input)
        {
            _input = input;
        }

        private sealed record Block(int OpenLine, int CloseLine, string Indent, string Fence, string Rest, string Code);

        public async Task<int> RunAsync()
        {
            var inputFullPath = Path.GetFullPath(_input);
            if (!Directory.Exists(inputFullPath))
            {
                Console.Error.WriteLine($"[tesserae-heights] Input directory not found: {inputFullPath}");
                return 2;
            }

            var swTotal = Stopwatch.StartNew();

            // Keep build artifacts inside the project's .neko-cache (never OS temp),
            // matching the rest of the CLI.
            TesseraeCompiler.SetCacheRoot(Path.Combine(inputFullPath, ".neko-cache"));
            var tempOutputRoot = Path.Combine(inputFullPath, ".neko-cache", "_heights");
            Directory.CreateDirectory(tempOutputRoot);

            var configPath = Path.Combine(inputFullPath, "neko.yml");
            var config = File.Exists(configPath)
                ? Neko.Configuration.ConfigParser.Parse(configPath)
                : new Neko.Configuration.NekoConfig();
            TesseraeCompiler.Configure(
                config.Tesserae?.Version,
                config.Tesserae?.MaxParallelism ?? 0,
                config.Tesserae?.MeasureWidth ?? 0);

            // Discover every Markdown file, skipping hidden/dot folders (.neko-cache,
            // .neko, .git, .claude, …) and underscore-prefixed partials.
            var markdownFiles = Directory.EnumerateFiles(inputFullPath, "*.md", SearchOption.AllDirectories)
                .Where(f => !PathHasHiddenSegment(inputFullPath, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            // Parse blocks per file first so we can warm-compile every unique sample
            // in parallel before the (serial) measurement pass.
            var fileBlocks = new Dictionary<string, List<Block>>();
            var allSamples = new List<(string Arguments, string Code)>();
            foreach (var file in markdownFiles)
            {
                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch (Exception ex) { Console.WriteLine($"[tesserae-heights] Could not read {file}: {ex.Message}"); continue; }

                var blocks = ParseBlocks(lines);
                if (blocks.Count == 0) continue;
                fileBlocks[file] = blocks;
                foreach (var b in blocks) allSamples.Add((b.Rest.Trim(), b.Code));
            }

            if (fileBlocks.Count == 0)
            {
                Console.WriteLine("[tesserae-heights] No tesserae samples found.");
                return 0;
            }

            var sampleCount = fileBlocks.Sum(kv => kv.Value.Count);
            Console.WriteLine($"[tesserae-heights] {sampleCount} sample(s) across {fileBlocks.Count} file(s). Compiling...");

            var swCompile = Stopwatch.StartNew();
            await TesseraeCompiler.WarmAsync(allSamples, tempOutputRoot);
            swCompile.Stop();
            Console.WriteLine($"[tesserae-heights] Compile phase took {swCompile.Elapsed.TotalSeconds:n1}s.");

            // Measure each unique sample once (keyed by code), then rewrite files.
            var heightByCode = new Dictionary<string, int>();
            int measured = 0, failed = 0, updated = 0, filesChanged = 0;

            // One headless render per unique sample; report progress against that.
            var totalToMeasure = allSamples.Select(s => s.Code).Distinct().Count();
            int measureIndex = 0;
            var swMeasure = Stopwatch.StartNew();
            Console.WriteLine($"[tesserae-heights] Measuring {totalToMeasure} unique sample(s) with the headless browser...");

            foreach (var (file, blocks) in fileBlocks)
            {
                var lines = File.ReadAllLines(file).ToList();
                bool fileDirty = false;

                foreach (var b in blocks)
                {
                    if (!heightByCode.TryGetValue(b.Code, out var height))
                    {
                        var label = string.IsNullOrWhiteSpace(b.Rest) ? Path.GetFileName(file) : b.Rest.Trim();
                        measureIndex++;
                        Console.WriteLine($"[tesserae-heights] [{measureIndex}/{totalToMeasure}] measuring {label} ...");
                        var swSample = Stopwatch.StartNew();
                        var result = await TesseraeCompiler.CompileAsync(label, b.Code, tempOutputRoot);
                        height = result?.OutputHtml != null
                            ? await TesseraeCompiler.MeasureHeightAsync(result.OutputHtml, tempOutputRoot, label)
                            : 0;
                        swSample.Stop();
                        heightByCode[b.Code] = height;
                        if (height > 0) measured++; else failed++;
                        Console.WriteLine($"[tesserae-heights] [{measureIndex}/{totalToMeasure}] {label} -> {(height > 0 ? height + "px" : "no measurement")} in {swSample.Elapsed.TotalSeconds:n1}s");
                    }

                    if (height <= 0) continue; // measurement failed — leave the fence as-is

                    var newOpen = BuildOpenLine(b, height);
                    if (newOpen != lines[b.OpenLine])
                    {
                        lines[b.OpenLine] = newOpen;
                        fileDirty = true;
                        updated++;
                    }
                }

                if (fileDirty)
                {
                    // Preserve the file's trailing newline convention.
                    var text = string.Join("\n", lines);
                    var original = File.ReadAllText(file);
                    if (original.EndsWith("\n")) text += "\n";
                    File.WriteAllText(file, text);
                    filesChanged++;
                    Console.WriteLine($"[tesserae-heights] Updated {Path.GetRelativePath(inputFullPath, file)}");
                }
            }

            swMeasure.Stop();
            Console.WriteLine($"[tesserae-heights] Measure phase took {swMeasure.Elapsed.TotalSeconds:n1}s.");
            Console.WriteLine($"[tesserae-heights] Done in {swTotal.Elapsed.TotalSeconds:n1}s. Measured {measured}, failed {failed}, tokens updated {updated} in {filesChanged} file(s).");
            return 0;
        }

        // Build the rewritten opening fence with the height token set/replaced,
        // preserving indent, fence length, and any other args.
        private static string BuildOpenLine(Block b, int height)
        {
            var rest = HeightTokenRegex.Replace(b.Rest, "").Trim();
            var newRest = rest.Length > 0 ? $"{rest} height={height}" : $"height={height}";
            return $"{b.Indent}{b.Fence}tesserae {newRest}";
        }

        // Scan raw lines for tesserae fenced blocks. A closing fence is a line of the
        // same indentation made only of >= as many backticks as the opener.
        private static List<Block> ParseBlocks(string[] lines)
        {
            var blocks = new List<Block>();
            for (int i = 0; i < lines.Length; i++)
            {
                var open = OpenFenceRegex.Match(lines[i]);
                if (!open.Success) continue;

                var indent = open.Groups["indent"].Value;
                var fence = open.Groups["fence"].Value;
                var rest = open.Groups["rest"].Value;

                int close = -1;
                var code = new StringBuilder();
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (IsClosingFence(lines[j], fence.Length))
                    {
                        close = j;
                        break;
                    }
                    code.Append(lines[j]).Append('\n');
                }

                if (close < 0) break; // unterminated fence — leave the rest alone
                blocks.Add(new Block(i, close, indent, fence, rest, code.ToString()));
                i = close;
            }
            return blocks;
        }

        private static bool IsClosingFence(string line, int minBackticks)
        {
            var t = line.Trim();
            if (t.Length < minBackticks) return false;
            foreach (var c in t) if (c != '`') return false;
            return true;
        }

        private static bool PathHasHiddenSegment(string root, string file)
        {
            var rel = Path.GetRelativePath(root, file);
            foreach (var seg in rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (seg.StartsWith(".") || seg.StartsWith("_")) return true;
            }
            return false;
        }
    }
}
