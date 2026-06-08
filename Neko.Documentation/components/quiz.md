---
icon: graduation-cap
tags: [component]
---
# Quiz

A `quiz` block is a self-scoring multiple-choice comprehension check. The reader
selects answers, clicks **Check answers**, and sees which were right along with
an optional explanation per question. Scoring and "answered" state are handled
in the browser and stored in `localStorage` — there is no backend.

Use it to close a tutorial or lesson page with a quick knowledge check.

## Component syntax

Like a code block, a quiz uses a `` ``` `` fence with the `quiz` specifier. The
body is YAML.

~~~ Sample quiz
```quiz
title: Check yourself
questions:
  - q: "Why include a volume mount when starting the container?"
    options:
      - "To expose the HTTP port"
      - "So ingested data survives a restart"
      - "To set the admin password"
    answer: 1
    explain: "Without the mount, data is lost when the container is recreated."
  - q: "Which retrieval modes are available out of the box?"
    options:
      - "Text"
      - "Vector"
      - "Hybrid"
    answers: [0, 1, 2]
    explain: "Text, vector, and hybrid are all available without configuration."
```
~~~

## Fields

| Field | Level | Notes |
| --- | --- | --- |
| `title` | quiz | Optional heading. Defaults to "Check yourself". |
| `id` | quiz | Optional stable id for `localStorage`. If omitted, a deterministic id is derived from the question text. |
| `questions` | quiz | List of questions. Required. |
| `q` | question | The question text. |
| `options` | question | List of answer options shown in order. |
| `answer` | question | Index (0-based) of the single correct option. Renders radio buttons. |
| `answers` | question | List of indices (0-based) for multiple correct options. Renders checkboxes and shows "(select all that apply)". |
| `explain` | question | Optional explanation revealed after checking. |

Provide either `answer` (single) or `answers` (multiple) per question, not both.

## Behaviour

- **Check answers** grades every question, highlights correct and incorrect
  options, and reveals explanations.
- **Reset** clears the selections and grading.
- The score is stored per-browser. State the limitation if you rely on it — it
  is per-device and cleared with the cache, not an authoritative record.

## Tips

- Keep questions focused on understanding, not recall of exact UI labels.
- Put one quiz at the end of a page; pair it with the [Lesson](/components/) flow
  for multi-page tracks.

## Live Demo

Try it out yourself! Visit the [quiz sample page](/samples/quiz.md).
