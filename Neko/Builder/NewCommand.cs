using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace Neko.Builder
{
    public static class NewCommand
    {
        public const string TemplateResourceName = "Neko.Resources.template.zip";

        public static int Run(string? path, bool force)
        {
            var targetPath = string.IsNullOrWhiteSpace(path)
                ? Directory.GetCurrentDirectory()
                : Path.GetFullPath(path);

            if (Directory.Exists(targetPath))
            {
                if (!force && Directory.EnumerateFileSystemEntries(targetPath).Any())
                {
                    Console.Error.WriteLine($"Refusing to initialize: '{targetPath}' is not empty.");
                    Console.Error.WriteLine("Pass --force to overwrite, or choose an empty directory with --path.");
                    return 1;
                }
            }
            else
            {
                Directory.CreateDirectory(targetPath);
            }

            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(TemplateResourceName);
            if (stream == null)
            {
                Console.Error.WriteLine($"Embedded template resource '{TemplateResourceName}' was not found in the Neko assembly.");
                return 2;
            }

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var targetFullPath = Path.GetFullPath(targetPath) + Path.DirectorySeparatorChar;
            var extracted = 0;

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/", StringComparison.Ordinal))
                {
                    var dirPath = Path.Combine(targetPath, entry.FullName);
                    Directory.CreateDirectory(dirPath);
                    continue;
                }

                var destination = Path.GetFullPath(Path.Combine(targetPath, entry.FullName));
                if (!destination.StartsWith(targetFullPath, StringComparison.Ordinal))
                {
                    throw new IOException($"Refusing to extract '{entry.FullName}' outside of '{targetPath}'.");
                }

                var destinationDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                entry.ExtractToFile(destination, overwrite: force);
                extracted++;
            }

            Console.WriteLine($"Initialized a new Neko documentation project at: {targetPath}");
            Console.WriteLine($"  {extracted} file(s) extracted.");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            if (!string.Equals(targetPath, Directory.GetCurrentDirectory(), StringComparison.Ordinal))
            {
                Console.WriteLine($"  cd \"{targetPath}\"");
            }
            Console.WriteLine("  neko watch");

            return 0;
        }
    }
}
