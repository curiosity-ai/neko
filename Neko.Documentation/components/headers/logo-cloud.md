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
    "https://cdn.worldvectorlogo.com/logos/nasa-6.svg" "NASA"
    "https://cdn.worldvectorlogo.com/logos/spacex.svg" "SpaceX"
    "https://cdn.worldvectorlogo.com/logos/blue-origin.svg" "Blue Origin"
    "https://cdn.worldvectorlogo.com/logos/lockheed-martin.svg" "Lockheed Martin"
    "https://cdn.worldvectorlogo.com/logos/airbus.svg" "Airbus"
]
```

**Preview:**

[!logo-cloud
    heading="Trusted by the world’s most innovative teams"
    "https://cdn.worldvectorlogo.com/logos/nasa-6.svg" "NASA"
    "https://cdn.worldvectorlogo.com/logos/spacex.svg" "SpaceX"
    "https://cdn.worldvectorlogo.com/logos/blue-origin.svg" "Blue Origin"
    "https://cdn.worldvectorlogo.com/logos/lockheed-martin.svg" "Lockheed Martin"
    "https://cdn.worldvectorlogo.com/logos/airbus.svg" "Airbus"
]

## Attributes

| Attribute | Description |
| :--- | :--- |
| `heading` | Optional heading text. |
| Positional Arguments | Groups of 2: Logo URL, Alt text. |
