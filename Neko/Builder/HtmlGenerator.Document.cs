using Neko.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        /// <summary>
        /// Renders a paged document: a standalone page of A4 (or Letter) sheets on a desk,
        /// with a slim bar for the back control and the <c>docx</c> download, and none of
        /// the documentation shell. Each <c>---</c> in the source starts a sheet; a sheet
        /// whose content runs long grows, and the Word export lets it flow onto more
        /// pages. The same sheets are what the Word export reads, so the file matches
        /// what the page shows.
        /// </summary>
        public string GenerateDocument(ParsedDocument document)
        {
            var options = document.PagedDocument ?? new DocumentOptions();
            var pages = document.Pages ?? new List<DocumentPage>();

            var docTitle = !string.IsNullOrEmpty(document.FrontMatter.Title)
                ? document.FrontMatter.Title
                : document.FrontMatter.Label;
            if (string.IsNullOrEmpty(docTitle)) docTitle = _config.Branding.Title;

            var description = !string.IsNullOrEmpty(document.FrontMatter.Description)
                ? document.FrontMatter.Description
                : _config.Meta.Description;

            var effectivePassword = ResolveEffectivePassword(document);
            var isProtected = !string.IsNullOrEmpty(effectivePassword);

            // A protected document ships nothing page-specific in the static HTML.
            var headTitle = isProtected ? _config.Branding.Title : docTitle;
            var headDescription = isProtected ? _config.Meta.Description : description;

            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang=\"en\" class=\"neko-doc-html\" data-doc-theme=\"{EscapeHtmlAttr(options.Theme)}\" data-doc-size=\"{EscapeHtmlAttr(options.IsLetter ? "letter" : "a4")}\">");
            GenerateDocumentHead(sb, headTitle, headDescription, options, prefix);

            sb.AppendLine("<body class=\"neko-doc-body\">");
            RenderDocumentBar(sb, options, docTitle, description, document.FrontMatter, prefix);

            var body = new StringBuilder();
            RenderDocumentPages(body, pages, options);
            body.AppendLine("<script>(function r(n){ if (window.nekoDocInit) { window.nekoDocInit(); } else if (n > 0) { setTimeout(function(){ r(n - 1); }, 50); } })(60);</script>");

            if (isProtected)
            {
                sb.AppendLine("<div class=\"doc-locked\">");
                RenderProtectedColumn(sb, body.ToString(), effectivePassword);
                sb.AppendLine("</div>");
            }
            else
            {
                sb.Append(body);
            }

            sb.AppendLine($"<script src=\"{prefix}/assets/document.js\"></script>");

            if (_isWatchMode)
            {
                RenderLiveReloadScript(sb);
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private void GenerateDocumentHead(StringBuilder sb, string title, string description, DocumentOptions options, string prefix)
        {
            sb.AppendLine("<head>");
            RenderHeadMeta(sb, title, description);
            RenderHeadTailwindAndTheme(sb);
            RenderHeadNekoConfig(sb);

            // The typefaces both themes use are bundled with Neko (assets/deckfonts/), so a
            // document needs no font host and the Word export embeds the very same files.
            if (options.FontSource != "none")
            {
                sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/deckfonts/deck-fonts.css\">");
            }

            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/uicons-regular-rounded.css\">");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/uicons-brands.css\">");
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/emoji.css\">");

            RenderHeadKatex(sb);
            RenderHeadMermaid(sb);
            RenderHeadHighlightJs(sb);

            // Loaded last so the sheet palette wins over the documentation chrome's rules above.
            sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{prefix}/assets/document.css\">");
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

        private void RenderDocumentBar(StringBuilder sb, DocumentOptions options, string docTitle, string description, FrontMatter frontMatter, string prefix)
        {
            var href = !string.IsNullOrEmpty(options.Back) ? options.Back : (string.IsNullOrEmpty(prefix) ? "/" : prefix + "/");
            var text = string.IsNullOrEmpty(options.BackText) ? "Back" : options.BackText;
            var pinned = !string.IsNullOrEmpty(options.Back) ? " data-doc-back-pinned=\"true\"" : string.Empty;

            sb.AppendLine("<div class=\"doc-bar\" id=\"doc-bar\">");
            sb.AppendLine($"  <a class=\"doc-back\" id=\"doc-back\" href=\"{EscapeHtmlAttr(href)}\"{pinned}><i class=\"fi fi-rr-angle-small-left\" aria-hidden=\"true\"></i> <span>{EscapeHtmlText(text)}</span></a>");
            sb.AppendLine("  <span class=\"doc-bar-spacer\"></span>");
            sb.AppendLine("  <span class=\"doc-count\" id=\"doc-count\"></span>");
            if (options.Download)
            {
                // Exports the pages to Word in the browser: document.js lazy-loads the
                // vendored docx bundle and document-docx.js on click. The title rides on the
                // button — inside the encrypted payload on a protected document — to name
                // the file and fill its properties.
                var fileName = DocxFileName(docTitle);
                sb.AppendLine(
                    $"  <button id=\"doc-download\" type=\"button\" aria-label=\"Download as Word\" title=\"Download as Word (.docx)\" " +
                    $"data-doc-title=\"{EscapeHtmlAttr(docTitle)}\" data-doc-description=\"{EscapeHtmlAttr(description ?? string.Empty)}\" " +
                    $"data-doc-author=\"{EscapeHtmlAttr(options.Author ?? _config.Meta.Author ?? string.Empty)}\" data-doc-company=\"{EscapeHtmlAttr(options.Company ?? string.Empty)}\" " +
                    $"data-doc-file=\"{EscapeHtmlAttr(fileName)}\">" +
                    "<i class=\"fi fi-rr-download\" aria-hidden=\"true\"></i> <span class=\"doc-download-label\">docx</span></button>");
            }
            sb.AppendLine("</div>");
        }

        private static string DocxFileName(string docTitle)
        {
            var slug = new StringBuilder();
            foreach (var ch in (docTitle ?? string.Empty).ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch)) slug.Append(ch);
                else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
            }
            var result = slug.ToString().Trim('-');
            return (string.IsNullOrEmpty(result) ? "document" : result) + ".docx";
        }

        private void RenderDocumentPages(StringBuilder sb, List<DocumentPage> pages, DocumentOptions options)
        {
            sb.AppendLine("<main class=\"docs\" id=\"docs\">");

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var layout = page.Get("layout") ?? "page";
                var ground = page.Get("ground") ?? DefaultGround(layout);
                var art = page.Get("art");
                var id = page.Get("id") ?? $"page-{i + 1}";
                var extraClass = page.Get("class");

                var classes = "doc-page" + (string.IsNullOrEmpty(extraClass) ? "" : " " + extraClass);
                sb.AppendLine(
                    $"<section class=\"{EscapeHtmlAttr(classes)}\" id=\"{EscapeHtmlAttr(id)}\" data-layout=\"{EscapeHtmlAttr(layout)}\" " +
                    $"data-ground=\"{EscapeHtmlAttr(ground)}\" " +
                    (string.IsNullOrEmpty(art) ? string.Empty : $"data-art=\"{EscapeHtmlAttr(art)}\" ") +
                    $"aria-label=\"Page {i + 1} of {pages.Count}\">");

                // Decorative art sits behind the text and is placed by the layout.
                if (!string.IsNullOrEmpty(art))
                {
                    var svg = RenderPageArt(page, layout, art, i);
                    if (svg != null) sb.AppendLine($"  <div class=\"doc-art\" data-art=\"{EscapeHtmlAttr(art)}\" data-doc-bg=\"art\" aria-hidden=\"true\">{svg}</div>");
                }
                if (layout == "sidebar") sb.AppendLine("  <div class=\"doc-panel\" data-doc-bg=\"panel\" aria-hidden=\"true\"></div>");

                RenderPageHead(sb, page, layout, options, i);

                sb.AppendLine("  <div class=\"doc-body\">");
                var number = page.Get("number");
                if (!string.IsNullOrEmpty(number)) sb.AppendLine($"    <p class=\"doc-number\">{EscapeHtmlText(number)}</p>");
                sb.AppendLine(page.Html ?? string.Empty);
                sb.AppendLine("  </div>");

                RenderPageFoot(sb, page, layout, options, i);
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</main>");
        }

        private static string DefaultGround(string layout) => layout switch
        {
            "opener" or "horizon" or "numbers" => "ink",
            "takeaways" or "back" => "deep",
            _ => "paper",
        };

        // The running head: a mono label at the top left, and optionally a line at the right.
        // The cover's carries the two-square mark. `head="none"` leaves it off.
        private void RenderPageHead(StringBuilder sb, DocumentPage page, string layout, DocumentOptions options, int index)
        {
            var headAttr = page.Get("head");
            if (string.Equals(headAttr, "none", StringComparison.OrdinalIgnoreCase)) return;

            var running = page.Get("running") ?? page.Get("eyebrow") ?? options.Running;
            var right = page.Get("head-right");
            if (string.IsNullOrEmpty(running) && string.IsNullOrEmpty(right)) return;

            sb.AppendLine("  <header class=\"doc-head\" data-doc-part=\"head\">");
            sb.Append("    <span class=\"doc-head-left\">");
            if (layout == "cover" && options.IsCuriosity) sb.Append(DocumentArt.Sq2());
            sb.AppendLine($"{EscapeHtmlText(running)}</span>");
            if (!string.IsNullOrEmpty(right)) sb.AppendLine($"    <span class=\"doc-head-right\">{EscapeHtmlText(right)}</span>");
            sb.AppendLine("  </header>");
        }

        // The foot: the brand at the left and the page number at the right. A page that
        // sets `foot="line one|line two"` prints that at the right instead of the number
        // (the cover's address, the back cover's contact), and `foot="none"` omits the foot.
        private void RenderPageFoot(StringBuilder sb, DocumentPage page, string layout, DocumentOptions options, int index)
        {
            var footAttr = page.Get("foot");
            if (string.Equals(footAttr, "none", StringComparison.OrdinalIgnoreCase)) return;

            sb.AppendLine("  <footer class=\"doc-foot\" data-doc-part=\"foot\">");
            sb.Append("    <span class=\"doc-brand\">");
            if (options.IsCuriosity)
            {
                sb.Append($"<span class=\"doc-lockup\">{DocumentArt.Lockup()}</span>");
            }
            else
            {
                if (!string.IsNullOrEmpty(options.Logo)) sb.Append($"<img src=\"{EscapeHtmlAttr(EncodeAssetUrl(options.Logo))}\" alt=\"\">");
                var text = options.LogoText ?? _config.Branding?.Title;
                if (!string.IsNullOrEmpty(text)) sb.Append($"<span class=\"doc-brand-text\">{EscapeHtmlText(text)}</span>");
            }
            sb.AppendLine("</span>");

            if (!string.IsNullOrEmpty(footAttr))
            {
                var lines = footAttr.Split('|').Select(l => EscapeHtmlText(l.Trim()));
                sb.AppendLine($"    <span class=\"doc-foot-text\">{string.Join("<br>", lines)}</span>");
            }
            else if (options.Numbers)
            {
                var number = options.IsCuriosity ? (index + 1).ToString("00", CultureInfo.InvariantCulture) : (index + 1).ToString(CultureInfo.InvariantCulture);
                sb.AppendLine($"    <span class=\"doc-pagenum\" data-doc-field=\"page\" data-doc-format=\"{(options.IsCuriosity ? "zero" : "plain")}\">{number}</span>");
            }
            sb.AppendLine("  </footer>");
        }

        // The art a page asks for with `art="field"`: the kind, and where its signal sits
        // (`dx` and `dy`, 0 to 1 across the drawing). Sized for the box the layout gives it.
        private string RenderPageArt(DocumentPage page, string layout, string art, int index)
        {
            double Num(string key, double fallback)
                => double.TryParse(page.Get(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

            var kind = art.Trim().ToLowerInvariant();
            int w = 595, h = 420; double dx = .6, dy = .45; string fit = null, fade = page.Get("fade") ?? "none";
            switch (layout)
            {
                case "cover": h = 440; dx = .55; dy = .42; break;
                case "opener": h = 260; dx = .78; dy = .5; fit = "xMaxYMax meet"; break;
                case "horizon": h = 370; dx = .66; dy = .5; fit = "xMidYMax meet"; break;
                case "sidebar": h = 420; break;
            }
            // Bars and the window are drawn smaller than their box and sit on one edge of it.
            if (kind == "bars") { w = (int)Math.Round(w * .8); h = (int)Math.Round(h * .8); }
            return DocumentArt.Render(kind, w, h, Num("dx", dx), Num("dy", dy), page.Get("fit") ?? fit, fade, index + 1);
        }
    }
}
