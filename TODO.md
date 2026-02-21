# TODO

## Core
- [ ] Implement CLI entry point and `watch` command. (Done)
- [ ] Implement file watching and Kestrel integration. (Done)
- [ ] Implement configuration parsing (`taildocs.yml`). (Done)
- [ ] Implement Markdown parsing logic (Frontmatter, basic markdown). (Done)
- [ ] Implement HTML template generation. (Done)

## Components / Syntax
- [x] Badge component (`[!badge ...]`).
- [x] Alert component (`!!!` and `> [!ALERT]`).
- [ ] Tabs component (`+++`). (Partially implemented, needs refinement)
- [ ] Code blocks with syntax highlighting (Highlight.js).
- [ ] TOC generation.
- [ ] Search index generation. (Done)

### Missing Components to Implement
Please refer to the following list for future implementation. Suggested syntax follows standard Markdown or Retype-compatible conventions.

- [ ] **Backlinks**: Automatic generation of backlinks at the bottom of pages.
- [ ] **Button**: `[!button text="Click Me" link="/url" variant="primary"]`
- [ ] **Callout**: Enhanced alerts/callouts. Already partially covered by Alerts.
- [ ] **Code Block**: Standard markdown fence with language. Support `title="filename.cs"` and line highlighting.
- [ ] **Code Snippet**: `:::code source="path/to/file.cs" :::`
- [ ] **Color Chip**: `[!color-chip color="#ff0000" text="Red"]`
- [ ] **Column**: `:::column ... :::` for multi-column layouts.
- [ ] **Comments**: Integration with Giscus or Disqus.
- [ ] **Container**: `:::div class="custom-class" ... :::`
- [ ] **Embed**: Generic embed component.
- [ ] **Emoji**: `:smile:` support. (Basic support enabled)
- [ ] **File Download**: `[!file text="Download" link="file.zip"]`
- [ ] **Icon**: `[!icon name="home"]` or inline `:icon-home:`
- [ ] **Image**: Enhanced image syntax `![Alt](img.png){width=500}`
- [ ] **List**: Enhanced lists.
- [ ] **Math Formulas**: KaTeX integration. `$$ ... $$`
- [ ] **Mermaid**: Mermaid.js integration. ```mermaid ... ```
- [ ] **UIcons**: Flaticon integration. (Already embedded, need component helper)
- [ ] **Panel**: `:::panel ... :::`
- [ ] **Reference Link**: `[!ref text="Link" link="url"]`
- [ ] **Tab**: `+++ Title ... +++` (In progress)
- [ ] **Table**: Enhanced markdown tables.
- [ ] **YouTube Embed**: `[!youtube id="xyz"]`

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
