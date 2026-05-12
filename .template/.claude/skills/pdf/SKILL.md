---
name: pdf
description: Embed a PDF inline using the standard Markdown image syntax. Neko auto-detects the `.pdf` extension and renders an inline viewer. Use for whitepapers, specs, and any printable references that should be readable on the page.
---

# PDF

Neko embeds PDFs inline whenever a Markdown image reference points at a `.pdf`
file. No special component is needed.

## Syntax

```markdown
![Caption](/assets/document.pdf)
![](https://example.com/spec.pdf)
![Annual report 2026](/assets/report.pdf){ height="700px" }
```

The attribute block accepts `width`, `height`, and other HTML attributes the
same way as [`image`](../image/SKILL.md).

## Examples

```markdown
![Whitepaper](/assets/whitepaper.pdf)

![Spec](/assets/spec.pdf){ width="100%" height="800px" }
```

## Tips

- Provide a caption — it doubles as the alt text for screen readers and link
  text for the fallback download.
- For a download card (no inline preview), use [`file`](../file/SKILL.md):
  `[!file "Spec" link="/assets/spec.pdf"]`.
- Large PDFs can be slow to render in browsers. Keep them under a few MB or
  link out instead.
