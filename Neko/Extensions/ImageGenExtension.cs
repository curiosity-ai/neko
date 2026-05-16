using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neko.Extensions
{
    public class ImageGenInline : Inline
    {
        public string Prompt { get; set; } = "";
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public string Raw { get; set; } = "";
    }

    public class ImageGenParser : InlineParser
    {
        public ImageGenParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            if (slice.CurrentChar != '!')
            {
                slice = saved;
                return false;
            }
            slice.NextChar();

            if (!slice.Match("img-gen"))
            {
                slice = saved;
                return false;
            }
            slice.Start += "img-gen".Length;

            if (slice.CurrentChar != ' ' && slice.CurrentChar != '\n' && slice.CurrentChar != '\r' && slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }

            var contentStart = slice.Start;
            int depth = 1;
            while (!slice.IsEmpty)
            {
                if (slice.CurrentChar == '[') depth++;
                else if (slice.CurrentChar == ']')
                {
                    depth--;
                    if (depth == 0) break;
                }
                slice.NextChar();
            }

            if (slice.CurrentChar != ']')
            {
                slice = saved;
                return false;
            }

            var rawContent = slice.Text.Substring(contentStart, slice.Start - contentStart);
            slice.NextChar();

            var inline = new ImageGenInline { Raw = rawContent };
            ParseContent(rawContent, inline);
            processor.Inline = inline;
            return true;
        }

        private static void ParseContent(string raw, ImageGenInline inline)
        {
            var trimmed = raw.TrimStart();
            var firstNewline = trimmed.IndexOfAny(new[] { '\r', '\n' });

            string optionsLine;
            string prompt;
            if (firstNewline >= 0)
            {
                optionsLine = trimmed.Substring(0, firstNewline).Trim();
                prompt = trimmed.Substring(firstNewline).Trim();
            }
            else
            {
                // Single-line form: either everything is the prompt, or attrs only.
                if (LooksLikeAttributes(trimmed))
                {
                    optionsLine = trimmed;
                    prompt = "";
                }
                else
                {
                    optionsLine = "";
                    prompt = trimmed;
                }
            }

            inline.Prompt = prompt;
            foreach (var kv in ParseAttributes(optionsLine))
            {
                inline.Options[kv.Key] = kv.Value;
            }
        }

        private static bool LooksLikeAttributes(string line)
        {
            // A line is treated as the attributes line when every token is key=value.
            if (string.IsNullOrWhiteSpace(line)) return false;
            int i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
                if (i >= line.Length) break;
                int keyStart = i;
                while (i < line.Length && line[i] != '=' && !char.IsWhiteSpace(line[i])) i++;
                if (i >= line.Length || line[i] != '=' || i == keyStart) return false;
                i++;
                if (i >= line.Length) return false;
                if (line[i] == '"')
                {
                    i++;
                    while (i < line.Length && line[i] != '"') i++;
                    if (i >= line.Length) return false;
                    i++;
                }
                else
                {
                    while (i < line.Length && !char.IsWhiteSpace(line[i])) i++;
                }
            }
            return true;
        }

        public static IEnumerable<KeyValuePair<string, string>> ParseAttributes(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) yield break;
            int i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
                if (i >= line.Length) yield break;

                int keyStart = i;
                while (i < line.Length && line[i] != '=' && !char.IsWhiteSpace(line[i])) i++;
                if (i >= line.Length || line[i] != '=' || i == keyStart) yield break;
                var key = line.Substring(keyStart, i - keyStart);
                i++; // skip '='

                string value;
                if (i < line.Length && line[i] == '"')
                {
                    i++;
                    int valStart = i;
                    while (i < line.Length && line[i] != '"') i++;
                    value = line.Substring(valStart, i - valStart);
                    if (i < line.Length) i++; // skip closing quote
                }
                else
                {
                    int valStart = i;
                    while (i < line.Length && !char.IsWhiteSpace(line[i])) i++;
                    value = line.Substring(valStart, i - valStart);
                }
                yield return new KeyValuePair<string, string>(key, value);
            }
        }
    }

    public class ImageGenRenderer : HtmlObjectRenderer<ImageGenInline>
    {
        protected override void Write(HtmlRenderer renderer, ImageGenInline obj)
        {
            // The directive itself produces no HTML. Once `neko gen-images` runs it
            // is rewritten into a real Markdown image; until then the page simply
            // shows nothing in place of the directive.
        }
    }

    public class ImageGenExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<ImageGenParser>())
            {
                pipeline.InlineParsers.Insert(0, new ImageGenParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<ImageGenRenderer>();
            }
        }
    }
}
