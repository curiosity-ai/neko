using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// The alignment sheet of a video: every scene, every animation and every sentence of
    /// the voice-over on the one clock of the video, in time order. It is what the
    /// timing panel of the preview draws and what <c>neko voiceover --preview</c> prints,
    /// so the narration can be lined up with the animation before anything is rendered.
    /// </summary>
    public static class VideoAlignment
    {
        public enum Lane { Scene, Animation, Voice }

        /// <summary>One thing that happens at a time: a scene coming in, an element entering, a sentence being spoken.</summary>
        public sealed record Row(double Time, double End, Lane Lane, int Scene, string Text, string Detail, bool Estimated = false, bool Shifted = false);

        public static List<Row> Build(ParsedDocument document)
        {
            var rows = new List<Row>();
            var scenes = document.Scenes ?? new List<VideoScene>();
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var transition = i == 0 ? "start" : scene.Transition + (scene.TransitionDuration > 0 ? " " + scene.TransitionDuration.ToString("0.##", CultureInfo.InvariantCulture) + "s" : string.Empty);
                var name = scene.Get("id") ?? $"scene-{i + 1}";
                rows.Add(new Row(scene.Start, scene.End, Lane.Scene, i + 1, $"{name} · {scene.Get("layout", "default")}", transition));
                foreach (var e in VideoTimeline.Events(scene.Html))
                {
                    var label = string.IsNullOrEmpty(e.Text) ? "<" + e.Tag + ">" : e.Text;
                    var detail = e.Motion + (string.IsNullOrEmpty(e.Id) ? string.Empty : " #" + e.Id);
                    rows.Add(new Row(scene.Start + e.At, scene.Start + e.At + e.Duration, Lane.Animation, i + 1, label, detail));
                }
            }

            if (document.Narration != null)
            {
                foreach (var sentence in document.Narration.Sentences)
                {
                    var detail = $"line {sentence.Cue.Index + 1}" + (sentence.Cue.Sentences.Count > 1 ? $".{sentence.Cue.Sentences.IndexOf(sentence) + 1}" : string.Empty)
                        + (sentence.Cue.Sentences[0] == sentence && sentence.Cue.At != null ? $" @ {sentence.Cue.At}" : string.Empty)
                        + (sentence.Voice.Name != "narrator" ? " · " + sentence.Voice.Name : string.Empty);
                    var spill = SceneAt(scenes, sentence.End - 0.01);
                    var own = SceneAt(scenes, sentence.Start);
                    if (spill > own) detail += $" · runs into scene {spill}";
                    rows.Add(new Row(sentence.Start, sentence.End, Lane.Voice, own, sentence.Text, detail,
                        sentence.Estimated, sentence.Cue.Shifted && sentence.Cue.Sentences[0] == sentence));
                }
            }

            // Scenes first at a tie, then what moves, then what is said.
            return rows.OrderBy(r => Math.Round(r.Time, 3)).ThenBy(r => r.Lane).ToList();
        }

        /// <summary>The scene (1-based) on screen at a time: the latest one that has started.</summary>
        public static int SceneAt(IList<VideoScene> scenes, double time)
        {
            var index = 0;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].Start <= time + 1e-6) index = i + 1;
            }
            return Math.Max(1, index);
        }

        /// <summary>The sheet as text: a table on the video's clock, then the voice-over's warnings.</summary>
        public static string Format(ParsedDocument document, string name)
        {
            var rows = Build(document);
            var sb = new StringBuilder();
            var narration = document.Narration;
            var sentences = narration?.Sentences.Count() ?? 0;
            sb.Append($"{name}: {VideoNarration.Clock(document.VideoDuration)}, {document.Scenes?.Count ?? 0} scenes");
            if (narration != null)
            {
                sb.Append($", {narration.Cues.Count} voice-over lines in {sentences} sentences");
                if (narration.EstimatedCount > 0) sb.Append($" ({narration.EstimatedCount} estimated)");
            }
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("  time      end       sc  animation                                      voice-over");
            sb.AppendLine("  " + new string('─', 118));
            foreach (var row in rows)
            {
                var time = VideoNarration.Clock(row.Time).PadRight(9);
                var end = (row.Lane == Lane.Animation && row.End - row.Time < 0.05 ? string.Empty : VideoNarration.Clock(row.End)).PadRight(9);
                var scene = row.Scene.ToString("00", CultureInfo.InvariantCulture);
                switch (row.Lane)
                {
                    case Lane.Scene:
                        sb.AppendLine($"  {time} {end} {scene}  ▶ {Fit(row.Text + " (" + row.Detail + ")", 100)}");
                        break;
                    case Lane.Animation:
                        sb.AppendLine($"  {time} {end} {scene}    {Fit(row.Text, 30).PadRight(30)} {Fit(row.Detail, 14)}");
                        break;
                    default:
                        var mark = row.Estimated ? "~" : "●";
                        var shifted = row.Shifted ? " ⚠ shifted" : string.Empty;
                        sb.AppendLine($"  {time} {end} {scene}  {new string(' ', 47)}{mark} {Fit(row.Text, 60)}  [{row.Detail}{shifted}]");
                        break;
                }
            }
            if (narration != null)
            {
                sb.AppendLine();
                sb.AppendLine("  ● synthesized take   ~ estimated at " + narration.Options.Wpm.ToString("0", CultureInfo.InvariantCulture) + " wpm (run `neko voiceover` to synthesize)");
                foreach (var warning in narration.Warnings) sb.AppendLine("  ⚠ " + warning);
            }
            return sb.ToString();
        }

        /// <summary>The sheet as data, for <c>--json</c> and the preview page.</summary>
        public static object Json(ParsedDocument document)
        {
            var rows = Build(document);
            return new
            {
                duration = document.VideoDuration,
                scenes = (document.Scenes ?? new List<VideoScene>()).Select((s, i) => new
                {
                    index = i + 1,
                    id = s.Get("id") ?? $"scene-{i + 1}",
                    start = Math.Round(s.Start, 3),
                    end = Math.Round(s.End, 3),
                    settled = Math.Round(s.Start + s.TransitionDuration, 3),
                    transition = s.Transition,
                }),
                animation = rows.Where(r => r.Lane == Lane.Animation).Select(r => new
                {
                    time = Math.Round(r.Time, 3),
                    end = Math.Round(r.End, 3),
                    scene = r.Scene,
                    motion = r.Detail,
                    text = r.Text,
                }),
                voiceover = document.Narration == null ? null : document.Narration.Cues.Select(c => new
                {
                    line = c.Index + 1,
                    at = c.At,
                    requested = c.Requested,
                    shifted = c.Shifted,
                    start = Math.Round(c.Start, 3),
                    end = Math.Round(c.End, 3),
                    sentences = c.Sentences.Select(s => new
                    {
                        index = s.Index + 1,
                        start = s.Start,
                        end = Math.Round(s.End, 3),
                        duration = s.Duration,
                        estimated = s.Estimated,
                        scene = SceneAt(document.Scenes ?? new List<VideoScene>(), s.Start),
                        voice = s.Voice.CatalogId ?? s.Voice.Name,
                        text = s.Text,
                        audio = s.Estimated ? null : s.Url,
                    }),
                }),
                warnings = document.Narration?.Warnings ?? new List<string>(),
            };
        }

        private static string Fit(string text, int width)
        {
            text ??= string.Empty;
            return text.Length <= width ? text : text[..(width - 1)] + "…";
        }
    }
}
