---
name: quiz
description: Render a self-scoring multiple-choice comprehension quiz from a ```quiz fenced block. Single- or multi-answer questions, optional per-question explanations, scored client-side and stored in localStorage. Use to close a lesson or tutorial page with a knowledge check.
---

# Quiz

A `quiz` is a fenced code block (` ```quiz `) whose body is YAML. The reader
picks answers, clicks **Check answers**, and sees which were correct plus an
optional explanation. Scoring and answered-state are client-side
(`localStorage`); there is no backend.

## Syntax

````markdown
```quiz
title: Check yourself
questions:
  - q: "Question text?"
    options:
      - "Option A"
      - "Option B"
      - "Option C"
    answer: 1
    explain: "Why B is correct."
  - q: "Select all that apply"
    options:
      - "First"
      - "Second"
      - "Third"
    answers: [0, 2]
    explain: "First and third are correct."
```
````

## Fields

| Field | Level | Notes |
| --- | --- | --- |
| `title` | quiz | Optional heading. Defaults to "Check yourself". |
| `id` | quiz | Optional stable `localStorage` id. Derived from question text if omitted. |
| `questions` | quiz | List of questions (required). |
| `q` | question | Question text. |
| `options` | question | Answer options, shown in order. |
| `answer` | question | 0-based index of the single correct option → radio buttons. |
| `answers` | question | 0-based indices of multiple correct options → checkboxes. |
| `explain` | question | Optional explanation revealed after checking. |

Use either `answer` or `answers` per question, not both.

## Behaviour

- **Check answers** grades and highlights correct/incorrect options and reveals
  explanations.
- **Reset** clears selections.
- Score is stored per-browser — not authoritative.

## Tips

- One quiz per page, usually at the end.
- Test understanding, not memorisation of exact UI labels.
- Indices are 0-based — the first option is `0`.
