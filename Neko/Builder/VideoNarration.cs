using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Voxwerk.Voices;

namespace Neko.Builder
{
    /// <summary>
    /// The <c>narration:</c> key of a video's front matter: the voice the voice-over is
    /// spoken in, and how its lines are paced. A bare string is the voice; a mapping
    /// takes the options below.
    /// <code>
    /// video:
    ///   narration:
    ///     voice: "A warm, low female voice, calm and measured."   # or a catalog id: en-gb-rupert-vale
    ///     language: english
    ///     voices:
    ///       guest: "An elderly man with a slow, raspy, gentle voice."
    /// </code>
    /// </summary>
    public class VideoNarrationOptions
    {
        /// <summary>The Qwen3-TTS checkpoint that speaks the voice-over: voice design, so any description is a voice.</summary>
        public const string DefaultModel = "Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign";

        /// <summary>The checkpoint that speaks a catalog voice by cloning its reference sample.</summary>
        public const string DefaultCloneModel = "Qwen/Qwen3-TTS-12Hz-1.7B-Base";

        /// <summary>The voice used when the front matter names none.</summary>
        public const string DefaultVoice = "A warm, clear voice of a professional narrator in her thirties, calm and friendly, speaking at an even, unhurried pace.";

        /// <summary>A catalog id (<c>en-gb-rupert-vale</c>), a name from <see cref="Voices"/>, or a description of the voice.</summary>
        public string Voice { get; set; }

