---
name: math
description: Render inline or block LaTeX math via KaTeX (`$…$`, `$$…$$`, `\(…\)`, `\[…\]`). Use for equations, symbols, and notation in technical docs.
---

# Math

Neko renders LaTeX math with KaTeX. Both inline and block forms are supported.

## Inline

```markdown
The mass-energy equivalence is $E = mc^2$.
Also acceptable: \(E = mc^2\).
```

## Block

```markdown
$$
E = mc^2
$$
```

Or with the alternative delimiters:

```markdown
\[
\int_0^\infty e^{-x^2} \, dx = \frac{\sqrt{\pi}}{2}
\]
```

## Tips

- Escape literal `$` outside math with `\$` if your prose mixes them.
- For long aligned derivations, use the dedicated
  [`math-formulas`](../math-formulas/SKILL.md) component — it lets you write
  full LaTeX inside a fenced block.
- KaTeX supports most of the AMS-LaTeX environment. Unsupported macros render
  as the source text wrapped in a warning.
