using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    /// <summary>
    /// Deck-level options, read from the <c>presentation:</c> front-matter key that
    /// marks a Markdown file as a slide deck. The key accepts either a bare
    /// <c>true</c> (all defaults) or a mapping of the options below.
    /// </summary>
    public class PresentationOptions
    {
        /// <summary>Visual theme of the deck. <c>midnight</c> (default) or <c>daylight</c>.</summary>
        public string Theme { get; set; } = "midnight";

        /// <summary>Accent used by slides that don't set their own. One of cyan/amber/rose/leaf.</summary>
        public string Accent { get; set; } = "cyan";

        /// <summary>Eyebrow applied to slides that don't declare one. Optional.</summary>
        public string Eyebrow { get; set; }

        /// <summary>Where the "back" control goes. Defaults to the referring page, then the site root.</summary>
        public string Back { get; set; }

        /// <summary>Label of the back control.</summary>
        public string BackText { get; set; } = "Back";

        /// <summary>Blueprint grid behind the slides.</summary>
        public bool Grid { get; set; } = true;

        /// <summary>Thin progress rail pinned to the top of the viewport.</summary>
        public bool Progress { get; set; } = true;

        /// <summary>The "n / total" counter in the control bar.</summary>
        public bool Counter { get; set; } = true;

        /// <summary>The vertical slide gauge pinned to the left edge.</summary>
        public bool Gauge { get; set; } = true;

        /// <summary>
        /// Typeface source. <c>google</c> (default) pulls the deck's display/serif/mono
        /// trio from Google Fonts; <c>none</c> emits no font link and falls back to the
        /// local stacks, for air-gapped or self-hosted-font sites.
        /// </summary>
        public string Fonts { get; set; } = "google";

        /// <summary>Aspect ratio advertised to the <c>[!deck]</c> card. Informational.</summary>
        public string Ratio { get; set; } = "16:9";

        /// <summary>
        /// True when the front matter marks this document as a deck. Everything else on
        /// this object is only meaningful when this is set.
        /// </summary>
        public static bool IsPresentation(object node) => TryParse(node, out _);

        /// <summary>
        /// Reads the <c>presentation:</c> front-matter node. Returns false — with a null
        /// <paramref name="options"/> — when the node is absent or explicitly disabled
        /// (<c>false</c>, <c>no</c>, <c>off</c>, <c>none</c>).
        /// </summary>
        public static bool TryParse(object node, out PresentationOptions options)
        {
            options = null;
            if (node == null) return false;

            if (node is string scalar)
            {
                var v = scalar.Trim();
                if (v.Length == 0) return false;
                if (IsFalsey(v)) return false;
                options = new PresentationOptions();
                return true;
            }

            if (node is bool flag)
            {
                if (!flag) return false;
                options = new PresentationOptions();
                return true;
            }

            if (node is System.Collections.IDictionary map)
            {
                options = new PresentationOptions();
                foreach (System.Collections.DictionaryEntry entry in map)
                {
                    var key = Convert.ToString(entry.Key)?.Trim().ToLowerInvariant();
                    var value = entry.Value is string s ? s.Trim() : Convert.ToString(entry.Value)?.Trim();
                    if (string.IsNullOrEmpty(key)) continue;

                    switch (key)
                    {
                        case "enabled":
                            if (IsFalsey(value)) { options = null; return false; }
                            break;
                        case "theme": if (!string.IsNullOrEmpty(value)) options.Theme = value.ToLowerInvariant(); break;
                        case "accent": if (!string.IsNullOrEmpty(value)) options.Accent = value.ToLowerInvariant(); break;
                        case "eyebrow": options.Eyebrow = value; break;
                        case "back": options.Back = value; break;
                        case "backtext": case "back-text": options.BackText = value; break;
                        case "grid": options.Grid = !IsFalsey(value); break;
                        case "progress": options.Progress = !IsFalsey(value); break;
                        case "counter": options.Counter = !IsFalsey(value); break;
                        case "gauge": options.Gauge = !IsFalsey(value); break;
                        case "fonts": if (!string.IsNullOrEmpty(value)) options.Fonts = value.ToLowerInvariant(); break;
                        case "ratio": if (!string.IsNullOrEmpty(value)) options.Ratio = value; break;
                    }
                }
                return true;
            }

            return false;
        }

        private static bool IsFalsey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            switch (value.Trim().ToLowerInvariant())
            {
                case "false":
                case "no":
                case "off":
                case "none":
                case "0":
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>One slide: the Markdown between two separators, plus its own attributes.</summary>
    public class PresentationSlide
    {
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Markdown { get; set; } = string.Empty;
        public string Html { get; set; }

        public string Get(string key, string fallback = null)
            => Attributes.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }

    /// <summary>
    /// Splits a deck's Markdown body into slides. A deck is authored as a single
    /// self-contained file: slides are separated by a horizontal rule (<c>---</c>) on
    /// its own line, optionally carrying per-slide attributes in Neko's usual
    /// <c>{key="value"}</c> form.
    /// </summary>
    public static class PresentationParser
    {
        // A separator is a line of three-or-more dashes, optionally followed by an
        // attribute block. Anchored to the whole line so table rules (`|---|`),
        // front-matter fences inside fenced code, and `-- text` never match.
        private static readonly Regex SeparatorRegex = new Regex(
            @"^[ \t]*-{3,}[ \t]*(?<attrs>\{.*\})?[ \t]*$",
            RegexOptions.Compiled);

        private static readonly Regex FrontMatterRegex = new Regex(
            @"\A﻿?[ \t]*-{3,}[ \t]*\r?\n.*?\r?\n[ \t]*-{3,}[ \t]*(\r?\n|\z)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Removes the leading YAML front-matter block, leaving the deck body. Markdig
        /// consumes front matter during parsing, but the splitter works on raw source
        /// (so a slide's Markdown can be re-parsed on its own), and must not mistake
        /// the closing <c>---</c> for a slide separator.
        /// </summary>
        public static string StripFrontMatter(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return markdown ?? string.Empty;
            var match = FrontMatterRegex.Match(markdown);
            return match.Success ? markdown.Substring(match.Length) : markdown;
        }

        /// <summary>
        /// Splits the deck body into slides. The separator is only honoured at the top
        /// level: lines inside a fenced code block (``` or ~~~) are left alone, so an
        /// embedded SVG or HTML block can contain dashes freely. A separator must also
        /// be preceded by a blank line (or start the body), which keeps Markdown's
        /// setext headings (<c>Title\n---</c>) working inside a slide.
        /// </summary>
        public static List<PresentationSlide> Split(string body)
        {
            var slides = new List<PresentationSlide>();
            if (body == null) return slides;

            var lines = body.Replace("\r\n", "\n").Split('\n');
            var current = new PresentationSlide();
            var buffer = new List<string>();
            string fence = null;          // the fence string that opened the current code block
            var previousBlank = true;     // start-of-body counts as "preceded by a blank line"
            var seenContent = false;

            void Flush()
            {
                current.Markdown = string.Join("\n", buffer).Trim('\n');
                slides.Add(current);
                buffer = new List<string>();
            }

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                if (fence != null)
                {
                    if (trimmed.StartsWith(fence) && trimmed.TrimEnd().All(c => c == fence[0]))
                    {
                        fence = null;
                    }
                    buffer.Add(line);
                    previousBlank = false;
                    continue;
                }

                if (trimmed.StartsWith("```") || trimmed.StartsWith("~~~"))
                {
                    fence = trimmed.StartsWith("```") ? "```" : "~~~";
                    buffer.Add(line);
                    previousBlank = false;
                    seenContent = true;
                    continue;
                }

                var separator = previousBlank ? SeparatorRegex.Match(line) : Match.Empty;
                if (separator.Success)
                {
                    var attributes = ParseAttributes(separator.Groups["attrs"].Value);

                    // A separator before any content doesn't open an empty first slide —
                    // it just carries the first slide's attributes.
                    if (!seenContent && buffer.All(string.IsNullOrWhiteSpace))
                    {
                        current.Attributes = attributes;
                        buffer.Clear();
                    }
                    else
                    {
                        Flush();
                        current = new PresentationSlide { Attributes = attributes };
                    }

                    previousBlank = true;
                    continue;
                }

                buffer.Add(line);
                previousBlank = string.IsNullOrWhiteSpace(line);
                if (!previousBlank) seenContent = true;
            }

            if (buffer.Any(l => !string.IsNullOrWhiteSpace(l)) || slides.Count == 0)
            {
                Flush();
            }

            // Drop trailing empties so a deck that ends with a separator doesn't get a
            // blank slide pinned to the end.
            while (slides.Count > 1 && string.IsNullOrWhiteSpace(slides[^1].Markdown) && slides[^1].Attributes.Count == 0)
            {
                slides.RemoveAt(slides.Count - 1);
            }

            return slides;
        }

        // `{key="value" key2=value2 .class #id}` — the same shape Neko's generic
        // attributes and `::: container {…}` blocks already use.
        private static readonly Regex AttributeRegex = new Regex(
            @"(?<key>[A-Za-z_][\w-]*)\s*=\s*(""(?<q>[^""]*)""|'(?<q>[^']*)'|(?<v>[^\s}]+))|\.(?<class>[\w-]+)|#(?<id>[\w-]+)",
            RegexOptions.Compiled);

        internal static Dictionary<string, string> ParseAttributes(string raw)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var inner = raw.Trim();
            if (inner.StartsWith("{")) inner = inner.Substring(1);
            if (inner.EndsWith("}")) inner = inner.Substring(0, inner.Length - 1);

            var classes = new List<string>();
            foreach (Match m in AttributeRegex.Matches(inner))
            {
                if (m.Groups["class"].Success) { classes.Add(m.Groups["class"].Value); continue; }
                if (m.Groups["id"].Success) { result["id"] = m.Groups["id"].Value; continue; }

                var key = m.Groups["key"].Value;
                var value = m.Groups["q"].Success ? m.Groups["q"].Value : m.Groups["v"].Value;
                if (!string.IsNullOrEmpty(key)) result[key] = value;
            }

            if (classes.Count > 0)
            {
                result["class"] = result.TryGetValue("class", out var existing) && !string.IsNullOrEmpty(existing)
                    ? existing + " " + string.Join(" ", classes)
                    : string.Join(" ", classes);
            }

            return result;
        }
    }
}
