---
title: Badge Component
---

# Badge

Badges (or chips, tags) are small labels used to highlight status, version, or other metadata.

## Syntax

Use the `[!badge ...]` syntax to add a badge. Attributes are used to customize the badge.

```markdown
[!badge text="New" variant="primary" corners="pill" size="m" icon="star"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `text` | The text content of the badge. | (Required) |
| `variant` | The color variant of the badge. | `base` |
| `corners` | The shape of the corners: `round`, `square`, or `pill`. | `round` |
| `size` | The size of the badge: `xs`, `s`, `m`, `l`, `xl`. | `m` |
| `icon` | An icon name (Flaticon) or an emoji (starting with `:`) to prepend to the text. | - |

## Variants

Available variants: `base`, `primary`, `success`, `danger`, `warning`, `info`.

- [!badge text="Base" variant="base"]
- [!badge text="Primary" variant="primary"]
- [!badge text="Success" variant="success"]
- [!badge text="Danger" variant="danger"]
- [!badge text="Warning" variant="warning"]
- [!badge text="Info" variant="info"]

## Shapes

Available shapes: `round` (default), `square`, `pill`.

- [!badge text="Round" corners="round"]
- [!badge text="Square" corners="square"]
- [!badge text="Pill" corners="pill"]

## Sizes

Available sizes: `xs`, `s`, `m` (default), `l`, `xl`.

- [!badge text="Extra Small" size="xs"]
- [!badge text="Small" size="s"]
- [!badge text="Medium" size="m"]
- [!badge text="Large" size="l"]
- [!badge text="Extra Large" size="xl"]

## With Icons

You can add icons to badges using the `icon` attribute.

- [!badge text="Verified" variant="success" icon="check"]
- [!badge text="Star" variant="warning" icon="star"]
- [!badge text="Rocket" variant="primary" icon="rocket-lunch"]
- [!badge text="Emoji" variant="info" icon=":rocket:"]
