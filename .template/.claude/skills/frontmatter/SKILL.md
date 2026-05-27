---
name: frontmatter
description: Author the YAML frontmatter block of a Neko Markdown page — label, icon, order, tags, visibility, permalink, redirect, layout, meta, nav, toc, backlinks, password. Use when configuring an individual page.
---

# Page frontmatter

Page metadata lives in a `---`-fenced YAML block at the top of a `.md`, or in
a sibling `.yml` file with the same base name (e.g. `page.yml` next to
`page.md`). Frontmatter wins if both exist.

```md
---
label: Friendly nav label
icon: rocket
order: 100
tags: [guide, install]
visibility: public
---
# Page title
```

## Frequently used keys

| Key              | Type / values                                       | Notes |
| ---              | ---                                                 | --- |
| `label`          | string                                              | Override the sidebar + top-nav label. Also the `<title>` fallback when no `title` is set. |
| `icon`           | UIcon name, `:emoji:`, `<svg>`, image path          | Sidebar / breadcrumb icon. |
| `order`          | number / alpha string / `vSemver`                   | Reorders within parent folder. Higher number = higher position. |
| `tags`           | list                                                | Auto-generates `/tags/<tag>/` index pages. |
| `category`       | string or list                                      | Auto-generates `/categories/<cat>/` pages. |
| `date`           | `yyyy-mm-dd` or `yyyy-mm-ddThh:mm`                  | Used by blog ordering. |
| `author`         | string, email, list, object (`name`, `email`, `link`, `avatar`) | Shown under the title. |
| `layout`         | `default`, `page`, `central`, `blog`                | Page chrome variant. |
| `visibility`     | `public`, `hidden`, `protected`, `private`          | Navigation & search behaviour; password gating. |
| `password`       | string                                              | Per-page encryption. Pair with `--password` at build. |
| `searchExclude`  | boolean                                             | `true` to omit the page from `search.json`. Folder-level: set the same key in `index.yml`. |
| `permalink`      | string                                              | Custom URL. Wins over the file path. |
| `redirect`       | string                                              | Redirect this slug to another page or URL. |
| `target`         | `blank`, `self`, `parent`, `top`                    | How sidebar link opens. |
| `templating`     | boolean                                             | `false` to disable `{{ … }}` for this page. |
| `expanded`       | boolean                                             | Folder navigation: expand on load (used in `index.yml`). |
| `breadcrumb`     | boolean                                             | Hide the breadcrumb on this page. |
| `nav.badge`      | string `TEXT|variant` or object                     | Badge next to the sidebar entry. |
| `nav.mode`       | `default`, `stack`                                  | Per-top-level-folder navigation style. |
| `nextprev.mode`  | `show`, `hide`, `exclude`                           | Next/prev button visibility and sequence membership. |
| `toc.depth`      | `2`, `2-3` (default), `1-4`, `2,4`, etc.            | Right-sidebar heading depth. |
| `toc.label`      | string                                              | Right-sidebar heading. |
| `backlinks.*`    | `enabled`, `title`, `maxResults`                    | See [`backlinks`](../backlinks/SKILL.md). |
| `meta.title`     | string                                              | Custom `<title>` value. |
| `meta.description` | string                                            | Custom meta description. |
| `image`          | path or URL                                         | Feature image (overrides auto-detected first image). |
| `cover`          | path or URL                                         | Hero cover image. |
| `title`          | string                                              | Discouraged — write a real `# H1` instead. |

## Order rules in one paragraph

`order: 100` beats `order: 10` beats no-order alpha beats `order: zulu` beats
`order: v1.0` beats `order: -10`. Folders cluster at the top of each `order`
group. Home page defaults to `order: 10000`.

## Sibling .yml form

```
page.md
page.yml   ← only one or the other, frontmatter wins if both
```

## Tips

- Put **all** frontmatter at the very top of the file, no blank lines before
  the first `---`.
- Stick to lowercase keys.
- Prefer `label` over `title`; prefer a real `# H1`.
- Use `_drafts/` (folder prefix `_`) for unfinished pages.
