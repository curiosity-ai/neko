---
icon: diagram-project
tags: [component]
---
# Force Graph

Neko supports rendering interactive force-directed graphs using [force-graph](https://github.com/vasturiano/force-graph). These graphs are defined inside Markdown code blocks with the `force-graph` specifier.

## Component syntax

To create a force graph, use a fenced code block with `force-graph` as the language specifier.

The syntax supports two types of relationships:

1. **Simple Link**: `Source --> Target`
2. **Labeled Link**: `Source -- Label --> Target`

### Simple Graph

~~~
```force-graph
A --> B
B --> C
C --> A
```
~~~

```force-graph
A --> B
B --> C
C --> A
```

### Labeled Graph

You can add labels to the links by placing text between the `--` and `-->` markers.

~~~
```force-graph
User -- logs in --> System
System -- authenticates --> Database
Database -- returns user --> System
System -- redirects --> Dashboard
```
~~~

```force-graph
User -- logs in --> System
System -- authenticates --> Database
Database -- returns user --> System
System -- redirects --> Dashboard
```

### Mixed Graph

You can mix both types of links in the same graph.

~~~
```force-graph
Node A --> Node B
Node B -- complex link --> Node C
Node C --> Node A
Node A -- direct --> Node C
```
~~~

```force-graph
Node A --> Node B
Node B -- complex link --> Node C
Node C --> Node A
Node A -- direct --> Node C
```

---

## Features

- **Interactive**: You can drag nodes, zoom, and pan the graph.
- **Responsive**: The graph container automatically adjusts to the width of the page.
- **Curved Links**: Links are curved to handle multiple connections between nodes gracefully.
- **Directional Particles**: Links show moving particles to indicate directionality.

[!ref :globe: Force Graph website](https://github.com/vasturiano/force-graph)
