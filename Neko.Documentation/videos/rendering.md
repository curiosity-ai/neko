---
title: Rendering to MP4
label: Rendering to MP4
description: neko render films a video page with headless Chromium (Playwright) and encodes it with ffmpeg — options, the soundtrack, quality, parallel capture, stills and CI.
order: 7
icon: film
---

# Rendering to MP4

```bash
neko render videos/launch.md                 # → videos/launch.mp4
neko render videos/launch.md -o launch.mp4 -q high
neko render videos/ -o out/                  # every video page in the folder
neko render videos/launch.md --still 4.5     # one PNG of that moment
```

## What happens

1. **Build.** Neko builds the page exactly as `neko build` would — the same
   parser, the same timeline compiler, the same standalone HTML — but only that
   page, and in memory.
2. **Open.** Headless Chromium (through Playwright) opens it at the video's own
   size with a device scale factor of 1, in render mode: no player chrome, the
   canvas at 1:1. The page is served from memory; anything it asks for that it
   did not inline (a clip) is served from the project folder. Neko waits for the
   fonts, every image's decode and every clip.
3. **Capture.** For frame *n* Neko calls `window.nekoVideo.seek(n / fps)` and
   takes a screenshot. Several browser pages capture at once, each a slice of
   the timeline (`--workers`).
4. **Encode.** ffmpeg turns the frames into H.264 in BT.709 (`yuv420p`,
   `+faststart`), and mixes the soundtrack: the music bed, each scene's voice
   line and the [voice-over](voice-over.md) track, each delayed to its time, the
   music faded in and out.

A video with a voice-over has one more step before the build: every sentence
without a take is synthesized first (see [Voice-over](voice-over.md)), because a
take's length is its sentence's place on the clock.

The page protocol is small and public, so anything that can load a page and call
a function can drive it:

```js
await window.nekoVideo.ready;          // fonts, images and clips are in
window.nekoVideo.duration;             // seconds
window.nekoVideo.renderMode();         // strip the chrome, canvas at 1:1
await window.nekoVideo.seek(12.5);     // paint 12.5 s (resolves once clips have seeked)
```

## Options

| Option | Meaning |
| --- | --- |
| `-o`, `--output` | The file to write (or the folder, for a folder input). Defaults to beside the source, same name. |
| `-w`, `--workers` | Browser pages capturing in parallel. Defaults to half the cores (1–6). |
| `-q`, `--quality` | `draft` (ultrafast, CRF 28), `standard` (medium, CRF 18), `high` (slow, CRF 15). |
| `--format` | `mp4` (H.264 + AAC) or `webm` (VP9 + Opus). |
| `--fps` / `--size` | Override the front matter for this render: `--size 720p`, `--size vertical`. |
| `--from` / `--to` | Render a range, in seconds. |
| `--still <s>` | Write one PNG of that moment — posters, docs images, checking a frame. |
| `--jpeg` | Capture JPEG frames: faster, slightly softer before encoding. |
| `--keep-frames` | Keep the captured frames and print their folder. |
| `--no-audio` | Leave the soundtrack out. |
| `--no-voiceover` | Do not synthesize the voice-over; sentences without a take stay silent. |
| `--preview` | Print the timing sheet — scenes, animation and voice-over on one clock — and render nothing. |
| `--theme` | Re-theme the video: `neko` or `curiosity`. |
| `--browser` | A Chromium or Chrome executable to use instead of Playwright's. |
| `--ffmpeg` | ffmpeg (or its folder) to use. |

## ffmpeg

Neko does not bundle ffmpeg. It looks, in order, for:

1. `--ffmpeg`;
2. the `NEKO_FFMPEG` environment variable;
3. the `Curiosity.FFmpeg.Runtimes.<rid>` layout (`runtimes/<rid>/native/ffmpeg-dist`)
   next to the Neko binaries;
4. `ffmpeg` on the `PATH`;
5. a copy it downloaded before, in `~/.neko/ffmpeg`;

and failing those it downloads the official build into `~/.neko/ffmpeg` once,
with Xabe.FFmpeg's downloader. Neko drives ffmpeg through Xabe.FFmpeg.

## Chromium

`neko render` uses Playwright's Chromium. When Playwright's own build is
missing it uses any Chromium Playwright already installed
(`PLAYWRIGHT_BROWSERS_PATH`, `~/.cache/ms-playwright`), and otherwise installs it
once. `--browser` points at any other Chromium or Chrome — the way to get H.264
playback for `::: media` clips, which open-source Chromium does not decode.

## Speed

Capture is most of the time; the encode is a few seconds. On four cores the
35-second sample above captures its 1,062 frames in about 50 seconds with four
workers. `--jpeg` and `-q draft` make a quick check faster; `--from`/`--to` and
`--still` render only what you are working on.

## In CI

```yaml
- run: dotnet tool install -g Neko
- run: npx playwright install --with-deps chromium   # or let neko render install it
- run: sudo apt-get install -y ffmpeg                  # or let neko render download it
- run: neko render videos/ -o site/assets/videos -q high
```

Render before `neko build` and embed the files with `[!video]`:

```markdown
[!video src="/assets/videos/launch.mp4" link="/videos/launch" title="Launch" poster="/assets/videos/launch.png"]
```

| Attribute | Meaning |
| --- | --- |
| `src` | The MP4 (or WebM). |
| `link` | The video page, opened by the card's link — the composition you can scrub. |
| `title` / `description` | The card's title bar and caption. |
| `poster` | The image shown before playing (`neko render --still`). |
| `ratio` | The player's aspect ratio. `16:9` by default. |
| `autoplay` | Plays muted and loops — for short loops. |
