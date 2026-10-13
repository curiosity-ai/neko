---
title: Video
description: Embed a rendered video — the MP4 neko render wrote — in a framed player, with a link to the live composition it came from.
icon: clapperboard-play
---

# Video

`[!video]` embeds a video file in a framed player. It is the counterpart of
[video mode](/videos/videos.md): write the video as Markdown, render it with
`neko render`, and show the result on any page — with a link back to the
composition, where a reader can scrub it frame by frame.

```markdown
[!video src="/videos/assets/neko-video.mp4" poster="/videos/assets/neko-video-poster.png" link="/videos/samples/neko-video" title="Write Markdown. Get video."]
```

[!video src="/videos/assets/neko-video.mp4" poster="/videos/assets/neko-video-poster.png" link="/videos/samples/neko-video" title="Write Markdown. Get video."]

## Attributes

| Attribute | Meaning |
| --- | --- |
| `src` | The video file (MP4 or WebM). Required. |
| `link` | A page to open from the card — usually the video page the file was rendered from. |
| `title` | Shown in the card's title bar. |
| `description` | A caption under the player. |
| `poster` | The image shown before playing. `neko render --still 2.5` writes one. |
| `ratio` | The player's aspect ratio: `16:9` (default), `9:16`, `1:1`, `4:3`. |
| `autoplay` | `true` plays the video muted, in a loop. |
| `loop` | `true` loops it. |
| `open-text` | The label of the link. Default: *Open the composition*. |

For an embed without the card, `[!embed el="video"](clip.mp4)` still works.
