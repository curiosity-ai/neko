---
title: Hero
description: Display a large hero section at the top of your page.
icon: star
order: 1
---

# Hero

The `hero` component is used to display a prominent section at the top of your page, typically used for landing pages or major announcements. It is designed to stand out and break the standard prose layout.

The hero ships with a curiosity-inspired layout: a soft eyebrow label with a status dot, a large title with an optional gradient accent word, a subtitle, and CTAs.

## Usage

Use the `[!hero ...]` syntax to insert a hero section.

### Left-aligned with accent

```markdown
[!hero
    eyebrow="Neko Docs · For developers"
    title="Build with the"
    title-accent="knowledge graph"
    subtitle="Hands-on tracks, runnable components, and recipes that map straight to the SDK."
    cta1-text="Start free"
    cta1-link="/lesson/index"
    cta2-text="Sign in"
    cta2-link="#"
]
```

**Preview:**

[!hero
    eyebrow="Neko Docs · For developers"
    title="Build with the"
    title-accent="knowledge graph"
    subtitle="Hands-on tracks, runnable components, and recipes that map straight to the SDK."
    cta1-text="Start free"
    cta1-link="/lesson/index"
    cta2-text="Sign in"
    cta2-link="#"
]

### Three CTAs

A third CTA (`cta3-text` / `cta3-link`) renders after `cta2`, in the same
outline style. Use it when a page splits into more than one next step —
e.g. two tracks a reader can pick between.

```markdown
[!hero
    eyebrow="Training paths"
    title="Curiosity Academy"
    subtitle="Guided training for the people who build projects and the people who run them."
    align="center"
    cta1-text="Start with the Introduction"
    cta1-link="/lesson/index"
    cta2-text="Developer track"
    cta2-link="#"
    cta3-text="Consultant track"
    cta3-link="#"
]
```

**Preview:**

[!hero
    eyebrow="Training paths"
    title="Curiosity Academy"
    subtitle="Guided training for the people who build projects and the people who run them."
    align="center"
    cta1-text="Start with the Introduction"
    cta1-link="/lesson/index"
    cta2-text="Developer track"
    cta2-link="#"
    cta3-text="Consultant track"
    cta3-link="#"
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
| `title-accent` | Optional word/phrase appended to the title with a gradient accent. | |
| `eyebrow` | Small uppercase label with a status dot rendered above the title. | |
| `subtitle` | The description text below the title. | |
| `badge-text` | Optional pill badge above the title (alternative to `eyebrow`). | |
| `badge-link` | Link for the badge. If omitted, badge is just text. | |
| `cta1-text` | Text for the primary button. | |
| `cta1-link` | URL for the primary button. | `#` |
| `cta2-text` | Text for the secondary button (outline style). | |
| `cta2-link` | URL for the secondary button. | `#` |
| `cta3-text` | Text for a third button, after `cta2` (same outline style). | |
| `cta3-link` | URL for the third button. | `#` |
| `image` | URL for an optional background image (overlaid with low opacity). | |
| `align` | Alignment of text content (`center` or `left`). | `left` |
