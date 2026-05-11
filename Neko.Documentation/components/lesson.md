---
title: Lesson
description: Render a curriculum-style track with progress tracking saved to localStorage.
icon: graduation-cap
---

# Lesson

The `[!lesson]` component renders a curriculum page. It auto-discovers sibling Markdown files in the folder where the component is used and displays them as ordered steps. User progress (which steps have been checked off) is persisted in the browser via `localStorage`.

See the [sample lesson](/lesson/index) for a live example.

## Usage

Place `[!lesson]` in the index file of a folder. Every other `.md` file in that folder becomes a step.

```markdown
---
title: Learn Neko
---

[!lesson
  title="Learn Neko"
  description="Hands-on track from your first build to publishing."
  badge="Neko Apprentice"
  prerequisites=".NET 10 SDK | Markdown comfort | A terminal"
  up-next-title="Components Reference"
  up-next-summary="Once you finish the track, browse the components."
  up-next-link="/components/components"
]
```

## Step files

Each sibling Markdown file (apart from the index itself) becomes a step. Set the following frontmatter on each step:

```yaml
---
title: Install Neko
order: 1
kind: Reading      # Reading | Interactive | Project | Video
duration: 5 min
---
```

Steps are sorted by `order`, then by title.

## Attributes

| Attribute | Description |
| :--- | :--- |
| `title` | Heading shown at the top of the lesson card. |
| `description` | Short intro under the title. |
| `badge` | Name of the badge awarded for completing the track. |
| `prerequisites` | Pipe-separated (`|`) list of prerequisites. |
| `up-next-title` | Heading of the "Up next" sidebar block. |
| `up-next-summary` | One-line summary for the "Up next" card. |
| `up-next-link` | Destination for the "Preview track →" link. |

## Progress

Each lesson is identified by its file path. Progress is stored under the key `neko-lesson:<path>` in `localStorage`. The **Continue** button jumps to the first unfinished step; **Start over** clears the progress entry.
