using Neko.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Voxwerk;
using Voxwerk.Audio;
using Voxwerk.Talker;

namespace Neko.Builder
{
    /// <summary>What <c>neko voiceover</c> was asked to do.</summary>
    public sealed class VoiceOverSettings
    {
        /// <summary>A video page, or a folder of them.</summary>
        public string Input { get; set; }

        /// <summary>Only compute and print the timing sheet: no model, no synthesis.</summary>
        public bool Preview { get; set; }

        /// <summary>Also write the timing sheet as JSON to this file (or folder, for a folder input).</summary>
        public string Json { get; set; }

        /// <summary>Synthesize every sentence again, even those with a take.</summary>
        public bool Force { get; set; }

        /// <summary>Keep takes no sentence uses any more (by default they are deleted).</summary>
        public bool KeepUnused { get; set; }

        /// <summary>Worker threads for the TTS kernels; 0 = all cores.</summary>
        public int Threads { get; set; }

        /// <summary>Store the talker weights as int8: faster, approximate.</summary>
        public bool Int8 { get; set; }
    }

    /// <summary>
    /// <c>neko voiceover</c>: speaks a video's narration track. Every sentence without a
    /// take is synthesized with Voxwerk (Qwen3-TTS VoiceDesign, pure C#) in the voice its
    /// line names, trimmed, and kept as Ogg Opus under <c>assets/voiceover/&lt;page&gt;/</c>
    /// — keyed by a hash of voice, language, seed and text, so editing one sentence only
    /// re-speaks that sentence. Then it prints the timing sheet: scenes, animation and
    /// voice-over on one clock. With <c>--preview</c> it prints the sheet straight away,
    /// timing the sentences that have no take yet from an estimate.
    /// </summary>
    public static class VoiceOverCommand
    {
        public static async Task<int> RunAsync(VoiceOverSettings settings, CancellationToken ct = default)
        {
            var sources = VideoSources(settings.Input, out var isBatch);
            if (sources == null) return 1;
            VideoNarration.PrintWarnings = false;

            var found = 0;
            foreach (var source in sources)
            {
                var doc = Parse(source, out var root);
                if (!doc.IsVideo)
                {
                    if (!isBatch) Console.Error.WriteLine($"[voiceover] {source} is not a video page: add `video: true` to its front matter.");
                    continue;
                }
                found++;
                if (doc.Narration == null)
                {
                    if (!isBatch) Console.WriteLine($"[voiceover] {Path.GetFileName(source)} has no voice-over: add a `--- {{narration}}` part with its lines.");
                    continue;
                }

                if (!settings.Preview)
                {
                    await SynthesizeAsync(doc.Narration, settings, ct);
                    doc = Parse(source, out root);
                }

                Console.WriteLine();
                Console.WriteLine(VideoAlignment.Format(doc, Path.GetRelativePath(root, source).Replace('\\', '/')));

                if (!string.IsNullOrEmpty(settings.Json))
                {
                    var target = Path.GetFullPath(settings.Json);
                    if (isBatch || Directory.Exists(target)) target = Path.Combine(target, Path.GetFileNameWithoutExtension(source) + ".timing.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    await File.WriteAllTextAsync(target, JsonSerializer.Serialize(VideoAlignment.Json(doc), new JsonSerializerOptions { WriteIndented = true }), ct);
                    Console.WriteLine($"[voiceover] Timing sheet → {target}");
                }
            }

            if (found == 0)
            {
                Console.Error.WriteLine("[voiceover] No video pages found (a video page has `video:` in its front matter).");
                return 1;
            }
            return 0;
        }

        /// <summary>The video sources of an input: the file, or every Markdown file under the folder.</summary>
        internal static List<string> VideoSources(string inputPath, out bool isBatch)
        {
            var input = Path.GetFullPath(inputPath ?? ".");
            isBatch = Directory.Exists(input);
            if (isBatch)
            {
                return Directory.GetFiles(input, "*.md", SearchOption.AllDirectories)
                    .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(seg => seg.StartsWith(".") && seg.Length > 1))
                    .OrderBy(s => s, StringComparer.Ordinal).ToList();
            }
            if (File.Exists(input)) return new List<string> { input };
            Console.Error.WriteLine($"Not found: {input}");
            return null;
        }

        internal static ParsedDocument Parse(string source, out string root)
        {
            root = VideoRenderer.FindProjectRoot(source);
            var config = ConfigParser.Parse(Path.Combine(root, "neko.yml"));
            return new MarkdownParser(config).Parse(File.ReadAllText(source), source, root);
        }

        // -----------------------------------------------------------------
        // synthesis
        // -----------------------------------------------------------------

