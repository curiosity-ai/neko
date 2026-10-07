---
title: Deck structure
label: Deck structure
description: The front-matter marker, the slide separator, and the per-slide options a presentation understands.
order: 2
icon: file-code
---

# Deck structure

A deck is one Markdown file. Front matter marks it as a presentation and sets
deck-wide defaults; a horizontal rule separates the slides.

## The marker

Any page whose front matter carries `presentation` is built as a deck:

```yaml
---
title: The similarity engine, end to end
description: From raw text to a ranked list.
presentation: true
---
```

The short form above takes every default. To set deck-wide options, make
`presentation` a mapping instead:

```yaml
---
title: Five layers between a question and your data
presentation:
  eyebrow: Curiosity Workspace
  theme: midnight
  accent: cyan
  back: /presentations/presentations
---
```

`presentation: false` (or `no`, `off`, `none`) builds the file as an ordinary
page, which is handy while a deck is still a draft.

### Deck options

| Key | Default | What it does |
| --- | --- | --- |
| `eyebrow` | — | The kicker shown above the heading on every slide that doesn't set its own. |
| `theme` | `midnight` | `midnight` — deep navy ground with a blueprint grid. `daylight` — the same geometry on warm paper. `curiosity` — the Curiosity brand on a fixed 16:9 canvas. See [Deck themes](/presentations/deck-themes); `--theme` on the command line overrides it for a whole build. |
| `accent` | `cyan` | Default accent for eyebrows and rules. One of `cyan`, `amber`, `rose`, `leaf`. |
| `back` | referrer, then `/` | Pins the back control to a specific page instead of the page the reader came from. |
| `backText` | `Back` | Label of the back control. |
| `grid` | `true` | The blueprint grid behind the slides. |
| `progress` | `true` | The thin progress rail across the top of the viewport. |
| `gauge` | `true` | The vertical slide gauge pinned to the left edge. |
| `counter` | `true` | The `3 / 14` counter in the control bar. |
| `download` | `true` | The **pptx** control in the control bar, which downloads the deck as a PowerPoint file. See below. |
| `fonts` | theme's | Whether the deck carries its typefaces. Every theme's fonts ship with Neko and are embedded in the deck's own HTML (`curiosity`: *Schibsted Grotesk* and *Geist Mono*; `midnight` and `daylight`: *Archivo*, *Source Serif 4* and *IBM Plex Mono*, vendored from Google Fonts), so no font host is called; `bundled` and `google` both mean that. `none` embeds nothing and falls back to local stacks. |
| `ratio` | `16:9` | The aspect ratio the deck is designed for; read by `[!deck]` previews. |
| `logo` | — | Image for the brand mark in the bottom-right corner. See below. |
| `logoText` | — | Text beside the logo. Works on its own, with no image. |
| `logoLink` | — | Turns the brand mark into a link. External URLs open in a new tab. |
| `logoAlt` | branding title | Alt text for the logo — only used when there is no `logoText`. |

Ordinary page keys still apply. `title` and `description` become the document
title and meta description, and `password` locks the deck — see
[Protecting a deck](/presentations/protecting-a-deck).

### The brand mark

`logo` and `logoText` pin a standing mark to the **bottom-right corner of every
slide** — the deck equivalent of the logo in the corner of a PowerPoint master.

```yaml
---
title: The similarity engine, end to end
presentation:
  eyebrow: Curiosity Workspace
  logo: /assets/neko-logo.png
  logoText: Built with Neko
  logoLink: https://neko.curiosity.ai
---
```

Either key works on its own: a logo with no text, or a wordmark with no image.

The mark shares the control bar's baseline and the bar reserves exactly as much
room as the mark needs, so the slide counter always sits to its left. Below
560px the wordmark is dropped and the logo carries the brand alone — unless
there is no logo, in which case the text stays.

Like `cover:`, the `logo` path is resolved against the page and then up the
folder tree, so `logo: neko-logo.png` finds the nearest `assets/` folder.

The mark is page furniture rather than content, so on a
[password-protected deck](/presentations/protecting-a-deck.md) it renders
alongside the unlock prompt instead of being encrypted with the slides.

### One self-contained file

A deck is a **single HTML file with no external dependencies**. Everything it
needs is embedded in the page itself: its stylesheet and Tailwind utilities, the
theme's fonts, the icons and emoji it shows, KaTeX and Mermaid when a slide has
math or a diagram, highlight.js, the deck runtime, and the PowerPoint exporter
with PptxGenJS. Local images (the logo, the favicon, pictures on a slide) are
inlined as data URIs too. Nothing is loaded from a CDN, a font host, or the
site's `assets/` folder, so the file can be saved, mailed, or opened offline and
still present — and export — exactly as it does on the site.

Only what the deck uses is included: icons and emoji are cut down to the ones on
its slides, and KaTeX (≈ 0.7 MB) and Mermaid (≈ 3.3 MB) only come along when
the deck has math or a diagram. A typical deck is about 3 MB before its images. Links to other pages
(the back control, a link on a slide) and remote images (`https://…`) stay links.

