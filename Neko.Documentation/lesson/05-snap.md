---
title: Capture screenshots
order: 5
kind: Interactive
duration: 10 min
---

# Capture screenshots

Reference live screenshots with `[!snapframe]`. The image right after the directive is what Neko renders &mdash; and what `neko snap` captures via Playwright.

```markdown
[!snapframe https://example.com]
![Example homepage](/assets/example.png)
```

Run the capture pipeline once you've added a directive:

```bash
neko snap            # captures any missing screenshots
neko snap --all      # re-captures every screenshot, overwriting existing files
```

Capturing is a separate command on purpose: regular `neko build` stays fast and offline.
