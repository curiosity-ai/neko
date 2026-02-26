---
title: "Getting Started with Neko Alerts"
description: "Learn how to draw attention to critical information using Neko's powerful Alert system and GitHub-style callouts."
author: "Neko Team"
date: "2023-10-15"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/alerts/800/400"
layout: post
---

# Getting Started with Neko Alerts

When writing documentation, sometimes you need to make sure your readers don't miss important information. Neko provides a robust **Alert** system to highlight content effectively.

## Standard Alerts

Neko supports standard alerts using the `!!!` syntax. You can specify the type of alert to change its appearance.

### Syntax

```markdown
!!! note
This is a standard note alert.
!!!

!!! warning
This is a warning! Be careful.
!!!

!!! success "Great Job!"
You successfully configured Neko.
!!!
```

### Supported Types

- `note` (Blue)
- `tip` (Green)
- `warning` (Yellow)
- `danger` (Red)
- `success` (Green)

## GitHub-Style Alerts

If you are coming from GitHub, you might prefer the blockquote syntax for alerts. Neko supports these out of the box!

```markdown
> [!NOTE]
> This is a note using GitHub syntax.

> [!TIP]
> Here's a helpful tip for you.

> [!IMPORTANT]
> Do not skip this step!

> [!WARNING]
> Proceed with caution.

> [!CAUTION]
> This action cannot be undone.
```

Using alerts consistently helps guide your users through your documentation and prevents them from missing critical steps. Happy documenting!
