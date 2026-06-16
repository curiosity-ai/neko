using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Neko.Builder
{
    /// <summary>
    /// Builds the project into a throwaway output directory and then verifies
    /// every link in the generated HTML. Internal page/asset links are resolved
    /// against the files actually written to disk; <c>#fragment</c> anchors are
    /// checked against the <c>id</c>/<c>name</c> attributes in the target page;
    /// external <c>http(s)</c> links are only contacted when <c>--external</c> is
    /// passed. Returns a non-zero exit code when any broken link is found, so it
    /// can gate a CI pipeline.
    /// </summary>
    public class CheckLinksCommand
    {
        private readonly string _input;
        private readonly bool _checkExternal;
        private readonly bool _checkAnchors;

        // href="..." / href='...' / src="..." / src='...' across any tag.
        private static readonly Regex LinkRegex = new(
            "(?:href|src)\\s*=\\s*([\"'])(.*?)\\1",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // id="..." / name="..." used as anchor targets.
        private static readonly Regex AnchorRegex = new(
            "(?:id|name)\\s*=\\s*([\"'])(.*?)\\1",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public CheckLinksCommand(string input, bool checkExternal, bool checkAnchors)
        {
            _input = input;
            _checkExternal = checkExternal;
            _checkAnchors = checkAnchors;
        }

        // Detail carries per-page context (e.g. the path a relative link actually
        // resolved to), which is empty for links whose target is already obvious.
        private sealed record BrokenLink(string SourcePage, string Url, string Reason, string Detail = "");

        public async Task<int> RunAsync()
        {
            var inputFullPath = Path.GetFullPath(_input);
            if (!Directory.Exists(inputFullPath))
            {
                Console.Error.WriteLine($"[check-links] Input directory not found: {inputFullPath}");
                return 2;
            }

            // Build into a dedicated temp folder so we validate the real, rendered
            // output (with all of Neko's link rewriting) rather than raw Markdown.
            var outputRoot = Path.Combine(Path.GetTempPath(), "neko", "_linkcheck");
            Console.WriteLine($"[check-links] Building {inputFullPath} into {outputRoot}...");
            outputRoot = await BuildRunner.RunAsync(_input, outputRoot);

            // Scan documentation pages, but skip HTML that lives under an
            // `assets/` folder: those are build artifacts of components (e.g. the
            // Tesserae/H5 live-preview micro-app), not documentation pages, and
            // their internal asset wiring is not something we should validate.
            var allHtml = Directory.GetFiles(outputRoot, "*.html", SearchOption.AllDirectories);
            var htmlFiles = allHtml.Where(f => !IsUnderAssets(outputRoot, f)).ToArray();
            var skippedAssetPages = allHtml.Length - htmlFiles.Length;

            Console.WriteLine($"[check-links] Scanning {htmlFiles.Length} generated page(s)"
                + (skippedAssetPages > 0 ? $" (skipped {skippedAssetPages} HTML file(s) under assets/)" : "") + "...");

            var broken = new List<BrokenLink>();
            var anchorCache = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            // Distinct external URLs mapped to the pages that reference them.
            var externalRefs = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            var checkedLinks = 0;

            foreach (var file in htmlFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[check-links] Could not read {file}: {ex.Message}");
                    continue;
                }

                var pageLabel = ToSiteRelative(outputRoot, file);
                var fileDir = Path.GetDirectoryName(file) ?? outputRoot;

                foreach (Match m in LinkRegex.Matches(content))
                {
                    var raw = WebUtility.HtmlDecode(m.Groups[2].Value).Trim();
                    if (raw.Length == 0) continue;

                    checkedLinks++;

                    // Split off the fragment and the query string.
                    var hashIdx = raw.IndexOf('#');
                    var fragment = hashIdx >= 0 ? raw[(hashIdx + 1)..] : null;
                    var pathPart = hashIdx >= 0 ? raw[..hashIdx] : raw;

                    var queryIdx = pathPart.IndexOf('?');
                    if (queryIdx >= 0) pathPart = pathPart[..queryIdx];

                    // Same-page anchor (e.g. "#section" or a bare "#").
                    if (pathPart.Length == 0)
                    {
                        if (string.IsNullOrEmpty(fragment) || fragment == "/") continue;
                        if (_checkAnchors && !AnchorExists(file, fragment, content, anchorCache))
                            broken.Add(new BrokenLink(pageLabel, raw, "no matching anchor on the page"));
                        continue;
                    }

                    // Non-file schemes and protocol-relative URLs.
                    if (pathPart.StartsWith("//") || HasScheme(pathPart))
                    {
                        var lower = pathPart.ToLowerInvariant();
                        var isHttp = lower.StartsWith("http://") || lower.StartsWith("https://") || pathPart.StartsWith("//");
                        if (_checkExternal && isHttp)
                        {
                            var url = pathPart.StartsWith("//") ? "https:" + pathPart : pathPart;
                            if (!externalRefs.TryGetValue(url, out var pages))
                                externalRefs[url] = pages = new List<string>();
                            pages.Add(pageLabel);
                        }
                        continue; // mailto:, tel:, data:, javascript:, etc. are never followed.
                    }

                    // Internal page or asset link — resolve against the output tree.
                    var dirLink = pathPart.EndsWith("/");
                    var resolved = ResolveInternal(outputRoot, fileDir, pathPart, dirLink);
                    if (resolved == null)
                    {
                        // For a page-relative link, spell out where it actually
                        // pointed (it is resolved against the current page's URL,
                        // not the site root) so an existing target reached via the
                        // wrong path is not mistaken for a checker mistake.
                        var detail = pathPart.StartsWith("/")
                            ? ""
                            : "resolves to " + ResolvedTargetDisplay(outputRoot, fileDir, pathPart);
                        broken.Add(new BrokenLink(pageLabel, raw, "no page or asset at that path", detail));
                        continue;
                    }

                    if (_checkAnchors && fragment != null && fragment.Length > 0 && fragment != "/"
                        && resolved.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                        && !AnchorExists(resolved, fragment, null, anchorCache))
                    {
                        broken.Add(new BrokenLink(pageLabel, raw, "no matching anchor in the target page",
                            "target " + ToSiteRelative(outputRoot, resolved)));
                    }
                }
            }

            if (_checkExternal && externalRefs.Count > 0)
            {
                Console.WriteLine($"[check-links] Checking {externalRefs.Count} external URL(s)...");
                await CheckExternalAsync(externalRefs, broken);
            }

            // Report. Links are grouped by their (target, reason) so a single
            // root cause repeated across the site — e.g. a navbar/footer link
            // present on every page — is shown once with an occurrence count,
            // rather than once per page.
            Console.WriteLine();
            if (broken.Count == 0)
            {
                Console.WriteLine($"[check-links] No broken links found across {htmlFiles.Length} page(s) ({checkedLinks} link(s) checked).");
                return 0;
            }

            var pageCount = broken.Select(b => b.SourcePage).Distinct().Count();
            var groups = broken
                .GroupBy(b => (b.Url, b.Reason))
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.Url, StringComparer.Ordinal)
                .ToList();

            Console.WriteLine($"[check-links] Found {groups.Count} broken link(s) ({broken.Count} reference(s) across {pageCount} page(s)):");
            Console.WriteLine();

            foreach (var g in groups)
            {
                var (url, reason) = g.Key;
                var occurrences = g.Count();
                var pages = g.GroupBy(x => x.SourcePage).ToList();

                Console.WriteLine($"  ✗ {url}   — {reason}");

                const int maxPages = 3;
                foreach (var pg in pages.Take(maxPages))
                {
                    var sampleDetail = pg.Select(x => x.Detail).FirstOrDefault(d => !string.IsNullOrEmpty(d));
                    Console.WriteLine(string.IsNullOrEmpty(sampleDetail)
                        ? $"      on {pg.Key}"
                        : $"      on {pg.Key}  ({sampleDetail})");
                }

                var morePages = pages.Count - maxPages;
                if (morePages > 0)
                    Console.WriteLine($"      … and {morePages} more page(s) — {occurrences} reference(s) total");
                else if (occurrences > pages.Count)
                    Console.WriteLine($"      ({occurrences} reference(s) total)");

                Console.WriteLine();
            }

            Console.WriteLine($"[check-links] {groups.Count} distinct broken link(s), {broken.Count} reference(s), across {pageCount} page(s).");
            return 1;
        }

        // Computes the site-relative path a link points at (resolved against the
        // page's own directory for relative links), purely for display.
        private static string ResolvedTargetDisplay(string outputRoot, string fileDir, string pathPart)
        {
            var decoded = Uri.UnescapeDataString(pathPart);
            var relative = decoded.Replace('/', Path.DirectorySeparatorChar);
            var basePath = decoded.StartsWith("/")
                ? Path.Combine(outputRoot, relative.TrimStart(Path.DirectorySeparatorChar))
                : Path.Combine(fileDir, relative);
            try { return ToSiteRelative(outputRoot, Path.GetFullPath(basePath)); }
            catch { return pathPart; }
        }

        // True when the value begins with a URI scheme such as "http:", "mailto:",
        // "tel:", "data:" or "javascript:" — but not a Windows-style "C:\" path,
        // which never appears in generated links.
        private static bool HasScheme(string value)
            => Regex.IsMatch(value, "^[a-zA-Z][a-zA-Z0-9+.\\-]*:");

        // Resolves an internal link to an actual file on disk, mirroring how the
        // dev server / static host would serve clean (extension-less) URLs.
        // Returns the resolved absolute path, or null if nothing matches.
        private static string? ResolveInternal(string outputRoot, string fileDir, string pathPart, bool dirLink)
        {
            var decoded = Uri.UnescapeDataString(pathPart);
            var relative = decoded.Replace('/', Path.DirectorySeparatorChar);

            string basePath = decoded.StartsWith("/")
                ? Path.Combine(outputRoot, relative.TrimStart(Path.DirectorySeparatorChar))
                : Path.Combine(fileDir, relative);

            string full;
            try { full = Path.GetFullPath(basePath); }
            catch { return null; }

            // A link that escapes the output root can never resolve to a page.
            var rootPrefix = outputRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.Equals(outputRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal)
                && !full.StartsWith(rootPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            // A trailing slash (or the bare root) addresses a directory's index.
            if (dirLink || decoded == "/" || decoded.Length == 0)
            {
                var index = Path.Combine(full, "index.html");
                return File.Exists(index) ? index : null;
            }

            if (File.Exists(full)) return full;                 // explicit file (.html or asset)
            if (File.Exists(full + ".html")) return full + ".html"; // clean URL -> page.html

            var dirIndex = Path.Combine(full, "index.html");    // clean URL -> dir/index.html
            if (File.Exists(dirIndex)) return dirIndex;

            return null;
        }

        // Loads (and caches) the set of anchor ids/names declared in a page.
        private static bool AnchorExists(string filePath, string fragment, string? knownContent, Dictionary<string, HashSet<string>> cache)
        {
            if (!cache.TryGetValue(filePath, out var anchors))
            {
                anchors = new HashSet<string>(StringComparer.Ordinal);
                string content = knownContent ?? SafeRead(filePath);
                foreach (Match m in AnchorRegex.Matches(content))
                {
                    var value = WebUtility.HtmlDecode(m.Groups[2].Value);
                    if (!string.IsNullOrEmpty(value)) anchors.Add(value);
                }
                cache[filePath] = anchors;
            }

            // Fragments are matched case-sensitively, matching browser behaviour.
            return anchors.Contains(fragment);
        }

        private static string SafeRead(string path)
        {
            try { return File.ReadAllText(path); }
            catch { return string.Empty; }
        }

        private static string ToSiteRelative(string outputRoot, string fullPath)
            => "/" + Path.GetRelativePath(outputRoot, fullPath).Replace('\\', '/');

        // True when the file sits inside an `assets/` directory anywhere in the
        // output tree (e.g. assets/, components/assets/, assets/tesserae/).
        private static bool IsUnderAssets(string outputRoot, string fullPath)
        {
            var rel = Path.GetRelativePath(outputRoot, fullPath);
            foreach (var part in rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (string.Equals(part, "assets", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static async Task CheckExternalAsync(Dictionary<string, List<string>> externalRefs, List<BrokenLink> broken)
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 10 };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Neko-LinkChecker/1.0");

            using var throttle = new SemaphoreSlim(8);
            var results = new ConcurrentBag<BrokenLink>();

            var tasks = externalRefs.Select(async kvp =>
            {
                var url = kvp.Key;
                await throttle.WaitAsync();
                try
                {
                    var reason = await ProbeAsync(client, url);
                    if (reason != null)
                    {
                        // Report once per (page, url) so the source pages are listed.
                        foreach (var page in kvp.Value.Distinct())
                            results.Add(new BrokenLink(page, url, reason));
                    }
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks);
            broken.AddRange(results);
        }

        // Returns null when the URL is reachable, otherwise a short failure reason.
        // Tries a cheap HEAD first and falls back to GET for hosts that reject it.
        private static async Task<string?> ProbeAsync(HttpClient client, string url)
        {
            try
            {
                using var head = new HttpRequestMessage(HttpMethod.Head, url);
                using var resp = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead);
                if ((int)resp.StatusCode == 405 || (int)resp.StatusCode == 501 || (int)resp.StatusCode == 403)
                    return await ProbeGetAsync(client, url);
                return resp.IsSuccessStatusCode ? null : $"HTTP {(int)resp.StatusCode}";
            }
            catch (HttpRequestException)
            {
                return await ProbeGetAsync(client, url);
            }
            catch (TaskCanceledException)
            {
                return "request timed out";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static async Task<string?> ProbeGetAsync(HttpClient client, string url)
        {
            try
            {
                using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return resp.IsSuccessStatusCode ? null : $"HTTP {(int)resp.StatusCode}";
            }
            catch (TaskCanceledException)
            {
                return "request timed out";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
