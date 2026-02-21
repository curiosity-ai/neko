using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace TailDocs.CLI.Server
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

            // Serve static files from the output directory
            if (Directory.Exists(_rootPath))
            {
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
