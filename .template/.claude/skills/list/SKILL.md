---
name: list
description: Render bullet, ordered, task, alphabetic, roman, definition, and icon-prefixed lists. Use for any enumerated content; this skill covers Neko's list extensions on top of standard Markdown.
---

# List

Neko supports every standard Markdown list, plus a few extensions.

## Bullet list

```markdown
- Item
- Item
- Item
```

## Ordered list

```markdown
1. First
2. Second
3. Third
```

## Task list

```markdown
- [x] Done
- [ ] Pending
- [ ] To do
```

## Letter list

```markdown
a. Alpha
b. Beta
c. Charlie
```

## Roman list

```markdown
i. Iota
ii. Omicron
iii. Tau
```

## Definition list

```markdown
Term
: Definition body for Term.

Another term
: Definition body for Another term.
```

## Icon-prefixed list

Add `{.list-icon}` immediately above a list and start each item with an
[icon](../icon/SKILL.md):

```markdown
{.list-icon}
- :icon-check-circle: Build succeeded
- :icon-bell: Notification configured
- :icon-rocket: Deployed
```

## Tips

- Use a task list for visible checklists (releases, onboarding steps).
- Use a definition list for glossaries and option references.
- Prefer the [`steps`](../steps/SKILL.md) component for **sequential
  procedures** — it numbers and styles them automatically.
