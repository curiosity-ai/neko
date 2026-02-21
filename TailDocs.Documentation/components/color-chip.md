---
title: Color Chip Component
---

# Color Chip

Color Chips are small visual indicators of a color value, often used in design systems or color palettes.

## Syntax

Use the `[!color-chip ...]` syntax.

```markdown
[!color-chip color="#ff0000" text="Red"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `color` | The CSS color value (hex, rgb, etc.) to display. | `#000000` |
| `text` | The label for the color. Defaults to the color value if omitted. | `color` value |

### Examples

Here are some primary colors:

- [!color-chip color="#ff0000" text="Red"]
- [!color-chip color="#00ff00" text="Green"]
- [!color-chip color="#0000ff" text="Blue"]

Here are some UI colors:

- [!color-chip color="#3b82f6" text="Primary"]
- [!color-chip color="#10b981" text="Success"]
- [!color-chip color="#ef4444" text="Danger"]
- [!color-chip color="#f59e0b" text="Warning"]
