---
name: video
description: Author a video as a single Markdown file (`video:` front matter) — scenes separated by horizontal rules, every block animating in on its own, video components (lockup, swap, typewriter, scramble, counter, stats, chart, code, terminal, browser/window/phone, cursor, lower-third, captions, quote, steps, checklist, grid/card, cta, logo, media), scene transitions and camera moves, and a synthesized voice-over (`--- {narration}` parts, `narration:` voice, `neko voiceover`) — preview it in the browser and render it to MP4 with `neko render` (Playwright + ffmpeg). Also covers the `[!video]` component that embeds the rendered file in a page. Use when asked for a video, an animated explainer, a product or release teaser, a changelog video, a motion graphic, a narrated walkthrough, or an MP4 from docs content.
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

Other keys: `transitionDuration` (0.6), `hold` (1.6 s still after the last element; a
scene whose `duration` would cut its content short is lengthened to last arrival + `hold`),
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
![Screenshot](/assets/shot.png)
:::

::: cursor {from="105%,95%" to="52%,58%" label="Click" at="1.5"}
:::

::: cursor {on="screen" from="104%,96%" to="63.6%,12.9%" label="XML" at="1.2"}
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

A screenshot in `browser`/`window`/`phone` arrives with its frame and never animates
on its own (no `kenburns` inside a frame: it grows the picture inside fixed chrome);
a frame holding one screenshot takes its proportions, so nothing is cropped. Put
dark screenshots on `ground="ink"`: in curiosity the chrome follows the ground. Move
the whole scene with `camera` instead.

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

## Voice-over

Narration is written as text in the same file and spoken by Voxwerk (Qwen3-TTS
VoiceDesign, pure C#). It is a **separate track on the video's clock**: it never
moves the animation, and its lines never overlap each other.

```yaml
video:
  narration:
    voice: "A warm, clear female narrator in her thirties, calm and friendly."  # a description…
    # voice: en-gb-rupert-vale   # …or a Voxwerk catalog id (`neko voices --search calm`):
    #                            # cloned from its reference sample, one speaker throughout
    #                            # (a description is re-designed per sentence and drifts)
    language: english            # optional; seed: 7, gap: 0.5, pause: 0.25, volume: 1, wpm: 150
    voices:                      # optional named voices for lines
      guest: "An elderly man with a slow, raspy, gentle voice."
```

```markdown
--- {id="opening" layout="title"}

# Say it *in Markdown*

--- {id="results"}

## Revenue *doubled*

Up from 12 to 26 in one quarter. {#bars}

--- {narration}

# Headings are labels, not spoken. Each paragraph or list item is one line.

{at="opening+0.6"}
Every Neko video can carry a voice-over.

Revenue doubled in the second quarter. {at="#bars" voice="guest"}

And this line simply follows the one before.
```

- `--- {narration}` (or `{voiceover}`) marks a part as narration, not a scene;
  put parts anywhere (after each scene, or one at the end). The rule may carry
  defaults: `--- {narration voice="guest" at="scene-4"}`.
- Line attributes (a `{…}` at the end of the line or on its own line before it):
  `at`, `voice`, `language`, `seed`, `pause` (between its sentences), `gap`
  (after the line before), `volume`.
- `at`: `12.5` / `1:04.2` (video time), `+0.8` (after the line before ends),
  `scene-3` or a scene `id` (when its transition in is over), `#id` (when that
  element enters: `{#id}` on any block, list item or heading), plus an offset:
  `scene-3+1.2`, `#bars-0.3`. No `at` → follows the line before after `gap`.
- A line anchored while the previous is still speaking **waits** (and is reported
  as *shifted*). If the voice-over outlasts the last scene, that scene holds.
- **Scenes fit their narration**: a scene with no `duration` lasts until every line
  anchored to it (or to an element in it) has been spoken, plus `tail` (0.8 s,
  a `narration:` option), before the next transition. So in a narrated video,
  anchor each line to its scene (`at="scene-id+0.4"`) and leave `duration` out;
  `fit: false` turns this off.

Workflow — always preview before synthesizing:

```bash
neko voiceover videos/launch.md --preview   # timing sheet: scenes, animation, sentences (~ = estimated)
neko voiceover videos/launch.md             # synthesize missing sentences → assets/voiceover/launch/*.ogg
neko render videos/launch.md                # mixes the takes (synthesizes anything missing first)
```

In the browser preview, **Timing** (or <kbd>T</kbd>, `#timing`) shows the lanes;
the current sentence shows as a subtitle (<kbd>C</kbd>). Align by anchoring every
line to its scene and letting Neko fit the scenes, then moving anchors or shortening
lines until nothing is *shifted* or *runs into scene N*. Commit the `.ogg` takes with the page; they are keyed by
voice + text, so editing one sentence re-speaks only that one. The first
synthesis downloads a ~4.5 GB model; it runs about 5× slower than real time on CPU.

## Rendering

```bash
neko render videos/launch.md                    # → videos/launch.mp4
neko render videos/launch.md -o out.mp4 -q high # draft | standard | high
neko render videos/launch.md --still 3.5        # one PNG (poster, checking a frame)
neko render videos/ -o out/ --from 4 --to 9 --workers 4 --format webm
neko render videos/launch.md --preview          # the timing sheet only; --no-voiceover skips synthesis
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
- With a voice-over, write the script first, anchor each line to its scene, leave
  `duration` out so scenes fit their lines, and check `neko voiceover --preview`;
  keep lines to one or two sentences per scene.
- **Let the last state be read.** An animation that ends as the scene leaves is
  never seen (a swap's last word, a chart's last bar). Neko holds every scene at
  least a second after its last arrival; give a scene that needs reading more
  (`hold="2.5"`), and use `{delay="1"}` on an element to pause *before* it.
- **A swap needs room**: its words take `hold` × words seconds; a short line of
  narration over it does not make the scene long enough by itself.
- **Click on something.** On a screenshot, write the cursor with `on="screen"` and
  aim it in the picture's own coordinates (pixel ÷ image size), at the control the
  narration names; start it after the frame settles (`at` ≥ 1). Check the landing
  point with `neko render --still`.
- **Stats in the curiosity theme**: `::: stats {class="cards" count="4"}` steps any
  number of cards down a 180 px stair; keep values short enough for the column.
