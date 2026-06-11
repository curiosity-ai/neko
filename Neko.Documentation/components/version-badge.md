---
icon: tags
tags: [component]
---
# Version Badge

The **Version Badge** is a compact, theme-agnostic *package pill*: a muted label,
a bold monospace version, an optional link, and a one-click copy button. It is
prefixed with a `!version-badge` identifier and uses `key="value"` attributes.

||| Demo
[!version-badge text="Curiosity.FrontEnd" version="v26.6.1753" url="https://www.nuget.org/packages/Neko"]
||| Source
```md
[!version-badge text="Curiosity.FrontEnd" version="v26.6.1753" url="https://www.nuget.org/packages/Neko"]
```
|||

Several badges placed next to each other flow inline and wrap onto the same line:

||| Demo
[!version-badge text="Curiosity.FrontEnd" version="v26.6.1753" url="https://www.nuget.org/packages/Neko"] [!version-badge text="Tesserae" version="v2026.6.67285" url="https://www.nuget.org/packages/Neko"] [!version-badge text="Curiosity.CLI" version="v26.6.1718"]
||| Source
```md
[!version-badge text="Curiosity.FrontEnd" version="v26.6.1753" url="https://www.nuget.org/packages/Neko"]
[!version-badge text="Tesserae" version="v2026.6.67285" url="https://www.nuget.org/packages/Neko"]
[!version-badge text="Curiosity.CLI" version="v26.6.1718"]
```
|||

---

## Attributes

| Attribute | Required | Description |
| ---       | ---      | ---         |
| `text`    | optional | The muted label shown before the version (e.g. a package name). |
| `version` | optional | The version string, rendered in bold monospace. |
| `url`     | optional | When set, the label + version become a link to this URL. |
| `copy`    | optional | What the copy button writes to the clipboard. Defaults to `version`, then `text`. |
| `icon`    | optional | A [UIcon](icon.md) name shown at the start of the pill. |

At least one of `text` or `version` should be provided.

---

## Copy button

Every version badge has a copy button. Clicking it copies the `copy` value (the
`version` by default) to the clipboard and briefly swaps the icon to a checkmark.
Use `copy=` to override what gets copied — handy for full image references:

||| Demo
[!version-badge text="Workspace · Docker" version="curiosityai/curiosity:67298" copy="curiosityai/curiosity:67298"]
||| Source
```md
[!version-badge text="Workspace · Docker" version="curiosityai/curiosity:67298" copy="curiosityai/curiosity:67298"]
```
|||

---

## With an icon

||| Demo
[!version-badge icon="box" text="Package" version="v1.4.0" url="#with-an-icon"]
||| Source
```md
[!version-badge icon="box" text="Package" version="v1.4.0" url="#with-an-icon"]
```
|||

---

## Related

- [Badge](badge.md) — general-purpose inline label.
- [Link Card](link-card.md) — a titled card of links, each with a version pill.
- The [Changelog](/changelog) uses a version badge in each release header.
