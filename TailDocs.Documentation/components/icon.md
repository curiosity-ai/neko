---
title: Icon Component
---

# Icon

The Icon component allows you to insert icons inline or within other components. TailDocs uses the **Flaticon UIcons** set (Regular Rounded style).

## Syntax

Use the `[!icon name="..."]` syntax, where `name` is the name of the icon (without the `fi-rr-` prefix).

```markdown
This is a star: [!icon name="star"]
```

## Attributes

| Attribute | Description | Default |
| :--- | :--- | :--- |
| `name` | The name of the icon. | (Required) |

### Common Icons

- `star` [!icon name="star"]
- `check` [!icon name="check"]
- `cross` [!icon name="cross"]
- `menu-burger` [!icon name="menu-burger"]
- `search` [!icon name="search"]
- `home` [!icon name="home"]
- `settings` [!icon name="settings"]
- `user` [!icon name="user"]
- `info` [!icon name="info"]
- `exclamation` [!icon name="exclamation"]

### Styling

Icons inherit the font size and color of their surrounding text. You can style them further using CSS or by wrapping them in spans if needed.
