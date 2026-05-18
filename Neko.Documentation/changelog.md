---
order: 2
icon: memo
layout: changelog
---
# Changelog

Neko is currently under active development.

Please note that Neko uses a calendar versioning approach.


## v26.5

* **Feature**: Added a `neko gen-dark-images` command that backfills missing dark-mode variants for images previously created by `[!img-gen]` (or any `assets/img-gen/*.png` reference authored manually). The command walks every `.md` file under `--input`, finds image references that don't already carry a `{src-dark="…"}` attribute, calls the OpenAI image-**edit** endpoint to regenerate each in dark mode using the configured `imageGen.darkModePrompt`, saves the result as `<name>-dark.png` next to the original, and rewrites the Markdown attribute — preserving any other attributes already on the image. Idempotent: paired images and `<name>-dark.png` files themselves are skipped, and dark variants already present on disk are relinked into the Markdown without a second API call. Where possible the dark generation matches the PNG header's pixel dimensions of the source image so the pair lines up visually; otherwise it falls back to `imageGen.size`. Docs at `components/img-gen.md#backfilling-dark-variants-for-existing-images`.
* **Feature**: Expanded `[!img-gen]` with project-level defaults, a sensible landscape default size, and automatic dark-mode variants. A new `imageGen:` section in `neko.yml` exposes a global `systemPrompt` appended to every image prompt, a `size` default (now **`1536x1024` landscape** out of the box, instead of the model's square default), and `lightMode` / `darkMode` toggles (both on by default). When `lightMode` is on, a "render for a light theme" instruction is appended to every prompt; when `darkMode` is on, Neko follows the light generation with a second call to the OpenAI image-**edit** endpoint using the freshly generated light image plus a "redo this in dark mode" prompt, producing a paired `name-dark.png`. The rewritten Markdown now carries a `src-dark="…"` attribute that the image renderer expands into two `<img>` tags (`dark:hidden` / `hidden dark:inline-block`) so the active theme picks the right variant automatically. The `size` attribute on a directive now accepts every `TornadoImageSizes` value (`1024x1024`, `1536x1024`, `1024x1536`, `2048x2048` / `2048x1152`, `3840x2160` / `2160x3840`, `auto`, plus any explicit `<width>x<height>` going through `Custom`), and two new per-directive overrides — `light=` and `dark=` — let an individual image opt out of the light-mode hint or the dark-variant pass. Docs at `components/img-gen.md`, snippets refreshed in `templates.json`, and both the `img-gen` and `neko-yml` skills updated accordingly.
* **Feature**: Added a `searchExclude` configuration option for opting pages and folders out of the in-site search index. Set `searchExclude: true` in a page's frontmatter (or sibling `.yml`) to omit that single page from `search.json`, or set it in a folder's `index.yml` / `<foldername>.yml` to exclude every page under that folder recursively. Excluded pages are still built and reachable by direct link — they just do not appear in search results. Documentation lives at `configuration/page.md#searchexclude` and `configuration/folder.md#searchexclude`, and the page/folder skills (`frontmatter`, `folder-index`) have been updated accordingly.
* **Feature**: Added a new `neko update-skills` command that refreshes the Neko-managed skills under an existing project's `.claude/skills/` folder to match the versions bundled with the running CLI. Pass `--path` to point at a project (default: current directory) and `--dry-run` to preview the changes. The command requires a `.claude/` folder to exist (otherwise it errors out and suggests `neko new`), replaces every Neko-shipped skill folder in place, and leaves any custom (non-Neko) skills untouched — reporting both the count of skills replaced/added and the names of the custom skills preserved.
* **Feature**: Added a new `[!img-gen]` component and a matching `neko gen-images` command that uses the [LlmTornado](https://www.nuget.org/packages/LlmTornado) NuGet package to generate images from inline prompts via OpenAI. Authors describe an image inside a `[!img-gen ...]` block; running `neko gen-images --api-key sk-... [--image-model gpt-image-1] [--llm-model gpt-4o-mini]` walks every Markdown file under `--input`, asks the chat model for a slug and alt-text (strict JSON), generates the PNG with the image model, saves it into the page's `assets/img-gen/` folder, and rewrites the directive into a regular Markdown image with the original directive preserved as an HTML comment so it can be re-generated later. Only OpenAI is supported for now. The directive renders nothing in HTML until you run the command, and supports `size`, `quality`, `background`, and `style` attributes. Documentation lives at `components/img-gen.md`, with two `neko-img-gen*` snippets in `templates.json` and a new `img-gen` skill in the starter template.
* **Fix**: Pipe tables whose delimiter row used centered-alignment markers (`:---:`) were rendered as a paragraph instead of a table. The `EmojiParser` was greedily matching `:---:` as an emoji named `---`, consuming the alignment markers before Markdig's pipe-table parser could see them. Emoji names now require at least one alphanumeric character, so alignment markers are left intact. This restores the rendering of the **All Icons** table on the Icon component documentation page.
* **Fix**: Search results now display the page's first `# H1` heading when the page has no `title:` set in its frontmatter, falling back to the file name only if neither is present. Previously, untitled pages always showed the bare file name in the search modal.
* **Fix**: Search now indexes the **rendered** page content instead of the raw markdown source. Auto-injected blog and changelog listings, callouts, tabs, and other component bodies are searchable, while YAML frontmatter is no longer mixed into the indexed text. Password-protected pages are skipped entirely (previously their frontmatter password and body leaked into `search.json`). The client honours `--route-prefix` for both the `search.json` fetch and result links.
* **Feature**: Added a new `neko new` command that scaffolds a fresh hello-world documentation project (with `neko.yml`, three sample pages, and a `.claude/` folder of skills) into the current directory or a directory passed via `--path`. The starter lives under `.template/` in the repository and is zipped at build time and embedded as a resource in the CLI assembly. Pass `--force` to overwrite a non-empty target.
* **Feature**: Added a new `Roadmap` component that renders a kanban-style product roadmap board. Lanes (`:::: lane`) carry a title, count badge and accent colour; items (`::: roadmap-item`) carry a title, tag pill, optional date, vote count, and optional clickable link. Lane count badges reuse the icon-badge tint+ring style from grid cards, item tag pills follow the soft `bg-{color}-100 / text-{color}-800` palette from `[!badge]`, and lanes/items use `rounded-2xl` / `rounded-xl` with `hover:border-primary-*` to match Neko cards. Documentation lives at `components/roadmap.md`, with three `neko-roadmap*` snippets in `templates.json`.
* **Feature**: Added drag-and-drop reordering of sidebar items while running `neko watch`. Items can be reordered within their parent group; on drop, the corresponding `.md` frontmatter `order` (or folder `index.yml` `order`) values are rewritten as multiples of 10, and the site reloads automatically. A new `POST /api/neko/reorder` endpoint handles the update.
* **Theme**: Refreshed the default light / dark themes to match the look-and-feel of [docs.curiosity.ai](https://docs.curiosity.ai). Introduced a new `curiosity` default theme (deep navy `#050914` background in dark mode), a paired `accent` palette, and a `neko-text-gradient` utility used by the hero accent word.
* **Feature**: Split screenshot capture into a dedicated `neko snap` command. `neko build` and `neko watch` no longer call Playwright; instead, run `neko snap` to capture missing screenshots, or `neko snap --all` to re-capture everything.
* **Feature**: Added a new `[!lesson]` Markdown component that renders a curriculum-style track. Steps are auto-discovered from sibling `.md` files in the folder, ordered by their `order` frontmatter. User progress is persisted to `localStorage` per lesson.
* **Feature**: Pages inside a `[!lesson]` folder now render a dedicated `Go back: …` / `Next step: …` navigation block at the bottom, with chevron icons and links to the previous and next siblings in the same curriculum order as the parent lesson page. Detected automatically &mdash; no per-page configuration required.
* **Sample**: Added a `Learn Neko` sample track under `lesson/` showcasing the new component end-to-end.
* **Cards**: Redesigned the `grid` card variant to match the curiosity card style &mdash; rounded icon badge with palette-coloured tint, hover glow, no image required. Added a new `palette` attribute and a deterministic palette fallback.
* **Hero**: Redesigned the `[!hero]` component with an eyebrow label, gradient accent word (`title-accent`), and subtle radial glow accents. Default alignment is now `left` to match the reference.
* **Breaking**: Removed all gradient card options (`gradient`, `gradient-mode`, `gradient-colors`, `gradient-noise`, `gradient-speed`) and the bundled `makegradient.js` asset. Use the new icon-badge style instead.
* **Breaking**: Removed the `--disable-snapframe` build/watch flag &mdash; capturing is now opt-in via `neko snap`.

## v26.3.16

* **Feature**: Added a new component for rendering PDF files inline in the text. You can now use the standard markdown image syntax pointing to a `.pdf` file to automatically render it in an iframe using pdf.js.

## v26.3.12

* **Feature**: Added support for configuring a global password in `neko.yml`. You can now protect the entire documentation by defining `password: "my-secret"` in your global configuration. Individual pages can bypass this global protection by setting `password: none` in their frontmatter.
* **Documentation**: Updated the Tesserae component documentation (`components/tesserae.md`) to include a full interactive TODO sample application that demonstrates building UI components and persisting state via `window.localStorage`.

## v26.3.11

* **Feature**: Added `csharp-docs` code block language mode which leverages Roslyn to parse C# code blocks containing XML comments and beautifully renders them with DocFx-like layouts detailing the summary, parameters, remarks, return types, and exceptions.
* **Feature**: Added a `sitemap` boolean configuration option in `neko.yml` to automatically generate a `sitemap.xml` file containing all generated HTML pages, utilizing the configured `url` as the base address.
* **Improvement**: Made the sidebar search box sticky when scrolling the sidebar, allowing quick access to the filter functionality. This was achieved by updating the HTML generation in `Neko/Builder/HtmlGenerator.cs` to wrap the search input in a sticky container while maintaining the proper layout for the rest of the navigation list.
* **Feature**: Added Monaco Editor support for auto-completing templates for all valid components of neko starting with the "neko-" prefix. The template list is loaded dynamically from `templates.json` on the first render of the editor.
* **Documentation**: Added a new guide for the Live Editing feature in Watch mode, including details on auto-completing templates.


## v26.3.3

**Snapframe Component**

- Added a new `[!snapframe]` Markdown extension that automatically generates screenshots of external websites using the SnapFrame .NET tool during the build process.
- Extended the `[!snapframe]` Markdown extension to support multi-line command execution, allowing interaction with the page before taking the screenshot.
- Merged the image alignment documentation into the main `image.md` file.

## v26.3

**Icon Search in Watch Mode Editor**

- Added a searchable list of icons in the watch mode editor modal.
- Accessible via the `Ctrl+I` or `Cmd+I` keyboard shortcut.
- Allows inserting the selected icon's name directly into the editor at the current cursor position.

**Workflow LeaderLines Clipping Improvements**

- Modified the workflow component javascript to appropriately clip connection lines within the workflow container.

**Mermaid Diagram Zoom Controls**

- Added built-in zoom controls to Mermaid diagrams.
- Hover over any Mermaid diagram to access Zoom In, Zoom Out, and Reset buttons.
- Applied a minimum height of 400px to all Mermaid diagrams to give ample space for interacting with diagrams.

**Initial Release of Neko v26.3**

We are excited to announce the initial release of Neko, a powerful static site generator designed to help you create beautiful, documentation-first websites with ease.

### Key Features
- **Markdown First**: Write your documentation in standard Markdown. Neko handles the rest.
- **Rich Components**: Enhance your docs with built-in components like Alerts, Badges, Tabs, and more.
- **Theming**: Dynamic Tailwind themes configurable via `neko.yml` under the `theme` key. Users can specify a built-in palette (e.g., `name: violet`) and override specific shades.
- **Blog Support**: Neko supports a 'Blog Mode' where files in a `blog/` directory are processed as posts, sorted by date (descending), and displayed in a responsive grid layout of cards.
- **Changelog Support**: Neko supports a 'Changelog Mode' where files in a `changelog/` directory are processed as entries, sorted by date (descending), and displayed in a vertical timeline layout.
- **Watch Mode**: The CLI supports a `watch` command that serves the site on localhost and auto-reloads on file changes, including a built-in Monaco editor for quick edits.
- **Multi-Repo Mode**: Simultaneously build, watch, and serve multiple sub-projects located in immediate subdirectories containing a `neko.yml` file.
- **Tesserae Support**: Write and compile Tesserae C# code blocks directly in your Markdown, generating live interactive components.

### Other Important Features
- **Markdown Custom Containers**: Support for custom Markdown syntax like Icons, Badges, Alerts, Tabs, Columns, Steps, Generic Components, Code Snippets, Panels, Emojis, and Cards.
- **Navigation History**: Tracks the last visited pages in browser `localStorage`, with a flyover popup UI.
- **Built-in Search**: Full-text client-side search across your documentation using Minisearch.
- **Dynamic Card Backgrounds**: `makegradient.js` integration for beautiful, dynamic card backgrounds.
- **Mathematical Formulas & Diagrams**: Integrated support for KaTeX math formulas and Mermaid diagrams.
