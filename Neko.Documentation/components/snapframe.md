---
title: SnapFrame Component
icon: camera
tags: [component, screenshot]
---
# SnapFrame

The `snapframe` extension allows you to easily embed automated screenshots of external websites directly into your documentation. Under the hood, it uses the [SnapFrame .NET tool](https://www.nuget.org/packages/SnapFrame) to navigate to the specified URL and capture a screenshot.

If the image file does not exist locally during the documentation build, Neko will automatically invoke the CLI to generate it and save it to your assets folder.

## Basic Syntax

To use it, add the `[!snapframe]` tag on a line *immediately before* the Markdown image block. You can pass the URL and any optional parameters accepted by the `snapframe capture` CLI command.

```md
[!snapframe https://neko.curiosity.ai]
![Screenshot of Neko Documentation](/assets/screenshots/neko-screenshot.png)
```

[!snapframe https://neko.curiosity.ai]
![Screenshot of Neko Documentation](/assets/screenshots/neko-screenshot.png)

## Optional Parameters

You can also pass additional CLI arguments inside the `[!snapframe]` tag to customize the screenshot (e.g., adding a browser window chrome or changing the background).

```md
[!snapframe https://neko.curiosity.ai --chrome macOS --bg GradientSunset]
![Neko with macOS Chrome and Sunset Background](/assets/screenshots/neko-styled.png)
```

[!snapframe https://neko.curiosity.ai --chrome macOS --bg GradientSunset]
![Neko with macOS Chrome and Sunset Background](/assets/screenshots/neko-styled.png)

Available options mirror the `snapframe capture` command:
* `--chrome <macOS|None|Windows11>`: Window chrome style.
* `--bg <Gradient|GradientAqua|GradientBreeze|GradientCandy|GradientMidnight|GradientPeach|GradientSunset|Transparent>`: Background style.
* `--full-page`: Capture the full scrollable page (not just the visible viewport).

## Command Execution

You can execute a series of commands on the page before taking the screenshot. Each line after the URL maps to one `snapframe` CLI call to execute a command on the page.

Supported commands are `click` and `interact`.

```md
[!snapframe http://localhost:8080/#/admin/endpoints
click 'sample button'
click 10 20
interact #elementId value='text to type']
![Curiosity Workspace Custom Endpoints](/assets/screenshots/workspace-customization/custom-endpoints.png)
```

## How It Works

When the documentation is being compiled:
1. Neko checks if the target image file exists (e.g., `/assets/screenshots/neko-screenshot.png`).
2. If it does not exist, Neko runs `snapframe navigate-json <url>` to get the page ready.
3. Neko runs `snapframe capture <page_id> <path> <options>`.
4. Finally, it closes the page. The image is now successfully created in the documentation folder!

Because Neko saves the captured image locally, subsequent builds will skip the capture step, ensuring fast compilation times.
