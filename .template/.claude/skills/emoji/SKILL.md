---
name: emoji
description: Insert an emoji with the `:shortcode:` syntax, or list every available emoji with `[!emoji-table]`. Use for friendly headings, status indicators, and decorative accents.
---

# Emoji

Neko ships with the full [Mojee](https://mojeeio.github.io/Mojee/) shortcode
set. Wrap a name with colons to insert that emoji.

## Inline

```markdown
Great job! :thumbsup: You did it! :tada:

:rocket: Launch :sparkles:
:heart:  :smile:  :+1:
```

## All available emojis

Render the full searchable table:

```markdown
[!emoji-table]
```

## Use in frontmatter and attributes

Emoji shortcodes are also valid:

- as the page `icon:` in frontmatter (`icon: ":rocket:"`).
- as the `icon=` attribute of a [`badge`](../badge/SKILL.md),
  [`button`](../button/SKILL.md), [`file`](../file/SKILL.md), or
  [`ref`](../reference-link/SKILL.md).

## Tips

- Don't use raw Unicode emoji directly — the shortcode form renders
  consistently across platforms and themes.
- Use icons ([`icon`](../icon/SKILL.md)) for UI affordances; reserve emoji for
  prose and decoration.
