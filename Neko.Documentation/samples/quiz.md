---
title: Quiz Sample
---
# Quiz Sample

This page demonstrates a live `quiz` component. Pick an answer for each
question and press **Check answers** to grade and reveal explanations. Your
score is saved locally in this browser.

```quiz
title: Neko knowledge check
questions:
  - q: "Which fenced code block info renders an interactive quiz?"
    options:
      - "```check"
      - "```quiz"
      - "```survey"
    answer: 1
    explain: "A ```quiz fenced block whose body is YAML defines a quiz."
  - q: "Where is the score stored?"
    options:
      - "On the Neko server"
      - "In a cookie sent on every request"
      - "In the browser's localStorage"
    answer: 2
    explain: "Scoring is fully client-side and persisted per-browser in localStorage. There is no backend."
  - q: "Which fields can mark correct option(s)? (select all that apply)"
    options:
      - "answer (single index)"
      - "answers (list of indices)"
      - "correct"
    answers: [0, 1]
    explain: "Use answer for a single correct option (renders radios), or answers for multi-select (renders checkboxes)."
```
