# Plan: pure-C# Tailwind generation (drop the standalone CLI)

## Goal

Replace the build-time dependency on the Tailwind **standalone CLI** (a
downloaded native binary, see `TailwindBuilder.cs`) with a **pure-C#**
generator that produces the same `assets/tailwind.css`. No npm, no Node, no
JS execution, no external binary download at build time — just .NET, matching
Neko's "embedded resources, no Node/npm at runtime" rule.

The current CLI-based implementation already works and is the **reference
oracle** for this work: it stays in the tree until the C# generator reaches
parity, then becomes an optional dev-only verification path (or is deleted).

## Strategy: generate utilities in C#, ship base + components as static CSS

Tailwind output is three `@layer`s. Their difficulty in C# is very uneven, so
split the work:

| Layer | Content | Approach |
| --- | --- | --- |
| `base` | Preflight (the CSS reset) | **Ship verbatim** as an embedded resource. It does not depend on content or per-site config. |
| `components` | `@tailwindcss/typography` (`.prose…`) | **Ship verbatim** as an embedded resource. `prose` uses the gray ramp + CSS variables (`--tw-prose-*`), independent of the per-site `primary`/`accent` palette, so a single captured copy works for every site. |
| `utilities` | `flex`, `p-4`, `dark:bg-gray-900`, `bg-primary-600`, … | **Generate in C#.** This is the only layer that depends on the scanned content and the dynamic palette, and it is the only part worth implementing. |

This collapses the task from "reimplement Tailwind" to "implement the
utility-generation slice for the class vocabulary Neko actually emits."

### How to capture the static layers

Run the existing CLI once (offline, dev machine) with an input that emits only
`@tailwind base;` then only `@tailwind components;`, and save each output:

```
tailwindcss -i base.css   -o Neko/Resources/tailwind/preflight.css     # @tailwind base;
tailwindcss -i comp.css   -o Neko/Resources/tailwind/typography.css    # @tailwind components; + typography plugin
```

Embed both as resources (they get copied like the other `Neko/Resources/*.css`
via `CopyAssetsAsync`). The C# generator concatenates `preflight.css` +
`typography.css` + the generated utilities into `assets/tailwind.css`.
Re-capture only when the pinned Tailwind version changes.

## The golden-master test harness (build this first)

Before writing any generator code, make the CLI output the spec:

1. Build the real sites (`.template` and `Neko.Documentation`) with the
   **current CLI path** and save each `assets/tailwind.css` as a fixture.
2. Extract the set of utility selectors the CLI produced (everything in
   `@layer utilities`, i.e. rules that are not in the captured preflight or
   typography files). This is the **exact surface the C# generator must
   reproduce** for that content — nothing more is required, and anything extra
   is acceptable as long as it is correct.
3. Parity test = for each fixture content set, every CLI utility rule must be
   produced by the C# generator with an **equivalent declaration block**
   (normalize whitespace/property order before comparing). Track a coverage
   number: `% of CLI utility rules reproduced`. Ship at 100% for the two real
   sites; below that, the CLI fallback stays the default.

This makes the work bounded, measurable, and regression-proof.

## Architecture / new files (under `Neko/Builder/Tailwind/`)

- `TailwindGenerator.cs` — entry point. `string Generate(IEnumerable<string>
  htmlAndJsFiles, NekoConfig config)`. Orchestrates scan → parse → emit →
  assemble.
- `ClassExtractor.cs` — stage 1. Harvest candidate tokens from file contents.
- `Candidate.cs` — parsed token: `Variants` (ordered list), `BaseUtility`,
  `ArbitraryValue`, `OpacityModifier`, `Important`, `Negative`.
- `CandidateParser.cs` — stage 2. String → `Candidate`.
- `TailwindTheme.cs` — the theme scales (spacing, colors, fontSize, …) built
  from `ThemeDefinitions.ResolvePalettes(config)` + Tailwind defaults.
- `UtilityRegistry.cs` — stage 3. The utility table: maps a base utility to a
  declaration generator. The bulk of the code.
