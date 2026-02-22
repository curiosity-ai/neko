---
title: Reference Link Component
icon: link-alt
---

# Reference Link

The Reference Link component creates a styled link with an icon, designed for referencing other documents or external resources.

## Syntax

Use the `[!ref text="..." link="..."]` syntax.

```markdown
[!ref text="GitHub Repository" link="https://github.com/my-repo"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `text` | The text to display. | (Required) |
| `link` | The URL to link to. | `#` |

### Example

Here are some useful links:

- [!ref text="TailDocs Documentation" link="../index"]
- [!ref text="Official Website" link="https://example.com"]
