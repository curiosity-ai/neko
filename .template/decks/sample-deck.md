---
title: A Neko deck in one file
description: The sample presentation that ships with the Neko starter.
presentation:
  eyebrow: Neko starter
  back: /
---

# A whole deck, in one Markdown file

Slides are separated by a `---` rule. Everything else is the Markdown you
already write.

> A deck is a page whose front matter says `presentation: true`. There is
> nothing else to set up.

--- {eyebrow="The building blocks"}

## What a slide is made of

Heading
:   `#` for the deck title or a section divider, `##` for a slide headline.

Lead
:   The paragraph right after the heading. No markup — it is styled for you.

Claim
:   A blockquote. The one sentence the slide is actually making.

Rows
:   A definition list, exactly like this one.

--- {eyebrow="Side by side" accent="leaf"}

## Two-up columns

:::: cols
::: box {title="How to nest"}
- `:::: cols` on the outside, `::: box` inside.
- The outer container always needs **more colons** than the inner one.
- `{count="3"}` asks for three columns.
:::

::: box {title="Tones" tone="warn"}
- `tone="warn"` — amber, for the thing to plan for.
- `tone="stop"` — rose, for the thing that will bite.
- `tone="ok"` — green, for the thing that is settled.
:::
::::

::: note
`::: note {tone="limit"}` turns this aside rose, for a caveat that has to land.
:::

--- {eyebrow="Diagrams" accent="amber"}

## Bring your own SVG

::: figure {caption="An embed fence emits its contents verbatim." label="Three boxes: write, build, present."}
```embed
<svg viewBox="0 0 560 80">
  <g font-family="IBM Plex Mono, monospace" font-size="13" fill="#93AAC6">
    <rect x="2" y="12" width="160" height="56" rx="4" fill="none" stroke="#5FD0D8"/>
    <text x="82" y="46" text-anchor="middle" fill="#5FD0D8">one .md file</text>

    <text x="182" y="46" text-anchor="middle" font-size="16">&#8594;</text>

    <rect x="202" y="12" width="160" height="56" rx="4" fill="none" stroke="#2F5478"/>
    <text x="282" y="46" text-anchor="middle">neko build</text>

    <text x="382" y="46" text-anchor="middle" font-size="16">&#8594;</text>

    <rect x="402" y="12" width="156" height="56" rx="4" fill="none" stroke="#E9A94A"/>
    <text x="480" y="46" text-anchor="middle" fill="#E9A94A">a full-screen deck</text>
  </g>
</svg>
```
:::

--- {eyebrow="Getting around"}

## Moving through a deck

| Input | Action |
| --- | --- |
| `→` `Space` `PageDown` | Next slide |
| `←` `PageUp` | Previous slide |
| `Home` / `End` | First / last slide |
| `Esc` | Back out of the deck |

::: note
Read the `presentation` skill in `.claude/skills/presentation/` for every option,
component and attribute.
:::
