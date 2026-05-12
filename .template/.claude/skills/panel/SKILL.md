---
name: panel
description: Render a panel (titled box) or a collapsible accordion item. Use `===` for expanded panels, `==-` for collapsed. Stack adjacent panels for an accordion.
---

# Panel

Panels are titled boxes. They can start expanded (`===`) or collapsed (`==-`),
and several adjacent panels become an accordion.

## Expanded panel

```markdown
=== Panel title
Body content. Any Markdown is allowed.
===
```

## Collapsed panel (click to expand)

```markdown
==- Collapsed by default
Hidden until the user clicks the header.
===
```

## Accordion

```markdown
==- What is Neko?
A .NET static site generator for Markdown docs.
===
==- How do I install it?
`dotnet tool install -g Neko`
===
==- Is it open source?
Yes — MIT licensed on GitHub.
===
```

Each item still closes with `===`. The accordion layout is automatic — there
is no enclosing wrapper.

## Tips

- Use panels for FAQs, optional details, advanced sections, and anything the
  reader might want to skip.
- Don't hide critical information inside a collapsed panel.
- For tabs (one visible at a time, no hide-on-load), use
  [`tab`](../tab/SKILL.md) instead.
