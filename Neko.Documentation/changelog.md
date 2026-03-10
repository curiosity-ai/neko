---
order: 2
icon: memo
layout: changelog
---
# Changelog

Neko is currently under active development.

Please note that Neko uses a calendar versioning approach.

## v26.3.3

**Snapframe Component**

- Added a new `[!snapframe]` Markdown extension that automatically generates screenshots of external websites using the SnapFrame .NET tool during the build process.
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
