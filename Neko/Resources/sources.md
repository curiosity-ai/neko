# Sources and Licenses

## External Libraries

### Markdig
- **Source**: https://github.com/xoofx/markdig
- **License**: BSD-2-Clause

### System.CommandLine
- **Source**: https://github.com/dotnet/command-line-api
- **License**: MIT

### YamlDotNet
- **Source**: https://github.com/aaubry/YamlDotNet
- **License**: MIT

### Tailwind CSS
- **Source**: https://tailwindcss.com
- **License**: MIT
- **Approach**: Neko generates `assets/tailwind.css` at build time with a
  pure-C# port of Tailwind v3's utility generator (`Neko/Builder/Tailwind/`) —
  no Node/npm, no CDN, no downloaded binary. The `base` (Preflight) and
  `components` (`@tailwindcss/typography`) layers are captured verbatim from
  the official Tailwind v3.4 standalone CLI and shipped as embedded resources
  (`Neko/Resources/tailwind/preflight.css`, `typography.css`); only the
  content-dependent `utilities` layer is generated in C#. Re-capture the two
  static layers when the pinned Tailwind version changes.
- **Static layers**: `Neko/Resources/tailwind/preflight.css`,
  `Neko/Resources/tailwind/typography.css` (Tailwind v3.4.17, typography MIT).

### Flaticon UIcons
- **Source**: https://github.com/freepik-company/flaticon-uicons
- **License**: Flaticon License
- **File**: `Neko/Resources/uicons-regular-rounded.css` (Referenced via CDN)

### MiniSearch
- **Source**: https://github.com/lucaong/minisearch
- **License**: MIT
- **File**: `Neko/Resources/minisearch.min.js` (Downloaded from CDN)

### PptxGenJS
- **Source**: https://github.com/gitbrent/PptxGenJS
- **License**: MIT (bundles JSZip, MIT/GPLv3 dual-licensed)
- **Version**: 4.0.1
- **File**: `Neko/Resources/pptxgen.bundle.js` (vendored copy of `dist/pptxgen.bundle.js`; never loaded from a CDN)
- **Used by**: `Neko/Resources/presentation-pptx.js` — the "Download PPTX" control on presentation decks. Loaded lazily, only when the reader asks for the download.
- **Local patch**: in the text-body writer, paragraph properties (`<a:pPr>`) are written for a paragraph's first run only (`i+=0===e?n.replace(...):""`). Upstream writes them for every run, so a paragraph with mixed formatting carried several `<a:pPr>` (invalid DrawingML), each later one with `<a:buNone/>`, and renderers dropped the bullet of any list item that starts with a bold lead-in. Second patch: `line.beginArrowSize` / `line.endArrowSize` (`sm`, `med`, `lg`) survive the line-option normalisation and are written as the `w` and `len` of `<a:headEnd>` / `<a:tailEnd>`; upstream only writes the end type, so every arrowhead was the medium default. Re-apply both when updating the file.

### docx
- **Source**: https://github.com/dolanmiu/docx
- **License**: MIT
- **Version**: 9.8.1
- **File**: `Neko/Resources/docx.bundle.js` (vendored copy of `dist/index.umd.cjs`, minified; never loaded from a CDN)
- **Local patch**: writes `<w:embedTrueTypeFonts/>` to `settings.xml` when a document embeds fonts, so Word keeps
  them when a reader re-saves the file. Rebuild with `reference/docx/build-docx-bundle.py`.
- **Used by**: `Neko/Resources/document-docx.js` — the "docx" control on paged documents (`document:` front matter). Loaded lazily, only when the reader asks for the download.

