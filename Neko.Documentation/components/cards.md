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

## Icon Card

The `grid` variant doubles as an icon card. Pass an `icon` to render a colored icon badge in the top-left of the card &mdash; no image required.

:::: card-grid
::: card {variant="grid" icon="route" title="Graph Query Language" palette="blue" tags="24 lessons, 4h, All levels"}
Traversals, projections, joins. Think in graphs.
:::
::: card {variant="grid" icon="star" title="NLP — Patterns & Spotters" palette="violet" tags="18 lessons, 3h, Intermediate"}
Build entity capture pipelines that scale.
:::
::: card {variant="grid" icon="plug" title="Building data connectors" palette="emerald" tags="12 lessons, 2h, Beginner"}
Ingest anything into the graph with the SDK.
:::
::::

```markdown
:::: card-grid
::: card {variant="grid" icon="route" title="Graph Query Language" palette="blue" tags="24 lessons, 4h, All levels"}
Traversals, projections, joins. Think in graphs.
:::
::::
```

Available `palette` values: `blue`, `violet`, `emerald`, `amber`, `rose`, `sky`, `fuchsia`, `orange`. When omitted, Neko picks a palette deterministically from the card's title.

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
| `icon` | Icon shown in the colored badge of the `grid` variant. |
| `palette` | Color palette for the `grid` variant icon badge. One of `blue`, `violet`, `emerald`, `amber`, `rose`, `sky`, `fuchsia`, `orange`. |
