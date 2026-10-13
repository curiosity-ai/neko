using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Neko.Builder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    /// <summary>
    /// The components of a video scene (<c>video:</c> front matter). They are ordinary
    /// <c>::: name</c> containers, claimed only inside a scene (see <see cref="VideoScope"/>).
    /// Each renders plain HTML with <c>nv-*</c> classes for the video stylesheets, and
    /// marks the parts that move with <c>data-v</c> (the motion) and
    /// <c>data-at="auto"</c>; <see cref="VideoTimeline.Compile"/> turns those into
    /// times, and <c>video.js</c> plays them.
    ///
    /// They are the Neko take on HyperFrames' registry: title lockups, kinetic word
    /// swaps, typewriters, count-ups, charts, code and terminals typing, browser and
    /// phone frames, an oversized cursor, lower thirds, quotes, captions, logo closes
    /// and calls to action — written as Markdown rather than as HTML and GSAP.
    /// </summary>
    internal static class VideoContainers
    {
        public static bool TryRender(HtmlRenderer renderer, CustomContainer obj)
        {
            switch (obj.Info)
            {
                case "lockup": case "title-card": Lockup(renderer, obj); return true;
                case "swap": Swap(renderer, obj); return true;
                case "typewriter": Wrap(renderer, obj, "nv-typewriter"); return true;
                case "scramble": Wrap(renderer, obj, "nv-scramble"); return true;
                case "counter": Counter(renderer, obj); return true;
                case "stats": WrapCols(renderer, obj, "nv-stats"); return true;
                case "stat": Stat(renderer, obj); return true;
                case "chart": Chart(renderer, obj); return true;
                case "code": Code(renderer, obj); return true;
                case "terminal": Terminal(renderer, obj); return true;
                case "browser": case "window": case "phone": case "device": Device(renderer, obj); return true;
                case "cursor": Cursor(renderer, obj); return true;
                case "lower-third": LowerThird(renderer, obj); return true;
                case "cta": Cta(renderer, obj); return true;
                case "logo": Logo(renderer, obj); return true;
                case "logos": Logos(renderer, obj); return true;
                case "quote": Quote(renderer, obj); return true;
                case "checklist": Wrap(renderer, obj, "nv-checklist"); return true;
                case "steps": Wrap(renderer, obj, "nv-steps"); return true;
                case "grid": WrapCols(renderer, obj, "nv-grid"); return true;
                case "card": Card(renderer, obj); return true;
                case "box": Card(renderer, obj, "nv-box"); return true;
                case "cols": WrapCols(renderer, obj, "nv-cols"); return true;
                case "col": Wrap(renderer, obj, "nv-col"); return true;
                case "stack": Wrap(renderer, obj, "nv-stack"); return true;
                case "captions": Captions(renderer, obj); return true;
                case "media": Media(renderer, obj); return true;
                case "reveal": Reveal(renderer, obj); return true;
                case "callout": Callout(renderer, obj); return true;
                case "tile": Tile(renderer, obj); return true;
                case "pixel-window": PixelWindow(renderer, obj); return true;
                case "glyph": Glyph(renderer, obj); return true;
                case "glyphs": Glyphs(renderer, obj); return true;
                case "sudo": Sudo(renderer, obj); return true;
                case "mark": Mark(renderer, obj); return true;
                case "wordmark": Wordmark(renderer, obj); return true;
                default: return false;
            }
        }

        // -----------------------------------------------------------------
        // helpers
        // -----------------------------------------------------------------

        private static string Attr(CustomContainer obj, string key, string fallback = null)
        {
            var props = obj.TryGetAttributes()?.Properties;
            if (props == null) return fallback;
            foreach (var p in props)
            {
                if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p.Value)) return p.Value;
            }
            return fallback;
        }

        private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string Num(double value) => VideoTimeline.Num(value);

        // The motion an author put on the container itself (`::: card {enter="pop" at="2"}`)
        // overrides the component's own; written as the runtime's attributes.
        private static string Motion(CustomContainer obj, string fallback, string extra = null)
        {
            var sb = new StringBuilder();
            sb.Append($" data-v=\"{Enc(Attr(obj, "enter", fallback))}\"");
            foreach (var (key, data) in new[] { ("at", "data-at"), ("dur", "data-dur"), ("ease", "data-ease"), ("exit", "data-exit"), ("until", "data-until"), ("delay", "data-delay"), ("gap", "data-gap") })
            {
                var value = Attr(obj, key);
                if (!string.IsNullOrEmpty(value)) sb.Append($" {data}=\"{Enc(value)}\"");
            }
            if (!string.IsNullOrEmpty(extra)) sb.Append(extra);
            return sb.ToString();
        }

        private static string ClassAttr(CustomContainer obj, string cls)
        {
            var extra = Attr(obj, "class");
            return string.IsNullOrEmpty(extra) ? cls : cls + " " + extra;
        }

        private static void Wrap(HtmlRenderer renderer, CustomContainer obj, string cls)
        {
            var enter = Attr(obj, "enter");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, cls))}\"");
            if (!string.IsNullOrEmpty(enter)) renderer.Write(Motion(obj, enter));
            renderer.Write(">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        private static void WrapCols(HtmlRenderer renderer, CustomContainer obj, string cls)
        {
            var count = Attr(obj, "count") ?? Attr(obj, "cols");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, cls))}\"");
            if (!string.IsNullOrEmpty(count)) renderer.Write($" style=\"--nv-cols:{Enc(count)}\"");
            renderer.Write(">");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        /// <summary>A display value split into its affixes and number: <c>$1,250</c>, <c>98.6%</c>, <c>3x</c>, <c>-12</c>.</summary>
        internal static (string Prefix, double Value, int Decimals, string Suffix, bool Grouped) ParseValue(string raw)
        {
            raw = (raw ?? string.Empty).Trim();
            var m = Regex.Match(raw, @"^(?<pre>[^\d\-+.]*)(?<num>[-+]?[\d,]*\.?\d+)(?<suf>.*)$");
            if (!m.Success) return (string.Empty, 0, 0, raw, false);
            var num = m.Groups["num"].Value;
            var grouped = num.Contains(',');
            var clean = num.Replace(",", string.Empty);
            var decimals = clean.Contains('.') ? clean.Length - clean.IndexOf('.') - 1 : 0;
            double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out var value);
            return (m.Groups["pre"].Value, value, decimals, m.Groups["suf"].Value, grouped);
        }

        private static string FormatValue(double value, int decimals, bool grouped)
            => value.ToString((grouped ? "#,0" : "0") + (decimals > 0 ? "." + new string('0', decimals) : string.Empty), CultureInfo.InvariantCulture);

        // The count-up span: the final value is in the markup (what a reader without the
        // runtime sees); the runtime counts from `from` to it.
        private static string CountSpan(double from, double to, int decimals, bool grouped, string extraAttributes = "")
            => $"<span class=\"nv-num\" data-v=\"count\" data-from=\"{Num(from)}\" data-to=\"{Num(to)}\" data-decimals=\"{decimals}\"" +
               (grouped ? " data-group=\"true\"" : string.Empty) + extraAttributes + $">{FormatValue(to, decimals, grouped)}</span>";

        // -----------------------------------------------------------------
        // Titles and text
        // -----------------------------------------------------------------

        // `::: lockup {kicker="…" label="…"}` — the title card: a mono kicker rises, the
        // headline settles word by word, a hairline rule draws under it, the label fades in.
        private static void Lockup(HtmlRenderer renderer, CustomContainer obj)
        {
            var kicker = Attr(obj, "kicker");
            var label = Attr(obj, "label");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-lockup"))}\">");
            if (!string.IsNullOrEmpty(kicker)) renderer.Write($"<p class=\"nv-kicker\" data-v=\"rise\">{Enc(kicker)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("<svg class=\"nv-rule\" viewBox=\"0 0 1000 4\" preserveAspectRatio=\"none\" aria-hidden=\"true\"><line x1=\"0\" y1=\"2\" x2=\"1000\" y2=\"2\" pathLength=\"1\" data-v=\"draw\" data-dur=\"0.6\"/></svg>");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"nv-label\" data-v=\"fade\">{Enc(label)}</p>");
            renderer.Write("</div>");
        }

        // `::: swap {prefix="Write" words="docs|decks|video" suffix="in Markdown." hold="0.9"}`
        // — a fixed line with one slot that rolls through the words and lands on the last.
        private static void Swap(HtmlRenderer renderer, CustomContainer obj)
        {
            var words = (Attr(obj, "words") ?? string.Empty).Split('|').Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
            if (words.Count == 0) words.Add("…");
            var hold = VideoOptions.TryDouble(Attr(obj, "hold"), out var h) && h > 0.2 ? h : 0.9;
            var tag = Attr(obj, "tag", "h2");
            if (tag is not ("h1" or "h2" or "h3" or "p")) tag = "h2";

            renderer.Write($"<{tag} class=\"{Enc(ClassAttr(obj, "nv-swap-line"))}\"{Motion(obj, "rise")}>");
            var prefix = Attr(obj, "prefix");
            if (!string.IsNullOrEmpty(prefix)) renderer.Write($"<span class=\"nv-swap-fix\">{Enc(prefix)}</span> ");
            // The slot reserves the width of the longest word, so the suffix never moves.
            var longest = words.OrderByDescending(w => w.Length).First();
            renderer.Write($"<span class=\"nv-swap\" data-v=\"swap\" data-at=\"+0.15\" data-hold=\"{Num(hold)}\" data-dur=\"{Num(hold * words.Count)}\" data-free=\"true\">");
            renderer.Write($"<span class=\"nv-swap-size\" aria-hidden=\"true\">{Enc(longest)}</span>");
            for (int i = 0; i < words.Count; i++)
            {
                renderer.Write($"<span class=\"nv-swap-word\"{(i == words.Count - 1 ? " data-last=\"true\"" : string.Empty)}>{Enc(words[i])}</span>");
            }
            renderer.Write("</span>");
            var suffix = Attr(obj, "suffix");
            // Punctuation closes up against the slot; a word gets its space.
            if (!string.IsNullOrEmpty(suffix))
            {
                renderer.Write(char.IsPunctuation(suffix[0]) ? string.Empty : " ");
                renderer.Write($"<span class=\"nv-swap-fix\">{Enc(suffix)}</span>");
            }
            renderer.Write($"</{tag}>");
            // The rest of the scene waits for the slot to land.
            renderer.Write($"<span class=\"nv-spacer\" data-v=\"none\" data-at=\"+{Num(hold * words.Count)}\"></span>");
        }

        // `::: quote {author="…" role="…"}` — the quote reads in word by word at reading
        // pace; the attribution settles after it.
        private static void Quote(HtmlRenderer renderer, CustomContainer obj)
        {
            var author = Attr(obj, "author");
            var role = Attr(obj, "role");
            renderer.Write($"<figure class=\"{Enc(ClassAttr(obj, "nv-quote"))}\">");
            renderer.Write("<blockquote>");
            renderer.WriteChildren(obj);
            renderer.Write("</blockquote>");
            if (!string.IsNullOrEmpty(author) || !string.IsNullOrEmpty(role))
            {
                renderer.Write("<figcaption data-v=\"rise\">");
                if (!string.IsNullOrEmpty(author)) renderer.Write($"<span class=\"nv-quote-author\">{Enc(author)}</span>");
                if (!string.IsNullOrEmpty(role)) renderer.Write($"<span class=\"nv-quote-role\">{Enc(role)}</span>");
                renderer.Write("</figcaption>");
            }
            renderer.Write("</figure>");
        }

        // `::: callout` — one line set large in the accent, slammed in.
        private static void Callout(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-callout"))}\"{Motion(obj, "slam")}>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: reveal {enter="pop" at="2.4"}` — no look of its own: moves whatever is inside as one.
        private static void Reveal(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-reveal"))}\"{Motion(obj, "rise")}>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: card {title="…" icon="…"}` / `::: box` — a panel that rises in; its own content follows.
        private static void Card(HtmlRenderer renderer, CustomContainer obj, string cls = "nv-card")
        {
            var title = Attr(obj, "title");
            var icon = Attr(obj, "icon");
            var label = Attr(obj, "label");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, cls))}\"{Motion(obj, "rise", " data-gap=\"0.14\"")}>");
            if (!string.IsNullOrEmpty(icon)) renderer.Write($"<i class=\"nv-card-icon fi fi-rr-{Enc(icon)}\" aria-hidden=\"true\"></i>");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"nv-card-label\">{Enc(label)}</p>");
            if (!string.IsNullOrEmpty(title)) renderer.Write($"<h3 class=\"nv-card-title\">{Enc(title)}</h3>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // -----------------------------------------------------------------
        // Numbers and data
        // -----------------------------------------------------------------

        // `::: counter {to="1250" from="0" prefix="$" suffix="+" label="…"}` — counts up,
        // lands exactly on the value and pulses once. `value="$1,250+"` is the short form.
        private static void Counter(HtmlRenderer renderer, CustomContainer obj)
        {
            var (prefix, to, decimals, suffix, grouped) = ResolveCounter(obj);
            var from = VideoOptions.TryDouble(Attr(obj, "from"), out var f) ? f : 0;
            var label = Attr(obj, "label");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-counter"))}\"{Motion(obj, "rise")}>");
            renderer.Write("<div class=\"nv-counter-value\">");
            if (!string.IsNullOrEmpty(prefix)) renderer.Write($"<span class=\"nv-affix\">{Enc(prefix)}</span>");
            renderer.Write(CountSpan(from, to, decimals, grouped, " data-at=\"with\"" + (Attr(obj, "count-dur") is { } cd ? $" data-dur=\"{Enc(cd)}\"" : string.Empty)));
            if (!string.IsNullOrEmpty(suffix)) renderer.Write($"<span class=\"nv-affix\">{Enc(suffix)}</span>");
            renderer.Write("</div>");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"nv-counter-label\" data-v=\"fade\" data-at=\"+0.3\">{Enc(label)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        private static (string, double, int, string, bool) ResolveCounter(CustomContainer obj)
        {
            var value = Attr(obj, "value");
            if (!string.IsNullOrEmpty(value)) return ParseValue(value);
            var parsed = ParseValue(Attr(obj, "to", "0"));
            return (Attr(obj, "prefix") ?? parsed.Prefix, parsed.Value,
                    VideoOptions.TryInt(Attr(obj, "decimals"), out var d) ? d : parsed.Decimals,
                    Attr(obj, "suffix") ?? parsed.Suffix, parsed.Grouped || Attr(obj, "group") == "true");
        }

        // `::: stat {value="98%" label="…"}` inside `::: stats` — a card whose number counts up.
        private static void Stat(HtmlRenderer renderer, CustomContainer obj)
        {
            var (prefix, to, decimals, suffix, grouped) = ParseValue(Attr(obj, "value", "0"));
            var label = Attr(obj, "label");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-stat"))}\"{Motion(obj, "rise", " data-gap=\"0.16\"")}>");
            renderer.Write("<div class=\"nv-stat-value\">");
            if (!string.IsNullOrEmpty(prefix)) renderer.Write($"<span class=\"nv-affix\">{Enc(prefix)}</span>");
            renderer.Write(CountSpan(0, to, decimals, grouped, " data-at=\"with\""));
            if (!string.IsNullOrEmpty(suffix)) renderer.Write($"<span class=\"nv-affix\">{Enc(suffix)}</span>");
            renderer.Write("</div>");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<p class=\"nv-stat-label\">{Enc(label)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        private sealed record Datum(string Label, double Value, string Display, bool Highlight);

        // Data rows from a bullet list (`- Label: 42`) or a two-column table. `*` or `!`
        // after the value (`- 2026: 88 *`) marks the one datum the chart calls out.
        private static List<Datum> ReadData(CustomContainer obj)
        {
            var data = new List<Datum>();
            void Add(string label, string raw)
            {
                raw = (raw ?? string.Empty).Trim();
                var highlight = raw.EndsWith("*") || raw.EndsWith("!");
                raw = raw.TrimEnd('*', '!', ' ');
                var (_, value, _, _, _) = ParseValue(raw);
                data.Add(new Datum(label.Trim(), value, raw, highlight));
            }

            foreach (var block in obj.Descendants())
            {
                if (block is ListItemBlock item)
                {
                    var paragraph = item.Descendants<ParagraphBlock>().FirstOrDefault();
                    var text = DocumentContainers.PlainText(paragraph?.Inline);
                    var colon = text.LastIndexOf(':');
                    if (colon > 0) Add(text[..colon], text[(colon + 1)..]);
                }
                else if (block is Markdig.Extensions.Tables.TableRow row && !row.IsHeader)
                {
                    var cells = row.OfType<Markdig.Extensions.Tables.TableCell>().ToList();
                    if (cells.Count >= 2)
                    {
                        Add(DocumentContainers.PlainText(cells[0].Descendants<ParagraphBlock>().FirstOrDefault()?.Inline),
                            DocumentContainers.PlainText(cells[1].Descendants<ParagraphBlock>().FirstOrDefault()?.Inline));
                    }
                }
            }
            return data;
        }

        // `::: chart {type="bar" title="…" unit="%"}` — bars grow in one after another
        // with their values counting up; `line` draws its line and pops its points;
        // `hbar` grows rows left to right; `donut` draws its segments round.
        private static void Chart(HtmlRenderer renderer, CustomContainer obj)
        {
            var data = ReadData(obj);
            var type = (Attr(obj, "type", "bar")).ToLowerInvariant();
            var title = Attr(obj, "title");
            var caption = Attr(obj, "caption");
            var unit = Attr(obj, "unit", string.Empty);
            var max = VideoOptions.TryDouble(Attr(obj, "max"), out var mx) && mx > 0 ? mx : (data.Count == 0 ? 1 : Math.Max(1e-9, data.Max(d => d.Value)));

            renderer.Write($"<figure class=\"{Enc(ClassAttr(obj, "nv-chart"))}\" data-type=\"{Enc(type)}\">");
            if (!string.IsNullOrEmpty(title)) renderer.Write($"<figcaption class=\"nv-chart-title\" data-v=\"rise\">{Enc(title)}</figcaption>");

            switch (type)
            {
                case "line": LineChart(renderer, data, max, unit); break;
                case "donut": case "pie": DonutChart(renderer, data, unit); break;
                case "hbar": case "rows": HBarChart(renderer, data, max, unit); break;
                default: BarChart(renderer, data, max, unit); break;
            }

            if (!string.IsNullOrEmpty(caption)) renderer.Write($"<p class=\"nv-chart-caption\" data-v=\"fade\">{Enc(caption)}</p>");
            renderer.Write("</figure>");
        }

        private static string ValueHtml(Datum d, string unit, string at = "with")
        {
            var (prefix, value, decimals, suffix, grouped) = ParseValue(d.Display);
            if (string.IsNullOrEmpty(suffix)) suffix = unit;
            return (string.IsNullOrEmpty(prefix) ? string.Empty : $"<span class=\"nv-affix\">{Enc(prefix)}</span>")
                + CountSpan(0, value, decimals, grouped, $" data-at=\"{at}\" data-dur=\"0.9\"")
                + (string.IsNullOrEmpty(suffix) ? string.Empty : $"<span class=\"nv-affix\">{Enc(suffix)}</span>");
        }

        private static void BarChart(HtmlRenderer renderer, List<Datum> data, double max, string unit)
        {
            renderer.Write($"<div class=\"nv-chart-plot\" style=\"--nv-n:{data.Count}\">");
            renderer.Write("<div class=\"nv-chart-axis\" data-v=\"wipe\" data-gap=\"0.2\"></div>");
            foreach (var d in data)
            {
                var pct = Math.Clamp(d.Value / max * 100, 0, 100);
                renderer.Write($"<div class=\"nv-bar-col{(d.Highlight ? " is-hi" : string.Empty)}\">");
                // The value rides beside the bar, not in it: a growing bar is scaled, and its label must not be.
                renderer.Write($"<div class=\"nv-bar-track\"><div class=\"nv-col-bar\" style=\"height:{Num(pct)}%\" data-v=\"grow\" data-gap=\"0.12\"></div>");
                renderer.Write($"<span class=\"nv-bar-value\" style=\"bottom:{Num(pct)}%\" data-v=\"rise\" data-at=\"with\">{ValueHtml(d, unit)}</span></div>");
                renderer.Write($"<div class=\"nv-bar-label\">{Enc(d.Label)}</div>");
                renderer.Write("</div>");
            }
            renderer.Write("</div>");
        }

        private static void HBarChart(HtmlRenderer renderer, List<Datum> data, double max, string unit)
        {
            renderer.Write("<div class=\"nv-hbars\">");
            foreach (var d in data)
            {
                var pct = Math.Clamp(d.Value / max * 100, 0, 100);
                renderer.Write($"<div class=\"nv-hbar-row{(d.Highlight ? " is-hi" : string.Empty)}\" data-v=\"fade\" data-gap=\"0.05\" data-dur=\"0.3\">");
                renderer.Write($"<span class=\"nv-hbar-label\">{Enc(d.Label)}</span>");
                renderer.Write($"<span class=\"nv-hbar-track\"><span class=\"nv-hbar\" style=\"width:{Num(pct)}%\" data-v=\"grow-x\" data-gap=\"0.12\"></span></span>");
                renderer.Write($"<span class=\"nv-hbar-value\">{ValueHtml(d, unit)}</span>");
                renderer.Write("</div>");
            }
            renderer.Write("</div>");
        }

        private static void LineChart(HtmlRenderer renderer, List<Datum> data, double max, string unit)
        {
            const double W = 1000, H = 420, padY = 30;
            var n = Math.Max(1, data.Count - 1);
            var points = data.Select((d, i) => (X: data.Count == 1 ? W / 2 : i * W / n, Y: padY + (H - 2 * padY) * (1 - Math.Clamp(d.Value / max, 0, 1)))).ToList();
            var line = string.Join(" ", points.Select((p, i) => (i == 0 ? "M" : "L") + Num(p.X) + " " + Num(p.Y)));
            var area = points.Count == 0 ? string.Empty : line + $" L{Num(points[^1].X)} {Num(H)} L{Num(points[0].X)} {Num(H)} Z";

            renderer.Write($"<div class=\"nv-line-plot\"><svg viewBox=\"-20 0 {Num(W + 40)} {Num(H + 10)}\" preserveAspectRatio=\"none\" aria-hidden=\"true\">");
            for (int g = 0; g <= 4; g++)
            {
                var y = padY + (H - 2 * padY) * g / 4.0;
                renderer.Write($"<line class=\"nv-grid-line\" x1=\"0\" x2=\"{Num(W)}\" y1=\"{Num(y)}\" y2=\"{Num(y)}\" pathLength=\"1\" data-v=\"draw\" data-dur=\"0.6\" data-gap=\"0.06\"/>");
            }
            renderer.Write($"<path class=\"nv-area\" d=\"{area}\" data-v=\"fade\" data-gap=\"0\"/>");
            renderer.Write($"<path class=\"nv-line\" d=\"{line}\" pathLength=\"1\" data-v=\"draw\" data-dur=\"1.4\" data-ease=\"power2.inOut\" data-gap=\"0.1\"/>");
            renderer.Write("</svg>");
            // Points and their values ride on top as HTML, so text stays crisp at any aspect.
            for (int i = 0; i < data.Count; i++)
            {
                var p = points[i];
                var left = (p.X + 20) / (W + 40) * 100;
                var top = p.Y / (H + 10) * 100;
                renderer.Write($"<span class=\"nv-point{(data[i].Highlight ? " is-hi" : string.Empty)}\" style=\"left:{Num(left)}%;top:{Num(top)}%\" data-v=\"pop\" data-gap=\"{Num(1.3 / Math.Max(1, data.Count))}\">");
                renderer.Write($"<span class=\"nv-point-value\">{ValueHtml(data[i], unit)}</span></span>");
            }
            renderer.Write("</div>");
            renderer.Write($"<div class=\"nv-line-labels\" style=\"--nv-n:{data.Count}\">");
            foreach (var d in data) renderer.Write($"<span>{Enc(d.Label)}</span>");
            renderer.Write("</div>");
        }

        private static void DonutChart(HtmlRenderer renderer, List<Datum> data, string unit)
        {
            var total = data.Sum(d => Math.Max(0, d.Value));
            if (total <= 0) total = 1;
            const double R = 150, C = 200;
            renderer.Write("<div class=\"nv-donut\"><svg viewBox=\"0 0 400 400\" aria-hidden=\"true\">");
            renderer.Write($"<circle class=\"nv-donut-track\" cx=\"{C}\" cy=\"{C}\" r=\"{R}\"/>");
            var angle = -90.0;
            for (int i = 0; i < data.Count; i++)
            {
                var sweep = Math.Max(0, data[i].Value) / total * 360;
                var a0 = angle + 1.2;
                var a1 = angle + sweep - 1.2;
                angle += sweep;
                if (a1 <= a0) continue;
                var x0 = C + R * Math.Cos(a0 * Math.PI / 180); var y0 = C + R * Math.Sin(a0 * Math.PI / 180);
                var x1 = C + R * Math.Cos(a1 * Math.PI / 180); var y1 = C + R * Math.Sin(a1 * Math.PI / 180);
                var large = a1 - a0 > 180 ? 1 : 0;
                renderer.Write($"<path class=\"nv-donut-seg\" data-i=\"{i}\" d=\"M{Num(x0)} {Num(y0)} A{R} {R} 0 {large} 1 {Num(x1)} {Num(y1)}\" pathLength=\"1\" data-v=\"draw\" data-dur=\"{Num(0.4 + sweep / 360 * 1.2)}\" data-gap=\"{Num(0.1 + sweep / 360 * 1.1)}\" data-ease=\"power1.inOut\"/>");
            }
            renderer.Write("</svg>");
            var hi = data.FirstOrDefault(d => d.Highlight) ?? data.FirstOrDefault();
            if (hi != null)
            {
                renderer.Write($"<div class=\"nv-donut-center\" data-v=\"pop\"><span class=\"nv-donut-value\">{ValueHtml(hi, unit, "+0.1")}</span><span class=\"nv-donut-label\">{Enc(hi.Label)}</span></div>");
            }
            renderer.Write("</div>");
            renderer.Write("<ul class=\"nv-legend\">");
            for (int i = 0; i < data.Count; i++)
            {
                renderer.Write($"<li data-i=\"{i}\" data-v=\"fade\" data-gap=\"0.08\"><i></i>{Enc(data[i].Label)} <b>{Enc(data[i].Display)}{Enc(ParseValue(data[i].Display).Suffix.Length == 0 ? unit : string.Empty)}</b></li>");
            }
            renderer.Write("</ul>");
        }

        // -----------------------------------------------------------------
        // Code and terminals
        // -----------------------------------------------------------------

        private static FencedCodeBlock FirstCode(CustomContainer obj) => obj.Descendants<FencedCodeBlock>().FirstOrDefault();

        private static string CodeText(FencedCodeBlock code)
        {
            var lines = new List<string>();
            for (int i = 0; i < code.Lines.Count; i++) lines.Add(code.Lines.Lines[i].Slice.ToString());
            return string.Join("\n", lines).TrimEnd('\n');
        }

        // `::: code {file="Program.cs" typing="true" speed="0.03"}` around a fenced block —
        // an editor window; with typing, the code types itself in behind a caret.
        private static void Code(HtmlRenderer renderer, CustomContainer obj)
        {
            var code = FirstCode(obj);
            if (code == null) return;
            var text = CodeText(code);
            var language = (code.Info ?? string.Empty).Split(' ', '{')[0].Trim();
            var file = Attr(obj, "file") ?? Attr(obj, "title");
            var typing = !VideoOptions.IsFalsey(Attr(obj, "typing", "true"));
            var speed = VideoOptions.TryDouble(Attr(obj, "speed"), out var s) && s > 0 ? s : 0.028;
            var visible = text.Count(c => !char.IsWhiteSpace(c));

            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-editor"))}\"{Motion(obj, "zoom", " data-gap=\"0.3\"")}>");
            renderer.Write("<div class=\"nv-editor-bar\"><i></i><i></i><i></i>");
            if (!string.IsNullOrEmpty(file)) renderer.Write($"<span class=\"nv-editor-file\">{Enc(file)}</span>");
            renderer.Write("</div>");
            renderer.Write("<pre class=\"nv-editor-code\"");
            if (typing) renderer.Write($" data-v=\"code\" data-dur=\"{Num(Math.Max(0.5, visible * speed))}\" data-gap=\"{Num(Math.Max(0.5, visible * speed) + 0.3)}\"");
            renderer.Write($"><code class=\"{(string.IsNullOrEmpty(language) ? "nohighlight" : "language-" + Enc(language))}\">{Enc(text)}</code></pre>");
            renderer.Write("</div>");
        }

        // `::: terminal {title="zsh"}` around a fenced block — lines starting `$ ` are
        // commands and type themselves in; the lines after a command are its output and
        // appear one by one once it is "run".
        private static void Terminal(HtmlRenderer renderer, CustomContainer obj)
        {
            var code = FirstCode(obj);
            var title = Attr(obj, "title", "Terminal");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-terminal"))}\"{Motion(obj, "zoom", " data-gap=\"0.35\"")}>");
            renderer.Write($"<div class=\"nv-editor-bar\"><i></i><i></i><i></i><span class=\"nv-editor-file\">{Enc(title)}</span></div>");
            renderer.Write("<div class=\"nv-terminal-body\">");
            if (code != null)
            {
                foreach (var line in CodeText(code).Split('\n'))
                {
                    if (line.StartsWith("$ ") || line.StartsWith("> "))
                    {
                        renderer.Write($"<p class=\"nv-cmd\"><span class=\"nv-prompt\">{Enc(line[..1])}</span> <span data-v=\"type\" data-gap=\"0.45\">{Enc(line[2..])}</span></p>");
                    }
                    else
                    {
                        var cls = line.TrimStart().StartsWith("✓") || line.TrimStart().StartsWith("√") ? "nv-out is-ok"
                            : line.TrimStart().StartsWith("✗") || line.TrimStart().StartsWith("!") ? "nv-out is-err" : "nv-out";
                        renderer.Write($"<p class=\"{cls}\" data-v=\"fade\" data-dur=\"0.15\" data-gap=\"0.16\">{(line.Length == 0 ? "&nbsp;" : Enc(line))}</p>");
                    }
                }
            }
            renderer.Write("</div></div>");
        }

        // -----------------------------------------------------------------
        // Frames, cursors, overlays
        // -----------------------------------------------------------------

        // `::: browser {url="…"}`, `::: window {title="…"}`, `::: phone` — a device that
        // settles in, with whatever the author writes (a screenshot, Markdown) as its screen.
        private static void Device(HtmlRenderer renderer, CustomContainer obj)
        {
            var chrome = obj.Info == "device" ? Attr(obj, "chrome", "browser") : obj.Info;
            var url = Attr(obj, "url");
            var title = Attr(obj, "title");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-device"))}\" data-chrome=\"{Enc(chrome)}\"{Motion(obj, "settle", " data-gap=\"0.35\"")}>");
            if (chrome != "phone")
            {
                renderer.Write("<div class=\"nv-device-bar\"><i></i><i></i><i></i>");
                if (chrome == "browser") renderer.Write($"<span class=\"nv-url\">{Enc(url ?? title ?? string.Empty)}</span>");
                else if (!string.IsNullOrEmpty(title)) renderer.Write($"<span class=\"nv-device-title\">{Enc(title)}</span>");
                renderer.Write("</div>");
            }
            else
            {
                renderer.Write("<div class=\"nv-notch\" aria-hidden=\"true\"></div>");
            }
            renderer.Write("<div class=\"nv-screen\">");
            renderer.WriteChildren(obj);
            renderer.Write("</div></div>");
        }

        // `::: cursor {from="105%,80%" to="62%,48%" click="true" label="…"}` — the oversized
        // pointer: enters from off-frame, travels to its target, taps (a press and a ring),
        // and the beat that follows lands on the click. `path` takes more than two points.
        private static void Cursor(HtmlRenderer renderer, CustomContainer obj)
        {
            var path = Attr(obj, "path");
            if (string.IsNullOrEmpty(path))
            {
                path = Attr(obj, "from", "108%,86%") + " " + Attr(obj, "to", "50%,50%");
            }
            var click = !VideoOptions.IsFalsey(Attr(obj, "click", "true"));
            var label = Attr(obj, "label");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-cursor"))}\"{Motion(obj, "cursor", $" data-path=\"{Enc(path)}\" data-click=\"{(click ? "true" : "false")}\"")}>");
            renderer.Write("<span class=\"nv-ripple\" aria-hidden=\"true\"></span>");
            renderer.Write("<svg class=\"nv-pointer\" viewBox=\"0 0 32 32\" aria-hidden=\"true\"><path d=\"M6 3 L6 25 L11.5 19.8 L15.4 28.6 L19.3 26.9 L15.5 18.3 L23 18.3 Z\"/></svg>");
            if (!string.IsNullOrEmpty(label)) renderer.Write($"<span class=\"nv-cursor-label\">{Enc(label)}</span>");
            renderer.Write("</div>");
        }

        // `::: lower-third {name="…" role="…"}` — the card wipes in, its accent tab grows,
        // the name and role rise.
        private static void LowerThird(HtmlRenderer renderer, CustomContainer obj)
        {
            var name = Attr(obj, "name") ?? Attr(obj, "title");
            var role = Attr(obj, "role");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-lt"))}\"{Motion(obj, "wipe", " data-gap=\"0.18\"")}>");
            renderer.Write("<span class=\"nv-lt-tab\" data-v=\"grow\" data-dur=\"0.45\" data-gap=\"0.06\"></span>");
            renderer.Write("<span class=\"nv-lt-body\">");
            if (!string.IsNullOrEmpty(name)) renderer.Write($"<span class=\"nv-lt-name\" data-v=\"rise\" data-gap=\"0.1\">{Enc(name)}</span>");
            if (!string.IsNullOrEmpty(role)) renderer.Write($"<span class=\"nv-lt-role\" data-v=\"rise\">{Enc(role)}</span>");
            renderer.Write("</span></div>");
        }

        // `::: cta {button="Get started" url="neko.curiosity.ai"}` — the action line lands
        // word by word, the button pops, the address settles; then a still hold.
        private static void Cta(HtmlRenderer renderer, CustomContainer obj)
        {
            var button = Attr(obj, "button");
            var url = Attr(obj, "url");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-cta"))}\">");
            renderer.WriteChildren(obj);
            if (!string.IsNullOrEmpty(button)) renderer.Write($"<span class=\"nv-cta-button\" data-v=\"pop\" data-gap=\"0.3\">{Enc(button)}</span>");
            if (!string.IsNullOrEmpty(url)) renderer.Write($"<p class=\"nv-cta-url\" data-v=\"fade\">{Enc(url)}</p>");
            renderer.Write("</div>");
        }

        // `::: logo {src="logo.svg" text="Neko" tagline="…"}` — the closing lockup: the mark
        // pops, the wordmark's letters cascade in, the tagline settles.
        private static void Logo(HtmlRenderer renderer, CustomContainer obj)
        {
            var src = Attr(obj, "src");
            var text = Attr(obj, "text");
            var tagline = Attr(obj, "tagline");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-logo"))}\">");
            renderer.Write("<div class=\"nv-logo-lockup\">");
            if (!string.IsNullOrEmpty(src)) renderer.Write($"<img class=\"nv-logo-mark\" src=\"{Enc(src)}\" alt=\"\" data-v=\"pop\" data-gap=\"0.2\">");
            else renderer.Write("<span class=\"nv-logo-glyph\" data-v=\"pop\" data-gap=\"0.2\" aria-hidden=\"true\"></span>");
            if (!string.IsNullOrEmpty(text)) renderer.Write($"<span class=\"nv-wordmark\" data-v=\"letters\">{Enc(text)}</span>");
            renderer.Write("</div>");
            if (!string.IsNullOrEmpty(tagline)) renderer.Write($"<p class=\"nv-tagline\" data-v=\"rise\">{Enc(tagline)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // `::: logos` around images — a "trusted by" row, each logo popping in turn.
        private static void Logos(HtmlRenderer renderer, CustomContainer obj)
        {
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-logos"))}\">");
            foreach (var image in obj.Descendants<LinkInline>().Where(l => l.IsImage))
            {
                var alt = DocumentContainers.PlainText(image.FirstChild);
                renderer.Write($"<img src=\"{Enc(image.Url)}\" alt=\"{Enc(alt)}\" data-v=\"pop\" data-gap=\"0.1\">");
            }
            renderer.Write("</div>");
        }

        // `::: media {src="clip.webm" start="2"}` — a video clip on the scene's clock. The
        // runtime owns playback: it seeks the clip to the scene time on every frame, so a
        // render is frame-exact. Muted: the soundtrack is mixed by `neko render`.
        private static void Media(HtmlRenderer renderer, CustomContainer obj)
        {
            var src = Attr(obj, "src");
            if (string.IsNullOrEmpty(src)) return;
            var start = VideoOptions.TryDouble(Attr(obj, "start"), out var st) ? st : 0;
            var fit = Attr(obj, "fit", "cover");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-media"))}\" data-fit=\"{Enc(fit)}\"{Motion(obj, "zoom")}>");
            renderer.Write($"<video src=\"{Enc(src)}\" muted playsinline preload=\"auto\" data-media-start=\"{Num(start)}\"></video>");
            renderer.Write("</div>");
        }

        // -----------------------------------------------------------------
        // The Curiosity brand's drawn parts
        // -----------------------------------------------------------------

        private static Dictionary<string, string> AttributeMap(CustomContainer obj)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var props = obj.TryGetAttributes()?.Properties;
            if (props != null) foreach (var p in props) map[p.Key] = p.Value;
            return map;
        }

        private static int _tileSeed;

        // `::: tile {art="mountain" scheme="ink" label="Rail & mobility"}` — a panel drawn live: a dash tile
        // (wave, rows, chart, chart-grid, mountain, band, dither), a story tile (step-grid, join-grid,
        // lattice, dot-chart), the field or the square field. Its content, if any, sits under the drawing as
        // its caption. The website's industry tiles and story tiles.
        private static void Tile(HtmlRenderer renderer, CustomContainer obj)
        {
            var map = AttributeMap(obj);
            var art = Attr(obj, "art", "mountain");
            var label = Attr(obj, "label");
            var height = Attr(obj, "height");
            var canvas = VideoArt.Canvas(art, map, "curiosity", "ink", null, ownGround: true, 0.62, 0.42, at: "with", seed: ++_tileSeed % 97);
            renderer.Write($"<figure class=\"{Enc(ClassAttr(obj, "nv-tile"))}\" data-art=\"{Enc(art)}\"{Motion(obj, "wipe-up", " data-gap=\"0.16\"")}>");
            renderer.Write($"<div class=\"nv-tile-art\"{(string.IsNullOrEmpty(height) ? string.Empty : $" style=\"height:{Enc(height)}\"")}>{canvas}</div>");
            if (!string.IsNullOrEmpty(label) || obj.Count > 0)
            {
                renderer.Write("<figcaption class=\"nv-tile-caption\">");
                if (!string.IsNullOrEmpty(label)) renderer.Write($"<span class=\"nv-tile-label\">{Enc(label)}</span>");
                renderer.WriteChildren(obj);
                renderer.Write("</figcaption>");
            }
            renderer.Write("</figure>");
        }

        // `::: pixel-window {art="mountain"}` — the pixel window (BRAND.md s7): the mark scaled up to a square, its
        // block a window onto a drawing (the security section's dash mountain by default), the escaped square
        // solid Signal one third its size, out on the diagonal.
        private static void PixelWindow(HtmlRenderer renderer, CustomContainer obj)
        {
            var map = AttributeMap(obj);
            var art = Attr(obj, "art", "mountain");
            if (!map.ContainsKey("scheme")) map["scheme"] = "ink";
            var canvas = VideoArt.Canvas(art, map, "curiosity", "ink", null, ownGround: true, 0.7, 0.75, at: "with", seed: 63);
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-window"))}\"{Motion(obj, "settle", " data-gap=\"0.3\"")}>");
            renderer.Write($"<div class=\"nv-window-pane\">{canvas}</div>");
            renderer.Write("<span class=\"nv-window-px\" data-v=\"escape\" data-at=\"+0.5\" data-dur=\"0.34\"></span>");
            renderer.Write("</div>");
        }

        // On the website a built glyph repeats its build while it is on screen; in a film that reads as a blink,
        // so a build plays once and holds unless the author asks for `loop`. A glyph's own loop still loops.
        private static string GlyphMarkup(CuriosityBrand.GlyphSpec spec, string size, string extra, string loop = null)
        {
            var loops = loop != null ? !VideoOptions.IsFalsey(loop) : spec.Loop && !spec.IsBuild;
            var frames = CuriosityBrand.Decode(spec.Animation);
            var encoded = string.Join(" ", frames.Select(f =>
                CuriosityBrand.Encode(f.Cells) + ":" + f.Ms.ToString(CultureInfo.InvariantCulture) + (f.Label == null ? string.Empty : ":" + f.Label.Replace(' ', '_'))));
            var total = frames.Sum(f => f.Ms) / 1000.0;
            var rest = frames.Last().Cells;
            var sb = new StringBuilder();
            sb.Append($"<span class=\"nv-glyph{(spec.ShowEmpty ? " is-grid" : string.Empty)}\" data-v=\"glyph\" data-frames=\"{Enc(encoded)}\" data-loop=\"{(loops ? "true" : "false")}\" data-dur=\"{Num(total)}\" data-gap=\"0.22\"");
            if (!string.IsNullOrEmpty(size)) sb.Append($" style=\"--nv-glyph:{Enc(size)}\"");
            sb.Append(extra).Append('>');
            string[] classes = { "", "g-k", "g-s", "g-a", "g-sig", "g-o", "g-d", "g-t" };
            foreach (var k in rest) sb.Append($"<i class=\"nv-gc {(k < classes.Length ? classes[k] : string.Empty)}\"></i>");
            sb.Append("</span>");
            return sb.ToString();
        }

        // `::: glyph {name="loop-search-permission-aware" size="160px" label="Permission aware"}` — one of the
        // website's 4 by 4 pixel glyphs (CuriosityBrand.Glyphs: values, use cases, capabilities, categories,
        // the three problem plays and the developer loops), playing its frames: a still glyph builds itself
        // in on the diagonal with its Signal last, a loop plays and holds its rest frame, a play runs once
        // with its captions. `name` also takes 16 cells in the notation (`k..*k...kkk.....`).
        private static void Glyph(HtmlRenderer renderer, CustomContainer obj)
        {
            var spec = CuriosityBrand.ResolveGlyph(Attr(obj, "name") ?? Attr(obj, "cells"));
            if (spec == null) return;
            var label = Attr(obj, "label");
            var hasCaption = !string.IsNullOrEmpty(label) || obj.Count > 0 || spec.Animation.Contains(':') && CuriosityBrand.Decode(spec.Animation).Any(f => f.Label != null);
            renderer.Write($"<figure class=\"{Enc(ClassAttr(obj, "nv-glyph-fig"))}\"{Motion(obj, "fade", " data-dur=\"0.3\" data-gap=\"0.05\"")}>");
            renderer.Write(GlyphMarkup(spec, Attr(obj, "size"), string.Empty, Attr(obj, "loop")));
            if (hasCaption)
            {
                renderer.Write("<figcaption>");
                renderer.Write($"<span class=\"nv-glyph-label\">{Enc(label ?? string.Empty)}</span>");
                renderer.WriteChildren(obj);
                renderer.Write("</figcaption>");
            }
            renderer.Write("</figure>");
        }

        // `::: glyphs {names="value-connected|value-resolved|value-traceable" labels="Connected|Resolved|Traceable"}`
        // — a row of glyphs as hairline cells, each building in on its own beat.
        private static void Glyphs(HtmlRenderer renderer, CustomContainer obj)
        {
            var names = (Attr(obj, "names") ?? string.Empty).Split('|').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            var labels = (Attr(obj, "labels") ?? string.Empty).Split('|').Select(n => n.Trim()).ToList();
            var size = Attr(obj, "size");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-glyphs"))}\" style=\"--nv-cols:{Math.Max(1, names.Count)}\">");
            for (int i = 0; i < names.Count; i++)
            {
                var spec = CuriosityBrand.ResolveGlyph(names[i]);
                if (spec == null) continue;
                renderer.Write("<figure class=\"nv-glyph-cell\" data-v=\"rise\" data-gap=\"0.05\">");
                renderer.Write(GlyphMarkup(spec, size, string.Empty, Attr(obj, "loop")));
                if (i < labels.Count && labels[i].Length > 0) renderer.Write($"<figcaption><span class=\"nv-glyph-label\">{Enc(labels[i])}</span></figcaption>");
                renderer.Write("</figure>");
            }
            renderer.Write("</div>");
        }

        // `::: sudo {clip="workIdle" size="72px"}` — Sudo, the house pixel cat, from the website's sprite sheet:
        // a clip (move, idle, sit, sitIdle, crouch, crouchIdle, sleep, sleepIdle, work, workIdle, stretch,
        // startle, jumpUp, jumpDown, interact) playing at its own rate, his ears in Signal. With `from` and
        // `to` (percent of the frame) he walks between them over `dur` seconds. A detail, never the subject:
        // the brand keeps him 36 to 72px; the website lets him walk the foot of the page.
        private static void Sudo(HtmlRenderer renderer, CustomContainer obj)
        {
            var name = Attr(obj, "clip", Attr(obj, "from") != null ? "move" : "sitIdle");
            if (!CuriosityBrand.SudoClips.TryGetValue(name, out var clip)) clip = CuriosityBrand.SudoClips["sitIdle"];
            var onInk = Attr(obj, "tone") is "ink" or "dark";
            var size = Attr(obj, "size", "72px");
            var from = Attr(obj, "from");
            var to = Attr(obj, "to");
            var walk = from != null && to != null && VideoOptions.TryDouble(from.TrimEnd('%'), out var f) && VideoOptions.TryDouble(to.TrimEnd('%'), out var t);
            var extra = new StringBuilder($" data-ms=\"{clip.Ms}\" data-loop=\"{(clip.Loops ? "true" : "false")}\" style=\"--nv-sudo:{Enc(size)}\"");
            if (walk) extra.Append($" data-from=\"{Enc(from.TrimEnd('%'))}\" data-to=\"{Enc(to.TrimEnd('%'))}\"");
            if (Attr(obj, "dur") == null) extra.Append($" data-dur=\"{Num(walk ? 3 : clip.Frames.Count * clip.Ms / 1000.0)}\"");
            renderer.Write($"<span class=\"{Enc(ClassAttr(obj, "nv-sudo" + (walk ? " is-walking" : string.Empty)))}\"{Motion(obj, "sudo", extra.ToString())} aria-hidden=\"true\">");
            for (int i = 0; i < clip.Frames.Count; i++)
            {
                renderer.Write(CuriosityBrand.SudoFrameSvg(clip.Frames[i], onInk, "nv-sudo-f"));
            }
            renderer.Write("</span>");
        }

        // `::: mark {cube="true" size="320px"}` — the Escape mark. It builds in square by square on the
        // diagonal and the escaped square steps out last; with `cube` it plays the website's click sequence
        // instead (js/logo-cube.js): the square tucks in, the block becomes a 3 by 3 by 3 cube, three layers
        // turn, and it lands flat with the square stepping back out.
        private static void Mark(HtmlRenderer renderer, CustomContainer obj)
        {
            var size = Attr(obj, "size", "240px");
            var cube = !VideoOptions.IsFalsey(Attr(obj, "cube", "false"));
            var onInk = Attr(obj, "tone") is "ink" or "paper" or "light";
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-brandmark"))}\" style=\"--nv-mark:{Enc(size)}\"{Motion(obj, "fade", " data-dur=\"0.2\" data-gap=\"0.05\"")}>");
            if (cube)
            {
                var color = onInk ? VideoArt.Paper : VideoArt.Ink;
                var inner = onInk ? VideoArt.Ink : VideoArt.Ink2;
                var loop = Attr(obj, "loop");
                var o = $"{{\"color\":\"{color}\",\"inner\":\"{inner}\",\"escaped\":\"{VideoArt.Signal}\"{(loop == null ? string.Empty : ",\"loop\":" + Num(VideoOptions.TryDouble(loop, out var l) ? l : 4))}}}";
                renderer.Write($"<canvas class=\"nv-paint nv-cube\" data-v=\"paint\" data-paint=\"cube\" data-at=\"+0.2\" data-o=\"{Enc(o)}\"></canvas>");
                renderer.Write("<span class=\"nv-spacer\" data-v=\"none\" data-at=\"+2.9\"></span>");
            }
            else
            {
                // The cells pop on the diagonal; the escaped square steps out of the block last.
                var svg = CuriosityBrand.MarkSvg();
                svg = System.Text.RegularExpressions.Regex.Replace(svg, "data-mark=\"(\\d)\"", m => m.Value + " data-v=\"pop\" data-dur=\"0.35\" data-gap=\"0.05\"");
                svg = svg.Replace("data-mark=\"escape\"", "data-mark=\"escape\" data-v=\"escape\" data-at=\"+0.25\" data-dur=\"0.34\"");
                renderer.Write(svg);
            }
            renderer.Write("</div>");
        }

        // `::: wordmark {text="curiosity" mark="true"}` — the wordmark with its pixel dots (the second one Signal
        // when it stands alone), its letters rising in one after another; with `mark` the Escape mark beside it
        // as the horizontal lockup.
        private static void Wordmark(HtmlRenderer renderer, CustomContainer obj)
        {
            var text = Attr(obj, "text", "curiosity");
            var withMark = !VideoOptions.IsFalsey(Attr(obj, "mark", "false"));
            var tagline = Attr(obj, "tagline");
            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-lockup-brand"))}\">");
            renderer.Write("<div class=\"nv-lockup-row\">");
            if (withMark)
            {
                var svg = CuriosityBrand.MarkSvg();
                svg = System.Text.RegularExpressions.Regex.Replace(svg, "data-mark=\"(\\d)\"", m => m.Value + " data-v=\"pop\" data-dur=\"0.3\" data-gap=\"0.04\"");
                svg = svg.Replace("data-mark=\"escape\"", "data-mark=\"escape\" data-v=\"escape\" data-dur=\"0.34\" data-gap=\"0.1\"");
                renderer.Write($"<span class=\"nv-lockup-mark\">{svg}</span>");
            }
            renderer.Write($"<span class=\"nv-wordmark-wrap\" data-v=\"wipe\" data-dur=\"0.8\">{CuriosityBrand.WordmarkHtml(text, signal: !withMark)}</span>");
            renderer.Write("</div>");
            if (!string.IsNullOrEmpty(tagline)) renderer.Write($"<p class=\"nv-tagline\" data-v=\"rise\">{Enc(tagline)}</p>");
            renderer.WriteChildren(obj);
            renderer.Write("</div>");
        }

        // -----------------------------------------------------------------
        // Captions
        // -----------------------------------------------------------------

        private static readonly Regex CaptionLine = new(@"^\s*(?<start>\d+(?:\.\d+)?)(?:\s*[-–]\s*(?<end>\d+(?:\.\d+)?))?\s+(?<text>.+)$", RegexOptions.Compiled);

        private static string InlineLines(ContainerInline inline)
        {
            var sb = new StringBuilder();
            void Walk(Inline node)
            {
                for (; node != null; node = node.NextSibling)
                {
                    switch (node)
                    {
                        case LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                        case CodeInline code: sb.Append(code.Content); break;
                        case LineBreakInline: sb.Append('\n'); break;
                        case ContainerInline container: Walk(container.FirstChild); break;
                    }
                }
            }
            Walk(inline?.FirstChild);
            return sb.ToString();
        }

        // `::: captions {style="pill"}` with one line per caption: `0.4 What the voice says`
        // or `0.4-2.9 What the voice says`. Each line shows on its own clock (scene time),
        // and its words light up as they are spoken — spread evenly over the line.
        private static void Captions(HtmlRenderer renderer, CustomContainer obj)
        {
            var style = Attr(obj, "style", "rail");
            var lines = new List<(double Start, double End, string Text)>();
            foreach (var paragraph in obj.Descendants<ParagraphBlock>())
            {
                // The paragraph's raw lines are gone once inlines are parsed: read them back
                // from the inlines, a soft line break being the end of a caption line.
                var raw = InlineLines(paragraph.Inline);
                foreach (var line in raw.Split('\n'))
                {
                    var m = CaptionLine.Match(line);
                    if (!m.Success) continue;
                    double.TryParse(m.Groups["start"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var start);
                    var end = m.Groups["end"].Success && double.TryParse(m.Groups["end"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var e) ? e : -1;
                    lines.Add((start, end, m.Groups["text"].Value.Trim()));
                }
            }

            renderer.Write($"<div class=\"{Enc(ClassAttr(obj, "nv-captions"))}\" data-style=\"{Enc(style)}\">");
            for (int i = 0; i < lines.Count; i++)
            {
                var (start, end, text) = lines[i];
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (end <= start)
                {
                    var natural = start + 0.4 + words.Length * 0.32;
                    end = i + 1 < lines.Count ? Math.Min(natural, lines[i + 1].Start - 0.05) : natural;
                }
                renderer.Write($"<p class=\"nv-cap\" data-v=\"cap\" data-at=\"{Num(start)}\" data-dur=\"0.18\" data-until=\"{Num(end)}\" data-free=\"true\">");
                var spoken = Math.Max(0.2, end - start - 0.2);
                for (int w = 0; w < words.Length; w++)
                {
                    var wAt = start + spoken * w / words.Length;
                    renderer.Write($"<span class=\"nv-cw\" data-v=\"karaoke\" data-at=\"{Num(wAt)}\" data-dur=\"{Num(spoken / words.Length)}\" data-free=\"true\">{Enc(words[w])}</span>");
                    if (w < words.Length - 1) renderer.Write(" ");
                }
                renderer.Write("</p>");
            }
            renderer.Write("</div>");
        }
    }
}
