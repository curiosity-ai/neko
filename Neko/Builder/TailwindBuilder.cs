using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Neko.Configuration;

namespace Neko.Builder
{
    /// <summary>
    /// Generates a static <c>assets/tailwind.css</c> at build time instead of
    /// relying on the Tailwind Play CDN (<c>cdn.tailwindcss.com</c>), which
    /// generates CSS in the browser on every page load and causes a flash of
    /// unstyled (white) content on navigation — especially noticeable in dark
    /// mode, where the dark <c>dark:*</c> utilities don't exist until the CDN
    /// script runs.
    ///
    /// This does what the Play CDN does, but once, at build time: it runs the
    /// official Tailwind <b>standalone</b> CLI (a self-contained executable — no
    /// Node/npm required at runtime, honouring Neko's embedded-resources rule)
    /// over the generated HTML/JS, scanning for the utility classes actually used
    /// and emitting only those, plus Preflight and the typography (<c>prose</c>)
    /// plugin. The result is a real, cacheable stylesheet applied before first
    /// paint.
    /// </summary>
    public static class TailwindBuilder
    {
        // Tailwind v3 to match the v3 Play CDN this replaces (class-based dark
        // mode + `require('@tailwindcss/typography')`, both bundled in the
        // standalone binary). v4 would require a different, CSS-first config.
        private const string TailwindVersion = "v3.4.17";

        private static readonly SemaphoreSlim _cliGate = new SemaphoreSlim(1, 1);
        private static string _resolvedCli;
        private static bool _resolveAttempted;

