---
title: Button Component
---

# Button

Buttons are used to create actionable links, such as "Download", "Get Started", or "Learn More".

## Syntax

Use the `[!button ...]` syntax. Attributes control the button's appearance and behavior.

```markdown
[!button text="Click Me" link="#" variant="primary" icon="arrow-right"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `text` | The text label of the button. | (Required) |
| `link` | The URL the button links to. | `#` |
| `variant` | The visual style of the button. | `primary` |
| `icon` | An optional icon to display before the text. | - |

## Variants

TailDocs offers several button variants:

- `primary`: Standard blue button.
- `secondary`: Gray button for less prominent actions.
- `outline`: Transparent background with colored border.
- `danger`: Red button for critical actions.

### Examples

[!button text="Primary Button" variant="primary"]

[!button text="Secondary Button" variant="secondary"]

[!button text="Outline Button" variant="outline"]

[!button text="Danger Button" variant="danger"]

## With Icons

Buttons can include icons for better visual cues.

[!button text="Download Now" variant="primary" icon="download"]

[!button text="Next Page" variant="outline" icon="arrow-right"]

[!button text="Warning" variant="danger" icon="exclamation"]
