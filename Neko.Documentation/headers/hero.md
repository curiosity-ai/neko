---
title: Hero
description: Display a large hero section at the top of your page.
icon: star
order: 1
---

# Hero

The `hero` component is used to display a prominent section at the top of your page, typically used for landing pages or major announcements. It is designed to stand out and break the standard prose layout.

## Usage

Use the `[!hero ...]` syntax to insert a hero section. The component supports various attributes to customize the title, subtitle, buttons, badge, and background.

### Simple Centered

This is the default style, perfect for main landing pages.

```markdown
[!hero
    title="Data to enrich your online business"
    subtitle="Anim aute id magna aliqua ad ad non deserunt sunt. Qui irure qui lorem cupidatat commodo. Elit sunt amet fugiat veniam occaecat fugiat aliqua."
    badge-text="Announcing our next round of funding"
    badge-link="#"
    cta1-text="Get started"
    cta1-link="#"
    cta2-text="Learn more"
    cta2-link="#"
]
```

**Preview:**

[!hero
    title="Data to enrich your online business"
    subtitle="Anim aute id magna aliqua ad ad non deserunt sunt. Qui irure qui lorem cupidatat commodo. Elit sunt amet fugiat veniam occaecat fugiat aliqua."
    badge-text="Announcing our next round of funding"
    badge-link="#"
    cta1-text="Get started"
    cta1-link="#"
    cta2-text="Learn more"
    cta2-link="#"
]

### Left Aligned with Background Image

You can align content to the left and provide a custom background image.

```markdown
[!hero
    title="Supercharge your workflow"
    subtitle="Unlock your potential with our cutting-edge tools designed for modern developers."
    align="left"
    cta1-text="Start Free Trial"
    cta1-link="#"
    image="/assets/demo-image.png"
]
```

**Preview:**

[!hero
    title="Supercharge your workflow"
    subtitle="Unlock your potential with our cutting-edge tools designed for modern developers."
    align="left"
    cta1-text="Start Free Trial"
    cta1-link="#"
    image="/assets/demo-image.png"
]

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `title` | The main heading text. | |
| `subtitle` | The description text below the title. | |
| `badge-text` | Text for the small pill badge above the title. | |
| `badge-link` | Link for the badge. If omitted, badge is just text. | |
| `cta1-text` | Text for the primary button. | |
| `cta1-link` | URL for the primary button. | `#` |
| `cta2-text` | Text for the secondary button. | |
| `cta2-link` | URL for the secondary button. | `#` |
| `image` | URL for the background image. If omitted, a default gradient is used. | |
| `align` | Alignment of text content (`center` or `left`). | `center` |
