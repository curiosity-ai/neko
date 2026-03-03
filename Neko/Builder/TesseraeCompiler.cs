using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private static readonly Dictionary<UID128, TesseraeCompilerResult> _cache = new();
        private static readonly HttpClient _httpClient = new HttpClient();
        private static Dictionary<string, NuGetVersion> _cachedLatestVersion = new Dictionary<string, NuGetVersion>();

        public static UID128 ComputeHash(string input)
        {
            return input.Hash128();
        }

        private static async Task<NuGetVersion> GetLatestVersionAsync(string package)
        {
            if (_cachedLatestVersion.TryGetValue(package, out var cachedVersion))
            {
                return cachedVersion;
            }

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
                Console.WriteLine($"Resolved latest {package} version: {version}");

                await EnsurePackageRestored(version, package);

                return version;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to fetch or restore latest {package} version.", ex);
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

            var tempDir = Path.Combine(Path.GetTempPath(), "H5_Restore_" + Guid.NewGuid());
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

        public static async Task<TesseraeCompilerResult> CompileAsync(string csharpCode, string siteOutputRoot)
        {
            var siteAssetsDir = Path.Combine(siteOutputRoot, "assets", "tesserae");
            Directory.CreateDirectory(siteAssetsDir);

            var hash = ComputeHash(csharpCode);

            if (Directory.EnumerateFiles(siteAssetsDir).Any()) //Only re-use cached if the files are already in the output
            {
                if (_cache.TryGetValue(hash, out var cachedResult))
                {
                    return cachedResult;
                }
            }

            var h5Version = await GetLatestVersionAsync("h5");
            var tesseraeVersion = await GetLatestVersionAsync("Tesserae");
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

            var compiledJavascript = await CompilationProcessor.CompileAsync(request);

            if (compiledJavascript.Output == null || !compiledJavascript.Output.Any())
            {
                 throw new Exception("H5 compilation failed or produced no output.");
            }

            var result = new TesseraeCompilerResult();


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

            foreach (var file in compiledJavascript.Output.DistinctBy(v => v.Key.Replace(".min.", ".")))
            {
                var fileName = Path.GetFileName(file.Key);
                if (fileName.Equals("app.js", StringComparison.OrdinalIgnoreCase)) continue;

                var relativePath = file.Key.Replace("\\", "/");
                var destPath = Path.Combine(siteAssetsDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destPath));

                using(var f = File.OpenWrite(destPath))
                {
                    await file.Value.CopyToAsync(f);
                }

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
            htmlBuilder.AppendLine($"<script>{result.AppJsContent}</script>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            result.OutputHtml = htmlBuilder.ToString();
            
            _cache[hash] = result;
            return result;
        }
    }
}