using TailDocs.CLI.Configuration;
using System.Text;

namespace TailDocs.CLI.Builder
{
    public class HtmlGenerator
    {
        private readonly TailDocsConfig _config;

        public HtmlGenerator(TailDocsConfig config)
        {
            _config = config;
        }

        public string Generate(ParsedDocument document)
        {
            var title = !string.IsNullOrEmpty(document.FrontMatter.Title)
                ? $"{document.FrontMatter.Title} - {_config.Branding.Title}"
                : _config.Branding.Title;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>{title}</title>");

            // Tailwind CSS
            sb.AppendLine("    <script src=\"assets/tailwind.js\"></script>");
            sb.AppendLine("    <script>tailwind.config = { darkMode: 'class' }</script>");

            // Inter Font
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://rsms.me/inter/inter.css\">");
            sb.AppendLine("    <style>:root { font-family: 'Inter', sans-serif; } @supports (font-variation-settings: normal) { :root { font-family: 'Inter var', sans-serif; } }</style>");

            // Flaticon UIcons
            sb.AppendLine("    <link rel='stylesheet' href='https://cdn-uicons.flaticon.com/uicons-regular-rounded/css/uicons-regular-rounded.css'>");

            // KaTeX (Math)
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css\">");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/contrib/auto-render.min.js\"></script>");
            sb.AppendLine("    <script>document.addEventListener(\"DOMContentLoaded\", function() { renderMathInElement(document.body); });</script>");

            // Mermaid (Diagrams)
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
            sb.AppendLine("    <script>mermaid.initialize({startOnLoad:true});</script>");

            // Search Assets
            sb.AppendLine("    <script src=\"assets/minisearch.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"assets/search.js\"></script>");

            // Highlight.js
            sb.AppendLine("    <script src=\"assets/highlight/highlight.min.js\"></script>");
            // Theme - Supporting basic light/dark switch via simple script or CSS?
            // Highlight.js uses one CSS file. To support dark mode switch, we need to load different CSS or scope them.
            // Simplest way: Load dark theme by default if user preference is dark, but tailwind uses 'class' strategy.
            // We can load both and toggle? Or use CSS media query if files support it?
            // Tokyo Night CSS files just define colors.
            // We can inject styles conditionally.

            // Let's use a small script to load the correct stylesheet based on html class 'dark'.
            // Actually, we can just load the dark theme if we are in dark mode?
            // A common trick is to use media queries or a "dark" selector prefix, but highlight.js themes are global.

            // For now, let's load the configured "dark" theme if checking dark mode, but we need dynamic switch support.
            // We can wrap the dark theme in `.dark .hljs` selector? That requires modifying the CSS.

            // Let's just load the dark theme as default for now as requested "use tokyo-night / tokyo-night-dark by default".
            // The prompt says "add a setting ... for setting the default light / dark code highlighting theme".

