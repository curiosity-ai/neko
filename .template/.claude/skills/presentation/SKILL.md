---
name: presentation
description: Author a full-screen slide deck as a single self-contained Markdown file (`presentation: true` front matter), with slides separated by `---`. Covers deck options, per-slide attributes, the slide components (lead, claim, rows, cols, box, note, tag, figure), embedded HTML/SVG diagrams, password protection, and the `[!deck]` component that embeds a deck in a documentation page. Use when building a talk, a pitch deck, a walkthrough, or any "presentation-like" page.
---

# Presentation mode

A Neko presentation is **one Markdown file** that builds into a full-screen slide
deck. There are no per-slide files and no separate deck config: front matter
marks the page as a deck, and a horizontal rule separates the slides.

A deck page carries none of the documentation shell — no navbar, sidebar, table
of contents or footer. It draws its own chrome instead (progress rail, slide
gauge, prev/next bar, counter) plus a **back control**, because otherwise there
would be no way out.

## Marking a deck

```yaml
---
title: Five layers between a question and your data
description: How applications keep an assistant useful without letting it become the weakest link.
presentation: true
---
```

Use the mapping form to set deck-wide options:

```yaml
---
title: The similarity engine, end to end
presentation:
  eyebrow: Curiosity Workspace
  theme: midnight
  accent: cyan
  back: /guides/talks
---
```

`presentation: false` (or `no`, `off`, `none`) builds the file as an ordinary
page — handy while a deck is still a draft.

### Deck options

| Key | Default | Notes |
| --- | --- | --- |
| `eyebrow` | — | Kicker above the heading on every slide that doesn't set its own. |
| `theme` | `midnight` | `midnight` (deep navy + blueprint grid), `daylight` (warm paper) or `curiosity` (the Curiosity brand on a fixed 16:9 canvas — see **Themes** below). `neko build --theme <name>` overrides it for every deck in a build. |
| `accent` | `cyan` | `cyan`, `amber`, `rose` or `leaf`. |
| `back` | referrer, then `/` | Pins the back control to a page instead of the referrer. |
| `backText` | `Back` | Label of the back control. |
| `grid` | `true` | The blueprint grid behind the slides. |
| `progress` | `true` | Progress rail across the top. |
| `gauge` | `true` | Vertical slide gauge on the left edge. |
| `counter` | `true` | The `3 / 14` counter. |
| `download` | `true` | The **pptx** button that downloads the deck as PowerPoint. |
| `fonts` | theme's | Every theme's fonts ship with Neko and are embedded in the deck (`bundled` and `google` both mean that — no font host is called); `none` embeds nothing and uses local stacks. |
| `ratio` | `16:9` | Aspect ratio the deck is designed for; read by `[!deck]`. |
| `logo` | — | Image for the brand mark in the bottom-right corner of every slide. |
| `logoText` | — | Text beside the logo. Works on its own, with no image. |
| `logoLink` | — | Turns the brand mark into a link; external URLs open in a new tab. |
| `logoAlt` | branding title | Alt text for the logo — only used when there is no `logoText`. |

Ordinary page keys still work — `title`, `description`, and `password` (see
below).

### The brand mark

`logo` and `logoText` pin a standing mark to the bottom-right corner of **every**
slide — the deck equivalent of the logo on a PowerPoint master:

```yaml
presentation:
  logo: /assets/logo.png
  logoText: Built with Neko
  logoLink: https://example.com
```

Either key works alone. The mark shares the control bar's baseline and the bar
reserves room for it, so the slide counter always sits to its left; below 560px
the wordmark drops and the logo carries the brand (unless there is no logo).
The `logo` path is resolved like `cover:`, so a bare file name finds the nearest
`assets/` folder. The mark is furniture rather than content, so a protected deck
still shows it beside the unlock prompt.

### One self-contained file

