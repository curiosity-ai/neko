---
title: Tesserae C# Component
label: Tesserae
icon: fi fi-rr-browser
description: Embed live-compiled C# Tesserae code inside documentation with code and live preview tabs.
order: 100
---

# Tesserae

You can embed interactive UI components written in C# using the **Tesserae** framework directly into your documentation. Neko will automatically compile the C# code using the `H5.Compiler.Service`, bundle it with the required Tesserae packages, and render a two-tab interface containing your original code and a live HTML preview.

## Usage

Create a code block and set its language to `tesserae`:

```tesserae
using System;
using Tesserae;
using static Tesserae.UI;

namespace Neko.Documentation
{
    public class App
    {
        public static void Main()
        {
            var btn = Button("Click me!").Primary();
            var text = TextBlock("Hello from Tesserae!");
            
            btn.OnClick((s, e) => {
                text.Text = "Button clicked at " + DateTime.Now.ToString("HH:mm:ss");
            });

            document.body.appendChild(
                VStack().Children(
                    btn,
                    text
                ).Render()
            );
        }
    }
}
```
