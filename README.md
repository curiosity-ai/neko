# Neko

Neko is a powerful static site generator designed to help you create beautiful, documentation-first websites with ease. Built with .NET 10 and leveraging the flexibility of Markdown and Tailwind CSS, Neko transforms your content into a modern, responsive, and feature-rich documentation site.

GitHub: [https://github.com/curiosity-ai/neko/](https://github.com/curiosity-ai/neko/)
Docs: [https://neko.curiosity.ai/](https://neko.curiosity.ai/)

## Key Features

- **Markdown First**: Write your documentation in standard Markdown. Neko handles the rest.
- **Rich Components**: Enhance your docs with built-in components like Alerts, Badges, Tabs, and more.
- **Syntax Highlighting**: Beautiful code blocks with syntax highlighting and copy functionality.
- **Diagrams & Math**: Integrated support for Mermaid diagrams and KaTeX math formulas.
- **Responsive Design**: Mobile-friendly layout powered by Tailwind CSS.
- **Dark Mode**: Built-in support for light and dark themes.
- **Fast & Lightweight**: Generates static HTML files that can be hosted anywhere.

## Installation

Install Neko as a global .NET tool:

```bash
dotnet tool install -g Neko
```

## Usage

Build your site:

```bash
neko build
```

Watch for changes and serve locally:

```bash
neko watch
```

## Multi-Repo Mode

Neko automatically supports building and watching multiple documentation projects from a single repository. If Neko detects multiple `neko.yml` files in subdirectories, it automatically enables multi-repo mode.

In this mode:
- Each nested `neko.yml` creates a dedicated route (e.g., `./api-docs/neko.yml` gets mapped to `localhost:5000/api-docs/`).
- The root `index.md` and `neko.yml` are also respected and serve as your central landing page.
- Sub-projects will automatically inherit essential configuration settings (such as the Theme, Branding, and Snippets configuration) from parent `neko.yml` files, letting you maintain consistency effortlessly while still allowing granular overrides where needed.
