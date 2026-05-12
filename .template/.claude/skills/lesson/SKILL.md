---
name: lesson
description: Render a lesson/curriculum header that auto-discovers sibling Markdown files as steps and tracks completion in localStorage. Use to build interactive courses, tutorials, and learning tracks.
---

# Lesson

A `lesson` block is a curriculum-style header. Place it at the top of an index
page in a folder; Neko picks up the sibling `.md` files as steps and renders a
progress tracker. Completion is stored per-visitor in `localStorage`.

## Syntax

```markdown
[!lesson
  title="Course Title"
  description="One-paragraph course description."
  badge="Course completion badge"
  prerequisites="Item 1 | Item 2 | Item 3"
  up-next-title="Next steps"
  up-next-summary="Where to go after finishing this track."
  up-next-link="/next/course"
]
```

## Attributes

| Attribute        | Notes                                                               |
| ---              | ---                                                                 |
| `title`          | Course / track title.                                               |
| `description`    | Intro sentence shown under the title.                               |
| `badge`          | Label of the badge awarded on completion.                           |
| `prerequisites`  | `|`-delimited list of pre-requirements shown as chips.              |
| `up-next-title`  | Heading of the "what's next" card shown on completion.              |
| `up-next-summary`| Body of the "what's next" card.                                     |
| `up-next-link`   | Destination of the "what's next" card.                              |

## Folder layout

```
guides/
├── learn-neko/
│   ├── index.md              ← contains [!lesson …]
│   ├── 01-install.md
│   ├── 02-first-page.md
│   ├── 03-add-components.md
│   └── 04-deploy.md
```

The order of steps follows the same rules as the sidebar
(`order:` in each step's frontmatter, then alphabetical).

## Example

```markdown
[!lesson
  title="Learn Neko"
  description="Hands-on track from install to deployment in under 30 minutes."
  badge="Neko Apprentice"
  prerequisites=".NET 10 | Familiarity with Markdown | A terminal"
  up-next-title="Advanced Neko"
  up-next-summary="Now that you know the basics, try the advanced track."
  up-next-link="/guides/learn-neko-advanced"
]
```

## Tips

- Put `[!lesson …]` at the **very top** of the folder's `index.md`.
- Step pages should have short, action-oriented titles.
- Completion state is per-browser. Don't rely on it as authoritative.