### Downloading as PowerPoint

Every deck carries a **pptx** button in its control bar. Clicking it builds a
`.pptx` of the whole deck right in the browser and downloads it, named after the
deck title (`the-similarity-engine-end-to-end.pptx`).

The file is made of native, editable PowerPoint shapes rather than screenshots:
headings, paragraphs and bullets are text boxes, boxes and rules are shapes,
diagrams and images are pictures (SVG stays vector, with a PNG fallback). Each
slide is laid out at 16:9 by the deck's own stylesheet and every element is
placed where it lands on screen, so the export matches the deck as presented.

The exporter uses [PptxGenJS](https://github.com/gitbrent/PptxGenJS), which
ships with Neko and is embedded in the deck — nothing is fetched from a CDN.
The library and the exporter ride along as inert script blocks and only run
when someone clicks the button. On a [password-protected deck](/presentations/protecting-a-deck.md) the
button is part of the encrypted payload, so it only appears once the deck is
unlocked.

Text keeps the typefaces the browser actually used. For a theme whose fonts
Neko ships (`curiosity`: *Schibsted Grotesk* and *Geist Mono*) the fonts are
**embedded in the file**, as Embedded OpenType in `ppt/fonts/`, so the deck
looks the same on a machine that has never installed them. A weight PowerPoint
has no flag for is written as its own family, the way the static font names
itself: headlines at 500 are *Schibsted Grotesk Medium*. With the web fonts
of `midnight` and `daylight` (*Archivo*, *Source Serif 4*, *IBM Plex Mono*)
nothing is embedded in the `.pptx` and PowerPoint substitutes a similar face where they are
not installed.

What the stylesheet draws comes along too. Generated content (`::before` and
`::after`, CSS counters included) is exported as text, solid colour layers
and SVG masks become shapes (the curiosity theme's marks are native
rectangles), and a theme's grid layouts keep their places. Each text box is
placed by its first baseline, so lines sit where they do on screen whatever
the line height. Gradients, shadows and icon-font glyphs are not carried over.

A page that cannot start a download itself (a sandboxed frame) can take the
file instead: define `window.nekoDeckSave = (fileName, blob) => …` and the
exporter hands it the finished `.pptx` rather than downloading it.

Set `download: false` to remove the button:

```yaml
presentation:
  download: false
```

## Slides

Slides are separated by a horizontal rule on its own line — the same `---` you
would write in any Markdown document:

```markdown
# The first slide

Its lead paragraph.

---

## The second slide

Its lead paragraph.
```

Everything before the first separator is slide 1. The rule must sit on its own
line with a blank line above it, which keeps Markdown's setext headings
(`Title` followed by `---`) working inside a slide, and leaves table rules and
fenced code untouched.

> A separator inside a fenced code block is never a separator. An embedded SVG
> or HTML block can contain as many dashes as it likes.

### Per-slide options

A separator can carry attributes in Neko's usual `{key="value"}` form:

```markdown
--- {eyebrow="Layer 4 · Honest limits" accent="rose"}

## What guardrails are not
```

To give **slide 1** its own options, open the body with a separator — it sets
the first slide's attributes rather than creating an empty slide before it:

```markdown
---
presentation: true
---

--- {eyebrow="Where we are" accent="leaf"}

# Slide one, with an eyebrow of its own
```

| Attribute | What it does |
| --- | --- |
| `eyebrow` | The kicker above the heading. Overrides the deck-wide `eyebrow`. |
| `accent` | `cyan`, `amber`, `rose` or `leaf` — tints the eyebrow, rules and claim bar for this slide. |
| `layout` | `default`, `title`, `center`, `wide` or `full`. See below. |
| `id` | The slide's element id, and the anchor that links to it. Defaults to `slide-<n>`. |
| `class` | Extra classes on the `<section>`, for site-specific CSS. |

### Layouts

| Layout | Effect |
| --- | --- |
| `default` | The standard reading column. |
| `title` | Applied automatically to any slide that opens on an `#` heading. |
| `center` | Centres the text and drops the measure limits — good for a single statement. |
| `wide` | Widens the column to 1480px, for a broad diagram or a six-column table. |
| `full` | Removes the column cap entirely and trims the side padding. |

## Titles and headings

A slide's heading is plain Markdown:

- `#` is the deck title, or a section divider. A slide that opens on one is laid
  out as a `title` slide unless you say otherwise.
- `##` is a slide headline.
- `###` is a sub-heading inside a box or column.

The paragraph that directly follows `#` or `##` is styled as the slide's **lead**
— larger, dimmed, wider measure — without any markup of its own.

## Where decks live

Nothing forces a particular folder, but keeping decks together makes them easy
to manage. The Neko docs use `presentations/decks/`, with a folder config that
keeps the folder out of the sidebar:

```yaml
# presentations/decks/index.yml
label: Decks
visibility: hidden
searchExclude: true
```

Individual decks are already excluded from the sidebar and the search index —
the folder config just stops the empty group itself from showing up.
