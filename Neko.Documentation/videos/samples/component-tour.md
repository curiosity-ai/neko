---
title: Video components, a tour
description: Every Neko video component in one Markdown file, in the default neko theme.
video:
  theme: neko
  accent: cyan
  transition: fade
  back: /videos/video-components
---

--- {layout="title" background="aurora"}

::: lockup {kicker="Neko video mode" label="A tour of the components"}
# Motion, *from Markdown*
:::

--- {background="grid" transition="push"}

::: typewriter
neko render tour.md --quality high
:::

::: scramble
RENDERED FRAME BY FRAME
:::

--- {layout="title" transition="zoom"}

::::: stats {count="3"}
::: stat {value="1,062" label="Frames"}
:::
::: stat {value="35.4s" label="Running time"}
:::
::: stat {value="9" label="Scenes"}
:::
:::::

--- {layout="full" transition="push"}

::: chart {type="line" title="Weekly renders" caption="Sample data"}
- Mon: 12
- Tue: 18
- Wed: 15
- Thu: 26
- Fri: 31
- Sat: 22
- Sun: 40 *
:::

--- {layout="split" transition="push"}

## Where the *time* goes

Capture dominates; the encode is a rounding error.

::: chart {type="donut"}
- Capture: 82% *
- Encode: 12%
- Build: 6%
:::

--- {layout="full" transition="push"}

::: chart {type="hbar" title="Frames per second, by worker count" unit=" fps"}
- 1 worker: 6.1
- 2 workers: 11.4
- 4 workers: 19.7 *
- 6 workers: 21.2
:::

--- {background="glow" transition="blur"}

::: quote {author="A documentation team" role="Shipping weekly"}
We stopped exporting slides to screen recordings. The changelog video is a Markdown file now.
:::

--- {layout="split" transition="wipe"}

## Three steps

::: steps
1. **Write** scenes in one Markdown file.
2. **Preview** them in the browser with neko watch.
3. **Render** an MP4 with neko render.
:::

--- {transition="iris"}

::::: grid {count="3"}
::: card {title="Kinetic text" icon="text"}
Words rise, swap, type and decode.
:::
::: card {title="Data" icon="chart-histogram"}
Counters, stats and four kinds of chart.
:::
::: card {title="Frames" icon="browser"}
Browsers, phones, editors, terminals.
:::
:::::

--- {layout="split" transition="push"}

## On a *phone*

The same frame components hold screenshots at any size.

::: phone
![Neko](/assets/screenshots/neko-styled.png){enter="kenburns"}
:::

--- {ground="deep" transition="fade-through" duration="5"}

::: callout
30 fps.
:::

::: lower-third {name="Rendered by Neko" role="Playwright · ffmpeg · C#" at="1.2"}
:::

::: captions {style="pill"}
0.6-2.4 Every frame is a screenshot of the page
2.5-4.6 seeked to its exact time.
:::

--- {layout="title" transition="zoom-out"}

::: logo {text="Neko" tagline="Docs, decks, documents — and video."}
:::
