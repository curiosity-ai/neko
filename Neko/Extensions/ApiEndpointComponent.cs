using Markdig.Renderers;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Neko.Extensions
{
    /// <summary>
    /// Renders a REST API endpoint reference entry in the Microsoft Learn / Swagger
    /// style: a colour-coded HTTP method badge, the monospace path, a summary, and a
    /// labelled details grid (Auth, Body, Returns, …). Authored as a fenced
    /// <c>```endpoint</c> block (dispatched from <see cref="CodeBlockRenderer"/>).
    ///
    /// Body format: the first non-empty line is <c>METHOD /path</c> (the method is
    /// optional). The lines after it, up to the first blank line (or the first
    /// <c>Label:</c> line), are the summary; every later non-empty line is a
    /// <c>Label: value</c> detail row. The method/path lives in the body — not the
    /// fence info line — so paths containing <c>{placeholders}</c> are not eaten by
    /// the generic-attributes parser. Summary and values render as inline Markdown,
    /// so <c>`code`</c>, <c>**bold**</c>, and links work.
    /// </summary>
    public static class ApiEndpointComponent
    {
        public static void Write(HtmlRenderer renderer, FencedCodeBlock block)
        {
            var raw = ReadLines(block);
            // First non-empty line is the "METHOD /path" header.
            int start = 0;
            while (start < raw.Count && string.IsNullOrWhiteSpace(raw[start])) start++;
            var header = start < raw.Count ? raw[start].Trim() : string.Empty;
            var (method, path) = SplitMethodPath(header);
            var (summary, details) = ParseBody(raw, start + 1);

            var anchor = Slugify((string.IsNullOrEmpty(method) ? "" : method + "-") + path);

            // The component carries its own CSS (emitted once in the page <head>), so it
            // does not depend on Tailwind utility classes being available.
            renderer.Write($"<div class=\"api-endpoint not-prose\" id=\"{WebUtility.HtmlEncode(anchor)}\">");

            // Header: method badge + path (+ permalink anchor)
            renderer.Write("<div class=\"api-endpoint-header\">");
            if (!string.IsNullOrEmpty(method))
            {
                renderer.Write($"<span class=\"api-method\" data-method=\"{WebUtility.HtmlEncode(method)}\">{WebUtility.HtmlEncode(method)}</span>");
            }
            renderer.Write($"<code class=\"api-path\">{WebUtility.HtmlEncode(path)}</code>");
            if (!string.IsNullOrEmpty(anchor))
            {
                renderer.Write($"<a href=\"#{WebUtility.HtmlEncode(anchor)}\" class=\"api-anchor\" aria-label=\"Permalink\"><i class=\"fi fi-rr-link\"></i></a>");
            }
            renderer.Write("</div>");

            // Body: summary + details
            renderer.Write("<div class=\"api-endpoint-body\">");

            if (!string.IsNullOrEmpty(summary))
            {
                renderer.Write($"<p class=\"api-summary\">{Inline(summary)}</p>");
            }

            if (details.Count > 0)
            {
                renderer.Write("<dl class=\"api-details\">");
                foreach (var (label, value) in details)
                {
                    renderer.Write($"<dt>{WebUtility.HtmlEncode(label)}</dt>");
                    renderer.Write($"<dd>{Inline(value)}</dd>");
                }
                renderer.Write("</dl>");
            }

            renderer.Write("</div>"); // body
            renderer.Write("</div>"); // api-endpoint
        }

        private static (string Method, string Path) SplitMethodPath(string args)
        {
            if (string.IsNullOrWhiteSpace(args)) return ("", "");
            var parts = args.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            // A leading token is treated as the HTTP method only when it looks like one.
            if (parts.Length == 2 && IsHttpMethod(parts[0]))
            {
                return (parts[0].ToUpperInvariant(), parts[1].Trim());
            }
            return ("", args);
        }

        private static bool IsHttpMethod(string s) => s.ToUpperInvariant() switch
        {
            "GET" or "POST" or "PUT" or "PATCH" or "DELETE" or "HEAD" or "OPTIONS" or "TRACE" or "CONNECT" => true,
            _ => false,
        };

        private static readonly Regex LabelLine = new(@"^\s*([A-Za-z][A-Za-z0-9 _/+-]{0,24}):\s+(.+)$", RegexOptions.Compiled);

        private static List<string> ReadLines(FencedCodeBlock block)
        {
            var result = new List<string>();
            var lines = block.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                result.Add(lines.Lines[i].Slice.ToString() ?? string.Empty);
            }
            return result;
        }

        private static (string Summary, List<(string Label, string Value)> Details) ParseBody(List<string> lines, int from)
        {
            var summary = new StringBuilder();
            var details = new List<(string, string)>();
            var inDetails = false;

            for (int i = from; i < lines.Count; i++)
            {
                var raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw))
                {
                    // A blank line ends the summary and switches to the details region.
                    if (summary.Length > 0) inDetails = true;
                    continue;
                }

                var m = LabelLine.Match(raw);
                if (!inDetails && m.Success)
                {
                    // First labelled line also ends the summary, even without a blank line.
                    inDetails = true;
                }

                if (inDetails)
                {
                    if (m.Success)
                    {
                        details.Add((m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim()));
                    }
                    else if (details.Count > 0)
                    {
                        // Continuation line: append to the previous detail value.
                        var last = details[details.Count - 1];
                        details[details.Count - 1] = (last.Item1, (last.Item2 + " " + raw.Trim()).Trim());
                    }
                }
                else
                {
                    if (summary.Length > 0) summary.Append(' ');
                    summary.Append(raw.Trim());
                }
            }

            return (summary.ToString().Trim(), details);
        }

        // Render a single line/snippet of inline Markdown (code spans, links, emphasis),
        // stripping the wrapping <p> that the block renderer adds.
        private static string Inline(string md)
        {
            if (string.IsNullOrWhiteSpace(md)) return "";
            var html = Markdig.Markdown.ToHtml(md.Trim()).Trim();
            if (html.StartsWith("<p>") && html.EndsWith("</p>"))
            {
                html = html.Substring(3, html.Length - 7).Trim();
            }
            return html;
        }

        private static string Slugify(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            char prev = '\0';
            foreach (var ch in s.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch)) { sb.Append(ch); prev = ch; }
                else if (prev != '-' && sb.Length > 0) { sb.Append('-'); prev = '-'; }
            }
            return sb.ToString().Trim('-');
        }
    }
}
