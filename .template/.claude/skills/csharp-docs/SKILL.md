---
name: csharp-docs
description: Render C# API docs from a code block with XML doc comments (`/// <summary>`, `<param>`, `<returns>`, etc.). Use to embed API reference inline without a separate generator.
---

# C# docs

Renders a C# code snippet *and* a formatted API reference extracted from XML
doc comments (`/// <summary>`, `<param>`, `<returns>`, `<remarks>`,
`<typeparam>`, `<exception>`). Backed by Roslyn at build time.

## Syntax

````markdown
```csharp-docs
/// <summary>
/// Calculates the age in whole years on a given date.
/// </summary>
/// <param name="dateOfBirth">Person's date of birth.</param>
/// <param name="date">The target date.</param>
/// <returns>Age in years.</returns>
public static int AgeAt(DateOnly dateOfBirth, DateOnly date)
{
    var age = date.Year - dateOfBirth.Year;
    if (date < dateOfBirth.AddYears(age)) age--;
    return age;
}
```
````

Neko parses the code, pulls out the doc comments, and renders a tidy API
reference section.

## Layout

When the block wraps an enclosing type (class / struct / interface / record /
enum), the page is split in two:

1. A **sticky header** with the type kind badge ("class", "interface", …),
   the type name (with a small permalink icon after it), the signature, and
   the summary. The header stays pinned to the top of the `csharp-docs`
   section while the visitor scrolls through the rest of the API.
2. The type's documented members grouped by kind, in this order:
   **Constructors → Properties → Methods → Events → Fields**. Each member is
   prefixed with a kind badge ("Constructor", "Method", "Property", …) and
   qualified with the parent type, e.g. `DetailsList.OnColumnClick`.

The parent class body is **not** included in the type signature — only the
modifiers, keyword, identifier, type parameters, base list, and constraint
clauses. Member signatures have their whitespace normalized at render time,
so column-aligned source (`public void        OnColumnClick()`) shows as
`public void OnColumnClick()`.

For fragments without an enclosing type (e.g. a single method like the
example above), members render inline in source order without the grouping.

## Supported XML tags

- `<summary>` — short description.
- `<remarks>` — long-form notes.
- `<param name="…">` — per-parameter docs.
- `<returns>` — return value.
- `<typeparam name="…">` — generic type parameters.
- `<exception cref="…">` — thrown exceptions.
- `<example>` — usage examples.

## Tips

- The fenced block must be `csharp-docs`, not `csharp`.
- Include the full method/class signature so Roslyn can resolve parameter names.
- For larger types use [`code-snippet`](../code-snippet/SKILL.md) with a `region`.
- Keep examples self-contained; missing `using` directives are OK in the doc
  block — only doc comments are parsed, not compilation.
