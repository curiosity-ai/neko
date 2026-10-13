using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Neko.Builder;
using Neko.Configuration;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Unit coverage for the curiosity video theme's brand layer: the pixel glyph catalog and its notation,
    /// the build animation, Sudo's sprite sheet, the painted art's options, the brand word timing and the
    /// brand components.
    /// </summary>
    [TestFixture]
    public class VideoBrandTests
    {
        private static MarkdownParser NewParser() => new MarkdownParser(new NekoConfig());

        private static string CuriosityScene(string body)
            => NewParser().Parse("---\ntitle: T\nvideo:\n  theme: curiosity\n---\n\n" + body).Scenes![0].Html!;

        [Test]
        public void EveryGlyph_DecodesToSixteenCellFrames()
        {
            Assert.That(CuriosityBrand.Glyphs, Has.Count.EqualTo(85));
            foreach (var (name, spec) in CuriosityBrand.Glyphs)
            {
                var frames = CuriosityBrand.Decode(spec.Animation);
                Assert.That(frames, Is.Not.Empty, name);
                Assert.That(frames.All(f => f.Cells.Length == 16 && f.Ms > 0), name);
            }
        }

        [Test]
        public void Encode_RoundTripsThroughDecode()
        {
            const string glyph = "k..*k...kkk.....";
            var cells = CuriosityBrand.Decode(glyph).Single().Cells;
            Assert.That(CuriosityBrand.Decode(CuriosityBrand.Encode(cells)).Single().Cells, Is.EqualTo(cells));
        }

        [Test]
        public void BuildAnimation_StartsEmpty_AndLandsTheSignalLast()
        {
            var frames = CuriosityBrand.Decode(CuriosityBrand.BuildAnimation("k..*k...kkk....."));
            var target = CuriosityBrand.Decode("k..*k...kkk.....").Single().Cells;

            Assert.That(frames[0].Cells.All(k => k == 0), "the build starts from nothing");
            Assert.That(frames[^1].Cells, Is.EqualTo(target), "and ends on the glyph");
            Assert.That(frames[^2].Cells[3], Is.EqualTo(0), "the Signal cell is the last to land");
        }

        [Test]
        public void ResolveGlyph_TakesNamesAndInlineCells()
        {
            Assert.That(CuriosityBrand.ResolveGlyph("value-connected"), Is.Not.Null);
            Assert.That(CuriosityBrand.ResolveGlyph("k..*k...kkk....."), Is.Not.Null);
            Assert.That(CuriosityBrand.ResolveGlyph("no-such-glyph"), Is.Null);
        }

        [Test]
        public void SudoSheet_DecodesItsClips()
        {
            var clips = CuriosityBrand.SudoClips;
            Assert.That(clips, Has.Count.EqualTo(15));
            Assert.That(clips.Values.Sum(c => c.Frames.Count), Is.EqualTo(53));
            Assert.That(clips.Values.SelectMany(c => c.Frames).All(f => f.Cells.Length == CuriosityBrand.SudoWidth * CuriosityBrand.SudoHeight));
            Assert.That(clips, Does.ContainKey("workIdle"));
        }

        [Test]
        public void SudoFrameSvg_DrawsTheEarsInSignal()
        {
            var frame = CuriosityBrand.SudoClips.Values.SelectMany(c => c.Frames).First(f => f.Ear != null);
            var svg = CuriosityBrand.SudoFrameSvg(frame, onInk: false);
            Assert.That(svg, Does.StartWith("<svg"));
            Assert.That(svg, Does.Contain(VideoArt.Signal).IgnoreCase);
        }

        [Test]
        public void Canvas_WritesThePainterAndItsOptions()
        {
            var html = VideoArt.Canvas("wave", new Dictionary<string, string>(), "curiosity", "paper", null, ownGround: true);
            var painter = Regex.Match(html, "data-paint=\"([^\"]+)\"").Groups[1].Value;
            var json = System.Net.WebUtility.HtmlDecode(Regex.Match(html, "data-o=\"([^\"]+)\"").Groups[1].Value);
            var o = JsonDocument.Parse(json).RootElement;

            Assert.That(painter, Is.EqualTo("mosaic"));
            Assert.That(o.GetProperty("shape").GetString(), Is.EqualTo("wave"));
            Assert.That(o.GetProperty("bg").GetString(), Is.EqualTo(VideoArt.Signal), "a wave sits on the signal scheme");
            Assert.That(VideoArt.Canvas("aurora", null, "curiosity", "paper", null, false), Is.Null);
        }

        [Test]
        public void FieldOnDeep_TakesAPaperSignal()
        {
            var html = VideoArt.Canvas("field", null, "curiosity", "deep", null, ownGround: false);
            var json = System.Net.WebUtility.HtmlDecode(Regex.Match(html, "data-o=\"([^\"]+)\"").Groups[1].Value);
            Assert.That(JsonDocument.Parse(json).RootElement.GetProperty("hot").GetString(), Is.EqualTo(VideoArt.Paper));
        }

        [Test]
        public void BrandWords_RiseSlowerAndCloser()
        {
            var html = CuriosityScene("# Write *Markdown.* Get *video.*\n");
            var times = Regex.Matches(html, @"data-v=""word""[^>]*data-at=""([\d.]+)""").Select(m => double.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();

            Assert.That(times, Has.Count.EqualTo(4));
            Assert.That(times[1] - times[0], Is.EqualTo(0.055).Within(1e-9));
            Assert.That(html, Does.Contain("class=\"nv-wi\""), "each word rises inside its mask");
        }

        [Test]
        public void PaintedSceneArt_IsACanvas()
        {
            var html = CuriosityScene("--- {layout=\"split\" art=\"mountain\"}\n\n## Title\n");
            Assert.That(html, Does.Contain("data-painted=\"true\""));
            Assert.That(html, Does.Contain("data-paint=\"mosaic\""));
        }

        [Test]
        public void Intro_LaysAnOverlayOnTheFirstScene()
        {
            var doc = NewParser().Parse("---\ntitle: T\nvideo:\n  theme: curiosity\n  intro: squares\n---\n\n# One\n\n---\n\n## Two\n");
            Assert.That(doc.Scenes![0].Html, Does.Contain("class=\"nv-intro\""));
            Assert.That(doc.Scenes![1].Html, Does.Not.Contain("class=\"nv-intro\""));
        }

        [Test]
        public void StillGlyphs_BuildOnce_UnlessAskedToLoop()
        {
            var once = CuriosityScene("::: glyph {name=\"value-connected\"}\n:::\n");
            var loops = CuriosityScene("::: glyph {name=\"value-connected\" loop=\"true\"}\n:::\n");
            Assert.That(once, Does.Contain("data-loop=\"false\""));
            Assert.That(loops, Does.Contain("data-loop=\"true\""));
        }

        [Test]
        public void BrandComponents_Render()
        {
            var html = CuriosityScene(
                "::: tile {art=\"join-grid\" label=\"Join\"}\n:::\n\n" +
                "::: glyphs {names=\"value-connected|value-resolved\" labels=\"A|B\"}\n:::\n\n" +
                "::: sudo {clip=\"workIdle\" class=\"corner\"}\n:::\n\n" +
                "::: mark {cube=\"true\"}\n:::\n\n" +
                "::: wordmark {tagline=\"Line\"}\n:::\n");

            Assert.That(html, Does.Contain("data-paint=\"joinGrid\""));
            Assert.That(Regex.Matches(html, "data-v=\"glyph\"").Count, Is.EqualTo(2));
            Assert.That(html, Does.Contain("data-v=\"sudo\""));
            Assert.That(html, Does.Contain("data-paint=\"cube\""));
            Assert.That(html, Does.Contain("Line"));
        }
    }
}
