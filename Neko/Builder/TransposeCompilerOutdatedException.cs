using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Neko.Builder
{
    /// <summary>
    /// Raised when Transpose refuses to compile because the compiler Neko is built
    /// against is older than an assembly the compilation binds to — Transpose's
    /// <c>TPS0008</c> diagnostic.
    /// </summary>
    /// <remarks>
    /// Every other Tesserae compile failure is a problem with the sample, so it is
    /// rendered into the page as an error block and the rest of the site still
    /// builds. This one is not: it says nothing about the sample and applies to
    /// <em>every</em> sample in the site, so baking it into the output would publish
    /// a page full of "Tesserae compilation failed" boxes that look like content
    /// errors. It has exactly one fix — bump the <c>Transpose.Compiler.Library</c>
    /// PackageReference in <c>Neko/Neko.csproj</c> and re-release Neko — so it fails
    /// the build instead.
    /// </remarks>
    public sealed class TransposeCompilerOutdatedException : Exception
    {
        /// <summary>The Transpose diagnostic id this exception stands for.</summary>
        public const string DiagnosticId = "TPS0008";

        public TransposeCompilerOutdatedException(string message) : base(message) { }

        /// <summary>
        /// Throws when <paramref name="diagnostics"/> carries Transpose's
        /// outdated-compiler error. Safe to call on a successful build too — the
        /// diagnostic is only ever reported as an error, so a build that succeeded
        /// never carries one.
        /// </summary>
        public static void ThrowIfOutdated(IReadOnlyList<Diagnostic> diagnostics)
        {
            if (diagnostics == null) return;

            var outdated = diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error && d.Id == DiagnosticId)
                .ToList();

            if (outdated.Count == 0) return;

            throw new TransposeCompilerOutdatedException(
                "The Transpose compiler Neko is built against is out of date and cannot compile "
                + "Tesserae samples correctly. Update the Transpose.Compiler.Library PackageReference "
                + "in Neko/Neko.csproj to the version Transpose reports below, then rebuild Neko."
                + Environment.NewLine
                + string.Join(Environment.NewLine, outdated.Select(d => d.GetMessage())));
        }
    }
}
