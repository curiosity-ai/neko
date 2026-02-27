---
title: "Reuse Content with Includes"
description: "Learn how to modularize your documentation and maintain consistency using the powerful Include directive."
author: "Neko Team"
date: "2023-11-05"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/includes/800/400"
layout: post
---

# Reuse Content with Includes

One of the biggest challenges in maintaining large documentation sites is duplication. If you have the same disclaimer, licensing info, or common steps across multiple pages, you don't want to copy-paste it everywhere.

Neko solves this with the `{{ include }}` directive.

## Syntax

```markdown
{{\u200B include "_includes/disclaimer.md" }}
```

## How it works

The include directive is a pre-processor. It reads the file content from the path (relative to the current file or project root) and injects it verbatim before Markdown parsing begins.

This means you can include **Markdown**, **HTML**, **Code**, or even **Components**.

### Example: Shared Footer

Create `_includes/footer.md`:

```markdown
---
> [!NOTE]
> This documentation is subject to change.
---
```

Then include it in every page:

```markdown
# Page 1
...content...
{{\u200B include "_includes/footer.md" }}
```

## Benefits

- **Consistency**: Update in one place, reflect everywhere.
- **Maintainability**: Smaller, focused files are easier to manage.
- **Organization**: Structure your docs logically.

Start reusing content and save yourself from copy-paste errors.
