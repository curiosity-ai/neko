using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// Generated slide art for presentation decks: the backgrounds and figures a
    /// deck draws from numbers rather than image files. Every kind is an inline
    /// SVG whose colours come from classes (<c>art-ink</c>, <c>art-dim</c>,
    /// <c>art-hi</c>, <c>art-sig</c>) that each deck theme colours through
    /// <c>--deck-art-*</c> variables, so the same art reads on any ground.
    ///
    /// <list type="bullet">
    ///   <item><c>field</c> — a grid of short dashes bending round one point, the signal square.</item>
    ///   <item><c>bars</c> — rows of short vertical bars, a band of them lit, rising left to right.</item>
    ///   <item><c>squares</c> — a grid of squares turning toward one point, the square nearest it the signal.</item>
    ///   <item><c>glyph</c> — a 4 by 4 pixel glyph with one marked cell.</item>
    ///   <item><c>stair</c> — a diagonal band of short bars, the bar-rhythm staircase.</item>
    /// </list>
    ///
    /// The output is deterministic for a given kind, size and seed, so a rebuild
    /// does not churn the generated HTML.
    /// </summary>
    public static class DeckArt
    {
        public static readonly string[] Kinds = { "field", "bars", "squares", "glyph", "stair" };

        /// <summary>
        /// The named 4 by 4 glyphs: 16 cells read row by row, top to bottom —
        /// <c>k</c> a cell, <c>o</c> the marked cell, <c>.</c> empty.
        /// </summary>
        public static readonly Dictionary<string, string> Glyphs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["connected"] = "...o..k..k..k...",
            ["resolved"] = ".k..kok..k......",
            ["traceable"] = ".o...k...k..kkk.",
            ["sovereign"] = "kkk.kok.kkk.....",
            ["fast"] = "..k.kkko..k.....",
            ["open"] = "k..ok...kkk.....",
            ["precise"] = "k.k..o..k.k.....",
            ["curious"] = "...okk..kkk.kkk.",
            ["graph"] = "k..o.kk..kk.k..k",
            ["retrieval"] = "kk.kkkkkk..k...o",
            ["permissions"] = ".kk.k..kkkkkkkok",
            ["models"] = "kk..kk....kk..ko",
            ["connectors"] = ".kk.kkkokkkk.kk.",
            ["agenda"] = "k.kkk.kkokkkk.kk",
        };

        /// <summary>Renders one kind of art as an inline SVG, or null for an unknown kind.</summary>
        public static string Render(string kind, int width = 1920, int height = 360, int seed = 1, string glyph = null, double dotX = 0.5, double dotY = 0.5)
        {
            switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "field": return Field(width, height, dotX, dotY);
                case "bars": return Bars(width, height, seed);
                case "squares": return Squares(width, height, dotX, dotY);
                case "glyph": return Glyph(glyph);
                case "stair": return Stair(width, height);
                default: return null;
            }
        }

        private static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        private static string Open(int w, int h, string kind, bool slice = true)
            => $"<svg class=\"deck-art-svg\" data-art=\"{kind}\" viewBox=\"0 0 {w} {h}\" preserveAspectRatio=\"{(slice ? "xMidYMid slice" : "xMidYMid meet")}\" aria-hidden=\"true\" focusable=\"false\">";

        // The dash field: short dashes whose angle turns round the signal square,
        // circulating near it and flowing level far from it.
        private static string Field(int w, int h, double dotX, double dotY)
        {
            const double pitch = 48, len = 18;
            int cols = (int)Math.Round(w / pitch), rows = (int)Math.Max(1, Math.Round(h / pitch));
            double cw = (double)w / cols, ch = (double)h / rows, sigma = 0.3 * Math.Max(w, h);
            double ax = dotX * w, ay = dotY * h;
            var sb = new StringBuilder(Open(w, h, "field"));
            sb.Append("<g class=\"art-dim\" stroke-width=\"1.5\" stroke-linecap=\"square\" fill=\"none\">");
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double x = c * cw + cw / 2, y = r * ch + ch / 2, dx = x - ax, dy = y - ay, d = Math.Sqrt(dx * dx + dy * dy);
                    if (d < 14) continue;
                    double wgt = Math.Exp(-(d * d) / (2 * sigma * sigma));
                    double circ = Math.Atan2(dy, dx) + Math.PI / 2;
                    double vx = (1 - wgt) + wgt * Math.Cos(circ), vy = wgt * Math.Sin(circ);
                    double a = Math.Atan2(vy, vx), hx = Math.Cos(a) * len / 2, hy = Math.Sin(a) * len / 2;
                    sb.Append($"<path d=\"M{F(x - hx)} {F(y - hy)}L{F(x + hx)} {F(y + hy)}\"/>");
                }
            }
            sb.Append("</g>");
            sb.Append($"<rect class=\"art-sig\" x=\"{F(ax - 6)}\" y=\"{F(ay - 6)}\" width=\"12\" height=\"12\"/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        // Bar rhythm: rows of short vertical bars in the dim tone, and in each row
        // a lit run whose start walks right as the rows go down, so the lit runs
        // read as one rising band. One bar is the signal.
        private static string Bars(int w, int h, int seed)
        {
            const double pitch = 12, barW = 3, rowH = 26, barH = 14;
            int cols = (int)(w / pitch), rows = Math.Max(1, (int)(h / rowH));
            var rnd = new Random(seed * 7919);
            var sb = new StringBuilder(Open(w, h, "bars"));
            var dim = new StringBuilder();
            var hi = new StringBuilder();
            int sigRow = rows / 2, sigCol = -1;
            for (int r = 0; r < rows; r++)
            {
                double y = r * rowH + (rowH - barH) / 2;
                int start = (int)(cols * (0.08 + 0.72 * (rows - 1 - r) / Math.Max(1, rows - 1))) + rnd.Next(-4, 5);
                int run = 10 + rnd.Next(0, 18);
                if (r == sigRow) sigCol = Math.Min(cols - 1, Math.Max(0, start + run / 2));
                for (int c = 0; c < cols; c++)
                {
                    double x = c * pitch + (pitch - barW) / 2;
                    var lit = c >= start && c < start + run;
                    if (r == sigRow && c == sigCol) continue;
                    (lit ? hi : dim).Append($"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(barW)}\" height=\"{F(barH)}\"/>");
                }
            }
            sb.Append("<g class=\"art-dim\">").Append(dim).Append("</g>");
            sb.Append("<g class=\"art-hi\">").Append(hi).Append("</g>");
            if (sigCol >= 0)
            {
                sb.Append($"<rect class=\"art-sig\" x=\"{F(sigCol * pitch + (pitch - barW) / 2)}\" y=\"{F(sigRow * rowH + (rowH - barH) / 2)}\" width=\"{F(barW)}\" height=\"{F(barH)}\"/>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        // The square field: squares rotated by 45° times their nearness to the
        // signal point; the square nearest it is the signal and stays level.
        private static string Squares(int w, int h, double dotX, double dotY)
        {
            const double pitch = 40, side = 18;
            int cols = (int)Math.Round(w / pitch), rows = Math.Max(1, (int)Math.Round(h / pitch));
            double cw = (double)w / cols, ch = (double)h / rows, sigma = 0.3 * Math.Max(w, h);
            double ax = dotX * w, ay = dotY * h;
            int bestR = 0, bestC = 0; double best = double.MaxValue;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    double x = c * cw + cw / 2, y = r * ch + ch / 2, d = (x - ax) * (x - ax) + (y - ay) * (y - ay);
                    if (d < best) { best = d; bestR = r; bestC = c; }
                }
            var sb = new StringBuilder(Open(w, h, "squares"));
            sb.Append("<g class=\"art-hi\">");
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (r == bestR && c == bestC) continue;
                    double x = c * cw + cw / 2, y = r * ch + ch / 2, dx = x - ax, dy = y - ay;
                    double wgt = Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma));
                    double deg = 45 * wgt, s = side * (0.55 + 0.45 * wgt);
                    sb.Append($"<rect x=\"{F(x - s / 2)}\" y=\"{F(y - s / 2)}\" width=\"{F(s)}\" height=\"{F(s)}\" transform=\"rotate({F(deg)} {F(x)} {F(y)})\" opacity=\"{F(0.35 + 0.65 * wgt)}\"/>");
                }
            }
            sb.Append("</g>");
            double sx = bestC * cw + cw / 2, sy = bestR * ch + ch / 2;
            sb.Append($"<rect class=\"art-sig\" x=\"{F(sx - side / 2)}\" y=\"{F(sy - side / 2)}\" width=\"{F(side)}\" height=\"{F(side)}\"/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        // A 4 by 4 pixel glyph: 20-unit squares in 24-unit cells, one cell marked.
        private static string Glyph(string glyph)
        {
            var cells = ResolveGlyph(glyph);
            var sb = new StringBuilder(Open(96, 96, "glyph", slice: false));
            for (int n = 0; n < 16; n++)
            {
                var ch = cells[n];
                if (ch == '.') continue;
                var cls = ch == 'o' ? "art-sig" : "art-ink";
                sb.Append($"<rect class=\"{cls}\" x=\"{(n % 4) * 24 + 2}\" y=\"{(n / 4) * 24 + 2}\" width=\"20\" height=\"20\"/>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        // A diagonal band of short bars climbing from bottom left to top right,
        // the section divider's staircase.
        private static string Stair(int w, int h)
        {
            const double pitch = 12, barW = 3, rowH = 22, barH = 12;
            int cols = (int)(w / pitch), rows = Math.Max(1, (int)(h / rowH));
            var sb = new StringBuilder(Open(w, h, "stair"));
            sb.Append("<g class=\"art-hi\">");
            for (int r = 0; r < rows; r++)
            {
                double t = (double)(rows - 1 - r) / Math.Max(1, rows - 1);
                int start = (int)(cols * (0.1 + 0.55 * t)), run = Math.Max(6, cols / 6);
                double y = r * rowH + (rowH - barH) / 2;
                for (int c = start; c < Math.Min(cols, start + run); c++)
                {
                    sb.Append($"<rect x=\"{F(c * pitch + (pitch - barW) / 2)}\" y=\"{F(y)}\" width=\"{F(barW)}\" height=\"{F(barH)}\"/>");
                }
            }
            sb.Append("</g></svg>");
            return sb.ToString();
        }

        /// <summary>A named glyph, a 16-cell pattern, or <c>curious</c> when neither.</summary>
        public static string ResolveGlyph(string glyph)
        {
            if (!string.IsNullOrWhiteSpace(glyph))
            {
                if (Glyphs.TryGetValue(glyph.Trim(), out var named)) return named;
                var raw = glyph.Trim();
                if (raw.Length == 16 && raw.All(c => c == 'k' || c == 'o' || c == '.') && raw.Count(c => c == 'o') <= 1) return raw;
            }
            return Glyphs["curious"];
        }
    }
}
