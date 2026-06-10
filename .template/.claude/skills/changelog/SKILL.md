---
name: changelog
description: Build a timeline changelog page from a folder of version-named Markdown files. Mark the folder with `changelog: true` in its index.yml; name one file per release (v1.2.0.md). Use when documenting releases / "what's new".
---

# Changelog (version-folder)

Neko turns a folder into a single, timeline-style changelog page. You keep one
small Markdown file per release — named after its version — and Neko parses each
file name as a version, sorts them newest-first, and renders one page at the
folder URL. Version files are **not** standalone pages; the folder collapses to
a single sidebar entry, and the aggregated page is what is indexed for search.

## Setup

Mark a folder as a changelog with its folder config (`index.yml` or
`<foldername>.yml`):

```yml
# changelog/index.yml
changelog: true
title: Changelog
description: All notable changes to this project.
icon: memo
order: 2
```

Then add one file per release, named after the version:

```
changelog/
├── index.yml
├── v1.2.0.md
├── v1.1.0.md
└── v1.0.0.md
```

## Folder config keys

| Key           | Notes                                                            |
| ---           | ---                                                              |
| `changelog`   | `true` to turn the folder into a changelog. Required.            |
| `title`       | Page heading (used when there is no `index.md`).                 |
| `description` | Lead paragraph under the title.                                  |
| `icon`        | Sidebar icon (UIcon / `:emoji:` / image path).                  |
| `order`       | Sidebar position, same rules as any folder.                     |
| `label`       | Sidebar text (defaults to `title`, then the folder name).        |

## Version file names

The file name (minus extension) is parsed into numeric components for ordering.
A leading `v`/`V` is optional; `.`, `-`, `_` separators all work.

| File          | Display      | Kind                    |
| ---           | ---          | ---                     |
| `v1.2.0.md`   | `v1.2.0`     | Semantic versioning     |
| `1.0.0.md`    | `v1.0.0`     | `v` added for display   |
| `v26.6.md`    | `v26.6`      | Calendar versioning     |
| `v26.3.16.md` | `v26.3.16`   | CalVer + patch          |

Ordering is numeric, component-by-component — `v26.6` sorts above `v26.3.16`.

## Entry frontmatter (optional)

```md
---
title: Ready for Production   # headline shown next to the version badge
date: 2024-06-18              # rendered verbatim in the timeline's date column
---
* **Feature**: Describe the change.
* **Fix**: Describe the fix.
```

Both keys are optional. The rest of the file is normal Markdown and may use any
Neko component.

## Examples

A minimal release file:

```md
# changelog/v1.0.0.md
---
title: First stable release
date: 2024-06-18
---
* **Feature**: Shipped the public API.
```

Adding `changelog/v1.1.0.md` later automatically appears **above** `v1.0.0` on
the `/changelog` page — no manual reordering.

## Intro content

To add prose above the timeline, drop an `index.md` in the folder. Its content
(and H1) become the page header and the timeline is appended below; the
`index.yml` `title`/`description` then act only as fallbacks.

## Tips

- One file per release. Don't accumulate everything in a single file.
- Keep file names purely versions (`v1.2.0.md`), not titles — put the headline
  in frontmatter `title:`.
- Link to it from `neko.yml` top nav with `link: /changelog`.
- The folder name is not special; any folder with `changelog: true` works.
