---
title: Pricing Tiers
description: A component to display pricing plans.
icon: coins
order: 7
---

# Pricing Tiers

The `pricing-tiers` component displays pricing plans with features.

## Usage

```markdown
[!pricing-tiers
    title="Pricing plans for teams of all sizes"
    subtitle="Choose the best plan for your needs."
    highlight="2"
    "Freelancer" "$24" "The essentials to provide your best work for clients." "5 products,Up to 1,000 subscribers,Basic analytics,48-hour support response time"
    "Startup" "$32" "A plan that scales with your rapidly growing business." "25 products,Up to 10,000 subscribers,Advanced analytics,24-hour support response time,Marketing automations"
]
```

**Preview:**

[!pricing-tiers
    title="Pricing plans for teams of all sizes"
    subtitle="Choose the best plan for your needs."
    highlight="2"
    "Freelancer" "$24" "The essentials to provide your best work for clients." "5 products,Up to 1,000 subscribers,Basic analytics,48-hour support response time"
    "Startup" "$32" "A plan that scales with your rapidly growing business." "25 products,Up to 10,000 subscribers,Advanced analytics,24-hour support response time,Marketing automations"
]

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `title` | The main title. | |
| `subtitle` | The subtitle text. | |
| `highlight` | The index (1-based) of the plan to highlight as "Most popular". | |
| Positional Arguments | Groups of 4: Plan Name, Price, Description, Features (comma separated string). | |
