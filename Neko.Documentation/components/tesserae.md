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
