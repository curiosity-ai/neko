---
title: Code Component
---

# Code Blocks

Code blocks support syntax highlighting and other features.

## Syntax

Use fenced code blocks with language hint.

### Examples

#### C#
```csharp
public class HelloWorld
{
    public static void Main()
    {
        Console.WriteLine("Hello World");
    }
}
```

#### JSON
```json
{
    "key": "value",
    "number": 123
}
```

### Line Highlighting

You can highlight specific lines using `data-highlight="#1-3"` (or similar syntax if configured).

```csharp
// This line is highlighted
var x = 10;
// This line is normal
var y = 20;
```
*(Need to verify if `data-highlight` is directly supported via attribute or if markdig extension handles `#range` syntax)*.

Assuming `#range` syntax:

```csharp #1,3
// Highlighted
// Normal
// Highlighted
```

### File Title

```csharp title="Program.cs"
using System;
```
