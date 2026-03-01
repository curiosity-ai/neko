# AGENTS.md

## Overview
This repository contains the source code for `Neko`, a CLI tool for generating static documentation sites from Markdown files. The tool is designed to be compatible with existing Markdown syntax used by other popular documentation tools, specifically supporting:
- Frontmatter configuration (YAML).
- Custom components like Badges, Callouts/Alerts, Tabs, etc.
- A specific configuration file format (`neko.yml` - for compatibility).

## Directives
1.  **Code Style**: Use modern C# 10 features. Follow standard .NET naming conventions.
2.  **Testing**:
    -   Unit tests in `Neko.Tests`.
    -   End-to-end tests using Playwright.
    -   **Documentation pages should always be checked with Playwright as instructed.**
    -   Run tests frequently.
3.  **Documentation**:
    -   Update `Neko.Documentation` with examples of supported features.
    -   **All components and features of Neko should have an equivalent documentation page in the documentation project.**
    -   Maintain `TODO.md` and `WIP.md`.
4.  **Assets**:
    -   Use embedded resources for CSS, JS, and Fonts.
    -   Do not use Node.js or NPM at runtime.
    -   Use vanilla JavaScript. No frameworks like React/Vue/Angular.
    -   **Icons**: Use Flaticon UIcons (Regular Rounded) as the official icon set. Octicons are no longer supported.
5.  **Architecture**:
    -   The CLI should support a `watch` command that serves the site on localhost and auto-reloads on file changes.
    -   Use `System.CommandLine` for argument parsing.
    -   Use `Markdig` for Markdown parsing.
    -   Use `Kestrel` for the development server.

## Resources
-   External libraries are tracked in `reference/` folder.
-   License information for assets should be in `Neko/Resources/sources.md`.

## Testing
- ALWAYS use the Neko project targeting an output folder in the temp directory.