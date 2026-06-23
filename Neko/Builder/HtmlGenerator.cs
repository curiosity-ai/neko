using Neko.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        private readonly NekoConfig _config;
        private readonly bool _isWatchMode;
        private readonly string _headIncludes;

        public HtmlGenerator(NekoConfig config, bool isWatchMode = false, string headIncludes = null)
        {
            _config = config;
            _isWatchMode = isWatchMode;
            _headIncludes = headIncludes;
        }

        public string GenerateNotFound(NotFoundConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\" class=\"scroll-smooth h-full\">");

            GenerateHead(sb, config.Title, config.Message);

            sb.AppendLine("<body class=\"h-full\">");

            // Tailwind UI 404 Simple Layout
            sb.AppendLine("  <main class=\"grid min-h-full place-items-center bg-white dark:bg-gray-900 px-6 py-24 sm:py-32 lg:px-8\">");
            sb.AppendLine("    <div class=\"text-center\">");
            sb.AppendLine("      <p class=\"text-base font-semibold text-indigo-600 dark:text-indigo-400\">404</p>");
            sb.AppendLine($"      <h1 class=\"mt-4 text-3xl font-bold tracking-tight text-gray-900 dark:text-gray-100 sm:text-5xl\">{config.Title}</h1>");
            sb.AppendLine($"      <p class=\"mt-6 text-base leading-7 text-gray-600 dark:text-gray-400\">{config.Message}</p>");
            sb.AppendLine("      <div class=\"mt-10 flex items-center justify-center gap-x-6\">");
            sb.AppendLine($"        <a href=\"{config.HomeLink}\" class=\"rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600\">{config.HomeText}</a>");

            if (!string.IsNullOrEmpty(config.ContactText))
            {
                sb.AppendLine($"        <a href=\"{config.ContactLink}\" class=\"text-sm font-semibold text-gray-900 dark:text-gray-100\">{config.ContactText} <span aria-hidden=\"true\">&rarr;</span></a>");
            }

            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </main>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        // Returns the rendered article HTML for `document`, including blog/changelog
        // listings that would be injected at this URL. Used by the page generator
        // and by the search indexer so both see the same content.
        public string BuildIndexableContent(ParsedDocument document, List<(ParsedDocument Doc, string Url)> blogPosts = null, List<(ParsedDocument Doc, string Url, string Version)> changelogEntries = null, string currentUrl = null)
        {
            var htmlContent = document.Html ?? string.Empty;
            if (!string.IsNullOrEmpty(htmlContent))
            {
                htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "href=\"((?!http:|https:|ftp:|mailto:|#|/)[^\"]+)\\.(md|html)\"", "href=\"$1\"");
            }

            if (currentUrl != null && (currentUrl == "/blog/index" || document.FrontMatter.Layout == "blog"))
            {
                var sbBlog = new StringBuilder();
                RenderBlogIndex(sbBlog, blogPosts);
                htmlContent += sbBlog.ToString();
            }

            if (document.FrontMatter.Layout == "changelog")
            {
                var sbChangelog = new StringBuilder();
                RenderChangelogIndex(sbChangelog, changelogEntries);
                htmlContent += sbChangelog.ToString();
            }

            return htmlContent;
        }

        public string Generate(
            ParsedDocument document,
            List<(string Url, string Title)> backlinks = null,
            NavigationContext navContext = null,
            List<LinkConfig> sidebarLinks = null,
            List<(ParsedDocument Doc, string Url)> blogPosts = null,
            List<(ParsedDocument Doc, string Url, string Version)> changelogEntries = null,
            string currentUrl = null)
        {
            var pageTitle = !string.IsNullOrEmpty(document.FrontMatter.Title)
                ? document.FrontMatter.Title
                : document.FrontMatter.Label;
            var title = !string.IsNullOrEmpty(pageTitle)
                ? $"{_config.Branding.Title} - {pageTitle}"
                : _config.Branding.Title;
            var description = !string.IsNullOrEmpty(document.FrontMatter.Description)
                ? document.FrontMatter.Description
                : _config.Meta.Description;

            // A protected page ships nothing page-specific in the static HTML: the
            // whole content column is encrypted, and the <title>/description are
            // masked to the site defaults. password.js sets the real title from the
            // decrypted H1 once unlocked.
            var effectivePassword = ResolveEffectivePassword(document);
            var isProtected = !string.IsNullOrEmpty(effectivePassword);
            var headTitle = isProtected ? _config.Branding.Title : title;
            var headDescription = isProtected ? _config.Meta.Description : description;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\" class=\"scroll-smooth\">");

            GenerateHead(sb, headTitle, headDescription);

            sb.AppendLine("<body class=\"bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col h-screen overflow-hidden\">");

            RenderBanner(sb);
            RenderNavbar(sb, currentUrl);
            RenderPivot(sb, currentUrl);

            var maxWidthClass = LayoutMaxWidthClass();
            var rowWidthClass = string.IsNullOrEmpty(maxWidthClass) ? string.Empty : $" {maxWidthClass} w-full";
            sb.AppendLine($"    <div class=\"flex flex-1 overflow-hidden{rowWidthClass}\">");

            if (_config.Layout.Sidebar)
            {
                RenderSidebar(sb, sidebarLinks);
            }

            sb.AppendLine("        <div class=\"flex-1 flex overflow-hidden\">");
            sb.AppendLine("            <main class=\"flex-1 overflow-y-auto overflow-x-clip p-4 md:p-8 scroll-smooth\" id=\"main-scroll\">");
            sb.AppendLine("                <div class=\"max-w-4xl mx-auto prose dark:prose-invert\">");

            if (isProtected)
            {
                // Encrypt the whole page-specific column as one payload so it reveals
                // atomically on unlock — and nothing (breadcrumbs, headings, prev/next,
                // backlinks) is readable in the page source before then.
                var inner = new StringBuilder();
                RenderBreadcrumbs(inner, navContext);
                RenderBlogPostHeader(inner, document, currentUrl);
                inner.AppendLine(BuildIndexableContent(document, blogPosts, changelogEntries, currentUrl));
                RenderPageNavigation(inner, navContext);
                RenderBacklinks(inner, backlinks);
                RenderProtectedColumn(sb, inner.ToString(), effectivePassword);
            }
            else
            {
                RenderBreadcrumbs(sb, navContext);
                RenderBlogPostHeader(sb, document, currentUrl);
                sb.AppendLine(BuildIndexableContent(document, blogPosts, changelogEntries, currentUrl));
                RenderPageNavigation(sb, navContext);
                RenderBacklinks(sb, backlinks);
            }
            RenderFooter(sb);

            sb.AppendLine("                </div>");
            sb.AppendLine("            </main>");

            if (_config.Layout.Toc && document.Toc != null && document.Toc.Any())
            {
                if (isProtected)
                {
                    // The rail keeps its width (no layout shift) but ships no heading
                    // text. password.js builds the list from the decrypted content and
                    // reveals the rail together with the body, in one tick.
                    RenderProtectedTocShell(sb, document, currentUrl);
                }
                else
                {
                    RenderTocSidebar(sb, document, currentUrl);
                }
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");

            if (_isWatchMode)
            {
                RenderWatchModeUi(sb);
            }

            RenderPageScripts(sb);

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        // Tailwind classes that cap and centre a layout region at the
        // configured `layout.maxWidth`. Returns an empty string when the cap
        // is disabled ("full"/"none"), leaving the region full-width.
        private string LayoutMaxWidthClass()
        {
            var mw = _config.Layout?.MaxWidth?.Trim();
            if (string.IsNullOrEmpty(mw)
                || mw.Equals("full", System.StringComparison.OrdinalIgnoreCase)
                || mw.Equals("none", System.StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string cls;
            if (mw.StartsWith("max-w-"))
            {
                cls = mw;
            }
            else if (mw.IndexOf("px", System.StringComparison.OrdinalIgnoreCase) >= 0
                || mw.IndexOf("rem", System.StringComparison.OrdinalIgnoreCase) >= 0
                || mw.IndexOf("em", System.StringComparison.OrdinalIgnoreCase) >= 0
                || mw.Contains("%")
                || mw.IndexOf("vw", System.StringComparison.OrdinalIgnoreCase) >= 0
                || mw.IndexOf("ch", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cls = $"max-w-[{mw}]";
            }
            else
            {
                cls = $"max-w-{mw}";
            }

            return $"{cls} mx-auto";
        }

        private static string EscapeHtmlAttr(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string NormalizeTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) return null;
            var t = target.Trim();
            if (t.StartsWith("_")) return t;
            if (t.Equals("blank", System.StringComparison.OrdinalIgnoreCase)
                || t.Equals("self", System.StringComparison.OrdinalIgnoreCase)
                || t.Equals("parent", System.StringComparison.OrdinalIgnoreCase)
                || t.Equals("top", System.StringComparison.OrdinalIgnoreCase))
            {
                return "_" + t.ToLowerInvariant();
            }
            return t;
        }

        private string ResolvePageAbsoluteUrl(string currentUrl)
        {
            var relative = currentUrl ?? string.Empty;
            var baseUrl = _config?.Url;
            var isPlaceholder = string.IsNullOrWhiteSpace(baseUrl)
                || baseUrl.Equals("localhost", System.StringComparison.OrdinalIgnoreCase);
            if (isPlaceholder) return relative;

            if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
            {
                baseUrl = "https://" + baseUrl;
            }
            baseUrl = baseUrl.TrimEnd('/');
            if (string.IsNullOrEmpty(relative)) return baseUrl;
            if (!relative.StartsWith("/")) relative = "/" + relative;
            return baseUrl + relative;
        }
    }
}
