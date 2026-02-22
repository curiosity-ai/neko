using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Neko.Server
{
    public class DevServer
    {
        private readonly string _rootPath;
        private readonly int _port;

        public DevServer(string rootPath, int port = 5000)
        {
            _rootPath = Path.GetFullPath(rootPath);
            _port = port;
        }

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders(); // Reduce noise
            builder.WebHost.UseUrls($"http://localhost:{_port}");

            var app = builder.Build();

            if (Directory.Exists(_rootPath))
            {
                // Rewriter for extensionless URLs
                var rewriteOptions = new RewriteOptions()
                    .Add(context => {
                        var request = context.HttpContext.Request;
                        var path = request.Path.Value;
                        if (string.IsNullOrEmpty(path) || path == "/") return;

                        // If path has extension, skip
                        if (Path.HasExtension(path)) return;

                        // Check if .html file exists
                        var relativePath = path.TrimStart('/');
                        var filePath = Path.Combine(_rootPath, relativePath + ".html");

                        // Also check if directory + index.html exists? usually FileServer handles directory index but rewrite might be needed if url is /folder without slash
                        // But let's stick to extensionless file rewriting first.

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

            await app.RunAsync();
        }
    }
}
