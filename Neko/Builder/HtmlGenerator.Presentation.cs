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

            sb.AppendLine($"<body class=\"neko-deck-body\" data-deck-accent=\"{EscapeHtmlAttr(options.Accent)}\">");

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

            var body = new StringBuilder();
            RenderSlides(body, slides, options);
            RenderDeckBar(body, options);
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

            // The deck's own typeface trio — a geometric display face, a reading serif
            // and a mono for labels. Pulled from Google Fonts by default; `fonts: none`
            // in the deck options drops the link and falls back to the local stacks
            // declared in presentation.css (for air-gapped or self-hosted-font sites).
            if (!string.Equals(options.Fonts, "none", System.StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
                sb.AppendLine("    <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
                sb.AppendLine("    <link href=\"https://fonts.googleapis.com/css2?family=Archivo:wght@500;600;800&family=Source+Serif+4:opsz,wght@8..60,400;8..60,600&family=IBM+Plex+Mono:wght@400;500&display=swap\" rel=\"stylesheet\">");
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

                var classes = "deck-slide" + (string.IsNullOrEmpty(extraClass) ? "" : " " + extraClass);
                sb.AppendLine(
                    $"<section class=\"{EscapeHtmlAttr(classes)}\" id=\"{EscapeHtmlAttr(id)}\" " +
                    $"data-accent=\"{EscapeHtmlAttr(accent)}\" data-layout=\"{EscapeHtmlAttr(layout)}\" " +
                    $"aria-label=\"Slide {i + 1} of {slides.Count}\">");

                if (!string.IsNullOrEmpty(eyebrow))
                {
                    sb.AppendLine($"  <p class=\"deck-eyebrow\"><span aria-hidden=\"true\"></span>{EscapeHtmlText(eyebrow)}</p>");
                }

                sb.AppendLine(slide.Html ?? string.Empty);
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</main>");
        }

        // A slide that opens on an <h1> is the deck's title (or a section divider) and
        // gets the roomier, centred treatment without the author having to say so.
        private static string InferLayout(PresentationSlide slide)
        {
            var html = slide.Html ?? string.Empty;
            return html.Contains("<h1", System.StringComparison.OrdinalIgnoreCase) ? "title" : "default";
        }

        private void RenderDeckBar(StringBuilder sb, PresentationOptions options)
        {
            sb.AppendLine("<div class=\"deck-bar\">");
            sb.AppendLine("  <button id=\"deck-prev\" type=\"button\" aria-label=\"Previous slide\"><i class=\"fi fi-rr-angle-small-left\" aria-hidden=\"true\"></i> prev</button>");
            sb.AppendLine("  <button id=\"deck-next\" type=\"button\" aria-label=\"Next slide\">next <i class=\"fi fi-rr-angle-small-right\" aria-hidden=\"true\"></i></button>");
            if (options.Counter)
            {
                sb.AppendLine("  <span class=\"deck-count\" id=\"deck-count\"></span>");
            }
            sb.AppendLine("</div>");
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

        private static string EscapeHtmlText(string value)
            => string.IsNullOrEmpty(value) ? value : System.Net.WebUtility.HtmlEncode(value);
    }
}
