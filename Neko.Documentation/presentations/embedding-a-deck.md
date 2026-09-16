---
title: Embedding a deck
label: Embedding a deck
description: The [!deck] component — a live presentation framed as a slide inside macOS window chrome, at a presentation aspect ratio.
order: 4
icon: browser
---

# Embedding a deck

Decks stay out of the sidebar and out of search, so a documentation page needs a
way to point at one. `[!deck]` is that way: it renders the presentation **live**
in an iframe, framed as a slide inside macOS-style window chrome and locked to a
presentation aspect ratio.

```markdown
[!deck link="/presentations/decks/defense-in-depth"
       title="Five layers between a question and your data"
       description="A 14-slide deck built from a single Markdown file."]
```

[!deck link="/presentations/decks/defense-in-depth" title="Five layers between a question and your data" description="A 14-slide deck built from a single Markdown file — definition lists, two-up boxes, inline SVG diagrams and per-slide accents."]

## Attributes

| Attribute | Default | What it does |
| --- | --- | --- |
| `link` | *(required)* | The deck's URL. A `.md` suffix is stripped for you, so `decks/foo.md` and `/decks/foo` both work. |
| `title` | `Presentation` | Shown in the title bar and used as the frame's accessible name. |
| `description` | — | A caption under the frame. |
| `slide` | `1` | Opens the preview — and the full-screen link — at a given slide. A number is the slide's position; anything else is used as the anchor, so a slide with a custom `id` works. |
| `ratio` | `16:9` | `16:9`, `16:10`, `4:3` or `1:1`. |
| `chrome` | `macos` | `none` drops the title bar and leaves a bare framed slide. |
| `open-text` | `Open presentation` | Label on the title-bar link and the hover affordance. |

`url` is accepted as an alias for `link`, and a bare first argument works too:
`[!deck "/presentations/decks/similarity-engine"]`.

## Why the preview is inert

The embedded deck is a **preview**, not a player. A transparent overlay takes
the click and opens the presentation full-screen instead of letting the reader
drive slides inside a postage stamp, and the frame is removed from the tab order
so keyboard users land on the link rather than inside the iframe.

The deck itself notices it is embedded: it hides its own back control (the page
around it already offers the way out) and tightens its padding so the slide
still reads at preview size.

## Variations

A 4:3 deck with no title bar:

```markdown
[!deck link="/presentations/decks/similarity-engine" ratio="4:3" chrome="none"]
```

Opening on a specific slide:

```markdown
[!deck link="/presentations/decks/similarity-engine" slide="3" title="The whole path"]
```

[!deck link="/presentations/decks/similarity-engine" slide="3" title="The whole path" description="The same deck, opened at slide 3 — the embed and the full-screen link both land there."]

## Embedding a protected deck

`[!deck]` frames a password-protected deck exactly the same way; the preview
shows the unlock prompt until someone has entered the password in that session.
That makes it a reasonable way to advertise a deck without leaking it — the page
around it stays public, and the deck's content never reaches the generated HTML
in readable form.

[!deck link="/presentations/decks/internal-roadmap" title="Internal roadmap review" description="A password-protected deck. The password for this demo is neko."]

## Linking without a frame

Nothing stops an ordinary link — `[the deck](/presentations/decks/similarity-engine)`
— when a page just needs a mention. The deck's back control returns to whichever
page the reader came from either way.
