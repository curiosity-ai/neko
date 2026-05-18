using System;

namespace Neko.Configuration
{
    /// <summary>
    /// Strips source-file extensions (.md, .html) from internal link targets so
    /// authors can reference pages by their on-disk filename and still get the
    /// clean URL the rendered site uses. External URLs, anchors, and mailto:
    /// links are left untouched. Fragment (#anchor) and query (?x=y) parts are
    /// preserved on the trailing side of the path.
    /// </summary>
    public static class LinkNormalizer
    {
        public static string Normalize(string link)
        {
            if (string.IsNullOrEmpty(link)) return link;
            if (link.Contains("://")) return link;
            if (link.StartsWith("#") || link.StartsWith("mailto:") || link.StartsWith("tel:")) return link;

            var path = link;
            string suffix = "";

            var hashIndex = path.IndexOf('#');
            var queryIndex = path.IndexOf('?');
            var splitIndex = (hashIndex >= 0 && queryIndex >= 0) ? Math.Min(hashIndex, queryIndex)
                            : (hashIndex >= 0 ? hashIndex : queryIndex);
            if (splitIndex >= 0)
            {
                suffix = path.Substring(splitIndex);
                path = path.Substring(0, splitIndex);
            }

            if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 3);
            else if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - 5);

            return path + suffix;
        }
    }
}
