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

        private static void WarnIfInvalid(string iconName)
        {
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
