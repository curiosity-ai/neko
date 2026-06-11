---
icon: list-check
tags: [component]
---
# Link Card

The **Link Card** renders a titled, theme-agnostic *card of links*. Each row is a
labelled link on the left and a version pill on the right — ideal for "current
packages", download lists, related releases, and version manifests.

It is authored as a fenced ` ```links ` block. Each line in the body is one row,
in the form `text | url | version` (both `url` and `version` are optional).

||| Demo
```links title="Current packages" icon="box"
Workspace · Docker | https://hub.docker.com/r/curiosityai/curiosity | curiosityai/curiosity:67298
Curiosity.FrontEnd | https://www.nuget.org/packages/Neko | v26.6.1753
Tesserae | https://www.nuget.org/packages/Neko | v2026.6.67285
Curiosity.CLI | https://www.nuget.org/packages/Neko | v26.6.1718
```
||| Source
````md
```links title="Current packages" icon="box"
Workspace · Docker | https://hub.docker.com/r/curiosityai/curiosity | curiosityai/curiosity:67298
Curiosity.FrontEnd | https://www.nuget.org/packages/Neko | v26.6.1753
Tesserae | https://www.nuget.org/packages/Neko | v2026.6.67285
Curiosity.CLI | https://www.nuget.org/packages/Neko | v26.6.1718
```
````
|||

---

## Block arguments

Arguments go on the fence info line, after `links`:

| Argument | Required | Description |
| ---      | ---      | ---         |
| `title`  | optional | The card heading shown next to the icon. |
| `icon`   | optional | A [UIcon](icon.md) name for the heading. Defaults to `box`. |

---

## Rows

Each non-empty body line is one row, split on the pipe `|` character:

```
text | url | version
```

| Part      | Required | Description |
| ---       | ---      | ---         |
| `text`    | required | The row label. Becomes a link when a `url` is given. |
| `url`     | optional | Link target for the row label. |
| `version` | optional | Shown as a monospace pill on the right. |

A row with only a label and version (no URL) is fine:

||| Demo
```links title="Versions" icon="tags"
Engine | | v26.6
Theme | | v3.2
```
||| Source
````md
```links title="Versions" icon="tags"
Engine | | v26.6
Theme | | v3.2
```
````
|||

---

## Related

- [Version Badge](version-badge.md) — the inline pill used for a single version, with a copy button.
- [Card](cards.md) — richer content tiles with images, tags, and palettes.
- [Reference Link](reference-link.md) — a single prominent link card.