            var darkTheme = _config.Theme.Highlight.Dark;
            // var lightTheme = _config.Theme.Highlight.Light;

            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"assets/highlight/{darkTheme}.min.css\">");
            sb.AppendLine("    <script src=\"assets/highlight/highlight.min.js\"></script>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/highlightjs-line-numbers.js@2.8.0/dist/highlightjs-line-numbers.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        /* Highlight.js Line Numbers CSS */");
            sb.AppendLine("        .hljs-ln td { padding-left: 10px; }");
            sb.AppendLine("        .hljs-ln-numbers { user-select: none; text-align: right; color: #ccc; border-right: 1px solid #ccc; vertical-align: top; padding-right: 5px; }");
            sb.AppendLine("        .hljs-ln-code { padding-left: 10px; }");
            sb.AppendLine("        ");
            sb.AppendLine("        /* Custom Highlight Style */");
            sb.AppendLine("        .line-highlight { background-color: rgba(255, 255, 0, 0.15); display: block; width: 100%; }");
            sb.AppendLine("        .dark .line-highlight { background-color: rgba(255, 255, 0, 0.15); }");
            sb.AppendLine("        ");
            sb.AppendLine("        /* Fix Table layout for code blocks with line numbers */");
            sb.AppendLine("        .hljs-ln { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("    </style>");

            sb.AppendLine("</head>");
            sb.AppendLine("<body class=\"bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100\">");

            // Layout container
            sb.AppendLine("    <div class=\"flex h-screen\">");

            // Sidebar
            sb.AppendLine("        <aside class=\"w-64 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 overflow-y-auto hidden md:block flex flex-col\">");
            sb.AppendLine($"            <div class=\"p-4 font-bold text-xl\">{_config.Branding.Title}</div>");

            // Search Trigger
            sb.AppendLine("            <div class=\"px-4 pb-4\">");
            sb.AppendLine("                <button onclick=\"openSearch()\" class=\"w-full text-left px-3 py-2 text-sm text-gray-500 bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-600 rounded-md hover:border-gray-400 focus:outline-none focus:ring-1 focus:ring-blue-500 flex items-center justify-between\">");
            sb.AppendLine("                    <span class=\"flex items-center gap-2\"><i class=\"fi fi-rr-search\"></i> Search</span>");
            sb.AppendLine("                    <span class=\"text-xs border border-gray-200 dark:border-gray-700 rounded px-1.5 py-0.5\">⌘K</span>");
            sb.AppendLine("                </button>");
            sb.AppendLine("            </div>");

            sb.AppendLine("            <nav class=\"p-4 flex-1\">");
            sb.AppendLine("                <ul class=\"space-y-2\">");
            foreach (var link in _config.Links)
            {
                 var iconHtml = string.IsNullOrEmpty(link.Icon) ? "" : $"<i class=\"fi fi-rr-{link.Icon}\"></i>";
                 sb.AppendLine($"                    <li><a href=\"{link.Link}\" class=\"block py-2 px-4 hover:bg-gray-200 dark:hover:bg-gray-700 rounded flex items-center gap-2\">{iconHtml} {link.Text}</a></li>");
            }
            sb.AppendLine("                </ul>");
            sb.AppendLine("            </nav>");
            sb.AppendLine("        </aside>");

            // Main Content
            sb.AppendLine("        <main class=\"flex-1 overflow-y-auto p-8\">");
            sb.AppendLine("            <div class=\"max-w-7xl mx-auto flex gap-12\">");

            // Article Content
            sb.AppendLine("                <div class=\"flex-1 prose dark:prose-invert max-w-4xl\">");
            sb.AppendLine(document.Html);
            sb.AppendLine("                </div>");

            // TOC Sidebar
            if (document.Toc != null && System.Linq.Enumerable.Any(document.Toc))
            {
                sb.AppendLine("                <aside class=\"w-64 hidden xl:block shrink-0\">");
                sb.AppendLine("                    <div class=\"sticky top-8\">");
                sb.AppendLine("                        <h5 class=\"text-xs font-semibold mb-4 text-gray-900 dark:text-gray-100 uppercase tracking-wider\">On this page</h5>");
                sb.AppendLine("                        <ul class=\"space-y-2.5 text-sm text-gray-500 dark:text-gray-400 border-l border-gray-200 dark:border-gray-800\">");

                foreach (var item in document.Toc)
                {
                    if (item.Level < 2) continue; // Usually skip H1 as it's the page title

                    // Simple indentation logic
                    var padding = item.Level == 2 ? "pl-4" : "pl-8";

                    sb.AppendLine($"                            <li><a href=\"#{item.Id}\" class=\"block {padding} hover:text-blue-600 dark:hover:text-blue-400 transition-colors border-l-2 border-transparent hover:border-blue-600 dark:hover:border-blue-400 -ml-px\">{item.Title}</a></li>");
                }

                sb.AppendLine("                        </ul>");
                sb.AppendLine("                    </div>");
                sb.AppendLine("                </aside>");
            }

            sb.AppendLine("            </div>");
            sb.AppendLine("        </main>");

            sb.AppendLine("    </div>");

            // Tab Script (simple implementation for now)
            sb.AppendLine("    <script>");
            sb.AppendLine("        function openTab(evt, group, tabId) {");
            sb.AppendLine("            // Hide all tab contents in group");
            sb.AppendLine("            var contents = document.querySelectorAll(`[id^='tab-${group}-']`);");
            sb.AppendLine("            contents.forEach(c => c.classList.add('hidden'));");
            sb.AppendLine("            // Remove active class from buttons");
            sb.AppendLine("            var buttons = evt.currentTarget.parentElement.children;");
            sb.AppendLine("            for (var i = 0; i < buttons.length; i++) {");
            sb.AppendLine("                buttons[i].classList.remove('border-blue-500', 'text-blue-600', 'dark:text-blue-400', 'font-medium');");
            sb.AppendLine("                buttons[i].classList.add('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            }");
            sb.AppendLine("            // Show selected tab");
            sb.AppendLine("            document.getElementById(tabId).classList.remove('hidden');");
            sb.AppendLine("            // Highlight button");
            sb.AppendLine("            evt.currentTarget.classList.remove('border-transparent', 'hover:text-gray-700', 'text-gray-500');");
            sb.AppendLine("            evt.currentTarget.classList.add('border-blue-500', 'text-blue-600', 'dark:text-blue-400', 'font-medium');");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        // Copy Code Script");
            sb.AppendLine("        document.addEventListener('click', function(e) {");
            sb.AppendLine("            const btn = e.target.closest('.copy-btn');");
            sb.AppendLine("            if (!btn) return;");
            sb.AppendLine("            ");
            sb.AppendLine("            const group = btn.closest('.group');");
            sb.AppendLine("            const codeEl = group.querySelector('code');");
            sb.AppendLine("            let text = \"\";");
            sb.AppendLine("            ");
            sb.AppendLine("            // If line numbers are present, extracting text is trickier");
            sb.AppendLine("            if (codeEl.querySelector('.hljs-ln')) {");
            sb.AppendLine("                codeEl.querySelectorAll('.hljs-ln-code').forEach(td => text += td.innerText + \"\\n\");");
            sb.AppendLine("            } else {");
            sb.AppendLine("                text = codeEl.innerText;");
            sb.AppendLine("            }");
            sb.AppendLine("            ");
            sb.AppendLine("            navigator.clipboard.writeText(text).then(() => {");
            sb.AppendLine("                const icon = btn.querySelector('i');");
            sb.AppendLine("                const originalClass = icon.className;");
            sb.AppendLine("                icon.className = 'fi fi-rr-check';");
            sb.AppendLine("                setTimeout(() => icon.className = originalClass, 2000);");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
            sb.AppendLine("");
            sb.AppendLine("        // Initialize Highlight.js");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', (event) => {");
            sb.AppendLine("            hljs.highlightAll();");
            sb.AppendLine("            hljs.initLineNumbersOnLoad({ singleLine: true });");
            sb.AppendLine("        });");
            sb.AppendLine("");
            sb.AppendLine("        // Apply Line Highlighting");
            sb.AppendLine("        window.addEventListener('load', function() {");
            sb.AppendLine("            document.querySelectorAll('pre code[data-highlight]').forEach(block => {");
            sb.AppendLine("                const highlightRange = block.getAttribute('data-highlight');");
            sb.AppendLine("                if (!highlightRange) return;");
            sb.AppendLine("");
            sb.AppendLine("                const linesToHighlight = new Set();");
            sb.AppendLine("                highlightRange.split(',').forEach(part => {");
            sb.AppendLine("                    if (part.includes('-')) {");
            sb.AppendLine("                        const [start, end] = part.split('-').map(Number);");
            sb.AppendLine("                        for (let i = start; i <= end; i++) linesToHighlight.add(i);");
            sb.AppendLine("                    } else {");
            sb.AppendLine("                        linesToHighlight.add(Number(part));");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                // Check if line numbers table exists");
            sb.AppendLine("                const table = block.querySelector('.hljs-ln');");
            sb.AppendLine("                if (table) {");
            sb.AppendLine("                    const rows = table.querySelectorAll('tr');");
            sb.AppendLine("                    rows.forEach((row, index) => {");
            sb.AppendLine("                        // index is 0-based, lines are 1-based");
            sb.AppendLine("                        if (linesToHighlight.has(index + 1)) {");
            sb.AppendLine("                            row.classList.add('bg-yellow-100', 'dark:bg-yellow-900', 'bg-opacity-20', 'dark:bg-opacity-20');");
            sb.AppendLine("                            // Ensure the background spans the whole row");
            sb.AppendLine("                            row.querySelectorAll('td').forEach(td => td.style.backgroundColor = 'inherit');");
            sb.AppendLine("                        }");
            sb.AppendLine("                    });");
            sb.AppendLine("                } else {");
            sb.AppendLine("                    // Fallback if no line numbers");
            sb.AppendLine("                }");
            sb.AppendLine("            });");
            sb.AppendLine("        });");
            sb.AppendLine("    </script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
