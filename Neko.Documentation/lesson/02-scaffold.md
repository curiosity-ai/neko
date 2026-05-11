---
title: Scaffold your first site
order: 2
kind: Interactive
duration: 10 min
---

# Scaffold your first site

A Neko site is just a folder of Markdown files plus a `neko.yml` configuration file at the root.

```bash
mkdir my-docs && cd my-docs
```

Create `neko.yml`:

```yaml
branding:
  title: My Docs

links:
  - text: Home
    link: /
    icon: house-chimney
```

Then add a `index.md` &mdash; this becomes your landing page.
