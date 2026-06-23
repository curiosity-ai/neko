using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neko.Builder
{
    /// <summary>
    /// Keeps API-reference pages in sync with the <b>public surface</b> of real
    /// source code. Pages carry marker pairs:
    /// <code>
    /// &lt;!-- api:source start repo="mosaik" file="src/Graph.cs" type="Graph" --&gt;
    /// ```csharp-docs
    /// …regenerated public surface…
    /// ```
    /// &lt;!-- api:source end --&gt;
    /// </code>
    /// The block between the markers is fully regenerated on every run. Only
    /// public/protected types and members (with their XML doc comments) are kept;
    /// method/accessor bodies, field initializers, attribute lists, and every
    /// private/internal member are stripped, so no implementation is vendored into
    /// the docs. When a marker's source root can't be resolved the block is left
    /// untouched (a notice is printed) so a build without the source checked out
    /// still succeeds against the committed snapshot.
    /// </summary>
    public static class ApiDocsSync
    {
        public sealed class Result
        {
            public int FilesUpdated;
            public int MarkerRefreshes;
            public int Skipped;
            public int Errors;
        }

        private static readonly Regex MarkerPattern = new(
            @"<!--\s*api:source\s+start\s+(?<attrs>.*?)-->(?<body>[\s\S]*?)<!--\s*api:source\s+end\s*-->",
            RegexOptions.Compiled);

        private static readonly Regex AttrPattern = new(@"(?<k>\w+)\s*=\s*""(?<v>[^""]*)""", RegexOptions.Compiled);

        /// <summary>
        /// Regenerates every <c>api:source</c> marker block under <paramref name="inputDir"/>.
        /// Source roots are resolved solely from the root <c>neko.yml</c>'s <c>apiDocs.roots</c>
        /// (paths relative to that file). There is no CLI override, environment-variable, or
        /// hard-coded path fallback: a root the root config doesn't declare is treated as
        /// missing and its block is left untouched.
        /// </summary>
        public static Result Run(string inputDir, bool verbose = false, bool dryRun = false)
        {
            var result = new Result();
            if (!Directory.Exists(inputDir)) return result;

            var roots = GatherConfiguredRoots(inputDir);

            // Only scan pages that actually carry a marker (cheap pre-filter).
            var markdownFiles = Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories)
                .Where(p => !HasHiddenSegment(Path.GetRelativePath(inputDir, Path.GetDirectoryName(p)!)));

            var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach (var mdPath in markdownFiles)
            {
                var originalText = File.ReadAllText(mdPath);
                if (!MarkerPattern.IsMatch(originalText)) continue;

                var newText = MarkerPattern.Replace(originalText, m =>
                {
                    // Ignore markers shown as examples inside a fenced code block
                    // (e.g. a docs page documenting the marker syntax itself).
                    if (InsideFence(originalText, m.Index)) return m.Value;

                    var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (Match a in AttrPattern.Matches(m.Groups["attrs"].Value))
                        attrs[a.Groups["k"].Value] = a.Groups["v"].Value;

                    attrs.TryGetValue("repo", out var repo);
                    attrs.TryGetValue("file", out var fileAttr);
                    attrs.TryGetValue("type", out var typeAttr);

                    var startMarker = $"<!-- api:source start repo=\"{repo}\" file=\"{fileAttr}\"" +
                                      (string.IsNullOrEmpty(typeAttr) ? "" : $" type=\"{typeAttr}\"") + " -->";

                    if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(fileAttr))
                    {
                        Console.WriteLine($"  api-docs: malformed marker in {Path.GetRelativePath(inputDir, mdPath)} (needs repo= and file=).");
                        result.Errors++;
                        return m.Value;
                    }

                    if (!resolved.TryGetValue(repo, out var root))
                    {
                        root = ResolveRoot(repo, roots);
                        resolved[repo] = root;
                    }
                    if (root is null)
                    {
                        Console.WriteLine($"  api-docs: no source for repo '{repo}' — leaving {Path.GetRelativePath(inputDir, mdPath)} as-is.");
                        result.Skipped++;
                        return m.Value;
                    }

                    var wantedTypes = (typeAttr ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var files = fileAttr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    var blocks = new List<string>();
                    foreach (var rel in files)
                    {
                        var full = Path.Combine(root, rel.Replace('\\', '/'));
                        if (!File.Exists(full))
                        {
                            Console.WriteLine($"  api-docs: source file not found ({rel}) — leaving {Path.GetRelativePath(inputDir, mdPath)} as-is.");
                            result.Skipped++;
                            return m.Value;
                        }
                        blocks.AddRange(ExtractPublicSurface(File.ReadAllText(full), wantedTypes));
                    }

                    var sb = new StringBuilder();
                    sb.Append(startMarker).Append('\n');
                    foreach (var block in blocks)
                    {
                        sb.Append("\n```csharp-docs\n");
                        sb.Append(block);
                        if (!block.EndsWith("\n")) sb.Append('\n');
                        sb.Append("```\n\n");
                    }
                    sb.Append("<!-- api:source end -->");
                    result.MarkerRefreshes++;
                    return sb.ToString();
                });

                if (newText != originalText)
                {
                    if (!dryRun) File.WriteAllText(mdPath, newText);
                    result.FilesUpdated++;
                    if (verbose) Console.WriteLine($"  api-docs: {(dryRun ? "would update" : "updated")} {Path.GetRelativePath(inputDir, mdPath)}");
                }
            }

            if (result.FilesUpdated > 0 || result.Skipped > 0 || result.Errors > 0)
                Console.WriteLine($"api-docs sync: {result.FilesUpdated} updated, {result.MarkerRefreshes} blocks, {result.Skipped} skipped, {result.Errors} errors.");

            return result;
        }

        // Reads apiDocs.roots from the root neko.yml only (the project config at
        // the input directory), resolving each path relative to that file. Nested
        // sub-project configs are intentionally not consulted: roots are declared
        // once, at the root, so a multi-repo watch has a single source of truth.
        private static Dictionary<string, string> GatherConfiguredRoots(string inputDir)
        {
            var roots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var cfgPath = Path.Combine(inputDir, "neko.yml");
            if (!File.Exists(cfgPath)) return roots;

            Configuration.NekoConfig cfg;
            try { cfg = Configuration.ConfigParser.Parse(cfgPath); }
            catch { return roots; }

            if (cfg.ApiDocs?.Roots == null) return roots;
            foreach (var kv in cfg.ApiDocs.Roots)
            {
                if (string.IsNullOrWhiteSpace(kv.Value)) continue;
                var path = Path.IsPathRooted(kv.Value) ? kv.Value : Path.GetFullPath(Path.Combine(inputDir, kv.Value));
                roots[kv.Key] = path;
            }
            return roots;
        }

        private static string? ResolveRoot(string repo, IDictionary<string, string> configured) =>
            configured.TryGetValue(repo, out var p) && Directory.Exists(p) ? p : null;

        // True when the offset sits inside a fenced code block (an odd number of
        // ``` / ~~~ fences precede it), so marker examples in docs aren't rewritten.
        private static bool InsideFence(string text, int index)
        {
            int fences = Regex.Matches(text.Substring(0, index), @"(?m)^[ \t]*(`{3,}|~{3,})").Count;
            return (fences % 2) == 1;
        }

        private static bool HasHiddenSegment(string relativeDir)
        {
            foreach (var part in relativeDir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                if (part.Length > 0 && part != "." && part.StartsWith('.')) return true;
            return false;
        }

        // --- extraction (mirrors the standalone update-api-docs.cs script) ---

        private static List<string> ExtractPublicSurface(string source, string[] wantedTypes)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetCompilationUnitRoot();

            var allTypes = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
                .Where(t => t.Parent is not BaseTypeDeclarationSyntax)
                .ToList();

            IEnumerable<BaseTypeDeclarationSyntax> selected = wantedTypes.Length > 0
                ? wantedTypes.SelectMany(w => allTypes.Where(t => t.Identifier.Text == w))
                : allTypes.Where(t => IsVisible(t.Modifiers));

            var rewriter = new PublicSurfaceRewriter();
            var output = new List<string>();
            foreach (var type in selected.Distinct())
            {
                if (rewriter.Visit(type) is not MemberDeclarationSyntax reduced) continue;
                var ns = GetNamespace(type);
                var body = reduced.ToFullString().Replace("\r\n", "\n");
                // A stripped body leaves the semicolon adrift from the signature
                // (on its own line, or after the space that preceded the old '{');
                // pull it back onto the signature token.
                body = Regex.Replace(body, @"(\S)[ \t]*\n?[ \t]*;", "$1;").Trim('\n');
                output.Add(string.IsNullOrEmpty(ns) ? body + "\n" : $"namespace {ns}\n{{\n{body}\n}}\n");
            }
            return output;
        }

        private static string GetNamespace(SyntaxNode node)
        {
            for (var p = node.Parent; p is not null; p = p.Parent)
                if (p is BaseNamespaceDeclarationSyntax ns) return ns.Name.ToString();
            return "";
        }

        private static bool IsVisible(SyntaxTokenList mods) =>
            (mods.Any(t => t.IsKind(SyntaxKind.PublicKeyword)) || mods.Any(t => t.IsKind(SyntaxKind.ProtectedKeyword)))
            && !mods.Any(t => t.IsKind(SyntaxKind.PrivateKeyword));

        // Rewrites a type to its public surface: drops non-visible members, strips
        // bodies, keeps the XML doc trivia on what remains.
        private sealed class PublicSurfaceRewriter : CSharpSyntaxRewriter
        {
            private static readonly SyntaxToken Semi = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
            private static readonly SyntaxToken None = SyntaxFactory.Token(SyntaxKind.None);

            private bool _inInterface;

            private bool Visible(SyntaxTokenList mods)
            {
                if (mods.Any(t => t.IsKind(SyntaxKind.PrivateKeyword))) return false;
                if (_inInterface) return true;
                return mods.Any(t => t.IsKind(SyntaxKind.PublicKeyword)) || mods.Any(t => t.IsKind(SyntaxKind.ProtectedKeyword));
            }

            private static T NoAttrs<T>(T n) where T : MemberDeclarationSyntax => (T)n.WithAttributeLists(default);

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax n)
            {
                if (!Visible(n.Modifiers)) return null;
                if (n.Body is null && n.ExpressionBody is null) return NoAttrs(n);
                return NoAttrs(n).WithBody(null).WithExpressionBody(null).WithSemicolonToken(Semi);
            }

            public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n).WithBody(null).WithExpressionBody(null).WithInitializer(null).WithSemicolonToken(Semi) : null;

            public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n).WithBody(null).WithExpressionBody(null).WithSemicolonToken(Semi) : null;

            public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n).WithBody(null).WithExpressionBody(null).WithSemicolonToken(Semi) : null;

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax n)
            {
                if (!Visible(n.Modifiers)) return null;
                if (!HasAccessorBodies(n.AccessorList) && n.ExpressionBody is null) return NoAttrs(n);
                return NoAttrs(n).WithAccessorList(StripAccessors(n.AccessorList)).WithExpressionBody(null).WithInitializer(null).WithSemicolonToken(None);
            }

            public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax n)
            {
                if (!Visible(n.Modifiers)) return null;
                if (!HasAccessorBodies(n.AccessorList) && n.ExpressionBody is null) return NoAttrs(n);
                return NoAttrs(n).WithAccessorList(StripAccessors(n.AccessorList)).WithExpressionBody(null).WithSemicolonToken(None);
            }

            public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n).WithAccessorList(StripAccessors(n.AccessorList)) : null;

            public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n) : null;

            public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n) : null;

            public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax n) =>
                Visible(n.Modifiers) ? NoAttrs(n) : null;

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax n) =>
                VisitType(n, () => (ClassDeclarationSyntax)base.VisitClassDeclaration(n)!);
            public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax n) =>
                VisitType(n, () => (StructDeclarationSyntax)base.VisitStructDeclaration(n)!);
            public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax n) =>
                VisitType(n, () => (RecordDeclarationSyntax)base.VisitRecordDeclaration(n)!);

            public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax n)
            {
                if (!IsTopLevel(n) && !Visible(n.Modifiers)) return null;
                var prev = _inInterface;
                _inInterface = true;
                var visited = (InterfaceDeclarationSyntax)base.VisitInterfaceDeclaration(n)!;
                _inInterface = prev;
                return NoAttrs(visited);
            }

            public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax n) =>
                (IsTopLevel(n) || Visible(n.Modifiers)) ? NoAttrs(n) : null;

            private SyntaxNode? VisitType<T>(T n, Func<T> recurse) where T : TypeDeclarationSyntax
            {
                if (!IsTopLevel(n) && !Visible(n.Modifiers)) return null;
                return NoAttrs(recurse());
            }

            private static bool IsTopLevel(SyntaxNode n) => n.Parent is not BaseTypeDeclarationSyntax;

            private static bool HasAccessorBodies(AccessorListSyntax? list) =>
                list is not null && list.Accessors.Any(a => a.Body is not null || a.ExpressionBody is not null);

            private static AccessorListSyntax StripAccessors(AccessorListSyntax? accessors)
            {
                if (accessors is null)
                    return SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Semi)
                    }));

                var stripped = accessors.Accessors.Select(a => a
                    .WithAttributeLists(default).WithBody(null).WithExpressionBody(null).WithSemicolonToken(Semi));
                return accessors.WithAccessors(SyntaxFactory.List(stripped));
            }
        }
    }
}
