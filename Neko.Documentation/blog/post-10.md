---
title: "Organizing Complex Info with Panels"
description: "Use Panels to create collapsible content sections, FAQs, and more."
author: "Neko Team"
date: "2023-11-11"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/panels/800/400"
layout: post
---

# Organizing Complex Info with Panels

Documentation often needs to provide details without overwhelming the reader. **Panels** (collapsible sections or accordions) are the perfect tool for FAQs, troubleshooting guides, and optional advanced steps.

## Syntax

Panels use the `===` delimiter.

```markdown
=== "Panel Title"
Content inside the panel.
===
```

### Collapsible by Default

Use `==` to make the panel closed (collapsed) by default, or `===` for open.

```markdown
== "Click to expand"
Hidden details here.
==
```

## Grouping Panels

You can group panels to create an accordion effect (only one open at a time).

```markdown
=== "Question 1" group="faq"
Answer 1.
===
=== "Question 2" group="faq"
Answer 2.
===
```

Organize your FAQs and troubleshooting guides effectively with Neko Panels.
