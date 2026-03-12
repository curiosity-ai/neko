---
title: C# Docs
order: 8
icon: file-code
---
# C# Documentation Block

Neko supports an automated way to document your C# APIs directly within your Markdown using a special `csharp-docs` code block modifier. This allows you to generate beautiful, DocFx-like layouts for your C# classes, methods, properties, and more, all from a standard code block.

Neko uses Roslyn to parse your code and extracts standard XML documentation tags like `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<typeparam>`, and `<exception>`.

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
