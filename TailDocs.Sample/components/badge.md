---
title: Badge Component
---

# Badge

Badges are used to display small pieces of information, such as status, version, or tags.

## Syntax

Use `[!badge text="Value" variant="primary"]`.

### Examples

- Default: [!badge text="v1.0.0"]
- Primary: [!badge text="New" variant="primary"]
- Secondary: [!badge text="Draft" variant="secondary"]
- Success: [!badge text="Done" variant="success"]
- Danger: [!badge text="Error" variant="danger"]
- Warning: [!badge text="Pending" variant="warning"]
- Info: [!badge text="Info" variant="info"]
- Light: [!badge text="Light" variant="light"]
- Dark: [!badge text="Dark" variant="dark"]

### Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `text` | The text to display. | Required |
| `variant` | The color variant (primary, secondary, success, danger, warning, info, light, dark). | `primary` |
| `icon` | An icon name (e.g., `user`). | None |

### With Icon

[!badge text="User" icon="user" variant="info"]
