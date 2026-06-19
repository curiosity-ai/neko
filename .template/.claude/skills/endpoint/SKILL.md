---
name: endpoint
description: Render a REST API endpoint reference entry (Microsoft Learn / Swagger style) — colour-coded HTTP method badge, monospace path, summary, and an Auth/Body/Returns details grid. Use for documenting REST endpoints in an API reference.
---

# API endpoint

Renders one REST endpoint as a reference card: a colour-coded HTTP method
badge, the monospace path, a summary, and an aligned details grid. Authored
as a fenced ` ```endpoint ` block (dispatched from the code-block renderer).

## Syntax

````markdown
```endpoint
POST /api/login/create
Exchange a username/password for a session JWT.

Auth: Bearer (session JWT)
Body: `{ "user": "...", "password": "..." }`
Returns: `{ "token": "...", "expiresAt": "..." }`
```
````

## Format

- **First body line**: `<METHOD> <path>`.
  - `METHOD` is one of `GET`, `POST`, `PUT`, `PATCH`, `DELETE`, `HEAD`,
    `OPTIONS` — colour-coded (GET=blue, POST=green, PUT=amber, PATCH=teal,
    DELETE=red). If the first token isn't a known verb, the whole line is the
    path and no badge renders.
  - Method/path go on the **first body line**, not the fence info line, so a
    path with `{placeholders}` (e.g. `/api/items/{id}`) is not eaten by the
    generic-attributes parser.
- **Summary**: the line(s) after the first line, before the first blank line or
  the first `Label:` line.
- **Detail rows**: each later non-empty line in the form `Label: value`. Only
  the first colon splits label from value, so JSON colons survive. A line that
  isn't `Label: value` is appended to the previous value as a continuation.

Summary and values render as **inline Markdown** — `` `code` ``, **bold**, and
links all work.

## Behaviour

- Each block gets a stable anchor id from the method + path (e.g.
  `post-api-login-create`) and a permalink icon in the header.
- Stack multiple blocks on a page — each is its own card.
- The component carries its own CSS, so it renders the same in light and dark
  mode and does not depend on utility classes.

## Tips

- One endpoint per block. Group related endpoints under a normal `###` heading.
- Keep the summary to a sentence or two; put specifics in `Auth`, `Body`,
  `Returns`, `Scope`, `Params`, etc. detail rows.
- Wrap JSON and identifiers in backticks so they render as code.
