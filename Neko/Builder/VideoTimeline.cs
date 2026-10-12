using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.Tables;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Neko.Builder
{
    /// <summary>
    /// The timeline compiler of video mode. Everything that decides <em>when</em>
    /// something happens is worked out here, at build time, and written into the
    /// scene's HTML as plain numbers, so the page's runtime (<c>video.js</c>) is a pure
    /// function of time: given <c>t</c>, it computes each element's state from its
    /// <c>data-v</c> (the motion), <c>data-at</c> and <c>data-dur</c> (scene-local
    /// seconds) and nothing else. That is what makes a render deterministic.
    ///
    /// Three passes:
    /// <list type="number">
    ///   <item><see cref="Annotate"/> walks a scene's Markdown tree before it is rendered
    ///   and gives every top-level block a default motion — headings rise word by word,
    ///   paragraphs and quotes rise, list items and table rows stagger in — so plain
    ///   Markdown animates without a single attribute.</item>
    ///   <item><see cref="Compile"/> walks the rendered HTML in document order and turns
    ///   every <c>data-at="auto"</c> into a time on the scene's running cursor, splits
    ///   text into words or characters where a motion asks for it, and sizes the scene
    ///   when its <c>duration</c> is left to Neko.</item>
    ///   <item><see cref="Sequence"/> lays the scenes end to end, overlapping each pair by
    ///   the transition between them.</item>
    /// </list>
    /// </summary>
    public static class VideoTimeline
    {
        /// <summary>A motion's default duration and the gap it leaves before the next element starts.</summary>
        private readonly record struct Motion(double Duration, double Gap);

        private static readonly Dictionary<string, Motion> Motions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["rise"] = new(0.6, 0.22),
            ["fade"] = new(0.5, 0.2),
            ["up"] = new(0.6, 0.22),
            ["drop"] = new(0.6, 0.22),
            ["left"] = new(0.6, 0.22),
            ["right"] = new(0.6, 0.22),
            ["pop"] = new(0.55, 0.2),
            ["zoom"] = new(0.8, 0.3),
            ["blur"] = new(0.7, 0.3),
            ["slam"] = new(0.45, 0.35),
            ["wipe"] = new(0.7, 0.3),
            ["wipe-up"] = new(0.7, 0.3),
            ["draw"] = new(0.9, 0.3),
            ["grow"] = new(0.8, 0.1),
            ["grow-x"] = new(0.8, 0.1),
            ["count"] = new(1.6, 0.4),
            ["sweep"] = new(0.6, 0.2),
            ["scramble"] = new(1.0, 0.4),
            ["check"] = new(0.4, 0.12),
            ["cursor"] = new(1.2, 1.5),
            ["swap"] = new(2.7, 0.4),
            ["word"] = new(0.55, 0.06),
            ["letter"] = new(0.45, 0.035),
            ["settle"] = new(0.9, 0.35),
            ["code"] = new(1.5, 0.3),
            ["cap"] = new(0.18, 0),
            ["karaoke"] = new(0.3, 0),
            ["art"] = new(1.6, 0),
            ["kenburns"] = new(0, 0),
            ["none"] = new(0, 0),
        };

        /// <summary>Motions that split their element's text: into words, into letters, into typed characters.</summary>
        private static readonly HashSet<string> SplitMotions = new(StringComparer.OrdinalIgnoreCase) { "words", "letters", "type" };

        // Authoring attributes (Markdig's generic `{enter=… at=…}`) and what they become.
        private static readonly Dictionary<string, string> AuthoringAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["enter"] = "data-v",
            ["at"] = "data-at",
            ["dur"] = "data-dur",
            ["ease"] = "data-ease",
            ["exit"] = "data-exit",
            ["until"] = "data-until",
            ["delay"] = "data-delay",
            ["gap"] = "data-gap",
            ["stagger"] = "data-stagger",
        };

        private const double TypePerCharacter = 0.035;

        // -----------------------------------------------------------------
        // Pass 1: default motions on the Markdown tree
        // -----------------------------------------------------------------

        /// <summary>
        /// Gives every top-level block of a scene its default motion, unless the author
        /// already chose one with <c>{enter=…}</c>. Containers that lay out flowing
        /// content (<c>cols</c>, <c>col</c>, <c>stack</c>) are walked into, so their
        /// children animate the same way; every other container is a component that
        /// schedules its own parts.
        /// </summary>
        public static void Annotate(ContainerBlock parent) => Annotate(parent, null);

        // How a component wants the blocks inside it to move: `typewriter` types them,
        // `scramble` decodes them, `quote` reads them word by word, `steps` and
        // `checklist` take their items one at a time. Components that read their own
        // content (charts, captions, code, terminal, …) are not walked at all.
        private static readonly HashSet<string> OpaqueContainers = new(StringComparer.OrdinalIgnoreCase)
        {
            "chart", "captions", "code", "terminal", "swap", "counter", "media", "cursor", "lower-third", "logo", "logos", "stat",
        };

        private static void Annotate(ContainerBlock parent, string component)
        {
            var textMotion = component switch
            {
                "typewriter" => "type",
                "scramble" => "scramble",
                "quote" => "words",
                _ => null,
            };
            var itemGap = component switch
            {
                "steps" => "0.36",
                "checklist" => "0.3",
                _ => "0.14",
            };

            foreach (var block in parent)
            {
                switch (block)
                {
                    case HeadingBlock heading:
                        Default(heading, textMotion ?? (heading.Level <= 2 ? "words" : "rise"));
                        break;
                    case ParagraphBlock paragraph:
                        if (textMotion == "words") Default(paragraph, "words", stagger: "0.07");
                        else Default(paragraph, textMotion ?? (IsImageOnly(paragraph) ? "zoom" : "rise"));
                        break;
                    case QuoteBlock quote:
                        Default(quote, "rise");
                        break;
                    case ListBlock list:
                        if (HasEnter(list)) break;
                        foreach (var item in list.OfType<ListItemBlock>())
                        {
                            LiftItemAttributes(item);
                            Default(item, "rise", gap: itemGap);
                        }
                        break;
                    case Table table:
                        if (HasEnter(table)) break;
                        foreach (var row in table.OfType<TableRow>())
                        {
                            Default(row, "rise", gap: "0.12");
                        }
                        break;
                    case FencedCodeBlock code:
                        Default(code, "rise");
                        break;
                    case CustomContainer container when !OpaqueContainers.Contains(container.Info ?? string.Empty):
                        Annotate(container, container.Info);
                        break;
                }
            }
        }

        // `- item {enter=fade}` puts the attributes on the item's paragraph, which a
        // tight list never renders as an element: move them to the item itself.
        private static void LiftItemAttributes(ListItemBlock item)
        {
            if (item.FirstOrDefault() is not ParagraphBlock paragraph) return;
            var props = paragraph.TryGetAttributes()?.Properties;
            if (props == null || !props.Any(p => AuthoringAttributes.ContainsKey(p.Key))) return;
            var target = item.GetAttributes();
            foreach (var prop in props.Where(p => AuthoringAttributes.ContainsKey(p.Key)).ToList())
            {
                target.AddProperty(prop.Key, prop.Value);
                props.Remove(prop);
            }
        }

        private static bool IsImageOnly(ParagraphBlock paragraph)
        {
            var inline = paragraph.Inline;
            if (inline == null) return false;
            var meaningful = inline.Where(i => !(i is Markdig.Syntax.Inlines.LiteralInline lit && lit.Content.IsEmptyOrWhitespace())
                                             && i is not Markdig.Syntax.Inlines.LineBreakInline).ToList();
            return meaningful.Count == 1 && meaningful[0] is Markdig.Syntax.Inlines.LinkInline { IsImage: true };
        }

        private static bool HasEnter(IMarkdownObject block)
        {
            var props = block.TryGetAttributes()?.Properties;
            return props != null && props.Any(p => p.Key is "enter" or "data-v");
        }

        private static void Default(IMarkdownObject block, string motion, string gap = null, string stagger = null)
        {
            if (HasEnter(block)) return;
            var attributes = block.GetAttributes();
            attributes.AddProperty("data-v", motion);
            if (gap != null) attributes.AddProperty("data-gap", gap);
            if (stagger != null) attributes.AddProperty("data-stagger", stagger);
        }

        // -----------------------------------------------------------------
        // Pass 2: times on the rendered HTML
        // -----------------------------------------------------------------

        private static readonly Regex TokenRegex = new(@"<!--.*?-->|<[^>]+>|[^<]+", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex AttrRegex = new(@"([\w:-]+)\s*=\s*(""([^""]*)""|'([^']*)'|([^\s""'>]+))", RegexOptions.Compiled);
        private static readonly Regex TagNameRegex = new(@"^<\s*/?\s*([\w:-]+)", RegexOptions.Compiled);
        private static readonly Regex CharRegex = new(@"&#?\w+;|[\uD800-\uDBFF][\uDC00-\uDFFF]|.", RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr",
        };

        /// <summary>The result of compiling one scene: its HTML with every time resolved, and how long it needs.</summary>
        public sealed record CompiledScene(string Html, double ContentEnd, int AnimatedCount);

        /// <summary>
        /// Resolves every motion in a scene's HTML to scene-local times. Elements are
        /// scheduled in document order on one running cursor: each starts where the
        /// previous one left the cursor (<c>at</c>), and moves the cursor on by its gap.
        /// An explicit <c>at</c> is a time in seconds (<c>at=2.4</c>), relative to the
        /// cursor (<c>at=+0.5</c>), or <c>with</c> — together with the previous element.
        /// </summary>
        public static CompiledScene Compile(string html, double leadIn, double pace)
        {
            if (string.IsNullOrEmpty(html)) return new CompiledScene(html ?? string.Empty, 0, 0);

            var output = new List<string>();
            var cursor = leadIn;
            var previousAt = leadIn;
            var contentEnd = 0.0;
            var animated = 0;

            // Elements whose text is being split: the tag name, its nesting depth, and how to split.
            var splits = new Stack<SplitState>();

            foreach (Match token in TokenRegex.Matches(html))
            {
                var text = token.Value;

                if (text.StartsWith("<!--"))
                {
                    output.Add(text);
                    continue;
                }

                if (text.StartsWith("<"))
                {
                    var nameMatch = TagNameRegex.Match(text);
                    var name = nameMatch.Success ? nameMatch.Groups[1].Value : string.Empty;
                    var isClose = text.StartsWith("</");
                    var selfClosing = text.EndsWith("/>") || VoidElements.Contains(name);

                    if (isClose)
                    {
                        output.Add(text);
                        if (splits.Count > 0 && string.Equals(splits.Peek().Tag, name, StringComparison.OrdinalIgnoreCase))
                        {
                            var split = splits.Peek();
                            if (split.Depth == 0)
                            {
                                splits.Pop();
                                FinishSplit(split, output, ref cursor, ref contentEnd, pace);
                            }
                            else
                            {
                                split.Depth--;
                            }
                        }
                        continue;
                    }

                    if (splits.Count > 0 && !selfClosing && string.Equals(splits.Peek().Tag, name, StringComparison.OrdinalIgnoreCase))
                    {
                        splits.Peek().Depth++;
                    }

                    // Inside <svg>, <pre> and <code> nothing is split; a tag there may still carry a motion.
                    var tag = RewriteAuthoringAttributes(text);
                    var attributes = ParseAttributes(tag);

                    // A highlight (`==text==`) sweeps in by default.
                    if (string.Equals(name, "mark", StringComparison.OrdinalIgnoreCase) && !attributes.ContainsKey("data-v"))
                    {
                        tag = SetAttribute(tag, "data-v", "sweep");
                        attributes["data-v"] = "sweep";
                    }

                    if (!attributes.TryGetValue("data-v", out var motion) || string.IsNullOrEmpty(motion))
                    {
                        output.Add(tag);
                        continue;
                    }

                    motion = motion.Trim().ToLowerInvariant();
                    animated++;

                    if (attributes.TryGetValue("data-delay", out var delayRaw) && VideoOptions.TryDouble(delayRaw, out var delay))
                    {
                        cursor += delay;
                    }

                    // Inside a split element the "cursor" is the word being reached, so a
                    // highlight in a headline lands as its words do.
                    var localCursor = splits.Count > 0
                        ? splits.Peek().Start + splits.Peek().Count * splits.Peek().Stagger + 0.15
                        : cursor;
                    var at = ResolveAt(attributes.GetValueOrDefault("data-at"), motion, localCursor, previousAt);
                    previousAt = at;

                    if (SplitMotions.Contains(motion) && !selfClosing)
                    {
                        // The element itself stays put; its words (or letters, or characters) move.
                        var stagger = attributes.TryGetValue("data-stagger", out var st) && VideoOptions.TryDouble(st, out var s) ? s
                            : motion == "letters" ? Motions["letter"].Gap : Motions["word"].Gap;
                        var state = new SplitState
                        {
                            Tag = name,
                            Mode = motion,
                            Start = at,
                            Stagger = motion == "type" ? TypePerCharacter : stagger * pace,
                            Duration = attributes.TryGetValue("data-dur", out var d) && VideoOptions.TryDouble(d, out var dv) ? dv : -1,
                            Gap = attributes.TryGetValue("data-gap", out var g) && VideoOptions.TryDouble(g, out var gv) ? gv : -1,
                            OpenTagIndex = output.Count,
                        };
                        output.Add(tag);
                        splits.Push(state);
                        cursor = at;
                        continue;
                    }

                    var known = Motions.TryGetValue(motion, out var m) ? m : Motions["rise"];
                    var duration = attributes.TryGetValue("data-dur", out var durRaw) && VideoOptions.TryDouble(durRaw, out var durValue) ? durValue : known.Duration;
                    var gap = attributes.TryGetValue("data-gap", out var gapRaw) && VideoOptions.TryDouble(gapRaw, out var gapValue) ? gapValue : known.Gap * pace;

                    // Backgrounds and camera moves span the scene; they never hold up the content.
                    var isAmbient = motion is "art" or "kenburns";

                    tag = SetAttribute(tag, "data-at", Num(at));
                    tag = SetAttribute(tag, "data-dur", Num(duration));
                    output.Add(tag);

                    // `with` joins the previous element, and a free element (a caption line
                    // on its own clock) lives outside the sequence: neither moves the cursor.
                    var isFree = attributes.ContainsKey("data-free")
                        || string.Equals(attributes.GetValueOrDefault("data-at"), "with", StringComparison.OrdinalIgnoreCase);

                    if (!isAmbient)
                    {
                        // A word inside a split element advances the split, not the cursor.
                        if (splits.Count == 0 && !isFree) cursor = at + gap;
                        contentEnd = Math.Max(contentEnd, at + duration);
                    }
                    continue;
                }

                // Text.
                if (splits.Count > 0 && !string.IsNullOrWhiteSpace(text))
                {
                    output.Add(SplitText(text, splits.Peek(), pace));
                }
                else
                {
                    output.Add(text);
                }
            }

            // An element left open (malformed HTML) still finishes its schedule.
            while (splits.Count > 0)
            {
                FinishSplit(splits.Pop(), output, ref cursor, ref contentEnd, pace);
            }

            return new CompiledScene(string.Concat(output), contentEnd, animated);
        }

        private sealed class SplitState
        {
            public string Tag;
            public string Mode;
            public double Start;
            public double Stagger;
            public double Duration;
            public double Gap;
            public int Depth;
            public int OpenTagIndex;
            public int Count;
        }

        // `at` as written: seconds, `+0.5` from the cursor, `with` alongside the previous element.
        private static double ResolveAt(string raw, string motion, double cursor, double previousAt)
        {
            if (motion is "kenburns") return string.IsNullOrEmpty(raw) || raw == "auto" ? 0 : ParseOr(raw, 0);
            if (motion is "art" && (string.IsNullOrEmpty(raw) || raw == "auto")) return 0;
            if (string.IsNullOrWhiteSpace(raw) || raw == "auto") return cursor;
            raw = raw.Trim();
            if (raw.Equals("with", StringComparison.OrdinalIgnoreCase)) return previousAt;
            if (raw.StartsWith("+") && VideoOptions.TryDouble(raw[1..], out var rel)) return cursor + rel;
            return ParseOr(raw, cursor);
        }

        private static double ParseOr(string raw, double fallback) => VideoOptions.TryDouble(raw, out var v) ? Math.Max(0, v) : fallback;

        private static string SplitText(string text, SplitState split, double pace)
        {
            var sb = new StringBuilder();
            if (split.Mode == "words")
            {
                foreach (Match part in Regex.Matches(text, @"\s+|[^\s]+"))
                {
                    if (string.IsNullOrWhiteSpace(part.Value))
                    {
                        sb.Append(part.Value);
                        continue;
                    }
                    var at = split.Start + split.Count * split.Stagger;
                    sb.Append($"<span class=\"nv-w\" data-v=\"word\" data-at=\"{Num(at)}\" data-dur=\"{Num(Motions["word"].Duration)}\">{part.Value}</span>");
                    split.Count++;
                }
                return sb.ToString();
            }

            foreach (Match ch in CharRegex.Matches(text))
            {
                if (split.Mode == "letters")
                {
                    if (string.IsNullOrWhiteSpace(ch.Value)) { sb.Append(ch.Value); continue; }
                    var at = split.Start + split.Count * split.Stagger;
                    sb.Append($"<span class=\"nv-l\" data-v=\"letter\" data-at=\"{Num(at)}\" data-dur=\"{Num(Motions["letter"].Duration)}\">{ch.Value}</span>");
                }
                else
                {
                    // Typed characters carry no times of their own: the element reveals
                    // the first n of them, n growing linearly over its duration.
                    sb.Append($"<span class=\"nv-c\">{ch.Value}</span>");
                }
                split.Count++;
            }
            return sb.ToString();
        }

        private static void FinishSplit(SplitState split, List<string> output, ref double cursor, ref double contentEnd, double pace)
        {
            var open = output[split.OpenTagIndex];
            double end;
            double gap;
            if (split.Mode == "type")
            {
                var duration = split.Duration > 0 ? split.Duration : Math.Max(0.4, split.Count * TypePerCharacter);
                open = SetAttribute(open, "data-at", Num(split.Start));
                open = SetAttribute(open, "data-dur", Num(duration));
                end = split.Start + duration;
                gap = split.Gap >= 0 ? split.Gap : duration + 0.25 * pace;
            }
            else
            {
                var last = split.Start + Math.Max(0, split.Count - 1) * split.Stagger;
                var unit = split.Mode == "letters" ? Motions["letter"].Duration : Motions["word"].Duration;
                open = SetAttribute(open, "data-at", Num(split.Start));
                open = SetAttribute(open, "data-dur", Num(last - split.Start + unit));
                end = last + unit;
                gap = split.Gap >= 0 ? split.Gap : (last - split.Start) + 0.32 * pace;
            }
            open = SetAttribute(open, "data-split", split.Mode);
            output[split.OpenTagIndex] = open;
            cursor = split.Start + gap;
            contentEnd = Math.Max(contentEnd, end);
        }

        /// <summary>Turns <c>enter="…"</c>, <c>at="…"</c> and friends from Markdig's generic attributes into the runtime's <c>data-*</c> names.</summary>
        private static string RewriteAuthoringAttributes(string tag)
        {
            if (!tag.Contains('=')) return tag;
            return AttrRegex.Replace(tag, m =>
            {
                var key = m.Groups[1].Value;
                return AuthoringAttributes.TryGetValue(key, out var mapped) ? mapped + "=" + m.Groups[2].Value : m.Value;
            });
        }

        private static Dictionary<string, string> ParseAttributes(string tag)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in AttrRegex.Matches(tag))
            {
                var value = m.Groups[3].Success ? m.Groups[3].Value : m.Groups[4].Success ? m.Groups[4].Value : m.Groups[5].Value;
                result[m.Groups[1].Value] = System.Net.WebUtility.HtmlDecode(value);
            }
            return result;
        }

        internal static string SetAttribute(string tag, string name, string value)
        {
            var pattern = new Regex($@"\s{Regex.Escape(name)}\s*=\s*(""[^""]*""|'[^']*'|[^\s""'>]+)", RegexOptions.IgnoreCase);
            var encoded = System.Net.WebUtility.HtmlEncode(value);
            if (pattern.IsMatch(tag)) return pattern.Replace(tag, $" {name}=\"{encoded}\"", 1);
            var close = tag.EndsWith("/>") ? tag.Length - 2 : tag.Length - 1;
            return tag.Substring(0, close).TrimEnd() + $" {name}=\"{encoded}\"" + tag.Substring(close);
        }

        internal static string Num(double value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);

        // -----------------------------------------------------------------
        // Pass 3: the scenes, end to end
        // -----------------------------------------------------------------

        /// <summary>
        /// Lays the scenes on one timeline. A scene starts where the previous one ends
        /// minus the transition between them, so a 0.6 s push overlaps the outgoing and
        /// incoming scene by 0.6 s; a cut overlaps nothing. Returns the video's length.
        /// </summary>
        public static double Sequence(IList<VideoScene> scenes)
        {
            var time = 0.0;
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                if (i == 0)
                {
                    scene.Start = 0;
                }
                else
                {
                    var overlap = Math.Min(scene.TransitionDuration, Math.Min(scenes[i - 1].Duration, scene.Duration) * 0.5);
                    scene.TransitionDuration = overlap;
                    scene.Start = Math.Max(0, time - overlap);
                }
                time = scene.Start + scene.Duration;
            }
            return Math.Round(time, 3);
        }

        /// <summary>The transition into a scene: its own <c>transition</c>, or the video's default. The first scene has none.</summary>
        public static (string Name, double Duration) ResolveTransition(VideoScene scene, VideoOptions options, int index)
        {
            if (index == 0) return ("cut", 0);
            var name = (scene.Get("transition") ?? options.DefaultTransition).Trim().ToLowerInvariant();
            if (!VideoOptions.Transitions.Contains(name)) name = options.DefaultTransition;
            if (name == "cut") return ("cut", 0);
            var duration = VideoOptions.TryDouble(scene.Get("transition-duration") ?? scene.Get("tdur"), out var d) ? Math.Max(0.1, d) : options.TransitionDuration;
            return (name, duration);
        }
    }
}