A built deck is a single HTML file with no external dependencies: CSS, fonts,
icons, emoji, KaTeX/Mermaid (only when used), highlight.js, the runtime and the
PowerPoint exporter are all embedded, and local images (logo, favicon, slide
pictures) are inlined as data URIs. It can be mailed or opened offline. Mermaid
adds ≈ 3.3 MB, so only use a diagram where it earns its place; remote images
(`https://…`) stay links, so keep images in the docs folder if the deck must
work offline.

### PowerPoint download

Every deck has a **pptx** button in its control bar that exports the whole deck
to an editable `.pptx` in the browser (via PptxGenJS, which ships with Neko and
is embedded in the deck — no CDN). Each slide is laid out at 16:9 and every heading, paragraph,
bullet, box, rule, SVG and image becomes a native PowerPoint shape at the same
position. An SVG of straight edges and flat colours (rects, lines, polygons,
`M`/`L`/`H`/`V`/`Z` paths) becomes vector freeforms, one per paint; an SVG with
text, curves, transforms or gradients is a picture. PowerPoint sets fonts a hair
wider than the browser, so text sized exactly to its words (a big number, a
short label) can wrap its last character in PowerPoint while the slide
thumbnail still looks right: give text that must stay on one line
`white-space: nowrap` (a `<span style="white-space:nowrap">` in Markdown), and
the export writes an unwrapped text box. Generated content (`::before`/`::after`, counters) is exported as
text; solid colour layers and SVG masks become shapes. A theme can mark a thin
bar `--pptx-line: arrow` to export it as a PowerPoint line with an arrowhead,
and a screen-only decoration `--pptx: none` to leave it out (the curiosity
`flow` connectors use both). In the curiosity theme
the fonts (Schibsted Grotesk, Geist Mono) are embedded in the file, so it looks
right where they are not installed. Gradients, shadows and icon glyphs are
dropped, so don't put meaning in them. `download: false` removes the button. On a protected
deck the button only appears after unlocking.

## Slides

Separate slides with `---` on its own line, with a blank line above it:

```markdown
# The first slide

Its lead paragraph.

---

## The second slide

Its lead paragraph.
```

Everything before the first separator is slide 1. A separator inside a fenced
code block is never a separator, so an embedded SVG can contain dashes freely.

### Per-slide attributes

```markdown
--- {eyebrow="Layer 4 · Honest limits" accent="rose" layout="center"}
```

| Attribute | Notes |
| --- | --- |
| `eyebrow` | Kicker above the heading; overrides the deck-wide value. |
| `accent` | `cyan`, `amber`, `rose`, `leaf`. |
| `layout` | `default`, `title`, `center`, `wide`, `full`; with `theme: curiosity` also `cover`, `section`, `section-panel`, `statement`, `split`, `aside`, `number`, `closing`. |
| `ground` | curiosity: the slide's surface — `paper`, `stone`, `ink`, `slate`, `deep`. |
| `art` | Generated art placed by the layout: `field`, `bars`, `stair`, `squares`, `glyph`. |
| `glyph` | With `art="glyph"`: a name (`graph`, `curious`, `permissions`, …) or 16 cells (`k` cell, `o` marked, `.` empty). |
| `seed` | Varies the `bars` art. |
| `id` | Element id and link anchor; defaults to `slide-<n>`. |
| `class` | Extra classes for site CSS. |

To give **slide 1** attributes, open the body with a separator — it sets the
first slide's attributes rather than creating an empty slide:

```markdown
---
presentation: true
---

--- {eyebrow="Where we are"}

# Slide one, with its own eyebrow
```

A slide that opens on an `#` heading is laid out as a `title` slide
automatically.

## Writing a slide

Reach for plain Markdown first — it already maps onto the deck look.

| You want | Write |
| --- | --- |
| Slide title | `##` (`#` for the deck title / a section divider) |
| Lead paragraph | The paragraph right after the heading — no markup needed |
| The claim | A blockquote (`> …`) |
| Key/value rows | A definition list (`Term` / `:   Definition`) |
| A table | A Markdown table |
| Bulleted points | A list; `**bold lead-ins**` become mini-headings |
| Code | A fenced code block |

