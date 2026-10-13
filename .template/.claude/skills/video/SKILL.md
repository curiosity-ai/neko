---
name: video
description: Author a video as a single Markdown file (`video:` front matter) — scenes separated by horizontal rules, every block animating in on its own, video components (lockup, swap, typewriter, scramble, counter, stats, chart, code, terminal, browser/window/phone, cursor, lower-third, captions, quote, steps, checklist, grid/card, cta, logo, media), scene transitions and camera moves — preview it in the browser and render it to MP4 with `neko render` (Playwright + ffmpeg). Also covers the `[!video]` component that embeds the rendered file in a page. Use when asked for a video, an animated explainer, a product or release teaser, a changelog video, a motion graphic, or an MP4 from docs content.
---

# Video mode

A Neko video is **one Markdown file**. Front matter marks it as a video; a
horizontal rule (`---`) starts each **scene**. Every block in a scene animates in
by itself, in the order it is written; components cover what Markdown can't
(numbers counting up, charts growing, code typing, a cursor clicking). The page
is a preview player; `neko render` films it to MP4.

## The marker

```yaml
---
title: October release
video:
  theme: curiosity     # neko (default, dark) or curiosity (the Curiosity brand)
  size: 1920x1080      # 720p, 1080p, 4k, vertical (1080x1920), square; or ratio: 9:16
  fps: 30
  transition: push     # into every scene that names none (neko: fade, curiosity: push)
  music: assets/bed.mp3   # optional; musicVolume: 0.35
---
```

Other keys: `transitionDuration` (0.6), `hold` (1.6 s still after the last element),
`pace` (`brisk`/`normal`/`calm` or a number), `accent` (neko: cyan, violet, amber,
rose, leaf), `eyebrow`, `logo`/`logoText`, `grain`, `vignette`, `foot`, `autoplay`,
`loop`, `back`/`backText`, and (curiosity) `intro: squares|tiles` — the website's
first-load animation over the first scene.

## Scenes

```markdown
--- {layout="cover" art="field" eyebrow="Neko · Video mode"}

# Write *Markdown.* Get *video.*

One file. Scenes split by a rule.

--- {layout="split" ground="ink" transition="zoom" duration="5"}

## A scene is *Markdown*

- Headings rise word by word
- Paragraphs rise
- List items stagger in
```

A `---` needs a blank line before it. A rule before the first scene carries the
first scene's options. Rules inside fenced code are ignored.

Scene options (on the rule):

| Key | Values |
| --- | --- |
| `duration` | seconds; omit and Neko sizes the scene (last arrival + `hold`) |
| `transition` | `cut`, `fade`, `fade-through`, `push`, `push-up`, `zoom`, `zoom-out`, `wipe`, `iris`, `blur` |
| `transition-duration` | seconds of overlap with the previous scene |
| `layout` | `title`, `center`, `split`, `full`, `bottom`, `top`, `bleed`; curiosity also `cover`, `section`, `panel`, `statement`, `number`, `closing` |
| `ground` | `paper`, `stone`, `ink`, `slate`, `deep` (neko also `accent`) |
| `art` / `background` | `field`, `bars`, `squares`, `stair`, `glyph` (+ `glyph="graph"`, `seed`), or `grid`, `dots`, `glow`, `aurora` |
| `camera` | `push`, `pull`, `pan-left`, `pan-right`, `rise` |
| `eyebrow`, `align="center"`, `accent`, `pace`, `hold`, `wash`, `id`, `class` | |
| `voice` | audio file for the scene (`voice-at`, `voice-volume`) |

`*emphasis*` in an `#`/`##` heading is the accent colour (the second tone), not
italic. `==text==` sweeps a highlight under the words.

## Timing — leave it to Neko first

Neko schedules every element on one cursor in document order (the timeline
compiler, at build time). Only reach for attributes when a beat needs it:

```markdown
# The headline {enter="slam"}

A line that waits. {at="2.4"}

{enter="left" dur="0.8"}
A paragraph that slides in.

- An item that only fades {enter="fade"}

![Screenshot](shot.png){enter="kenburns"}
```

| Attribute | Meaning |
| --- | --- |
| `enter` | motion: `rise`, `words`, `letters`, `fade`, `up`, `drop`, `left`, `right`, `pop`, `zoom`, `settle`, `blur`, `slam`, `wipe`, `wipe-up`, `grow`, `grow-x`, `draw`, `type`, `scramble`, `sweep`, `kenburns`, `none` |
| `at` | seconds (`2.4`), relative to the cursor (`+0.5`), or `with` (same time as the previous element) |
| `dur`, `delay`, `gap`, `stagger` | duration, extra wait, cursor hold, word/letter spacing |
| `ease` | `power1`–`power4`, `expo`, `sine` + `.in`/`.out`/`.inOut`, `back.out(1.6)`, `linear` |
| `exit` + `until` | leave early: `fade`, `up`, `down`, `left`, `right`, `blur`, `shrink` at `until` seconds |

