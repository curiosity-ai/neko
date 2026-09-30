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
