---
title: Steps
label: Steps
icon: list-check
order: 14
---

# Steps

The Steps component allows you to present a sequence of actions or events clearly.

>>> **Step 1: Install Neko**

Run the installation command in your terminal.

:::code
```bash
dotnet tool install -g Neko
```
:::

>>> **Step 2: Build the project**

Run Neko to build your static files.

:::code
```bash
neko build
```
:::

>>> **Step 3: Profit**

Your website is now generated and ready to be deployed.

>>>

## Syntax

Steps are created using `>>>` at the start of a line, optionally followed by a step title. The content block continues until the next `>>>` or the end of the markdown block. Close the final step by adding a final `>>>` on an empty line.

:::code
```markdown
>>> Step 1
This is the first step.
>>> Step 2
This is the second step.
>>>
```
:::