- `VariantEngine.cs` — stage 4. Wraps a rule's selector/at-rules per variant.
- `RuleSorter.cs` — stage 5. Tailwind's utility ordering.
- `CssRule.cs` — `{ Selector, IReadOnlyList<(string Prop,string Val)> Decls,
  AtRules }` plus a stable sort key.

Integration: replace the body of `TailwindBuilder.GenerateAsync` with a call
into `TailwindGenerator`. Keep the existing public surface
(`GenerateAsync(outputDir, config, …)`) and the `HtmlGenerator`/`SiteBuilder`
wiring unchanged — only the *how* changes. `ResolveCliAsync` and the download
code can be deleted once parity holds (or kept behind
`NEKO_TAILWIND_CLI`/an env flag as a dev oracle).

## Pipeline detail

### Stage 1 — extract candidates (`ClassExtractor`)

- Read each content file (same globs as today: `**/*.html`, `**/*.js`, minus
  `*.min.js` and `tailwind.js`).
- Tailwind's extractor is deliberately dumb: a broad regex that grabs any
  substring that could be a class. Port its behaviour — split on quotes,
  whitespace, `<>`, backticks, `=`, etc., and keep tokens matching roughly
  `^[A-Za-z0-9_\-:/\[\].!#%]+$`, including bracketed arbitrary values
  `min-h-[inherit]` and slashes `bg-black/50`.
- Dedupe into a `HashSet<string>`. Order does not matter yet.
- Because Neko inlines most scripts into the HTML, JS-toggled classes (active
  sidebar link, TOC, tabs, mermaid template literals) are present as string
  literals and get picked up here; external `history.js` is covered by the
  `*.js` glob. Keep both in scope.

### Stage 2 — parse candidates (`CandidateParser`)

Split `dark:hover:md:-bg-primary-500/50!` into parts:

- **Variants**: everything before the final `:` that is not inside `[]`. Split
  on `:` at bracket depth 0. Order preserved (matters for stacking).
- **Important**: trailing or leading `!`.
- **Negative**: leading `-` on the base utility (e.g. `-mt-4`, `-translate-x-full`).
- **Opacity modifier**: a trailing `/<number|[value]>` on the base
  (`bg-black/50`, `text-white/[.06]`).
- **Arbitrary value**: a trailing `-[...]` (respect nested brackets; the value
  may contain `:` and `/`). Also arbitrary *properties* `[mask-type:luminance]`
  (low priority — Neko likely emits none; confirm against the fixture).
- Reject tokens whose base doesn't map to any known utility (silently drop —
  most harvested tokens are not classes).

### Stage 3 — theme model (`TailwindTheme`)

Hardcode the **default Tailwind v3 scales** that Neko uses, overlaid with
config:

- `spacing` (0, px, 0.5…96 — the standard rem scale) → used by `p/m/gap/space/
  w/h/inset/translate/…`.
- `colors`: the full default palette (slate, gray, zinc, red, …, plus
  `white`/`black`/`transparent`/`current`) **plus** `primary` and `accent`
  from `ThemeDefinitions.ResolvePalettes(config)`. Each color is a ramp
  `50…950`.
- `fontSize`, `fontWeight`, `lineHeight`, `letterSpacing`, `borderRadius`,
  `borderWidth`, `boxShadow`, `opacity`, `zIndex`, `screens`
  (sm/md/lg/xl/2xl), etc.

Only include scales the fixture proves are needed — but the color ramp and
spacing are certainly required. Keep this data-driven (dictionaries) so it is
easy to extend when a fixture surfaces a gap.

### Stage 4 — utility registry (`UtilityRegistry`)

The core table. Each entry maps a utility **prefix** to a function
`(value, theme) → declarations`. Organize by family; implement in
fixture-driven priority order (most-used first). Categories Neko uses (verify
against the fixture, but expect):

- **Layout**: `block hidden inline inline-block flex inline-flex grid table …`,
  `static relative absolute fixed sticky`, `inset-* top/right/bottom/left-*`,
  `z-*`, `overflow-*`, `float-*`, `isolate`.
- **Flex/Grid**: `flex-row/col/wrap`, `flex-1/auto/none`, `grow shrink basis-*`,
  `order-*`, `items-* justify-* content-* self-*`, `gap-* gap-x/y-*`,
  `grid-cols-* col-span-* row-*`, `place-*`.
