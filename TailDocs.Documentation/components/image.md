---
title: Image Component
---

# Image

Standard Markdown images are fully supported, with additional features for resizing and styling.

## Syntax

Use the standard `![alt](src)` syntax.

```markdown
![Alt Text](path/to/image.png)
```

## Resizing

You can specify the width and height of an image using the attributes syntax `{width=... height=...}` immediately after the image link.

```markdown
![Small Image](image.png){width=200}
![Stretched Image](image.png){width=500 height=300}
```

## Styling

Images are automatically styled to be responsive (max-width: 100%) and centered within their container if they are block-level (stand-alone).

### Example

![Placeholder Image](https://via.placeholder.com/600x400)
*Example caption below the image (italicized text).*
