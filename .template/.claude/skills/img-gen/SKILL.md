---
name: img-gen
description: Declare an AI-generated image inline — Neko calls an OpenAI image model when the `neko gen-images` command runs, saves a PNG into the page's assets/img-gen/ folder, and rewrites the directive into a regular Markdown image with the original directive preserved as an HTML comment.
---

# Image Generation

A `[!img-gen]` directive describes an image you want generated. Running
`neko gen-images --api-key sk-...` walks every Markdown file under the input
folder, calls OpenAI to (1) pick a slug and an alt-text for the image and
(2) generate the PNG itself, writes the PNG into the page's
`assets/img-gen/` folder, then rewrites the directive in place to a real
Markdown image — keeping the original directive as an HTML comment so you
can re-generate later.

Only OpenAI is supported at the moment. The default models are
`gpt-image-1` for the image and `gpt-4o-mini` for the filename/alt-text
step; both can be overridden on the command line.

By default Neko generates each image **at landscape `1536x1024`**, appends a
"render for a light theme" instruction to the prompt, and then **automatically
generates a paired dark-mode variant** by calling the image-edit endpoint
with the light image as input. The rewritten Markdown carries a
`src-dark="…"` attribute so the active theme picks the right variant. All of
this is configurable in `neko.yml` (`imageGen` section) or per-directive.

## Syntax

```markdown
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
```

The block body is the image prompt — anything is allowed, including
multi-line descriptions.

## With attributes

Attributes go on the first line, before the newline:

```markdown
[!img-gen size=1024x1024 quality=high
A diagram of how Neko builds a static site, isometric pixel art
]
```

Supported attributes:

- `size` — `auto`, `1024x1024` (square), `1536x1024` (landscape, default),
  `1024x1536` (portrait), `2048x2048` (2K square), `2048x1152` (2K landscape),
  `3840x2160` (4K landscape), `2160x3840` (4K portrait), or any explicit
  `<width>x<height>`. Sizes matching the `TornadoImageSizes` enum are sent
  directly; the larger 2K/4K resolutions go through the `Custom` enum value
  with explicit `Width`/`Height`.
- `quality` — `auto`, `low`, `medium`, `high`, `standard`, `hd`.
- `background` — `auto`, `opaque`, `transparent`. Useful for models that
  support transparent backgrounds (gpt-image-1).
- `style` — `natural`, `vivid`. DALL·E-3 only.
- `light` — `true` / `false`. Override the global `imageGen.lightMode`
  setting for this directive only.
- `dark` — `true` / `false`. Override the global `imageGen.darkMode`
  setting for this directive only — set `dark=false` to skip the dark
  variant for a single image.

## Project-level defaults (`neko.yml`)

Defaults live under `imageGen:` in `neko.yml`. Every key is optional.

```yml
imageGen:
  systemPrompt: "Use a flat illustration style with thin strokes."
  size: 1536x1024            # default size when a directive omits it
  lightMode: true            # append a light-theme instruction to each prompt
  darkMode: true             # auto-generate a *-dark.png paired variant
  lightModePrompt: "Render this in light mode: bright background..."
  darkModePrompt: "Recreate this image in dark mode: dark background..."
```

The `systemPrompt` is appended to every `[!img-gen]` prompt — perfect for a
global house style. The light/dark prompts boilerplate; override them if the
defaults don't match your visual language.

## Command-line

```bash
neko gen-images \
  --input . \
  --api-key sk-... \
  --image-model gpt-image-1 \
  --llm-model gpt-4o-mini
```

If `--api-key` is omitted, Neko reads `OPENAI_API_KEY` from the
environment.

## What the rewrite looks like

Before:

```markdown
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
```

After running `neko gen-images` (with the default `darkMode: true`):

```markdown
<!--
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
-->
![A cozy cat by a rainy window, watercolor.](assets/img-gen/cozy-cat-rainstorm.png){src-dark="assets/img-gen/cozy-cat-rainstorm-dark.png"}
```

The `src-dark="…"` attribute makes Neko emit two `<img>` tags — the light
one hidden in dark mode (`dark:hidden`), the dark one hidden in light mode —
so the right variant shows automatically.

With `dark=false` (or `imageGen.darkMode: false` in `neko.yml`) you instead
get a plain Markdown image:

```markdown
<!-- … directive … -->
![A cozy cat by a rainy window, watercolor.](assets/img-gen/cozy-cat-rainstorm.png)
```

Subsequent `neko build` runs treat the page as a normal Markdown image
reference.

## Tips

- Commit both the rewritten Markdown and the generated PNGs (light **and**
  dark) so visitors see the image without anyone needing an API key.
- Use `quality=low` for drafts, then re-generate at higher quality once the
  prompt is right. To re-generate, delete the rewritten image line and
  uncomment the directive.
- The directive renders nothing in HTML until you run `neko gen-images`.
  This is intentional — pages with un-generated directives simply show no
  image yet.
- Filenames are picked by the LLM as a short lowercase ASCII slug, then
  sanitised and de-duplicated. They live alongside the page in
  `assets/img-gen/`. The dark variant uses the same slug with a `-dark`
  suffix.
- A dark variant doubles the per-image API cost. Turn it off site-wide with
  `imageGen.darkMode: false`, or selectively with `dark=false` on individual
  directives.
