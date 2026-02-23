using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Security.Cryptography;

namespace Neko.Server
{
    public class DevServer
    {
        private readonly string _rootPath;
        private readonly string _inputPath;
        private readonly int _port;
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public DevServer(string rootPath, string inputPath, int port = 5000)
        {
            _rootPath = Path.GetFullPath(rootPath);
            _inputPath = Path.GetFullPath(inputPath);
            _port = port;
        }

        public async Task NotifyChange()
        {
            var buffer = Encoding.UTF8.GetBytes("reload");
            var segment = new ArraySegment<byte>(buffer);
            var deadSockets = new List<string>();

            foreach (var kvp in _sockets)
            {
                var socket = kvp.Value;
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        deadSockets.Add(kvp.Key);
                    }
                }
                else
                {
                    deadSockets.Add(kvp.Key);
                }
            }

            foreach (var key in deadSockets)
            {
                _sockets.TryRemove(key, out _);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders(); // Reduce noise
            builder.WebHost.UseUrls($"http://localhost:{_port}");

            var app = builder.Build();

            app.UseWebSockets();

            app.Map("/neko-live", async (HttpContext context) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var id = System.Guid.NewGuid().ToString();
                    _sockets.TryAdd(id, webSocket);

                    var buffer = new byte[1024 * 4];
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", cancellationToken);
                            }
                        }
                    }
                    catch {}
                    finally
                    {
                        _sockets.TryRemove(id, out _);
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            });

            app.MapGet("/api/neko/content", async (string path) => {
                if (string.IsNullOrEmpty(path)) return Results.BadRequest("Path is required");

                var mdPath = ResolveMarkdownPath(path);
                if (mdPath == null) return Results.NotFound($"File not found: {path}");

                // Prevent directory traversal outside input path
                if (!Path.GetFullPath(mdPath).StartsWith(_inputPath))
                {
                    return Results.BadRequest("Invalid path");
                }

                var content = await File.ReadAllTextAsync(mdPath);
                return Results.Text(content);
            });

            app.MapPost("/api/neko/content", async (HttpRequest request) => {
                try
                {
                    var body = await request.ReadFromJsonAsync<ContentUpdate>();
                    if (body == null || string.IsNullOrEmpty(body.Path)) return Results.BadRequest("Invalid body");

                    var mdPath = ResolveMarkdownPath(body.Path);
                    if (mdPath == null) return Results.NotFound($"File not found to update: {body.Path}");

                    if (!Path.GetFullPath(mdPath).StartsWith(_inputPath))
                    {
                        return Results.BadRequest("Invalid path");
                    }

                    await File.WriteAllTextAsync(mdPath, body.Content);
                    return Results.Ok();
                }
                catch (System.Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            app.MapPost("/api/neko/upload-image", async (HttpRequest request) => {
                if (!request.HasFormContentType) return Results.BadRequest("Expected form data");

                var form = await request.ReadFormAsync();
                var file = form.Files["file"];
                var path = form["path"];

                if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");
                if (string.IsNullOrEmpty(path)) return Results.BadRequest("Path is required");

                var mdPath = ResolveMarkdownPath(path);
                // If the markdown file doesn't exist yet (new page?), we might want to allow it, but for now strict check.
                if (mdPath == null) return Results.NotFound($"Markdown file not found for path: {path}");

                if (!Path.GetFullPath(mdPath).StartsWith(_inputPath))
                {
                    return Results.BadRequest("Invalid path");
                }

                var mdDir = Path.GetDirectoryName(mdPath);
                var assetsDir = Path.Combine(mdDir, "assets");

                if (!Directory.Exists(assetsDir))
                {
                    Directory.CreateDirectory(assetsDir);
                }

                // Generate safe filename with hash
                using var sha256 = SHA256.Create();
                using var stream = file.OpenReadStream();
                var hashBytes = await sha256.ComputeHashAsync(stream);
                var hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8).ToLower();

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (string.IsNullOrEmpty(extension)) extension = ".png"; // Default fallback

                var safeName = "image_" + hash + extension;
                var savePath = Path.Combine(assetsDir, safeName);

                // Rewind stream to save
                stream.Position = 0;
                using var fileStream = File.Create(savePath);
                await stream.CopyToAsync(fileStream);

                // Return relative path for markdown
                // format: assets/filename.ext
                return Results.Ok(new { url = $"assets/{safeName}" });
            });

            if (Directory.Exists(_rootPath))
            {
                // Rewriter for extensionless URLs
                var rewriteOptions = new RewriteOptions()
                    .Add(context => {
                        var request = context.HttpContext.Request;
                        var path = request.Path.Value;
                        if (string.IsNullOrEmpty(path) || path == "/") return;

                        // If api or websocket, skip
                        if (path.StartsWith("/api/") || path == "/neko-live") return;

                        // If path has extension, skip
                        if (Path.HasExtension(path)) return;

                        // Check if .html file exists
                        var relativePath = path.TrimStart('/');
                        var filePath = Path.Combine(_rootPath, relativePath + ".html");

                        if (File.Exists(filePath))
                        {
                            request.Path = path + ".html";
                            context.Result = RuleResult.SkipRemainingRules;
                        }
                    });
                app.UseRewriter(rewriteOptions);

                // Serve static files from the output directory
                app.UseFileServer(new FileServerOptions
                {
                    FileProvider = new PhysicalFileProvider(_rootPath),
                    RequestPath = "",
                    EnableDirectoryBrowsing = false
                });
            }

            System.Console.WriteLine($"Server started at http://localhost:{_port}");

            await app.RunAsync(cancellationToken);
        }

        public class ContentUpdate
        {
            public string Path { get; set; }
            public string Content { get; set; }
        }

        private string ResolveMarkdownPath(string path)
        {
            var relativePath = path.TrimStart('/');
            if (relativePath.EndsWith(".html")) relativePath = relativePath.Substring(0, relativePath.Length - 5);

            // Try .md
            var mdPath = Path.Combine(_inputPath, relativePath + ".md");
            if (File.Exists(mdPath)) return mdPath;

            // Try index.md
            mdPath = Path.Combine(_inputPath, relativePath, "index.md");
            if (File.Exists(mdPath)) return mdPath;

            // Try just file
            mdPath = Path.Combine(_inputPath, relativePath);
            if (File.Exists(mdPath)) return mdPath;

            return null;
        }
    }
}
