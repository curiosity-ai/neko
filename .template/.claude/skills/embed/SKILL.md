---
name: embed
description: Embed an external URL (iframe, video, CodePen, etc.) with optional caption, aspect ratio, dimensions, and fullscreen control. Use for live demos and third-party content that won't fit inline.
---

# Embed

Embeds external content inside an iframe. Aspect ratio, dimensions, and
fullscreen behaviour are all configurable.

## Syntax

```markdown
[!embed](https://example.com/widget)
[!embed aspect="16:9"](https://www.youtube.com/embed/VIDEO_ID)
[!embed aspect="4:3"](https://example.com)
[!embed height="120"](https://example.com)
[!embed width="300" height="600"](https://example.com)
[!embed allowFullScreen="false"](https://example.com)
[!embed el="iframe"](https://example.com)
[!embed text="Live demo"](https://example.com)
```

## Attributes

| Attribute         | Notes                                                            |
| ---               | ---                                                              |
| `aspect`          | `16:9`, `4:3`, `1:1`, …                                          |
| `width`           | CSS width or px number.                                          |
| `height`          | CSS height or px number.                                         |
| `allowFullScreen` | `true` (default) or `false`.                                     |
| `el`              | Element type — defaults to `iframe`.                             |
| `text`            | Optional caption rendered below the frame.                       |

The URL goes in the parenthesised `()` part, just like a link.

## Examples

```markdown
[!embed aspect="16:9" text="Product walkthrough"](https://www.youtube.com/embed/dQw4w9WgXcQ)

[!embed aspect="1:1"](https://example.com/widget)
```

## Tips

- Prefer the dedicated [`youtube`](../youtube/SKILL.md) component for YouTube
  links — it auto-detects the URL.
- For PDFs, use a regular image link: `![](file.pdf)`.
- If the embedded site sends `X-Frame-Options: DENY`, no embed library can
  override that — link to the page instead.
