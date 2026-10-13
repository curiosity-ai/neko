using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Drives video mode in a real browser: builds a small site with a video page
    /// and checks that the player is ready with the timeline the build computed, that
    /// seeking shows the right scene, that a counter lands on its value, that the same
    /// time always paints the same frame, and that render mode strips the chrome and
    /// puts the canvas at 1:1. Then runs <c>neko render</c> itself: a still, and a
    /// short MP4 when ffmpeg is available.
    ///
    /// Skipped when a Playwright browser is unavailable. Point
    /// <c>NEKO_TEST_CHROMIUM</c> at a preinstalled Chromium when Playwright's own
    /// build is not installed.
    /// </summary>
    [TestFixture]
    public class VideoPlaywrightTests
    {
        private string _inputDir;
        private string _outDir;

        [OneTimeSetUp]
        public void Setup()
        {
            _inputDir = Path.Combine(Path.GetTempPath(), "neko-video-pw-in-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_inputDir, "videos"));
            File.WriteAllText(Path.Combine(_inputDir, "neko.yml"), "url: https://example.com\nbranding:\n  title: Demo\n");
            File.WriteAllText(Path.Combine(_inputDir, "index.md"), "# Home\n\n[!video src=\"/videos/demo.mp4\" link=\"/videos/demo\" title=\"Demo\"]\n");
            File.WriteAllText(Path.Combine(_inputDir, "videos", "demo.md"), @"---
title: Demo film
video:
  theme: curiosity
  size: 640x360
  fps: 12
  transition: push
---

--- {duration=""2"" layout=""cover"" art=""field""}

# Write *Markdown*

--- {duration=""2.5"" layout=""number"" ground=""deep""}

::: counter {value=""1,250"" label=""Frames""}
:::

--- {duration=""2"" transition=""zoom""}

## The *end*
");
            _outDir = Path.Combine(Path.GetTempPath(), "neko-video-pw-out-" + Guid.NewGuid().ToString("N"));
            new SiteBuilder(_inputDir, _outDir).BuildAsync().GetAwaiter().GetResult();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            try { Directory.Delete(_inputDir, true); } catch (IOException) { }
            try { Directory.Delete(_outDir, true); } catch (IOException) { }
        }

        private string PageUrl(string fragment = "") => new Uri(Path.Combine(_outDir, "videos", "demo.html")).AbsoluteUri + fragment;

        private static async Task<(IPlaywright, IBrowser)> LaunchAsync()
        {
            try
            {
                var pw = await Playwright.CreateAsync();
                var launch = new BrowserTypeLaunchOptions { Headless = true, Args = new[] { "--host-resolver-rules=MAP * ~NOTFOUND , EXCLUDE localhost" } };
                var exe = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM");
                if (!string.IsNullOrEmpty(exe) && File.Exists(exe)) launch.ExecutablePath = exe;
                var browser = await pw.Chromium.LaunchAsync(launch);
                return (pw, browser);
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Playwright browser unavailable: {ex.Message}");
                throw;
            }
        }

        private static async Task<IPage> OpenReadyAsync(IBrowser browser, string url, int width = 1280, int height = 800)
        {
            var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
            await page.GotoAsync(url);
            await page.WaitForFunctionAsync("() => window.nekoVideo && window.nekoVideo.isReady", null, new() { PollingInterval = 50, Timeout = 30000 });
            return page;
        }

        [Test]
        public void VideoPage_IsKeptOutOfTheSearchIndex()
        {
            var search = File.ReadAllText(Path.Combine(_outDir, "search.json"));
            Assert.That(search, Does.Not.Contain("Demo film"));
            Assert.That(File.Exists(Path.Combine(_outDir, "videos", "demo.html")), Is.True);
        }

        [Test]
        public async Task Player_IsReady_WithTheTimelineTheBuildComputed()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl());
                var duration = await page.EvaluateAsync<double>("() => window.nekoVideo.duration");
                // 2 + 2.5 + 2, less two 0.6 s overlaps.
                Assert.That(duration, Is.EqualTo(5.3).Within(1e-6));
                Assert.That(await page.EvaluateAsync<int>("() => window.nekoVideo.fps"), Is.EqualTo(12));
                Assert.That(await page.Locator(".nv-scene").CountAsync(), Is.EqualTo(3));
                Assert.That(await page.Locator("#nv-bar").IsVisibleAsync(), Is.True, "the preview has its control bar");
                Assert.That(await page.Locator(".nv-tick").CountAsync(), Is.EqualTo(2), "the scrubber marks every scene after the first");

                await page.EvaluateAsync("() => window.nekoVideo.seek(0.5)");
                Assert.That(await page.Locator(".nv-scene.is-on").CountAsync(), Is.EqualTo(1));
                Assert.That(await page.Locator(".nv-scene.is-on").GetAttributeAsync("id"), Is.EqualTo("scene-1"));

                // Inside the push from scene 1 to scene 2 both are on screen.
                await page.EvaluateAsync("() => window.nekoVideo.seek(1.6)");
                Assert.That(await page.Locator(".nv-scene.is-on").CountAsync(), Is.EqualTo(2));

                await page.EvaluateAsync("() => window.nekoVideo.seek(3.0)");
                Assert.That(await page.Locator(".nv-scene.is-on").GetAttributeAsync("id"), Is.EqualTo("scene-2"));

                // The last frame belongs to the last scene.
                await page.EvaluateAsync("() => window.nekoVideo.seek(window.nekoVideo.duration)");
                Assert.That(await page.Locator(".nv-scene.is-on").GetAttributeAsync("id"), Is.EqualTo("scene-3"));
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task Counter_CountsUp_AndLandsOnItsValue()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl());
                var counter = page.Locator("#scene-2 .nv-num");

                await page.EvaluateAsync("() => window.nekoVideo.seek(1.45)");
                var early = int.Parse((await counter.TextContentAsync())!.Replace(",", ""));
                await page.EvaluateAsync("() => window.nekoVideo.seek(3.7)");
                Assert.That(await counter.TextContentAsync(), Is.EqualTo("1,250"));
                Assert.That(early, Is.LessThan(1250));
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task SameTime_PaintsTheSameFrame_InAnyOrder()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl("#render"), 640, 360);
                await page.EvaluateAsync("() => window.nekoVideo.seek(0.8)");
                var first = await page.ScreenshotAsync();
                await page.EvaluateAsync("() => window.nekoVideo.seek(4.2)");
                await page.EvaluateAsync("() => window.nekoVideo.seek(0.2)");
                await page.EvaluateAsync("() => window.nekoVideo.seek(0.8)");
                var second = await page.ScreenshotAsync();

                Assert.That(second, Is.EqualTo(first), "seek is a pure function of time");
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task BrandArt_IsPainted_AndNothingBlurs()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl("#render"), 640, 360);
                await page.EvaluateAsync("() => window.nekoVideo.seek(1.2)");

                Assert.That(await page.EvaluateAsync<string>("() => document.body.dataset.motion"), Is.EqualTo("brand"));

                // The cover's field canvas has drawn something other than one flat colour.
                var tones = await page.EvaluateAsync<int>(@"() => {
                    const c = document.querySelector('#scene-1 canvas.nv-paint');
                    const d = c.getContext('2d').getImageData(0, 0, c.width, c.height).data;
                    const seen = new Set();
                    for (let i = 0; i < d.length; i += 4 * 37) seen.add(d[i] + ',' + d[i + 1] + ',' + d[i + 2] + ',' + d[i + 3]);
                    return seen.size;
                }");
                Assert.That(tones, Is.GreaterThan(1));

                for (var t = 0.2; t < 2; t += 0.3)
                {
                    await page.EvaluateAsync($"() => window.nekoVideo.seek({t.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
                    var blurred = await page.EvaluateAsync<int>("() => [...document.querySelectorAll('#scene-1 [data-v]')].filter(e => (e.style.filter || '').includes('blur')).length");
                    Assert.That(blurred, Is.Zero, $"nothing blurs at {t:0.0}s");
                }
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task RenderMode_StripsTheChrome_AndSetsTheCanvasAtOneToOne()
        {
            var (pw, browser) = await LaunchAsync();
            try
            {
                var page = await OpenReadyAsync(browser, PageUrl("#render"), 640, 360);
                Assert.That(await page.Locator("#nv-bar").IsVisibleAsync(), Is.False);
                Assert.That(await page.Locator("#nv-back").IsVisibleAsync(), Is.False);
                var box = await page.Locator("#nv-stage").BoundingBoxAsync();
                Assert.That((box!.X, box.Y, box.Width, box.Height), Is.EqualTo((0f, 0f, 640f, 360f)));
            }
            finally
            {
                await browser.CloseAsync();
                pw.Dispose();
            }
        }

        [Test]
        public async Task NekoRender_WritesAStill_AndAnMp4()
        {
            // The renderer launches its own browser: make sure one is reachable first.
            var (pw, browser) = await LaunchAsync();
            await browser.CloseAsync();
            pw.Dispose();

            var source = Path.Combine(_inputDir, "videos", "demo.md");
            var still = Path.Combine(_outDir, "still.png");
            var exit = await VideoRenderer.RunAsync(new VideoRenderSettings
            {
                Input = source,
                Output = still,
                Still = 3.9,
                Browser = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM"),
            });
            Assert.That(exit, Is.EqualTo(0));
            Assert.That(File.Exists(still), Is.True);
            var png = File.ReadAllBytes(still);
            Assert.That(png.Take(4).ToArray(), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));

            if (FfmpegLocator.Find() == null)
            {
                Assert.Ignore("ffmpeg is not installed; the still was checked, the MP4 encode was not.");
            }

            var mp4 = Path.Combine(_outDir, "demo.mp4");
            exit = await VideoRenderer.RunAsync(new VideoRenderSettings
            {
                Input = source,
                Output = mp4,
                Quality = "draft",
                Workers = 2,
                Browser = Environment.GetEnvironmentVariable("NEKO_TEST_CHROMIUM"),
            });
            Assert.That(exit, Is.EqualTo(0));
            var bytes = File.ReadAllBytes(mp4);
            Assert.That(bytes.Length, Is.GreaterThan(1000));
            Assert.That(System.Text.Encoding.ASCII.GetString(bytes, 4, 4), Is.EqualTo("ftyp"), "an MP4 container");
        }
    }
}
