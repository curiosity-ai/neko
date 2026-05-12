---
name: color-chip
description: Render a small color swatch with hex code or label. Use in design-system / palette documentation to preview colours inline.
---

# Color chip

Renders a colour swatch next to its hex code (or custom label).

## Syntax

```markdown
[!color-chip #5495f1]
[!color-chip #5495f1 text="Primary Blue"]
[!color-chip color="#6610f2" text="Indigo"]
```

## Attributes

| Attribute | Notes                                                                  |
| ---       | ---                                                                    |
| `color`   | Any valid CSS colour: hex (`#5495f1`), rgb, rgba, hsl, or named.       |
| `text`    | Optional label. Falls back to the colour value if omitted.             |

The first positional token after `[!color-chip` is interpreted as the colour
when no `color=` attribute is given.

## Examples

```markdown
| Token         | Sample                                  |
| ---           | ---                                     |
| brand.primary | [!color-chip #5495f1 text="Brand primary"] |
| brand.accent  | [!color-chip #f59e0b text="Brand accent"]  |
| danger        | [!color-chip color="#dc2626" text="Danger"] |
```

## When to use

- Design-system reference pages.
- Theme documentation listing the available palette.
- Comparison tables of light/dark token values.
