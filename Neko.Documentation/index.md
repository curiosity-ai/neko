---
order: 1
title: Introduction
icon: home
---

# Welcome to Neko: The Modern .NET Static Site Generator

![Neko Logo](assets/neko-logo.png){max-height="400px"}

Neko is a modern, high-performance static site generator built on .NET 10. Designed for developers who value simplicity and speed, Neko effortlessly transforms your Markdown files into a stunning, responsive documentation site using the power of Tailwind CSS. Whether you're documenting an API, a library, or a full-scale product, Neko provides the tools you need to create a professional experience.

*Markdown flows to web,*
*Simple code, stunning design,*
*Neko builds it fast.*

## Key Features

- **Markdown First**: Write your documentation in standard Markdown. Neko handles the rest.
- **Rich Components**: Enhance your docs with built-in components like [Alerts](components/alert), [Badges](components/badge), [Tabs](components/tab), and more.
- **Syntax Highlighting**: Beautiful code blocks with syntax highlighting and copy functionality.
- **Diagrams & Math**: Integrated support for [Mermaid diagrams](components/mermaid) and [KaTeX math](components/math) formulas.
- **Responsive Design**: Mobile-friendly layout powered by Tailwind CSS.
- **Dark Mode**: Built-in support for light and dark themes.
- **Fast & Lightweight**: Generates static HTML files that can be hosted anywhere.

## Getting Started

To get started with Neko, simply create a `neko.yml` configuration file and structure your Markdown files.

### Directory Structure

```plaintext
my-docs/
├── neko.yml
├── index.md
└── components/
    ├── alert.md
    └── button.md
```

### Configuration

The `neko.yml` file allows you to customize your site's branding and navigation.

```yaml
branding:
  title: My Documentation

links:
  - text: Home
    link: /
  - text: Components
    items:
      - text: Alert
        link: components/alert
```

## Contributing

Neko is open-source. We welcome contributions to help make it even better.

---
*Generated with Neko*
