using Neko.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        /// <summary>
        /// Renders a presentation page: a standalone, full-viewport slide deck with its
        /// own chrome (progress rail, slide gauge, prev/next bar, back button) and none
        /// of the documentation shell — no navbar, sidebar, table of contents or footer.
        /// Password protection works exactly as on an ordinary page: the slides are
        /// encrypted into <c>#content-container</c> and password.js unlocks them.
        /// </summary>
        public string GeneratePresentation(ParsedDocument document)
        {
            var options = document.Presentation ?? new PresentationOptions();
            var slides = document.Slides ?? new List<PresentationSlide>();

            var deckTitle = !string.IsNullOrEmpty(document.FrontMatter.Title)
                ? document.FrontMatter.Title
                : document.FrontMatter.Label;
            if (string.IsNullOrEmpty(deckTitle)) deckTitle = _config.Branding.Title;

            var description = !string.IsNullOrEmpty(document.FrontMatter.Description)
                ? document.FrontMatter.Description
                : _config.Meta.Description;

            var effectivePassword = ResolveEffectivePassword(document);
            var isProtected = !string.IsNullOrEmpty(effectivePassword);

            // A protected deck ships nothing page-specific in the static HTML — the
            // slides are the content, and the title would give them away.
            var headTitle = isProtected ? _config.Branding.Title : deckTitle;
            var headDescription = isProtected ? _config.Meta.Description : description;

            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang=\"en\" class=\"neko-deck-html\" data-deck-theme=\"{EscapeHtmlAttr(options.Theme)}\">");
            GeneratePresentationHead(sb, headTitle, headDescription, options, prefix);

            var brandAttr = options.HasBrand ? " data-deck-brand=\"true\"" : string.Empty;
            sb.AppendLine($"<body class=\"neko-deck-body\" data-deck-accent=\"{EscapeHtmlAttr(options.Accent)}\"{brandAttr}>");

            if (options.Grid)
            {
                sb.AppendLine("<div class=\"deck-grid\" aria-hidden=\"true\"></div>");
            }
            if (options.Progress)
            {
                sb.AppendLine("<div class=\"deck-track\" id=\"deck-track\"></div>");
            }
            if (options.Gauge)
            {
                sb.AppendLine("<nav class=\"deck-gauge\" id=\"deck-gauge\" aria-hidden=\"true\"></nav>");
            }

            RenderDeckBackButton(sb, options, prefix);
            RenderDeckBrand(sb, options);

            var body = new StringBuilder();
            RenderSlides(body, slides, options);
            RenderDeckBar(body, options, deckTitle, description);
            // Re-run the deck runtime over freshly injected slides. On a protected page
            // password.js re-creates the <script> tags it injects, so this fires once the
            // deck is decrypted; on a public page presentation.js has already wired
            // itself up and the call is a cheap no-op re-init.
            body.AppendLine("<script>(function r(n){ if (window.nekoDeckInit) { window.nekoDeckInit(); } else if (n > 0) { setTimeout(function(){ r(n - 1); }, 50); } })(60);</script>");

            if (isProtected)
            {
                sb.AppendLine("<div class=\"deck-locked\">");
                RenderProtectedColumn(sb, body.ToString(), effectivePassword);
                sb.AppendLine("</div>");
            }
            else
            {
                sb.Append(body);
            }

            sb.AppendLine($"<script src=\"{prefix}/assets/presentation.js\"></script>");

            if (_isWatchMode)
            {
                RenderLiveReloadScript(sb);
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private void GeneratePresentationHead(StringBuilder sb, string title, string description, PresentationOptions options, string prefix)
        {
            sb.AppendLine("<head>");
            RenderHeadMeta(sb, title, description);
            RenderHeadTailwindAndTheme(sb);
            RenderHeadNekoConfig(sb);

            // The theme's typefaces. midnight and daylight use a trio — a geometric
            // display face, a reading serif and a mono for labels, pulled from Google
            // Fonts; curiosity uses the Curiosity brand's two (Schibsted Grotesk and
            // Geist Mono), which Neko ships (assets/deckfonts/), so the deck needs no
            // font host and the PowerPoint export can embed the same files. `fonts:
            // google` or `fonts: none` in the deck options overrides the source.
            var fontSource = options.FontSource;
            if (fontSource == "bundled" && options.ThemeHasBundledFonts)
            {
                sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/deckfonts/deck-fonts.css\">");
            }
            else if (fontSource != "none")
            {
                sb.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
                sb.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
                sb.AppendLine($"    <link href=\"https://fonts.googleapis.com/css2?{options.ThemeFontsQuery}&display=swap\" rel=\"stylesheet\">");
            }

            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/uicons-regular-rounded.css\">");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/uicons-brands.css\">");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/emoji.css\">");

            RenderHeadKatex(sb);
            RenderHeadMermaid(sb);
            RenderHeadHighlightJs(sb);

            // Loaded last so the deck palette wins over the documentation chrome's
            // background rules emitted above.
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/presentation.css\">");
            // A theme with a look of its own layers its stylesheet on top.
            if (!string.IsNullOrEmpty(options.ThemeStylesheet))
            {
                sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/{options.ThemeStylesheet}\">");
            }

            if (!string.IsNullOrEmpty(_headIncludes))
            {
                sb.AppendLine(_headIncludes);
            }

            sb.AppendLine("</head>");
        }

        private void RenderSlides(StringBuilder sb, List<PresentationSlide> slides, PresentationOptions options)
        {
            sb.AppendLine("<main class=\"deck\" id=\"deck\">");

            for (int i = 0; i < slides.Count; i++)
            {
                var slide = slides[i];
                var eyebrow = slide.Get("eyebrow", options.Eyebrow);
                var accent = slide.Get("accent", options.Accent);
                var layout = slide.Get("layout") ?? InferLayout(slide);
                var extraClass = slide.Get("class");
                var id = slide.Get("id") ?? $"slide-{i + 1}";

                // `ground` picks the slide's surface (paper, stone, ink, deep — what a
                // theme makes of each is its own business) and `art` puts generated
                // art on it (see DeckArt), placed by the slide's layout.
                var ground = slide.Get("ground");
                var art = slide.Get("art");

                var classes = "deck-slide" + (string.IsNullOrEmpty(extraClass) ? "" : " " + extraClass);
                sb.AppendLine(
                    $"<section class=\"{EscapeHtmlAttr(classes)}\" id=\"{EscapeHtmlAttr(id)}\" " +
                    $"data-accent=\"{EscapeHtmlAttr(accent)}\" data-layout=\"{EscapeHtmlAttr(layout)}\" " +
                    (string.IsNullOrEmpty(ground) ? string.Empty : $"data-ground=\"{EscapeHtmlAttr(ground)}\" ") +
                    (string.IsNullOrEmpty(art) ? string.Empty : $"data-art=\"{EscapeHtmlAttr(art)}\" ") +
                    $"aria-label=\"Slide {i + 1} of {slides.Count}\">");

                if (!string.IsNullOrEmpty(art))
                {
                    var svg = RenderSlideArt(slide, art, i);
                    if (svg != null) sb.AppendLine($"  <div class=\"deck-art\" data-art=\"{EscapeHtmlAttr(art)}\" aria-hidden=\"true\">{svg}</div>");
                }

                if (!string.IsNullOrEmpty(eyebrow))
                {
                    sb.AppendLine($"  <p class=\"deck-eyebrow\"><span aria-hidden=\"true\"></span>{EscapeHtmlText(eyebrow)}</p>");
                }

                sb.AppendLine(slide.Html ?? string.Empty);

                // The slide's own foot: a mark and the page number. Themes that draw
                // slide furniture show it (curiosity); the others leave it hidden and
                // keep the counter in the control bar.
                sb.AppendLine($"  <div class=\"deck-slide-foot\" aria-hidden=\"true\"><span class=\"deck-slide-mark\"></span><span class=\"deck-slide-num\">{(i + 1).ToString("00")}</span></div>");
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</main>");
        }

        // The art a slide asks for with `art="field"`: the kind, and for a glyph
        // which one (`glyph="graph"` or 16 cells). The art is sized for the band a
        // layout gives it: a full-width strip on a cover, a tall panel elsewhere.
        private static string RenderSlideArt(PresentationSlide slide, string art, int index)
        {
            var layout = slide.Get("layout") ?? string.Empty;
            var wide = layout == "cover" || layout == "title";
            int.TryParse(slide.Get("seed"), out var seed);
            return DeckArt.Render(art, wide ? 1920 : 960, wide ? 360 : 1080, seed == 0 ? index + 1 : seed, slide.Get("glyph"),
                wide ? 0.5 : 0.62, wide ? 0.5 : 0.45);
        }

        // A slide that opens on an <h1> is the deck's title (or a section divider) and
        // gets the roomier, centred treatment without the author having to say so.
        private static string InferLayout(PresentationSlide slide)
        {
            var html = slide.Html ?? string.Empty;
            return html.Contains("<h1", System.StringComparison.OrdinalIgnoreCase) ? "title" : "default";
        }

        private void RenderDeckBar(StringBuilder sb, PresentationOptions options, string deckTitle, string description)
        {
            sb.AppendLine("<div class=\"deck-bar\">");
            sb.AppendLine("  <button id=\"deck-prev\" type=\"button\" aria-label=\"Previous slide\"><i class=\"fi fi-rr-angle-small-left\" aria-hidden=\"true\"></i> prev</button>");
            sb.AppendLine("  <button id=\"deck-next\" type=\"button\" aria-label=\"Next slide\">next <i class=\"fi fi-rr-angle-small-right\" aria-hidden=\"true\"></i></button>");
            if (options.Download)
            {
                // Exports the deck to PowerPoint in the browser: presentation.js
                // lazy-loads the vendored PptxGenJS bundle and presentation-pptx.js
                // on click. The title rides on the button — inside the encrypted
                // payload on a protected deck — to name the file and fill its
                // document properties.
                var fileName = PptxFileName(deckTitle);
                sb.AppendLine(
                    $"  <button id=\"deck-download\" type=\"button\" aria-label=\"Download as PowerPoint\" title=\"Download as PowerPoint (.pptx)\" " +
                    $"data-deck-title=\"{EscapeHtmlAttr(deckTitle)}\" data-deck-description=\"{EscapeHtmlAttr(description ?? string.Empty)}\" " +
                    $"data-deck-file=\"{EscapeHtmlAttr(fileName)}\">" +
                    "<i class=\"fi fi-rr-download\" aria-hidden=\"true\"></i> <span class=\"deck-download-label\">pptx</span></button>");
            }
            if (options.Counter)
            {
                sb.AppendLine("  <span class=\"deck-count\" id=\"deck-count\"></span>");
            }
            sb.AppendLine("</div>");
        }

        // The download is named after the deck title, slugged to something every
        // file system accepts (`The similarity engine` → `the-similarity-engine.pptx`).
        private static string PptxFileName(string deckTitle)
        {
            var slug = new StringBuilder();
            foreach (var ch in (deckTitle ?? string.Empty).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch)) slug.Append(ch);
                else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
            }
            var result = slug.ToString().Trim('-');
            return (string.IsNullOrEmpty(result) ? "presentation" : result) + ".pptx";
        }

        // The deck fills the window, so the only way back into the documentation is the
        // control the deck itself draws. `back:` pins an explicit target; otherwise
        // presentation.js prefers the same-origin referrer and falls back to this href
        // (the site root). Hidden when the deck is embedded in a `[!deck]` card, where
        // the surrounding page already provides the way out.
        private void RenderDeckBackButton(StringBuilder sb, PresentationOptions options, string prefix)
        {
            var href = !string.IsNullOrEmpty(options.Back) ? options.Back : (string.IsNullOrEmpty(prefix) ? "/" : prefix + "/");
            var text = string.IsNullOrEmpty(options.BackText) ? "Back" : options.BackText;
            var pinned = !string.IsNullOrEmpty(options.Back) ? " data-deck-back-pinned=\"true\"" : string.Empty;

            sb.AppendLine($"<a class=\"deck-back\" id=\"deck-back\" href=\"{EscapeHtmlAttr(href)}\"{pinned}>");
            sb.AppendLine("  <i class=\"fi fi-rr-angle-small-left\" aria-hidden=\"true\"></i>");
            sb.AppendLine($"  <span>{EscapeHtmlText(text)}</span>");
            sb.AppendLine("</a>");
        }

        // The deck's standing brand mark: a logo, a line of text, or both, pinned to
        // the bottom-right corner of every slide. It sits outside the encrypted
        // payload alongside the back control, so a locked deck still carries its
        // owner's mark, and the control bar reserves room for it so the slide
        // counter never lands underneath (see presentation.css / presentation.js).
        private void RenderDeckBrand(StringBuilder sb, PresentationOptions options)
        {
            if (!options.HasBrand) return;

            var hasLink = !string.IsNullOrEmpty(options.LogoLink);
            var tag = hasLink ? "a" : "div";

            sb.Append($"<{tag} class=\"deck-brand\"");
            if (hasLink)
            {
                sb.Append($" href=\"{EscapeHtmlAttr(options.LogoLink)}\"");
                if (options.LogoLink.Contains("://"))
                {
                    sb.Append(" target=\"_blank\" rel=\"noopener noreferrer\"");
                }
            }
            sb.AppendLine(">");

            if (!string.IsNullOrEmpty(options.Logo))
            {
                // With text beside it the logo is decorative — an alt would have a
                // screen reader announce the same brand twice.
                var alt = !string.IsNullOrEmpty(options.LogoText)
                    ? string.Empty
                    : (options.LogoAlt ?? _config.Branding?.Title ?? string.Empty);
                sb.AppendLine($"  <img src=\"{EscapeHtmlAttr(EncodeAssetUrl(options.Logo))}\" alt=\"{EscapeHtmlAttr(alt)}\">");
            }

            if (!string.IsNullOrEmpty(options.LogoText))
            {
                sb.AppendLine($"  <span class=\"deck-brand-text\">{EscapeHtmlText(options.LogoText)}</span>");
            }

            sb.AppendLine($"</{tag}>");
        }

        private static string EscapeHtmlText(string value)
            => string.IsNullOrEmpty(value) ? value : System.Net.WebUtility.HtmlEncode(value);
    }
}
