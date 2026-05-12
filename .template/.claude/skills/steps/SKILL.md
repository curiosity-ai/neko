---
name: steps
description: Render a numbered sequence of steps using `>>>`. Use for tutorials, install procedures, getting-started flows — anything that is fundamentally a numbered checklist.
---

# Steps

Renders an automatically numbered, styled list of steps. Each step has a title
and a body of arbitrary Markdown.

## Syntax

```markdown
>>> Step 1 title
Body of step 1. Any Markdown.
>>> Step 2 title
Body of step 2.
>>> Step 3 title
Body of step 3.
>>>
```

The trailing `>>>` on a line of its own closes the block. Title is required;
the body may include code blocks, images, and other components.

## Example

````markdown
>>> Install Neko
```bash
dotnet tool install -g Neko
```
>>> Create a `neko.yml`
Copy the starter from `.template/neko.yml`.
>>> Run `neko start`
Open the URL printed in your terminal.
>>>
````

## Tips

- Use [`steps`](./SKILL.md) for sequential procedures.
- Use a task list (`- [ ] …`) for **non-sequential** checklists.
- Limit steps to ~7 items — split into multiple blocks (or pages) for longer
  procedures.
- Step titles should be short imperative phrases ("Install Neko", not
  "Installing").
