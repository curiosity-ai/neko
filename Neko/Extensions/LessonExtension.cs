using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Neko.Extensions
{
    public class LessonInline : Inline
    {
        public Dictionary<string, string> Attributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class LessonParser : InlineParser
    {
        public LessonParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            if (slice.CurrentChar != '[') return false;
            slice.NextChar();
            if (slice.CurrentChar != '!') { slice = saved; return false; }
            slice.NextChar();

            if (!slice.Match("lesson")) { slice = saved; return false; }
            slice.Start += "lesson".Length;

            if (slice.CurrentChar != ']' && !slice.CurrentChar.IsWhitespace()) { slice = saved; return false; }

            var lesson = new LessonInline();

            while (slice.CurrentChar != ']' && !slice.IsEmpty)
            {
                while (slice.CurrentChar.IsWhitespace()) slice.NextChar();
                if (slice.CurrentChar == ']') break;

                var keyStart = slice.Start;
                while (slice.CurrentChar != '=' && slice.CurrentChar != ']' && !slice.CurrentChar.IsWhitespace() && !slice.IsEmpty)
                {
                    slice.NextChar();
                }
                var key = slice.Text.Substring(keyStart, slice.Start - keyStart).Trim();
                if (string.IsNullOrEmpty(key)) break;

                if (slice.CurrentChar == '=')
                {
                    slice.NextChar();
                    string val;
                    if (slice.CurrentChar == '"')
                    {
                        slice.NextChar();
                        var valStart = slice.Start;
                        while (slice.CurrentChar != '"' && !slice.IsEmpty) slice.NextChar();
                        val = slice.Text.Substring(valStart, slice.Start - valStart);
                        if (slice.CurrentChar == '"') slice.NextChar();
                    }
                    else
                    {
                        var valStart = slice.Start;
                        while (slice.CurrentChar != ']' && !slice.CurrentChar.IsWhitespace() && !slice.IsEmpty) slice.NextChar();
                        val = slice.Text.Substring(valStart, slice.Start - valStart);
                    }
                    lesson.Attributes[key] = val;
                }
                else
                {
                    // Boolean-style flag
                    lesson.Attributes[key] = "true";
                }
            }

            if (slice.CurrentChar != ']') { slice = saved; return false; }
            slice.NextChar();

            processor.Inline = lesson;
            return true;
        }
    }

    public class LessonRenderer : HtmlObjectRenderer<LessonInline>
    {
        protected override void Write(HtmlRenderer renderer, LessonInline obj)
        {
            var filePath = LessonExtension.CurrentFilePath;
            var rootDir = LessonExtension.CurrentRootDirectory;

            var attrs = obj.Attributes;
            string Get(string key, string fallback = "") => attrs.TryGetValue(key, out var v) ? v : fallback;

            var title = Get("title");
            var description = Get("description");
            var badgeTitle = Get("badge");
            var prerequisites = Get("prerequisites");
            var upNextTitle = Get("up-next-title");
            var upNextLink = Get("up-next-link");
            var upNextSummary = Get("up-next-summary");

            // Discover steps from sibling markdown files in the same directory.
            var steps = LessonExtension.DiscoverSteps(filePath, rootDir);

            // Storage id: derived from the current page URL.
            var storageId = LessonExtension.GetStorageId(filePath, rootDir);

            // Compute fallback title from frontmatter? We only have file path here. Title from parent doc isn't available; use folder name as fallback.
            if (string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(filePath))
            {
                var dir = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
                title = string.IsNullOrEmpty(dir) ? "Lesson" : dir;
            }

            renderer.Write("<div class=\"not-prose my-8 grid grid-cols-1 lg:grid-cols-[1fr_320px] gap-8\" data-neko-lesson=\"");
            renderer.Write(WebUtility.HtmlEncode(storageId));
            renderer.Write("\" data-step-count=\"");
            renderer.Write(steps.Count.ToString());
            renderer.Write("\">");

            // Main column
            renderer.Write("<div>");

            // Breadcrumb-style heading
            renderer.Write("<div class=\"text-xs font-mono tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-2\">Tracks → ");
            renderer.Write(WebUtility.HtmlEncode(title));
            renderer.Write("</div>");

            // Title block
            renderer.Write("<div class=\"flex items-start gap-4 mb-4\">");
            renderer.Write("<div class=\"flex h-14 w-14 items-center justify-center rounded-2xl bg-primary-500/15 ring-1 ring-primary-500/30 text-primary-400\"><i class=\"fi fi-rr-book-alt text-2xl\"></i></div>");
            renderer.Write("<div><h2 class=\"text-3xl font-bold tracking-tight text-gray-900 dark:text-white mt-0\">");
            renderer.Write(WebUtility.HtmlEncode(title));
            renderer.Write("</h2>");
            if (!string.IsNullOrEmpty(description))
            {
                renderer.Write("<p class=\"mt-2 text-gray-600 dark:text-gray-300 max-w-2xl\">");
                renderer.Write(WebUtility.HtmlEncode(description));
                renderer.Write("</p>");
            }
            renderer.Write("</div></div>");

            // Action buttons
            renderer.Write("<div class=\"flex items-center gap-3 mt-6\">");
            renderer.Write("<button type=\"button\" data-neko-lesson-continue class=\"inline-flex items-center rounded-md bg-primary-600 hover:bg-primary-500 px-4 py-2 text-sm font-semibold text-white transition-colors\">Continue →</button>");
            renderer.Write("<button type=\"button\" data-neko-lesson-reset class=\"inline-flex items-center rounded-md px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-200 ring-1 ring-gray-200 dark:ring-gray-700 hover:ring-gray-300 dark:hover:ring-gray-500 transition-colors\">Start over</button>");
            renderer.Write("</div>");

            // Progress bar
            renderer.Write("<div class=\"mt-8 rounded-xl border border-gray-200 dark:border-gray-700 p-4\">");
            renderer.Write("<div class=\"flex items-center justify-between mb-2\">");
            renderer.Write("<span class=\"text-sm text-gray-600 dark:text-gray-300\">Your progress</span>");
            renderer.Write("<span class=\"text-sm font-medium text-gray-900 dark:text-gray-100\"><span data-neko-lesson-completed>0</span> / ");
            renderer.Write(steps.Count.ToString());
            renderer.Write(" lessons</span>");
            renderer.Write("</div>");
            renderer.Write("<div class=\"h-2 w-full overflow-hidden rounded-full bg-gray-100 dark:bg-gray-800\">");
            renderer.Write("<div data-neko-lesson-bar class=\"h-full bg-gradient-to-r from-primary-500 to-accent-500 transition-[width] duration-300\" style=\"width: 0%;\"></div>");
            renderer.Write("</div>");
            renderer.Write("</div>");

            // Curriculum list
            renderer.Write("<div class=\"mt-8\">");
            renderer.Write("<h3 class=\"text-xs font-mono tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-3\">Curriculum</h3>");
            renderer.Write("<ol class=\"divide-y divide-gray-100 dark:divide-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden\">");

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                renderer.Write("<li class=\"group flex items-center gap-4 px-4 py-3 hover:bg-gray-50 dark:hover:bg-gray-800/40 transition-colors\" data-neko-lesson-step=\"");
                renderer.Write(WebUtility.HtmlEncode(step.Slug));
                renderer.Write("\">");

                // Toggle / status indicator
                renderer.Write("<button type=\"button\" data-neko-lesson-toggle aria-label=\"Mark complete\" class=\"flex h-7 w-7 shrink-0 items-center justify-center rounded-full border border-gray-300 dark:border-gray-600 text-transparent hover:border-primary-500 hover:text-primary-500 transition-colors\">");
                renderer.Write("<i class=\"fi fi-rr-check text-sm\"></i>");
                renderer.Write("</button>");

                // Index
                renderer.Write($"<span class=\"font-mono text-xs text-gray-500 dark:text-gray-400 w-6 shrink-0\">{(i + 1).ToString("D2")}</span>");

                // Title (linked)
                renderer.Write("<a href=\"");
                renderer.Write(WebUtility.HtmlEncode(step.Url));
                renderer.Write("\" data-neko-lesson-link class=\"flex-1 text-sm font-medium text-gray-900 dark:text-gray-100 hover:text-primary-600 dark:hover:text-primary-400 no-underline\">");
                renderer.Write(WebUtility.HtmlEncode(step.Title));
                renderer.Write("</a>");

                // Kind badge
                if (!string.IsNullOrEmpty(step.Kind))
                {
                    var (bg, text) = KindStyle(step.Kind);
                    renderer.Write($"<span class=\"hidden sm:inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium {bg} {text}\">");
                    renderer.Write(WebUtility.HtmlEncode(step.Kind));
                    renderer.Write("</span>");
                }

                // Duration
                if (!string.IsNullOrEmpty(step.Duration))
                {
                    renderer.Write("<span class=\"hidden sm:inline text-xs font-mono text-gray-500 dark:text-gray-400 w-16 text-right\">");
                    renderer.Write(WebUtility.HtmlEncode(step.Duration));
                    renderer.Write("</span>");
                }

                renderer.Write("</li>");
            }

            renderer.Write("</ol>");
            renderer.Write("</div>"); // End curriculum

            renderer.Write("</div>"); // End main column

            // Sidebar
            renderer.Write("<aside class=\"flex flex-col gap-4\">");

            if (!string.IsNullOrEmpty(badgeTitle))
            {
                renderer.Write("<div class=\"rounded-xl border border-gray-200 dark:border-gray-700 p-5\">");
                renderer.Write("<div class=\"text-xs font-mono tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-3\">Earn the badge</div>");
                renderer.Write("<div class=\"flex items-center gap-3\">");
                renderer.Write("<div class=\"h-12 w-12 rounded-full bg-gradient-to-br from-primary-500 to-accent-500 flex items-center justify-center text-white\"><i class=\"fi fi-rr-star text-lg\"></i></div>");
                renderer.Write("<div><div class=\"text-sm font-semibold text-gray-900 dark:text-white\">");
                renderer.Write(WebUtility.HtmlEncode(badgeTitle));
                renderer.Write("</div><div class=\"text-xs text-gray-500 dark:text-gray-400\">Add to your profile</div></div>");
                renderer.Write("</div></div>");
            }

            if (!string.IsNullOrEmpty(prerequisites))
            {
                renderer.Write("<div class=\"rounded-xl border border-gray-200 dark:border-gray-700 p-5\">");
                renderer.Write("<div class=\"text-xs font-mono tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-3\">Prerequisites</div>");
                renderer.Write("<ul class=\"space-y-2 text-sm text-gray-700 dark:text-gray-300\">");
                foreach (var pre in prerequisites.Split('|', StringSplitOptions.RemoveEmptyEntries))
                {
                    renderer.Write("<li class=\"flex items-start gap-2\"><i class=\"fi fi-rr-check text-emerald-500 mt-0.5\"></i><span>");
                    renderer.Write(WebUtility.HtmlEncode(pre.Trim()));
                    renderer.Write("</span></li>");
                }
                renderer.Write("</ul></div>");
            }

            if (!string.IsNullOrEmpty(upNextTitle))
            {
                renderer.Write("<div class=\"rounded-xl border border-gray-200 dark:border-gray-700 p-5\">");
                renderer.Write("<div class=\"text-xs font-mono tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-2\">Up next</div>");
                renderer.Write("<div class=\"text-sm font-semibold text-gray-900 dark:text-white\">");
                renderer.Write(WebUtility.HtmlEncode(upNextTitle));
                renderer.Write("</div>");
                if (!string.IsNullOrEmpty(upNextSummary))
                {
                    renderer.Write("<div class=\"text-xs text-gray-500 dark:text-gray-400 mt-1\">");
                    renderer.Write(WebUtility.HtmlEncode(upNextSummary));
                    renderer.Write("</div>");
                }
                if (!string.IsNullOrEmpty(upNextLink))
                {
                    renderer.Write("<a href=\"");
                    renderer.Write(WebUtility.HtmlEncode(upNextLink));
                    renderer.Write("\" class=\"mt-3 inline-flex items-center text-sm font-medium text-primary-600 dark:text-primary-400 no-underline\">Preview track →</a>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("</aside>"); // End sidebar

            renderer.Write("</div>"); // End grid

            // Inline progress script (scoped via data-neko-lesson id)
            renderer.Write("<script>");
            renderer.Write(LessonExtension.GetClientScript());
            renderer.Write("</script>");
        }

        private static (string Bg, string Text) KindStyle(string kind)
        {
            return kind.ToLowerInvariant() switch
            {
                "reading"     => ("bg-gray-100 dark:bg-gray-800", "text-gray-700 dark:text-gray-300"),
                "interactive" => ("bg-primary-500/15", "text-primary-600 dark:text-primary-300"),
                "project"     => ("bg-amber-500/15", "text-amber-700 dark:text-amber-300"),
                "video"       => ("bg-rose-500/15", "text-rose-700 dark:text-rose-300"),
                _              => ("bg-gray-100 dark:bg-gray-800", "text-gray-700 dark:text-gray-300"),
            };
        }
    }

    public class LessonExtension : IMarkdownExtension
    {
        // Set per-document by MarkdownParser.Parse so renderers can resolve sibling files.
        public static string CurrentFilePath;
        public static string CurrentRootDirectory;

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<LessonParser>())
            {
                // Insert before generic ComponentParser so [!lesson] wins.
                pipeline.InlineParsers.Insert(0, new LessonParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                htmlRenderer.ObjectRenderers.AddIfNotAlready<LessonRenderer>();
            }
        }

        public class Step
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public string Kind { get; set; }
            public string Duration { get; set; }
            public string Slug { get; set; }
        }

        public static List<Step> DiscoverSteps(string filePath, string rootDirectory)
        {
            var steps = new List<Step>();
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return steps;

            var dir = Path.GetDirectoryName(filePath);
            if (dir == null || !Directory.Exists(dir)) return steps;

            string currentFileName = Path.GetFileName(filePath);

            var entries = new List<(string Path, int Order, string Title, string Kind, string Duration, string Slug)>();

            foreach (var f in Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(f);
                if (string.Equals(name, currentFileName, StringComparison.OrdinalIgnoreCase)) continue;

                var meta = ReadFrontMatter(f);
                var title = meta.Title ?? Path.GetFileNameWithoutExtension(f);
                var kind = meta.Kind;
                var duration = meta.Duration;
                var order = meta.Order ?? int.MaxValue;

                var rel = !string.IsNullOrEmpty(rootDirectory)
                    ? Path.GetRelativePath(rootDirectory, f).Replace('\\', '/')
                    : Path.GetFileName(f);
                var url = "/" + rel;
                if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
                if (!string.IsNullOrEmpty(Neko.Builder.SiteBuilder.CurrentRoutePrefix))
                {
                    url = Neko.Builder.SiteBuilder.CurrentRoutePrefix + url;
                }

                var slug = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();

                entries.Add((f, order, title, kind, duration, slug));
            }

            return entries
                .OrderBy(e => e.Order)
                .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
                .Select(e => new Step
                {
                    Title = e.Title,
                    Url = ResolveUrlFromPath(e.Path, rootDirectory),
                    Kind = e.Kind,
                    Duration = e.Duration,
                    Slug = e.Slug
                })
                .ToList();
        }

        private static string ResolveUrlFromPath(string filePath, string rootDirectory)
        {
            var rel = !string.IsNullOrEmpty(rootDirectory)
                ? Path.GetRelativePath(rootDirectory, filePath).Replace('\\', '/')
                : Path.GetFileName(filePath);
            var url = "/" + rel;
            if (url.EndsWith(".md")) url = url.Substring(0, url.Length - 3);
            if (!string.IsNullOrEmpty(Neko.Builder.SiteBuilder.CurrentRoutePrefix))
            {
                url = Neko.Builder.SiteBuilder.CurrentRoutePrefix + url;
            }
            return url;
        }

        private class LessonFrontMatter
        {
            [YamlMember(Alias = "title")] public string Title { get; set; }
            [YamlMember(Alias = "order")] public int? Order { get; set; }
            [YamlMember(Alias = "kind")] public string Kind { get; set; }
            [YamlMember(Alias = "duration")] public string Duration { get; set; }
        }

        private static LessonFrontMatter ReadFrontMatter(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                if (!content.StartsWith("---")) return new LessonFrontMatter();
                int end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
                if (end < 0) return new LessonFrontMatter();
                var yaml = content.Substring(3, end - 3).Trim();
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                return deserializer.Deserialize<LessonFrontMatter>(yaml) ?? new LessonFrontMatter();
            }
            catch
            {
                return new LessonFrontMatter();
            }
        }

        public static string GetStorageId(string filePath, string rootDirectory)
        {
            if (string.IsNullOrEmpty(filePath)) return "lesson";
            var rel = !string.IsNullOrEmpty(rootDirectory)
                ? Path.GetRelativePath(rootDirectory, filePath).Replace('\\', '/')
                : Path.GetFileName(filePath);
            if (rel.EndsWith(".md")) rel = rel.Substring(0, rel.Length - 3);
            return rel.ToLowerInvariant();
        }

        public static string GetClientScript()
        {
            return @"(function(){
  if (window.__nekoLessonInit) return; window.__nekoLessonInit = true;
  function key(id){ return 'neko-lesson:' + id; }
  function load(id){ try { return JSON.parse(localStorage.getItem(key(id))) || {}; } catch(e){ return {}; } }
  function save(id, state){ try { localStorage.setItem(key(id), JSON.stringify(state)); } catch(e){} }
  function hydrate(root){
    var id = root.getAttribute('data-neko-lesson');
    var total = parseInt(root.getAttribute('data-step-count') || '0', 10);
    var state = load(id);
    var steps = root.querySelectorAll('[data-neko-lesson-step]');
    var done = 0;
    steps.forEach(function(li){
      var slug = li.getAttribute('data-neko-lesson-step');
      var toggle = li.querySelector('[data-neko-lesson-toggle]');
      var setDone = function(v){
        if (v){
          li.classList.add('opacity-80');
          toggle.classList.remove('border-gray-300','dark:border-gray-600','text-transparent');
          toggle.classList.add('bg-emerald-500','border-emerald-500','text-white');
        } else {
          li.classList.remove('opacity-80');
          toggle.classList.add('border-gray-300','dark:border-gray-600','text-transparent');
          toggle.classList.remove('bg-emerald-500','border-emerald-500','text-white');
        }
      };
      setDone(!!state[slug]);
      if (state[slug]) done++;
      toggle.addEventListener('click', function(e){
        e.preventDefault();
        state[slug] = !state[slug];
        save(id, state);
        if (state[slug]) done++; else done--;
        setDone(!!state[slug]);
        update();
      });
    });
    function update(){
      var pct = total > 0 ? Math.round((done/total)*100) : 0;
      var bar = root.querySelector('[data-neko-lesson-bar]');
      var num = root.querySelector('[data-neko-lesson-completed]');
      if (bar) bar.style.width = pct + '%';
      if (num) num.textContent = done;
    }
    update();
    var cont = root.querySelector('[data-neko-lesson-continue]');
    if (cont) cont.addEventListener('click', function(){
      var next = null;
      steps.forEach(function(li){
        if (next) return;
        var s = li.getAttribute('data-neko-lesson-step');
        if (!state[s]){ next = li.querySelector('[data-neko-lesson-link]'); }
      });
      if (!next && steps.length){ next = steps[0].querySelector('[data-neko-lesson-link]'); }
      if (next) window.location.href = next.getAttribute('href');
    });
    var reset = root.querySelector('[data-neko-lesson-reset]');
    if (reset) reset.addEventListener('click', function(){
      if (!confirm('Reset progress for this lesson?')) return;
      state = {}; save(id, state); done = 0;
      steps.forEach(function(li){
        var toggle = li.querySelector('[data-neko-lesson-toggle]');
        li.classList.remove('opacity-80');
        toggle.classList.add('border-gray-300','dark:border-gray-600','text-transparent');
        toggle.classList.remove('bg-emerald-500','border-emerald-500','text-white');
      });
      update();
    });
  }
  function init(){ document.querySelectorAll('[data-neko-lesson]').forEach(hydrate); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();";
        }
    }
}
