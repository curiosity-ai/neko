using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    public static class IconHelper
    {
        private const string IconsResourceName = "Neko.Resources.icons.js";

        private static readonly Lazy<HashSet<string>> _validIcons = new(LoadValidIcons);
        private static readonly ConcurrentDictionary<string, byte> _warnedIcons = new();

        public static string GetIconClass(string iconName)
        {
            if (string.IsNullOrEmpty(iconName)) return string.Empty;

            WarnIfInvalid(iconName);

            if (iconName.StartsWith("brands-"))
            {
                return $"fi fi-{iconName}";
            }

            return $"fi fi-rr-{iconName}";
        }

        private static readonly string[] _imageExtensions =
            { ".svg", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".avif", ".ico" };

        // An icon value points at an image (rather than naming a UIcon) when it
        // looks like a path or URL, or ends in a known image extension.
        public static bool IsImagePath(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.StartsWith("/") || value.StartsWith("./") || value.StartsWith("../") ||
                value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var ext in _imageExtensions)
            {
                if (value.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Renders an icon value as a complete HTML element. Accepts a UIcons
        /// catalog name (<c>&lt;i class="fi …"&gt;</c>), a raw inline SVG, or an
        /// image path / URL such as a brand logo under <c>/assets/</c> (rendered
        /// as an <c>&lt;img&gt;</c>). Image paths are sized to <c>1em</c> so the
        /// caller's text-size classes (e.g. <c>text-lg</c>) keep controlling the
        /// rendered size, and they bypass the UIcons catalog warning.
        /// </summary>
        public static string RenderIcon(string icon, string extraClasses = "")
        {
            if (string.IsNullOrEmpty(icon)) return string.Empty;

            var trimmed = icon.Trim();
            var extra = string.IsNullOrEmpty(extraClasses) ? string.Empty : " " + extraClasses;

            if (trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            {
                return System.Net.WebUtility.HtmlDecode(trimmed);
            }

            if (IsImagePath(trimmed))
            {
                var src = System.Net.WebUtility.HtmlEncode(trimmed);
                return $"<img src=\"{src}\" alt=\"\" class=\"inline-block object-contain align-middle{extra}\" style=\"height:1em;width:1em;\" />";
            }

            return $"<i class=\"{GetIconClass(icon)}{extra}\"></i>";
        }

        private static void WarnIfInvalid(string iconName)
        {
            // Image paths / URLs and raw SVG are valid icon values that don't
            // live in the UIcons catalog — never warn about them.
            if (IsImagePath(iconName) ||
                iconName.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var catalog = _validIcons.Value;
            // If the catalog failed to load, skip validation rather than emitting
            // a warning for every icon on the page.
            if (catalog.Count == 0) return;
            if (catalog.Contains(iconName)) return;

            if (_warnedIcons.TryAdd(iconName, 0))
            {
                Console.WriteLine($"Warning: Invalid icon '{iconName}' — not found in UIcons catalog.");
            }
        }

        private static HashSet<string> LoadValidIcons()
        {
            var icons = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(IconsResourceName);
                if (stream == null) return icons;

                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                foreach (Match m in Regex.Matches(content, "id:\\s*\"([^\"]+)\""))
                {
                    icons.Add(m.Groups[1].Value);
                }
            }
            catch
            {
                // If catalog loading fails for any reason, fall back to skipping
                // validation instead of breaking the build.
            }
            return icons;
        }
    }
}
