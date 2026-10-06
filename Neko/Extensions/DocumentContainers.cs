using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Neko.Builder;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Neko.Extensions
{
    /// <summary>
    /// The page components of a paged document (<c>document:</c> front matter). They
    /// are ordinary <c>::: name</c> containers, but only inside a document (see
    /// <see cref="DocumentScope"/>); each renders plain HTML with <c>doc-*</c> classes
    /// that the document stylesheets draw and the Word export reads.
    /// </summary>
    internal static class DocumentContainers
    {
        public static bool TryRender(HtmlRenderer renderer, CustomContainer obj)
        {
            switch (obj.Info)
            {
                case "cols": Cols(renderer, obj); return true;
                case "col": Wrap(renderer, obj, "div", "doc-col"); return true;
                case "term": Term(renderer, obj); return true;
                case "box": Box(renderer, obj); return true;
                case "note": Note(renderer, obj); return true;
                case "keypoint": case "key": KeyPoint(renderer, obj); return true;
                case "lead": Wrap(renderer, obj, "div", "doc-lead"); return true;
                case "claim": Wrap(renderer, obj, "div", "doc-claim"); return true;
                case "figure": Figure(renderer, obj); return true;
                case "flow": Flow(renderer, obj); return true;
                case "facts": Facts(renderer, obj); return true;
                case "entries": WrapData(renderer, obj, "doc-entries", "style", "contents"); return true;
                case "entry": Entry(renderer, obj); return true;
                case "steps": WrapData(renderer, obj, "doc-steps", "style", "stairs"); return true;
                case "step": Step(renderer, obj); return true;
                case "stats": WrapData(renderer, obj, "doc-stats", "style", "stagger"); return true;
                case "stat": Stat(renderer, obj); return true;
                case "quote": Quote(renderer, obj); return true;
                case "glossary": Wrap(renderer, obj, "div", "doc-glossary"); return true;
                case "aside": Wrap(renderer, obj, "aside", "doc-aside"); return true;
                case "art": Art(renderer, obj); return true;
                default: return false;
            }
        }

        private static string Attr(CustomContainer obj, string key)
        {
            var props = obj.GetAttributes().Properties;
            if (props == null) return null;
            foreach (var p in props) if (p.Key == key) return p.Value;
            return null;
        }

        // The text of an inline tree as written: literals, code, and the vertical bars the pipe-table
        // parser lifts out of a paragraph. The flow diagram reads its boxes from bullets this way.
        private static string PlainText(Markdig.Syntax.Inlines.Inline inline)
        {
            var sb = new System.Text.StringBuilder();
            void Walk(Markdig.Syntax.Inlines.Inline node)
            {
                for (; node != null; node = node.NextSibling)
                {
                    switch (node)
                    {
                        case Markdig.Syntax.Inlines.LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                        case Markdig.Syntax.Inlines.CodeInline code: sb.Append(code.Content); break;
                        case Markdig.Syntax.Inlines.LineBreakInline: sb.Append(' '); break;
                        case Markdig.Extensions.Tables.PipeTableDelimiterInline: sb.Append('|'); break;
                        case Markdig.Syntax.Inlines.ContainerInline container: Walk(container.FirstChild); break;
                    }
                }
            }
            Walk(inline);
            return sb.ToString();
        }

        private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static void Wrap(HtmlRenderer renderer, CustomContainer obj, string tag, string cls)
        {
            renderer.Write($"<{tag} class=\"{cls}\">");
            renderer.WriteChildren(obj);
            renderer.Write($"</{tag}>");
        }

        private static void WrapData(HtmlRenderer renderer, CustomContainer obj, string cls, string key, string fallback)
        {
            var value = Attr(obj, key) ?? fallback;
            renderer.Write($"<div class=\"{cls}\" data-{key}=\"{Enc(value)}\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: cols {count="3" ratio="1.15:1"}` — side-by-side columns; the children are whatever the author writes.
        private static void Cols(HtmlRenderer renderer, CustomContainer obj)
        {
            var count = Attr(obj, "count") ?? Attr(obj, "cols") ?? "2";
            var ratio = Attr(obj, "ratio");
            renderer.Write($"<div class=\"doc-cols\" data-cols=\"{Enc(count)}\"");
            if (!string.IsNullOrEmpty(ratio)) renderer.Write($" style=\"--doc-cols-ratio:{Enc(string.Join(' ', ratio.Split(':', ' ').Where(x => x.Length > 0).Select(x => x + "fr")))}\"");
            var style = Attr(obj, "style");
            if (!string.IsNullOrEmpty(style)) renderer.Write($" data-style=\"{Enc(style)}\"");
            renderer.Write(">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: box {label="…" title="…" ground="stone"}` — a panel on its own ground.
        private static void Box(HtmlRenderer renderer, CustomContainer obj)
        {
            var ground = Attr(obj, "ground");
            renderer.Write("<div class=\"doc-box\"" + (string.IsNullOrEmpty(ground) ? "" : $" data-ground=\"{Enc(ground)}\"") + ">");
            var label = Attr(obj, "label");
            var title = Attr(obj, "title");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"doc-box-label\">{Enc(label)}</p>");
            if (!string.IsNullOrEmpty(title)) renderer.Write($"<h3 class=\"doc-box-title\">{Enc(title)}</h3>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: note {label="BM25"}` — a margin note: a rule, a mono label, a muted line.
        private static void Note(HtmlRenderer renderer, CustomContainer obj)
        {
            var label = Attr(obj, "label") ?? Attr(obj, "title");
            var ground = Attr(obj, "ground");
            var plain = Attr(obj, "plain");
            renderer.Write("<aside class=\"doc-note\"" + (string.IsNullOrEmpty(ground) ? "" : $" data-ground=\"{Enc(ground)}\"")
                + (string.IsNullOrEmpty(plain) ? "" : " data-plain=\"true\"") + ">");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"doc-note-label\">{Enc(label)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</aside>");
        }

        // `::: keypoint` — the line to take away, on the deep blue (or any ground).
        private static void KeyPoint(HtmlRenderer renderer, CustomContainer obj)
        {
            var label = Attr(obj, "label") ?? "Key point";
            var ground = Attr(obj, "ground");
            renderer.Write("<div class=\"doc-key\"" + (string.IsNullOrEmpty(ground) ? "" : $" data-ground=\"{Enc(ground)}\"") + ">");
            if (label != "none") renderer.Write($"<p class=\"doc-key-label\">{Enc(label)}</p>");
            renderer.Write("<div class=\"doc-key-body\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div></div>");
        }

        // `::: figure {caption="…" plate="stone"}` — a figure with a mono caption, optionally on a full-bleed plate.
        private static void Figure(HtmlRenderer renderer, CustomContainer obj)
        {
            var caption = Attr(obj, "caption");
            var plate = Attr(obj, "plate");
            renderer.Write("<figure class=\"doc-figure\"" + (string.IsNullOrEmpty(plate) ? "" : $" data-plate=\"{Enc(plate)}\"") + ">");
            renderer.WriteChildren(obj);
            if (!string.IsNullOrEmpty(caption)) renderer.Write($"<figcaption class=\"doc-figure-caption\">{Enc(caption)}</figcaption>");
            renderer.Write("</figure>");
        }

        // `::: flow {caption="…" layout="row|path|compact"}` with a bullet list: one bullet per row, boxes
        // separated by `||`, each box `[mark] [solid] label | title | note`.
        private static void Flow(HtmlRenderer renderer, CustomContainer obj)
        {
            var rows = new List<List<FlowNode>>();
            foreach (var list in obj.Descendants().OfType<ListBlock>())
            {
                foreach (var item in list.OfType<ListItemBlock>())
                {
                    var text = string.Join(" ", item.Descendants().OfType<LeafBlock>().Where(b => b.Inline != null)
                        .Select(b => PlainText(b.Inline).Trim()));
                    rows.Add(text.Split(new[] { "||" }, System.StringSplitOptions.None).Select(FlowNode.Parse).ToList());
                }
                break;
            }
            var caption = Attr(obj, "caption");
            var label = Attr(obj, "label") ?? caption ?? "Diagram";
            var plate = Attr(obj, "plate");
            renderer.Write("<figure class=\"doc-figure doc-flow\"" + (string.IsNullOrEmpty(plate) ? "" : $" data-plate=\"{Enc(plate)}\"") + ">");
            renderer.Write(DocumentArt.Flow(rows, Attr(obj, "layout"), label));
            if (!string.IsNullOrEmpty(caption)) renderer.Write($"<figcaption class=\"doc-figure-caption\">{Enc(caption)}</figcaption>");
            renderer.Write("</figure>");
        }

        // `::: facts` over a definition list — `Topic` / `:   Search architecture` — becomes a row of
        // cells, each a mono label over its value.
        private static void Facts(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write("<div class=\"doc-facts\">");
            foreach (var item in obj.Descendants().OfType<Markdig.Extensions.DefinitionLists.DefinitionItem>())
            {
                var term = item.OfType<Markdig.Extensions.DefinitionLists.DefinitionTerm>().FirstOrDefault();
                renderer.Write("<div class=\"doc-fact\"><p class=\"doc-fact-label\">");
                if (term != null) renderer.Write(Enc(term.Inline != null ? PlainText(term.Inline).Trim() : term.Lines.ToString().Trim()));
                renderer.Write("</p><div class=\"doc-fact-value\">");
                foreach (var child in item.Where(b => b is not Markdig.Extensions.DefinitionLists.DefinitionTerm)) renderer.Render(child);
                renderer.Write("</div></div>");
            }
            renderer.Write("</div>");
        }

        // `::: term {name="BM25"}` — one glossary definition: the term in mono, then its plain-words meaning.
        private static void Term(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write($"<div class=\"doc-term\"><p class=\"doc-term-name\">{Enc(Attr(obj, "name") ?? Attr(obj, "title"))}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: entry {num="01" title="Search is not one thing" page="03"}` — a row of a contents list or a takeaway list.
        private static void Entry(HtmlRenderer renderer, CustomContainer obj)
        {
            var num = Attr(obj, "num") ?? Attr(obj, "n");
            var title = Attr(obj, "title");
            var page = Attr(obj, "page");
            renderer.Write("<div class=\"doc-entry\"" + (string.IsNullOrEmpty(Attr(obj, "muted")) ? "" : " data-muted=\"true\"") + ">");
            renderer.Write($"<span class=\"doc-entry-num\">{Enc(num)}</span>");
            renderer.Write("<div class=\"doc-entry-body\">");
            if (!string.IsNullOrEmpty(title)) renderer.Write($"<p class=\"doc-entry-title\">{Enc(title)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
            renderer.Write($"<span class=\"doc-entry-page\">{Enc(page)}</span>");
            renderer.Write("</div>");
        }

        // `::: step {num="01" ground="ink"}` — one stage of a `::: steps` staircase.
        private static void Step(HtmlRenderer renderer, CustomContainer obj)
        {
            var ground = Attr(obj, "ground");
            var num = Attr(obj, "num") ?? Attr(obj, "n");
            renderer.Write("<div class=\"doc-step\"" + (string.IsNullOrEmpty(ground) ? "" : $" data-ground=\"{Enc(ground)}\"") + ">");
            if (!string.IsNullOrEmpty(num)) renderer.Write($"<p class=\"doc-step-num\">{Enc(num)}</p>");
            renderer.Write("<div class=\"doc-step-body\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div></div>");
        }

        // `::: stat {value="30TB+" label="Data connected" ground="paper"}` — one figure of a `::: stats` grid.
        private static void Stat(HtmlRenderer renderer, CustomContainer obj)
        {
            var ground = Attr(obj, "ground");
            renderer.Write("<div class=\"doc-stat\"" + (string.IsNullOrEmpty(ground) ? "" : $" data-ground=\"{Enc(ground)}\"") + ">");
            renderer.Write($"<p class=\"doc-stat-value\">{Enc(Attr(obj, "value"))}</p>");
            var label = Attr(obj, "label");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"doc-stat-label\">{Enc(label)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: quote {by="Services Innovation Team · Airline Services at Airbus"}` — a customer's own words. A
        // lone image at the top is the customer's mark: it is drawn in the page's ink, through the image as a mask.
        private static void Quote(HtmlRenderer renderer, CustomContainer obj)
        {
            var by = Attr(obj, "by");
            string logoUrl = null, logoAlt = null;
            Block logoBlock = null;
            foreach (var child in obj)
            {
                if (child is ParagraphBlock para && para.Inline?.FirstChild is Markdig.Syntax.Inlines.LinkInline link && link.IsImage && link.NextSibling == null)
                {
                    logoUrl = link.Url;
                    logoAlt = PlainText(link.FirstChild);
                    logoBlock = para;
                }
                break;
            }
            renderer.Write("<figure class=\"doc-quote\">");
            if (logoUrl != null)
            {
                renderer.Write($"<div class=\"doc-quote-logo\" role=\"img\" aria-label=\"{Enc(logoAlt)}\" style=\"-webkit-mask-image:url('{Enc(logoUrl)}');mask-image:url('{Enc(logoUrl)}')\"></div>");
            }
            renderer.Write("<blockquote>");
            foreach (var child in obj) if (!ReferenceEquals(child, logoBlock)) renderer.Render(child);
            renderer.Write("</blockquote>");
            if (!string.IsNullOrEmpty(by)) renderer.Write($"<figcaption>{Enc(by)}</figcaption>");
            renderer.Write("</figure>");
        }

        // `::: art {kind="field" width="595" height="260" dx=".5" dy=".5"}` — generated art placed in the flow.
        private static void Art(HtmlRenderer renderer, CustomContainer obj)
        {
            int.TryParse(Attr(obj, "width"), out var w);
            int.TryParse(Attr(obj, "height"), out var h);
            double.TryParse(Attr(obj, "dx"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dx);
            double.TryParse(Attr(obj, "dy"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var dy);
            var svg = DocumentArt.Render(Attr(obj, "kind") ?? "field", w == 0 ? 595 : w, h == 0 ? 260 : h, dx == 0 ? .5 : dx, dy == 0 ? .5 : dy);
            if (svg != null) renderer.Write($"<div class=\"doc-art-block\" aria-hidden=\"true\">{svg}</div>");
        }
    }
}
