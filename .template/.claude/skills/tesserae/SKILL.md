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

## Preview height

A `height=<px>` argument pins the live-preview iframe to a fixed height:

````markdown
```tesserae chrome="macos" demo.js height=420
…
```
````

Without it the iframe uses a resizable 400px minimum. The value is normally
written for you by `neko gen-tesserae-heights`, which measures each sample's
rendered height and rewrites the argument. Target a single file with
`neko gen-tesserae-heights --file <path>` and rerun it after editing a sample —
it is file-targeted with no hash cache, so nothing tracks staleness for you.

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
