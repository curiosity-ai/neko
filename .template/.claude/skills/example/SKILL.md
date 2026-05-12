---
name: example
description: Side-by-side prose + code layout (description on the left, code on the right). Use to explain a code sample without losing the example next to its explanation.
---

# Example

Renders a two-column layout with a descriptive text block on the left and one
or more code blocks on the right. The right column is sticky on desktop and
stacks below the description on mobile.

## Syntax

````markdown
::: example
Description of the example, supporting **bold**, _italic_, lists, and links.

```js
function greet(name) {
  return `Hello, ${name}`;
}
```
:::
````

## With multiple snippets

````markdown
::: example
A `User` is created and then greeted.

```ts
const user = { name: "Sam" };
```

```ts
console.log(greet(user.name));
```
:::
````

## Tips

- Keep the description focused — 1-3 short paragraphs is ideal.
- The code on the right is fenced exactly like a standard
  [`code-block`](../code-block/SKILL.md); titles and line numbers still work.
- Use multiple `example` blocks back-to-back rather than cramming several
  unrelated snippets into one.
