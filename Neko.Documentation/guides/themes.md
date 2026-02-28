---
order: 6
icon: paint-brush
nav:
  badge: NEW|info
tags: [guide]
---
# Themes

Neko's [`theme`](/configuration/project.md#theme) system allows you to customize the visual appearance of your website easily by configuring primary colors used by components such as buttons, badges, navigation links, and more. With themes, you can adjust the look and feel to match your brand or project preferences for both light and dark modes.

## Quick Start

The quickest way to customize your site's appearance is by adding a `theme` configuration to your `neko.yml` file and setting a predefined `name`.

```yml
theme:
  name: violet
```

## Built-in Themes

Neko comes with several predefined color palettes based on Tailwind CSS colors. You can select any of the following themes by specifying its `name`:

- `blue` (Default)
- `violet`
- `emerald`
- `rose`
- `amber`
- `sky`
- `fuchsia`

## Custom Color Palettes

If you want fine-grained control or need to match a specific brand color, you can override the individual color shades used by the theme. Neko uses a 50 to 950 scale (like Tailwind CSS) for its primary colors.

To define a custom palette, use the `colors` dictionary under `theme`:

```yml
theme:
  colors:
    "50": "#f0fdfa"
    "100": "#ccfbf1"
    "200": "#99f6e4"
    "300": "#5eead4"
    "400": "#2dd4bf"
    "500": "#14b8a6"
    "600": "#0d9488"
    "700": "#0f766e"
    "800": "#115e59"
    "900": "#134e4a"
    "950": "#042f2e"
```

!!!note
You can partially override colors. If you specify a `name` like `blue` but also provide a `colors` dictionary, your custom shades will overwrite only the specific keys you provide, while the rest of the shades will fall back to the selected base theme.
!!!

## Dark Mode

Neko handles dark mode automatically. It uses the darker shades of your configured primary color (typically 400, 300, etc.) for interactive elements and highlights when dark mode is active. This ensures that your site remains accessible and visually appealing regardless of the user's system preferences.

## Syntax Highlighting

You can also customize the code block syntax highlighting themes for light and dark modes via the `highlight` settings:

```yml
theme:
  highlight:
    light: tokyo-night-light
    dark: tokyo-night-dark
```

## Troubleshooting

### Theme Not Applied

- [x] Check that the syntax is correct in your `neko.yml` file.
- [x] Ensure your `colors` keys are correctly quoted as strings (e.g., `"50"` rather than `50`).

### Colors Look Wrong in Dark Mode

- [x] If using custom colors, ensure your palette has appropriate contrast. Tailwind CSS color generators (like UI Colors) are great tools to generate a well-balanced 50-950 scale.
