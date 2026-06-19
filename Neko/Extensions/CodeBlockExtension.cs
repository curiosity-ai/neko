using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    public class ForceGraphNode { public string id { get; set; } }
    public class ForceGraphLink { public string source { get; set; } public string target { get; set; } public string name { get; set; } }
    public class ForceGraphData { public List<ForceGraphNode> nodes { get; set; } = new List<ForceGraphNode>(); public List<ForceGraphLink> links { get; set; } = new List<ForceGraphLink>(); }
    public class WorkflowNode { public string Id { get; set; } public string Title { get; set; } public string Icon { get; set; } public string Badge { get; set; } public string Description { get; set; } public int Column { get; set; } }
    public class WorkflowEdge { public string Source { get; set; } public string Target { get; set; } }

    public class CodeBlockRenderer : Markdig.Renderers.Html.CodeBlockRenderer
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            // Only enhance FencedCodeBlocks (```)
            if (obj is not FencedCodeBlock fencedBlock)
            {
                base.Write(renderer, obj);
                return;
            }

            // Handle Tesserae
            if ((fencedBlock.Info ?? "").ToLower() == "tesserae")
            {
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var slices = leafBlock.Lines;
                    var csharpCode = new System.Text.StringBuilder();
                    for (int i = 0; i < slices.Count; i++)
                    {
                        var slice = slices.Lines[i].Slice;
                        if (slice.Text == null) continue;
                        csharpCode.AppendLine(slice.ToString());
                    }

                    var codeString = csharpCode.ToString();

                    // SiteBuilder always set Environment.CurrentDirectory to the output folder
                    var siteOutputRoot = Environment.CurrentDirectory;

                    var result = Neko.Builder.TesseraeCompiler.CompileAsync(fencedBlock.Arguments, codeString, siteOutputRoot).GetAwaiter().GetResult(); //Can't use async as Markdig doesn't expose an async method

                    if (result != null)
                    {
                        var groupId = System.Guid.NewGuid().ToString("N");

                        renderer.Write("<div class=\"my-4 border rounded-md dark:border-gray-700\">");

                        // Tab Headers
                        renderer.Write("<div class=\"flex border-b bg-gray-50 dark:bg-gray-800 dark:border-gray-700 overflow-x-auto\">");
                        renderer.Write($"<button class=\"px-4 py-2 border-b-2 focus:outline-none whitespace-nowrap border-primary-500 text-primary-600 dark:text-primary-400 font-medium\" onclick=\"openTab(event, '{groupId}', 'tab-{groupId}-0')\">Live Preview</button>");
                        renderer.Write($"<button class=\"px-4 py-2 border-b-2 focus:outline-none whitespace-nowrap border-transparent hover:text-gray-700 dark:hover:text-gray-300 text-gray-500 dark:text-gray-400\" onclick=\"openTab(event, '{groupId}', 'tab-{groupId}-1')\">Code</button>");
                        renderer.Write("</div>");

                        // Tab Contents
                        renderer.Write("<div class=\"p-4\">");

                        // Live Preview Tab (Active)
                        renderer.Write($"<div id=\"tab-{groupId}-0\" class=\"tab-content\">");
                        var encodedHtml = System.Net.WebUtility.HtmlEncode(result.OutputHtml);
                        renderer.Write($"<iframe class=\"w-full rounded border border-gray-200 dark:border-gray-700\" style=\"min-height: 400px; resize: vertical;\" srcdoc=\"{encodedHtml}\"></iframe>");
                        renderer.Write("</div>");

                        // Code Tab (Hidden)
                        renderer.Write($"<div id=\"tab-{groupId}-1\" class=\"tab-content hidden\">");

                        // Fake a csharp code block to render it
                        fencedBlock.Info = "csharp";
                        fencedBlock.GetAttributes().AddClass("tesserae-code");
                    }
                    else
                    {
                        // Compilation failed (e.g. an offline build with no Tesserae
                        // toolchain). Fall back to rendering the source as a normal C#
                        // code block so it stays syntax-highlighted and legible instead
                        // of an unhighlighted `language-tesserae` block in flat grey.
                        fencedBlock.Info = "csharp";
                    }
                }
            }

            // Handle Mermaid
            if ((fencedBlock.Info ?? "").ToLower() == "mermaid")
            {
                renderer.Write("<div class=\"mermaid relative flex justify-center bg-gray-50 dark:bg-gray-800 p-4 rounded-md border border-gray-200 dark:border-gray-700 overflow-hidden my-6 min-h-[400px]\">");
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var slices = leafBlock.Lines;
                    for (int i = 0; i < slices.Count; i++)
                    {
                        var slice = slices.Lines[i].Slice;
                        if (slice.Text == null) continue;
                        renderer.WriteEscape(slice.ToString());
                        renderer.Write("\n");
                    }
                }
                renderer.Write("</div>");
                return;
            }

            // Handle Quiz
            if ((fencedBlock.Info ?? "").ToLower() == "quiz")
            {
                QuizComponent.Write(renderer, fencedBlock);
                return;
            }

            // Handle Link Card (card of links)
            if ((fencedBlock.Info ?? "").ToLower() == "links")
            {
                LinkCardComponent.Write(renderer, fencedBlock);
                return;
            }

            // Handle API endpoint reference entry
            if ((fencedBlock.Info ?? "").ToLower() == "endpoint")
            {
                ApiEndpointComponent.Write(renderer, fencedBlock);
                return;
            }

            // Handle CSharp Docs
            if ((fencedBlock.Info ?? "").ToLower() == "csharp-docs")
            {
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var slices = leafBlock.Lines;
                    var csharpCode = new StringBuilder();
                    for (int i = 0; i < slices.Count; i++)
                    {
                        var slice = slices.Lines[i].Slice;
                        if (slice.Text == null) continue;
                        csharpCode.AppendLine(slice.ToString());
                    }

                    var codeString = csharpCode.ToString();
                    // Script mode lets bare members (methods, properties) at the file root
                    // parse as proper MemberDeclarationSyntax instead of being wrapped in
                    // GlobalStatementSyntax/LocalFunctionStatementSyntax.
                    var parseOptions = new CSharpParseOptions(kind: SourceCodeKind.Script);
                    var tree = CSharpSyntaxTree.ParseText(codeString, parseOptions);
                    var root = tree.GetRoot();

                    renderer.Write("<div class=\"csharp-docs my-8\">");

                    // Collect top-level members: types and standalone declarations,
                    // unwrapping namespace declarations while remembering the namespace name
                    // they were declared in (used for the Microsoft Learn-style metadata).
                    var topLevel = new List<(MemberDeclarationSyntax Member, string Namespace)>();
                    void Collect(SyntaxNode container, string ns)
                    {
                        foreach (var child in container.ChildNodes())
                        {
                            if (child is NamespaceDeclarationSyntax nsDecl)
                            {
                                Collect(nsDecl, nsDecl.Name.ToString());
                            }
                            else if (child is FileScopedNamespaceDeclarationSyntax fsNs)
                            {
                                Collect(fsNs, fsNs.Name.ToString());
                            }
                            else if (child is MemberDeclarationSyntax m)
                            {
                                topLevel.Add((m, ns));
                            }
                        }
                    }
                    Collect(root, "");

                    foreach (var (member, ns) in topLevel)
                    {
                        if (member is BaseTypeDeclarationSyntax typeDecl)
                        {
                            RenderCSharpType(renderer, typeDecl, ns);
                        }
                        else if (HasXmlDoc(member))
                        {
                            RenderCSharpMember(renderer, member, classPrefix: null);
                        }
                    }

                    renderer.Write("</div>"); // Close csharp-docs
                }
                return;
            }

            // Handle ForceGraph
            if ((fencedBlock.Info ?? "").ToLower() == "force-graph")
            {
                var graphData = new ForceGraphData();
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var slices = leafBlock.Lines;
                    for (int i = 0; i < slices.Count; i++)
                    {
                        var line = slices.Lines[i].Slice.ToString();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // Parse "Source --> Target" or "Source -- Label --> Target"
                        var labelMatch = Regex.Match(line, @"(.+?)\s+--\s+(.+?)\s+-->\s+(.+)");
                        if (labelMatch.Success)
                        {
                            var source = labelMatch.Groups[1].Value.Trim();
                            var label = labelMatch.Groups[2].Value.Trim();
                            var target = labelMatch.Groups[3].Value.Trim();

                            if (!graphData.nodes.Any(n => n.id == source)) graphData.nodes.Add(new ForceGraphNode { id = source });
                            if (!graphData.nodes.Any(n => n.id == target)) graphData.nodes.Add(new ForceGraphNode { id = target });
                            graphData.links.Add(new ForceGraphLink { source = source, target = target, name = label });
                        }
                        else
                        {
                            var simpleMatch = Regex.Match(line, @"(.+?)\s+-->\s+(.+)");
                            if (simpleMatch.Success)
                            {
                                var source = simpleMatch.Groups[1].Value.Trim();
                                var target = simpleMatch.Groups[2].Value.Trim();

                                if (!graphData.nodes.Any(n => n.id == source)) graphData.nodes.Add(new ForceGraphNode { id = source });
                                if (!graphData.nodes.Any(n => n.id == target)) graphData.nodes.Add(new ForceGraphNode { id = target });
                                graphData.links.Add(new ForceGraphLink { source = source, target = target });
                            }
                        }
                    }
                }

                var graphId = "force-graph-" + System.Guid.NewGuid().ToString("N");
                var jsonData = JsonSerializer.Serialize(graphData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                renderer.Write($"<div id=\"{graphId}\" class=\"force-graph-container w-full h-96 rounded-md border border-gray-200 dark:border-gray-700 my-6\"></div>");
                renderer.Write("<script>");
                renderer.Write($"(function() {{ const gData = {jsonData}; ");
                renderer.Write($"const container = document.getElementById('{graphId}');");
                renderer.Write($"const width = container.clientWidth > 0 ? container.clientWidth : 800;");
                renderer.Write($"const height = container.clientHeight > 0 ? container.clientHeight : 400;");
                renderer.Write($"const graph = ForceGraph() (container) .graphData(gData) .nodeId('id') .nodeLabel('id') .linkLabel('name') .linkDirectionalArrowLength(6) .linkDirectionalArrowRelPos(1) .linkCurvature(0.3) .nodeColor(() => '#4a5568') .linkColor(() => '#cbd5e0') .backgroundColor('#f9fafb') .width(width) .height(height);");
                renderer.Write($"new ResizeObserver(() => {{ graph.width(container.clientWidth).height(container.clientHeight); }}).observe(container); }})()");
                renderer.Write("</script>");
                return;
            }

            // Handle Workflow
            if ((fencedBlock.Info ?? "").ToLower() == "workflow")
            {
                var leafBlock = obj as Markdig.Syntax.LeafBlock;
                if (leafBlock != null)
                {
                    var lines = leafBlock.Lines;
                    var nodes = new Dictionary<string, WorkflowNode>();
                    var edges = new List<WorkflowEdge>();

                    for (int i = 0; i < lines.Count; i++)
                    {
                        var line = lines.Lines[i].Slice.ToString().Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

                        // Edge
                        var edgeMatch = Regex.Match(line, @"^([a-zA-Z0-9_-]+)\s*-->\s*([a-zA-Z0-9_-]+)$");
                        if (edgeMatch.Success)
                        {
                            edges.Add(new WorkflowEdge { Source = edgeMatch.Groups[1].Value, Target = edgeMatch.Groups[2].Value });
                            continue;
                        }

                        // Description
                        var descMatch = Regex.Match(line, @"^([a-zA-Z0-9_-]+)\.description:\s*(.+)$");
                        if (descMatch.Success)
                        {
                            var id = descMatch.Groups[1].Value;
                            if (!nodes.ContainsKey(id)) nodes[id] = new WorkflowNode { Id = id };
                            nodes[id].Description = descMatch.Groups[2].Value;
                            continue;
                        }

                        // Node
                        var nodeMatch = Regex.Match(line, @"^([a-zA-Z0-9_-]+):\s*(.+)$");
                        if (nodeMatch.Success)
                        {
                            var id = nodeMatch.Groups[1].Value;
                            var parts = nodeMatch.Groups[2].Value.Split('|').Select(p => p.Trim()).ToArray();
                            if (!nodes.ContainsKey(id)) nodes[id] = new WorkflowNode { Id = id };
                            nodes[id].Title = parts.Length > 0 ? parts[0] : id;
                            nodes[id].Icon = parts.Length > 1 ? parts[1] : "";
                            nodes[id].Badge = parts.Length > 2 ? parts[2] : "";
                            continue;
                        }
                    }

                    // Layout DAG
                    foreach (var edge in edges)
                    {
                        if (!nodes.ContainsKey(edge.Source)) nodes[edge.Source] = new WorkflowNode { Id = edge.Source, Title = edge.Source };
                        if (!nodes.ContainsKey(edge.Target)) nodes[edge.Target] = new WorkflowNode { Id = edge.Target, Title = edge.Target };
                    }

                    // Compute columns (longest path) with cycle detection
                    bool changed = true;
                    int maxIterations = nodes.Count + 1;
                    int iterations = 0;
                    while (changed && iterations < maxIterations)
                    {
                        changed = false;
                        foreach (var edge in edges)
                        {
                            if (nodes[edge.Target].Column <= nodes[edge.Source].Column)
                            {
                                nodes[edge.Target].Column = nodes[edge.Source].Column + 1;
                                changed = true;
                            }
                        }
                        iterations++;
                    }

                    var columns = nodes.Values.GroupBy(n => n.Column).OrderBy(g => g.Key).ToList();
                    var groupId = "workflow-" + System.Guid.NewGuid().ToString("N");

                    renderer.Write($"<div id=\"{groupId}\" class=\"workflow-container relative w-full overflow-x-auto bg-gray-50/50 dark:bg-gray-800/20 p-8 rounded-xl border border-gray-200 dark:border-gray-700 my-8 min-h-[300px]\">");
                    renderer.Write($"<div id=\"{groupId}-lines\" class=\"absolute inset-0 pointer-events-none z-0\"></div>");
                    renderer.Write("<div class=\"workflow-layout flex items-stretch justify-start gap-16 relative z-10 w-max mx-auto px-4\">");

                    foreach (var col in columns)
                    {
                        renderer.Write("<div class=\"workflow-column flex flex-col justify-center gap-6\">");
                        foreach (var node in col)
                        {
                            var nodeId = $"{groupId}-node-{node.Id}";
                            var clickableClass = !string.IsNullOrEmpty(node.Description) ? " cursor-pointer" : "";
                            var onclickAttr = !string.IsNullOrEmpty(node.Description) ? " onclick=\"this.querySelector('.workflow-description')?.classList.toggle('hidden'); setTimeout(() => window.dispatchEvent(new Event('resize')), 10);\"" : "";

                            renderer.Write($"<div id=\"{nodeId}\" data-node-id=\"{node.Id}\" class=\"workflow-node relative group bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg shadow-sm{clickableClass} transition-all duration-150 hover:scale-[1.02] hover:shadow-lg w-64\"{onclickAttr}>");

                            // Top Right Badge
                            if (!string.IsNullOrEmpty(node.Badge))
                            {
                                renderer.Write($"<div class=\"absolute -top-3 -right-3 min-w-[1.5rem] h-6 px-1.5 rounded-full bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-xs flex items-center justify-center text-gray-600 dark:text-gray-400 font-bold z-20\">{System.Net.WebUtility.HtmlEncode(node.Badge)}</div>");
                            }

                            renderer.Write("<div class=\"p-5\">");
                            renderer.Write("<div class=\"flex flex-col gap-3\">");

                            if (!string.IsNullOrEmpty(node.Icon))
                            {
                                if (node.Icon.Contains("/") || node.Icon.Contains(".")) {
                                    renderer.Write($"<img src=\"{System.Net.WebUtility.HtmlEncode(node.Icon)}\" class=\"w-8 h-8 object-contain\">");
                                } else {
                                    renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(System.Net.WebUtility.HtmlEncode(node.Icon))} text-primary-500 text-3xl\"></i>");
                                }
                            }

                            renderer.Write($"<h4 class=\"font-semibold text-gray-900 dark:text-gray-100 m-0 text-base leading-tight\">{System.Net.WebUtility.HtmlEncode(node.Title)}</h4>");
                            renderer.Write("</div>");

                            if (!string.IsNullOrEmpty(node.Description))
                            {
                                renderer.Write($"<div class=\"workflow-description hidden mt-4 text-sm text-gray-600 dark:text-gray-400 border-t pt-3 border-gray-100 dark:border-gray-800\">");
                                renderer.Write(System.Net.WebUtility.HtmlEncode(node.Description));
                                renderer.Write("</div>");
                            }

                            // Connectors (dots for start/end of lines)
                            renderer.Write("<div class=\"workflow-connector-left absolute top-1/2 -left-1 w-2 h-2 rounded-full border border-gray-400 bg-white dark:bg-gray-900 -translate-y-1/2 z-20\"></div>");
                            renderer.Write("<div class=\"workflow-connector-right absolute top-1/2 -right-1 w-2 h-2 rounded-full border border-gray-400 bg-white dark:bg-gray-900 -translate-y-1/2 z-20\"></div>");

                            renderer.Write("</div>"); // p-5
                            renderer.Write("</div>"); // workflow-node
                        }
                        renderer.Write("</div>"); // workflow-column
                    }

                    renderer.Write("</div>"); // workflow-layout

                    var edgesJson = System.Text.Json.JsonSerializer.Serialize(edges, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                    if (!renderer.EnableHtmlForBlock)
                    {
                        // Ignore leader-line scripts if not outputting HTML block
                    }
                    else
                    {
                        // Add script once
                        renderer.Write("<script src=\"https://cdn.jsdelivr.net/npm/leader-line-new@1.1.9/leader-line.min.js\"></script>");
                    }

                    renderer.Write("<script>");
                    renderer.Write($"(function() {{");
                    renderer.Write($"    const container = document.getElementById('{groupId}');");
                    renderer.Write($"    const edges = {edgesJson};");
                    renderer.Write($"    let lines = [];");
                    renderer.Write($"    let lineElements = [];");
                    renderer.Write($"    let observer;");
                    renderer.Write($"    function drawLines() {{");
                    renderer.Write($"        if (typeof LeaderLine === 'undefined') {{ setTimeout(drawLines, 50); return; }}");
                    renderer.Write($"        if (observer) observer.disconnect();");
                    renderer.Write($"        lines.forEach(l => l.remove());");
                    renderer.Write($"        lineElements.forEach(item => item.svgClip.remove());");
                    renderer.Write($"        lines = [];");
                    renderer.Write($"        lineElements = [];");
                    renderer.Write($"        edges.forEach((edge, i) => {{");
                    renderer.Write($"            const sourceEl = document.getElementById(`{groupId}-node-${{edge.source}}`);");
                    renderer.Write($"            const targetEl = document.getElementById(`{groupId}-node-${{edge.target}}`);");
                    renderer.Write($"            if (!sourceEl || !targetEl) return;");
                    renderer.Write($"            lines.push(new LeaderLine(sourceEl, targetEl, {{ color: '#9ca3af', path: 'grid', size: 2, startPlug: 'behind', endPlug: 'arrow1' }}));");
                    renderer.Write($"            const allLines = document.querySelectorAll('.leader-line');");
                    renderer.Write($"            const lineEl = allLines[allLines.length - 1];");
                    renderer.Write($"            const clipId = `{groupId}-clip-${{i}}`;");
                    renderer.Write($"            const svgClip = document.createElementNS('http://www.w3.org/2000/svg', 'svg');");
                    renderer.Write($"            svgClip.setAttribute('style', 'position: absolute; width: 0; height: 0; pointer-events: none;');");
                    renderer.Write($"            svgClip.innerHTML = `<defs><clipPath id=\"${{clipId}}\"><rect x=\"0\" y=\"0\" width=\"0\" height=\"0\"></rect></clipPath></defs>`;");
                    renderer.Write($"            document.body.appendChild(svgClip);");
                    renderer.Write($"            lineEl.style.clipPath = `url(#${{clipId}})`;");
                    renderer.Write($"            lineElements.push({{ lineEl, svgClip, rectEl: svgClip.querySelector('rect') }});");
                    renderer.Write($"        }});");
                    renderer.Write($"        document.querySelectorAll('.leader-line').forEach(el => el.style.zIndex = 0);");
                    renderer.Write($"        updatePosition(false);");
                    renderer.Write($"        if (observer) observer.observe(container, {{ childList: true, subtree: true, attributes: true }});");
                    renderer.Write($"    }}");
                    renderer.Write($"    let timeoutLines = 0;");
                    renderer.Write($"    function updatePosition(scroll) {{");
                    renderer.Write($"        lines.forEach(l => l.position());");
                    renderer.Write($"        if(scroll) {{lines.forEach(l => l.hide('fade', {{duration:50}}));");
                    renderer.Write($"        window.clearTimeout(timeoutLines); timeoutLines = window.setTimeout(_ => lines.forEach(l => l.show('fade', {{duration:50}})), 250);}}");
                    renderer.Write($"        const rectFrame = container.getBoundingClientRect();");
                    renderer.Write($"        const FRAME_LEFT = rectFrame.left;");
                    renderer.Write($"        const FRAME_TOP = rectFrame.top;");
                    renderer.Write($"        const FRAME_RIGHT = FRAME_LEFT + container.clientWidth;");
                    renderer.Write($"        const FRAME_BOTTOM = FRAME_TOP + container.clientHeight;");
                    renderer.Write($"        lineElements.forEach(item => {{");
                    renderer.Write($"            item.svgClip.style.left = item.lineEl.style.left;");
                    renderer.Write($"            item.svgClip.style.top = item.lineEl.style.top;");
                    renderer.Write($"            const posPoint = item.svgClip.createSVGPoint();");
                    renderer.Write($"            posPoint.x = FRAME_LEFT;");
                    renderer.Write($"            posPoint.y = FRAME_TOP;");
                    renderer.Write($"            const ctm = item.svgClip.getScreenCTM();");
                    renderer.Write($"            if (!ctm) return;");
                    renderer.Write($"            const invCtm = ctm.inverse();");
                    renderer.Write($"            const pointLT = posPoint.matrixTransform(invCtm);");
                    renderer.Write($"            posPoint.x = FRAME_RIGHT;");
                    renderer.Write($"            posPoint.y = FRAME_BOTTOM;");
                    renderer.Write($"            const pointRB = posPoint.matrixTransform(invCtm);");
                    renderer.Write($"            item.rectEl.x.baseVal.value = pointLT.x;");
                    renderer.Write($"            item.rectEl.y.baseVal.value = pointLT.y;");
                    renderer.Write($"            item.rectEl.width.baseVal.value = pointRB.x - pointLT.x;");
                    renderer.Write($"            item.rectEl.height.baseVal.value = pointRB.y - pointLT.y;");
                    renderer.Write($"        }});");
                    renderer.Write($"    }}");
                    renderer.Write($"    window.addEventListener('resize', drawLines);");
                    renderer.Write($"    container.addEventListener('scroll', (_) => updatePosition(true));");
                    renderer.Write($"    document.getElementById('main-scroll').addEventListener('scroll', (_) => updatePosition(true));");
                    renderer.Write($"    window.addEventListener('scroll', (_) => updatePosition(true));");
                    renderer.Write($"    observer = new MutationObserver(drawLines);");
                    renderer.Write($"    observer.observe(container, {{ childList: true, subtree: true, attributes: true }});");
                    renderer.Write($"    setTimeout(drawLines, 100);");
                    renderer.Write($"    if (document.fonts) document.fonts.ready.then(drawLines);");
                    renderer.Write($"}})();");
                    renderer.Write("</script>");
                    renderer.Write("</div>"); // workflow-container
                }
                return;
            }

            var args = fencedBlock.Arguments ?? "";
            string title = null;
            string highlight = null;
            string chrome = null;

            // Extract chrome modifier
            var chromeMatch = Regex.Match(args, "chrome=\"([^\"]+)\"");
            if (chromeMatch.Success)
            {
                chrome = chromeMatch.Groups[1].Value.ToLower();
                args = args.Replace(chromeMatch.Value, "").Trim();
            }

            // 1. Handle Line Numbers Flags
            bool enableLineNumbers = false;
            bool disableLineNumbers = false;

            // Check for disable line numbers (!#) with optional range
            var disableLnMatch = Regex.Match(args, @"!#([\d,-]*)");
            if (disableLnMatch.Success)
            {
                disableLineNumbers = true;
                var range = disableLnMatch.Groups[1].Value;
                if (!string.IsNullOrEmpty(range))
                {
                    highlight = range;
                }
                args = args.Replace(disableLnMatch.Value, "").Trim();
            }

            // 2. Extract Highlight (#1-5,7) or {1-5,7}
            var highlightMatch = Regex.Match(args, @"#([\d,-]+)");
            if (highlightMatch.Success)
            {
                highlight = highlightMatch.Groups[1].Value;
                args = args.Replace(highlightMatch.Value, "").Trim();
            }
            else
            {
                var curlyHighlightMatch = Regex.Match(args, @"\{([\d,-]+)\}");
                if (curlyHighlightMatch.Success)
                {
                    highlight = curlyHighlightMatch.Groups[1].Value;
                    args = args.Replace(curlyHighlightMatch.Value, "").Trim();
                }
            }

            // 3. Check for enable line numbers (#) - AFTER extracting highlight
            var lnMatch = Regex.Match(args, @"(?:^|\s)#(?:$|\s)");
            if (lnMatch.Success)
            {
                enableLineNumbers = true;
                args = Regex.Replace(args, @"(?:^|\s)#(?:$|\s)", " ").Trim();
            }

            // 4. Extract Title (remaining string or title="...")
            var titleMatch = Regex.Match(args, "title=\"([^\"]+)\"");
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[1].Value;
            }
            else if (!string.IsNullOrWhiteSpace(args))
            {
                title = args.Trim();
            }

            // Wrapper
            // Using a darker style matching the image reference.
            // The `neko-code-block` class lets us reset Tailwind Typography's <pre>
            // defaults (dark background, margin, padding, rounding) so the code fills
            // the wrapper card instead of floating as a dark inset box. See HtmlGenerator.Head.cs.
            renderer.Write("<div class=\"neko-code-block relative group my-6 bg-gray-50 dark:bg-[#1a1a1a] rounded-xl border border-gray-200 dark:border-white/10 shadow-sm overflow-hidden\">");

            // Header (always render if chrome is set or title exists)
            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(chrome))
            {
                renderer.Write("<div class=\"flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-white/10 bg-gray-100/50 dark:bg-transparent\">");

                if (chrome == "mac")
                {
                    renderer.Write("<div class=\"flex items-center gap-1.5\">");
                    renderer.Write("<div class=\"w-3 h-3 rounded-full bg-[#ff5f56]\"></div>");
                    renderer.Write("<div class=\"w-3 h-3 rounded-full bg-[#ffbd2e]\"></div>");
                    renderer.Write("<div class=\"w-3 h-3 rounded-full bg-[#27c93f]\"></div>");
                    if (!string.IsNullOrEmpty(title))
                    {
                        renderer.Write($"<span class=\"ml-3 font-mono text-sm text-gray-500 dark:text-gray-400\">{title}</span>");
                    }
                    renderer.Write("</div>");

                    // Copy Button in Header
                    renderer.Write("<button class=\"copy-btn p-1.5 text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition-colors rounded hover:bg-gray-200 dark:hover:bg-white/10\" title=\"Copy to clipboard\">");
                    renderer.Write("<i class=\"fi fi-rr-copy\"></i>");
                    renderer.Write("</button>");
                }
                else if (chrome == "windows")
                {
                    renderer.Write("<div>");
                    if (!string.IsNullOrEmpty(title))
                    {
                        renderer.Write($"<span class=\"font-mono text-sm text-gray-500 dark:text-gray-400\">{title}</span>");
                    }
                    renderer.Write("</div>");

                    renderer.Write("<div class=\"flex items-center gap-4 text-gray-400 dark:text-gray-500\">");
                    renderer.Write("<button class=\"copy-btn hover:text-gray-600 dark:hover:text-gray-300 transition-colors\" title=\"Copy to clipboard\"><i class=\"fi fi-rr-copy\"></i></button>");
                    renderer.Write("<i class=\"fi fi-rr-minus text-xs\"></i>");
                    renderer.Write("<i class=\"fi fi-rr-square text-xs\"></i>");
                    renderer.Write("<i class=\"fi fi-rr-cross-small\"></i>");
                    renderer.Write("</div>");
                }
                else
                {
                    // Default chrome (single blue dot like image, or just title)
                    renderer.Write("<div class=\"flex items-center gap-2\">");
                    renderer.Write("<div class=\"w-2 h-2 rounded-full bg-blue-500\"></div>");
                    if (!string.IsNullOrEmpty(title))
                    {
                        renderer.Write($"<span class=\"font-mono text-sm text-gray-500 dark:text-gray-400\">{title}</span>");
                    }
                    renderer.Write("</div>");

                    // Copy Button in Header
                    renderer.Write("<button class=\"copy-btn p-1.5 text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition-colors rounded hover:bg-gray-200 dark:hover:bg-white/10\" title=\"Copy to clipboard\">");
                    renderer.Write("<i class=\"fi fi-rr-copy\"></i>");
                    renderer.Write("</button>");
                }

                renderer.Write("</div>");
            }
            else
            {
                // Copy Button Overlay (no header)
                renderer.Write("<button class=\"copy-btn absolute top-2 right-2 p-1.5 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 bg-white dark:bg-gray-800 bg-opacity-80 dark:bg-opacity-80 backdrop-blur opacity-0 group-hover:opacity-100 transition-opacity rounded border border-gray-200 dark:border-white/10 shadow-sm z-10\" title=\"Copy to clipboard\">");
                renderer.Write("<i class=\"fi fi-rr-copy\"></i>");
                renderer.Write("</button>");
            }

            // Code Content Wrapper
            // We strip the default <pre> styles Markdig might add if we rely on our wrapper.
            // Markdig's base.Write adds <pre><code ...
            // We want to add attributes to <pre> or <code>?
            // Actually, base.Write adds attributes from obj.GetAttributes() to the <pre> tag.

            var attributes = obj.GetAttributes();

            // Add custom class for styling the inner pre/code
            attributes.AddClass("!my-0 !rounded-none !bg-transparent !border-0 overflow-x-auto p-4 font-mono text-sm"); // Override prose defaults

            // Line Numbers Classes
            if (enableLineNumbers)
            {
                attributes.AddClass("line-numbers");
            }
            if (disableLineNumbers)
            {
                attributes.AddClass("no-line-numbers");
            }

            // Store highlight info in data attribute
            if (!string.IsNullOrEmpty(highlight))
            {
                attributes.AddProperty("data-highlight", highlight);
            }

            // Write the actual code
            // Note: base.Write writes <pre><code>...</code></pre>
            base.Write(renderer, obj);

            renderer.Write("</div>"); // End Wrapper

            // End Tesserae Tab
            if ((fencedBlock.Info ?? "").ToLower() == "csharp" && obj.GetAttributes()?.Classes?.Contains("tesserae-code") == true)
            {
                renderer.Write("</div>"); // Close tab-content
                renderer.Write("</div>"); // Close p-4
                renderer.Write("</div>"); // Close outer my-4 border
            }
        }

        // ---- csharp-docs helpers ----

        private sealed class CSharpXmlDoc
        {
            public string Summary = "";
            public string Returns = "";
            public string Remarks = "";
            public readonly List<(string Name, string Text)> Params = new();
            public readonly List<(string Name, string Text)> TypeParams = new();
            public readonly List<(string Name, string Text)> Exceptions = new();
        }

        private static bool HasXmlDoc(SyntaxNode node) => GetXmlDoc(node) != null;

        private static DocumentationCommentTriviaSyntax GetXmlDoc(SyntaxNode node)
        {
            return node.GetLeadingTrivia()
                .Select(i => i.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();
        }

        private static string ReadElementText(XmlElementSyntax el)
        {
            return string.Join("", el.Content
                .OfType<XmlTextSyntax>()
                .SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim();
        }

        private static CSharpXmlDoc ParseXmlDoc(DocumentationCommentTriviaSyntax xml)
        {
            var doc = new CSharpXmlDoc();
            if (xml == null) return doc;

            foreach (var el in xml.ChildNodes().OfType<XmlElementSyntax>())
            {
                var tag = el.StartTag.Name.ToString();
                switch (tag)
                {
                    case "summary": doc.Summary = ReadElementText(el); break;
                    case "returns": doc.Returns = ReadElementText(el); break;
                    case "remarks": doc.Remarks = ReadElementText(el); break;
                    case "param":
                    {
                        var name = el.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText ?? "";
                        doc.Params.Add((name, ReadElementText(el)));
                        break;
                    }
                    case "typeparam":
                    {
                        var name = el.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText ?? "";
                        doc.TypeParams.Add((name, ReadElementText(el)));
                        break;
                    }
                    case "exception":
                    {
                        var name = el.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault()?.Cref.ToFullString().Trim()
                                 ?? el.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText
                                 ?? "";
                        doc.Exceptions.Add((name, ReadElementText(el)));
                        break;
                    }
                }
            }
            return doc;
        }

        private static string NormalizeSignatureWhitespace(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = Regex.Replace(s, @"\s+", " ").Trim();
            // Drop a trailing semicolon left over from declarations without a body.
            while (s.EndsWith(";")) s = s.Substring(0, s.Length - 1).TrimEnd();
            return s;
        }

        private static string GetTypeName(BaseTypeDeclarationSyntax type)
        {
            var name = type.Identifier.Text;
            if (type is TypeDeclarationSyntax t && t.TypeParameterList != null)
            {
                name += t.TypeParameterList.ToString();
            }
            return name;
        }

        private static string GetTypeKindLabel(BaseTypeDeclarationSyntax type) => type switch
        {
            InterfaceDeclarationSyntax => "interface",
            StructDeclarationSyntax => "struct",
            RecordDeclarationSyntax r => r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record",
            EnumDeclarationSyntax => "enum",
            ClassDeclarationSyntax => "class",
            _ => "type",
        };

        private static string BuildTypeSignature(BaseTypeDeclarationSyntax type)
        {
            var sb = new StringBuilder();
            // Modifiers
            foreach (var mod in type.Modifiers)
            {
                sb.Append(mod.Text).Append(' ');
            }
            // Keyword (class/struct/interface/record/enum)
            if (type is TypeDeclarationSyntax td)
            {
                if (type is RecordDeclarationSyntax rec && !rec.ClassOrStructKeyword.IsKind(SyntaxKind.None))
                {
                    sb.Append(td.Keyword.Text).Append(' ').Append(rec.ClassOrStructKeyword.Text).Append(' ');
                }
                else
                {
                    sb.Append(td.Keyword.Text).Append(' ');
                }
                sb.Append(td.Identifier.Text);
                if (td.TypeParameterList != null) sb.Append(td.TypeParameterList.ToString());
                if (td.BaseList != null) sb.Append(' ').Append(td.BaseList.ToString());
                foreach (var c in td.ConstraintClauses) sb.Append(' ').Append(c.ToString());
            }
            else if (type is EnumDeclarationSyntax en)
            {
                sb.Append(en.EnumKeyword.Text).Append(' ').Append(en.Identifier.Text);
                if (en.BaseList != null) sb.Append(' ').Append(en.BaseList.ToString());
            }
            else
            {
                sb.Append(type.Identifier.Text);
            }
            return NormalizeSignatureWhitespace(sb.ToString());
        }

        private static string BuildMemberSignature(MemberDeclarationSyntax node)
        {
            MemberDeclarationSyntax sig = node;
            if (node is BaseMethodDeclarationSyntax method)
            {
                sig = method
                    .WithBody(null)
                    .WithExpressionBody(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                    .WithAttributeLists(default);
            }
            else if (node is PropertyDeclarationSyntax prop)
            {
                sig = prop
                    .WithAccessorList(StripAccessorBodies(prop.AccessorList))
                    .WithExpressionBody(null)
                    .WithInitializer(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                    .WithAttributeLists(default);
            }
            else if (node is IndexerDeclarationSyntax indexer)
            {
                sig = indexer
                    .WithAccessorList(StripAccessorBodies(indexer.AccessorList))
                    .WithExpressionBody(null)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                    .WithAttributeLists(default);
            }
            else if (node is EventDeclarationSyntax evt)
            {
                sig = evt
                    .WithAccessorList(null)
                    .WithAttributeLists(default);
            }
            else if (node is EventFieldDeclarationSyntax evtField)
            {
                sig = evtField.WithAttributeLists(default);
            }
            else if (node is FieldDeclarationSyntax field)
            {
                sig = field.WithAttributeLists(default);
            }
            else if (node is DelegateDeclarationSyntax del)
            {
                sig = del.WithAttributeLists(default);
            }
            else if (node is EnumMemberDeclarationSyntax em)
            {
                sig = em.WithAttributeLists(default);
            }

            var raw = sig.WithoutTrivia().ToFullString();
            var normalized = NormalizeSignatureWhitespace(raw);
            if (node is PropertyDeclarationSyntax || node is IndexerDeclarationSyntax)
            {
                normalized = Regex.Replace(normalized, @";(?=\S)", "; ");
            }
            return normalized;
        }

        private static AccessorListSyntax StripAccessorBodies(AccessorListSyntax accessors)
        {
            if (accessors == null) return null;
            var stripped = accessors.Accessors.Select(a => a
                .WithAttributeLists(default)
                .WithBody(null)
                .WithExpressionBody(null)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            return accessors.WithAccessors(SyntaxFactory.List(stripped));
        }

        private static string GetMemberName(MemberDeclarationSyntax node) => node switch
        {
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            DestructorDeclarationSyntax d => "~" + d.Identifier.Text,
            MethodDeclarationSyntax m => m.Identifier.Text + (m.TypeParameterList?.ToString() ?? ""),
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax i => "this[" + string.Join(", ", i.ParameterList.Parameters.Select(p => p.Type?.ToString() ?? "")) + "]",
            EventDeclarationSyntax e => e.Identifier.Text,
            EventFieldDeclarationSyntax ef => string.Join(", ", ef.Declaration.Variables.Select(v => v.Identifier.Text)),
            FieldDeclarationSyntax f => string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.Text)),
            DelegateDeclarationSyntax d => d.Identifier.Text + (d.TypeParameterList?.ToString() ?? ""),
            EnumMemberDeclarationSyntax em => em.Identifier.Text,
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax conv => conv.ImplicitOrExplicitKeyword.Text + " operator " + (conv.Type?.ToString() ?? ""),
            _ => "",
        };

        private static (string Group, string Badge) GetMemberKind(MemberDeclarationSyntax node) => node switch
        {
            ConstructorDeclarationSyntax => ("constructors", "Constructor"),
            DestructorDeclarationSyntax => ("constructors", "Destructor"),
            PropertyDeclarationSyntax => ("properties", "Property"),
            IndexerDeclarationSyntax => ("properties", "Indexer"),
            FieldDeclarationSyntax => ("fields", "Field"),
            EventDeclarationSyntax => ("events", "Event"),
            EventFieldDeclarationSyntax => ("events", "Event"),
            EnumMemberDeclarationSyntax => ("fields", "Value"),
            OperatorDeclarationSyntax => ("methods", "Operator"),
            ConversionOperatorDeclarationSyntax => ("methods", "Conversion"),
            MethodDeclarationSyntax => ("methods", "Method"),
            DelegateDeclarationSyntax => ("methods", "Delegate"),
            _ => ("methods", "Member"),
        };

        private static string SlugifyId(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.') sb.Append(ch);
                else if (ch == ' ') sb.Append('-');
            }
            return sb.ToString();
        }

        // A base type whose name matches the interface naming convention (I followed by an
        // uppercase letter, e.g. IDisposable) is treated as an implemented interface.
        private static bool LooksLikeInterface(string name)
        {
            var bare = name;
            var lt = bare.IndexOf('<');
            if (lt >= 0) bare = bare.Substring(0, lt);
            var dot = bare.LastIndexOf('.');
            if (dot >= 0) bare = bare.Substring(dot + 1);
            return bare.Length >= 2 && bare[0] == 'I' && char.IsUpper(bare[1]);
        }

        // Renders the Microsoft Learn-style "Definition" block: namespace, inheritance chain,
        // and implemented interfaces. Skipped entirely when there is nothing to show.
        private static void RenderCSharpDefinition(HtmlRenderer renderer, BaseTypeDeclarationSyntax type, string typeName, string namespaceName)
        {
            string baseClass = null;
            var implements = new List<string>();

            if (type.BaseList != null && !(type is EnumDeclarationSyntax))
            {
                var bases = type.BaseList.Types.Select(t => t.Type.ToString()).ToList();
                if (type is InterfaceDeclarationSyntax)
                {
                    implements.AddRange(bases);
                }
                else
                {
                    foreach (var b in bases)
                    {
                        if (baseClass == null && !LooksLikeInterface(b)) baseClass = b;
                        else implements.Add(b);
                    }
                }
            }

            var hasNamespace = !string.IsNullOrEmpty(namespaceName);
            if (!hasNamespace && baseClass == null && implements.Count == 0) return;

            renderer.Write("<dl class=\"csharp-definition grid grid-cols-[max-content_1fr] gap-x-4 gap-y-1 text-sm mb-6 pl-3 border-l-2 border-gray-200 dark:border-gray-700\">");

            void Row(string label, string value, bool mono)
            {
                renderer.Write($"<dt class=\"font-semibold text-gray-500 dark:text-gray-400\">{System.Net.WebUtility.HtmlEncode(label)}</dt>");
                var cls = mono ? "font-mono text-gray-800 dark:text-gray-200" : "text-gray-800 dark:text-gray-200";
                renderer.Write($"<dd class=\"m-0 {cls}\">{System.Net.WebUtility.HtmlEncode(value)}</dd>");
            }

            if (hasNamespace) Row("Namespace", namespaceName, mono: false);
            if (baseClass != null) Row("Inheritance", $"{baseClass} → {typeName}", mono: true);
            if (implements.Count > 0) Row("Implements", string.Join(", ", implements), mono: true);

            renderer.Write("</dl>");
        }

        // Renders a Microsoft Learn / DocFX-style summary table (Name → anchor link, Description)
        // that sits above the detailed member entries for a group.
        private static void RenderCSharpMemberTable(HtmlRenderer renderer, List<MemberDeclarationSyntax> members, string classPrefix)
        {
            renderer.Write("<div class=\"csharp-member-table overflow-x-auto mb-6 rounded-md border border-gray-200 dark:border-white/10\">");
            renderer.Write("<table class=\"w-full text-sm border-collapse m-0\">");
            renderer.Write("<thead><tr class=\"bg-gray-50 dark:bg-gray-800/60 text-left\">");
            renderer.Write("<th class=\"font-semibold text-gray-700 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-white/10\">Name</th>");
            renderer.Write("<th class=\"font-semibold text-gray-700 dark:text-gray-300 px-3 py-2 border-b border-gray-200 dark:border-white/10\">Description</th>");
            renderer.Write("</tr></thead><tbody>");

            foreach (var node in members)
            {
                var memberName = GetMemberName(node);
                // Must match the detail anchor produced by RenderCSharpMember.
                var anchor = SlugifyId(!string.IsNullOrEmpty(classPrefix)
                    ? $"{classPrefix}.{memberName}"
                    : memberName);
                var summary = ParseXmlDoc(GetXmlDoc(node)).Summary;

                renderer.Write("<tr class=\"border-b border-gray-100 dark:border-white/5 last:border-0 align-top\">");
                renderer.Write($"<td class=\"px-3 py-2 whitespace-nowrap\"><a href=\"#{System.Net.WebUtility.HtmlEncode(anchor)}\" class=\"font-mono text-primary-600 dark:text-primary-400 no-underline hover:underline\">{System.Net.WebUtility.HtmlEncode(memberName)}</a></td>");
                renderer.Write($"<td class=\"px-3 py-2 text-gray-700 dark:text-gray-300\">{System.Net.WebUtility.HtmlEncode(summary)}</td>");
                renderer.Write("</tr>");
            }

            renderer.Write("</tbody></table></div>");
        }

        private static void RenderCSharpType(HtmlRenderer renderer, BaseTypeDeclarationSyntax type, string namespaceName = "")
        {
            var xml = GetXmlDoc(type);
            var doc = ParseXmlDoc(xml);
            var typeName = GetTypeName(type);
            var simpleName = type.Identifier.Text;
            var kindLabel = GetTypeKindLabel(type);
            var signature = BuildTypeSignature(type);
            var anchor = SlugifyId(simpleName);

            renderer.Write($"<section class=\"csharp-type mb-12\" id=\"{System.Net.WebUtility.HtmlEncode(anchor)}\">");

            // Sticky type header
            renderer.Write("<header class=\"csharp-type-header sticky top-0 z-10 -mx-2 px-2 pt-4 pb-3 mb-6 bg-white/95 dark:bg-gray-900/95 backdrop-blur border-b border-gray-200 dark:border-gray-700\">");

            renderer.Write("<div class=\"flex items-center gap-2 flex-wrap\">");
            renderer.Write($"<span class=\"csharp-kind-badge inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold uppercase tracking-wide bg-primary-100 text-primary-800 dark:bg-primary-900/40 dark:text-primary-300\">{System.Net.WebUtility.HtmlEncode(kindLabel)}</span>");
            renderer.Write($"<h3 class=\"text-2xl font-semibold m-0 text-gray-900 dark:text-gray-100\">{System.Net.WebUtility.HtmlEncode(typeName)}");
            renderer.Write($"<a href=\"#{System.Net.WebUtility.HtmlEncode(anchor)}\" class=\"csharp-anchor no-underline text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 ml-2 text-xs align-middle\" aria-label=\"Permalink\"><i class=\"fi fi-rr-link\"></i></a>");
            renderer.Write("</h3>");
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(signature))
            {
                renderer.Write("<div class=\"bg-gray-50 dark:bg-[#1a1a1a] rounded-md p-3 font-mono text-sm text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-white/10 overflow-x-auto mt-3\">");
                renderer.Write($"<pre class=\"!m-0 !p-0 !bg-transparent !border-0\"><code>{System.Net.WebUtility.HtmlEncode(signature)}</code></pre>");
                renderer.Write("</div>");
            }

            if (!string.IsNullOrEmpty(doc.Summary))
            {
                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 mt-3 mb-0 leading-relaxed\">{System.Net.WebUtility.HtmlEncode(doc.Summary)}</p>");
            }

            RenderXmlDocDetails(renderer, doc, includeSummary: false, includeReturns: false, compact: true);

            renderer.Write("</header>");

            // Microsoft Learn-style "Definition" metadata: namespace, inheritance, and
            // implemented interfaces derived from the declaration.
            RenderCSharpDefinition(renderer, type, typeName, namespaceName);

            // Group child members by kind
            if (type is TypeDeclarationSyntax td)
            {
                var groups = new[]
                {
                    ("constructors", "Constructors"),
                    ("properties", "Properties"),
                    ("methods", "Methods"),
                    ("events", "Events"),
                    ("fields", "Fields"),
                };

                var members = td.Members
                    .Where(HasXmlDoc)
                    .Where(m => !(m is BaseTypeDeclarationSyntax))
                    .ToList();

                foreach (var (groupKey, groupLabel) in groups)
                {
                    var inGroup = members.Where(m => GetMemberKind(m).Group == groupKey).ToList();
                    if (inGroup.Count == 0) continue;

                    renderer.Write("<div class=\"csharp-member-group mt-8\">");
                    renderer.Write($"<h4 class=\"text-xl font-semibold mb-4 text-gray-900 dark:text-gray-100\">{groupLabel}</h4>");
                    RenderCSharpMemberTable(renderer, inGroup, simpleName);
                    foreach (var m in inGroup)
                    {
                        RenderCSharpMember(renderer, m, classPrefix: simpleName);
                    }
                    renderer.Write("</div>");
                }
            }
            else if (type is EnumDeclarationSyntax en)
            {
                var values = en.Members.Where(HasXmlDoc).ToList();
                if (values.Count > 0)
                {
                    renderer.Write("<div class=\"csharp-member-group mt-8\">");
                    renderer.Write("<h4 class=\"text-xl font-semibold mb-4 text-gray-900 dark:text-gray-100\">Values</h4>");
                    RenderCSharpMemberTable(renderer, values.Cast<MemberDeclarationSyntax>().ToList(), simpleName);
                    foreach (var v in values)
                    {
                        RenderCSharpMember(renderer, v, classPrefix: simpleName);
                    }
                    renderer.Write("</div>");
                }
            }

            renderer.Write("</section>");
        }

        private static void RenderCSharpMember(HtmlRenderer renderer, MemberDeclarationSyntax node, string classPrefix)
        {
            var xml = GetXmlDoc(node);
            if (xml == null) return;
            var doc = ParseXmlDoc(xml);

            var memberName = GetMemberName(node);
            var (_, badge) = GetMemberKind(node);
            var signature = BuildMemberSignature(node);
            var displayName = !string.IsNullOrEmpty(classPrefix) && !(node is ConstructorDeclarationSyntax)
                ? $"{classPrefix}.{memberName}"
                : memberName;
            // For constructors, display class name (which matches the constructor identifier).
            var anchor = SlugifyId(!string.IsNullOrEmpty(classPrefix) ? $"{classPrefix}.{memberName}" : memberName);

            renderer.Write($"<div class=\"csharp-member-doc mb-8\" id=\"{System.Net.WebUtility.HtmlEncode(anchor)}\">");

            renderer.Write("<div class=\"flex items-center gap-2 flex-wrap mb-2\">");
            renderer.Write($"<span class=\"csharp-kind-badge inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold uppercase tracking-wide bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300\">{System.Net.WebUtility.HtmlEncode(badge)}</span>");
            renderer.Write($"<h5 class=\"text-lg font-semibold m-0 text-gray-900 dark:text-gray-100\">{System.Net.WebUtility.HtmlEncode(displayName)}");
            renderer.Write($"<a href=\"#{System.Net.WebUtility.HtmlEncode(anchor)}\" class=\"csharp-anchor no-underline text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 ml-2 text-xs align-middle\" aria-label=\"Permalink\"><i class=\"fi fi-rr-link\"></i></a>");
            renderer.Write("</h5>");
            renderer.Write("</div>");

            if (!string.IsNullOrEmpty(signature))
            {
                renderer.Write("<div class=\"bg-gray-50 dark:bg-[#1a1a1a] rounded-md p-3 font-mono text-sm text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-white/10 overflow-x-auto my-3\">");
                renderer.Write($"<pre class=\"!m-0 !p-0 !bg-transparent !border-0\"><code>{System.Net.WebUtility.HtmlEncode(signature)}</code></pre>");
                renderer.Write("</div>");
            }

            if (!string.IsNullOrEmpty(doc.Summary))
            {
                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 my-3 leading-relaxed\">{System.Net.WebUtility.HtmlEncode(doc.Summary)}</p>");
            }

            RenderXmlDocDetails(renderer, doc, includeSummary: false, includeReturns: true, compact: false);

            renderer.Write("</div>");
        }

        private static void RenderXmlDocDetails(HtmlRenderer renderer, CSharpXmlDoc doc, bool includeSummary, bool includeReturns, bool compact)
        {
            string h4 = compact
                ? "font-semibold text-sm uppercase tracking-wide mt-3 mb-1 text-gray-500 dark:text-gray-400"
                : "font-semibold text-base mt-4 mb-2 text-gray-900 dark:text-gray-100";

            if (includeSummary && !string.IsNullOrEmpty(doc.Summary))
            {
                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 my-3 leading-relaxed\">{System.Net.WebUtility.HtmlEncode(doc.Summary)}</p>");
            }

            if (doc.TypeParams.Any())
            {
                renderer.Write($"<h4 class=\"{h4}\">Type Parameters</h4>");
                renderer.Write("<dl class=\"mt-1 mb-3 space-y-2\">");
                foreach (var p in doc.TypeParams)
                {
                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-1 sm:gap-3\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{System.Net.WebUtility.HtmlEncode(p.Name)}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{System.Net.WebUtility.HtmlEncode(p.Text)}</dd></div>");
                }
                renderer.Write("</dl>");
            }

            if (doc.Params.Any())
            {
                renderer.Write($"<h4 class=\"{h4}\">Parameters</h4>");
                renderer.Write("<dl class=\"mt-1 mb-3 space-y-2\">");
                foreach (var p in doc.Params)
                {
                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-1 sm:gap-3\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{System.Net.WebUtility.HtmlEncode(p.Name)}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{System.Net.WebUtility.HtmlEncode(p.Text)}</dd></div>");
                }
                renderer.Write("</dl>");
            }

            if (includeReturns && !string.IsNullOrEmpty(doc.Returns))
            {
                renderer.Write($"<h4 class=\"{h4}\">Returns</h4>");
                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 mb-3\">{System.Net.WebUtility.HtmlEncode(doc.Returns)}</p>");
            }

            if (doc.Exceptions.Any())
            {
                renderer.Write($"<h4 class=\"{h4}\">Exceptions</h4>");
                renderer.Write("<dl class=\"mt-1 mb-3 space-y-2\">");
                foreach (var p in doc.Exceptions)
                {
                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-1 sm:gap-3\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{System.Net.WebUtility.HtmlEncode(p.Name)}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{System.Net.WebUtility.HtmlEncode(p.Text)}</dd></div>");
                }
                renderer.Write("</dl>");
            }

            if (!string.IsNullOrEmpty(doc.Remarks))
            {
                renderer.Write($"<h4 class=\"{h4}\">Remarks</h4>");
                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 mb-3\">{System.Net.WebUtility.HtmlEncode(doc.Remarks)}</p>");
            }
        }
    }

    public class CodeBlockExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                var codeBlockRenderer = htmlRenderer.ObjectRenderers.FindExact<Markdig.Renderers.Html.CodeBlockRenderer>();
                if (codeBlockRenderer != null)
                {
                    htmlRenderer.ObjectRenderers.Remove(codeBlockRenderer);
                    htmlRenderer.ObjectRenderers.Add(new CodeBlockRenderer());
                }
                else
                {
                     htmlRenderer.ObjectRenderers.Add(new CodeBlockRenderer());
                }
            }
        }
    }
}
