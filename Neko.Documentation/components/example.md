---
icon: browser
tags: [component]
---
# Example

The `Example` component provides a side-by-side layout designed specifically for demonstrating code next to descriptive text. On larger screens, the descriptive text displays in the left column, and the code blocks display in the right column with a sticky position. On smaller screens, they stack vertically.

To create an example component, wrap your content with `::: example`.

||| :icon-code-simple: Source
~~~
::: example
This is an example description. You can use standard markdown here like **bold text** or lists:
- Item 1
- Item 2

Then, simply include your code block in the same container. Neko will automatically parse the container and place the code on the right!

```js
function sayHello() {
  console.log("Hello, world!");
}
```
:::
~~~
||| :icon-play: Demo
::: example
This is an example description. You can use standard markdown here like **bold text** or lists:
- Item 1
- Item 2

Then, simply include your code block in the same container. Neko will automatically parse the container and place the code on the right!

```js
function sayHello() {
  console.log("Hello, world!");
}
```
:::
|||
