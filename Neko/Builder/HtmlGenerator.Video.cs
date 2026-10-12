using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Neko.Builder
{
    public partial class HtmlGenerator
    {
        /// <summary>
        /// Renders a video page: a standalone player of timed scenes on a fixed canvas
        /// (1920 by 1080 by default) scaled to the window, with a play button, a scrubber
        /// marked at each scene and none of the documentation shell. Like a deck, it is a
        /// single self-contained HTML file.
        ///
        /// The same page is what <c>neko render</c> films: with <c>#render</c> in the URL
        /// the chrome goes away, the canvas sits at 1:1, and headless Chromium drives it
        /// frame by frame through <c>window.nekoVideo.seek(t)</c> (see <c>video.js</c>).
        /// </summary>
        public string GenerateVideo(ParsedDocument document, string sourcePath = null)
        {
            var options = document.Video ?? new VideoOptions();
            var scenes = document.Scenes ?? new List<VideoScene>();

            var title = !string.IsNullOrEmpty(document.FrontMatter.Title) ? document.FrontMatter.Title : document.FrontMatter.Label;
            if (string.IsNullOrEmpty(title)) title = _config.Branding.Title;
            var description = !string.IsNullOrEmpty(document.FrontMatter.Description) ? document.FrontMatter.Description : _config.Meta.Description;

            var effectivePassword = ResolveEffectivePassword(document);
            var isProtected = !string.IsNullOrEmpty(effectivePassword);
            var headTitle = isProtected ? _config.Branding.Title : title;
            var headDescription = isProtected ? _config.Meta.Description : description;

            var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');

            var page = new StringBuilder();
            page.AppendLine($"<body class=\"neko-video-body\" data-accent=\"{EscapeHtmlAttr(options.Accent)}\"{(options.IsPortrait ? " data-portrait=\"true\"" : string.Empty)}>");
            RenderVideoBackButton(page, options, prefix);
            var chrome = InlineLocalMedia(page.ToString(), sourcePath);
            page.Clear().Append(chrome);

            var body = new StringBuilder();
            body.AppendLine("<div class=\"nv-viewport\" id=\"nv-viewport\">");
            body.AppendLine($"<div class=\"nv-stage\" id=\"nv-stage\" style=\"width:{options.Width}px;height:{options.Height}px\" role=\"img\" aria-label=\"{EscapeHtmlAttr(title)}\">");
            for (int i = 0; i < scenes.Count; i++)
            {
                RenderVideoScene(body, scenes[i], options, i, scenes.Count);
            }
            body.AppendLine("<div class=\"nv-wash\" aria-hidden=\"true\"></div>");
            if (options.Vignette) body.AppendLine("<div class=\"nv-vignette\" aria-hidden=\"true\"></div>");
            if (options.Grain) body.AppendLine("<div class=\"nv-grain\" aria-hidden=\"true\"></div>");
            RenderVideoBrand(body, options);
            body.AppendLine("</div>");
            body.AppendLine("</div>");

            RenderVideoAudio(body, document, options);
            RenderVideoBar(body, document, title);
            body.AppendLine("<script type=\"application/json\" id=\"nv-timeline\">" + TimelineJson(document, options, title) + "</script>");
            body.AppendLine("<script>(function r(n){ if (window.nekoVideoInit) { window.nekoVideoInit(); } else if (n > 0) { setTimeout(function(){ r(n - 1); }, 50); } })(60);</script>");

            var bodyHtml = InlineLocalMedia(InlineContentCdnScripts(body.ToString()), sourcePath);

            if (isProtected)
            {
                page.AppendLine("<div class=\"nv-locked\">");
                RenderProtectedColumn(page, bodyHtml, effectivePassword, inlineScript: true);
                page.AppendLine("</div>");
            }
            else
            {
                page.Append(bodyHtml);
            }

            var scanHtml = page.ToString();
            RenderStandaloneRuntime(page, "video.js");
            if (_isWatchMode) RenderLiveReloadScript(page);
            page.AppendLine("</body>");

            var standalone = AnalyzeStandalonePage(scanHtml, isProtected ? bodyHtml : string.Empty, "video.css", options.ThemeStylesheet);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine($"<html lang=\"en\" class=\"neko-video-html\" data-video-theme=\"{EscapeHtmlAttr(options.Theme)}\">");
            sb.AppendLine("<head>");
            RenderStandaloneHeadMeta(sb, headTitle, headDescription, sourcePath);
            RenderHeadTailwindAndTheme(sb, StandaloneTailwindCss(standalone, "video.js", "password.js"));
            RenderHeadNekoConfig(sb);
            // Both themes set their type in faces Neko ships as TrueType (deckfonts/): the
            // render must not depend on what the machine running Chromium has installed.
            RenderStandaloneFonts(sb, standalone, "deckfonts/deck-fonts.css", catalog: false);
            RenderStandaloneIconsAndEmoji(sb, standalone);
            RenderStandaloneContentLibraries(sb, standalone);
            RenderStandaloneStylesheets(sb, "video.css", options.ThemeStylesheet);
            if (!string.IsNullOrEmpty(_headIncludes)) sb.AppendLine(_headIncludes);
            sb.AppendLine("</head>");
            sb.Append(page);
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private void RenderVideoScene(StringBuilder sb, VideoScene scene, VideoOptions options, int index, int count)
        {
            var id = scene.Get("id") ?? $"scene-{index + 1}";
            var classes = "nv-scene" + (scene.Get("class") is { } extra ? " " + extra : string.Empty);
            sb.Append($"<section class=\"{EscapeHtmlAttr(classes)}\" id=\"{EscapeHtmlAttr(id)}\"");
            sb.Append($" data-start=\"{VideoTimeline.Num(scene.Start)}\" data-duration=\"{VideoTimeline.Num(scene.Duration)}\"");
            sb.Append($" data-transition=\"{EscapeHtmlAttr(scene.Transition)}\" data-tdur=\"{VideoTimeline.Num(scene.TransitionDuration)}\"");
            sb.Append($" data-layout=\"{EscapeHtmlAttr(scene.Get("layout", "default"))}\"");
            foreach (var key in new[] { "ground", "align", "accent", "wash" })
            {
                var value = scene.Get(key);
                if (!string.IsNullOrEmpty(value)) sb.Append($" data-{key}=\"{EscapeHtmlAttr(value)}\"");
            }
            sb.AppendLine($" aria-label=\"Scene {index + 1} of {count}\">");
            sb.AppendLine(scene.Html ?? string.Empty);
            sb.AppendLine("</section>");
        }

        private void RenderVideoBrand(StringBuilder sb, VideoOptions options)
        {
            if (!options.HasBrand) return;
            sb.Append("<div class=\"nv-brand\" aria-hidden=\"true\">");
            if (!string.IsNullOrEmpty(options.Logo)) sb.Append($"<img src=\"{EscapeHtmlAttr(EncodeAssetUrl(options.Logo))}\" alt=\"\">");
            if (!string.IsNullOrEmpty(options.LogoText)) sb.Append($"<span>{EscapeHtmlText(options.LogoText)}</span>");
            sb.AppendLine("</div>");
        }

        // The soundtrack, for the preview: the music bed and each scene's voice line as
        // <audio> elements the runtime starts and stops with the playhead. A render does
        // not play them — `neko render` mixes the same files with ffmpeg.
        private void RenderVideoAudio(StringBuilder sb, ParsedDocument document, VideoOptions options)
        {
            if (!string.IsNullOrEmpty(options.Music))
            {
                sb.AppendLine($"<audio class=\"nv-audio\" data-kind=\"music\" data-start=\"0\" data-volume=\"{VideoTimeline.Num(options.MusicVolume)}\" preload=\"auto\" src=\"{EscapeHtmlAttr(EncodeAssetUrl(options.Music))}\"></audio>");
            }
            foreach (var scene in document.Scenes ?? new List<VideoScene>())
            {
                if (string.IsNullOrEmpty(scene.Voice)) continue;
                var offset = VideoOptions.TryDouble(scene.Get("voice-at"), out var at) ? at : scene.TransitionDuration;
                var volume = VideoOptions.TryDouble(scene.Get("voice-volume"), out var v) ? v : 1;
                sb.AppendLine($"<audio class=\"nv-audio\" data-kind=\"voice\" data-start=\"{VideoTimeline.Num(scene.Start + offset)}\" data-volume=\"{VideoTimeline.Num(volume)}\" preload=\"auto\" src=\"{EscapeHtmlAttr(EncodeAssetUrl(scene.Voice))}\"></audio>");
            }
        }

        private static void RenderVideoBar(StringBuilder sb, ParsedDocument document, string title)
        {
            var total = document.VideoDuration <= 0 ? 1 : document.VideoDuration;
            sb.AppendLine("<div class=\"nv-bar\" id=\"nv-bar\">");
            sb.AppendLine("  <button class=\"nv-play\" id=\"nv-play\" type=\"button\" aria-label=\"Play\"><i class=\"fi fi-rr-play\" aria-hidden=\"true\"></i></button>");
            sb.AppendLine("  <span class=\"nv-time\" id=\"nv-time\">0:00.0</span>");
            sb.AppendLine("  <div class=\"nv-scrub\" id=\"nv-scrub\">");
            sb.AppendLine("    <div class=\"nv-scrub-fill\" id=\"nv-scrub-fill\"></div>");
            foreach (var scene in (document.Scenes ?? new List<VideoScene>()).Skip(1))
            {
                sb.AppendLine($"    <span class=\"nv-tick\" style=\"left:{(scene.Start / total * 100).ToString("0.###", CultureInfo.InvariantCulture)}%\"></span>");
            }
            sb.AppendLine("    <input type=\"range\" id=\"nv-range\" min=\"0\" max=\"1000\" step=\"1\" value=\"0\" aria-label=\"Seek\">");
            sb.AppendLine("  </div>");
            sb.AppendLine($"  <span class=\"nv-total\">{FormatClock(total)}</span>");
            sb.AppendLine($"  <span class=\"nv-title\" title=\"Render this video to MP4 with: neko render\">{EscapeHtmlText(title)}</span>");
            sb.AppendLine("</div>");
        }

        private static string FormatClock(double seconds)
        {
            var m = (int)(seconds / 60);
            var s = seconds - m * 60;
            return m + ":" + s.ToString("00.0", CultureInfo.InvariantCulture);
        }

        private static string TimelineJson(ParsedDocument document, VideoOptions options, string title)
        {
            var scenes = document.Scenes ?? new List<VideoScene>();
            var json = JsonSerializer.Serialize(new
            {
                title,
                duration = document.VideoDuration,
                fps = options.Fps,
                width = options.Width,
                height = options.Height,
                theme = options.Theme,
                autoplay = options.Autoplay,
                loop = options.Loop,
                scenes = scenes.Select((s, i) => new
                {
                    id = s.Get("id") ?? $"scene-{i + 1}",
                    start = s.Start,
                    duration = s.Duration,
                    transition = s.Transition,
                    transitionDuration = s.TransitionDuration,
                }),
            });
            // Inside a <script> block, "</" must not appear verbatim.
            return json.Replace("</", "<\\/");
        }

        private void RenderVideoBackButton(StringBuilder sb, VideoOptions options, string prefix)
        {
            var href = !string.IsNullOrEmpty(options.Back) ? options.Back : (string.IsNullOrEmpty(prefix) ? "/" : prefix + "/");
            var text = string.IsNullOrEmpty(options.BackText) ? "Back" : options.BackText;
            sb.AppendLine($"<a class=\"nv-back\" id=\"nv-back\" href=\"{EscapeHtmlAttr(href)}\"{(string.IsNullOrEmpty(options.Back) ? string.Empty : " data-pinned=\"true\"")}>");
            sb.AppendLine("  <i class=\"fi fi-rr-angle-small-left\" aria-hidden=\"true\"></i>");
            sb.AppendLine($"  <span>{EscapeHtmlText(text)}</span>");
            sb.AppendLine("</a>");
        }
    }
}
