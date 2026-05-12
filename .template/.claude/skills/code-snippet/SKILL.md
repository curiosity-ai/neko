---
name: code-snippet
description: Include source code from an external file (whole file, line range, or named region) with auto-detected highlighting. Use to embed real code from the repo without copy-pasting and risking drift.
---

# Code snippet

Includes the contents of a file at build time, with syntax highlighting based
on the file extension. Useful for keeping documentation in sync with real code.

## Syntax

```markdown
:::code source="../static/sample.js" :::
:::code source="../src/Program.cs" language="csharp" :::
:::code source="../src/Util.cs" range="1-10" :::
:::code source="../src/Util.cs" region="MyRegion" :::
:::code source="../src/Util.cs" title="Util.cs" :::
```

## Attributes

| Attribute  | Notes                                                                          |
| ---        | ---                                                                            |
| `source`   | **Required.** Path to the file (relative to the current `.md`).                |
| `language` | Override auto-detection (e.g. `language="csharp"`).                            |
| `range`    | Line range, inclusive, 1-indexed. `range="1-20"`.                              |
| `region`   | C# `#region NAME` / `#endregion` block to extract.                             |
| `title`    | Optional title shown in the snippet header.                                    |

## Examples

````markdown
:::code source="../samples/basic-page.md" title="Minimum page" :::

:::code source="../src/Calculator.cs" region="Add" language="csharp" :::

:::code source="../scripts/build.sh" range="1-15" language="bash" :::
````

## When to use vs. inline code blocks

- Use `code-snippet` when the source lives elsewhere and you want it to stay
  fresh as the file changes.
- Use a plain fenced [`code-block`](../code-block/SKILL.md) for short illustrative
  examples that have no source-of-truth file.
