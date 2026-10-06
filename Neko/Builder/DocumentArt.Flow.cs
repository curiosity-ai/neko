using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Neko.Builder
{
    /// <summary>One box of a flow diagram: a mono label, a title and a line of detail.</summary>
    public class FlowNode
    {
        public string Label { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        /// <summary>The diagram's marked step: a signal square in the corner.</summary>
        public bool Mark { get; set; }

        /// <summary>An inverted box: ink on the page's own ground.</summary>
        public bool Solid { get; set; }

        /// <summary>
        /// Reads <c>[mark] [solid] label | title | note</c>. The two flags are optional and
        /// may come in either order; the three fields are separated by a vertical bar.
        /// </summary>
        public static FlowNode Parse(string text)
        {
            var node = new FlowNode();
            var rest = (text ?? string.Empty).Trim();
            while (true)
            {
                if (rest.StartsWith("[mark]", StringComparison.OrdinalIgnoreCase)) { node.Mark = true; rest = rest.Substring(6).TrimStart(); continue; }
                if (rest.StartsWith("[solid]", StringComparison.OrdinalIgnoreCase)) { node.Solid = true; rest = rest.Substring(7).TrimStart(); continue; }
                break;
            }
            var parts = rest.Split('|').Select(p => p.Trim()).ToArray();
            if (parts.Length == 1) { node.Title = parts[0]; }
            else if (parts.Length == 2) { node.Label = parts[0]; node.Title = parts[1]; }
            else { node.Label = parts[0]; node.Title = parts[1]; node.Note = string.Join(" | ", parts.Skip(2)); }
            return node;
        }
    }

    public static partial class DocumentArt
    {
        private static string Esc(string s) => WebUtility.HtmlEncode(s ?? string.Empty);

        /// <summary>
        /// A flow diagram: rows of boxes joined by arrows. A single row runs left to
        /// right ("row"); several rows run top to bottom, and a row of two boxes
        /// forks the arrow ("path"); "compact" is the narrow vertical stack. Boxes
        /// draw in the current colour, so the diagram follows the page's tones.
        /// </summary>
        public static string Flow(List<List<FlowNode>> rows, string layout, string title)
        {
            rows = rows.Where(r => r.Count > 0).ToList();
            if (rows.Count == 0) return string.Empty;
            layout = string.IsNullOrEmpty(layout) ? (rows.Count == 1 ? "row" : "path") : layout.ToLowerInvariant();

            var body = new StringBuilder();
            double width, height;

            void Text(double x, double y, string text, string cls, double size, string extra = "")
                => body.Append($"<text x=\"{F(x)}\" y=\"{F(y)}\" class=\"{cls}\" font-size=\"{F(size)}\" {extra}>{Esc(text)}</text>");

            void Box(double x, double y, double w, double h, FlowNode n, string mode)
            {
                body.Append($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" class=\"{(n.Solid ? "f-solid" : "f-box")}\"/>");
                var ink = n.Solid ? "f-inv" : "f-ink";
                var mute = n.Solid ? "f-inv2" : "f-mute";
                var label = n.Label.ToUpperInvariant();
                if (mode == "compact")
                {
                    if (label.Length > 0) Text(x + 7, y + 14, label, mute + " f-mono", 6.2, "letter-spacing=\".5\"");
                    Text(x + 7, y + h - 7, n.Title, ink, 10, "font-weight=\"500\"");
                    if (n.Note.Length > 0) Text(x + w - 7, y + h - 7, n.Note, mute, 6.6, "text-anchor=\"end\"");
                }
                else if (mode == "row")
                {
                    if (label.Length > 0) Text(x + 7, y + 14, label, mute + " f-mono", 6.2, "letter-spacing=\".5\"");
                    Text(x + 7, y + h - 22, n.Title, ink, 10, "font-weight=\"500\"");
                    if (n.Note.Length > 0) Text(x + 7, y + h - 10, n.Note, mute, 6.6);
                }
                else
                {
                    if (label.Length > 0) Text(x + 10, y + 16, label, mute + " f-mono", 7, "letter-spacing=\".5\"");
                    Text(x + 10, y + h - 24, n.Title, ink, 12, "font-weight=\"500\"");
                    if (n.Note.Length > 0) Text(x + 10, y + h - 10, n.Note, mute, 7.6);
                }
                if (n.Mark) body.Append($"<rect class=\"art-sig\" x=\"{F(x + w - (mode == "path" ? 16 : 13))}\" y=\"{F(y + (mode == "path" ? 8 : 6))}\" width=\"{(mode == "path" ? 8 : 7)}\" height=\"{(mode == "path" ? 8 : 7)}\"/>");
            }

            if (layout == "row")
            {
                var nodes = rows[0];
                int n = nodes.Count;
                width = 480; height = 78;
                double gap = 9.6, bw = (width - gap * (n - 1) - 1) / n;
                for (int i = 0; i < n; i++)
                {
                    double x = i * (bw + gap) + .5;
                    Box(x, .5, bw, height - 1, nodes[i], "row");
                    if (i < n - 1)
                    {
                        double x1 = x + bw + 1.5, x2 = x + bw + gap - 1.5, y = height / 2;
                        body.Append($"<line x1=\"{F(x1)}\" y1=\"{F(y)}\" x2=\"{F(x2)}\" y2=\"{F(y)}\" class=\"f-line\"/>");
                        body.Append($"<polyline points=\"{F(x2 - 2.6)},{F(y - 2.6)} {F(x2)},{F(y)} {F(x2 - 2.6)},{F(y + 2.6)}\" class=\"f-line\" fill=\"none\"/>");
                    }
                }
            }
            else
            {
                bool compact = layout == "compact";
                width = compact ? 200 : 400;
                double bh = compact ? 34 : 58, g = compact ? 10 : 22, y = .5;
                string mode = compact ? "compact" : "path";
                double? prevMid = null;
                List<double> prevCenters = null;
                for (int r = 0; r < rows.Count; r++)
                {
                    var nodes = rows[r];
                    int n = nodes.Count;
                    double inner = width - 1, ngap = n > 1 ? 11 : 0, bw = (inner - ngap * (n - 1)) / n;
                    var centers = new List<double>();
                    for (int i = 0; i < n; i++)
                    {
                        double x = .5 + i * (bw + ngap);
                        Box(x, y, bw, bh, nodes[i], mode);
                        centers.Add(x + bw / 2);
                    }
                    if (r > 0)
                    {
                        // The arrows join the row above to this one at the centres of whichever has more boxes.
                        var at = (prevCenters.Count >= centers.Count ? prevCenters : centers);
                        foreach (var cx in at)
                        {
                            double y1 = y - g + 2, y2 = y - 2;
                            body.Append($"<line x1=\"{F(cx)}\" y1=\"{F(y1)}\" x2=\"{F(cx)}\" y2=\"{F(y2)}\" class=\"f-line\"/>");
                            body.Append($"<polyline points=\"{F(cx - 3)},{F(y2 - 3)} {F(cx)},{F(y2)} {F(cx + 3)},{F(y2 - 3)}\" class=\"f-line\" fill=\"none\"/>");
                        }
                    }
                    prevCenters = centers;
                    prevMid = y + bh / 2;
                    y += bh + g;
                }
                height = Math.Ceiling(y - g + 1);
            }

            return $"<svg class=\"doc-flow-svg\" data-layout=\"{layout}\" viewBox=\"0 0 {F(width)} {F(height)}\" role=\"img\" aria-label=\"{Esc(title)}\" focusable=\"false\">{body}</svg>";
        }
    }
}
