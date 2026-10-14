using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Unit coverage for video mode: the front-matter marker, sizes, scene splitting,
    /// the timeline compiler (default motions, the running cursor, word splitting,
    /// explicit times), scene sequencing with transition overlaps, the video
    /// components, the generated page, and the ffmpeg command line of neko render.
    /// </summary>
    [TestFixture]
    public class VideoTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        private static double Num(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        // Every tag that moves, in document order, whatever order its attributes are in.
        private static List<(string Motion, double At)> Motions(string html)
            => Regex.Matches(html, @"<[^>]*\bdata-v=""[^""]+""[^>]*>")
                .Select(m => (Regex.Match(m.Value, @"data-v=""([^""]+)""").Groups[1].Value, Regex.Match(m.Value, @"data-at=""([\d.]+)""")))
                .Where(t => t.Item2.Success)
                .Select(t => (t.Item1, Num(t.Item2.Groups[1].Value))).ToList();

        [TearDown]
        public void ResetOverrides()
        {
            VideoOptions.SizeOverride = null;
            VideoOptions.FpsOverride = null;
        }

        [Test]
        public void Marker_BareTrue_EnablesVideoWithDefaults()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n# One\n");

            Assert.That(doc.IsVideo, Is.True);
            Assert.That(doc.IsPresentation, Is.False);
            Assert.That(doc.Video!.Theme, Is.EqualTo("neko"));
            Assert.That(doc.Video.Width, Is.EqualTo(1920));
            Assert.That(doc.Video.Height, Is.EqualTo(1080));
            Assert.That(doc.Video.Fps, Is.EqualTo(30));
            Assert.That(doc.Html, Is.Null, "a video renders scenes, not an article body");
            Assert.That(doc.Scenes, Has.Count.EqualTo(1));
        }

        [Test]
        public void Marker_Mapping_ReadsOptions()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  theme: curiosity\n  size: vertical\n  fps: 25\n  transition: zoom\n  pace: brisk\n  grain: true\n---\n\n# One\n");

            Assert.That(doc.Video!.Theme, Is.EqualTo("curiosity"));
            Assert.That((doc.Video.Width, doc.Video.Height), Is.EqualTo((1080, 1920)));
            Assert.That(doc.Video.IsPortrait, Is.True);
            Assert.That(doc.Video.Fps, Is.EqualTo(25));
            Assert.That(doc.Video.DefaultTransition, Is.EqualTo("zoom"));
            Assert.That(doc.Video.Pace, Is.EqualTo(0.75));
            Assert.That(doc.Video.Grain, Is.True);
        }

        [TestCase("false")]
        [TestCase("no")]
        [TestCase("off")]
        public void Marker_Falsey_BuildsAnOrdinaryPage(string value)
        {
            var doc = NewParser().Parse($"---\ntitle: T\nvideo: {value}\n---\n\n# One\n");

            Assert.That(doc.IsVideo, Is.False);
            Assert.That(doc.Html, Does.Contain("<h1"));
        }

        [TestCase("1280x720", 1280, 720)]
        [TestCase("720p", 1280, 720)]
        [TestCase("4k", 3840, 2160)]
        [TestCase("square", 1080, 1080)]
        [TestCase("1001x601", 1000, 600)]
        public void Size_IsParsed_AndKeptEven(string size, int width, int height)
        {
            var options = new VideoOptions();
            options.ApplySize(size);
            Assert.That((options.Width, options.Height), Is.EqualTo((width, height)));
        }

        [TestCase("9:16", 1080, 1920)]
        [TestCase("4:5", 1080, 1350)]
        [TestCase("16:9", 1920, 1080)]
        public void Ratio_KeepsTheShortSideAt1080(string ratio, int width, int height)
        {
            var options = new VideoOptions();
            options.ApplyRatio(ratio);
            Assert.That((options.Width, options.Height), Is.EqualTo((width, height)));
        }

        [Test]
        public void RenderOverrides_ApplyToEveryVideo()
        {
            VideoOptions.SizeOverride = "720p";
            VideoOptions.FpsOverride = 60;
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n# One\n");
            Assert.That((doc.Video!.Width, doc.Video.Height, doc.Video.Fps), Is.EqualTo((1280, 720, 60)));
        }

        [Test]
        public void DefaultTransition_DependsOnTheTheme()
        {
            Assert.That(new VideoOptions().DefaultTransition, Is.EqualTo("fade"));
            Assert.That(new VideoOptions { Theme = "curiosity" }.DefaultTransition, Is.EqualTo("push"));
        }

        [Test]
        public void Scenes_AreSequenced_WithTransitionOverlaps()
        {
            var doc = NewParser().Parse(
                "---\ntitle: T\nvideo:\n  transition: fade\n---\n\n--- {duration=\"3\"}\n\n# One\n\n--- {duration=\"4\"}\n\n## Two\n\n--- {duration=\"2\" transition=\"cut\"}\n\n## Three\n");

            var scenes = doc.Scenes!;
            Assert.That(scenes, Has.Count.EqualTo(3));
            Assert.That(scenes[0].Transition, Is.EqualTo("cut"), "the first scene has nothing to come in over");
            Assert.That(scenes[1].Transition, Is.EqualTo("fade"));
            Assert.That(scenes[1].TransitionDuration, Is.EqualTo(0.6).Within(1e-9));
            Assert.That(scenes[1].Start, Is.EqualTo(2.4).Within(1e-9), "overlaps the first scene by the transition");
            Assert.That(scenes[2].Transition, Is.EqualTo("cut"));
            Assert.That(scenes[2].Start, Is.EqualTo(6.4).Within(1e-9));
            Assert.That(doc.VideoDuration, Is.EqualTo(8.4).Within(1e-9));
        }

        [Test]
        public void SceneDuration_LeftToNeko_IsLastArrivalPlusHold()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  hold: 1\n---\n\n# A heading of four words\n\nA paragraph.\n\nAnother paragraph.\n");

            var scene = doc.Scenes![0];
            var last = Motions(scene.Html!).Max(m => m.At);
            Assert.That(scene.Duration, Is.GreaterThan(last + 0.5));
            Assert.That(scene.Duration, Is.LessThan(last + 0.6 + 1 + 0.01));
        }

        [Test]
        public void ExplicitDuration_TooShortForItsContent_IsHeld()
        {
            const string swap = "::: swap {prefix=\"Read it as\" words=\"WYSIWYG|XML|PDF\" hold=\"1.2\"}\n:::\n";
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n--- {duration=\"3.4\"}\n\n" + swap + "\n--- {duration=\"3.4\" hold=\"0\"}\n\n" + swap);

            var held = doc.Scenes![0];
            Assert.That(held.ContentEnd, Is.GreaterThan(3.4), "three words of 1.2 s each outlast the written duration");
            Assert.That(held.Duration, Is.EqualTo(held.ContentEnd + 1.6).Within(0.01), "the last word stays on screen for hold");
            Assert.That(doc.Scenes[1].Duration, Is.EqualTo(3.4), "hold=0 keeps the written duration");
        }

        [Test]
        public void ExplicitDuration_WithRoomToSettle_IsKept()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n--- {duration=\"3\"}\n\n## Two words\n");
            Assert.That(doc.Scenes![0].Duration, Is.EqualTo(3));
        }

        [Test]
        public void Cursor_OnScreen_CarriesItsTarget()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: browser\n![Shot](/a.png)\n:::\n\n::: cursor {on=\"screen\" to=\"63.6%,12.9%\"}\n:::\n").Scenes![0].Html!;
            Assert.That(html, Does.Contain("data-on=\"screen\""));
            Assert.That(html, Does.Contain("data-path=\"108%,86% 63.6%,12.9%\""));
        }

        [Test]
        public void Annotate_GivesPlainMarkdownDefaultMotions()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n## Two words\n\nA paragraph.\n\n- one\n- two\n\n> A quote.\n").Scenes![0].Html!;

            Assert.That(html, Does.Contain("data-split=\"words\""), "the heading is split into words");
            Assert.That(Regex.Matches(html, "class=\"nv-w\"").Count, Is.EqualTo(2));
            Assert.That(html, Does.Match(@"<p data-v=""rise"""));
            Assert.That(Regex.Matches(html, @"<li data-v=""rise""").Count, Is.EqualTo(2));
            Assert.That(html, Does.Match(@"<blockquote data-v=""rise"""));
        }

        [Test]
        public void Annotate_AScreenshotInAFrame_ArrivesWithTheFrame()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: browser {url=\"example.com\"}\n![Shot](shot.png)\n:::\n\n![Loose](loose.png)\n").Scenes![0].Html!;

            Assert.That(html, Does.Contain("class=\"nv-device\""));
            Assert.That(html, Does.Match(@"<div class=""nv-screen""><img [^>]*alt=""Shot"""), "the screenshot is the screen's only child");
            Assert.That(Regex.Match(html, @"<div class=""nv-screen"">.*?</div>").Value, Does.Not.Contain("data-v="), "it has no entrance of its own: it arrives with the frame");
        }

        [Test]
        public void Timeline_PlacesElementsInDocumentOrder_OnOneCursor()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\nFirst.\n\nSecond.\n\nThird.\n").Scenes![0].Html!;
            var times = Motions(html).Where(m => m.Motion == "rise").Select(m => m.At).ToList();

            Assert.That(times, Has.Count.EqualTo(3));
            Assert.That(times, Is.Ordered.Ascending);
            Assert.That(times[0], Is.EqualTo(0.3).Within(1e-9), "the first scene's content starts 0.3 s in");
            Assert.That(times[1] - times[0], Is.EqualTo(0.22).Within(1e-9));
        }

        [Test]
        public void Timeline_WordsAreStaggered()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n# Write *Markdown.* Get *video.*\n").Scenes![0].Html!;
            var words = Motions(html).Where(m => m.Motion == "word").Select(m => m.At).ToList();

            Assert.That(words, Has.Count.EqualTo(4));
            for (int i = 1; i < words.Count; i++) Assert.That(words[i] - words[i - 1], Is.EqualTo(0.06).Within(1e-9));
            Assert.That(html, Does.Contain("<em><span class=\"nv-w\""), "emphasis survives the split");
        }

        [Test]
        public void Timeline_HonoursExplicitTimes()
        {
            var html = NewParser().Parse(
                "---\ntitle: T\nvideo: true\n---\n\nFirst line. {at=\"2\"}\n\nSecond line. {at=\"with\" enter=\"fade\"}\n\nThird line. {at=\"+0.5\" enter=\"pop\" dur=\"0.9\"}\n").Scenes![0].Html!;
            var m = Motions(html);

            Assert.That(m[0], Is.EqualTo(("rise", 2.0)));
            Assert.That(m[1], Is.EqualTo(("fade", 2.0)), "`with` starts alongside the previous element");
            Assert.That(m[2].Motion, Is.EqualTo("pop"));
            Assert.That(m[2].At, Is.EqualTo(2.0 + 0.22 + 0.5).Within(1e-9), "`with` leaves the cursor where it was");
            Assert.That(html, Does.Contain("data-dur=\"0.9\""));
        }

        [Test]
        public void Timeline_ListItemAttributes_ReachTheItem()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n- only fades {enter=\"fade\"}\n- rises\n").Scenes![0].Html!;

            Assert.That(html, Does.Match(@"<li data-v=""fade"""));
            Assert.That(html, Does.Match(@"<li data-v=""rise"""));
        }

        [Test]
        public void Timeline_TypeSplitsCharacters_AndSizesItsDuration()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: typewriter\nabcdefghij\n:::\n").Scenes![0].Html!;

            Assert.That(Regex.Matches(html, "class=\"nv-c\"").Count, Is.EqualTo(10));
            var dur = Num(Regex.Match(html, @"data-v=""type""[^>]*data-dur=""([\d.]+)""").Groups[1].Value);
            Assert.That(dur, Is.EqualTo(0.4).Within(1e-9), "ten characters at 0.035 s, floored at 0.4 s");
        }

        [Test]
        public void Highlight_SweepsByDefault()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\nA ==marked== word.\n").Scenes![0].Html!;
            Assert.That(html, Does.Match(@"<mark[^>]*data-v=""sweep"""));
        }

        [Test]
        public void Counter_RendersTheFinalValue_AndItsCount()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: counter {value=\"$1,250+\" label=\"Saved\"}\n:::\n").Scenes![0].Html!;

            Assert.That(html, Does.Contain("data-v=\"count\""));
            Assert.That(html, Does.Contain("data-to=\"1250\""));
            Assert.That(html, Does.Contain("data-group=\"true\""));
            Assert.That(html, Does.Contain(">1,250</span>"), "the markup carries the value a reader without the runtime sees");
            Assert.That(html, Does.Contain(">$</span>"));
            Assert.That(html, Does.Contain(">+</span>"));
        }

        [TestCase("98.6%", "", 98.6, 1, "%")]
        [TestCase("$4.2M", "$", 4.2, 1, "M")]
        [TestCase("3x", "", 3, 0, "x")]
        [TestCase("-12", "", -12, 0, "")]
        public void ParseValue_SplitsAffixes(string raw, string prefix, double value, int decimals, string suffix)
        {
            var parsed = Neko.Extensions.VideoContainers.ParseValue(raw);
            Assert.That(parsed.Prefix, Is.EqualTo(prefix));
            Assert.That(parsed.Value, Is.EqualTo(value).Within(1e-9));
            Assert.That(parsed.Decimals, Is.EqualTo(decimals));
            Assert.That(parsed.Suffix, Is.EqualTo(suffix));
        }

        [Test]
        public void BarChart_ScalesBars_AndCallsOutTheMarkedDatum()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: chart {type=\"bar\"}\n- A: 10\n- B: 40 *\n- C: 20\n:::\n").Scenes![0].Html!;

            Assert.That(Regex.Matches(html, "class=\"nv-col-bar\"").Count, Is.EqualTo(3));
            Assert.That(html, Does.Contain("height:25%"));
            Assert.That(html, Does.Contain("height:100%"));
            Assert.That(html, Does.Contain("height:50%"));
            Assert.That(Regex.Matches(html, "nv-bar-col is-hi").Count, Is.EqualTo(1));
        }

        [Test]
        public void Captions_AreTimedOnTheSceneClock()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n::: captions {style=\"pill\"}\n0.5-2 one two three\n2.5 four five\n:::\n").Scenes![0].Html!;

            Assert.That(html, Does.Contain("data-at=\"0.5\" data-dur=\"0.18\" data-until=\"2\""));
            Assert.That(Regex.Matches(html, "class=\"nv-cw\"").Count, Is.EqualTo(5));
            Assert.That(html, Does.Contain("data-style=\"pill\""));
        }

        [Test]
        public void Code_TypesItself_AndKeepsTheLanguage()
        {
            var html = NewParser().Parse("---\ntitle: T\nvideo: true\n---\n\n:::: code {file=\"a.cs\"}\n```csharp\nvar x = 1;\n```\n::::\n").Scenes![0].Html!;

            Assert.That(html, Does.Contain("class=\"language-csharp\""));
            Assert.That(html, Does.Contain("data-v=\"code\""));
            Assert.That(html, Does.Contain("nv-editor-file\">a.cs"));
        }

        [Test]
        public void Components_AreOnlyClaimedInsideAVideo()
        {
            var html = NewParser().Parse("---\ntitle: T\n---\n\n::: counter {value=\"5\"}\nText.\n:::\n").Html!;
            Assert.That(html, Does.Not.Contain("data-v=\"count\""));
        }

        [Test]
        public void GenerateVideo_IsAStandalonePlayer()
        {
            var config = new NekoConfig();
            var doc = new MarkdownParser(config).Parse("---\ntitle: The film\nvideo:\n  theme: curiosity\n  size: 1280x720\n---\n\n# One\n\n---\n\n## Two\n");
            var page = new HtmlGenerator(config).GenerateVideo(doc);

            Assert.That(page, Does.Contain("data-video-theme=\"curiosity\""));
            Assert.That(page, Does.Contain("id=\"nv-stage\" style=\"width:1280px;height:720px\""));
            Assert.That(Regex.Matches(page, "class=\"nv-scene\"").Count, Is.EqualTo(2));
            Assert.That(page, Does.Contain("id=\"nv-timeline\""));
            Assert.That(page, Does.Contain("window.nekoVideo"), "the runtime is inlined");
            Assert.That(page, Does.Not.Contain("id=\"sidebar-list\""), "no documentation shell");
            Assert.That(page, Does.Contain("Schibsted Grotesk"), "the theme's fonts are inlined");
        }

        [Test]
        public void FfmpegArguments_EncodeBt709_AndMixTheSoundtrack()
        {
            var audio = new List<VideoRenderer.AudioClip>
            {
                new("/tmp/music.mp3", 0, 0.35, true),
                new("/tmp/voice.wav", 2.5, 1, false),
            };
            var args = VideoRenderer.BuildFfmpegArguments("/tmp/f_%06d.png", 30, 12, audio, new VideoRenderSettings(), "/tmp/out.mp4");

            Assert.That(args, Does.StartWith("-y -framerate 30 -i \"/tmp/f_%06d.png\""));
            Assert.That(args, Does.Contain("libx264"));
            Assert.That(args, Does.Contain("-crf 18"));
            Assert.That(args, Does.Contain("out_color_matrix=bt709"));
            Assert.That(args, Does.Contain("-colorspace bt709"));
            Assert.That(args, Does.Contain("+faststart"));
            Assert.That(args, Does.Contain("adelay=2500|2500"));
            Assert.That(args, Does.Contain("afade=t=out"));
            Assert.That(args, Does.Contain("amix=inputs=2"));
            Assert.That(args, Does.Contain("-t 12 \"/tmp/out.mp4\""));
        }

        [Test]
        public void FfmpegArguments_WebmUsesVp9()
        {
            var args = VideoRenderer.BuildFfmpegArguments("f_%06d.png", 24, 5, new List<VideoRenderer.AudioClip>(), new VideoRenderSettings { Format = "webm", Quality = "draft" }, "o.webm");
            Assert.That(args, Does.Contain("libvpx-vp9"));
            Assert.That(args, Does.Not.Contain("amix"));
        }

        [Test]
        public void ResolveLocalAsset_StaysInsideTheProject()
        {
            var root = Path.Combine(Path.GetTempPath(), "neko-video-assets-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            File.WriteAllText(Path.Combine(root, "assets", "bed.mp3"), "x");
            File.WriteAllText(Path.Combine(root, "v.md"), "x");
            try
            {
                var source = Path.Combine(root, "v.md");
                Assert.That(VideoRenderer.ResolveLocalAsset("/assets/bed.mp3", source, root), Is.EqualTo(Path.Combine(root, "assets", "bed.mp3")));
                Assert.That(VideoRenderer.ResolveLocalAsset("assets/bed.mp3", source, root), Is.Not.Null);
                Assert.That(VideoRenderer.ResolveLocalAsset("/../../etc/passwd", source, root), Is.Null);
                Assert.That(VideoRenderer.ResolveLocalAsset("https://example.com/a.mp3", source, root), Is.Null);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
