---
title: "Advanced Code Blocks and Highlighting"
description: "Master the art of displaying code with line highlighting, file names, syntax coloring, and copy-to-clipboard functionality."
author: "Neko Team"
date: "2023-10-27"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/code/800/400"
layout: post
---

# Advanced Code Blocks and Highlighting

Code is at the heart of technical documentation. Neko provides a powerful **Code Block** experience built on top of Highlight.js and Markdig.

## Basic Syntax

Use standard Markdown triple backticks:

```csharp
public void Hello()
{
    Console.WriteLine("Hello World");
}
```

## Advanced Features

### Line Numbers

You can enable line numbers globally in `neko.yml` or per block:

```yaml
snippets:
  lineNumbers: ["csharp", "javascript"]
```

Or locally:

```markdown
```csharp #
// Line numbers enabled
```

### Highlighting Lines

Use the `!#` flag followed by line ranges to highlight specific lines.

```csharp !#2-3
public void HighlightMe()
{
    // This line is highlighted
    // And this one too
}
```

### File Names

You can add a file name or title to your code block:

```markdown
:::code source="Program.cs"
using System;
:::
```

Or using the standard syntax with title attribute (requires generic attribute extension support, or use the `:::code` component):

```csharp title="Program.cs"
// Some code
```

## Supported Languages

Neko supports over 180 languages via Highlight.js, including:

- C#, F#, VB.NET
- JavaScript, TypeScript
- Python, Ruby, Go, Rust
- HTML, CSS, XML, JSON, YAML
- Bash, PowerShell, Dockerfile

Ensure your code samples look their best with Neko's syntax highlighting.
