---
title: Cards
description: Display content in flexible card containers with image, title, description, and actions.
icon: layout-fluid
---

# Cards

Cards are versatile containers used to group related information. Neko provides two card variants: Stacked and Horizontal.

## Stacked Card

The stacked card displays an image at the top, followed by the content. This is the default variant.

::: card {image="https://v1.tailwindcss.com/img/card-top.jpg" title="The Coldest Sunset" tags="photography, travel, winter"}
Lorem ipsum dolor sit amet, consectetur adipisicing elit. Voluptatibus quia, nulla! Maiores et perferendis eaque, exercitationem praesentium nihil.
:::

```markdown
::: card {image="https://v1.tailwindcss.com/img/card-top.jpg" title="The Coldest Sunset" tags="photography, travel, winter"}
Lorem ipsum dolor sit amet, consectetur adipisicing elit. Voluptatibus quia, nulla! Maiores et perferendis eaque, exercitationem praesentium nihil.
:::
```

## Horizontal Card

The horizontal card displays an image on the left (on desktop) and content on the right.

::: card {variant="horizontal" image="https://v1.tailwindcss.com/img/card-left.jpg" title="Can coffee make you a better developer?" see-more="#" link="#"}
Lorem ipsum dolor sit amet, consectetur adipisicing elit. Voluptatibus quia, nulla! Maiores et perferendis eaque, exercitationem praesentium nihil.
:::

```markdown
::: card {variant="horizontal" image="https://v1.tailwindcss.com/img/card-left.jpg" title="Can coffee make you a better developer?" see-more="#" link="#"}
Lorem ipsum dolor sit amet, consectetur adipisicing elit. Voluptatibus quia, nulla! Maiores et perferendis eaque, exercitationem praesentium nihil.
:::
```

## Grid Layout

Cards can be arranged in a responsive grid using the `card-grid` container. The `grid` variant is optimized for this layout, featuring a clean design with a centered image area.

:::: card-grid
::: card {variant="grid" image="../assets/neko-logo.png" title="Breadcrumb" link="#"}
The Breadcrumb component is used to display a navigational trail, indicating the user's current page within a hierarchical structure.
:::
::: card {variant="grid" image="../assets/neko-logo.png" title="Details List" link="#"}
DetailsList is a robust component for displaying an information-rich collection of items. It extends the basic List capabilities.
:::
::: card {variant="grid" image="../assets/neko-logo.png" title="Grid" link="#"}
The Grid component provides a flexible layout container that arranges its child components into a grid structure.
:::
::::

```markdown
:::: card-grid
::: card {variant="grid" image="../assets/neko-logo.png" title="Breadcrumb" link="#"}
The Breadcrumb component is used to display a navigational trail, indicating the user's current page within a hierarchical structure.
:::
::: card {variant="grid" image="../assets/neko-logo.png" title="Details List" link="#"}
DetailsList is a robust component for displaying an information-rich collection of items. It extends the basic List capabilities.
:::
::: card {variant="grid" image="../assets/neko-logo.png" title="Grid" link="#"}
The Grid component provides a flexible layout container that arranges its child components into a grid structure.
:::
::::
```

## Gradient Card

You can replace the standard image of a card with an animated gradient. This is configured using `gradient="true"` or passing a `gradient-mode` parameter. When no colors are provided, Neko will automatically pick colors from your chosen theme to generate the gradient.

::: card {title="Impasto Gradient" gradient="true" gradient-mode="impasto"}
A rich, textured oil-paint style gradient background using theme colors automatically.
:::

```markdown
::: card {title="Impasto Gradient" gradient="true" gradient-mode="impasto"}
A rich, textured oil-paint style gradient background using theme colors automatically.
:::
```

### Custom Gradient Settings

You can pass an array of custom hex colors via `gradient-colors`, adjust the `gradient-noise`, and change the `gradient-speed`. Valid modes include `mesh`, `aurora`, `grainy`, `deep-sea`, `holographic`, `impasto`, `spectral`, and `fractal`.

::: card {variant="horizontal" title="Holographic Vibe" gradient-mode="holographic" gradient-colors='["#dee3ff","#d0c5ff","#ffe6f6","#dca512"]' gradient-speed="2"}
An energetic, faster, holographic gradient card utilizing custom defined colors instead of the theme default.
:::

```markdown
::: card {variant="horizontal" title="Holographic Vibe" gradient-mode="holographic" gradient-colors='["#dee3ff","#d0c5ff","#ffe6f6","#dca512"]' gradient-speed="2"}
An energetic, faster, holographic gradient card utilizing custom defined colors instead of the theme default.
:::
```

## Link Card

The link card variant is designed for navigation and reference links. It features a clean, text-centric layout with an optional call-to-action link at the bottom.

:::: card-grid
::: card {variant="link" title="View APIs" link="#" link-text="View docs"}
Browse the API docs, read about endpoints response types, and more.
:::
::: card {variant="link" title="View clients" link="#" link-text="View docs"}
Browse the the existing client libraries for Java, .Net, Python, and more.
:::
::: card {variant="link" theme="dark" title="Release notes" link="#" link-text="View release notes" arrow="true"}
Explore the latest features and changes in Elastic.
:::
::: card {variant="link" theme="dark" title="Extend and contribute" link="#" link-text="View extend and contribute docs" arrow="true"}
Learn how to contribute to products and extend capabilities.
:::
::::

```markdown
:::: card-grid
::: card {variant="link" title="View APIs" link="#" link-text="View docs"}
Browse the API docs, read about endpoints response types, and more.
:::
::: card {variant="link" title="View clients" link="#" link-text="View docs"}
Browse the the existing client libraries for Java, .Net, Python, and more.
:::
::: card {variant="link" theme="dark" title="Release notes" link="#" link-text="View release notes" arrow="true"}
Explore the latest features and changes in Elastic.
:::
::: card {variant="link" theme="dark" title="Extend and contribute" link="#" link-text="View extend and contribute docs" arrow="true"}
Learn how to contribute to products and extend capabilities.
:::
::::
```

## Properties

| Property | Description |
| :--- | :--- |
| `image` | URL of the image to display. |
| `title` | The title of the card. |
| `description` | The main content of the card (passed as the block body). |
| `link` | URL to link the image and title to. |
| `see-more` | URL for a secondary "See more" link at the bottom. |
| `tags` | Comma-separated list of tags to display at the bottom. |
| `variant` | Layout variant: `stacked` (default), `horizontal`, `grid`, or `link`. |
| `link-text` | The text for the bottom link action (used with `variant="link"`). |
| `theme` | Set to `dark` for a dark background variant (used with `variant="link"`). |
| `arrow` | Set to `true` to display an arrow icon next to the link (used with `variant="link"`). |
| `gradient` | Set to `true` to replace the top image with an animated gradient based on your theme. |
| `gradient-mode` | Type of gradient: `mesh`, `aurora`, `grainy`, `deep-sea`, `holographic`, `impasto`, `spectral`, or `fractal`. |
| `gradient-colors` | Custom JSON string array of hex codes for the gradient (e.g., `["#dee3ff", "#dca512"]`). |
| `gradient-noise` | Control the level of noise over the gradient. Decimal values (e.g., `0.05`). |
| `gradient-speed` | Speed multiplier for the gradient animation. (e.g., `1`, `1.5`, `0.5`). |
