---
title: Blog Column
description: A three-column blog post layout.
icon: newspaper
order: 9
---

# Blog Column

The `blog-column` component displays a list of blog posts in a grid.

## Usage

```markdown
[!blog-column
    title="From the blog"
    subtitle="Learn how to grow your business with our expert advice."
    "Boost your conversion rate" "Illo sint voluptas. Error voluptates culpa eligendi. Hic vel totam vitae illo. Non aliquid explicabo necessitatibus unde." "Michael Foster" "Mar 16, 2020" "https://cdn2.thecatapi.com/images/20f.png"
    "How to use search engine optimization" "Optio cum necessitatibus dolor voluptatum provident commodi et. Qui aperiam fugiat nemo cumque." "Lindsay Walton" "Mar 10, 2020" "https://cdn2.thecatapi.com/images/db3.jpg"
    "Improve your customer experience" "Cupiditate maiores ullam eveniet adipisci in doloribus nulla minus. Voluptas iusto libero adipisci rem et corporis." "Tom Cook" "Feb 12, 2020" "https://cdn2.thecatapi.com/images/e12.jpg"
]
```

**Preview:**

[!blog-column
    title="From the blog"
    subtitle="Learn how to grow your business with our expert advice."
    "Boost your conversion rate" "Illo sint voluptas. Error voluptates culpa eligendi. Hic vel totam vitae illo. Non aliquid explicabo necessitatibus unde." "Michael Foster" "Mar 16, 2020" "https://cdn2.thecatapi.com/images/20f.png"
    "How to use search engine optimization" "Optio cum necessitatibus dolor voluptatum provident commodi et. Qui aperiam fugiat nemo cumque." "Lindsay Walton" "Mar 10, 2020" "https://cdn2.thecatapi.com/images/db3.jpg"
    "Improve your customer experience" "Cupiditate maiores ullam eveniet adipisci in doloribus nulla minus. Voluptas iusto libero adipisci rem et corporis." "Tom Cook" "Feb 12, 2020" "https://cdn2.thecatapi.com/images/e12.jpg"
]

## Attributes

| Attribute | Description |
| :--- | :--- |
| `title` | Main title. |
| `subtitle` | Subtitle text. |
| Positional Arguments | Groups of 5: Post Title, Description, Author Name, Date, Image URL. |
