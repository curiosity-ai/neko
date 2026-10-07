using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    /// <summary>
    /// The embedded files a presentation deck or paged document inlines into its own
    /// HTML, so the page is a single self-contained file: no CDN, no font host and no
    /// request to the site's <c>assets/</c> folder. Paths are relative to
    /// <c>Neko/Resources/</c> (<c>presentation.js</c>, <c>highlight/github.min.css</c>,
    /// <c>standalone/mermaid.min.js</c>, …).
    /// </summary>
    internal static class StandaloneAssets
    {
        private static readonly ConcurrentDictionary<string, byte[]> Cache = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, string> Derived = new(StringComparer.Ordinal);
        private static readonly Lazy<Dictionary<string, byte[]>> Twemoji = new(LoadTwemoji);

        public static byte[] Bytes(string path)
        {
            return Cache.GetOrAdd(path, p =>
            {
                var name = "Neko.Resources." + p.Replace('/', '.');
                using var stream = typeof(StandaloneAssets).Assembly.GetManifestResourceStream(name)
                    ?? throw new FileNotFoundException($"Embedded resource not found: {name}");
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            });
        }

        public static bool Exists(string path)
            => typeof(StandaloneAssets).Assembly.GetManifestResourceInfo("Neko.Resources." + path.Replace('/', '.')) != null;

        public static string Text(string path) => Encoding.UTF8.GetString(Bytes(path)).TrimStart('﻿');

        /// <summary>A <c>&lt;style&gt;</c> block holding <paramref name="css"/>.</summary>
        public static string Style(string css, string id = null)
        {
            var idAttr = string.IsNullOrEmpty(id) ? string.Empty : $" id=\"{id}\"";
            return $"<style{idAttr}>{EscapeEndTag(css, "style")}</style>";
        }

        /// <summary>A <c>&lt;script&gt;</c> block running <paramref name="js"/>.</summary>
        public static string Script(string js) => $"<script>{EscapeScript(js)}</script>";

        /// <summary>
        /// An inert script block (<c>type="text/x-neko-asset"</c>) the page runs only
        /// when it asks for it — the exporters, which are only needed for a download.
        /// </summary>
        public static string LazyScript(string name, string js)
            => $"<script type=\"text/x-neko-asset\" data-neko-asset=\"{name}\">{EscapeScript(js)}</script>";

        /// <summary>
        /// Keeps the HTML parser out of a script's text: <c>&lt;/script</c> would end the
        /// element and <c>&lt;!--</c> can swallow the real end tag. Both rewrites are the
        /// same characters to JavaScript, in a string or in a regular expression.
        /// </summary>
        public static string EscapeScript(string js)
        {
            js = Regex.Replace(js, "</(script)", "<\\/$1", RegexOptions.IgnoreCase);
            return js.Replace("<!--", "<\\x21--");
        }

        private static string EscapeEndTag(string css, string tag)
            => Regex.Replace(css, "</(" + tag + ")", "<\\/$1", RegexOptions.IgnoreCase);

        public static string DataUri(string mime, byte[] data) => $"data:{mime};base64,{Convert.ToBase64String(data)}";

        public static string MimeFor(string file)
        {
            switch (Path.GetExtension(file).ToLowerInvariant())
            {
                case ".woff2": return "font/woff2";
                case ".woff": return "font/woff";
                case ".ttf": return "font/ttf";
                case ".otf": return "font/otf";
                case ".svg": return "image/svg+xml";
                case ".png": return "image/png";
                default: return "application/octet-stream";
            }
        }

        private static readonly Regex CssUrl = new(@"url\(\s*(['""]?)(?!data:)([^'"")]+)\1\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// Rewrites every relative <c>url(...)</c> in <paramref name="css"/> to a data URI
        /// of the embedded file it names, resolved against <paramref name="folder"/>.
        /// </summary>
        public static string InlineUrls(string css, string folder)
        {
            return CssUrl.Replace(css, m =>
            {
                var file = m.Groups[2].Value.Trim();
                if (file.StartsWith("./", StringComparison.Ordinal)) file = file.Substring(2);
                var path = string.IsNullOrEmpty(folder) ? file : folder + "/" + file;
                if (file.Contains("://") || !Exists(path)) return m.Value;
                return $"url(\"{DataUri(MimeFor(file), Bytes(path))}\")";
            });
        }

        /// <summary>A stylesheet with its <c>url(...)</c> references inlined, cached per path.</summary>
        public static string InlinedCss(string path)
        {
            return Derived.GetOrAdd("css:" + path, p =>
            {
                var slash = path.LastIndexOf('/');
                return InlineUrls(Text(path), slash < 0 ? string.Empty : path.Substring(0, slash));
            });
        }

        /// <summary>Top-level CSS rules: (selector or at-rule prelude, full rule text).</summary>
        public static IEnumerable<(string Selector, string Rule)> SplitRules(string css)
        {
            int depth = 0, start = 0;
            for (int i = 0; i < css.Length; i++)
            {
                var c = css[i];
                if (c == '/' && i + 1 < css.Length && css[i + 1] == '*')
                {
                    var end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    if (end < 0) break;
                    if (depth == 0) start = end + 2;
                    i = end + 1;
                }
                else if (c == '"' || c == '\'')
                {
                    var end = css.IndexOf(c, i + 1);
                    if (end < 0) break;
                    i = end;
                }
                else if (c == '{') depth++;
                else if (c == '}' && --depth == 0)
                {
                    var rule = css.Substring(start, i + 1 - start).Trim();
                    var brace = rule.IndexOf('{');
                    yield return (rule.Substring(0, brace).Trim(), rule);
                    start = i + 1;
                }
            }
        }

        private static readonly Regex ClassSelector = new(@"\.((?:[A-Za-z0-9_-]|\\.)+)", RegexOptions.Compiled);

        /// <summary>
        /// Drops the rules that only style classes the page never uses: a rule naming
        /// any class with one of <paramref name="prefixes"/> survives only when one of
        /// those classes is in <paramref name="used"/>. Every other rule is kept.
        /// </summary>
        public static string TreeShake(string css, ISet<string> used, params string[] prefixes)
        {
            var sb = new StringBuilder();
            foreach (var (selector, rule) in SplitRules(css))
            {
                if (!selector.StartsWith("@", StringComparison.Ordinal))
                {
                    var classes = ClassSelector.Matches(selector)
                        .Select(m => m.Groups[1].Value.Replace("\\", string.Empty))
                        .Where(c => prefixes.Any(p => c.StartsWith(p, StringComparison.Ordinal)))
                        .ToList();
                    if (classes.Count > 0 && !classes.Any(used.Contains)) continue;
                }
                sb.Append(rule).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Keeps the <c>@font-face</c> rules whose family the page names somewhere in
        /// <paramref name="usage"/>, inlines their files, and tags each with the file it
        /// came from (<c>/*neko-font:File.ttf*/</c>) so the PowerPoint and Word exporters
        /// can embed the very same bytes.
        /// </summary>
        public static string FontFaces(string path, string usage)
        {
            var slash = path.LastIndexOf('/');
            var folder = slash < 0 ? string.Empty : path.Substring(0, slash);
            var sb = new StringBuilder();
            foreach (var (selector, rule) in SplitRules(Text(path)))
            {
                if (!selector.Equals("@font-face", StringComparison.OrdinalIgnoreCase)) continue;
                var family = Regex.Match(rule, @"font-family\s*:\s*['""]?([^'"";]+)").Groups[1].Value.Trim();
                if (family.Length > 0 && !Regex.IsMatch(usage, @"(?<![\w-])" + Regex.Escape(family) + @"(?![\w-])")) continue;
                var file = CssUrl.Match(rule).Groups[2].Value.Trim();
                sb.Append("/*neko-font:").Append(file).Append("*/");
                sb.Append(Derived.GetOrAdd("face:" + path + ":" + rule, _ => InlineUrls(rule, folder))).Append('\n');
            }
            return sb.ToString();
        }

        private static readonly Regex TwemojiUrl = new(@"url\(\s*([""']?)https://cdn\.jsdelivr\.net/gh/twitter/twemoji@[^/]+/assets/(?:72x72|svg)/([0-9a-f-]+)\.(?:png|svg)\1\s*\)", RegexOptions.Compiled);

        /// <summary>
        /// emoji.css cut down to the emoji the page shows, each pointing at its Twemoji
        /// SVG as a data URI instead of the jsDelivr CDN.
        /// </summary>
        public static string EmojiCss(ISet<string> used)
        {
            var css = TreeShake(Text("emoji.css"), used, "em-");
            return TwemojiUrl.Replace(css, m =>
            {
                return Twemoji.Value.TryGetValue(m.Groups[2].Value + ".svg", out var svg)
                    ? $"url(\"{DataUri("image/svg+xml", svg)}\")"
                    : "none";
            });
        }

        private static Dictionary<string, byte[]> LoadTwemoji()
        {
            var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using var zip = new ZipArchive(new MemoryStream(Bytes("standalone/twemoji.zip")), ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                using var s = entry.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                map[entry.Name] = ms.ToArray();
            }
            return map;
        }

        private static readonly Regex ClassAttr = new(@"class\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex IconToken = new(@"\bfi-[a-z]+-[a-z0-9-]+", RegexOptions.Compiled);

        /// <summary>
        /// Every class the page's markup names, plus any icon class (<c>fi-rr-…</c>) that
        /// appears anywhere in it — scripts build some of those as strings.
        /// </summary>
        public static HashSet<string> UsedClasses(string scan)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in ClassAttr.Matches(scan))
            {
                var value = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
                foreach (var token in value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)) used.Add(token);
            }
            foreach (Match m in IconToken.Matches(scan)) used.Add(m.Value);
            return used;
        }
    }
}
