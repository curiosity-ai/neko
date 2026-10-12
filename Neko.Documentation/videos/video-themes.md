---
title: Video themes
label: Video themes
description: The two video themes Neko ships — the dark neko look and the Curiosity brand system — with their grounds, layouts and generated art.
order: 5
icon: palette
---

# Video themes

```yaml
video:
  theme: curiosity   # or neko (the default)
```

`neko build --theme curiosity`, `neko watch --theme curiosity` and
`neko render --theme curiosity` re-theme every video in the run.

## neko

The default: a dark canvas set in Inter, with Geist Mono for labels, rounded
panels and one accent — `cyan` (default), `violet`, `amber`, `rose` or `leaf`,
per video (`accent:`) or per scene (`accent="…"`).

| Ground | Look |
| --- | --- |
| (default) | Near-black ink. |
| `deep` | Navy. |
| `slate` | A lighter dark. |
| `paper` / `stone` | Light. |
| `accent` | The accent itself, with dark type. |

Backgrounds: `background="aurora"`, `glow`, `grid` and `dots` — and the
generated art below works here too.

[!video src="/videos/assets/component-tour.mp4" poster="/videos/assets/component-tour-poster.jpg" link="/videos/samples/component-tour" title="The neko theme"]

## curiosity

The Curiosity brand system, the same one the
[curiosity deck theme](../presentations/deck-themes.md) and the
[whitepaper theme](../documents/document-themes.md) speak, set in motion:

- **paper and ink**, with stone, slate and ash, the **deep blue**, and one
  electric-blue **Signal** (`#2F6BFF`) — the second tone of a headline
  (`*line two*`), the called-out bar, the cursor's ring;
- **Schibsted Grotesk** for everything read, **Geist Mono** — small, uppercase —
  for labels, counters and the scene number;
- **hairlines, not shadows; square corners**, bar the pills;
- the brand furniture: the **eyebrow mark** (a square with a smaller one stepping
  out of it) and, at the foot, the **Escape mark** and the scene number;
- the **generated art** — the dash field, bars, squares, the stair and the 4 by 4
  glyphs — **drawn in from its signal point**, the Signal square popping last;
- `push` (cut on the curve) as the default transition, so the film keeps one
  direction of travel.

[!video src="/videos/assets/neko-video.mp4" poster="/videos/assets/neko-video-poster.png" link="/videos/samples/neko-video" title="The curiosity theme"]

### Grounds

`paper` (default), `stone`, `ink`, `slate` and `deep`. On `deep`, Signal would
not read, so the marks, highlights and the call to action take paper.

### Layouts

| Layout | Use |
| --- | --- |
| `cover` | The opening: the headline left, the lead right, the art a dark band across the foot. |
| `section` | A divider: big type bottom left, `art="stair"` or `art="bars"` behind. |
| `panel` | Text left, the art on a deep-blue panel to the right. |
| `statement` | One sentence, large, low on the frame. |
| `number` | One `counter`, huge, its label beside it and the Signal square on its shoulder. |
| `split` | The headline and lead left, the content (code, a chart, a list) right. |
| `full` | A heading over a chart, a terminal or a device that takes the frame. |
| `closing` | The last line bottom left, the art across the top. |
| `title` / `center` | Centred. |

### Art

| `art` | Drawing |
| --- | --- |
| `field` | A grid of short dashes bending round the signal square. |
| `bars` | Rows of short bars with a rising lit band. |
| `squares` | Squares turning toward the signal point. |
| `stair` | A diagonal band of bars — the section staircase. |
| `glyph` | A 4 by 4 pixel glyph (`glyph="graph"`, `retrieval`, `permissions`, … or 16 cells). |

`seed` varies the bars between scenes.
