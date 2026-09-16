using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Neko.Builder;
using Transpose.Compiler.Library;

namespace Neko.Tests
{
    // Pins the contract Neko relies on to fail a build rather than publish the
    // failure: Transpose reports an assembly built by a newer compiler as the
    // TPS0008 diagnostic on the compilation result. Neko reads it by id, so a
    // rename or a severity change in a future Transpose release has to show up
    // here rather than as a site full of red error boxes.
    public class TransposeOutdatedCompilerIntegrationTests
    {
        // A .NET assembly carrying nothing but the stamp Transpose reads: a build of
        // a compiler far newer than anything that will ever consume it.
        private static string WriteStampedAssembly(string dir, string minimumCompilerVersion)
        {
            var stamp = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(new
            {
                compilerVersion = minimumCompilerVersion,
                minimumCompilerVersion,
            }));

            var corlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var compilation = CSharpCompilation.Create(
                "FromTheFuture",
                new[] { CSharpSyntaxTree.ParseText("public class FromTheFuture { }") },
                references: new[] { corlib },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var path = Path.Combine(dir, "FromTheFuture.dll");

            using (var stream = File.Create(path))
            {
                // `Transpose.Build.json` is the manifest-resource name Transpose's
                // BuildStamp reads the minimum compiler version out of.
                var emit = compilation.Emit(stream, manifestResources: new[]
                {
                    new ResourceDescription("Transpose.Build.json", () => new MemoryStream(stamp), isPublic: true),
                });

                Assert.That(emit.Success, Is.True,
                    "the stub reference assembly failed to emit: "
                    + string.Join("; ", emit.Diagnostics.Select(d => d.ToString())));
            }

            return path;
        }

        [Test]
        public void ANewerAssemblyIsReportedAsTps0008AndFailsTheBuild()
        {
            var dir = Path.Combine(Path.GetTempPath(), "neko-tps0008-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                var reference = WriteStampedAssembly(dir, "99.9.9999");

                var result = TransposeCompilerLibrary.Compile(
                    new CompilationRequest("App")
                        .WithReferenceAssembly(reference)
                        .WithSource("public class App { public static void Main() { } }"));

                var outdated = result.Diagnostics.SingleOrDefault(
                    d => d.Id == TransposeCompilerOutdatedException.DiagnosticId);

                Assert.That(outdated, Is.Not.Null,
                    "Transpose no longer reports an assembly built by a newer compiler as "
                    + TransposeCompilerOutdatedException.DiagnosticId
                    + "; Neko's fail-the-build check keys off that id.");
                Assert.That(outdated!.Severity, Is.EqualTo(DiagnosticSeverity.Error));
                Assert.That(result.Success, Is.False);

                Assert.Throws<TransposeCompilerOutdatedException>(
                    () => TransposeCompilerOutdatedException.ThrowIfOutdated(result.Diagnostics));
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* best effort */ }
            }
        }
    }
}
