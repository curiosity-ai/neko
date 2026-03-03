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

                    Neko.Builder.TesseraeCompilerResult result = null;
                    try
                    {
                        result = Neko.Builder.TesseraeCompiler.CompileAsync(fencedBlock.Arguments, codeString, siteOutputRoot).GetAwaiter().GetResult(); //Can't use async as Markdig doesn't expose an async method
                    }
                    catch (System.Exception ex)
                    {
                        result = new Builder.TesseraeCompilerResult()
                        {
                            OutputHtml = $"<div class=\"text-red-500 font-bold p-4 border border-red-500 rounded my-4\">Tesserae compilation failed:<br/><pre>{ex.Message}</pre></div>"
                        };
                    }

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
                renderer.Write("<div class=\"mermaid flex justify-center bg-gray-50 dark:bg-gray-800 p-4 rounded-md border border-gray-200 dark:border-gray-700 overflow-x-auto my-6\">");
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
                renderer.Write($"const graph = ForceGraph() (container) .graphData(gData) .nodeId('id') .nodeLabel('id') .linkLabel('name') .linkDirectionalParticles(2) .linkCurvature(0.3) .nodeColor(() => '#4a5568') .linkColor(() => '#cbd5e0') .backgroundColor('#f9fafb') .width(width) .height(height);");
                renderer.Write($"new ResizeObserver(() => {{ graph.width(container.clientWidth).height(container.clientHeight); }}).observe(container); }})()");
                renderer.Write("</script>");
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
