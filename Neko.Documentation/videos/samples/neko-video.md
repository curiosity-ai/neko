---
title: Write Markdown. Get video.
description: The Neko video mode sample — nine scenes in one Markdown file, in the curiosity theme, rendered to MP4 with neko render.
video:
  theme: curiosity
  size: 1920x1080
  fps: 30
  transition: push
  back: /videos/videos
---

--- {layout="cover" art="field" eyebrow="Neko · Video mode"}

# Write *Markdown.* Get *video.*

One file. Scenes split by a rule. Rendered to MP4 by headless Chromium and ffmpeg.

--- {layout="statement" ground="ink" eyebrow="One tool"}

::: swap {prefix="Neko builds" words="docs|decks|documents|video" suffix="." tag="h2"}
:::

The same Markdown, the same components, the same `neko watch` loop.

--- {layout="split" eyebrow="The source"}

## A scene is *Markdown*

Every block moves on its own. Headings rise word by word, lists stagger in.

::::: code {file="launch.md" speed="0.022"}
```markdown
--- {layout="number" ground="deep"}

::: counter {value="98%" label="Ready to ship"}
:::

--- {transition="zoom"}

## Every block *animates*
```
:::::

--- {layout="number" ground="deep" transition="zoom" eyebrow="Frames"}

::: counter {value="900" label="Frames in this film" count-dur="1.8"}
Thirty seconds at thirty frames a second, each one a screenshot.
:::

--- {layout="full" eyebrow="Render time" transition="push"}

::: chart {type="bar" title="Seconds to render, by scene" unit="s" caption="1080p · 30 fps · one Chromium worker"}
- Cover: 3.1
- Swap: 2.4
- Code: 4.2
- Counter: 2.2
- Chart: 3.6 *
- Terminal: 3.4
:::

--- {layout="full" ground="stone" eyebrow="Rendering"}

::: terminal {title="neko render"}
```bash
$ neko render videos/samples/neko-video.md -o neko-video.mp4
Building the page… 9 scenes, 31.4 s
Capturing 942 frames with Chromium (4 workers)
✓ Frames captured
Encoding with ffmpeg · libx264 · crf 18 · yuv420p
✓ Wrote neko-video.mp4 (6.2 MB)
```
:::

--- {layout="full" eyebrow="The preview" camera="push"}

::: browser {url="localhost:5000/videos/samples/neko-video"}
![The Neko site](/assets/screenshots/neko-screenshot.png)
:::

::: cursor {from="105%,95%" to="52%,58%" label="Scrub any frame" at="1.6"}
:::

--- {layout="split" ground="ink" transition="fade-through" eyebrow="Components"}

## Built from *what you know*

::: checklist
- Titles, swaps and typewriters
- Counters, stats and charts
- Code, terminals and devices
- Cursors, captions and lower thirds
:::

--- {layout="closing" art="squares" transition="zoom-out"}

::: cta {button="neko render video.md" url="neko.curiosity.ai/videos"}
# Your docs *can move.*
:::
