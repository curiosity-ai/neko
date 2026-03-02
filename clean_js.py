import re

content = open('Neko/Builder/HtmlGenerator.cs').read()

block_to_remove = r"""                sb.AppendLine\("        function nekoOpenEditorPath\(path, event\) \{"\);
sb.AppendLine\("            if \(event\) \{"\);
sb.AppendLine\("                event.preventDefault\(\);"\);
sb.AppendLine\("                event.stopPropagation\(\);"\);
sb.AppendLine\("            \}"\);
sb.AppendLine\("            fetch\('/api/neko/content\?path=' \+ encodeURIComponent\(path\)\)"\);
sb.AppendLine\("                .then\(res => \{"\);
sb.AppendLine\("                    if \(!res.ok\) throw new Error\('Not found'\);"\);
sb.AppendLine\("                    return res.text\(\);"\);
sb.AppendLine\("                \}\)"\);
sb.AppendLine\("                .then\(markdown => \{"\);
sb.AppendLine\("                    modal.classList.remove\('hidden'\);"\);
sb.AppendLine\("                    loadMonaco\(\(\) => \{"\);
sb.AppendLine\("                        require.config\(\{ paths: \{ 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' \}\}\);"\);
sb.AppendLine\("                        require\(\['vs/editor/editor.main'\], function\(\) \{"\);
sb.AppendLine\("                            if \(editor\) \{"\);
sb.AppendLine\("                                monaco.editor.setModelLanguage\(editor.getModel\(\), 'yaml'\);"\);
sb.AppendLine\("                                editor.setValue\(markdown\);"\);
sb.AppendLine\("                            \} else \{"\);
sb.AppendLine\("                                editor = monaco.editor.create\(container, \{"\);
sb.AppendLine\("                                    value: markdown,"\);
sb.AppendLine\("                                    language: 'yaml',"\);
sb.AppendLine\("                                    theme: document.documentElement.classList.contains\('dark'\) \? 'vs-dark' : 'vs',"\);
sb.AppendLine\("                                    automaticLayout: true,"\);
sb.AppendLine\("                                    minimap: \{ enabled: false \},"\);
sb.AppendLine\("                                    wordWrap: 'on',"\);
sb.AppendLine\("                                    pasteAs: \{ enabled: false, \},"\);
sb.AppendLine\("                                    fontSize: 14"\);
sb.AppendLine\("                                \}\);"\);
sb.AppendLine\("                            \}"\);
sb.AppendLine\("                            window.nekoCurrentEditPath = path;"\);
sb.AppendLine\("                        \}\);"\);
sb.AppendLine\("                    \}\);"\);
sb.AppendLine\("                \}\)"\);
sb.AppendLine\("                .catch\(err => \{"\);
sb.AppendLine\("                    modal.classList.remove\('hidden'\);"\);
sb.AppendLine\("                    loadMonaco\(\(\) => \{"\);
sb.AppendLine\("                        require.config\(\{ paths: \{ 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' \}\}\);"\);
sb.AppendLine\("                        require\(\['vs/editor/editor.main'\], function\(\) \{"\);
sb.AppendLine\("                            const defaultYaml = \"order: 0\\\\nlabel: ''\\\\nicon: ''\\\\nexpanded: false\\\\n\";"\);
sb.AppendLine\("                            if \(editor\) \{"\);
sb.AppendLine\("                                monaco.editor.setModelLanguage\(editor.getModel\(\), 'yaml'\);"\);
sb.AppendLine\("                                editor.setValue\(defaultYaml\);"\);
sb.AppendLine\("                            \} else \{"\);
sb.AppendLine\("                                editor = monaco.editor.create\(container, \{"\);
sb.AppendLine\("                                    value: defaultYaml,"\);
sb.AppendLine\("                                    language: 'yaml',"\);
sb.AppendLine\("                                    theme: document.documentElement.classList.contains\('dark'\) \? 'vs-dark' : 'vs',"\);
sb.AppendLine\("                                    automaticLayout: true,"\);
sb.AppendLine\("                                    minimap: \{ enabled: false \},"\);
sb.AppendLine\("                                    wordWrap: 'on',"\);
sb.AppendLine\("                                    pasteAs: \{ enabled: false, \},"\);
sb.AppendLine\("                                    fontSize: 14"\);
sb.AppendLine\("                                \}\);"\);
sb.AppendLine\("                            \}"\);
sb.AppendLine\("                            window.nekoCurrentEditPath = path;"\);
sb.AppendLine\("                        \}\);"\);
sb.AppendLine\("                    \}\);"\);
sb.AppendLine\("                \}\);"\);
sb.AppendLine\("        \}"\);"""


