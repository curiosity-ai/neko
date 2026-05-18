# CLAUDE.md — Writing Neko documentation

This file gives Claude (and any agent) the context it needs to author and edit
documentation in a [Neko](https://neko.curiosity.ai) project. It applies to
**this folder and everything below it**. If you are looking at the Neko CLI/.NET
source, see the root `.claude/CLAUDE.md` instead.

## What Neko is

Neko is a static site generator. It turns a tree of Markdown files into a
documentation website. You write `.md`, Neko produces HTML. There is no JS
framework, no build step on top, no SSR — just Markdown plus a small set of
extensions ("components") that are recognised at build time.

The CLI lives at the root of the parent repository. You only need two commands:

- `neko start` — live-reload dev server. Watches the folder, rebuilds, refreshes.
- `neko build` — one-shot static build into the `output:` folder (default `.neko`).

## Folder structure

Neko discovers content automatically. Anything ending in `.md` becomes a page;
any folder becomes a section in the left sidebar. There is no manifest of routes.

A typical project looks like:

```
my-docs/
├── neko.yml                # project configuration (root)
├── index.md                # homepage
├── about.md
├── assets/                 # images, fonts, files, screenshots
│   └── logo.png
├── guides/
│   ├── index.yml           # folder configuration (label/icon/order)
│   ├── installation.md
│   └── getting-started.md
├── components/
│   ├── alert.md
│   └── tab.md
└── _includes/              # partials, available to `{{ include "..." }}`
    └── head.html
```

Important conventions:

- **`neko.yml`** at the root is the project config. A nested `neko.yml`
  triggers **multi-repo mode** and creates a sub-site at that path.
- **`index.md`** or **`readme.md`** is the default page for a folder.
- **`index.yml`** configures the folder itself (label, icon, order, expanded,
  permalink, visibility). It is *not* a page.
- **Filenames starting with `_`** are ignored by the builder. Use `_includes/`,
  `_drafts/`, etc. when you want files on disk but not in the site.
- **`assets/`** is the conventional place for images, downloads, fonts. The
  folder name is not magic — Neko will serve any non-`.md` file from the input
  tree as a static asset. But keeping assets together makes life easier.
- **Page metadata** lives in a YAML frontmatter block at the top of the `.md`,
  or in a sibling `.md`-named `.yml` file (e.g. `page.yml` next to `page.md`).
  Frontmatter wins if both are present.

## The neko.yml (project config)

Minimum useful `neko.yml`:

```yml
input: ./
output: .neko
url: docs.example.com

branding:
  title: My Project
  label: Docs
  logo: /assets/logo.png
  favicon: /assets/favicon.ico
  baseColor: "#5495f1"

theme:
  name: curiosity

meta:
  description: Short site description.
  keywords: docs, project
  author: Your Name

links:                       # top navigation (header)
  - text: Home
    link: /
    icon: house-chimney
  - text: Guides
    link: /guides/getting-started
    icon: book
  - text: GitHub
    link: https://github.com/...
    icon: brands-github

footer:
  copyright: "&copy; Copyright {{ year }}. All rights reserved."
```

Less obvious but useful keys:

- `banner:` — site-wide announcement bar (text/link/background/dismissible).
- `branding.logoDark` — alternate logo for dark mode.
- `links[].items` — dropdown menus (top nav).
- `links[].footerItems` — extra items shown only in the dropdown footer.
- `theme.name` — `curiosity` (default), or any theme name supported by Neko.

For the exhaustive list, see [Neko's project config docs](https://neko.curiosity.ai/configuration/core/project)
or the source `Neko/Configuration/NekoConfig.cs` in the parent repo.

## Page frontmatter

Place a `---`-fenced YAML block at the top of any `.md`:

```md
---
label: Friendly nav label
icon: rocket             # UIcon, :emoji:, <svg>…</svg>, or /path/to/img
order: 100               # higher = higher in sidebar; negative = bottom
tags: [guide, install]
category: news
date: 2026-01-15
author: Frank
layout: default          # default | page | central | blog
visibility: public       # public | hidden | protected | private
permalink: /custom/url
redirect: /other/page
nextprev: { mode: hide } # show | hide | exclude
toc: { depth: 2-3, label: "On this page" }
backlinks: { enabled: false }
meta:
  title: "Custom <title> override"
  description: "Custom description for SEO."
nav:
  badge: NEW|info        # text|variant shorthand
---
```

Rules of thumb:

- Prefer a real `# Page Title` H1 over `title:` in frontmatter.
- Use `label:` to shorten/rename the sidebar entry without changing the H1.
- `order:` is the lever for reordering pages and folders.
- Folders use the same metadata via `index.yml`.

## Assets and links

- Reference images and downloads with **root-relative paths** when possible:
  `/assets/logo.png`. This is robust under `permalink` and multi-repo modes.
- Markdown image syntax works:
  `![Alt](/assets/diagram.png)` — Neko adds captions, sizing, alignment via
  attributes (see Image component).
- PDFs embed inline simply by linking them as images:
  `![Manual](/assets/manual.pdf)`.
- Cross-page links: prefer the file path so Neko can validate them and produce
  smart links: `[See Install](guides/installation.md)`. Wiki-style
  `[[installation]]` also works.

## Components — overview

Components extend Markdown without leaving Markdown. They fall into a few
syntactic families. **Use the specialised skills under `.claude/skills/` for
the full per-component reference** — this section is just a map.

| Family             | Syntax marker          | Examples                          |
| ---                | ---                    | ---                               |
| Alert / Callout    | `!!! variant`          | alert, callout                    |
| Tab                | `+++`                  | tab                               |
| Column             | `\|\|\|`               | column                            |
| Steps              | `>>>`                  | steps                             |
| Panel (collapse)   | `===` / `==-`          | panel                             |
| Inline shortcode   | `[!name attr=…]`       | badge, button, ref, file, embed, lesson, command-example, color-chip |
| Fence block        | ` ```lang `            | code blocks, mermaid, force-graph, workflow, tesserae, csharp-docs, math-formulas |
| Container          | `::: name { attrs }`   | card, example, generic container  |
| Inline emoji/icon  | `:name:` / `:icon-…:`  | emoji, icon                       |
| Image attrs        | `![cap](url){…}`       | image, pdf                        |
| Front-matter only  | n/a                    | banner (project-level), backlinks |

Components you should know first:

- **Alert** (`!!! variant Title … !!!`) — info / success / warning / danger / tip / question.
- **Tab** (`+++ Title … +++`) — tabbed content panels, anchorable.
- **Column** (`||| Title … ||| Title2 … |||`) — multi-column layouts.
- **Steps** (`>>> Title … >>>`) — numbered tutorial steps.
- **Card** / **card-grid** (`::: card { attrs } … :::`) — feature/landing tiles.
- **Mermaid** (` ```mermaid `) — diagrams.
- **Code block** (` ```lang title.ext #2-4 chrome="mac" `) — code with title, line
  numbers, range highlights, and macOS/Windows chrome.
- **Badge** / **Button** / **Ref** (`[!badge …]`, `[!button …]`, `[!ref …]`).
- **Icon** (`:icon-name:`) — uses Flaticon UIcons (Regular Rounded). Icons are
  also accepted in frontmatter `icon:` and component `icon=` attributes.

For every component, look up its skill:
`.claude/skills/<component>/SKILL.md`. Each skill includes syntax, every
attribute, and a copy-paste example.

## Writing style for Neko docs

- One H1 per page (or none, with `title:` frontmatter — but H1 is preferred).
- Lead with what the page is for in one or two sentences.
- Prefer concrete examples to abstract description.
- Show, then explain. A code/component block followed by a short paragraph
  beats a long paragraph followed by code.
- Cross-link aggressively. Use `[[Foo]]` or `[Foo](path.md)`.
- Tag pages so the auto-generated `/tags/` and `/categories/` index pages are
  useful.
- Tables are excellent for option references; use `{.compact}` on long ones.

## Templating and includes

Neko supports a small templating layer:

- `{{ include "snippets/foo.md" }}` — inline another file from `_includes/`
  (or any path). Use this to share boilerplate, e.g. install instructions,
  changelogs, footer notes.
- `{{ year }}` — current year (useful in `footer.copyright`).
- Templating can be disabled per page with `templating: false` in frontmatter.

## Multi-repo / nested projects

If you put another `neko.yml` inside a subfolder, Neko treats that subfolder as
its own documentation project mounted at that path (e.g. `api-docs/neko.yml` →
`/api-docs/`). Child projects inherit `theme`, `branding`, and snippets from
the parent and can override anything.

## Quick checklist before committing docs

- [ ] Every page has an H1 (or explicit `title:`) and an `icon:` if it sits in
      the sidebar.
- [ ] New folder? Add an `index.yml` with at least `label`, `icon`, `order`.
- [ ] Images live under `assets/` and are referenced with root-relative paths.
- [ ] Components used are valid — see the matching skill in `.claude/skills/`.
- [ ] `neko build` succeeds with no warnings.
- [ ] Spot-check the rendered output (`neko start`) for layout and dark mode.

## Skills available in this folder

The `.claude/skills/` folder contains one skill per Neko component. Skills are
just `SKILL.md` files; load them on demand when you need precise syntax for a
component. The set covers:

alert · backlinks · badge · banner · button · callout · cards · code-block ·
code-inline · code-snippet · color-chip · column · command-example · comments · container ·
csharp-docs · embed · emoji · example · file · file-download · force-graph ·
icon · image · img-gen · lesson · list · math · math-formulas · mermaid ·
panel · pdf · reference-link · snapframe · steps · tab · table · tesserae ·
workflow · youtube · frontmatter · neko-yml · folder-index.

When you add a new component to Neko or change a component's syntax, **update
the matching skill** so this template stays in sync with the engine.
