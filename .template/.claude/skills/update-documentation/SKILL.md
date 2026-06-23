---
name: update-documentation
description: Workflow for updating Neko documentation — base every page on the real source code, never invent APIs, warn loudly when the source isn't available, and check that the page (and any changed `tesserae` sample) renders correctly with Playwright / snapframe / a visual check. Use whenever you add or edit docs that describe code, APIs, components, or live samples.
---

# Updating documentation

This skill is the workflow to follow **before and while editing any
documentation page**, not a Markdown component. It exists to stop two
failure modes: documenting code that doesn't match the source, and shipping
`tesserae` samples that compile but render badly.

## Rule 1 — documentation must reflect the real source code

Never describe an API, signature, option, component, or behaviour from memory
or from an older version of a page. Open the actual source first and write
from what you read.

- For C# / API surfaces, read the upstream `.cs` files and let the tooling
  vendor the signatures in rather than hand-typing them:
  - `csharp-docs` blocks (see the [`csharp-docs`](../csharp-docs/SKILL.md) skill)
    render API docs from real XML-doc'd source.
  - `tesserae:source` markers regenerate from the Tesserae checkout via the
    `update-tesserae-docs.cs` script — the block between the markers is
    **fully regenerated**, so never edit it by hand.
  - `api:source` markers regenerate the public surface via
    `neko sync-api-docs` (runs by default before `neko build`).
- For Neko's own config keys, components, and CLI flags, check
  `Neko/Configuration/*.cs`, `Neko/Extensions/*.cs`, and the command sources —
  not just what an existing page claims.
- When the source and an existing page disagree, the source wins. Update the
  page to match and note what changed.

### If the source is NOT available — STOP and warn the user

If you cannot get the real source — the repo isn't checked out, a clone fails,
the network is blocked, or a `*:source` root can't be resolved — do **not**
guess, paraphrase from training data, or copy a stale page forward as if it
were verified. Instead:

1. **Stop** and tell the user, clearly and prominently, that the source for
   `<thing>` is not available, so the change cannot be verified against it.
2. Say exactly what's missing (which repo / file / root) and what you tried.
3. Offer the path forward: provide the checkout / restore network access / point
   `--tesserae` or `apiDocs.roots` at the source, then re-run.
4. Only proceed on explicit instruction, and when you do, **flag every
   unverified claim** in your reply so the user knows what still needs checking.

A wrong-but-confident API doc is worse than an obvious gap. When in doubt, warn.

## Rule 2 — verify changed `tesserae` samples actually render

A `tesserae` sample that compiles is not the same as one that looks right.
Whenever you add or change a sample, confirm the rendered preview before
committing.

1. **Regenerate the height** for the file you touched so the preview iframe is
   sized to the sample (no layout shift, no clipped/empty preview):

   ```bash
   neko gen-tesserae-heights --file path/to/page.md
   ```

   It is file-targeted with no staleness cache — rerun it on each file you
   changed. See the [`tesserae`](../tesserae/SKILL.md) skill for the marker
   regions (`// <hide>`, `// <docs>`) and caching details.

2. **Look at it rendered.** Run `neko start`, open the page, and check the
   preview in light and dark mode: the layout isn't broken, nothing overflows
   or is cut off, and the sample demonstrates what the prose says it does.

3. **Confirm with a browser capture / Playwright** rather than trusting that it
   compiled:
   - Use Playwright (or `snapframe` / `neko snap`) to load the page and capture
     the sample, then eyeball the image. See the
     [`snapframe`](../snapframe/SKILL.md) skill.
   - In the Neko repo itself, samples and components are validated with the
     Playwright tests under `Neko.Tests` — run them when a component changes.

   Always actually view the screenshot; "the build passed" does not prove the
   sample looks nice.

## Rule 3 — check the rendered page with Playwright / snapframe

Verification isn't just for `tesserae` samples. Any documentation change can
render differently from how it reads in Markdown — a broken component, an
overflowing table, a mis-sized image, a layout that falls apart in dark mode.
Don't trust that a clean `neko build` means the page looks right; load it in a
real browser and look.

A few ways to do that, all Playwright-backed:

1. **`snapframe`** — a Playwright-driven browser CLI for navigating to a page
   and capturing a screenshot. Use it to check an arbitrary page, not just one
   that already carries a `[!snapframe …]` directive:

   ```bash
   # serve the built output, then drive a real browser at it
   dotnet serve <output-folder> --port 5000 &
   PAGE_ID=$(snapframe navigate http://localhost:5000/path/to/page)
   snapframe capture "$PAGE_ID" /tmp/page-check.png
   snapframe stop
   ```

   Then **open the PNG and eyeball it** — light and dark mode, layout intact,
   components rendered, nothing clipped or overflowing. See the
   [`snapframe`](../snapframe/SKILL.md) skill for `--chrome`, `--bg`,
   `--full-page`, and the interaction verbs (`click`, `interact`, `wait`).

2. **`neko snap`** — the built-in capture command. It refreshes the images for
   the `[!snapframe …]` directives already in a page (same Playwright engine),
   which is the right tool when the change is *to* a screenshot embedded in the
   docs.

3. **Playwright tests** — in the Neko repo itself, pages and components are
   validated by the Playwright tests under `Neko.Tests`. Run them when a
   component changes; they build the docs into a temp folder and assert on the
   rendered HTML.

Whichever you use: actually view the screenshot or the test result. "The build
passed" does not prove the page looks right.

## Quick checklist before committing a docs change

- [ ] Wrote the page from the **real source**, not from memory.
- [ ] Source-synced blocks (`csharp-docs`, `tesserae:source`, `api:source`)
      were **regenerated**, not hand-edited.
- [ ] If source was unavailable, **warned the user** and flagged every
      unverified claim instead of guessing.
- [ ] Changed a `tesserae` sample? Reran `neko gen-tesserae-heights --file …`.
- [ ] **Checked the rendered page** with Playwright / `snapframe` / `neko snap`
      / `neko start` in light and dark mode — and looked at the result.
- [ ] Verified any changed `tesserae` sample **renders correctly** the same way.
- [ ] `neko build` succeeds with no warnings.
