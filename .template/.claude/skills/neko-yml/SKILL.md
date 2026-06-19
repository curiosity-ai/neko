---
name: neko-yml
description: Author the project-level `neko.yml` — input/output paths, URL, branding, theme, meta tags, top navigation, banner, footer, snippets. Use when configuring or editing a Neko site at the project level.
---

# neko.yml (project configuration)

The `neko.yml` file at the project root configures the whole site. It is
optional — Neko will auto-generate one on first `neko start` — but you almost
always want one.

## Minimum useful config

```yml
input: ./
output: .neko
url: docs.example.com

branding:
  title: Project Name
  label: Docs

links:
  - text: Home
    link: /
  - text: GitHub
    link: https://github.com/...
```

## Top-level keys

| Key         | Purpose                                                                    |
| ---         | ---                                                                        |
| `input`     | Root folder containing your `.md` files. Default `./`.                     |
| `output`    | Where to write the built site. Default `.neko`.                            |
| `url`       | Public hostname (no scheme). Used in canonical URLs and sitemap.           |
| `cname`     | Value written to `CNAME` for GitHub Pages.                                 |
| `sitemap`   | Generate `sitemap.xml`. Default `true`; skipped when `url` is unset.       |
| `branding`  | Logo, title, label, colours.                                               |
| `theme`     | Theme name and overrides.                                                  |
| `meta`      | Default `<meta>` tags (description, keywords, author, image, og:type).     |
| `links`     | Header navigation.                                                         |
| `pageLinks` | Site-wide links rendered on top of every page's "On this page" TOC.        |
| `banner`    | Site-wide announcement bar. See [`banner`](../banner/SKILL.md).            |
| `footer`    | Footer content (`copyright`, custom links).                                |
| `nav`       | Project-wide nav settings (e.g. `mode: stack`).                            |
| `toc`       | Default right-sidebar TOC settings.                                        |
| `backlinks` | Default inbound-link block behaviour.                                      |
| `start`     | Dev-server tweaks (`pro: true`, ports).                                    |
| `snippets`  | Map of template variables substituted into pages.                          |
| `imageGen`  | Defaults for the `[!img-gen]` component (system prompt, size, light/dark). |

## Branding

```yml
branding:
  title: Project
  label: Docs                  # text right of the title
  logo: /assets/logo.png
  logoDark: /assets/logo-dark.png
  logoAlign: left              # or right
  favicon: /assets/favicon.ico
  baseColor: "#5495f1"
  repository: https://github.com/owner/repo
```

## Theme

```yml
theme:
  name: curiosity              # built-in: curiosity, …
  baseColor: "#5495f1"
  # full overrides per theme — see https://neko.curiosity.ai
```

## Meta defaults

```yml
meta:
  description: Short site description.
  keywords: docs, neko, project
  author: Your Team
  image: /assets/og-image.png
  type: website
```

## Top navigation (`links:`)

```yml
links:
  - text: Home
    link: /
    icon: house-chimney
  - text: Guides
    link: /guides/getting-started
    icon: book
  - text: Features            # dropdown
    items:
      - text: Components
        link: /components/components
        description: Rich content blocks
        icon: cube
      - text: Configuration
        link: /configuration
        icon: settings
    footerItems:              # extra rows in dropdown footer
      - text: GitHub
        link: https://github.com/...
        icon: brands-github
  - text: GitHub
    link: https://github.com/...
    icon: brands-github
```

Any group with `items` also doubles as a **contextual pivot**: while the reader
is inside the section, the items render as a tab bar directly below the header,
with the active tab highlighted. A page is "inside" when its URL sits under one
of the item links. This is the default — no extra key is needed — and the flyout
dropdown still works on top of it. See the
[project config docs](https://neko.curiosity.ai/configuration/core/project#items).

**Navigation icons are hidden by default.** The `icon:` you set on links still
lives in `neko.yml`, but you opt into showing it per context under `nav:`:

```yml
nav:
  headerIcons: true     # icons on top-bar links + dropdown triggers
  dropdownIcons: true   # icons on items inside dropdown flyouts
  pivotIcons: true      # icons on the pivot tab bar
```

All three default to `false`. (This is separate from `nav.icons.mode`, which
governs the left **sidebar** icons.)

## Page links (`pageLinks:`)

Site-wide links rendered above the right-sidebar "On this page" TOC. Use for
actions that should be one click away from every page — *Report an issue*,
*Suggest an edit*, *Quote this page* in an email, etc.

Each entry takes a `label`, an `icon`, a `url` template, and an optional
`target`. Three placeholders in the `url` template are URL-encoded and
substituted at click time:

- `${page}` — the current page title.
- `${url}` — the absolute URL of the current page.
- `${selection}` — the visitor's current text selection (empty if none).

```yml
pageLinks:
  - label: Report an issue
    icon: bug
    url: "https://github.com/owner/repo/issues/new?title=Issue%20on%20page%20${url}"
    target: blank
  - label: Quote this page
    icon: quote-right
    url: "mailto:editor@example.com?subject=${page}&body=${selection}"
```

`pageLinks` only render when the page has a TOC; pages with `toc: false`
hide them.

## Footer

```yml
footer:
  copyright: "&copy; Copyright {{ year }}. All rights reserved."
```

## Image generation defaults

Sets global behaviour for the `[!img-gen]` component (run with
`neko gen-images`). Each key is optional.

```yml
imageGen:
  systemPrompt: "Use a flat illustration style with thin strokes."
  size: 1536x1024            # default landscape, used when a directive omits `size`
  lightMode: true            # append a light-theme instruction to every prompt
  darkMode: true             # also generate a paired *-dark.png variant
  lightModePrompt: "Render this in light mode: bright background..."
  darkModePrompt: "Recreate this image in dark mode: dark background..."
```

See the `img-gen` skill for per-directive overrides (`size=`, `light=`,
`dark=`).

## Multi-repo

A nested `neko.yml` in a subfolder becomes its own documentation project
mounted at that subfolder's URL (e.g. `api-docs/neko.yml` → `/api-docs/`).
Children inherit `theme`, `branding`, and `snippets` from the parent.

## Tips

- Use **root-relative** paths (`/assets/logo.png`) so `permalink` overrides
  don't break references.
- Keep `links` short — long top navs hide on mobile.
- The 404 page is configured separately in `404.yml` (sibling of `neko.yml`).
