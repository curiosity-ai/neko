---
title: Logo Cloud
description: A grid of partner or client logos.
icon: grid
---

# Logo Cloud

The `logo-cloud` component displays a grid of logos, typically used to show trusted partners or clients.

## Usage

```markdown
[!logo-cloud
    heading="Trusted by the world’s most innovative teams"
    "https://laracasts.com/images/series/2018/transistor-logo.svg" "Transistor"
    "https://cdn.prod.website-files.com/660f132d9a66031a172a6ed2/68937c22c4b02702748d5a15_Reform%20by%20Funnel%20Envy%20Logo.svg" "Reform"
    "https://laracasts.com/images/series/2018/tuple-logo.svg" "Tuple"
    "https://cdn.prod.website-files.com/660f38f06a23cf2d2fb65170/6932d5548e9cef3b0f3e86a7_660f38ff6f83171ad7ec13f1_savvycal-logo.svg" "SavvyCal"
    "https://statamic.com/assets/branding/squircle/statamic-logo-black.svg" "Statamic"
]
```

**Preview:**

[!logo-cloud
    heading="Trusted by the world’s most innovative teams"
    "https://laracasts.com/images/series/2018/transistor-logo.svg" "Transistor"
    "https://cdn.prod.website-files.com/660f132d9a66031a172a6ed2/68937c22c4b02702748d5a15_Reform%20by%20Funnel%20Envy%20Logo.svg" "Reform"
    "https://laracasts.com/images/series/2018/tuple-logo.svg" "Tuple"
    "https://cdn.prod.website-files.com/660f38f06a23cf2d2fb65170/6932d5548e9cef3b0f3e86a7_660f38ff6f83171ad7ec13f1_savvycal-logo.svg" "SavvyCal"
    "https://statamic.com/assets/branding/squircle/statamic-logo-black.svg" "Statamic"
]

## Attributes

| Attribute | Description |
| :--- | :--- |
| `heading` | Optional heading text. |
| Positional Arguments | Groups of 2: Logo URL, Alt text. |
