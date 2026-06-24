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
| `mode`      | Site personality: `docs` (default) or `blog` (curiosity.ai marketing look).|
| `cname`     | Value written to `CNAME` for GitHub Pages.                                 |
| `sitemap`   | Generate `sitemap.xml`. Default `true`; skipped when `url` is unset.       |
| `branding`  | Logo, title, label, colours.                                               |
| `breadcrumb`| Friendly project name shown as the leading crumb in cross-project search.   |
| `theme`     | Theme name and overrides.                                                  |
| `meta`      | Default `<meta>` tags (description, keywords, author, image, og:type).     |
| `links`     | Header navigation.                                                         |
| `pageLinks` | Site-wide links rendered on top of every page's "On this page" TOC.        |
| `banner`    | Site-wide announcement bar. See [`banner`](../banner/SKILL.md).            |
| `footer`    | Footer content (`copyright`, links; plus marketing columns/social/badges). |
| `actions`   | Header call-to-action buttons (pills), e.g. *Book a Demo*. Best in blog mode.|
| `nav`       | Project-wide nav settings (`mode: stack`; `icons.mode` — sidebar icons, default `none`). |
| `layout`    | Page chrome: `sidebar`/`toc` toggles and the `maxWidth` content cap.       |
| `toc`       | Default right-sidebar TOC settings.                                        |
| `backlinks` | Default inbound-link block behaviour.                                      |
| `start`     | Dev-server tweaks (`pro: true`, ports).                                    |
| `snippets`  | Map of template variables substituted into pages.                          |
| `imageGen`  | Defaults for the `[!img-gen]` component (system prompt, size, light/dark). |
| `tesserae`  | Live `tesserae` sample compilation: `version` (pin) and `maxParallelism`.   |

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

## Breadcrumb (cross-project search)

In a multi-repo site, every search result leads with a crumb naming the
sub-project it belongs to (e.g. `Connect & Ingest › Guides`). This name comes
from the **navbar** by default: the `text` of the `links` entry whose target is
the project's root. The navbar already names every sub-project, so it is the
single shared source — no per-project setup needed.

