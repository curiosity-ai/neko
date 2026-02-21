using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TailDocs.Tools.UIcons
{
    class Program
    {
        private const string MIN_Version = "3.0.0";

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

            // Fallback version if parsing fails
            return "3.0.0";
        }


        static async Task Main(string[] args)
        {
            var outputDir = args.Length > 0 ? args[0] : ".";
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

            var fontsDir = Path.Combine(outputDir, "assets", "fonts");
            var cssDir = Path.Combine(outputDir, "assets", "css");

            if (!Directory.Exists(fontsDir)) Directory.CreateDirectory(fontsDir);
            if (!Directory.Exists(cssDir)) Directory.CreateDirectory(cssDir);

            Console.WriteLine("download fonts");

            foreach (var type in types)
            {
                var fontFileName = $"{type}.woff2";
                await DownloadFileAsync(GetWoff2Url(version, type), Path.Combine(fontsDir, fontFileName));
            }

            Console.WriteLine("download css");

            foreach (var type in types)
            {
                await DownloadFileAsync(GetCssUrl(version, type), Path.Combine(cssDir, $"{type}.css"));
            }

            var icons = new Dictionary<string, List<string>>();

            foreach (var type in types)
            {
                var file = Path.Combine(cssDir, $"{type}.css");
                Console.WriteLine("Parsing CSS: " + file);

                bool isRegularRounded = Path.GetFileName(file) == "uicons-regular-rounded.css";

                var lines = File.ReadAllLines(file);
                var extraLines = new List<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];

                    foreach (var kvp in IconsToFixInCss)
                    {
                        var replace = kvp.Key;
                        var with = kvp.Value;
                        if (line.Contains(replace))
                        {
                            line = line.Replace(replace, with);
                            lines[i] = line;
                        }
                    }

                    if (line.Contains("line-height: 1;"))
                    {
                        var startIndex = line.IndexOf("line-height: 1;");
                        var newLine = line.Substring(0, startIndex) + "line-height: inherit;" + line.Substring(startIndex + "line-height: 1;".Length);
                        lines[i] = newLine;
                    }

                    // Remove other font formats
                    if (line.Contains("eot#iefix") && line.Contains("format(\"embedded-opentype\")")) lines[i] = "";
                    if (line.Contains(".woff") && line.Contains("format(\"woff\")")) lines[i] = "";

                    if (line.Contains(".woff2") && line.Contains("format(\"woff2\")"))
                    {
                         // Adjust relative path if needed, assuming fonts are in ../fonts relative to css
                         // Original code used hardcoded path.
                         // Let's keep relative path simple
                         // lines[i] = $"     src: url(\"../fonts/{type}.woff2\") format(\"woff2\"); ";
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

                // Write back CSS (optional, if we want modified CSS)
                // File.WriteAllLines(file, extraLines.Concat(lines));
            }

            Console.WriteLine($"Found {icons.Count} icons from css");

            var uiconsCsPath = Path.Combine(outputDir, "UIcons.cs");
            var allIcons = icons.OrderBy(i => i.Key).ToArray();

            File.WriteAllText(uiconsCsPath, CreateStaticClass(
                allIcons.Where(i => !i.Value.Any(v => v.Contains(_brandsPrefix))).Select(i => i.Key).ToArray(),
                allIcons.Where(i => i.Value.Any(v => v.Contains(_brandsPrefix))).Select(i => i.Key).ToArray()
            ));

            Console.WriteLine($"Generated {uiconsCsPath}");
        }

        public static async Task DownloadFileAsync(string url, string filename)
        {
            Console.WriteLine($"Downloading {url} to {filename}");
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

        private static string CreateStaticClass(string[] iconsRegular, string[] iconsBrands)
        {
            var sb = new StringBuilder();
            sb.AppendLine("namespace TailDocs.CLI");
            sb.AppendLine("{");
            sb.AppendLine("    public static class UIcons");
            sb.AppendLine("    {");

            sb.AppendLine($"        public const string Default = \"fi-rr-default-empty\";");

            foreach (var i in iconsRegular)
            {
                var validName = ToValidName(i);
                sb.AppendLine($"        public const string {validName} = \"{i}\";");
            }

            foreach (var i in iconsBrands)
            {
                var validName = ToValidBrandsName(i);
                sb.AppendLine($"        public const string {validName} = \"{i}\";");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string ToValidBrandsName(string icon)
        {
            var words = icon.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(i => i.Substring(0, 1).ToUpper() + i.Substring(1))
               .ToArray();

            var name = string.Join("", words);

            if (char.IsDigit(name[0])) return "Brands_" + name;
            else return "Brands" + name;
        }

        private static string ToValidName(string icon)
        {
            var words = icon.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
               .Select(i => i.Substring(0, 1).ToUpper() + i.Substring(1))
               .ToArray();

            var name = string.Join("", words);

            if (char.IsDigit(name[0])) return "_" + name;
            else return name;
        }
    }
}
