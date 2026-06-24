---
name: mermaid
description: Render a Mermaid diagram (flowchart, sequence, class, ER, state, gantt, journey, mindmap, sankey, XY, architecture) from a fenced ```mermaid``` block. Use for any structured diagram in docs.
---

# Mermaid

Renders Mermaid diagrams at build time. Supports the full Mermaid v10+
catalogue.

## Basic flowchart

````markdown
```mermaid
graph LR
    A[Start] -->|Process| B[End]
```
````

## Decision tree

````markdown
```mermaid
graph TD
    A[Login] --> B{Valid?}
    B -->|Yes| C[Dashboard]
    B -->|No| D[Show error]
```
````

## Sequence diagram

````markdown
```mermaid
sequenceDiagram
    User->>+API: POST /login
    API->>+DB: SELECT user
    DB-->>-API: row
    API-->>-User: 200 OK
```
````

## Other supported types

- `classDiagram` — class structure.
- `erDiagram` — entity relationships.
- `stateDiagram-v2` — state machines.
- `gantt` — project schedules.
- `journey` — user journeys.
- `mindmap` — radial idea maps.
- `sankey-beta` — flow diagrams.
- `xychart-beta` — quick bar / line charts.
- `architecture-beta` — cloud architecture.

## Theming

Diagrams adopt the site palette automatically — Neko renders them with
Mermaid's `base` theme fed the resolved `theme` colours + accent ramp from
`neko.yml`, in both a light and a dark variant. No directive needed.

## Theme override

To opt a diagram out of the brand palette and use a stock Mermaid theme, set
your own `theme` in the init directive. Neko then renders that diagram
verbatim (no brand variables applied):

````markdown
```mermaid
%%{init: { 'theme': 'forest' }}%%
graph LR
    A --> B
```
````

## Tips

- Keep node labels short.
- Use `graph LR` (left-to-right) for processes; `graph TD` (top-down) for
  hierarchies and decisions.
- For freeform relationship graphs without a fixed layout, use
  [`force-graph`](../force-graph/SKILL.md).
- For step-by-step processes, prefer [`workflow`](../workflow/SKILL.md) — it
  has a richer "lane" layout.
