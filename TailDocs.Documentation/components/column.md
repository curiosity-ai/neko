---
title: Column Component
---

# Column

Create multi-column layouts using `::: columns` and `::: column` directives. This is useful for creating side-by-side content or grid-like structures.

## Syntax

Wrap content in a `::: columns` block, and then divide it into `::: column` blocks.

```markdown
::: columns

::: column
### Left Column
This content is in the left column.
:::

::: column
### Right Column
This content is in the right column.
:::

:::
```

## Features

- **Responsive**: Columns automatically stack vertically on mobile devices.
- **Flexible**: You can have any number of columns (though 2 or 3 is recommended for readability).
- **Styling**: Columns automatically adjust width based on content, but generally try to share equal space if possible (flex-1 behavior).

### Example

::: columns

::: column
### Features
- **Easy to Use**
- **Fast**
- **Customizable**
:::

::: column
### Benefits
- **Better Docs**
- **Happy Users**
- **Less Maintenance**
:::

:::