### Components

```markdown
::: lead
Any block can be the standfirst, not just the first paragraph.
:::

::: claim
The one sentence this slide is making.
:::

:::: cols
::: box {title="How it is wired up"}
- point
- point
:::

::: box {title="Two things to plan for" tone="warn"}
- point
:::
::::

::: note
The aside at the foot of the slide.
:::

::: note {tone="limit"}
The caveat that has to land.
:::
```

- `::: cols` — `{count="3"}` for three columns, `{count="1"}` to stay single.
- `::: box` — `{title="…"}` and `{tone="warn" | "stop" | "ok"}`.
- `::: note` — `{tone="limit"}` turns it rose.
- `[!tag text="flag" tone="warn"]` — an inline chip; `tone` takes `warn`,
  `stop`, `ok`, `solid` (a filled pill) or nothing.
- `::: box` also takes `label` (a mono kicker), `ground` and `art`.

More components, in every theme:

```markdown
:::: steps {style="stairs"}          <!-- or flow, cards, art, glyphs -->
::: step {label="Describe" art="glyph" glyph="graph"}
### Step one
One line
:::
::::

:::: stats {style="cards"}           <!-- omit style for a ruled row -->
::: stat {value="30TB+" label="Data connected" ground="deep"}
A line of context.
:::
::::

::: agenda
1. **Where we are** One line
:::

:::: timeline
::: milestone {when="Q3 2026" state="now"}   <!-- done, now, next -->
**Milestone**
One line
:::
::::

::: quote {by="Company" context="Team" art="squares"}
Their words, verbatim.
:::

::: compare {highlight="3"}
| Criterion | Other | Ours |
| --- | --- | --- |
| Where it runs | Value | On premises |
:::

::: art {kind="bars"}
:::
```

> **Nesting:** the outer container needs **more colons than the inner one** —
> four outside, three inside. That is standard Markdown container nesting, not a
> Neko rule.

`cols`, `box`, `note`, `claim`, `lead`, `figure`, `steps`, `step`, `stats`,
`stat`, `agenda`, `timeline`, `milestone`, `quote`, `compare` and `art` are **scoped to decks**.
On any other page `::: name` remains Neko's generic container (the name becomes
the div's class), so these names are not reserved site-wide.

### Diagrams

A block-level HTML element on its own line passes straight through, but only
while it has no blank line inside it. For anything longer, fence it:

````markdown
::: figure {caption="The path from a committed node to a ranked list." label="Pipeline diagram"}
```embed
<svg viewBox="0 0 880 300">
  <rect x="108" y="118" width="96" height="64" rx="4" fill="none" stroke="#5FD0D8"/>

  <text x="156" y="142" text-anchor="middle" fill="#5FD0D8">graph</text>
</svg>
```
:::
````

` ```embed ` emits its contents verbatim, unescaped — `embed-html` and
`embed-svg` are aliases. Only put markup you control in one.

`::: figure` makes the diagram scroll instead of overflow, and adds a `caption`
and an accessible `label`. To share a diagram across decks, keep it in a file
and pull it in: `{{ include "diagrams/pipeline.svg" }}`.

Style diagrams against the deck palette: `--deck-cyan` `#5FD0D8`,
`--deck-amber` `#E9A94A`, `--deck-rose` `#E0736B`, `--deck-leaf` `#7FC08A`,
rules `#2F5478`, dim ink `#93AAC6`, ink `#DCE6F2`.

## Themes

`theme: curiosity` is a different kind of deck: every slide is a fixed
1920 x 1080 composition scaled to the window, in the Curiosity brand (Schibsted
Grotesk, Geist Mono, paper/ink/deep blue, one signal colour). It draws an
eyebrow mark, the Escape mark and a page number on every slide, and reads the
`layout`, `ground` and `art` slide attributes:

