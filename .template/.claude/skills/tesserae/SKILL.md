---
name: tesserae
description: Render a live Tesserae C# UI block — Neko compiles and executes the C# at build time and shows both the code and a working preview side-by-side. Use only when the project documents Tesserae or runs live C# samples.
---

# Tesserae

A `tesserae` fenced block contains C# UI code that Neko compiles and runs at
build time. The output is a tabbed view: source code on one side, an
interactive preview on the other.

## Syntax

````markdown
```tesserae
using Tesserae;
using static Tesserae.UI;

public class TodoApp
{
    public static void Main()
    {
        MountToBody(TextBlock("Hello, Tesserae!"));
    }
}
```
````

## What it expects

- A complete C# program with a `Main` entry point.
- A call into Tesserae's `MountToBody(...)` (or equivalent) at the end so the
  block produces a rendered UI.
- The `Tesserae` and `Tesserae.UI` namespaces — both already on the classpath.

## When to use

- This component is meaningful **only** in projects that embed Tesserae (e.g.
  `Tesserae`'s own docs). Most Neko sites will never use it.
- For C# code with API documentation but no live preview, use
  [`csharp-docs`](../csharp-docs/SKILL.md).
- For arbitrary C# code blocks, use a plain
  [`code-block`](../code-block/SKILL.md) with ` ```csharp `.

## Showing different code than what runs

By default a `tesserae` block is **both compiled and displayed as-is** — write a
complete program and the reader sees exactly what runs.

When a sample can't run as-is in the sandboxed preview iframe (e.g. it needs the
History API, which `about:srcdoc` forbids), put the version to **display** inside
an `// <overwrite-sample-code>` … `// </overwrite-sample-code>` region. Then:

- everything **outside** the region is compiled and run (and is *not* shown), and
- everything **inside** the region is shown in the Code tab verbatim and is
  *never* compiled.

````markdown
```tesserae
// Compiled + run (powers the live preview), but not shown:
public class App { public static void Main() { /* sandbox-safe variant */ } }
// <overwrite-sample-code>
// Shown in the Code tab (the real-app version), but not compiled:
public class App { public static void Main() { /* idiomatic version */ } }
// </overwrite-sample-code>
```
````

- The markers must be on their own line (leading/trailing whitespace is fine);
  matching is case-insensitive and a single space after `//` is optional.
- Keep it rare. Most samples need no overwrite — show what runs.
- The displayed (overwrite) code is never compiled, so it is not checked; keep it
  a faithful, complete program (usings, namespace, `Main`).

## Preview sizing

- The live preview renders in an `<iframe>`. By default it uses a fixed
  placeholder height, so short samples leave empty space and tall ones scroll.
- A `height=NNN` token on the fence info line
  (e.g. ` ```tesserae sample.js height=360 `) pins the iframe to that height. A
  normal `build`/`watch` just reads the token and reserves the space up front —
  no browser runs during a build, so there's no layout shift. Samples without a
  token keep the placeholder height.
- Run **`neko gen-tesserae-heights`** to fill the tokens in: it compiles each
  sample, measures its rendered height with a headless browser, and writes the
  token back. It is **incremental/resumable** — it skips samples that already
  have a `height=` token and saves each file as it goes, so re-running only
  measures new samples. Pass `--force` to re-measure everything; tune the
  measurement width with `tesserae.measureWidth`. Commit the rewritten Markdown.
- Target a single file with **`neko gen-tesserae-heights --file <path>`**. A
  targeted run always re-measures that file's samples (there is no hash cache),
  so rerun it after editing a sample to refresh its `height=`.
- You can also set the token by hand; the preview stays manually resizable via
  the iframe's drag handle either way.

## Caching and performance

- Each compiled sample is cached on disk under a `.neko-cache/` folder in the
  project root, keyed by a hash of its code plus the Tesserae version, so
  unchanged samples are reused across builds and `neko start` restarts instead
  of recompiling. Add `.neko-cache/` to your `.gitignore`.
- Samples compile in parallel in a warm-up pass before pages render. Tune the
  degree with `tesserae.maxParallelism` in `neko.yml` (`0` = CPU count).
- Pin the Tesserae version with `tesserae.version` in `neko.yml` for
  deterministic builds — otherwise Neko resolves the latest stable version once
  and records it on disk, reusing it on every later build (no expiry). See the
  `neko-yml` skill.

## Tips

- Failures in the embedded program become build errors — keep examples
  self-contained and exercise common paths.
- Use small, focused examples; full apps don't fit the inline preview pane.
