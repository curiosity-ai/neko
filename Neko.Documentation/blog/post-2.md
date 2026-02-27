---
title: "Organizing Content with Tabs"
description: "Tabs are a powerful way to present multiple variations of content, such as code examples for different languages."
author: "Neko Team"
date: "2023-10-18"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/tabs/800/400"
layout: post
---

# Organizing Content with Tabs

When documenting libraries or APIs that support multiple languages or operating systems, you often need to show different instructions for the same step. Neko's **Tabs** component makes this seamless.

## Basic Syntax

Tabs are defined using the `+++` syntax. Each tab block is separated by `+++` followed by the tab title.

```markdown
+++ "Windows"
Run this command on Windows:
`choco install neko`
+++ "macOS"
Run this command on macOS:
`brew install neko`
+++ "Linux"
Run this command on Linux:
`apt-get install neko`
+++
```

## Nested Content

You can include any Markdown content inside a tab, including code blocks, lists, and images.

```markdown
+++ "C#"
:::code
using System;
Console.WriteLine("Hello World!");
:::
+++ "Python"
:::code
print("Hello World!")
:::
+++
```

## Grouping Tabs

Use the `group` attribute to synchronize tab selection across multiple tab components on the same page. If a user selects "macOS" in one tab group, all other groups with the same identifier will switch to "macOS".

```markdown
+++ "Windows" group="os"
// Windows code
+++ "macOS" group="os"
// macOS code
+++
```

Tabs help keep your documentation clean and reduce visual clutter by hiding irrelevant information until the user needs it.
