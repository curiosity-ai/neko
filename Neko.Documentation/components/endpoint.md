---
icon: cloud
tags: [component]
---
# API Endpoint

The **API Endpoint** component renders a REST endpoint reference entry in the
Microsoft Learn / Swagger style: a colour-coded HTTP method badge, the
monospace path, a summary, and an aligned details grid (Auth, Body, Returns, …).

It is authored as a fenced ` ```endpoint ` block. The first body line carries the
HTTP method and path; the rest holds the summary and the detail rows.

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

- **First line** — `<METHOD> <path>`. The method (`GET`, `POST`, `PUT`,
  `PATCH`, `DELETE`, `HEAD`, `OPTIONS`) is colour-coded; the rest is the path. If
  the first token isn't a known HTTP verb, the whole line is treated as the path
  and no badge is shown. The method/path go on the first body line (not the fence
  info line) so paths containing `{placeholders}` survive intact.
- **Summary** — the lines after the first line, up to the first blank line (or
  the first `Label:` line), become the summary.
- **Detail rows** — every later non-empty line in the form `Label: value` becomes
  a row in the details grid. Only the first colon splits the label from the
  value, so JSON colons in the value are preserved.

Summary and values are rendered as **inline Markdown**, so `` `code` ``,
**bold**, and links work.

## Example

```endpoint
POST /api/login/create
Exchange a username/password for a session JWT.

Auth: Bearer (session JWT)
Body: `{ "user": "...", "password": "..." }`
Returns: `{ "token": "...", "expiresAt": "..." }`
```

```endpoint
GET /api/login/check
Verify a session JWT is still valid.

Auth: Bearer (session JWT)
Returns: `{ "user": { "uid": "...", "name": "..." } }` or `401`.
```

```endpoint
DELETE /api/items/{id}
Delete an item by id.

Auth: Bearer (API token with the `items:write` scope)
Returns: `204 No Content`, or `404` if the item does not exist.
```

Each entry gets a stable anchor id derived from the method and path (e.g.
`#post-api-login-create`), with a permalink icon in the header.

## Notes

- A page can stack as many endpoint blocks as needed; each renders as its own
  card.
- The component ships its own styling, so it renders consistently in light and
  dark mode without relying on utility classes.
