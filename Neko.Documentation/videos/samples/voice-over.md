---
title: A voice-over, written in Markdown
description: A short Neko video with a voice-over track — the narration is written apart from the scenes, laid on the video's clock, and spoken by Voxwerk.
video:
  theme: neko
  accent: violet
  transition: push
  back: /videos/voice-over
  narration:
    voice: "A warm, clear voice of a professional narrator in her thirties, calm and friendly, speaking at an even, unhurried pace."
    language: english
---

--- {id="opening" layout="title" background="aurora" duration="6"}

::: lockup {kicker="Neko video mode" label="Voice-over"}
# Say it *in Markdown*
:::

--- {id="track" duration="6.5"}

## The narration is *its own track*

Scenes animate on their own clock.

The voice-over runs on the video's. {#own-clock}

--- {id="anchors" duration="9.5" transition="zoom"}

## Lines land on *anchors*

- A time {enter="left"}
- A scene {enter="left"}
- An element {enter="left" #element}

--- {id="preview" duration="6"}

::: terminal {title="zsh"}
```bash
$ neko voiceover videos/launch.md --preview
✓ 5 lines · 9 sentences on one clock
```
:::

--- {id="close" layout="title" background="glow" duration="5"}

# Preview. Align. *Render.*

--- {narration}

# The script: each paragraph is a line, each line a list of sentences.

{at="opening+0.6"}
Every Neko video can carry a voice-over, written in the same Markdown file.

{at="track"}
The narration is a track of its own. It never moves the animation.

{at="#element"}
Each line starts at an anchor: a time, a scene, or an element on screen.

{at="preview+0.3"}
Preview the timings first, then synthesize the voice with Voxwerk.

{at="close"}
Then render. Picture and voice, in step.