Containers take the same attributes on their fence: `::: card {enter="pop" at="1"}`.

## Components

Nest with longer outer fences (`:::::` around `:::`), and use `::::` around a
`code` block whose code contains `:::`.

```markdown
::: lockup {kicker="Kicker" label="Label under the rule"}
# Title *card*
:::

::: swap {prefix="Neko builds" words="docs|decks|video" suffix="." hold="0.9"}
:::

::: typewriter
neko render video.md
:::

::: scramble
DECODED LINE
:::

::: callout
30 fps.
:::

::: counter {value="$1,250" label="Label"}
Optional line.
:::

::::: stats {count="3"}
::: stat {value="98%" label="Uptime"}
:::
::: stat {value="3x" label="Faster"}
:::
:::::

::: chart {type="bar" title="Title" unit="s" caption="Source"}
- Mon: 12
- Tue: 18 *
:::
```

`chart` types: `bar` (default), `hbar`, `line`, `donut`. Data is `- Label: value`
bullets or a two-column table; `*` after a value calls it out in the accent.

````markdown
:::: code {file="Program.cs" speed="0.025"}
```csharp
app.Run();
```
::::

::: terminal {title="zsh"}
```bash
$ neko render video.md
✓ Wrote video.mp4
```
:::

::: browser {url="example.com"}
![Screenshot](/assets/shot.png)
:::

::: phone
![Screenshot](/assets/shot.png){enter="kenburns"}
:::

::: cursor {from="105%,95%" to="52%,58%" label="Click" at="1.5"}
:::

::: lower-third {name="Ada Lovelace" role="Engineer" at="0.8" until="4.5"}
:::

::: captions {style="pill"}
0.5-2.4 First caption line, timed on the scene clock
2.5-4.4 and the second.
:::

::: quote {author="Name" role="Role"}
The quote, read in word by word.
:::

::: steps
1. **Write** the scenes.
2. **Render** the MP4.
:::

::: checklist
- Ticked as it arrives
:::

::::: grid {count="3"}
::: card {title="Title" icon="bolt"}
Text.
:::
:::::

::: cta {button="Get started" url="example.com"}
# The call to *action*
:::

::: logo {src="logo.svg" text="Brand" tagline="Tagline"}
:::

::: media {src="assets/clip.webm" start="0"}
:::
````

Also: `window {title}`, `cols`/`col`, `box`, `stack`, `reveal {enter at}`, `logos`
(a row of images). Captions styles: `rail`, `pill`, `highlight`. `media` clips
are muted and seeked frame by frame; prefer WebM.

## Curiosity brand components

In `theme: curiosity` headlines rise word by word out of a mask, counters ease
out with no pulse and nothing blurs. Scene `art=` also takes the website's live
drawings: `field`, `squares`, the dash mosaics `wave|rows|chart|chart-grid|mountain|band|dither`
(with `scheme="signal|ink|deep|stone|paper"`) and the story tiles
`step-grid|join-grid|lattice|dot-chart`. Brand components:

````markdown
::: tile {art="mountain" label="Mountain" height="300px"}
:::

::: pixel-window {art="mountain"}
:::

::: glyphs {names="value-connected|value-resolved|value-traceable" labels="Connected|Resolved|Traceable"}
:::

::: glyph {name="k..*k...kkk....." size="160px" label="Custom"}
:::

::: sudo {clip="workIdle" class="corner"}
:::

::: mark {cube="true" size="300px"}
:::

::: wordmark {tagline="One line under the name."}
:::
````

`steps {class="stair"}` draws steps as a widening staircase; `stats {class="cards"}`
staggers the stats as paper, deep-blue and slate cards. Still glyphs build once and
hold (`loop="true"` repeats them). Keep Sudo in a corner: a detail, never the subject.

## Rendering

```bash
neko render videos/launch.md                    # → videos/launch.mp4
neko render videos/launch.md -o out.mp4 -q high # draft | standard | high
neko render videos/launch.md --still 3.5        # one PNG (poster, checking a frame)
neko render videos/ -o out/ --from 4 --to 9 --workers 4 --format webm
```

ffmpeg is found via `--ffmpeg`, `NEKO_FFMPEG`, the PATH, or downloaded once to
`~/.neko/ffmpeg`. Chromium comes from Playwright (installed on first use) or
`--browser`.

Embed the result in a normal page:

```markdown
[!video src="/assets/videos/launch.mp4" poster="/assets/videos/launch.png" link="/videos/launch" title="Launch"]
```

## Tips

- One idea per scene; 3–6 s per scene; 2–3 transition types per film.
- Prefer letting Neko time things; check with `--still` at a few moments.
- Holds are still: give a long scene a `camera` move or a later beat rather than idle motion.
- Keep one direction of travel (`push` always moves left); use `zoom` for "deeper", `zoom-out` for the payoff.
- Video pages never appear in the sidebar or search; link them or embed their render.
