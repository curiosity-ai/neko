---
title: Image Generation
icon: wand-magic-sparkles
tags: [component, ai, image, openai]
---
# Image Generation

The `img-gen` component lets you describe an image inline in your Markdown and
have Neko generate it for you on demand using an OpenAI image model. The
matching `neko gen-images` command walks every `.md` file under the input
directory, asks an LLM for a filename and an alt-text description for each
directive, calls the OpenAI image API, saves the PNG into the page's
`assets/img-gen/` folder, and rewrites the directive into a regular Markdown
image — preserving the original directive as an HTML comment so you can
re-generate the image later if needed.

By default Neko generates each image **at landscape `1536x1024`**, adds an
instruction to make it look good on a light theme, then **automatically
generates a paired dark-mode variant** by calling the image-edit endpoint with
the freshly generated light image. Both files are saved side-by-side
(`name.png` and `name-dark.png`) and the rewritten Markdown uses a
`src-dark="…"` attribute so the active theme picks the right one at runtime.
All defaults can be overridden globally in `neko.yml` or per-directive.

```bash
# Generate images for every [!img-gen ...] directive under the current
# directory. Defaults to the gpt-image-1 image model and gpt-4o-mini for the
# filename/alt-text generation step.
neko gen-images --api-key sk-... --input .
```

If `--api-key` is omitted, Neko reads `OPENAI_API_KEY` from the environment.
Only OpenAI is supported at the moment.

## Basic syntax

Place the directive on its own block. The body is the image prompt.

```md
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
```

Running `neko build` (or `neko watch`) **before** generating the image renders
nothing in place of the directive. After running `neko gen-images`, the
directive in the source file is rewritten to:

```md
<!--
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
-->
![A cozy cat by a rainy window in watercolor.](assets/img-gen/cozy-cat-rainstorm.png){src-dark="assets/img-gen/cozy-cat-rainstorm-dark.png"}
```

Subsequent `neko build` runs then render the image as any other Markdown
image. The `src-dark="…"` attribute makes Neko emit two `<img>` tags — the
light one hidden in dark mode, the dark one hidden in light mode — so the
right variant shows for the active theme. Re-enable regeneration by
uncommenting the directive and removing the image line.

If you set `darkMode: false` in `neko.yml` (or pass `dark=false` on a
single directive) the rewrite is just the plain `![alt](url)` with no dark
variant.

## Options

Attributes can be passed on the first line of the directive, before the
prompt:

```md
[!img-gen size=1024x1024 quality=high
A diagram of how Neko builds a static site, isometric pixel art
]
```

| Attribute    | Values                                                                                  | Notes                                                                                                |
| ---          | ---                                                                                     | ---                                                                                                  |
| `size`       | `auto`, `1024x1024` (square), `1536x1024` (landscape, default), `1024x1536` (portrait), `2048x2048` (2K square), `2048x1152` (2K landscape), `3840x2160` (4K landscape), `2160x3840` (4K portrait), plus any `<width>x<height>` | Defaults to the `imageGen.size` value in `neko.yml` (which itself defaults to `1536x1024`). Sizes from the `TornadoImageSizes` enum are sent directly; the larger 2K/4K resolutions go through `Custom` with explicit width/height. |
| `quality`    | `auto`, `low`, `medium`, `high`, `standard`, `hd`                                       | Forwarded to the API. Costs and latency scale with quality.                                          |
| `background` | `auto`, `opaque`, `transparent`                                                         | Only meaningful for models that support transparent backgrounds.                                     |
| `style`      | `natural`, `vivid`                                                                      | DALL·E-3 style preset. Ignored by other models.                                                      |
| `light`      | `true`, `false`                                                                         | Per-directive override of `imageGen.lightMode`. Defaults to the value in `neko.yml` (true).          |
| `dark`       | `true`, `false`                                                                         | Per-directive override of `imageGen.darkMode`. Defaults to the value in `neko.yml` (true).           |

## neko.yml configuration

Global defaults live under the `imageGen` section. Every key is optional.

```yml
imageGen:
  systemPrompt: "Use a flat illustration style with thin strokes and the Curiosity blue accent."
  size: 1536x1024            # default landscape
  lightMode: true            # append a 'render for light theme' instruction
  darkMode: true             # also generate a *-dark.png variant after the light one
  lightModePrompt: "Render this in light mode: bright background, colors tuned for a white theme."
  darkModePrompt: "Recreate this image in dark mode: dark background, colors tuned for a dark theme."
```

| Key               | Default                                                       | Purpose                                                                                                                 |
| ---               | ---                                                           | ---                                                                                                                     |
| `systemPrompt`    | _(empty)_                                                     | Appended to every `[!img-gen]` prompt before the image API is called. Use it for a global house style.                  |
| `size`            | `1536x1024`                                                   | Default image size when a directive doesn't specify one.                                                                |
| `lightMode`       | `true`                                                        | When true, `lightModePrompt` is appended to every prompt so the generated image renders well on a light theme.          |
| `darkMode`        | `true`                                                        | When true, Neko follows up the light generation with an image-edit call using `darkModePrompt` to make a dark variant.  |
| `lightModePrompt` | _(see source)_                                                | Override the boilerplate appended for light mode.                                                                       |
| `darkModePrompt`  | _(see source)_                                                | Override the boilerplate sent to the image-edit endpoint for the dark variant.                                          |

## Command-line options

| Flag             | Default          | Description                                                                                   |
| ---              | ---              | ---                                                                                           |
| `--input`, `-i`  | `.`              | Directory to scan recursively for `.md` files.                                                |
| `--api-key`      | `OPENAI_API_KEY` | OpenAI API key. If omitted, the env var is used.                                              |
| `--image-model`  | `gpt-image-1`    | OpenAI image model to call. Pass `dall-e-3` if you prefer DALL·E-3.                            |
| `--llm-model`    | `gpt-4o-mini`    | OpenAI chat model used to pick a filename and alt-text from the prompt.                        |

## How filenames and alt text are chosen

For every directive, Neko sends the prompt and the page name to the configured
chat model with a short system prompt asking for a JSON object of the form:

```json
{
  "filename": "lowercase-ascii-slug",
  "alt": "Short factual description of the generated image."
}
```

The filename is sanitised (lowercased, ASCII-only, `-`-separated, max 60
chars) and de-duplicated against files that already exist in the target
`assets/img-gen/` folder. If the chat call fails or returns invalid JSON,
Neko falls back to a slug derived from the prompt.

## Tips

- Commit the generated PNGs (and the rewritten Markdown) so visitors don't
  need an API key just to view the site.
- Use `quality=low` for drafts to keep API costs down, then re-generate with
  `quality=high` once you're happy with the prompt.
- The directive renders nothing in `neko build` until you generate the
  image — pages with un-generated directives will simply show no image yet.
- A dark variant doubles the per-image cost. Turn it off site-wide with
  `imageGen.darkMode: false`, or selectively with `dark=false` on individual
  directives (e.g. for screenshots that already work in both themes).
