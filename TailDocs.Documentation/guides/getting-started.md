---
order: 2000
icon: rocket
tags: [guide]
---
# Getting Started

***
This guide will have you up and running generating your own TailDocs website in just a few minutes.
***

## What is TailDocs?

TailDocs is a website generator that turns your Markdown `.md` files into a beautiful and functional documentation website. No coding required. Just write in [Markdown](/guides/formatting.md) and TailDocs handles the rest.

TailDocs is perfect for:
- [x] Project documentation
- [x] Knowledge bases
- [x] API docs
- [x] Personal blogs or notes
- [x] Team wikis
- [x] Self-publishing content

---

## Step 1: Installation

The first thing to complete is [installing](installation.md) TailDocs.

Once you have TailDocs installed, you can verify using the following command to output the TailDocs version number:

```
taildocs --version
```

If the above is not working, then TailDocs is not installed.

---

## Step 2: Start TailDocs

Still using the command line, navigate to any folder with Markdown files:

```bash
cd your-project-folder
```

If you do not have Markdown files in that directory, create a new file such as the following `readme.md`:

:::code source="../samples/_includes/basic-page.md" :::

Then run the command `taildocs start`:

```bash
taildocs start
```

That's it! TailDocs will automatically:

1. Find your Markdown files
1. Build your website
1. Open it in your browser
1. Watch for changes and reload the browser automatically

---

## Next Steps: Add more content

TailDocs supports standard [Markdown](formatting.md) plus powerful extensions. Experiment with adding the following markdown to your page.

### Basic Markdown

```md
{{ include "snippets/markdown-sample" }}
```

### Components and Settings

Now with a basic introduction to Markdown options, explore the following TailDocs features:

[[Components]]
: Rich content blocks like [[table]]s [[callout]]s, [[tab]]s, and [much more](/components/components.md).

[Project](/configuration/project.md) settings
: Project level configuration for your website

[Page](https://example.com/guides/deployment/) settings
: Page level configuration options

## Hosting

To generate the static website files, run the following command:

```bash
taildocs build
```

By default, the files will be copied to a new `.taildocs` folder within your project, although this is configurable with the [`output`](/configuration/project.md#output) setting.

The build should only take a few moments to complete. If you have your own web server, you can FTP or copy the files from the `.taildocs` directory to the web server.

You can also host your new website using [[GitHub Pages]], [[Cloudflare]], [[GitLab Pages]], [[Docker]], [[Netlify]], or absolutely any other web hosting service.

{{ include "snippets/support" }}
