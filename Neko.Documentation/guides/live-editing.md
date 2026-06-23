---
title: Live Editing
description: Edit your documentation directly from the browser in watch mode.
icon: edit
---

# Live Editing

Neko provides a seamless live editing experience when running your site in `watch` mode. This allows you to edit content directly from your browser without needing to switch context between an editor and the documentation preview.

## Getting Started

To enable live editing, simply run the Neko CLI with the `watch` command:

```bash
neko watch --input docs/
```

When you navigate to your locally hosted site, Neko injects an embedded Monaco Editor instance into the page.

## Editing Pages and Folders

There are two primary ways to access the editor:

1.  **Global Editor Shortcut:** You can press `Ctrl + I` (or `Cmd + I` on macOS) anywhere on a page to open the Monaco Editor with the markdown content of the current page.
2.  **Sidebar Pencil Icon:** When hovering over folder items in the sidebar, a pencil icon will appear. Clicking this icon opens the editor specifically targeted at the folder's configuration file (`index.yml`), allowing you to quickly modify metadata such as `order`, `title`, or `icon`.

![Pencil Icon](/assets/editor-sidebar.png){max-width=400px}

## Component Auto-Complete

To help you remember and quickly insert Neko's extensive custom components, the built-in editor supports auto-complete snippets.

While editing a markdown file, type the prefix `neko-` to trigger a dropdown containing templates for all supported custom syntax blocks, such as tabs, alerts, callouts, cards, and more.

![Auto-complete Menu](/assets/editor-autocomplete.png)

This ensures you can always access the right snippet structure without leaving the browser or consulting external documentation.

## Live preview only (disable the editor)

Sometimes you want the watch server's live-reload on `localhost` without any of
the editing chrome — for example, to preview exactly what a release build ships,
or to demo the site without exposing the edit and reorder controls. Pass
`--live` (alias `--no-editor`) to `neko watch`:

```bash
neko watch --input docs/ --live
```

In this mode Neko still rebuilds and refreshes the browser on every file change,
but it omits the in-browser editor entirely: the header edit button, the sidebar
pencil icons, the drag-to-reorder handles, and the Monaco editor modal are all
left out, just as they are in a `neko build` output.
