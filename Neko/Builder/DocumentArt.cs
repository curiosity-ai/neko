using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// Generated art for paged documents: the fields, bars and marks the curiosity
    /// theme draws on its covers, section openers and page feet. Every kind is an
    /// inline SVG whose colours come from classes (<c>art-dash</c>, <c>art-sq</c>,
    /// <c>art-bar</c>, <c>art-win</c>, <c>art-sig</c>) that the theme colours through
    /// <c>--doc-art-*</c> variables, so one drawing reads on paper and on ink. The
    /// Word export rasterises the SVG with the colours the page resolved.
    ///
    /// <list type="bullet">
    ///   <item><c>field</c> — short dashes turning round one signal square.</item>
    ///   <item><c>squares</c> — squares turning toward one point, the nearest one the signal.</item>
    ///   <item><c>bars</c> — three rows of vertical bars rising toward one point, one bar the signal.</item>
    ///   <item><c>window</c> — the Escape mark scaled up, its block a window onto a dash field.</item>
    /// </list>
    ///
    /// Units are points on a 595-wide page. The output is deterministic for a given
    /// kind and size, so a rebuild does not churn the generated HTML.
    /// </summary>
    public static partial class DocumentArt
    {
        public static readonly string[] Kinds = { "field", "squares", "bars", "window" };

        // The Escape mark: eight squares in a 3-row block and the ninth, escaped, at the top right.
        private static readonly int[][] Block =
        {
            new[] { 26, 26 }, new[] { 50, 26 }, new[] { 26, 50 }, new[] { 50, 50 },
            new[] { 74, 50 }, new[] { 26, 74 }, new[] { 50, 74 }, new[] { 74, 74 },
        };

        private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        /// <summary>Renders one kind of art as an inline SVG, or null for an unknown kind.</summary>
        public static string Render(string kind, int w, int h, double dx, double dy, string fit = null, string fade = "none", int id = 1)
        {
            switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "field": case "dash": case "dashes": return Field(w, h, dx, dy, fade, fit ?? "xMidYMid slice");
                case "squares": case "square": return Squares(w, h, dx, dy, fit ?? "xMidYMid slice");
                case "bars": case "bar": return Bars(w, h, dx, fit ?? "xMidYMax meet");
                case "window": return Window(id, fit ?? "xMaxYMax meet");
                default: return null;
            }
        }

        private static string Open(string kind, string viewBox, string fit)
            => $"<svg class=\"doc-art-svg\" data-art=\"{kind}\" viewBox=\"{viewBox}\" preserveAspectRatio=\"{fit}\" aria-hidden=\"true\" focusable=\"false\">";

        // The dash field. Each dash turns by how near it is to the signal square:
        // circling it close in, level far away. `fade` dims it to one side.
        private static string Dashes(double w, double h, double dx, double dy, double pitch, string fade, double len, double sw, double hole, string cls)
        {
            int cols = (int)Math.Round(w / pitch), rows = (int)Math.Round(h / pitch);
            double cw = w / cols, ch = h / rows, sig = .3 * Math.Max(w, h), ax = dx * w, ay = dy * h;
            var sb = new StringBuilder();
            sb.Append($"<g class=\"{cls}\" stroke-width=\"{F(sw)}\" stroke-linecap=\"square\" fill=\"none\">");
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double x = c * cw + cw / 2, y = r * ch + ch / 2, ddx = x - ax, ddy = y - ay, d = Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (d < hole) continue;
                    double wt = Math.Exp(-(d * d) / (2 * sig * sig)), circ = Math.Atan2(ddy, ddx) + Math.PI / 2;
                    double a = Math.Atan2(wt * Math.Sin(circ), (1 - wt) + wt * Math.Cos(circ));
                    double o = fade == "right" ? .15 + .85 * (1 - (double)c / Math.Max(1, cols - 1))
                             : fade == "left" ? .15 + .85 * ((double)c / Math.Max(1, cols - 1)) : 1;
                    double hx = Math.Cos(a) * len / 2, hy = Math.Sin(a) * len / 2;
                    sb.Append($"<path d=\"M{F(x - hx)} {F(y - hy)}L{F(x + hx)} {F(y + hy)}\"");
                    if (o < 1) sb.Append($" opacity=\"{o.ToString("0.##", CultureInfo.InvariantCulture)}\"");
                    sb.Append("/>");
                }
            }
            sb.Append("</g>");
            return sb.ToString();
        }

        private static string Field(int w, int h, double dx, double dy, string fade, string fit)
        {
            var sb = new StringBuilder(Open("field", $"0 0 {w} {h}", fit));
            sb.Append(Dashes(w, h, dx, dy, 34, fade, 17, 1.5, 16, "art-dash"));
            sb.Append($"<rect class=\"art-sig\" x=\"{F(dx * w - 6)}\" y=\"{F(dy * h - 6)}\" width=\"12\" height=\"12\"/>");
            return sb.Append("</svg>").ToString();
        }

        private static string Squares(int w, int h, double dx, double dy, string fit)
        {
            const double p = 34, z = 10;
            int cols = (int)Math.Round(w / p), rows = (int)Math.Round(h / p);
            double cw = (double)w / cols, ch = (double)h / rows, sig = .3 * Math.Max(w, h), ax = dx * w, ay = dy * h;
            var cells = new List<(double x, double y, double d)>();
            double best = 1e9; int bi = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double x = c * cw + cw / 2, y = r * ch + ch / 2, d = Math.Sqrt((x - ax) * (x - ax) + (y - ay) * (y - ay));
                    cells.Add((x, y, d));
                    if (d < best) { best = d; bi = cells.Count - 1; }
                }
            }
            var sb = new StringBuilder(Open("squares", $"0 0 {w} {h}", fit));
            for (int i = 0; i < cells.Count; i++)
            {
                var (x, y, d) = cells[i];
                if (i == bi)
                {
                    sb.Append($"<rect class=\"art-sig\" x=\"{F(x - z)}\" y=\"{F(y - z)}\" width=\"{F(2 * z)}\" height=\"{F(2 * z)}\"/>");
                    continue;
                }
                double wt = Math.Exp(-(d * d) / (2 * sig * sig));
                sb.Append($"<rect class=\"art-sq\" x=\"{F(x - z)}\" y=\"{F(y - z)}\" width=\"{F(2 * z)}\" height=\"{F(2 * z)}\" opacity=\"{(.12 + .5 * wt).ToString("0.##", CultureInfo.InvariantCulture)}\" transform=\"rotate({F(45 * wt)} {F(x)} {F(y)})\"/>");
            }
            return sb.Append("</svg>").ToString();
        }

        private static string Bars(int w, int h, double dx, string fit)
        {
            const double pitch = 11.5, gap = 14;
            int cols = (int)Math.Floor(w / pitch);
            double ax = dx * w;
            var hs = new[] { .46, .32, .22 }.Select(f => f * (h - 2 * gap)).ToArray();
            var sb = new StringBuilder(Open("bars", $"0 0 {w} {h}", fit));
            double y = 0;
            for (int r = 0; r < hs.Length; r++)
            {
                double rh = hs[r], sig = .22 * w * (1 - r * .15), best = 1e9; int bx = 0;
                for (int c = 0; c < cols; c++)
                {
                    double x = c * pitch + pitch / 2, d = Math.Abs(x - ax - r * 14);
                    if (d < best) { best = d; bx = c; }
                }
                for (int c = 0; c < cols; c++)
                {
                    double x = c * pitch + pitch / 2, d = x - ax - r * 14, wt = Math.Exp(-(d * d) / (2 * sig * sig));
                    double bh = rh * (.12 + .88 * wt);
                    bool on = r == 0 && c == bx;
                    sb.Append($"<rect class=\"{(on ? "art-sig" : "art-bar")}\" x=\"{F(x - 2.6)}\" y=\"{F(y + rh - bh)}\" width=\"5.2\" height=\"{F(bh)}\"/>");
                }
                y += rh + gap;
            }
            return sb.Append("</svg>").ToString();
        }

        // The pixel window: the mark scaled up, its block a window onto the field.
        private static string Window(int id, string fit)
        {
            var clip = string.Concat(Block.Select(b => $"<rect x=\"{b[0] - 24}\" y=\"{b[1]}\" width=\"20\" height=\"20\"/>"));
            var sb = new StringBuilder(Open("window", "0 0 96 96", fit));
            sb.Append($"<defs><clipPath id=\"doc-win-{id}\">{clip}</clipPath></defs>");
            sb.Append($"<g clip-path=\"url(#doc-win-{id})\"><rect class=\"art-win\" x=\"0\" y=\"24\" width=\"72\" height=\"72\"/>");
            sb.Append("<g transform=\"translate(0 24)\">").Append(Dashes(72, 72, .7, .3, 4.2, "none", 2.4, .32, 0, "art-dash2")).Append("</g></g>");
            sb.Append("<rect class=\"art-sig\" x=\"74\" y=\"2\" width=\"20\" height=\"20\"/>");
            return sb.Append("</svg>").ToString();
        }

        /// <summary>The Escape mark alone: the block in the current colour, the escaped square in the signal colour.</summary>
        public static string Mark(bool signal)
        {
            var sb = new StringBuilder("<svg class=\"doc-mark-svg\" viewBox=\"24 0 96 96\" aria-hidden=\"true\" focusable=\"false\"><g fill=\"currentColor\">");
            foreach (var b in Block) sb.Append($"<rect x=\"{b[0]}\" y=\"{b[1]}\" width=\"20\" height=\"20\"/>");
            sb.Append("</g>");
            sb.Append($"<rect x=\"98\" y=\"2\" width=\"20\" height=\"20\"{(signal ? " class=\"art-sig\"" : " fill=\"currentColor\"")}/>");
            return sb.Append("</svg>").ToString();
        }

        /// <summary>The brand lockup — the Escape mark and the outlined wordmark — in the current colour. Height is 1em.</summary>
        public static string Lockup()
        {
            // Em units * 1000. The mark is .86em tall and sits on the baseline box's bottom,
            // .32em before the wordmark.
            const double markH = 860, markY = 120, gap = 320;
            double scale = markH / 96.0, wordX = markH + gap, width = wordX + WordmarkWidth;
            var sb = new StringBuilder($"<svg class=\"doc-lockup-svg\" viewBox=\"0 0 {F(width)} 1000\" role=\"img\" aria-label=\"curiosity\" focusable=\"false\"><g fill=\"currentColor\">");
            sb.Append($"<g transform=\"translate(0 {F(markY)}) scale({scale.ToString("0.####", CultureInfo.InvariantCulture)}) translate(-24 0)\">");
            foreach (var b in Block) sb.Append($"<rect x=\"{b[0]}\" y=\"{b[1]}\" width=\"20\" height=\"20\"/>");
            sb.Append("<rect x=\"98\" y=\"2\" width=\"20\" height=\"20\"/></g>");
            sb.Append($"<path transform=\"translate({F(wordX)} 0)\" d=\"{WordmarkPath}\"/>");
            foreach (var d in WordmarkDots) sb.Append($"<rect x=\"{F(wordX + d[0])}\" y=\"{F(d[1])}\" width=\"{F(d[2])}\" height=\"{F(d[2])}\"/>");
            return sb.Append("</g></svg>").ToString();
        }

        /// <summary>The two small squares that open the running head of a cover.</summary>
        public static string Sq2()
            => "<svg class=\"doc-sq2-svg\" viewBox=\"0 0 12 10\" aria-hidden=\"true\" focusable=\"false\"><g fill=\"currentColor\"><rect x=\"0\" y=\"4\" width=\"5\" height=\"5\"/><rect x=\"6\" y=\"1\" width=\"5\" height=\"5\"/></g></svg>";
    }
}
