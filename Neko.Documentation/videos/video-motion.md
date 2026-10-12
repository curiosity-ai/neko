---
title: Motion and transitions
label: Motion and transitions
description: The motions any block in a scene can take, the transitions between scenes, camera moves, exits, easing, and the rules that keep a render deterministic.
order: 4
icon: bolt
---

# Motion and transitions

## Motions

Any block takes a motion with `{enter="…"}`; components choose their own and
take an `enter` to override it. Durations and eases are defaults — `dur` and
`ease` change them.

| Motion | What it does | Default |
| --- | --- | --- |
| `rise` | Fades up 40 px through a 6 px blur. The default for paragraphs, quotes and list items. | 0.6 s `power3.out` |
| `words` | Splits the text into words; each rises from half an em below, 0.06 s apart. The default for `#` and `##`. | 0.55 s per word |
| `letters` | The same, letter by letter, 0.035 s apart. | 0.45 s per letter |
| `fade` | Opacity only. | 0.5 s `power2.out` |
| `up` / `drop` | Fades up (or down) 40 px, no blur. | 0.6 s |
| `left` / `right` | Slides in 90 px from that side. | 0.6 s `power4.out` |
| `pop` | Scales from 0.85 with a small overshoot. | 0.55 s `back.out(1.6)` |
| `zoom` | Settles from 0.96. The default for an image. | 0.8 s `power3.out` |
| `settle` | Rises and settles from 0.965 — devices. | 0.9 s |
| `blur` | Comes into focus from 14 px of blur. | 0.7 s |
| `slam` | Drops from 1.6× and lands with a short shake. | 0.45 s `power4.out` |
| `wipe` / `wipe-up` | Revealed left to right (bottom to top) by a clip. | 0.7 s `power3.inOut` |
| `grow` / `grow-x` | Scales up from its base (left edge) — bars. | 0.8 s |
| `draw` | Draws an SVG stroke along its length. | 0.9 s |
| `type` | Types its text in behind a caret, 0.035 s per character. | — |
| `scramble` | Decodes from random glyphs, left to right. | 1 s |
| `count` | Counts a number up (used by counters). | 1.6 s `sine.inOut` |
| `sweep` | A highlight drawn under the text. The default for `==marks==`. | 0.6 s |
| `kenburns` | A slow push across the whole scene — screenshots and photos. | the scene |
| `none` | Holds still. | — |

Every animated element also gets a `--p` CSS custom property, its eased progress
from 0 to 1 — the checklist's tick is drawn with it, and your own CSS in
`_includes/head.html` can use it too.

## Timing rules of thumb

The defaults follow the motion rules HyperFrames' studio works by:

- **One entry is under a second.** Most are 0.45–0.8 s.
- **Similar elements share one motion and duration** — every list item, every bar.
- **Staggers stay short.** Words 0.06 s apart, list items 0.14 s, cards 0.14 s.
- **Holds are still.** Nothing breathes or floats while a scene holds: if a
  scene needs life, give it a `camera` move or a later beat (`at="+1"`).
- **The cut lands mid-motion.** A scene's content starts while its transition
  in is still running.

## Scene transitions

A transition overlaps the end of one scene with the start of the next for
`transition-duration` seconds (0.6 by default). Set it per scene with
`transition="…"`, or for the whole video in the front matter.

| Transition | What happens |
| --- | --- |
| `cut` | No overlap. |
| `fade` | The new scene fades in over the old one. The neko default. |
| `fade-through` | Out to a wash colour (`wash`, paper by default), then in. |
| `push` | **Cut on the curve**: the old scene accelerates out to the left, blurring; the new one arrives decelerating from the right, so the two read as one continuous move. The curiosity default. |
| `push-up` | The same, upwards. |
| `zoom` | Through the screen: the old scene scales up and blurs away, the new one settles from 0.75. Deeper into the same thought. |
| `zoom-out` | The inverse: the old scene shrinks away, the new one settles from 1.25. Arrival, payoff. |
| `wipe` | The new scene is revealed left to right as the old one slides back and softens. |
| `iris` | A circle opens from the centre. |
| `blur` | A rack focus: the old scene blurs out, the new one sharpens in. |

Pick two or three transitions for a film and repeat them; keep one direction of
travel (the curiosity theme's `push` always moves left) and save the others for
a reason.

## Camera moves

`camera` on a scene moves its whole content slowly across the scene's duration,
`sine.inOut`: `push` (scale to 1.06), `pull` (from 1.08 back to 1),
`pan-left`, `pan-right`, `rise`.

## Exits

Scenes leave through their transition, so most elements never need an exit.
When one should leave early — a lower third, a caption, a callout that makes
room — give it `until` (scene seconds) and optionally `exit`:

```markdown
::: lower-third {name="Ada Lovelace" role="Engineer" at="0.8" until="4.2" exit="left"}
:::
```

## Easing

`linear`, `power1` to `power4`, `expo` and `sine`, each with `.in`, `.out` or
`.inOut` (`power3.out`), and `back.out(s)` for an overshoot. The names are
GSAP's, so a timing found in a HyperFrames recipe carries over as written.

## Determinism

What makes a render repeatable:

- Every time is computed at **build time** by the timeline compiler and written
  into the page. The runtime is a pure function of time.
- The runtime has **no timers and no CSS animations**; nothing reads the clock
  while rendering. The scramble's random glyphs come from a hash of the frame.
- Fonts are **bundled and inlined**, and the render waits for them, for every
  image to decode and for every clip to load before the first frame.
- Clips are **seeked**, never played, during a render.

So frames can be captured in any order, by several pages at once, and the same
Markdown always renders the same video.
