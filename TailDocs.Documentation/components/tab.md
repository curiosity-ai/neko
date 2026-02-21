---
title: Tab Component
---

# Tab

Tabs are used to organize content into separate views, allowing users to switch between them. This is useful for code examples in different languages or alternative instructions.

## Syntax

Tabs are defined using the `+++` syntax. Each `+++ Title` starts a new tab, and `+++` (no title) closes the group.

```markdown
+++ Tab 1 Title
Content for Tab 1 goes here.
+++ Tab 2 Title
Content for Tab 2 goes here.
+++
```

### Example

+++ C#
```csharp
Console.WriteLine("Hello from C#");
```
+++ JavaScript
```javascript
console.log("Hello from JavaScript");
```
+++ Python
```python
print("Hello from Python")
```
+++

## Features

- **Styling**: Tabs inherit the active theme colors.
- **Nested Content**: Tabs can contain any Markdown content, including code blocks, lists, and images.
- **Responsive**: Tabs handle overflow elegantly on smaller screens.

### Nested Tabs

You can nest tab groups inside other components (like panels), but nesting tabs inside tabs is not directly supported by the current parser (it might flatten them). Use with caution or verify the output.
