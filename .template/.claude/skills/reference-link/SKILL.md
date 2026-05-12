---
name: reference-link
description: Render a prominent reference link card with optional icon and text — bigger than a plain hyperlink, smaller than a button. Use for "see also" sections, related-page boxes, and external resource lists.
---

# Reference link

A reference link is a card-style hyperlink. It is more prominent than a plain
Markdown link and lighter weight than a [`button`](../button/SKILL.md).

## Syntax

```markdown
[!ref](/guides/getting-started.md)
[!ref Getting Started](/guides/getting-started.md)
[!ref text="Learn more" icon="rocket"](/guides/learn.md)
[!ref text="External" target="blank"](https://example.com)
```

## Attributes

| Attribute | Notes                                                          |
| ---       | ---                                                            |
| `text`    | Optional label. Falls back to the linked page's title.         |
| `icon`    | UIcon name, `:emoji:`, `<svg>`, or image path.                 |
| `target`  | `blank`, `self`, `parent`, `top`.                              |

When a target page is internal, Neko fetches its title and description
automatically so the card is rich without extra attributes.

## Examples

```markdown
[!ref](/guides/installation.md)
[!ref Installation guide](/guides/installation.md)
[!ref icon="brands-github" text="GitHub" target="blank"](https://github.com/curiosity-ai/neko)
```

## When to use Ref vs Card vs Button

- **Ref** — inline "see also" link card, mainly text.
- **Card** — richer tile with image/icon/tags, used in grids.
- **Button** — primary action, deserves attention.
