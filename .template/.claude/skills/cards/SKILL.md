---
name: cards
description: Render Neko Card and card-grid blocks — content tiles with optional image, icon, title, tags, link, and palette. Use for landing pages, feature grids, navigation hubs.
---

# Cards

Cards are flexible content tiles. They support multiple layouts (stacked,
horizontal, grid, link) and can be combined into a `card-grid` for responsive
multi-column layouts.

## Single card

```markdown
::: card { image="/assets/photo.jpg" title="Photo" tags="landscape, nature" }
Beautiful sunset photo.
:::
```

## Card variants

| `variant`     | Description                                                                 |
| ---           | ---                                                                         |
| (default)     | Stacked card with optional image at top.                                    |
| `horizontal`  | Image on the side, content next to it.                                      |
| `grid`        | Compact tile suitable for a card grid (use icon or small image).            |
| `link`        | Renders as a clickable card with title + description.                       |

## Attributes

| Attribute | Description                                                              |
| ---       | ---                                                                      |
| `image`   | Path/URL to an image displayed on the card.                              |
| `icon`    | UIcon name, `:emoji:`, `<svg>`, or image path (alternative to `image`).  |
| `title`   | Card heading.                                                            |
| `tags`    | Comma-separated tag list rendered as small badges.                       |
| `link`    | Makes the card clickable.                                                |
| `target`  | `blank`, `self`, `parent`, `top` for the link.                           |
| `palette` | Named colour palette (`blue`, `green`, `red`, etc.) for grid variant.    |
| `variant` | One of the variants above.                                               |

## Card grid

```markdown
:::: card-grid
::: card { variant="grid" icon="rocket" title="Get started" link="/guides/getting-started" palette="blue" }
Install Neko and run your first build.
:::
::: card { variant="grid" icon="cube" title="Components" link="/components/components" palette="green" }
The complete component reference.
:::
::: card { variant="grid" icon="palette" title="Themes" link="/guides/themes" palette="purple" }
Customise the look of your site.
:::
::::
```

Notes:
- `::::` (four colons) opens the **grid container**; `:::` (three colons) opens
  each card inside it.
- Cards inside a grid should use `variant="grid"` for consistent sizing.
- The grid is responsive — cards reflow to one column on mobile.

## When to use Cards

- Landing pages and section index pages.
- Feature comparisons.
- Navigation hubs (e.g. a docs root listing major sections).

For a small inline link, prefer [`reference-link`](../reference-link/SKILL.md);
for a primary action, prefer [`button`](../button/SKILL.md).
