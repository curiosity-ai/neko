---
label: Includes
order: 10
icon: file-code
tags: [config]
toc:
  depth: 2-3
---
# Includes

Neko supports injecting custom HTML content into your generated pages using the `_includes` directory.

## Head Includes

You can inject custom HTML into the `<head>` section of every generated page by creating a file named `head.html` inside an `_includes` directory in your project root.

This is useful for adding:
- Custom CSS styles
- JavaScript libraries (e.g., analytics scripts)
- Custom meta tags
- Fonts

### Usage

1. Create a directory named `_includes` in your project root (where your `neko.yml` is located).
2. Create a file named `head.html` inside `_includes`.
3. Add your HTML content to `head.html`.

### Example

**Structure:**

```text
my-project/
├── _includes/
│   └── head.html
├── index.md
└── neko.yml
```

**_includes/head.html:**

```html
<!-- Add a custom font -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap" rel="stylesheet">

<!-- Add a custom script -->
<script>
  console.log("Hello from the head include!");
</script>

<!-- Add custom styles -->
<style>
  body {
    font-family: 'Roboto', sans-serif;
  }
</style>
```

Neko will automatically detect this file and insert its contents into the `<head>` section of every page, just before the closing `</head>` tag.
