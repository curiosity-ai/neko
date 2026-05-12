---
name: math-formulas
description: Render multi-line LaTeX formulas (aligned equations, matrices, cases) in a fenced `latex` block. Use for derivations and any math too large for inline `$…$`.
---

# Math formulas

For multi-line formulas — aligned equations, matrices, piecewise functions —
use a fenced `latex` block or block-form `$$ … $$`.

## Block form

```markdown
$$
\begin{aligned}
a &= b \\
  &= c
\end{aligned}
$$
```

## Fenced latex block

````markdown
```latex
\begin{aligned}
f(x) &= ax^2 + bx + c \\
f'(x) &= 2ax + b
\end{aligned}
```
````

## Matrices

```markdown
$$
\begin{pmatrix}
a & b \\
c & d
\end{pmatrix}
$$
```

## Cases

```markdown
$$
f(x) = \begin{cases}
  x^2 & \text{if } x \geq 0 \\
  -x  & \text{otherwise}
\end{cases}
$$
```

## Tips

- KaTeX is fast but not 100% LaTeX-complete; complex packages may not work.
- For very long derivations consider breaking into multiple blocks with
  surrounding prose.
- Inline math (`$…$`) is fine for single symbols; reach for this skill when you
  need an environment (`aligned`, `matrix`, `cases`, …).
