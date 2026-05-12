---
name: icon
description: Render a UIcon (Flaticon Regular Rounded) inline with `:icon-name:`, or use the same name in frontmatter `icon:` and component `icon=` attributes. Use for visual cues in headings, lists, badges, and navigation.
---

# Icon

Neko's official icon set is **Flaticon UIcons — Regular Rounded** (~2285+
icons). Octicons and other older sets are no longer supported.

## Inline syntax

```markdown
:icon-rocket:
:icon-star:
:icon-check-circle:
:icon-brands-github:
```

Always prefix with `icon-`. Without the prefix the colons fall back to
[emoji](../emoji/SKILL.md) resolution.

## In frontmatter

```yml
---
icon: rocket
---
```

`icon:` accepts:

- a UIcon name (`rocket`, `bell`, `palette` …),
- an emoji shortcode (`":rocket:"`),
- a raw inline SVG (`"<svg>…</svg>"`),
- a path to an image (`"../assets/icon.png"`).

## In component attributes

```markdown
[!badge icon="rocket" text="Launch"]
[!button icon=":heart:" text="Like"]
[!ref icon="book" text="Guide"](guides/start.md)
```

## Brand icons

Use the `brands-*` prefix for service logos:

`brands-github`, `brands-twitter`, `brands-linkedin`, `brands-discord`,
`brands-youtube`, etc.

## Tips

- Pick a name from the Flaticon UIcons catalog
  (<https://www.flaticon.com/uicons>).
- Don't mix icon families on a page — UIcons everywhere.
- Use emoji ([`emoji`](../emoji/SKILL.md)) in prose; reserve icons for UI
  affordances (links, buttons, navigation, headings).
