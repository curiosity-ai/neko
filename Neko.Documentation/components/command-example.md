---
title: Command Example Component
---

# Command Example

The `command-example` component provides a side-by-side display of an installation command and a quickstart command. It features interactive hover states and one-click copy functionality.

## Usage

Use the `[!command-example ...]` component and provide the `install` and `quickstart` attributes.

```md
[!command-example install="npm install -g mlld" quickstart="mlld quickstart"]
```

## Preview

[!command-example install="npm install -g mlld" quickstart="mlld quickstart"]

## Attributes

| Attribute | Description | Required | Default |
| :--- | :--- | :---: | :--- |
| `install` | The installation command to display in the left box. | Yes | - |
| `quickstart` | The quickstart command to display in the right box. | Yes | - |