- **Spacing**: `p{,t,r,b,l,x,y}-*`, `m{…}-*` (incl. negative + `auto`),
  `space-x/y-*`.
- **Sizing**: `w-* h-* min-w/h-* max-w/h-*` (incl. `full screen min max fit
  auto` and fractions like `1/2`, `1/3`).
- **Typography**: `text-{xs…9xl}`, `font-{thin…black}`, `font-{sans,mono}`,
  `leading-* tracking-*`, `text-{left,center,right}`, `truncate`,
  `underline line-through no-underline`, `uppercase lowercase capitalize`,
  `whitespace-* break-* italic`, `list-*`.
- **Colors** (the dynamic part): `text-* bg-* border-* ring-* ring-offset-*
  divide-* from-* via-* to-* outline-* decoration-* placeholder-* fill-*
  stroke-* accent-* caret-*` — each resolves a `color[/opacity]` from the
  theme, emitting the right property (and the `--tw-*` custom-property dance
  for `ring`/gradients/`bg-opacity`). Generate for every ramp shade including
  `primary`/`accent`.
- **Borders/effects**: `rounded{,-t,-tr,…}-*`, `border{,-x,-y,-t,…}-*` (width),
  `border-{solid,dashed,…}`, `shadow-* shadow-{color}`, `opacity-*`,
  `ring-* ring-inset`, `divide-*`.
- **Transforms/transitions**: `translate-x/y-* scale-* rotate-* skew-*`,
  `transform transform-none origin-*`, `transition{,-colors,-transform,…}`,
  `duration-* ease-* delay-*`, `animate-*`.
- **Filters/misc**: `backdrop-* blur-*`, `cursor-* select-* pointer-events-*`,
  `appearance-none`, `sr-only`, `object-* aspect-*`.

Each generator returns the *unprefixed* `.class { … }` rule; variants and the
final selector escaping are applied in stage 4b. Reuse the static layers for
anything that turns out to be a `base`/`components` rule rather than a utility.

#### 4b — selector escaping

Tailwind escapes special chars in class selectors: `.` `:` `/` `[` `]` `%` `#`
become `\.` `\:` `\/` etc. Implement `EscapeClassName` and match the CLI's
output exactly (compare against the fixture — e.g. `.dark\:bg-gray-900`,
`.min-h-\[inherit\]`, `.bg-black\/50`).

### Stage 4 (variants) — `VariantEngine`

Apply variants to a base rule. Each variant either rewrites the selector or
wraps the rule in an at-rule. Implement at least the ones in the fixture:

- **Pseudo-class**: `hover focus focus-within focus-visible active visited
  disabled checked first last odd even` → append `:hover` etc.
- **`dark`** (class strategy, since `darkMode:'class'`) → prefix selector with
  `.dark ` (i.e. `.dark .dark\:bg-gray-900`).
- **Responsive**: `sm md lg xl 2xl` → wrap in `@media (min-width: <screen>)`.
- **Group/peer**: `group-hover group-focus …` → `.group:hover &`; `peer-*` →
  `.peer:hover ~ &`.
- **Before/after/placeholder/marker/selection** → `::before` etc.
- Stacking: variants apply out-in in the listed order; responsive at-rules wrap
  pseudo-selectors. Validate ordering against the fixture (e.g.
  `dark:hover:bg-…`).

### Stage 5 — sort, dedupe, assemble (`RuleSorter` + `TailwindGenerator`)

- **Dedupe** identical rules.
- **Sort** to match Tailwind's cascade: layer order (base → components →
  utilities) is handled by concatenation; within utilities, replicate
  Tailwind's property-based ordering so specificity ties resolve the same way.
  Simplest robust approach: derive a canonical order index from the order the
  utilities appear in the **CLI fixture** and sort generated rules by it; this
  guarantees cascade parity without re-deriving Tailwind's internal sort.
- **Responsive grouping**: emit base rules first, then each `@media` block in
  `sm→2xl` order (Tailwind groups responsive variants at the end).
