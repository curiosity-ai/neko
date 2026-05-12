---
name: container
description: Wrap a chunk of Markdown in a `<div>` with custom CSS classes and HTML attributes. Use for layout helpers (center, max-width, background tint) and one-off styling without leaving Markdown.
---

# Container

Generic block wrapper that emits a `<div>` with custom classes and attributes.

## Syntax

```markdown
:::
Plain container — just a wrapping div.
:::

:::text-center
Centered content.
:::

:::callout-soft { #my-anchor .extra }
Extra classes and an id.
:::
```

The first token after the opening `:::` becomes the primary CSS class. An
optional `{ … }` block can add an `#id` and additional `.classes`.

## Attribute block

Inside `{ … }`:

- `#anchor-id` — id attribute.
- `.foo .bar` — extra classes.
- `key="value"` — arbitrary HTML attributes (use sparingly).

## Examples

```markdown
:::text-center
This text is centered.
:::

:::bg-zinc-100 dark:bg-zinc-900 rounded-lg p-4
A soft panel with Tailwind utilities.
:::

:::grid { #features .gap-4 .grid-cols-3 }
Three columns of children.
:::
```

## When to use

- Quick layout helpers (centering, padding, max-width).
- Wrapping a Tailwind utility chain around several blocks.
- Naming a region with an `#id` for cross-page anchors.

Avoid containers for things that have a dedicated component — use
[`card`](../cards/SKILL.md), [`panel`](../panel/SKILL.md),
[`column`](../column/SKILL.md), [`tab`](../tab/SKILL.md), or
[`alert`](../alert/SKILL.md) instead.
