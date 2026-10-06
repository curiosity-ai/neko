---
title: Document components
label: Document components
description: The Markdown that maps onto the page layouts — columns, notes, key points, diagrams, facts, entries, steps, stats, quotes and a glossary.
order: 4
icon: cube
---

# Document components

Write plain Markdown where plain Markdown already says the right thing —
headings, paragraphs, lists, tables, code, images and task lists all take the
theme. Reach for a component where it doesn't. Every other Neko component still
works inside a page.

!!! info Document components are scoped to documents
`cols`, `col`, `box`, `note`, `keypoint`, `lead`, `claim`, `figure`, `flow`,
`facts`, `entries`, `entry`, `steps`, `step`, `stats`, `stat`, `quote`,
`glossary`, `term`, `aside` and `art` only mean what this page describes **inside
a document**. Everywhere else `::: name` stays Neko's
[generic container](/components/container.md).
!!!

> A container with containers inside needs **more colons than the inner one** —
> five outside, four for a column, three for a note.

## Columns

```markdown
::::: cols {count="3"}
:::: col
First column.
::::

:::: col
Second column.
::::
:::::
```

`ratio="1:1.1"` sets the column widths. In the Word export the columns are a table.

## Label, note and key point

A paragraph ending in `{.doc-label}` is a mono label (`1.1`, `Table 1.1`). A note
is a rule, a mono label and a muted line; a key point is the line to take away.

```markdown
1.1 {.doc-label}

::: note {label="BM25"}
Ranks by how often a term appears.
:::

::: keypoint
Keep keyword for identifiers. Add vectors for meaning.
:::
```

`::: note {label="In one paragraph" ground="stone"}` sets the note on a ground.
`::: claim` (or `lead`) is a line set large.

## Flow diagrams

A bullet list becomes boxes and arrows, drawn as SVG (a picture in Word). One
bullet is one **row**; boxes in a row are separated by `||`; each box is
`label | title | note`. `[mark]` puts the signal square in a box and `[solid]`
inverts it.

```markdown
::: flow {caption="Fig. 1.1 · The six layers of search."}
- 01 · Keyword | Keyword | Exact terms || [solid] 06 · Agents | Agents | Search as a tool
:::
```

A single row runs left to right; several rows run top to bottom and a row of two
forks the arrow (`layout="path"`); `layout="compact"` is a narrow vertical stack.
`plate="stone"` puts the figure on a full-bleed plate.

## Facts, entries, steps, stats, terms

```markdown
::: facts
Topic
:   Search architecture

Date
:   October 2026
:::

:::: entries
::: entry {num="01" title="Search is not one thing" page="03"}
Why most disagreements are about which layer is meant.
:::
::::

:::: steps
::: step {num="01" ground="ink"}
Index every identifier field first.
:::
::::

:::: stats
::: stat {value="30TB+" label="Data connected" ground="paper"}
:::
::::

:::: glossary
::: term {name="BM25"}
A keyword ranking.
:::
::::
```

`entries {style="takeaways"}` sets the entries as numbered takeaways.

## Quote

An image alone on the first line is the customer's mark; it is drawn in the page's
ink, through the image as a mask.

```markdown
::: quote {by="Services Innovation Team · Airline Services"}
![Airbus](assets/airbus.svg)

“By intelligently enhancing our search efficiency…”
:::
```

## Aside, figure, art

`::: aside` is the sidebar column of the `sidebar` layout. `::: figure
{caption="Listing 2.1"}` wraps a code block or image with a mono caption.
`::: art {kind="bars" width="595" height="260"}` places generated art in the flow.

## Table marks

`[!mark yes]`, `[!mark part]` and `[!mark no]` are small squares for a table
cell — solid, grey and outlined — a legend that survives grayscale printing.
`[!tag text="curiosity.ai/request-demo" tone="solid"]` is a pill.

A task list (`- [ ] Item`) becomes a checklist of square boxes.
