---
title: "PDF Integration"
description: "How to render PDF files inline in the text."
icon: file-pdf
order: 8
---

# PDF Integration

Neko supports rendering PDF files inline within your markdown documents. It uses the same syntax as images, but automatically detects that the file is a PDF based on the `.pdf` extension.

## Basic Usage

To embed a PDF, use the standard markdown image syntax:

```markdown
![PDF Document](/assets/dummy.pdf)
```

This will automatically render the PDF in an iframe.

## Example

![Example PDF](https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf)
