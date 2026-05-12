---
name: comments
description: Add hidden comments inside Markdown that don't appear in the rendered output. Use for TODOs, author notes, draft sections, internal review comments.
---

# Comments

Hidden comments in Neko docs are wrapped in double-percent signs. They are
stripped at build time and never appear in the HTML.

## Inline form

```markdown
This is %%a hidden note%% visible text.
```

## Block form

```markdown
%%
TODO: update once the API is finalised
Reviewer: confirm the example matches v2.0
%%
```

## Tips

- Use comments for TODOs, draft notes, and reviewer comments that should not
  ship.
- Block comments may span multiple lines and contain Markdown — but none of it
  renders.
- Prefer `_drafts/` (filenames starting with `_`) for whole pages you do not
  want published.
- Comments are not safe for secrets. They are removed from HTML but remain in
  the `.md` source in source control.
