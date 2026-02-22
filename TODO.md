# TODO

## Core
- [x] Implement CLI entry point and `watch` command.
- [x] Implement file watching and Kestrel integration.
- [x] Implement configuration parsing (`taildocs.yml`).
- [x] Implement Markdown parsing logic (Frontmatter, basic markdown).
- [x] Implement HTML template generation.

## Components / Syntax
- [x] Badge component (`[!badge ...]`).
- [x] Alert component (`!!!` and `> [!ALERT]`).
- [x] Tabs component (`+++`).
- [x] Code blocks with syntax highlighting (Highlight.js).
- [x] TOC generation.
- [x] Search index generation.

### Missing Components to Implement
Please refer to the following list for future implementation. Suggested syntax follows standard Markdown or Standard conventions.

- [x] **Backlinks**: Automatic generation of backlinks at the bottom of pages.
- [x] **Button**: `[!button text="Click Me" link="/url" variant="primary"]`
- [x] **Callout**: Enhanced alerts/callouts. Already partially covered by Alerts.
- [x] **Code Block**: Standard markdown fence with language. Support `title="filename.cs"` and line highlighting.
- [x] **Code Snippet**: `:::code source="path/to/file.cs" :::`
- [x] **Color Chip**: `[!color-chip color="#ff0000" text="Red"]`
- [x] **Column**: `:::column ... :::` for multi-column layouts.
- [x] **Container**: `:::div class="custom-class" ... :::`
- [x] **Embed**: Generic embed component.
- [x] **Emoji**: `:smile:` support.
- [x] **File Download**: `[!file text="Download" link="file.zip"]`
- [x] **Icon**: `[!icon name="home"]` or inline `:icon-home:`
- [x] **Image**: Enhanced image syntax `![Alt](img.png){width=500}`
- [x] **List**: Enhanced lists.
- [x] **Math Formulas**: KaTeX integration. `$$ ... $$`
- [x] **Mermaid**: Mermaid.js integration. ```mermaid ... ```
- [x] **UIcons**: Flaticon integration. (Added `Neko.Tools.UIcons` helper tool).
- [x] **Panel**: `:::panel ... :::`
- [x] **Reference Link**: `[!ref text="Link" link="url"]`
- [x] Tab: `+++ Title ... +++`
- [x] **Table**: Enhanced markdown tables.
- [x] **YouTube Embed**: `[!youtube id="xyz"]`

## Assets
- [x] Embed Tailwind CSS.
- [x] Embed Inter font.
- [x] Embed Flaticon UIcons.
- [x] Embed Minisearch script.
- [x] Implement Search UI.

## Testing
- [x] Add unit tests for Markdown parsing.
- [x] Add end-to-end tests (Text verification).
- [ ] Verify output against sample docs.
