---
title: Math Component
icon: calculator
---

# Math

Neko supports rendering mathematical formulas using **KaTeX**. This allows you to write LaTeX equations directly in your Markdown.

## Syntax

You can use both inline and block-level syntax.

### Inline Math

Wrap your equation in single dollar signs `$ ... $` or `\( ... \)`.

```markdown
The mass-energy equivalence is described by $E=mc^2$.
```

Result: The mass-energy equivalence is described by $E=mc^2$.

### Block Math

Wrap your equation in double dollar signs `$$ ... $$` or `\[ ... \]` for a centered block.

```markdown
$$
\int_0^\infty e^{-x^2} dx = \frac{\sqrt{\pi}}{2}
$$
```

Result:

$$
\int_0^\infty e^{-x^2} dx = \frac{\sqrt{\pi}}{2}
$$

## Advanced Features

KaTeX supports a wide range of LaTeX commands. You can write matrices, aligned equations, and more.

### Aligned Equations

$$
\begin{aligned}
a &= b + c \\
  &= d + e + f
\end{aligned}
$$

### Matrices

$$
\begin{pmatrix}
a & b \\
c & d
\end{pmatrix}
$$
