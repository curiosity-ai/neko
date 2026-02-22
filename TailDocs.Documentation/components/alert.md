---
title: Alert Component
icon: exclamation
---

# Alert

Alerts (also known as Callouts or Admonitions) are used to highlight important information, warnings, or tips within your documentation.

## Syntax

The syntax for an alert is a block starting with `!!!` followed by the variant name and an optional title. The content of the alert follows on the next lines. The block is closed by another `!!!` (optional if it's the end of the file, but recommended for nested structures or clarity, although TailDocs usually auto-closes on blank lines or next block).

Actually, TailDocs syntax is:

```markdown
!!! variant Title
Content goes here.
!!!
```

If no title is provided, the variant name is used as the title (capitalized), or just "Info" for the default.

## Variants

TailDocs supports a wide range of alert variants to suit different contexts.

### Primary (Default)

!!! primary Primary Alert
This is a primary alert, often used for general information.
!!!

### Info

!!! info Information
This is an info alert.
!!!

### Success

!!! success Success
This is a success alert. Great for positive feedback or successful steps.
!!!

### Warning

!!! warning Warning
This is a warning alert. Use it for potential issues or important notices.
!!!

### Danger

!!! danger Danger
This is a danger alert. Use it for critical errors or destructive actions.
!!!

### Tip

!!! tip Pro Tip
This is a tip alert. Useful for helpful hints.
!!!

### Question

!!! question Question
This is a question alert.
!!!

### Other Variants

There are also stylistic variants:

!!! secondary Secondary
Secondary alert.
!!!

!!! light Light
Light alert.
!!!

!!! dark Dark
Dark alert.
!!!

!!! ghost Ghost
Ghost alert (minimal styling).
!!!

!!! contrast Contrast
Contrast alert.
!!!

## Complex Content

Alerts can contain other Markdown elements, such as lists, code blocks, and even other components.

!!! info Complex Content
Here is a list:
- Item 1
- Item 2

And a code block:
```csharp
Console.WriteLine("Hello within Alert!");
```
!!!
