---
name: badge
description: Render a Neko Badge — small inline label with optional variant, icon, size, corner style, and link. Use for tags, status pills, version markers, NEW/BETA flags, etc.
---

# Badge

Inline badge. Looks like a small pill of text with an optional icon. Visually
similar to a Button, but typically used as a label rather than a primary CTA.

## Syntax

```markdown
[!badge Badge Text]
[!badge variant="success" text="Approved"]
[!badge variant="info" icon="user" text="User"]
[!badge variant="danger" text="Beta" size="l" corners="pill"]
[!badge icon=":heart:" text="Like"](https://example.com)
```

The optional `(url)` at the end turns the whole badge into a link.

## Attributes

| Attribute   | Values                                                                                  | Notes |
| ---         | ---                                                                                     | --- |
| `text`      | string                                                                                  | Label content. Implicit when written as `[!badge Some text]`. |
| `variant`   | `base`, `primary` (default), `secondary`, `success`, `danger`, `warning`, `info`, `light`, `dark`, `ghost`, `contrast` | Colour scheme. |
| `size`      | `xs`, `s`, `m`, `l`, `xl`, `2xl`, `3xl`                                                 | Default `s`. |
| `corners`   | `round` (default), `square`, `pill`                                                     | Corner radius. |
| `icon`      | UIcon name, `:emoji:`, `<svg>`, or image path                                            | Optional icon. |
| `iconAlign` | `left` (default), `right`                                                               | Icon position. |
| `margin`    | `top right bottom left` in px (e.g. `"0 8 0 0"`)                                        | Spacing override. |
| `link`      | url/path                                                                                | Same as the parenthesised link. |
| `target`    | `blank`, `self`, `parent`, `top`                                                        | Link target. |

## Examples

```markdown
[!badge variant="success" text="stable"]
[!badge variant="warning" text="beta" corners="pill"]
[!badge variant="info" icon="rocket" text="v2.0"]
[!badge icon=":zap:" text="fast"](performance.md)
```

## Frontmatter shorthand

In a page's `nav.badge` you can use the pipe shorthand `TEXT|variant`:

```yml
---
nav:
  badge: NEW|info
---
```

Or the full object form with the same attributes as above.
