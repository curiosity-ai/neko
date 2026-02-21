---
title: Introduction
---

# Welcome to TailDocs

TailDocs is a powerful static site generator designed to help you create beautiful, documentation-first websites with ease. Built with .NET 10 and leveraging the flexibility of Markdown and Tailwind CSS, TailDocs transforms your content into a modern, responsive, and feature-rich documentation site.

## Key Features

- **Markdown First**: Write your documentation in standard Markdown. TailDocs handles the rest.
- **Rich Components**: Enhance your docs with built-in components like [Alerts](components/alert), [Badges](components/badge), [Tabs](components/tab), and more.
- **Syntax Highlighting**: Beautiful code blocks with syntax highlighting and copy functionality.
- **Diagrams & Math**: Integrated support for [Mermaid diagrams](components/mermaid) and [KaTeX math](components/math) formulas.
- **Responsive Design**: Mobile-friendly layout powered by Tailwind CSS.
- **Dark Mode**: Built-in support for light and dark themes.
- **Fast & Lightweight**: Generates static HTML files that can be hosted anywhere.

## Getting Started

To get started with TailDocs, simply create a `taildocs.yml` configuration file and structure your Markdown files.

### Directory Structure

```plaintext
my-docs/
├── taildocs.yml
├── index.md
└── components/
    ├── alert.md
    └── button.md
```

### Configuration

The `taildocs.yml` file allows you to customize your site's branding and navigation.

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

TailDocs is open-source. We welcome contributions to help make it even better.

---
*Generated with TailDocs*
