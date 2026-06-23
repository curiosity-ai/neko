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

Create a code block and set its language to `tesserae`. Below is a full TODO sample app that demonstrates building a functional UI and using `window.localStorage` to save state.

```tesserae
using System;
using Tesserae;
using static Tesserae.UI;
using static H5.Core.dom;
using H5;

namespace Neko.Documentation
{
    public class TodoApp
    {
        public class TodoItem
        {
            public string Text { get; set; }
            public bool IsDone { get; set; }
        }

        public static void Main()
        {
            var todoList = new ObservableList<TodoItem>();
            
            // Load from Local Storage
            var savedTodos = window.localStorage.getItem("tesserae-todos");
            if (!string.IsNullOrEmpty(savedTodos))
            {
                try {
                    var parsed = Script.Write<TodoItem[]>("JSON.parse({0})", savedTodos);
                    if (parsed != null) {
                        todoList.AddRange(parsed);
                    }
                } catch {}
            }

            // Save to Local Storage whenever list changes
            todoList.Observe(_ => {
                var json = Script.Write<string>("JSON.stringify({0})", todoList.Value);
                window.localStorage.setItem("tesserae-todos", json);
            });

            var input = TextBox().SetPlaceholder("Add a new task...").Width(100.percent());
            var addButton = Button("Add").Primary();

            Action addTodo = () =>
            {
                if (!string.IsNullOrWhiteSpace(input.Text))
                {
                    todoList.Add(new TodoItem { Text = input.Text, IsDone = false });
                    input.Text = "";
                }
            };

            addButton.OnClick((s, e) => addTodo());
            input.OnKeyUp((s, e) => { if (e.key == "Enter") addTodo(); });

            var listContainer = VStack().ScrollY().MaxHeight(300.px());

            // Build UI for each item
            todoList.Observe(todos =>
            {
                listContainer.Clear();
                foreach (var todo in todos)
                {
                    var cb = CheckBox(todo.Text);
                    cb.IsChecked = todo.IsDone;
                    cb.OnChange((s, e) => {
                        todo.IsDone = cb.IsChecked;
                        // trigger save
                        var json = Script.Write<string>("JSON.stringify({0})", todoList.Value);
                        window.localStorage.setItem("tesserae-todos", json);
                    });

                    var delBtn = Button().SetIcon(UIcons.Trash).Danger().OnClick((s, e) => {
                        todoList.Remove(todo);
                    });

                    listContainer.Add(HStack().Children(cb, delBtn).JustifyContent(ItemJustify.Between).Width(100.percent()));
                }
            });

            var card = Card(
                VStack().Children(
                    TextBlock("TODO List").Medium(),
                    HStack().Children(input, addButton),
                    listContainer
                ).Gap(16.px())
            );

            MountToBody(card);
        }
    }
}
```

## Preview height

A `height=<px>` argument on the block pins its live-preview iframe to a fixed
height; without it the iframe uses a resizable 400px minimum:

````markdown
```tesserae chrome="macos" demo.js height=420
…
```
````

Rather than hand-tuning the value, run `neko gen-tesserae-heights`, which renders
each sample, measures its content height, and writes the `height=` argument back
onto the block. Scope it to one file with `--file <path>` and rerun it after
editing a sample — it is file-targeted with no hash cache, so the file you target
is exactly what gets regenerated. Measurement uses the same Playwright/snapframe
harness as `neko snap`.

## Showing different code than what runs

By default a `tesserae` block is **both compiled and displayed as-is** — write a
complete program and the reader sees exactly what runs.

Some samples can't run as-is in the sandboxed preview iframe (an `about:srcdoc`
document, where the History API and a few other browser features are
unavailable). For those, put the version to **display** inside an
`// <overwrite-sample-code>` … `// </overwrite-sample-code>` region: everything
outside it is compiled and run (and not shown), and everything inside it is shown
in the Code tab verbatim and never compiled.

```tesserae
using Tesserae;
using static Tesserae.UI;

namespace Neko.Documentation
{
    public class Demo
    {
        public static void Main()
        {
            // Compiled + run — powers the live preview.
            MountToBody(TextBlock("Running in the sandboxed preview"));
        }
    }
}
// <overwrite-sample-code>
using Tesserae;
using static Tesserae.UI;

namespace Neko.Documentation
{
    public class Demo
    {
        public static void Main()
        {
            // Shown in the Code tab — the version you'd write in a real app.
            MountToBody(TextBlock("Hello, Tesserae!"));
        }
    }
}
// </overwrite-sample-code>
```

The marker lines must sit on their own line (surrounding whitespace is fine);
matching is case-insensitive and the space after `//` is optional. Keep the
feature rare — most samples should just show what runs. The overwrite code is
never compiled, so it isn't checked; keep it a faithful, complete program.
