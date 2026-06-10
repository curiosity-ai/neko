using System;
using System.Collections.Generic;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// Represents a changelog entry version parsed from a file name (e.g. <c>v26.6.md</c>,
    /// <c>1.2.0.md</c>, <c>v2024.06.18.md</c>). The numeric components drive ordering
    /// (newest first), while <see cref="Display"/> preserves a friendly label and
    /// <see cref="Anchor"/> provides a stable in-page id for deep links.
    /// </summary>
    public sealed class ChangelogVersion : IComparable<ChangelogVersion>
    {
        /// <summary>Friendly label shown to readers, always prefixed with <c>v</c> (e.g. <c>v26.6</c>).</summary>
        public string Display { get; }

        /// <summary>URL/id-safe slug for the version (e.g. <c>v26-6</c>), used for anchors.</summary>
        public string Anchor { get; }

        private readonly int[] _components;
        private readonly string _raw;

        private ChangelogVersion(string display, string anchor, int[] components, string raw)
        {
            Display = display;
            Anchor = anchor;
            _components = components;
            _raw = raw;
        }

        /// <summary>
        /// Attempts to parse a version out of a file name (without extension). A leading
        /// <c>v</c>/<c>V</c> and any <c>.</c>, <c>-</c> or <c>_</c> separators are accepted.
        /// Returns <c>false</c> when no leading numeric component can be found.
        /// </summary>
        public static bool TryParse(string fileNameWithoutExtension, out ChangelogVersion version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension)) return false;

            var raw = fileNameWithoutExtension.Trim();

            // Strip an optional leading "v"/"V" prefix when followed by a digit or separator.
            var core = raw;
            if (core.Length > 1
                && (core[0] == 'v' || core[0] == 'V')
                && (char.IsDigit(core[1]) || core[1] == '.' || core[1] == '-' || core[1] == '_'))
            {
                core = core.Substring(1);
            }
            core = core.TrimStart('.', '-', '_', ' ');

            var parts = core.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<int>();
            foreach (var part in parts)
            {
                // Consume the leading run of digits in each segment; stop at the first
                // segment that does not start with a number (e.g. "beta").
                var i = 0;
                while (i < part.Length && char.IsDigit(part[i])) i++;
                if (i == 0) break;
                if (int.TryParse(part.Substring(0, i), out var n)) components.Add(n);
                else break;
            }

            if (components.Count == 0) return false;

            // Display label: keep the author's spelling, but guarantee a "v" prefix.
            var display = raw;
            if (display.Length > 0 && char.IsDigit(display[0])) display = "v" + display;

            version = new ChangelogVersion(display, Slugify(display), components.ToArray(), raw);
            return true;
        }

        public int CompareTo(ChangelogVersion other)
        {
            if (other is null) return 1;

            var length = Math.Max(_components.Length, other._components.Length);
            for (var i = 0; i < length; i++)
            {
                var a = i < _components.Length ? _components[i] : 0;
                var b = i < other._components.Length ? other._components[i] : 0;
                if (a != b) return a.CompareTo(b);
            }

            // Stable tie-break so equal numeric versions keep a deterministic order.
            return string.Compare(_raw, other._raw, StringComparison.OrdinalIgnoreCase);
        }

        private static string Slugify(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }
    }
}
