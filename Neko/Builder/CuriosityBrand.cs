using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neko.Builder
{
    /// <summary>
    /// The Curiosity brand's own drawn vocabulary, for the curiosity video theme: the website's 4 by 4 pixel
    /// glyphs (with their frame animations) and Sudo, the house pixel cat, decoded from the website's sprite
    /// sheet. Both are data, not drawings: the glyph frames are copied from Tesserae's Curiosity theme
    /// (<c>Tesserae.Themes.Curiosity/src/Components/PixelGlyphKind.cs</c>, itself generated from the website's
    /// <c>site/figures.js</c>), and the sheet from the website's <c>js/sudo-sheet.js</c>. When either changes
    /// there, copy the new strings across rather than redrawing them here.
    ///
    /// Everything is decoded at build time; the page only switches between frames the build already drew.
    /// </summary>
    public static class CuriosityBrand
    {
        // ---------------------------------------------------------------------
        // Pixel glyphs
        // ---------------------------------------------------------------------

        /// <summary>Palette keys of the glyph notation (Tesserae's <c>PixelGlyph</c> constants).</summary>
        public const int Empty = 0, Ink = 1, Slate = 2, Ash = 3, Signal = 4, Hollow = 5, Deep = 6, Stone = 7;

        /// <summary>One frame: 16 palette keys in reading order, how long it holds (ms) and an optional caption.</summary>
        public sealed record GlyphFrame(int[] Cells, int Ms, string Label);

        /// <summary>A glyph's animation in the pixel notation, whether it loops, and whether empty cells show a hairline.</summary>
        public sealed record GlyphSpec(string Animation, bool Loop, bool ShowEmpty)
        {
            /// <summary>True for a still glyph given the diagonal build (<see cref="BuildAnimation"/>) rather than frames of its own.</summary>
            public bool IsBuild => Animation.StartsWith(BuildStart, StringComparison.Ordinal);
        }

        private const string BuildStart = "................:220";

        /// <summary>
        /// Every glyph the website draws, by kebab-case name: <c>value-*</c> (the brand's eight values),
        /// <c>use-case-*</c>, <c>capability-*</c> (in the deep blue), <c>category-*</c>, the three
        /// <c>play-*</c> stories (played once, captioned) and the 54 <c>loop-*</c> developer glyphs.
        /// </summary>
        public static readonly Dictionary<string, GlyphSpec> Glyphs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["value-connected"] = new GlyphSpec(Build("...*..k..k..k..."), true, false),
            ["value-resolved"] = new GlyphSpec(Build(".k..k*k..k......"), true, false),
            ["value-traceable"] = new GlyphSpec(Build(".*...k...k..kkk."), true, false),
            ["value-sovereign"] = new GlyphSpec(Build("kkk.k*k.kkk....."), true, false),
            ["value-fast"] = new GlyphSpec(Build("..k.kkk*..k....."), true, false),
            ["value-open"] = new GlyphSpec(Build("k..*k...kkk....."), true, false),
            ["value-precise"] = new GlyphSpec(Build("k.k..*..k.k....."), true, false),
            ["value-curious"] = new GlyphSpec(Build("...*kk..kkk.kkk."), true, false),
            ["use-case-knowledge-management"] = new GlyphSpec(Build("kkk.k*k.kkk....k"), true, false),
            ["use-case-technical-customer-support"] = new GlyphSpec(Build("kkkkk.*kkkkkk..."), true, false),
            ["use-case-engineering"] = new GlyphSpec(Build(".k*k.kk..k..k..."), true, false),
            ["use-case-quality-management"] = new GlyphSpec(Build("...*..kkkkk..k.."), true, false),
            ["use-case-contracts-and-compliance"] = new GlyphSpec(Build("kk*.k.kkk..kkkkk"), true, false),
            ["capability-graph"] = new GlyphSpec(Build("d..d.dd..dd.d..d"), true, false),
            ["capability-retrieval"] = new GlyphSpec(Build("dd.dddddd..d...d"), true, false),
            ["capability-permissions"] = new GlyphSpec(Build(".dd.d..ddddddd.d"), true, false),
            ["capability-models"] = new GlyphSpec(Build("dd..dd....dd..dd"), true, false),
            ["capability-connectors"] = new GlyphSpec(Build(".dd.dddddddd.dd."), true, false),
            ["capability-memory"] = new GlyphSpec(Build("dddddddddddddd.."), true, false),
            ["category-communication"] = new GlyphSpec(Build("kkkkk..kkkkkk..."), true, false),
            ["category-email"] = new GlyphSpec(Build("kkkk.kk.k..kkkkk"), true, false),
            ["category-calendars"] = new GlyphSpec(Build("k..kkkkkkk.kkkkk"), true, false),
            ["category-productivity"] = new GlyphSpec(Build("...k..kk.kkkkkkk"), true, false),
            ["category-project-management"] = new GlyphSpec(Build("kk.kkk.kk..kk..."), true, false),
            ["category-development"] = new GlyphSpec(Build("kkk..kk..kkkkk.."), true, false),
            ["category-storage"] = new GlyphSpec(Build("kk..kkkkk..kkkkk"), true, false),
            ["category-crm"] = new GlyphSpec(Build(".kk..kk.kkkkk..k"), true, false),
            ["category-web"] = new GlyphSpec(Build(".kk.k..kkkkk.kk."), true, false),
            ["play-backlog"] = new GlyphSpec("*.../..../..../....:600:Position_01 k*../..../..../....:170:Position_02 ks*./..../..../....:170:Position_03 ksk*/..../..../....:170:Position_04 kska/*.../..../....:170:Position_05 kska/k*../..../....:170:Position_06 kska/kk*./..../....:170:Position_07 kska/kka*/..../....:170:Position_08 kska/kkas/*.../....:170:Position_09 kska/kkas/k*../....:170:Position_10 kska/kkas/ks*./....:170:Position_11 kska/kkas/ksk*/....:170:Position_12 kska/kkas/kskk/*...:170:Position_13 kska/kkas/kskk/a*..:170:Position_14 kska/kkas/kskk/as*.:170:Position_15 kska/kkas/kskk/ask*:170:Position_16", false, true),
            ["play-spreadsheet"] = new GlyphSpec("kkkk/..../..../....:260:Week_1 kkkk/o.../..../....:70 kkkk/so../..../....:70 kkkk/sao./..../....:70 kkkk/saao/..../....:70 kkkk/saaa/o.../....:70 kkkk/saaa/so../....:70 kkkk/saaa/sao./....:70 kkkk/saaa/saao/....:70 kkkk/saaa/saaa/o...:70 kkkk/saaa/saaa/so..:70 kkkk/saaa/saaa/sao.:70 kkkk/saaa/saaa/saao:70 kkkk/saaa/saaa/saaa:520 kkkk/..../..../....:260:Week_2 kkkk/o.../..../....:70 kkkk/so../..../....:70 kkkk/sao./..../....:70 kkkk/saao/..../....:70 kkkk/saaa/o.../....:70 kkkk/saaa/so../....:70 kkkk/saaa/sao./....:70 kkkk/saaa/saao/....:70 kkkk/saaa/saaa/o...:70 kkkk/saaa/saaa/so..:70 kkkk/saaa/saaa/sao.:70 kkkk/saaa/saaa/saao:70 kkkk/saaa/saaa/saaa:520 kkkk/..../..../....:260:Week_3 kkkk/o.../..../....:70 kkkk/so../..../....:70 kkkk/sao./..../....:70 kkkk/saao/..../....:70 kkkk/saaa/o.../....:70 kkkk/saaa/so../....:70 kkkk/saaa/sao./....:70 kkkk/saaa/saao/....:70 kkkk/saaa/saaa/o...:70 kkkk/saaa/saaa/so..:70 kkkk/saaa/saaa/sao.:70 kkkk/saaa/saaa/saao:70 kkkk/saaa/saaa/saaa:520", false, true),
            ["play-dead-prototype"] = new GlyphSpec("k.../..../..../....:45:Demo kk../..../..../....:45 kkk./..../..../....:45 kkkk/..../..../....:45 kkkk/k.../..../....:45 kkkk/ks../..../....:45 kkkk/ksa./..../....:45 kkkk/ksak/..../....:45 kkkk/ksak/k.../....:45 kkkk/ksak/ka../....:45 kkkk/ksak/kaa./....:45 kkkk/ksak/kaak/....:45 kkkk/ksak/kaak/k...:45 kkkk/ksak/kaak/kk..:45 kkkk/ksak/kaak/kkk.:45 kkkk/ksak/kaak/kkkk:700 kkkk/k.ak/kaak/kkkk:150:Real kkkk/k.ak/kaa./kkkk:150 kk.k/k.ak/kaa./kkkk:150 kk.k/k.ak/kaa./.kkk:150 kk.k/k.a./kaa./.kkk:150 k..k/k.a./kaa./.kkk:150 k..k/k.a./kaa./.k.k:150 a..a/a.a./aaa./.a.a:380 ..../a..a/aaa./aaaa:110:Dead ..../a.../aaaa/aaaa:110", false, true),
            ["loop-search-hybrid-by-default"] = new GlyphSpec("k.a./..../..../.... k.a./k.a./..../.... k.a./k.a./.*../.... k.a./k.a./.*../.k..", true, false),
            ["loop-search-permission-aware"] = new GlyphSpec("kaka/..../ssss/.... ..../kaka/ssss/.... ..../..../ksks/.... ..../..../ssss/k.*.", true, false),
            ["loop-search-traversal-in-the-query"] = new GlyphSpec("k.../..../..../.... k.../k.../..../.... k.../kk../..../.... k.../kk../.k../.... k.../kk../.kk./.... k.../kk../.kk./..k. k.../kk../.kk./..k*", true, false),
            ["loop-search-facets-and-filters"] = new GlyphSpec("kaaa/..../aaak/.... akaa/..../aaka/.... aaka/..../akaa/.... aaka/..../a*aa/....", true, false),
            ["loop-search-ranking-you-can-inspect"] = new GlyphSpec("kk../kkkk/k.../kkk. kkkk/kk../kkk./k... kkkk/kkk./kk../k... kkk*/kkk./kk../k...", true, false),
            ["loop-search-sub-second-at-scale"] = new GlyphSpec("..../k.../..../....:70 ..../ak../..../....:70 ..../aak./..../....:70 ..../aaak/..../....:70 ..k./kkk*/..k./....", true, false),
            ["loop-graph-types-you-define"] = new GlyphSpec("oo../oo../..../.... kk../kk../..../.... kk../kk../..oo/..oo kk../kk../..aa/..a*", true, false),
            ["loop-graph-traversal-at-speed"] = new GlyphSpec("*..a/.a../..a./a..k:90 k..a/.*../..a./a..k:90 k..a/.a../..*./a..k:90 k..a/.a../..a./a..*", true, false),
            ["loop-graph-revisions-kept"] = new GlyphSpec("ss../ss../..../.... ss../saa./.aa./.... ss../saa./.akk/..kk ss../s*a./.akk/..kk", true, false),
            ["loop-graph-records-stay-put"] = new GlyphSpec(".k../..../..../....:120 kkk./.k../..../....:120 k*k./kkk./.k../....:120 kkk./k*k./kkk./.k..:260 k*k./kkk./.k../....:120 kkk./k*k./kkk./.k..", true, false),
            ["loop-graph-permissions-on-edges"] = new GlyphSpec("k.../..../..../.... ka../s.../..../.... kaa./s.../s.../.... kaak/s.../s.../s... kaak/s..a/s.../s... kaak/s..a/s..a/s... kaak/s..a/s..a/s..*", true, false),
            ["loop-graph-incremental-updates"] = new GlyphSpec("k.*./.a../k.a./....:260 k.k./.*../k.a./....:260 k.k./.a../*.a./....:260 k.k./.a../k.*./....", true, false),
            ["loop-enrichment-entity-extraction"] = new GlyphSpec("..../aaaa/aaaa/aa.. ..../aaka/aaaa/aa..:260 ..k./aa.a/aaaa/aa.. ..*./aa.a/aaaa/aa..", true, false),
            ["loop-enrichment-classification"] = new GlyphSpec("..../..../..../k..a .k../..../..../k..a ..../k.../..../k..a ..../..../k.../k..a ..a./..../k.../k..a ..../...a/k.../k..a ..../..../k..a/k..a ..*./..../k..a/k..a", true, false),
            ["loop-enrichment-identifier-normalization"] = new GlyphSpec("k.kk/.kk./kk.k/....:360 kkk./kkk./kkk./....:300 aaa./kkk*/aaa./....", true, false),
            ["loop-enrichment-documents-and-media"] = new GlyphSpec("kkk./k.k./k.k./kkk. kkk./kak./k.k./kkk. kkk./kaka/k.k./kkk. kkk./kaka/kak./kkk. kkk./kaka/kak*/kkk.", true, false),
            ["loop-enrichment-your-own-pipelines"] = new GlyphSpec("..*./aa.a/..../.... ..../aa*a/..../.... ..../aa*a/..../a... ..../aa*a/..../.a.. ..../aa*a/..../..k. ..../aa*a/..../...k", true, false),
            ["loop-enrichment-incremental"] = new GlyphSpec("kkk./..../..../.... kkk./aaa./..../.... kkk./a*a./..../.... kkk./kkk./..../.... kkk./kkk./aaa./.... kkk./kkk./a*a./....", true, false),
            ["loop-embeddings-model-of-your-choice"] = new GlyphSpec(".kk./a..a/a..a/aaaa ..../akka/akka/aaaa:320 .oo./a..a/a..a/aaaa ..../aooa/aooa/aaaa:320 .kk./a..a/a..a/aaaa ..../akka/akka/aa*a", true, false),
            ["loop-embeddings-no-second-datastore"] = new GlyphSpec("kkk./k.k./kkk./...a kkk./k.k./kkka/.... kkk./k.ka/kkk./.... kkk./k*k./kkk./....", true, false),
            ["loop-embeddings-chunking-that-respects-structure"] = new GlyphSpec("k.../..../..../.... k.../aaa./..../.... k.../aaa./k.../.... k.../aaa./k.../aa*.", true, false),
            ["loop-embeddings-fused-retrieval"] = new GlyphSpec("kkk./aaa./ooo./.... kk../aa../oo../k... kk../aa../oo../ka.. kk../aa../oo../kao. kk../aa../oo../kao*", true, false),
            ["loop-embeddings-permission-aware"] = new GlyphSpec("..../.*../..../.... .k../k*k./.k../....:320 .s../k*s./.k../....", true, false),
            ["loop-embeddings-re-embedding-handled"] = new GlyphSpec("a.a./.a.a/a.a./.a.a k.a./.a.a/k.a./.a.a k.a./.k.a/k.a./.k.a k.k./.k.a/k.k./.k.a k.k./.k.k/k.k./.k.k k.k./.k.k/k.k./.k.*", true, false),
            ["loop-agents-tools-over-the-graph"] = new GlyphSpec("..../.*../..../.... ..../.*ak/..../.... ..../.*ak/.a../.k.. ..../.*ak/.aa./.k.k", true, false),
            ["loop-agents-acting-as-auser"] = new GlyphSpec("kkkk/k*.k/k..k/kkkk:260 kkkk/k.*k/k..k/kkkk:260 kkkk/k..k/k.*k/kkkk:260 kkkk/k..k/k*.k/kkkk:260 kkkk/k*.k/k..k/kkkk", true, false),
            ["loop-agents-full-trace"] = new GlyphSpec("k.../..../..../.... ka../..../..../.... kaa./..../..../.... kaa./..a./..../.... kaa./..a./.a../.... kaa./..a./.a../*...", true, false),
            ["loop-agents-your-own-tools"] = new GlyphSpec("k.k./..../k.../.... k.k*/..../k.../.... k.k./...*/k.../.... k.k./..../k.*./....", true, false),
            ["loop-agents-any-model"] = new GlyphSpec("...*/k..a/...a/...o ...a/k..*/...a/...o ...a/k..a/...*/...o ...a/kaa*/...a/...o", true, false),
            ["loop-agents-long-running-work"] = new GlyphSpec("aa../k.../..../....:280 aa../kk../..../....:280 aa../kkk./..../....:280 aa../kkkk/..../....:280 aa../kkkk/k.../....:280 aa../kkkk/kk../....:280 aa../kkkk/kk*./....", true, false),
            ["loop-integrations-seventy-plus-systems"] = new GlyphSpec("..../.k../..../....:60 ..../.k../..../..a.:60 k.../.k../..../..a.:60 k.../.k../..k./..a.:60 k..a/.k../..k./..a.:60 k..a/.k../k.k./..a.:60 k..a/.k../k.k./.ka.:60 ka.a/.k../k.k./.ka.:60 ka.a/.ka./k.k./.ka.:60 ka.a/.ka./k.ka/.ka.:60 ka.a/aka./k.ka/.ka.:60 ka.a/aka./kaka/.ka.:60 kaka/aka./kaka/.ka.:60 kaka/aka./kaka/aka.:60 kaka/akak/kaka/aka.:60 kaka/akak/kaka/aka*:60", true, false),
            ["loop-integrations-structure-preserved"] = new GlyphSpec("k.../a.../..../.... kk../aa../..../.... kk../aa../...k/...a kk../aa../..kk/..a*", true, false),
            ["loop-integrations-permissions-synced"] = new GlyphSpec("k.../a.../k.../k... k..k/a.../k.../k... k..k/a..a/k.../k... k..k/a..a/k..k/k... k..k/a..a/k..k/k..*", true, false),
            ["loop-integrations-connector-sdk"] = new GlyphSpec("...a/k..a/...a/...a ...a/kk.a/k..a/...a ...a/kkka/kk.a/...a ...a/kkk*/kk.a/...a", true, false),
            ["loop-integrations-incremental-sync"] = new GlyphSpec("..../kk.a/k.ka/.... ..../k.ka/kk.a/.... ..../kk.a/k.ka/.... ..../k.ka/kk.a/.... ..../k.k*/kk.a/....", true, false),
            ["loop-integrations-nothing-moves"] = new GlyphSpec("..../..../..kk/..kk a.../..../..kk/..kk a.../.a../..kk/..kk a.../.a../..kk/..k*", true, false),
            ["loop-tools-csharp-sdks"] = new GlyphSpec("*.../..../..../.... k*../..../..../.... kk*./..../..../.... kka*/..../..../.... kka./.*../..../.... kka./.k*./..../.... kka./.ka*/..../.... kka./.kaa/*.../.... kka./.kaa/k*../....:400 kka./.kaa/k.../....:400 kka./.kaa/k*../....", true, false),
            ["loop-tools-rest-apis"] = new GlyphSpec("..../k..k/k..k/.... ..../k*.k/k..k/.... ..../k.*k/k..k/.... ..../k..k/k.ak/.... ..../k..k/ka.k/.... ..../k*.k/k.ak/....", true, false),
            ["loop-tools-build-environment"] = new GlyphSpec("kkkk/k..k/kkkk/.kk. kkkk/ka.k/kkkk/.kk. kkkk/kaak/kkkk/.kk. kkkk/ka*k/kkkk/.kk.", true, false),
            ["loop-tools-query-inspection"] = new GlyphSpec("*kkk/.kkk/..kk/...k kkkk/.*kk/..kk/...k kkkk/.kkk/..*k/...k kkkk/.kkk/..kk/...*", true, false),
            ["loop-tools-versioned-configuration"] = new GlyphSpec("k.../..../..../.... ka../..../..../.... ka../..a./..../.... ka../..a./..a./.... ka../k.a./..a./.... ka../k.a./k.a./.... ka../k.a./k.a./k... ka../k.a./k.a./k*..", true, false),
            ["loop-tools-operational-visibility"] = new GlyphSpec("..../..../.k.k/k.k. ..../..../k.k./.k.k ...*/..../.k../k.k.", true, false),
            ["loop-extensibility-custom-entity-types"] = new GlyphSpec("k.../..../..../.... ka../..../..../.... kaa./..../..../.... kaa./a.../..../.... kaa./a.../a.../.... kaa./a.../a.../k... kaa*/a.../a.../k...", true, false),
            ["loop-extensibility-custom-endpoints"] = new GlyphSpec("kkk./k.k./kkk./.... kkk./k.k*/kkk./.... kkk./k*k./kkk./....:300 kkk./k.k*/kkk./....", true, false),
            ["loop-extensibility-custom-interfaces"] = new GlyphSpec("kkkk/k.../k.../k... kkkk/k.aa/k.../k... kkkk/k.aa/k.aa/k... kkkk/k.aa/k.aa/k.a*", true, false),
            ["loop-extensibility-pipeline-steps"] = new GlyphSpec("aaa./aaa./..../aaa. aaa./aaa./...*/aaa. aaa./aaa./..*k/aaa. aaa./aaa./.*kk/aaa. aaa./aaa./*kk./aaa.", true, false),
            ["loop-extensibility-agent-tools"] = new GlyphSpec("kk.a/kk../..../.... kk.a/kk.a/..../.... kk.a/kk.a/...a/.... kk.a/kk.a/...a/...*", true, false),
            ["loop-extensibility-same-permissions"] = new GlyphSpec("k.kk/..../..../.... k.kk/o.oo/..../.... k.kk/k.kk/..../.... k.kk/k.kk/o.oo/.... k.kk/k.kk/k.k*/....", true, false),
            ["loop-security-relationship-based-permissions"] = new GlyphSpec("aaa./aaa./aaa./.... kaa./aaa./aaa./.... kaa./aak./aaa./.... kaa./aak./a*a./....", true, false),
            ["loop-security-synced-from-the-source"] = new GlyphSpec("kkk./kkk./..../.... kkk./kkk./.a../.... kkk./kkk./.a../.*..", true, false),
            ["loop-security-sso-with-enforced-mfa"] = new GlyphSpec("kkkk/kk.k/..../....:320 kkkk/kk.k/..../a... kkkk/kk.k/..../aa.. kkkk/kk.k/..../aaa. kkkk/kk.k/..../aaa*", true, false),
            ["loop-security-full-audit-trail"] = new GlyphSpec("..../..../..../kka. ..../..../kka./ka.. ..../kka./ka../kkk. kka./ka../kkk./ka*.", true, false),
            ["loop-security-your-infrastructure"] = new GlyphSpec("kkk*/kkka/kkka/a..a:260 kkka/kkk*/kkka/a..a:260 kkka/kkka/kkk*/a..a", true, false),
            ["loop-security-no-training-on-your-data"] = new GlyphSpec("kkk./k*k./kkk./...a:420 kkk./k*k./kkka/....:520 kkk./k*k./kkk./...a", true, false),
        };

        private static string Build(string glyph) => BuildAnimation(glyph);

        /// <summary>The spec for a name, a 16-cell glyph written inline (<c>k..*k...kkk.....</c>), or null.</summary>
        public static GlyphSpec ResolveGlyph(string nameOrCells)
        {
            if (string.IsNullOrWhiteSpace(nameOrCells)) return null;
            var key = nameOrCells.Trim();
            if (Glyphs.TryGetValue(key, out var spec)) return spec;
            if (Glyphs.TryGetValue("value-" + key, out spec)) return spec;
            try
            {
                // An animation or a single glyph in the notation: a single frame builds itself in.
                var frames = Decode(key);
                if (frames.Count == 1) return new GlyphSpec(BuildAnimation(key), true, false);
                if (frames.Count > 1) return new GlyphSpec(key, true, false);
            }
            catch (FormatException)
            {
            }
            return null;
        }

        /// <summary>
        /// Decodes the pixel notation: frames separated by spaces; a frame is 16 cells (rows may be split by
        /// <c>/</c>), then optionally <c>:ms</c> (default 190) and <c>:label</c> (<c>_</c> for a space).
        /// Cells: <c>.</c> empty, <c>k</c> ink, <c>s</c> slate, <c>a</c> ash, <c>*</c> Signal, <c>o</c> hollow,
        /// <c>d</c> deep blue, <c>t</c> stone, or a digit.
        /// </summary>
        public static List<GlyphFrame> Decode(string animation)
        {
            var frames = new List<GlyphFrame>();
            if (string.IsNullOrWhiteSpace(animation)) return frames;
            foreach (var token in animation.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = token.Split(':');
                var cells = new int[16];
                var n = 0;
                foreach (var ch in parts[0])
                {
                    if (ch == '/') continue;
                    if (n >= 16) throw new FormatException($"Pixel glyph frame '{token}' has more than 16 cells.");
                    cells[n++] = KeyOf(ch, token);
                }
                if (n != 16) throw new FormatException($"Pixel glyph frame '{token}' has {n} cells, not 16.");
                var ms = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : 190;
                var label = parts.Length > 2 ? parts[2].Replace('_', ' ') : null;
                frames.Add(new GlyphFrame(cells, ms, label));
            }
            return frames;
        }

        /// <summary>
        /// The build-in animation for a still glyph, as Tesserae and the website play it: a diagonal sweep from
        /// the top left lays each cell down behind an ash leading edge, and the Signal cells land last.
        /// </summary>
        public static string BuildAnimation(string glyph)
        {
            var target = Decode(glyph).Last().Cells;
            var sb = new StringBuilder(BuildStart);
            var accent = target.Any(k => k == Signal);
            for (int d = 0; d <= 7; d++)
            {
                var frame = new int[16];
                for (int i = 0; i < 16; i++)
                {
                    var k = target[i];
                    var diag = (i % 4) + (i / 4);
                    if (k == Empty || k == Signal) continue;
                    if (diag < d) frame[i] = k;
                    else if (diag == d) frame[i] = Ash;
                }
                sb.Append(' ').Append(Encode(frame)).Append(":70");
            }
            var settled = target.Select(k => k == Signal ? Empty : k).ToArray();
            sb.Append(' ').Append(Encode(settled)).Append(accent ? ":320" : string.Empty);
            if (accent) sb.Append(' ').Append(Encode(target));
            return sb.ToString();
        }

        /// <summary>16 keys as a frame string, digits and letters as in the notation.</summary>
        public static string Encode(int[] cells)
        {
            var sb = new StringBuilder(16);
            foreach (var k in cells) sb.Append(k < 10 ? (char)('0' + k) : (char)('A' + k - 10));
            return sb.ToString();
        }

        private static int KeyOf(char ch, string token)
        {
            if (ch >= '0' && ch <= '9') return ch - '0';
            if (ch >= 'A' && ch <= 'Z') return ch - 'A' + 10;
            switch (ch)
            {
                case '.': return Empty;
                case 'k': return Ink;
                case 's': return Slate;
                case 'a': return Ash;
                case '*': return Signal;
                case 'o': return Hollow;
                case 'd': return Deep;
                case 't': return Stone;
                default: throw new FormatException($"Pixel glyph frame '{token}' has an unknown cell '{ch}'.");
            }
        }

        // ---------------------------------------------------------------------
        // Sudo
        // ---------------------------------------------------------------------

        /// <summary>A frame of Sudo: 11 by 8 cells indexing <see cref="SudoInk"/>, and his ear cell (or null).</summary>
        public sealed record SudoFrame(int[] Cells, (int X, int Y)? Ear);

        /// <summary>A clip of the sheet: its frames, ms per frame, whether it loops, and what follows it.</summary>
        public sealed record SudoClip(string Name, IReadOnlyList<SudoFrame> Frames, int Ms, bool Loops, string Next);

        public const int SudoWidth = 11, SudoHeight = 8;

        /// <summary>The brand's Sudo tones on paper (BRAND.md s8), indexed by cell value; 0 is empty.</summary>
        public static readonly string[] SudoInk = { "", "#4A5462", "#4A5462", "#4A5462", "#1E2430", "#1E2430", "#1E2430",
            "#1E2430", "#1E2430", "#1E2430", "#12161F", "#12161F", "#8A9099", "#C5C9CE", "#2A2F38" };

        /// <summary>The same cat on ink: ash, line, paper, the laptop one step lighter.</summary>
        public static readonly string[] SudoInkDark = { "", "#8A9099", "#8A9099", "#8A9099", "#DEE0DF", "#DEE0DF", "#DEE0DF",
            "#DEE0DF", "#DEE0DF", "#DEE0DF", "#F4F4F2", "#F4F4F2", "#6B7280", "#C5C9CE", "#8A9099" };

        /// <summary>His ears: the two Signal half cells.</summary>
        public const string SudoEar = "#2F6BFF";

        // The website's sheet, compressed the way the blog compresses it (js/sudo-sheet.js), exactly.
        private const string SudoSheet =
            "HHHlrHHqrHHlr0rqpHqrls5Hl7vtvHl5wH0igvgHHHHrHHlrHrqp0rHs5HrlovtvHq7swHl5qvgHligHHrHHqrHHlrHH0r0rqpHq"
          + "7qs5Hq5ovtvHigqsjhHHlvgHHHHHHl6HHrHH0rlrqpHl7s5Hl5vtvHligvgHHHHqrHHqrHHlr0rqpHqrls5Hl7vtvHl5wH0igvgH"
          + "HHHqrHHqrHHlrHH0rlrqpHl7s5Hl5vtvHligvgHHHHHHHrHHqrHHlr0rqpHqrls5Hl7vtvHligvgHHHHlrHHqrHHlr0rqpHqrls5"
          + "Hl7vtvHl5wzHigqvHHHHlrHHqrHHlr0rqpHqrls5Hl7vtvHl5wHligqvqgHHHHHHlrHHqrHHlr0rqpHqr7s5Hl5vtvHligvgHHHH"
          + "qrHHqrHrqp0rHs5HrlovtvHq7swHl5qvgHligHHrHHqrHHlrHH0r0rqpHq7qs5Hq5ovtvHigqsjhHHlvgHHH0rHHqrHHlrHH0rlr"
          + "qpHl7s5Hl5vtvHligvgHHHHHHHrHHqrHHlr0rqpHqr7s5Hl5vtvHligvgHq6lrqpHr0s5HqrsovtvHl5wHliHvHHHHHHHHHlrHHq"
          + "r0rqpHr0s5HqrlvtvHl7wH05/H0ilvHHHHHHHHH0rHHqrHHlr0rqpHqrls5Hl7vtvHligvgHHHHrHHqrHHlr0rqpHqrls5Hl7vtv"
          + "Hl5wH0igqvgHHHHrHHqrHHlrHHlrlrqpHl7s5Hl5vtvH0igqvgHqrHHqrHHlrHHlrHH07rqpHlu/5H0uvtvH0igqvgHqrHHqrHHl"
          + "rHHlrHH07rqpHlu/5H0uvtvH0igqvgHHHHrHHqrHHlrHHlrlrqpHl7s5Hl5vtvH0igqvgHHHHrHHqrHHlr0rqpHqrls5Hl7vtvHl"
          + "5wH0igqvgHHHHHHHrHHqr0rqpHr0s5HqrqovtvHlouwH0igvgHHHHHHHlrqpHHs5HlrqvtvHqrqowHlrqo/H0rig+HHHHHHHlrqp"
          + "HHs5HlrqvtvHqrqowHlrqo/H0rig+HHHHHHHlrqpHHs5HlrqvtvHqrqowHlrqo/H0rig+HHHHHHHlrqpHHs5HlrqvtvHqrqowHlr"
          + "qo/H0rig+HHHHHHHlrqpHHs5HlrqvtvHqrqowHlrqo/H0rig+HHHHHHHlrqpHlrqs5HqrlvtvHqrqowH0ro/HHig+HHHHHHHlrqp"
          + "Hlrqs5HqrlvtvHqrqowH0ro/HHig+HHHHqrHHqrHHlrHH0rlrqpHl7s5Hl5vtvHligvgHHHHHHHrHHqrHHlr0rqpHqrls5Hl7vtv"
          + "HligvgHHHHHHHrHHqrHHlr0rqpHqrls5Hl7vtvHligvgHHHHHH0rHHlrHHlr0rqpHqrls5Hl7vtvHligvgHHHHHHlrHH0rHHlr0r"
          + "qpHqrls5Hl7vtvHligvgHHHHHH0rHHlrHHlr0rqpHqrls5Hl7vtvHligvgHHHHHHHrHHqrHHlr0rqpHqrls5Hl7vtvHligvgHHHH"
          + "HHHrHHqrHHlr0rqpHqrls5Hl7vtvHligvgHHHHHHHrHHqrHHlr0rqpHqr7s5Hl5vtvHligvgHHHHHHHHHHHHqrHHlr0rqpHqr7s5"
          + "HligvtvHHHHHHHHHHHHHHH0rqpHl7s5H6igvtvHHHHHHHHHHHHHHH0rqpHl7s5H6igvtvHHHHHHlrHHqr0rqpHr0s5HqrqovtvHl"
          + "ouwH0igvgTHHHHHHlrqpHlrqs5HqrlvtvHqrqowHlrqo/+lmlrig+THHHHHHlrqpHlrqs5HqrlvtvHqrqowHmqrqo/+qnmlrig+T"
          + "HHHHHHlrqpHlrqs5Hqrlvtv0mqrqow0nmqrqo/+qnmlrig+THHHHHHlrqpHlrqs5Hqrlvtv0mqrqow0nmqrqo/+qnmlrig+THHHH"
          + "HHlrqpHqrls5Hqrlvtv0mqrqow0nmqrqo/vlnmlrigJAHHHHHHlrqpHr0s5Hqrlvtv0mqrqow0kmqrqo/+qnmlrig+THHHHHHlrq"
          + "pHqrls5Hqrlvtv0mqrqow0kmqrqo/vlnmlrigJAHHHHHHrqrqpHqrls5Hqrlvtv0mqrqow0nmlro/+qnm0ig+THHHHHHrqrqpHqr"
          + "ls5Hqrlvtv0mqrqow0nmlro/vlnm0igJA";

        private static readonly (string Name, int Count, int Ms, bool Loops, string Next)[] SudoTable =
        {
            ("move", 4, 80, true, "move"), ("idle", 3, 260, true, "idle"), ("interact", 2, 140, false, "idle"),
            ("jumpUp", 2, 110, false, "jumpDown"), ("jumpDown", 2, 110, false, "idle"), ("startle", 4, 100, false, "idle"),
            ("stretch", 6, 120, false, "sit"), ("sit", 2, 130, false, "sitIdle"), ("sitIdle", 6, 300, true, "sitIdle"),
            ("crouch", 2, 130, false, "crouchIdle"), ("crouchIdle", 6, 300, true, "crouchIdle"), ("sleep", 3, 200, false, "sleepIdle"),
            ("sleepIdle", 1, 450, true, "sleepIdle"), ("work", 4, 150, false, "workIdle"), ("workIdle", 6, 130, true, "workIdle"),
        };

        private static Dictionary<string, SudoClip> _sudoClips;

        /// <summary>Sudo's clips by name (<c>move</c>, <c>idle</c>, <c>sit</c>, <c>sitIdle</c>, <c>work</c>, <c>workIdle</c>, <c>sleep</c>, …).</summary>
        public static IReadOnlyDictionary<string, SudoClip> SudoClips => _sudoClips ??= DecodeSudo();

        private static Dictionary<string, SudoClip> DecodeSudo()
        {
            const string digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
            const string symbols = ".123456789abcde";
            var sb = new StringBuilder();
            foreach (var ch in SudoSheet)
            {
                var v = digits.IndexOf(ch) ^ 42;
                var c = symbols[v % symbols.Length];
                sb.Append(c, v / symbols.Length + 1);
            }
            var sheet = sb.ToString();

            var clips = new Dictionary<string, SudoClip>(StringComparer.OrdinalIgnoreCase);
            var cursor = 0;
            foreach (var (name, count, ms, loops, next) in SudoTable)
            {
                var frames = new List<SudoFrame>();
                for (int f = 0; f < count; f++)
                {
                    var cells = new int[SudoWidth * SudoHeight];
                    (int, int)? ear = null;
                    for (int p = 0; p < cells.Length; p++)
                    {
                        var v = symbols.IndexOf(sheet[cursor + p]);
                        cells[p] = v < 0 ? 0 : v;
                        if (v == 3) ear = (p % SudoWidth, p / SudoWidth);
                    }
                    frames.Add(new SudoFrame(cells, ear));
                    cursor += cells.Length;
                }
                clips[name] = new SudoClip(name, frames, ms, loops, next);
            }
            return clips;
        }

        /// <summary>
        /// One frame of Sudo as an SVG in cell units, facing right: his ten columns (the sheet's spare column 0
        /// left out), each cell a square in his tones, his ears two Signal half cells.
        /// </summary>
        public static string SudoFrameSvg(SudoFrame frame, bool onInk, string cls = null)
        {
            var tones = onInk ? SudoInkDark : SudoInk;
            var sb = new StringBuilder();
            sb.Append($"<svg{(cls == null ? string.Empty : $" class=\"{cls}\"")} viewBox=\"1 0 10 8\" shape-rendering=\"crispEdges\" aria-hidden=\"true\">");
            for (int p = 0; p < frame.Cells.Length; p++)
            {
                var v = frame.Cells[p];
                if (v == 0 || string.IsNullOrEmpty(tones[v])) continue;
                sb.Append($"<rect x=\"{p % SudoWidth}\" y=\"{p / SudoWidth}\" width=\"1.02\" height=\"1.02\" fill=\"{tones[v]}\"/>");
            }
            if (frame.Ear is { } ear)
            {
                sb.Append($"<rect x=\"{ear.X - 2}\" y=\"{ear.Y}\" width=\".5\" height=\".5\" fill=\"{SudoEar}\"/>");
                sb.Append($"<rect x=\"{ear.X}\" y=\"{ear.Y}\" width=\".5\" height=\".5\" fill=\"{SudoEar}\"/>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        // ---------------------------------------------------------------------
        // The mark and the wordmark
        // ---------------------------------------------------------------------

        /// <summary>
        /// The Escape mark (BRAND.md s5): a 3 by 3 block of 20 unit squares on a 24 unit grid, the top right one
        /// moved one cell out on the diagonal. Each square carries <c>data-mark</c> (its cell) so the runtime can
        /// build it in; the escaped one is <c>data-mark="escape"</c>.
        /// </summary>
        public static string MarkSvg(string cls = "nv-mark-svg")
        {
            var sb = new StringBuilder($"<svg class=\"{cls}\" viewBox=\"24 0 96 96\" aria-hidden=\"true\">");
            var cells = new[] { (26, 26), (50, 26), (26, 50), (50, 50), (74, 50), (26, 74), (50, 74), (74, 74) };
            for (int i = 0; i < cells.Length; i++)
            {
                sb.Append($"<rect class=\"nv-mark-cell\" data-mark=\"{i}\" x=\"{cells[i].Item1}\" y=\"{cells[i].Item2}\" width=\"20\" height=\"20\"/>");
            }
            sb.Append("<rect class=\"nv-mark-escape\" data-mark=\"escape\" x=\"98\" y=\"2\" width=\"20\" height=\"20\"/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>
        /// The wordmark: <c>curiosity</c> set with both i's as the dotless ı and a square where each dot would be
        /// (the website's construction at weight 600). The second square is Signal when the wordmark stands
        /// alone. The plain word travels beside the letters for assistive technology.
        /// </summary>
        public static string WordmarkHtml(string word = "curiosity", bool signal = true)
        {
            var sb = new StringBuilder("<span class=\"nv-wordmark-brand\"><span class=\"nv-sr\">").Append(System.Net.WebUtility.HtmlEncode(word)).Append("</span><span aria-hidden=\"true\">");
            var seen = 0;
            foreach (var ch in word)
            {
                if (ch == 'i')
                {
                    seen++;
                    var sig = signal && seen == 2;
                    sb.Append($"<span class=\"nv-wm-i\">ı<i class=\"nv-wm-dot{(sig ? " is-signal" : string.Empty)}\"></i></span>");
                }
                else
                {
                    sb.Append(System.Net.WebUtility.HtmlEncode(ch.ToString()));
                }
            }
            sb.Append("</span></span>");
            return sb.ToString();
        }
    }
}
