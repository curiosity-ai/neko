using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using NuGet.Versioning;
using Transpose.Compiler.Library;
using UID;

namespace Neko.Builder
{
    public class TesseraeCompilerResult
    {
        public string AppJsContent { get; set; }
        public List<string> AssetsPath { get; set; } = new List<string>();
        public string OutputHtml { get; set; }
    }

    public static class TesseraeCompiler
    {
        private static readonly System.Text.RegularExpressions.Regex _closingScriptTag =
            new System.Text.RegularExpressions.Regex("</(?=script)", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly HttpClient _httpClient = new HttpClient();
        private static Dictionary<string, NuGetVersion> _cachedLatestVersion = new Dictionary<string, NuGetVersion>();

        // Resolving a package version touches the network and writes the on-disk
        // version record, so serialise it across the parallel warm pass.
        private static readonly SemaphoreSlim _versionLock = new SemaphoreSlim(1, 1);

        // In-memory cache of compiled results, keyed by {hash}_{version}. Survives
        // across watch rebuilds (the process stays alive), so an unchanged sample
        // is served instantly on every rebuild without even touching disk.
        private static readonly ConcurrentDictionary<string, TesseraeCompilerResult> _memCache =
            new ConcurrentDictionary<string, TesseraeCompilerResult>();

        // Parallel compiles restore the same shared runtime files (tps.js, css, …);
        // serialise the actual file writes so two threads never write one path at
        // once. The Transpose compilation itself stays parallel.
        private static readonly object _assetWriteLock = new object();

        // Bumped whenever the *shape* of a compiled result changes (the generated
        // OutputHtml, the asset file names it references, …). It is part of the cache
        // key so a stale `.neko-cache` written by an older neko isn't reused by a
        // newer one — otherwise the cached HTML can reference asset variants the new
        // build no longer writes (e.g. the h5 runtime's `h5.js` vs Transpose's
        // `tps.js`), 404ing the runtime and leaving the live preview blank.
        // 7: samples are compiled by Transpose (`Transpose.Compiler.Library`) rather
        //    than H5, so every asset name and the load order changed.
        private const string CacheFormatVersion = "7";

        // Tesserae's own surface colours (see tss.common.css): white in light
        // mode, #222 in dark mode. Hard-coded here so the live-preview iframe can
        // paint the right background *before* the Tesserae stylesheet has loaded.
        private const string LightBackground = "#ffffff";
        private const string DarkBackground = "#222222";

        /// <summary>
        /// Makes compiled app JS safe to inline inside a &lt;script&gt; element. The compiled
        /// app can legitimately contain the literal "&lt;/script&gt;" — for example a Sandbox
        /// sample whose <c>srcdoc</c> HTML embeds its own &lt;script&gt; block. Inlined verbatim,
        /// the HTML parser closes the surrounding &lt;script&gt; tag at that inner sequence and
        /// dumps the rest of the app as page text, so the sample never runs. Backslash-escaping
        /// the slash keeps the JS identical (the sequence only ever occurs inside a string,
        /// regex or comment) while hiding it from the HTML parser.
        /// </summary>
        internal static string EscapeForInlineScript(string js) =>
            string.IsNullOrEmpty(js) ? js : _closingScriptTag.Replace(js, "<\\/");

        // Runs in <head>, before the Tesserae stylesheet, so the iframe paints in
        // the docs page's light/dark surface colour from the very first frame
        // instead of flashing white while the sample compiles and boots. It sets
        // the background on <html> directly (covers the pre-CSS phase) and adds
        // `tss-dark-mode` to <html> so the Tesserae CSS variables cascade dark the
        // moment the stylesheet loads — long before the body-end bridge runs. Kept
        // dependency-free vanilla JS and resilient to cross-origin failures (falls
        // back to the OS preference). Changing this changes the shape of the
        // generated HTML, so bump CacheFormatVersion too.
        private const string ThemeBridgeHeadScript =
            "<script>(function(){try{var p=window.parent;" +
            "var d=(p&&p!==window)?p.document.documentElement.classList.contains('dark'):" +
            "(window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches);" +
            "var e=document.documentElement;e.classList.toggle('tss-dark-mode',!!d);" +
            "e.style.background=d?'" + DarkBackground + "':'" + LightBackground + "';" +
            "e.style.colorScheme=d?'dark':'light';}catch(e){}})();</script>";

        // Runs at the end of <body> once <body> exists. Tesserae's own runtime
        // detects dark mode via `tss-dark-mode` on <body> (UI.Theme.IsDarkMode),
        // so the class must live there too — not only on <html>. This also wires
        // up the postMessage listener so later theme toggles on the docs page are
        // mirrored into the iframe (see RenderThemeSwitchScript). Keeping <html>
        // in sync as well preserves the cascading background set in the head.
        private const string ThemeBridgeScript =
            "<script>(function(){" +
            "function apply(d){try{document.body.classList.toggle('tss-dark-mode',!!d);" +
            "var e=document.documentElement;e.classList.toggle('tss-dark-mode',!!d);" +
            "e.style.background=d?'" + DarkBackground + "':'" + LightBackground + "';" +
            "e.style.colorScheme=d?'dark':'light';}catch(e){}}" +
            "function parentDark(){try{return window.parent&&window.parent!==window&&" +
            "window.parent.document.documentElement.classList.contains('dark');}" +
            "catch(e){return window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches;}}" +
            "apply(parentDark());" +
            "window.addEventListener('message',function(ev){" +
            "if(ev&&ev.data&&ev.data.type==='neko-theme')apply(!!ev.data.dark);});" +
            "})();</script>";

        // Default viewport width (CSS px) used when measuring sample heights. Chosen
        // to approximate the live-preview iframe width inside the docs content column
        // (`max-w-4xl` minus the preview box's padding).
        private const int DefaultMeasureWidth = 820;

        // Viewport height used while measuring. A full-page capture returns
        // max(contentHeight, viewportHeight), so this must be small to recover the
        // true natural height of short samples — it doubles as a sensible minimum
        // iframe height. Kept above zero to avoid collapsing the rare sample that
        // sizes itself relative to the viewport.
        private const int MeasureViewportHeight = 40;

        // Build configuration, set from neko.yml before the build runs.
        private static string _pinnedTesseraeVersion;
        private static int _maxParallelism = Environment.ProcessorCount;

        // Viewport width used by the `gen-tesserae-heights` command when measuring.
        // Normal builds never measure, so this is only consulted by that command.
        private static int _measureWidth = DefaultMeasureWidth;

        public static int MaxParallelism => _maxParallelism;

        // `neko watch --no-tesserae`: skip compiling and running live samples for the
        // session. WarmAsync and CompileAsync become no-ops, and the renderer falls
        // back to a plain, syntax-highlighted C# block (its normal behaviour when a
        // sample can't be compiled).
        public static bool Disabled { get; set; }

        // Headless height measurement spins up a Chromium page per unique sample.
        // Cap how many run at once so a high compile parallelism doesn't open a
        // browser tab per core; the Transpose compilation stays at _maxParallelism.
        private static readonly SemaphoreSlim _measureLock = new SemaphoreSlim(Math.Max(1, Math.Min(4, Environment.ProcessorCount)));

        // Whether the snapframe toolchain is usable, resolved once on first measure.
        private static bool? _measureToolAvailable;
        private static readonly object _measureToolLock = new object();

        // Apply project configuration. `pinnedVersion` empty => resolve latest;
        // `maxParallelism` <= 0 => Environment.ProcessorCount; `measureWidth` <= 0
        // => DefaultMeasureWidth (only used by `gen-tesserae-heights`).
        public static void Configure(string pinnedVersion, int maxParallelism, int measureWidth = 0)
        {
            _pinnedTesseraeVersion = string.IsNullOrWhiteSpace(pinnedVersion) ? null : pinnedVersion.Trim();
            _maxParallelism = maxParallelism > 0 ? maxParallelism : Environment.ProcessorCount;
            _measureWidth = measureWidth > 0 ? measureWidth : DefaultMeasureWidth;
        }

        // Root for all on-disk Tesserae build artifacts (compiled samples, shared
        // runtime, version record). Set once per invocation to the project's
        // `.neko-cache` folder so nothing is written to the OS temp directory.
        private static string _cacheRoot;

        public static void SetCacheRoot(string cacheRoot)
        {
            if (!string.IsNullOrWhiteSpace(cacheRoot)) _cacheRoot = cacheRoot;
        }

        public static string ComputeHash(string input)
        {
            return input.Hash128().ToString();
        }

        // Split a sample's raw lines into the source that is compiled/run and the
        // (optional) source shown in the Code tab.
        //
        // By default the whole block is both compiled and displayed. When a sample
        // can't run as-is in the sandboxed preview iframe, wrap the version to *show*
        // in an `// <overwrite-sample-code>` … `// </overwrite-sample-code>` region:
        // that region is displayed verbatim and never compiled, while everything
        // outside it is what compiles and runs (and is itself not shown).
        //
        // Returns the compiled source plus, when an overwrite region is present, the
        // exact lines to display instead (otherwise null — display the block as-is).
        // A null line entry represents a blank line and is never emitted into the
        // compiled source. Both the cache-warming and render passes call this, so the
        // compiled source (and cache key) is identical in both.
        public static (string Compiled, List<string>? DisplayOverride) PartitionSampleSource(IReadOnlyList<string> lines)
        {
            var compiled = new StringBuilder();
            var overrideLines = new List<string>();
            var inOverride = false;
            var hasOverride = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var lineText = lines[i];
                var trimmed = (lineText ?? string.Empty).Trim();

                var isStart = trimmed.Equals("//<overwrite-sample-code>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// <overwrite-sample-code>", StringComparison.OrdinalIgnoreCase);
                var isEnd = trimmed.Equals("//</overwrite-sample-code>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// </overwrite-sample-code>", StringComparison.OrdinalIgnoreCase);

                if (isStart) { inOverride = true; hasOverride = true; continue; }
                if (isEnd) { inOverride = false; continue; }

                if (inOverride)
                {
                    // Display-only: shown in the Code tab, never compiled.
                    overrideLines.Add(lineText ?? string.Empty);
                }
                else if (lineText != null)
                {
                    // Compiled and run; only shown when there is no overwrite region.
                    compiled.AppendLine(lineText);
                }
            }

            return (compiled.ToString(), hasOverride ? overrideLines : null);
        }

