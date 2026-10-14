using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Unit coverage for the video voice-over: the narration track as its own part of the
    /// file, the <c>narration:</c> voice options, reading lines and sentences, laying them
    /// on the root timeline at their anchors without overlaps, takes on disk (their cache
    /// key, their length, the mix), the timing sheet, and what the page carries for the
    /// preview. Synthesis itself (the Voxwerk model) is not run here.
    /// </summary>
    [TestFixture]
    public class VideoNarrationTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        private const string Video = @"---
title: T
video:
  narration:
    voice: ""A calm narrator.""
    voices:
      guest: ""An elderly man with a raspy voice.""
---

--- {id=""intro"" duration=""4""}

# One

--- {duration=""4"" transition=""cut""}

## Two

- First
- Second {#second}

--- {narration}

Hello there. This is the intro.

{at=""scene-2""}
Now the second scene.
";

        private string _dir;

        [SetUp]
        public void SetUp()
        {
            VideoNarration.PrintWarnings = false;
            _dir = Path.Combine(Path.GetTempPath(), "neko-narration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_dir, "videos"));
            File.WriteAllText(Path.Combine(_dir, "neko.yml"), "url: https://example.com\n");
        }

        [TearDown]
        public void TearDown()
        {
            VideoNarration.PrintWarnings = true;
            try { Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private ParsedDocument ParseOnDisk(string markdown, string name = "film.md")
        {
            var path = Path.Combine(_dir, "videos", name);
            File.WriteAllText(path, markdown);
            return NewParser().Parse(markdown, path, _dir);
        }

        // ------------------------------------------------------------- the track

        [Test]
        public void NarrationPart_IsATrack_NotAScene()
        {
            var doc = NewParser().Parse(Video);

            Assert.That(doc.Scenes, Has.Count.EqualTo(2), "the narration part is not filmed");
            Assert.That(doc.Narration, Is.Not.Null);
            Assert.That(doc.Narration!.Cues, Has.Count.EqualTo(2));
            Assert.That(doc.Narration.Cues[0].Sentences.Select(s => s.Text), Is.EqualTo(new[] { "Hello there.", "This is the intro." }));
            Assert.That(doc.Scenes!.All(s => !s.Html.Contains("Hello there")), Is.True, "the voice-over never lands in a scene");
        }

        [TestCase("--- {narration}")]
        [TestCase("--- {voiceover}")]
        [TestCase("--- {narration voice=\"guest\"}")]
        public void SeparatorFlag_MarksANarrationPart(string separator)
        {
            var parts = PresentationParser.Split($"# Scene\n\n{separator}\n\nA line.\n");
            Assert.That(parts, Has.Count.EqualTo(2));
            Assert.That(VideoNarration.IsNarrationPart(parts[1].Attributes), Is.True);
            Assert.That(VideoNarration.IsNarrationPart(parts[0].Attributes), Is.False);
        }

        [Test]
        public void BareFlags_DoNotDisturbOtherAttributes()
        {
            var parts = PresentationParser.Split("--- {layout=\"cover\" .wide #intro art=field}\n\n# A\n");
            var a = parts[0].Attributes;
            Assert.That(a["layout"], Is.EqualTo("cover"));
            Assert.That(a["art"], Is.EqualTo("field"));
            Assert.That(a["id"], Is.EqualTo("intro"));
            Assert.That(a["class"], Is.EqualTo("wide"));
            Assert.That(a.Keys, Has.No.Member("wide").And.No.Member("intro").And.No.Member("cover"));
        }

        [Test]
        public void VideoWithoutNarration_HasNone()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n# One\n");
            Assert.That(doc.Narration, Is.Null);
        }

        // ------------------------------------------------------------- voices

        [Test]
        public void NarrationOptions_ReadAVoiceOrAMapping()
        {
            var bare = NewParser().Parse("---\ntitle: T\nvideo:\n  narration: A deep, slow voice.\n---\n\n# One\n\n--- {narration}\n\nHi.\n");
            Assert.That(bare.Video!.Narration!.Voice, Is.EqualTo("A deep, slow voice."));
            Assert.That(bare.Narration!.Sentences.Single().Voice.Description, Is.EqualTo("A deep, slow voice."));

            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  narration:\n    voice: host\n    language: German\n    seed: 3\n    gap: 1\n    pause: 0.4\n    wpm: 120\n    voices:\n      host: A bright young voice.\n---\n\n# One\n");
            var n = doc.Video!.Narration!;
            Assert.That((n.Voice, n.Language, n.Seed, n.Gap, n.Pause, n.Wpm), Is.EqualTo(("host", "german", 3, 1.0, 0.4, 120.0)));
            Assert.That(VideoNarration.ResolveVoice(null, n).Description, Is.EqualTo("A bright young voice."));
            Assert.That(VideoNarration.ResolveVoice(null, n).Name, Is.EqualTo("host"));
        }

        [Test]
        public void LineVoice_PicksANamedVoice_AndTheDefaultOtherwise()
        {
            var doc = NewParser().Parse(Video.Replace("Now the second scene.", "Now the second scene. {voice=\"guest\"}"));
            var sentences = doc.Narration!.Sentences.ToList();
            Assert.That(sentences[0].Voice.Description, Is.EqualTo("A calm narrator."));
            Assert.That(sentences[2].Voice.Description, Is.EqualTo("An elderly man with a raspy voice."));
            Assert.That(sentences[2].Voice.Name, Is.EqualTo("guest"));
        }

        [Test]
        public void UnknownCatalogId_FallsBackToTheDefaultNarrator_WithAWarning()
        {
            var warnings = new List<string>();
            var voice = VideoNarration.ResolveVoice("en-zz-nobody-here", new VideoNarrationOptions(), warnings);
            Assert.That(voice.Description, Is.EqualTo(VideoNarrationOptions.DefaultVoice));
            Assert.That(warnings.Single(), Does.Contain("en-zz-nobody-here"));
        }

        [Test]
        public void NoVoice_IsTheDefaultNarrator()
        {
            var voice = VideoNarration.ResolveVoice(null, new VideoNarrationOptions());
            Assert.That(voice.Description, Is.EqualTo(VideoNarrationOptions.DefaultVoice));
        }

        [TestCase("en", "english")]
        [TestCase("de", "german")]
        [TestCase("", null)]
        public void CatalogLanguages_MapToQwenNames(string code, string? name)
            => Assert.That(VideoNarration.LanguageName(code), Is.EqualTo(name));

        // ------------------------------------------------------------- lines and sentences

        [Test]
        public void ReadCues_ParagraphsListItemsAttributesHeadingsAndComments()
        {
            var cues = VideoNarration.ReadCues(@"# A label, not spoken

<!-- a note to self -->
First line, with **bold** and a [link](https://x.y).
It carries on here. {at=""2.5"" voice=""guest""}

{at=""scene-2""}
Second line.

- Third line.
- Fourth line. {pause=""0.6""}
", new Dictionary<string, string> { ["voice"] = "host", ["language"] = "english" });

            Assert.That(cues.Select(c => c.Text), Is.EqualTo(new[]
            {
                "First line, with bold and a link. It carries on here.",
                "Second line.",
                "Third line.",
                "Fourth line.",
            }));
            Assert.That(cues[0].At, Is.EqualTo("2.5"));
            Assert.That(cues[0].Get("voice"), Is.EqualTo("guest"), "a line's own voice wins over the part's");
            Assert.That(cues[1].At, Is.EqualTo("scene-2"));
            Assert.That(cues[1].Get("voice"), Is.EqualTo("host"), "the part's voice is every line's default");
            Assert.That(cues[3].Get("pause"), Is.EqualTo("0.6"));
            Assert.That(cues.All(c => c.Get("language") == "english"), Is.True);
        }

        [Test]
        public void PartAnchor_PlacesItsFirstLine()
        {
            var cues = VideoNarration.ReadCues("One.\n\nTwo.\n", new Dictionary<string, string> { ["at"] = "scene-3" });
            Assert.That(cues[0].At, Is.EqualTo("scene-3"));
            Assert.That(cues[1].At, Is.Null);
        }

        [Test]
        public void SplitSentences_KeepsAbbreviationsTogether()
        {
            var s = VideoNarration.SplitSentences("Dr. Smith uses e.g. Neko. It works! Does it? Yes… “Quoted.” Done");
            Assert.That(s, Is.EqualTo(new[] { "Dr. Smith uses e.g. Neko.", "It works!", "Does it?", "Yes…", "“Quoted.”", "Done" }));
        }

        [Test]
        public void Estimate_GrowsWithWords_AndSlowsWithALowerRate()
        {
            var shortLine = VideoNarration.Estimate("Hello there.", 150);
            var longLine = VideoNarration.Estimate("Hello there, this is a much longer sentence with many more words in it.", 150);
            Assert.That(longLine, Is.GreaterThan(shortLine * 3));
            Assert.That(VideoNarration.Estimate("Hello there.", 100), Is.GreaterThan(shortLine));
        }

        // ------------------------------------------------------------- the root timeline

        [Test]
        public void Lines_FollowEachOther_WithTheGap_AndSentencesWithThePause()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  narration:\n    gap: 1\n    pause: 0.5\n    start: 0.25\n---\n\n--- {duration=\"30\"}\n\n# One\n\n--- {narration}\n\nOne. Two.\n\nThree.\n");
            var s = doc.Narration!.Sentences.ToList();
            Assert.That(s[0].Start, Is.EqualTo(0.25));
            Assert.That(s[1].Start, Is.EqualTo(s[0].End + 0.5).Within(1e-3));
            Assert.That(s[2].Start, Is.EqualTo(s[1].End + 1).Within(1e-3));
            Assert.That(s.All(x => x.Estimated), Is.True, "nothing synthesized: every sentence is estimated");
        }

        [Test]
        public void Anchors_ResolveToTimesScenesAndElements()
        {
            var doc = NewParser().Parse(Video.Replace("Now the second scene.", "Now the second scene.\n\nOn the item. {at=\"#second\"}\n\nLater. {at=\"0:20.5\"}\n\nAfter. {at=\"+2\"}"));
            var scenes = doc.Scenes!;
            var cues = doc.Narration!.Cues;

            // scene-2 starts after scene 1 (a cut: no overlap), settled at once.
            Assert.That(cues[1].Requested, Is.EqualTo(scenes[1].Start + scenes[1].TransitionDuration).Within(1e-3));
            var item = VideoTimeline.Events(scenes[1].Html).Single(e => e.Id == "second");
            Assert.That(cues[2].Requested, Is.EqualTo(scenes[1].Start + item.At).Within(1e-3));
            Assert.That(cues[3].Start, Is.EqualTo(20.5).Within(1e-3));
            Assert.That(cues[4].Start, Is.EqualTo(cues[3].End + 2).Within(1e-3));
        }

        private const string LongLine = "This line is long enough that a two-second scene could never hold it, so the scene has to grow until every word of it has been spoken aloud.";

        [Test]
        public void SceneWithoutDuration_FitsTheLinesAnchoredToIt()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  narration: A calm narrator.\n---\n\n--- {id=\"one\"}\n\n## One\n\n--- {id=\"two\"}\n\n## Two\n\n--- {narration}\n\n{at=\"one+0.4\"}\n" + LongLine + "\n\n{at=\"two+0.4\"}\nShort.\n");

            var scenes = doc.Scenes!;
            var first = doc.Narration!.Cues[0];
            var second = doc.Narration.Cues[1];
            Assert.That(scenes[0].End - scenes[1].TransitionDuration, Is.GreaterThanOrEqualTo(first.End + 0.8 - 0.01), "the line ends, then the tail, then the transition out");
            Assert.That(first.Shifted || second.Shifted, Is.False, "no line has to wait once the scenes fit");
            Assert.That(second.Start, Is.EqualTo(scenes[1].Start + scenes[1].TransitionDuration + 0.4).Within(0.01), "the second line still starts at its anchor");
            Assert.That(doc.Narration.Warnings, Is.Empty);
        }

        [Test]
        public void SceneWithDuration_OrFitOff_IsNotFitted()
        {
            var timed = NewParser().Parse("---\ntitle: T\nvideo:\n  narration: A calm narrator.\n---\n\n--- {id=\"one\" duration=\"3\"}\n\n## One\n\n--- {id=\"two\"}\n\n## Two\n\n--- {narration}\n\n{at=\"one+0.4\"}\n" + LongLine + "\n");
            Assert.That(timed.Scenes![0].Duration, Is.LessThan(timed.Narration!.Cues[0].End), "a written duration is the author's");

            var off = NewParser().Parse("---\ntitle: T\nvideo:\n  narration:\n    voice: A calm narrator.\n    fit: false\n---\n\n--- {id=\"one\"}\n\n## One\n\n--- {id=\"two\"}\n\n## Two\n\n--- {narration}\n\n{at=\"one+0.4\"}\n" + LongLine + "\n");
            Assert.That(off.Scenes![0].End, Is.LessThan(off.Narration!.Cues[0].End), "fit: false leaves the line running into the next scene");
        }

        [Test]
        public void ResolveAnchor_ReadsEveryForm()
        {
            var anchors = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["scene-2"] = 4, ["chart"] = 6.5 };
            Assert.That(VideoNarration.ResolveAnchor("12.5", anchors, 3), Is.EqualTo(12.5));
            Assert.That(VideoNarration.ResolveAnchor("1:02.5", anchors, 3), Is.EqualTo(62.5));
            Assert.That(VideoNarration.ResolveAnchor("+0.5", anchors, 3), Is.EqualTo(3.5));
            Assert.That(VideoNarration.ResolveAnchor("scene-2", anchors, 3), Is.EqualTo(4));
            Assert.That(VideoNarration.ResolveAnchor("scene-2+1.5", anchors, 3), Is.EqualTo(5.5));
            Assert.That(VideoNarration.ResolveAnchor("#chart-0.5", anchors, 3), Is.EqualTo(6.0));
            Assert.That(VideoNarration.ResolveAnchor("#nowhere", anchors, 3), Is.Null);
        }

        [Test]
        public void ALineAnchoredWhileAnotherSpeaks_WaitsForIt_AndSaysSo()
        {
            var doc = NewParser().Parse(Video.Replace("Hello there. This is the intro.",
                "A long first line that keeps on talking well past the start of the second scene, on and on and on and on, for quite a while."));
            var cues = doc.Narration!.Cues;

            Assert.That(cues[1].Requested, Is.LessThan(cues[0].End), "the anchor falls while line 1 speaks");
            Assert.That(cues[1].Shifted, Is.True);
            Assert.That(cues[1].Start, Is.EqualTo(cues[0].End + VideoNarration.MinimumGap).Within(1e-3));
            Assert.That(doc.Narration.Warnings, Has.Some.Contains("line 2").And.Some.Contains("scene-2"));
        }

        [Test]
        public void NoTwoSentences_EverOverlap()
        {
            var doc = NewParser().Parse(Video + "\n{at=\"0\"}\nBack to the very start, which is already taken.\n\n{at=\"scene-1\"}\nAnd again.\n");
            var s = doc.Narration!.Sentences.OrderBy(x => x.Start).ToList();
            for (int i = 1; i < s.Count; i++) Assert.That(s[i].Start, Is.GreaterThanOrEqualTo(s[i - 1].End), $"sentence {i + 1}");
        }

        [Test]
        public void UnknownAnchor_FollowsTheLineBefore_WithAWarning()
        {
            var doc = NewParser().Parse(Video.Replace("{at=\"scene-2\"}", "{at=\"#missing\"}"));
            var cues = doc.Narration!.Cues;
            Assert.That(cues[1].Start, Is.EqualTo(cues[0].End + doc.Narration.Options.Gap).Within(1e-3));
            Assert.That(doc.Narration.Warnings, Has.Some.Contains("#missing"));
        }

        [Test]
        public void TheAnimationNeverMoves_TheLastSceneHoldsForTheVoiceOver()
        {
            var silent = NewParser().Parse(Video.Substring(0, Video.IndexOf("--- {narration}")));
            var doc = NewParser().Parse(Video.Replace("Now the second scene.", "Now the second scene, and a great deal more to say about it, sentence after sentence. And more. And still more words to be spoken."));

            for (int i = 0; i < silent.Scenes!.Count; i++)
            {
                Assert.That(doc.Scenes![i].Start, Is.EqualTo(silent.Scenes[i].Start), "scene starts never depend on the voice-over");
                Assert.That(doc.Scenes[i].Html, Is.EqualTo(silent.Scenes[i].Html), "nor does any animation");
            }
            Assert.That(doc.Narration!.End, Is.GreaterThan(silent.VideoDuration));
            Assert.That(doc.VideoDuration, Is.EqualTo(doc.Narration.End + 0.6).Within(0.02));
            Assert.That(doc.Narration.Warnings, Has.Some.Contains("holds"));
        }

        // ------------------------------------------------------------- takes on disk

        [Test]
        public void TakeKey_ChangesWithTheTake_AndOnlyThen()
        {
            var a = VideoNarration.TakeKey("m", "voice", "english", 7, "Hello.");
            Assert.That(a, Has.Length.EqualTo(16));
            Assert.That(VideoNarration.TakeKey("m", "voice", "english", 7, "Hello."), Is.EqualTo(a));
            Assert.That(VideoNarration.TakeKey("m", "voice", "english", 7, "Hello!"), Is.Not.EqualTo(a));
            Assert.That(VideoNarration.TakeKey("m", "other", "english", 7, "Hello."), Is.Not.EqualTo(a));
            Assert.That(VideoNarration.TakeKey("m", "voice", "english", 8, "Hello."), Is.Not.EqualTo(a));
        }

        private static float[] Tone(double seconds, int rate = 24000)
            => Enumerable.Range(0, (int)(seconds * rate)).Select(i => 0.3f * MathF.Sin(i * 2 * MathF.PI * 220 / rate)).ToArray();

        [Test]
        public void Takes_AreKeptBesideThePage_AndTimeTheirSentences()
        {
            var estimated = ParseOnDisk(Video);
            var first = estimated.Narration!.Sentences.First();
            Assert.That(first.File, Is.EqualTo(Path.Combine(_dir, "videos", "assets", "voiceover", "film", first.Key + ".ogg")));
            Assert.That(first.Url, Is.EqualTo($"/videos/assets/voiceover/film/{first.Key}.ogg"));
            Assert.That(first.Estimated, Is.True);

            VoiceOverCommand.WriteTake(first.File, Tone(1.5), 24000);
            Assert.That(VideoNarration.TakeDuration(first.File), Is.EqualTo(1.5).Within(0.01));

            var measured = ParseOnDisk(Video);
            var s = measured.Narration!.Sentences.ToList();
            Assert.That(s[0].Estimated, Is.False);
            Assert.That(s[0].Duration, Is.EqualTo(1.5).Within(0.01));
            Assert.That(s[1].Estimated, Is.True);
            Assert.That(s[1].Start, Is.EqualTo(s[0].End + measured.Narration.Options.Pause).Within(1e-3));
        }

        [Test]
        public void Folder_CanBeSetInTheOptions()
        {
            var doc = ParseOnDisk(Video.Replace("    voice: \"A calm narrator.\"", "    voice: \"A calm narrator.\"\n    folder: assets/vo"));
            Assert.That(doc.Narration!.Folder, Is.EqualTo(Path.Combine(_dir, "videos", "assets", "vo")));
            Assert.That(doc.Narration.FolderUrl, Is.EqualTo("/videos/assets/vo"));
        }

        [Test]
        public void TrimSilence_CutsTheEdges_KeepingABreath()
        {
            var audio = new float[24000].Concat(Tone(1)).Concat(new float[24000]).ToArray();
            var trimmed = VoiceOverCommand.TrimSilence(audio, 24000);
            Assert.That(trimmed.Length / 24000.0, Is.EqualTo(1.12).Within(0.03));
        }

        [Test]
        public void MixTrack_PlacesEveryTakeAtItsTime()
        {
            var doc = ParseOnDisk(Video);
            foreach (var s in doc.Narration!.Sentences) VoiceOverCommand.WriteTake(s.File, Tone(1), 24000);
            doc = ParseOnDisk(Video);
            var first = doc.Narration!.Sentences.First();

            var wav = Path.Combine(_dir, "mix.wav");
            var placed = VoiceOverCommand.MixTrack(doc.Narration, 0, doc.VideoDuration, wav);
            Assert.That(placed, Is.EqualTo(doc.Narration.Sentences.Count()));

            var track = Voxwerk.Audio.AudioFile.ReadMono(wav, 24000);
            Assert.That(track.Length / 24000.0, Is.EqualTo(doc.VideoDuration).Within(0.01));
            float Peak(double from, double to) => track.Skip((int)(from * 24000)).Take((int)((to - from) * 24000)).Max(Math.Abs);
            Assert.That(Peak(0, first.Start - 0.05), Is.LessThan(0.01), "silence before the first sentence");
            Assert.That(Peak(first.Start + 0.2, first.Start + 0.6), Is.GreaterThan(0.1), "the take plays at its sentence's time");
        }

        // ------------------------------------------------------------- the preview

        [Test]
        public void Events_ListEveryEntrance_WithItsText()
        {
            var doc = NewParser().Parse(Video);
            var events = VideoTimeline.Events(doc.Scenes![1].Html);
            Assert.That(events.Select(e => e.Text), Has.Member("Two").And.Member("First").And.Member("Second"));
            Assert.That(events.Select(e => e.Motion), Has.No.Member("word"), "words are part of their headline");
            Assert.That(events.Single(e => e.Text == "Second").Id, Is.EqualTo("second"), "an {#id} on a list item is kept on the item");
        }

        [Test]
        public void TimingSheet_ListsScenesAnimationAndSentences_InTimeOrder()
        {
            var doc = NewParser().Parse(Video);
            var rows = VideoAlignment.Build(doc);
            Assert.That(rows.Select(r => r.Time), Is.Ordered);
            Assert.That(rows.Count(r => r.Lane == VideoAlignment.Lane.Scene), Is.EqualTo(2));
            Assert.That(rows.Count(r => r.Lane == VideoAlignment.Lane.Voice), Is.EqualTo(3));
            Assert.That(rows.Any(r => r.Lane == VideoAlignment.Lane.Animation && r.Text == "One"), Is.True);

            var text = VideoAlignment.Format(doc, "film.md");
            Assert.That(text, Does.Contain("3 sentences (3 estimated)"));
            Assert.That(text, Does.Contain("~ Hello there."));
            Assert.That(text, Does.Contain("▶ intro"));
        }

        [Test]
        public void Page_CarriesSubtitlesTimingPanel_AndTheSynthesizedTakes()
        {
            var doc = ParseOnDisk(Video);
            var first = doc.Narration!.Sentences.First();
            VoiceOverCommand.WriteTake(first.File, Tone(1.2), 24000);
            doc = ParseOnDisk(Video);
            first = doc.Narration!.Sentences.First();

            var html = new HtmlGenerator(new NekoConfig()).GenerateVideo(doc);
            Assert.That(html, Does.Contain("id=\"nv-subtitle\""));
            Assert.That(html, Does.Contain("id=\"nv-timing\""));
            Assert.That(html, Does.Contain("id=\"nv-timing-toggle\""));
            Assert.That(html, Does.Contain("class=\"nv-sent is-estimated\""), "a sentence with no take is drawn as an estimate");
            Assert.That(html, Does.Contain($"data-kind=\"narration\" data-start=\"{VideoTimeline.Num(first.Start)}\" data-duration=\"{VideoTimeline.Num(first.Duration)}\""));
            Assert.That(html, Does.Contain(first.Key + ".ogg"));
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "data-kind=\"narration\"").Count, Is.EqualTo(1), "only synthesized sentences have audio");
            Assert.That(html, Does.Contain("\"narration\":[{\"start\":"));
        }

        [Test]
        public void Page_WithoutNarration_StillHasTheTimingPanel_ButNoSubtitle()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n# One\n");
            var html = new HtmlGenerator(new NekoConfig()).GenerateVideo(doc);
            Assert.That(html, Does.Contain("id=\"nv-timing\""));
            Assert.That(html, Does.Not.Contain("id=\"nv-subtitle\""));
        }
    }
}
