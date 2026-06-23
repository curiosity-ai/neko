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
    /// The run is incremental and resumable: a sample whose fence already carries a
    /// <c>height=</c> token is left untouched (pass <c>--force</c> to recompute
    /// everything), and each file is saved as soon as one of its samples is
    /// measured, so an interrupted run keeps the heights it already computed.
    /// </summary>
    public class TesseraeHeightsCommand
    {
        private readonly string _input;
        private readonly bool _force;
        private readonly string? _file;

        // Opening fence of a tesserae block: optional indent, 3+ backticks, the
        // `tesserae` info word, then the rest of the info string (args).
        private static readonly Regex OpenFenceRegex = new(
            @"^(?<indent>[ \t]*)(?<fence>`{3,})tesserae(?<rest>.*)$",
            RegexOptions.Compiled);

        private static readonly Regex HeightTokenRegex = new(
            @"\s*\bheight\s*=\s*\d+\b", RegexOptions.Compiled);

        private static readonly Regex HeightValueRegex = new(
            @"\bheight\s*=\s*(\d+)\b", RegexOptions.Compiled);

        public TesseraeHeightsCommand(string input, bool force = false, string? file = null)
        {
            _input = input;
            _force = force;
            _file = file;
        }

        private sealed record Block(int OpenLine, int CloseLine, string Indent, string Fence, string Rest, string Code, int ExistingHeight);

        public async Task<int> RunAsync()
        {
            // When a single file is targeted, the input/project root is derived from
            // it (nearest ancestor neko.yml, else the file's directory). A targeted
            // run always re-measures that file's samples — there is no hash cache, so
            // rerun it after editing a sample.
            string? targetFileFullPath = null;
            string inputFullPath;
            if (!string.IsNullOrEmpty(_file))
            {
                targetFileFullPath = Path.GetFullPath(_file);
                if (!File.Exists(targetFileFullPath))
                {
                    Console.Error.WriteLine($"[tesserae-heights] File not found: {targetFileFullPath}");
                    return 2;
                }
                inputFullPath = FindProjectRoot(targetFileFullPath);
            }
            else
            {
                inputFullPath = Path.GetFullPath(_input);
                if (!Directory.Exists(inputFullPath))
                {
                    Console.Error.WriteLine($"[tesserae-heights] Input directory not found: {inputFullPath}");
                    return 2;
                }
            }

            var swTotal = Stopwatch.StartNew();
            // A targeted file re-measures unconditionally so an edited sample is resized.
            var force = _force || targetFileFullPath != null;

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

            // A targeted run measures just that one file; otherwise discover every
            // Markdown file, skipping hidden/dot folders (.neko-cache, .neko, .git,
            // .claude, …) and underscore-prefixed partials.
            var markdownFiles = targetFileFullPath != null
                ? new List<string> { targetFileFullPath }
                : Directory.EnumerateFiles(inputFullPath, "*.md", SearchOption.AllDirectories)
                    .Where(f => !PathHasHiddenSegment(inputFullPath, f))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

            var fileBlocks = new Dictionary<string, List<Block>>();
            foreach (var file in markdownFiles)
            {
                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch (Exception ex) { Console.WriteLine($"[tesserae-heights] Could not read {file}: {ex.Message}"); continue; }

                var blocks = ParseBlocks(lines);
                if (blocks.Count > 0) fileBlocks[file] = blocks;
            }

            if (fileBlocks.Count == 0)
            {
                Console.WriteLine("[tesserae-heights] No tesserae samples found.");
                return 0;
            }

            // A sample needs measuring when its fence has no height yet — unless
            // --force recomputes everything. Already-sized samples are skipped, so
            // re-runs only do the new work.
            bool NeedsMeasure(Block b) => force || b.ExistingHeight <= 0;

            var sampleCount = fileBlocks.Sum(kv => kv.Value.Count);
            var skippedExisting = fileBlocks.Sum(kv => kv.Value.Count(b => !NeedsMeasure(b)));

            // Compile only the samples we will actually measure.
            var toMeasureSamples = fileBlocks
                .SelectMany(kv => kv.Value)
                .Where(NeedsMeasure)
                .Select(b => (Arguments: b.Rest.Trim(), b.Code))
                .ToList();

            var totalToMeasure = toMeasureSamples.Select(s => s.Code).Distinct().Count();

            Console.WriteLine($"[tesserae-heights] {sampleCount} sample(s) across {fileBlocks.Count} file(s): "
                + $"{totalToMeasure} to measure, {skippedExisting} already sized"
                + (force ? " (recomputing all)." : "."));

            if (totalToMeasure == 0)
            {
                Console.WriteLine("[tesserae-heights] Nothing to do — every sample already has a height (use --force to recompute).");
                return 0;
            }

            var swCompile = Stopwatch.StartNew();
            await TesseraeCompiler.WarmAsync(toMeasureSamples, tempOutputRoot);
            swCompile.Stop();
            Console.WriteLine($"[tesserae-heights] Compile phase took {swCompile.Elapsed.TotalSeconds:n1}s.");

            // Measure each unique sample once (keyed by code), updating files as we go.
            var heightByCode = new Dictionary<string, int>();
            int measured = 0, failed = 0, updated = 0, filesChanged = 0;
            int measureIndex = 0;
            var swMeasure = Stopwatch.StartNew();
            Console.WriteLine($"[tesserae-heights] Measuring {totalToMeasure} unique sample(s) with the headless browser...");

            foreach (var (file, blocks) in fileBlocks)
            {
                var rel = Path.GetRelativePath(inputFullPath, file);
                var lines = File.ReadAllLines(file).ToList();
                var endsWithNewline = File.ReadAllText(file).EndsWith("\n");
                bool fileChanged = false;

                foreach (var b in blocks)
                {
                    if (!NeedsMeasure(b)) continue; // already sized — don't recompute

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
                    if (newOpen == lines[b.OpenLine]) continue;

                    lines[b.OpenLine] = newOpen;
                    updated++;

                    // Save immediately so an interrupted run keeps what it measured.
                    WriteLines(file, lines, endsWithNewline);
                    if (!fileChanged) { fileChanged = true; filesChanged++; }
                    Console.WriteLine($"[tesserae-heights] Saved {rel} ({b.Indent}{b.Fence}tesserae … height={height})");
                }
            }

            swMeasure.Stop();
            Console.WriteLine($"[tesserae-heights] Measure phase took {swMeasure.Elapsed.TotalSeconds:n1}s.");
            Console.WriteLine($"[tesserae-heights] Done in {swTotal.Elapsed.TotalSeconds:n1}s. "
                + $"Measured {measured}, failed {failed}, skipped {skippedExisting}, tokens updated {updated} in {filesChanged} file(s).");
            return 0;
        }

        private static void WriteLines(string file, List<string> lines, bool endsWithNewline)
        {
            var text = string.Join("\n", lines);
            if (endsWithNewline) text += "\n";
            File.WriteAllText(file, text);
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

                var hm = HeightValueRegex.Match(rest);
                var existingHeight = hm.Success && int.TryParse(hm.Groups[1].Value, out var h) ? h : 0;

                int close = -1;
                var bodyLines = new List<string>();
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (IsClosingFence(lines[j], fence.Length))
                    {
                        close = j;
                        break;
                    }
                    bodyLines.Add(lines[j]);
                }

                if (close < 0) break; // unterminated fence — leave the rest alone

                // Compile/measure the runnable source, honouring an
                // `// <overwrite-sample-code>` region exactly as the build does so the
                // measured sample matches what the preview actually renders.
                var (code, _) = TesseraeCompiler.PartitionSampleSource(bodyLines);
                blocks.Add(new Block(i, close, indent, fence, rest, code, existingHeight));
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

        // Walk up from a file to the nearest directory containing a neko.yml; fall
        // back to the file's own directory when none is found.
        private static string FindProjectRoot(string fileFullPath)
        {
            var dir = Path.GetDirectoryName(fileFullPath);
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "neko.yml"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return Path.GetDirectoryName(fileFullPath)!;
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
