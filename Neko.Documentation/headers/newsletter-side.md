---
title: Newsletter Side
description: A newsletter subscription component.
icon: poll-h
order: 10
---

# Newsletter Side

The `newsletter-side` component displays a newsletter subscription form with side details.

## Usage

```markdown
[!newsletter-side
    title="Get notified when we’re launching."
    desc="Reprehenderit ad tristique aliquet ut risus vel metus nisl."
    cta="Subscribe"
    placeholder="Enter your email"
    "Weekly articles" "No spam" "Unsubscribe anytime"
]
```

**Preview:**

[!newsletter-side
    title="Get notified when we’re launching."
    desc="Reprehenderit ad tristique aliquet ut risus vel metus nisl."
    cta="Subscribe"
    placeholder="Enter your email"
    "Weekly articles" "No spam" "Unsubscribe anytime"
]

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `title` | The main title. | |
| `desc` | The description. | |
| `cta` | The button text. | `Subscribe` |
| `placeholder` | The input placeholder. | `Enter your email` |
| Positional Arguments | List of benefits/features (rendered as links). | |
