using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Neko.Builder;

namespace Neko.Server
{
    /// <summary>
    /// The Monaco editor behind the "edit this page" modal of <c>neko watch</c>, served from the zip embedded in
    /// Neko (<c>Resources/monaco.zip</c>, built by <c>reference/monaco/build-monaco.py</c>), in memory, at
    /// <c>/neko-monaco/vs/…</c>. It is not part of a site's output and never comes from a CDN.
    /// </summary>
    internal static class MonacoAssets
    {
        /// <summary>The URL folder the dev server serves the editor from.</summary>
        public const string UrlPrefix = "/neko-monaco/";

        private static readonly Lazy<Dictionary<string, byte[]>> Files = new(Load);

        /// <summary>The file at <paramref name="path"/> (e.g. <c>vs/loader.js</c>), if the embedded editor has it.</summary>
        public static bool TryGet(string path, out byte[] bytes, out string contentType)
        {
            bytes = null;
            contentType = ContentTypeFor(path);
            return !string.IsNullOrEmpty(path) && !path.Contains("..") && Files.Value.TryGetValue(path.TrimStart('/'), out bytes);
        }

        private static string ContentTypeFor(string path)
        {
            switch (Path.GetExtension(path ?? string.Empty).ToLowerInvariant())
            {
                case ".js": return "text/javascript; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".ttf": return "font/ttf";
                case ".json": return "application/json";
                default: return "application/octet-stream";
            }
        }

        private static Dictionary<string, byte[]> Load()
        {
            var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using var zip = new ZipArchive(new MemoryStream(StandaloneAssets.Bytes("monaco.zip")), ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                using var stream = entry.Open();
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                map[entry.FullName] = ms.ToArray();
            }
            return map;
        }
    }
}
