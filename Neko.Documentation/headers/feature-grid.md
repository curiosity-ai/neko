---
title: Feature Grid
description: A grid layout to showcase features.
icon: layout-grid
order: 4
---

# Feature Grid

The `feature-grid` component displays a grid of features with icons, titles, and descriptions.

## Usage

```markdown
[!feature-grid
    title="Everything you need to deploy your app"
    subtitle="Quis tellus eget adipiscing convallis sit sit eget aliquet quis. Suspendisse eget egestas a elementum pulvinar et feugiat blandit at. In mi viverra elit nunc."
    cloud "Push to deploy" "Morbi viverra dui mi arcu sed. Tellus semper adipiscing suspendisse semper morbi."
    lock "SSL certificates" "Sit quis amet rutrum tellus ullamcorper ultricies libero dolor eget. Sem sodales gravida quam turpis."
    refresh "Simple queues" "Quisque est vel vulputate cursus. Risus proin diam nunc commodo. Lobortis auctor congue commodo diam neque."
    shield "Advanced security" "Arcu egestas dolor vel iaculis in ipsum mauris. Tincidunt mattis aliquet hac quis. Id hac maecenas ac donec pharetra eget."
]
```

**Preview:**

[!feature-grid
    title="Everything you need to deploy your app"
    subtitle="Quis tellus eget adipiscing convallis sit sit eget aliquet quis. Suspendisse eget egestas a elementum pulvinar et feugiat blandit at. In mi viverra elit nunc."
    cloud "Push to deploy" "Morbi viverra dui mi arcu sed. Tellus semper adipiscing suspendisse semper morbi."
    lock "SSL certificates" "Sit quis amet rutrum tellus ullamcorper ultricies libero dolor eget. Sem sodales gravida quam turpis."
    refresh "Simple queues" "Quisque est vel vulputate cursus. Risus proin diam nunc commodo. Lobortis auctor congue commodo diam neque."
    shield "Advanced security" "Arcu egestas dolor vel iaculis in ipsum mauris. Tincidunt mattis aliquet hac quis. Id hac maecenas ac donec pharetra eget."
]

## Attributes

| Attribute | Description |
| :--- | :--- |
| `title` | The main heading. |
| `subtitle` | The subtitle text. |
| Positional Arguments | Groups of 3: Icon (UIcon name or emoji), Title, Description. |
