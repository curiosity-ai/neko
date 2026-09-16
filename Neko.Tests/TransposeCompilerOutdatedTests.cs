using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    // A Tesserae sample that fails to compile is rendered into the page as an error
    // block, so one bad sample doesn't take the whole site down. Transpose's
    // outdated-compiler error (TPS0008) is the exception to that rule: it fails
    // every sample in the site for a reason that has nothing to do with any of
    // them, so it has to fail the build instead of being published.
    public class TransposeCompilerOutdatedTests
    {
        private static Diagnostic Make(string id, DiagnosticSeverity severity, string message) =>
            Diagnostic.Create(
                new DiagnosticDescriptor(id, id, "{0}", "Transpose", severity, isEnabledByDefault: true),
                Location.None,
                message);

        [Test]
        public void ThrowsOnTheOutdatedCompilerDiagnostic()
        {
            var diagnostics = new List<Diagnostic>
            {
                Make("TPS0008", DiagnosticSeverity.Error,
                     "This project references assemblies built by a newer Transpose — 'Tesserae' (26.9.9999). "
                     + "The compiler in use is 26.9.5180, but 26.9.9999 or newer is required."),
            };

            var ex = Assert.Throws<TransposeCompilerOutdatedException>(
                () => TransposeCompilerOutdatedException.ThrowIfOutdated(diagnostics));

            // The message Transpose wrote carries the version to install, so it has to
            // survive into what Neko reports.
            Assert.That(ex!.Message, Does.Contain("26.9.9999"));
            Assert.That(ex.Message, Does.Contain("Transpose.Compiler.Library"));
        }

        [Test]
        public void IgnoresOrdinaryCompileErrors()
        {
            var diagnostics = new List<Diagnostic>
            {
                Make("CS0103", DiagnosticSeverity.Error, "The name 'x' does not exist in the current context"),
                Make("TPS0108", DiagnosticSeverity.Warning, "a transitive package was not found"),
            };

            Assert.DoesNotThrow(() => TransposeCompilerOutdatedException.ThrowIfOutdated(diagnostics));
        }

        [Test]
        public void IgnoresNullAndEmptyDiagnostics()
        {
            Assert.DoesNotThrow(() => TransposeCompilerOutdatedException.ThrowIfOutdated(null));
            Assert.DoesNotThrow(() => TransposeCompilerOutdatedException.ThrowIfOutdated(Array.Empty<Diagnostic>()));
        }
    }
}
