---
name: table
description: Render a Markdown table with alignment, compact mode, and no-wrap option. Use for any tabular comparison, reference matrix, option list.
---

# Table

Standard pipe tables with Neko-specific modifiers.

## Basic

```markdown
| Name | Value |
| ---  | ---   |
| A    | 1     |
| B    | 2     |
```

## Alignment

Use colons in the separator row:

```markdown
| Left | Center | Right |
| :--- | :---:  | ---:  |
| a    | b      | c     |
```

## Compact

Add `{.compact}` directly above the table to reduce padding:

```markdown
{.compact}
| Feature | Status |
| ---     | ---    |
| API     | Active |
| Docs    | Done   |
```

## No wrap

Add `{.whitespace-nowrap}` to force each cell onto one line:

```markdown
{.whitespace-nowrap}
| Identifier | Description |
| ---        | ---         |
| `id`       | The unique identifier of the record |
```

## Tips

- Use tables for **flat** data. For nested structures use lists or
  [`panel`](../panel/SKILL.md) accordions.
- For long tables, prefer `{.compact}`.
- Inline code (`` `…` ``), badges, and icons all render inside cells.
- Keep header cells short — long headers force wide columns.
