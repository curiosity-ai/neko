---
title: C# Docs
order: 8
icon: file-code
---
# C# Documentation Block

Neko supports an automated way to document your C# APIs directly within your Markdown using a special `csharp-docs` code block modifier. This allows you to generate DocFx-like layouts for your C# classes, methods, properties, and more, all from a standard C# code block.

Neko uses Roslyn to parse your code and extracts standard XML documentation tags like `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<typeparam>`, and `<exception>`.

## Layout

When the block contains an enclosing type declaration (class / struct / interface / record / enum), Neko renders:

1. A **sticky header** with the type kind (class, interface, …), the type name, its signature, and its summary. The header stays pinned to the top of the `csharp-docs` area as the visitor scrolls through the rest of the section.
2. The type's documented members grouped by kind, in this order: **Constructors → Properties → Methods → Events → Fields**. Each member is prefixed with a small kind badge (Constructor, Method, Property, …) and its name is qualified with the parent type (e.g. `DetailsList.OnColumnClick`).

Permalink anchors are rendered as a small link icon *after* each name. Signature whitespace is normalized at render time, so column-aligned source code (`public void        OnColumnClick()`) is shown collapsed to a single space.

For fragments without an enclosing type (e.g. the single-method example below), members are rendered inline in source order.

## Usage

Simply create a code block with the language set to `csharp-docs` and drop in your standard C# code containing XML comments.

```csharp
    ```csharp-docs
    /// <summary>
    /// Calculates the age of a person on a certain date based on the supplied date of birth.
    /// Takes account of leap years, using the convention that someone born on 29th February
    /// in a leap year is not legally one year older until 1st March of a non-leap year.
    /// </summary>
    /// <param name="dateOfBirth">Individual's date of birth.</param>
    /// <param name="date">Date at which to evaluate age at.</param>
    /// <returns>Age of the individual in years (as an integer).</returns>
    /// <remarks>This code is not guaranteed to be correct for non-UK locales.</remarks>
    public static int AgeAt(this DateOnly dateOfBirth, DateOnly date)
    {
        int age = date.Year - dateOfBirth.Year;
        return dateOfBirth > date.AddYears(-age) ? --age : age;
    }
    ```
```

## Example

Here is how the above snippet renders:

```csharp-docs
/// <summary>
/// Calculates the age of a person on a certain date based on the supplied date of birth.
/// Takes account of leap years, using the convention that someone born on 29th February
/// in a leap year is not legally one year older until 1st March of a non-leap year.
/// </summary>
/// <param name="dateOfBirth">Individual's date of birth.</param>
/// <param name="date">Date at which to evaluate age at.</param>
/// <returns>Age of the individual in years (as an integer).</returns>
/// <remarks>This code is not guaranteed to be correct for non-UK locales.</remarks>
public static int AgeAt(this DateOnly dateOfBirth, DateOnly date)
{
    int age = date.Year - dateOfBirth.Year;
    return dateOfBirth > date.AddYears(-age) ? --age : age;
}
```

## Supported Tags

Neko currently looks for the following XML documentation tags:
* `<summary>`
* `<param>`
* `<returns>`
* `<remarks>`
* `<typeparam>`
* `<exception>`
