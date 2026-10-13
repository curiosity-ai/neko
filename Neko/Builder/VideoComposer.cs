using System;
using System.Net;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// Builds one scene of a video page around its rendered Markdown: the background
    /// art, the eyebrow, the camera rig the content sits in, and the scene foot — then
    /// hands the whole thing to <see cref="VideoTimeline.Compile"/> and sizes the scene.
    /// </summary>
    public static class VideoComposer
    {
        /// <summary>Background kinds that are not <see cref="DeckArt"/>: drawn by the theme's CSS.</summary>
        private static readonly string[] CssBackgrounds = { "grid", "glow", "aurora", "dots" };

        public static void Compose(VideoScene scene, string contentHtml, VideoOptions options, int index)
        {
            var (transition, transitionDuration) = VideoTimeline.ResolveTransition(scene, options, index);
            scene.Transition = transition;
            scene.TransitionDuration = transitionDuration;

            var layout = scene.Get("layout") ?? InferLayout(contentHtml);
            scene.Attributes["layout"] = layout;

            var pace = VideoOptions.ParsePace(scene.Get("pace"), options.Pace);

            var sb = new StringBuilder();

            // The background: generated art (field, bars, squares, stair, glyph) drawn in
            // from its signal point, or one of the theme's CSS grounds.
            var art = scene.Get("art") ?? scene.Get("background");
            if (!string.IsNullOrEmpty(art))
            {
                var kind = art.Trim().ToLowerInvariant();
                if (VideoArt.IsPainted(kind))
                {
                    // Drawn live on a canvas: the dash field circling its pixel, the square field,
                    // the dash tiles and the story tiles (VideoArt, video-paint.js).
                    var (_, _, dotX, dotY) = ArtBox(layout, kind, options);
                    // In a side panel the pixel sits near the middle, where a crop never hides it.
                    if (layout is not ("cover" or "closing" or "full" or "bleed")) (dotX, dotY) = (0.52, 0.45);
                    var ownGround = layout == "cover" && kind is "field" or "squares";
                    var canvas = VideoArt.Canvas(kind, scene.Attributes, options.Theme, scene.Get("ground"), scene.Get("accent", options.Accent),
                        ownGround, dotX, dotY, seed: index + 1);
                    sb.Append($"<div class=\"nv-art\" data-art=\"{Enc(kind)}\" data-painted=\"true\" aria-hidden=\"true\">{canvas}</div>");
                }
                else if (Array.IndexOf(CssBackgrounds, kind) >= 0)
                {
                    sb.Append($"<div class=\"nv-bg\" data-bg=\"{Enc(kind)}\" data-v=\"art\" aria-hidden=\"true\"></div>");
                }
                else
                {
                    var (w, h, dotX, dotY) = ArtBox(layout, kind, options);
                    var seed = VideoOptions.TryInt(scene.Get("seed"), out var s) && s != 0 ? s : index + 1;
                    var svg = DeckArt.Render(kind, w, h, seed, scene.Get("glyph"), dotX, dotY);
                    if (svg != null)
                    {
                        sb.Append($"<div class=\"nv-art\" data-art=\"{Enc(kind)}\" data-v=\"art\" data-ox=\"{VideoTimeline.Num(dotX * 100)}\" data-oy=\"{VideoTimeline.Num(dotY * 100)}\" aria-hidden=\"true\">{svg}</div>");
                    }
                }
            }

            // The website's first-load intro, over the opening scene: the square field sweeping in on ink
            // and clearing (or the tile sweep), the content arriving as it lifts.
            var intro = index == 0 ? (scene.Get("intro") ?? options.Intro) : scene.Get("intro");
            var introLength = 0.0;
            if (!string.IsNullOrEmpty(intro) && !VideoOptions.IsFalsey(intro))
            {
                var style = intro.Trim().ToLowerInvariant() == "tiles" ? "tiles" : "squares";
                sb.Append($"<div class=\"nv-intro\" aria-hidden=\"true\"><canvas class=\"nv-paint\" data-v=\"paint\" data-paint=\"intro\" data-at=\"0\" data-o='{{\"style\":\"{style}\"}}'></canvas></div>");
                introLength = style == "tiles" ? 1.4 : 1.68;
            }

            var camera = scene.Get("camera");
            sb.Append("<div class=\"nv-cam\"");
            if (!string.IsNullOrEmpty(camera)) sb.Append($" data-camera=\"{Enc(camera.Trim().ToLowerInvariant())}\"");
            sb.Append('>');

            var eyebrow = scene.Get("eyebrow", options.Eyebrow);
            if (!string.IsNullOrEmpty(eyebrow))
            {
                sb.Append($"<p class=\"nv-eyebrow\" data-v=\"fade\" data-gap=\"0.12\"><span aria-hidden=\"true\"></span>{Enc(eyebrow)}</p>");
            }

            sb.Append(contentHtml);
            sb.Append("</div>");

            if (options.Foot)
            {
                sb.Append($"<div class=\"nv-foot\" aria-hidden=\"true\"><span class=\"nv-mark\"></span><span class=\"nv-foot-num\">{(index + 1):00}</span></div>");
            }

            // The content starts as the transition in is half done: the cut lands mid-motion.
            var leadIn = (index == 0 ? 0.3 : 0.15 + transitionDuration * 0.5) + introLength * 0.7;
            var brand = string.Equals(options.Theme, "curiosity", StringComparison.OrdinalIgnoreCase);
            var compiled = VideoTimeline.Compile(sb.ToString(), leadIn, pace, brand);
            scene.Html = compiled.Html;

            var hold = VideoOptions.TryDouble(scene.Get("hold"), out var h2) ? Math.Max(0, h2) : options.Hold;
            var auto = Math.Max(2.5 + introLength, compiled.ContentEnd + hold);
            scene.Duration = VideoOptions.TryDouble(scene.Get("duration"), out var d) && d > 0 ? d : Math.Round(auto, 2);

            if (compiled.ContentEnd > scene.Duration + 0.01)
            {
                Console.WriteLine($"Warning: video scene {index + 1} lasts {scene.Duration:0.##}s but its content is still arriving until {compiled.ContentEnd:0.##}s; raise its duration or leave it out.");
            }
        }

        // A scene that opens on an <h1> is a title: centred, roomier.
        private static string InferLayout(string html)
        {
            var trimmed = (html ?? string.Empty).TrimStart();
            return trimmed.StartsWith("<h1", StringComparison.OrdinalIgnoreCase) ? "title" : "default";
        }

        // Where the art goes for each layout, in design pixels, and where its signal point sits.
        private static (int W, int H, double X, double Y) ArtBox(string layout, string kind, VideoOptions options)
        {
            if (kind == "glyph") return (96, 96, 0.5, 0.5);
            int w = options.Width, h = options.Height;
            return layout switch
            {
                "cover" => (w, (int)(h / 3.0), 0.72, 0.5),
                "closing" => (w, (int)(h * 0.42), 0.8, 0.55),
                "section" => (w, h, 0.72, 0.36),
                "number" => (w, h, 0.84, 0.24),
                "panel" or "section-panel" => ((int)(w * 0.4), h, 0.5, 0.42),
                _ => (w, h, 0.78, 0.3),
            };
        }

        private static string Enc(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
    }
}
