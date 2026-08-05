---
name: hero
description: Render a Neko Hero block — a large landing-page header with eyebrow/badge, title with optional gradient accent, subtitle, and up to three CTAs. Use for the top of a homepage or a track/section landing page.
---

# Hero

`hero` is a self-closing inline shortcode: `[!hero attr="..." attr="..."]`. It
takes no Markdown children — everything is passed as attributes.

## Syntax

```markdown
[!hero
    eyebrow="Training paths"
    title="Curiosity Academy"
    subtitle="Guided training for the people who build projects and the people who run them."
    align="center"
    cta1-text="Start with the Introduction"
    cta1-link="/academy/introduction/"
    cta2-text="Developer track"
    cta2-link="/academy/developer/"
    cta3-text="Consultant track"
    cta3-link="/academy/#consultant-track"
]
```

## Attributes

| Attribute       | Notes                                                                    |
| ---             | ---                                                                      |
| `title`         | Main heading text.                                                      |
| `title-accent`  | Optional word/phrase appended to `title` with a gradient accent.        |
| `eyebrow`       | Small uppercase label with a status dot, shown above the title.         |
| `subtitle`      | Description text below the title.                                      |
| `badge-text`    | Pill badge above the title — alternative to `eyebrow` (eyebrow wins if both are set). |
| `badge-link`    | Link for the badge; if omitted the badge is plain text.                 |
| `cta1-text` / `cta1-link` | Primary button — filled. `cta1-link` defaults to `#`.        |
| `cta2-text` / `cta2-link` | Secondary button — outline with a trailing arrow.             |
| `cta3-text` / `cta3-link` | Third button, after `cta2` — same outline style. Omit for two CTAs (or one, or none). |
| `image`         | Background image URL, overlaid at low opacity behind the text.          |
| `align`         | `left` (default) or `center`.                                          |

Only `cta1` is filled/primary — `cta2` and `cta3` render identically (outline
pill + arrow), so they read as equal-weight alternatives rather than a
second-priority action.

## Tips

- CTAs render only for the ones you provide — set `cta1` alone for a single
  button, or all three for a page that splits into multiple next steps (e.g.
  two tracks a reader picks between).
- `eyebrow` and `badge-text` are alternatives, not additive — set one or the
  other depending on whether you want a status-dot label or a linkable pill.
- Prefer `align="center"` for a standalone landing page hero; `left` (default)
  reads better when the hero sits above other left-aligned page content.
