---
name: folder-index
description: Configure a folder's navigation behaviour with an `index.yml` — label, icon, order, expanded, visibility, permalink, nextprev. Use when shaping the sidebar tree.
---

# Folder configuration (`index.yml`)

A folder's appearance in the sidebar and its base URL are configured by an
`index.yml` placed inside the folder. The same keys work as page frontmatter,
just sitting in a `.yml` instead of being above content.

```yml
# guides/index.yml
label: Guides
icon: book
order: 100
expanded: true
```

## Recognised keys

| Key             | Notes                                                                |
| ---             | ---                                                                  |
| `label`         | Sidebar text for the folder node.                                    |
| `icon`          | UIcon, `:emoji:`, `<svg>`, or image path.                            |
| `order`         | Same rules as pages (higher number = higher position).               |
| `expanded`      | `true` to expand the folder on initial load.                         |
| `visibility`    | `public` (default), `hidden`, `protected`, `private`.                |
| `searchExclude` | `true` to exclude every page in this folder (recursively) from the search index. |
| `permalink`     | Base URL for everything under this folder.                           |
| `changelog`     | `true` turns the folder into a [changelog](../changelog/SKILL.md) (version-named files → one timeline page). Pairs with `title` / `description`. |
| `nextprev.mode` | `show`, `hide`, `exclude` for the whole folder.                      |
| `nav.mode`      | `default`, `stack` (per top-level folder).                           |
| `backlinks`     | Override project defaults inside this folder.                        |

## Examples

Expand a folder by default:

```yml
expanded: true
```

Reorder a folder:

```yml
order: 1000   # near the top
order: -1000  # near the bottom
```

Re-base every page under `guides/` to `/tutorials/`:

```yml
permalink: /tutorials
```

Protect a whole folder with the project password:

```yml
visibility: protected
```

Exclude every page in a folder from the search index:

```yml
searchExclude: true
```

## Alternatives

You can put the same metadata on a folder's default page (`index.md` /
`readme.md` / `default.md`) instead of creating `index.yml`. They behave the
same; pick one to avoid confusion.

## Tip

Prefix a folder with `_` (e.g. `_drafts/`) to make Neko ignore it completely
— no `visibility` field needed.
