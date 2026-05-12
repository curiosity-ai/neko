---
name: snapframe
description: Define an automated browser screenshot — Neko captures the URL when the `neko snap` command runs and stores the image. Use to keep screenshots in docs fresh without manual capture.
---

# Snapframe

A `snapframe` block declares a screenshot recipe. Running `neko snap` walks
every snapframe in the docs and writes a fresh PNG. The accompanying
`![]()` image reference then displays the latest capture.

## Syntax

```markdown
[!snapframe https://neko.curiosity.ai]
![Neko site](/assets/snap/neko-home.png)
```

The first line is the snap directive; the second is a normal image reference
that points at where the snapshot will be written.

## With chrome and background

```markdown
[!snapframe https://neko.curiosity.ai --chrome macOS --bg GradientSunset]
![Neko homepage](/assets/snap/neko-home.png)
```

Supported chromes (varies by Neko version): `macOS`, `windows`, `none`.
Supported backgrounds: gradient or solid presets, or `--bg "#1f2937"`.

## With interactions

```markdown
[!snapframe https://example.com/app
click 'button.cookies-accept'
interact #email value='test@example.com'
wait 500
]
![Logged-in view](/assets/snap/example-app.png)
```

Each interaction line runs before the screenshot. Common verbs:

- `click 'selector'` — click an element.
- `interact 'selector' value='…'` — type into an input.
- `wait <ms>` — pause before the next step.
- `scroll 'selector'` — scroll an element into view.

## Tips

- Run `neko snap` only in CI or locally when you want updated screenshots.
- Commit the generated PNGs so visitors see images even if a future snap
  fails.
- Path-relative writes are resolved against the page's location; keep all
  snap PNGs under `assets/snap/` for tidiness.
