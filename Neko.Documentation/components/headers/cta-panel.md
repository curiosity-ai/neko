---
title: CTA Panel
description: A call-to-action panel with an image.
icon: bolt
---

# CTA Panel

The `cta-panel` component displays a prominent call to action with a title, description, buttons, and an image.

## Usage

```markdown
[!cta-panel
    title="Boost your productivity today."
    desc="Incididunt sint fugiat pariatur cupidatat consectetur sit cillum anim id veniam aliqua proident excepteur commodo do ea."
    cta1="Get started"
    cta2="Learn more"
    image="https://cdn2.thecatapi.com/images/20f.png"
]
```

**Preview:**

[!cta-panel
    title="Boost your productivity today."
    desc="Incididunt sint fugiat pariatur cupidatat consectetur sit cillum anim id veniam aliqua proident excepteur commodo do ea."
    cta1="Get started"
    cta2="Learn more"
    image="https://cdn2.thecatapi.com/images/20f.png"
]

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `title` | Main title text. | |
| `desc` | Description text. | |
| `cta1` | Text for the primary button. | |
| `link1` | URL for the primary button. | `#` |
| `cta2` | Text for the secondary button. | |
| `link2` | URL for the secondary button. | `#` |
| `image` | URL for the image. | |
| `align` | Alignment of the image (`right` or `left`). | `right` |
