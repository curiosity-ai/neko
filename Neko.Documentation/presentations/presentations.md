---
title: Presentation mode
label: Overview
description: Author a full-screen slide deck as a single Markdown file, and embed it in your documentation as a live, framed preview.
order: 1
icon: presentation
---

# Presentation mode

Presentation mode turns one Markdown file into a full-screen slide deck. It is
the same Neko build, the same components and the same `neko watch` loop — a deck
is just a page whose front matter says `presentation: true`.

A deck is **self-contained**: every slide lives in the one file, separated by a
horizontal rule. There are no per-slide files to keep in order, and no separate
deck config.

```markdown
---
title: Five layers between a question and your data
presentation:
  eyebrow: Curiosity Workspace
---

# Five layers between a question and your data

How applications keep an assistant useful without letting it become the
weakest link.

> The LLM never decides what a user is allowed to read.

--- {eyebrow="The problem"}

## A single control is a single point of failure

Leaked retrieval
:   A vector store built without ACLs returns the CFO's deck to a contractor.
```

## Try it

Both decks below are built from the Markdown in this repository. Click either
one to open it full-screen; the deck has its own back control to bring you home.

[!deck link="/presentations/decks/defense-in-depth" title="Five layers between a question and your data" description="A 14-slide deck built from a single Markdown file — definition lists, two-up boxes, inline SVG diagrams and per-slide accents."]

[!deck link="/presentations/decks/similarity-engine" title="The similarity engine, end to end" description="The same components again, with tables, claims and a wide pipeline diagram embedded as raw SVG."]

## What a deck page is (and is not)

A presentation page is deliberately not a documentation page:

- **It fills the viewport.** No navbar, no sidebar, no table of contents, no
  footer — one slide at a time, centred, with its own chrome.
- **It draws its own way out.** Because the documentation shell is gone, every
  deck renders a back control. It returns to the page you arrived from, or to
  the target you pin with `back:`.
- **It never joins the sidebar.** Decks are reached from the page that embeds
  them, not from the navigation tree.
- **It is never indexed.** A search hit on slide 9 would land a reader in the
  middle of a talk with no context, so decks are excluded from `search.json`
  automatically — no `searchExclude:` needed.
- **It can be locked.** `password:` works exactly as it does on any other page.
- **It can carry a standing brand mark.** `logo:` and `logoText:` pin a logo and
  a line of text to the bottom-right corner of every slide.

## Reading order

:::: card-grid
::: card {variant="grid" title="Deck structure" link="/presentations/deck-structure" icon="file-code" palette="blue"}
Front matter, the slide separator, per-slide options, and how a deck picks up its title.
:::

::: card {variant="grid" title="Slide components" link="/presentations/slide-components" icon="cube" palette="violet"}
The Markdown that maps onto the deck look — leads, claims, key/value rows, boxes, notes, tags — plus raw HTML and SVG.
:::

::: card {variant="grid" title="Embedding a deck" link="/presentations/embedding-a-deck" icon="browser" palette="emerald"}
The `[!deck]` component: a live, framed preview in macOS window chrome at a presentation aspect ratio.
:::

::: card {variant="grid" title="Protecting a deck" link="/presentations/protecting-a-deck" icon="lock" palette="amber"}
Password-protected decks, and what client-side encryption does and does not buy you.
:::
::::

## Keyboard and touch

| Input | Action |
| --- | --- |
| <kbd>→</kbd> <kbd>Space</kbd> <kbd>PageDown</kbd> | Next slide |
| <kbd>←</kbd> <kbd>PageUp</kbd> | Previous slide |
| <kbd>Home</kbd> / <kbd>End</kbd> | First / last slide |
| <kbd>Esc</kbd> | Back out of the deck |
| Swipe left / right | Next / previous slide |

The current slide is mirrored into the URL fragment (`#slide-7`), so a link can
point at a specific slide and a reload keeps your place — and because that is a
real element id, the link resolves even with JavaScript off. A bare `#7` is
accepted too. Printing a deck lays every slide out one per page.
