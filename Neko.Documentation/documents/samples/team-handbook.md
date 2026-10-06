---
title: Team handbook
description: "A plain document in the default neko theme: headings, lists, a table, code and a callout, flowing across as many pages as it needs."
document:
  theme: neko
  author: The Neko team
  logoText: Neko
---

# Team handbook

How we work, what we expect of each other, and where to find things. Written once, kept in Markdown, and downloadable as a Word document for anyone who wants a copy to annotate.

## 1. Working together

We keep meetings short and decisions written down. A decision that is not in writing did not happen: it goes in the project's `decisions/` folder with a date, the people who made it, and the reasoning that would not be obvious in a year.

- **Be reachable in working hours**, and say when you are not.
- **Ask in public first.** A question in a shared channel is answered once and read many times.
- **Review within a day.** A pull request that waits becomes a merge conflict.

### Reviews

A review answers three questions, in this order:

1. Does it do what it says?
2. Can the next person change it?
3. Is there anything here that will hurt us later?

> Review the change, not the person. If a comment would sting in a meeting, rewrite it until it would not.

## 2. Releases

Releases follow calendar versioning, `vYY.M`, and every release has a changelog entry written by the person who made the change.

| Step | Owner | When |
| --- | --- | --- |
| Freeze the branch | Release manager | Tuesday |
| Run the full test suite | CI | On every push |
| Publish the package | Release manager | Thursday |
| Announce | Whoever shipped the headline change | Within the hour |

```bash
dotnet build Neko.sln
dotnet test Neko.Tests
dotnet pack Neko/Neko.csproj -c Release
```

!!! info Hotfixes
A hotfix skips the freeze but not the tests. Write the changelog entry first; it is the cheapest way to find out whether the fix is understood.
!!!

## 3. Where things live

Documentation lives beside the code, in `Neko.Documentation/`. Decisions live in `decisions/`. Anything that has to be secret lives in the secret store, never in the repository.

For everything else, ask in the team channel. If the answer is useful for the next person, add it here.
