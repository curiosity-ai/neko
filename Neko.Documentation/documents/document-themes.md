---
title: Document themes
label: Document themes
description: The neko and curiosity themes, and the layouts, grounds and generated art the curiosity theme adds.
order: 3
icon: palette
---

# Document themes

A document's look is its theme: `theme:` in the `document:` block, or `--theme`
on the command line (`neko build --theme curiosity` re-themes every document
that has that theme; a name a document does not have leaves it alone).

## `neko`

The default. A plain page in **Inter**: headings, paragraphs, lists, tables, code
blocks, blockquotes and callouts, set for reading and for editing in Word. Pages
are separated with `---` when you want a cover or a hard break; otherwise the
sheet simply flows. The foot carries the site title (or `logoText`) and the page
number.

## `curiosity`

The Curiosity brand's whitepaper system. Paper and ink with stone and slate, the
deep blue and **one** electric-blue signal per page; **Schibsted Grotesk** for
everything read and **Geist Mono**, small and uppercase, for labels, numbers and
counters; hairlines instead of shadows; square corners, bar the pills. A sheet is
laid out in `cqw` — one hundredth of its width — so it scales as one piece and
the Word export lands every block where it sits on the page.

### `layout`

| Layout | What it is |
| --- | --- |
| `cover` | The title large at the left (`*the second line*` in the signal colour), the lede at the right, the details as cells across, a band of art with the lockup. |
| `contents` | The title, the sections as hairline rows (`::: entry`), a paragraph on stone at the foot. |
| `opener` | A section opener: the numeral huge in mono, the title, three points, art across the foot. |
| `horizon` | The same, with the art above and the numeral beside the title. |
| `statement` | A subsection heading, the line to take away set large, three columns and a figure across the foot. |
| `plate` | Heading and lede, a figure on a full-bleed stone plate, two columns with the key point. |
| `sidebar` | A stone sidebar down the right third (`::: aside`) beside the text. |
| `diagram` | A figure drawn large — a query path — with numbered notes under it. |
| `table` | A reference table with a legend and a note. |
| `technical` | A listing in an ink shell and a spec table in mono. |
| `quote` | A customer's quote and mark, the problem and the change at the foot. |
| `numbers` | The proof figures as staggered cards. |
| `steps` | A staircase of steps and a checklist. |
| `takeaways` | Three numbered takeaways on the deep blue. |
| `glossary` | Terms in mono, definitions in two columns. |
| `back` | The call, a paragraph, a pill, three facts and the lockup, on the deep blue. |

### `ground`

`paper` (the default), `stone`, `ink`, `slate` or `deep`. Type, rules and art
follow the ground; openers default to `ink`, the takeaways and the back cover to
`deep`. `::: box`, `::: stat` and `::: step` take a `ground` too.

### `art`

Generated from numbers, drawn as inline SVG in the page's tones, and exported as a
picture behind the text.

| `art` | What it draws |
| --- | --- |
| `field` | Short dashes turning round one signal square. `fade="right"` dims it to one side. |
| `squares` | A grid of squares turning toward one point, the nearest one the signal. |
| `bars` | Three rows of vertical bars rising toward one point, one bar the signal. |
| `window` | The Escape mark scaled up, its block a window onto a dash field. |

`dx` and `dy` (0 to 1) move the signal across the drawing.

### The whitepaper

[Modern enterprise search](/documents/samples/modern-enterprise-search) is the
whole system in one file — every layout above, 21 pages. Copy the pages you need.
