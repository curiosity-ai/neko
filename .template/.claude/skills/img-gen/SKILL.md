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

- `size` — `auto`, `1024x1024`, `1024x1536`, `1536x1024`, `512x512`,
  `1024x1792`, `1792x1024`. Forwarded to the OpenAI API.
- `quality` — `auto`, `low`, `medium`, `high`, `standard`, `hd`.
- `background` — `auto`, `opaque`, `transparent`. Useful for models that
  support transparent backgrounds (gpt-image-1).
- `style` — `natural`, `vivid`. DALL·E-3 only.

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

After running `neko gen-images`:

```markdown
<!--
[!img-gen
A cozy cat sitting by a window during a rainstorm, watercolor style
]
-->
![A cozy cat by a rainy window, watercolor.](assets/img-gen/cozy-cat-rainstorm.png)
```

Subsequent `neko build` runs treat the page as a normal Markdown image
reference.

## Tips

- Commit both the rewritten Markdown and the generated PNG so visitors see
  the image without anyone needing an API key.
- Use `quality=low` for drafts, then re-generate at higher quality once the
  prompt is right. To re-generate, delete the rewritten image line and
  uncomment the directive.
- The directive renders nothing in HTML until you run `neko gen-images`.
  This is intentional — pages with un-generated directives simply show no
  image yet.
- Filenames are picked by the LLM as a short lowercase ASCII slug, then
  sanitised and de-duplicated. They live alongside the page in
  `assets/img-gen/`.