```markdown
--- {layout="cover" art="field" eyebrow="Event · Month 2026"}

# Deck title, line one *line two*

One sentence on what this deck is for.

[!tag text="Presenter" tone="solid"] [!tag text="Team"]

--- {layout="section" ground="ink" art="bars" eyebrow="Section / 01"}

# Section title

One line on what this section argues.

--- {layout="statement" ground="deep" eyebrow="Label / Topic"}

## One sentence that states the point.
```

- `cover`: title left (`*…*` is the signal colour), lead and chips right, art along the foot.
- `section` / `section-panel` (`class="panel-left"`): dividers, art on top or on a deep panel.
- `statement`: one sentence, large. `split`: headline left, lead right, stats under.
- `aside`: headline left, steps / rows / table right. `number`: one huge `::: stat`.
- `closing`: the call on the deep blue with a stats row.

Keep a slide to its canvas: a slide that needs more room is two slides. The
Neko docs deck `presentations/decks/curiosity-templates.md` has every layout.
Build the same deck in another look with `neko build --theme midnight`.

## Linking to a deck

Decks stay out of the sidebar and out of the search index, so a page has to
point at one. `[!deck]` renders the deck live in an iframe, framed as a slide in
macOS window chrome at a presentation aspect ratio:

```markdown
[!deck link="/decks/defense-in-depth"
       title="Five layers between a question and your data"
       description="A 14-slide walkthrough."]
```

| Attribute | Default | Notes |
| --- | --- | --- |
| `link` | *(required)* | Deck URL; a `.md` suffix is stripped. `url` is an alias. |
| `title` | `Presentation` | Title-bar text and the frame's accessible name. |
| `description` | — | Caption under the frame. |
| `slide` | `1` | Opens the preview and the link at a given slide (a number, or a slide `id`). |
| `ratio` | `16:9` | `16:9`, `16:10`, `4:3`, `1:1`. |
| `chrome` | `macos` | `none` for a bare framed slide. |
| `open-text` | `Open presentation` | Label on the link and hover affordance. |

The preview is inert on purpose: clicking anywhere opens the deck full-screen
rather than driving slides inside a postage stamp. The embedded deck notices it
is framed, scales itself down to a true miniature, and hides its own back
control.

## Password-protected decks

`password:` works exactly as on any other page — the slides are encrypted at
build time and decrypted in the browser.

```yaml
---
title: Internal roadmap review
password: internal-2026
presentation:
  eyebrow: Internal
---
```

Everything that is content is encrypted: the slides, the control bar, the
document title. The blueprint ground, progress rail and back control stay in the
clear and frame the unlock prompt.

Client-side encryption keeps a deck out of crawlers, search and casual reading.
It is **not** access control — anyone with the password has the content.

`neko watch --no-password` clears passwords for a local preview session.

## Keyboard and touch

<kbd>→</kbd> / <kbd>Space</kbd> / <kbd>PageDown</kbd> advance, <kbd>←</kbd> /
<kbd>PageUp</kbd> go back, <kbd>Home</kbd> and <kbd>End</kbd> jump to the ends,
<kbd>Esc</kbd> leaves the deck, and a horizontal swipe changes slide. The current
slide is mirrored into the URL fragment (`#slide-7`), so a link can point at a
slide; a bare `#7` is accepted too.
Printing lays out one slide per page.

## Tips

- **Write the slide, not the page.** A slide holds one heading, one lead and one
  structure. If it needs scrolling, it is two slides.
- **Let the separator carry the eyebrow.** It keeps the slide body pure Markdown.
- **Set the deck-wide `eyebrow`** to the talk or product name, then override it
  per slide with the section you're in.
- **Switch `accent` to `rose`** on the slide that names a limitation — it reads
  as a deliberate change of tone.
- **Keep decks in their own folder** with `visibility: hidden` in its
  `index.yml`, so the empty group never shows up in the sidebar.
