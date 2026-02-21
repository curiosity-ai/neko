---
title: Code Blocks
---

# Code Blocks

TailDocs provides enhanced code blocks with syntax highlighting, line numbers, copy-to-clipboard functionality, and title bars.

## Syntax

Use standard Markdown fenced code blocks (triple backticks) and specify the language identifier.

```csharp
public class HelloWorld
{
    public static void Main()
    {
        Console.WriteLine("Hello, World!");
    }
}
```

## Features

### Title Bar

You can add a title to your code block by appending `title="Filename.ext"` after the language identifier.

```csharp title="Program.cs"
Console.WriteLine("Hello from Program.cs");
```

### Line Highlighting

Highlight specific lines using the `#` syntax followed by line numbers or ranges.

```csharp #2,4-5
public void Highlight()
{
    var x = 1; // Not highlighted
    var y = 2; // Highlighted
    var z = 3; // Not highlighted
    var a = 4; // Highlighted
    var b = 5; // Highlighted
}
```

### Copy Button

All code blocks automatically include a copy button in the top-right corner. When a title is present, the button is integrated into the title bar.

## Code Snippets (Experimental)

You can also embed code from external files using the `:::code` directive (currently a placeholder feature in development).

```markdown
:::code source="path/to/file.cs" title="Snippet Title"
```
