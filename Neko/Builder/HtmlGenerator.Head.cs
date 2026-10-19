using Neko.Configuration;
using System.Collections.Generic;
using System.Linq;
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

        private void RenderHeadTailwindAndTheme(StringBuilder sb, string inlineTailwindCss = null)
        {
            // Tailwind CSS — a real, cacheable stylesheet generated at build time
            // by the pure-C# TailwindGenerator (no Play CDN, no Node, no binary).
            // This avoids the CDN's flash of unstyled content on navigation (the
            // dark: utilities don't exist until the CDN script runs in the
            // browser). Each (sub-)site links its OWN tailwind.css under its route
            // prefix so multi-repo projects each get their used-class set + palette.
            // A standalone page (deck, paged document) carries its own stylesheet,
            // generated from just its markup — see HtmlGenerator.Standalone.
            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            if (inlineTailwindCss != null)
            {
                sb.AppendLine("    " + StandaloneAssets.Style(inlineTailwindCss));
            }
            else
            {
                sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/tailwind.css\">");
            }

            var (primaryTheme, accentTheme) = ThemeDefinitions.ResolvePalettes(_config);
            var themeJson = System.Text.Json.JsonSerializer.Serialize(primaryTheme);
            var accentJson = System.Text.Json.JsonSerializer.Serialize(accentTheme);
            // Expose the resolved palettes for runtime JS (dynamic theming, charts).
            sb.AppendLine($"    <script>window.nekoThemeColors = {themeJson}; window.nekoAccentColors = {accentJson};</script>");

            // Curiosity-inspired neutral palette overrides (deep navy in dark mode, soft white in light mode)
            sb.AppendLine("    <style>");
            sb.AppendLine("      :root { --neko-bg: #ffffff; --neko-bg-elevated: #f8fafc; --neko-bg-muted: #f1f5f9; --neko-border: #e5e7eb; }");
            sb.AppendLine("      html.dark { --neko-bg: #050914; --neko-bg-elevated: #0b1226; --neko-bg-muted: #111a33; --neko-border: #1f2a44; }");
            sb.AppendLine("      /* Paint the root element itself, not just <body>. Otherwise the white");
            sb.AppendLine("         default html background shows through during a cross-document View");
            sb.AppendLine("         Transition cross-fade (and on overscroll) — a white flash in dark mode. */");
            sb.AppendLine("      html { background-color: var(--neko-bg); }");
            sb.AppendLine("      html.dark body { background-color: var(--neko-bg); }");
            sb.AppendLine("      html.dark .dark\\:bg-gray-900 { background-color: var(--neko-bg) !important; }");
            sb.AppendLine("      html.dark .dark\\:bg-gray-800 { background-color: var(--neko-bg-elevated) !important; }");
            sb.AppendLine("      html.dark .dark\\:border-gray-800 { border-color: var(--neko-border) !important; }");
            sb.AppendLine("      html.dark .dark\\:border-gray-700 { border-color: var(--neko-border) !important; }");
            sb.AppendLine("      .neko-text-gradient { background-image: linear-gradient(90deg, var(--neko-grad-from, #5b94ff) 0%, var(--neko-grad-to, #a78bfa) 100%); -webkit-background-clip: text; background-clip: text; color: transparent; }");
            sb.AppendLine($"      :root {{ --neko-grad-from: {(primaryTheme.TryGetValue("400", out var pf) ? pf : "#5b94ff")}; --neko-grad-to: {(accentTheme.TryGetValue("400", out var af) ? af : "#a78bfa")}; }}");
            // The resolved primary and accent ramps as variables (--neko-primary-50 … 950, --neko-accent-50 … 950), so a
            // stylesheet can use the site's palette without script: the diagram theme (MermaidCss) is one.
            sb.AppendLine("      :root { " + string.Join(" ", primaryTheme.Select(kv => $"--neko-primary-{kv.Key}: {kv.Value};")
                .Concat(accentTheme.Select(kv => $"--neko-accent-{kv.Key}: {kv.Value};"))) + " }");
            if (_isBlogMode)
            {
                // Blog-mode palette as CSS variables. Light always comes from
                // theme.base; the dark override is only emitted when the site opts
                // into dark mode via theme.dark (otherwise blog mode stays light).
                sb.AppendLine($"      :root {{ --blog-bg: {BlogBaseBg()}; --blog-ink: {BlogBaseColor()}; }}");
                if (_blogDarkEnabled)
                {
                    sb.AppendLine($"      html.dark {{ --blog-bg: {BlogDarkBg()}; --blog-ink: {BlogDarkColor()}; }}");
                }
            }
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
            // Base font. Neko itself does not pin a typeface — it ships no default
            // web font and emits no `font-family` rule unless the site opts in. With
            // nothing configured the browser/system sans-serif stack from the CSS
            // preflight applies. A content repo sets the font via `theme.font`
            // (family + optional stylesheet URL) to match its brand — e.g. the blog's
            // Plus Jakarta Sans. Any per-element overrides (such as keeping the blog
            // header chrome on Inter) live in that repo's own stylesheet too, not here.
            var font = _config.Theme?.Font;
            var customFont = font != null && !string.IsNullOrEmpty(font.Family);
            if (customFont)
            {
                if (!string.IsNullOrEmpty(font.Url))
                {
                    sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{EscapeHtmlAttr(font.Url)}\">");
                }
                var family = font.Family.Trim();
                // Quote the family name unless the author already quoted it or passed
                // a full font stack (containing a comma).
                var cssFamily = (family.Contains(",") || family.StartsWith("'") || family.StartsWith("\""))
                    ? family
                    : $"'{family}'";
                sb.AppendLine($"    <style>:root {{ font-family: {cssFamily}, sans-serif; }}</style>");
            }
            else if (_isBlogMode)
            {
                // Inter Font (default) — the blog's marketing chrome is built on it. Docs mode pins
                // nothing: the typeface is the content repo's choice (see ThemeFontTests).
                sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://rsms.me/inter/inter.css\">");
                sb.AppendLine("    <style>:root { font-family: 'Inter', sans-serif; } @supports (font-variation-settings: normal) { :root { font-family: 'Inter var', sans-serif; } }</style>");
            }

            // In blog mode the marketing header chrome (nav links + CTA pills) uses
            // Inter, exactly like curiosity.ai — even when `theme.font` sets a brand
            // font for the content and footer. We pin the header to 'Inter'/'Inter var'
            // (below) so a custom body font doesn't change the nav, and make sure those
            // families are actually defined.
            //
            // Only pull Inter from the rsms.me CDN when the site does NOT supply its own
            // font stylesheet via `theme.font.url`. A site that self-hosts fonts is
            // expected to define 'Inter'/'Inter var' itself (curiosity.ai's blog maps
            // them to Inter *Display* Medium for pixel-exact chrome) — loading the CDN
            // copy afterwards would override that mapping with the regular Inter cut and
            // add an external dependency, so we skip it.
            if (_isBlogMode && customFont && string.IsNullOrEmpty(font!.Url))
            {
                sb.AppendLine("    <link rel=\"stylesheet\" href=\"https://rsms.me/inter/inter.css\">");
            }
            // Pin the header chrome to Inter ONLY when Neko is the one providing Inter
            // (the rsms.me CDN copy, loaded above or as the default). A site that
            // self-hosts its fonts via `theme.font.url` defines the header font in its
            // own stylesheet — curiosity.ai's blog maps it to Inter *Display* Medium for
            // pixel-exact chrome (`header { font-family: 'InterDisplay', 'Inter' }`).
            // Emitting our 'Inter var' override here would win by source order yet
            // resolve to nothing (the self-hosted CSS never defines 'Inter var'),
            // dropping the header to the system sans-serif. So when the site brings its
            // own font stylesheet, defer to it.
            if (_isBlogMode && string.IsNullOrEmpty(font?.Url))
            {
                sb.AppendLine("    <style>header { font-family: 'Inter', sans-serif; } @supports (font-variation-settings: normal) { header { font-family: 'Inter var', sans-serif; } }</style>");
            }

            // The curiosity.ai marketing look renders the whole page with
            // smoothed (thinned) text — `-webkit-font-smoothing: antialiased` /
            // `-moz-osx-font-smoothing: grayscale`. Without it the same Inter/Inter
            // Display at weight 500 paints noticeably heavier on macOS/Retina, so the
            // nav links and CTA pills read bolder than the live site. Blog mode pins
            // it so the chrome (and body/footer) match curiosity.ai pixel-for-pixel.
            // Docs mode keeps the browser default.
            if (_isBlogMode)
            {
                sb.AppendLine("    <style>html { -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale; }</style>");
            }

            // Flaticon UIcons (self-hosted v4 — see Neko.Tools.UIcons). Root-relative
            // like the other stylesheets below, but scoped to this (sub-)site's own
            // route prefix — otherwise a sub-project (multi-repo `neko watch`, or any
            // deployment that nests projects under one domain) requests the *root*
            // project's copy of these assets instead of its own, 404ing (and leaving
            // icons unrendered) whenever there's no root project sharing that path.
            var iconsPrefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            // Every navigation is a full page load, and the icon fonts are only
            // requested once layout first meets a glyph, so without this the page
            // paints before them: icons pop in a frame later and every row that
            // holds one grows, which moves the sidebar (and its scroll position).
            // Preloading starts the fetch with the HTML, so a cached font is ready
            // for the first layout. `crossorigin` because @font-face fetches are
            // CORS requests; without it the preload is not reused. Only the
            // regular set, which the sidebar and header use on every page: a page
            // with no brand icon would log an unused-preload warning for brands.
            sb.AppendLine($"    <link rel=\"preload\" href=\"{iconsPrefix}/assets/uicons-regular-rounded.woff2\" as=\"font\" type=\"font/woff2\" crossorigin>");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{iconsPrefix}/assets/uicons-regular-rounded.css\">");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{iconsPrefix}/assets/uicons-brands.css\">");

            // Emoji CSS
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{iconsPrefix}/assets/emoji.css\">");

        }

        /// <summary>
        /// KaTeX is embedded in Neko (<c>Resources/standalone/</c>) and never fetched from a CDN. A standalone page
        /// (deck, document) inlines it, with the fonts as data URIs. A site page loads the <c>assets/</c> copy on
        /// demand, only when the page has math, so a page without any no longer fetches it. Either way
        /// <c>nekoRenderMath(root)</c> typesets the math under <c>root</c>.
        /// </summary>
        private void RenderHeadKatex(StringBuilder sb, bool inline = false)
        {
            if (inline)
            {
                // Inline scripts can't be deferred, so render once the DOM is parsed.
                sb.AppendLine("    " + StandaloneAssets.Style(StandaloneAssets.InlinedCss("standalone/katex.min.css")));
                sb.AppendLine("    " + StandaloneAssets.Script(StandaloneAssets.Text("standalone/katex.min.js")));
                sb.AppendLine("    " + StandaloneAssets.Script(StandaloneAssets.Text("standalone/auto-render.min.js")));
                sb.AppendLine("    <script>window.nekoRenderMath = function (root) { renderMathInElement(root || document.body); }; document.addEventListener(\"DOMContentLoaded\", function () { nekoRenderMath(document.body); });</script>");
                return;
            }

            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            sb.AppendLine("    <script>");
            sb.AppendLine(KatexScript.Replace("__ASSETS__", System.Text.Json.JsonSerializer.Serialize($"{prefix}/assets/")));
            sb.AppendLine("    </script>");
        }

        private const string KatexScript = """
        (function () {
            const assets = __ASSETS__;
            let loading = null;

            // Math is written as a .math element, or as TeX in the text ($$ … $$, \( … \), \[ … \], \begin{…}),
            // which is what auto-render looks for. Scripts and styles are not text to typeset.
            const tex = /\$\$|\\\(|\\\[|\\begin\{/;
            function hasMath(root) {
                if (root.querySelector && root.querySelector('.math')) return true;
                const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
                    acceptNode: n => /^(SCRIPT|STYLE|TEXTAREA)$/.test(n.parentNode.nodeName) ? NodeFilter.FILTER_REJECT : NodeFilter.FILTER_ACCEPT
                });
                for (let n = walker.nextNode(); n; n = walker.nextNode()) if (tex.test(n.nodeValue)) return true;
                return false;
            }

            function script(src) {
                return new Promise(function (resolve, reject) {
                    const el = document.createElement('script');
                    el.src = src;
                    el.onload = resolve;
                    el.onerror = function () { el.remove(); reject(new Error('Could not load ' + src)); };
                    document.head.appendChild(el);
                });
            }

            function load() {
                if (window.renderMathInElement) return Promise.resolve();
                if (!loading) {
                    const css = document.createElement('link');
                    css.rel = 'stylesheet';
                    css.href = assets + 'katex.min.css';
                    document.head.appendChild(css);
                    // auto-render needs katex, so they load in that order.
                    loading = script(assets + 'katex.min.js').then(() => script(assets + 'auto-render.min.js')).catch(function (e) { loading = null; throw e; });
                }
                return loading;
            }

            window.nekoRenderMath = async function (root) {
                root = root || document.body;
                if (!hasMath(root)) return;
                try {
                    await load();
                } catch (e) {
                    console.error('Math rendering failed:', e);
                    return;
                }
                renderMathInElement(root);
            };

            document.addEventListener('DOMContentLoaded', function () { nekoRenderMath(document.body); });
        })();
""";

        /// <summary>
        /// Diagrams are drawn by Pele (https://pele.run), which turns Mermaid syntax into SVG. It is embedded in
        /// Neko (<c>Resources/pele.min.js</c>) and never fetched from a CDN: a standalone page (deck, document)
        /// inlines it, and a site page loads the <c>assets/pele.min.js</c> copy on demand, only when the page
        /// has a diagram. The SVG takes its colours from CSS variables (<see cref="MermaidCss"/>), so the one
        /// drawing follows the page's light and dark mode with no second render.
        /// </summary>
        private void RenderHeadMermaid(StringBuilder sb, bool inline = false)
        {
            string peleUrl = string.Empty;
            if (inline)
            {
                sb.AppendLine("    " + StandaloneAssets.Script(StandaloneAssets.Text("pele.min.js")));
            }
            else
            {
                var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
                peleUrl = $"{prefix}/assets/pele.min.js";
            }

            sb.AppendLine("    " + StandaloneAssets.Style(MermaidCss, "neko-mermaid-theme"));
            sb.AppendLine("    <script>");
            sb.AppendLine(MermaidScript.Replace("__PELE_URL__", System.Text.Json.JsonSerializer.Serialize(peleUrl)));
            sb.AppendLine("    </script>");
        }

        /// <summary>
        /// Pele colours everything from <c>--pele-*</c> variables; this maps them onto the site's own theme
        /// variables, the way Tesserae's <c>tss.mermaid.css</c> maps them onto its tokens. The primary and accent
        /// ramps (<c>--neko-primary-N</c>, <c>--neko-accent-N</c>) are the site's resolved <c>theme</c> colours,
        /// and the surfaces are the page's (<c>--neko-bg-elevated</c> and friends, which switch with
        /// <c>html.dark</c>). Dark mode is a second set of the same variables, not a second drawing, so a diagram
        /// follows a switch at once. The fallbacks are for a page that does not define the Neko variables.
        /// </summary>
        private const string MermaidCss = """
        .mermaid {
            --pele-bg: var(--neko-bg-elevated, #f9fafb);
            --pele-fg: #1e293b;
            --pele-muted: #64748b;
            --pele-line: var(--neko-primary-500, #3b82f6);
            --pele-surface: var(--neko-primary-100, #dce8ff);
            --pele-surface-alt: var(--neko-primary-50, #eef4ff);
            --pele-border: var(--neko-primary-500, #3b82f6);
            --pele-accent: var(--neko-accent-500, #8b5cf6);
            --pele-critical: #dc2626;
            --pele-error: #dc2626;
            --pele-radius: 6px;
            --pele-series-1: var(--neko-primary-500, #3b82f6);
            --pele-series-2: var(--neko-accent-500, #8b5cf6);
            --pele-series-3: #059669;
            --pele-series-4: #d97706;
            --pele-series-5: #dc2626;
            --pele-series-6: #0891b2;
            --pele-series-7: #db2777;
            --pele-series-8: #65a30d;
            background-color: var(--pele-bg);
        }
        .dark .mermaid {
            --pele-bg: var(--neko-bg-elevated, #1f2937);
            --pele-fg: #cdd7ee;
            --pele-muted: #94a3b8;
            --pele-line: var(--neko-primary-300, #8bb6ff);
            --pele-surface: var(--neko-primary-800, #1e3a8a);
            --pele-surface-alt: var(--neko-bg-muted, #111a33);
            --pele-border: var(--neko-primary-400, #5b94ff);
            --pele-accent: var(--neko-accent-400, #a78bfa);
            --pele-critical: #f87171;
            --pele-error: #f87171;
            --pele-series-1: var(--neko-primary-400, #5b94ff);
            --pele-series-2: var(--neko-accent-400, #a78bfa);
            --pele-series-3: #34d399;
            --pele-series-4: #fbbf24;
            --pele-series-5: #f87171;
            --pele-series-6: #22d3ee;
            --pele-series-7: #f472b6;
            --pele-series-8: #a3e635;
        }
""";

        private const string MermaidScript = """
        // On a standalone page Pele is already inlined above; on a site page it is fetched the
        // first time a page has a diagram (and not at all on a page without one).
        (function () {
            const peleUrl = __PELE_URL__;
            let loading = null;
            window.nekoLoadPele = function () {
                if (window.pele) return Promise.resolve();
                if (!loading) {
                    loading = new Promise(function (resolve, reject) {
                        const script = document.createElement('script');
                        script.src = peleUrl;
                        script.onload = resolve;
                        script.onerror = function () { loading = null; script.remove(); reject(new Error('Could not load ' + peleUrl)); };
                        document.head.appendChild(script);
                    });
                }
                return loading;
            };
        })();

        async function renderMermaid() {
            const elements = Array.from(document.querySelectorAll('.mermaid')).filter(el => !el.getAttribute('data-processed'));
            if (!elements.length) return;

            // Claimed before the library is awaited, so a second call (a theme switch, a page
            // swap) does not draw the same diagram twice.
            elements.forEach(el => el.setAttribute('data-processed', 'true'));

            try {
                await window.nekoLoadPele();
            } catch (e) {
                console.error('Mermaid rendering failed:', e);
                elements.forEach(el => { el.innerHTML = '<div class="text-red-500">Error rendering diagram</div>'; });
                return;
            }

            for (const el of elements) {
                const source = el.textContent;
                el.textContent = '';
                // mount() sizes the diagram to the box (and redraws when it resizes), shows a
                // syntax error in the box rather than throwing, and adds zoom and pan controls
                // when the diagram had to shrink to fit.
                pele.mount(el, source, { onError: e => console.error('Mermaid rendering failed:', e) });
            }
        }

        document.addEventListener('DOMContentLoaded', renderMermaid);
""";

        private void RenderHeadAuxiliaryScripts(StringBuilder sb)
        {
            // Root-relative but scoped to this (sub-)site's own route prefix — see
            // the note on the UIcons links above for why the prefix matters here.
            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');

            // Force Graph
            sb.AppendLine($"    <script src=\"{prefix}/assets/force-graph.min.js\"></script>");

            // Search Assets
            sb.AppendLine($"    <script src=\"{prefix}/assets/minisearch.min.js\"></script>");
            sb.AppendLine($"    <script defer src=\"{prefix}/assets/search.js\"></script>");
            sb.AppendLine($"    <script defer src=\"{prefix}/assets/history.js\"></script>");
            sb.AppendLine($"    <script defer src=\"{prefix}/assets/icons.js\"></script>");
        }

        /// <param name="inline">
        /// Inline the theme and, when <paramref name="includeScripts"/> is set, the
        /// highlighter itself — for a standalone page (deck, paged document).
        /// </param>
        private void RenderHeadHighlightJs(StringBuilder sb, bool inline = false, bool includeScripts = true)
        {
            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
            var darkTheme = _config.Theme.Highlight.Dark;
            if (inline)
            {
                var themePath = $"highlight/{darkTheme}.min.css";
                if (StandaloneAssets.Exists(themePath))
                {
                    sb.AppendLine("    " + StandaloneAssets.Style(StandaloneAssets.Text(themePath), "highlight-theme"));
                }
                if (includeScripts)
                {
                    sb.AppendLine("    " + StandaloneAssets.Script(StandaloneAssets.Text("highlight/highlight.min.js")));
                    sb.AppendLine("    " + StandaloneAssets.Script(StandaloneAssets.Text("standalone/highlightjs-line-numbers.min.js")));
                }
            }
            else
            {
                sb.AppendLine($"    <link id=\"highlight-theme\" rel=\"stylesheet\" href=\"{prefix}/assets/highlight/{darkTheme}.min.css\">");
                sb.AppendLine($"    <script src=\"{prefix}/assets/highlight/highlight.min.js\"></script>");
                sb.AppendLine($"    <script src=\"{prefix}/assets/highlightjs-line-numbers.min.js\"></script>");
            }
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
            sb.AppendLine("        /* Keep code text legible. Tailwind Typography colours <pre>/<code> */");
            sb.AppendLine("        /* with low-specificity :where() rules that can win over the highlight */");
            sb.AppendLine("        /* theme's .hljs and leave plain tokens in a light grey on the light card; */");
            sb.AppendLine("        /* blocks highlight.js doesn't recognise (unknown language, or a Tesserae */");
            sb.AppendLine("        /* block whose compile fell back to raw source) get no .hljs class at all */");
            sb.AppendLine("        /* and render entirely in that grey. Re-assert the base text colour per */");
            sb.AppendLine("        /* mode at higher specificity; highlighted .hljs-* spans keep their colours. */");
            sb.AppendLine("        .prose .neko-code-block pre code { color: #24292e; }");
            sb.AppendLine("        .dark .prose .neko-code-block pre code { color: #9aa5ce; }");
            sb.AppendLine("        /* Blocks highlight.js doesn't recognise never get .hljs (which sets */");
            sb.AppendLine("        /* display:block), so the <code>'s p-4 padding renders as inline padding */");
            sb.AppendLine("        /* — first line indented, the rest flush. Force block so padding is even. */");
            sb.AppendLine("        .prose .neko-code-block pre code { display: block; }");
            sb.AppendLine("");
            sb.AppendLine("        /* Alerts / notes: the content has to stay inside the box. The body */");
            sb.AppendLine("        /* column carries min-w-0 + break-words, so prose and inline code wrap */");
            sb.AppendLine("        /* instead of widening the note. What cannot wrap scrolls: a code */");
            sb.AppendLine("        /* block already scrolls (its <code> carries overflow-x-auto), and a */");
            sb.AppendLine("        /* table becomes a scroll container of its own — display:block, since */");
            sb.AppendLine("        /* overflow does not apply to a table box. */");
            sb.AppendLine("        .neko-alert-body table:not(.hljs-ln) { display: block; overflow-x: auto; max-width: 100%; }");
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
            if (_isBlogMode)
            {
                // Blog mode: do NOT reserve a stable scrollbar gutter. The marketing
                // footer is full-bleed, and a reserved gutter would leave a sliver of
                // page background down the footer's right edge. With `auto` the
                // scrollbar sits at the pane edge (as on curiosity.ai) instead.
                sb.AppendLine("        #main-scroll { scrollbar-gutter: auto; }");
            }
            else
            {
                // Always reserve the scrollbar gutter so content doesn't reflow
                // horizontally when the vertical scrollbar appears (e.g. once async
                // content — password-decrypted bodies, mermaid, images — grows the page).
                sb.AppendLine("        #main-scroll { scrollbar-gutter: stable; }");
            }
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
            sb.AppendLine("        /* Generated API reference pages (neko sync-api-docs, api:type / api:members): the");
            sb.AppendLine("           Microsoft Learn layout. Every page carries an empty .api-ref-page marker. */");
            sb.AppendLine("        .api-ref-def { margin: 0 0 1rem; font-size: 0.875rem; line-height: 1.75; color: #111827; }");
            sb.AppendLine("        .dark .api-ref-def { color: #e5e7eb; }");
            sb.AppendLine("        .api-ref-def p, .api-ref-attrs p { margin: 0; }");
            sb.AppendLine("        .api-ref-def span, .api-ref-attrs span:first-child { color: #6b7280; margin-right: 0.4rem; }");
            sb.AppendLine("        .dark .api-ref-def span, .dark .api-ref-attrs span:first-child { color: #9ca3af; }");
            sb.AppendLine("        .api-ref-attrs { display: grid; gap: 0.75rem; margin: 0 0 1.5rem; }");
            sb.AppendLine("        .api-ref-arrow { color: #9ca3af; margin: 0 0.25rem; }");
            sb.AppendLine("        .api-ref-def a, .api-ref-attrs a, .api-ref-type a, .api-ref-table a, .api-ref-applies a { color: var(--neko-grad-from, #0443D3); text-decoration: none; }");
            sb.AppendLine("        .api-ref-def a:hover, .api-ref-attrs a:hover, .api-ref-type a:hover, .api-ref-table a:hover, .api-ref-applies a:hover { text-decoration: underline; }");
            sb.AppendLine("        .api-ref-member { padding-left: 1.25rem; }");
            sb.AppendLine("        .prose .api-ref-info { font-size: 1.25rem; font-weight: 600; line-height: 1.3; margin: 1.75rem 0 0.75rem; color: #111827; }");
            sb.AppendLine("        .dark .prose .api-ref-info { color: #f3f4f6; }");
            sb.AppendLine("        .prose .api-ref-param { margin: 0.9rem 0 0.25rem; }");
            sb.AppendLine("        .prose .api-ref-param + p { margin-top: 0; }");
            sb.AppendLine("        .prose code.api-ref-name { font-weight: 600; font-size: 0.875rem; padding: 0.15rem 0.4rem; margin-right: 0.5rem; }");
            sb.AppendLine("        .api-ref-table, .api-ref-applies { margin: 0.5rem 0 1.5rem; overflow-x: auto; }");
            sb.AppendLine("        .api-ref-table table, .api-ref-applies table { width: 100%; border-collapse: collapse; font-size: 0.875rem; border: 1px solid rgba(148, 163, 184, 0.35); }");
            sb.AppendLine("        .api-ref-table :is(th, td), .api-ref-applies :is(th, td) { text-align: left; vertical-align: top; padding: 0.5rem 0.75rem; line-height: 1.55; border-top: 1px solid rgba(148, 163, 184, 0.35); color: #374151; }");
            sb.AppendLine("        .dark .api-ref-table :is(th, td), .dark .api-ref-applies :is(th, td) { color: #d1d5db; border-color: rgba(148, 163, 184, 0.2); }");
            sb.AppendLine("        .api-ref-table th, .api-ref-applies th { font-weight: 600; }");
            sb.AppendLine("        .api-ref-table td:first-child { width: 32%; padding-right: 1rem; overflow-wrap: anywhere; }");
            sb.AppendLine("        .api-ref-applies :is(th, td) { border: 1px solid rgba(148, 163, 184, 0.35); }");
            sb.AppendLine("        .api-ref-applies th { background: rgba(148, 163, 184, 0.12); }");
            sb.AppendLine("        .api-ref-table code, .api-ref-applies code { font-size: 0.85em; background: rgba(148, 163, 184, 0.18); padding: 0 0.3em; border-radius: 0.2rem; }");
            sb.AppendLine("        /* The C# title bar on a reference page's code blocks: no decorative dot. */");
            sb.AppendLine("        #main-scroll:has(.api-ref-page) .neko-code-block .w-2.h-2.rounded-full { display: none; }");
            sb.AppendLine("        #main-scroll:has(.api-ref-page) .neko-code-block { margin: 0.75rem 0 1rem; }");
            sb.AppendLine("        #main-scroll:has(.api-ref-page) .neko-code-block > div:first-child { padding: 0.3rem 0.5rem 0.3rem 0.75rem; }");
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

        // View Transitions. Navigation is a normal full page load — there is no
        // cross-document navigation cross-fade (it animated the whole page and was
        // prone to flicker on the header/sidebar and a white flash in dark mode).
        // The View Transition API is instead used only client-side, in password.js,
        // to mask the password-decryption reveal on protected pages: the decrypted
        // content fades in via document.startViewTransition() instead of popping in.
        // This rule only disables that transition's default animation under
        // reduced-motion; on browsers without the API the content just appears.
        private void RenderHeadViewTransitions(StringBuilder sb)
        {
            sb.AppendLine("    <style>");
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
            if (_isBlogMode && !_blogDarkEnabled)
            {
                // Light-only blog (no theme.dark configured): lock light, since
                // there is no toggle and the marketing palette is light.
                sb.AppendLine("        try { localStorage.theme = 'light'; } catch (e) {}");
                sb.AppendLine("        document.documentElement.classList.remove('dark');");
            }
            else
            {
                sb.AppendLine("        if (localStorage.theme === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {");
                sb.AppendLine("            document.documentElement.classList.add('dark');");
                sb.AppendLine("        } else {");
                sb.AppendLine("            document.documentElement.classList.remove('dark');");
                sb.AppendLine("        }");
            }
            sb.AppendLine("    </script>");
        }
    }
}
