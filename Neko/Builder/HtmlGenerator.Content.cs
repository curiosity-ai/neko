using Neko.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        private void RenderBreadcrumbs(StringBuilder sb, NavigationContext navContext)
        {
            if (navContext == null || !navContext.Breadcrumbs.Any()) return;

            sb.AppendLine("                    <nav class=\"flex mb-4 text-sm text-gray-500 dark:text-gray-400 not-prose\">");
            sb.AppendLine("                        <ol class=\"flex items-center space-x-2 flex-wrap\">");
            for (int i = 0; i < navContext.Breadcrumbs.Count; i++)
            {
                var item = navContext.Breadcrumbs[i];
                if (i > 0)
                {
                    sb.AppendLine("                            <li><i class=\"fi fi-rr-angle-small-right text-xs\"></i></li>");
                }
                if (!string.IsNullOrEmpty(item.Url))
                {
                    sb.AppendLine($"                            <li><a href=\"{item.Url}\" class=\"hover:text-gray-700 dark:hover:text-gray-200 transition-colors\">{item.Title}</a></li>");
                }
                else
                {
                    sb.AppendLine($"                            <li><span>{item.Title}</span></li>");
                }
            }
            sb.AppendLine("                        </ol>");
            sb.AppendLine("                    </nav>");
        }

        private void RenderBlogPostHeader(StringBuilder sb, ParsedDocument document, string currentUrl)
        {
            if (currentUrl == null || !currentUrl.StartsWith("/blog/") || currentUrl.EndsWith("/index")) return;

            sb.AppendLine("<div class=\"mb-8 not-prose\">");
            if (!string.IsNullOrEmpty(document.FrontMatter.Cover))
            {
                sb.AppendLine($"<div class=\"aspect-video w-full rounded-lg overflow-hidden mb-6 bg-gray-100 dark:bg-gray-900\">");
                sb.AppendLine($"    <img src=\"{document.FrontMatter.Cover}\" class=\"w-full h-full object-cover\">");
                sb.AppendLine($"</div>");
            }

            var blogTitle = !string.IsNullOrEmpty(document.FrontMatter.Title) ? document.FrontMatter.Title : document.FrontMatter.Label;
            sb.AppendLine($"<h1 class=\"text-3xl md:text-4xl font-bold text-gray-900 dark:text-gray-100 mb-4\">{blogTitle}</h1>");

            sb.AppendLine("<div class=\"flex items-center gap-4 text-sm text-gray-600 dark:text-gray-400\">");
            if (!string.IsNullOrEmpty(document.FrontMatter.Author))
            {
                 sb.AppendLine("<div class=\"flex items-center gap-2\">");
                 if (!string.IsNullOrEmpty(document.FrontMatter.AuthorImage)) {
                    sb.AppendLine($"<img src=\"{document.FrontMatter.AuthorImage}\" class=\"w-8 h-8 rounded-full bg-gray-100\">");
                 }
                 sb.AppendLine($"<span class=\"font-medium text-gray-900 dark:text-gray-100\">{document.FrontMatter.Author}</span>");
                 sb.AppendLine("</div>");
            }
            if (!string.IsNullOrEmpty(document.FrontMatter.Date))
            {
                if (!string.IsNullOrEmpty(document.FrontMatter.Author)) sb.AppendLine($"<span>•</span>");
                sb.AppendLine($"<time>{document.FrontMatter.Date}</time>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }

        private void RenderArticleBody(
            StringBuilder sb,
            ParsedDocument document,
            List<(ParsedDocument Doc, string Url)> blogPosts,
            List<(ParsedDocument Doc, string Url, string Version)> changelogEntries,
            string currentUrl)
        {
            var htmlContent = BuildIndexableContent(document, blogPosts, changelogEntries, currentUrl);

            string effectivePassword = null;
            if (!string.IsNullOrEmpty(document.FrontMatter.Password))
            {
                if (!document.FrontMatter.Password.Equals("none", System.StringComparison.OrdinalIgnoreCase))
                {
                    effectivePassword = document.FrontMatter.Password;
                }
            }
            else if (!string.IsNullOrEmpty(_config.Password))
            {
                effectivePassword = _config.Password;
            }

            if (!string.IsNullOrEmpty(effectivePassword))
            {
                RenderPasswordProtectedBody(sb, htmlContent, effectivePassword, document);
            }
            else
            {
                sb.AppendLine(htmlContent);
            }
        }

        private void RenderPasswordProtectedBody(StringBuilder sb, string htmlContent, string effectivePassword, ParsedDocument document)
        {
            var isGlobalPassword = string.IsNullOrEmpty(document.FrontMatter.Password) && !string.IsNullOrEmpty(_config.Password);
            sb.AppendLine($"<script>window.nekoIsGlobalPassword = {isGlobalPassword.ToString().ToLower()};</script>");

            var encryptionResult = Neko.Encryption.PageEncryptor.Encrypt(htmlContent, effectivePassword);

            sb.AppendLine($"<div id=\"content-container\">");
            sb.AppendLine($"    <div id=\"password-form-container\" class=\"flex flex-col items-center justify-center py-20 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700\">");
            sb.AppendLine($"        <div class=\"p-8 bg-white dark:bg-gray-900 rounded-lg shadow-sm border border-gray-200 dark:border-gray-800 max-w-md w-full text-center\">");
            sb.AppendLine($"            <div class=\"w-12 h-12 bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400 rounded-full flex items-center justify-center mx-auto mb-4\">");
            sb.AppendLine($"                <i class=\"fi fi-rr-lock text-xl\"></i>");
            sb.AppendLine($"            </div>");
            sb.AppendLine($"            <h2 class=\"text-xl font-bold text-gray-900 dark:text-gray-100 mb-2\">Password Protected</h2>");
            sb.AppendLine($"            <p class=\"text-sm text-gray-500 dark:text-gray-400 mb-6\">This page is password protected. Please enter the password to view the content.</p>");
            sb.AppendLine($"            <div class=\"space-y-4\">");
            sb.AppendLine($"                <div>");
            sb.AppendLine($"                    <input type=\"password\" id=\"password-input\" class=\"w-full px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-md focus:outline-none focus:ring-2 focus:ring-primary-500 text-gray-900 dark:text-gray-100\" placeholder=\"Enter password...\" autofocus>");
            sb.AppendLine($"                </div>");
            sb.AppendLine($"                <button id=\"password-submit\" class=\"w-full px-4 py-2 bg-primary-600 hover:bg-primary-700 text-white rounded-md font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900\">Unlock</button>");
            sb.AppendLine($"                <p id=\"password-error\" class=\"hidden text-sm text-red-600 dark:text-red-400\">Incorrect password. Please try again.</p>");
            sb.AppendLine($"            </div>");
            sb.AppendLine($"        </div>");
            sb.AppendLine($"    </div>");
            sb.AppendLine($"</div>");

            var payload = System.Text.Json.JsonSerializer.Serialize(new {
                salt = encryptionResult.Salt,
                iv = encryptionResult.Iv,
                data = encryptionResult.Data
            });
            sb.AppendLine($"<script type=\"application/json\" id=\"encrypted-data\">{payload}</script>");
            sb.AppendLine($"<script src=\"/assets/password.js\"></script>");
        }

        private void RenderPageNavigation(StringBuilder sb, NavigationContext navContext)
        {
            if (navContext == null) return;

            // Lesson-step navigation: rendered on pages inside a [!lesson] folder,
            // ordered by the curriculum (frontmatter `order`, then title) defined
            // by the parent lesson page. Replaces the generic Prev/Next on these
            // pages so the labels are tailored to the learning flow.
            if (navContext.IsLessonStep && (navContext.LessonPrev != null || navContext.LessonNext != null))
            {
                RenderLessonNavigation(sb, navContext);
            }
            // Standard Prev/Next Navigation (suppressed on lesson step pages — see above).
            else if (navContext.Prev != null || navContext.Next != null)
            {
                RenderPrevNextNavigation(sb, navContext);
            }
        }

        private void RenderLessonNavigation(StringBuilder sb, NavigationContext navContext)
        {
            sb.AppendLine("                    <div class=\"grid grid-cols-1 md:grid-cols-2 gap-4 mt-12 pt-8 border-t border-gray-200 dark:border-gray-800 not-prose\">");

            if (navContext.LessonPrev != null)
            {
                sb.AppendLine($"                        <a href=\"{navContext.LessonPrev.Url}\" class=\"flex flex-col p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-primary-500 dark:hover:border-primary-500 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-all group\">");
                sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400\">Go back</span>");
                sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\"><i class=\"fi fi-rr-angle-small-left transition-transform group-hover:-translate-x-1\"></i> {navContext.LessonPrev.Title}</span>");
                sb.AppendLine("                        </a>");
            }
            else
            {
                sb.AppendLine("                        <div></div>");
            }

            if (navContext.LessonNext != null)
            {
                sb.AppendLine($"                        <a href=\"{navContext.LessonNext.Url}\" class=\"flex flex-col items-end p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-primary-500 dark:hover:border-primary-500 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-all group text-right\">");
                sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400\">Next step</span>");
                sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\">{navContext.LessonNext.Title} <i class=\"fi fi-rr-angle-small-right transition-transform group-hover:translate-x-1\"></i></span>");
                sb.AppendLine("                        </a>");
            }
            else
            {
                sb.AppendLine("                        <div></div>");
            }

            sb.AppendLine("                    </div>");
        }

        private void RenderPrevNextNavigation(StringBuilder sb, NavigationContext navContext)
        {
            sb.AppendLine("                    <div class=\"grid grid-cols-1 md:grid-cols-2 gap-4 mt-12 pt-8 border-t border-gray-200 dark:border-gray-800 not-prose\">");

            if (navContext.Prev != null)
            {
                sb.AppendLine($"                        <a href=\"{navContext.Prev.Url}\" class=\"flex flex-col p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-primary-500 dark:hover:border-primary-500 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-all group\">");
                sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400\">Previous</span>");
                sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\"><i class=\"fi fi-rr-arrow-small-left transition-transform group-hover:-translate-x-1\"></i> {navContext.Prev.Title}</span>");
                sb.AppendLine("                        </a>");
            }
            else
            {
                sb.AppendLine("                        <div></div>");
            }

            if (navContext.Next != null)
            {
                sb.AppendLine($"                        <a href=\"{navContext.Next.Url}\" class=\"flex flex-col items-end p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-primary-500 dark:hover:border-primary-500 hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-all group text-right\">");
                sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-primary-600 dark:group-hover:text-primary-400\">Next</span>");
                sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\">{navContext.Next.Title} <i class=\"fi fi-rr-arrow-small-right transition-transform group-hover:translate-x-1\"></i></span>");
                sb.AppendLine("                        </a>");
            }

            sb.AppendLine("                    </div>");
        }

        private void RenderBacklinks(StringBuilder sb, List<(string Url, string Title)> backlinks)
        {
            if (backlinks == null || backlinks.Count == 0) return;

            sb.AppendLine("                    <div class=\"mt-8 pt-8 border-t border-gray-200 dark:border-gray-800\">");
            sb.AppendLine("                        <h3 class=\"text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4\">Referenced by</h3>");
            sb.AppendLine("                        <ul class=\"space-y-2 list-none pl-0\">");
            foreach (var link in backlinks)
            {
                sb.AppendLine($"                            <li><a href=\"{link.Url}\" class=\"text-primary-600 dark:text-primary-400 no-underline flex items-center gap-2\"><i class=\"fi fi-rr-arrow-small-right no-underline\"></i><span class=\"hover:underline\">{link.Title}</span></a></li>");
            }
            sb.AppendLine("                        </ul>");
            sb.AppendLine("                    </div>");
        }

        private void RenderFooter(StringBuilder sb)
        {
            sb.AppendLine("                    <footer class=\"mt-12 py-6 border-t border-gray-200 dark:border-gray-800 text-sm text-gray-500 dark:text-gray-400 flex flex-col md:flex-row justify-between items-center not-prose\">");
            sb.AppendLine($"                        <div>&copy; {System.DateTime.Now.Year} {_config.Branding.Title}. All rights reserved.</div>");

            if (!string.IsNullOrEmpty(_config.Branding.Repository))
            {
                 sb.AppendLine($"                        <div class=\"mt-2 md:mt-0\">");
                 sb.AppendLine($"                            <a href=\"{_config.Branding.Repository}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"hover:text-primary-600 dark:hover:text-primary-400 transition-colors flex items-center gap-1\">");
                 sb.AppendLine("                                <i class=\"fi fi-rr-edit\"></i> Edit on GitHub");
                 sb.AppendLine("                            </a>");
                 sb.AppendLine("                        </div>");
            }
            else if (_config.PoweredByNeko)
            {
                 sb.AppendLine("                        <div class=\"mt-2 md:mt-0\">Powered by Neko</div>");
            }
            sb.AppendLine("                    </footer>");
        }

        private void RenderTocSidebar(StringBuilder sb, ParsedDocument document, string currentUrl)
        {
            sb.AppendLine("            <aside id=\"toc-sidebar\" class=\"w-64 hidden xl:block shrink-0 overflow-y-auto border-l border-gray-200 dark:border-gray-800 p-6\">");
            sb.AppendLine("                <div class=\"sticky top-0\">");
            RenderPageLinks(sb, document, currentUrl);
            sb.AppendLine("                    <h5 class=\"text-xs font-semibold mb-4 text-gray-900 dark:text-gray-100 uppercase tracking-wider\">On this page</h5>");
            sb.AppendLine("                    <ul class=\"space-y-2.5 text-sm text-gray-500 dark:text-gray-400 border-l border-gray-200 dark:border-gray-800 relative\" id=\"toc-list\">");
            sb.AppendLine("                        <div id=\"toc-highlight\" class=\"absolute left-0 border-l-2 border-primary-600 dark:border-primary-400 transition-all duration-200 ease-in-out -ml-px\" style=\"top: 0; height: 0; opacity: 0;\"></div>");
            foreach (var item in document.Toc)
            {
                if (item.Level < 2) continue;
                var padding = item.Level == 2 ? "pl-4" : "pl-8";
                sb.AppendLine($"                        <li><a href=\"#{item.Id}\" class=\"block {padding} hover:text-primary-600 dark:hover:text-primary-400 transition-colors toc-link\" data-id=\"{item.Id}\">{item.Title}</a></li>");
            }
            sb.AppendLine("                    </ul>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </aside>");
        }

        private void RenderPageLinks(StringBuilder sb, ParsedDocument document, string currentUrl)
        {
            var pageLinks = _config?.PageLinks;
            if (pageLinks == null || pageLinks.Count == 0) return;

            var pageTitle = document?.FrontMatter?.Title;
            if (string.IsNullOrEmpty(pageTitle) && document?.Toc != null)
            {
                foreach (var item in document.Toc)
                {
                    if (item.Level == 1 && !string.IsNullOrEmpty(item.Title))
                    {
                        pageTitle = item.Title;
                        break;
                    }
                }
            }
            pageTitle ??= string.Empty;
            var pageUrl = ResolvePageAbsoluteUrl(currentUrl);
            var encodedPage = System.Uri.EscapeDataString(pageTitle);
            var encodedUrl = System.Uri.EscapeDataString(pageUrl);

            sb.AppendLine("                    <ul class=\"neko-page-links mb-6 space-y-2 text-sm text-gray-500 dark:text-gray-400\">");
            foreach (var link in pageLinks)
            {
                if (link == null || string.IsNullOrEmpty(link.Url)) continue;

                var template = link.Url;
                var fallback = template
                    .Replace("${page}", encodedPage)
                    .Replace("${url}", encodedUrl)
                    .Replace("${selection}", string.Empty);

                var target = NormalizeTarget(link.Target);
                var targetAttr = string.IsNullOrEmpty(target) ? string.Empty : $" target=\"{EscapeHtmlAttr(target)}\"";
                var relAttr = target == "_blank" ? " rel=\"noopener noreferrer\"" : string.Empty;

                var iconHtml = string.Empty;
                if (!string.IsNullOrEmpty(link.Icon))
                {
                    iconHtml = $"<i class=\"{IconHelper.GetIconClass(link.Icon)} text-base\"></i>";
                }

                var label = EscapeHtmlAttr(link.Label ?? string.Empty);

                sb.AppendLine(
                    $"                        <li><a class=\"neko-page-link flex items-center gap-2 hover:text-primary-600 dark:hover:text-primary-400 transition-colors\" " +
                    $"href=\"{EscapeHtmlAttr(fallback)}\" " +
                    $"data-neko-link-template=\"{EscapeHtmlAttr(template)}\" " +
                    $"data-neko-page=\"{EscapeHtmlAttr(pageTitle)}\" " +
                    $"data-neko-url=\"{EscapeHtmlAttr(pageUrl)}\"" +
                    $"{targetAttr}{relAttr}>" +
                    $"{iconHtml}<span>{label}</span></a></li>");
            }
            sb.AppendLine("                    </ul>");
        }

        private void RenderBlogIndex(StringBuilder sb, List<(ParsedDocument Doc, string Url)> posts)
        {
            if (posts == null || posts.Count == 0) return;

            sb.AppendLine("<div class=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mt-8 not-prose\">");

            foreach (var post in posts)
            {
                var title = !string.IsNullOrEmpty(post.Doc.FrontMatter.Title) ? post.Doc.FrontMatter.Title : post.Doc.FrontMatter.Label;
                var desc = post.Doc.FrontMatter.Description;
                var date = post.Doc.FrontMatter.Date;
                var author = post.Doc.FrontMatter.Author;
                var cover = post.Doc.FrontMatter.Cover;
                var url = post.Url;

                sb.AppendLine($"<a href=\"{url}\" class=\"group flex flex-col bg-white dark:bg-gray-800 rounded-lg shadow-sm hover:shadow-md border border-gray-200 dark:border-gray-700 transition-all overflow-hidden hover:-translate-y-1 duration-300\">");

                if (!string.IsNullOrEmpty(cover))
                {
                    sb.AppendLine($"    <div class=\"aspect-video w-full overflow-hidden bg-gray-100 dark:bg-gray-900\">");
                    sb.AppendLine($"        <img src=\"{cover}\" alt=\"{title}\" class=\"w-full h-full object-cover transition-transform duration-500 group-hover:scale-105\">");
                    sb.AppendLine($"    </div>");
                }

                sb.AppendLine($"    <div class=\"flex flex-col flex-1 p-6\">");

                if (!string.IsNullOrEmpty(date))
                {
                    sb.AppendLine($"        <div class=\"text-xs text-gray-500 dark:text-gray-400 mb-2\">{date}</div>");
                }

                sb.AppendLine($"        <h3 class=\"text-lg font-bold text-gray-900 dark:text-gray-100 mb-2 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors\">{title}</h3>");

                if (!string.IsNullOrEmpty(desc))
                {
                    sb.AppendLine($"        <p class=\"text-sm text-gray-600 dark:text-gray-400 flex-1 line-clamp-3\">{desc}</p>");
                }

                if (!string.IsNullOrEmpty(author))
                {
                    sb.AppendLine($"        <div class=\"mt-4 flex items-center gap-2 text-xs font-medium text-gray-900 dark:text-gray-100\">");
                    if (!string.IsNullOrEmpty(post.Doc.FrontMatter.AuthorImage)) {
                        sb.AppendLine($"            <img src=\"{post.Doc.FrontMatter.AuthorImage}\" class=\"w-6 h-6 rounded-full bg-gray-100\">");
                    }
                    sb.AppendLine($"            <span>{author}</span>");
                    sb.AppendLine($"        </div>");
                }

                sb.AppendLine($"    </div>");
                sb.AppendLine($"</a>");
            }

            sb.AppendLine("</div>");
        }

        private void RenderChangelogIndex(StringBuilder sb, List<(ParsedDocument Doc, string Url, string Version)> entries)
        {
            if (entries == null || entries.Count == 0) return;

            sb.AppendLine("<div class=\"space-y-12 mt-8 not-prose relative\">");

            // Timeline line
            sb.AppendLine("<div class=\"absolute left-4 top-2 bottom-0 w-0.5 bg-gray-200 dark:bg-gray-700 md:left-[8.5rem]\"></div>");

            foreach (var entry in entries)
            {
                var version = entry.Version;
                // A version file may carry an optional headline (frontmatter title/label),
                // e.g. "Ready for Production", shown next to the version badge.
                var title = !string.IsNullOrEmpty(entry.Doc.FrontMatter.Title) ? entry.Doc.FrontMatter.Title : entry.Doc.FrontMatter.Label;
                var date = entry.Doc.FrontMatter.Date;
                var html = entry.Doc.Html;
                var anchor = entry.Url != null && entry.Url.StartsWith("#") ? entry.Url.Substring(1) : null;

                sb.AppendLine($"<div class=\"relative flex flex-col md:flex-row gap-8 scroll-mt-24\"{(string.IsNullOrEmpty(anchor) ? string.Empty : $" id=\"{EscapeHtmlAttr(anchor)}\"")}>");

                // Left Column (Date)
                sb.AppendLine("    <div class=\"flex md:w-32 flex-col items-start md:items-end md:text-right shrink-0 relative\">");

                // Dot
                sb.AppendLine("        <div class=\"absolute left-4 md:left-full md:-ml-[5px] w-2.5 h-2.5 rounded-full ring-4 ring-white dark:ring-gray-900 bg-primary-600 top-2 z-10 -translate-x-1/2 md:translate-x-1/2\"></div>");

                sb.AppendLine($"        <div class=\"pl-10 md:pl-0 pt-1\">");
                if (!string.IsNullOrEmpty(date))
                {
                    sb.AppendLine($"            <time class=\"text-sm font-medium text-gray-500 dark:text-gray-400\">{date}</time>");
                }
                sb.AppendLine($"        </div>");
                sb.AppendLine("    </div>");

                // Right Column (Version heading + content)
                sb.AppendLine("    <div class=\"flex-1 pl-10 md:pl-0 pb-8\">");
                sb.AppendLine("        <div class=\"flex items-baseline gap-3 mb-4 flex-wrap\">");
                sb.AppendLine($"            <span class=\"inline-flex items-center rounded-full bg-primary-100 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 px-3 py-1 text-sm font-semibold\">{EscapeHtmlAttr(version)}</span>");
                if (!string.IsNullOrEmpty(title))
                {
                    sb.AppendLine($"            <h2 class=\"text-xl font-bold text-gray-900 dark:text-gray-100 m-0\">{title}</h2>");
                }
                sb.AppendLine("        </div>");
                sb.AppendLine("        <div class=\"prose dark:prose-invert max-w-none prose-sm prose-headings:font-semibold prose-a:text-primary-600\">");
                sb.AppendLine(html);
                sb.AppendLine("        </div>");
                sb.AppendLine("    </div>");

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
        }
    }
}
