---
title: Document structure
label: Document structure
description: The front-matter marker, the page separator, the per-page options, and how the Word download is built.
order: 2
icon: file-code
---

# Document structure

A document is one Markdown file. Front matter marks it and sets document-wide
options; a horizontal rule starts a new page.

## The marker

```yaml
---
title: Modern enterprise search
document: true
---
```

To set options, make `document` a mapping:

```yaml
---
title: Modern enterprise search
document:
  theme: curiosity
  running: Modern enterprise search
  author: Curiosity
  company: Curiosity GmbH
---
```

`document: false` (or `no`, `off`, `none`) builds the file as an ordinary page.
A file that also says `presentation:` is a deck.

### Document options

| Key | Default | What it does |
| --- | --- | --- |
| `theme` | `neko` | `neko` or `curiosity`. See [Document themes](/documents/document-themes). `--theme curiosity` on the command line re-themes every document that has that theme. |
| `size` | `a4` | `a4` (210 × 297 mm) or `letter` (8.5 × 11 in). |
| `running` | — | The running head printed at the top of every page that doesn't set its own. |
| `numbers` | `true` | Print the page number in the foot. |
| `download` | `true` | The **docx** control in the bar. `docx` is an alias. |
| `fonts` | bundled | `none` links no fonts and falls back to local stacks. The fonts Neko ships are loaded from the site's own `assets/deckfonts/`. |
| `back`, `backText` | referrer | Where the back control goes and what it says. |
| `logo`, `logoText` | site title | The brand mark in the foot of the `neko` theme. |
| `author`, `company` | — | Written to the Word file's properties. |

`title` and `description` become the document title and meta description, and
`password` locks the document exactly as it locks any page.

## Pages

Pages are separated by a rule on its own line, with a blank line above — the same
`---` you would write in any Markdown document, and the same rule decks use:

```markdown
--- {layout="opener" art="squares" number="01"}

# Search is<br>not one thing
```

Everything before the first separator is page 1. A separator inside a fenced code
block is never a separator. A sheet whose content runs long **grows**; Word
breaks it onto more pages and repeats the header and footer.

### Page options

| Attribute | What it does |
| --- | --- |
| `layout` | The page's layout. In `neko` there is only `page`; `curiosity` has many. |
| `ground` | `paper`, `stone`, `ink`, `slate` or `deep`. Each layout has its own default. |
| `art` | Generated art: `field`, `squares`, `bars` or `window`. `dx` and `dy` (0 to 1) place its signal. |
| `running` | This page's running head. |
| `head-right` | A second line at the right of the running head. |
| `foot` | Text for the right of the foot instead of the page number; `|` starts a new line. `foot="none"` leaves the foot off. |
| `head="none"` | Leaves the running head off. |
| `number` | The big numeral of a section opener. |
| `id`, `class` | The sheet's element id, and extra classes for site CSS. |

## Downloading as Word

Every document carries a **docx** button in its bar. Clicking it builds the file
in the browser — nothing is sent anywhere — and downloads it, named after the
title (`modern-enterprise-search.docx`).

The exporter lays the sheets out exactly one page wide (794 CSS px for A4, which
is 210 mm at 96 dpi) and reads what the browser drew, so the file matches the
page:

- **Sections, headers and footers.** One section per sheet. The running head is
  the section's header, the foot its footer, and the page number a live `PAGE`
  field (`01`, `02`, … in the curiosity theme).
- **Text.** Runs keep the typeface, size, colour, tracking and line height of the
  page. Paragraphs and list items are Word paragraphs and Word lists.
- **Layout.** Space between blocks is measured, so the vertical rhythm survives.
  Grids, flex rows and columns become tables; a box with a ground, borders or
  padding becomes a one-cell table; a table is a table, with its header row
  repeating.
- **Pictures.** The ground, generated art and panels float behind the text, in the
  header. Diagrams, SVG and a mask-drawn logo are pictures at 3× resolution.
- **Fonts.** The theme's typefaces are embedded as obfuscated TrueType
  (`word/fonts/*.odttf`) — the format Word reads — under the names the fonts
  carry themselves, and `settings.xml` asks Word to keep them when a reader saves.
  Word embeds one face per name, so a weight Word has no flag for is its own
  family, as in the pptx export: a heading at 500 is *Schibsted Grotesk Medium*,
  at 600 *Inter SemiBold*. Bold is set in the heaviest embedded instance.

The exporter uses [docx](https://github.com/dolanmiu/docx), which Neko ships in
its own `assets/` folder — nothing is fetched from a CDN, and neither the library
nor the exporter is downloaded until someone clicks the button. On a
[password-protected](/presentations/protecting-a-deck) document the button is
part of the encrypted payload.

What does not survive: shadows, gradients, icon-font glyphs and CSS-generated
content. Text on a path of art stays editable; the art itself is a picture.

A page that cannot start a download itself (a sandboxed frame) can take the
file instead: define `window.nekoDocSave = (fileName, blob) => …` and the exporter
hands it the finished `.docx`.

### Printing

Print the page, or *Save as PDF*, and each sheet lands on one A4 page: the bar is
hidden and the sheets lose their shadows.
