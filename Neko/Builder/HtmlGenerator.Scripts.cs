using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        private void RenderPageScripts(StringBuilder sb)
        {
            sb.AppendLine("    <script>");

            RenderBannerScript(sb);
            RenderMobileMenuScript(sb);
            RenderSidebarScrollScript(sb);
            RenderSidebarFilterScript(sb);
            RenderSidebarMatchHelper(sb);
            RenderActiveSidebarLinkScript(sb);
            RenderSidebarSectionStateScript(sb);
            RenderTocHighlightScript(sb);
            RenderThemeSwitchScript(sb);
            RenderTabScript(sb);
            RenderCopyCodeScript(sb);
            RenderCopyTextScript(sb);
            RenderHighlightInitScript(sb);
            RenderLineHighlightScript(sb);
            RenderFragmentScrollScript(sb);
            RenderPageLinkClickScript(sb);
            RenderChangelogStickyScript(sb);

            sb.AppendLine("    </script>");
        }

        private void RenderBannerScript(StringBuilder sb)
        {
            if (_config.Banner == null || !_config.Banner.Visible || string.IsNullOrEmpty(_config.Banner.Text)) return;

            var bannerId = _config.Banner.Id ?? "neko-banner";
            sb.AppendLine($"        const bannerId = '{bannerId}';");
            sb.AppendLine("        function dismissBanner(id) {");
            sb.AppendLine("            const banner = document.getElementById(id);");
            sb.AppendLine("            if (banner) {");
            sb.AppendLine("                banner.classList.add('hidden');");
            sb.AppendLine("                localStorage.setItem('banner-dismissed-' + id, 'true');");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        ");
            sb.AppendLine("        if (localStorage.getItem('banner-dismissed-' + bannerId) !== 'true') {");
            sb.AppendLine("            const banner = document.getElementById(bannerId);");
            sb.AppendLine("            if (banner) banner.classList.remove('hidden');");
            sb.AppendLine("        }");
        }

        private void RenderMobileMenuScript(StringBuilder sb)
        {
            sb.AppendLine("        const mobileMenuBtn = document.getElementById('mobile-menu-btn');");
            sb.AppendLine("        const sidebar = document.getElementById('sidebar');");
            sb.AppendLine("        const sidebarOverlay = document.getElementById('sidebar-overlay');");
            sb.AppendLine("        ");
            sb.AppendLine("        function toggleMobileMenu() {");
            sb.AppendLine("            const isClosed = sidebar.classList.contains('-translate-x-full');");
            sb.AppendLine("            if (isClosed) {");
            sb.AppendLine("                sidebar.classList.remove('-translate-x-full');");
            sb.AppendLine("                sidebarOverlay.classList.remove('hidden');");
            sb.AppendLine("                document.body.style.overflow = 'hidden';");
            sb.AppendLine("            } else {");
            sb.AppendLine("                sidebar.classList.add('-translate-x-full');");
            sb.AppendLine("                sidebarOverlay.classList.add('hidden');");
            sb.AppendLine("                document.body.style.overflow = '';");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        ");
            sb.AppendLine("        if (mobileMenuBtn) {");
            sb.AppendLine("            mobileMenuBtn.addEventListener('click', toggleMobileMenu);");
            sb.AppendLine("            sidebarOverlay.addEventListener('click', toggleMobileMenu);");
            sb.AppendLine("        }");
        }

        private void RenderSidebarScrollScript(StringBuilder sb)
        {
            sb.AppendLine("        if (sidebar) {");
            var keyBase = System.Text.RegularExpressions.Regex.Replace(_config.Branding.Title ?? "neko", "[^a-zA-Z0-9]", "-").ToLower();
            sb.AppendLine($"            const scrollKey = '{keyBase}-sidebar-scroll';");
            sb.AppendLine($"            const timeKey = '{keyBase}-sidebar-scroll-time';");
            // Restore the saved scroll position (if still fresh). Exposed globally
            // so password.js can re-apply it after it reveals the protected sidebar
            // entries: on a protected page the inline call below runs while those
            // entries are still hidden, so the sidebar is too short for scrollTop to
            // take effect and the position is lost once the entries pop in.
            sb.AppendLine("            window.nekoRestoreSidebarScroll = function () {");
            sb.AppendLine("                const savedScroll = localStorage.getItem(scrollKey);");
            sb.AppendLine("                const savedTime = localStorage.getItem(timeKey);");
            sb.AppendLine("                if (savedScroll && savedTime) {");
            sb.AppendLine("                    const now = new Date().getTime();");
            sb.AppendLine("                    if (now - parseInt(savedTime) < 60000) {");
            sb.AppendLine("                        sidebar.scrollTop = parseInt(savedScroll);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            };");
            sb.AppendLine("            window.nekoRestoreSidebarScroll();");
            sb.AppendLine("            let timeout;");
            sb.AppendLine("            sidebar.addEventListener('scroll', () => {");
            sb.AppendLine("                clearTimeout(timeout);");
            sb.AppendLine("                timeout = setTimeout(() => {");
            sb.AppendLine("                    localStorage.setItem(scrollKey, sidebar.scrollTop);");
            sb.AppendLine("                    localStorage.setItem(timeKey, new Date().getTime());");
            sb.AppendLine("                }, 100);");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
        }

        private void RenderSidebarFilterScript(StringBuilder sb)
        {
            sb.AppendLine("        const sidebarFilter = document.getElementById('sidebar-filter');");
            sb.AppendLine("        const sidebarList = document.getElementById('sidebar-list');");
            sb.AppendLine("        if (sidebarFilter && sidebarList) {");
            sb.AppendLine("            sidebarFilter.addEventListener('input', (e) => {");
            sb.AppendLine("                const term = e.target.value.toLowerCase();");
            sb.AppendLine("                // Expand all details when searching");
            sb.AppendLine("                const details = sidebarList.querySelectorAll('details');");
            sb.AppendLine("                if (term) details.forEach(d => d.open = true);");
            sb.AppendLine("                ");
            sb.AppendLine("                const items = sidebarList.querySelectorAll('li, summary');");
            sb.AppendLine("                items.forEach(item => {");
            sb.AppendLine("                    // Avoid filtering the container li of nested lists, just filter leaf nodes or summary text");
            sb.AppendLine("                    if (item.tagName === 'LI' && item.querySelector('details')) return;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const text = item.textContent.toLowerCase();");
            sb.AppendLine("                    if (text.includes(term)) {");
            sb.AppendLine("                        item.classList.remove('hidden');");
            sb.AppendLine("                        // Make sure parent is visible");
            sb.AppendLine("                        let parent = item.parentElement;");
            sb.AppendLine("                        while(parent && parent !== sidebarList) {");
            sb.AppendLine("                            parent.classList.remove('hidden');");
            sb.AppendLine("                            if (parent.tagName === 'DETAILS') parent.open = true;");
            sb.AppendLine("                            parent = parent.parentElement;");
            sb.AppendLine("                        }");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        item.classList.add('hidden');");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
        }

        // Shared link-matching used by both the active-link highlighter and the
        // section-state script, so a sidebar entry and the current URL are compared
        // the same way in both places.
        //
        // A folder's index.md / readme.md renders as a leaf link whose href ends in
        // `/index` (e.g. `/foo/index`), but the page is actually served at the folder
        // URL `/foo/` (or `/foo`). Canonicalising both sides — dropping `.html`, a
        // trailing slash, and a trailing `/index` — makes those forms compare equal so
        // the folder's own entry highlights when you are on the folder page.
        //
        // Index links match by exact (canonical) path only. The prefix rule, which
        // lets a real parent page stay highlighted while you browse its children, is
        // deliberately skipped for index links: the canonical index path is a prefix
        // of every sibling in the folder, and applying it would wrongly highlight the
        // index entry on every sibling page.
        private void RenderSidebarMatchHelper(StringBuilder sb)
        {
            sb.AppendLine("        function nekoCanonicalPath(p) {");
            sb.AppendLine("            if (!p) return p;");
            sb.AppendLine("            if (p.endsWith('.html')) p = p.substring(0, p.length - 5);");
            sb.AppendLine("            if (p.length > 1 && p.endsWith('/')) p = p.substring(0, p.length - 1);");
            sb.AppendLine("            if (p.endsWith('/index')) p = p.substring(0, p.length - 6) || '/';");
            sb.AppendLine("            return p;");
            sb.AppendLine("        }");
            sb.AppendLine("        function nekoSidebarLinkMatches(href, currentPath) {");
            sb.AppendLine("            if (!href || href === '#') return false;");
            sb.AppendLine("            const isIndex = href.endsWith('/index') || href === '/index' || href.endsWith('/');");
            sb.AppendLine("            const cHref = nekoCanonicalPath(href);");
            sb.AppendLine("            const cCur = nekoCanonicalPath(currentPath);");
            sb.AppendLine("            if (cHref === cCur) return true;");
            sb.AppendLine("            if (isIndex || cHref === '/') return false;");
            sb.AppendLine("            return cCur.startsWith(cHref) && cCur.charAt(cHref.length) === '/';");
            sb.AppendLine("        }");
        }

        private void RenderActiveSidebarLinkScript(StringBuilder sb)
        {
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', () => {");
            sb.AppendLine("            const currentPath = window.location.pathname;");
            sb.AppendLine("            const sidebarLinks = document.querySelectorAll('#sidebar-list a');");
            sb.AppendLine("            sidebarLinks.forEach(link => {");
            sb.AppendLine("                const href = link.getAttribute('href');");
            sb.AppendLine("                if (nekoSidebarLinkMatches(href, currentPath)) {");
            sb.AppendLine("                    link.classList.add('bg-primary-50', 'dark:bg-primary-900', 'text-primary-700', 'dark:text-primary-300', 'font-medium');");
            sb.AppendLine("                    link.classList.remove('text-gray-700', 'dark:text-gray-200');");
            sb.AppendLine("                    // (Revealing the parent <details> is handled synchronously by the section-state script.)");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        private void RenderSidebarSectionStateScript(StringBuilder sb)
        {
            // Persist the open/collapsed state of each collapsible sidebar section across
            // page navigations (the site is statically served, so every navigation is a full
            // reload that would otherwise reset every <details> to its default `open` state).
            //
            // Everything here is synchronous and event-driven: read localStorage, set the
            // <details> state, and listen for user toggles. No MutationObserver (or any other
            // observer) is used. This script runs at the end of <body>, after the sidebar is
            // parsed but before the first paint, so the restored state never flashes.
            var keyBase = System.Text.RegularExpressions.Regex.Replace(_config.Branding.Title ?? "neko", "[^a-zA-Z0-9]", "-").ToLower();
            sb.AppendLine("        if (sidebar) {");
            sb.AppendLine($"            const sectionStateKey = '{keyBase}-sidebar-sections';");
            sb.AppendLine("            let sectionState = {};");
            sb.AppendLine("            try { sectionState = JSON.parse(localStorage.getItem(sectionStateKey) || '{}') || {}; } catch (e) { sectionState = {}; }");
            sb.AppendLine("            const sectionDetails = sidebar.querySelectorAll('details[data-section-key]');");
            sb.AppendLine("            // 1) Restore each section to the user's last saved choice.");
            sb.AppendLine("            sectionDetails.forEach(d => {");
            sb.AppendLine("                const key = d.getAttribute('data-section-key');");
            sb.AppendLine("                if (Object.prototype.hasOwnProperty.call(sectionState, key)) { d.open = !!sectionState[key]; }");
            sb.AppendLine("            });");
            sb.AppendLine("            // 2) Always reveal the section(s) containing the current page (transient, not saved).");
            sb.AppendLine("            const currentPath = window.location.pathname;");
            sb.AppendLine("            sidebar.querySelectorAll('#sidebar-list a').forEach(link => {");
            sb.AppendLine("                const href = link.getAttribute('href');");
            sb.AppendLine("                if (nekoSidebarLinkMatches(href, currentPath)) {");
            sb.AppendLine("                    let parent = link.parentElement;");
            sb.AppendLine("                    while (parent && parent.id !== 'sidebar-list') {");
            sb.AppendLine("                        if (parent.tagName === 'DETAILS') parent.open = true;");
            sb.AppendLine("                        parent = parent.parentElement;");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("            // 3) Commit the restored state with the transition suppressed, then re-enable it");
            sb.AppendLine("            //    so the chevron only animates on a later user click, never on this load.");
            sb.AppendLine("            void sidebar.offsetWidth;");
            sb.AppendLine("            sidebar.classList.remove('neko-no-anim');");
            sb.AppendLine("            // 4) Persist genuine user toggles only (ignore the programmatic opens above");
            sb.AppendLine("            //    and the filter's 'expand all', which fire toggle events without a click).");
            sb.AppendLine("            sectionDetails.forEach(d => {");
            sb.AppendLine("                const key = d.getAttribute('data-section-key');");
            sb.AppendLine("                const summary = d.querySelector(':scope > summary');");
            sb.AppendLine("                let userAction = false;");
            sb.AppendLine("                if (summary) {");
            sb.AppendLine("                    summary.addEventListener('click', () => { userAction = true; });");
            sb.AppendLine("                    summary.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') userAction = true; });");
            sb.AppendLine("                }");
            sb.AppendLine("                d.addEventListener('toggle', () => {");
            sb.AppendLine("                    if (!userAction) return;");
            sb.AppendLine("                    userAction = false;");
            sb.AppendLine("                    sectionState[key] = d.open;");
            sb.AppendLine("                    try { localStorage.setItem(sectionStateKey, JSON.stringify(sectionState)); } catch (e) {}");
            sb.AppendLine("                });");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
        }

        private void RenderTocHighlightScript(StringBuilder sb)
        {
            sb.AppendLine("        const tocLinks = document.querySelectorAll('.toc-link');");
            sb.AppendLine("        const sections = [];");
            sb.AppendLine("        tocLinks.forEach(link => {");
            sb.AppendLine("            const id = link.getAttribute('data-id');");
            sb.AppendLine("            const section = document.getElementById(id);");
            sb.AppendLine("            if (section) sections.push(section);");
            sb.AppendLine("        });");
            sb.AppendLine("");
            sb.AppendLine("        const visibleSections = new Set();");
            sb.AppendLine("        const highlightLine = document.getElementById('toc-highlight');");
            sb.AppendLine("        const tocList = document.getElementById('toc-list');");
            sb.AppendLine("");
            sb.AppendLine("        function updateTocHighlight() {");
            sb.AppendLine("            if (!highlightLine || !tocList) return;");
            sb.AppendLine("");
            sb.AppendLine("            // Clear all highlights");
            sb.AppendLine("            document.querySelectorAll('.toc-link').forEach(link => {");
            sb.AppendLine("                link.classList.remove('text-primary-600', 'dark:text-primary-400', 'font-medium');");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            let activeLinks = Array.from(document.querySelectorAll('.toc-link')).filter(link => ");
            sb.AppendLine("                visibleSections.has(link.getAttribute('data-id'))");
            sb.AppendLine("            );");
            sb.AppendLine("");
            sb.AppendLine("            if (activeLinks.length === 0) {");
            sb.AppendLine("                // Fallback to last passed section");
            sb.AppendLine("                const passedSections = sections.filter(section => {");
            sb.AppendLine("                    const rect = section.getBoundingClientRect();");
            sb.AppendLine("                    return rect.top < 100;");
            sb.AppendLine("                });");
            sb.AppendLine("                ");
            sb.AppendLine("                if (passedSections.length > 0) {");
            sb.AppendLine("                    const lastPassedSection = passedSections[passedSections.length - 1];");
            sb.AppendLine("                    const id = lastPassedSection.id;");
            sb.AppendLine("                    const link = document.querySelector(`.toc-link[data-id=\"${id}\"]`);");
            sb.AppendLine("                    if (link) {");
            sb.AppendLine("                        activeLinks = [link];");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            if (activeLinks.length === 0) {");
            sb.AppendLine("                highlightLine.style.opacity = '0';");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            // Apply highlight");
            sb.AppendLine("            activeLinks.forEach(link => {");
            sb.AppendLine("                link.classList.add('text-primary-600', 'dark:text-primary-400', 'font-medium');");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            // Scroll TOC to active link");
            sb.AppendLine("            const activeLink = activeLinks[0];");
            sb.AppendLine("            const tocSidebar = document.getElementById('toc-sidebar');");
            sb.AppendLine("            if (activeLink && tocSidebar) {");
            sb.AppendLine("                const linkRect = activeLink.getBoundingClientRect();");
            sb.AppendLine("                const sidebarRect = tocSidebar.getBoundingClientRect();");
            sb.AppendLine("                if (linkRect.top < sidebarRect.top || linkRect.bottom > sidebarRect.bottom) {");
            sb.AppendLine("                     activeLink.scrollIntoView({ block: 'center', behavior: 'smooth' });");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            const firstLink = activeLinks[0];");
            sb.AppendLine("            const lastLink = activeLinks[activeLinks.length - 1];");
            sb.AppendLine("            ");
            sb.AppendLine("            const listRect = tocList.getBoundingClientRect();");
            sb.AppendLine("            const firstRect = firstLink.getBoundingClientRect();");
            sb.AppendLine("            const lastRect = lastLink.getBoundingClientRect();");
            sb.AppendLine("");
            sb.AppendLine("            const top = firstRect.top - listRect.top;");
            sb.AppendLine("            const height = lastRect.bottom - firstRect.top;");
            sb.AppendLine("");
            sb.AppendLine("            highlightLine.style.top = `${top}px`;");
            sb.AppendLine("            highlightLine.style.height = `${height}px`;");
            sb.AppendLine("            highlightLine.style.opacity = '1';");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        const observer = new IntersectionObserver((entries) => {");
            sb.AppendLine("            entries.forEach(entry => {");
            sb.AppendLine("                const id = entry.target.id;");
            sb.AppendLine("                if (entry.isIntersecting) {");
            sb.AppendLine("                    visibleSections.add(id);");
            sb.AppendLine("                } else {");
            sb.AppendLine("                    visibleSections.delete(id);");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("            requestAnimationFrame(updateTocHighlight);");
            sb.AppendLine("        }, {");
            sb.AppendLine("            root: document.getElementById('main-scroll'),");
            sb.AppendLine("            rootMargin: '0px 0px 0px 0px',");
            sb.AppendLine("            threshold: 0");
            sb.AppendLine("        });");
            sb.AppendLine("        sections.forEach(section => observer.observe(section));");
        }

        private void RenderThemeSwitchScript(StringBuilder sb)
        {
            var darkTheme = _config.Theme.Highlight.Dark;
            var lightTheme = _config.Theme.Highlight.Light;

            sb.AppendLine("        const themeToggleBtn = document.getElementById('theme-toggle');");
            sb.AppendLine("        const highlightLink = document.getElementById('highlight-theme');");
            sb.AppendLine($"        const darkHref = '/assets/highlight/{darkTheme}.min.css';");
            sb.AppendLine($"        const lightHref = '/assets/highlight/{lightTheme}.min.css';");
            sb.AppendLine("");
            sb.AppendLine("        function setTheme(isDark) {");
            sb.AppendLine("            if (isDark) {");
            sb.AppendLine("                document.documentElement.classList.add('dark');");
            sb.AppendLine("                localStorage.setItem('theme', 'dark');");
            sb.AppendLine("                highlightLink.href = darkHref;");
            sb.AppendLine("            } else {");
            sb.AppendLine("                document.documentElement.classList.remove('dark');");
            sb.AppendLine("                localStorage.setItem('theme', 'light');");
            sb.AppendLine("                highlightLink.href = lightHref;");
            sb.AppendLine("            }");
            sb.AppendLine("            if (typeof renderMermaid === 'function') renderMermaid();");
            sb.AppendLine("            notifyTesseraePreviews(isDark);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        // Tesserae live-preview samples render in their own iframe; tell each");
            sb.AppendLine("        // one to follow the docs page's light/dark mode (it reacts in-frame).");
            sb.AppendLine("        function notifyTesseraePreviews(isDark) {");
            sb.AppendLine("            document.querySelectorAll('iframe.tesserae-preview').forEach(function (f) {");
            sb.AppendLine("                try { f.contentWindow.postMessage({ type: 'neko-theme', dark: isDark }, '*'); } catch (e) {}");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        themeToggleBtn.addEventListener('click', () => {");
            sb.AppendLine("            setTheme(!document.documentElement.classList.contains('dark'));");
            sb.AppendLine("        });");
            sb.AppendLine("");
            sb.AppendLine("        if (document.documentElement.classList.contains('dark')) {");
            sb.AppendLine("            highlightLink.href = darkHref;");
            sb.AppendLine("        } else {");
            sb.AppendLine("            highlightLink.href = lightHref;");
            sb.AppendLine("        }");
        }

        private void RenderTabScript(StringBuilder sb)
        {
            sb.AppendLine("        function openTab(evt, group, tabId) {");
            sb.AppendLine("            var contents = document.querySelectorAll(`[id^='tab-${group}-']`);");
            sb.AppendLine("            contents.forEach(c => c.classList.add('hidden'));");
            sb.AppendLine("            var buttons = evt.currentTarget.parentElement.children;");
            sb.AppendLine("            for (var i = 0; i < buttons.length; i++) {");
            sb.AppendLine("                buttons[i].classList.remove('border-primary-500', 'text-primary-600', 'dark:text-primary-400', 'font-medium');");
            sb.AppendLine("                buttons[i].classList.add('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            }");
            sb.AppendLine("            document.getElementById(tabId).classList.remove('hidden');");
            sb.AppendLine("            evt.currentTarget.classList.remove('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            evt.currentTarget.classList.add('border-primary-500', 'text-primary-600', 'dark:text-primary-400', 'font-medium');");
            sb.AppendLine("        }");
        }

        private void RenderCopyCodeScript(StringBuilder sb)
        {
            sb.AppendLine("        document.addEventListener('click', function(e) {");
            sb.AppendLine("            const btn = e.target.closest('.copy-btn');");
            sb.AppendLine("            if (!btn) return;");
            sb.AppendLine("            const group = btn.closest('.group');");
            sb.AppendLine("            const codeEl = group.querySelector('code');");
            sb.AppendLine("            let text = \"\";");
            sb.AppendLine("            if (codeEl.querySelector('.hljs-ln')) {");
            sb.AppendLine("                codeEl.querySelectorAll('.hljs-ln-code').forEach(td => text += td.innerText + \"\\n\");");
            sb.AppendLine("            } else {");
            sb.AppendLine("                text = codeEl.innerText;");
            sb.AppendLine("            }");
            sb.AppendLine("            navigator.clipboard.writeText(text).then(() => {");
            sb.AppendLine("                const icon = btn.querySelector('i');");
            sb.AppendLine("                const originalClass = icon.className;");
            sb.AppendLine("                icon.className = 'fi fi-rr-check';");
            sb.AppendLine("                setTimeout(() => icon.className = originalClass, 2000);");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        // Copy-to-clipboard for components that carry their payload in a
        // `data-copy` attribute (version badges, link cards, …). Distinct from the
        // code-block `.copy-btn` handler, which scrapes the rendered <code>.
        private void RenderCopyTextScript(StringBuilder sb)
        {
            sb.AppendLine("        document.addEventListener('click', function(e) {");
            sb.AppendLine("            const btn = e.target.closest('.neko-copy-btn');");
            sb.AppendLine("            if (!btn) return;");
            sb.AppendLine("            e.preventDefault();");
            sb.AppendLine("            const text = btn.getAttribute('data-copy') || '';");
            sb.AppendLine("            navigator.clipboard.writeText(text).then(() => {");
            sb.AppendLine("                const icon = btn.querySelector('i');");
            sb.AppendLine("                if (!icon) return;");
            sb.AppendLine("                const originalClass = icon.className;");
            sb.AppendLine("                icon.className = originalClass.replace('fi-rr-copy', 'fi-rr-check');");
            sb.AppendLine("                setTimeout(() => icon.className = originalClass, 2000);");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        private void RenderHighlightInitScript(StringBuilder sb)
        {
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', (event) => {");
            sb.AppendLine("            hljs.highlightAll();");
            sb.AppendLine("            ");
            sb.AppendLine("            // Init Line Numbers");
            sb.AppendLine("            document.querySelectorAll('code.hljs').forEach(block => {");
            sb.AppendLine("                const language = Array.from(block.classList).find(c => c.startsWith('language-'))?.replace('language-', '');");
            sb.AppendLine("                const configLineNumbers = window.nekoConfig?.snippets?.lineNumbers || [];");
            sb.AppendLine("                const globalEnabled = language && configLineNumbers.includes(language);");
            sb.AppendLine("                const localEnabled = block.classList.contains('line-numbers') || block.parentElement.classList.contains('line-numbers');");
            sb.AppendLine("                const localDisabled = block.classList.contains('no-line-numbers') || block.parentElement.classList.contains('no-line-numbers');");
            sb.AppendLine("                const hasHighlight = block.parentElement.getAttribute('data-highlight') || block.getAttribute('data-highlight');");
            sb.AppendLine("                ");
            sb.AppendLine("                // We need the table if line numbers are enabled OR if highlighting is requested (as highlighting depends on the table)");
            sb.AppendLine("                const needsTable = (globalEnabled || localEnabled || hasHighlight) && !(localDisabled && !hasHighlight);");
            sb.AppendLine("                ");
            sb.AppendLine("                if (needsTable) {");
            sb.AppendLine("                    // Hide numbers if explicitly disabled, or if not enabled (but table exists for highlight)");
            sb.AppendLine("                    const hideNumbers = localDisabled || (!globalEnabled && !localEnabled);");
            sb.AppendLine("                    if (hideNumbers) {");
            sb.AppendLine("                        block.parentElement.classList.add('hide-line-numbers');");
            sb.AppendLine("                    }");
            sb.AppendLine("                    hljs.lineNumbersBlock(block, { singleLine: true });");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            // Anchor Links");
            sb.AppendLine("            document.querySelectorAll('h2, h3, h4, h5, h6').forEach(heading => {");
            sb.AppendLine("                if (heading.id) {");
            sb.AppendLine("                    heading.classList.add('group', 'relative');");
            sb.AppendLine("                    const link = document.createElement('a');");
            sb.AppendLine("                    link.href = '#' + heading.id;");
            sb.AppendLine("                    // Anchor sits entirely to the left of the heading: `-translate-x-full`");
            sb.AppendLine("                    // aligns its right edge with the heading's left edge, and `pr-2`");
            sb.AppendLine("                    // adds a fixed gap so the icon never abuts the text — independent");
            sb.AppendLine("                    // of how large the heading (and therefore the icon) is.");
            sb.AppendLine("                    link.className = 'absolute left-0 top-0 bottom-0 -translate-x-full flex items-center justify-center text-primary-500 opacity-0 group-hover:opacity-100 transition-opacity pr-2 no-underline';");
            sb.AppendLine("                    link.innerHTML = '<i class=\"fi fi-rr-hashtag\"></i>';");
            sb.AppendLine("                    link.setAttribute('aria-label', 'Anchor');");
            sb.AppendLine("                    heading.prepend(link);");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        private void RenderLineHighlightScript(StringBuilder sb)
        {
            sb.AppendLine("        window.addEventListener('load', function() {");
            sb.AppendLine("            document.querySelectorAll('pre').forEach(pre => {");
            sb.AppendLine("                const highlightRange = pre.getAttribute('data-highlight') || pre.querySelector('code')?.getAttribute('data-highlight');");
            sb.AppendLine("                if (!highlightRange) return;");
            sb.AppendLine("                const block = pre.querySelector('code');");
            sb.AppendLine("                if (!block) return;");
            sb.AppendLine("                const linesToHighlight = new Set();");
            sb.AppendLine("                highlightRange.split(',').forEach(part => {");
            sb.AppendLine("                    if (part.includes('-')) {");
            sb.AppendLine("                        const [start, end] = part.split('-').map(Number);");
            sb.AppendLine("                        for (let i = start; i <= end; i++) linesToHighlight.add(i);");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        linesToHighlight.add(Number(part));");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("                const table = block.querySelector('.hljs-ln');");
            sb.AppendLine("                if (table) {");
            sb.AppendLine("                    const rows = table.querySelectorAll('tr');");
            sb.AppendLine("                    rows.forEach((row, index) => {");
            sb.AppendLine("                        if (linesToHighlight.has(index + 1)) {");
            sb.AppendLine("                            row.classList.add('bg-yellow-100', 'dark:bg-yellow-900', 'bg-opacity-20', 'dark:bg-opacity-20');");
            sb.AppendLine("                            row.querySelectorAll('td').forEach(td => td.style.backgroundColor = 'inherit');");
            sb.AppendLine("                        }");
            sb.AppendLine("                    });");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        // Fragment scroll on load. The content scrolls inside #main-scroll, not
        // the document, so on reload the browser restores that container's offset
        // instead of re-running the hash scroll — and async content (highlight.js
        // line numbers, mermaid, KaTeX) shifts layout after the initial jump. Once
        // everything has settled, re-align to the URL fragment so reloading a
        // deep link lands on the right heading.
        private void RenderFragmentScrollScript(StringBuilder sb)
        {
            sb.AppendLine("        window.addEventListener('load', function() {");
            sb.AppendLine("            if (!location.hash) return;");
            sb.AppendLine("            var id;");
            sb.AppendLine("            try { id = decodeURIComponent(location.hash.slice(1)); } catch (_) { id = location.hash.slice(1); }");
            sb.AppendLine("            if (!id) return;");
            sb.AppendLine("            var target = document.getElementById(id);");
            sb.AppendLine("            if (!target) return;");
            sb.AppendLine("            requestAnimationFrame(function() {");
            sb.AppendLine("                requestAnimationFrame(function() { target.scrollIntoView({ block: 'start', behavior: 'auto' }); });");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        // Page links: resolve ${page}, ${url}, ${selection} variables at click time
        private void RenderPageLinkClickScript(StringBuilder sb)
        {
            sb.AppendLine("        document.querySelectorAll('.neko-page-link').forEach(function(el) {");
            sb.AppendLine("            el.addEventListener('click', function(e) {");
            sb.AppendLine("                var template = el.getAttribute('data-neko-link-template');");
            sb.AppendLine("                if (!template) return;");
            sb.AppendLine("                e.preventDefault();");
            sb.AppendLine("                var page = encodeURIComponent(el.getAttribute('data-neko-page') || '');");
            sb.AppendLine("                var url = encodeURIComponent(el.getAttribute('data-neko-url') || '');");
            sb.AppendLine("                var sel = '';");
            sb.AppendLine("                try { sel = window.getSelection ? (window.getSelection().toString() || '') : ''; } catch (_) { sel = ''; }");
            sb.AppendLine("                var selection = encodeURIComponent(sel);");
            sb.AppendLine("                var finalUrl = template.split('${page}').join(page).split('${url}').join(url).split('${selection}').join(selection);");
            sb.AppendLine("                var target = el.getAttribute('target');");
            sb.AppendLine("                if (target && target !== '_self') { window.open(finalUrl, target); } else { window.location.href = finalUrl; }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
        }

        // Changelog version headers swap between an in-flow rounded card and a full-bleed
        // sticky bar. There is no widely-supported CSS `:stuck` selector, so a zero-size
        // sentinel marks each header's natural top and an IntersectionObserver (rooted at
        // #main-scroll) toggles `.is-stuck` once the sentinel scrolls above the pane top.
        // When stuck, the bar's background is `position: fixed` so it spans the whole
        // viewport (the content row is max-width-capped and centred); its top/height are
        // set inline here to track the pinned header, re-applied on resize.
        private void RenderChangelogStickyScript(StringBuilder sb)
        {
            sb.AppendLine("        (function() {");
            sb.AppendLine("            var root = document.getElementById('main-scroll');");
            sb.AppendLine("            var sentinels = document.querySelectorAll('.neko-cl-sentinel');");
            sb.AppendLine("            if (!root || !sentinels.length || !('IntersectionObserver' in window)) return;");
            sb.AppendLine("            function place(header) {");
            sb.AppendLine("                var bleed = header.querySelector('.neko-cl-bleed');");
            sb.AppendLine("                if (!bleed) return;");
            sb.AppendLine("                if (header.classList.contains('is-stuck')) {");
            sb.AppendLine("                    var r = header.getBoundingClientRect();");
            sb.AppendLine("                    bleed.style.top = (r.top - 8) + 'px';");
            sb.AppendLine("                    bleed.style.height = (r.height + 8) + 'px';");
            sb.AppendLine("                } else {");
            sb.AppendLine("                    bleed.style.top = '';");
            sb.AppendLine("                    bleed.style.height = '';");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            var io = new IntersectionObserver(function(entries) {");
            sb.AppendLine("                entries.forEach(function(e) {");
            sb.AppendLine("                    var header = e.target.nextElementSibling;");
            sb.AppendLine("                    if (!header) return;");
            sb.AppendLine("                    var rb = e.rootBounds;");
            sb.AppendLine("                    var stuck = !e.isIntersecting && !!rb && e.boundingClientRect.top <= rb.top;");
            sb.AppendLine("                    header.classList.toggle('is-stuck', stuck);");
            sb.AppendLine("                    place(header);");
            sb.AppendLine("                });");
            sb.AppendLine("            }, { root: root, threshold: [0] });");
            sb.AppendLine("            sentinels.forEach(function(s) { io.observe(s); });");
            sb.AppendLine("            window.addEventListener('resize', function() {");
            sb.AppendLine("                document.querySelectorAll('.neko-changelog-version.is-stuck').forEach(place);");
            sb.AppendLine("            });");
            sb.AppendLine("        })();");
        }
    }
}
