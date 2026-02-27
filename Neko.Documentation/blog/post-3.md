---
title: "Responsive Layouts with Columns"
description: "Use the Column component to create multi-column layouts, side-by-side content comparisons, and responsive designs."
author: "Neko Team"
date: "2023-10-21"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/columns/800/400"
layout: post
---

# Responsive Layouts with Columns

Markdown is inherently linear, but sometimes you need to display content side-by-side. The **Column** component (`|||`) allows you to create responsive multi-column layouts directly in your documentation.

## Syntax

Columns are defined using `|||` separators. You can have as many columns as you need.

```markdown
|||
### Left Column
This content will appear on the left.
|||
### Right Column
This content will appear on the right.
|||
```

## Custom Widths

You can control the width of each column using a `flex` ratio or specific width classes.

```markdown
||| flex=1
This column takes up 1/3 of the space.
||| flex=2
This column takes up 2/3 of the space.
|||
```

## Responsive Behavior

By default, columns stack vertically on mobile devices (`md:flex-row`). This ensures your documentation remains readable on smaller screens.

### Example: Feature List

```markdown
|||
:icon-zap: **Fast**
Blazing fast rendering with Markdig.
|||
:icon-mobile: **Responsive**
Looks great on all devices.
|||
:icon-box: **Components**
Rich set of UI components.
|||
```

Columns are perfect for feature grids, comparisons, and breaking up long blocks of text.
