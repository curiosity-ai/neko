---
order: 50
icon: info
---
# About

This is a sample "about" page in the Neko starter. Replace it with whatever you like — a project overview, a team page, a changelog, contact info, anything.

## Markdown is the source of truth

Everything you see is plain Markdown with a small frontmatter header. Common metadata you can use on any page:

```yml
---
label: Friendly nav label
icon: info        # UIcon name, emoji shortcode, SVG, or image path
order: 50         # ordering inside the parent folder
tags: [intro]     # auto-generates tag index pages
visibility: public # public | hidden | protected | private
---
```

## A few formatting bits

A table:

| Feature | Notes |
| ---     | ---   |
| Markdown | All standard CommonMark plus extensions. |
| Components | Alerts, tabs, columns, cards, mermaid, math, etc. |
| Themes | Configured via `neko.yml`. |

A code block:

```csharp
Console.WriteLine("Hello, Neko!");
```

A reference link to another page:

[!ref Getting Started](getting-started.md)
