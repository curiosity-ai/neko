---
name: change
description: Render a Neko `::: change` block — one changelog entry as a coloured badge in a left column with a title and description beside it. Use when writing release notes inside a changelog folder.
---

# Change (changelog entry)

A single changelog entry. Renders as a two-column row: the change-type badge in
a fixed-width left column (so badges line up down the page), and the title plus
description to its right. Consecutive entries are separated by a hairline rule
automatically.

It belongs inside a **changelog folder** — a folder whose `index.yml` sets
`changelog: true`, holding one version-named file per release. See the
[`changelog`](../changelog/SKILL.md) skill for that folder feature, the version
file naming, and the entry frontmatter. `::: change` works in an ordinary page
too, but the separator and section styling only apply inside a changelog
timeline.

## Syntax

```markdown
::: change {badge="New" title="Folder-based changelogs"}
Changelogs are now built from a folder of version-named files.
:::
```

The body is ordinary Markdown — links, `code`, lists and other components all
work. Keep it to one to three sentences.

## Attributes

| Attribute     | Values                                                                                                          | Notes |
| ---           | ---                                                                                                             | --- |
| `badge`       | free text; `New`, `Feature`, `Improved`, `Improvement`, `Changed`, `Fixed`, `Fix`, `Deprecated`, `Removed`, `Security`, `Docs` are recognised | Badge label **and** colour. Defaults to `New`. Printed verbatim, so the casing you write is what readers see. |
| `title`       | string                                                                                                          | Bold heading of the entry. Optional, but effectively always used. |
| `badge-color` | any recognised `badge` value                                                                                    | Borrows that keyword's colour while `badge` supplies the text — use for a custom label (e.g. `badge="Preview" badge-color="feature"`). |

A `badge` outside the recognised list still renders; it just falls back to the
neutral `Improved` colours. `badge-color` overrides the colour only, never the
text.

## Badge colours

| Keyword                   | Colour                 |
| ---                       | ---                    |
| `New`, `Feature`          | primary (theme accent) |
| `Improved`, `Improvement` | neutral grey           |
| `Fixed`, `Fix`            | green                  |
| `Changed`                 | amber                  |
| `Deprecated`              | orange                 |
| `Removed`                 | red                    |
| `Security`                | purple                 |
| `Docs`                    | sky                    |

## Sections

Group entries under `#` headings prefixed with an [icon](../icon/SKILL.md)
shortcode. Inside a changelog folder each H1 renders as a labelled section
header with its icon in a coloured tile, so use H1 (not H2) for these.

```markdown
# :icon-sparkles: Features

::: change {badge="New" title="Folder-based changelogs"}
Changelogs are now built from a folder of version-named files.
:::

::: change {badge="Improved" title="Faster incremental builds"}
Only pages whose inputs changed are re-rendered.
:::

# :icon-bug: Fixes

::: change {badge="Fixed" title="Skip commented directives"}
Re-running image generation no longer regenerates commented-out directives.
:::
```

Conventional section/badge pairings: `# :icon-sparkles: Features` for
`New`/`Feature`, `# :icon-wrench-simple: Improvements` for `Improved`,
`# :icon-bug: Fixes` for `Fixed`, `# :icon-document: Documentation` for `Docs`.

## Tips

- One entry per user-visible change; describe the impact, not the mechanics.
- Put the change type in `badge=` rather than repeating it in the title (write
  `title="Faster builds"`, not `title="Improvement: faster builds"`).
- Order entries within a section newest/most significant first; badges align
  down the page regardless.

## Related

- [`changelog`](../changelog/SKILL.md) — the folder feature these entries live in.
- [`links`](../links/SKILL.md) — a version manifest card, often the first block of a release entry.
- [`badge`](../badge/SKILL.md) — the inline `[!badge …]` component, unrelated to this one.
- [`container`](../container/SKILL.md) — the generic `::: name` container syntax.
