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
        private readonly bool _editorEnabled;
        private readonly string _headIncludes;

        // The in-browser editing chrome (navbar edit button, sidebar edit/reorder
        // controls, Monaco modal) is only rendered when watch mode is on *and* the
        // editor hasn't been disabled (e.g. `neko watch --live` for a read-only
        // localhost preview that still live-reloads). Live reload itself stays tied
        // to `_isWatchMode` so the preview keeps refreshing on file changes.
        private bool _showEditor => _isWatchMode && _editorEnabled;

        // Utility-class tokens harvested from password-protected pages *before*
        // their HTML is encrypted. The emitted file only carries the encrypted
        // blob, so the Tailwind scanner (which re-reads emitted files) never sees
        // these classes and would otherwise omit their CSS rules — leaving
        // protected pages with broken styling (most visibly missing `dark:`
        // variants). The static stylesheet generator unions these in.
        public HashSet<string> ProtectedPageClassTokens { get; } = new HashSet<string>();

        public HtmlGenerator(NekoConfig config, bool isWatchMode = false, string headIncludes = null, bool editorEnabled = true)
        {
            _config = config;
            _isWatchMode = isWatchMode;
            _editorEnabled = editorEnabled;
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
                RenderBlogIndex(sbBlog, blogPosts, document);
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

            // Blog mode uses the `theme.base` palette (curiosity.ai: #f1f1f1 page,
            // #1f1f1f ink) so the white post cards and content stand out; docs stay
            // on a white canvas.
            if (_isBlogMode)
            {
                // `relative` so the blog-mode header can be an absolutely-positioned
                // translucent overlay (curiosity.ai pins a `backdrop-filter:blur` bar
                // over the scrolling content rather than reserving a row for it).
                sb.AppendLine("<body style=\"background-color:var(--blog-bg);color:var(--blog-ink)\" class=\"relative flex flex-col h-screen overflow-hidden\">");
            }
            else
            {
                sb.AppendLine("<body class=\"bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col h-screen overflow-hidden\">");
            }

            RenderBanner(sb);
            RenderNavbar(sb, currentUrl);
            RenderPivot(sb, currentUrl);

            var maxWidthClass = LayoutMaxWidthClass();
            // In blog mode the content row is NOT capped at `layout.maxWidth`: `main`
            // runs full-width so the marketing footer can span the pane edge-to-edge
            // (the curiosity.ai look). Centering is instead applied per-region — the
            // navbar caps its own inner row and the content keeps its reading column —
            // so the header and posts still line up at `maxWidth`.
            var rowWidthClass = (_isBlogMode || string.IsNullOrEmpty(maxWidthClass)) ? string.Empty : $" {maxWidthClass} w-full";
            sb.AppendLine($"    <div class=\"flex flex-1 overflow-hidden{rowWidthClass}\">");

            if (_config.Layout.Sidebar)
            {
                RenderSidebar(sb, sidebarLinks);
            }

            sb.AppendLine("        <div class=\"flex-1 flex overflow-hidden\">");
            // Blog mode pins the header as an overlay (see RenderNavbar), so the
            // scroll pane reserves top padding equal to the header height (h-20 =
            // 80px) plus the usual content gap — content then scrolls up under the
            // translucent bar instead of stopping below an opaque row.
            // Blog mode lays `main` out as a flex column so the content column can
            // `grow` to fill the viewport when a page is short (e.g. the index when a
            // tag filter narrows it to a few cards). Without this the full-bleed
            // footer floats partway up and leaves a large band of page background
            // below it.
            var mainPadClass = _isBlogMode
                ? "px-4 md:px-8 pt-24 md:pt-28 pb-4 md:pb-8 flex flex-col"
                : "p-4 md:p-8";
            sb.AppendLine($"            <main class=\"flex-1 overflow-y-auto overflow-x-clip {mainPadClass} scroll-smooth\" id=\"main-scroll\">");
            // The reading column is capped at `max-w-4xl` (896px) — comfortable for
            // article prose. The blog *index* is a card grid + search box, not prose,
            // so it gets a wider column (`max-w-6xl`, ~1152px) to match the roomier
            // curiosity.ai/resources/blog layout. Article (post) pages keep 4xl.
            var isBlogIndex = _isBlogMode && (currentUrl == "/blog/index" || document.FrontMatter.Layout == "blog");
            var contentColClass = isBlogIndex ? "max-w-6xl" : "max-w-4xl";
            // `grow` (blog mode) lets the column expand to fill the flex-column main,
            // pushing the deferred full-bleed footer to the bottom of short pages.
            var growClass = _isBlogMode ? " grow w-full" : string.Empty;
            sb.AppendLine($"                <div class=\"{contentColClass}{growClass} mx-auto prose dark:prose-invert\">");

            // The marketing (blog-mode) footer breaks out of the reading column to
            // span the full content pane, so it is rendered after the prose div.
            // Everything else keeps the slim in-column footer.
            var deferFooterFullWidth = _isBlogMode && _config.Footer != null && _config.Footer.HasRichContent;

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

            // The full-width marketing footer is site chrome (rendered after the
            // prose div); otherwise the slim footer stays in the reading column.
            if (!deferFooterFullWidth)
            {
                RenderFooter(sb);
            }

            sb.AppendLine("                </div>");

            if (deferFooterFullWidth)
            {
                RenderBlogMegaFooter(sb, currentUrl);
            }

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

        // Blog-mode palette, driven by `theme.base` so a site can match its own
        // brand. Defaults reproduce the curiosity.ai look: a near-white page
        // (#f1f1f1) with near-black ink (#1f1f1f) used for nav text, the solid
        // CTA fill, and the footer panel.
        private string BlogBaseBg()
        {
            if (_config.Theme?.Base != null && _config.Theme.Base.TryGetValue("base-bg", out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            return "#f1f1f1";
        }

        private string BlogBaseColor()
        {
            if (_config.Theme?.Base != null && _config.Theme.Base.TryGetValue("base-color", out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            return "#1f1f1f";
        }

        // Dark-mode counterparts, from `theme.dark`. Defaults to a near-black page
        // with light ink so blog mode reads correctly when the site is in dark mode.
        private string BlogDarkBg()
        {
            if (_config.Theme?.Dark != null && _config.Theme.Dark.TryGetValue("base-bg", out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            return "#0f1115";
        }

        private string BlogDarkColor()
        {
            if (_config.Theme?.Dark != null && _config.Theme.Dark.TryGetValue("base-color", out var v) && !string.IsNullOrWhiteSpace(v)) return v;
            return "#f1f1f1";
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
