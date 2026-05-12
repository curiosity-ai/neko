---
name: banner
description: Configure the site-wide announcement banner shown above the header — text, link, colours, dismissibility, and version id. This is a neko.yml setting, not a Markdown component.
---

# Banner

The banner is a strip displayed at the top of every page, above the navigation
header. It is configured in `neko.yml` and **not** placed inline in Markdown.

## Syntax (neko.yml)

```yml
banner:
  text: "Welcome to Neko! Check out the new release."
  link: /changelog
  linkText: "Read the changelog"
  visible: true
  background: bg-indigo-600     # any Tailwind bg-* utility
  color: text-white             # any Tailwind text-* utility
  id: announcement-v2           # version key for dismissal
  dismissible: true
```

## Attributes

| Attribute     | Type    | Notes                                                                 |
| ---           | ---     | ---                                                                   |
| `text`        | string  | Banner copy.                                                          |
| `link`        | string  | Optional URL/path; the whole banner becomes clickable if set.         |
| `linkText`    | string  | Custom call-to-action label appended to the text.                     |
| `visible`     | boolean | Master switch; default `true`.                                        |
| `background`  | string  | Tailwind bg utility, e.g. `bg-indigo-600`, `bg-gradient-to-r ...`.    |
| `color`       | string  | Tailwind text colour, e.g. `text-white`.                              |
| `id`          | string  | Unique version key. Changing it re-shows the banner to users who dismissed an earlier one. |
| `dismissible` | boolean | When `true`, shows an "x" button; user choice persists in localStorage keyed by `id`. |

## Usage tips

- Change `id` whenever the banner content changes so previously dismissed users
  see the new message.
- Keep `text` short — long banners wrap awkwardly on mobile.
- Use a contrasting `background`/`color` pair (e.g. `bg-indigo-600` +
  `text-white`).
- For multi-repo setups, the root `neko.yml` banner is inherited by sub-sites
  unless they override it.
