---
title: Logo Cloud
description: A grid of partner or client logos.
icon: cloud
order: 8
---

# Logo Cloud

The `logo-cloud` component displays a grid of logos, typically used to show trusted partners or clients.

## Usage

```markdown
[!logo-cloud
    heading="Trusted by the world’s most innovative teams"
    "https://tailwindui.com/img/logos/158x48/transistor-logo-gray-900.svg" "Transistor"
    "https://tailwindui.com/img/logos/158x48/reform-logo-gray-900.svg" "Reform"
    "https://tailwindui.com/img/logos/158x48/tuple-logo-gray-900.svg" "Tuple"
    "https://tailwindui.com/img/logos/158x48/savvycal-logo-gray-900.svg" "SavvyCal"
    "https://tailwindui.com/img/logos/158x48/statamic-logo-gray-900.svg" "Statamic"
]
```

**Preview:**

[!logo-cloud
    heading="Trusted by the world’s most innovative teams"
    "https://tailwindui.com/img/logos/158x48/transistor-logo-gray-900.svg" "Transistor"
    "https://tailwindui.com/img/logos/158x48/reform-logo-gray-900.svg" "Reform"
    "https://tailwindui.com/img/logos/158x48/tuple-logo-gray-900.svg" "Tuple"
    "https://tailwindui.com/img/logos/158x48/savvycal-logo-gray-900.svg" "SavvyCal"
    "https://tailwindui.com/img/logos/158x48/statamic-logo-gray-900.svg" "Statamic"
]

## Attributes

| Attribute | Description |
| :--- | :--- |
| `heading` | Optional heading text. |
| Positional Arguments | Groups of 2: Logo URL, Alt text. |
