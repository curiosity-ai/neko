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
| `theme` | `midnight` | `midnight` — deep navy ground with a blueprint grid. `daylight` — the same geometry on warm paper. |
| `accent` | `cyan` | Default accent for eyebrows and rules. One of `cyan`, `amber`, `rose`, `leaf`. |
| `back` | referrer, then `/` | Pins the back control to a specific page instead of the page the reader came from. |
| `backText` | `Back` | Label of the back control. |
| `grid` | `true` | The blueprint grid behind the slides. |
| `progress` | `true` | The thin progress rail across the top of the viewport. |
| `gauge` | `true` | The vertical slide gauge pinned to the left edge. |
| `counter` | `true` | The `3 / 14` counter in the control bar. |
| `fonts` | `google` | `none` drops the Google Fonts link and falls back to local stacks — for air-gapped sites. |
| `ratio` | `16:9` | The aspect ratio the deck is designed for; read by `[!deck]` previews. |

Ordinary page keys still apply. `title` and `description` become the document
title and meta description, and `password` locks the deck — see
[Protecting a deck](/presentations/protecting-a-deck).

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
