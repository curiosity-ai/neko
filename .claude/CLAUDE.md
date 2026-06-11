# CLAUDE.md — Neko engine development

This file applies to the **Neko repository itself** — the .NET source, the
documentation site, and the build/test workflow. If you are editing
documentation in a Neko-generated site (a project that uses Neko), see
[`.template/.claude/CLAUDE.md`](../.template/.claude/CLAUDE.md) instead.

> See also [`AGENTS.md`](../AGENTS.md) — the canonical, top-level instructions
> for agents working in this repo. This file augments it with development
> conventions and the template workflow.

## Repository layout

```
neko/
├── Neko/                     # CLI source (.NET 10)
│   ├── Builder/              # site builder pipeline
│   ├── Configuration/        # neko.yml / page metadata models
│   ├── Extensions/           # Markdig extensions (one per component)
│   ├── Server/               # Kestrel-backed `neko start` dev server
│   ├── Resources/            # embedded CSS/JS/fonts/icons + templates.json
│   └── Program.cs            # System.CommandLine entry point
├── Neko.Documentation/       # the official docs site (built with Neko itself)
├── Neko.Tests/               # unit + Playwright tests
├── Neko.Tools.UIcons/        # UIcon catalog tooling
├── .template/                # ← Hello-world starter for downstream projects
│   ├── neko.yml
│   ├── index.md, getting-started.md, about.md
│   └── .claude/              # ← Per-template Claude context and skills
│       ├── CLAUDE.md         # How to author Neko docs
│       └── skills/<name>/SKILL.md  # one skill per component
├── reference/                # third-party libraries pinned for reference
├── AGENTS.md                 # top-level agent rules (read this first)
├── README.md
├── TODO.md
└── neko.sln
```

## What `.template/` is for

`.template/` is a **blank, working Neko documentation project** that downstream
users (and Claude, when bootstrapping a new docs site) can copy as a starting
point. It contains:

- a minimal `neko.yml`
- a homepage `index.md` plus two sample pages (`getting-started.md`,
  `about.md`)
- a `.claude/` folder with:
  - a `CLAUDE.md` explaining how to author Neko docs
  - a `skills/` folder with one `SKILL.md` per component, covering every
    syntax form and attribute

### How it ships in the CLI

At build time the `ZipNekoTemplate` target in `Neko/Neko.csproj` zips the
entire `.template/` folder into `obj/<config>/<tfm>/template.zip` and embeds
it in the `Neko` assembly under the manifest name
`Neko.Resources.template.zip`. The `neko new` command
(`Neko/Builder/NewCommand.cs`) reads that resource and extracts it into the
target directory (current directory by default; `--path X` to choose another).

Two things to watch out for if you touch the wiring:

- MSBuild's `**\*` glob **does not** match hidden directories. The
  `NekoTemplateFile` item in the csproj therefore includes
  `.template\.claude\**\*` explicitly so that edits to skills invalidate the
  incremental build.
- The zip is created with `<ZipDirectory>` (built-in MSBuild task). Run
  `dotnet build` once to (re-)generate it before invoking `neko new`.

**Always keep `.template/` in sync with the engine.** When you add, remove, or
change a component / config key:

1. Update the relevant `Neko/Extensions/*.cs` (or `Neko/Configuration/*.cs`).
2. Update `Neko/Resources/templates.json` (mandatory — `AGENTS.md` rule).
3. Update `Neko.Documentation/components/<name>.md` so the official docs cover
   the change.
4. Update `.template/.claude/skills/<name>/SKILL.md` — the skill that teaches
   Claude how to use the component. **This is part of the change, not a
   follow-up task.**
5. If the change is broad enough that the starter pages should demonstrate
   it, update `.template/index.md`, `getting-started.md`, or `about.md` too.

For brand-new components, create a new skill file at
`.template/.claude/skills/<name>/SKILL.md`. Follow the existing format:
frontmatter (`name`, `description`) plus syntax, attributes, examples, and
tips.

## Development workflow

### Build

```bash
dotnet build Neko.sln
```

