---
order: 1
icon: house-chimney
---
# Hello, Neko!

Welcome to your blank [Neko](https://neko.curiosity.ai) documentation starter. This page is the homepage of your site, generated from `index.md` at the project root.

!!! tip Where to go next
- Edit `neko.yml` to set the title, branding, navigation, and theme.
- Edit this `index.md` to write your landing page.
- Open [Getting Started](getting-started.md) or [About](about.md) for the other two sample pages.
!!!

## What is this?

This folder is a minimal Neko documentation project. It contains:

- `neko.yml` — project configuration.
- `index.md` — the homepage (this page).
- `getting-started.md`, `about.md` — two sample pages.
- `.claude/` — instructions and skills for Claude on how to author Neko docs.

## Run the site

Install Neko once as a global .NET tool:

[!command-example install="dotnet tool install -g Neko" quickstart="neko start"]

Then from this folder run `neko start` to launch the live-reload dev server, or `neko build` to generate the static site to the `.neko` output folder.

## Built-in components

Neko ships with rich components. Here are a few examples to confirm the renderer is working:

+++ Alert
!!! info
This is an alert / callout.
!!!
+++ Badge
[!badge variant="success" text="ready"]
+++ Steps
>>> Install Neko
`dotnet tool install -g Neko`
>>> Run start
`neko start`
>>> Edit content
Open `index.md` and start writing.
>>>
+++

Explore every component and option in the `.claude/skills` folder and in the main [Neko documentation](https://neko.curiosity.ai).
