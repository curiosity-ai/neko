---
name: force-graph
description: Render an interactive force-directed graph from a simple node/link DSL or JSON. Use to visualise relationships, dependencies, knowledge graphs, mind maps.
---

# Force graph

Renders an interactive, draggable, zoomable force-directed graph. Two input
formats are supported: a concise arrow-based DSL and a more explicit JSON form.

## Arrow DSL

````markdown
```force-graph
User --> System
System --> Database
Database --> Backup
```
````

Labelled edges:

````markdown
```force-graph
User -- logs in --> System
System -- authenticates --> Database
```
````

## JSON form

````markdown
```force-graph
{
  "nodes": [
    { "id": "user", "name": "User", "val": 5 },
    { "id": "api",  "name": "API",  "val": 8 }
  ],
  "links": [
    { "source": "user", "target": "api", "label": "calls" }
  ]
}
```
````

## Node and link fields

- `id` — unique node id (required).
- `name` — display label.
- `val` — relative size weight.
- `color`, `group` — optional styling/grouping.
- `source` / `target` — link endpoints (must match node ids).
- `label` — optional edge label.

## Tips

- Keep graphs under ~50 nodes for readability.
- The graph is interactive — users can drag nodes and zoom; nothing extra
  needed.
- For diagrams with strict layout (sequence, flowchart, gantt, ER), prefer
  [`mermaid`](../mermaid/SKILL.md). Force-graph is best for unstructured
  relationship maps.
