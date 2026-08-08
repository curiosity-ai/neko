using System;
using System.Collections.Generic;
using System.IO;

namespace Neko.Builder
{
    /// <summary>
    /// One project's slice of the <c>neko watch --focus</c> path.
    ///
    /// Focus mode narrows a watch session to a single folder (or file) of the
    /// documentation tree: only pages under it are regenerated, every other page
    /// keeps the HTML the previous build already wrote, and projects with nothing
    /// under the focus path are not built at all.
    ///
    /// The CLI takes the focus path relative to the watch root; <see cref="Resolve"/>
    /// turns it into the per-project scope each <see cref="SiteBuilder"/> needs.
    /// </summary>
    public sealed class FocusScope
    {
        /// <summary>
        /// Project-relative focus target, using <c>/</c> separators and no leading or
        /// trailing slash. Empty means the whole project is in focus.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// True when the project has nothing under the focus path: it is skipped
        /// entirely and its previously generated output is reused as-is.
        /// </summary>
        public bool SkipsProject { get; }

        private FocusScope(string path, bool skipsProject)
        {
            Path = path ?? string.Empty;
            SkipsProject = skipsProject;
        }

        /// <summary>Every page of the project is in focus.</summary>
        public static FocusScope WholeProject { get; } = new FocusScope(string.Empty, false);

        /// <summary>The project is outside the focus path and is not rebuilt.</summary>
        public static FocusScope Skipped { get; } = new FocusScope(string.Empty, true);

        /// <summary>True when the focus targets a sub-path rather than the whole project.</summary>
        public bool IsPartial => !SkipsProject && Path.Length > 0;

        /// <summary>
        /// True when <paramref name="relativePath"/> (project-relative, either
        /// separator) is the focus target itself or sits under it.
        /// </summary>
        public bool Includes(string relativePath)
        {
            if (SkipsProject) return false;
            if (Path.Length == 0) return true;

            var normalized = Normalize(relativePath);
            if (normalized.Length == 0) return false;

            return normalized.Equals(Path, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(Path + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when <paramref name="relativeDir"/> overlaps the focus path in either
        /// direction — the folder sits under the focus target, or contains it. Used for
        /// aggregated pages (changelog folders), which must be regenerated as soon as
        /// any of their entries is in focus.
        /// </summary>
        public bool Intersects(string relativeDir)
        {
            if (SkipsProject) return false;
            if (Path.Length == 0) return true;

            var normalized = Normalize(relativeDir);
            if (normalized.Length == 0) return true; // project root contains everything

            return normalized.Equals(Path, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(Path + "/", StringComparison.OrdinalIgnoreCase)
                || Path.StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves the scope for one project of a (possibly multi-repo) build.
        /// <paramref name="focusFullPath"/> is the absolute focus target;
        /// <paramref name="allProjectDirs"/> is every project root in the build, so a
        /// focus path that lands inside a nested project is attributed to that project
        /// alone rather than to each of its ancestors.
        /// </summary>
        public static FocusScope Resolve(string projectDir, string focusFullPath, IReadOnlyList<string> allProjectDirs)
        {
            var project = System.IO.Path.GetFullPath(projectDir);
            var focus = System.IO.Path.GetFullPath(focusFullPath);

            // The focus target sits inside this project — build just that sub-path,
            // unless a more deeply nested project owns it (then this one has nothing
            // to rebuild).
            if (IsSameOrUnder(focus, project))
            {
                var owner = MostSpecificOwner(focus, allProjectDirs);
                if (owner != null && !PathsEqual(owner, project)) return Skipped;

                var relative = System.IO.Path.GetRelativePath(project, focus);
                var normalized = Normalize(relative);
                return normalized.Length == 0 || normalized == "." ? WholeProject : new FocusScope(normalized, false);
            }

            // The whole project sits inside the focus folder — build all of it.
            if (IsSameOrUnder(project, focus)) return WholeProject;

            return Skipped;
        }

        private static string MostSpecificOwner(string focusFullPath, IReadOnlyList<string> projectDirs)
        {
            string best = null;
            if (projectDirs == null) return null;

            foreach (var dir in projectDirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                var full = System.IO.Path.GetFullPath(dir);
                if (!IsSameOrUnder(focusFullPath, full)) continue;
                if (best == null || full.Length > best.Length) best = full;
            }

            return best;
        }

        private static bool IsSameOrUnder(string candidate, string root)
        {
            if (PathsEqual(candidate, root)) return true;
            var prefix = root.EndsWith(System.IO.Path.DirectorySeparatorChar)
                ? root
                : root + System.IO.Path.DirectorySeparatorChar;
            return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PathsEqual(string a, string b)
            => string.Equals(a.TrimEnd(System.IO.Path.DirectorySeparatorChar), b.TrimEnd(System.IO.Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

        private static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var p = path.Replace('\\', '/').Trim();
            while (p.StartsWith("./")) p = p.Substring(2);
            return p.Trim('/');
        }
    }
}
