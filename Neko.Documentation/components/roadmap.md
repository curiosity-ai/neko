---
title: Roadmap
description: Showcase product progress as a multi-column kanban-style roadmap.
icon: route
tags: [component]
---

# Roadmap

The Roadmap component renders a kanban-style board with multiple lanes (e.g. _Under Consideration_, _Planned_, _In Progress_, _Public Beta_). Each lane contains one or more items, with an optional tag, date, and vote count &mdash; ideal for surfacing a public product roadmap directly from your documentation.

!!!
The roadmap uses three levels of nesting (roadmap &rarr; lane &rarr; item). As with any nested custom container, the outermost wrapper must have **more colons** than the next level. The examples below use five (`:::::`) for `roadmap`, four (`::::`) for `lane`, and three (`:::`) for `roadmap-item`.
!!!

## Basic example

::::: roadmap
:::: lane {title="Under Consideration" count="2" accent="gray"}
::: roadmap-item {title="User permission settings" tag="Feature" tag-color="emerald" votes="12"}
:::
::: roadmap-item {title="Add more filter and sorting options" tag="Feature" tag-color="emerald" votes="5"}
:::
::::
:::: lane {title="Planned" count="3" accent="teal"}
::: roadmap-item {title="Private Categories" tag="Feature" tag-color="emerald" votes="3"}
:::
::: roadmap-item {title="Trash on dashboard" tag="Issues" tag-color="rose" votes="3"}
:::
::: roadmap-item {title="Option to change text color on the top area" tag="User Interface" tag-color="blue" votes="1"}
:::
::::
:::: lane {title="In Progress" count="6" accent="amber"}
::: roadmap-item {title="[WordPress Plugin] Option to hide the widget from logged-in users" tag="Integrations" tag-color="amber" date="Sep, 21" votes="17"}
:::
::: roadmap-item {title="Integromat as alternative to Zapier" tag="Integrations" tag-color="amber" date="Sep, 21" votes="11"}
:::
::: roadmap-item {title="Add screenshot when the user sends feedback" tag="Feature" tag-color="emerald" votes="9"}
:::
::: roadmap-item {title="Add an option to remove the email form and force anonymous" tag="Feature" tag-color="emerald" votes="8"}
:::
::: roadmap-item {title="[WordPress Plugin] Option to hide the widget for non-logged users" tag="Integrations" tag-color="amber" date="Sep, 21" votes="3"}
:::
::: roadmap-item {title="Safe-Area Padding in fullscreen (PWA) mode" tag="Feature" tag-color="emerald" votes="2"}
:::
::::
:::: lane {title="Public Beta" count="4" accent="sky"}
::: roadmap-item {title="Zapier Integration" tag="Integrations" tag-color="amber" date="Sep, 21" votes="141"}
:::
::: roadmap-item {title="Customisable email domain" tag="Feature" tag-color="emerald" votes="34"}
:::
::: roadmap-item {title="Add a contact us form option" tag="Feature" tag-color="emerald" votes="6"}
:::
::: roadmap-item {title="Sign-on with company email" tag="Feature" tag-color="emerald" date="Aug, 21" votes="1"}
:::
::::
:::::

```markdown
::::: roadmap
:::: lane {title="Under Consideration" count="2" accent="gray"}
::: roadmap-item {title="User permission settings" tag="Feature" tag-color="emerald" votes="12"}
:::
::: roadmap-item {title="Add more filter and sorting options" tag="Feature" tag-color="emerald" votes="5"}
:::
::::
:::: lane {title="Planned" count="3" accent="teal"}
::: roadmap-item {title="Private Categories" tag="Feature" tag-color="emerald" votes="3"}
:::
::::
:::::
```

## With descriptions and links

Each roadmap item can carry a short description as block content, plus a `link` attribute to make the entire card clickable.

::::: roadmap
:::: lane {title="Planned" count="2" accent="teal"}
::: roadmap-item {title="Dark theme support" tag="Feature" tag-color="emerald" votes="42" link="#"}
A polished dark mode that follows the system preference and remembers the user choice.
:::
::: roadmap-item {title="API rate limiting" tag="Improvement" tag-color="sky" votes="18" link="#"}
Per-key quotas with friendly error messages and a status header surfacing the remaining budget.
:::
::::
:::: lane {title="In Progress" count="1" accent="amber"}
::: roadmap-item {title="Bulk export" tag="Feature" tag-color="emerald" date="May, 26" votes="27" link="#"}
Stream large exports as multipart zip archives so the UI no longer has to load the whole dataset.
:::
::::
:::::

```markdown
::: roadmap-item {title="Dark theme support" tag="Feature" tag-color="emerald" votes="42" link="#"}
A polished dark mode that follows the system preference and remembers the user choice.
:::
```

## Lane accents

The `accent` attribute on a `lane` sets the colour of the top border and the count badge. Available values: `gray`, `teal`, `amber`, `sky`, `blue`, `violet`, `emerald`, `rose`, `orange`, `fuchsia`.

::::: roadmap
:::: lane {title="gray" count="1" accent="gray"}
::: roadmap-item {title="Example" tag="Feature" tag-color="emerald" votes="1"}
:::
::::
:::: lane {title="teal" count="1" accent="teal"}
::: roadmap-item {title="Example" tag="Feature" tag-color="emerald" votes="1"}
:::
::::
:::: lane {title="amber" count="1" accent="amber"}
::: roadmap-item {title="Example" tag="Feature" tag-color="emerald" votes="1"}
:::
::::
:::: lane {title="sky" count="1" accent="sky"}
::: roadmap-item {title="Example" tag="Feature" tag-color="emerald" votes="1"}
:::
::::
:::::

## Item tag colours

The `tag-color` attribute on `roadmap-item` picks the colour of the tag pill. The pill follows the same soft-tinted style as the [Badge](badge.md) component (`bg-{color}-100 text-{color}-800` light / `bg-{color}-900 text-{color}-300` dark). Available values: `emerald`, `green`, `teal`, `amber`, `yellow`, `rose`, `red`, `sky`, `blue` (alias `primary`), `violet` (alias `purple`), `orange`, `gray`.

## Properties

### Roadmap (`::::: roadmap`)

The outer container. Lays the lanes out in a responsive grid: 1 column on phones, 2 on tablets, 4 on desktop.

### Lane (`:::: lane`)

| Property | Description |
| :--- | :--- |
| `title` | The lane heading (e.g. `Planned`, `In Progress`). |
| `count` | Number shown in the badge next to the lane title. |
| `accent` | Accent colour. One of `gray`, `teal`, `amber`, `sky`, `blue`, `violet`, `emerald`, `rose`, `orange`, `fuchsia`. Defaults to `gray`. |

### Roadmap item (`::: roadmap-item`)

| Property | Description |
| :--- | :--- |
| `title` | The item title. |
| `tag` | Optional tag label (e.g. `Feature`, `Issues`, `Integrations`). |
| `tag-color` | Tag pill colour. Defaults to `emerald`. |
| `date` | Optional date shown next to the tag (e.g. `Sep, 21`). |
| `votes` | Optional vote count shown on the right of the item footer. |
| `link` | Optional URL that turns the whole item into a clickable card. |
