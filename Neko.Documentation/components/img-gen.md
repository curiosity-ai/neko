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
![A cozy cat by a rainy window in watercolor.](assets/img-gen/cozy-cat-rainstorm.png)
```

Subsequent `neko build` runs then render the image as any other Markdown
image. Re-enable regeneration by uncommenting the directive and removing the
image line.

## Options

Attributes can be passed on the first line of the directive, before the
prompt:

```md
[!img-gen size=1024x1024 quality=high
A diagram of how Neko builds a static site, isometric pixel art
]
```

| Attribute    | Values                                                      | Notes                                                                 |
| ---          | ---                                                         | ---                                                                   |
| `size`       | `auto`, `1024x1024`, `1024x1536`, `1536x1024`, `512x512`, ... | Forwarded to the OpenAI image API. Defaults to the model's default.    |
| `quality`    | `auto`, `low`, `medium`, `high`, `standard`, `hd`            | Forwarded to the API. Costs and latency scale with quality.            |
| `background` | `auto`, `opaque`, `transparent`                              | Only meaningful for models that support transparent backgrounds.       |
| `style`      | `natural`, `vivid`                                           | DALL·E-3 style preset. Ignored by other models.                        |

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
