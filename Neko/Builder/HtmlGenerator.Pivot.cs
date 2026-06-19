using Neko.Configuration;
using System;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        // The "pivot" is a contextual secondary navigation bar shown directly below
        // the header. Any top-nav link group that has `items` renders those items as
        // a horizontal row of tabs — but only when the current page lives inside the
        // group (i.e. its URL falls under one of the items). The tab for the section
        // the reader is currently in is highlighted. This is the default behaviour
        // for dropdown groups; no extra configuration is required.
        //
        // This lets a single site expose several "sections" (e.g. documentation
        // pillars vs. a developer/API area) and surface the right set of tabs
        // depending on where the reader is, without the reader having to open a
        // dropdown to switch pages within the section.
        private void RenderPivot(StringBuilder sb, string currentUrl)
        {
            if (_config.Links == null || _config.Links.Count == 0) return;

            var currentPath = BuildCurrentPivotPath(currentUrl);

            // Find the most specific matching item across all groups. The longest
            // matching item link wins, so `/workspace-build` is preferred over a
            // shorter sibling that happens to share a prefix.
            LinkConfig activeGroup = null;
            LinkConfig activeItem = null;
            var bestLength = -1;

            foreach (var link in _config.Links)
            {
                if (link.Items == null || link.Items.Count == 0) continue;

                foreach (var item in link.Items)
                {
                    var itemPath = NormalizePivotPath(item.Link);
                    if (itemPath.Length == 0 || itemPath == "/") continue;

                    var matches = string.Equals(currentPath, itemPath, StringComparison.OrdinalIgnoreCase)
                        || currentPath.StartsWith(itemPath + "/", StringComparison.OrdinalIgnoreCase);

                    if (matches && itemPath.Length > bestLength)
                    {
                        bestLength = itemPath.Length;
                        activeGroup = link;
                        activeItem = item;
                    }
                }
            }

            if (activeGroup == null) return;

            sb.AppendLine("    <nav class=\"shrink-0 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-6 z-20\">");
            sb.AppendLine("        <div class=\"flex items-center gap-6 overflow-x-auto text-sm font-medium\">");
            foreach (var item in activeGroup.Items)
            {
                var href = item.Link ?? "#";
                var target = !string.IsNullOrEmpty(item.Target) ? $" target=\"{item.Target}\"" : "";
                var iconHtml = (!_showPivotIcons || string.IsNullOrEmpty(item.Icon)) ? "" : $"<i class=\"{Neko.Builder.IconHelper.GetIconClass(item.Icon)}\"></i>";
                var isActive = ReferenceEquals(item, activeItem);
                var stateClass = isActive
                    ? "border-primary-600 text-primary-600 dark:text-primary-400 dark:border-primary-400"
                    : "border-transparent text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100";
                var ariaCurrent = isActive ? " aria-current=\"page\"" : "";
                sb.AppendLine($"            <a href=\"{href}\"{target}{ariaCurrent} class=\"{stateClass} flex items-center gap-2 border-b-2 py-3 whitespace-nowrap transition-colors\">{iconHtml}{item.Text}</a>");
            }
            sb.AppendLine("        </div>");
            sb.AppendLine("    </nav>");
        }

        // Combines the current sub-site route prefix with the page URL to produce
        // the absolute route path used to match against pivot item links.
        private static string BuildCurrentPivotPath(string currentUrl)
        {
            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            var rel = (currentUrl ?? string.Empty).TrimStart('/');
            var path = prefix + "/" + rel;
            if (!path.StartsWith("/")) path = "/" + path;
            path = path.TrimEnd('/');
            return path.Length == 0 ? "/" : path;
        }

        // Reduces a link target to a comparable route path: external URLs, anchors,
        // and mailto/tel links are ignored; query/fragment suffixes and trailing
        // slashes are stripped.
        private static string NormalizePivotPath(string link)
        {
            if (string.IsNullOrEmpty(link)) return string.Empty;
            if (link.Contains("://") || link.StartsWith("#") || link.StartsWith("mailto:") || link.StartsWith("tel:")) return string.Empty;

            var path = link;
            var splitIndex = path.IndexOfAny(new[] { '#', '?' });
            if (splitIndex >= 0) path = path.Substring(0, splitIndex);

            path = path.TrimEnd('/');
            if (path.Length == 0) return "/";
            if (!path.StartsWith("/")) path = "/" + path;
            return path;
        }
    }
}
