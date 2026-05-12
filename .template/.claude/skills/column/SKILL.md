---
name: column
description: Lay out content in equal-width columns using `|||`. Use for side-by-side comparisons, demo + source, before/after, two-pane layouts.
---

# Column

Renders content in equal-width columns. Each column gets an optional title and
may contain any Markdown — including other components.

## Syntax

```markdown
||| Title of column 1
Content of column 1.
||| Title of column 2
Content of column 2.
|||
```

Use additional `||| Title` separators for three or more columns. The final
`|||` closes the block.

## Examples

Demo + source:

````markdown
||| Demo
[!button variant="primary" text="Click me"](#)
||| Source
```markdown
[!button variant="primary" text="Click me"](#)
```
|||
````

Three columns:

```markdown
||| Free
- Markdown
- Components
- Themes
||| Pro
- Search
- Analytics
- Multi-repo
||| Enterprise
- SSO
- Audit log
- SLAs
|||
```

## Tips

- Columns collapse to a single column on narrow screens automatically.
- Don't put very wide content (long code blocks, large images) inside columns;
  use a [`tab`](../tab/SKILL.md) or full-width layout instead.
- Headings (`##`) work inside columns but the column title is often enough.
