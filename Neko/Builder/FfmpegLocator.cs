using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Neko.Builder
{
    /// <summary>
    /// Finds the ffmpeg binary <c>neko render</c> encodes with, and points
    /// <see cref="FFmpeg"/> (Xabe.FFmpeg, the wrapper Mosaik uses) at it.
    ///
    /// Neko does not bundle ffmpeg — the per-platform builds are tens of megabytes
    /// each — so it looks, in order, for:
    /// <list type="number">
    ///   <item><c>--ffmpeg</c> on the command line (a file or its folder);</item>
    ///   <item>the <c>NEKO_FFMPEG</c> environment variable;</item>
    ///   <item>the <c>Curiosity.FFmpeg.Runtimes.&lt;rid&gt;</c> layout Mosaik ships
    ///   (<c>runtimes/&lt;rid&gt;/native/ffmpeg-dist</c>) next to the Neko binaries;</item>
    ///   <item><c>ffmpeg</c> on the <c>PATH</c>;</item>
    ///   <item>a copy Neko downloaded before, under <c>~/.neko/ffmpeg</c>;</item>
    ///   <item>and failing all of those, downloads the official build there with
    ///   <see cref="FFmpegDownloader"/> — once.</item>
    /// </list>
    /// </summary>
    public static class FfmpegLocator
    {
        private static string Ext => OperatingSystem.IsWindows() ? ".exe" : string.Empty;

        public static string CacheDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neko", "ffmpeg");

        /// <summary>Returns the full path of ffmpeg, downloading it when nothing else is found.</summary>
        public static async Task<string> EnsureAsync(string explicitPath = null)
        {
            var found = Find(explicitPath);
            if (found != null) return Use(found);

            Console.WriteLine($"[render] ffmpeg not found; downloading the official build to {CacheDirectory} (once)…");
            Directory.CreateDirectory(CacheDirectory);
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, CacheDirectory);
            var downloaded = Path.Combine(CacheDirectory, "ffmpeg" + Ext);
            if (!File.Exists(downloaded))
            {
                throw new FileNotFoundException("ffmpeg could not be downloaded. Install it, or pass --ffmpeg <path>.");
            }
            if (!OperatingSystem.IsWindows())
            {
                foreach (var tool in new[] { "ffmpeg", "ffprobe" })
                {
                    var path = Path.Combine(CacheDirectory, tool);
                    if (File.Exists(path)) File.SetUnixFileMode(path, File.GetUnixFileMode(path) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                }
            }
            return Use(downloaded);
        }

        /// <summary>The first ffmpeg found without downloading, or null.</summary>
        public static string Find(string explicitPath = null)
        {
            foreach (var candidate in new[] { explicitPath, Environment.GetEnvironmentVariable("NEKO_FFMPEG") })
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                foreach (var name in new[] { "ffmpeg" + Ext, "ffmpeg-dist" + Ext })
                {
                    var inDir = Path.Combine(candidate, name);
                    if (File.Exists(inDir)) return Path.GetFullPath(inDir);
                }
            }

            // Mosaik's Curiosity.FFmpeg.Runtimes packages: a referencing app gets the
            // binary as ffmpeg-dist in runtimes/<rid>/native (or flattened beside it).
            var baseDir = AppContext.BaseDirectory;
            foreach (var path in new[]
            {
                Path.Combine(baseDir, "ffmpeg-dist" + Ext),
                Path.Combine(baseDir, "runtimes", RuntimeIdentifier, "native", "ffmpeg-dist" + Ext),
            })
            {
                if (File.Exists(path)) return path;
            }

            var onPath = FindOnPath("ffmpeg" + Ext);
            if (onPath != null) return onPath;

            var cached = Path.Combine(CacheDirectory, "ffmpeg" + Ext);
            return File.Exists(cached) && new FileInfo(cached).Length > 0 ? cached : null;
        }

        private static string Use(string ffmpegPath)
        {
            var dir = Path.GetDirectoryName(ffmpegPath);
            var name = Path.GetFileName(ffmpegPath);
            var probe = name.StartsWith("ffmpeg-dist", StringComparison.OrdinalIgnoreCase) ? "ffprobe-dist" + Ext : "ffprobe" + Ext;
            FFmpeg.SetExecutablesPath(dir, name, probe);
            return ffmpegPath;
        }

        private static string FindOnPath(string file)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(dir => Path.Combine(dir.Trim('"'), file))
                .FirstOrDefault(File.Exists);
        }

        /// <summary>linux-x64, linux-arm64, osx-arm64, win-x64, … — the names the runtime packages use.</summary>
        public static string RuntimeIdentifier
        {
            get
            {
                var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
                var arch = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.Arm64 => "arm64",
                    Architecture.X86 => "x86",
                    Architecture.Arm => "arm",
                    _ => "x64",
                };
                return os + "-" + arch;
            }
        }
    }
}