### Run the dev server against the official docs

```bash
dotnet run --project Neko/Neko.csproj -- watch \
  --input Neko.Documentation \
  --output /tmp/test_out
```

Always point `--output` at a **temp folder**, never inside the repo. See
`AGENTS.md` for the rule about avoiding committed test artifacts.

### Tests

- Unit tests: `dotnet test Neko.Tests`.
- Playwright tests: also under `Neko.Tests`. They generate the documentation
  site into a temp folder and check rendered HTML.
- **Documentation pages must be validated with Playwright** when components
  change. See `AGENTS.md`.

### Adding a new component — checklist

- [ ] Markdig extension under `Neko/Extensions/<Name>Extension.cs`
- [ ] Wired into the pipeline (search for existing extensions' `Setup` calls)
- [ ] Entry in `Neko/Resources/templates.json` (snippet for the editor)
- [ ] Documentation page `Neko.Documentation/components/<name>.md` (with
      frontmatter, syntax, attribute table, examples)
- [ ] Linked from `Neko.Documentation/components/components.md`
- [ ] Unit tests in `Neko.Tests`
- [ ] Playwright test that opens the generated page and verifies render
- [ ] **Skill** at `.template/.claude/skills/<name>/SKILL.md`
- [ ] Optional: example use in `.template/index.md`
- [ ] Changelog entry in `Neko.Documentation/changelog/vYY.M.md` (current month)

## Code style and architectural rules

(Recap of `AGENTS.md` — read that file for the authoritative list.)

- Modern C# 10+, standard .NET naming conventions.
- `System.CommandLine` for CLI parsing.
- `Markdig` for Markdown.
- `Kestrel` for the dev server.
- Embedded resources for CSS/JS/fonts/icons (no Node.js, no NPM at runtime).
- Vanilla JS only — no React/Vue/Angular.
- Icons: Flaticon UIcons (Regular Rounded). Octicons are gone.
- License attributions live in `Neko/Resources/sources.md`.

## Changelog policy

The Neko changelog uses Neko's own **folder-based changelog** feature
(`Neko.Documentation/configuration/changelog.md`). It lives at
`Neko.Documentation/changelog/`:

- `index.yml` marks the folder with `changelog: true` plus `title` / `description`.
- Each release is **its own Markdown file named after the version**, using
  **Calendar Versioning** `vYY.M` (e.g. `changelog/v26.6.md`). Neko parses the
  file name as a version, sorts newest-first, and renders the `/changelog` page.

Each version file is authored as **sections + entries**:

- Sections are `#` headings prefixed with an icon shortcode
  (`# :icon-sparkles: Features`, `# :icon-bug: Fixes`, …).
- Each change is a `::: change {badge="New" title="…"}` block — the badge renders
  in a left column, vertically aligned, with the title + description next to it.
- The `link:` frontmatter key links the sticky version header to the **latest
  Neko NuGet package of that month** (`https://www.nuget.org/packages/Neko/<YY>.<M>.<build>`).
  When unsure of the build, query the NuGet registration index
  (`https://api.nuget.org/v3/registration5-gz-semver2/neko/index.json`) and pick
  the highest build for that `YY.M`.

When you make a change, **add a `::: change` entry to the right section of the
file for the current month's `vYY.M` version** (create `changelog/vYY.M.md` if it
doesn't exist yet, copying the `date:`/`link:` frontmatter convention from the
sibling files). Don't replace existing entries; merge new ones in, preserving
structure depth. Never recreate a single `changelog.md` — each release is a
separate file. The authoring format is documented in the `changelog` skill
(`.template/.claude/skills/changelog/SKILL.md`).

## Quick reference

- Top-level agent rules: [`AGENTS.md`](../AGENTS.md)
- Authoring Neko docs (template): [`.template/.claude/CLAUDE.md`](../.template/.claude/CLAUDE.md)
- Component skills (template): [`.template/.claude/skills/`](../.template/.claude/skills/)
- Public docs: <https://neko.curiosity.ai/>
- GitHub: <https://github.com/curiosity-ai/neko/>
