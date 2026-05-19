using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Neko.Tools.UIcons
{
    class Program
    {
        private const string MIN_Version = "4.0.0";

        static async Task<string> FetchVersion()
        {
            Console.WriteLine($"Fetching Version");
            var updateFontsJsUrlFromGithub = "https://raw.githubusercontent.com/freepik-company/flaticon-uicons/main/utils/update-fonts.js";

            using var client = new HttpClient();
            var s = await client.GetStringAsync(updateFontsJsUrlFromGithub);

            foreach (var line in s.Split("\n"))
            {
                var prefix = "const CDN_URL = 'https://cdn-uicons.flaticon.com/";

                if (line.StartsWith(prefix))
                {
                    var versionFetched = new Version(line.Substring(prefix.Length, line.Length - prefix.Length - "';".Length));
                    var versionMin = new Version(MIN_Version);

                    return versionFetched > versionMin ? versionFetched.ToString() : versionMin.ToString();
                }
            }

            return MIN_Version;
        }

        // Resolve <repo>/Neko/Resources without relying on the current working
        // directory. CallerFilePath is captured at compile time, so this works
        // regardless of where `dotnet run` is invoked from.
        private static string DefaultResourcesDir([CallerFilePath] string thisFile = "")
        {
            var toolDir = Path.GetDirectoryName(thisFile)!;
            return Path.GetFullPath(Path.Combine(toolDir, "..", "Neko", "Resources"));
        }

        static async Task Main(string[] args)
        {
            var resourcesDir = args.Length > 0 ? Path.GetFullPath(args[0]) : DefaultResourcesDir();

            if (!Directory.Exists(resourcesDir))
            {
                Console.Error.WriteLine($"Resources directory not found: {resourcesDir}");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"Writing into: {resourcesDir}");

            var version = await FetchVersion();
            Console.WriteLine($"Using version: {version}");

            var types = new string[]
            {
                "uicons-brands",
                "uicons-regular-straight",
                "uicons-regular-rounded",
                "uicons-bold-straight",
                "uicons-bold-rounded",
                "uicons-solid-rounded",
                "uicons-solid-straight",
                "uicons-thin-straight",
                "uicons-thin-rounded"
            };

            Console.WriteLine("Downloading fonts");
            foreach (var type in types)
            {
                await DownloadFileAsync(GetWoff2Url(version, type), Path.Combine(resourcesDir, $"{type}.woff2"));
            }

            Console.WriteLine("Downloading CSS");
            foreach (var type in types)
            {
                await DownloadFileAsync(GetCssUrl(version, type), Path.Combine(resourcesDir, $"{type}.css"));
            }

            var icons = new Dictionary<string, List<string>>();

            foreach (var type in types)
            {
                var file = Path.Combine(resourcesDir, $"{type}.css");
                Console.WriteLine("Parsing CSS: " + file);

                bool isRegularRounded = type == "uicons-regular-rounded";

                var lines = File.ReadAllLines(file);
                var extraLines = new List<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    foreach (var kvp in IconsToFixInCss)
                    {
                        if (line.Contains(kvp.Key))
                        {
                            line = line.Replace(kvp.Key, kvp.Value);
                            lines[i] = line;
                        }
                    }

                    if (line.Contains("line-height: 1;"))
                    {
                        var startIndex = line.IndexOf("line-height: 1;");
                        var newLine = line.Substring(0, startIndex) + "line-height: inherit;" + line.Substring(startIndex + "line-height: 1;".Length);
                        lines[i] = newLine;
                        line = newLine;
                    }

                    // Collapse the multi-line src: declaration into a single
                    // line pointing at the locally-hosted woff2 (same dir as
                    // the CSS once extracted into assets/). The woff/eot lines
                    // that followed the original woff2 entry get cleared.
                    if (line.Contains(".woff2") && line.Contains("format(\"woff2\")"))
                    {
                        var leading = line.Substring(0, line.Length - line.TrimStart().Length);
                        lines[i] = $"{leading}src: url(\"./{type}.woff2\") format(\"woff2\");";
                        line = lines[i];
                    }
                    else if (line.Contains(".woff") && line.Contains("format(\"woff\")"))
                    {
                        lines[i] = string.Empty;
                        line = string.Empty;
                    }
                    else if (line.Contains("eot#iefix") && line.Contains("format(\"embedded-opentype\")"))
                    {
                        lines[i] = string.Empty;
                        line = string.Empty;
                    }

                    var iconLine = line.Trim();

                    if (iconLine.StartsWith(".fi") && iconLine.EndsWith(":before {"))
                    {
                        var prefix = IconPrefixes.FirstOrDefault(p => iconLine.Contains($".fi-{p}-"));
                        if (prefix != null)
                        {
                            string iconName = iconLine.Substring($".fi-{prefix}-".Length).Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).First();

                            if (!icons.ContainsKey(iconName)) icons[iconName] = new List<string>();
                            icons[iconName].Add(type);

                            if (isRegularRounded && ExportAsVariables.Contains(iconName) && i + 1 < lines.Length)
                            {
                                var contentLineParts = lines[i + 1].Trim().Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
                                if (contentLineParts.Length > 1)
                                {
                                    var contentValue = contentLineParts[1];
                                    extraLines.Add($"--uicon-var-{iconName}: '{contentValue}';");
                                }
                            }
                        }
                    }
                }

                if (extraLines.Count > 0)
                {
                    extraLines.Insert(0, ":root {");
                    extraLines.Add("}");
                }

                File.WriteAllLines(file, extraLines.Concat(lines));
            }

            Console.WriteLine($"Found {icons.Count} icons from css");

            var allIcons = icons.OrderBy(i => i.Key, StringComparer.Ordinal).ToArray();
            // An icon name can appear in both the brands font and a regular
            // font (e.g. "c"). They are distinct icons in the codebase
            // ("brands-c" vs "c"), so classify each side independently.
            var regularIcons = allIcons.Where(i => i.Value.Any(v => !v.Contains(_brandsPrefix))).Select(i => i.Key).ToArray();
            var brandIcons = allIcons.Where(i => i.Value.Any(v => v.Contains(_brandsPrefix))).Select(i => i.Key).ToArray();

            var iconsJsPath = Path.Combine(resourcesDir, "icons.js");
            UpdateIconList(iconsJsPath, regularIcons, brandIcons);
            Console.WriteLine($"Updated {iconsJsPath} ({regularIcons.Length} regular + {brandIcons.Length} brand icons)");
        }

        // Rewrites the `const iconList = [ ... ];` block at the top of icons.js
        // while preserving everything below it (the search UI code). If the
        // file does not yet exist, the JS tail starts empty.
        private static void UpdateIconList(string path, string[] regular, string[] brands)
        {
            string tail = string.Empty;
            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path);
                // Match the block but leave following whitespace in the tail
                // so we preserve any blank line between `];` and the JS code.
                var match = Regex.Match(existing, @"const\s+iconList\s*=\s*\[.*?\];", RegexOptions.Singleline);
                tail = match.Success ? existing.Substring(match.Index + match.Length) : existing;
            }

            var sb = new StringBuilder();
            sb.AppendLine("const iconList = [");
            foreach (var i in regular)
            {
                sb.AppendLine($"{{ id: \"{i}\", title: \"{i}\", icon: \"{i}\" }},");
            }
            foreach (var i in brands)
            {
                sb.AppendLine($"{{ id: \"brands-{i}\", title: \"{i}\", icon: \"brands-{i}\" }},");
            }
            sb.Append("];");
            sb.Append(tail.Length > 0 ? tail : Environment.NewLine);

            File.WriteAllText(path, sb.ToString());
        }

        public static async Task DownloadFileAsync(string url, string filename)
        {
            Console.WriteLine($"Downloading {url} -> {filename}");
            try
            {
                using var client = new HttpClient();
                using var s = await client.GetStreamAsync(url);
                using var fs = new FileStream(filename, FileMode.Create);
                await s.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {url}: {ex.Message}");
            }
        }

        public static string GetCssUrl(string version, string type)
        {
            return $"https://cdn-uicons.flaticon.com/{version}/{type}/css/{type}.css";
        }

        public static string GetWoff2Url(string version, string type)
        {
            return $"https://cdn-uicons.flaticon.com/{version}/{type}/webfonts/{type}.woff2";
        }

        private const string _brandsPrefix = "brands";
        private const string _regularRoundPrefix = "rr";
        private const string _solidRoundPrefix = "sr";
        private const string _thinRoundPrefix = "tr";
        private const string _boldRoundPrefix = "br";
        private const string _regularStraiightPrefix = "rs";
        private const string _boldStraiightPrefix = "bs";
        private const string _solidStraiightPrefix = "ss";
        private const string _thinStraiightPrefix = "ts";

        public static readonly string[] IconPrefixes = new string[]
        {
            _brandsPrefix,
            _boldRoundPrefix,
            _thinRoundPrefix,
            _solidRoundPrefix,
            _regularRoundPrefix,
            _regularStraiightPrefix,
            _boldStraiightPrefix,
            _solidStraiightPrefix,
            _thinStraiightPrefix
        };

        private static Dictionary<string, string> IconsToFixInCss = new Dictionary<string, string>
        {
            { "-social-network:before", "-thumbs-up:before" },
            { "-hastag:before", "-hashtag:before" },
            { "-hand:before", "-thumbs-down:before" },
        };

        private static HashSet<string> ExportAsVariables = new HashSet<string>()
        {
            "checkbox", "square", "sidebar", "sidebar-flip", "angle-right", "angle-left",
            "angle-top", "angle-bottom", "slash", "lock", "lock-open-alt", "unlock",
            "upload", "download", "cloud-upload-alt", "cloud-upload", "refresh",
            "square-a", "thumbtack", "thumbtack-slash", "heart", "heart-slash",
            "bookmark", "bookmark-slash", "thumbs-up", "thumbs-down", "block", "sparkles",
        };
    }
}
