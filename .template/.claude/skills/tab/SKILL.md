---
name: tab
description: Render a tabbed panel where only one tab is visible at a time. Use for "same operation, multiple languages/platforms/tools" content — JS/Python/C#, npm/yarn/pnpm, macOS/Windows/Linux, etc.
---

# Tab

Tabbed content. Each `+++` opens a new tab; the closing `+++` (on its own line)
ends the block. Tabs are anchorable — Neko gives each one a stable id.

## Syntax

````markdown
+++ Tab 1
Content of tab 1.
+++ Tab 2
Content of tab 2.
+++
````

## Example — multiple languages

````markdown
+++ JavaScript
```js
console.log("Hello");
```
+++ Python
```py
print("Hello")
```
+++ C#
```csharp
Console.WriteLine("Hello");
```
+++
````

## Linking to a tab

Each tab gets an id derived from its title. Link to it with `#tab-<slug>`:

```markdown
[Open the Python tab](#tab-python)
```

## Tips

- Use tabs only when the content of each tab is *parallel* (same task, different
  way). Don't put unrelated content in tabs.
- 2-4 tabs is the sweet spot; more becomes a horizontal scroll on mobile.
- For collapsible alternative content, prefer
  [`panel`](../panel/SKILL.md).
