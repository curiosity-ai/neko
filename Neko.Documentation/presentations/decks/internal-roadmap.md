---
title: Internal roadmap review
description: A short, password-protected deck showing how presentation mode reuses Neko's page encryption.
password: neko
presentation:
  eyebrow: Internal
  accent: amber
  back: /presentations/protecting-a-deck
---

# The deck behind the lock

Everything on this page — every slide, every heading, the document title — ships encrypted. Nothing readable reaches the generated HTML until someone types the password.

> The password for this demo deck is `neko`.

--- {eyebrow="How it works"}

## Same machinery as a protected page

A deck adds `password:` to its front matter exactly like any other Neko page, and gets the same AES-GCM payload, the same PBKDF2 key derivation, and the same session key cache.

The slides are the payload
:   The whole deck — slides, controls, the runtime's init call — is encrypted into `#content-container`. The static file holds a blob and an unlock form.

The chrome stays outside
:   The blueprint ground, progress rail and back control are page furniture, not content, so they render before the unlock and frame the prompt.

One unlock, many decks
:   Decks sharing a password share a derived key for the session, so a reader who unlocked one opens the next without a second prompt.

Never in the index
:   Presentations are excluded from `search.json` whether or not they carry a password — so is any protected page.

--- {eyebrow="Where to use it"}

## Good candidates

- A customer-specific walkthrough linked from a public docs page.
- An internal architecture review you want to share by URL, not by attachment.
- A pre-announcement deck that goes public later by deleting one front-matter line.

::: note {tone="limit"}
Client-side encryption keeps a deck out of crawlers, search and casual reading. It is not an access-control system: anyone with the password has the content, and the password travels with the link you send.
:::
