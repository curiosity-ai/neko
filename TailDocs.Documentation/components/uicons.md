---
icon: smile
---
# UIcons

[UIcons](https://www.flaticon.com/uicons) by Flaticon are the official icon set for TailDocs. Specifically, the **Regular Rounded** style is included by default.

## Usage

You can use UIcons in various components by specifying the icon name. The full list of available icons can be found on the [Flaticon UIcons website](https://www.flaticon.com/uicons).

### Component

For the [Icon](icon.md) component, the icon is specified using the syntax `:icon-shortcode:`, where `shortcode` is the name of the icon.

For example, use the code `:icon-rocket:` for a :icon-rocket: icon.

When an icon is used in other components, the icon is referred to by only the `shortcode`.

For example, the following demonstrates using the icon in a [Badge](badge.md#icon-and-emoji) and a [Button](button.md#icon-and-emoji).

Component | Sample
--- | ---
[!badge icon="rocket" text="rocket"] | `[!badge icon="rocket" text="rocket"]`
[!button icon="rocket" text="rocket"] | `[!button icon="rocket" text="rocket"]`

### Metadata

When an icon is specified within the [Page](../configuration/page.md) or [Project](../configuration/project.md) metadata, the icon can be referred to by only its `shortcode`.

The following sample demonstrates setting a Page [icon](../configuration/page.md/#icon) to a :icon-rocket:.

```yaml
---
icon: rocket
---
# Sample

This is a sample page with a :icon-rocket: icon.
```

## Icon Search

You can search for available icons on the [Flaticon UIcons website](https://www.flaticon.com/uicons). Note that only the **Regular Rounded** style is currently included.
