using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

            // Handle CSharp Docs
            if ((fencedBlock.Info ?? "").ToLower() == "csharp-docs")
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
                    var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(codeString);
                    var root = tree.GetRoot();

                    renderer.Write("<div class=\"csharp-docs my-8\">");

                    foreach (var node in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax>())
                    {
                        var xml = node.GetLeadingTrivia()
                            .Select(i => i.GetStructure())
                            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.DocumentationCommentTriviaSyntax>()
                            .FirstOrDefault();

                        if (xml != null)
                        {
                            var summary = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");
                            var summaryText = summary != null ? string.Join("", summary.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim() : "";

                            var paramsDict = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().Where(e => e.StartTag.Name.ToString() == "param")
                                .Select(e => new {
                                    Name = e.StartTag.Attributes.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText,
                                    Text = string.Join("", e.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim()
                                }).ToList();

                            var returns = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.ToString() == "returns");
                            var returnsText = returns != null ? string.Join("", returns.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim() : "";

                            var remarks = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().FirstOrDefault(e => e.StartTag.Name.ToString() == "remarks");
                            var remarksText = remarks != null ? string.Join("", remarks.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim() : "";

                            var typeparamsDict = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().Where(e => e.StartTag.Name.ToString() == "typeparam")
                                .Select(e => new {
                                    Name = e.StartTag.Attributes.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText,
                                    Text = string.Join("", e.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim()
                                }).ToList();

                            var exceptionsDict = xml.ChildNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlElementSyntax>().Where(e => e.StartTag.Name.ToString() == "exception")
                                .Select(e => new {
                                    Name = e.StartTag.Attributes.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlCrefAttributeSyntax>().FirstOrDefault()?.Cref.ToFullString().Trim() ??
                                           e.StartTag.Attributes.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText,
                                    Text = string.Join("", e.Content.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.XmlTextSyntax>().SelectMany(t => t.TextTokens.Select(tk => tk.ValueText))).Trim()
                                }).ToList();

                            var signature = "";
                            var name = "";

                            if (node is Microsoft.CodeAnalysis.CSharp.Syntax.BaseMethodDeclarationSyntax methodLikeNode)
                            {
                                var sigNode = methodLikeNode.WithBody(null).WithExpressionBody(null).WithSemicolonToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None));
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(sigNode).ToFullString().Trim();
                            }
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propNode)
                            {
                                var sigNode = propNode.WithAccessorList(null).WithExpressionBody(null).WithSemicolonToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None));
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(sigNode).ToFullString().Trim();
                            }
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax typeNode)
                            {
                                var sigNode = typeNode.WithOpenBraceToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None)).WithCloseBraceToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None));
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(sigNode).ToFullString().Trim();
                                if (signature.Contains("{")) signature = signature.Substring(0, signature.IndexOf("{")).Trim();
                            }
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax enumNode)
                            {
                                var sigNode = enumNode.WithOpenBraceToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None)).WithCloseBraceToken(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(Microsoft.CodeAnalysis.CSharp.SyntaxKind.None));
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(sigNode).ToFullString().Trim();
                                if (signature.Contains("{")) signature = signature.Substring(0, signature.IndexOf("{")).Trim();
                            }
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.DelegateDeclarationSyntax delegateNode)
                            {
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(delegateNode).ToFullString().Trim();
                            }
                            else
                            {
                                signature = Microsoft.CodeAnalysis.SyntaxNodeExtensions.WithoutTrivia(node).ToFullString().Trim();
                                if (signature.Contains("{")) signature = signature.Substring(0, signature.IndexOf("{")).Trim();
                            }

                            if (node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classNode) name = classNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax interfaceNode) name = interfaceNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax structNode) name = structNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.RecordDeclarationSyntax recordNode) name = recordNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax enumNode2) name = enumNode2.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.DelegateDeclarationSyntax delegateNode2) name = delegateNode2.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodNode) name = methodNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax propNode2) name = propNode2.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax fieldNode) name = string.Join(", ", fieldNode.Declaration.Variables.Select(v => v.Identifier.Text));
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.EventDeclarationSyntax eventNode) name = eventNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax ctorNode) name = ctorNode.Identifier.Text;
                            else if (node is Microsoft.CodeAnalysis.CSharp.Syntax.EnumMemberDeclarationSyntax enumMember) name = enumMember.Identifier.Text;

                            var encodedName = System.Net.WebUtility.HtmlEncode(name);
                            var encodedSignature = System.Net.WebUtility.HtmlEncode(signature);
                            var encodedSummary = System.Net.WebUtility.HtmlEncode(summaryText);
                            var encodedReturns = System.Net.WebUtility.HtmlEncode(returnsText);
                            var encodedRemarks = System.Net.WebUtility.HtmlEncode(remarksText);

                            renderer.Write($"<div class=\"csharp-member-doc mb-12\" id=\"{encodedName}\">");
                            if (!string.IsNullOrEmpty(encodedName))
                            {
                                renderer.Write($"<h3 class=\"text-2xl font-semibold mt-8 mb-4 text-gray-900 dark:text-gray-100 border-b border-gray-200 dark:border-gray-700 pb-2\"><a href=\"#{encodedName}\" class=\"header-anchor text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 mr-2\"><i class=\"fi fi-rr-link\"></i></a>{encodedName}</h3>");
                            }

                            renderer.Write($"<div class=\"bg-gray-50 dark:bg-[#1a1a1a] rounded-md p-4 font-mono text-sm text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-white/10 overflow-x-auto my-4\">");
                            renderer.Write($"<pre class=\"!m-0 !p-0 !bg-transparent !border-0\"><code>{encodedSignature}</code></pre>");
                            renderer.Write("</div>");

                            if (!string.IsNullOrEmpty(encodedSummary))
                            {
                                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 my-4 leading-relaxed\">{encodedSummary}</p>");
                            }

                            if (typeparamsDict.Any())
                            {
                                renderer.Write("<h4 class=\"font-semibold text-lg mt-6 mb-2 text-gray-900 dark:text-gray-100\">Type Parameters</h4>");
                                renderer.Write("<dl class=\"mt-2 mb-4 space-y-4\">");
                                foreach (var p in typeparamsDict)
                                {
                                    var encodedPName = System.Net.WebUtility.HtmlEncode(p.Name);
                                    var encodedPText = System.Net.WebUtility.HtmlEncode(p.Text);
                                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-2 sm:gap-4\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{encodedPName}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{encodedPText}</dd></div>");
                                }
                                renderer.Write("</dl>");
                            }

                            if (paramsDict.Any())
                            {
                                renderer.Write("<h4 class=\"font-semibold text-lg mt-6 mb-2 text-gray-900 dark:text-gray-100\">Parameters</h4>");
                                renderer.Write("<dl class=\"mt-2 mb-4 space-y-4\">");
                                foreach (var p in paramsDict)
                                {
                                    var encodedPName = System.Net.WebUtility.HtmlEncode(p.Name);
                                    var encodedPText = System.Net.WebUtility.HtmlEncode(p.Text);
                                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-2 sm:gap-4\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{encodedPName}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{encodedPText}</dd></div>");
                                }
                                renderer.Write("</dl>");
                            }

                            if (!string.IsNullOrEmpty(encodedReturns))
                            {
                                renderer.Write("<h4 class=\"font-semibold text-lg mt-6 mb-2 text-gray-900 dark:text-gray-100\">Returns</h4>");
                                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 mb-4\">{encodedReturns}</p>");
                            }

                            if (exceptionsDict.Any())
                            {
                                renderer.Write("<h4 class=\"font-semibold text-lg mt-6 mb-2 text-gray-900 dark:text-gray-100\">Exceptions</h4>");
                                renderer.Write("<dl class=\"mt-2 mb-4 space-y-4\">");
                                foreach (var p in exceptionsDict)
                                {
                                    var encodedPName = System.Net.WebUtility.HtmlEncode(p.Name);
                                    var encodedPText = System.Net.WebUtility.HtmlEncode(p.Text);
                                    renderer.Write($"<div class=\"flex flex-col sm:flex-row gap-2 sm:gap-4\"><dt class=\"font-mono text-sm font-semibold text-primary-600 dark:text-primary-400 min-w-[120px]\">{encodedPName}</dt><dd class=\"text-gray-700 dark:text-gray-300\">{encodedPText}</dd></div>");
                                }
                                renderer.Write("</dl>");
                            }

                            if (!string.IsNullOrEmpty(encodedRemarks))
                            {
                                renderer.Write("<h4 class=\"font-semibold text-lg mt-6 mb-2 text-gray-900 dark:text-gray-100\">Remarks</h4>");
                                renderer.Write($"<p class=\"text-gray-700 dark:text-gray-300 mb-4\">{encodedRemarks}</p>");
                            }

                            renderer.Write("</div>"); // Close csharp-member-doc
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
            renderer.Write("<div class=\"relative group my-6 bg-gray-50 dark:bg-[#1a1a1a] rounded-xl border border-gray-200 dark:border-white/10 shadow-sm overflow-hidden\">");

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