- Assemble: `preflight.css` + `typography.css` + generated utilities. Minify
  (strip comments/whitespace) to match the `--minify` output, or ship
  unminified — parity test should normalize either way.
- Write `assets/tailwind.css` (same path/return contract as today).

## Dynamic palette

Already solved structurally: `ThemeDefinitions.ResolvePalettes(config)` returns
the per-site `primary`/`accent` ramps. Feed them into `TailwindTheme` so the
color-utility generators emit `bg-primary-600`, `dark:text-primary-400`, etc.
with the right hex. No other layer depends on the palette (prose uses gray), so
the static `preflight.css`/`typography.css` are palette-independent.

## Gotchas / edge cases

- **Arbitrary values** (`min-h-[inherit]`, `-translate-x-full`,
  `w-[32rem]`): the value passes through to CSS mostly verbatim; underscores
  become spaces (`grid-cols-[1fr_500px]`). Confirm Neko's actual arbitrary
  values from the fixture and support exactly those forms first.
- **`!important`**: append `!important` to every declaration in the rule.
- **Opacity modifiers** (`/50`, `/[.06]`): color utilities emit
  `rgb(... / <alpha>)`; mirror the CLI's exact `color-mix`/rgb form.
- **`ring`, gradients, `divide`, `space`** use Tailwind's `--tw-*` custom
  properties and (for `space`/`divide`) the `> :not([hidden]) ~ :not([hidden])`
  selector. Copy the CLI output for these verbatim per utility.
- **Don't scan `tailwind.js` or `*.min.js`** — already excluded in
  `BuildConfigJs`; keep the same exclusions in `ClassExtractor`.
- Classes added by JS that are *not* string literals (e.g. built by
  concatenation) won't be found — same limitation as the CLI; covered today
  because Neko's scripts use literal class strings. If a future script
  concatenates classes, add them to a safelist.

## Milestones

1. **Harness + static layers**: capture `preflight.css`/`typography.css`,
   embed them, build the golden-master fixtures and the parity/coverage test.
   (No generator yet; test reports 0% utilities.)
2. **Skeleton pipeline**: extractor + parser + escaping + assembler, with a
   tiny registry (display, spacing, a few colors). Wire into `TailwindBuilder`
   behind an env flag (`NEKO_TAILWIND_PURE=1`) so the CLI stays default.
3. **Registry to parity**: implement utilities family-by-family, driven by the
   coverage number against `.template`, then `Neko.Documentation`. Add the
   variant engine. Iterate until 100% on both sites.
4. **Visual validation**: render a few pages (Playwright) in light + dark and
   diff against a CLI-built site; confirm no visual regressions, especially the
   dark backgrounds and `prose`.
5. **Flip the default**: make pure-C# the default; keep the CLI reachable via
   `NEKO_TAILWIND_CLI` as a dev oracle, or delete the download path and the
   `TailwindVersion` pin entirely. Update `sources.md` (CLI no longer fetched)
   and add a changelog entry.

## Effort & risk

- **Effort**: a few thousand lines, most of it the utility registry, but
  tractable because the fixture bounds the surface and the static layers remove
  the two hardest pieces (preflight, prose). Estimate: skeleton in 1–2 days,
  parity on the real sites in roughly 1–2 weeks of focused work.
- **Main risk**: cascade ordering and the `--tw-*`-property utilities (`ring`,
  gradients, `space`/`divide`). Mitigate by ordering from the fixture and by
  copying the CLI's exact declarations for those utilities rather than
  re-deriving them.
- **Safety net**: the CLI/CDN fallback chain stays until parity is proven, so
  this can land incrementally without breaking builds.

## Integration checkpoints (don't regress)

- Keep `TailwindBuilder.GenerateAsync`'s signature and the
  `SiteBuilder`/`HtmlGenerator` wiring (`_staticTailwind`, the per-site
  route-prefixed `<link>`) exactly as-is.
- Per-site generation still runs once per sub-project (multi-repo), because the
  used-class set and palette differ — `TailwindGenerator.Generate` is called
  per `OutputDirectory`.
- Honour `NEKO_DISABLE_STATIC_TAILWIND` (CDN fallback) the same way.
