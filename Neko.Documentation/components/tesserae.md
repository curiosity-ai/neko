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

## Tailoring the displayed source

Two marker pairs let the source shown in the **Code** tab differ from what is
compiled and run:

- `// <hide>` … `// </hide>` — compiled and run, but **removed** from the Code
  tab. Use it to keep styling, layout chrome, or demo-only plumbing out of the
  snippet while the live preview still reflects the full code.
- `// <docs>` … `// </docs>` — **shown** in the Code tab, but **not compiled**.
  Use it to display the idiomatic call a sample can't run as-is (for example an
  API a sandboxed preview can't use) while a `// <hide>` block runs a
  preview-safe variant.

```tesserae
using Tesserae;
using static Tesserae.UI;

namespace Neko.Documentation
{
    public class HideDemo
    {
        public static void Main()
        {
            var bar = HStack().Children(Button("Home"), Button("About"));
            // <hide>
            // Chrome only — keep the styling out of the displayed snippet.
            bar.WS().AlignItemsCenter().Gap(8.px()).Background("#f3f4f6").P(10);
            // </hide>
            MountToBody(bar);
        }
    }
}
```

The marker lines must sit on their own line (surrounding whitespace is fine);
matching is case-insensitive and the space after `//` is optional. The markers
are removed from both the displayed and the compiled code, and hidden code still
runs — a syntax error inside a hidden region fails the build.
