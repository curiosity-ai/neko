---
title: Video structure
label: Video structure
description: The video front matter, the scene separator, per-scene options, and how Neko schedules every element on a scene's clock.
order: 2
icon: file-code
---

# Video structure

A video is one Markdown file. Front matter marks it as a video and sets
video-wide defaults; a horizontal rule starts each scene.

## The marker

```yaml
---
title: Release notes, October
video: true
---
```

`video: true` takes every default. For video-wide options, make it a mapping:

```yaml
---
title: Release notes, October
video:
  theme: curiosity      # neko (default) or curiosity
  size: 1920x1080       # or 720p, 4k, vertical, square
  fps: 30
  transition: push      # the transition into every scene that names none
  music: assets/bed.mp3 # mixed under the whole video, faded out at the end
---
```

### Video options

| Key | Default | Meaning |
| --- | --- | --- |
| `theme` | `neko` | `neko` (dark, Inter) or `curiosity` (the Curiosity brand). See [Video themes](video-themes.md). |
| `size` | `1920x1080` | Canvas in pixels — `1280x720` — or a name: `720p`, `1080p`, `4k`, `vertical` (1080×1920), `square`. |
| `ratio` | — | `16:9`, `9:16`, `1:1`, `4:5`: the canvas keeps 1080 pixels on its short side. |
| `fps` | `30` | Frames per second of the render. The preview plays at the display's rate. |
| `transition` | `fade` (neko), `push` (curiosity) | The transition into every scene that does not set its own. |
| `transitionDuration` | `0.6` | Seconds a transition overlaps the two scenes it joins. |
| `hold` | `1.6` | Seconds a scene holds still after its last element arrives, when Neko sizes it. |
| `pace` | `normal` | `brisk`, `normal`, `calm`, or a number: multiplies every automatic gap between elements. |
| `accent` | `cyan` | The neko theme's accent: `cyan`, `violet`, `amber`, `rose`, `leaf`. |
| `eyebrow` | — | Eyebrow for scenes that don't set one. |
| `music` | — | Background track. `musicVolume` (0–1, default `0.35`) sets its level. |
| `logo` / `logoText` | — | A brand mark pinned to the corner of every frame. |
| `grain` / `vignette` | `false` | Film grain or a vignette over the whole canvas. |
| `intro` | — | curiosity: `squares` or `tiles` plays the website's first-load animation over the first scene. |
| `foot` | `true` | The scene mark and number the curiosity theme draws at the foot. |
| `autoplay` / `loop` | `false` | Preview behaviour only. |
| `back` / `backText` | referrer | Where the preview's back link goes. |

## Scenes

Every `---` on its own line, after a blank line, starts a scene. The rule can
carry the scene's options in Neko's attribute syntax:

```markdown
--- {layout="split" ground="ink" transition="zoom" duration="5"}

## A scene is *Markdown*

Every block in it moves on its own.
```

A rule before the first scene carries the first scene's options. Rules inside
fenced code are ignored, and a setext heading (`Title` over `---`) still works.

### Scene options

| Key | Meaning |
| --- | --- |
| `duration` | Seconds on screen, transition in included. Leave it out and Neko sizes the scene: its last element's arrival plus `hold`. |
| `transition` | How this scene comes in: `cut`, `fade`, `fade-through`, `push`, `push-up`, `zoom`, `zoom-out`, `wipe`, `iris`, `blur`. See [Motion](video-motion.md#scene-transitions). |
| `transition-duration` | Seconds of overlap with the scene before. |
| `layout` | `title`, `center`, `split`, `full`, `bottom`, `top`, `bleed`, and the curiosity layouts `cover`, `section`, `panel`, `statement`, `number`, `closing`. A scene that opens on `# Heading` is a `title`. |
| `ground` | The scene's surface: `paper`, `stone`, `ink`, `slate`, `deep` (and `accent` in neko). |
| `art` / `background` | Generated art — `field`, `bars`, `squares`, `stair`, `glyph`, and in the curiosity theme the painted mosaics and story tiles (`wave`, `mountain`, `join-grid`, … see [Art](video-themes.md#art)) — drawn in from its signal point, or a CSS ground: `grid`, `dots`, `glow`, `aurora`. |
| `intro` | curiosity: `squares` or `tiles` — the first-load animation over this scene. |
| `camera` | A slow move across the whole scene: `push`, `pull`, `pan-left`, `pan-right`, `rise`. |
| `eyebrow` | The small label at the top of the scene. |
| `align` | `center` centres the content. |
| `accent` | The scene's own accent (neko). |
| `pace` / `hold` | Per-scene versions of the video options. |
| `voice` | An audio file for this scene (a voice-over line), starting as the scene settles; `voice-at` moves it, `voice-volume` sets its level. |
| `wash` | The colour a `fade-through` passes through. |
| `id` / `class` | The scene element's id and extra classes. |

## How Neko schedules a scene

Nothing in a scene needs a time. Neko walks it in document order and gives every
element that moves a start on one running **cursor**:

- the cursor starts a beat after the scene begins (half way through its
  transition in, so the content lands as the cut does);
- each element starts at the cursor and moves it on by the element's **gap** —
  about a quarter of a second for a paragraph, a sixth for a list item,
  longer for a counter or a typed line;
- a headline is split into words, each starting 0.06 s after the one before;
- the scene ends when the last element has arrived, plus `hold`.

So plain Markdown already animates — headings rise word by word, paragraphs and
quotes rise, list items and table rows stagger in, images settle — and the
order you write things in is the order they arrive.

When you want to take over, any block takes timing attributes in Markdig's
attribute syntax — at the end of a heading, a paragraph or a list item, or on
its own line just before a block:

```markdown
# The headline {enter="slam"}

A line that waits for its moment. {at="2.4"}

{enter="left" dur="0.8"}
A paragraph that slides in from the left.

- A list item that only fades {enter="fade"}

![A product shot](shot.png){enter="kenburns"}
```

| Attribute | Meaning |
| --- | --- |
| `enter` | The motion (see [Motion](video-motion.md)). `none` holds the block still. |
| `at` | Start, in seconds from the scene's start (`2.4`), from the cursor (`+0.5`), or `with` — together with the element before. |
| `dur` | How long the motion takes. |
| `delay` | Extra seconds before this element. |
| `gap` | Seconds this element holds the cursor before the next one starts. |
| `ease` | `power1`–`power4`, `expo`, `sine` with `.in`, `.out` or `.inOut`; `back.out(1.6)`; `linear`. |
| `exit` | How it leaves: `fade`, `up`, `down`, `left`, `right`, `blur`, `shrink`. |
| `until` | When it leaves, in scene seconds. |
| `stagger` | Seconds between words, for a `words` or `letters` motion. |

Containers take the same attributes: `::: card {enter="pop" at="1.2"}`.

## Nesting containers

Components nest the way Neko containers always do: the outer fence needs more
colons than the inner one, or Markdig closes the outer container at the first
inner `:::`.

```markdown
::::: stats {count="3"}
::: stat {value="1,062" label="Frames"}
:::
::: stat {value="35.4s" label="Running time"}
:::
:::::
```

## Audio

The `music` track plays under the whole video and fades out over its last two
seconds; each scene's `voice` starts as that scene settles. The preview plays
them with the playhead; `neko render` mixes the same files with ffmpeg at the
same times. Files resolve like images: next to the page, or from the nearest
`assets/` folder.

## A standalone page

A video page carries everything it needs: the theme's fonts, icons, highlight.js
and the runtime are inlined, and local images become data URIs. A
`password:` works as on any page — the scenes are encrypted and the player
unlocks them; `neko render` always films the content.
