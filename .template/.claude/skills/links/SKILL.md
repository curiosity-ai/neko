---
name: links
description: Render a Neko ```links block (the Link Card) — a titled card where each row is a labelled link with a version pill on the right. Use for package/version manifests, download lists, and "current releases" tables.
---

# Links (Link Card)

A titled card whose rows are `text | url | version`. The label is rendered as a
link (when a URL is given) and the version as a monospace pill on the right.
Rows are separated by hairlines and the card adapts to light and dark mode.

Typical uses: a "current packages" manifest at the top of a changelog entry, a
download list, or a set of related releases.

## Syntax

````markdown
```links title="Current packages" icon="box"
Workspace · Docker | https://hub.docker.com/r/example/app | example/app:67298
Example.Client | https://www.nuget.org/packages/Example.Client | v26.6.1753
Example.CLI | https://www.nuget.org/packages/Example.CLI | v26.6.1718
```
````

## Block arguments

Arguments go on the fence info line, after `links`:

| Argument | Values                          | Notes |
| ---      | ---                             | --- |
| `title`  | string                          | Card heading, shown next to the icon. Omit for a card with no header. |
| `icon`   | [UIcon](../icon/SKILL.md) name  | Header icon. Defaults to `box`. |

## Row format

One row per body line, pipe-separated:

```
text | url | version
```

| Part      | Required | Notes |
| ---       | ---      | --- |
| `text`    | required | The row label. Becomes a link when a `url` is given. |
| `url`     | optional | Link target for the label. |
| `version` | optional | Rendered as the monospace pill on the right. |

Leave the URL empty to keep a plain label with a version pill:

````markdown
```links title="Versions" icon="tags"
Engine | | v26.6
Theme | | v3.2
```
````

Blank lines are skipped, and a line with neither a label nor a version is
ignored. Long labels truncate rather than wrap, so keep them short.

## Examples

Downloads with no versions:

````markdown
```links title="Downloads" icon="download"
Installer (Windows) | /assets/setup.exe
Compose file | /assets/docker-compose.yml
```
````

Related releases:

````markdown
```links title="Also released" icon="box"
Engine | /changelog/#v26-6 | v26.6
Theme | https://example.com/theme/releases | v3.2
```
````

## Related

- [`changelog`](../changelog/SKILL.md) — release timelines, where this card is commonly the manifest.
- [`change`](../change/SKILL.md) — the individual entries below the manifest.
- [`reference-link`](../reference-link/SKILL.md) — a single prominent link card.
- [`cards`](../cards/SKILL.md) — richer tiles with images, tags and palettes.
- [`file`](../file/SKILL.md) — a downloadable file card with a size.
