---
title: Deck themes
label: Deck themes
description: The three built-in presentation themes, how to pick one per deck or for a whole build with --theme, and the layouts, grounds and generated art the curiosity theme adds.
order: 3
icon: palette
---

# Deck themes

A deck's look is its theme. Neko ships three, set per deck with `theme:` in the
`presentation:` block, or for every deck in a build with `--theme`.

| Theme | Geometry | Look |
| --- | --- | --- |
| `midnight` | A responsive column that fills the window | Deep navy ground, blueprint grid, a geometric display face, a reading serif and a mono. The default. |
| `daylight` | The same as midnight | The same type on warm paper. |
| `curiosity` | A fixed 1920 x 1080 canvas scaled to the window as one piece | The Curiosity brand: paper and ink with stone and slate, a deep blue and one electric-blue signal, Schibsted Grotesk and Geist Mono, hairlines and square corners. |

```yml
---
title: Quarterly review
presentation:
  theme: curiosity
---
```

## Re-theming a build: `--theme`

`neko build` and `neko watch` take `--theme <name>`. Every deck in that build
uses the named theme, whatever its front matter says, so the same Markdown can
be rendered in another look without touching a file:

```bash
neko build --theme curiosity
neko watch --theme midnight
```

An unknown name stops the build with an error and a non-zero exit code rather
than falling back to the default.

## What the curiosity theme adds

Everything a deck already does works in every theme. The curiosity theme reads
three more slide attributes, set on the `---` separator like `eyebrow` and
`accent`, and draws page furniture of its own: the eyebrow with its two-square
mark at the top left, the Escape mark at the bottom left and the page number at
the bottom right. The control bar and the back control fade out while the
pointer is still, and come back when it moves.

### `layout`

| Layout | What it is |
| --- | --- |
| `default` | Eyebrow, headline, content under it. |
| `cover` | The deck title large on the left (`*second part*` of it in the signal colour), the lead and the chips on the right, a band of art along the foot. |
| `section` | A section divider: eyebrow, `#` title and a line, at the foot of the slide; the art above. |
| `section-panel` | The same, with the art on a deep blue panel on the right (`class="panel-left"` moves the panel left and the text to the top). |
| `statement` | One sentence, set large, at the foot of the slide. |
| `split` | Headline on the left, lead on the right, the rest (stats, columns) across the foot. |
| `aside` | Headline and lead on the left, the content (steps, rows, a table) on the right. |
| `number` | One `::: stat`, set huge, with its label and context beside it. |
| `closing` | The call: a large headline on the deep blue, a contact line, a row of stats and the wordmark. |

In midnight and daylight these layouts fall back to the ordinary column.

### `ground`

The slide's surface: `paper` (the default), `stone`, `ink`, `slate` or `deep`
(the deep blue). Type, rules and art follow the ground. `::: box`,
`::: stat` and `::: step` take a `ground` too.

### `art`

Art drawn from numbers rather than image files, placed by the layout. It is an
inline SVG coloured by the theme, so it reads on any ground; midnight and
daylight draw it in their own palette.

| `art` | What it draws |
| --- | --- |
| `field` | A grid of short dashes bending round one signal square. |
| `bars` | Rows of short bars with a rising band of them lit. |
| `stair` | A diagonal band of bars climbing to the right. |
| `squares` | A grid of squares turning toward one point, the nearest one the signal. |
| `glyph` | A 4 by 4 pixel glyph with one marked cell. Pick one with `glyph=`. |

`glyph` takes a name (`connected`, `resolved`, `traceable`, `sovereign`, `fast`,
`open`, `precise`, `curious`, `graph`, `retrieval`, `permissions`, `models`,
`connectors`, `agenda`) or 16 cells read row by row: `k` a cell, `o` the marked
cell, `.` empty — `glyph="k..o.kk..kk.k..k"`. `seed=` varies the bars.

```markdown
--- {layout="cover" art="field" eyebrow="Event · Month 2026"}

# Deck title, line one *line two*

One sentence on what this deck is for and who it is for.

[!tag text="Presenter name" tone="solid"] [!tag text="Team"]
```

## The templates deck

[!deck link="/presentations/decks/curiosity-templates" title="Curiosity slide templates" description="Every layout and component of the curiosity theme — covers, sections, statements, steps, numbers, timelines, quotes, comparisons and the closing call — from one Markdown file."]

The deck's source, `presentations/decks/curiosity-templates.md` in this
repository, is the quickest way to start one: copy the slides you need.
