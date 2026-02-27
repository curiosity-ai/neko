---
title: "Embedding Video Content with YouTube"
description: "Learn how to embed YouTube videos seamlessly into your Neko documentation."
author: "Neko Team"
date: "2023-11-08"
authorImage: "https://github.com/github.png"
cover: "https://picsum.photos/seed/youtube/800/400"
layout: post
---

# Embedding Video Content with YouTube

Documentation often needs visual aids. While images are great, sometimes a video is the best way to explain a complex topic.

Neko makes embedding **YouTube** videos trivial.

## Syntax

Simply paste the full YouTube URL on its own line.

```markdown
https://www.youtube.com/watch?v=dQw4w9WgXcQ
```

Neko automatically detects this and converts it into a responsive iframe embed.

## Time Stamps

You can specify a start time using the `t` parameter in the URL.

```markdown
https://youtu.be/dQw4w9WgXcQ?t=43
```

## Responsive Design

The embedded video player is automatically responsive, scaling correctly on mobile, tablet, and desktop devices. No custom CSS required!

Enhance your documentation with rich media today.
