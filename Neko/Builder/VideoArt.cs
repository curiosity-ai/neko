using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Neko.Builder
{
    /// <summary>
    /// The drawn art of a video: which canvas painter (<c>video-paint.js</c>) draws a kind, with which
    /// options and in which tones. The options are worked out here, from the theme, the scene's ground and the
    /// author's attributes, and written into the page as JSON; the painter only draws them.
    ///
    /// The kinds and their tones are the Curiosity website's (<c>js/field.js</c>, <c>js/decor.js</c>) and
    /// Tesserae's Curiosity components (<c>FlowField</c>, <c>DashMosaic</c>):
    /// <list type="bullet">
    ///   <item><c>field</c> — the live dash field, its signal pixel circling its place.</item>
    ///   <item><c>squares</c> — the square field, turning toward the Signal.</item>
    ///   <item><c>wave</c>, <c>rows</c>, <c>chart</c>, <c>chart-grid</c>, <c>mountain</c>, <c>band</c>,
    ///   <c>dither</c> — the dash tiles, on a scheme: <c>signal</c>, <c>ink</c>, <c>deep</c>, <c>stone</c>,
    ///   <c>paper</c>.</item>
    ///   <item><c>step-grid</c>, <c>join-grid</c>, <c>lattice</c>, <c>dot-chart</c> — the customer story tiles.</item>
    /// </list>
    /// </summary>
    public static class VideoArt
    {
        public const string Paper = "#F4F4F2", Stone = "#E6E8E8", Line = "#DEE0DF", Ink = "#111418", Slate = "#2A2F38",
            Ash = "#8A9099", Ink2 = "#C5C9CE", Signal = "#2F6BFF", Deep = "#0029E7";

        private static readonly Dictionary<string, string> MosaicShapes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["wave"] = "wave", ["rows"] = "rows", ["chart"] = "chart", ["chart-grid"] = "chartGrid",
            ["mountain"] = "mountain", ["band"] = "band", ["dither"] = "dither",
        };

        private static readonly Dictionary<string, string> StoryTiles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["step-grid"] = "stepGrid", ["join-grid"] = "joinGrid", ["lattice"] = "lattice", ["dot-chart"] = "dotChart",
        };

        /// <summary>True for a kind a canvas painter draws.</summary>
        public static bool IsPainted(string kind)
        {
            var k = (kind ?? string.Empty).Trim();
            return k is "field" or "squares" || MosaicShapes.ContainsKey(k) || StoryTiles.ContainsKey(k);
        }

        /// <summary>The scheme the website pairs with each dash-tile shape (decor.js TILES).</summary>
        public static string DefaultScheme(string shape) => (shape ?? string.Empty).ToLowerInvariant() switch
        {
            "wave" => "signal",
            "chart" or "chart-grid" => "stone",
            "rows" or "dither" => "deep",
            "band" => "paper",
            _ => "ink",
        };

        /// <summary>Background, dim, lit and accent bars of a dash-tile scheme (DashMosaic.Tones and decor.js).</summary>
        public static (string Bg, string Off, string On, string Acc) Scheme(string scheme, string shape) => (scheme ?? string.Empty).ToLowerInvariant() switch
        {
            "signal" => (Signal, Deep, Paper, Paper),
            "deep" => shape == "dither" ? (Deep, null, Signal, Paper) : (Deep, Signal, Paper, Paper),
            "stone" => (Stone, "#D6D9DA", Ink, Deep),
            "paper" => (Paper, Line, Ink, Signal),
            _ => (Ink, shape == "mountain" ? null : Slate, Paper, Signal),
        };

        /// <summary>
        /// The tones a scene's ground gives the art drawn straight on it: dashes slate on paper and ash on ink,
        /// the pixel Signal (paper on the deep blue, where Signal would not read).
        /// </summary>
        public static (string Dash, string InkTone, string Hot) OnGround(string theme, string ground, string accent)
        {
            var g = (ground ?? string.Empty).ToLowerInvariant();
            var curiosity = string.Equals(theme, "curiosity", StringComparison.OrdinalIgnoreCase);
            if (!curiosity)
            {
                var light = g is "paper" or "stone";
                return (light ? "#3A414C" : "#3A4250", light ? Ink : Paper, AccentHex(accent));
            }
            return g switch
            {
                "ink" or "slate" => (Ash, Paper, Signal),
                "deep" => ("rgba(244,244,242,.35)", Paper, Paper),
                _ => (Slate, Ink, Signal),
            };
        }

        public static string AccentHex(string accent) => (accent ?? string.Empty).ToLowerInvariant() switch
        {
            "violet" => "#A78BFA",
            "amber" => "#FBBF24",
            "rose" => "#FB7185",
            "leaf" => "#4ADE80",
            _ => "#22D3EE",
        };

        /// <summary>
        /// A canvas for a kind, with its painter and options, or null for a kind no painter draws.
        /// <paramref name="ownGround"/> gives the art a background of its own (a band, a tile); without it the
        /// art is drawn straight on the scene's ground.
        /// </summary>
        public static string Canvas(string kind, IReadOnlyDictionary<string, string> attributes, string theme, string ground, string accent,
            bool ownGround, double dotX = 0.5, double dotY = 0.5, double reveal = 1.4, string at = "0", int seed = 7)
        {
            string Get(string key) => attributes != null && attributes.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : null;
            var k = (kind ?? string.Empty).Trim().ToLowerInvariant();
            var o = new Dictionary<string, object>();
            string painter;

            if (k is "field" or "squares")
            {
                painter = k;
                var (dash, inkTone, hot) = OnGround(theme, ownGround ? "ink" : ground, accent);
                if (ownGround) o["bg"] = Ink;
                if (k == "field")
                {
                    o["dash"] = dash;
                    o["pitch"] = Num(Get("pitch"), 32);
                    if (Get("fade") is { } fade) o["fade"] = fade;
                }
                else
                {
                    o["ink"] = inkTone;
                    o["pitch"] = Num(Get("pitch"), 50);
                }
                o["hot"] = hot;
                o["dot"] = new[] { Num(Get("dot-x"), dotX), Num(Get("dot-y"), dotY) };
                o["drift"] = VideoOptions.IsFalsey(Get("drift")) ? 0 : Num(Get("drift"), 1);
                o["reveal"] = Num(Get("reveal"), reveal);
            }
            else if (MosaicShapes.TryGetValue(k, out var shape))
            {
                painter = "mosaic";
                var (bg, off, on, acc) = Scheme(Get("scheme") ?? DefaultScheme(k), shape);
                o["shape"] = shape;
                o["bg"] = bg;
                if (off != null) o["off"] = off;
                o["on"] = on;
                o["acc"] = acc;
                o["seed"] = seed;
                o["reveal"] = Num(Get("reveal"), 0.9);
                if (Get("bar-scale") is { } bs) o["scale"] = Num(bs, 1);
            }
            else if (StoryTiles.TryGetValue(k, out var tile))
            {
                painter = tile;
                o["seed"] = 80 + seed;
                switch (tile)
                {
                    case "stepGrid": o["bg"] = Paper; o["dot"] = Slate; o["a"] = Ink; o["b"] = Deep; break;
                    case "joinGrid": o["bg"] = Deep; o["rule"] = "rgba(244,244,242,.32)"; o["ink"] = Paper; o["hot"] = Signal; break;
                    case "lattice": o["bg"] = Stone; o["ink"] = Slate; o["hot"] = Signal; o["pitch"] = 32; o["arm"] = 0.3; o["lw"] = 1.5; break;
                    default: o["bg"] = Signal; o["ink"] = Paper; o["hot"] = Ink; break;
                }
            }
            else
            {
                return null;
            }

            var json = JsonSerializer.Serialize(o);
            return $"<canvas class=\"nv-paint\" data-v=\"paint\" data-paint=\"{painter}\" data-at=\"{WebUtility.HtmlEncode(at)}\" data-o=\"{WebUtility.HtmlEncode(json)}\"></canvas>";
        }

        private static double Num(string raw, double fallback)
            => VideoOptions.TryDouble(raw, out var v) ? v : fallback;
    }
}
