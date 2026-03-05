---
title: Workflow
description: Create visual workflows with blocks and arrows.
icon: share
---

# Workflow

The Workflow component allows you to create visual diagrams showing a series of blocks and connecting lines, perfect for architectural diagrams, data pipelines, or step-by-step processes.

## Syntax

Create a fenced code block with the `workflow` language modifier. Inside the block, define nodes and connections.

- **Nodes**: `nodeId: Title | Icon | Badge`
- **Descriptions**: `nodeId.description: A longer text that is revealed on click.`
- **Connections**: `nodeId --> anotherNodeId`

Nodes without a title will use their `nodeId` as the title. The pipeline will automatically arrange nodes into columns based on their connections.

## Example

```workflow
dataset: Dataset | book-alt | 2
dataset.description: Datasets can either be statically uploaded, or dynamically synchronized to external systems via Data connections.

pipeline: Pipeline | code-branch
ontology: Ontology | cube | 6

function: Function | diagram-nested
application: Application | browser | 2

dataset --> pipeline
pipeline --> ontology
ontology --> function
ontology --> application
```
