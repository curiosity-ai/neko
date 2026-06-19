using Neko.Configuration;
using System.Collections.Generic;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        private void GenerateHead(StringBuilder sb, string title, string description)
        {
            sb.AppendLine("<head>");
            RenderHeadMeta(sb, title, description);
            RenderHeadTailwindAndTheme(sb);
            RenderHeadNekoConfig(sb);
            RenderHeadFontsAndIcons(sb);
            RenderHeadKatex(sb);
            RenderHeadMermaid(sb);
            RenderHeadAuxiliaryScripts(sb);
            RenderHeadHighlightJs(sb);
            RenderHeadViewTransitions(sb);
            RenderHeadDarkModeInit(sb);

            if (!string.IsNullOrEmpty(_headIncludes))
            {
                sb.AppendLine(_headIncludes);
            }

            sb.AppendLine("</head>");
        }

        private void RenderHeadMeta(StringBuilder sb, string title, string description)
        {
            sb.AppendLine("    <meta charset=\"UTF-8\">");
            sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"    <title>{title}</title>");

            if (!string.IsNullOrEmpty(_config.Branding.Favicon))
            {
                sb.AppendLine($"    <link rel=\"icon\" href=\"{EscapeHtmlAttr(_config.Branding.Favicon)}\">");
            }

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine($"    <meta name=\"description\" content=\"{description}\">");
                sb.AppendLine($"    <meta property=\"og:description\" content=\"{description}\">");
                sb.AppendLine($"    <meta name=\"twitter:description\" content=\"{description}\">");
            }

            if (!string.IsNullOrEmpty(_config.Meta.Keywords)) sb.AppendLine($"    <meta name=\"keywords\" content=\"{_config.Meta.Keywords}\">");
            if (!string.IsNullOrEmpty(_config.Meta.Author)) sb.AppendLine($"    <meta name=\"author\" content=\"{_config.Meta.Author}\">");

            sb.AppendLine($"    <meta property=\"og:title\" content=\"{title}\">");
            sb.AppendLine($"    <meta name=\"twitter:title\" content=\"{title}\">");

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
        }

        private void RenderHeadTailwindAndTheme(StringBuilder sb)
        {
            // Tailwind CSS — a real, cacheable stylesheet generated at build time
            // by the pure-C# TailwindGenerator (no Play CDN, no Node, no binary).
            // This avoids the CDN's flash of unstyled content on navigation (the
            // dark: utilities don't exist until the CDN script runs in the
            // browser). Each (sub-)site links its OWN tailwind.css under its route
            // prefix so multi-repo projects each get their used-class set + palette.
            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/tailwind.css\">");

            var (primaryTheme, accentTheme) = ThemeDefinitions.ResolvePalettes(_config);
            var themeJson = System.Text.Json.JsonSerializer.Serialize(primaryTheme);
            var accentJson = System.Text.Json.JsonSerializer.Serialize(accentTheme);
            // Expose the resolved palettes for runtime JS (dynamic theming, charts).
            sb.AppendLine($"    <script>window.nekoThemeColors = {themeJson}; window.nekoAccentColors = {accentJson};</script>");

            // Curiosity-inspired neutral palette overrides (deep navy in dark mode, soft white in light mode)
            sb.AppendLine("    <style>");
            sb.AppendLine("      :root { --neko-bg: #ffffff; --neko-bg-elevated: #f8fafc; --neko-bg-muted: #f1f5f9; --neko-border: #e5e7eb; }");
            sb.AppendLine("      html.dark { --neko-bg: #050914; --neko-bg-elevated: #0b1226; --neko-bg-muted: #111a33; --neko-border: #1f2a44; }");
            sb.AppendLine("      html.dark body { background-color: var(--neko-bg); }");
            sb.AppendLine("      html.dark .dark\\:bg-gray-900 { background-color: var(--neko-bg) !important; }");
            sb.AppendLine("      html.dark .dark\\:bg-gray-800 { background-color: var(--neko-bg-elevated) !important; }");
            sb.AppendLine("      html.dark .dark\\:border-gray-800 { border-color: var(--neko-border) !important; }");
            sb.AppendLine("      html.dark .dark\\:border-gray-700 { border-color: var(--neko-border) !important; }");
            sb.AppendLine("      .neko-text-gradient { background-image: linear-gradient(90deg, var(--neko-grad-from, #5b94ff) 0%, var(--neko-grad-to, #a78bfa) 100%); -webkit-background-clip: text; background-clip: text; color: transparent; }");
            sb.AppendLine($"      :root {{ --neko-grad-from: {(primaryTheme.TryGetValue("400", out var pf) ? pf : "#5b94ff")}; --neko-grad-to: {(accentTheme.TryGetValue("400", out var af) ? af : "#a78bfa")}; }}");
            sb.AppendLine("    </style>");
        }

        private void RenderHeadNekoConfig(StringBuilder sb)
        {
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var snippetsJson = _config.Snippets != null ? System.Text.Json.JsonSerializer.Serialize(_config.Snippets, jsonOptions) : "{}";
            var brandingJson = _config.Branding != null ? System.Text.Json.JsonSerializer.Serialize(_config.Branding, jsonOptions) : "{}";
            var routePrefixJson = System.Text.Json.JsonSerializer.Serialize(SiteBuilder.CurrentRoutePrefix ?? string.Empty);
            sb.AppendLine($"    <script>window.nekoConfig = {{ snippets: {snippetsJson}, branding: {brandingJson} }}; window.NEKO_ROUTE_PREFIX = {routePrefixJson};</script>");
        }

        private void RenderHeadFontsAndIcons(StringBuilder sb)
        {
            // Inter Font
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://rsms.me/inter/inter.css\">");
            sb.AppendLine("    <style>:root { font-family: 'Inter', sans-serif; } @supports (font-variation-settings: normal) { :root { font-family: 'Inter var', sans-serif; } }</style>");

            // Flaticon UIcons (self-hosted v4 — see Neko.Tools.UIcons)
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"/assets/uicons-regular-rounded.css\">");
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"/assets/uicons-brands.css\">");

            // Emoji CSS
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"/assets/emoji.css\">");

        }

        private void RenderHeadKatex(StringBuilder sb)
        {
            sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.css\">");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/katex.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"https://cdn.jsdelivr.net/npm/katex@0.16.8/dist/contrib/auto-render.min.js\"></script>");
            sb.AppendLine("    <script>document.addEventListener(\"DOMContentLoaded\", function() { renderMathInElement(document.body); });</script>");
        }

        private void RenderHeadMermaid(StringBuilder sb)
        {
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
            sb.AppendLine("    <script src=\"https://unpkg.com/panzoom@9.4.0/dist/panzoom.min.js\"></script>");
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
            sb.AppendLine("                        <div class=\"dark:hidden w-full h-full flex items-center justify-center min-h-[inherit]\">${rLight.svg}</div>");
            sb.AppendLine("                        <div class=\"hidden dark:block w-full h-full flex items-center justify-center min-h-[inherit]\">${rDark.svg}</div>");
            sb.AppendLine("                        <div class=\"absolute bottom-4 right-4 flex bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-sm z-10 overflow-hidden\">");
            sb.AppendLine("                            <button class=\"zoom-in-btn px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition-colors\" title=\"Zoom In\"><i class=\"fi fi-rr-plus\"></i></button>");
            sb.AppendLine("                            <button class=\"zoom-out-btn px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition-colors border-l border-r border-gray-200 dark:border-gray-700\" title=\"Zoom Out\"><i class=\"fi fi-rr-minus\"></i></button>");
            sb.AppendLine("                            <button class=\"zoom-reset-btn px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition-colors\" title=\"Reset Zoom\"><i class=\"fi fi-rr-expand\"></i></button>");
            sb.AppendLine("                        </div>");
            sb.AppendLine("                    `;");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const lightSvg = el.querySelector('.dark\\\\:hidden > svg');");
            sb.AppendLine("                    const darkSvg = el.querySelector('.hidden.dark\\\\:block > svg');");
            sb.AppendLine("                    lightSvg.classList.add(\"w-full\");");
            sb.AppendLine("                    lightSvg.classList.add(\"h-full\");");
            sb.AppendLine("                    lightSvg.classList.add(\"min-h-[inherit]\");");
            sb.AppendLine("                    darkSvg.classList.add(\"w-full\");");
            sb.AppendLine("                    darkSvg.classList.add(\"h-full\");");
            sb.AppendLine("                    darkSvg.classList.add(\"min-h-[inherit]\");");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (rLight.bindFunctions && lightSvg) rLight.bindFunctions(lightSvg);");
            sb.AppendLine("                    if (rDark.bindFunctions && darkSvg) rDark.bindFunctions(darkSvg);");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const instances = [];");
            sb.AppendLine("                    // beforeWheel returns true so the wheel never zooms the diagram —");
            sb.AppendLine("                    // this lets page scroll pass through instead of being captured.");
            sb.AppendLine("                    const pzOptions = { bounds: true, boundsPadding: 0.1, beforeWheel: () => true };");
            sb.AppendLine("                    if (lightSvg) instances.push({svg: lightSvg, gpz: panzoom(lightSvg, pzOptions)});");
            sb.AppendLine("                    if (darkSvg) instances.push({svg: darkSvg, gpz: panzoom(darkSvg, pzOptions)});");
            sb.AppendLine("                    ");
            sb.AppendLine("                    const zoomInBtn = el.querySelector('.zoom-in-btn');");
            sb.AppendLine("                    const zoomOutBtn = el.querySelector('.zoom-out-btn');");
            sb.AppendLine("                    const zoomResetBtn = el.querySelector('.zoom-reset-btn');");
            sb.AppendLine("                    ");
            sb.AppendLine("                    if (zoomInBtn) {");
            sb.AppendLine("                        zoomInBtn.addEventListener('click', (e) => {");
            sb.AppendLine("                            e.preventDefault();");
            sb.AppendLine("                            instances.forEach(instance => {");
            sb.AppendLine("                                const rect = el.getBoundingClientRect();");
            sb.AppendLine("                                instance.gpz.smoothZoom(rect.left + rect.width / 2, rect.top + rect.height / 2, 1.2);");
            sb.AppendLine("                            });");
            sb.AppendLine("                        });");
            sb.AppendLine("                    }");
            sb.AppendLine("                    if (zoomOutBtn) {");
            sb.AppendLine("                        zoomOutBtn.addEventListener('click', (e) => {");
            sb.AppendLine("                            e.preventDefault();");
            sb.AppendLine("                            instances.forEach(instance => {");
            sb.AppendLine("                                const rect = el.getBoundingClientRect();");
            sb.AppendLine("                                instance.gpz.smoothZoom(rect.left + rect.width / 2, rect.top + rect.height / 2, 0.8);");
            sb.AppendLine("                            });");
            sb.AppendLine("                        });");
            sb.AppendLine("                    }");
            sb.AppendLine("                    if (zoomResetBtn) {");
            sb.AppendLine("                        zoomResetBtn.addEventListener('click', (e) => {");
            sb.AppendLine("                            e.preventDefault();");
            sb.AppendLine("                            instances.forEach(instance => {");
            sb.AppendLine("                                zoomToFit(instance.svg, instance.gpz);");
            sb.AppendLine("                            });");
            sb.AppendLine("                        });");
            sb.AppendLine("                        window.setTimeout(_ => zoomResetBtn.click(), 250);");
            sb.AppendLine("                    }");
            sb.AppendLine("                } catch (e) {");
            sb.AppendLine("                    console.error('Mermaid rendering failed:', e);");
            sb.AppendLine("                    el.innerHTML = `<div class=\"text-red-500\">Error rendering diagram</div>`;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        ");
            sb.AppendLine("        document.addEventListener('DOMContentLoaded', renderMermaid);");
            sb.AppendLine(@"async function zoomToFit(svg, gpz) {
                const parent = svg.parentElement.parentElement;
                const rectParent = parent.getBoundingClientRect();
                const rectScene = svg.getBoundingClientRect();

                const xys = gpz.getTransform();
                const originWidth  = (rectScene.width + 20) / xys.scale;
                const originHeight = (rectScene.height + 20) / xys.scale;
                const zoomX = (rectParent.width - 40) / originWidth;
                const zoomY = (rectParent.height - 40) / originHeight;

                let targetScale = zoomX < zoomY ? zoomX : zoomY;

                //when target scale is the same as currently, we reset back to 100%, so it acts as toggle.
                if (Math.abs(targetScale - xys.scale) < 0.005) {
                    //reset to 100%
                    targetScale = 1;
                }

                const targetWidth = originWidth * xys.scale;
                const targetHeight = originHeight * xys.scale;
                const newX = targetWidth > rectParent.width ? -(targetWidth / 2) + rectParent.width / 2 : (rectParent.width / 2) - (targetWidth / 2);
                const newY = targetHeight > rectParent.height ? -(targetHeight / 2) + rectParent.height / 2 : (rectParent.height / 2) - (targetHeight / 2);

                //we need to cancel current running animations
                gpz.pause();
                gpz.resume();

                const xDiff = Math.abs(newX - xys.x);
                const yDiff = Math.abs(newX - xys.x);
                if (xDiff > 5 || yDiff > 5) {
                    //overything over 5px change will be animated
                    gpz.moveBy(
                        newX - xys.x,
                        newY - xys.y,
                        true
                    );
                    await new Promise(r => setTimeout(r, 250));
                } else {
                    gpz.moveBy(
                        newX - xys.x,
                        newY - xys.y,
                        false
                    );
                }

                //correct way to zoom with center of graph as origin when scaled
                gpz.smoothZoomAbs(
                    xys.x + originWidth * xys.scale / 2,
                    xys.y + originHeight * xys.scale / 2,
                    targetScale,
                );
            }");
            sb.AppendLine("    </script>");
        }

        private void RenderHeadAuxiliaryScripts(StringBuilder sb)
        {
            // Force Graph
            sb.AppendLine("    <script src=\"/assets/force-graph.min.js\"></script>");

            // Search Assets
            sb.AppendLine("    <script src=\"/assets/minisearch.min.js\"></script>");
            sb.AppendLine("    <script defer src=\"/assets/search.js\"></script>");
            sb.AppendLine("    <script defer src=\"/assets/history.js\"></script>");
            sb.AppendLine("    <script defer src=\"/assets/icons.js\"></script>");
        }

        private void RenderHeadHighlightJs(StringBuilder sb)
        {
            var darkTheme = _config.Theme.Highlight.Dark;
            sb.AppendLine($"    <link id=\"highlight-theme\" rel=\"stylesheet\" href=\"/assets/highlight/{darkTheme}.min.css\">");
            sb.AppendLine("    <script src=\"/assets/highlight/highlight.min.js\"></script>");
            sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/highlightjs-line-numbers.js@2.8.0/dist/highlightjs-line-numbers.min.js\"></script>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        /* Inline code (Tailwind Typography overrides) */");
            sb.AppendLine("        /* Remove the backtick quotes that prose adds via ::before/::after */");
            sb.AppendLine("        .prose :where(code):not(:where([class~=\"not-prose\"] *))::before,");
            sb.AppendLine("        .prose :where(code):not(:where([class~=\"not-prose\"] *))::after { content: none !important; }");
            sb.AppendLine("        /* Give inline code (not inside <pre>) a visible background and padding */");
            sb.AppendLine("        .prose :not(pre) > code { background-color: rgba(148, 163, 184, 0.18); color: inherit; padding: 0.15em 0.4em; border-radius: 0.25rem; font-weight: 500; font-size: 0.875em; }");
            sb.AppendLine("        .dark .prose :not(pre) > code { background-color: rgba(148, 163, 184, 0.22); }");
            sb.AppendLine("");
            sb.AppendLine("        /* Reset Tailwind Typography's <pre> defaults inside Neko code blocks. */");
            sb.AppendLine("        /* Without this the prose <pre> keeps its own dark background, margin, */");
            sb.AppendLine("        /* padding and rounding, so the code renders as a dark inset box floating */");
            sb.AppendLine("        /* with gaps inside the lighter wrapper card. Let the wrapper own the */");
            sb.AppendLine("        /* background, spacing and corners; the inner <code> keeps its padding. */");
            sb.AppendLine("        .prose .neko-code-block > pre { margin: 0 !important; padding: 0 !important; background: transparent !important; border: 0 !important; border-radius: 0 !important; }");
            sb.AppendLine("");
            sb.AppendLine("        /* Keep unhighlighted code text legible. Tailwind Typography colours */");
            sb.AppendLine("        /* <pre>/<code> with low-specificity :where() rules at the same (0,1,0) */");
            sb.AppendLine("        /* weight as the highlight theme's .hljs, so whichever loads last wins — */");
            sb.AppendLine("        /* and the prose pre-code colour (a light grey tuned for prose's dark */");
            sb.AppendLine("        /* <pre>) can override .hljs and wash out plain tokens on the light card. */");
            sb.AppendLine("        /* Re-assert the active default theme's base text colour at higher */");
            sb.AppendLine("        /* specificity; highlighted .hljs-* spans keep their own colours. */");
            sb.AppendLine("        .prose .neko-code-block pre code.hljs { color: #24292e; }");
            sb.AppendLine("        .dark .prose .neko-code-block pre code.hljs { color: #9aa5ce; }");
            sb.AppendLine("");
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
            sb.AppendLine("        /* A disclosure chevron rotates with its OWN <details>, at every nesting");
            sb.AppendLine("           level. Scoping to the direct child summary (rather than Tailwind's");
            sb.AppendLine("           group-open, which matches any ancestor .group[open]) keeps a nested");
            sb.AppendLine("           chevron from being forced down just because its parent section is open. */");
            sb.AppendLine("        #sidebar details[open] > summary .neko-chevron { transform: rotate(90deg); }");
            sb.AppendLine("        /* Sidebar chevrons only animate on user interaction, never on initial");
            sb.AppendLine("           load while saved/active section state is being restored. */");
            sb.AppendLine("        #sidebar.neko-no-anim .neko-chevron { transition: none !important; }");
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
            sb.AppendLine("");
            sb.AppendLine("        /* Changelog sections: H1s become labelled section headers with an icon */");
            sb.AppendLine("        .neko-changelog-body > h1:first-child { margin-top: 0; }");
            sb.AppendLine("        .neko-changelog-body h1 { display: flex; align-items: center; gap: 0.625rem; font-size: 1.125rem; line-height: 1.75rem; font-weight: 600; margin: 1.75rem 0 0.25rem; padding: 0; border: 0; }");
            sb.AppendLine("        .neko-changelog-body h1 > i:first-child { display: inline-flex; align-items: center; justify-content: center; width: 2rem; height: 2rem; flex: none; border-radius: 0.625rem; font-size: 1rem; background: color-mix(in srgb, var(--neko-grad-from) 14%, transparent); color: var(--neko-grad-from); }");
            sb.AppendLine("        /* Changelog entries: hairline separator between consecutive entries */");
            sb.AppendLine("        .neko-changelog-body .neko-change + .neko-change { border-top: 1px solid rgba(148, 163, 184, 0.18); }");
            sb.AppendLine("        .neko-changelog-body .neko-change-body > :first-child { margin-top: 0; }");
            sb.AppendLine("        .neko-changelog-body .neko-change-body > :last-child { margin-bottom: 0; }");
            sb.AppendLine("");
            sb.AppendLine("        /* API endpoint reference (```endpoint), styled after docs.microsoft.com / Swagger. */");
            sb.AppendLine("        .api-endpoint { margin: 1.5rem 0; border: 1px solid rgba(148, 163, 184, 0.28); border-radius: 0.75rem; overflow: hidden; }");
            sb.AppendLine("        .dark .api-endpoint { border-color: rgba(148, 163, 184, 0.2); }");
            sb.AppendLine("        .api-endpoint-header { display: flex; align-items: center; flex-wrap: wrap; gap: 0.75rem; padding: 0.6rem 1rem; background: rgba(148, 163, 184, 0.08); border-bottom: 1px solid rgba(148, 163, 184, 0.28); }");
            sb.AppendLine("        .dark .api-endpoint-header { background: rgba(148, 163, 184, 0.06); border-bottom-color: rgba(148, 163, 184, 0.2); }");
            sb.AppendLine("        .api-method { flex: none; display: inline-flex; align-items: center; justify-content: center; min-width: 4rem; padding: 0.2rem 0.6rem; border-radius: 0.375rem; font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.03em; line-height: 1.4; }");
            sb.AppendLine("        .api-method[data-method=\"GET\"]     { background: #dbeafe; color: #1e40af; }");
            sb.AppendLine("        .api-method[data-method=\"POST\"]    { background: #dcfce7; color: #166534; }");
            sb.AppendLine("        .api-method[data-method=\"PUT\"]     { background: #fef3c7; color: #92400e; }");
            sb.AppendLine("        .api-method[data-method=\"PATCH\"]   { background: #ccfbf1; color: #115e59; }");
            sb.AppendLine("        .api-method[data-method=\"DELETE\"]  { background: #fee2e2; color: #991b1b; }");
            sb.AppendLine("        .api-method[data-method=\"HEAD\"], .api-method[data-method=\"OPTIONS\"] { background: #e5e7eb; color: #374151; }");
            sb.AppendLine("        .dark .api-method[data-method=\"GET\"]    { background: rgba(30, 58, 138, 0.4); color: #93c5fd; }");
            sb.AppendLine("        .dark .api-method[data-method=\"POST\"]   { background: rgba(20, 83, 45, 0.4); color: #86efac; }");
            sb.AppendLine("        .dark .api-method[data-method=\"PUT\"]    { background: rgba(120, 53, 15, 0.4); color: #fcd34d; }");
            sb.AppendLine("        .dark .api-method[data-method=\"PATCH\"]  { background: rgba(19, 78, 74, 0.4); color: #5eead4; }");
            sb.AppendLine("        .dark .api-method[data-method=\"DELETE\"] { background: rgba(127, 29, 29, 0.4); color: #fca5a5; }");
            sb.AppendLine("        .dark .api-method[data-method=\"HEAD\"], .dark .api-method[data-method=\"OPTIONS\"] { background: rgba(55, 65, 81, 0.6); color: #d1d5db; }");
            sb.AppendLine("        .api-path { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 0.95rem; font-weight: 600; color: #111827; word-break: break-all; }");
            sb.AppendLine("        .dark .api-path { color: #f3f4f6; }");
            sb.AppendLine("        .api-anchor { color: #9ca3af; text-decoration: none; font-size: 0.8rem; }");
            sb.AppendLine("        .api-anchor:hover { color: #4b5563; }");
            sb.AppendLine("        .dark .api-anchor:hover { color: #d1d5db; }");
            sb.AppendLine("        .api-endpoint-body { padding: 0.75rem 1rem; }");
            sb.AppendLine("        .api-summary { margin: 0; color: #374151; line-height: 1.6; }");
            sb.AppendLine("        .dark .api-summary { color: #d1d5db; }");
            sb.AppendLine("        .api-details { display: grid; grid-template-columns: max-content 1fr; column-gap: 1rem; row-gap: 0.35rem; margin: 0.75rem 0 0; font-size: 0.9rem; }");
            sb.AppendLine("        .api-details dt { font-weight: 600; color: #6b7280; }");
            sb.AppendLine("        .dark .api-details dt { color: #9ca3af; }");
            sb.AppendLine("        .api-details dd { margin: 0; color: #1f2937; }");
            sb.AppendLine("        .dark .api-details dd { color: #e5e7eb; }");
            sb.AppendLine("        /* Inline code inside an endpoint (paths excepted) gets a subtle pill. */");
            sb.AppendLine("        .api-endpoint :not(.api-path) > code, .api-endpoint code:not(.api-path) { background: rgba(148, 163, 184, 0.18); padding: 0.12em 0.4em; border-radius: 0.25rem; font-size: 0.85em; }");
            sb.AppendLine("");
            sb.AppendLine("        /* API class/type reference (Microsoft Learn-style). Opt-in via .api-* helper");
            sb.AppendLine("           classes authored as containers (:::api-definition, :::api-members, …). */");
            sb.AppendLine("        /* Auto-widen the content column on reference pages that use these helpers. */");
            sb.AppendLine("        #main-scroll:has(.api-definition, .api-members, .csharp-type) > .max-w-4xl { max-width: 72rem; }");
            sb.AppendLine("        .api-definition { margin: 0 0 1rem; padding: 0.75rem 1rem; border-left: 3px solid var(--neko-grad-from, #0443D3); background: rgba(148, 163, 184, 0.08); border-radius: 0 0.5rem 0.5rem 0; font-size: 0.9rem; line-height: 1.7; }");
            sb.AppendLine("        .dark .api-definition { background: rgba(148, 163, 184, 0.06); }");
            sb.AppendLine("        .api-definition p { margin: 0; color: #374151; }");
            sb.AppendLine("        .dark .api-definition p { color: #d1d5db; }");
            sb.AppendLine("        .api-definition strong { color: #6b7280; font-weight: 600; margin-right: 0.25rem; }");
            sb.AppendLine("        .dark .api-definition strong { color: #9ca3af; }");
            sb.AppendLine("        .api-definition code { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 0.85em; color: #111827; }");
            sb.AppendLine("        .dark .api-definition code { color: #f3f4f6; }");
            sb.AppendLine("        .api-members table { width: 100%; border-collapse: collapse; margin: 0.5rem 0 1.5rem; font-size: 0.9rem; }");
            sb.AppendLine("        .api-members thead th { text-align: left; font-weight: 600; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.03em; color: #6b7280; padding: 0.5rem 0.75rem; border-bottom: 1px solid rgba(148, 163, 184, 0.4); }");
            sb.AppendLine("        .dark .api-members thead th { color: #9ca3af; border-bottom-color: rgba(148, 163, 184, 0.25); }");
            sb.AppendLine("        .api-members tbody td { vertical-align: top; padding: 0.6rem 0.75rem; border-bottom: 1px solid rgba(148, 163, 184, 0.2); color: #374151; line-height: 1.6; }");
            sb.AppendLine("        .dark .api-members tbody td { color: #d1d5db; border-bottom-color: rgba(148, 163, 184, 0.15); }");
            sb.AppendLine("        .api-members tbody td:first-child { width: 38%; font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size: 0.85rem; }");
            sb.AppendLine("        .api-members tbody td:first-child a { color: var(--neko-grad-from, #0443D3); text-decoration: none; font-weight: 600; }");
            sb.AppendLine("        .api-members tbody td:first-child a:hover { text-decoration: underline; }");
            sb.AppendLine("        .api-members tbody tr:hover td { background: rgba(148, 163, 184, 0.06); }");
            sb.AppendLine("        .api-member { margin: 1.75rem 0; padding-top: 0.25rem; }");
            sb.AppendLine("        .api-member-params { display: grid; grid-template-columns: max-content 1fr; column-gap: 1rem; row-gap: 0.35rem; margin: 0.5rem 0 0; font-size: 0.9rem; }");
            sb.AppendLine("        .api-member-params dt { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-weight: 600; color: var(--neko-grad-from, #0443D3); }");
            sb.AppendLine("        .api-member-params dd { margin: 0; color: #374151; }");
            sb.AppendLine("        .dark .api-member-params dd { color: #d1d5db; }");
            sb.AppendLine("        .api-applies-to { margin-top: 2.5rem; padding-top: 1rem; border-top: 1px solid rgba(148, 163, 184, 0.3); font-size: 0.9rem; color: #6b7280; }");
            sb.AppendLine("        .dark .api-applies-to { color: #9ca3af; border-top-color: rgba(148, 163, 184, 0.2); }");
            sb.AppendLine("        /* Changelog version header — two states.");
            sb.AppendLine("           In flow it is a rounded, blurred, inset card. When it sticks to the");
            sb.AppendLine("           top of the scroll pane (`.is-stuck`, toggled by an observer) the card");
            sb.AppendLine("           styling drops and a full-bleed background layer spans the whole pane. */");
            sb.AppendLine("        .neko-changelog-version { position: sticky; top: -1rem; z-index: 20; display: flex; align-items: center; gap: 0.75rem; flex-wrap: wrap; padding: 0.625rem 1rem; border-radius: 1rem; border: 1px solid rgba(229, 231, 235, 0.7); background-color: rgba(255, 255, 255, 0.92); -webkit-backdrop-filter: blur(8px); backdrop-filter: blur(8px); transition: border-radius 0.15s ease, border-color 0.15s ease; }");
            sb.AppendLine("        @media (min-width: 768px) { .neko-changelog-version { top: -2rem; } }");
            sb.AppendLine("        .dark .neko-changelog-version { border-color: rgba(255, 255, 255, 0.1); background-color: rgba(17, 24, 39, 0.92); }");
            sb.AppendLine("        @supports ((-webkit-backdrop-filter: blur(1px)) or (backdrop-filter: blur(1px))) { .neko-changelog-version { background-color: rgba(255, 255, 255, 0.78); } .dark .neko-changelog-version { background-color: rgba(17, 24, 39, 0.78); } }");
            sb.AppendLine("        .neko-cl-sentinel { position: absolute; top: 0; left: 0; width: 1px; height: 1px; pointer-events: none; }");
            sb.AppendLine("        .neko-cl-bleed { display: none; }");
            sb.AppendLine("        .neko-changelog-version.is-stuck { border-radius: 0; border-color: transparent; background-color: transparent; -webkit-backdrop-filter: none; backdrop-filter: none; }");
            sb.AppendLine("        /* When stuck the background is `position: fixed` so it spans the whole");
            sb.AppendLine("           viewport width — escaping the centred, max-width-capped content row —");
            sb.AppendLine("           while the header's pill/date stay aligned with the column. Its `top`");
            sb.AppendLine("           and `height` are set inline by the observer to match the pinned header");
            sb.AppendLine("           (extended a little upward, hidden behind the opaque navbar, so the top");
            sb.AppendLine("           edge is fully covered). */");
            sb.AppendLine("        .neko-changelog-version.is-stuck .neko-cl-bleed { display: block; position: fixed; left: 0; right: 0; z-index: -10; border-bottom: 1px solid rgba(203, 213, 225, 0.8); background-color: rgba(255, 255, 255, 0.97); -webkit-backdrop-filter: blur(10px); backdrop-filter: blur(10px); }");
            sb.AppendLine("        .dark .neko-changelog-version.is-stuck .neko-cl-bleed { border-bottom-color: rgba(255, 255, 255, 0.12); background-color: rgba(17, 24, 39, 0.97); }");
            sb.AppendLine("        @supports ((-webkit-backdrop-filter: blur(1px)) or (backdrop-filter: blur(1px))) { .neko-changelog-version.is-stuck .neko-cl-bleed { background-color: rgba(255, 255, 255, 0.86); } .dark .neko-changelog-version.is-stuck .neko-cl-bleed { background-color: rgba(17, 24, 39, 0.86); } }");
            sb.AppendLine("    </style>");
        }

        // Cross-document View Transitions. Neko ships a fully static, multi-page
        // site, so every sidebar click is a real navigation that re-parses and
        // repaints the whole document — including the (identical) sidebar, which
        // visibly flashes and resets its scroll/active state before the inline
        // scripts restore it. Opting the document into the View Transitions API
        // turns each same-origin navigation into a quick content cross-fade and,
        // crucially, lets us hold the persistent chrome (header + sidebar) in
        // place across pages so it no longer flashes. This is a progressive
        // enhancement: browsers that don't support cross-document view
        // transitions simply perform a normal full navigation, exactly as before.
        private void RenderHeadViewTransitions(StringBuilder sb)
        {
            sb.AppendLine("    <style>");
            sb.AppendLine("        @view-transition { navigation: auto; }");
            sb.AppendLine("        /* Give the persistent chrome a stable name so the browser keeps it");
            sb.AppendLine("           across navigations instead of cross-fading it with the rest. */");
            sb.AppendLine("        header { view-transition-name: neko-header; }");
            sb.AppendLine("        #sidebar { view-transition-name: neko-sidebar; }");
            sb.AppendLine("        /* Hold the chrome perfectly still — no fade or morph — while the");
            sb.AppendLine("           page content swaps underneath it. */");
            sb.AppendLine("        ::view-transition-group(neko-header),");
            sb.AppendLine("        ::view-transition-group(neko-sidebar) { animation-duration: 0s; }");
            sb.AppendLine("        /* Quick, subtle cross-fade for the page content only. */");
            sb.AppendLine("        ::view-transition-old(root),");
            sb.AppendLine("        ::view-transition-new(root) { animation-duration: 0.18s; }");
            sb.AppendLine("        @media (prefers-reduced-motion: reduce) {");
            sb.AppendLine("            ::view-transition-group(*),");
            sb.AppendLine("            ::view-transition-old(*),");
            sb.AppendLine("            ::view-transition-new(*) { animation: none !important; }");
            sb.AppendLine("        }");
            sb.AppendLine("    </style>");
        }

        private void RenderHeadDarkModeInit(StringBuilder sb)
        {
            sb.AppendLine("    <script>");
            sb.AppendLine("        if (localStorage.theme === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {");
            sb.AppendLine("            document.documentElement.classList.add('dark');");
            sb.AppendLine("        } else {");
            sb.AppendLine("            document.documentElement.classList.remove('dark');");
            sb.AppendLine("        }");
            sb.AppendLine("    </script>");
        }
    }
}
