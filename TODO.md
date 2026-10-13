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
- [x] **Presentation mode**: `presentation: true` front matter turns a file into a full-screen slide deck (slides split on `---`); `[!deck link="…"]` embeds one in a page.
- [x] **Document mode**: `document: true` front matter turns a file into paged A4/Letter sheets (pages split on `---`) with a **docx** download (native Word, fonts embedded); `neko` and `curiosity` themes.

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

## Video mode
- [x] `video:` front matter, scenes split on `---`, timeline compiler (C#), `video.js` seek runtime.
- [x] Scene components (lockup, swap, typewriter, scramble, counter, stats, chart, code, terminal, devices, cursor, lower-third, captions, quote, steps, checklist, grid/card, cta, logo, media).
- [x] `neko render` (Playwright frames + ffmpeg via Xabe.FFmpeg, audio mix), `[!video]` embed, `neko` and `curiosity` themes.
- [x] Voice-over track: `--- {narration}` parts on the video's clock (anchors, no overlaps), `narration:` voices (descriptions, Voxwerk catalog ids), `neko voiceover` synthesis with Voxwerk, timing sheet and preview panel.
- [ ] Size a scene from its `voice` file's length (ffprobe) when `duration` is left out.
- [ ] Voice-over: optionally size scenes to their lines (`duration="voice"`) — needs a fixed-point pass, since anchors depend on scene starts.
- [ ] Voice-over: burn the sentences in as on-screen captions (the `captions` component, timed from the takes).
- [ ] Voice-over: clone catalog voices with the Base model when their audio package is installed, for one consistent voice across sentences.
- [ ] Captions from a word-level transcript file instead of hand-timed lines.
- [ ] Shader (WebGL) transitions, composited per frame like HyperFrames' engine mode.
- [ ] Stream frames to ffmpeg's stdin instead of writing them to a temp folder.
