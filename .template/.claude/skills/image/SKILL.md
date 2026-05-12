---
name: image
description: Embed an image with optional caption, title, dimensions, alignment, and HTML attributes. Use for screenshots, diagrams, hero images, inline figures.
---

# Image

Standard Markdown image syntax extended with sizing, alignment, captions, and
arbitrary HTML attributes.

## Basic

```markdown
![Alt text](/assets/diagram.png)
![Caption shown below](/assets/diagram.png)
![Caption](/assets/diagram.png "Title shown in tooltip")
```

## Sizing

Pipe-separated `widthxheight` or just width:

```markdown
![Resized|300x200](/assets/photo.jpg)
![Width only|300](/assets/photo.jpg)
```

Or with the attribute block:

```markdown
![Caption](/assets/photo.jpg){ width="300" height="200" }
```

## Attribute block

Add `{ … }` for id, classes, and any HTML attribute:

```markdown
![Caption](/assets/photo.jpg){ #hero .rounded-lg width="600" }
![Caption](/assets/photo.jpg){ .border .shadow loading="lazy" }
```

## Alignment

Wrap the markdown image with `-` (single) or `--` (with extra padding):

```markdown
-![Left aligned](/assets/img.png)
![Right aligned](/assets/img.png)-

--![Left plus](/assets/img.png)
![Right plus](/assets/img.png)--
```

## Tips

- Prefer **root-relative** paths (`/assets/foo.png`). They survive `permalink`
  overrides and multi-repo nesting.
- Always provide alt text — leave it empty (`![](…)`) only when the image is
  purely decorative.
- For PDF previews, the same `![]()` syntax works because Neko detects the
  `.pdf` extension. See [`pdf`](../pdf/SKILL.md).
- Add `loading="lazy"` via the attribute block for non-critical images.
