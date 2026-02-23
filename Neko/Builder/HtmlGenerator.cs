using Neko.Configuration;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    public class HtmlGenerator
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

        public string Generate(ParsedDocument document, List<(string Url, string Title)> backlinks = null, NavigationContext navContext = null, List<LinkConfig> sidebarLinks = null)
        {
            var title = !string.IsNullOrEmpty(document.FrontMatter.Title)
                ? $"{document.FrontMatter.Title} - {_config.Branding.Title}"
                : _config.Branding.Title;

            var darkTheme = _config.Theme.Highlight.Dark;
            var lightTheme = _config.Theme.Highlight.Light;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\" class=\"scroll-smooth\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>{title}</title>");

            if (!string.IsNullOrEmpty(_config.Branding.Favicon))
            {
                sb.AppendLine($"    <link rel=\"icon\" href=\"{_config.Branding.Favicon}\">");
            }

            var description = !string.IsNullOrEmpty(document.FrontMatter.Description)
                ? document.FrontMatter.Description
                : _config.Meta.Description;

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine($"    <meta name=\"description\" content=\"{description}\">");
                sb.AppendLine($"    <meta property=\"og:description\" content=\"{description}\">");
                sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{description}\">");
            }

            if (!string.IsNullOrEmpty(_config.Meta.Keywords)) sb.AppendLine($"    <meta name=\"keywords\" content=\"{_config.Meta.Keywords}\">");
            if (!string.IsNullOrEmpty(_config.Meta.Author)) sb.AppendLine($"    <meta name=\"author\" content=\"{_config.Meta.Author}\">");

            var pageTitle = !string.IsNullOrEmpty(document.FrontMatter.Title) ? $"{document.FrontMatter.Title} - {_config.Branding.Title}" : _config.Branding.Title;
            sb.AppendLine($"    <meta property=\"og:title\" content=\"{pageTitle}\">");
            sb.AppendLine($"    <meta name=\"twitter:title\" content=\"{pageTitle}\">");

            if (!string.IsNullOrEmpty(_config.Meta.Type)) sb.AppendLine($"    <meta property=\"og:type\" content=\"{_config.Meta.Type}\">");
            if (!string.IsNullOrEmpty(_config.Meta.Url)) sb.AppendLine($"    <meta property=\"og:url\" content=\"{_config.Meta.Url}\">");
            if (!string.IsNullOrEmpty(_config.Meta.Image))
            {
                sb.AppendLine($"    <meta property=\"og:image\" content=\"{_config.Meta.Image}\">");
                sb.AppendLine($"    <meta name=\"twitter:image\" content=\"{_config.Meta.Image}\">");
            }

            if (!string.IsNullOrEmpty(_config.Meta.TwitterCard)) sb.AppendLine($"    <meta name=\"twitter:card\" content=\"{_config.Meta.TwitterCard}\">");
            if (!string.IsNullOrEmpty(_config.Meta.TwitterSite)) sb.AppendLine($"    <meta name=\"twitter:site\" content=\"{_config.Meta.TwitterSite}\">");
            if (!string.IsNullOrEmpty(_config.Meta.TwitterCreator)) sb.AppendLine($"    <meta name=\"twitter:creator\" content=\"{_config.Meta.TwitterCreator}\">");

            // Tailwind CSS
            // Use CDN to ensure plugins (like typography) are available.
            // The local resource might be missing the typography plugin.
            sb.AppendLine("    <script src=\"https://cdn.tailwindcss.com?plugins=typography\"></script>");
            sb.AppendLine("    <script>tailwind.config = { darkMode: 'class' }</script>");

            // Neko Config
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var snippetsJson = _config.Snippets != null ? System.Text.Json.JsonSerializer.Serialize(_config.Snippets, jsonOptions) : "{}";
            sb.AppendLine($"    <script>window.nekoConfig = {{ snippets: {snippetsJson} }};</script>");

            // Inter Font
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://rsms.me/inter/inter.css\">");
            sb.AppendLine("    <style>:root { font-family: 'Inter', sans-serif; } @supports (font-variation-settings: normal) { :root { font-family: 'Inter var', sans-serif; } }</style>");

            // Flaticon UIcons
            sb.AppendLine("    <link rel='stylesheet' href='https://cdn-uicons.flaticon.com/uicons-regular-rounded/css/uicons-regular-rounded.css'>");

            // Emoji CSS
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"/assets/emoji.css\">");

            // KaTeX (Math)
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css\">");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/contrib/auto-render.min.js\"></script>");
            sb.AppendLine("    <script>document.addEventListener(\"DOMContentLoaded\", function() { renderMathInElement(document.body); });</script>");

            // Mermaid (Diagrams)
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
            sb.AppendLine("    <script>");
            sb.AppendLine("        mermaid.initialize({ startOnLoad: false });");
            sb.AppendLine("        ");
            sb.AppendLine("        async function renderMermaid() {");
            sb.AppendLine("            const elements = document.querySelectorAll('.mermaid');");
            sb.AppendLine("            if (!elements.length) return;");
            sb.AppendLine("            ");
            sb.AppendLine("            for (const el of elements) {");
            sb.AppendLine("                if (el.getAttribute('data-processed')) continue;");
            sb.AppendLine("                el.setAttribute('data-processed', 'true');");
            sb.AppendLine("                ");
            sb.AppendLine("                const source = el.textContent;");
            sb.AppendLine("                const idBase = 'mermaid-' + Math.random().toString(36).substr(2, 9);");
            sb.AppendLine("                const idLight = idBase + '-light';");
            sb.AppendLine("                const idDark = idBase + '-dark';");
            sb.AppendLine("                ");
            sb.AppendLine("                // Render Light");
            sb.AppendLine("                const sourceLight = `%%{init: {'theme':'default'}}%%\\n${source}`;");
            sb.AppendLine("                // Render Dark");
            sb.AppendLine("                const sourceDark = `%%{init: {'theme':'dark'}}%%\\n${source}`;");
            sb.AppendLine("                ");
            sb.AppendLine("                try {");
            sb.AppendLine("                    const rLight = await mermaid.render(idLight, sourceLight);");
            sb.AppendLine("                    const rDark = await mermaid.render(idDark, sourceDark);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    el.innerHTML = `");
            sb.AppendLine("                        <div class=\"dark:hidden\">${rLight.svg}</div>");
            sb.AppendLine("                        <div class=\"hidden dark:block\">${rDark.svg}</div>");
            sb.AppendLine("                    `;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (rLight.bindFunctions) rLight.bindFunctions(el.querySelector('.dark\\\\:hidden'));");
            sb.AppendLine("                    if (rDark.bindFunctions) rDark.bindFunctions(el.querySelector('.dark\\\\:block'));");
            sb.AppendLine("                } catch (e) {");
            sb.AppendLine("                    console.error('Mermaid rendering failed:', e);");
            sb.AppendLine("                    el.innerHTML = `<div class=\"text-red-500\">Error rendering diagram</div>`;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        ");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', renderMermaid);");
            sb.AppendLine("    </script>");

            // Force Graph
            sb.AppendLine("    <script src=\"/assets/force-graph.min.js\"></script>");

            // Search Assets
            sb.AppendLine("    <script src=\"/assets/minisearch.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"/assets/search.js\"></script>");

            // Highlight.js
            sb.AppendLine($"    <link id=\"highlight-theme\" rel=\"stylesheet\" href=\"/assets/highlight/{darkTheme}.min.css\">");
            sb.AppendLine("    <script src=\"/assets/highlight/highlight.min.js\"></script>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/highlightjs-line-numbers.js@2.8.0/dist/highlightjs-line-numbers.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        /* Highlight.js Line Numbers CSS */");
            sb.AppendLine("        .prose table.hljs-ln tr { border: none !important; }");
            sb.AppendLine("        .prose table.hljs-ln td { padding: 0 !important; }");
            sb.AppendLine("        .prose table.hljs-ln td.hljs-ln-numbers { user-select: none; text-align: right; color: #ccc; border-right: 1px solid #ccc; vertical-align: top; padding-right: 5px !important; padding-left: 10px !important; }");
            sb.AppendLine("        .prose table.hljs-ln td.hljs-ln-code { padding-left: 10px !important; }");
            sb.AppendLine("        ");
            sb.AppendLine("        /* Hide Line Numbers when disabled but table is present (for highlighting) */");
            sb.AppendLine("        .hide-line-numbers .hljs-ln-numbers { display: none !important; }");
            sb.AppendLine("        .hide-line-numbers .hljs-ln-code { padding-left: 5px !important; }");
            sb.AppendLine("        ");
            sb.AppendLine("        /* Custom Highlight Style */");
            sb.AppendLine("        .line-highlight { background-color: rgba(255, 255, 0, 0.15); display: block; width: 100%; }");
            sb.AppendLine("        .dark .line-highlight { background-color: rgba(255, 255, 0, 0.15); }");
            sb.AppendLine("        ");
            sb.AppendLine("        /* Fix Table layout for code blocks with line numbers */");
            sb.AppendLine("        .hljs-ln { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("");
            sb.AppendLine("        /* Custom Scrollbars */");
            sb.AppendLine("        /* Sidebar */");
            sb.AppendLine("        #sidebar::-webkit-scrollbar { width: 6px; height: 6px; background-color: transparent; }");
            sb.AppendLine("        #sidebar::-webkit-scrollbar-thumb { background-color: transparent; border-radius: 3px; }");
            sb.AppendLine("        #sidebar:hover::-webkit-scrollbar-thumb { background-color: rgba(156, 163, 175, 0.5); }");
            sb.AppendLine("        .dark #sidebar:hover::-webkit-scrollbar-thumb { background-color: rgba(75, 85, 99, 0.5); }");
            sb.AppendLine("");
            sb.AppendLine("        /* TOC */");
            sb.AppendLine("        #toc-sidebar::-webkit-scrollbar { display: none; }");
            sb.AppendLine("        #toc-sidebar { -ms-overflow-style: none; scrollbar-width: none; }");
            sb.AppendLine("");
            sb.AppendLine("        /* Main Content */");
            sb.AppendLine("        #main-scroll::-webkit-scrollbar { width: 8px; height: 8px; background-color: transparent; }");
            sb.AppendLine("        #main-scroll::-webkit-scrollbar-track { background-color: transparent; }");
            sb.AppendLine("        #main-scroll::-webkit-scrollbar-thumb { background-color: rgba(209, 213, 219, 0.5); border-radius: 4px; border: 2px solid transparent; background-clip: content-box; }");
            sb.AppendLine("        .dark #main-scroll::-webkit-scrollbar-thumb { background-color: rgba(75, 85, 99, 0.5); }");
            sb.AppendLine("        #main-scroll::-webkit-scrollbar-thumb:hover { background-color: rgba(156, 163, 175, 0.8); }");
            sb.AppendLine("        .dark #main-scroll::-webkit-scrollbar-thumb:hover { background-color: rgba(107, 114, 128, 0.8); }");
            sb.AppendLine("    </style>");

            // Dark Mode Init
            sb.AppendLine("    <script>");
            sb.AppendLine("        if (localStorage.theme === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {");
            sb.AppendLine("            document.documentElement.classList.add('dark');");
            sb.AppendLine("        } else {");
            sb.AppendLine("            document.documentElement.classList.remove('dark');");
            sb.AppendLine("        }");
            sb.AppendLine("    </script>");

            if (!string.IsNullOrEmpty(_headIncludes))
            {
                sb.AppendLine(_headIncludes);
            }

            sb.AppendLine("</head>");
            sb.AppendLine("<body class=\"bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col h-screen overflow-hidden\">");

            // Banner
            if (_config.Banner != null && _config.Banner.Visible && !string.IsNullOrEmpty(_config.Banner.Text))
            {
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

            // Navbar
            sb.AppendLine("    <header class=\"h-16 shrink-0 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shadow-sm flex items-center justify-between px-6 z-10\">");
            sb.AppendLine("        <div class=\"flex items-center gap-4\">");
            sb.AppendLine("            <button id=\"mobile-menu-btn\" class=\"md:hidden text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none\">");
            sb.AppendLine("                <i class=\"fi fi-rr-menu-burger text-xl\"></i>");
            sb.AppendLine("            </button>");
            if (!string.IsNullOrEmpty(_config.Branding.Logo))
            {
                sb.AppendLine($"            <img src=\"{_config.Branding.Logo}\" class=\"h-8 w-auto dark:hidden\">");
                if (!string.IsNullOrEmpty(_config.Branding.LogoDark))
                {
                    sb.AppendLine($"            <img src=\"{_config.Branding.LogoDark}\" class=\"h-8 w-auto hidden dark:block\">");
                }
                else
                {
                    sb.AppendLine($"            <img src=\"{_config.Branding.Logo}\" class=\"h-8 w-auto hidden dark:block\">");
                }
            }
            else if (!string.IsNullOrEmpty(_config.Branding.Icon))
            {
                sb.AppendLine($"            <i class=\"{_config.Branding.Icon} text-2xl text-blue-600 dark:text-blue-400\"></i>");
            }
            sb.AppendLine($"            <a href=\"/index\" class=\"font-bold text-xl hover:text-blue-600 transition-colors\">{_config.Branding.Title}</a>");
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"hidden md:flex items-center gap-6 text-sm font-medium text-gray-600 dark:text-gray-300\">");
            if (_config.Links != null)
            {
                foreach (var link in _config.Links)
                {
                    if (link.Items != null && link.Items.Count > 0)
                    {
                        // Flyout Menu
                        sb.AppendLine($"            <div class=\"relative group z-50\">");
                        sb.AppendLine($"                <button class=\"flex items-center gap-1 hover:text-blue-600 dark:hover:text-blue-400 transition-colors focus:outline-none\">");
                        if (!string.IsNullOrEmpty(link.Icon))
                        {
                            sb.AppendLine($"                    <i class=\"fi fi-rr-{link.Icon}\"></i>");
                        }
                        sb.AppendLine($"                    <span>{link.Text}</span>");
                        sb.AppendLine($"                    <i class=\"fi fi-rr-angle-small-down transition-transform group-hover:rotate-180\"></i>");
                        sb.AppendLine($"                </button>");
                        sb.AppendLine($"                <div class=\"absolute -left-8 top-full mt-3 w-screen max-w-md overflow-hidden rounded-3xl bg-white dark:bg-gray-800 shadow-lg ring-1 ring-gray-900/5 dark:ring-gray-700 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all duration-200 ease-out z-50 delay-200 group-hover:delay-0\">");
                        sb.AppendLine($"                    <div class=\"p-4\">");
                        foreach (var item in link.Items)
                        {
                            var itemHref = item.Link ?? "#";
                            var itemTarget = !string.IsNullOrEmpty(item.Target) ? $" target=\"{item.Target}\"" : "";
                            sb.AppendLine($"                        <div class=\"group relative flex items-center gap-x-6 rounded-lg p-4 text-sm leading-6 hover:bg-gray-50 dark:hover:bg-gray-700/50\">");
                            sb.AppendLine($"                            <div class=\"flex h-11 w-11 flex-none items-center justify-center rounded-lg bg-gray-50 dark:bg-gray-700 group-hover:bg-white dark:group-hover:bg-gray-600\">");
                            if (!string.IsNullOrEmpty(item.Icon))
                            {
                                sb.AppendLine($"                                <i class=\"fi fi-rr-{item.Icon} text-gray-600 dark:text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-400\"></i>");
                            }
                            else
                            {
                                sb.AppendLine($"                                <i class=\"fi fi-rr-arrow-small-right text-gray-600 dark:text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-400\"></i>");
                            }
                            sb.AppendLine($"                            </div>");
                            sb.AppendLine($"                            <div class=\"flex-auto\">");
                            sb.AppendLine($"                                <a href=\"{itemHref}\"{itemTarget} class=\"block font-semibold text-gray-900 dark:text-gray-100\">");
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
                                 if (!string.IsNullOrEmpty(footerItem.Icon))
                                 {
                                     sb.AppendLine($"                            <i class=\"fi fi-rr-{footerItem.Icon} text-gray-400 dark:text-gray-500\"></i>");
                                 }
                                 sb.AppendLine($"                            {footerItem.Text}");
                                 sb.AppendLine($"                        </a>");
                             }
                             sb.AppendLine($"                    </div>");
                        }
                        sb.AppendLine($"                </div>");
                        sb.AppendLine($"            </div>");
                    }
                    else
                    {
                         var href = link.Link ?? "#";
                         var iconHtml = string.IsNullOrEmpty(link.Icon) ? "" : $"<i class=\"fi fi-rr-{link.Icon} mr-1\"></i>";
                         var target = !string.IsNullOrEmpty(link.Target) ? $" target=\"{link.Target}\"" : "";
                         sb.AppendLine($"            <a href=\"{href}\"{target} class=\"hover:text-blue-600 dark:hover:text-blue-400 transition-colors flex items-center\">{iconHtml}{link.Text}</a>");
                    }
                }
            }
            sb.AppendLine("        </div>");

            sb.AppendLine("        <div class=\"flex items-center gap-4\">");
            sb.AppendLine("            <button onclick=\"openSearch()\" class=\"flex items-center gap-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 border border-transparent rounded-md px-4 py-2 transition-colors focus:ring-2 focus:ring-blue-500 w-64 justify-between\">");
            sb.AppendLine("                <div class=\"flex items-center gap-2\">");
            sb.AppendLine("                    <i class=\"fi fi-rr-search text-sm\"></i>");
            sb.AppendLine("                    <span class=\"text-sm font-medium\">Search</span>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <kbd class=\"hidden lg:inline text-xs bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded px-1.5 py-0.5 text-gray-500 dark:text-gray-400\">⌘K</kbd>");
            sb.AppendLine("            </button>");
            sb.AppendLine("            <button id=\"theme-toggle\" class=\"flex items-center justify-center text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 focus:outline-none p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors focus:ring-2 focus:ring-blue-500\">");
            sb.AppendLine("                <i class=\"fi fi-rr-moon dark:hidden text-lg\"></i>");
            sb.AppendLine("                <i class=\"fi fi-rr-sun hidden dark:block text-lg\"></i>");
            sb.AppendLine("            </button>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </header>");

            sb.AppendLine("    <div class=\"flex flex-1 overflow-hidden\">");

            // Sidebar
            sb.AppendLine("        <div id=\"sidebar-overlay\" class=\"fixed inset-0 bg-black/50 z-20 hidden md:hidden glassmorphism\"></div>");
            sb.AppendLine("        <aside id=\"sidebar\" class=\"w-64 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 overflow-y-auto flex flex-col shrink-0 fixed md:static inset-y-0 left-0 z-30 transform -translate-x-full md:translate-x-0 transition-transform duration-200 ease-in-out h-full\">");
            sb.AppendLine("            <nav class=\"p-4 flex-1\">");
            sb.AppendLine("                <div class=\"mb-6 relative\">");
            sb.AppendLine("                    <div class=\"absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none\">");
            sb.AppendLine("                        <i class=\"fi fi-rr-filter text-gray-400\"></i>");
            sb.AppendLine("                    </div>");
            sb.AppendLine("                    <input type=\"text\" id=\"sidebar-filter\" placeholder=\"Filter...\" class=\"w-full pl-10 pr-3 py-2 text-sm bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm\">");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <ul class=\"space-y-1\" id=\"sidebar-list\">");

            RenderSidebarItems(sb, sidebarLinks ?? new List<LinkConfig>(), 0);

            sb.AppendLine("                </ul>");
            sb.AppendLine("            </nav>");
            sb.AppendLine("        </aside>");

            // Content
            sb.AppendLine("        <div class=\"flex-1 flex overflow-hidden\">");
            sb.AppendLine("            <main class=\"flex-1 overflow-y-auto p-8 scroll-smooth\" id=\"main-scroll\">");
            sb.AppendLine("                <div class=\"max-w-4xl mx-auto prose dark:prose-invert\">");

            // Breadcrumbs
            if (navContext != null && navContext.Breadcrumbs.Any())
            {
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

            // Link Cleanup Logic
            var htmlContent = document.Html;
            if (!string.IsNullOrEmpty(htmlContent))
            {
                 // Strip extension, preserve relative/absolute nature
                 htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "href=\"((?!http:|https:|ftp:|mailto:|#|/)[^\"]+)\\.(md|html)\"", "href=\"$1\"");
            }

            // Watch Mode Button Injection
            if (_isWatchMode)
            {
                var editButtonHtml = "<button onclick=\"nekoOpenEditor()\" class=\"ml-3 inline-flex items-center rounded-md bg-white dark:bg-gray-800 px-2.5 py-1.5 text-sm font-semibold text-gray-900 dark:text-gray-100 shadow-sm ring-1 ring-inset ring-gray-300 dark:ring-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700 not-prose\"><i class=\"fi fi-rr-edit mr-1\"></i> Edit</button>";

                // Inject after the first header opening tag and content, but before closing tag?
                // Or just append to the content.
                // Regex: Find <h[1-6]...>(...)</h[1-6]>
                var regex = new Regex(@"(<h[1-6][^>]*>)(.*?)(</h[1-6]>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var match = regex.Match(htmlContent);
                if (match.Success)
                {
                    // Inject button after the content of the header, but inside the tag
                    htmlContent = regex.Replace(htmlContent, m => {
                        var openTag = m.Groups[1].Value;
                        var content = m.Groups[2].Value;
                        var closeTag = m.Groups[3].Value;

                        // Add classes to openTag
                        if (openTag.Contains("class=\""))
                        {
                            openTag = openTag.Replace("class=\"", "class=\"flex justify-between ");
                        }
                        else if (openTag.Contains("class='"))
                        {
                            openTag = openTag.Replace("class='", "class='flex justify-between ");
                        }
                        else
                        {
                            // Insert class before the closing >
                            openTag = openTag.Substring(0, openTag.Length - 1) + " class=\"flex justify-between\">";
                        }

                        return $"{openTag}{content}{editButtonHtml}{closeTag}";
                    }, 1);
                }
                else
                {
                    // If no header, prepend to top
                    htmlContent = editButtonHtml + htmlContent;
                }
            }

            if (!string.IsNullOrEmpty(document.FrontMatter.Password))
            {
                var encryptionResult = Neko.Encryption.PageEncryptor.Encrypt(htmlContent, document.FrontMatter.Password);

                sb.AppendLine($"<div id=\"content-container\">");
                sb.AppendLine($"    <div id=\"password-form-container\" class=\"flex flex-col items-center justify-center py-20 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-200 dark:border-gray-700\">");
                sb.AppendLine($"        <div class=\"p-8 bg-white dark:bg-gray-900 rounded-lg shadow-sm border border-gray-200 dark:border-gray-800 max-w-md w-full text-center\">");
                sb.AppendLine($"            <div class=\"w-12 h-12 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full flex items-center justify-center mx-auto mb-4\">");
                sb.AppendLine($"                <i class=\"fi fi-rr-lock text-xl\"></i>");
                sb.AppendLine($"            </div>");
                sb.AppendLine($"            <h2 class=\"text-xl font-bold text-gray-900 dark:text-gray-100 mb-2\">Password Protected</h2>");
                sb.AppendLine($"            <p class=\"text-sm text-gray-500 dark:text-gray-400 mb-6\">This page is password protected. Please enter the password to view the content.</p>");
                sb.AppendLine($"            <div class=\"space-y-4\">");
                sb.AppendLine($"                <div>");
                sb.AppendLine($"                    <input type=\"password\" id=\"password-input\" class=\"w-full px-4 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 text-gray-900 dark:text-gray-100\" placeholder=\"Enter password...\" autofocus>");
                sb.AppendLine($"                </div>");
                sb.AppendLine($"                <button id=\"password-submit\" class=\"w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:focus:ring-offset-gray-900\">Unlock</button>");
                sb.AppendLine($"                <p id=\"password-error\" class=\"hidden text-sm text-red-600 dark:text-red-400\">Incorrect password. Please try again.</p>");
                sb.AppendLine($"            </div>");
                sb.AppendLine($"        </div>");
                sb.AppendLine($"    </div>");
                sb.AppendLine($"</div>");

                // Inject Encrypted Payload
                var payload = System.Text.Json.JsonSerializer.Serialize(new {
                    salt = encryptionResult.Salt,
                    iv = encryptionResult.Iv,
                    data = encryptionResult.Data
                });
                sb.AppendLine($"<script type=\"application/json\" id=\"encrypted-data\">{payload}</script>");
                sb.AppendLine($"<script src=\"/assets/password.js\"></script>");
            }
            else
            {
                sb.AppendLine(htmlContent);
            }

            // Prev/Next Navigation
            if (navContext != null && (navContext.Prev != null || navContext.Next != null))
            {
                sb.AppendLine("                    <div class=\"grid grid-cols-1 md:grid-cols-2 gap-4 mt-12 pt-8 border-t border-gray-200 dark:border-gray-800 not-prose\">");

                // Prev
                if (navContext.Prev != null)
                {
                    sb.AppendLine($"                        <a href=\"{navContext.Prev.Url}\" class=\"flex flex-col p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all group\">");
                    sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-blue-600 dark:group-hover:text-blue-400\">Previous</span>");
                    sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\"><i class=\"fi fi-rr-arrow-small-left transition-transform group-hover:-translate-x-1\"></i> {navContext.Prev.Title}</span>");
                    sb.AppendLine("                        </a>");
                }
                else
                {
                    sb.AppendLine("                        <div></div>");
                }

                // Next
                if (navContext.Next != null)
                {
                    sb.AppendLine($"                        <a href=\"{navContext.Next.Url}\" class=\"flex flex-col items-end p-4 rounded border border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-all group text-right\">");
                    sb.AppendLine("                            <span class=\"text-xs text-gray-500 dark:text-gray-400 mb-1 group-hover:text-blue-600 dark:group-hover:text-blue-400\">Next</span>");
                    sb.AppendLine($"                            <span class=\"font-medium text-gray-900 dark:text-gray-100 flex items-center gap-2\">{navContext.Next.Title} <i class=\"fi fi-rr-arrow-small-right transition-transform group-hover:translate-x-1\"></i></span>");
                    sb.AppendLine("                        </a>");
                }

                sb.AppendLine("                    </div>");
            }

            if (backlinks != null && backlinks.Count > 0)
            {
                sb.AppendLine("                    <div class=\"mt-8 pt-8 border-t border-gray-200 dark:border-gray-800\">");
                sb.AppendLine("                        <h3 class=\"text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4\">Referenced by</h3>");
                sb.AppendLine("                        <ul class=\"space-y-2\">");
                foreach (var link in backlinks)
                {
                    sb.AppendLine($"                            <li><a href=\"{link.Url}\" class=\"text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-2\"><i class=\"fi fi-rr-arrow-small-right\"></i> {link.Title}</a></li>");
                }
                sb.AppendLine("                        </ul>");
                sb.AppendLine("                    </div>");
            }

            // Footer
            sb.AppendLine("                    <footer class=\"mt-12 py-6 border-t border-gray-200 dark:border-gray-800 text-sm text-gray-500 dark:text-gray-400 flex flex-col md:flex-row justify-between items-center not-prose\">");
            sb.AppendLine($"                        <div>&copy; {System.DateTime.Now.Year} {_config.Branding.Title}. All rights reserved.</div>");

            if (!string.IsNullOrEmpty(_config.Branding.Repository))
            {
                 sb.AppendLine($"                        <div class=\"mt-2 md:mt-0\">");
                 sb.AppendLine($"                            <a href=\"{_config.Branding.Repository}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"hover:text-blue-600 dark:hover:text-blue-400 transition-colors flex items-center gap-1\">");
                 sb.AppendLine("                                <i class=\"fi fi-rr-edit\"></i> Edit on GitHub");
                 sb.AppendLine("                            </a>");
                 sb.AppendLine("                        </div>");
            }
            else
            {
                 sb.AppendLine("                        <div class=\"mt-2 md:mt-0\">Powered by Neko</div>");
            }
            sb.AppendLine("                    </footer>");

            sb.AppendLine("                </div>");
            sb.AppendLine("            </main>");

            // TOC
            if (document.Toc != null && System.Linq.Enumerable.Any(document.Toc))
            {
                sb.AppendLine("            <aside id=\"toc-sidebar\" class=\"w-64 hidden xl:block shrink-0 overflow-y-auto border-l border-gray-200 dark:border-gray-800 p-6\">");
                sb.AppendLine("                <div class=\"sticky top-0\">");
                sb.AppendLine("                    <h5 class=\"text-xs font-semibold mb-4 text-gray-900 dark:text-gray-100 uppercase tracking-wider\">On this page</h5>");
                sb.AppendLine("                    <ul class=\"space-y-2.5 text-sm text-gray-500 dark:text-gray-400 border-l border-gray-200 dark:border-gray-800 relative\" id=\"toc-list\">");
                sb.AppendLine("                        <div id=\"toc-highlight\" class=\"absolute left-0 border-l-2 border-blue-600 dark:border-blue-400 transition-all duration-200 ease-in-out -ml-px\" style=\"top: 0; height: 0; opacity: 0;\"></div>");
                foreach (var item in document.Toc)
                {
                    if (item.Level < 2) continue;
                    var padding = item.Level == 2 ? "pl-4" : "pl-8";
                    sb.AppendLine($"                        <li><a href=\"#{item.Id}\" class=\"block {padding} hover:text-blue-600 dark:hover:text-blue-400 transition-colors toc-link\" data-id=\"{item.Id}\">{item.Title}</a></li>");
                }
                sb.AppendLine("                    </ul>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </aside>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");

            // Watch Mode UI
            if (_isWatchMode)
            {
                // Editor Modal
                sb.AppendLine("    <div id=\"neko-editor-modal\" class=\"fixed inset-0 z-50 hidden\">");
                sb.AppendLine("        <div class=\"absolute inset-0 bg-black/50 backdrop-blur-sm\"></div>");
                sb.AppendLine("        <div class=\"absolute inset-0 flex items-center justify-center p-4\">");
                sb.AppendLine("            <div class=\"bg-white dark:bg-gray-900 w-full max-w-6xl h-[90vh] rounded-lg shadow-xl flex flex-col border border-gray-200 dark:border-gray-700\">");
                sb.AppendLine("                <div class=\"flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700\">");
                sb.AppendLine("                    <h3 class=\"text-lg font-semibold text-gray-900 dark:text-gray-100\">Edit Content</h3>");
                sb.AppendLine("                    <div class=\"flex items-center gap-2\">");
                sb.AppendLine("                        <button onclick=\"nekoCancelEdit()\" class=\"px-3 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md transition-colors\">Cancel</button>");
                sb.AppendLine("                        <button onclick=\"nekoSaveContent()\" class=\"px-3 py-1.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-md transition-colors\">Save</button>");
                sb.AppendLine("                    </div>");
                sb.AppendLine("                </div>");
                sb.AppendLine("                <div id=\"neko-editor-container\" class=\"flex-1 overflow-hidden\"></div>");
                sb.AppendLine("            </div>");
                sb.AppendLine("        </div>");
                sb.AppendLine("    </div>");

                // Watch Script
                sb.AppendLine("    <script>");
                sb.AppendLine("        // WebSocket for Live Reload");
                sb.AppendLine("        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';");
                sb.AppendLine("        const wsUrl = `${protocol}//${window.location.host}/neko-live`;");
                sb.AppendLine("        let ws;");
                sb.AppendLine("");
                sb.AppendLine("        function connectWs() {");
                sb.AppendLine("            ws = new WebSocket(wsUrl);");
                sb.AppendLine("            ws.onmessage = (event) => {");
                sb.AppendLine("                if (event.data === 'reload') {");
                sb.AppendLine("                    window.location.reload();");
                sb.AppendLine("                }");
                sb.AppendLine("            };");
                sb.AppendLine("            ws.onclose = () => {");
                sb.AppendLine("                setTimeout(connectWs, 1000);");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine("        connectWs();");
                sb.AppendLine("");
                sb.AppendLine("        // Monaco Editor");
                sb.AppendLine("        let editor;");
                sb.AppendLine("        const modal = document.getElementById('neko-editor-modal');");
                sb.AppendLine("        const container = document.getElementById('neko-editor-container');");
                sb.AppendLine("");
              
                sb.AppendLine("        // Image Paste Handler");
                sb.AppendLine("        container.addEventListener('paste', (event) => {");
                sb.AppendLine("            if (!editor) return;");
                sb.AppendLine("            const items = (event.clipboardData || event.originalEvent.clipboardData).items;");
                sb.AppendLine("            for (const item of items) {");
                sb.AppendLine("                if (item.kind === 'file' && item.type.startsWith('image/')) {");
                sb.AppendLine("                    event.preventDefault();");
                sb.AppendLine("                    const file = item.getAsFile();");
                sb.AppendLine("                    const path = window.location.pathname;");
                sb.AppendLine("                    const formData = new FormData();");
                sb.AppendLine("                    formData.append('file', file);");
                sb.AppendLine("                    formData.append('path', path);");
                sb.AppendLine("                    fetch('/api/neko/upload-image', {");
                sb.AppendLine("                        method: 'POST',");
                sb.AppendLine("                        body: formData");
                sb.AppendLine("                    })");
                sb.AppendLine("                    .then(res => {");
                sb.AppendLine("                        if (!res.ok) throw new Error('Upload failed');");
                sb.AppendLine("                        return res.json();");
                sb.AppendLine("                    })");
                sb.AppendLine("                    .then(data => {");
                sb.AppendLine("                        const imageUrl = data.url;");
                sb.AppendLine("                        const markdownImage = `![Image](${imageUrl})`;");
                sb.AppendLine("                        const position = editor.getPosition();");
                sb.AppendLine("                        editor.executeEdits('paste-image', [{");
                sb.AppendLine("                            range: new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column),");
                sb.AppendLine("                            text: markdownImage,");
                sb.AppendLine("                            forceMoveMarkers: true");
                sb.AppendLine("                        }]);");
                sb.AppendLine("                    })");
                sb.AppendLine("                    .catch(err => {");
                sb.AppendLine("                        console.error('Image upload failed', err);");
                sb.AppendLine("                        alert('Failed to upload image: ' + err.message);");
                sb.AppendLine("                    });");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine("        });");
              
                sb.AppendLine("        function loadMonaco(callback) {");
                sb.AppendLine("            if (window.nekoMonacoLoaded) {");
                sb.AppendLine("                callback();");
                sb.AppendLine("                return;");
                sb.AppendLine("            }");
                sb.AppendLine("            const script = document.createElement('script');");
                sb.AppendLine("            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs/loader.min.js';");
                sb.AppendLine("            script.onload = () => {");
                sb.AppendLine("                window.nekoMonacoLoaded = true;");
                sb.AppendLine("                callback();");
                sb.AppendLine("            };");
                sb.AppendLine("            document.body.appendChild(script);");
                sb.AppendLine("        }");
                sb.AppendLine("");
              
                sb.AppendLine("        function nekoOpenEditor() {");
                sb.AppendLine("            const path = window.location.pathname;");
                sb.AppendLine("            fetch('/api/neko/content?path=' + encodeURIComponent(path))");
                sb.AppendLine("                .then(res => res.text())");
                sb.AppendLine("                .then(markdown => {");
                sb.AppendLine("                    modal.classList.remove('hidden');");
                sb.AppendLine("                    loadMonaco(() => {");
                sb.AppendLine("                        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }});");
                sb.AppendLine("                        require(['vs/editor/editor.main'], function() {");
                sb.AppendLine("                            if (editor) {");
                sb.AppendLine("                                editor.setValue(markdown);");
                sb.AppendLine("                            } else {");
                sb.AppendLine("                                editor = monaco.editor.create(container, {");
                sb.AppendLine("                                    value: markdown,");
                sb.AppendLine("                                    language: 'markdown',");
                sb.AppendLine("                                    theme: document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs',");
                sb.AppendLine("                                    automaticLayout: true,");
                sb.AppendLine("                                    minimap: { enabled: false },");
                sb.AppendLine("                                    wordWrap: 'on',");
                sb.AppendLine("                                    fontSize: 14");
                sb.AppendLine("                                });");
                sb.AppendLine("                            }");
                sb.AppendLine("                        });");
                sb.AppendLine("                    });");
                sb.AppendLine("                })");
                sb.AppendLine("                .catch(err => console.error('Failed to load content', err));");
                sb.AppendLine("        }");
                sb.AppendLine("");
                sb.AppendLine("        function nekoCancelEdit() {");
                sb.AppendLine("            modal.classList.add('hidden');");
                sb.AppendLine("        }");
                sb.AppendLine("");
                sb.AppendLine("        function nekoSaveContent() {");
                sb.AppendLine("            if (!editor) return;");
                sb.AppendLine("            const content = editor.getValue();");
                sb.AppendLine("            const path = window.location.pathname;");
                sb.AppendLine("            fetch('/api/neko/content', {");
                sb.AppendLine("                method: 'POST',");
                sb.AppendLine("                headers: { 'Content-Type': 'application/json' },");
                sb.AppendLine("                body: JSON.stringify({ path, content })");
                sb.AppendLine("            })");
                sb.AppendLine("            .then(res => {");
                sb.AppendLine("                if (res.ok) {");
                sb.AppendLine("                    nekoCancelEdit();");
                sb.AppendLine("                    // Notification handled by websocket reload usually, but we can do instant feedback");
                sb.AppendLine("                } else {");
                sb.AppendLine("                    alert('Failed to save');");
                sb.AppendLine("                }");
                sb.AppendLine("            })");
                sb.AppendLine("            .catch(err => alert('Failed to save: ' + err));");
                sb.AppendLine("        }");
                sb.AppendLine("    </script>");
            }

            // Scripts
            sb.AppendLine("    <script>");

            // Banner
            if (_config.Banner != null && _config.Banner.Visible && !string.IsNullOrEmpty(_config.Banner.Text))
            {
                var banner = _config.Banner;
                var bannerId = banner.Id ?? "neko-banner";
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

            // Mobile Menu
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

            // Sidebar Scroll Preservation
            sb.AppendLine("        if (sidebar) {");
            var keyBase = System.Text.RegularExpressions.Regex.Replace(_config.Branding.Title ?? "neko", "[^a-zA-Z0-9]", "-").ToLower();
            sb.AppendLine($"            const scrollKey = '{keyBase}-sidebar-scroll';");
            sb.AppendLine($"            const timeKey = '{keyBase}-sidebar-scroll-time';");
            sb.AppendLine("            const savedScroll = localStorage.getItem(scrollKey);");
            sb.AppendLine("            const savedTime = localStorage.getItem(timeKey);");
            sb.AppendLine("            if (savedScroll && savedTime) {");
            sb.AppendLine("                const now = new Date().getTime();");
            sb.AppendLine("                if (now - parseInt(savedTime) < 60000) {");
            sb.AppendLine("                    sidebar.scrollTop = parseInt(savedScroll);");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            let timeout;");
            sb.AppendLine("            sidebar.addEventListener('scroll', () => {");
            sb.AppendLine("                clearTimeout(timeout);");
            sb.AppendLine("                timeout = setTimeout(() => {");
            sb.AppendLine("                    localStorage.setItem(scrollKey, sidebar.scrollTop);");
            sb.AppendLine("                    localStorage.setItem(timeKey, new Date().getTime());");
            sb.AppendLine("                }, 100);");
            sb.AppendLine("            });");
            sb.AppendLine("        }");

            // Sidebar Filter
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

            // TOC Highlighting
            // Active Link Highlighting
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', () => {");
            sb.AppendLine("            let currentPath = window.location.pathname;");
            sb.AppendLine("            if (currentPath.endsWith('.html')) currentPath = currentPath.substring(0, currentPath.length - 5);");
            sb.AppendLine("            const sidebarLinks = document.querySelectorAll('#sidebar-list a');");
            sb.AppendLine("            sidebarLinks.forEach(link => {");
            sb.AppendLine("                const href = link.getAttribute('href');");
            sb.AppendLine("                if (href === currentPath || (href !== '/' && currentPath.startsWith(href) && (href.endsWith('/') || currentPath.charAt(href.length) === '/'))) {");
            sb.AppendLine("                    link.classList.add('bg-blue-50', 'dark:bg-blue-900', 'text-blue-700', 'dark:text-blue-300', 'font-medium');");
            sb.AppendLine("                    link.classList.remove('text-gray-700', 'dark:text-gray-200');");
            sb.AppendLine("                    // Open parent details");
            sb.AppendLine("                    let parent = link.parentElement;");
            sb.AppendLine("                    while (parent && parent.id !== 'sidebar-list') {");
            sb.AppendLine("                        if (parent.tagName === 'DETAILS') {");
            sb.AppendLine("                            parent.open = true;");
            sb.AppendLine("                        }");
            sb.AppendLine("                        parent = parent.parentElement;");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");

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
            sb.AppendLine("                link.classList.remove('text-blue-600', 'dark:text-blue-400', 'font-medium');");
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
            sb.AppendLine("                link.classList.add('text-blue-600', 'dark:text-blue-400', 'font-medium');");
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

            // Theme Switch
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

            // Tab Script
            sb.AppendLine("        function openTab(evt, group, tabId) {");
            sb.AppendLine("            var contents = document.querySelectorAll(`[id^='tab-${group}-']`);");
            sb.AppendLine("            contents.forEach(c => c.classList.add('hidden'));");
            sb.AppendLine("            var buttons = evt.currentTarget.parentElement.children;");
            sb.AppendLine("            for (var i = 0; i < buttons.length; i++) {");
            sb.AppendLine("                buttons[i].classList.remove('border-blue-500', 'text-blue-600', 'dark:text-blue-400', 'font-medium');");
            sb.AppendLine("                buttons[i].classList.add('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            }");
            sb.AppendLine("            document.getElementById(tabId).classList.remove('hidden');");
            sb.AppendLine("            evt.currentTarget.classList.remove('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            evt.currentTarget.classList.add('border-blue-500', 'text-blue-600', 'dark:text-blue-400', 'font-medium');");
            sb.AppendLine("        }");

            // Copy Code Script
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

            // Init Highlight.js
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
            sb.AppendLine("                    link.className = 'absolute -left-6 top-0 bottom-0 flex items-center justify-center text-blue-500 opacity-0 group-hover:opacity-100 transition-opacity p-1 no-underline';");
            sb.AppendLine("                    link.innerHTML = '<i class=\"fi fi-rr-hashtag\"></i>';");
            sb.AppendLine("                    link.setAttribute('aria-label', 'Anchor');");
            sb.AppendLine("                    heading.prepend(link);");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");

            // Line Highlighting
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
            sb.AppendLine("    </script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private void RenderSidebarItems(StringBuilder sb, List<LinkConfig> links, int level)
        {
            if (links == null || links.Count == 0) return;

            foreach (var link in links)
            {
                var iconHtml = "";
                if (!string.IsNullOrEmpty(link.Icon))
                {
                    iconHtml = $"<i class=\"fi fi-rr-{link.Icon}\"></i>";
                }
                else
                {
                     // Add invisible icon spacer to align with siblings
                     iconHtml = "<i class=\"fi fi-rr-circle opacity-0\"></i>";
                }

                if (link.Items != null && link.Items.Count > 0)
                {
                    if (level == 0)
                    {
                        // Render as a flat header
                        sb.AppendLine($"                    <li class=\"mt-10 mb-2 first:mt-0 px-2\">");
                        sb.AppendLine($"                        <span class=\"text-xs font-bold text-gray-500 uppercase tracking-wider\">{link.Text}</span>");
                        sb.AppendLine($"                    </li>");

                        RenderSidebarItems(sb, link.Items, level + 1);
                    }
                    else
                    {
                        // Render as a collapsible group
                        sb.AppendLine($"                    <li class=\"space-y-1\">");
                        sb.AppendLine($"                        <details class=\"group\" open>");
                        sb.AppendLine($"                            <summary class=\"flex items-center justify-between py-1 px-2 text-[13px] whitespace-nowrap font-medium text-gray-700 dark:text-gray-200 rounded hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer select-none\">");
                        sb.AppendLine($"                                <span class=\"flex items-center gap-2\">{iconHtml} {link.Text}</span>");
                        sb.AppendLine($"                                <i class=\"fi fi-rr-angle-small-down transition-transform group-open:rotate-180\"></i>");
                        sb.AppendLine($"                            </summary>");
                        sb.AppendLine($"                            <ul class=\"pl-0 space-y-1 mt-1 border-l border-gray-200 dark:border-gray-700 ml-3\">");

                        RenderSidebarItems(sb, link.Items, level + 1);

                        sb.AppendLine($"                            </ul>");
                        sb.AppendLine($"                        </details>");
                        sb.AppendLine($"                    </li>");
                    }
                }
                else
                {
                    // Render as a leaf link
                    var href = link.Link ?? "#";
                    if (href.EndsWith(".md")) href = href.Substring(0, href.Length - 3);
                    if (href.EndsWith(".html")) href = href.Substring(0, href.Length - 5);

                    // Ensure absolute path for internal links if not already absolute/external
                    if (!href.StartsWith("/") && !href.Contains("://") && href != "#")
                    {
                        href = "/" + href;
                    }

                    sb.AppendLine($"                    <li><a href=\"{href}\" class=\"block py-1 px-2 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2 text-[13px] whitespace-nowrap text-gray-700 dark:text-gray-300\">{iconHtml} {link.Text}</a></li>");
                }
            }
        }
    }
}
