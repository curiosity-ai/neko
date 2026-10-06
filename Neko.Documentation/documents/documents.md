---
title: Document mode
label: Overview
description: Author a paged document as one Markdown file — A4 sheets in the browser, and the same pages as a native Word file, with the fonts embedded.
order: 1
icon: document
---

# Document mode

Document mode turns one Markdown file into a **paged document**: A4 (or Letter)
sheets in the browser, and a **docx** button that downloads the same pages as a
real Word file. It is the same Neko build, the same components and the same
`neko watch` loop — a document is a page whose front matter says `document: true`.

It is the sibling of [presentation mode](/presentations/presentations): a deck is
a file of slides, a document is a file of pages. A whitepaper, a brief, a
handbook, a proposal — anything you want to read on a screen, print, send, and
let someone else edit in Word.

```markdown
---
title: Team handbook
document: true
---

# Team handbook

How we work, written once and kept in Markdown.

## 1. Working together

- **Be reachable in working hours**, and say when you are not.
- **Ask in public first.**
```

A file with no separators is one flowing document: the sheet grows as long as the
text, and Word breaks it into pages. A `---` rule on its own line starts a new
sheet, and can carry options of its own — see [Document structure](/documents/document-structure).

## Two themes

| Theme | Look |
| --- | --- |
| `neko` | The default. A plain, readable page in Inter that follows nothing but the content: headings, lists, tables, code and callouts. |
| `curiosity` | The Curiosity brand's whitepaper system: paper, ink, stone and the deep blue, Schibsted Grotesk and Geist Mono, hairlines, generated art and one signal square. Covers, section openers, contents, figures, key points, tables, a customer quote, numbers, steps, takeaways, a glossary and a back cover. |

See [Document themes](/documents/document-themes).

## What you get in Word

The download is made of **native, editable Word content** — paragraphs, tables,
headers and footers, lists — not a picture of the page:

- every sheet is a Word **section**, with its running head as the **header** and
  its foot as the **footer**, and the page number a live **field**;
- the ground, the generated art and any panel behind the text are floating
  pictures behind it; diagrams are pictures too;
- the theme's **typefaces are embedded** in the file, so it looks the same on a
  machine that has never installed them. Word reads the fonts from the file and
  keeps them when a reader saves.

The exporter reads what the browser laid out, so the file matches the page.
Details are in [Document structure](/documents/document-structure#downloading-as-word).

## The samples

Both samples are single Markdown files in this repository; open them, press
**docx**, and read the source beside the result.

- [Modern enterprise search](/documents/samples/modern-enterprise-search) — a
  21-page whitepaper in the `curiosity` theme, every page type of the system.
  Its source is `documents/samples/modern-enterprise-search.md`.
- [Team handbook](/documents/samples/team-handbook) — a flowing document in the
  `neko` theme. Its source is `documents/samples/team-handbook.md`.

Documents are kept out of the sidebar and the search index, like decks: they are
reached by a link.