        /// <summary>
        /// Synthesizes every sentence of the narration that has no take yet (all of them
        /// with <see cref="VoiceOverSettings.Force"/>), and deletes takes no sentence uses.
        /// Returns how many sentences were spoken.
        /// </summary>
        public static async Task<int> SynthesizeAsync(VideoNarration narration, VoiceOverSettings settings, CancellationToken ct = default)
        {
            if (narration == null || narration.IsEmpty) return 0;
            if (narration.Folder == null)
            {
                Console.WriteLine("[voiceover] The page has no folder on disk to keep takes in; nothing synthesized.");
                return 0;
            }

            var sentences = narration.Sentences.ToList();
            var todo = sentences.Where(s => settings.Force || s.Estimated)
                .GroupBy(s => s.Key).Select(g => g.First()).ToList();

            if (!settings.KeepUnused) Prune(narration.Folder, sentences.Select(s => s.Key));

            if (todo.Count == 0)
            {
                Console.WriteLine($"[voiceover] All {sentences.Count} sentences have a take in {narration.Folder}.");
                return 0;
            }

            var designed = todo.Where(s => !s.Voice.Cloned).ToList();
            var cloned = todo.Where(s => s.Voice.Cloned).ToList();
            Console.WriteLine($"[voiceover] Synthesizing {todo.Count} of {sentences.Count} sentences (models are downloaded once to {Voxwerk.IO.ModelDownloader.DefaultCacheDir})…");
            var clock = Stopwatch.StartNew();
            var progress = new Progress<string>(message => Console.WriteLine("[voiceover]   " + message));
            Directory.CreateDirectory(narration.Folder);
            var done = 0;
            var spoken = 0.0;

            void Took(NarrationSentence sentence, Voxwerk.SynthesisResult result)
            {
                var audio = TrimSilence(result.Audio, result.SampleRate);
                WriteTake(sentence.File, audio, result.SampleRate);
                done++;
                var seconds = audio.Length / (double)result.SampleRate;
                spoken += seconds;
                Console.WriteLine($"[voiceover]   {done}/{todo.Count} {seconds,5:0.00}s  \"{Shorten(sentence.Text, 70)}\"  (RTF {result.RealTimeFactor:0.0})");
            }

            // Described voices: the VoiceDesign checkpoint invents the voice from its description.
            if (designed.Count > 0)
            {
                using var tts = await Qwen3Tts.LoadAsync(narration.Options.Model, settings.Threads,
                    settings.Int8 ? WeightPrecision.Int8 : WeightPrecision.BFloat16, progress, ct);
                if (tts.ModelType != Qwen3TtsModelType.VoiceDesign)
                {
                    throw new InvalidOperationException($"A described voice needs a VoiceDesign checkpoint ({VideoNarrationOptions.DefaultModel}); {narration.Options.Model} is a {tts.ModelType} model.");
                }
                Console.WriteLine($"[voiceover] {narration.Options.Model} ready in {clock.Elapsed.TotalSeconds:0.0} s.");
                foreach (var sentence in designed)
                {
                    ct.ThrowIfCancellationRequested();
                    var language = tts.Languages.Contains(sentence.Language, StringComparer.OrdinalIgnoreCase) ? sentence.Language : "auto";
                    Took(sentence, tts.SynthesizeVoiceDesign(sentence.Text, sentence.Voice.Description, language,
                        new GenerationOptions { Seed = sentence.Seed }, ct));
                }
            }

            // Catalog voices with a reference sample: the Base checkpoint clones the sample, so every
            // sentence is the same speaker. Designing them per sentence drifts from take to take.
            if (cloned.Count > 0)
            {
                using var tts = await Qwen3Tts.LoadAsync(narration.Options.CloneModel, settings.Threads,
                    settings.Int8 ? WeightPrecision.Int8 : WeightPrecision.BFloat16, progress, ct);
                if (tts.ModelType != Qwen3TtsModelType.Base)
                {
                    throw new InvalidOperationException($"A catalog voice is cloned with a Base checkpoint ({VideoNarrationOptions.DefaultCloneModel}); {narration.Options.CloneModel} is a {tts.ModelType} model. Set `clone: false` to design it from its description instead.");
                }
                Console.WriteLine($"[voiceover] {narration.Options.CloneModel} ready in {clock.Elapsed.TotalSeconds:0.0} s.");
                var prompts = new Dictionary<string, Voxwerk.Talker.VoiceClonePrompt>(StringComparer.OrdinalIgnoreCase);
                foreach (var sentence in cloned)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!prompts.TryGetValue(sentence.Voice.CatalogId, out var prompt))
                    {
                        prompt = tts.CreateVoiceClonePrompt(sentence.Voice.CatalogId, null);
                        prompts[sentence.Voice.CatalogId] = prompt;
                    }
                    var language = tts.Languages.Contains(sentence.Language, StringComparer.OrdinalIgnoreCase) ? sentence.Language : "auto";
                    Took(sentence, tts.Synthesize(sentence.Text, prompt, language, new GenerationOptions { Seed = sentence.Seed }, ct));
                }
            }