Set `breadcrumb.label` only to override the navbar (or to name a sub-project
that isn't in the navbar):

```yml
breadcrumb:
  label: Connect & Ingest
```

Resolution order: navbar label → `breadcrumb.label` → `branding.label` →
`branding.title` → title-cased mount path. The override is project-local (not
inherited from a parent) and has no effect on the root project (no mount path).

## Theme

```yml
theme:
  name: curiosity              # built-in: curiosity, …
  baseColor: "#5495f1"
  # full overrides per theme — see https://neko.curiosity.ai
  font:                        # optional brand font (default: Inter)
    family: Plus Jakarta Sans
    url: https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap
```

`theme.font` overrides the site's base font. `family` is the CSS font-family
name (a full comma stack is accepted too); `url` is an optional stylesheet that
provides it (Google Fonts, a CDN, or a self-hosted `/assets/….css`). Leave it
unset to keep Neko's bundled Inter. It is inherited by multi-repo child sites.

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

## Mode (docs vs blog)

`mode` switches the chrome Neko renders around your pages. Default `docs`.

```yml
mode: blog
```

- **`docs`** (default) — the documentation chrome: a bordered white header with
  the dark-mode toggle, the logo paired with the branding title, the slim
  in-content footer.
- **`blog`** — the marketing-site look (as on curiosity.ai): a borderless
  header with the nav `links` **clustered next to the logo** (used on its own as
  a **wordmark**, no duplicated title) and the `actions` CTA buttons pushed to
  the right, plus an **edge-to-edge** marketing footer (the dark panel spans the
  pane width; its inner content is centred at `layout.maxWidth`). The
  page/header/CTA palette comes from `theme.base`. Blog mode is **light-only by
  default** (the theme toggle is hidden and light is locked). Define a
  `theme.dark` palette to **opt into dark mode** and bring the toggle back. The
  search box moves out of the header to the **top of the post list** and searches
  **inline** — typing filters the index live and renders the matching posts (with
  their tags) in place of the post grid, instead of opening the `⌘K` modal.

  ```yml
  mode: blog
  theme:
    base:                 # light palette
      base-bg: "#f1f1f1"
      base-color: "#1f1f1f"
    # dark:               # add this to enable dark mode + the theme toggle
    #   base-bg: "#0f1115"
    #   base-color: "#f1f1f1"
  ```

Blog mode pairs naturally with `layout.sidebar: false`, `layout.toc: false`,
`actions:`, and the marketing `footer:` fields below. It is inherited by
multi-repo child sites unless they set their own `mode`.

## Header actions (CTA buttons)

`actions` renders pill-shaped call-to-action buttons on the right of the navbar.
Available in both modes; most at home in blog mode.

```yml
actions:
  - text: Book a Demo
    link: https://example.com/request-demo
    variant: primary       # solid, filled pill (default)
    target: blank
  - text: Talk to Sales
    link: https://example.com/contact
    variant: outline        # bordered pill
    target: blank
```

| Key       | Purpose                                              |
| ---       | ---                                                  |
| `text`    | Button label.                                        |
| `link`    | Destination URL.                                     |
| `variant` | `primary` (default, solid) or `outline` (bordered).  |
| `icon`    | Optional leading UIcon name.                         |
| `target`  | `blank` to open in a new tab.                        |

## Footer

The slim footer (both modes) just needs a copyright line:

```yml
footer:
  copyright: "&copy; Copyright {{ year }}. All rights reserved."
```

In **blog mode**, add `columns` / `social` / `badges` (and optionally `logo` /
`tagline`) to render the edge-to-edge, dark marketing footer (its inner content
is centred at `layout.maxWidth`, lining the footer logo up under the header
logo). When none are set, blog mode falls back to a slim centred footer.

```yml
mode: blog

footer:
  copyright: "&copy; Copyright {{ year }} Example GmbH. All rights reserved."
  logo: /assets/logo-dark.png      # light/white logo for the dark panel
  tagline: The context graph for industrial AI.
  social:
    - icon: brands-twitter
      link: https://twitter.com/example
      label: X
    - icon: brands-linkedin
      link: https://www.linkedin.com/company/example/
      label: LinkedIn
  badges:
    - icon: shield-check
      title: GDPR Compliant
      description: Hosted in the EU
  columns:
    - title: Product
      links:
        - text: Integrations
          link: https://example.com/integrations
    - title: Company
      links:
        - text: Pricing
          link: https://example.com/pricing
```

## Layout

Controls the page chrome shared by every page.

```yml
layout:
  sidebar: true          # show the left navigation sidebar (default true)
  toc: true              # show the right "On this page" TOC (default true)
  maxWidth: screen-2xl   # cap + centre the content on wide screens (default screen-2xl)
```

`maxWidth` caps the width of the header content, the pivot tabs, and the
sidebar + content + TOC row, then centres them. On wide monitors the layout
stops expanding past this width instead of stretching edge-to-edge. Accepted
values:

- a Tailwind max-width token — `screen-2xl` (default, 1536px), `7xl`, `6xl`, …
- a full class — `max-w-[1800px]`
- a raw CSS length — `1600px`, `90rem`
- `full` or `none` to disable the cap and span the whole viewport (the previous
  behaviour).

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

## Tesserae sample compilation

Controls how live `tesserae` C# samples are compiled and cached. Both keys are
optional.

```yml
tesserae:
  version: "2026.6.67522"    # pin the Tesserae NuGet version (omit = resolve latest once, then reuse)
  maxParallelism: 4          # parallel compiles during the warm pass (0 = CPU count)
```

When `version` is omitted, Neko resolves the latest version once and records it
on disk under `.neko-cache/` in the project root, reusing it on every later build
(no expiry). Pin `version` (or delete the `.neko-cache/` folder) to move to a
different version. See the `tesserae` skill.

## Multi-repo

A nested `neko.yml` in a subfolder becomes its own documentation project
mounted at that subfolder's URL (e.g. `api-docs/neko.yml` → `/api-docs/`).
Children inherit `theme`, `branding`, and `snippets` from the parent.

## Tips

- Use **root-relative** paths (`/assets/logo.png`) so `permalink` overrides
  don't break references.
- Keep `links` short — long top navs hide on mobile.
- The 404 page is configured separately in `404.yml` (sibling of `neko.yml`).
