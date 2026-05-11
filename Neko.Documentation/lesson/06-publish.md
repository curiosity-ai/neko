---
title: Publish to GitHub Pages
order: 6
kind: Project
duration: 15 min
---

# Publish to GitHub Pages

Build the static site to the `docs/` folder of your repo:

```bash
neko build --input . --output ./docs
```

Commit the result and enable **GitHub Pages** for the `docs/` branch &mdash; you now have a published documentation site. For a custom domain, set `cname:` in your `neko.yml`.

Congratulations &mdash; you've completed the Neko track! 🎉