        private static string GetCacheDir()
        {
            // `_cacheRoot` is always set by the CLI entry points before a build.
            // The temp fallback only applies to direct unit-test calls that skip
            // SetCacheRoot.
            var root = _cacheRoot ?? Path.Combine(Path.GetTempPath(), "neko", ".neko-cache");
            var cacheDir = Path.Combine(root, "tesserae");
            Directory.CreateDirectory(cacheDir);
            return cacheDir;
        }

        private static string GetCacheFilePath(string hash, NuGetVersion tesseraeVersion)
        {
            return Path.Combine(GetCacheDir(), $"{hash}_{tesseraeVersion}.json");
        }

        // The compiled shared runtime (tps.js, tss.js, the Tesserae stylesheet and
        // its fonts, …) is identical for every sample built against the same
        // Tesserae version, so it is stored once per version rather than once per
        // sample. See EnsureSharedRuntimeAsync for how it is produced.
        private static string GetSharedAssetsDir(NuGetVersion tesseraeVersion)
        {
            return Path.Combine(GetCacheDir(), $"shared_{tesseraeVersion}");
        }

        // The order in which the shared runtime files must be loaded, recorded next
        // to (not inside) the shared assets directory so RestoreSharedAssets never
        // copies it into the site output.
        private static string GetSharedLayoutPath(NuGetVersion tesseraeVersion)
        {
            return Path.Combine(GetCacheDir(), $"layout_{tesseraeVersion}.json");
        }

