---
title: Slide components
label: Slide components
description: The Markdown that maps onto the deck look — leads, claims, key/value rows, boxes, notes and tags — plus raw HTML and SVG.
order: 3
icon: cube
---

# Slide components

Presentation mode is built around one rule: **write plain Markdown where plain
Markdown already says the right thing**, and reach for a component only where it
doesn't. Headings, paragraphs, lists, tables, code and images all render against
the deck palette with no markup of their own.

Every other Neko component still works inside a slide — the slide body goes
through the same pipeline as any page.

!!! info Presentation components are scoped to decks
`cols`, `box`, `note`, `claim`, `lead` and `figure` only mean what this page
describes **inside a presentation**. Everywhere else `::: name` stays Neko's
[generic container](/components/container.md) — the name becomes the div's
class — so an existing `::: note` on a documentation page is untouched.
!!!

## Lead

The paragraph directly after a slide's `#` or `##` heading is the lead: larger,
dimmed, wider measure. No markup needed.

```markdown
## One vector index answers one question

"Find me something like this" is never a single question.
```

To lead with something else — a second paragraph, a list — wrap it:

```markdown
::: lead
This paragraph is a lead even though it isn't the first one.
:::
```

## Claim

A blockquote is the deck's **claim**: the one sentence the slide is actually
making, set in the display face against an accent rule.

```markdown
> A similarity score here is not one number from one index. It is a sum of
> named signals, and every result carries the breakdown that produced it.
```

`::: claim` is the explicit form, and renders identically. A slide with
`accent="rose"` turns its claim rule rose.

## Key/value rows

The row list — a bold key on the left, prose on the right — is a Markdown
**definition list**:

```markdown
Typed nodes
:   Every node has a type and a stable key, addressed by a `UID128`.

Typed edges, both ways
:   Relationships are first-class and stored as pairs, so a traversal is cheap
    in either direction.
```

Rows stack on narrow screens and split into two columns from 760px up. `::: rows`
on a plain `<div>` gets the same frame if you need to build the rows by hand.

## Columns

`:::: cols` lays its children out side by side — one column on a phone, two from
820px up.

```markdown
:::: cols
::: box {title="How it is wired up"}
- Built under **Settings → NLP → Pipelines**, one per language.
- Assigned to node-field pairs on the **Used for** tab.
:::

::: box {title="Mixed languages"}
- Assign the same field to several pipelines.
- The workspace splits at sentence level and routes each sentence on its own.
:::
::::
```

> The outer container needs **more colons than the inner one** — that is how
> Markdown container nesting works everywhere, not a Neko rule. Four outside,
> three inside.

`{count="3"}` asks for three columns; `{count="1"}` keeps a single column at
every width.

## Box

A bordered panel. `tone` tints the border and the title.

```markdown
::: box {title="Two things to plan for" tone="warn"}
- **It is not trained on commit.** Call `TrainAsync` once there is data.
- **It is sensitive to density.** Sparse subgraphs produce poor vectors.
:::
```

| `tone` | Border and title |
| --- | --- |
| *(omitted)* | Neutral rule, accent-coloured title |
| `warn` | Amber |
| `stop` | Rose |
| `ok` | Green |

A `###` heading inside a box works too, when you want more than one titled
section in the same panel.

## Note

The small, rule-led aside at the foot of a slide — the caveat, the source line,
the "and one more thing".

```markdown
::: note
Everything left of `signals` happens at ingest time. Everything right of it
happens per request, in a scenario you write.
:::

::: note {tone="limit"}
Rules only filter — they never change a score.
:::
```

`tone="limit"` turns the note rose, for the limitation that has to land.

## Tags

An inline chip, for marking a verdict or a state inside a sentence or a row.

```markdown
[!tag text="flag" tone="warn"] conversation continues, verdict recorded.
[!tag text="reject" tone="stop"] the turn is refused.
```

`tone` takes `warn`, `stop`, `ok`, or nothing for the neutral chip.

## Tables

Plain Markdown tables, styled for a deck: a display-face header row, hairline
rules, dimmed body text. A cell that holds **only** inline code reads as a mono
key column, which is what the source decks use for API names.

```markdown
| Fuse | Effect |
| --- | --- |
| `Sum` | Add the signal scores. The default. |
| `Max` | Keep the strongest single signal. |
```

## Lists

Bullets and numbers render as you'd expect. A **bold lead-in** inside a bullet
is styled as a mini-heading, which is the pattern the source decks use for
checklists:

```markdown
- **Model the edges first.** Structural signals and PageSpace both read them.
- **Point NLP at the fields that carry entities** and configure linking.
```

## Diagrams: raw HTML and SVG

A slide can carry a hand-drawn diagram with no side file.

### Inline, as Markdown already allows

A block-level HTML element on its own line passes straight through:

```markdown
<svg viewBox="0 0 200 60"><rect x="4" y="4" width="192" height="52" rx="4" fill="none" stroke="#5FD0D8"/></svg>
```

That only holds while the element has **no blank line inside it** — Markdown
ends an HTML block at the first blank line, which most hand-written SVGs have.

### Fenced, for anything longer

An ` ```embed ` fence emits its contents verbatim, blank lines and all. Use it
for any diagram you'd otherwise have to squash onto one line:

````markdown
::: figure {caption="The path from a committed node to a ranked list."}
```embed
<svg viewBox="0 0 880 300">
  <defs>
    <marker id="a2" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">
      <path d="M0 0 L8 4 L0 8 z" fill="#2F5478"/>
    </marker>
  </defs>

  <rect x="108" y="118" width="96" height="64" rx="4" fill="none" stroke="#5FD0D8"/>
  <text x="156" y="142" text-anchor="middle" fill="#5FD0D8">graph</text>
</svg>
```
:::
````

`embed-html` and `embed-svg` are aliases — pick whichever reads better. The
content is emitted as-is with no escaping, so only put markup you control in
one.

### Figure

`::: figure` frames a diagram so it scrolls instead of overflowing on a narrow
screen, and adds an optional caption and accessible label.

| Attribute | What it does |
| --- | --- |
| `caption` | Mono caption rendered under the figure. |
| `label` | `aria-label` on the figure, describing the diagram for screen readers. |

### Reusing a diagram across decks

When the same diagram appears in several decks, keep it in a file and pull it in
with an include — the deck file stays the source of truth for the slides, and
the SVG has one home:

```markdown
::: figure {caption="Shared pipeline diagram"}
{{ include "diagrams/pipeline.svg" }}
:::
```

## Deck palette

Slides read these CSS variables, so site CSS can retune a deck without forking
the stylesheet:

| Variable | Role |
| --- | --- |
| `--deck-ground` | Page background |
| `--deck-panel` | Box background |
| `--deck-rule` / `--deck-rule-bright` | Hairlines and borders |
| `--deck-ink` / `--deck-ink-dim` | Body text and secondary text |
| `--deck-cyan` / `--deck-amber` / `--deck-rose` / `--deck-leaf` | Accents |
| `--deck-accent` | The current slide's accent |
| `--deck-font-display` / `--deck-font-body` / `--deck-font-mono` | The type trio |
