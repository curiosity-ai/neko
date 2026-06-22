---
title: C# Docs
order: 8
icon: file-code
---
# C# Documentation Block

Neko supports an automated way to document your C# APIs directly within your Markdown using a special `csharp-docs` code block modifier. This allows you to generate DocFx-like layouts for your C# classes, methods, properties, and more, all from a standard C# code block.

Neko uses Roslyn to parse your code and extracts standard XML documentation tags like `<summary>`, `<param>`, `<returns>`, `<remarks>`, `<typeparam>`, and `<exception>`.

## Layout

The layout follows the Microsoft Learn / DocFX convention for API reference pages. When the block contains an enclosing type declaration (class / struct / interface / record / enum), Neko renders:

1. A **sticky header** with the type kind (class, interface, …), the type name, its signature, and its summary. The header stays pinned to the top of the `csharp-docs` area as the visitor scrolls through the rest of the section.
2. A **Definition** block listing the type's `Namespace` (taken from the enclosing `namespace` declaration), its `Inheritance` chain (the base class, shown as `Base → Type`), and the interfaces it `Implements`. Rows are omitted when there is nothing to show.
3. For each member kind, a **summary table** (`Name` → anchor link, `Description`) followed by the full member documentation. Members are grouped in this order: **Constructors → Properties → Methods → Events → Fields**. Each member is prefixed with a small kind badge (Constructor, Method, Property, …) and its name is qualified with the parent type (e.g. `DetailsList.OnColumnClick`).

Permalink anchors are rendered as a small link icon *after* each name, and the summary-table names link to those anchors. Signature whitespace is normalized at render time, so column-aligned source code (`public void        OnColumnClick()`) is shown collapsed to a single space.

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

## Example: a full type

When the block declares a type inside a namespace, Neko renders the Definition
block and the per-group summary tables:

```csharp-docs
namespace Geometry
{
    /// <summary>
    /// An axis-aligned rectangle defined by its width and height.
    /// </summary>
    public class Rectangle : Shape, IEquatable<Rectangle>
    {
        /// <summary>Initializes a new rectangle with the given dimensions.</summary>
        /// <param name="width">The width, in pixels.</param>
        /// <param name="height">The height, in pixels.</param>
        public Rectangle(double width, double height) { }

        /// <summary>Gets the width of the rectangle, in pixels.</summary>
        public double Width { get; }

        /// <summary>Gets the height of the rectangle, in pixels.</summary>
        public double Height { get; }

        /// <summary>Computes the area of the rectangle.</summary>
        /// <returns>The area, in square pixels.</returns>
        public double Area() => Width * Height;

        /// <summary>Determines whether this rectangle equals another.</summary>
        /// <param name="other">The rectangle to compare against.</param>
        /// <returns><c>true</c> if the rectangles have the same dimensions.</returns>
        public bool Equals(Rectangle other) => false;
    }
}
```

## Overloads

Members that share a name but differ in signature are grouped, then rendered in
the **Microsoft Learn / DocFX** style:

1. One header and a stable permalink anchor for the method name (e.g.
   `#Client.Connect`).
2. An optional shared intro, taken from the standard `<overloads>` XML tag.
3. An **Overloads table** — one row per signature (disambiguated by its
   parameter types, e.g. `Connect(string, string, string)`) linking to that
   overload's section, with the overload's own `<summary>` beside it.
4. One **complete, self-contained section per overload**: its typed signature
   heading and anchor, the signature, its summary, and its own typed
   `Parameters` / `Returns` / `Exceptions` / `Remarks` blocks.

Parameters are documented **in full inside each overload** — shared parameters
are repeated by design, so every overload reads on its own and the layout scales
to any number of overloads.

```csharp-docs
namespace Demo
{
    /// <summary>The .NET client for a workspace.</summary>
    public class Client
    {
        /// <overloads>Opens an authenticated connection to a workspace.</overloads>
        /// <summary>Connects using an API token.</summary>
        /// <param name="endpoint">The workspace base URL.</param>
        /// <param name="token">An API token whose scopes gate access.</param>
        /// <param name="connectorName">A stable name recorded in audit logs.</param>
        public static Client Connect(string endpoint, string token, string connectorName) => null;

        /// <summary>Connects using a client certificate (mutual-TLS).</summary>
        /// <param name="endpoint">The workspace base URL.</param>
        /// <param name="clientCertificate">A certificate presented for mutual-TLS.</param>
        /// <param name="connectorName">A stable name recorded in audit logs.</param>
        public static Client Connect(string endpoint, X509Certificate2 clientCertificate, string connectorName) => null;
    }
}
```

## Supported Tags

Neko currently looks for the following XML documentation tags:
* `<summary>`
* `<overloads>` — shared description for an overload set (see [Overloads](#overloads))
* `<param>`
* `<returns>`
* `<remarks>`
* `<typeparam>`
* `<exception>`