        /// <summary>
        /// Locates a usable Tailwind CLI, downloading the standalone binary into a
        /// per-user cache on first use. Returns the executable path, or
        /// <c>null</c> if no CLI could be resolved (the caller then falls back to
        /// the CDN). Resolution order: <c>NEKO_TAILWIND_CLI</c> env var, then
        /// <c>tailwindcss</c> on <c>PATH</c>, then the cached/downloaded binary.
        /// Set <c>NEKO_DISABLE_STATIC_TAILWIND=1</c> to opt out entirely.
        /// </summary>
        public static async Task<string> ResolveCliAsync()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("NEKO_DISABLE_STATIC_TAILWIND"), "1", StringComparison.Ordinal))
                return null;

            await _cliGate.WaitAsync();
            try
            {
                if (_resolveAttempted) return _resolvedCli;
                _resolveAttempted = true;

                var explicitPath = Environment.GetEnvironmentVariable("NEKO_TAILWIND_CLI");
                if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
                {
                    _resolvedCli = explicitPath;
                    return _resolvedCli;
                }

                var onPath = FindOnPath("tailwindcss");
                if (onPath != null)
                {
                    _resolvedCli = onPath;
                    return _resolvedCli;
                }

                _resolvedCli = await DownloadStandaloneAsync();
                return _resolvedCli;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: could not resolve the Tailwind CLI ({ex.Message}); falling back to the Tailwind CDN.");
                _resolvedCli = null;
                return null;
            }
            finally
            {
                _cliGate.Release();
            }
        }

        /// <summary>
        /// Runs the Tailwind CLI over the generated site in <paramref name="outputDir"/>
        /// and writes <c>assets/tailwind.css</c>. Content is scanned from the
        /// emitted <c>.html</c> (which inlines most of Neko's scripts, so
        /// JS-applied classes are picked up) and the non-minified asset <c>.js</c>
        /// (e.g. <c>history.js</c>). Returns true on success.
        /// </summary>
        public static async Task<bool> GenerateAsync(string outputDir, NekoConfig config, string cliPath)
        {
            if (string.IsNullOrEmpty(cliPath)) return false;

            var assetsDir = Path.Combine(outputDir, "assets");
            Directory.CreateDirectory(assetsDir);
            var cssOut = Path.Combine(assetsDir, "tailwind.css");

            var tempDir = Path.Combine(Path.GetTempPath(), "neko-tailwind-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var configPath = Path.Combine(tempDir, "tailwind.config.js");
                var inputPath = Path.Combine(tempDir, "input.css");

                await File.WriteAllTextAsync(configPath, BuildConfigJs(outputDir, config));
                await File.WriteAllTextAsync(inputPath,
                    "@tailwind base;\n@tailwind components;\n@tailwind utilities;\n");

                var psi = new ProcessStartInfo
                {
                    FileName = cliPath,
                    WorkingDirectory = outputDir,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add(configPath);
                psi.ArgumentList.Add("-i");
                psi.ArgumentList.Add(inputPath);
                psi.ArgumentList.Add("-o");
                psi.ArgumentList.Add(cssOut);
                psi.ArgumentList.Add("--minify");

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    Console.WriteLine("Warning: failed to start the Tailwind CLI; keeping the CDN fallback.");
                    return false;
                }

                var stderr = await proc.StandardError.ReadToEndAsync();
                await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                if (proc.ExitCode != 0 || !File.Exists(cssOut))
                {
                    Console.WriteLine($"Warning: Tailwind CLI exited with code {proc.ExitCode}; the static stylesheet was not generated.");
                    if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine(stderr.Trim());
                    return false;
                }

                Console.WriteLine($"Generated static Tailwind stylesheet: {Path.GetRelativePath(outputDir, cssOut)}");
                return true;
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
            }
        }

        // Builds the Tailwind config. `content` mirrors what the Play CDN scans
        // at runtime — the rendered HTML plus the JS that toggles classes — and
        // `theme.extend.colors` injects the per-site primary/accent ramps so the
        // dynamic palette resolves at build time. Absolute, forward-slashed globs
        // so fast-glob matches on every platform.
        private static string BuildConfigJs(string outputDir, NekoConfig config)
        {
            var (primary, accent) = ThemeDefinitions.ResolvePalettes(config);
            var primaryJson = JsonSerializer.Serialize(primary);
            var accentJson = JsonSerializer.Serialize(accent);

            var root = outputDir.Replace("\\", "/").TrimEnd('/');

            var sb = new StringBuilder();
            sb.AppendLine("module.exports = {");
            sb.AppendLine("  content: {");
            sb.AppendLine("    files: [");
            sb.AppendLine($"      '{root}/**/*.html',");
            sb.AppendLine($"      '{root}/**/*.js',");
            // Exclude minified vendor blobs — their source contains many strings
            // that look like Tailwind utilities and would otherwise be picked up
            // as "used" and bloat the output.
            sb.AppendLine($"      '!{root}/**/*.min.js',");
            sb.AppendLine("    ],");
            sb.AppendLine("  },");
            sb.AppendLine("  darkMode: 'class',");
            sb.AppendLine("  theme: { extend: { colors: {");
            sb.AppendLine($"    primary: {primaryJson},");
            sb.AppendLine($"    accent: {accentJson},");
            sb.AppendLine("  } } },");
            sb.AppendLine("  plugins: [require('@tailwindcss/typography')],");
            sb.AppendLine("};");
            return sb.ToString();
        }

        private static string FindOnPath(string name)
        {
            var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? name + ".exe" : name;
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar)) return null;

            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    var candidate = Path.Combine(dir.Trim(), exeName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { /* malformed PATH entry */ }
            }
            return null;
        }

        private static async Task<string> DownloadStandaloneAsync()
        {
            var assetName = StandaloneAssetName();
            if (assetName == null)
            {
                Console.WriteLine($"Warning: no Tailwind standalone binary for {RuntimeInformation.OSDescription} / {RuntimeInformation.ProcessArchitecture}; falling back to the CDN.");
                return null;
            }

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "neko", "tools");
            Directory.CreateDirectory(cacheDir);

            var cachedExe = Path.Combine(cacheDir, $"tailwindcss-{TailwindVersion}" + (isWindows ? ".exe" : ""));
            if (File.Exists(cachedExe)) return cachedExe;

            var url = $"https://github.com/tailwindlabs/tailwindcss/releases/download/{TailwindVersion}/{assetName}";
            Console.WriteLine($"Downloading Tailwind standalone CLI ({TailwindVersion}) for first use...");

            var tempFile = cachedExe + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();
                await using var src = await resp.Content.ReadAsStreamAsync();
                await using var dst = File.Create(tempFile);
                await src.CopyToAsync(dst);
            }

            if (!isWindows)
            {
                // rwxr-xr-x
                File.SetUnixFileMode(tempFile,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            // Atomic-ish publish so a concurrent/aborted download never yields a
            // half-written executable.
            File.Move(tempFile, cachedExe, overwrite: true);
            return cachedExe;
        }

        private static string StandaloneAssetName()
        {
            string os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) os = "linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) os = "macos";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) os = "windows";
            else return null;

            string arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "armv7", // Linux only; macOS/Windows have no armv7 asset
                _ => null,
            };
            if (arch == null) return null;
            if (arch == "armv7" && os != "linux") return null;

            var name = $"tailwindcss-{os}-{arch}";
            return os == "windows" ? name + ".exe" : name;
        }
    }
}
