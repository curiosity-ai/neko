using Neko.Configuration;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        // Navigation icons are hidden by default; a site opts back in per-context
        // via the `nav:` block in neko.yml. `icons` covers the header (top-level
        // links + dropdown triggers), `dropdownIcons` the flyout menu items, and
        // `pivotIcons` the contextual pivot tab bar.
        private bool _showHeaderIcons => _config.Nav?.HeaderIcons ?? false;
        private bool _showDropdownIcons => _config.Nav?.DropdownIcons ?? false;
        private bool _showPivotIcons => _config.Nav?.PivotIcons ?? false;

        // `blog` mode renders the marketing-site chrome (borderless header,
        // logo-as-wordmark, pill CTAs). Anything else is the documentation look.
        private bool _isBlogMode => string.Equals(_config.Mode, "blog", System.StringComparison.OrdinalIgnoreCase);

        // Blog mode is light-only by default (the curiosity.ai look). Dark mode is
        // opt-in: a blog enables it — and gets the theme toggle back — by defining
        // a `theme.dark` palette in neko.yml. Without it, blog mode locks to light.
        private bool _blogDarkEnabled => _isBlogMode && _config.Theme?.Dark != null && _config.Theme.Dark.Count > 0;

        private void RenderBanner(StringBuilder sb)
        {
            if (_config.Banner == null || !_config.Banner.Visible || string.IsNullOrEmpty(_config.Banner.Text)) return;

            var banner = _config.Banner;
            var bannerId = banner.Id ?? "neko-banner";
            var bg = !string.IsNullOrEmpty(banner.Background) ? banner.Background : "bg-indigo-600";
            var color = !string.IsNullOrEmpty(banner.Color) ? banner.Color : "text-white";

            sb.AppendLine($"    <div id=\"{bannerId}\" class=\"{bg} {color} relative isolate flex items-center gap-x-6 overflow-hidden px-6 py-2.5 sm:px-3.5 sm:before:flex-1 hidden z-50\">");
            sb.AppendLine("        <div class=\"flex flex-wrap items-center gap-x-4 gap-y-2\">");
            sb.AppendLine($"            <p class=\"text-sm leading-6\">{banner.Text}</p>");
            if (!string.IsNullOrEmpty(banner.Link))
            {
                var linkText = !string.IsNullOrEmpty(banner.LinkText) ? banner.LinkText : "Read more";
                sb.AppendLine($"            <a href=\"{banner.Link}\" class=\"flex-none rounded-full bg-gray-900 px-3.5 py-1 text-sm font-semibold text-white shadow-sm hover:bg-gray-700 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-gray-900\">{linkText} <span aria-hidden=\"true\">&rarr;</span></a>");
            }
            sb.AppendLine("        </div>");
            if (banner.Dismissible)
            {
                sb.AppendLine("        <div class=\"flex flex-1 justify-end\">");
                sb.AppendLine($"            <button type=\"button\" class=\"-m-3 p-3 focus-visible:outline-offset-[-4px]\" onclick=\"dismissBanner('{bannerId}')\">");
                sb.AppendLine("                <span class=\"sr-only\">Dismiss</span>");
                sb.AppendLine("                <i class=\"fi fi-rr-cross-small text-xl\"></i>");
                sb.AppendLine("            </button>");
                sb.AppendLine("        </div>");
            }
            sb.AppendLine("    </div>");
        }

        private void RenderNavbar(StringBuilder sb, string currentUrl)
        {
            // Blog mode mirrors curiosity.ai: a light, borderless header with no
            // drop shadow that blends into the marketing-style page background
            // (driven by the `theme.base` palette).
            if (_isBlogMode)
            {
                // The header is pinned as an overlay bar at the top, with the page
                // content scrolling underneath it (`#main-scroll` is padded to clear
                // it). It is filled with the base page colour (#f1f1f1) so the bar reads
                // as a solid topbar rather than letting scrolled content show through.
                sb.AppendLine("    <header style=\"background-color:var(--blog-bg)\" class=\"absolute top-0 inset-x-0 h-20 flex items-center px-6 z-30\">");
            }
            else
            {
                sb.AppendLine("    <header class=\"h-16 shrink-0 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shadow-sm flex items-center px-6 z-30\">");
            }

            var maxWidthClass = LayoutMaxWidthClass();
            var innerWidthClass = string.IsNullOrEmpty(maxWidthClass) ? string.Empty : $" {maxWidthClass}";
            // Blog mode insets the header content inside the capped row (curiosity.ai
            // sits its bar ~44px in from the 1280px container edges), lining the logo
            // up over the footer's columns.
            var innerPadClass = _isBlogMode ? " md:px-11" : string.Empty;
            sb.AppendLine($"        <div class=\"flex items-center justify-between w-full{innerWidthClass}{innerPadClass}\">");

            sb.AppendLine("        <div class=\"flex items-center gap-4\">");

            if (_config.Layout.Sidebar)
            {
                sb.AppendLine("            <button id=\"mobile-menu-btn\" class=\"md:hidden text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none\">");
                sb.AppendLine("                <i class=\"fi fi-rr-menu-burger text-xl\"></i>");
                sb.AppendLine("            </button>");
            }

            RenderNavbarBrand(sb, currentUrl);

            // Blog mode clusters the nav links immediately to the right of the logo
            // (the curiosity.ai layout). Docs mode keeps them as a separate centred
            // group between the brand and the header actions.
            if (_isBlogMode)
            {
                RenderNavbarLinks(sb);
            }
            sb.AppendLine("        </div>");

            if (!_isBlogMode)
            {
                RenderNavbarLinks(sb);
            }
            RenderNavbarActions(sb);

            // Blog mode collapses its marketing nav into a hamburger on mobile (below
            // `md`) — the desktop links/CTAs are `hidden md:flex`, so on small screens
            // this button is the only way to reach them. The desktop layout is left
            // exactly as before. Docs mode has its own sidebar-driven mobile menu (see
            // the `mobile-menu-btn` above).
            if (_isBlogMode)
            {
                RenderBlogMobileMenuButton(sb);
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </header>");

            if (_isBlogMode)
            {
                RenderBlogMobileMenu(sb);
            }
        }

        // The hamburger trigger for the blog-mode mobile menu. Sits at the right of
        // the header row (the logo stays pinned left) and only shows below `md`.
        private void RenderBlogMobileMenuButton(StringBuilder sb)
        {
            sb.AppendLine("        <button id=\"blog-menu-btn\" class=\"md:hidden flex items-center justify-center p-2 -mr-2 shrink-0 focus:outline-none\" style=\"color:var(--blog-ink)\" aria-label=\"Open menu\" aria-expanded=\"false\">");
            sb.AppendLine("            <i class=\"fi fi-rr-menu-burger text-xl\"></i>");
            sb.AppendLine("            <i class=\"fi fi-rr-cross-small text-2xl hidden\"></i>");
            sb.AppendLine("        </button>");
        }

        // The blog-mode mobile menu panel: a full-width drop-down (below the fixed
        // header) holding the nav links and CTA pills, stacked. Hidden by default and
        // toggled by the hamburger; only ever visible below `md`.
        private void RenderBlogMobileMenu(StringBuilder sb)
        {
            sb.AppendLine("    <div id=\"blog-mobile-menu\" class=\"md:hidden hidden fixed inset-x-0 top-20 bottom-0 z-20 overflow-y-auto px-6 py-6\" style=\"background-color:var(--blog-bg);color:var(--blog-ink)\">");

            if (_config.Links != null && _config.Links.Count > 0)
            {
                sb.AppendLine("        <nav class=\"flex flex-col text-base font-medium\">");
                foreach (var link in _config.Links)
                {
                    if (link.Items != null && link.Items.Count > 0)
                    {
                        // Flyout groups flatten to a section header plus its items so the
                        // full marketing nav is reachable on mobile without hover menus.
                        sb.AppendLine($"            <div class=\"pt-4 pb-1 text-sm font-semibold uppercase tracking-wide opacity-60\">{link.Text}</div>");
                        foreach (var item in link.Items)
                        {
                            var itemHref = item.Link ?? "#";
                            var itemTarget = !string.IsNullOrEmpty(item.Target) ? $" target=\"{item.Target}\"" : "";
                            sb.AppendLine($"            <a href=\"{itemHref}\"{itemTarget} class=\"py-2.5 pl-3 hover:text-primary-600 dark:hover:text-primary-400 transition-colors\">{item.Text}</a>");
                        }
                    }
                    else
                    {
                        var href = link.Link ?? "#";
                        var target = !string.IsNullOrEmpty(link.Target) ? $" target=\"{link.Target}\"" : "";
                        sb.AppendLine($"            <a href=\"{href}\"{target} class=\"py-3 hover:text-primary-600 dark:hover:text-primary-400 transition-colors\">{link.Text}</a>");
                    }
                }
                sb.AppendLine("        </nav>");
            }

            if (_config.Actions != null && _config.Actions.Count > 0)
            {
                sb.AppendLine("        <div class=\"mt-6 flex flex-col gap-3\">");
                foreach (var action in _config.Actions)
                {
                    if (string.IsNullOrEmpty(action.Text)) continue;

                    var href = action.Link ?? "#";
                    var target = !string.IsNullOrEmpty(action.Target) ? $" target=\"{NormalizeTarget(action.Target)}\"" : "";
                    var rel = target.Contains("_blank") ? " rel=\"noopener noreferrer\"" : "";
                    var isOutline = string.Equals(action.Variant, "outline", System.StringComparison.OrdinalIgnoreCase);
                    var iconHtml = string.IsNullOrEmpty(action.Icon) ? "" : $"<i class=\"{Neko.Builder.IconHelper.GetIconClass(action.Icon)}\"></i>";

                    var blogClass = "inline-flex items-center justify-center gap-2 rounded-full px-[14px] py-2.5 text-sm font-medium transition-opacity hover:opacity-80 whitespace-nowrap";
                    var style = isOutline
                        ? "box-shadow:inset 0 0 0 1px var(--blog-ink);color:var(--blog-ink)"
                        : "background-color:var(--blog-ink);color:var(--blog-bg)";
                    sb.AppendLine($"            <a href=\"{href}\"{target}{rel} class=\"{blogClass}\" style=\"{style}\">{iconHtml}{action.Text}</a>");
                }
                sb.AppendLine("        </div>");
            }

            sb.AppendLine("    </div>");
        }

        private void RenderNavbarBrand(StringBuilder sb, string currentUrl)
        {
            // In blog mode the logo image is a full wordmark, so pairing it with
            // the branding title would duplicate the name (the "Curiosity
            // Curiosity" overlap). There the logo links home on its own and the
            // title is kept only for screen readers. In docs mode the logo is
            // typically an icon next to the visible title, so both are shown.
            var logoIsWordmark = _isBlogMode && !string.IsNullOrEmpty(_config.Branding.Logo);

            if (logoIsWordmark)
            {
                // `shrink-0` so the crowded marketing nav never squeezes the wordmark
                // (flexbox would otherwise shrink the image before the text links).
                sb.AppendLine($"            <a href=\"/index\" class=\"flex items-center shrink-0\" aria-label=\"{_config.Branding.Title}\">");
                RenderNavbarLogoImages(sb, currentUrl);
                sb.AppendLine("            </a>");
                return;
            }

            if (!string.IsNullOrEmpty(_config.Branding.Logo))
            {
                RenderNavbarLogoImages(sb, currentUrl);
            }
            else if (!string.IsNullOrEmpty(_config.Branding.Icon))
            {
                sb.AppendLine($"            <i class=\"{_config.Branding.Icon} text-2xl text-primary-600 dark:text-primary-400\"></i>");
            }

            sb.AppendLine($"            <a href=\"/index\" class=\"font-bold text-xl hover:text-primary-600 transition-colors\">{_config.Branding.Title}</a>");
        }

        private void RenderNavbarLogoImages(StringBuilder sb, string currentUrl)
        {
            // Blog mode uses the logo as a compact wordmark (curiosity.ai sizes it at
            // ~21px tall); docs mode pairs a larger icon-logo with the title.
            // `shrink-0` keeps the logo at its natural width inside the flex header
            // so a crowded nav can never compress it (the curiosity.ai wordmark bug).
            var logoH = _isBlogMode ? "h-[21px] shrink-0" : "h-8";
            string logoUrl = ResolveLogoPath(currentUrl, _config.Branding.Logo);
            sb.AppendLine($"                <img src=\"{logoUrl}\" class=\"{logoH} w-auto dark:hidden\">");
            if (!string.IsNullOrEmpty(_config.Branding.LogoDark))
            {
                string logoDarkUrl = ResolveLogoPath(currentUrl, _config.Branding.LogoDark);
                sb.AppendLine($"                <img src=\"{logoDarkUrl}\" class=\"{logoH} w-auto hidden dark:block\">");
            }
            else
            {
                sb.AppendLine($"                <img src=\"{logoUrl}\" class=\"{logoH} w-auto hidden dark:block\">");
            }
        }

        // Resolves a branding logo path. Walks up from the current page directory looking
        // for the asset so logos can be referenced from any depth without breaking when
        // the docs are mounted under a non-root route prefix.
        private string ResolveLogoPath(string currentUrl, string logo)
        {
            if (string.IsNullOrEmpty(logo) || logo.Contains("://") || logo.StartsWith("#")) return logo;

            var inputDir = System.IO.Path.GetFullPath(_config.Input);
            var currentDir = inputDir;

            if (!string.IsNullOrEmpty(currentUrl))
            {
                var fullUrlPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(inputDir, currentUrl.TrimStart('/')));
                currentDir = System.IO.Path.GetDirectoryName(fullUrlPath) ?? inputDir;
            }

            string WithRoutePrefix(string rootRelative)
            {
                var prefix = SiteBuilder.CurrentRoutePrefix;
                if (string.IsNullOrEmpty(prefix)) return rootRelative;
                if (rootRelative.StartsWith(prefix + "/", System.StringComparison.Ordinal) || rootRelative == prefix) return rootRelative;
                return prefix + rootRelative;
            }

            var trimmedLogo = logo.TrimStart('/');
            var targetPath = System.IO.Path.Combine(currentDir, trimmedLogo);

            if (System.IO.File.Exists(targetPath)) return WithRoutePrefix("/" + System.IO.Path.GetRelativePath(inputDir, targetPath).Replace("\\", "/"));

            var fileName = System.IO.Path.GetFileName(trimmedLogo);
            var urlDir = System.IO.Path.GetDirectoryName(trimmedLogo)?.Replace('\\', '/');

            var searchDir = currentDir;
            string foundPath = null;

            while (searchDir.StartsWith(inputDir, System.StringComparison.OrdinalIgnoreCase))
            {
                string candidateDir = string.IsNullOrEmpty(urlDir) ? System.IO.Path.Combine(searchDir, "assets") : System.IO.Path.Combine(searchDir, urlDir);
                var assetPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(candidateDir, fileName));

                if (System.IO.File.Exists(assetPath))
                {
                    foundPath = assetPath;
                    break;
                }

                var parent = System.IO.Directory.GetParent(searchDir);
                if (parent == null) break;
                searchDir = parent.FullName;
            }

            if (foundPath != null)
            {
                return WithRoutePrefix("/" + System.IO.Path.GetRelativePath(inputDir, foundPath).Replace("\\", "/"));
            }

            return logo;
        }

        private void RenderNavbarLinks(StringBuilder sb)
        {
            // Blog mode inks the nav links with the base colour (near-black on
            // curiosity.ai); docs use the muted grey. On mobile (below `md`) the links
            // collapse into the hamburger menu; the desktop layout is unchanged.
            if (_isBlogMode)
            {
                sb.AppendLine("        <div class=\"hidden md:flex items-center gap-[22px] text-sm font-medium md:ml-1.5\" style=\"color:var(--blog-ink)\">");
            }
            else
            {
                sb.AppendLine("        <div class=\"hidden md:flex items-center gap-6 text-sm font-medium text-gray-600 dark:text-gray-300\">");
            }
            if (_config.Links != null)
            {
                foreach (var link in _config.Links)
                {
                    if (link.Items != null && link.Items.Count > 0)
                    {
                        RenderNavbarFlyoutLink(sb, link);
                    }
                    else
                    {
                         var href = link.Link ?? "#";
                         var iconHtml = (!_showHeaderIcons || string.IsNullOrEmpty(link.Icon)) ? "" : $"<i class=\"{Neko.Builder.IconHelper.GetIconClass(link.Icon)} mr-2\"></i>";
                         var target = !string.IsNullOrEmpty(link.Target) ? $" target=\"{link.Target}\"" : "";
                         // Blog mode sizes links via the container (text-[15px]); docs
                         // keep the tight text-sm/3 on each link. Inheriting in blog mode
                         // also keeps plain links the same size as flyout-link triggers.
                         var sizeClass = _isBlogMode ? "" : " text-sm/3";
                         sb.AppendLine($"            <a href=\"{href}\"{target} class=\"hover:text-primary-600 dark:hover:text-primary-400 transition-colors flex items-center{sizeClass}\">{iconHtml}{link.Text}</a>");
                    }
                }
            }
            sb.AppendLine("        </div>");
        }

        private void RenderNavbarFlyoutLink(StringBuilder sb, LinkConfig link)
        {
            sb.AppendLine($"            <div class=\"relative group z-50\">");
            // Blog mode mirrors curiosity.ai's dropdown trigger: an 8px gap and a flat
            // 10×5 chevron SVG (the UIcon glyph rendered ~2px narrower, drifting the
            // links). curiosity.ai renders that chevron in a muted grey (~#b6bec5),
            // not the near-black nav ink, so we fade it with `opacity-25` — ~25% of the
            // #1f1f1f ink over the #f1f1f1 page reads as that grey, and it stays
            // theme-agnostic instead of hard-coding a colour. Docs keeps the UIcon
            // caret with the tighter gap.
            var triggerGap = _isBlogMode ? "gap-2.5" : "gap-1";
            sb.AppendLine($"                <button class=\"flex items-center {triggerGap} hover:text-primary-600 dark:hover:text-primary-400 transition-colors focus:outline-none\">");
            if (_showHeaderIcons && !string.IsNullOrEmpty(link.Icon))
            {
                sb.AppendLine($"                    <i class=\"{Neko.Builder.IconHelper.GetIconClass(link.Icon)}\"></i>");
            }
            sb.AppendLine($"                    <span>{link.Text}</span>");
            if (_isBlogMode)
            {
                sb.AppendLine($"                    <svg width=\"10\" height=\"5\" viewBox=\"0 0 10 6\" fill=\"none\" class=\"shrink-0 opacity-25 transition-transform group-hover:rotate-180\" aria-hidden=\"true\"><path d=\"M1 1.5 5 4.5 9 1.5\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>");
            }
            else
            {
                sb.AppendLine($"                    <i class=\"fi fi-rr-angle-small-down transition-transform group-hover:rotate-180\"></i>");
            }
            sb.AppendLine($"                </button>");
            sb.AppendLine($"                <div class=\"absolute -left-8 top-full mt-3 w-screen max-w-md overflow-hidden rounded-3xl bg-white dark:bg-gray-800 shadow-lg ring-1 ring-gray-900/5 dark:ring-gray-700 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 ease-out z-50 delay-200 group-hover:delay-0\">");
            sb.AppendLine($"                    <div class=\"p-4\">");
            foreach (var item in link.Items)
            {
                var itemHref = item.Link ?? "#";
                var itemTarget = !string.IsNullOrEmpty(item.Target) ? $" target=\"{item.Target}\"" : "";
                sb.AppendLine($"                        <div class=\"group relative flex items-center gap-x-6 rounded-lg p-4 text-sm leading-6 hover:bg-gray-50 dark:hover:bg-gray-700/50\">");
                if (_showDropdownIcons)
                {
                    sb.AppendLine($"                            <div class=\"flex h-11 w-11 flex-none items-center justify-center rounded-lg bg-gray-50 dark:bg-gray-700 group-hover:bg-white dark:group-hover:bg-gray-600\">");
                    if (!string.IsNullOrEmpty(item.Icon))
                    {
                        sb.AppendLine($"                                <i class=\"{Neko.Builder.IconHelper.GetIconClass(item.Icon)} text-gray-600 dark:text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400\"></i>");
                    }
                    else
                    {
                        sb.AppendLine($"                                <i class=\"fi fi-rr-arrow-small-right text-gray-600 dark:text-gray-400 group-hover:text-primary-600 dark:group-hover:text-primary-400\"></i>");
                    }
                    sb.AppendLine($"                            </div>");
                }
                sb.AppendLine($"                            <div class=\"flex-auto\">");
                sb.AppendLine($"                                <a href=\"{itemHref}\"{itemTarget} class=\"block font-semibold text-gray-900 dark:text-gray-100 text-sm/3\">");
                sb.AppendLine($"                                    {item.Text}");
                sb.AppendLine($"                                    <span class=\"absolute inset-0\"></span>");
                sb.AppendLine($"                                </a>");
                if (!string.IsNullOrEmpty(item.Description))
                {
                    sb.AppendLine($"                                <p class=\"mt-1 text-gray-600 dark:text-gray-400\">{item.Description}</p>");
                }
                sb.AppendLine($"                            </div>");
                sb.AppendLine($"                        </div>");
            }
            sb.AppendLine($"                    </div>");

            if (link.FooterItems != null && link.FooterItems.Count > 0)
            {
                 sb.AppendLine($"                    <div class=\"grid grid-cols-2 divide-x divide-gray-900/5 dark:divide-gray-700 bg-gray-50 dark:bg-gray-700/50\">");
                 foreach (var footerItem in link.FooterItems)
                 {
                     var footerHref = footerItem.Link ?? "#";
                     var footerTarget = !string.IsNullOrEmpty(footerItem.Target) ? $" target=\"{footerItem.Target}\"" : "";
                     sb.AppendLine($"                        <a href=\"{footerHref}\"{footerTarget} class=\"flex items-center justify-center gap-x-2.5 p-3 text-sm font-semibold leading-6 text-gray-900 dark:text-gray-100 hover:bg-gray-100 dark:hover:bg-gray-700\">");
                     if (_showDropdownIcons && !string.IsNullOrEmpty(footerItem.Icon))
                     {
                         sb.AppendLine($"                            <i class=\"{Neko.Builder.IconHelper.GetIconClass(footerItem.Icon)} text-gray-400 dark:text-gray-500\"></i>");
                     }
                     sb.AppendLine($"                            {footerItem.Text}");
                     sb.AppendLine($"                        </a>");
                 }
                 sb.AppendLine($"                    </div>");
            }
            sb.AppendLine($"                </div>");
            sb.AppendLine($"            </div>");
        }

        private void RenderNavbarActions(StringBuilder sb)
        {
            // Blog mode sits its CTA pills closer together (curiosity.ai uses ~10px);
            // docs keeps the wider gap for its search/history/toggle row.
            var actionsGap = _isBlogMode ? "gap-2.5" : "gap-4";
            sb.AppendLine($"        <div class=\"flex items-center {actionsGap} hidden md:flex\">");
            // In blog mode the search box moves out of the header to the top of the
            // post list (see RenderContentSearchBar); docs keep it in the header.
            if (!_isBlogMode)
            {
                sb.AppendLine("            <button onclick=\"openSearch()\" class=\"flex items-center gap-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 border border-transparent rounded-md px-4 py-2 transition-colors focus:ring-2 focus:ring-primary-500 w-64 justify-between\">");
                sb.AppendLine("                <div class=\"flex items-center gap-2\">");
                sb.AppendLine("                    <i class=\"fi fi-rr-search text-sm\"></i>");
                sb.AppendLine("                    <span class=\"text-sm font-medium\">Search</span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("                <kbd class=\"hidden lg:inline text-xs bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded px-1.5 py-0.5 text-gray-500 dark:text-gray-400\">⌘K</kbd>");
                sb.AppendLine("            </button>");
            }
            // The recent-pages history (clock) is documentation chrome and is not
            // part of the curiosity.ai marketing header, so it is omitted in blog mode.
            if (!_isBlogMode)
            {
                sb.AppendLine("            <div class=\"relative\">");
                sb.AppendLine("                <button id=\"history-btn\" onclick=\"toggleHistory()\" class=\"flex items-center justify-center text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors focus:ring-2 focus:ring-primary-500\">");
                sb.AppendLine("                    <i class=\"fi fi-rr-clock text-lg\"></i>");
                sb.AppendLine("                </button>");
                sb.AppendLine("                <div id=\"history-popup\" class=\"absolute right-0 top-full mt-2 w-64 bg-white dark:bg-gray-800 rounded-md shadow-lg py-1 z-50 hidden border border-gray-200 dark:border-gray-700\">");
                sb.AppendLine("                    <div class=\"px-4 py-2 border-b border-gray-200 dark:border-gray-700\">");
                sb.AppendLine("                        <h3 class=\"text-sm font-semibold text-gray-900 dark:text-gray-100\">Recent Pages</h3>");
                sb.AppendLine("                    </div>");
                sb.AppendLine("                    <ul id=\"history-list\" class=\"max-h-64 overflow-y-auto\">");
                sb.AppendLine("                    </ul>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");
            }
            if (_showEditor)
            {
                sb.AppendLine("            <button onclick=\"nekoOpenEditor()\" class=\"flex items-center justify-center text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors focus:ring-2 focus:ring-primary-500\" title=\"Edit Page\">");
                sb.AppendLine("                <i class=\"fi fi-rr-edit text-lg\"></i>");
                sb.AppendLine("            </button>");
            }
            // Light/dark toggle. Always in docs mode; in blog mode only when the
            // site opted into dark (theme.dark set) — light-only blogs hide it.
            if (!_isBlogMode || _blogDarkEnabled)
            {
                var toggleStyle = _isBlogMode ? " style=\"color:var(--blog-ink)\"" : "";
                sb.AppendLine($"            <button id=\"theme-toggle\"{toggleStyle} class=\"flex items-center justify-center text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors focus:ring-2 focus:ring-primary-500\">");
                sb.AppendLine("                <i class=\"fi fi-rr-moon dark:hidden text-lg\"></i>");
                sb.AppendLine("                <i class=\"fi fi-rr-sun hidden dark:block text-lg\"></i>");
                sb.AppendLine("            </button>");
            }

            RenderNavbarActionButtons(sb);

            sb.AppendLine("        </div>");
        }

        // Renders the configured `actions:` as pill-shaped call-to-action buttons
        // on the right of the navbar (e.g. "Book a Demo" / "Talk to Sales").
        private void RenderNavbarActionButtons(StringBuilder sb)
        {
            if (_config.Actions == null || _config.Actions.Count == 0) return;

            foreach (var action in _config.Actions)
            {
                if (string.IsNullOrEmpty(action.Text)) continue;

                var href = action.Link ?? "#";
                var target = !string.IsNullOrEmpty(action.Target) ? $" target=\"{NormalizeTarget(action.Target)}\"" : "";
                var rel = target.Contains("_blank") ? " rel=\"noopener noreferrer\"" : "";
                var isOutline = string.Equals(action.Variant, "outline", System.StringComparison.OrdinalIgnoreCase);

                var iconHtml = string.IsNullOrEmpty(action.Icon) ? "" : $"<i class=\"{Neko.Builder.IconHelper.GetIconClass(action.Icon)}\"></i>";

                if (_isBlogMode)
                {
                    // Driven by the blog palette vars so the pills invert with dark
                    // mode: solid = ink fill with page-bg text; outline = ink border
                    // + text. Matches the curiosity.ai pills in light mode.
                    var blogClass = "inline-flex items-center gap-2 rounded-full px-[14px] py-2 text-sm font-medium transition-opacity hover:opacity-80 whitespace-nowrap";
                    // Outline pill uses a 1px INSET box-shadow rather than a border so
                    // the pill stays the exact same box size as the filled one (matching
                    // curiosity.ai, whose outlined CTA has no layout-affecting border).
                    var style = isOutline
                        ? "box-shadow:inset 0 0 0 1px var(--blog-ink);color:var(--blog-ink)"
                        : "background-color:var(--blog-ink);color:var(--blog-bg)";
                    sb.AppendLine($"            <a href=\"{href}\"{target}{rel} class=\"{blogClass}\" style=\"{style}\">{iconHtml}{action.Text}</a>");
                    continue;
                }

                var btnClass = isOutline
                    ? "inline-flex items-center gap-2 rounded-full border border-gray-900 dark:border-gray-300 px-4 py-2 text-sm font-semibold text-gray-900 dark:text-gray-100 hover:bg-gray-900 hover:text-white dark:hover:bg-white dark:hover:text-gray-900 transition-colors whitespace-nowrap"
                    : "inline-flex items-center gap-2 rounded-full bg-gray-900 dark:bg-white px-4 py-2 text-sm font-semibold text-white dark:text-gray-900 hover:bg-gray-700 dark:hover:bg-gray-200 transition-colors whitespace-nowrap";

                sb.AppendLine($"            <a href=\"{href}\"{target}{rel} class=\"{btnClass}\">{iconHtml}{action.Text}</a>");
            }
        }
    }
}
