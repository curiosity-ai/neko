---
title: Write Markdown. Get video.
description: The Neko video mode sample, in the curiosity theme — the Curiosity brand's live field, pixel glyphs, drawn tiles, staircase, pixel window, the mark's cube and Sudo, from one Markdown file, rendered to MP4 with neko render.
video:
  theme: curiosity
  size: 1920x1080
  fps: 30
  transition: push
  intro: squares
  back: /videos/videos
  narration:
    voice: "A confident, professional American man in his early forties with a warm, resonant baritone and a clear General American accent, speaking at a steady, measured pace, like the narrator of a polished technology product film."
    language: english
---

--- {layout="cover" art="field" eyebrow="Neko · Video mode" duration="5"}

# Write *Markdown.* Get *video.*

One file. Scenes split by a rule. Rendered to MP4 by headless Chromium and ffmpeg.

--- {layout="statement" ground="ink" eyebrow="One tool"}

::: swap {prefix="Neko builds" words="docs|decks|documents|video" suffix="." tag="h2"}
:::

The same Markdown, the same components, the same `neko watch` loop.

--- {layout="split" eyebrow="The source"}

## A scene is *Markdown*

Every block moves on its own. Headlines rise word by word, lists stagger in.

::::: code {file="launch.md" speed="0.02"}
```markdown
--- {layout="number" ground="deep"}

::: counter {value="98%" label="Ready to ship"}
:::

--- {transition="zoom"}

## Every block *animates*
```
:::::

--- {layout="number" ground="deep" transition="zoom" eyebrow="Frames" art="squares" duration="6.3"}

::: counter {value="1,905" label="Frames in this film" count-dur="1.4"}
Sixty-three seconds at thirty frames a second, each one a screenshot.
:::

--- {eyebrow="The pixel vocabulary" duration="5.6"}

## Glyphs that *build themselves*

::: glyphs {names="value-connected|value-resolved|value-traceable|value-sovereign" labels="Connected|Resolved|Traceable|Sovereign"}
:::

--- {ground="stone" eyebrow="Drawn live" layout="full" duration="4.6"}

## Every tile is a *function of time*

::::: grid {count="3"}
::: tile {art="mountain" label="Mountain" height="300px"}
:::
::: tile {art="wave" label="Wave" height="300px"}
:::
::: tile {art="join-grid" label="Join" height="300px"}
:::
:::::

--- {layout="full" eyebrow="Render time" transition="push"}

::: chart {type="bar" title="Seconds to render, by scene" unit="s" caption="1080p · 30 fps · one Chromium worker"}
- Cover: 3.1
- Swap: 2.4
- Code: 4.2
- Counter: 2.2
- Chart: 3.6 *
- Terminal: 3.4
:::

--- {layout="full" ground="stone" eyebrow="Rendering" duration="5.6"}

::: terminal {title="neko render"}
```bash
$ neko render videos/samples/neko-video.md -o neko-video.mp4
Building the page… 13 scenes, 63.5 s
Capturing 1,905 frames with Chromium (4 workers)
✓ Frames captured
Encoding with ffmpeg · libx264 · crf 18 · yuv420p
✓ Wrote neko-video.mp4
```
:::

--- {layout="full" eyebrow="The preview" camera="push" duration="4.6"}

::: browser {url="localhost:5000/videos/samples/neko-video"}
![The Neko site](/assets/screenshots/neko-screenshot.png)
:::

::: cursor {from="105%,95%" to="52%,58%" label="Scrub any frame" at="1.6"}
:::

--- {layout="split" eyebrow="Describe · Ground · Ship" transition="push"}

## From a sentence to a *film*

::: steps {class="stair"}
1. **Write** the scenes in one Markdown file.
2. **Preview** them frame by frame with neko watch.
3. **Render** the MP4 with neko render.
:::

--- {ground="ink" eyebrow="Security" art="mountain" scheme="ink" duration="6.3"}

## Built for *your infrastructure*

Neko renders on your machine, with your fonts, your browser and your ffmpeg. Nothing leaves the build.

--- {ground="ink" layout="full" eyebrow="In production" transition="zoom"}

::::: stats {count="3" class="cards"}
::: stat {value="30TB+" label="Data connected"}
:::
::: stat {value="20,000+" label="Active users"}
:::
::: stat {value="15%" label="Efficiency gain"}
:::
:::::

--- {layout="center" transition="zoom-out" duration="8.2"}

::: mark {cube="true" size="300px"}
:::

::: wordmark {text="curiosity" tagline="Docs, decks, documents and video, from Markdown."}
:::

::: sudo {clip="workIdle" size="96px" class="corner"}
:::

--- {narration}

# The voice-over: one line per scene, each anchored to its scene.

{at="scene-1+1.3"}
Write Markdown. Get video.

{at="scene-2"}
Neko builds docs, decks, documents, and now, video.

{at="scene-3"}
A scene is just Markdown. Every block animates on its own.

{at="scene-4"}
Every frame is a screenshot, taken by headless Chromium.

{at="scene-5"}
Pixel glyphs build themselves, frame by frame.

{at="scene-6"}
Every tile is drawn live, as a function of time.

{at="scene-7"}
Charts grow straight from a Markdown list.

{at="scene-8"}
One command renders the film. FFmpeg does the encoding.

{at="scene-9"}
Preview it in the browser, and scrub to any frame.

{at="scene-10"}
Write it. Preview it. Render it.

{at="scene-11"}
It runs on your own infrastructure. Nothing leaves the build.

{at="scene-12"}
And it is already in production.

{at="scene-13+0.4"}
Neko, by Curiosity. Docs, decks, documents and video, from Markdown.
