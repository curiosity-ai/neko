---
title: "Enhancing UI with Badges, Buttons, and Cards"
description: "Discover Neko's custom UI components: Badges, Buttons, and Cards. Add interactivity and style to your documentation."
author: "Neko Team"
date: "2023-11-02"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/components/800/400"
layout: post
---

# Enhancing UI with Badges, Buttons, and Cards

Sometimes standard Markdown isn't enough. Neko provides a library of **UI Components** to make your documentation more interactive and visually appealing.

## Badges

Use badges to highlight status, versions, or tags.

### Syntax

```markdown
[!badge New]
[!badge Success green]
[!badge Warning yellow]
[!badge Error red]
```

### Examples

[!badge v1.0] [!badge Beta] [!badge Stable green]

## Buttons

Create call-to-action buttons directly in your text.

### Syntax

```markdown
[!button Click Me]
[!button Primary blue]
[!button :icon-download: Download]
```

### Examples

[!button Learn More] [!button :icon-home: Go Home]

## Cards

Cards are perfect for grouping related content, features, or links.

### Syntax

```markdown
::: card
### Card Title
Card content goes here.
:::
```

### Variants

- `default`
- `link` (for navigation)
- `horizontal` (image on the left)
- `grid` (for collections)

Start building rich UIs within your documentation with Neko's component library.
