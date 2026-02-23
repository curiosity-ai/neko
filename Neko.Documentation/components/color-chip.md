---
icon: paint-brush
tags: [component]
nav:
  badge: NEW|info
---
# Color Chip

Neko includes a `color-chip` component to display color previews alongside their hex codes or names.

## Usage

Use the `[!color-chip]` syntax to render a color preview. The component accepts the color as a positional argument or a named attribute.

### Basic Example

||| Demo
[!color-chip #5495f1]
[!color-chip #28a745]
[!color-chip #ffc107]
[!color-chip #dc3545]
||| Source
```md
[!color-chip #5495f1]
[!color-chip #28a745]
[!color-chip #ffc107]
[!color-chip #dc3545]
```
|||

### Custom Text

You can customize the displayed text using the `text` attribute.

||| Demo
[!color-chip #5495f1 text="Primary Blue"]
[!color-chip #28a745 text="Success Green"]
||| Source
```md
[!color-chip #5495f1 text="Primary Blue"]
[!color-chip #28a745 text="Success Green"]
```
|||

### Named Attributes

You can also specify the color using the `color` attribute.

||| Demo
[!color-chip color="#6610f2" text="Indigo"]
||| Source
```md
[!color-chip color="#6610f2" text="Indigo"]
```
|||

## Usage in Tables

Color chips work great in tables for documenting color palettes.

||| Demo
| Color Name | Hex Code | Usage |
| --- | --- | --- |
| Primary | [!color-chip #5495f1] | Main brand color |
| Success | [!color-chip #28a745] | Success states |
| Warning | [!color-chip #ffc107] | Warning messages |
| Danger | [!color-chip #dc3545] | Error states |
||| Source
```md
| Color Name | Hex Code | Usage |
| --- | --- | --- |
| Primary | [!color-chip #5495f1] | Main brand color |
| Success | [!color-chip #28a745] | Success states |
| Warning | [!color-chip #ffc107] | Warning messages |
| Danger | [!color-chip #dc3545] | Error states |
```
|||
