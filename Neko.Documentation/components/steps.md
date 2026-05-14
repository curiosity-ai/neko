---
title: Steps
label: Steps
icon: list-check
order: 14
---

# Steps

The **Steps** component lays out a numbered sequence of actions — perfect for
tutorials, install procedures, and any "do A, then B, then C" flow. Each step
gets an auto-incremented circle marker connected by a vertical guide line.

## Syntax

Open a step with `>>>` followed by a title on the same line. Everything below
becomes the body of that step until the next `>>>`. Close the whole group with
a final `>>>` on a line of its own.

```markdown
>>> First step title
Body of the first step.

>>> Second step title
Body of the second step.

>>> Third step title
Body of the third step.

>>>
```

The trailing `>>>` is optional at end-of-document but recommended for clarity.

## Example

>>> **Install Neko**

Run the installation command in your terminal.

```bash
dotnet tool install -g Neko
```

>>> **Build the project**

Generate your static site.

```bash
neko build
```

>>> **Publish anywhere**

The output folder is plain HTML — drop it on any static host (Netlify, Vercel,
GitHub Pages, S3, a Raspberry Pi…).

>>>

## Step titles

Titles support inline Markdown, so you can use **bold**, *emphasis*, `code`,
and [links](#) inside them. Keep them short and use imperative phrasing
(*"Install Neko"*, not *"Installing Neko"*).

## Step bodies

A step body accepts any block-level Markdown:

- paragraphs
- lists
- fenced code blocks
- images
- [callouts](alert.md) and other components

## When to use Steps

Use Steps for content that is genuinely **sequential** — each step depends on
the previous one. For non-sequential checklists, prefer a regular task list
(`- [ ] …`). Long procedures should be split into multiple Step groups or
separate pages; aim for fewer than ten steps per group.
