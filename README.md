# Neko

[![NuGet](https://img.shields.io/nuget/v/Neko.svg)](https://www.nuget.org/packages/Neko)

Neko is a powerful, elegant static site generator designed to help you create beautiful, documentation-first websites with incredible ease. Built with **.NET 10** and leveraging the full flexibility of **Markdown** paired with the modern styling of **Tailwind CSS**, Neko effortlessly transforms your content into a modern, highly responsive, and feature-rich documentation site.

Whether you're writing simple project READMEs or complex multi-repository enterprise documentation, Neko makes it look professional right out of the box! ✨

## 🌟 Key Features

- 📝 **Markdown First**: Write your documentation in standard Markdown. Neko intuitively handles the rest.
- 🧩 **Rich Components**: Enhance your docs with powerful built-in components like Callout Alerts, Badges, Tabs, Steps, Cards, and more!
- 🎨 **Syntax Highlighting**: Beautiful, automatically-styled code blocks with syntax highlighting and one-click copy functionality.
- 📊 **Diagrams & Math**: Integrated, out-of-the-box support for Mermaid diagrams and KaTeX math formulas.
- 📱 **Responsive Design**: A perfect mobile-friendly layout powered by Tailwind CSS that looks great on any screen size.
- 🌓 **Dark Mode**: Seamless built-in support for fluid light and dark themes.
- ⚡ **Fast & Lightweight**: Generates purely static HTML files that are lightning fast and can be hosted anywhere.

## 📚 Resources

- 🌐 **Official Documentation**: [neko.curiosity.ai](https://neko.curiosity.ai/)
- 💻 **GitHub Repository**: [github.com/curiosity-ai/neko](https://github.com/curiosity-ai/neko/)

## 🚀 Installation

Install Neko as a global .NET tool:

```bash
dotnet tool install -g Neko
```

## 🛠️ Usage

Build your site:

```bash
neko build
```

Watch for changes and serve locally:

```bash
neko watch
```

## 📂 Multi-Repo Mode

Neko automatically supports building and watching multiple documentation projects from a single repository. If Neko detects multiple `neko.yml` files in subdirectories, it automatically enables multi-repo mode.

In this mode:
- Each nested `neko.yml` creates a dedicated route (e.g., `./api-docs/neko.yml` gets mapped to `localhost:5000/api-docs/`).
- The root `index.md` and `neko.yml` are also respected and serve as your central landing page.
- Sub-projects will automatically inherit essential configuration settings (such as the Theme, Branding, and Snippets configuration) from parent `neko.yml` files, letting you maintain consistency effortlessly while still allowing granular overrides where needed.
