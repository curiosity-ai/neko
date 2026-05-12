---
name: alert
description: Render a Neko Alert/Callout (admonition) block in Markdown — info, success, warning, danger, tip, question, primary, secondary, light, dark, ghost, contrast. Use whenever the user wants to highlight a tip, warning, note, or error inside docs.
---

# Alert

Alerts (a.k.a. callouts or admonitions) highlight important text inside a page.

## Syntax

```markdown
!!! variant Optional Title
Markdown content goes here.
Multiple lines, lists, code, even other components are allowed.
!!!
```

If no title is supplied the variant name is used (capitalised); for the default
variant the title is `Info`. The closing `!!!` is recommended for clarity even
though Neko auto-closes on a blank line.

## Variants

`primary` (default) · `secondary` · `info` · `success` · `warning` · `danger` ·
`tip` · `question` · `light` · `dark` · `ghost` · `contrast`.

## Examples

```markdown
!!! info Information
Useful background information.
!!!

!!! success It worked
The deployment finished without errors.
!!!

!!! warning Heads up
This action cannot be undone.
!!!

!!! danger Stop
Production data will be deleted.
!!!

!!! tip Pro tip
Use `neko start` for live reload while editing.
!!!
```

## GitHub-style alternative

Neko also accepts the GitHub admonition syntax inside a blockquote:

```markdown
> [!NOTE]
> A standard note.

> [!WARNING]
> A warning.
```

## Nesting

Alerts may contain other components, lists, code blocks, images, and tabs.
Keep nested content shallow — two levels deep is the practical limit before
the layout becomes noisy.
