using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    /// <summary>
    /// Presentation decks and paged documents are standalone pages: everything they
    /// need — Tailwind, the theme stylesheets, fonts, icons, emoji, KaTeX, Mermaid,
    /// highlight.js and the deck/document runtime with its PowerPoint/Word exporter —
    /// is inlined into the HTML, so the file works on its own (saved to disk, mailed,
    /// opened offline) without a CDN, a font host or the site's <c>assets/</c> folder.
    /// Only what the page uses is included: icons and emoji are cut down to the ones
    /// it shows, and KaTeX and Mermaid only come along when it has math or a diagram.
    /// Local images (the logo, the favicon, pictures on a slide or a sheet) are inlined
    /// as data URIs too; only remote ones (<c>https://…</c>) stay links.
    /// </summary>
    public partial class HtmlGenerator
    {
        /// <summary>
        /// The site's source folder, which local image references on a standalone page
        /// resolve against. Without it, images stay links.
        /// </summary>
        public string InputDirectory { get; set; }

        private sealed class StandalonePage
        {
            /// <summary>The page's markup, with the plaintext of a protected body.</summary>
            public string Scan;

            /// <summary>The page's own stylesheets (deck/document theme), for font selection.</summary>
            public string ThemeCss;

            public HashSet<string> Classes;
            public bool HasMath;
            public bool HasMermaid;
            public bool HasCode;
        }

        private static StandalonePage AnalyzeStandalonePage(string markup, string plaintextBody, params string[] themeStylesheets)
        {
            var scan = markup + "\n" + plaintextBody;
            return new StandalonePage
            {
                Scan = scan,
                ThemeCss = string.Join("\n", themeStylesheets.Where(p => !string.IsNullOrEmpty(p)).Select(StandaloneAssets.Text)),
                Classes = StandaloneAssets.UsedClasses(scan),
                // Markdig's math extension wraps formulas in .math; $$ covers raw blocks.
                HasMath = scan.Contains("class=\"math") || scan.Contains("$$"),
                HasMermaid = scan.Contains("class=\"mermaid"),
                HasCode = scan.Contains("<code"),
            };
        }

        /// <summary>
        /// The page's own Tailwind stylesheet, generated from its markup and the scripts
        /// it runs (which toggle a few classes) rather than linked from the site's
        /// <c>assets/tailwind.css</c>.
        /// </summary>
        private string StandaloneTailwindCss(StandalonePage page, params string[] runtimeScripts)
        {
            var contents = new[] { page.Scan }.Concat(runtimeScripts.Select(StandaloneAssets.Text));
            return Tailwind.TailwindGenerator.Generate(contents, _config, minify: true);
        }

        /// <summary>
        /// The <c>@font-face</c> rules from <paramref name="path"/> (none when null) for
        /// the families the page uses, with the font files inlined, and the font catalog
        /// the exporters embed from — Neko's deck-font catalog when <paramref name="catalog"/>
        /// is set, so a download embeds the same faces, otherwise an empty one.
        /// </summary>
        private static void RenderStandaloneFonts(StringBuilder sb, StandalonePage page, string path, bool catalog)
        {
            var faces = path == null ? string.Empty : StandaloneAssets.FontFaces(path, page.ThemeCss + "\n" + page.Scan);
            if (faces.Length > 0)
            {
                sb.AppendLine("    " + StandaloneAssets.Style(faces, "neko-deck-fonts"));
            }
            // The exporters read the catalog from here rather than fetching
            // deck-fonts.json; an empty one when the page embeds no TrueType faces.
            var json = catalog && faces.Length > 0 ? StandaloneAssets.Text("deckfonts/deck-fonts.json") : "{}";
            sb.AppendLine("    " + StandaloneAssets.Script(
                "window.nekoDeckFonts = Object.assign(window.nekoDeckFonts || {}, { catalog: " + json + " });"));
        }

        /// <summary>UIcons and emoji, cut down to the ones the page shows.</summary>
        private static void RenderStandaloneIconsAndEmoji(StringBuilder sb, StandalonePage page)
        {
            foreach (var (sheet, prefix) in new[] { ("uicons-regular-rounded.css", "fi-rr-"), ("uicons-brands.css", "fi-brands-") })
            {
                var css = StandaloneAssets.TreeShake(StandaloneAssets.Text(sheet), page.Classes, prefix);
                // No icon from this set on the page: skip the font as well.
                if (!page.Classes.Any(c => c.StartsWith(prefix, System.StringComparison.Ordinal))) continue;
                sb.AppendLine("    " + StandaloneAssets.Style(StandaloneAssets.InlineUrls(css, string.Empty)));
            }

            if (page.Classes.Any(c => c.StartsWith("em-", System.StringComparison.Ordinal)))
            {
                sb.AppendLine("    " + StandaloneAssets.Style(StandaloneAssets.EmojiCss(page.Classes)));
            }
        }

        /// <summary>KaTeX, Mermaid and highlight.js — each only when the page needs it.</summary>
        private void RenderStandaloneContentLibraries(StringBuilder sb, StandalonePage page)
        {
            if (page.HasMath) RenderHeadKatex(sb, inline: true);
            if (page.HasMermaid) RenderHeadMermaid(sb, inline: true);
            RenderHeadHighlightJs(sb, inline: true, includeScripts: page.HasCode);
        }

        private static void RenderStandaloneStylesheets(StringBuilder sb, params string[] paths)
        {
            foreach (var path in paths.Where(p => !string.IsNullOrEmpty(p)))
            {
                sb.AppendLine("    " + StandaloneAssets.Style(StandaloneAssets.InlinedCss(path)));
            }
        }

        /// <summary>
        /// The page's runtime script, inlined, plus — when the page offers a download —
        /// the exporter and its library as inert blocks the runtime runs on demand.
        /// </summary>
        private static void RenderStandaloneRuntime(StringBuilder sb, string runtime, params string[] lazyScripts)
        {
            foreach (var name in lazyScripts)
            {
                sb.AppendLine(StandaloneAssets.LazyScript(name, StandaloneAssets.Text(name)));
            }
            sb.AppendLine(StandaloneAssets.Script(StandaloneAssets.Text(runtime)));
        }

        /// <summary>
        /// Swaps a script a component writes into the content from a CDN (leader-line,
        /// for code annotations) for the vendored copy.
        /// </summary>
        private static string InlineContentCdnScripts(string html)
        {
            const string leaderLine = "<script src=\"https://cdn.jsdelivr.net/npm/leader-line-new@1.1.9/leader-line.min.js\"></script>";
            var first = html.IndexOf(leaderLine, System.StringComparison.Ordinal);
            if (first < 0) return html;
            var rest = html.Substring(first + leaderLine.Length).Replace(leaderLine, string.Empty);
            return html.Substring(0, first)
                + StandaloneAssets.Script(StandaloneAssets.Text("standalone/leader-line.min.js"))
                + rest;
        }

        /// <summary>The head's meta block, with a local favicon inlined.</summary>
        private void RenderStandaloneHeadMeta(StringBuilder sb, string title, string description, string sourcePath)
        {
            var meta = new StringBuilder();
            RenderHeadMeta(meta, title, description);
            sb.Append(InlineLocalMedia(meta.ToString(), sourcePath));
        }

        private static readonly Regex LocalMediaRef = new(
            @"(<(?:img|source|link)\b[^>]*?\b(?:src|href)\s*=\s*)""([^""]+)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StyleAttr = new(@"\bstyle\s*=\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StyleUrl = new(@"url\(\s*(?:'|&quot;|&#39;)?([^'"")&]+)(?:'|&quot;|&#39;)?\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// Rewrites every reference to an image in the site's source folder to a data URI
        /// of that image: the <c>src</c>/<c>href</c> of an <c>img</c>, <c>source</c> or
        /// <c>link</c> (the favicon), and a <c>url(...)</c> in a <c>style</c> attribute
        /// (a customer logo drawn through a CSS mask). <paramref name="sourcePath"/> is
        /// the Markdown file the page comes from; relative references resolve against
        /// its folder.
        /// </summary>
        private string InlineLocalMedia(string html, string sourcePath)
        {
            if (string.IsNullOrEmpty(InputDirectory)) return html;
            html = LocalMediaRef.Replace(html, m =>
            {
                var data = LocalImageDataUri(System.Net.WebUtility.HtmlDecode(m.Groups[2].Value), sourcePath);
                return data == null ? m.Value : m.Groups[1].Value + "\"" + data + "\"";
            });
            return StyleAttr.Replace(html, attr =>
            {
                if (attr.Groups[1].Value.IndexOf("url(", StringComparison.OrdinalIgnoreCase) < 0) return attr.Value;
                var style = StyleUrl.Replace(attr.Groups[1].Value, m =>
                {
                    var data = LocalImageDataUri(System.Net.WebUtility.HtmlDecode(m.Groups[1].Value), sourcePath);
                    return data == null ? m.Value : "url('" + data + "')";
                });
                var value = attr.Groups[1];
                return attr.Value.Substring(0, value.Index - attr.Index) + style
                    + attr.Value.Substring(value.Index - attr.Index + value.Length);
            });
        }

        private string LocalImageDataUri(string url, string sourcePath)
        {
            var file = ResolveLocalFile(url, sourcePath);
            var mime = file == null ? null : ImageMime(file);
            if (mime == null) return null;
            try
            {
                return StandaloneAssets.DataUri(mime, File.ReadAllBytes(file));
            }
            catch (IOException)
            {
                return null;
            }
        }

        private string ResolveLocalFile(string url, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            if (url.StartsWith("//", StringComparison.Ordinal) || url.StartsWith("#", StringComparison.Ordinal)
                || Regex.IsMatch(url, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
            {
                return null;
            }

            var cut = url.IndexOfAny(new[] { '?', '#' });
            if (cut >= 0) url = url.Substring(0, cut);
            url = Uri.UnescapeDataString(url);

            var root = Path.GetFullPath(InputDirectory);
            string path;
            if (url.StartsWith("/", StringComparison.Ordinal))
            {
                var prefix = (SiteBuilder.CurrentRoutePrefix ?? string.Empty).TrimEnd('/');
                if (prefix.Length > 0 && url.StartsWith(prefix + "/", StringComparison.Ordinal)) url = url.Substring(prefix.Length);
                path = Path.Combine(root, url.TrimStart('/'));
            }
            else
            {
                var baseDir = string.IsNullOrEmpty(sourcePath) ? root : Path.GetDirectoryName(Path.GetFullPath(sourcePath));
                path = Path.Combine(baseDir, url);
            }

            path = Path.GetFullPath(path);
            // Only files that the site itself would serve.
            var rootWithSlash = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!path.StartsWith(rootWithSlash, comparison)) return null;
            return File.Exists(path) ? path : null;
        }

        private static string ImageMime(string file)
        {
            switch (Path.GetExtension(file).ToLowerInvariant())
            {
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".svg": return "image/svg+xml";
                case ".webp": return "image/webp";
                case ".avif": return "image/avif";
                case ".ico": return "image/x-icon";
                case ".bmp": return "image/bmp";
                default: return null;
            }
        }
    }
}
