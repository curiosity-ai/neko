---
order: 4
label: Changelog
icon: memo
tags: [config]
---
# Changelog

Neko can build a polished, timeline-style **changelog** page out of a folder of
per-version Markdown files. Instead of maintaining one ever-growing file, you
add a small file per release, name it after its version, and Neko stitches them
together — newest first — into a single page.

!!!
This is the recommended way to keep a changelog in Neko. The page you are
reading documents the engine itself; the live result is the
[Neko changelog](/changelog).
!!!

---

## How it works

A folder becomes a changelog when its [folder configuration](/configuration/folder.md)
(`index.yml` or `<foldername>.yml`) sets `changelog: true`:

```yml index.yml
changelog: true
title: Changelog
description: All notable changes to this project.
icon: memo
order: 2
```

Inside that folder, add **one Markdown file per release**, named after the
version it documents:

```
changelog/
├── index.yml      # changelog: true + title/description
├── v1.2.0.md
├── v1.1.0.md
└── v1.0.0.md
```

For every file Neko:

1. **Parses the file name as a version** (a leading `v` is optional).
2. **Sorts** all entries from newest to oldest.
3. **Builds a single page** at the folder URL (e.g. `/changelog`) showing each
   version as a timeline entry: a version badge, an optional headline and date,
   then the file's body.

The version files are **not** emitted as standalone pages, the folder collapses
to a single entry in the sidebar, and the aggregated page is what gets indexed
for search and listed in the `sitemap.xml`.

---

## Version file names

The file name (without extension) is parsed into numeric components for
ordering. A leading `v`/`V` and `.`, `-` or `_` separators are all accepted:

| File name      | Display | Notes                                   |
| ---            | ---     | ---                                     |
| `v1.2.0.md`    | `v1.2.0` | Semantic versioning.                    |
| `1.0.0.md`     | `v1.0.0` | A `v` prefix is added for display.      |
| `v26.6.md`     | `v26.6`  | Calendar versioning (`vYY.M`).          |
| `v26.3.16.md`  | `v26.3.16` | Calendar versioning with a patch.     |
| `2024.06.18.md`| `v2024.06.18` | Date-based versioning.             |

Ordering is numeric and component-by-component, so `v26.6` (June) correctly
sorts **above** `v26.3.16` (a March patch).

---

## Entry frontmatter

Each version file is a normal Markdown page. Two optional frontmatter keys
shape its appearance in the timeline:

=== title : `string`

An optional headline shown next to the version badge — e.g. `Ready for Production`.
If omitted, only the version badge is shown.

```md v1.0.0.md
---
title: Ready for Production
---
We are excited to announce the initial production release.
```
===

=== date : `string`

An optional date shown in the timeline's left column. It is rendered verbatim,
so any format works (`2024-06-18`, `Jun 2026`, …).

```md v1.0.0.md
---
date: 2024-06-18
---
```
===

The rest of the file is ordinary Markdown — use any [component](/components/components.md)
you like (alerts, images, code blocks, tabs…).

---

## title

=== title : `string`

The page heading for the aggregated changelog, set on the folder `index.yml`.
Used when the folder has no `index.md`.

```yml index.yml
changelog: true
title: Changelog
```
===

---

## description

=== description : `string`

A lead paragraph rendered under the title.

```yml index.yml
changelog: true
title: Changelog
description: All notable changes to this project.
```
===

---

## Adding an intro

To prepend custom content above the timeline, add an `index.md` to the
changelog folder. Its content (and H1) become the page header, and the version
timeline is appended below it. When an `index.md` is present, the `title` /
`description` from `index.yml` are only used as fallbacks.

---

## Example

```yml changelog/index.yml
changelog: true
title: Changelog
description: All notable changes to Acme.
icon: memo
order: 2
```

```md changelog/v1.0.0.md
---
title: First stable release
date: 2024-06-18
---
* **Feature**: Shipped the public API.
* **Fix**: Resolved the login redirect loop.
```

This produces a `/changelog` page with a single `v1.0.0` entry. Add a
`v1.1.0.md` next to it and it automatically appears **above** `v1.0.0`.