            Console.WriteLine($"[voiceover] {done} takes, {spoken:0.0} s of speech, in {clock.Elapsed.TotalSeconds:0.0} s → {narration.Folder}");
            return done;
        }

        /// <summary>Writes a take as Ogg Opus, atomically: a half-written file is never mistaken for a take.</summary>
        internal static void WriteTake(string file, float[] audio, int sampleRate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var bytes = OggOpus.Encode(audio, sampleRate, bitrate: 48000, new Dictionary<string, string> { ["encoder"] = "neko voiceover (Voxwerk)" });
            var temp = file + "." + Environment.ProcessId + ".tmp";
            File.WriteAllBytes(temp, bytes);
            File.Move(temp, file, overwrite: true);
        }

        private static readonly Regex TakeName = new(@"^[0-9a-f]{16}\.ogg$", RegexOptions.Compiled);

        private static void Prune(string folder, IEnumerable<string> keys)
        {
            if (!Directory.Exists(folder)) return;
            var used = new HashSet<string>(keys.Select(k => k + ".ogg"), StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.GetFiles(folder, "*.ogg"))
            {
                var name = Path.GetFileName(file);
                if (!TakeName.IsMatch(name) || used.Contains(name)) continue;
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"[voiceover] Removed unused take {name}");
                }
                catch (IOException) { }
            }
        }

        /// <summary>Cuts the silence the model leaves before and after a sentence, keeping a short breath of it.</summary>
        public static float[] TrimSilence(float[] audio, int sampleRate, float threshold = 0.012f, double keep = 0.06)
        {
            if (audio == null || audio.Length == 0) return audio ?? Array.Empty<float>();
            var window = Math.Max(1, sampleRate / 100);
            int first = -1, last = -1;
            for (int start = 0; start < audio.Length; start += window)
            {
                var peak = 0f;
                for (int i = start; i < Math.Min(audio.Length, start + window); i++) peak = Math.Max(peak, Math.Abs(audio[i]));
                if (peak < threshold) continue;
                if (first < 0) first = start;
                last = Math.Min(audio.Length, start + window);
            }
            if (first < 0) return audio;
            var pad = (int)(keep * sampleRate);
            var from = Math.Max(0, first - pad);
            var to = Math.Min(audio.Length, last + pad);
            return audio[from..to];
        }

        /// <summary>
        /// The voice-over as one track, <paramref name="from"/> to <paramref name="to"/> on the
        /// video's clock: every take decoded and placed at its sentence's time. Sentences
        /// without a take are silent. Returns the number of takes placed.
        /// </summary>
        public static int MixTrack(VideoNarration narration, double from, double to, string wavPath)
        {
            const int rate = 24000;
            var length = Math.Max(1, (int)Math.Ceiling((to - from) * rate));
            var track = new float[length];
            var placed = 0;
            foreach (var sentence in narration.Sentences)
            {
                if (sentence.Estimated || sentence.File == null || !File.Exists(sentence.File)) continue;
                if (sentence.End <= from || sentence.Start >= to) continue;
                float[] take;
                using (var stream = File.OpenRead(sentence.File)) take = OggOpus.Decode(stream, rate);
                var offset = (int)Math.Round((sentence.Start - from) * rate);
                var gain = (float)sentence.Volume;
                for (int i = Math.Max(0, -offset); i < take.Length; i++)
                {
                    var j = offset + i;
                    if (j >= length) break;
                    track[j] += take[i] * gain;
                }
                placed++;
            }
            for (int i = 0; i < track.Length; i++) track[i] = Math.Clamp(track[i], -1f, 1f);
            AudioFile.WriteWav(wavPath, track, rate, float32: true);
            return placed;
        }

        // -----------------------------------------------------------------
        // the catalog
        // -----------------------------------------------------------------

        /// <summary><c>neko voices</c>: lists or searches the Voxwerk voice catalog a narration can name.</summary>
        public static int ListVoices(string search, string gender, string country, string style, string age, bool json)
        {
            _ = VideoNarration.CatalogVoices;
            var voices = Voxwerk.Voices.VoiceRegistry.Search(search, country: country, gender: gender, style: style, age: age).ToList();
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(voices, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            if (VideoNarration.CatalogVoices.Count == 0)
            {
                Console.WriteLine("The installed Voxwerk.Voices.English catalog has no voices yet. Describe the voice instead:");
                Console.WriteLine("  narration:");
                Console.WriteLine("    voice: \"A warm, low female voice, calm and measured, speaking slowly.\"");
                return 0;
            }
            if (voices.Count == 0)
            {
                Console.WriteLine("No voice matches. Try fewer filters, or describe the voice in the `narration: voice:` option.");
                return 0;
            }
            foreach (var v in voices)
            {
                Console.WriteLine($"{v.Id,-30} {v.Gender,-7} {v.Age,-12} {v.Style,-15} {v.Category}");
                Console.WriteLine($"{"",-30} {Shorten(v.Description, 100)}");
            }
            Console.WriteLine();
            Console.WriteLine($"{voices.Count} voice(s). Use one with `narration: voice: <id>` in the video's front matter, or {{voice=\"<id>\"}} on a line.");
            return 0;
        }

        private static string Shorten(string text, int width)
            => string.IsNullOrEmpty(text) || text.Length <= width ? text : text[..(width - 1)] + "…";
    }
}
