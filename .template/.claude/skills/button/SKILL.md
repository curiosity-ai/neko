---
name: button
description: Render a Neko Button — a prominent clickable element with variant, size, icon, and link. Use for primary calls-to-action like "Get started", "Download", "Open dashboard".
---

# Button

Buttons are clickable elements used for primary actions or navigation. They
share most attributes with [Badge](../badge/SKILL.md) but render larger with
a clearly clickable surface.

## Syntax

```markdown
[!button Button Text]
[!button variant="primary" text="Click Me"](url)
[!button icon="rocket" iconAlign="right" text="Send"](url)
[!button size="l" corners="pill" text="Large pill"](url)
[!button icon=":rocket:" text="Launch"](#anchor)
```

## Attributes

| Attribute   | Values                                                                                  | Notes |
| ---         | ---                                                                                     | --- |
| `text`      | string                                                                                  | Implicit when written as `[!button Text]`. |
| `variant`   | `base`, `primary` (default), `secondary`, `success`, `danger`, `warning`, `info`, `light`, `dark`, `ghost`, `contrast` | Colour scheme. |
| `size`      | `xs`, `s`, `m`, `l`, `xl`, `2xl`, `3xl`                                                 | Default `m`. |
| `corners`   | `round` (default), `square`, `pill`                                                     | Corner radius. |
| `icon`      | UIcon name, `:emoji:`, `<svg>`, or image path                                            | Optional icon. |
| `iconAlign` | `left` (default), `right`                                                               | Icon position. |
| `target`    | `blank`, `self`, `parent`, `top`                                                        | Link target. |

## Examples

```markdown
[!button variant="success" text="Install" icon="download"](#install)

[!button variant="primary" size="l" corners="pill" text="Get started"](getting-started.md)

[!button variant="ghost" icon="brands-github" text="GitHub" target="blank"](https://github.com/curiosity-ai/neko)
```

## When to use Button vs Badge vs Ref

- **Button** — primary call-to-action, deserves attention.
- **Badge** — inline label (status, tag, version marker).
- **Ref** — card-style reference link, more visible than a regular hyperlink
  but less prominent than a button.
