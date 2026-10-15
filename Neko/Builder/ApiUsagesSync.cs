using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Neko.Builder
{
    /// <summary>
    /// One call site of a documented member, as stored in a usages sidecar and
    /// rendered under the member by the <c>csharp-docs</c> block.
    /// </summary>
    public sealed class ApiUsageEntry
    {
        /// <summary>The configured source name (e.g. a demo repository).</summary>
        public string Source { get; set; } = "";
        /// <summary>Path of the file inside the source, with forward slashes.</summary>
        public string File { get; set; } = "";
        /// <summary>1-based line of the call itself.</summary>
        public int Line { get; set; }
        /// <summary>1-based first line of the stored excerpt.</summary>
        public int Start { get; set; }
        /// <summary>1-based last line of the stored excerpt.</summary>
        public int End { get; set; }
        /// <summary>Permalink to the excerpt in the public repository.</summary>
        public string Url { get; set; } = "";
        /// <summary>The excerpt: the statement around the call, dedented.</summary>
        public string Code { get; set; } = "";
    }

    public sealed class ApiUsageSourceInfo
    {
        public string Url { get; set; } = "";
        public string Commit { get; set; } = "";
    }

    /// <summary>
    /// The usages sidecar a <c>csharp-docs usages="…"</c> block reads: call sites
    /// keyed by <c>Type.Member</c> (the same key as the member's anchor, minus
    /// type parameters) or by the bare member name for blocks with no enclosing type.
    /// Produced by <see cref="ApiUsagesSync"/>; hand-written files use the same shape.
    /// </summary>
    public sealed class ApiUsages
    {
        public Dictionary<string, ApiUsageSourceInfo> Sources { get; set; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<ApiUsageEntry>> Members { get; set; } = new(StringComparer.Ordinal);

        [JsonIgnore]
        public int CallSiteCount => Members.Values.Sum(v => v.Count);

        public List<ApiUsageEntry> For(string key) =>
            key != null && Members.TryGetValue(key, out var list) ? list : new List<ApiUsageEntry>();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static ApiUsages? Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            try
            {
                return JsonSerializer.Deserialize<ApiUsages>(System.IO.File.ReadAllText(path), JsonOptions);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"  api-usages: could not read {path}: {ex.Message}");
                return null;
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions) + "\n";

        /// <summary>
        /// Resolves the <c>usages="…"</c> value of a fence: a leading <c>/</c> is the
        /// input root, anything else is relative to the page's folder.
        /// </summary>
        public static string? ResolvePath(string spec, string? pageFilePath, string? rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(spec)) return null;
            spec = spec.Trim().Replace('\\', '/');
            if (spec.StartsWith("/"))
            {
                if (string.IsNullOrEmpty(rootDirectory)) return null;
                return Path.GetFullPath(Path.Combine(rootDirectory, spec.TrimStart('/')));
            }
            if (Path.IsPathRooted(spec)) return spec;
            var dir = string.IsNullOrEmpty(pageFilePath) ? rootDirectory : Path.GetDirectoryName(Path.GetFullPath(pageFilePath));
            if (string.IsNullOrEmpty(dir)) return null;
            return Path.GetFullPath(Path.Combine(dir, spec));
        }
    }

    /// <summary>
    /// Fills the usages sidecars that <c>csharp-docs usages="…"</c> blocks point at
    /// with real call sites, scanned from the application checkouts listed under
    /// <c>apiDocs.usages</c> in the root <c>neko.yml</c>. The scan is syntactic
    /// (Roslyn, no compilation): a member access counts when its receiver is the
    /// documented type itself (<c>Graph.Connect(…)</c>), a name that reads as an
    /// instance of it (<c>graph</c>, <c>ctx.Graph</c>, <c>_schema</c> for
    /// <c>ISchema</c>), a variable declared with that type in the same file, or a
    /// fluent chain rooted in one of those. Constructors match <c>new Type(…)</c>.
    /// A source with a <c>package</c> filter only contributes files from projects
    /// whose <c>.csproj</c> references that package.
    /// </summary>
    public static class ApiUsagesSync
    {
        public sealed class Result
        {
            public int FilesUpdated;
            public int Sidecars;
            public int CallSites;
            public int Skipped;
            public int Errors;
        }

        private const int DefaultMaxPerMember = 4;
        private const int MaxExcerptLines     = 12;

        private static readonly Regex FencePattern = new(
            @"(?m)^[ \t]*```+[ \t]*csharp-docs\b(?<info>[^\n]*)\n(?<body>[\s\S]*?)^[ \t]*```+[ \t]*$",
            RegexOptions.Compiled);

        private static readonly Regex UsagesAttr = new(@"\busages\s*=\s*""(?<v>[^""]+)""", RegexOptions.Compiled);

        private sealed class DocumentedMember
        {
            public string Key = "";
            public string? Type;      // null for a bare member (block with no enclosing type)
            public string Name = "";  // identifier without type parameters
            public bool IsConstructor;
        }

        private sealed class Candidate
        {
            public ApiUsageEntry Entry = new();
            public int SourceIndex;
        }

        private sealed class Source
        {
            public string Name = "";
            public string Path = "";
            public string Url = "";
            public string Revision = "";
            public string? Package;
        }

        public static Result Run(string inputDir, bool verbose = false, bool dryRun = false)
        {
            var result = new Result();
            if (!Directory.Exists(inputDir)) return result;

            var (sources, maxPerMember) = GatherSources(inputDir, result);

            // Every sidecar a page names, with the members documented by the fences
            // that point at it (two blocks on one page may share one file).
            var sidecars = new Dictionary<string, List<DocumentedMember>>(StringComparer.OrdinalIgnoreCase);

            var markdownFiles = Directory.EnumerateFiles(inputDir, "*.md", SearchOption.AllDirectories)
                .Where(p => !HasHiddenSegment(Path.GetRelativePath(inputDir, Path.GetDirectoryName(p)!)))
                .OrderBy(p => p, StringComparer.Ordinal);

            foreach (var mdPath in markdownFiles)
            {
                var text = File.ReadAllText(mdPath);
                if (!text.Contains("csharp-docs")) continue;

                foreach (Match m in FencePattern.Matches(text))
                {
                    var attr = UsagesAttr.Match(m.Groups["info"].Value);
                    if (!attr.Success) continue;
                    if (InsideFence(text, m.Index)) continue;

                    var sidecar = ApiUsages.ResolvePath(attr.Groups["v"].Value, mdPath, inputDir);
                    if (sidecar is null) continue;

                    if (!sidecars.TryGetValue(sidecar, out var members))
                    {
                        members = new List<DocumentedMember>();
                        sidecars[sidecar] = members;
                    }
                    foreach (var dm in CollectDocumentedMembers(m.Groups["body"].Value))
                        if (members.All(x => x.Key != dm.Key)) members.Add(dm);
                }
            }

            if (sidecars.Count == 0)
            {
                if (verbose) Console.WriteLine("  api-usages: no `csharp-docs usages=\"…\"` block found.");
                return result;
            }

            if (sources.Count == 0)
            {
                Console.WriteLine("  api-usages: no source checkout available (apiDocs.usages in the root neko.yml) — leaving sidecars as-is.");
                result.Skipped += sidecars.Count;
                return result;
            }

            // One pass over every source file, matched against every documented member.
            var allMembers = sidecars.Values.SelectMany(v => v).GroupBy(m => m.Key).Select(g => g.First()).ToList();
            var found = Scan(sources, allMembers, verbose);

            foreach (var (sidecar, members) in sidecars.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                var usages = new ApiUsages();
                foreach (var s in sources)
                    usages.Sources[s.Name] = new ApiUsageSourceInfo { Url = s.Url, Commit = s.Revision };

                foreach (var member in members.OrderBy(m => m.Key, StringComparer.Ordinal))
                {
                    if (!found.TryGetValue(member.Key, out var candidates) || candidates.Count == 0) continue;
                    usages.Members[member.Key] = Select(candidates, sources.Count, maxPerMember);
                }

                var json = usages.ToJson();
                var rel = Path.GetRelativePath(inputDir, sidecar);
                var matched = usages.Members.Count;
                result.Sidecars++;
                result.CallSites += usages.CallSiteCount;
                Console.WriteLine($"  api-usages: {matched}/{members.Count} members used, {usages.CallSiteCount} call site(s) → {rel}");

                var existing = File.Exists(sidecar) ? File.ReadAllText(sidecar) : null;
                if (existing == json) continue;

                if (!dryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
                    File.WriteAllText(sidecar, json);
                }
                result.FilesUpdated++;
                if (verbose) Console.WriteLine($"  api-usages: {(dryRun ? "would update" : "updated")} {rel}");
            }

            Console.WriteLine($"api-usages sync: {result.FilesUpdated} updated, {result.Sidecars} sidecar(s), {result.CallSites} call site(s), {result.Skipped} skipped, {result.Errors} errors.");
            return result;
        }

        // ---- configuration ----

        private static (List<Source> Sources, int MaxPerMember) GatherSources(string inputDir, Result result)
        {
            var sources = new List<Source>();
            var cfgPath = Path.Combine(inputDir, "neko.yml");
            if (!File.Exists(cfgPath)) return (sources, DefaultMaxPerMember);

            Configuration.NekoConfig cfg;
            try { cfg = Configuration.ConfigParser.Parse(cfgPath); }
            catch { return (sources, DefaultMaxPerMember); }

            var max = cfg.ApiDocs?.MaxUsagesPerMember is int n && n > 0 ? n : DefaultMaxPerMember;
            if (cfg.ApiDocs?.Usages == null) return (sources, max);

            foreach (var u in cfg.ApiDocs.Usages)
            {
                if (u is null || string.IsNullOrWhiteSpace(u.Path)) continue;
                var path = Path.IsPathRooted(u.Path) ? u.Path : Path.GetFullPath(Path.Combine(inputDir, u.Path));
                var name = string.IsNullOrWhiteSpace(u.Name) ? Path.GetFileName(path.TrimEnd('/', '\\')) : u.Name.Trim();

                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"  api-usages: no checkout for '{name}' at {u.Path} — skipping that source.");
                    result.Skipped++;
                    continue;
                }

                sources.Add(new Source
                {
                    Name     = name,
                    Path     = path,
                    Url      = (u.Url ?? "").Trim().TrimEnd('/'),
                    Revision = ReadHeadCommit(path) ?? (string.IsNullOrWhiteSpace(u.Branch) ? "main" : u.Branch.Trim()),
                    Package  = string.IsNullOrWhiteSpace(u.Package) ? null : u.Package.Trim(),
                });
            }
            return (sources, max);
        }

        // The checkout's HEAD, so links are permalinks that survive later commits.
        private static string? ReadHeadCommit(string checkout)
        {
            try
            {
                var psi = new ProcessStartInfo("git", "rev-parse HEAD")
                {
                    WorkingDirectory       = checkout,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                };
                using var p = Process.Start(psi);
                if (p is null) return null;
                var output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(10_000);
                return p.ExitCode == 0 && Regex.IsMatch(output, "^[0-9a-f]{7,40}$") ? output : null;
            }
            catch
            {
                return null;
            }
        }

        // ---- what a block documents ----

        private static List<DocumentedMember> CollectDocumentedMembers(string block)
        {
            var list = new List<DocumentedMember>();
            var tree = CSharpSyntaxTree.ParseText(block, new CSharpParseOptions(kind: SourceCodeKind.Script));
            var root = tree.GetRoot();

            void Collect(SyntaxNode container)
            {
                foreach (var child in container.ChildNodes())
                {
                    switch (child)
                    {
                        case BaseNamespaceDeclarationSyntax ns:
                            Collect(ns);
                            break;
                        case BaseTypeDeclarationSyntax type:
                            CollectType(type, list);
                            break;
                        case MemberDeclarationSyntax member:
                        {
                            var name = MemberIdentifier(member);
                            if (name is null || !HasXmlDoc(member)) break;
                            if (list.All(x => x.Key != name))
                                list.Add(new DocumentedMember { Key = name, Type = null, Name = name });
                            break;
                        }
                    }
                }
            }
            Collect(root);
            return list;
        }

        private static void CollectType(BaseTypeDeclarationSyntax type, List<DocumentedMember> list)
        {
            var typeName = type.Identifier.Text;
            IEnumerable<MemberDeclarationSyntax> members = type switch
            {
                TypeDeclarationSyntax td => td.Members,
                EnumDeclarationSyntax en => en.Members,
                _                        => Array.Empty<MemberDeclarationSyntax>(),
            };

            foreach (var member in members)
            {
                if (member is BaseTypeDeclarationSyntax || !HasXmlDoc(member)) continue;
                var isCtor = member is ConstructorDeclarationSyntax;
                var name = isCtor ? typeName : MemberIdentifier(member);
                if (name is null) continue;
                var key = typeName + "." + name;
                if (list.Any(x => x.Key == key)) continue;
                list.Add(new DocumentedMember { Key = key, Type = typeName, Name = name, IsConstructor = isCtor });
            }
        }

        private static string? MemberIdentifier(MemberDeclarationSyntax node) => node switch
        {
            MethodDeclarationSyntax m      => m.Identifier.Text,
            PropertyDeclarationSyntax p    => p.Identifier.Text,
            EventDeclarationSyntax e       => e.Identifier.Text,
            EventFieldDeclarationSyntax ef => ef.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
            FieldDeclarationSyntax f       => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
            EnumMemberDeclarationSyntax em => em.Identifier.Text,
            _                              => null,
        };

        private static bool HasXmlDoc(SyntaxNode node) =>
            node.GetLeadingTrivia().Any(t => t.GetStructure() is DocumentationCommentTriviaSyntax);

        // ---- the scan ----

        private static Dictionary<string, List<Candidate>> Scan(List<Source> sources, List<DocumentedMember> members, bool verbose)
        {
            var byName = members
                .Where(m => !m.IsConstructor)
                .GroupBy(m => m.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var ctors = members.Where(m => m.IsConstructor).ToDictionary(m => m.Type!, StringComparer.Ordinal);
            var typeNames = new HashSet<string>(members.Where(m => m.Type != null).Select(m => m.Type!), StringComparer.Ordinal);

            var found = new Dictionary<string, List<Candidate>>(StringComparer.Ordinal);

            for (int si = 0; si < sources.Count; si++)
            {
                var source = sources[si];
                var files = EnumerateSourceFiles(source).ToList();
                int hits = 0;

                foreach (var file in files)
                {
                    string text;
                    try { text = File.ReadAllText(file); }
                    catch { continue; }

                    // Cheap pre-filter: a file naming none of the members can't use them.
                    if (!byName.Keys.Any(n => text.Contains(n, StringComparison.Ordinal)) &&
                        !ctors.Keys.Any(t => text.Contains("new " + t, StringComparison.Ordinal)))
                        continue;

                    var tree = CSharpSyntaxTree.ParseText(text, path: file);
                    var root = tree.GetRoot();
                    var declaredTypes = DeclaredVariableTypes(root, typeNames);
                    var seen = new HashSet<(string Key, int Statement)>();
                    var rel = Path.GetRelativePath(source.Path, file).Replace('\\', '/');

                    foreach (var node in root.DescendantNodes())
                    {
                        switch (node)
                        {
                            case MemberAccessExpressionSyntax access when byName.TryGetValue(access.Name.Identifier.Text, out var list):
                                foreach (var dm in list)
                                {
                                    if (dm.Type != null && !ReceiverMatches(access.Expression, dm.Type, declaredTypes, 0)) continue;
                                    Record(found, seen, dm.Key, access.Name, source, si, rel, text, ref hits);
                                }
                                break;

                            case MemberBindingExpressionSyntax binding when byName.TryGetValue(binding.Name.Identifier.Text, out var list):
                            {
                                var conditional = binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault();
                                foreach (var dm in list)
                                {
                                    if (dm.Type != null && (conditional is null || !ReceiverMatches(conditional.Expression, dm.Type, declaredTypes, 0))) continue;
                                    Record(found, seen, dm.Key, binding.Name, source, si, rel, text, ref hits);
                                }
                                break;
                            }

                            case InvocationExpressionSyntax { Expression: IdentifierNameSyntax id } when byName.TryGetValue(id.Identifier.Text, out var list):
                                // A bare call (`Foo(…)`) only matches a member documented without a type.
                                foreach (var dm in list.Where(m => m.Type is null))
                                    Record(found, seen, dm.Key, id, source, si, rel, text, ref hits);
                                break;

                            case ObjectCreationExpressionSyntax creation:
                            {
                                var typeName = SimpleTypeName(creation.Type);
                                if (typeName != null && ctors.TryGetValue(typeName, out var ctor))
                                    Record(found, seen, ctor.Key, creation, source, si, rel, text, ref hits);
                                break;
                            }
                        }
                    }
                }

                if (verbose) Console.WriteLine($"  api-usages: {source.Name}: {files.Count} file(s) scanned, {hits} call site(s).");
            }

            return found;
        }

        private static IEnumerable<string> EnumerateSourceFiles(Source source)
        {
            IEnumerable<string> projectDirs = Array.Empty<string>();
            if (source.Package != null)
            {
                var needle = $"Include=\"{source.Package}\"";
                projectDirs = EnumerateFiles(source.Path, "*.csproj")
                    .Where(p => File.ReadAllText(p).Contains(needle, StringComparison.OrdinalIgnoreCase))
                    .Select(p => Path.GetDirectoryName(p)!)
                    .ToList();
            }

            foreach (var file in EnumerateFiles(source.Path, "*.cs"))
            {
                if (source.Package != null && !projectDirs.Any(d => file.StartsWith(d + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
                    continue;
                yield return file;
            }
        }

        // Walks the tree by hand so build output and hidden folders (.git, .claude, …)
        // are never entered.
        private static IEnumerable<string> EnumerateFiles(string root, string pattern)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var dir = pending.Pop();
                IEnumerable<string> files;
                IEnumerable<string> subDirs;
                try
                {
                    files = Directory.EnumerateFiles(dir, pattern).OrderBy(f => f, StringComparer.Ordinal).ToList();
                    subDirs = Directory.EnumerateDirectories(dir).OrderByDescending(d => d, StringComparer.Ordinal).ToList();
                }
                catch (UnauthorizedAccessException) { continue; }
                catch (IOException) { continue; }

                foreach (var f in files) yield return f;
                foreach (var d in subDirs)
                {
                    var name = Path.GetFileName(d);
                    if (name.StartsWith('.') || name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("obj", StringComparison.OrdinalIgnoreCase) || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                        continue;
                    pending.Push(d);
                }
            }
        }

        // Variables, parameters, fields and properties declared with one of the
        // documented types, plus `var x = Type.Factory(…)` chains: name → type.
        private static Dictionary<string, string> DeclaredVariableTypes(SyntaxNode root, HashSet<string> typeNames)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (typeNames.Count == 0) return map;

            foreach (var node in root.DescendantNodes())
            {
                switch (node)
                {
                    case VariableDeclarationSyntax decl:
                    {
                        var declared = SimpleTypeName(decl.Type);
                        foreach (var v in decl.Variables)
                        {
                            if (declared != null && !decl.Type.IsVar && typeNames.Contains(declared))
                                map[v.Identifier.Text] = declared;
                            else if (v.Initializer != null && RootTypeName(v.Initializer.Value, typeNames) is string t)
                                map[v.Identifier.Text] = t;
                        }
                        break;
                    }
                    case ParameterSyntax p when p.Type != null && SimpleTypeName(p.Type) is string pt && typeNames.Contains(pt):
                        map[p.Identifier.Text] = pt;
                        break;
                    case PropertyDeclarationSyntax prop when SimpleTypeName(prop.Type) is string propType && typeNames.Contains(propType):
                        map[prop.Identifier.Text] = propType;
                        break;
                }
            }
            return map;
        }

        // The documented type a chain is rooted in (`Graph.Connect(…).WithTimeout(…)` → Graph).
        private static string? RootTypeName(ExpressionSyntax expr, HashSet<string> typeNames)
        {
            while (true)
            {
                switch (expr)
                {
                    case InvocationExpressionSyntax inv: expr = inv.Expression; continue;
                    case MemberAccessExpressionSyntax ma: expr = ma.Expression; continue;
                    case AwaitExpressionSyntax aw: expr = aw.Expression; continue;
                    case ParenthesizedExpressionSyntax pe: expr = pe.Expression; continue;
                    case ConditionalAccessExpressionSyntax ca: expr = ca.Expression; continue;
                    case ObjectCreationExpressionSyntax oc: return SimpleTypeName(oc.Type) is string n && typeNames.Contains(n) ? n : null;
                    case IdentifierNameSyntax id: return typeNames.Contains(id.Identifier.Text) ? id.Identifier.Text : null;
                    default: return null;
                }
            }
        }

        private static bool ReceiverMatches(ExpressionSyntax expr, string type, Dictionary<string, string> declared, int depth)
        {
            if (depth > 16) return false;
            switch (expr)
            {
                case IdentifierNameSyntax id:
                    return TypeNameMatches(id.Identifier.Text, type)
                        || LooksLikeInstance(id.Identifier.Text, type)
                        || (declared.TryGetValue(id.Identifier.Text, out var dt) && TypeNameMatches(dt, type));
                case GenericNameSyntax gn:
                    return TypeNameMatches(gn.Identifier.Text, type);
                case MemberAccessExpressionSyntax ma:
                    // `Curiosity.Library.Graph.Connect` (qualified type), `ctx.Graph.Query` (an
                    // instance property), or a chain whose root is the type.
                    return TypeNameMatches(ma.Name.Identifier.Text, type)
                        || LooksLikeInstance(ma.Name.Identifier.Text, type)
                        || ReceiverMatches(ma.Expression, type, declared, depth + 1);
                case ThisExpressionSyntax:
                    return false;
                case InvocationExpressionSyntax inv:
                    return ReceiverMatches(inv.Expression, type, declared, depth + 1);
                case AwaitExpressionSyntax aw:
                    return ReceiverMatches(aw.Expression, type, declared, depth + 1);
                case ParenthesizedExpressionSyntax pe:
                    return ReceiverMatches(pe.Expression, type, declared, depth + 1);
                case ConditionalAccessExpressionSyntax ca:
                    return ReceiverMatches(ca.Expression, type, declared, depth + 1);
                case CastExpressionSyntax cast:
                    return SimpleTypeName(cast.Type) is string ct && TypeNameMatches(ct, type);
                case ObjectCreationExpressionSyntax oc:
                    return SimpleTypeName(oc.Type) is string on && TypeNameMatches(on, type);
                case AliasQualifiedNameSyntax aq:
                    return TypeNameMatches(aq.Name.Identifier.Text, type);
                default:
                    return false;
            }
        }

        // `ISchema` is matched by `Schema` too: the interface a fluent factory hands out
        // is reached through the class that creates it.
        private static bool TypeNameMatches(string name, string type)
        {
            if (name == type) return true;
            return IsInterfaceName(type) && name == type.Substring(1);
        }

        // A receiver that reads as "an instance of the type": `graph`, `_graph`,
        // `Graph`, `targetGraph`, `ctx.Graph`.
        private static bool LooksLikeInstance(string name, string type)
        {
            var bare = name.TrimStart('_');
            if (bare.Length == 0) return false;
            var t = IsInterfaceName(type) ? type.Substring(1) : type;
            return bare.Equals(t, StringComparison.OrdinalIgnoreCase) || bare.EndsWith(t, StringComparison.Ordinal);
        }

        private static bool IsInterfaceName(string type) => type.Length >= 2 && type[0] == 'I' && char.IsUpper(type[1]);

        private static string? SimpleTypeName(TypeSyntax? type) => type switch
        {
            null                        => null,
            IdentifierNameSyntax id     => id.Identifier.Text,
            GenericNameSyntax g         => g.Identifier.Text,
            QualifiedNameSyntax q       => SimpleTypeName(q.Right),
            AliasQualifiedNameSyntax aq => SimpleTypeName(aq.Name),
            NullableTypeSyntax n        => SimpleTypeName(n.ElementType),
            _                           => null,
        };

        // ---- recording a hit ----

        private static void Record(
            Dictionary<string, List<Candidate>> found,
            HashSet<(string Key, int Statement)> seen,
            string key,
            SyntaxNode at,
            Source source,
            int sourceIndex,
            string relativeFile,
            string text,
            ref int hits)
        {
            var anchor = at.Ancestors().FirstOrDefault(a => a is StatementSyntax && a is not BlockSyntax)
                      ?? at.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax)
                      ?? at;

            if (!seen.Add((key, anchor.SpanStart))) return;

            var lines = at.SyntaxTree.GetText().Lines;
            var callLine = lines.GetLineFromPosition(at.SpanStart).LineNumber;
            var anchorSpan = anchor.GetLocation().GetLineSpan();
            int start = anchorSpan.StartLinePosition.Line;
            int end = anchorSpan.EndLinePosition.Line;

            // A long statement (a whole `foreach`, a big initializer) is cut to a
            // window around the call so the excerpt stays readable; the cut is marked.
            int fullStart = start, fullEnd = end;
            if (end - start + 1 > MaxExcerptLines)
            {
                start = Math.Max(start, callLine - 3);
                end = Math.Min(end, start + MaxExcerptLines - 1);
            }

            var code = Excerpt(lines, start, end);
            if (string.IsNullOrWhiteSpace(code)) return;
            if (start > fullStart) code = "// …\n" + code;
            if (end < fullEnd) code += "\n// …";

            var entry = new ApiUsageEntry
            {
                Source = source.Name,
                File   = relativeFile,
                Line   = callLine + 1,
                Start  = start + 1,
                End    = end + 1,
                Url    = BuildUrl(source, relativeFile, start + 1, end + 1),
                Code   = code,
            };

            if (!found.TryGetValue(key, out var list))
            {
                list = new List<Candidate>();
                found[key] = list;
            }
            list.Add(new Candidate { Entry = entry, SourceIndex = sourceIndex });
            hits++;
        }

        private static string Excerpt(TextLineCollection lines, int start, int end)
        {
            var raw = new List<string>();
            for (int i = start; i <= end && i < lines.Count; i++)
                raw.Add(lines[i].ToString().Replace("\t", "    ").TrimEnd());

            while (raw.Count > 0 && raw[0].Length == 0) raw.RemoveAt(0);
            while (raw.Count > 0 && raw[^1].Length == 0) raw.RemoveAt(raw.Count - 1);
            if (raw.Count == 0) return "";

            var indent = raw.Where(l => l.Length > 0).Min(l => l.Length - l.TrimStart().Length);
            return string.Join("\n", raw.Select(l => l.Length >= indent ? l.Substring(indent) : l));
        }

        private static string BuildUrl(Source source, string relativeFile, int start, int end)
        {
            if (string.IsNullOrEmpty(source.Url)) return "";
            var path = string.Join("/", relativeFile.Split('/').Select(Uri.EscapeDataString));
            var range = start == end ? $"#L{start}" : $"#L{start}-L{end}";
            return $"{source.Url}/blob/{source.Revision}/{path}{range}";
        }

        // Up to `max` call sites per member, rotating across sources and, inside a
        // source, across files, so the examples come from different places.
        private static List<ApiUsageEntry> Select(List<Candidate> candidates, int sourceCount, int max)
        {
            var perSource = new List<Queue<ApiUsageEntry>>();
            for (int s = 0; s < sourceCount; s++)
            {
                var inSource = candidates.Where(c => c.SourceIndex == s).Select(c => c.Entry)
                    .OrderBy(e => e.File, StringComparer.Ordinal).ThenBy(e => e.Line).ToList();
                perSource.Add(new Queue<ApiUsageEntry>(RotateAcrossFiles(inSource)));
            }

            var picked = new List<ApiUsageEntry>();
            bool any = true;
            while (picked.Count < max && any)
            {
                any = false;
                foreach (var q in perSource)
                {
                    if (q.Count == 0) continue;
                    picked.Add(q.Dequeue());
                    any = true;
                    if (picked.Count >= max) break;
                }
            }
            return picked;
        }

        private static IEnumerable<ApiUsageEntry> RotateAcrossFiles(List<ApiUsageEntry> ordered)
        {
            var byFile = ordered.GroupBy(e => e.File).Select(g => new Queue<ApiUsageEntry>(g)).ToList();
            bool any = true;
            while (any)
            {
                any = false;
                foreach (var q in byFile)
                {
                    if (q.Count == 0) continue;
                    yield return q.Dequeue();
                    any = true;
                }
            }
        }

        // ---- helpers shared with ApiDocsSync ----

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
    }
}
