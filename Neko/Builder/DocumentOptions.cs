using System;
using System.Collections.Generic;
using System.Linq;

namespace Neko.Builder
{
    /// <summary>
    /// Document-level options, read from the <c>document:</c> front-matter key that
    /// marks a Markdown file as a paged document: A4 (or Letter) sheets laid out on
    /// the page, with a <c>docx</c> button that downloads the same pages as a Word
    /// file. The key accepts a bare <c>true</c> (all defaults) or a mapping of the
    /// options below.
    /// </summary>
    public class DocumentOptions
    {
        /// <summary>
        /// The document themes Neko ships. <c>neko</c> is the plain, readable default
        /// that follows the site's palette; <c>curiosity</c> is the Curiosity brand's
        /// whitepaper system (paper, ink, stone, deep blue and one signal square).
        /// </summary>
        public static readonly string[] BuiltInThemes = { "neko", "curiosity" };

        /// <summary>Visual theme of the document: one of <see cref="BuiltInThemes"/>. <c>neko</c> by default.</summary>
        public string Theme { get; set; } = "neko";

        /// <summary>Sheet size: <c>a4</c> (210 x 297 mm) or <c>letter</c> (8.5 x 11 in).</summary>
        public string Size { get; set; } = "a4";

        /// <summary>The running head printed at the top of every page that doesn't set its own.</summary>
        public string Running { get; set; }

        /// <summary>Prints the page number in the foot of every page.</summary>
        public bool Numbers { get; set; } = true;

        /// <summary>Shows the <c>docx</c> control, which exports the pages to a Word file in the browser.</summary>
        public bool Download { get; set; } = true;

        /// <summary>
        /// Typeface source: <c>bundled</c> loads the fonts Neko ships (<c>assets/deckfonts/</c>),
        /// which is also what the Word export embeds; <c>none</c> links nothing and falls back
        /// to local stacks.
        /// </summary>
        public string Fonts { get; set; }

        /// <summary>Where the "back" control goes. Defaults to the referring page, then the site root.</summary>
        public string Back { get; set; }

        /// <summary>Label of the back control.</summary>
        public string BackText { get; set; } = "Back";

        /// <summary>Image for the brand mark in the foot of every page (the neko theme).</summary>
        public string Logo { get; set; }

        /// <summary>Text for the brand mark in the foot of every page. Defaults to the site title.</summary>
        public string LogoText { get; set; }

        /// <summary>Document author, written to the Word file's properties.</summary>
        public string Author { get; set; }

        /// <summary>Company, written to the Word file's properties.</summary>
        public string Company { get; set; }

        public string ThemeStylesheet => string.Equals(Theme, "curiosity", StringComparison.OrdinalIgnoreCase) ? "document-curiosity.css" : null;

        /// <summary>Neko ships the typefaces of both themes (<c>Resources/deckfonts/</c>).</summary>
        public string FontSource => !string.IsNullOrEmpty(Fonts) ? Fonts : "bundled";

        public bool IsCuriosity => string.Equals(Theme, "curiosity", StringComparison.OrdinalIgnoreCase);

        public bool IsLetter => string.Equals(Size, "letter", StringComparison.OrdinalIgnoreCase);

        public static bool IsBuiltInTheme(string theme)
            => !string.IsNullOrWhiteSpace(theme) && BuiltInThemes.Contains(theme.Trim().ToLowerInvariant());

        /// <summary>
        /// Reads the <c>document:</c> front-matter node. Returns false — with a null
        /// <paramref name="options"/> — when the node is absent or explicitly disabled
        /// (<c>false</c>, <c>no</c>, <c>off</c>, <c>none</c>).
        /// </summary>
        public static bool TryParse(object node, out DocumentOptions options)
        {
            options = null;
            if (node == null) return false;

            if (node is string scalar)
            {
                var v = scalar.Trim();
                if (v.Length == 0 || IsFalsey(v)) return false;
                options = new DocumentOptions();
                return true;
            }

            if (node is bool flag)
            {
                if (!flag) return false;
                options = new DocumentOptions();
                return true;
            }

            if (node is System.Collections.IDictionary map)
            {
                options = new DocumentOptions();
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
                        case "size": case "page": if (!string.IsNullOrEmpty(value)) options.Size = value.ToLowerInvariant(); break;
                        case "running": case "eyebrow": options.Running = value; break;
                        case "numbers": case "pagenumbers": case "page-numbers": options.Numbers = !IsFalsey(value); break;
                        case "download": case "docx": options.Download = !IsFalsey(value); break;
                        case "fonts": if (!string.IsNullOrEmpty(value)) options.Fonts = value.ToLowerInvariant(); break;
                        case "back": options.Back = value; break;
                        case "backtext": case "back-text": options.BackText = value; break;
                        case "logo": options.Logo = value; break;
                        case "logotext": case "logo-text": options.LogoText = value; break;
                        case "author": options.Author = value; break;
                        case "company": options.Company = value; break;
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

    /// <summary>One page of a document: the Markdown between two separators, plus its own attributes.</summary>
    public class DocumentPage
    {
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string Markdown { get; set; } = string.Empty;
        public string Html { get; set; }

        public string Get(string key, string fallback = null)
            => Attributes.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
    }

    /// <summary>
    /// Splits a document's Markdown body into pages. The rule is the deck's: a
    /// horizontal rule (<c>---</c>) on its own line, preceded by a blank line,
    /// optionally carrying <c>{key="value"}</c> attributes, and never inside a
    /// fenced code block. A document that has no separator is a single page that
    /// flows onto as many sheets as its content needs.
    /// </summary>
    public static class DocumentParser
    {
        public static List<DocumentPage> Split(string body)
        {
            return PresentationParser.Split(body)
                .Select(s => new DocumentPage { Attributes = s.Attributes, Markdown = s.Markdown })
                .ToList();
        }
    }
}
