---
name: command-example
description: Show a paired install + quickstart command block with one-click copy. Use in getting-started pages or READMEs to highlight the two commands a new user needs.
---

# Command example

Two-column command box with an install command on the left and a quickstart
command on the right, both copyable in one click.

## Syntax

```markdown
[!command-example install="npm install -g pkg" quickstart="pkg start"]
```

## Attributes

| Attribute    | Notes                                                                |
| ---          | ---                                                                  |
| `install`    | Installation command shown in the left tile.                         |
| `quickstart` | First-run command shown in the right tile.                           |

Both fields are optional but the block is pointless with neither.

## Examples

```markdown
[!command-example install="dotnet tool install -g Neko" quickstart="neko start"]

[!command-example install="brew install gh" quickstart="gh auth login"]
```

## Tips

- Use this on the homepage and the "Getting Started" page only — it loses its
  punch if repeated.
- Keep commands short. If the install step is multi-line, use a code block
  instead.
