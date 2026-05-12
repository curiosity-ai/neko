---
name: code-block
description: Format a fenced code block with syntax highlighting, optional title, line numbers, range highlighting, and macOS/Windows window chrome. Use whenever you write a multi-line code example.
---

# Code block

Neko extends fenced code blocks with titles, line numbering, range highlighting,
and decorative window chrome.

## Basic

````markdown
```js
const msg = "Hello, world";
console.log(msg);
```
````

## With a title

The first token after the language is interpreted as a title:

````markdown
```js main.js
console.log("title appears in the block header");
```
````

## Line numbers

A `#` on its own (or with a title) enables line numbers:

````markdown
```js #
line 1
line 2
```

```js main.js #
with title and line numbers
```
````

## Highlighted ranges

```` text
```js #2-3,5
line 1
line 2   ← highlighted
line 3   ← highlighted
line 4
line 5   ← highlighted
```
````

To highlight ranges **without** rendering line numbers, prefix with `!`:

````markdown
```js !#2-3
line 1
line 2   ← highlighted, no gutter
line 3   ← highlighted, no gutter
```
````

## Window chrome

```` text
```js chrome="mac" main.js
// macOS-style traffic-light header
```

```js chrome="windows" main.js
// Windows-style title bar
```
````

## Supported languages

Most popular languages are highlighted via highlight.js (C#, JS, TS, Python,
Go, Rust, Ruby, Java, Kotlin, Swift, HTML, CSS, JSON, YAML, TOML, XML, Bash,
PowerShell, SQL, etc.). Use `text` or `plain` for no highlighting.

## Tips

- Always specify a language for proper highlighting and a11y.
- Use a title (`main.cs`, `Dockerfile`, …) when the language alone is ambiguous.
- Keep individual snippets short. For files included from disk, prefer the
  [`code-snippet`](../code-snippet/SKILL.md) component.
