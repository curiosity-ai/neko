---
title: Panel Component
---

# Panel

Panels are used to group related content together visually. They create a card-like container with a border and background.

## Syntax

Use the `::: panel` directive to wrap content inside a panel.

```markdown
::: panel
### Panel Title
This is the content of the panel. You can put anything here.
:::
```

## Features

- **Styling**: Panels automatically inherit light/dark mode styling.
- **Versatility**: Panels can contain images, code blocks, or other components.

### Example

::: panel
### Example Panel
This panel contains a list and a code block.

- Item 1
- Item 2

```csharp
Console.WriteLine("Inside Panel");
```
:::

::: columns

::: column
::: panel
### Left Panel
Panel in a column.
:::
:::

::: column
::: panel
### Right Panel
Panel in another column.
:::
:::

:::