content = re.sub(block_to_remove + r"\n", "", content)

# I also need to remove duplicate `window.nekoCurrentEditPath = window.location.pathname;`
# and empty string lines
content = re.sub(r'sb.AppendLine\(""\);\n        sb.AppendLine\("        function nekoOpenEditor\(\) \{"\);\nsb.AppendLine\("            window.nekoCurrentEditPath = window.location.pathname;"\);\nsb.AppendLine\("            window.nekoCurrentEditPath = window.location.pathname;"\);',
r'''sb.AppendLine("");
        sb.AppendLine("        function nekoOpenEditor() {");
        sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");''', content)

# also replace the last remaining duplicate if any
content = content.replace('''sb.AppendLine("        function nekoOpenEditor() {");
sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");
sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");''', '''sb.AppendLine("        function nekoOpenEditor() {");
sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");''')


content = content.replace('''sb.AppendLine("        function nekoOpenEditor() {");
sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");''',
'''sb.AppendLine("        function nekoOpenEditorPath(path, event) {");
sb.AppendLine("            if (event) {");
sb.AppendLine("                event.preventDefault();");
sb.AppendLine("                event.stopPropagation();");
sb.AppendLine("            }");
sb.AppendLine("            fetch('/api/neko/content?path=' + encodeURIComponent(path))");
sb.AppendLine("                .then(res => {");
sb.AppendLine("                    if (!res.ok) throw new Error('Not found');");
sb.AppendLine("                    return res.text();");
sb.AppendLine("                })");
sb.AppendLine("                .then(markdown => {");
sb.AppendLine("                    modal.classList.remove('hidden');");
sb.AppendLine("                    loadMonaco(() => {");
sb.AppendLine("                        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }});");
sb.AppendLine("                        require(['vs/editor/editor.main'], function() {");
sb.AppendLine("                            if (editor) {");
sb.AppendLine("                                monaco.editor.setModelLanguage(editor.getModel(), 'yaml');");
sb.AppendLine("                                editor.setValue(markdown);");
sb.AppendLine("                            } else {");
sb.AppendLine("                                editor = monaco.editor.create(container, {");
sb.AppendLine("                                    value: markdown,");
sb.AppendLine("                                    language: 'yaml',");
sb.AppendLine("                                    theme: document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs',");
sb.AppendLine("                                    automaticLayout: true,");
sb.AppendLine("                                    minimap: { enabled: false },");
sb.AppendLine("                                    wordWrap: 'on',");
sb.AppendLine("                                    pasteAs: { enabled: false, },");
sb.AppendLine("                                    fontSize: 14");
sb.AppendLine("                                });");
sb.AppendLine("                            }");
sb.AppendLine("                            window.nekoCurrentEditPath = path;");
sb.AppendLine("                        });");
sb.AppendLine("                    });");
sb.AppendLine("                })");
sb.AppendLine("                .catch(err => {");
sb.AppendLine("                    modal.classList.remove('hidden');");
sb.AppendLine("                    loadMonaco(() => {");
sb.AppendLine("                        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }});");
sb.AppendLine("                        require(['vs/editor/editor.main'], function() {");
sb.AppendLine("                            const defaultYaml = \\\"order: 0\\\\nlabel: ''\\\\nicon: ''\\\\nexpanded: false\\\\n\\\";");
sb.AppendLine("                            if (editor) {");
sb.AppendLine("                                monaco.editor.setModelLanguage(editor.getModel(), 'yaml');");
sb.AppendLine("                                editor.setValue(defaultYaml);");
sb.AppendLine("                            } else {");
sb.AppendLine("                                editor = monaco.editor.create(container, {");
sb.AppendLine("                                    value: defaultYaml,");
sb.AppendLine("                                    language: 'yaml',");
sb.AppendLine("                                    theme: document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs',");
sb.AppendLine("                                    automaticLayout: true,");
sb.AppendLine("                                    minimap: { enabled: false },");
sb.AppendLine("                                    wordWrap: 'on',");
sb.AppendLine("                                    pasteAs: { enabled: false, },");
sb.AppendLine("                                    fontSize: 14");
sb.AppendLine("                                });");
sb.AppendLine("                            }");
sb.AppendLine("                            window.nekoCurrentEditPath = path;");
sb.AppendLine("                        });");
sb.AppendLine("                    });");
sb.AppendLine("                });");
sb.AppendLine("        }");
sb.AppendLine("");
sb.AppendLine("        function nekoOpenEditor() {");
sb.AppendLine("            window.nekoCurrentEditPath = window.location.pathname;");''')


open('Neko/Builder/HtmlGenerator.cs', 'w').write(content)
