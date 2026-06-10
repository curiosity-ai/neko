---
order: 3
label: Folder
icon: folder
tags: [config]
---
# Folder configuration

Using the same **.yml** technique and options as [Page configuration](/configuration/page.md), a folder can be configured using a separate **index.yml** file placed inside the folder.

!!!
Folders support the same properties as [pages](/configuration/page.md), although a few properties that are not applicable in the context of a folder configuration would be ignored, such as [`description`](/configuration/page.md#description).
!!!

---

## icon

Set a custom [icon](/configuration/page.md#icon) for the folder.

```yml index.yml
icon: settings
```

---

## expanded

Expand the folder node in the tree navigation with the [expanded](/configuration/page.md#expanded) config.

```yml index.yml
expanded: true
```

---

## label

Change the folder [label](/configuration/page.md#label) used for the left navigation tree node label.

```yml index.yml
label: Custom label
```

---

## order

Move a folder up to the top of the navigation by setting the [order](/configuration/page.md#order). The higher the number, the higher in the stack the folder will be placed.

```yml index.yml
order: 1000
```

Move a folder to the bottom of the navigation. The lower the number, the lower in the stack it will be placed.

```yml index.yml
order: -1000
```

---

## nextprev

The `nextprev` configuration controls the display of "Next" and "Previous" navigation buttons at the bottom of each page and whether a page is included in the navigation sequence.

### mode

=== mode : `string`

Controls how the Next/Previous navigation buttons are displayed and whether the page is included in the navigation sequence.

Option | Description
--- | ---
`show` | Show Next and Previous buttons and include page in sequence (Default)
`hide` | Hide buttons but keep page in sequential order
`exclude` | Hide buttons and exclude page from sequential order

The default value is `show`.

```yml
nextprev:
  mode: hide
```

See also [Project](project.md#nextprev-mode) and [Page](page.md#nextprev-mode) configuration of `nextprev.mode`.
===

---

## permalink

Configures a new permanent base path for all pages within this directory.

See **Page** [`permalink`](page.md/#permalink) for full details.

```yml index.yml
permalink: /tutorials
```

---

## searchExclude

Exclude every page in this folder (and its subfolders) from the in-site search index.

```yml index.yml
searchExclude: true
```

Pages inside the folder are still built and reachable, but they will not appear in the search results. See the page-level [`searchExclude`](page.md#searchexclude) config to exclude a single page.

---

## changelog

Turn this folder into a [changelog](/configuration/changelog.md). The
version-named `.md` files inside are parsed (file name → version), sorted
newest-first, and rendered as a single timeline page at the folder URL. The
companion `title` and `description` keys set the page heading and lead
paragraph.

```yml index.yml
changelog: true
title: Changelog
description: All notable changes to this project.
icon: memo
order: 2
```

See the [Changelog](/configuration/changelog.md) configuration page for the full details.

---

## visibility

Hide a folder by setting the [visibility](/configuration/page.md#visibility) configuration.

```yml index.yml
visibility: hidden
```

Another option to completely ignore a folder or a file would be to prefix the folder name or file name with an underscore `_`. For instance, naming a folder `_guides` would instruct Neko to ignore the folder.

Password protect an entire folder by setting the `visibility` to either [`protected`](page.md#protected) or [`private`](page.md#private).

```yml index.yml
visibility: protected
```
