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

        public ImageGenCommand(string inputDirectory, string apiKey, string imageModel, string llmModel)
        {
            _inputDirectory = inputDirectory;
            _apiKey = apiKey;
            _imageModel = imageModel;
            _llmModel = llmModel;
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
                    var imagePath = Path.Combine(assetsDir, filename + ".png");

                    Console.WriteLine($"[img-gen]   generating '{filename}.png' ({alt})...");

                    try
                    {
                        await GenerateImageAsync(api, parsed, imagePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[img-gen]   image generation failed: {ex.Message}");
                        failed++;
                        continue;
                    }

                    var imageMarkdown = $"![{EscapeAlt(alt)}](assets/img-gen/{filename}.png)";
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

        private static (string Prompt, Dictionary<string, string> Options, string Raw) ParseDirective(string body)
        {
            var trimmed = body.TrimStart();
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

        private async Task GenerateImageAsync(TornadoApi api, (string Prompt, Dictionary<string, string> Options, string Raw) parsed, string targetPath)
        {
            var request = new ImageGenerationRequest
            {
                Model = new ImageModel(_imageModel, LLmProviders.OpenAi),
                Prompt = parsed.Prompt,
                NumOfImages = 1,
            };

            if (parsed.Options.TryGetValue("size", out var size))
            {
                request.Size = ParseSize(size);
            }
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

            var image = result.Data[0];
            byte[] bytes;
            if (!string.IsNullOrEmpty(image.Base64))
            {
                bytes = Convert.FromBase64String(image.Base64);
            }
            else if (!string.IsNullOrEmpty(image.Url))
            {
                using var http = new HttpClient();
                bytes = await http.GetByteArrayAsync(image.Url);
            }
            else
            {
                throw new InvalidOperationException("Image generation response had neither base64 nor URL.");
            }

            File.WriteAllBytes(targetPath, bytes);
        }

        private static TornadoImageSizes? ParseSize(string s)
        {
            s = s?.Trim().ToLowerInvariant() ?? "";
            return s switch
            {
                "auto"        => TornadoImageSizes.Auto,
                "256x256"     => TornadoImageSizes.Size256x256,
                "512x512"     => TornadoImageSizes.Size512x512,
                "1024x1024"   => TornadoImageSizes.Size1024x1024,
                "1024x1536"   => TornadoImageSizes.Size1024x1536,
                "1536x1024"   => TornadoImageSizes.Size1536x1024,
                "1024x1792"   => TornadoImageSizes.Size1024x1792,
                "1792x1024"   => TornadoImageSizes.Size1792x1024,
                _             => null,
            };
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