### Pele
- **Source**: https://github.com/obsidianmd/pele (https://pele.run)
- **License**: MIT
- **Version**: 1.6.0
- **File**: `Neko/Resources/pele.min.js` (the npm package's `dist/pele.global.js`, unchanged; rebuild with `reference/pele/build-pele.py`)
- **Used for**: drawing ```` ```mermaid ```` blocks. A site page loads it from `assets/pele.min.js` on demand, only when the page has a diagram; decks and documents inline it. It is never loaded from a CDN.

### Highlight.js
- **Source**: https://github.com/highlightjs/highlight.js
- **License**: BSD-3-Clause
- **File**: `Neko/Resources/highlight/highlight.min.js` (Downloaded from CDN)
- **Themes**: `github.min.css` (default light), `tokyo-night-dark.min.css` (default dark), `tokyo-night-light.min.css` (Downloaded from CDN)

### Schibsted Grotesk
- **Source**: https://github.com/schibsted/schibsted-grotesk (via Google Fonts)
- **License**: SIL Open Font License 1.1 (`reference/deck-fonts/OFL-schibstedgrotesk.txt`)
- **Files**: `Neko/Resources/deckfonts/SchibstedGrotesk-*.ttf`, static instances at 400, 500,
  600 and 700, built by `reference/deck-fonts/build-deck-fonts.py`. Used by the curiosity
  presentation theme and embedded in its PowerPoint exports.

### Geist Mono
- **Source**: https://github.com/vercel/geist-font (via Google Fonts)
- **License**: SIL Open Font License 1.1 (`reference/deck-fonts/OFL-geistmono.txt`)
- **Files**: `Neko/Resources/deckfonts/GeistMono-*.ttf`, static instances at 400, 500 and 700,
  built by `reference/deck-fonts/build-deck-fonts.py`.

### Inter
- **Source**: https://github.com/rsms/inter
- **License**: SIL Open Font License 1.1 (`reference/inter/`)
- **Files**: `Neko/Resources/deckfonts/Inter-Regular.ttf` and `Inter-SemiBold.ttf`, the site's woff2 files subset to
  Latin and written as TrueType by `reference/deck-fonts/build-deck-fonts.py`. Used by the neko document theme and
  embedded in its Word exports.

### Standalone deck and document libraries
Presentation decks and paged documents embed every dependency in their own HTML
(`Neko/Builder/HtmlGenerator.Standalone.cs`). The files below are vendored for that in
`Neko/Resources/standalone/`, never copied to a site's `assets/` and never loaded from a
CDN. Rebuild them with `reference/standalone/build-standalone.py`.

- **KaTeX** 0.16.8 — https://github.com/KaTeX/KaTeX — MIT. `katex.min.js`,
  `auto-render.min.js`, `katex.min.css` (rewritten to reference only the woff2 faces)
  and the `KaTeX_*.woff2` fonts.
- **highlightjs-line-numbers.js** 2.8.0 — https://github.com/wcoder/highlightjs-line-numbers.js — MIT.
- **leader-line-new** 1.1.9 — https://github.com/anseki/leader-line — MIT. `leader-line.min.js`.
- **Archivo, Source Serif 4, IBM Plex Mono** — Google Fonts, SIL Open Font License 1.1.
  Latin and Latin Extended subsets as woff2, with `deck-google-fonts.css`; the
  `midnight` and `daylight` deck themes' typefaces.
- **Twemoji** 14.0.2 — https://github.com/twitter/twemoji — graphics CC-BY 4.0.
  `twemoji.zip` holds the SVGs `emoji.css` references; a page inlines the ones it shows.

### Voxwerk (video voice-over)
- **Source**: https://github.com/theolivenbaum/voxwerk — NuGet `Voxwerk` and
  `Voxwerk.Voices.English`, referenced (not vendored) by `Neko/Neko.csproj`.
- **Use**: `neko voiceover` / `neko render` synthesize a video's narration with it:
  Qwen3-TTS inference in pure C#, plus the English voice catalog. It brings
  Concentus (Ogg Opus, BSD-3-Clause) and NAudio.Core (WAV I/O, MIT).
- **Model**: the Qwen3-TTS VoiceDesign weights are downloaded on first use from
  Hugging Face (`Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign`), Apache-2.0 by the Qwen team;
  they are never bundled with Neko.
- **Generated audio**: the voice-over takes a project keeps under
  `assets/voiceover/` are produced by that model from the project's own text.

## Curiosity brand material (video mode)

The curiosity video theme's drawings, glyphs and mascot are Curiosity GmbH's own brand
material, ported rather than vendored:

- **Pixel glyph frames** (`Neko/Builder/CuriosityBrand.cs`) — copied from Tesserae's
  `PixelGlyphKind.cs` (https://github.com/curiosity-ai/tesserae), which carries the
  Curiosity website's `site/figures.js` frames.
- **Sudo's sprite sheet** (`CuriosityBrand.SudoSheet`) — the compressed sheet from the
  Curiosity website's `js/sudo-sheet.js`.
- **Canvas painters** (`video-paint.js`) — re-written from the website's `field.js`,
  `decor.js`, `intro.js` and `logo-cube.js` and Tesserae's `FlowField`, `DashMosaic` and
  `PixelIntro` as pure functions of time.
