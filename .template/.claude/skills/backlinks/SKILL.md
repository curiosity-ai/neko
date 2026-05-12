---
name: backlinks
description: Configure or place a Backlinks block — the list of pages that link to the current page. Use when the user wants to show inbound references or to disable the auto-appended backlinks section.
---

# Backlinks

Backlinks show every page that links to the current page. By default Neko
appends a backlinks section to pages that have inbound links.

## Inline placement

Drop the block where you want the list to appear:

```markdown
[!backlinks]

[!backlinks "Related pages"]
```

The optional quoted string overrides the section heading.

## Project-level configuration

In `neko.yml`:

```yml
backlinks:
  enabled: true        # default
  title: "See also"    # default
  maxResults: 12       # default
```

## Page-level configuration

In page frontmatter:

```yml
---
backlinks:
  enabled: false       # disable on this page only
  title: "Referenced by"
  maxResults: 5
---
```

## When to use

- **Knowledge bases / wikis** — enabled by default (recommended).
- **Landing pages / index pages** — usually `backlinks: { enabled: false }` to
  avoid cluttering navigation.
- **Pages with many references** — set `maxResults` lower or use a custom
  heading.
