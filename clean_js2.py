import re

content = open('Neko/Builder/HtmlGenerator.cs').read()

search = r'sb\.AppendLine\(\$"                            <summary class=\\"flex items-center justify-between py-1 px-2 text-\\[13px\\] whitespace-nowrap font-medium text-gray-700 dark:text-gray-200 rounded hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer select-none\\">\);'
replace = r"""string editHtml = "";
                if (_isWatchMode && !string.IsNullOrEmpty(link.FolderPath))
                {
                    // Use Path.GetRelativePath correctly to resolve absolute/relative paths
                    var inputPathFull = System.IO.Path.GetFullPath(_config.Input);
                    var folderPathFull = System.IO.Path.GetFullPath(link.FolderPath);

                    var relativePath = System.IO.Path.GetRelativePath(inputPathFull, folderPathFull).Replace("\\\\", "/");
                    if (relativePath == ".") relativePath = "";

                    var ymlPath = string.IsNullOrEmpty(relativePath) ? "index.yml" : relativePath + "/index.yml";

                    editHtml = $"<button onclick=\"nekoOpenEditorPath('{ymlPath}', event)\" class=\"text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors focus:outline-none p-1 rounded hover:bg-gray-300 dark:hover:bg-gray-600 mr-1\" title=\"Edit Folder Config\"><i class=\"fi fi-rr-pencil text-xs\"></i></button>";
                }
                sb.AppendLine($"                            <summary class=\"flex items-center justify-between py-1 px-2 text-[13px] whitespace-nowrap font-medium text-gray-700 dark:text-gray-200 rounded hover:bg-gray-200 dark:hover:bg-gray-700 cursor-pointer select-none\">");"""

content = re.sub(search, replace, content)

search2 = r'sb\.AppendLine\(\$"                                <i class=\\"fi fi-rr-angle-small-down transition-transform group-open:rotate-180\\"></i>"\);'
replace2 = r'sb.AppendLine($"                                <div class=\"flex items-center\">{editHtml}<i class=\"fi fi-rr-angle-small-down transition-transform group-open:rotate-180\"></i></div>");'

content = re.sub(search2, replace2, content)

replace_js = r"""        function nekoOpenEditorPath(path, event) {
            if (event) {
                event.preventDefault();
                event.stopPropagation();
            }
            fetch('/api/neko/content?path=' + encodeURIComponent(path))
                .then(res => {
                    if (!res.ok) throw new Error('Not found');
                    return res.text();
                })
                .then(markdown => {
                    modal.classList.remove('hidden');
                    loadMonaco(() => {
                        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }});
                        require(['vs/editor/editor.main'], function() {
                            if (editor) {
                                monaco.editor.setModelLanguage(editor.getModel(), 'yaml');
                                editor.setValue(markdown);
                            } else {
                                editor = monaco.editor.create(container, {
                                    value: markdown,
                                    language: 'yaml',
                                    theme: document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs',
                                    automaticLayout: true,
                                    minimap: { enabled: false },
                                    wordWrap: 'on',
                                    pasteAs: { enabled: false, },
                                    fontSize: 14
                                });
                            }
                            window.nekoCurrentEditPath = path;
                        });
                    });
                })
                .catch(err => {
                    modal.classList.remove('hidden');
                    loadMonaco(() => {
                        require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.44.0/min/vs' }});
                        require(['vs/editor/editor.main'], function() {
                            const defaultYaml = "order: 0\\nlabel: ''\\nicon: ''\\nexpanded: false\\n";
                            if (editor) {
                                monaco.editor.setModelLanguage(editor.getModel(), 'yaml');
                                editor.setValue(defaultYaml);
                            } else {
                                editor = monaco.editor.create(container, {
                                    value: defaultYaml,
                                    language: 'yaml',
                                    theme: document.documentElement.classList.contains('dark') ? 'vs-dark' : 'vs',
                                    automaticLayout: true,
                                    minimap: { enabled: false },
                                    wordWrap: 'on',
                                    pasteAs: { enabled: false, },
                                    fontSize: 14
                                });
                            }
                            window.nekoCurrentEditPath = path;
                        });
                    });
                });
        }

        function nekoOpenEditor() {
            window.nekoCurrentEditPath = window.location.pathname;"""

csharp_search = 'sb.AppendLine("        function nekoOpenEditor() {");'
csharp_replace = "\n".join([f'sb.AppendLine("{line.replace("\"", "\\\"")}");' for line in replace_js.split("\n")])

content = content.replace(csharp_search, csharp_replace)


csharp_search_save = "sb.AppendLine(\"            const path = window.location.pathname;\");"
csharp_replace_save = "sb.AppendLine(\"            const path = window.nekoCurrentEditPath || window.location.pathname;\");"

content = content.replace(
    'sb.AppendLine("            const path = window.location.pathname;");\n                sb.AppendLine("            fetch(\'/api/neko/content\', {");',
    'sb.AppendLine("            const path = window.nekoCurrentEditPath || window.location.pathname;");\n                sb.AppendLine("            fetch(\'/api/neko/content\', {");'
)


open('Neko/Builder/HtmlGenerator.cs', 'w').write(content)
