---
name: workflow
description: Render a workflow diagram from a node-and-arrows DSL — labelled boxes with icons and optional badges, auto-arranged in columns. Use for pipelines, data flows, sign-off chains, ETL diagrams.
---

# Workflow

A higher-level diagram with named nodes (each carrying a title, icon, and
optional badge) and directional connections.

## Syntax

````markdown
```workflow
nodeId: Title | icon-name | badge-count
nodeId.description: Long description revealed when the node is clicked

other: Other node | gear

nodeId --> other
```
````

## Node syntax

```
nodeId: Title | icon-name | badge-count
```

- `nodeId` — unique id used in connections.
- `Title` — visible label.
- `icon-name` — UIcon name (no `icon-` prefix).
- `badge-count` — optional number shown as a small badge.

Add an optional description line for the click-to-expand details:

```
nodeId.description: A longer multi-sentence description.
```

## Connections

```
fromId --> toId
fromId --> midId --> endId
```

## Example

````markdown
```workflow
dataset:  Dataset   | book-alt      | 2
pipeline: Pipeline  | code-branch
function: Function  | diagram-nested
warehouse: Warehouse | database

dataset.description: Raw rows from upstream sources.
pipeline.description: Normalises, validates, and enriches each batch.

dataset --> pipeline
pipeline --> function
pipeline --> warehouse
```
````

## Tips

- Keep node titles to 1-3 words.
- Use icons from the UIcons set — same names as the [`icon`](../icon/SKILL.md)
  shortcode but without the `icon-` prefix.
- For very free-form relationship maps, prefer
  [`force-graph`](../force-graph/SKILL.md). For sequence/class/ER, prefer
  [`mermaid`](../mermaid/SKILL.md).
