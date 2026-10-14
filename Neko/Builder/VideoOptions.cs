using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Neko.Builder
{
    /// <summary>
    /// Video-level options, read from the <c>video:</c> front-matter key that marks a
    /// Markdown file as a video composition. Like <c>presentation:</c>, the key takes a
    /// bare <c>true</c> (all defaults) or a mapping of the options below.
    ///
    /// A video page is a timed sequence of scenes. In the browser it plays as a
    /// preview with a scrubber; <c>neko render</c> captures it frame by frame with
    /// headless Chromium and encodes an MP4 with ffmpeg (see <see cref="VideoRenderer"/>).
    /// </summary>
    public class VideoOptions
    {
        /// <summary>The themes Neko ships for video: a dark <c>neko</c> look and the Curiosity brand system.</summary>
        public static readonly string[] BuiltInThemes = { "neko", "curiosity" };

        /// <summary>The scene transitions the runtime knows. <c>cut</c> has no overlap.</summary>
        public static readonly string[] Transitions = { "cut", "fade", "fade-through", "push", "push-up", "zoom", "zoom-out", "wipe", "iris", "blur" };

        public string Theme { get; set; } = "neko";

        /// <summary>Canvas width in pixels. 1920 by default.</summary>
        public int Width { get; set; } = 1920;

        /// <summary>Canvas height in pixels. 1080 by default.</summary>
        public int Height { get; set; } = 1080;

        /// <summary>Frames per second of the render. The preview plays at the display rate.</summary>
        public int Fps { get; set; } = 30;

        /// <summary>The transition into every scene that does not name its own.</summary>
        public string Transition { get; set; }

        /// <summary>Seconds a transition overlaps the two scenes it joins.</summary>
        public double TransitionDuration { get; set; } = 0.6;

        /// <summary>
        /// Seconds a scene holds still after its last element has arrived, before the transition out begins.
        /// A scene whose <c>duration</c> is too short for that is lengthened, so an animation is never cut off as it lands.
        /// </summary>
        public double Hold { get; set; } = 1.6;

        /// <summary>Multiplies every automatic gap between elements: below 1 is brisker, above 1 calmer.</summary>
        public double Pace { get; set; } = 1.0;

        /// <summary>Accent of the <c>neko</c> theme: cyan, violet, amber, rose or leaf.</summary>
        public string Accent { get; set; } = "cyan";

        /// <summary>Eyebrow applied to scenes that don't declare one.</summary>
        public string Eyebrow { get; set; }

        /// <summary>Background music, mixed under the whole video and faded out at the end.</summary>
        public string Music { get; set; }

        /// <summary>Gain of the background music, 0 to 1. Quiet by default: it sits under voice.</summary>
        public double MusicVolume { get; set; } = 0.35;

        /// <summary>Brand mark pinned to the corner of every scene (image).</summary>
        public string Logo { get; set; }

        /// <summary>Text shown beside the logo in the brand mark.</summary>
        public string LogoText { get; set; }

        /// <summary>A film-grain overlay across the whole canvas.</summary>
        public bool Grain { get; set; }

        /// <summary>A soft vignette across the whole canvas.</summary>
        public bool Vignette { get; set; }

        /// <summary>The scene number and mark at the foot of each scene (the curiosity theme draws them).</summary>
        public bool Foot { get; set; } = true;

        /// <summary>Where the preview's back control goes.</summary>
        public string Back { get; set; }

        public string BackText { get; set; } = "Back";

        /// <summary>
        /// The Curiosity website's first-load intro over the opening scene: <c>squares</c> (the square field
        /// sweeping in on ink, turning toward the Signal and clearing) or <c>tiles</c>. None by default.
        /// </summary>
        public string Intro { get; set; }

        /// <summary>
        /// The voice-over's voice and pacing (<c>narration:</c>), or null when the front matter
        /// sets none. The lines themselves are written in <c>--- {narration}</c> parts of the
        /// file; see <see cref="VideoNarration"/>.
        /// </summary>
        public VideoNarrationOptions Narration { get; set; }

        /// <summary>Starts playing as soon as the preview is ready.</summary>
        public bool Autoplay { get; set; }

        /// <summary>Restarts the preview when it reaches the end.</summary>
        public bool Loop { get; set; }

        /// <summary>
        /// Set by <c>--theme</c> on the command line: every video in the build uses this
        /// theme when it is one of <see cref="BuiltInThemes"/>.
        /// </summary>
        public static string ThemeOverride => PresentationOptions.ThemeOverride;

        public static bool IsBuiltInTheme(string theme)
            => !string.IsNullOrWhiteSpace(theme) && BuiltInThemes.Contains(theme.Trim().ToLowerInvariant());

        /// <summary>The theme's own stylesheet, layered on <c>video.css</c>.</summary>
        public string ThemeStylesheet => string.Equals(Theme, "curiosity", StringComparison.OrdinalIgnoreCase) ? "video-curiosity.css" : null;

        /// <summary>The transition in effect when a scene names none: the curiosity theme cuts on the curve, neko crossfades.</summary>
        public string DefaultTransition => !string.IsNullOrEmpty(Transition)
            ? Transition
            : (string.Equals(Theme, "curiosity", StringComparison.OrdinalIgnoreCase) ? "push" : "fade");

        public bool HasBrand => !string.IsNullOrEmpty(Logo) || !string.IsNullOrEmpty(LogoText);

        public bool IsPortrait => Height > Width;

        /// <summary>
        /// Reads the <c>video:</c> front-matter node. Returns false when it is absent or
        /// explicitly disabled (<c>false</c>, <c>no</c>, <c>off</c>, <c>none</c>).
        /// </summary>
        public static bool TryParse(object node, out VideoOptions options)
        {
            if (!TryParseCore(node, out options)) return false;
            // `neko render --size/--fps` re-sizes the video for one render.
            if (!string.IsNullOrEmpty(SizeOverride)) options.ApplySize(SizeOverride);
            if (FpsOverride is > 0 and <= 120) options.Fps = FpsOverride.Value;
            return true;
        }

        /// <summary>Set by <c>neko render --size</c>: the canvas every video in the run uses.</summary>
        public static string SizeOverride { get; set; }

        /// <summary>Set by <c>neko render --fps</c>.</summary>
        public static int? FpsOverride { get; set; }

        private static bool TryParseCore(object node, out VideoOptions options)
        {
            options = null;
            if (node == null) return false;

            if (node is bool flag)
            {
                if (!flag) return false;
                options = new VideoOptions();
                return true;
            }

            if (node is string scalar)
            {
                var v = scalar.Trim();
                if (v.Length == 0 || IsFalsey(v)) return false;
                options = new VideoOptions();
                return true;
            }

            if (node is not System.Collections.IDictionary map) return false;

            options = new VideoOptions();
            foreach (System.Collections.DictionaryEntry entry in map)
            {
                var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture)?.Trim().ToLowerInvariant();
                var value = Convert.ToString(entry.Value, CultureInfo.InvariantCulture)?.Trim();
                if (string.IsNullOrEmpty(key)) continue;

                if (key is "narration" or "voiceover" or "voice-over")
                {
                    if (entry.Value is not string off || !IsFalsey(off)) options.Narration = VideoNarrationOptions.Parse(entry.Value);
                    continue;
                }

                switch (key)
                {
                    case "enabled":
                        if (IsFalsey(value)) { options = null; return false; }
                        break;
                    case "theme": if (!string.IsNullOrEmpty(value)) options.Theme = value.ToLowerInvariant(); break;
                    case "size": options.ApplySize(value); break;
                    case "ratio": case "aspect": options.ApplyRatio(value); break;
                    case "width": if (TryInt(value, out var w)) options.Width = w; break;
                    case "height": if (TryInt(value, out var h)) options.Height = h; break;
                    case "fps": if (TryInt(value, out var fps) && fps > 0 && fps <= 120) options.Fps = fps; break;
                    case "transition": if (!string.IsNullOrEmpty(value)) options.Transition = value.ToLowerInvariant(); break;
                    case "transitionduration": case "transition-duration":
                        if (TryDouble(value, out var td)) options.TransitionDuration = Math.Max(0, td);
                        break;
                    case "hold": if (TryDouble(value, out var hold)) options.Hold = Math.Max(0, hold); break;
                    case "pace": options.Pace = ParsePace(value, options.Pace); break;
                    case "accent": if (!string.IsNullOrEmpty(value)) options.Accent = value.ToLowerInvariant(); break;
                    case "eyebrow": options.Eyebrow = value; break;
                    case "music": options.Music = value; break;
                    case "musicvolume": case "music-volume": case "volume":
                        if (TryDouble(value, out var mv)) options.MusicVolume = Math.Clamp(mv, 0, 1);
                        break;
                    case "logo": options.Logo = value; break;
                    case "logotext": case "logo-text": options.LogoText = value; break;
                    case "grain": options.Grain = !IsFalsey(value); break;
                    case "vignette": options.Vignette = !IsFalsey(value); break;
                    case "foot": options.Foot = !IsFalsey(value); break;
                    case "back": options.Back = value; break;
                    case "backtext": case "back-text": options.BackText = value; break;
                    case "autoplay": options.Autoplay = !IsFalsey(value); break;
                    case "intro": options.Intro = IsFalsey(value) ? null : value; break;
                    case "loop": options.Loop = !IsFalsey(value); break;
                }
            }
            return true;
        }

        /// <summary><c>1920x1080</c>, or a named size: <c>1080p</c>, <c>720p</c>, <c>4k</c>, <c>vertical</c>, <c>square</c>.</summary>
        public void ApplySize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var v = value.Trim().ToLowerInvariant();
            switch (v)
            {
                case "1080p": Width = 1920; Height = 1080; return;
                case "720p": Width = 1280; Height = 720; return;
                case "4k": case "2160p": Width = 3840; Height = 2160; return;
                case "vertical": case "portrait": case "story": Width = 1080; Height = 1920; return;
                case "square": Width = 1080; Height = 1080; return;
            }
            var parts = v.Split('x', '×', '*');
            if (parts.Length == 2 && TryInt(parts[0], out var w) && TryInt(parts[1], out var h) && w >= 64 && h >= 64)
            {
                // Encoders want even dimensions for yuv420p.
                Width = w - w % 2;
                Height = h - h % 2;
            }
        }

        /// <summary><c>16:9</c>, <c>9:16</c>, <c>1:1</c>, <c>4:5</c>: the canvas keeps 1080 on its short side.</summary>
        public void ApplyRatio(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var parts = value.Trim().Split(':', '/');
            if (parts.Length != 2 || !TryDouble(parts[0], out var a) || !TryDouble(parts[1], out var b) || a <= 0 || b <= 0) return;
            if (a >= b)
            {
                Height = 1080;
                Width = (int)Math.Round(1080 * a / b / 2) * 2;
            }
            else
            {
                Width = 1080;
                Height = (int)Math.Round(1080 * b / a / 2) * 2;
            }
        }

        internal static double ParsePace(string value, double fallback)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "brisk": case "fast": return 0.75;
                case "normal": return 1.0;
                case "calm": case "slow": return 1.35;
            }
            return TryDouble(value, out var p) && p > 0 ? Math.Clamp(p, 0.25, 4) : fallback;
        }

        internal static bool TryInt(string value, out int result)
            => int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

        internal static bool TryDouble(string value, out double result)
        {
            var v = (value ?? string.Empty).Trim();
            if (v.EndsWith("s", StringComparison.OrdinalIgnoreCase) && !v.EndsWith("ms", StringComparison.OrdinalIgnoreCase)) v = v[..^1];
            if (v.EndsWith("ms", StringComparison.OrdinalIgnoreCase) && double.TryParse(v[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var ms))
            {
                result = ms / 1000.0;
                return true;
            }
            return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        internal static bool IsFalsey(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "false": case "no": case "off": case "none": case "0": return true;
                default: return false;
            }
        }
    }

    /// <summary>One scene of a video: the Markdown between two <c>---</c> separators, its attributes, and its place on the timeline.</summary>
    public class VideoScene
    {
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Markdown { get; set; } = string.Empty;

        /// <summary>The scene's HTML after the timeline pass: every animated element carries its own times.</summary>
        public string Html { get; set; }

        /// <summary>Seconds from the start of the video to the start of this scene (the start of its transition in).</summary>
        public double Start { get; set; }

        /// <summary>How long the scene is on screen, its transition in included.</summary>
        public double Duration { get; set; }

        public double End => Start + Duration;

        /// <summary>The transition into this scene, and how long it overlaps the scene before it.</summary>
        public string Transition { get; set; } = "cut";

        public double TransitionDuration { get; set; }

        /// <summary>The transition the scene asked for, before <see cref="VideoTimeline.Sequence"/> shortened it to fit two short scenes.</summary>
        public double RequestedTransition { get; set; }

        /// <summary>Scene-local second at which its last element has arrived (its entrance finished).</summary>
        public double ContentEnd { get; set; }

        /// <summary>Seconds the scene holds still after <see cref="ContentEnd"/>, before its transition out begins.</summary>
        public double Hold { get; set; }

        /// <summary>The author wrote a <c>duration</c>: Neko only lengthens it to keep content from being cut off.</summary>
        public bool HasExplicitDuration { get; set; }

        /// <summary>The scene's own audio (a voice-over line), resolved to a URL for the preview.</summary>
        public string Voice { get; set; }

        public string Get(string key, string fallback = null)
            => Attributes.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }
}
