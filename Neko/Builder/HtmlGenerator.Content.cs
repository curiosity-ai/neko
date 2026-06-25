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

        // The password that gates this page: the page's own `password`, or the
        // site-wide `password` from neko.yml — unless the page opts out with
        // `password: none`. Null when the page is public.
        private string ResolveEffectivePassword(ParsedDocument document)
        {
            if (!string.IsNullOrEmpty(document.FrontMatter.Password))
            {
                return document.FrontMatter.Password.Equals("none", System.StringComparison.OrdinalIgnoreCase)
                    ? null
                    : document.FrontMatter.Password;
            }
            return string.IsNullOrEmpty(_config.Password) ? null : _config.Password;
        }

        private void RenderProtectedColumn(StringBuilder sb, string innerHtml, string effectivePassword)
        {
            // Harvest the utility classes from the plaintext before it is
            // encrypted. Only the encrypted blob is written to disk, so the
            // Tailwind scanner can't recover these classes from the emitted file;
            // collecting them here keeps the generated stylesheet complete for
            // protected pages (notably their dark-mode variants).
            ProtectedPageClassTokens.UnionWith(
                Neko.Builder.Tailwind.ClassExtractor.Extract(new[] { innerHtml }));

            var encryptionResult = Neko.Encryption.PageEncryptor.Encrypt(innerHtml, effectivePassword);

            // The prompt is hidden by default and only revealed by password.js when
            // there is no cached key for this password (or a cached key fails). When
            // the visitor has already unlocked a page protected with the same
            // password this session, the key is reused from sessionStorage and the
            // content is decrypted straight away — so navigating between protected
            // pages never flashes this form. A <noscript> rule reveals it for clients
            // without JavaScript, who otherwise see an empty page.
            sb.AppendLine($"<noscript><style>#password-form-container.hidden{{display:flex !important;}}</style></noscript>");
            sb.AppendLine($"<div id=\"content-container\">");
            sb.AppendLine($"    <div id=\"password-form-container\" class=\"hidden flex flex-col items-center justify-center py-20 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700\">");
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

            // Synchronous, pre-paint body restore. password.js loads as an external
            // (parser-blocking but separately fetched) script, so the browser can paint
            // the empty #content-container above before it runs — the "empty flash" when
            // navigating between protected pages. This tiny inline script runs the moment
            // the parser reaches it, before that first paint: if this exact page was
            // decrypted earlier this session (or just hover-prefetched), its plaintext is
            // in sessionStorage, so we inject it now and the page paints with content on
            // the first frame. password.js sees the flag and skips re-injecting. The
            // plaintext is no more exposed than the derived key password.js already caches
            // in sessionStorage; both clear with the tab.
            sb.AppendLine($"<script>");
            sb.AppendLine($"(function(){{");
            sb.AppendLine($"    try {{");
            sb.AppendLine($"        var raw = sessionStorage.getItem('neko-pw-html:' + location.pathname);");
            sb.AppendLine($"        if (!raw) return;");
            sb.AppendLine($"        var cached = JSON.parse(raw);");
            sb.AppendLine($"        if (!cached || cached.salt !== '{encryptionResult.Salt}' || typeof cached.html !== 'string') return;");
            sb.AppendLine($"        var c = document.getElementById('content-container');");
            sb.AppendLine($"        if (!c) return;");
            sb.AppendLine($"        c.innerHTML = cached.html;");
            sb.AppendLine($"        window.__nekoProtectedInjected = true;");
            sb.AppendLine($"    }} catch (e) {{}}");
            sb.AppendLine($"}})();");
            sb.AppendLine($"</script>");

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
            if (_isBlogMode)
            {
                RenderBlogFooter(sb);
                return;
            }

            sb.AppendLine("                    <footer class=\"mt-12 py-6 border-t border-gray-200 dark:border-gray-800 text-sm text-gray-500 dark:text-gray-400 flex flex-col md:flex-row justify-between items-center not-prose\">");
            sb.AppendLine($"                        <div>{ResolveCopyright()}</div>");

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

        // Marketing-style footer used in blog mode: the top-nav links repeated as
        // a centred row above the copyright line, matching the curiosity.ai feel.
        private void RenderBlogFooter(StringBuilder sb)
        {
            sb.AppendLine("                    <footer class=\"mt-16 pt-8 border-t border-gray-200 dark:border-gray-800 text-sm text-gray-500 dark:text-gray-400 flex flex-col items-center gap-4 not-prose\">");

            var navLinks = _config.Links?.Where(l => !string.IsNullOrEmpty(l.Text) && !string.IsNullOrEmpty(l.Link)).ToList();
            if (navLinks != null && navLinks.Count > 0)
            {
                sb.AppendLine("                        <nav class=\"flex flex-wrap justify-center gap-x-6 gap-y-2 font-medium\">");
                foreach (var link in navLinks)
                {
                    var target = !string.IsNullOrEmpty(link.Target) ? $" target=\"{NormalizeTarget(link.Target)}\"" : "";
                    var rel = target.Contains("_blank") ? " rel=\"noopener noreferrer\"" : "";
                    sb.AppendLine($"                            <a href=\"{link.Link}\"{target}{rel} class=\"hover:text-gray-900 dark:hover:text-gray-100 transition-colors\">{link.Text}</a>");
                }
                sb.AppendLine("                        </nav>");
            }

            sb.AppendLine($"                        <div class=\"text-center text-xs\">{ResolveCopyright()}</div>");
            sb.AppendLine("                    </footer>");
        }

        // The configured copyright line (with `{{ year }}` expanded), or a sane
        // default built from the branding title.
        private string ResolveCopyright()
        {
            var text = _config.Footer?.Copyright;
            if (string.IsNullOrEmpty(text))
            {
                return $"&copy; {System.DateTime.Now.Year} {_config.Branding.Title}. All rights reserved.";
            }
            return text.Replace("{{ year }}", System.DateTime.Now.Year.ToString())
                       .Replace("{{year}}", System.DateTime.Now.Year.ToString());
        }

        // Full-width marketing footer used in blog mode when `footer.columns` /
        // `social` / `badges` are configured. Mirrors the curiosity.ai footer: a
        // dark panel with rounded top corners that breaks out of the content
        // column, a brand/social/badges block, link columns, and a copyright bar.
        private void RenderBlogMegaFooter(StringBuilder sb, string currentUrl)
        {
            var footer = _config.Footer;

            // Break out of #main-scroll's p-4 / md:p-8 padding to span the full pane
            // width edge-to-edge. In blog mode the content row is un-capped (see
            // Generate), so `main` is full-width and this footer reaches both edges —
            // the curiosity.ai full-bleed panel. It uses the base ink colour (#1f1f1f).
            sb.AppendLine($"                <footer style=\"background-color:{BlogBaseColor()}\" class=\"not-prose -mx-4 md:-mx-8 -mb-4 md:-mb-8 mt-12 md:mt-16 px-6 md:px-10 text-white rounded-t-[2rem] md:rounded-t-[2.75rem]\">");
            // Inner content is centred at the same width as the header/content. The
            // horizontal padding lives on the panel (above), so at the capped width the
            // content sits flush at the container edge — matching curiosity.ai's footer.
            var footerInnerWidth = LayoutMaxWidthClass();
            if (string.IsNullOrEmpty(footerInnerWidth)) footerInnerWidth = "max-w-screen-2xl";
            sb.AppendLine($"                    <div class=\"{footerInnerWidth} mx-auto pt-16 md:pt-20 pb-[43px]\">");
            // Brand block + one column per link group, laid out as a flex row with a
            // 32px gap (the curiosity.ai footer is content-width columns in a row, not
            // an equal grid). Stacks to a two-up grid on mobile.
            sb.AppendLine($"                        <div class=\"grid grid-cols-2 gap-8 md:flex md:flex-row md:items-start md:gap-8\">");

            // Brand / social / badges block. Shares the row width equally with the
            // link columns on desktop (all five columns are flex-1, ~230px each on
            // curiosity.ai); spans both cells on the mobile two-up grid.
            sb.AppendLine("                            <div class=\"col-span-2 md:flex-1 flex flex-col gap-6\">");
            var footerLogo = !string.IsNullOrEmpty(footer.Logo)
                ? footer.Logo
                : (!string.IsNullOrEmpty(_config.Branding.LogoDark) ? _config.Branding.LogoDark : _config.Branding.Logo);
            if (!string.IsNullOrEmpty(footerLogo))
            {
                var logoUrl = ResolveLogoPath(currentUrl, footerLogo);
                sb.AppendLine($"                                <img src=\"{logoUrl}\" alt=\"{EscapeHtmlAttr(_config.Branding.Title)}\" class=\"h-5 w-auto self-start\">");
            }
            else
            {
                sb.AppendLine($"                                <div class=\"text-xl font-bold self-start\">{_config.Branding.Title}</div>");
            }
            if (!string.IsNullOrEmpty(footer.Tagline))
            {
                sb.AppendLine($"                                <p class=\"text-sm text-[#b5bdc5] max-w-xs\">{footer.Tagline}</p>");
            }

            if (footer.Social != null && footer.Social.Count > 0)
            {
                // Image-icon social links (brand SVGs) render bare, like curiosity.ai —
                // 18px glyphs, no chip, slightly wider gap. UIcon-name social links keep
                // the muted round chip.
                var socialImages = footer.Social.All(s => !string.IsNullOrEmpty(s.Icon) && Neko.Builder.IconHelper.IsImagePath(s.Icon));
                var socialGap = socialImages ? "gap-4" : "gap-3";
                sb.AppendLine($"                                <div class=\"flex items-center {socialGap}\">");
                foreach (var s in footer.Social)
                {
                    if (string.IsNullOrEmpty(s.Link)) continue;
                    var label = EscapeHtmlAttr(s.Label ?? string.Empty);
                    if (Neko.Builder.IconHelper.IsImagePath(s.Icon))
                    {
                        sb.AppendLine($"                                    <a href=\"{s.Link}\" target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"{label}\" class=\"inline-flex items-center justify-center hover:opacity-80 transition-opacity\"><img src=\"{EscapeHtmlAttr(s.Icon)}\" alt=\"{label}\" class=\"w-[18px] h-[18px] object-contain\"></a>");
                    }
                    else
                    {
                        sb.AppendLine($"                                    <a href=\"{s.Link}\" target=\"_blank\" rel=\"noopener noreferrer\" aria-label=\"{label}\" class=\"inline-flex items-center justify-center w-9 h-9 rounded-full bg-white/5 text-gray-300 hover:bg-white/15 hover:text-white transition-colors\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(s.Icon)}\"></i></a>");
                    }
                }
                sb.AppendLine("                                </div>");
            }

            if (footer.Badges != null && footer.Badges.Count > 0)
            {
                sb.AppendLine("                                <div class=\"flex flex-col gap-8 mt-[17px]\">");
                foreach (var b in footer.Badges)
                {
                    sb.AppendLine("                                    <div class=\"flex items-start gap-2.5\">");
                    // A trust badge's icon is usually a real logo image (curiosity.ai
                    // renders them as ~28px circular thumbnails). A UIcon name falls back
                    // to the muted rounded chip.
                    if (Neko.Builder.IconHelper.IsImagePath(b.Icon))
                    {
                        sb.AppendLine($"                                        <img src=\"{EscapeHtmlAttr(b.Icon)}\" alt=\"\" class=\"w-7 h-7 shrink-0 object-cover rounded-full\">");
                    }
                    else
                    {
                        sb.AppendLine($"                                        <span class=\"flex items-center justify-center w-9 h-9 rounded-xl shrink-0 bg-white/5 text-gray-300\"><i class=\"{Neko.Builder.IconHelper.GetIconClass(b.Icon)}\"></i></span>");
                    }
                    sb.AppendLine("                                        <div class=\"leading-tight\">");
                    if (!string.IsNullOrEmpty(b.Title)) sb.AppendLine($"                                            <div class=\"text-sm font-medium text-[#f1f1f1]\">{b.Title}</div>");
                    if (!string.IsNullOrEmpty(b.Description)) sb.AppendLine($"                                            <div class=\"text-xs text-[#b5bdc5] mt-2.5\">{b.Description}</div>");
                    sb.AppendLine("                                        </div>");
                    sb.AppendLine("                                    </div>");
                }
                sb.AppendLine("                                </div>");
            }
            sb.AppendLine("                            </div>");

            // Link columns.
            if (footer.Columns != null)
            {
                foreach (var column in footer.Columns)
                {
                    // Each link column grows to claim an equal share of the row's
                    // leftover width (the brand block keeps its content width).
                    sb.AppendLine("                            <div class=\"md:flex-1\">");
                    if (!string.IsNullOrEmpty(column.Title))
                    {
                        sb.AppendLine($"                                <h3 class=\"text-[15px] font-semibold text-[#f1f1f1] capitalize mb-[15px] leading-[21px]\">{column.Title}</h3>");
                    }
                    sb.AppendLine("                                <ul class=\"space-y-[13px] list-none pl-0 m-0\">");
                    if (column.Links != null)
                    {
                        foreach (var link in column.Links)
                        {
                            if (string.IsNullOrEmpty(link.Text)) continue;
                            var href = link.Link ?? "#";
                            var target = !string.IsNullOrEmpty(link.Target) ? $" target=\"{NormalizeTarget(link.Target)}\"" : "";
                            var rel = target.Contains("_blank") ? " rel=\"noopener noreferrer\"" : "";
                            sb.AppendLine($"                                    <li><a href=\"{href}\"{target}{rel} class=\"text-[15px] leading-[21px] text-[#f1f1f1] hover:text-white transition-colors no-underline\">{link.Text}</a></li>");
                        }
                    }
                    sb.AppendLine("                                </ul>");
                    sb.AppendLine("                            </div>");
                }
            }

            sb.AppendLine("                        </div>");

            // Copyright bar — a small icon next to the line (curiosity.ai uses a
            // cookie glyph). The icon is configurable via footer.copyrightIcon (a
            // UIcon name or an image/SVG path, kept in the site's own assets);
            // defaults to the built-in cookie glyph so nothing site-specific is
            // baked into the engine.
            sb.AppendLine("                        <div class=\"mt-16 md:mt-[50px] pt-6 border-t border-white/5 text-sm text-[#b5bdc5] flex items-center gap-2\">");
            var copyrightIcon = footer.CopyrightIcon;
            if (string.IsNullOrEmpty(copyrightIcon))
            {
                sb.AppendLine("                            <i class=\"fi fi-rr-cookie text-base\"></i>");
            }
            else if (Neko.Builder.IconHelper.IsImagePath(copyrightIcon))
            {
                sb.AppendLine($"                            <img src=\"{EscapeHtmlAttr(copyrightIcon)}\" alt=\"\" class=\"w-6 h-6 shrink-0\">");
            }
            else
            {
                sb.AppendLine($"                            <i class=\"{Neko.Builder.IconHelper.GetIconClass(copyrightIcon)} text-base\"></i>");
            }
            sb.AppendLine($"                            <span>{ResolveCopyright()}</span>");
            sb.AppendLine("                        </div>");

            sb.AppendLine("                    </div>");
            sb.AppendLine("                </footer>");
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

        // The TOC rail for a protected page: same width as a normal rail (so the
        // layout doesn't shift), but it ships no heading text. The list is empty and
        // the inner block is hidden; password.js fills the list from the decrypted
        // headings and unhides it together with the body.
        private void RenderProtectedTocShell(StringBuilder sb, ParsedDocument document, string currentUrl)
        {
            sb.AppendLine("            <aside id=\"toc-sidebar\" class=\"w-64 hidden xl:block shrink-0 overflow-y-auto border-l border-gray-200 dark:border-gray-800 p-6\">");
            sb.AppendLine("                <div class=\"sticky top-0\">");
            sb.AppendLine("                    <div id=\"toc-protected\" class=\"hidden\">");
            RenderPageLinks(sb, document, currentUrl);
            sb.AppendLine("                        <h5 class=\"text-xs font-semibold mb-4 text-gray-900 dark:text-gray-100 uppercase tracking-wider\">On this page</h5>");
            sb.AppendLine("                        <ul class=\"space-y-2.5 text-sm text-gray-500 dark:text-gray-400 border-l border-gray-200 dark:border-gray-800 relative\" id=\"toc-list\">");
            sb.AppendLine("                            <div id=\"toc-highlight\" class=\"absolute left-0 border-l-2 border-primary-600 dark:border-primary-400 transition-all duration-200 ease-in-out -ml-px\" style=\"top: 0; height: 0; opacity: 0;\"></div>");
            sb.AppendLine("                        </ul>");
            sb.AppendLine("                    </div>");
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
                    iconHtml = IconHelper.RenderIcon(link.Icon, "text-base");
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

            // In blog mode the header has no search box — it lives at the top of
            // the post list instead. Unlike docs mode (which opens a modal), the
            // blog searches inline: typing filters the index live and renders the
            // hits in place of the post grid (see initInlineSearch in search.js).
            if (_isBlogMode)
            {
                sb.AppendLine("<div class=\"not-prose mt-8 mb-6\">");
                sb.AppendLine("    <div class=\"relative\">");
                sb.AppendLine("        <i class=\"fi fi-rr-search absolute left-5 top-1/2 -translate-y-1/2 text-base text-gray-400 dark:text-gray-500 pointer-events-none\"></i>");
                sb.AppendLine("        <input id=\"neko-inline-search\" type=\"text\" autocomplete=\"off\" placeholder=\"Search the blog…\" aria-label=\"Search the blog\" class=\"w-full text-gray-900 dark:text-gray-100 placeholder-gray-500 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-full pl-12 pr-5 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 transition-shadow\">");
                sb.AppendLine("    </div>");
                sb.AppendLine("    <div id=\"neko-inline-search-results\" class=\"hidden mt-4 rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 divide-y divide-gray-100 dark:divide-gray-700/60 overflow-hidden\"></div>");
                sb.AppendLine("</div>");
            }

            var gridId = _isBlogMode ? " id=\"neko-blog-grid\"" : string.Empty;
            // Card grid mirroring curiosity.ai/resources/blog: a dark, rounded
            // tile per post with the cover image faded behind the content. The
            // title sits top-left with an up-right arrow top-right; the author
            // (left) and date (right) share a bottom row split by a hairline
            // rule. Colours use the blog palette inverted — the card is the ink
            // colour with bg-coloured text — so it tracks the theme, with the
            // curiosity.ai defaults (#1f1f1f / #f1f1f1) as hardcoded fallbacks
            // for docs-mode index pages where the palette vars are absent.
            sb.AppendLine($"<div{gridId} class=\"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-[15px] mt-8 not-prose\">");

            foreach (var post in posts)
            {
                var title = !string.IsNullOrEmpty(post.Doc.FrontMatter.Title) ? post.Doc.FrontMatter.Title : post.Doc.FrontMatter.Label;
                var date = post.Doc.FrontMatter.Date;
                var author = post.Doc.FrontMatter.Author;
                var cover = post.Doc.FrontMatter.Cover;
                var url = post.Url;

                sb.AppendLine($"<a href=\"{url}\" class=\"group relative flex flex-col justify-between overflow-hidden rounded-[14px] h-[244px] p-[22px] no-underline\" style=\"background-color:var(--blog-ink, #1f1f1f)\">");

                // Cover image, faded behind the content and zoomed slightly on hover.
                if (!string.IsNullOrEmpty(cover))
                {
                    sb.AppendLine($"    <img src=\"{cover}\" alt=\"{EscapeHtmlAttr(title)}\" class=\"absolute inset-0 w-full h-full object-cover rounded-[14px] opacity-30 transition-transform duration-500 group-hover:scale-110\">");
                }

                // Top row: title + up-right arrow.
                sb.AppendLine("    <div class=\"relative z-10 flex items-center gap-2.5\">");
                sb.AppendLine($"        <h3 class=\"flex-1 m-0 text-base font-medium leading-[1.4]\" style=\"color:var(--blog-bg, #f1f1f1)\">{title}</h3>");
                sb.AppendLine("        <i class=\"fi fi-rr-arrow-up-right shrink-0 text-[32px] leading-none\" style=\"color:var(--blog-bg, #f1f1f1)\"></i>");
                sb.AppendLine("    </div>");

                // Bottom row: author (left) + date (right), split by a hairline rule.
                if (!string.IsNullOrEmpty(author) || !string.IsNullOrEmpty(date))
                {
                    sb.AppendLine("    <div class=\"relative z-10 flex items-center justify-between gap-2 pt-2 border-t\" style=\"border-color:var(--blog-bg, #f1f1f1)\">");
                    sb.AppendLine($"        <span class=\"text-sm font-medium\" style=\"color:var(--blog-bg, #f1f1f1)\">{author}</span>");
                    if (!string.IsNullOrEmpty(date))
                    {
                        sb.AppendLine($"        <span class=\"text-sm font-medium\" style=\"color:var(--blog-bg, #f1f1f1)\">{date}</span>");
                    }
                    sb.AppendLine("    </div>");
                }

                sb.AppendLine("</a>");
            }

            sb.AppendLine("</div>");
        }

        private void RenderChangelogIndex(StringBuilder sb, List<(ParsedDocument Doc, string Url, string Version)> entries)
        {
            if (entries == null || entries.Count == 0) return;

            sb.AppendLine("<div class=\"neko-changelog mt-8 not-prose relative\">");

            // Timeline rail (the version headers' opaque backgrounds scroll over it).
            sb.AppendLine("    <div class=\"absolute top-0 bottom-0 left-2 w-px bg-gray-200 dark:bg-white/10\" aria-hidden=\"true\"></div>");

            foreach (var entry in entries)
            {
                var version = entry.Version;
                // A version file may carry an optional headline (frontmatter title/label),
                // e.g. "Ready for Production", shown next to the version badge.
                var title = !string.IsNullOrEmpty(entry.Doc.FrontMatter.Title) ? entry.Doc.FrontMatter.Title : entry.Doc.FrontMatter.Label;
                var date = entry.Doc.FrontMatter.Date;
                var link = entry.Doc.FrontMatter.Link;
                var html = entry.Doc.Html;
                var anchor = entry.Url != null && entry.Url.StartsWith("#") ? entry.Url.Substring(1) : null;

                sb.AppendLine($"    <section class=\"relative pl-8 scroll-mt-24\"{(string.IsNullOrEmpty(anchor) ? string.Empty : $" id=\"{EscapeHtmlAttr(anchor)}\"")}>");

                // Sentinel marking the header's natural top. An IntersectionObserver
                // watches it (see RenderChangelogStickyScript): once it scrolls above the
                // pane's top edge the header is pinned, and the `is-stuck` class is added.
                sb.AppendLine("        <div class=\"neko-cl-sentinel\" aria-hidden=\"true\"></div>");

                // Sticky version header. Styling (and the two visual states) live in the
                // page <style> under `.neko-changelog-version`:
                //   - in flow  -> a rounded, blurred, inset card (the default look);
                //   - `.is-stuck` -> the card styling drops and the full-bleed background
                //                    layer below spans the whole content pane.
                // The negative sticky `top` cancels `#main-scroll`'s own top padding
                // (`p-4 md:p-8`) so the pinned header sits flush against the pane top.
                sb.AppendLine("        <div class=\"neko-changelog-version\">");

                // Full-bleed glassy background, shown only when stuck. It extends 50vw
                // past the column on each side and is clipped to the pane edges by
                // `#main-scroll`'s `overflow-x-clip`.
                sb.AppendLine("            <span class=\"neko-cl-bleed\" aria-hidden=\"true\"></span>");

                // Timeline dot, sitting on the rail to the left of the header.
                sb.AppendLine("            <span class=\"absolute -left-6 top-1/2 -translate-y-1/2 w-3 h-3 rounded-full ring-4 ring-white dark:ring-gray-900 bg-primary-600\" aria-hidden=\"true\"></span>");

                RenderChangelogVersionBadge(sb, version, link);

                if (!string.IsNullOrEmpty(title))
                {
                    sb.AppendLine($"            <h2 class=\"text-xl font-bold text-gray-900 dark:text-gray-100 m-0\">{title}</h2>");
                }
                if (!string.IsNullOrEmpty(date))
                {
                    sb.AppendLine($"            <time class=\"text-sm font-medium text-gray-400 dark:text-gray-500 ml-auto\">{EscapeHtmlAttr(date)}</time>");
                }
                sb.AppendLine("        </div>");

                // Release body. Sections (markdown H1s) and `::: change` entries are
                // styled by the .neko-changelog-body rules in the page <style>.
                sb.AppendLine("        <div class=\"neko-changelog-body prose dark:prose-invert max-w-none prose-sm prose-headings:font-semibold prose-a:text-primary-600 pt-2 pb-10\">");
                sb.AppendLine(html);
                sb.AppendLine("        </div>");

                sb.AppendLine("    </section>");
            }

            sb.AppendLine("</div>");
        }

        // The version pill in a changelog header: a linked version badge (to the
        // release/reference link when provided) with a copy-to-clipboard button.
        // Mirrors the markup of the `[!version-badge]` component.
        private void RenderChangelogVersionBadge(StringBuilder sb, string version, string link)
        {
            sb.AppendLine("            <span class=\"neko-version-badge inline-flex items-center gap-1.5 rounded-lg border border-gray-200 dark:border-white/10 bg-gray-50 dark:bg-white/5 pl-2.5 pr-1.5 py-1\">");

            var hasLink = !string.IsNullOrEmpty(link);
            if (hasLink)
            {
                sb.AppendLine($"                <a href=\"{EscapeHtmlAttr(link)}\" class=\"no-underline group/vb\" target=\"_blank\" rel=\"noopener\">");
            }
            sb.AppendLine($"                <span class=\"font-mono font-bold text-base text-gray-800 dark:text-gray-100{(hasLink ? " group-hover/vb:text-primary-600 dark:group-hover/vb:text-primary-400 transition-colors" : string.Empty)}\">{EscapeHtmlAttr(version)}</span>");
            if (hasLink)
            {
                sb.AppendLine("                </a>");
            }

            sb.AppendLine($"                <button type=\"button\" class=\"neko-copy-btn inline-flex items-center justify-center p-1 rounded text-gray-400 hover:text-gray-700 dark:text-gray-500 dark:hover:text-gray-200 hover:bg-gray-200/70 dark:hover:bg-white/10 transition-colors\" data-copy=\"{EscapeHtmlAttr(version)}\" title=\"Copy version\" aria-label=\"Copy version\"><i class=\"fi fi-rr-copy text-xs\"></i></button>");
            sb.AppendLine("            </span>");
        }
    }
}
