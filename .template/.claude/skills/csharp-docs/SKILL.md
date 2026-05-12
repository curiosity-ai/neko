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

Neko parses the code, pulls out the doc comments, and renders both a tidy
summary section and the original source.

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
