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

- The live preview renders in an `<iframe>`. By default it uses a fixed
  placeholder height, so short samples leave empty space and tall ones scroll.
- Run **`neko gen-tesserae-heights`** to size every preview exactly: it compiles
  each sample, measures its rendered height with a headless browser, and bakes a
  `height=NNN` token into the fence info line
  (e.g. ` ```tesserae sample.js height=360 `). Commit the rewritten Markdown.
- A normal `build`/`watch` just reads that token and reserves the right space up
  front — no browser runs during a build, so there's no layout shift. Samples
  without a token keep the placeholder height.
- The command is **incremental/resumable**: it skips samples that already have a
  `height=` token and saves each file as it goes, so re-running only measures new
  samples. Pass `--force` to re-measure everything. Tune the measurement width
  with `tesserae.measureWidth`.
- You can also set/override the token by hand (`height=NNN`); the preview stays
  manually resizable via the iframe's drag handle either way.

## Hiding setup code from the displayed source

Two marker pairs let the displayed source differ from what is compiled and run:

- `// <hide>` … `// </hide>` — **compiled and run, but removed from the Code tab.**
  Use it for boilerplate (styling, layout chrome, demo-only plumbing).
- `// <docs>` … `// </docs>` — **shown in the Code tab, but NOT compiled.**
  Use it to show the idiomatic call a sample can't run as-is (e.g. an API the
  sandboxed preview can't use), while a hidden block runs a preview-safe variant.

````markdown
```tesserae
var navbar = HStack().Children(/* … buttons … */);
// <hide>
// Chrome only — not part of what the snippet is teaching.
navbar.WS().AlignItemsCenter().Gap(8.px()).Background("#f3f4f6").P(10);
// </hide>
return Stack().Children(navbar /* … */).Render();
```
````

A show-real-code / run-safe-code pairing looks like:

```csharp
// <docs>
button.OnClick((s, e) => Router.Push("#/view/42")); // shown, not compiled
// </docs>
// <hide>
button.OnClick((s, e) => Go("#/view/42"));          // compiled and run
// </hide>
```

- Markers must be on their own line (leading/trailing whitespace is fine);
  matching is case-insensitive and a single space after `//` is optional.
- The marker lines are dropped from both the displayed and the compiled code.
- Hidden code still runs — a syntax error inside a `// <hide>` region still
  fails the build. Code inside `// <docs>` is not compiled, so it is not checked.

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
