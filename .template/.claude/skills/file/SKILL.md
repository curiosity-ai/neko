---
name: file
description: Render a downloadable file card with icon, label, and optional file size. Use for attaching docs, PDFs, sample assets, archives.
---

# File

A download card that displays a file with icon, name, and optional metadata.
Visually heavier than a plain Markdown link — use when the download itself is
the main thing on the line.

## Syntax

```markdown
[!file](path/file.pdf)
[!file Friendly Label](path/file.pdf)
[!file text="Project Proposal" link="/assets/proposal.pdf" size="1.2 MB"]
[!file icon="rocket" text="Download"](path/file.pdf)
[!file icon=":rocket:" text="Launch"](path/file.pdf)
```

## Attributes

| Attribute | Notes                                                                  |
| ---       | ---                                                                    |
| `text`    | Display label. Defaults to the filename if omitted.                    |
| `link`    | File path (same as the `( )` form).                                    |
| `size`    | Optional human-readable size (`"1.2 MB"`, `"450 KB"`).                 |
| `icon`    | UIcon name, `:emoji:`, `<svg>`, or image path. Auto-picks an icon by extension if omitted. |

## Examples

```markdown
[!file](/assets/whitepaper.pdf)
[!file "Sample CSV"](/assets/data.csv)
[!file text="Annual report 2026" link="/assets/report.pdf" size="3.4 MB" icon="file-pdf"]
```

## When to use

- Reference / download pages.
- "Attachments" sections at the bottom of articles.
- Anywhere a plain `[link](path)` would feel underwhelming.

For inline file mentions inside prose, a plain Markdown link is still better.
For embedded PDF preview, see [`pdf`](../pdf/SKILL.md).
