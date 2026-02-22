---
title: File Component
icon: file
---

# File

The File component creates a download card for linking to files. It displays a document icon and file details.

## Syntax

Use the `[!file ...]` syntax.

```markdown
[!file text="MyDocument.pdf" link="docs/MyDocument.pdf" size="1.2 MB"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `text` | The display name of the file. | "Download" |
| `link` | The URL to the file. | `#` |
| `size` | Optional text to display the file size or type. | - |

### Example

[!file text="Project Proposal.docx" link="#" size="45 KB"]
