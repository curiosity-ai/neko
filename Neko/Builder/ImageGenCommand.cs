using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LlmTornado;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using Neko.Configuration;

namespace Neko.Builder
{
    public class ImageGenCommand
    {
        // Matches `[!img-gen <body>]` where `<body>` may span multiple lines and
        // may itself contain balanced brackets. RightToLeft is not safe here so
        // we use balancing groups.
        private static readonly Regex ImgGenRegex = new(
            @"\[!img-gen(?<body>(?:[^\[\]]|\[(?<o>)|\](?<-o>))*(?(o)(?!)))\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly string _inputDirectory;
        private readonly string _apiKey;
        private readonly string _imageModel;
        private readonly string _llmModel;
        private readonly ImageGenConfig _imageGenConfig;

        public ImageGenCommand(string inputDirectory, string apiKey, string imageModel, string llmModel, ImageGenConfig imageGenConfig = null)
        {
            _inputDirectory = inputDirectory;
            _apiKey = apiKey;
            _imageModel = imageModel;
            _llmModel = llmModel;
            _imageGenConfig = imageGenConfig ?? new ImageGenConfig();
        }

        public async Task<int> RunAsync()
        {
            if (!Directory.Exists(_inputDirectory))
            {
                Console.WriteLine($"[img-gen] Input directory not found: {_inputDirectory}");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine("[img-gen] No OpenAI API key provided. Pass --api-key or set OPENAI_API_KEY.");
                return 1;
            }

            var markdownFiles = Directory.GetFiles(_inputDirectory, "*.md", SearchOption.AllDirectories);
            Console.WriteLine($"[img-gen] Scanning {markdownFiles.Length} markdown file(s) in {_inputDirectory}...");

            var api = new TornadoApi(LLmProviders.OpenAi, _apiKey);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int generated = 0, failed = 0;

            foreach (var file in markdownFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[img-gen] Could not read {file}: {ex.Message}");
                    continue;
                }

                var matches = ImgGenRegex.Matches(content);
                if (matches.Count == 0) continue;

                Console.WriteLine($"[img-gen] {Path.GetRelativePath(_inputDirectory, file)}: {matches.Count} directive(s).");

                var fileDir = Path.GetDirectoryName(file) ?? _inputDirectory;
                var assetsDir = Path.Combine(fileDir, "assets", "img-gen");

                // Replace from the end so earlier match indices stay valid.
                var ordered = matches.Cast<Match>().OrderByDescending(m => m.Index).ToList();
                var newContent = content;

                foreach (var match in ordered)
                {
                    var body = match.Groups["body"].Value;
                    var directive = match.Value;
                    var parsed = ParseDirective(body);

                    if (string.IsNullOrWhiteSpace(parsed.Prompt))
                    {
                        Console.WriteLine($"[img-gen]   skipping directive with empty prompt at offset {match.Index}.");
                        failed++;
                        continue;
                    }

                    var pageName = Path.GetFileNameWithoutExtension(file);
                    string filename, alt;
                    try
                    {
                        (filename, alt) = await DescribeAsync(api, parsed.Prompt, pageName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[img-gen]   LLM description failed: {ex.Message}");
                        failed++;
                        continue;
                    }

                    filename = Sanitize(filename, pageName);
                    filename = UniqueName(usedNames, assetsDir, filename);
                    usedNames.Add(filename);

                    Directory.CreateDirectory(assetsDir);
                    var lightPath = Path.Combine(assetsDir, filename + ".png");

                    var wantsLight = WantsLightMode(parsed.Options);
                    var wantsDark  = WantsDarkMode(parsed.Options);

                    Console.WriteLine($"[img-gen]   generating '{filename}.png' ({alt})...");

                    byte[] lightBytes;
                    try
                    {
                        lightBytes = await GenerateImageAsync(api, parsed, wantsLight);
                        File.WriteAllBytes(lightPath, lightBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[img-gen]   image generation failed: {ex.Message}");
                        failed++;
                        continue;
                    }

                    string darkRelative = null;
                    if (wantsDark)
                    {
                        var darkFilename = filename + "-dark";
                        var darkPath = Path.Combine(assetsDir, darkFilename + ".png");
                        Console.WriteLine($"[img-gen]   generating dark-mode variant '{darkFilename}.png'...");
                        try
                        {
                            var darkBytes = await GenerateDarkVariantAsync(api, parsed, lightBytes);
                            File.WriteAllBytes(darkPath, darkBytes);
                            darkRelative = $"assets/img-gen/{darkFilename}.png";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[img-gen]   dark-mode variant failed (keeping light only): {ex.Message}");
                        }
                    }

                    var imageMarkdown = darkRelative != null
                        ? $"![{EscapeAlt(alt)}](assets/img-gen/{filename}.png){{src-dark=\"{darkRelative}\"}}"
                        : $"![{EscapeAlt(alt)}](assets/img-gen/{filename}.png)";
                    var replacement = $"<!--\n{directive}\n-->\n{imageMarkdown}";
                    newContent = newContent.Substring(0, match.Index) + replacement + newContent.Substring(match.Index + match.Length);
                    generated++;
                }

                if (!ReferenceEquals(newContent, content) && newContent != content)
                {
                    File.WriteAllText(file, newContent);
                }
            }

            Console.WriteLine($"[img-gen] Done. Generated {generated} image(s){(failed > 0 ? $", {failed} failed" : "")}.");
            return failed > 0 ? 1 : 0;
        }

        // Matches a Markdown image whose URL points into `assets/img-gen/`.
        // Captures any attribute block `{...}` already attached so we can detect
        // (and preserve) existing attributes when rewriting.
        private static readonly Regex BackfillImgRegex = new(
            @"!\[(?<alt>[^\]]*)\]\((?<url>assets/img-gen/[^)\s]+\.png)\)(?<attrs>\{[^}]*\})?",
            RegexOptions.Compiled);

        public async Task<int> BackfillDarkImagesAsync()
        {
            if (!Directory.Exists(_inputDirectory))
            {
                Console.WriteLine($"[dark-fill] Input directory not found: {_inputDirectory}");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine("[dark-fill] No OpenAI API key provided. Pass --api-key or set OPENAI_API_KEY.");
                return 1;
            }

            var markdownFiles = Directory.GetFiles(_inputDirectory, "*.md", SearchOption.AllDirectories);
            Console.WriteLine($"[dark-fill] Scanning {markdownFiles.Length} markdown file(s) in {_inputDirectory}...");

            var api = new TornadoApi(LLmProviders.OpenAi, _apiKey);
            int generated = 0, failed = 0, skipped = 0;

            foreach (var file in markdownFiles)
            {
                string content;
                try { content = File.ReadAllText(file); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[dark-fill] Could not read {file}: {ex.Message}");
                    continue;
                }

                var matches = BackfillImgRegex.Matches(content);
                if (matches.Count == 0) continue;

                var fileDir = Path.GetDirectoryName(file) ?? _inputDirectory;
                var relFile = Path.GetRelativePath(_inputDirectory, file);

                // Rewrite from the end so earlier match indices stay valid.
                var ordered = matches.Cast<Match>().OrderByDescending(m => m.Index).ToList();
                var newContent = content;

                foreach (var match in ordered)
                {
                    var alt = match.Groups["alt"].Value;
                    var url = match.Groups["url"].Value;
                    var attrs = match.Groups["attrs"].Success ? match.Groups["attrs"].Value : "";

                    if (attrs.IndexOf("src-dark", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Already has a dark variant linked — nothing to do.
                        skipped++;
                        continue;
                    }

                    var filenameNoExt = Path.GetFileNameWithoutExtension(url);
                    if (filenameNoExt.EndsWith("-dark", StringComparison.OrdinalIgnoreCase))
                    {
                        // This *is* a dark variant; skip.
                        skipped++;
                        continue;
                    }

                    var lightPath = Path.Combine(fileDir, url.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(lightPath))
                    {
                        Console.WriteLine($"[dark-fill]   {relFile}: skipping '{url}' — source file not found at {lightPath}");
                        skipped++;
                        continue;
                    }

                    var lastSlash = url.LastIndexOf('/');
                    var urlDir = lastSlash >= 0 ? url.Substring(0, lastSlash + 1) : "";
                    var darkUrl = urlDir + filenameNoExt + "-dark.png";
                    var darkPath = Path.Combine(fileDir, darkUrl.Replace('/', Path.DirectorySeparatorChar));

                    if (!File.Exists(darkPath))
                    {
                        Console.WriteLine($"[dark-fill]   {relFile}: generating dark variant for '{filenameNoExt}.png'...");
                        byte[] lightBytes;
                        try { lightBytes = File.ReadAllBytes(lightPath); }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[dark-fill]   could not read source: {ex.Message}");
                            failed++;
                            continue;
                        }

                        // Try to match the size of the source PNG so the pair lines
                        // up visually; fall back to the configured default if the
                        // header can't be read.
                        string sizeStr = _imageGenConfig.Size;
                        try
                        {
                            var (w, h) = ReadPngDimensions(lightBytes);
                            sizeStr = $"{w}x{h}";
                        }
                        catch { /* fall back to config default */ }

                        try
                        {
                            var darkBytes = await EditImageToDarkAsync(api, lightBytes, sizeStr);
                            File.WriteAllBytes(darkPath, darkBytes);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[dark-fill]   generation failed: {ex.Message}");
                            failed++;
                            continue;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[dark-fill]   {relFile}: dark variant '{filenameNoExt}-dark.png' already on disk — relinking only.");
                    }

                    var newAttrs = MergeSrcDarkAttribute(attrs, darkUrl);
                    var replacement = $"![{alt}]({url}){newAttrs}";
                    newContent = newContent.Substring(0, match.Index) + replacement + newContent.Substring(match.Index + match.Length);
                    generated++;
                }

                if (!ReferenceEquals(newContent, content) && newContent != content)
                {
                    File.WriteAllText(file, newContent);
                }
            }

            Console.WriteLine($"[dark-fill] Done. Updated {generated} image(s){(skipped > 0 ? $", {skipped} skipped" : "")}{(failed > 0 ? $", {failed} failed" : "")}.");
            return failed > 0 ? 1 : 0;
        }

        // Inserts (or appends) a `src-dark="..."` attribute into an existing
        // `{...}` attribute block, preserving any other attributes that were
        // already there.
        public static string MergeSrcDarkAttribute(string existingAttrs, string darkUrl)
        {
            if (string.IsNullOrEmpty(existingAttrs))
            {
                return $"{{src-dark=\"{darkUrl}\"}}";
            }
            var inner = existingAttrs.Trim();
            if (inner.StartsWith("{")) inner = inner.Substring(1);
            if (inner.EndsWith("}")) inner = inner.Substring(0, inner.Length - 1);
            inner = inner.Trim();
            return inner.Length == 0
                ? $"{{src-dark=\"{darkUrl}\"}}"
                : $"{{{inner} src-dark=\"{darkUrl}\"}}";
        }

        // Reads width/height out of the PNG IHDR chunk (bytes 16-23 of a valid
        // PNG file). Throws if the byte stream isn't a PNG.
        public static (int Width, int Height) ReadPngDimensions(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24)
            {
                throw new InvalidOperationException("File too small to be a PNG.");
            }
            if (bytes[0] != 0x89 || bytes[1] != 0x50 || bytes[2] != 0x4E || bytes[3] != 0x47 ||
                bytes[4] != 0x0D || bytes[5] != 0x0A || bytes[6] != 0x1A || bytes[7] != 0x0A)
            {
                throw new InvalidOperationException("Not a PNG signature.");
            }
            int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return (w, h);
        }

        private bool WantsLightMode(Dictionary<string, string> opts)
        {
            if (opts.TryGetValue("light", out var v) && bool.TryParse(v, out var b)) return b;
            return _imageGenConfig.LightMode;
        }

        private bool WantsDarkMode(Dictionary<string, string> opts)
        {
            if (opts.TryGetValue("dark", out var v) && bool.TryParse(v, out var b)) return b;
            return _imageGenConfig.DarkMode;
        }

        private static (string Prompt, Dictionary<string, string> Options, string Raw) ParseDirective(string body)
        {
            var trimmed = body.TrimStart();
            var firstNewline = trimmed.IndexOfAny(new[] { '\r', '\n' });
            string optionsLine;
            string prompt;
            if (firstNewline >= 0)
            {
                // The first line is only an attributes line if every token is key=value.
                // Otherwise the whole body — including the first line — is the prompt.
                var firstLine = trimmed.Substring(0, firstNewline).Trim();
                if (LooksLikeAttributes(firstLine))
                {
                    optionsLine = firstLine;
                    prompt = trimmed.Substring(firstNewline).Trim();
                }
                else
                {
                    optionsLine = "";
                    prompt = trimmed.Trim();
                }
            }
            else
            {
                if (LooksLikeAttributes(trimmed))
                {
                    optionsLine = trimmed;
                    prompt = "";
                }
                else
                {
                    optionsLine = "";
                    prompt = trimmed.Trim();
                }
            }

            var opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in ParseAttributes(optionsLine))
            {
                opts[kv.Key] = kv.Value;
            }
            return (prompt, opts, body);
        }

        private static bool LooksLikeAttributes(string line)
        {
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

        private static IEnumerable<KeyValuePair<string, string>> ParseAttributes(string line)
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
                i++;
                string value;
                if (i < line.Length && line[i] == '"')
                {
                    i++;
                    int valStart = i;
                    while (i < line.Length && line[i] != '"') i++;
                    value = line.Substring(valStart, i - valStart);
                    if (i < line.Length) i++;
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

        private async Task<(string Filename, string Alt)> DescribeAsync(TornadoApi api, string prompt, string pageName)
        {
            const string systemPrompt =
                "You generate metadata for an AI-generated image about to be created for a documentation page. " +
                "Reply ONLY with a strict JSON object with two keys: " +
                "\"filename\" (lowercase ASCII slug, words separated by '-', no extension, max 60 chars, descriptive of the image) " +
                "and \"alt\" (a short, factual alt-text description of the image content, max 140 chars, no quoting). " +
                "Do not include any other text.";

            var chat = api.Chat.CreateConversation(new ChatRequest
            {
                Model = _llmModel,
                Temperature = 0.2,
                ResponseFormat = ChatRequestResponseFormats.Json,
            });
            chat.AppendSystemMessage(systemPrompt);
            chat.AppendUserInput($"Page: {pageName}\nImage prompt: {prompt}");

            var rich = await chat.GetResponseRich(System.Threading.CancellationToken.None);
            var text = rich?.Text ?? "";
            return ParseDescriptionJson(text, prompt);
        }

        private static (string Filename, string Alt) ParseDescriptionJson(string text, string fallbackPrompt)
        {
            text = text.Trim();
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return (SlugifyForFallback(fallbackPrompt), TruncateAlt(fallbackPrompt));
            }
            try
            {
                using var doc = JsonDocument.Parse(text.Substring(start, end - start + 1));
                var root = doc.RootElement;
                var filename = root.TryGetProperty("filename", out var fnEl) ? (fnEl.GetString() ?? "") : "";
                var alt = root.TryGetProperty("alt", out var altEl) ? (altEl.GetString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(filename)) filename = SlugifyForFallback(fallbackPrompt);
                if (string.IsNullOrWhiteSpace(alt)) alt = TruncateAlt(fallbackPrompt);
                return (filename, alt);
            }
            catch
            {
                return (SlugifyForFallback(fallbackPrompt), TruncateAlt(fallbackPrompt));
            }
        }

        private static string SlugifyForFallback(string s)
        {
            var slug = Slugify(s);
            if (slug.Length > 60) slug = slug.Substring(0, 60).TrimEnd('-');
            if (string.IsNullOrEmpty(slug)) slug = "image";
            return slug;
        }

        private static string Slugify(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in (s ?? "").ToLowerInvariant())
            {
                if (ch >= 'a' && ch <= 'z') sb.Append(ch);
                else if (ch >= '0' && ch <= '9') sb.Append(ch);
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }

        private static string Sanitize(string raw, string pageName)
        {
            var slug = Slugify(raw);
            if (slug.Length > 60) slug = slug.Substring(0, 60).TrimEnd('-');
            if (string.IsNullOrEmpty(slug)) slug = Slugify(pageName);
            if (string.IsNullOrEmpty(slug)) slug = "image";
            return slug;
        }

        private static string UniqueName(HashSet<string> used, string assetsDir, string baseName)
        {
            var name = baseName;
            int i = 2;
            while (used.Contains(name) || File.Exists(Path.Combine(assetsDir, name + ".png")))
            {
                name = baseName + "-" + i;
                i++;
            }
            return name;
        }

        private static string TruncateAlt(string s)
        {
            s = (s ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (s.Length <= 140) return s;
            return s.Substring(0, 137) + "...";
        }

        private static string EscapeAlt(string s)
        {
            return (s ?? "").Replace("]", "\\]").Replace("[", "\\[");
        }

        private string ComposePrompt(string userPrompt, bool appendLightModeHint)
        {
            var parts = new List<string> { userPrompt.Trim() };
            if (!string.IsNullOrWhiteSpace(_imageGenConfig.SystemPrompt))
            {
                parts.Add(_imageGenConfig.SystemPrompt.Trim());
            }
            if (appendLightModeHint && !string.IsNullOrWhiteSpace(_imageGenConfig.LightModePrompt))
            {
                parts.Add(_imageGenConfig.LightModePrompt.Trim());
            }
            return string.Join("\n\n", parts);
        }

        private async Task<byte[]> GenerateImageAsync(TornadoApi api, (string Prompt, Dictionary<string, string> Options, string Raw) parsed, bool appendLightModeHint)
        {
            var request = new ImageGenerationRequest
            {
                Model = new ImageModel(_imageModel, LLmProviders.OpenAi),
                Prompt = ComposePrompt(parsed.Prompt, appendLightModeHint),
                NumOfImages = 1,
            };

            // Size: per-directive override > config default.
            var sizeStr = parsed.Options.TryGetValue("size", out var s) && !string.IsNullOrWhiteSpace(s)
                ? s
                : _imageGenConfig.Size;
            ApplySize(request, sizeStr);

            if (parsed.Options.TryGetValue("quality", out var quality))
            {
                request.Quality = ParseQuality(quality);
            }
            if (parsed.Options.TryGetValue("background", out var bg))
            {
                request.Background = ParseBackground(bg);
            }
            if (parsed.Options.TryGetValue("style", out var style))
            {
                request.Style = ParseStyle(style);
            }

            // Prefer base64 so we don't depend on a downloadable URL after the call.
            request.ResponseFormat = TornadoImageResponseFormats.Base64;

            var result = await api.ImageGenerations.CreateImage(request);
            if (result?.Data == null || result.Data.Count == 0)
            {
                throw new InvalidOperationException("Image generation returned no data.");
            }

            return await DownloadAsync(result.Data[0]);
        }

        private async Task<byte[]> GenerateDarkVariantAsync(TornadoApi api, (string Prompt, Dictionary<string, string> Options, string Raw) parsed, byte[] lightBytes)
        {
            // Match the size used for the light image so the pair lines up visually.
            var sizeStr = parsed.Options.TryGetValue("size", out var s) && !string.IsNullOrWhiteSpace(s)
                ? s
                : _imageGenConfig.Size;
            return await EditImageToDarkAsync(api, lightBytes, sizeStr);
        }

        private async Task<byte[]> EditImageToDarkAsync(TornadoApi api, byte[] lightBytes, string sizeStr)
        {
            var darkPrompt = string.IsNullOrWhiteSpace(_imageGenConfig.DarkModePrompt)
                ? ImageGenConfig.DefaultDarkModePrompt
                : _imageGenConfig.DarkModePrompt;

            var request = new ImageEditRequest
            {
                Model = new ImageModel(_imageModel, LLmProviders.OpenAi),
                Prompt = darkPrompt,
                NumOfImages = 1,
                Image = new TornadoInputFile
                {
                    Base64 = Convert.ToBase64String(lightBytes),
                    MimeType = "image/png",
                },
                ResponseFormat = TornadoImageResponseFormats.Base64,
            };
            ApplyEditSize(request, sizeStr);

            var result = await api.ImageEdit.EditImage(request);
            if (result?.Data == null || result.Data.Count == 0)
            {
                throw new InvalidOperationException("Image edit returned no data.");
            }
            return await DownloadAsync(result.Data[0]);
        }

        private static async Task<byte[]> DownloadAsync(TornadoGeneratedImage image)
        {
            if (!string.IsNullOrEmpty(image.Base64))
            {
                return Convert.FromBase64String(image.Base64);
            }
            if (!string.IsNullOrEmpty(image.Url))
            {
                using var http = new HttpClient();
                return await http.GetByteArrayAsync(image.Url);
            }
            throw new InvalidOperationException("Image response had neither base64 nor URL.");
        }

        private static void ApplySize(ImageGenerationRequest request, string sizeStr)
        {
            var (size, width, height) = ParseSize(sizeStr);
            if (size.HasValue)
            {
                request.Size = size.Value;
                if (size.Value == TornadoImageSizes.Custom)
                {
                    request.Width = width;
                    request.Height = height;
                }
            }
        }

        private static void ApplyEditSize(ImageEditRequest request, string sizeStr)
        {
            var (size, _, _) = ParseSize(sizeStr);
            // ImageEditRequest doesn't expose Width/Height for Custom; fall back to Auto in that case.
            if (size.HasValue && size.Value != TornadoImageSizes.Custom)
            {
                request.Size = size.Value;
            }
            else if (size.HasValue)
            {
                request.Size = TornadoImageSizes.Auto;
            }
        }

        // Parses every popular size string used in OpenAI / LLM Tornado, plus the
        // 2K and 4K resolutions (which go through `TornadoImageSizes.Custom` with
        // explicit Width/Height).
        private static (TornadoImageSizes? Size, int? Width, int? Height) ParseSize(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            return s switch
            {
                ""           => (null, null, null),
                "auto"       => (TornadoImageSizes.Auto, null, null),
                "256x256"    => (TornadoImageSizes.Size256x256, null, null),
                "512x512"    => (TornadoImageSizes.Size512x512, null, null),
                "1024x1024"  => (TornadoImageSizes.Size1024x1024, null, null),
                "1024x1536"  => (TornadoImageSizes.Size1024x1536, null, null),
                "1536x1024"  => (TornadoImageSizes.Size1536x1024, null, null),
                "1024x1792"  => (TornadoImageSizes.Size1024x1792, null, null),
                "1792x1024"  => (TornadoImageSizes.Size1792x1024, null, null),
                "768x1408"   => (TornadoImageSizes.Size768x1408, null, null),
                "1408x768"   => (TornadoImageSizes.Size1408x768, null, null),
                "896x1280"   => (TornadoImageSizes.Size896x1280, null, null),
                "1280x896"   => (TornadoImageSizes.Size1280x896, null, null),
                "2048x2048"  => (TornadoImageSizes.Custom, 2048, 2048),
                "2048x1152"  => (TornadoImageSizes.Custom, 2048, 1152),
                "3840x2160"  => (TornadoImageSizes.Custom, 3840, 2160),
                "2160x3840"  => (TornadoImageSizes.Custom, 2160, 3840),
                _            => TryParseCustom(s),
            };
        }

        // Accepts any "<width>x<height>" size as a custom dimension.
        private static (TornadoImageSizes? Size, int? Width, int? Height) TryParseCustom(string s)
        {
            var m = Regex.Match(s, @"^(\d{2,5})x(\d{2,5})$");
            if (!m.Success) return (null, null, null);
            if (int.TryParse(m.Groups[1].Value, out var w) && int.TryParse(m.Groups[2].Value, out var h))
            {
                return (TornadoImageSizes.Custom, w, h);
            }
            return (null, null, null);
        }

        private static TornadoImageQualities? ParseQuality(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            return s switch
            {
                "auto"      => TornadoImageQualities.Auto,
                "low"       => TornadoImageQualities.Low,
                "medium"    => TornadoImageQualities.Medium,
                "high"      => TornadoImageQualities.High,
                "standard"  => TornadoImageQualities.Standard,
                "hd"        => TornadoImageQualities.Hd,
                _           => null,
            };
        }

        private static ImageBackgroundTypes? ParseBackground(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            return s switch
            {
                "auto"        => ImageBackgroundTypes.Auto,
                "opaque"      => ImageBackgroundTypes.Opaque,
                "transparent" => ImageBackgroundTypes.Transparent,
                _             => null,
            };
        }

        private static TornadoImageStyles? ParseStyle(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            return s switch
            {
                "natural" => TornadoImageStyles.Natural,
                "vivid"   => TornadoImageStyles.Vivid,
                _         => null,
            };
        }
    }
}