        /// <summary>Named voices a line can pick with <c>{voice="name"}</c>: each a catalog id or a description.</summary>
        public Dictionary<string, string> Voices { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Qwen3-TTS language: <c>auto</c>, <c>english</c>, <c>german</c>, … A catalog voice brings its own.</summary>
        public string Language { get; set; }

        /// <summary>Sampling seed: the same seed, voice and text always give the same take.</summary>
        public int Seed { get; set; } = 7;

        /// <summary>Seconds of silence between two lines that follow each other.</summary>
        public double Gap { get; set; } = 0.5;

        /// <summary>Seconds of silence between the sentences of one line.</summary>
        public double Pause { get; set; } = 0.25;

        /// <summary>When the first line starts, if it does not say.</summary>
        public double Start { get; set; } = 0.5;

        /// <summary>Gain of the voice-over, 0 to 2.</summary>
        public double Volume { get; set; } = 1.0;

        /// <summary>Words per minute assumed for a sentence that has not been synthesized yet.</summary>
        public double Wpm { get; set; } = 150;

        /// <summary>The TTS checkpoint (Hugging Face repo id or local folder).</summary>
        public string Model { get; set; } = DefaultModel;

        /// <summary>The checkpoint that clones catalog voices from their reference samples.</summary>
        public string CloneModel { get; set; } = DefaultCloneModel;

        /// <summary>
        /// Speak a catalog voice by cloning its reference sample, so every sentence is the same
        /// speaker. Off, the voice is designed from its description for each sentence, which
        /// drifts from one take to the next.
        /// </summary>
        public bool Clone { get; set; } = true;

        /// <summary>
        /// Lengthen a scene whose duration is left to Neko until every line anchored to it has been
        /// spoken, plus <see cref="Tail"/>. Off, scenes keep the length their content gives them and a
        /// long line runs on into the next scene.
        /// </summary>
        public bool Fit { get; set; } = true;

        /// <summary>Seconds a scene stays on screen after the last line anchored to it ends, before the transition out.</summary>
        public double Tail { get; set; } = 0.8;

        /// <summary>Where the synthesized sentences are kept, relative to the video page. <c>assets/voiceover/&lt;page&gt;</c> by default.</summary>
        public string Folder { get; set; }

        /// <summary>Reads the <c>narration:</c> node: a voice, or a mapping of options.</summary>
        public static VideoNarrationOptions Parse(object node)
        {
            var options = new VideoNarrationOptions();
            if (node is System.Collections.IDictionary map)
            {
                foreach (System.Collections.DictionaryEntry entry in map)
                {
                    var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture)?.Trim().ToLowerInvariant();
                    if (key == "voices" && entry.Value is System.Collections.IDictionary named)
                    {
                        foreach (System.Collections.DictionaryEntry voice in named)
                        {
                            var name = Convert.ToString(voice.Key, CultureInfo.InvariantCulture)?.Trim();
                            var value = Convert.ToString(voice.Value, CultureInfo.InvariantCulture)?.Trim();
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value)) options.Voices[name] = value;
                        }
                        continue;
                    }
                    options.Apply(key, Convert.ToString(entry.Value, CultureInfo.InvariantCulture)?.Trim());
                }
            }
            else
            {
                var voice = Convert.ToString(node, CultureInfo.InvariantCulture)?.Trim();
                if (!string.IsNullOrEmpty(voice) && !string.Equals(voice, "true", StringComparison.OrdinalIgnoreCase)) options.Voice = voice;
            }
            return options;
        }

        internal void Apply(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return;
            switch (key)
            {
                case "voice": case "narrator": Voice = value; break;
                case "language": case "lang": Language = value.ToLowerInvariant(); break;
                case "seed": if (VideoOptions.TryInt(value, out var seed)) Seed = seed; break;
                case "gap": if (VideoOptions.TryDouble(value, out var gap)) Gap = Math.Max(0, gap); break;
                case "pause": if (VideoOptions.TryDouble(value, out var pause)) Pause = Math.Max(0, pause); break;
                case "start": if (VideoOptions.TryDouble(value, out var start)) Start = Math.Max(0, start); break;
                case "volume": if (VideoOptions.TryDouble(value, out var volume)) Volume = Math.Clamp(volume, 0, 2); break;
                case "wpm": case "rate": if (VideoOptions.TryDouble(value, out var wpm) && wpm >= 40 && wpm <= 400) Wpm = wpm; break;
                case "model": if (value.Length > 0) Model = value; break;
                case "clonemodel": case "clone-model": if (value.Length > 0) CloneModel = value; break;
                case "clone": Clone = !(value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase)); break;
                case "fit": Fit = !VideoOptions.IsFalsey(value); break;
                case "tail": if (VideoOptions.TryDouble(value, out var tail)) Tail = Math.Max(0, tail); break;
                case "folder": case "cache": if (value.Length > 0) Folder = value; break;
            }
        }
    }

    /// <summary>
    /// A voice as the TTS model needs it: the description it is designed from, its language, and
    /// whether it is cloned from the catalog voice's reference sample instead.
    /// </summary>
    public sealed record NarrationVoice(string Description, string Language, string CatalogId, string Name, bool Cloned = false);

    /// <summary>One sentence of the voice-over: the unit that is synthesized, cached, timed and shown as a subtitle.</summary>
    public sealed class NarrationSentence
    {
        public int Index { get; set; }
        public NarrationCue Cue { get; set; }
        public string Text { get; set; }
        public NarrationVoice Voice { get; set; }
        public string Language { get; set; }
        public int Seed { get; set; }

        /// <summary>Hash of everything that changes the take: model, voice, language, seed and text.</summary>
        public string Key { get; set; }

        /// <summary>The synthesized take on disk (Ogg Opus), whether or not it exists yet.</summary>
        public string File { get; set; }

        /// <summary>The take's URL for the preview page.</summary>
        public string Url { get; set; }

        public double Start { get; set; }
        public double Duration { get; set; }
        public double End => Start + Duration;

        /// <summary>True while the sentence has not been synthesized: its duration is a words-per-minute estimate.</summary>
        public bool Estimated { get; set; }

        public double Volume { get; set; } = 1;
    }

    /// <summary>One line of the voice-over (a paragraph of the narration track): where it is anchored, and its sentences.</summary>
    public sealed class NarrationCue
    {
        public int Index { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string Text { get; set; }

        /// <summary>The anchor as written (<c>at="scene-3+0.4"</c>), or null to follow the line before.</summary>
        public string At { get; set; }

        /// <summary>Where the anchor asked the line to start, before the no-overlap rule; null when it follows the line before.</summary>
        public double? Requested { get; set; }

        public List<NarrationSentence> Sentences { get; } = new();
        public double Start => Sentences.Count > 0 ? Sentences[0].Start : 0;
        public double End => Sentences.Count > 0 ? Sentences[^1].End : 0;

        /// <summary>True when the line was pushed later than its anchor so it would not talk over the line before.</summary>
        public bool Shifted { get; set; }

        public string Get(string key, string fallback = null)
            => Attributes.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }

    /// <summary>
    /// The voice-over of a video: a track on the video's own clock, written apart from the
    /// scenes. The scenes' animations are timed by <see cref="VideoTimeline"/> on each
    /// scene's clock; the narration never moves them. Its lines are laid on the root
    /// timeline instead — each at an anchor (a time, a scene, an element) or right after
    /// the line before — and never overlap: a line whose anchor falls while the previous
    /// one is still speaking waits for it, and says so.
    ///
    /// Sentences are the unit of synthesis (<c>neko voiceover</c>, through Voxwerk's
    /// VoiceDesign model) and of timing: a sentence that has been synthesized is timed by
    /// its take, one that has not by an estimate, so the timeline can be previewed and
    /// aligned long before any audio exists.
    /// </summary>
    public sealed class VideoNarration
    {
        /// <summary>
        /// Whether the parser prints the voice-over's warnings as it builds a page. The
        /// commands that print the timing sheet turn it off: the sheet lists them.
        /// </summary>
        public static bool PrintWarnings { get; set; } = true;

        // Where the last Schedule's warnings start in Warnings, so the next one replaces them.
        private int _scheduleWarnings = -1;

        /// <summary>Closest two lines may get when a line is pushed back to avoid an overlap.</summary>
        public const double MinimumGap = 0.15;

        public VideoNarrationOptions Options { get; set; }
        public List<NarrationCue> Cues { get; } = new();
        public IEnumerable<NarrationSentence> Sentences => Cues.SelectMany(c => c.Sentences);
        public List<string> Warnings { get; } = new();

        /// <summary>The folder the takes live in, and its URL (both null when the page has no file on disk).</summary>
        public string Folder { get; set; }
        public string FolderUrl { get; set; }

        public double End => Cues.Count == 0 ? 0 : Cues.Max(c => c.End);
        public bool IsEmpty => Cues.All(c => c.Sentences.Count == 0);
        public int EstimatedCount => Sentences.Count(s => s.Estimated);

        /// <summary>The narration-track attribute that turns a <c>---</c> part into voice-over instead of a scene.</summary>
        public static bool IsNarrationPart(IReadOnlyDictionary<string, string> attributes)
            => attributes != null && (attributes.ContainsKey("narration") || attributes.ContainsKey("voiceover") || attributes.ContainsKey("voice-over"));

        // -----------------------------------------------------------------
        // reading the narration track
        // -----------------------------------------------------------------

        private static readonly Regex CommentRegex = new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex AttributeLineRegex = new(@"^\s*(\{[^{}]*\})\s*$", RegexOptions.Compiled);
        private static readonly Regex TrailingAttributesRegex = new(@"\s*(\{[^{}]*=[^{}]*\})\s*$", RegexOptions.Compiled);
        private static readonly Regex ListItemRegex = new(@"^\s*(?:[-*+]|\d+[.)])\s+", RegexOptions.Compiled);

        /// <summary>
        /// Reads one narration part (<c>--- {narration}</c>) into lines. Every paragraph or
        /// list item is a line; a <c>{at=… voice=…}</c> block at its end, or on its own line
        /// before it, sets its anchor and voice; headings label the script and are not spoken.
        /// </summary>
        public static List<NarrationCue> ReadCues(string markdown, IReadOnlyDictionary<string, string> partAttributes)
        {
            var cues = new List<NarrationCue>();
            if (string.IsNullOrWhiteSpace(markdown)) return cues;

            var text = CommentRegex.Replace(markdown.Replace("\r\n", "\n"), string.Empty);
            Dictionary<string, string> pending = null;
            var buffer = new List<string>();

            void Flush()
            {
                if (buffer.Count == 0) return;
                var raw = string.Join(" ", buffer).Trim();
                buffer.Clear();
                var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (pending != null) foreach (var kv in pending) attributes[kv.Key] = kv.Value;
                pending = null;
                var trailing = TrailingAttributesRegex.Match(raw);
                if (trailing.Success)
                {
                    foreach (var kv in PresentationParser.ParseAttributes(trailing.Groups[1].Value)) attributes[kv.Key] = kv.Value;
                    raw = raw.Substring(0, trailing.Index).Trim();
                }
                var spoken = CleanText(raw);
                if (spoken.Length == 0) return;
                cues.Add(new NarrationCue { Attributes = attributes, Text = spoken, At = attributes.TryGetValue("at", out var at) && !string.IsNullOrWhiteSpace(at) ? at.Trim() : null });
            }

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd();
                if (string.IsNullOrWhiteSpace(line)) { Flush(); continue; }
                if (line.TrimStart().StartsWith("#")) { Flush(); continue; }

                var attributeLine = AttributeLineRegex.Match(line);
                if (attributeLine.Success)
                {
                    Flush();
                    pending = PresentationParser.ParseAttributes(attributeLine.Groups[1].Value);
                    continue;
                }

                if (ListItemRegex.IsMatch(line))
                {
                    Flush();
                    line = ListItemRegex.Replace(line, string.Empty, 1);
                }
                buffer.Add(line.Trim());
            }
            Flush();

            // The part's own anchor places its first line; its voice and language are every line's default.
            if (cues.Count > 0 && cues[0].At == null && partAttributes != null && partAttributes.TryGetValue("at", out var partAt) && !string.IsNullOrWhiteSpace(partAt))
            {
                cues[0].At = partAt.Trim();
            }
            if (partAttributes != null)
            {
                foreach (var cue in cues)
                {
                    foreach (var key in new[] { "voice", "language", "seed", "pause", "volume" })
                    {
                        if (!cue.Attributes.ContainsKey(key) && partAttributes.TryGetValue(key, out var value)) cue.Attributes[key] = value;
                    }
                }
            }
            return cues;
        }

        private static readonly Regex LinkRegex = new(@"!?\[([^\]]*)\]\([^)]*\)", RegexOptions.Compiled);
        private static readonly Regex TagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex MarkRegex = new(@"(\*\*|__|==|~~|[*_`])", RegexOptions.Compiled);
        private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);

        /// <summary>What the voice says: the Markdown with its emphasis, links and tags taken out.</summary>
        public static string CleanText(string markdown)
        {
            var s = LinkRegex.Replace(markdown ?? string.Empty, "$1");
            s = TagRegex.Replace(s, " ");
            s = MarkRegex.Replace(s, string.Empty);
            s = System.Net.WebUtility.HtmlDecode(s);
            return SpaceRegex.Replace(s, " ").Trim();
        }

        private static readonly Regex SentenceEndRegex = new(@"(?<=[.!?…][""'”’)\]]*)\s+(?=\S)", RegexOptions.Compiled);
        private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            "mr.", "mrs.", "ms.", "dr.", "st.", "vs.", "e.g.", "i.e.", "etc.", "approx.", "no.", "fig.", "inc.", "ltd.", "jr.", "sr.",
        };

        /// <summary>Splits a line into sentences, keeping common abbreviations (<c>e.g.</c>, <c>Dr.</c>) inside theirs.</summary>
        public static List<string> SplitSentences(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            var start = 0;
            foreach (Match m in SentenceEndRegex.Matches(text))
            {
                var candidate = text.Substring(start, m.Index - start).Trim();
                var lastWord = candidate.Split(' ').LastOrDefault() ?? string.Empty;
                if (Abbreviations.Contains(lastWord) || Regex.IsMatch(lastWord, @"^[A-Z]\.$")) continue;
                result.Add(candidate);
                start = m.Index + m.Length;
            }
            var tail = text.Substring(start).Trim();
            if (tail.Length > 0) result.Add(tail);
            return result;
        }

        /// <summary>How long a sentence will take to say, before it has been synthesized.</summary>
        public static double Estimate(string sentence, double wpm)
        {
            var words = Regex.Matches(sentence ?? string.Empty, @"[\p{L}\p{N}]+(?:['’][\p{L}]+)?").Count;
            // Numbers are read out digit group by digit group: count them a little longer.
            var digits = Regex.Matches(sentence ?? string.Empty, @"\d").Count;
            var pauses = Regex.Matches(sentence ?? string.Empty, @"[,;:—–]").Count;
            var seconds = 0.2 + (words + digits * 0.3) * 60.0 / Math.Max(40, wpm) + pauses * 0.2;
            return Math.Round(seconds, 2);
        }

        // -----------------------------------------------------------------
        // voices
        // -----------------------------------------------------------------

        private static readonly Lazy<bool> Catalog = new(() =>
        {
            try
            {
                Voxwerk.Voices.English.EnglishVoices.Register();
                // The reference samples, so a catalog voice is cloned rather than re-designed per sentence.
                Voxwerk.Voices.English.African.EnglishAfricanVoices.Register();
                Voxwerk.Voices.English.American.Characters.EnglishAmericanCharactersVoices.Register();
                Voxwerk.Voices.English.American.Commercial.EnglishAmericanCommercialVoices.Register();
                Voxwerk.Voices.English.American.Conversational.Female.EnglishAmericanConversationalFemaleVoices.Register();
                Voxwerk.Voices.English.American.Conversational.Male.EnglishAmericanConversationalMaleVoices.Register();
                Voxwerk.Voices.English.American.Narration.Female.EnglishAmericanNarrationFemaleVoices.Register();
                Voxwerk.Voices.English.American.Narration.Male.EnglishAmericanNarrationMaleVoices.Register();
                Voxwerk.Voices.English.American.Regional.EnglishAmericanRegionalVoices.Register();
                Voxwerk.Voices.English.British.Narration.EnglishBritishNarrationVoices.Register();
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"Warning: the Voxwerk voice catalog could not be loaded: {ex.Message}"); return false; }
        });

        /// <summary>Every voice of the registered Voxwerk catalogs.</summary>
        public static IReadOnlyList<VoiceInfo> CatalogVoices
        {
            get { _ = Catalog.Value; return VoiceRegistry.All; }
        }

        private static readonly Regex VoiceIdRegex = new(@"^[a-z]{2}-[a-z]{2,8}-[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

        /// <summary>
        /// Resolves a voice as written — a name from <c>voices:</c>, a catalog id, or a
        /// description — to the description the VoiceDesign model speaks from.
        /// </summary>
        public static NarrationVoice ResolveVoice(string voice, VideoNarrationOptions options, ICollection<string> warnings = null)
        {
            var value = string.IsNullOrWhiteSpace(voice) ? options.Voice : voice.Trim();
            if (string.IsNullOrWhiteSpace(value)) return new NarrationVoice(VideoNarrationOptions.DefaultVoice, null, null, "narrator");

            var name = (string)null;
            if (options.Voices.TryGetValue(value, out var named))
            {
                name = value;
                value = named;
            }

            if (VoiceIdRegex.IsMatch(value))
            {
                _ = Catalog.Value;
                var info = VoiceRegistry.GetVoiceInfo(value);
                if (info != null)
                {
                    var prompt = !string.IsNullOrWhiteSpace(info.DesignPrompt) ? info.DesignPrompt : info.Description;
                    var clone = options.Clone && VoiceRegistry.HasSample(info.Id);
                    return new NarrationVoice(prompt, LanguageName(info.Language), info.Id, name ?? info.Name, clone);
                }
                warnings?.Add($"Voice '{value}' is not in the Voxwerk catalog (list it with `neko voices`); the default narrator speaks instead.");
                return new NarrationVoice(VideoNarrationOptions.DefaultVoice, null, null, name ?? value);
            }

            return new NarrationVoice(value, null, null, name ?? "narrator");
        }

        /// <summary>A catalog language code (<c>en</c>) as the name Qwen3-TTS takes (<c>english</c>).</summary>
        public static string LanguageName(string code) => (code ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "en" => "english",
            "zh" => "chinese",
            "de" => "german",
            "fr" => "french",
            "es" => "spanish",
            "it" => "italian",
            "pt" => "portuguese",
            "ru" => "russian",
            "ja" => "japanese",
            "ko" => "korean",
            "" => null,
            var other => other,
        };

        // -----------------------------------------------------------------
        // the root timeline
        // -----------------------------------------------------------------

        /// <summary>
        /// Builds the voice-over of a video from its narration parts and lays it on the
        /// root timeline the scenes already sit on.
        /// </summary>
        /// <param name="parts">The narration parts, in file order: their attributes and Markdown.</param>
        /// <param name="options">The <c>narration:</c> options.</param>
        /// <param name="scenes">The sequenced scenes (their starts and compiled HTML are anchors).</param>
        /// <param name="folder">Where the takes are kept, or null for a page with no file.</param>
        /// <param name="folderUrl">The folder's URL on the site.</param>
        public static VideoNarration Build(IEnumerable<(Dictionary<string, string> Attributes, string Markdown)> parts, VideoNarrationOptions options,
                                           IList<VideoScene> scenes, string folder, string folderUrl)
        {
            var narration = new VideoNarration { Options = options ?? new VideoNarrationOptions(), Folder = folder, FolderUrl = folderUrl };
            options = narration.Options;
            foreach (var part in parts)
            {
                foreach (var cue in ReadCues(part.Markdown, part.Attributes))
                {
                    cue.Index = narration.Cues.Count;
                    narration.Cues.Add(cue);
                }
            }

            var voiceWarnings = new HashSet<string>();
            var index = 0;
            foreach (var cue in narration.Cues)
            {
                var voice = ResolveVoice(cue.Get("voice"), options, voiceWarnings);
                var language = cue.Get("language")?.ToLowerInvariant() ?? options.Language ?? voice.Language ?? "auto";
                var seed = VideoOptions.TryInt(cue.Get("seed"), out var s) ? s : options.Seed;
                var volume = VideoOptions.TryDouble(cue.Get("volume"), out var v) ? Math.Clamp(v, 0, 2) : options.Volume;
                foreach (var text in SplitSentences(cue.Text))
                {
                    var key = voice.Cloned
                        ? TakeKey(options.CloneModel, "clone:" + voice.CatalogId, language, seed, text)
                        : TakeKey(options.Model, voice.Description, language, seed, text);
                    var sentence = new NarrationSentence
                    {
                        Index = index++,
                        Cue = cue,
                        Text = text,
                        Voice = voice,
                        Language = language,
                        Seed = seed,
                        Key = key,
                        Volume = volume,
                        File = folder == null ? null : Path.Combine(folder, key + ".ogg"),
                        Url = folderUrl == null ? null : folderUrl.TrimEnd('/') + "/" + key + ".ogg",
                    };
                    var measured = sentence.File != null ? TakeDuration(sentence.File) : null;
                    sentence.Estimated = measured == null;
                    sentence.Duration = measured ?? Estimate(text, options.Wpm);
                    cue.Sentences.Add(sentence);
                }
            }
            narration.Warnings.AddRange(voiceWarnings);
            narration.Schedule(scenes);
            return narration;
        }

        /// <summary>
        /// Places every line on the root timeline. A line starts at its anchor, or
        /// <see cref="VideoNarrationOptions.Gap"/> after the line before when it has none —
        /// and never before the line before has finished.
        /// </summary>
        public void Schedule(IList<VideoScene> scenes)
        {
            scenes ??= new List<VideoScene>();
            // Scheduling again (after scenes were fitted) restates its warnings rather than adding to them.
            if (_scheduleWarnings >= 0 && _scheduleWarnings <= Warnings.Count) Warnings.RemoveRange(_scheduleWarnings, Warnings.Count - _scheduleWarnings);
            _scheduleWarnings = Warnings.Count;
            var anchors = Anchors(scenes);
            double? previousEnd = null;
            foreach (var cue in Cues)
            {
                if (cue.Sentences.Count == 0) continue;
                var gap = VideoOptions.TryDouble(cue.Get("gap"), out var g) ? Math.Max(0, g) : Options.Gap;
                var follow = previousEnd.HasValue ? previousEnd.Value + gap : Options.Start;
                double start = follow;
                cue.Requested = null;
                cue.Shifted = false;

                if (cue.At != null)
                {
                    var resolved = ResolveAnchor(cue.At, anchors, previousEnd ?? 0);
                    if (resolved == null)
                    {
                        Warnings.Add($"Voice-over line {cue.Index + 1}: no scene or element '{cue.At}' to anchor to; it follows the line before.");
                    }
                    else
                    {
                        cue.Requested = resolved.Value;
                        start = resolved.Value;
                        var earliest = previousEnd.HasValue ? previousEnd.Value + MinimumGap : 0;
                        if (start < earliest - 1e-6)
                        {
                            cue.Shifted = true;
                            Warnings.Add($"Voice-over line {cue.Index + 1} is anchored at {Clock(resolved.Value)} ({cue.At}) but the line before speaks until {Clock(previousEnd.Value)}; it starts at {Clock(earliest)}. Move its anchor or shorten the line before.");
                            start = earliest;
                        }
                    }
                }

                var pause = VideoOptions.TryDouble(cue.Get("pause"), out var p) ? Math.Max(0, p) : Options.Pause;
                var t = Math.Max(0, start);
                foreach (var sentence in cue.Sentences)
                {
                    sentence.Start = Math.Round(t, 3);
                    t = sentence.End + pause;
                }
                previousEnd = cue.End;
            }
        }

        /// <summary>
        /// Lengthens every scene whose duration is left to Neko until the lines anchored to it — to the
        /// scene itself (<c>intro+0.4</c>, <c>scene-3</c>) or to an element inside it (<c>#chart</c>) —
        /// have been spoken, plus <see cref="VideoNarrationOptions.Tail"/>, before the next scene's
        /// transition begins. Scenes are fitted in order and the lines re-scheduled after each change,
        /// since a longer scene moves every anchor after it. A line with no anchor, or anchored to a
        /// time, belongs to no scene and is left to run on. Returns whether any scene changed.
        /// </summary>
        public bool FitScenes(IList<VideoScene> scenes, Func<IList<VideoScene>, double> sequence)
        {
            if (!Options.Fit || scenes == null || scenes.Count == 0) return false;
            var sceneOf = AnchorScenes(scenes);
            var owned = Cues.Where(c => c.Sentences.Count > 0 && c.At != null)
                            .Select(c => (Cue: c, Scene: AnchorScene(c.At, sceneOf)))
                            .Where(o => o.Scene != null)
                            .ToList();
            if (owned.Count == 0) return false;

            var changed = false;
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                if (scene.HasExplicitDuration) continue;
                var lines = owned.Where(o => o.Scene == i).ToList();
                if (lines.Count == 0) continue;
                var outgoing = i + 1 < scenes.Count ? scenes[i + 1].RequestedTransition : 0;
                var needed = Math.Round(lines.Max(o => o.Cue.End) - scene.Start + Options.Tail + outgoing, 2);
                if (needed <= scene.Duration + 0.01) continue;
                scene.Duration = needed;
                changed = true;
                sequence(scenes);
                Schedule(scenes);
            }
            return changed;
        }

        /// <summary>Which scene each anchor name belongs to: scene ids, <c>scene-N</c> and element ids.</summary>
        private static Dictionary<string, int> AnchorScenes(IList<VideoScene> scenes)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scenes.Count; i++)
            {
                foreach (var e in VideoTimeline.Events(scenes[i].Html))
                {
                    if (!string.IsNullOrEmpty(e.Id) && !map.ContainsKey(e.Id)) map[e.Id] = i;
                }
            }
            for (int i = 0; i < scenes.Count; i++)
            {
                map["scene-" + (i + 1)] = i;
                var id = scenes[i].Get("id");
                if (!string.IsNullOrEmpty(id)) map[id] = i;
            }
            return map;
        }

        // The scene an anchor names, read the way ResolveAnchor reads it; null for a time or `+0.8`.
        private static int? AnchorScene(string raw, IReadOnlyDictionary<string, int> sceneOf)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var value = raw.Trim();
            if (TryClock(value, out _) || value.StartsWith("+")) return null;
            if (sceneOf.TryGetValue(value.TrimStart('#'), out var exact)) return exact;
            var m = AnchorRegex.Match(value);
            if (!m.Success || !m.Groups["ref"].Success) return null;
            return sceneOf.TryGetValue(m.Groups["ref"].Value, out var scene) ? scene : null;
        }

        /// <summary>The anchor names a line can use: every scene (by id, settled) and every element with an id.</summary>
        public static Dictionary<string, double> Anchors(IList<VideoScene> scenes)
        {
            var anchors = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                foreach (var e in VideoTimeline.Events(scene.Html))
                {
                    if (!string.IsNullOrEmpty(e.Id) && !anchors.ContainsKey(e.Id)) anchors[e.Id] = scene.Start + e.At;
                }
            }
            // Scenes win over elements: `scene-3` is always the scene.
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var settled = scene.Start + scene.TransitionDuration;
                anchors["scene-" + (i + 1)] = settled;
                var id = scene.Get("id");
                if (!string.IsNullOrEmpty(id)) anchors[id] = settled;
            }
            return anchors;
        }

        private static readonly Regex AnchorRegex = new(@"^#?(?<ref>[A-Za-z_][\w-]*?)?\s*(?<off>[+-]\s*\d+(?:\.\d+)?\s*s?)?$", RegexOptions.Compiled);

        /// <summary>
        /// An anchor as written: seconds (<c>12.5</c>, <c>1:04.2</c>), after the line before
        /// (<c>+0.8</c>), or a scene or element id with an optional offset
        /// (<c>scene-3</c>, <c>#chart+0.4</c>, <c>intro-0.2</c>).
        /// </summary>
        public static double? ResolveAnchor(string raw, IReadOnlyDictionary<string, double> anchors, double previousEnd)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var value = raw.Trim();
            if (TryClock(value, out var absolute)) return Math.Max(0, absolute);
            if (value.StartsWith("+") && VideoOptions.TryDouble(value[1..], out var after)) return previousEnd + after;

            // Ids may contain dashes (`scene-3`), so try the whole thing as an id first.
            var bare = value.TrimStart('#');
            if (anchors.TryGetValue(bare, out var exact)) return exact;

            var m = AnchorRegex.Match(value);
            if (!m.Success || !m.Groups["ref"].Success) return null;
            if (!anchors.TryGetValue(m.Groups["ref"].Value, out var at)) return null;
            if (m.Groups["off"].Success && VideoOptions.TryDouble(m.Groups["off"].Value.Replace(" ", string.Empty).TrimStart('+'), out var offset)) at += offset;
            return Math.Max(0, at);
        }

        /// <summary><c>12.5</c>, <c>12.5s</c> or <c>m:ss.s</c>.</summary>
        internal static bool TryClock(string value, out double seconds)
        {
            seconds = 0;
            var v = value.Trim();
            var colon = v.IndexOf(':');
            if (colon > 0)
            {
                return int.TryParse(v[..colon], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
                    && double.TryParse(v[(colon + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var rest)
                    && (seconds = minutes * 60 + rest) >= 0;
            }
            if (!Regex.IsMatch(v, @"^\d+(\.\d+)?\s*(s|ms)?$", RegexOptions.IgnoreCase)) return false;
            return VideoOptions.TryDouble(v, out seconds);
        }

        internal static string Clock(double seconds)
        {
            var m = (int)(Math.Max(0, seconds) / 60);
            var s = Math.Max(0, seconds) - m * 60;
            return m + ":" + s.ToString("00.00", CultureInfo.InvariantCulture);
        }

        // -----------------------------------------------------------------
        // takes on disk
        // -----------------------------------------------------------------

        /// <summary>The take's cache key: a short hash of the model, voice, language, seed and text.</summary>
        public static string TakeKey(string model, string voice, string language, int seed, string text)
        {
            var payload = string.Join("\n", "neko-voiceover-1", model ?? string.Empty, voice ?? string.Empty, language ?? "auto",
                seed.ToString(CultureInfo.InvariantCulture), text ?? string.Empty);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }

        private static readonly ConcurrentDictionary<string, (DateTime Stamp, double? Seconds)> Durations = new();

        /// <summary>The length of an Ogg Opus take, read from its last granule position — or null when there is no take.</summary>
        public static double? TakeDuration(string file)
        {
            try
            {
                var info = new FileInfo(file);
                if (!info.Exists || info.Length < 64) return null;
                if (Durations.TryGetValue(file, out var cached) && cached.Stamp == info.LastWriteTimeUtc) return cached.Seconds;
                var seconds = ReadOggDuration(file);
                Durations[file] = (info.LastWriteTimeUtc, seconds);
                return seconds;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static double? ReadOggDuration(string file)
        {
            using var stream = File.OpenRead(file);
            var head = new byte[Math.Min(4096, (int)stream.Length)];
            stream.ReadExactly(head);
            var opusHead = IndexOf(head, "OpusHead"u8, 0);
            if (opusHead < 0 || opusHead + 12 > head.Length) return null;
            var preSkip = BitConverter.ToUInt16(head, opusHead + 10);

            var tailLength = (int)Math.Min(stream.Length, 65536);
            var tail = new byte[tailLength];
            stream.Seek(-tailLength, SeekOrigin.End);
            stream.ReadExactly(tail);
            var last = -1;
            for (var i = IndexOf(tail, "OggS"u8, 0); i >= 0; i = IndexOf(tail, "OggS"u8, i + 4)) last = i;
            if (last < 0 || last + 14 > tail.Length) return null;
            var granule = BitConverter.ToInt64(tail, last + 6);
            if (granule <= preSkip) return null;
            return Math.Round((granule - preSkip) / 48000.0, 3);
        }

        private static int IndexOf(byte[] data, ReadOnlySpan<byte> pattern, int from)
        {
            var i = data.AsSpan(from).IndexOf(pattern);
            return i < 0 ? -1 : i + from;
        }
    }
}
