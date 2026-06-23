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
using H5.Compiler;
using H5.Compiler.Hosted;
using H5.Translator;
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

        // Parallel compiles emit the same shared runtime files (h5.js, css, …);
        // serialise the actual file writes so two threads never write one path at
        // once. The heavy H5 compilation itself stays parallel.
        private static readonly object _assetWriteLock = new object();

        // Bumped whenever the *shape* of a compiled result changes (the generated
        // OutputHtml, the asset file names it references, …). It is part of the cache
        // key so a stale `.neko-cache` written by an older neko isn't reused by a
        // newer one — otherwise the cached HTML can reference asset variants the new
        // build no longer writes (e.g. `h5.min.js` vs `h5.js`), 404ing the runtime
        // and leaving the live preview blank.
        private const string CacheFormatVersion = "4";

        // Injected into every compiled sample so the live-preview iframe mirrors the
        // docs page's light/dark mode. Kept dependency-free vanilla JS and resilient
        // to cross-origin failures (falls back to the OS preference). Changing this
        // changes the shape of the generated HTML, so bump CacheFormatVersion too.
        private const string ThemeBridgeScript =
            "<script>(function(){" +
            "function apply(d){try{document.body.classList.toggle('tss-dark-mode',!!d);" +
            "document.documentElement.style.colorScheme=d?'dark':'light';}catch(e){}}" +
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

        // Headless height measurement spins up a Chromium page per unique sample.
        // Cap how many run at once so a high compile parallelism doesn't open a
        // browser tab per core; the heavy H5 compilation stays at _maxParallelism.
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

        // Partition a sample's raw lines into the source that gets compiled and the
        // indices to hide from the displayed Code tab. Two marker regions are honoured:
        //   // <hide> … // </hide>  — compiled and run, hidden from the Code tab.
        //   // <docs> … // </docs>  — shown in the Code tab, but NOT compiled.
        // Marker lines themselves are dropped from both. A null entry represents a
        // blank source line and is never emitted into the compiled source.
        // Both the cache-warming pass and the render pass call this, so the compiled
        // source (and therefore the cache key) is identical in both.
        public static (string Compiled, List<int> HiddenDisplayIndices) PartitionSampleSource(IReadOnlyList<string> lines)
        {
            var sb = new StringBuilder();
            var hidden = new List<int>();
            var inHide = false;
            var inDocs = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var lineText = lines[i];
                var trimmed = (lineText ?? string.Empty).Trim();

                var isHideStart = trimmed.Equals("//<hide>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// <hide>", StringComparison.OrdinalIgnoreCase);
                var isHideEnd = trimmed.Equals("//</hide>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// </hide>", StringComparison.OrdinalIgnoreCase);
                var isDocsStart = trimmed.Equals("//<docs>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// <docs>", StringComparison.OrdinalIgnoreCase);
                var isDocsEnd = trimmed.Equals("//</docs>", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("// </docs>", StringComparison.OrdinalIgnoreCase);

                if (isHideStart) { inHide = true; hidden.Add(i); continue; }
                if (isHideEnd) { inHide = false; hidden.Add(i); continue; }
                if (isDocsStart) { inDocs = true; hidden.Add(i); continue; }
                if (isDocsEnd) { inDocs = false; hidden.Add(i); continue; }

                // Compiled source includes hidden regions but not docs-only regions.
                if (!inDocs && lineText != null) sb.AppendLine(lineText);

                // Displayed source includes docs-only regions but not hidden regions.
                if (inHide) hidden.Add(i);
            }

            return (sb.ToString(), hidden);
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

        // The compiled shared runtime (h5.js, h5.core.js, css, …) is identical for
        // every sample built against the same Tesserae version, so it is stored
        // once per version rather than once per sample.
        private static string GetSharedAssetsDir(NuGetVersion tesseraeVersion)
        {
            return Path.Combine(GetCacheDir(), $"shared_{tesseraeVersion}");
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

        // Write a shared asset to both the live output directory and the persistent
        // per-version cache, skipping files that already exist. Idempotent and safe
        // to call from parallel compiles (serialised on _assetWriteLock).
        private static void WriteSharedAsset(string relativePath, byte[] content, string siteAssetsDir, string sharedAssetsDir)
        {
            lock (_assetWriteLock)
            {
                foreach (var baseDir in new[] { siteAssetsDir, sharedAssetsDir })
                {
                    var dest = Path.Combine(baseDir, relativePath);
                    if (File.Exists(dest)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.WriteAllBytes(dest, content);
                }
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

        private static string GetVersionsFilePath() => Path.Combine(GetCacheDir(), "versions.json");

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
                return pinned;
            }
            return await GetLatestVersionAsync("Tesserae");
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

            var tempDir = Path.Combine(GetCacheDir(), "h5-restore-" + Guid.NewGuid());
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
            var siteAssetsDir = Path.Combine(siteOutputRoot, "assets", "tesserae");
            Directory.CreateDirectory(siteAssetsDir);

            // The compiled HTML bakes in the route prefix (asset <script>/<link>
            // hrefs), so it is part of the cache key — otherwise a multi-site build
            // could serve one sub-site's prefix to another.
            var hash = ComputeHash(csharpCode + " " + (SiteBuilder.CurrentRoutePrefix ?? string.Empty) + " " + CacheFormatVersion);
            var tesseraeVersion = await ResolveTesseraeVersionAsync();
            var cacheKey = $"{hash}_{tesseraeVersion}";
            var cacheFilePath = GetCacheFilePath(hash, tesseraeVersion);
            var sharedAssetsDir = GetSharedAssetsDir(tesseraeVersion);

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

            var h5Version = await GetLatestVersionAsync("h5");
            var h5TargetVersion = await GetLatestVersionAsync("h5.Target");
            var h5CoreVer = await GetLatestVersionAsync("h5.core");
            var h5JsonVer = await GetLatestVersionAsync("h5.Newtonsoft.Json");

            var settings = new H5DotJson_AssemblySettings()
            {
                Reflection = new ReflectionConfig()
                {
                    Disabled = false,
                    Target = H5.Contract.MetadataTarget.Inline,
                },
                IgnoreDuplicateTypes = true
            };

            var request = new CompilationRequest("App", settings)
                            //.NoHTML()
                            //.WithLanguageVersion("Latest")
                            .WithPackageReference("h5", h5Version)
                            .WithPackageReference("Tesserae", tesseraeVersion)
                            .WithPackageReference("h5.core", h5CoreVer)
                            .WithPackageReference("h5.Newtonsoft.Json", h5JsonVer)
                            .WithSourceFile("App.cs", csharpCode);

            var result = new TesseraeCompilerResult();
            var compiled = false;

            try
            {

                var compiledJavascript = await CompilationProcessor.CompileAsync(request);

                if (compiledJavascript.Output == null || !compiledJavascript.Output.Any())
                {
                    throw new Exception("H5 compilation failed or produced no output.");
                }

                // Read app.js
                var appJsFile = compiledJavascript.Output.FirstOrDefault(f => f.Key.Equals("app.js", StringComparison.OrdinalIgnoreCase) || f.Key.EndsWith("/app.js", StringComparison.OrdinalIgnoreCase) || f.Key.EndsWith("\\app.js", StringComparison.OrdinalIgnoreCase));


                if (appJsFile.Value != null)
                {
                    result.AppJsContent = CompilationOutput.GetAsText(appJsFile.Value);
                }
                else
                {
                    throw new Exception("Could not find compiled app.js");
                }

                // Collect other assets
                var htmlBuilder = new StringBuilder();
                htmlBuilder.AppendLine("<!DOCTYPE html>");
                htmlBuilder.AppendLine("<html>");
                htmlBuilder.AppendLine("<head>");
                htmlBuilder.AppendLine("<meta charset=\"utf-8\" />");
                htmlBuilder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");

                // We need to keep h5.js, h5.core.js first, then other js, then css
                var jsFiles = new List<string>();
                var cssFiles = new List<string>();

                // Collapse each runtime file's min/non-min pair to a single variant.
                // Order first so the kept representative is deterministic across builds
                // (".js" sorts before ".min.js"): otherwise the generated <script>/<link>
                // hrefs could pick a different variant than a previous build wrote to
                // disk, 404ing the runtime in the live preview.
                foreach (var file in compiledJavascript.Output
                             .OrderBy(v => v.Key, StringComparer.Ordinal)
                             .DistinctBy(v => v.Key.Replace(".min.", ".")))
                {
                    var fileName = Path.GetFileName(file.Key);

                    // Skip the per-sample app, both the readable `app.js` (inlined
                    // above as AppJsContent) and its minified `App.min.js` sibling.
                    // The app is unique to each sample, so it must never be written
                    // to the shared, per-version assets directory: doing so makes
                    // every sample on a page load whichever sample compiled last and
                    // then throw "Class 'App' is already defined" on the inline copy.
                    var normalizedName = fileName.Replace(".min.", ".");
                    if (normalizedName.Equals("app.js", StringComparison.OrdinalIgnoreCase)) continue;

                    var relativePath = file.Key.Replace("\\", "/");

                    // app.js aside, every emitted file is part of the shared runtime
                    // that is identical for all samples of this Tesserae version, so
                    // it is written once (per version) into both the output and the
                    // shared cache rather than duplicated per sample.
                    byte[] bytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.Value.CopyToAsync(ms);
                        bytes = ms.ToArray();
                    }
                    WriteSharedAsset(relativePath, bytes, siteAssetsDir, sharedAssetsDir);

                    var assetUrl = $"/assets/tesserae/{relativePath}";
                    result.AssetsPath.Add(assetUrl);

                    if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        jsFiles.Add(assetUrl);
                    }
                    else if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                    {
                        cssFiles.Add(assetUrl);
                    }
                }

                // Sort JS: h5.js first, h5.core.js second, others
                jsFiles = jsFiles.OrderBy(f =>
                {
                    var name = Path.GetFileName(f).ToLowerInvariant();
                    if (name == "h5.js") return 0;
                    if (name == "h5.core.js") return 1;
                    if (name.StartsWith("h5.")) return 2;
                    return 10;
                }).ToList();

                foreach (var css in cssFiles)
                {
                    htmlBuilder.AppendLine($"<link rel=\"stylesheet\" href=\"{SiteBuilder.CurrentRoutePrefix}{css}\" />");
                }
                foreach (var js in jsFiles)
                {
                    htmlBuilder.AppendLine($"<script src=\"{SiteBuilder.CurrentRoutePrefix}{js}\"></script>");
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
                htmlBuilder.AppendLine($"<script>{result.AppJsContent}</script>");
                htmlBuilder.AppendLine("</body>");
                htmlBuilder.AppendLine("</html>");

                result.OutputHtml = htmlBuilder.ToString();
                compiled = true;

                Console.WriteLine($"Compiled Tesserae code for {codeBlockArguments} in {sw.Elapsed.TotalSeconds:n1}s");

            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Failed to compile code for {codeBlockArguments}: {ex.Message}");

                result = new Builder.TesseraeCompilerResult()
                {
                    OutputHtml = $"<div class=\"text-red-500 font-bold p-4 border border-red-500 rounded my-4\">Tesserae compilation failed:<br/>{System.Net.WebUtility.HtmlEncode(ex.Message)}</div>"
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

                var pageId = SnapFrameNavigate(pageUrl, _measureWidth);
                if (string.IsNullOrEmpty(pageId)) return 0;

                try
                {
                    // The H5 runtime compiles and mounts the app asynchronously, so a
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
