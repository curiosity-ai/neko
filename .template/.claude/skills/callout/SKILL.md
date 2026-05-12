---
name: callout
description: Alias for Alert/Admonition — block that highlights important text with a coloured variant. Use the Alert skill; this entry exists so "callout" requests resolve.
---

# Callout

"Callout" is another name for what Neko calls an **Alert**. Use the same
syntax. See [`alert`](../alert/SKILL.md) for the full reference.

## Quick reminder

```markdown
!!! variant Title
Content.
!!!
```

Variants: `primary`, `secondary`, `success`, `tip`, `danger`, `warning`,
`info`, `question`, `light`, `dark`, `ghost`, `contrast`.

## GitHub-style

```markdown
> [!NOTE]
> A standard note.

> [!WARNING]
> A warning.

> [!TIP]
> A tip.
```

These render as Neko callouts at build time.
