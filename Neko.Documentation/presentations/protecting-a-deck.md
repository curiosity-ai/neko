---
title: Protecting a deck
label: Protecting a deck
description: Password-protected presentations, and what client-side encryption does and does not buy you.
order: 5
icon: lock
---

# Protecting a deck

A deck takes the same `password:` front-matter key as any other Neko page, and
gets the same treatment: the content is encrypted at build time with AES-GCM,
and the browser derives the key with PBKDF2 when a reader unlocks it.

```yaml
---
title: Internal roadmap review
password: neko
presentation:
  eyebrow: Internal
  accent: amber
---
```

See [Password protection](/guides/password-protection) for the full mechanism —
the site-wide `password:` in `neko.yml`, the `password: none` opt-out, and the
session key cache that stops a reader being asked twice.

## What gets encrypted

Everything that is content:

- Every slide, including headings, notes and embedded diagrams.
- The control bar and the deck runtime's init call.
- The document `<title>` and meta description, which fall back to the site
  defaults until the deck is unlocked.

What stays in the clear is page furniture with nothing to give away — the
blueprint ground, the progress rail and the back control. They frame the unlock
prompt, which is centred in the deck viewport rather than left in a corner.

[!deck link="/presentations/decks/internal-roadmap" title="Internal roadmap review" description="The demo deck below is locked. Its password is neko."]

## Decks are never indexed

Presentations are excluded from `search.json` whether or not they carry a
password, and they never appear in the sidebar. A locked deck is therefore
invisible three times over: not in search, not in navigation, and not readable
in the generated HTML.

## What this is not

!!! warning
Client-side encryption keeps a deck out of crawlers, out of search results and
out of casual reading. It is **not** an access-control system.
!!!

- Anyone with the password has the content, and the password usually travels in
  the same message as the link.
- The encrypted payload is public; the only thing protecting it is the strength
  of the password and the PBKDF2 work factor.
- There is no per-reader identity, no revocation and no audit trail.

For anything that needs those, put the deck behind your own authenticated
hosting and leave `password:` off.

## Working on a locked deck

`neko watch --no-password` clears both the site-wide and per-page passwords for
the session, so a local preview never gates a deck you're editing. The published
build is unaffected.

```bash
neko watch --input . --output /tmp/preview --no-password
```