        private const string AssetUrlPrefix = "/assets/tesserae/";

        // Asset URLs in a result are rooted at "/assets/tesserae/"; map one back to
        // its path relative to the tesserae assets directory.
        private static string AssetRelativePath(string assetUrl)
        {
            var p = (assetUrl ?? string.Empty).Replace('\\', '/');
            if (p.StartsWith(AssetUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                p = p.Substring(AssetUrlPrefix.Length);
            }
            return p.TrimStart('/');
        }

        // ---- the shared runtime (built once per Tesserae version) ----

        /// <summary>
        /// The runtime files every sample of one Tesserae version loads, and the order
        /// they load in. Produced by a full Transpose site build of a scaffold project
        /// (see <see cref="BuildSharedRuntime"/>) and persisted next to the shared
        /// assets, so later builds — and later `neko` runs — reuse both.
        /// </summary>
        internal sealed class SharedRuntimeLayout
        {
            // Stylesheets, in the order the generated index.html links them.
            public List<string> Css { get; set; } = new List<string>();

            // Scripts, in the order the generated index.html loads them. The sample's
            // own bundle is not among them — it is inlined into the preview document.
            public List<string> Js { get; set; } = new List<string>();

            // Every shared file, including the ones nothing links directly (the icon
            // fonts the stylesheet pulls in).
            public List<string> Assets { get; set; } = new List<string>();
        }

        // Resolved layouts by Tesserae version, so a warm process answers without
        // touching disk.
        private static readonly ConcurrentDictionary<string, SharedRuntimeLayout> _sharedLayouts =
            new ConcurrentDictionary<string, SharedRuntimeLayout>();

        // One shared runtime build at a time: parallel sample compiles all need the
        // same one, and it writes a single directory.
        private static readonly SemaphoreSlim _sharedRuntimeLock = new SemaphoreSlim(1, 1);

        // The scaffold whose site build produces the shared runtime. Nothing about the
        // sample matters — the runtime comes from the referenced packages — but it has
        // to be a valid Tesserae app so the build succeeds and index.html is generated.
        private const string SharedRuntimeSource = @"using Tesserae;
using static Tesserae.UI;

public static class NekoSharedRuntime
{
    public static void Main()
    {
        MountToBody(TextBlock(""Neko""));
    }
}
";

        /// <summary>
        /// Makes sure the shared runtime for <paramref name="tesseraeVersion"/> exists in the
        /// per-version cache and has been copied into <paramref name="siteAssetsDir"/>, and
        /// returns its load order. Builds it on first use; every later call is a lookup.
        /// </summary>
        private static async Task<SharedRuntimeLayout> EnsureSharedRuntimeAsync(NuGetVersion tesseraeVersion, string siteAssetsDir)
        {
            var key = tesseraeVersion.ToString();

            if (_sharedLayouts.TryGetValue(key, out var known) && RestoreSharedAssets(tesseraeVersion, siteAssetsDir))
            {
                return known;
            }

            await _sharedRuntimeLock.WaitAsync();
            try
            {
                // Another sample may have built it while we waited.
                if (_sharedLayouts.TryGetValue(key, out known) && RestoreSharedAssets(tesseraeVersion, siteAssetsDir))
                {
                    return known;
                }

                // A previous run's shared build, still on disk: reuse it rather than
                // paying for a site build on every `neko build`.
                var layoutPath = GetSharedLayoutPath(tesseraeVersion);
                if (File.Exists(layoutPath))
                {
                    try
                    {
                        var stored = JsonSerializer.Deserialize<SharedRuntimeLayout>(await File.ReadAllTextAsync(layoutPath));
                        if (stored != null && stored.Js.Count > 0 && RestoreSharedAssets(tesseraeVersion, siteAssetsDir))
                        {
                            _sharedLayouts[key] = stored;
                            return stored;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to read the cached Tesserae runtime layout: {ex.Message}");
                    }
                }

                var layout = await Task.Run(() => BuildSharedRuntime(tesseraeVersion));
                _sharedLayouts[key] = layout;
                RestoreSharedAssets(tesseraeVersion, siteAssetsDir);
                return layout;
            }
            finally
            {
                _sharedRuntimeLock.Release();
            }
        }

        /// <summary>
        /// Builds the shared runtime: writes a scaffold Tesserae project into the cache,
        /// runs a full Transpose site build of it, then keeps everything the build
        /// emitted except the scaffold's own bundle and the generated page — whose
        /// script/link order is read out first and recorded as the layout.
        /// </summary>
        private static SharedRuntimeLayout BuildSharedRuntime(NuGetVersion tesseraeVersion)
        {
            Console.WriteLine($"Building the shared Tesserae {tesseraeVersion} runtime (once per version)...");
            var sw = Stopwatch.StartNew();

            var scaffoldDir = Path.Combine(GetCacheDir(), $"scaffold_{tesseraeVersion}");
            var sharedDir = GetSharedAssetsDir(tesseraeVersion);

            // Start from a clean slate in both: a half-written directory from an
            // interrupted run must not be mistaken for a usable runtime.
            TryDeleteDirectory(scaffoldDir);
            TryDeleteDirectory(sharedDir);
            Directory.CreateDirectory(scaffoldDir);

            File.WriteAllText(Path.Combine(scaffoldDir, "App.csproj"), $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AssemblyName>App</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Tesserae"" Version=""{tesseraeVersion}"" />
  </ItemGroup>
</Project>
");
            // `outputFormatting: Formatted` keeps the runtime readable, which is what
            // a live sample in the docs wants when something goes wrong in it.
            File.WriteAllText(Path.Combine(scaffoldDir, "tps.json"),
                @"{ ""fileName"": ""app.js"", ""outputFormatting"": ""Formatted"" }");
            File.WriteAllText(Path.Combine(scaffoldDir, "App.cs"), SharedRuntimeSource);

            var build = TransposeCompilerLibrary.BuildProject(new ProjectBuildRequest(Path.Combine(scaffoldDir, "App.csproj"))
            {
                Configuration = "Release",
                SiteDirectory = sharedDir,
                Quiet = true,
            });

            if (!build.Success)
            {
                throw new Exception("Failed to build the shared Tesserae runtime: " +
                                    (build.Errors.Count > 0
                                        ? string.Join(Environment.NewLine, build.Errors)
                                        : string.Join(Environment.NewLine, build.Output)));
            }

            var indexPath = Path.Combine(sharedDir, "index.html");
            if (!File.Exists(indexPath))
            {
                throw new Exception("The shared Tesserae runtime build produced no index.html to read the load order from.");
            }

            var layout = ReadLayout(File.ReadAllText(indexPath));

            // Drop the scaffold's own output. The bundle is per-sample (keeping it
            // would make every preview on a page load the scaffold's `App` class and
            // then collide with the sample's inline copy), and neither the generated
            // page nor the build manifest is an asset a preview loads.
            foreach (var file in Directory.GetFiles(sharedDir, "*", SearchOption.AllDirectories))
            {
                if (IsScaffoldOutput(Path.GetRelativePath(sharedDir, file)))
                {
                    try { File.Delete(file); } catch { /* the layout simply won't reference it */ }
                }
            }

            layout.Assets = Directory
                .GetFiles(sharedDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(sharedDir, f).Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            File.WriteAllText(GetSharedLayoutPath(tesseraeVersion), JsonSerializer.Serialize(layout));

            Console.WriteLine($"Built the shared Tesserae {tesseraeVersion} runtime in {sw.Elapsed.TotalSeconds:n1}s ({layout.Assets.Count} file(s))");
            return layout;
        }

        // Output that belongs to the scaffold rather than to the shared runtime.
        private static bool IsScaffoldOutput(string relativePath)
        {
            var name = Path.GetFileName(relativePath).Replace('\\', '/');

            if (name.StartsWith(".tps-manifest.", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("index.html", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("index.min.html", StringComparison.OrdinalIgnoreCase)) return true;

            // app.js / app.min.js / app.meta.js / app.min.meta.js — the scaffold bundle
            // in any of the variants a build can emit.
            var normalized = name.Replace(".min.", ".");
            return normalized.Equals("app.js", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("app.meta.js", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly System.Text.RegularExpressions.Regex _stylesheetHref = new System.Text.RegularExpressions.Regex(
            "<link\\b[^>]*rel\\s*=\\s*[\"']stylesheet[\"'][^>]*href\\s*=\\s*[\"']([^\"']+)[\"']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static readonly System.Text.RegularExpressions.Regex _scriptSrc = new System.Text.RegularExpressions.Regex(
            "<script\\b[^>]*src\\s*=\\s*[\"']([^\"']+)[\"']",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        // Read the generated index.html's own load order. Taking it from the compiler
        // rather than re-deriving it here means a new runtime file, or a change in
        // which file has to come first, needs no change in Neko.
        internal static SharedRuntimeLayout ReadLayout(string indexHtml)
        {
            var layout = new SharedRuntimeLayout();

            foreach (System.Text.RegularExpressions.Match m in _stylesheetHref.Matches(indexHtml ?? string.Empty))
            {
                var href = m.Groups[1].Value.Replace('\\', '/').TrimStart('/');
                if (href.Length > 0 && !IsScaffoldOutput(href)) layout.Css.Add(href);
            }

            foreach (System.Text.RegularExpressions.Match m in _scriptSrc.Matches(indexHtml ?? string.Empty))
            {
                var src = m.Groups[1].Value.Replace('\\', '/').TrimStart('/');
                if (src.Length > 0 && !IsScaffoldOutput(src)) layout.Js.Add(src);
            }

            return layout;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not clear '{path}': {ex.Message}");
            }
        }

        // Ensure a cached result's shared runtime files exist in the output
        // directory, copying them from the per-version cache. Returns false (forcing
        // a recompile) if the cache has no shared assets for this version yet.
        // Write-if-absent makes it idempotent and self-healing across rebuilds.
        private static bool RestoreSharedAssets(NuGetVersion tesseraeVersion, string siteAssetsDir)
        {
            var sharedDir = GetSharedAssetsDir(tesseraeVersion);
            if (!Directory.Exists(sharedDir)) return false;

            var files = Directory.GetFiles(sharedDir, "*", SearchOption.AllDirectories);
            if (files.Length == 0) return false;

            lock (_assetWriteLock)
            {
                foreach (var source in files)
                {
                    var rel = Path.GetRelativePath(sharedDir, source);
                    var dest = Path.Combine(siteAssetsDir, rel);
                    if (File.Exists(dest)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(source, dest, overwrite: true);
                }
            }

            return true;
        }

        // ---- version resolution (recorded on disk, no expiry) ----

        // Deliberately not the historical `versions.json`: that file may still record
        // an H5-era Tesserae release from a cache written before Neko moved to
        // Transpose, and reusing it would pin the build to a package that no longer
        // compiles. A fresh file name lets the version be resolved again.
        private static string GetVersionsFilePath() => Path.Combine(GetCacheDir(), "versions.tps.json");

        private static Dictionary<string, string> LoadVersionsFile()
        {
            try
            {
                var path = GetVersionsFilePath();
                if (!File.Exists(path)) return new Dictionary<string, string>();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static void SaveVersionEntry(string package, NuGetVersion version)
        {
            try
            {
                var map = LoadVersionsFile();
                map[package] = version.ToString();
                File.WriteAllText(GetVersionsFilePath(), JsonSerializer.Serialize(map));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to persist version record: {ex.Message}");
            }
        }

        // Resolve the Tesserae version for the cache key: an explicit neko.yml pin
        // when present, otherwise the version recorded on disk.
        private static async Task<NuGetVersion> ResolveTesseraeVersionAsync()
        {
            if (_pinnedTesseraeVersion != null && NuGetVersion.TryParse(_pinnedTesseraeVersion, out var pinned))
            {
                await EnsurePackageRestored(pinned, "Tesserae");
                EnsureTransposeCompatible(pinned);
                return pinned;
            }
            var latest = await GetLatestVersionAsync("Tesserae");
            EnsureTransposeCompatible(latest);
            return latest;
        }

        // Tesserae releases up to mid-2026 were built for the H5 compiler and bind
        // against `h5`/`h5.core`; Neko now compiles samples with Transpose, which
        // cannot bind them. Such a package fails with a wall of CS0234s about
        // missing `Tesserae`/`H5` namespaces, so check the restored package's own
        // dependency list up front and say what is actually wrong instead.
        private static void EnsureTransposeCompatible(NuGetVersion version)
        {
            var nuspec = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nuget", "packages", "tesserae", version.ToString(), "tesserae.nuspec");

            // Only reject a package we can actually read and that demonstrably
            // targets H5 — an unreadable or unusually-shaped nuspec is not evidence.
            string nuspecText;
            try
            {
                if (!File.Exists(nuspec)) return;
                nuspecText = File.ReadAllText(nuspec);
            }
            catch
            {
                return;
            }

            if (nuspecText.Contains("id=\"Transpose", StringComparison.OrdinalIgnoreCase)) return;
            if (!nuspecText.Contains("id=\"h5", StringComparison.OrdinalIgnoreCase)) return;

            throw new Exception(
                $"Tesserae {version} is built for the H5 compiler, which Neko no longer uses. " +
                "Remove the `tesserae.version` pin from neko.yml (or raise it to a Transpose-based " +
                "release, 2026.7 or newer) so live samples can be compiled with Transpose.");
        }

        private static async Task<NuGetVersion> GetLatestVersionAsync(string package)
        {
            if (_cachedLatestVersion.TryGetValue(package, out var cachedVersion))
            {
                return cachedVersion;
            }

            await _versionLock.WaitAsync();
            try
            {
                // Another thread may have resolved it while we waited.
                if (_cachedLatestVersion.TryGetValue(package, out cachedVersion))
                {
                    return cachedVersion;
                }

                // The resolved version is recorded on disk with no expiry. Once a
                // version is known, it is reused verbatim — so the sample cache key
                // stays identical across rebuilds and `neko watch` restarts, and a
                // later upstream release never silently invalidates the cache. The
                // on-disk record (not process memory) is the source of truth; the
                // in-memory map is only a within-process read-through. Delete the
                // cache directory or pin `tesserae.version` to move to a new version.
                var diskMap = LoadVersionsFile();
                if (diskMap.TryGetValue(package, out var recorded) && NuGetVersion.TryParse(recorded, out var recordedVersion))
                {
                    _cachedLatestVersion[package] = recordedVersion;
                    await EnsurePackageRestored(recordedVersion, package);
                    return recordedVersion;
                }

                Console.WriteLine($"Checking latest version for {package}");

                try
                {
                    var json = await _httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{package.ToLower()}/index.json");
                    var versions = new List<NuGetVersion>();

                    using (var doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("versions", out var versionsProp) && versionsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var v in versionsProp.EnumerateArray())
                            {
                                if (v.ValueKind == JsonValueKind.String)
                                {
                                    var versionString = v.GetString();
                                    if (!string.IsNullOrEmpty(versionString) && NuGetVersion.TryParse(versionString, out var candidateVersion))
                                    {
                                        if (!candidateVersion.IsPrerelease)
                                        {
                                            versions.Add(candidateVersion);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (versions.Count == 0)
                    {
                        throw new Exception($"No stable versions found for {package} package.");
                    }

                    var version = _cachedLatestVersion[package] = versions.Max();
                    Console.WriteLine($"Resolved {package} version: {version} (recorded for future builds)");

                    SaveVersionEntry(package, version);
                    await EnsurePackageRestored(version, package);

                    return version;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to fetch or restore latest {package} version.", ex);
                }
            }
            finally
            {
                _versionLock.Release();
            }
        }

        private static async Task EnsurePackageRestored(NuGetVersion version, string package)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var packagePath = Path.Combine(userProfile, ".nuget", "packages", package.ToLower(), version.ToString());

            if (Directory.Exists(packagePath))
            {
                return;
            }

            Console.WriteLine($"Package {package} version {version} not found in cache. Restoring...");

            var tempDir = Path.Combine(GetCacheDir(), "restore-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var csprojContent = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""{package}"" Version=""{version}"" />
  </ItemGroup>
</Project>";

                await File.WriteAllTextAsync(Path.Combine(tempDir, "Restore.csproj"), csprojContent);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) throw new Exception("Failed to start dotnet restore process.");

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"dotnet restore failed with exit code {process.ExitCode}.\nOutput: {output}\nError: {error}");
                }

                Console.WriteLine($"Successfully restored {package} version {version}.");
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        // Compile every supplied sample into the cache up front, in parallel, before
        // the (synchronous, sequential) page-render pass turns them into cache hits.
        // Identical samples are compiled only once.
        public static async Task WarmAsync(IReadOnlyList<(string Arguments, string Code)> samples, string siteOutputRoot)
        {
            if (Disabled) return;
            if (samples == null || samples.Count == 0) return;

            var routePrefix = SiteBuilder.CurrentRoutePrefix ?? string.Empty;
            var seen = new HashSet<string>();
            var distinct = new List<(string Arguments, string Code)>();
            foreach (var s in samples)
            {
                if (seen.Add(ComputeHash(s.Code + " " + routePrefix + " " + CacheFormatVersion)))
                {
                    distinct.Add(s);
                }
            }

            Console.WriteLine($"Warming {distinct.Count} Tesserae sample(s) using up to {_maxParallelism} parallel compile(s)...");
            var sw = Stopwatch.StartNew();

            await Parallel.ForEachAsync(
                distinct,
                new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism },
                async (sample, ct) =>
                {
                    try
                    {
                        await CompileAsync(sample.Arguments, sample.Code, siteOutputRoot);
                    }
                    catch (Exception ex)
                    {
                        // Never let one bad sample abort the warm pass; the failure is
                        // surfaced again (and rendered as an error block) at render time.
                        Console.WriteLine($"Warm compile failed for {sample.Arguments}: {ex.Message}");
                    }
                });

            Console.WriteLine($"Warmed Tesserae samples in {sw.Elapsed.TotalSeconds:n1}s");
        }

        public static async Task<TesseraeCompilerResult> CompileAsync(string codeBlockArguments, string csharpCode, string siteOutputRoot)
        {
            // --no-tesserae: no compile, no live preview. Returning null is the same
            // signal the renderer already handles for an unavailable toolchain, so the
            // sample renders as a static C# block.
            if (Disabled) return null;

            var siteAssetsDir = Path.Combine(siteOutputRoot, "assets", "tesserae");
            Directory.CreateDirectory(siteAssetsDir);

            // The compiled HTML bakes in the route prefix (asset <script>/<link>
            // hrefs), so it is part of the cache key — otherwise a multi-site build
            // could serve one sub-site's prefix to another.
            var hash = ComputeHash(csharpCode + " " + (SiteBuilder.CurrentRoutePrefix ?? string.Empty) + " " + CacheFormatVersion);
            var tesseraeVersion = await ResolveTesseraeVersionAsync();
            var cacheKey = $"{hash}_{tesseraeVersion}";
            var cacheFilePath = GetCacheFilePath(hash, tesseraeVersion);

            // In-memory hit: served without touching disk. Still make sure the shared
            // runtime exists in the (possibly freshly wiped) output directory.
            if (_memCache.TryGetValue(cacheKey, out var memResult))
            {
                if (string.IsNullOrEmpty(memResult.OutputHtml) || RestoreSharedAssets(tesseraeVersion, siteAssetsDir))
                {
                    return memResult;
                }
            }

            // On-disk hit: reuse the manifest and restore the shared runtime files
            // into the output directory. Restoring here is what lets the cache
            // survive across builds, which start by wiping the output directory.
            if (File.Exists(cacheFilePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(cacheFilePath);
                    var cachedResult = JsonSerializer.Deserialize<TesseraeCompilerResult>(json);
                    if (cachedResult != null && (cachedResult.AssetsPath.Count == 0 || RestoreSharedAssets(tesseraeVersion, siteAssetsDir)))
                    {
                        Console.WriteLine($"Using cached Tesserae code for {codeBlockArguments}");
                        _memCache[cacheKey] = cachedResult;
                        return cachedResult;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to read cache file: {ex.Message}");
                }
            }

            Console.WriteLine($"Compiling Tesserae code for {codeBlockArguments}");

            var sw = Stopwatch.StartNew();

            var result = new TesseraeCompilerResult();
            var compiled = false;

            try
            {
                // The runtime every sample shares (tps.js, the Tesserae bundles, its
                // stylesheet and fonts) is produced once per Tesserae version by a
                // full site build, and restored into the output directory from there.
                // Only the sample's own JavaScript is compiled per sample, in memory.
                var layout = await EnsureSharedRuntimeAsync(tesseraeVersion, siteAssetsDir);

                var request = new Transpose.Compiler.Library.CompilationRequest("App")
                                .WithPackageReference("Tesserae", tesseraeVersion.ToString())
                                // Inline, so the sample is a single self-contained script
                                // that can be embedded in the preview document — a separate
                                // `app.meta.js` would have to be served per sample, which
                                // is exactly what the shared assets directory cannot hold.
                                .WithMetadataTarget(Transpose.Translator.MetadataTarget.Inline)
                                .WithSourceFile("App.cs", csharpCode);

                var compilation = await TransposeCompilerLibrary.CompileAsync(request);

                if (!compilation.Success || string.IsNullOrEmpty(compilation.Javascript))
                {
                    throw new Exception(compilation.Errors.Count > 0
                        ? string.Join(Environment.NewLine, compilation.Errors)
                        : "Transpose compilation produced no output.");
                }

                result.AppJsContent = compilation.Javascript;

                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<!DOCTYPE html>");
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head>");
                htmlBuilder.AppendLine("<meta charset=\"utf-8\" />");
                htmlBuilder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
                // Paint the docs page's light/dark surface colour before the
                // Tesserae stylesheet loads, so the preview never flashes white
                // while the sample is still compiling/booting.
                htmlBuilder.AppendLine(ThemeBridgeHeadScript);

                // Every file the shared build produced is part of the runtime this
                // sample loads; record them all so a cached result knows the shared
                // assets have to be restored before it can be served.
                foreach (var asset in layout.Assets)
                {
                    result.AssetsPath.Add(AssetUrlPrefix + asset);
                }

                // The load order is the compiler's own — taken from the index.html it
                // generated for the shared build — rather than a hand-maintained rule
                // about which runtime file comes first. The scripts are emitted
                // without `defer` so they run during parsing, before the inline app
                // script at the end of <body>.
                foreach (var css in layout.Css)
                {
                    htmlBuilder.AppendLine($"<link rel=\"stylesheet\" href=\"{SiteBuilder.CurrentRoutePrefix}{AssetUrlPrefix}{css}\" />");
                }
                foreach (var js in layout.Js)
                {
                    htmlBuilder.AppendLine($"<script src=\"{SiteBuilder.CurrentRoutePrefix}{AssetUrlPrefix}{js}\"></script>");
                }

                htmlBuilder.AppendLine("</head>");
                htmlBuilder.AppendLine("<body>");
                // Make the live-preview follow the surrounding docs page's light/dark
                // mode. Tesserae's dark theme is the `tss-dark-mode` class on <body>;
                // the docs page marks dark mode with the `dark` class on <html>. This
                // is an `about:srcdoc` iframe, so it shares the parent's origin and can
                // read that class directly on load, then react to later toggles which
                // the page broadcasts via postMessage (see RenderThemeSwitchScript).
                htmlBuilder.AppendLine(ThemeBridgeScript);
                htmlBuilder.AppendLine($"<script>{EscapeForInlineScript(result.AppJsContent)}</script>");
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                result.OutputHtml = htmlBuilder.ToString();
                compiled = true;

                Console.WriteLine($"Compiled Tesserae code for {codeBlockArguments} in {sw.Elapsed.TotalSeconds:n1}s");

            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Failed to compile code for {codeBlockArguments}: {ex.Message}");

                // Transpose reports one diagnostic per line, so keep the line breaks
                // rather than running every error together into one paragraph.
                var message = System.Net.WebUtility.HtmlEncode(ex.Message)
                    .Replace("\r\n", "<br/>")
                    .Replace("\n", "<br/>");

                result = new Builder.TesseraeCompilerResult()
                {
                    OutputHtml = $"<div class=\"text-red-500 font-bold p-4 border border-red-500 rounded my-4\">Tesserae compilation failed:<br/>{message}</div>"
                };
            }

            // Only persist successful compiles. Caching a failure (e.g. a transient
            // network or restore error) would otherwise serve the error placeholder
            // forever. The shared asset files were already written to the per-version
            // cache above, so the manifest just records the result.
            if (compiled)
            {
                try
                {
                    var json = JsonSerializer.Serialize(result);
                    await File.WriteAllTextAsync(cacheFilePath, json);
                    _memCache[cacheKey] = result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write cache file: {ex.Message}");
                }
            }

            return result;
        }

        // ---- headless height measurement ----

        // Render the compiled sample HTML in a headless browser and return its
        // content height in CSS px, or 0 when measurement is unavailable or fails.
        // Best-effort: any failure leaves the caller to fall back to a placeholder
        // height. `siteOutputRoot` is the build output root whose
        // `assets/tesserae/` folder holds the runtime the sample references.
        // Invoked only by the `gen-tesserae-heights` command — normal builds never
        // measure.
        public static async Task<int> MeasureHeightAsync(string outputHtml, string siteOutputRoot, string label)
        {
            if (string.IsNullOrEmpty(outputHtml)) return 0;
            if (!EnsureMeasureToolAvailable()) return 0;

            var siteAssetsDir = Path.Combine(siteOutputRoot, "assets", "tesserae");

            await _measureLock.WaitAsync();
            try
            {
                return await Task.Run(() => MeasureHeightCore(outputHtml, siteAssetsDir, label));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tesserae] Height measurement failed for {label}: {ex.Message}");
                return 0;
            }
            finally
            {
                _measureLock.Release();
            }
        }

        private static bool EnsureMeasureToolAvailable()
        {
            if (_measureToolAvailable.HasValue) return _measureToolAvailable.Value;
            lock (_measureToolLock)
            {
                if (_measureToolAvailable.HasValue) return _measureToolAvailable.Value;
                bool ok;
                try
                {
                    ok = Neko.Extensions.SnapFrameExtension.EnsureToolInstalled();
                }
                catch
                {
                    ok = false;
                }
                if (!ok)
                {
                    Console.WriteLine("[Tesserae] snapframe unavailable; skipping height measurement (live-preview iframes fall back to a placeholder height).");
                }
                _measureToolAvailable = ok;
                return ok;
            }
        }

        private static int MeasureHeightCore(string outputHtml, string siteAssetsDir, string label)
        {
            // The compiled HTML references runtime assets with site-root-absolute URLs
            // (`/assets/tesserae/...`, optionally route-prefixed). Those don't resolve
            // from a file:// page, so rewrite them to absolute file URIs pointing at
            // the assets already written to the output directory.
            var assetsBaseUri = new Uri(siteAssetsDir.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? siteAssetsDir
                : siteAssetsDir + Path.DirectorySeparatorChar).AbsoluteUri;

            var routePrefix = SiteBuilder.CurrentRoutePrefix ?? string.Empty;
            var measureHtml = outputHtml;
            if (!string.IsNullOrEmpty(routePrefix))
            {
                measureHtml = measureHtml.Replace(routePrefix + AssetUrlPrefix, assetsBaseUri);
            }
            measureHtml = measureHtml.Replace(AssetUrlPrefix, assetsBaseUri);

            var tmpDir = Path.Combine(GetCacheDir(), "measure");
            Directory.CreateDirectory(tmpDir);
            var token = Guid.NewGuid().ToString("N");
            var htmlPath = Path.Combine(tmpDir, token + ".html");
            var pngPath = Path.Combine(tmpDir, token + ".png");

            try
            {
                File.WriteAllText(htmlPath, measureHtml);
                var pageUrl = new Uri(htmlPath).AbsoluteUri;

                // The first navigate of a run cold-starts Chromium, and that launch
                // occasionally returns before a PageId is reported — which would drop
                // whichever sample happens to be measured first (no capture is even
                // attempted). Retry the navigate a few times so a cold start can't
                // silently skip a sample.
                string pageId = null;
                for (int navAttempt = 0; navAttempt < NavigateAttempts && string.IsNullOrEmpty(pageId); navAttempt++)
                {
                    if (navAttempt > 0) Thread.Sleep(NavigateRetryDelayMs);
                    pageId = SnapFrameNavigate(pageUrl, _measureWidth);
                }
                if (string.IsNullOrEmpty(pageId)) return 0;

                try
                {
                    // The Transpose runtime boots and mounts the app asynchronously, so a
                    // capture taken too early grabs a blank page — a full-page shot of
                    // which is just the (tiny) viewport, i.e. a bogus ~viewport-height
                    // reading. Retry with growing settle/capture delays, and reject a
                    // capture that is still blank or collapsed to the viewport height.
                    int[] settleMs = { 1200, 2500, 4000 };
                    int[] captureDelay = { 2, 3, 4 };
                    for (int attempt = 0; attempt < settleMs.Length; attempt++)
                    {
                        Thread.Sleep(settleMs[attempt]);
                        if (!SnapFrameCaptureFullPage(pageId, pngPath, captureDelay[attempt])) continue;
                        if (!TryReadPngSize(pngPath, out _, out var height) || height <= 0) continue;

                        var blank = Neko.Extensions.SnapFrameExtension.IsBlankImage(pngPath, out _);
                        if (blank || height <= MeasureViewportHeight)
                        {
                            // Not rendered yet (or collapsed) — give it another, longer pass.
                            continue;
                        }

                        Console.WriteLine($"[Tesserae] Measured height {height}px for {label}");
                        return height;
                    }

                    Console.WriteLine($"[Tesserae] Could not measure a rendered height for {label} (kept default).");
                    return 0;
                }
                finally
                {
                    SnapFrameClose(pageId);
                }
            }
            finally
            {
                try { if (File.Exists(htmlPath)) File.Delete(htmlPath); } catch { }
                try { if (File.Exists(pngPath)) File.Delete(pngPath); } catch { }
            }
        }

        // How many times to attempt the initial navigate before giving up on a
        // sample, and how long to wait between attempts. The first attempt of a run
        // races Chromium's cold start; the retries let the browser finish launching.
        private const int NavigateAttempts = 4;
        private const int NavigateRetryDelayMs = 1000;

        // Hard ceiling on any single snapframe invocation. A wedged browser
        // (e.g. Chromium failing to launch) must not hang the whole command — on
        // timeout the process is killed and the call reports failure, so the sample
        // is skipped and keeps its placeholder height.
        private const int SnapFrameTimeoutMs = 60_000;

        private static string RunSnapFrame(string arguments, out int exitCode, out string stdErr)
        {
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            var exeName = isWindows ? "snapframe.exe" : "snapframe";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(SnapFrameTimeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                stdErr = $"snapframe timed out after {SnapFrameTimeoutMs / 1000}s";
                exitCode = -1;
                return string.Empty;
            }

            // Ensure the async stdout/stderr reads complete after exit.
            try { Task.WaitAll(new Task[] { stdOutTask, stdErrTask }, 5_000); } catch { /* best-effort */ }
            stdErr = stdErrTask.IsCompletedSuccessfully ? stdErrTask.Result : string.Empty;
            exitCode = process.ExitCode;
            return stdOutTask.IsCompletedSuccessfully ? stdOutTask.Result : string.Empty;
        }

        private static string SnapFrameNavigate(string url, int width)
        {
            var output = RunSnapFrame($"navigate-json --size {width}x{MeasureViewportHeight} \"{url}\"", out var exit, out var err);
            if (exit != 0)
            {
                Console.WriteLine($"[Tesserae] snapframe navigate failed: {err}");
                return null;
            }
            var match = System.Text.RegularExpressions.Regex.Match(output, "\"PageId\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static bool SnapFrameCaptureFullPage(string pageId, string targetPath, int delaySeconds = 2)
        {
            RunSnapFrame($"capture {pageId} \"{targetPath}\" --full-page --delay {delaySeconds}", out var exit, out var err);
            if (exit != 0)
            {
                Console.WriteLine($"[Tesserae] snapframe capture failed: {err}");
                return false;
            }
            return File.Exists(targetPath);
        }

        private static void SnapFrameClose(string pageId)
        {
            try { RunSnapFrame($"close {pageId}", out _, out _); } catch { }
        }

        // Read a PNG's pixel dimensions straight from its IHDR chunk (bytes 16..24).
        private static bool TryReadPngSize(string path, out int width, out int height)
        {
            width = 0;
            height = 0;
            try
            {
                Span<byte> head = stackalloc byte[24];
                using var fs = File.OpenRead(path);
                int read = 0;
                while (read < head.Length)
                {
                    int n = fs.Read(head.Slice(read));
                    if (n <= 0) return false;
                    read += n;
                }
                // PNG signature + IHDR length/type precede the 8-byte width/height.
                if (head[0] != 0x89 || head[1] != 0x50 || head[2] != 0x4E || head[3] != 0x47) return false;
                width = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                height = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
