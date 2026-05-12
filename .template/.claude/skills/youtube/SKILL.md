---
name: youtube
description: Embed a YouTube video by pasting its URL on its own line. Neko auto-detects watch/youtu.be/embed forms and honours timestamp, autoplay, loop, and mute parameters.
---

# YouTube

Neko detects YouTube URLs on their own line and embeds them with a responsive
player. No special component syntax is required.

## Plain embed

```markdown
https://www.youtube.com/watch?v=dQw4w9WgXcQ
https://youtu.be/dQw4w9WgXcQ
```

## With timestamp

```markdown
https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s
https://youtu.be/dQw4w9WgXcQ?t=45
```

## With player options

```markdown
https://www.youtube.com/watch?v=dQw4w9WgXcQ&autoplay=1&loop=1&mute=1
https://www.youtube.com/watch?v=dQw4w9WgXcQ&start=30&end=60
```

Supported parameters (passed straight to the YouTube player):

- `t` / `start` — start time in seconds (or `30s`, `1m20s`).
- `end` — end time in seconds.
- `autoplay=1` — auto-start (browsers usually require `mute=1` too).
- `loop=1` — loop playback.
- `mute=1` — mute by default.
- `controls=0` — hide native controls.

## Tips

- The URL **must be on its own line**. Inline YouTube links remain plain
  hyperlinks.
- For non-YouTube embeds (Vimeo, CodePen, your own iframe), use
  [`embed`](../embed/SKILL.md).
- Provide context for the video in the surrounding text — embedded videos
  often need a caption.
