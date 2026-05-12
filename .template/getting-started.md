---
order: 100
icon: rocket
tags: [guide]
---
# Getting Started

This page walks through running the starter site and adding your first content.

## 1. Install Neko

Neko is a .NET global tool. Install it once:

```bash
dotnet tool install -g Neko
```

## 2. Run the dev server

From this folder, run:

```bash
neko start
```

Neko will:

- [x] discover every `.md` file
- [x] build the site to the `.neko` output folder
- [x] open the site in your browser
- [x] watch for file changes and reload automatically

## 3. Add a page

Create a new `.md` file anywhere in this folder, for example `guides/install.md`. Add a frontmatter block and write Markdown:

```md
---
order: 50
icon: download
---
# Install
Write whatever you want here.
```

Neko will add the new page to the navigation automatically. Folders become navigation groups; use an `index.yml` to configure a folder's label, icon, or order.

## 4. Use a component

Components are written inline in Markdown. Two quick examples:

!!! success It works
This is an alert.
!!!

|||  What
A two-column layout.
|||  Why
Great for comparisons and side-by-side examples.
|||

For the full catalog, see the [components reference](https://neko.curiosity.ai/components/components) or browse the skills under `.claude/skills/`.

## 5. Build for production

When you are ready to publish:

```bash
neko build
```

The output goes to `.neko/` (configurable via `output:` in `neko.yml`). Deploy that folder to any static host (GitHub Pages, Netlify, Cloudflare Pages, S3, Nginx, etc.).
