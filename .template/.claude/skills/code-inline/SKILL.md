---
name: code-inline
description: Render a short piece of code, a variable name, a file path, or any technical term inline within a sentence using backtick syntax. Renders as a styled pill with background and monospace font.
---

# Inline code

Wraps a short snippet in a styled monospace pill — background tint, rounded
border, consistent font — so it stands out from surrounding prose without
breaking the reading flow.

## Syntax

Surround the text with single backticks:

```markdown
Use `scope.Graph` for graph access and `scope.CurrentUser` for the user.
Call `scope.ChatAI.GetTextFromNode(uid, limit)` to fetch indexed text.
```

## When to use

- Variable names, function calls, method signatures.
- File paths, CLI flags, config keys.
- Any short technical term that should not be prose-formatted.

## When NOT to use

- Multi-line code → use a fenced [code-block](../code-block/SKILL.md).
- Code pulled from a real source file → use [code-snippet](../code-snippet/SKILL.md).
- A literal backtick character → escape it with a backslash: `` \` ``.
