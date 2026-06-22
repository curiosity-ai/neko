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

## Preview sizing

- The live preview renders in an `<iframe>`. Neko measures each compiled
  sample's rendered height with a headless browser at build time and bakes an
  exact height into the iframe, so the page reserves the right space up front
  and doesn't reflow once the sample finishes rendering.
- The measured height is cached with the compiled output, so the browser runs
  only once per unique sample. Disable with `tesserae.measureHeight: false` in
  `neko.yml` (offline builds with no browser toolchain fall back to a fixed
  placeholder height); tune the measurement width with `tesserae.measureWidth`.
- The preview stays manually resizable via the iframe's drag handle either way.

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
