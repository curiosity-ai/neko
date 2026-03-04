using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;

namespace Neko.Extensions
{
    public class ComponentInline : Inline
    {
        public string Name { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<string> Arguments { get; set; } = new List<string>();

        public string GetAttribute(string key, string defaultValue = "")
        {
            return Attributes.TryGetValue(key.ToLower(), out var value) ? value : defaultValue;
        }
    }

    public class ComponentParser : InlineParser
    {
        public ComponentParser()
        {
            OpeningCharacters = new[] { '[' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var saved = slice;

            // Check for starting [
            if (slice.CurrentChar != '[') return false;
            slice.NextChar();

            // Check for !
            if (slice.CurrentChar != '!')
            {
                 slice = saved;
                 return false;
            }
            slice.NextChar();

            // Parse name (e.g. button, icon, etc.)
            // Name should be alphanumeric + dashes
            var nameStart = slice.Start;
            while (slice.CurrentChar.IsAlphaNumeric() || slice.CurrentChar == '-')
            {
                slice.NextChar();
            }
            var nameLength = slice.Start - nameStart;
            if (nameLength == 0)
            {
                slice = saved;
                return false;
            }

            var name = slice.Text.Substring(nameStart, nameLength).ToLower();

            // Avoid conflict with GitHub Alerts (which might be parsed as ComponentInline if we don't skip them)
            // GitHub alerts use > [!NOTE], which Markdig post-processes. If we parse [!NOTE] as a component,
            // Markdig's alert detection logic (likely inspecting inlines) fails.
            if (name == "note" || name == "tip" || name == "important" || name == "warning" || name == "caution")
            {
                slice = saved;
                return false;
            }

            // Avoid conflict with BadgeParser
            if (name == "badge")
            {
                slice = saved;
                return false;
            }

            // Skip space if any
            while (slice.CurrentChar.IsWhitespace())
            {
                slice.NextChar();
            }

            var component = new ComponentInline { Name = name };

            // Parse attributes loop
            while (slice.CurrentChar != ']')
            {
                if (slice.IsEmpty)
                {
                    slice = saved;
                    return false;
                }

                if (slice.CurrentChar.IsWhitespace())
                {
                    slice.NextChar();
                    continue;
                }

                // Check if current token starts with quote (positional quoted arg)
                if (slice.CurrentChar == '"')
                {
                    slice.NextChar(); // Skip "
                    var valStart = slice.Start;
                    while (slice.CurrentChar != '"' && !slice.IsEmpty)
                    {
                        slice.NextChar();
                    }

                    if (slice.CurrentChar != '"') break; // Should end with "

                    var val = slice.Text.Substring(valStart, slice.Start - valStart);
                    slice.NextChar(); // Skip closing "

                    component.Arguments.Add(val);
                    continue;
                }

                // Parse key="value" or positional value
                var start = slice.Start;
                while (slice.CurrentChar != '=' && slice.CurrentChar != ']' && !slice.IsEmpty && !slice.CurrentChar.IsWhitespace())
                {
                    slice.NextChar();
                }

                if (slice.CurrentChar == '=')
                {
                    var key = slice.Text.Substring(start, slice.Start - start).Trim();
                    slice.NextChar(); // Skip =

                    // Parse value
                    if (slice.CurrentChar == '"')
                    {
                        slice.NextChar(); // Skip "
                        var valStart = slice.Start;
                        while (slice.CurrentChar != '"' && !slice.IsEmpty)
                        {
                            slice.NextChar(); // Just advance
                        }

                        if (slice.CurrentChar != '"') break; // Should end with "

                        var val = slice.Text.Substring(valStart, slice.Start - valStart);
                        slice.NextChar(); // Skip closing "

                        component.Attributes[key.ToLower()] = val;
                    }
                    else
                    {
                        // Unquoted value until space or ]
                        var valStart = slice.Start;
                        while (slice.CurrentChar != ']' && !slice.CurrentChar.IsWhitespace() && !slice.IsEmpty)
                        {
                            slice.NextChar();
                        }
                        var val = slice.Text.Substring(valStart, slice.Start - valStart);
                        component.Attributes[key.ToLower()] = val;
                    }
                }
                else
                {
                    // Positional argument
                    var val = slice.Text.Substring(start, slice.Start - start).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        component.Arguments.Add(val);
                    }

                    if (slice.CurrentChar == ']') break;
                }
            }

            if (slice.CurrentChar == ']')
            {
                slice.NextChar();

                // Check for optional link in parenthesis (url)
                if (slice.CurrentChar == '(')
                {
                    if (LinkHelper.TryParseInlineLink(ref slice, out var link, out var title))
                    {
                        if (!string.IsNullOrEmpty(link))
                        {
                            component.Attributes["link"] = link;
                        }
                        if (!string.IsNullOrEmpty(title))
                        {
                            component.Attributes["title"] = title;
                        }
                    }
                }

                processor.Inline = component;
                return true;
            }

            slice = saved;
            return false;
        }
    }

    public class ComponentRenderer : HtmlObjectRenderer<ComponentInline>
    {
        private readonly MarkdownPipeline _pipeline;

        public ComponentRenderer(MarkdownPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        protected override void Write(HtmlRenderer renderer, ComponentInline obj)
        {
            switch (obj.Name)
            {
                case "command-example":
                    RenderCommandExample(renderer, obj);
                    break;
                case "button":
                    RenderButton(renderer, obj);
                    break;
                case "color-chip":
                    RenderColorChip(renderer, obj);
                    break;
                case "embed":
                    RenderEmbed(renderer, obj);
                    break;
                case "file":
                    RenderFile(renderer, obj);
                    break;
                case "icon":
                    RenderIcon(renderer, obj);
                    break;
                case "ref":
                    RenderRef(renderer, obj);
                    break;
                case "youtube":
                    RenderYouTube(renderer, obj);
                    break;
                case "emoji-table":
                    RenderEmojiTable(renderer, obj);
                    break;
                case "hero":
                    RenderHero(renderer, obj);
                    break;
                case "feature-grid":
                    RenderFeatureGrid(renderer, obj);
                    break;
                case "cta-panel":
                    RenderCtaPanel(renderer, obj);
                    break;
                case "bento-grid":
                    RenderBentoGrid(renderer, obj);
                    break;
                case "pricing-tiers":
                    RenderPricingTiers(renderer, obj);
                    break;
                case "header-stats":
                    RenderHeaderStats(renderer, obj);
                    break;
                case "newsletter-side":
                    RenderNewsletterSide(renderer, obj);
                    break;
                case "stats-simple":
                    RenderStatsSimple(renderer, obj);
                    break;
                case "hero-simple":
                    RenderHeroSimple(renderer, obj);
                    break;
                case "blog-column":
                    RenderBlogColumn(renderer, obj);
                    break;
                case "content-sticky":
                    RenderContentSticky(renderer, obj);
                    break;
                case "logo-cloud":
                    RenderLogoCloud(renderer, obj);
                    break;
                default:
                    // Fallback or ignore
                    renderer.Write($"<span class=\"text-red-500\">Unknown component: {obj.Name}</span>");
                    break;
            }
        }

        private void RenderCommandExample(HtmlRenderer renderer, ComponentInline obj)
        {
            var install = obj.GetAttribute("install");
            var quickstart = obj.GetAttribute("quickstart");

            renderer.Write("<div class=\"grid grid-cols-1 md:grid-cols-2 gap-6 my-8 not-prose\">");

            // Left Box: INSTALL
            renderer.Write("<div class=\"flex flex-col\">");
            renderer.Write("<div class=\"text-xs tracking-widest text-gray-500 dark:text-gray-400 font-mono uppercase mb-3 text-center md:text-left md:ml-4\">INSTALL</div>");
            renderer.Write("<div class=\"group relative flex items-center justify-between rounded-lg border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-[#111111] px-4 py-4 md:px-6 md:py-5 hover:border-gray-300 dark:hover:border-gray-600 transition-colors duration-200\">");
            renderer.Write($"<code class=\"text-sm md:text-base text-gray-800 dark:text-gray-200 font-mono bg-transparent border-0 p-0\">{install}</code>");
            renderer.Write($"<button class=\"copy-button text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors ml-4 focus:outline-none\" onclick=\"const btn = this; navigator.clipboard.writeText('{install}'); btn.innerHTML = '<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;20&quot; height=&quot;20&quot; viewBox=&quot;0 0 24 24&quot; fill=&quot;none&quot; stroke=&quot;currentColor&quot; stroke-width=&quot;2&quot; stroke-linecap=&quot;round&quot; stroke-linejoin=&quot;round&quot;><polyline points=&quot;20 6 9 17 4 12&quot;></polyline></svg>'; setTimeout(() => btn.innerHTML = '<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;20&quot; height=&quot;20&quot; viewBox=&quot;0 0 24 24&quot; fill=&quot;none&quot; stroke=&quot;currentColor&quot; stroke-width=&quot;2&quot; stroke-linecap=&quot;round&quot; stroke-linejoin=&quot;round&quot;><rect x=&quot;9&quot; y=&quot;9&quot; width=&quot;13&quot; height=&quot;13&quot; rx=&quot;2&quot; ry=&quot;2&quot;></rect><path d=&quot;M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1&quot;></path></svg>', 2000);\" title=\"Copy to clipboard\">");
            renderer.Write("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"9\" y=\"9\" width=\"13\" height=\"13\" rx=\"2\" ry=\"2\"></rect><path d=\"M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1\"></path></svg>");
            renderer.Write("</button>");
            renderer.Write("</div>");
            renderer.Write("</div>");

            // Right Box: GIVE YOUR LLM
            renderer.Write("<div class=\"flex flex-col\">");
            renderer.Write("<div class=\"text-xs tracking-widest text-gray-500 dark:text-gray-400 font-mono uppercase mb-3 text-center md:text-left md:ml-4\">GIVE YOUR LLM</div>");
            renderer.Write("<div class=\"group relative flex items-center justify-between rounded-lg border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-[#111111] px-4 py-4 md:px-6 md:py-5 hover:border-primary-500 hover:ring-1 hover:ring-primary-500 dark:hover:border-primary-500 transition-all duration-200\">");
            renderer.Write($"<code class=\"text-sm md:text-base text-gray-800 dark:text-gray-200 font-mono bg-transparent border-0 p-0\">{quickstart}</code>");
            renderer.Write($"<button class=\"copy-button text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors ml-4 focus:outline-none\" onclick=\"const btn = this; navigator.clipboard.writeText('{quickstart}'); btn.innerHTML = '<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;20&quot; height=&quot;20&quot; viewBox=&quot;0 0 24 24&quot; fill=&quot;none&quot; stroke=&quot;currentColor&quot; stroke-width=&quot;2&quot; stroke-linecap=&quot;round&quot; stroke-linejoin=&quot;round&quot;><polyline points=&quot;20 6 9 17 4 12&quot;></polyline></svg>'; setTimeout(() => btn.innerHTML = '<svg xmlns=&quot;http://www.w3.org/2000/svg&quot; width=&quot;20&quot; height=&quot;20&quot; viewBox=&quot;0 0 24 24&quot; fill=&quot;none&quot; stroke=&quot;currentColor&quot; stroke-width=&quot;2&quot; stroke-linecap=&quot;round&quot; stroke-linejoin=&quot;round&quot;><rect x=&quot;9&quot; y=&quot;9&quot; width=&quot;13&quot; height=&quot;13&quot; rx=&quot;2&quot; ry=&quot;2&quot;></rect><path d=&quot;M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1&quot;></path></svg>', 2000);\" title=\"Copy to clipboard\">");
            renderer.Write("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"20\" height=\"20\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"9\" y=\"9\" width=\"13\" height=\"13\" rx=\"2\" ry=\"2\"></rect><path d=\"M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1\"></path></svg>");
            renderer.Write("</button>");
            renderer.Write("</div>");
            renderer.Write("</div>");

            renderer.Write("</div>");
        }

        private void RenderFeatureGrid(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto max-w-2xl lg:text-center\">");

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h2 class=\"text-base font-semibold leading-7 text-indigo-600 dark:text-indigo-400\">Deploy faster</h2>"); // Hardcoded generic label or omit?
                renderer.Write("<p class=\"mt-2 text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
                renderer.WriteEscape(title);
                renderer.Write("</p>");
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                renderer.Write("<p class=\"mt-6 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
                renderer.WriteEscape(subtitle);
                renderer.Write("</p>");
            }
            renderer.Write("</div>");

            renderer.Write("<div class=\"mx-auto mt-16 max-w-2xl sm:mt-20 lg:mt-24 lg:max-w-4xl\">");
            renderer.Write("<dl class=\"grid max-w-xl grid-cols-1 gap-x-8 gap-y-10 lg:max-w-none lg:grid-cols-2 lg:gap-y-16\">");

            for (int i = 0; i < obj.Arguments.Count; i += 3)
            {
                if (i + 2 >= obj.Arguments.Count) break;
                var icon = obj.Arguments[i];
                var featTitle = obj.Arguments[i + 1];
                var featDesc = obj.Arguments[i + 2];

                renderer.Write("<div class=\"relative pl-16\">");
                renderer.Write("<dt class=\"text-base font-semibold leading-7 text-gray-900 dark:text-white\">");
                renderer.Write("<div class=\"absolute left-0 top-0 flex h-10 w-10 items-center justify-center rounded-lg bg-indigo-600\">");

                if (icon.StartsWith(":"))
                {
                     RenderInline(renderer, icon);
                }
                else
                {
                     renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)} text-white text-xl\"></i>");
                }

                renderer.Write("</div>");
                renderer.WriteEscape(featTitle);
                renderer.Write("</dt>");
                renderer.Write("<dd class=\"mt-2 text-base leading-7 text-gray-600 dark:text-gray-300\">");
                renderer.WriteEscape(featDesc);
                renderer.Write("</dd>");
                renderer.Write("</div>");
            }

            renderer.Write("</dl></div></div></div>");
        }

        private void RenderCtaPanel(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var desc = obj.GetAttribute("desc");
            var cta1 = obj.GetAttribute("cta1");
            var link1 = obj.GetAttribute("link1", "#");
            var cta2 = obj.GetAttribute("cta2");
            var link2 = obj.GetAttribute("link2", "#");
            var image = obj.GetAttribute("image");
            var align = obj.GetAttribute("align", "right"); // image alignment

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl py-24 sm:px-6 sm:py-32 lg:px-8\">");
            renderer.Write("<div class=\"relative isolate overflow-hidden bg-gray-50 dark:bg-gray-900 px-6 pt-16 shadow-2xl sm:rounded-3xl sm:px-16 md:pt-24 lg:flex lg:gap-x-20 lg:px-24 lg:pt-0 ring-1 ring-gray-200 dark:ring-gray-800\">");

            // Text Content
            renderer.Write("<div class=\"mx-auto max-w-md text-center lg:mx-0 lg:flex-auto lg:py-32 lg:text-left\">");
            renderer.Write("<h2 class=\"text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</h2>");
            renderer.Write("<p class=\"mt-6 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            renderer.WriteEscape(desc);
            renderer.Write("</p>");

            renderer.Write("<div class=\"mt-10 flex items-center justify-center gap-x-6 lg:justify-start\">");
            if (!string.IsNullOrEmpty(cta1))
            {
                renderer.Write($"<a href=\"{link1}\" class=\"rounded-md bg-white dark:bg-white/10 px-3.5 py-2.5 text-sm font-semibold text-gray-900 dark:text-white shadow-sm ring-1 ring-inset ring-gray-300 dark:ring-white/20 hover:bg-gray-50 dark:hover:bg-white/20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-white no-underline\">{cta1}</a>");
            }
            if (!string.IsNullOrEmpty(cta2))
            {
                renderer.Write($"<a href=\"{link2}\" class=\"text-sm font-semibold leading-6 text-gray-900 dark:text-white no-underline\">{cta2} <span aria-hidden=\"true\">→</span></a>");
            }
            renderer.Write("</div></div>");

            // Image
            if (!string.IsNullOrEmpty(image))
            {
                renderer.Write("<div class=\"relative mt-16 h-80 lg:mt-8\">");
                renderer.Write($"<img class=\"absolute left-0 top-0 w-[57rem] max-w-none rounded-md bg-white/5 ring-1 ring-gray-900/10 dark:ring-white/10\" src=\"{image}\" alt=\"App screenshot\" width=\"1824\" height=\"1080\">");
                renderer.Write("</div>");
            }

            renderer.Write("</div></div></div>");
        }

        private void RenderBentoGrid(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");

            renderer.Write("<div class=\"bg-gray-50 dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-2xl px-6 lg:max-w-7xl lg:px-8\">");

            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h2 class=\"text-center text-base font-semibold leading-7 text-indigo-600 dark:text-indigo-400\">Deploy faster</h2>");
                renderer.Write("<p class=\"mx-auto mt-2 max-w-lg text-center text-4xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-6xl\">");
                renderer.WriteEscape(title);
                renderer.Write("</p>");
            }

            if (!string.IsNullOrEmpty(subtitle))
            {
                renderer.Write("<p class=\"mx-auto mt-6 max-w-2xl text-center text-lg leading-8 text-gray-600 dark:text-gray-300\">");
                renderer.WriteEscape(subtitle);
                renderer.Write("</p>");
            }

            renderer.Write("<div class=\"mt-10 grid gap-4 sm:mt-16 lg:grid-cols-3\">");

            // Expecting items in groups of 3 (Title, Desc, Img)
            // Bento Layout Strategy (3-column):
            // Pattern repeats every 4 items:
            // Item 0: span-2
            // Item 1: span-1
            // Item 2: span-1
            // Item 3: span-2
            // This creates a nice alternating layout.

            for (int i = 0; i < obj.Arguments.Count; i += 3)
            {
                if (i + 2 >= obj.Arguments.Count) break;
                var itemTitle = obj.Arguments[i];
                var itemDesc = obj.Arguments[i + 1];
                var itemImg = obj.Arguments[i + 2];

                int itemIndex = i / 3;
                bool isWide = (itemIndex % 4 == 0) || (itemIndex % 4 == 3);
                var className = isWide ? "relative lg:col-span-2" : "relative lg:col-span-1";

                renderer.Write($"<div class=\"{className}\">");
                 renderer.Write("<div class=\"absolute inset-px rounded-lg bg-white dark:bg-gray-800 max-lg:rounded-t-[2rem] lg:rounded-tl-[2rem]\"></div>");
                 renderer.Write("<div class=\"relative flex h-full flex-col overflow-hidden rounded-[calc(theme(borderRadius.lg)+1px)] max-lg:rounded-t-[calc(2rem+1px)] lg:rounded-tl-[calc(2rem+1px)]\">");
                 renderer.Write($"<img class=\"h-80 object-cover object-left\" src=\"{itemImg}\" alt=\"\">");
                 renderer.Write("<div class=\"p-10 pt-4\">");
                 renderer.Write($"<p class=\"mt-2 text-lg font-medium tracking-tight text-gray-900 dark:text-white\">{itemTitle}</p>");
                 renderer.Write($"<p class=\"mt-2 max-w-lg text-sm/6 text-gray-600 dark:text-gray-400\">{itemDesc}</p>");
                 renderer.Write("</div></div><div class=\"pointer-events-none absolute inset-px rounded-lg shadow ring-1 ring-black/5 dark:ring-white/10 max-lg:rounded-t-[2rem] lg:rounded-tl-[2rem]\"></div></div>");
            }

            renderer.Write("</div></div></div>");
        }

        private void RenderPricingTiers(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");
            var highlight = obj.GetAttribute("highlight");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto max-w-4xl text-center\">");
            renderer.Write("<h2 class=\"text-base font-semibold leading-7 text-indigo-600 dark:text-indigo-400\">Pricing</h2>");
            renderer.Write($"<p class=\"mt-2 text-4xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-5xl\">{title}</p>");
            renderer.Write("</div>");
            renderer.Write($"<p class=\"mx-auto mt-6 max-w-2xl text-center text-lg leading-8 text-gray-600 dark:text-gray-300\">{subtitle}</p>");

            renderer.Write("<div class=\"isolate mx-auto mt-16 grid max-w-md grid-cols-1 gap-y-8 sm:mt-20 lg:mx-0 lg:max-w-none lg:grid-cols-2\">");

            int index = 0;
            for (int i = 0; i < obj.Arguments.Count; i += 4)
            {
                if (i + 3 >= obj.Arguments.Count) break;
                index++;
                var planName = obj.Arguments[i];
                var price = obj.Arguments[i + 1];
                var planDesc = obj.Arguments[i + 2];
                var features = obj.Arguments[i + 3].Split(',');

                var isHighlight = highlight == index.ToString();
                var ringClass = isHighlight ? "ring-2 ring-indigo-600" : "ring-1 ring-gray-200 dark:ring-white/10";
                var buttonClass = isHighlight
                    ? "bg-indigo-600 text-white shadow-sm hover:bg-indigo-500"
                    : "text-indigo-600 ring-1 ring-inset ring-indigo-200 hover:ring-indigo-300 dark:text-indigo-400 dark:ring-white/10 dark:hover:ring-white/20";

                renderer.Write($"<div class=\"flex flex-col justify-between rounded-3xl bg-white dark:bg-gray-800 p-8 {ringClass} xl:p-10\">");
                renderer.Write("<div>");
                renderer.Write("<div class=\"flex items-center justify-between gap-x-4\">");
                renderer.Write($"<h3 class=\"text-lg font-semibold leading-8 text-gray-900 dark:text-white\">{planName}</h3>");
                if (isHighlight)
                {
                    renderer.Write("<p class=\"rounded-full bg-indigo-600/10 px-2.5 py-1 text-xs font-semibold leading-5 text-indigo-600 dark:text-indigo-400\">Most popular</p>");
                }
                renderer.Write("</div>");
                renderer.Write("<p class=\"mt-4 text-sm leading-6 text-gray-600 dark:text-gray-300\">");
                renderer.WriteEscape(planDesc);
                renderer.Write("</p>");
                renderer.Write("<p class=\"mt-6 flex items-baseline gap-x-1\">");
                renderer.Write($"<span class=\"text-4xl font-bold tracking-tight text-gray-900 dark:text-white\">{price}</span>");
                renderer.Write("<span class=\"text-sm font-semibold leading-6 text-gray-600 dark:text-gray-300\">/month</span>");
                renderer.Write("</p>");

                renderer.Write("<ul role=\"list\" class=\"mt-8 space-y-3 text-sm leading-6 text-gray-600 dark:text-gray-300\">");
                foreach (var feature in features)
                {
                    renderer.Write("<li class=\"flex gap-x-3\">");
                    renderer.Write("<svg class=\"h-6 w-5 flex-none text-indigo-600 dark:text-indigo-400\" viewBox=\"0 0 20 20\" fill=\"currentColor\" aria-hidden=\"true\"><path fill-rule=\"evenodd\" d=\"M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z\" clip-rule=\"evenodd\" /></svg>");
                    renderer.WriteEscape(feature.Trim());
                    renderer.Write("</li>");
                }
                renderer.Write("</ul>");
                renderer.Write("</div>");
                renderer.Write($"<a href=\"#\" aria-describedby=\"tier-{index}\" class=\"mt-8 block rounded-md px-3 py-2 text-center text-sm font-semibold leading-6 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 no-underline {buttonClass}\">Buy plan</a>");
                renderer.Write("</div>");
            }
            renderer.Write("</div></div></div>");
        }

        private void RenderHeaderStats(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");

            renderer.Write("<div class=\"bg-gray-50 dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto max-w-2xl lg:mx-0\">");
            renderer.Write("<h2 class=\"text-4xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-6xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</h2>");
            renderer.Write("<p class=\"mt-6 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            renderer.WriteEscape(subtitle);
            renderer.Write("</p>");
            renderer.Write("</div>");

            renderer.Write("<div class=\"mx-auto mt-10 max-w-2xl lg:mx-0 lg:max-w-none\">");
            renderer.Write("<dl class=\"mt-16 grid grid-cols-1 gap-8 sm:mt-20 sm:grid-cols-2 lg:grid-cols-4\">");

            for (int i = 0; i < obj.Arguments.Count; i += 2)
            {
                if (i + 1 >= obj.Arguments.Count) break;
                var label = obj.Arguments[i];
                var value = obj.Arguments[i + 1];

                renderer.Write("<div class=\"flex flex-col-reverse\">");
                renderer.Write($"<dt class=\"text-base leading-7 text-gray-600 dark:text-gray-300\">{label}</dt>");
                renderer.Write($"<dd class=\"text-2xl font-bold leading-9 tracking-tight text-gray-900 dark:text-white\">{value}</dd>");
                renderer.Write("</div>");
            }

            renderer.Write("</dl></div></div></div>");
        }

        private void RenderNewsletterSide(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var desc = obj.GetAttribute("desc");
            var cta = obj.GetAttribute("cta", "Subscribe");
            var placeholder = obj.GetAttribute("placeholder", "Enter your email");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-16 sm:py-24 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl sm:px-6 lg:px-8\">");
            renderer.Write("<div class=\"relative isolate overflow-hidden bg-gray-50 dark:bg-gray-900 px-6 py-24 shadow-2xl sm:rounded-3xl sm:px-24 xl:py-32 ring-1 ring-gray-200 dark:ring-gray-800\">");

            renderer.Write("<h2 class=\"mx-auto max-w-2xl text-center text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</h2>");

            renderer.Write("<p class=\"mx-auto mt-2 max-w-xl text-center text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            renderer.WriteEscape(desc);
            renderer.Write("</p>");

            renderer.Write("<form class=\"mx-auto mt-10 flex max-w-md gap-x-4\">");
            renderer.Write("<label for=\"email-address\" class=\"sr-only\">Email address</label>");
            renderer.Write($"<input id=\"email-address\" name=\"email\" type=\"email\" autocomplete=\"email\" required class=\"min-w-0 flex-auto rounded-md border-0 bg-white dark:bg-white/5 px-3.5 py-2 text-gray-900 dark:text-white shadow-sm ring-1 ring-inset ring-gray-300 dark:ring-white/10 focus:ring-2 focus:ring-inset focus:ring-indigo-600 dark:focus:ring-indigo-500 sm:text-sm sm:leading-6\" placeholder=\"{placeholder}\">");
            renderer.Write($"<button type=\"submit\" class=\"flex-none rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600\">{cta}</button>");
            renderer.Write("</form>");

            // Optional: Render details if arguments present?
            if (obj.Arguments.Count > 0)
            {
                renderer.Write("<div class=\"mt-10 grid grid-cols-1 gap-x-8 gap-y-6 text-base font-semibold leading-7 text-gray-900 dark:text-white sm:grid-cols-2 md:flex lg:gap-x-10 justify-center\">");
                foreach(var arg in obj.Arguments)
                {
                    renderer.Write($"<a href=\"#\">{arg} <span aria-hidden=\"true\">&rarr;</span></a>");
                }
                renderer.Write("</div>");
            }

            renderer.Write("<svg viewBox=\"0 0 1024 1024\" class=\"absolute left-1/2 top-1/2 -z-10 h-[64rem] w-[64rem] -translate-x-1/2\" aria-hidden=\"true\"><circle cx=\"512\" cy=\"512\" r=\"512\" fill=\"url(#gradient)\" fill-opacity=\"0.7\" /><defs><radialGradient id=\"gradient\" cx=\"0\" cy=\"0\" r=\"1\" gradientUnits=\"userSpaceOnUse\" gradientTransform=\"translate(512 512) rotate(90) scale(512)\"><stop stop-color=\"#7775D6\" /><stop offset=\"1\" stop-color=\"#E935C1\" stop-opacity=\"0\" /></radialGradient></defs></svg>");

            renderer.Write("</div></div></div>");
        }

        private void RenderStatsSimple(HtmlRenderer renderer, ComponentInline obj)
        {
            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<dl class=\"grid grid-cols-1 gap-x-8 gap-y-16 text-center lg:grid-cols-3\">");

            for (int i = 0; i < obj.Arguments.Count; i += 2)
            {
                if (i + 1 >= obj.Arguments.Count) break;
                var label = obj.Arguments[i];
                var value = obj.Arguments[i + 1];

                renderer.Write("<div class=\"mx-auto flex max-w-xs flex-col gap-y-4\">");
                renderer.Write($"<dt class=\"text-base leading-7 text-gray-600 dark:text-gray-300\">{label}</dt>");
                renderer.Write($"<dd class=\"order-first text-3xl font-semibold tracking-tight text-gray-900 dark:text-white sm:text-5xl\">{value}</dd>");
                renderer.Write("</div>");
            }

            renderer.Write("</dl></div></div>");
        }

        private void RenderHeroSimple(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");
            var cta1Text = obj.GetAttribute("cta1-text");
            var cta1Link = obj.GetAttribute("cta1-link", "#");
            var cta2Text = obj.GetAttribute("cta2-text");
            var cta2Link = obj.GetAttribute("cta2-link", "#");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 not-prose\">");
            renderer.Write("<div class=\"px-6 py-24 sm:px-6 sm:py-32 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto max-w-2xl text-center\">");

            renderer.Write("<h2 class=\"text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</h2>");

            renderer.Write("<p class=\"mx-auto mt-6 max-w-xl text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            renderer.WriteEscape(subtitle);
            renderer.Write("</p>");

            renderer.Write("<div class=\"mt-10 flex items-center justify-center gap-x-6\">");
            if (!string.IsNullOrEmpty(cta1Text))
            {
                renderer.Write($"<a href=\"{cta1Link}\" class=\"rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 no-underline\">{cta1Text}</a>");
            }
            if (!string.IsNullOrEmpty(cta2Text))
            {
                renderer.Write($"<a href=\"{cta2Link}\" class=\"text-sm font-semibold leading-6 text-gray-900 dark:text-white no-underline\">{cta2Text} <span aria-hidden=\"true\">→</span></a>");
            }
            renderer.Write("</div></div></div></div>");
        }

        private void RenderBlogColumn(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto max-w-2xl text-center\">");
            renderer.Write("<h2 class=\"text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</h2>");
            renderer.Write("<p class=\"mt-2 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            renderer.WriteEscape(subtitle);
            renderer.Write("</p>");
            renderer.Write("</div>");

            renderer.Write("<div class=\"mx-auto mt-16 grid max-w-2xl grid-cols-1 gap-x-8 gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-3\">");

            for (int i = 0; i < obj.Arguments.Count; i += 5)
            {
                if (i + 4 >= obj.Arguments.Count) break;
                var postTitle = obj.Arguments[i];
                var postDesc = obj.Arguments[i + 1];
                var author = obj.Arguments[i + 2];
                var date = obj.Arguments[i + 3];
                var img = obj.Arguments[i + 4];

                renderer.Write("<article class=\"flex flex-col items-start justify-between\">");
                renderer.Write("<div class=\"relative w-full\">");
                renderer.Write($"<img src=\"{img}\" alt=\"\" class=\"aspect-[16/9] w-full rounded-2xl bg-gray-100 object-cover sm:aspect-[2/1] lg:aspect-[3/2]\">");
                renderer.Write("<div class=\"absolute inset-0 rounded-2xl ring-1 ring-inset ring-gray-900/10\"></div>");
                renderer.Write("</div>");
                renderer.Write("<div class=\"max-w-xl\">");
                renderer.Write("<div class=\"mt-8 flex items-center gap-x-4 text-xs\">");
                renderer.Write($"<time datetime=\"{date}\" class=\"text-gray-500 dark:text-gray-400\">{date}</time>");
                renderer.Write("<a href=\"#\" class=\"relative z-10 rounded-full bg-gray-50 px-3 py-1.5 font-medium text-gray-600 hover:bg-gray-100 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700\">Marketing</a>");
                renderer.Write("</div>");
                renderer.Write("<div class=\"group relative\">");
                renderer.Write("<h3 class=\"mt-3 text-lg font-semibold leading-6 text-gray-900 dark:text-white group-hover:text-gray-600 dark:group-hover:text-gray-300\">");
                renderer.Write($"<a href=\"#\"><span class=\"absolute inset-0\"></span>{postTitle}</a>");
                renderer.Write("</h3>");
                renderer.Write($"<p class=\"mt-5 line-clamp-3 text-sm leading-6 text-gray-600 dark:text-gray-300\">{postDesc}</p>");
                renderer.Write("</div>");
                renderer.Write("<div class=\"relative mt-8 flex items-center gap-x-4\">");
                renderer.Write("<img src=\"https://images.unsplash.com/photo-1519244703995-f4e0f30006d5?ixlib=rb-1.2.1&ixid=eyJhcHBfaWQiOjEyMDd9&auto=format&fit=facearea&facepad=2&w=256&h=256&q=80\" alt=\"\" class=\"h-10 w-10 rounded-full bg-gray-100\">");
                renderer.Write($"<div class=\"text-sm leading-6\"><p class=\"font-semibold text-gray-900 dark:text-white\"><a href=\"#\"><span class=\"absolute inset-0\"></span>{author}</a></p><p class=\"text-gray-600 dark:text-gray-400\">Co-Founder / CTO</p></div>");
                renderer.Write("</div></div></article>");
            }

            renderer.Write("</div></div></div>");
        }

        private void RenderContentSticky(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var image = obj.GetAttribute("image");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");
            renderer.Write("<div class=\"mx-auto grid max-w-2xl grid-cols-1 gap-x-8 gap-y-16 sm:gap-y-20 lg:mx-0 lg:max-w-none lg:grid-cols-2\">");

            renderer.Write("<div class=\"lg:pr-8 lg:pt-4\">");
            renderer.Write("<div class=\"lg:max-w-lg\">");
            renderer.Write("<h2 class=\"text-base font-semibold leading-7 text-indigo-600 dark:text-indigo-400\">Deploy faster</h2>");
            renderer.Write("<p class=\"mt-2 text-3xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-4xl\">");
            renderer.WriteEscape(title);
            renderer.Write("</p>");

            renderer.Write("<div class=\"mt-6 space-y-6 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
            foreach (var arg in obj.Arguments)
            {
                renderer.Write($"<p>{arg}</p>");
            }
            renderer.Write("</div>"); // End text stack

            renderer.Write("</div></div>"); // End left col

            // Right col sticky image
            renderer.Write("<div class=\"flex items-start justify-end lg:order-last sticky top-8\">");
            renderer.Write($"<img src=\"{image}\" alt=\"Product screenshot\" class=\"w-[48rem] max-w-none rounded-xl shadow-xl ring-1 ring-gray-400/10 sm:w-[57rem] md:-ml-4 lg:-ml-0\" width=\"2432\" height=\"1442\">");
            renderer.Write("</div>");

            renderer.Write("</div></div></div>");
        }

        private void RenderLogoCloud(HtmlRenderer renderer, ComponentInline obj)
        {
            var heading = obj.GetAttribute("heading");

            renderer.Write("<div class=\"bg-white dark:bg-gray-900 py-24 sm:py-32 not-prose\">");
            renderer.Write("<div class=\"mx-auto max-w-7xl px-6 lg:px-8\">");

            if (!string.IsNullOrEmpty(heading))
            {
                renderer.Write("<h2 class=\"text-center text-lg font-semibold leading-8 text-gray-900 dark:text-white\">");
                renderer.WriteEscape(heading);
                renderer.Write("</h2>");
            }

            renderer.Write("<div class=\"mx-auto mt-10 grid max-w-lg grid-cols-4 items-center gap-x-8 gap-y-10 sm:max-w-xl sm:grid-cols-6 sm:gap-x-10 lg:mx-0 lg:max-w-none lg:grid-cols-5\">");

            for (int i = 0; i < obj.Arguments.Count; i += 2)
            {
                if (i + 1 >= obj.Arguments.Count) break;
                var src = obj.Arguments[i];
                var alt = obj.Arguments[i + 1];

                renderer.Write($"<img class=\"col-span-2 max-h-12 w-full object-contain lg:col-span-1\" src=\"{src}\" alt=\"{alt}\" width=\"158\" height=\"48\">");
            }

            renderer.Write("</div></div></div>");
        }

        private void RenderHero(HtmlRenderer renderer, ComponentInline obj)
        {
            var title = obj.GetAttribute("title");
            var subtitle = obj.GetAttribute("subtitle");
            var badgeText = obj.GetAttribute("badge-text");
            var badgeLink = obj.GetAttribute("badge-link");
            var cta1Text = obj.GetAttribute("cta1-text");
            var cta1Link = obj.GetAttribute("cta1-link");
            var cta2Text = obj.GetAttribute("cta2-text");
            var cta2Link = obj.GetAttribute("cta2-link");
            var image = obj.GetAttribute("image");
            var align = obj.GetAttribute("align", "center");

            // Container
            renderer.Write("<div class=\"not-prose relative isolate overflow-hidden bg-gray-50 dark:bg-gray-900 py-24 sm:py-32 rounded-3xl my-8\">");

            // Background Image/Gradient
            if (!string.IsNullOrEmpty(image))
            {
                renderer.Write("<img src=\"");
                renderer.WriteEscapeUrl(image);
                renderer.Write("\" alt=\"\" class=\"absolute inset-0 -z-10 h-full w-full object-cover object-right md:object-center opacity-20\">");
            }
            else
            {
                // Default Dark Gradient
                renderer.Write("<div class=\"hidden sm:absolute sm:-top-10 sm:right-1/2 sm:-z-10 sm:mr-10 sm:block sm:transform-gpu sm:blur-3xl\" aria-hidden=\"true\">");
                renderer.Write("<div class=\"aspect-[1097/845] w-[68.5625rem] bg-gradient-to-tr from-[#ff4694] to-[#776fff] opacity-20\" style=\"clip-path: polygon(74.1% 44.1%, 100% 61.6%, 97.5% 26.9%, 85.5% 0.1%, 80.7% 2%, 72.5% 32.5%, 60.2% 62.4%, 52.4% 68.1%, 47.5% 58.3%, 45.2% 34.5%, 27.5% 76.7%, 0.1% 64.9%, 17.9% 100%, 27.6% 76.8%, 76.1% 97.7%, 74.1% 44.1%)\"></div>");
                renderer.Write("</div>");
                renderer.Write("<div class=\"absolute -top-52 left-1/2 -z-10 -translate-x-1/2 transform-gpu blur-3xl sm:top-[-28rem] sm:ml-16 sm:translate-x-0 sm:transform-gpu\" aria-hidden=\"true\">");
                renderer.Write("<div class=\"aspect-[1097/845] w-[68.5625rem] bg-gradient-to-tr from-[#ff4694] to-[#776fff] opacity-20\" style=\"clip-path: polygon(74.1% 44.1%, 100% 61.6%, 97.5% 26.9%, 85.5% 0.1%, 80.7% 2%, 72.5% 32.5%, 60.2% 62.4%, 52.4% 68.1%, 47.5% 58.3%, 45.2% 34.5%, 27.5% 76.7%, 0.1% 64.9%, 17.9% 100%, 27.6% 76.8%, 76.1% 97.7%, 74.1% 44.1%)\"></div>");
                renderer.Write("</div>");
            }

            // Content Wrapper
            var alignClass = align == "left" ? "text-left" : "text-center";
            var maxWClass = align == "left" ? "max-w-2xl" : "mx-auto max-w-2xl";

            renderer.Write($"<div class=\"{maxWClass} {alignClass} px-6 lg:px-8\">");

            // Badge
            if (!string.IsNullOrEmpty(badgeText))
            {
                var badgeAlign = align == "left" ? "justify-start" : "justify-center";
                renderer.Write($"<div class=\"hidden sm:mb-8 sm:flex {badgeAlign}\">");
                renderer.Write("<div class=\"relative rounded-full px-3 py-1 text-sm leading-6 text-gray-600 dark:text-gray-400 ring-1 ring-gray-900/10 dark:ring-white/10 hover:ring-gray-900/20 dark:hover:ring-white/20\">");

                if (!string.IsNullOrEmpty(badgeLink))
                {
                    renderer.WriteEscape(badgeText);
                    renderer.Write(" <a href=\"");
                    renderer.WriteEscapeUrl(badgeLink);
                    renderer.Write("\" class=\"font-semibold text-gray-900 dark:text-white\"><span class=\"absolute inset-0\" aria-hidden=\"true\"></span>Read more <span aria-hidden=\"true\">&rarr;</span></a>");
                }
                else
                {
                    renderer.WriteEscape(badgeText);
                }
                renderer.Write("</div></div>");
            }

            // Title
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write("<h2 class=\"text-4xl font-bold tracking-tight text-gray-900 dark:text-white sm:text-6xl\">");
                renderer.WriteEscape(title);
                renderer.Write("</h2>");
            }

            // Subtitle
            if (!string.IsNullOrEmpty(subtitle))
            {
                renderer.Write("<p class=\"mt-6 text-lg leading-8 text-gray-600 dark:text-gray-300\">");
                renderer.WriteEscape(subtitle);
                renderer.Write("</p>");
            }

            // Buttons
            if (!string.IsNullOrEmpty(cta1Text) || !string.IsNullOrEmpty(cta2Text))
            {
                var justifyClass = align == "left" ? "justify-start" : "justify-center";
                renderer.Write($"<div class=\"mt-10 flex items-center {justifyClass} gap-x-6\">");

                if (!string.IsNullOrEmpty(cta1Text))
                {
                     var link = cta1Link ?? "#";
                     renderer.Write("<a href=\"");
                     renderer.WriteEscapeUrl(link);
                     renderer.Write("\" class=\"rounded-md bg-indigo-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-indigo-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600 no-underline\">");
                     renderer.WriteEscape(cta1Text);
                     renderer.Write("</a>");
                }

                if (!string.IsNullOrEmpty(cta2Text))
                {
                     var link = cta2Link ?? "#";
                     renderer.Write("<a href=\"");
                     renderer.WriteEscapeUrl(link);
                     renderer.Write("\" class=\"text-sm font-semibold leading-6 text-gray-900 dark:text-white no-underline\">");
                     renderer.WriteEscape(cta2Text);
                     renderer.Write(" <span aria-hidden=\"true\">→</span></a>");
                }

                renderer.Write("</div>");
            }

            renderer.Write("</div>"); // End Content Wrapper
            renderer.Write("</div>"); // End Container
        }

        private void RenderButton(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text");
            if (string.IsNullOrEmpty(text) && obj.Arguments.Count > 0)
            {
                text = obj.Arguments[0];
            }

            var link = obj.GetAttribute("link", "#");
            var variant = obj.GetAttribute("variant", "primary");
            var corners = obj.GetAttribute("corners", "round");
            var size = obj.GetAttribute("size", "m");
            var icon = obj.GetAttribute("icon");
            var target = obj.GetAttribute("target");

            // 1. Variant Styles
            var bgClass = "bg-primary-600 hover:bg-primary-700 text-white dark:bg-primary-600 dark:hover:bg-primary-500"; // default primary
            switch (variant.ToLower())
            {
                case "base":
                    bgClass = "bg-gray-100 text-gray-800 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-100 dark:hover:bg-gray-600";
                    break;
                case "primary":
                    bgClass = "bg-primary-600 text-white hover:bg-primary-700 dark:bg-primary-600 dark:hover:bg-primary-500";
                    break;
                case "secondary":
                    bgClass = "bg-gray-600 text-white hover:bg-gray-700 dark:bg-gray-600 dark:hover:bg-gray-500";
                    break;
                case "success":
                    bgClass = "bg-green-600 text-white hover:bg-green-700 dark:bg-green-600 dark:hover:bg-green-500";
                    break;
                case "danger":
                    bgClass = "bg-red-600 text-white hover:bg-red-700 dark:bg-red-600 dark:hover:bg-red-500";
                    break;
                case "warning":
                    bgClass = "bg-yellow-500 text-white hover:bg-yellow-600 dark:bg-yellow-500 dark:hover:bg-yellow-400";
                    break;
                case "info":
                    bgClass = "bg-sky-500 text-white hover:bg-sky-600 dark:bg-sky-500 dark:hover:bg-sky-400";
                    break;
                case "light":
                    bgClass = "bg-white text-gray-800 border border-gray-300 hover:bg-gray-50 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:hover:bg-gray-700";
                    break;
                case "dark":
                    bgClass = "bg-gray-800 text-white hover:bg-gray-900 dark:bg-gray-50 dark:text-gray-900 dark:hover:bg-gray-200";
                    break;
                case "ghost":
                    bgClass = "bg-transparent text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800";
                    break;
                case "contrast":
                    bgClass = "bg-white text-black hover:bg-gray-100 dark:bg-black dark:text-white dark:hover:bg-gray-900 border border-gray-300 dark:border-gray-700";
                    break;
                case "outline":
                    bgClass = "bg-transparent border border-primary-600 text-primary-600 hover:bg-primary-50 dark:border-primary-400 dark:text-primary-400 dark:hover:bg-primary-900/20";
                    break;
            }

            // 2. Corners
            var roundedClass = "rounded-md";
            switch (corners.ToLower())
            {
                case "round": roundedClass = "rounded-md"; break;
                case "square": roundedClass = "rounded-none"; break;
                case "pill": roundedClass = "rounded-full"; break;
            }

            // 3. Size
            var sizeClass = "px-4 py-2 text-sm";
            switch (size.ToLower())
            {
                case "xs": sizeClass = "px-2.5 py-1.5 text-xs"; break;
                case "s": sizeClass = "px-3 py-2 text-sm"; break;
                case "m": sizeClass = "px-4 py-2 text-sm"; break;
                case "l": sizeClass = "px-4 py-2 text-base"; break;
                case "xl": sizeClass = "px-6 py-3 text-base"; break;
                case "2xl": sizeClass = "px-6 py-3.5 text-xl"; break;
                case "3xl": sizeClass = "px-7 py-4 text-2xl"; break;
            }

            // Target handling
            var targetAttr = "";
            if (!string.IsNullOrEmpty(target))
            {
                if (target.Equals("blank", System.StringComparison.OrdinalIgnoreCase)) target = "_blank";
                targetAttr = $" target=\"{target}\"";
            }

            renderer.Write($"<a href=\"{link}\"{targetAttr} class=\"inline-flex items-center border border-transparent font-medium shadow-sm focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 mr-2 no-underline {bgClass} {roundedClass} {sizeClass}\">");

            if (!string.IsNullOrEmpty(icon))
            {
                if (icon.Trim().StartsWith("<svg", System.StringComparison.OrdinalIgnoreCase))
                {
                     renderer.Write(System.Net.WebUtility.HtmlDecode(icon));
                     renderer.Write(" ");
                }
                else if (icon.StartsWith(":"))
                {
                     // Try to parse inline markdown for emoji or icon shortcode
                     renderer.Write("<span class=\"mr-2\">");
                     RenderInline(renderer, icon);
                     renderer.Write("</span>");
                }
                else
                {
                     renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)} mr-2\"></i>");
                }
            }

            RenderInline(renderer, text);
            renderer.Write("</a>");
        }

        private void RenderColorChip(HtmlRenderer renderer, ComponentInline obj)
        {
            var color = obj.GetAttribute("color", "#000000");

            // Priority to positional arg
            if (obj.Arguments.Count > 0)
            {
                color = obj.Arguments[0];
            }

            var text = obj.GetAttribute("text", color);

            renderer.Write($"<span class=\"inline-flex items-center align-middle bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded px-1.5 py-0.5 text-sm font-mono text-gray-800 dark:text-gray-200\">");
            renderer.Write($"<span class=\"w-3 h-3 rounded-full mr-1.5 border border-gray-300 dark:border-gray-600\" style=\"background-color: {color};\"></span>");
            renderer.Write(text);
            renderer.Write("</span>");
        }

        private void RenderEmojiTable(HtmlRenderer renderer, ComponentInline obj)
        {
            renderer.Write("<div class=\"overflow-x-auto my-4\">");
            renderer.Write("<table class=\"min-w-full divide-y divide-gray-200 dark:divide-gray-700\">");
            renderer.Write("<thead class=\"bg-gray-50 dark:bg-gray-800\">");
            renderer.Write("<tr>");
            renderer.Write("<th class=\"px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider\">Emoji</th>");
            renderer.Write("<th class=\"px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider\">Shortcode</th>");
            renderer.Write("</tr>");
            renderer.Write("</thead>");
            renderer.Write("<tbody class=\"bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700\">");

            // Markdig Emoji Mapping
            foreach (var kvp in Markdig.Extensions.Emoji.EmojiMapping.GetDefaultEmojiShortcodeToUnicode())
            {
                var shortcode = kvp.Key;
                var unicode = kvp.Value;

                renderer.Write("<tr>");
                renderer.Write($"<td class=\"px-6 py-4 whitespace-nowrap text-2xl\">{unicode}</td>");
                renderer.Write($"<td class=\"px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400\"><code>{shortcode}</code></td>");
                renderer.Write("</tr>");
            }

            renderer.Write("</tbody>");
            renderer.Write("</table>");
            renderer.Write("</div>");
        }

        private void RenderInline(HtmlRenderer renderer, string text)
        {
             if (string.IsNullOrEmpty(text)) return;
             // We need a pipeline to parse.
             if (_pipeline != null)
             {
                 // Parse as document but only render inlines of first paragraph
                 var doc = Markdig.Markdown.Parse(text, _pipeline);
                 foreach (var block in doc)
                 {
                     if (block is Markdig.Syntax.ParagraphBlock p)
                     {
                         renderer.Write(p.Inline);
                     }
                     else
                     {
                         renderer.Render(block);
                     }
                 }
                 return;
             }
             renderer.Write(text);
        }

        private void RenderEmbed(HtmlRenderer renderer, ComponentInline obj)
        {
            var src = obj.GetAttribute("src");
            var height = obj.GetAttribute("height", "400"); // Default height

            if (!string.IsNullOrEmpty(src))
            {
                renderer.Write($"<div class=\"my-4 w-full rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 shadow-sm\">");
                renderer.Write($"<iframe src=\"{src}\" style=\"width: 100%; height: {height}px;\" frameborder=\"0\" allowfullscreen></iframe>");
                renderer.Write("</div>");
            }
        }

        private void RenderFile(HtmlRenderer renderer, ComponentInline obj)
        {
            var link = obj.GetAttribute("link", "#");

            string text = null;
            if (obj.Attributes.ContainsKey("text"))
            {
                text = obj.Attributes["text"];
            }
            else if (obj.Arguments.Count > 0)
            {
                text = obj.Arguments[0];
            }

            if (string.IsNullOrEmpty(text))
            {
                try
                {
                    if (link != "#")
                        text = System.IO.Path.GetFileName(link);
                }
                catch { }
            }

            if (string.IsNullOrEmpty(text)) text = "Download";

            var size = obj.GetAttribute("size");
            var icon = obj.GetAttribute("icon");

            renderer.Write($"<a href=\"{link}\" class=\"inline-flex items-center p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors no-underline group my-2\" download>");
            renderer.Write($"<div class=\"p-2 bg-primary-50 dark:bg-primary-900 rounded-md mr-4 group-hover:bg-primary-100 dark:group-hover:bg-primary-800 transition-colors flex items-center justify-center w-11 h-11\">");

            if (!string.IsNullOrEmpty(icon))
            {
                if (icon.Trim().StartsWith("<svg", System.StringComparison.OrdinalIgnoreCase))
                {
                    renderer.Write(System.Net.WebUtility.HtmlDecode(icon));
                }
                else if (icon.StartsWith(":"))
                {
                    RenderInline(renderer, icon);
                }
                else if (icon.Contains("/") || icon.Contains("."))
                {
                    renderer.Write($"<img src=\"{icon}\" class=\"w-6 h-6 object-contain\" alt=\"\" />");
                }
                else
                {
                    renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(icon)} text-primary-600 dark:text-primary-400 text-xl\"></i>");
                }
            }
            else
            {
                renderer.Write($"<i class=\"fi fi-rr-document text-primary-600 dark:text-primary-400 text-xl\"></i>");
            }

            renderer.Write("</div>");
            renderer.Write("<div>");
            renderer.Write($"<div class=\"font-medium text-gray-900 dark:text-white\">{text}</div>");
            if (!string.IsNullOrEmpty(size))
            {
                renderer.Write($"<div class=\"text-xs text-gray-500 dark:text-gray-400\">{size}</div>");
            }
            renderer.Write("</div>");
            renderer.Write("</a>");
        }

        private void RenderIcon(HtmlRenderer renderer, ComponentInline obj)
        {
            var name = obj.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
            {
                renderer.Write($"<i class=\"{Neko.Builder.IconHelper.GetIconClass(name)} align-middle\"></i>");
            }
        }

        private void RenderRef(HtmlRenderer renderer, ComponentInline obj)
        {
            var text = obj.GetAttribute("text");
            var link = obj.GetAttribute("link");

            if (string.IsNullOrEmpty(link) && obj.Arguments.Count > 0)
            {
                link = obj.Arguments[0];
            }

            if (string.IsNullOrEmpty(text) && obj.Arguments.Count > 1)
            {
                text = string.Join(" ", obj.Arguments.GetRange(1, obj.Arguments.Count - 1));
            }

            if (string.IsNullOrEmpty(text))
            {
                text = link;
            }

            if (string.IsNullOrEmpty(link))
            {
                link = "#";
            }

            renderer.Write($"<a href=\"{link}\" class=\"inline-flex items-center text-primary-600 dark:text-primary-400 hover:underline no-underline\">");
            renderer.Write($"<i class=\"fi fi-rr-link-alt mr-1\"></i>");
            renderer.Write(text);
            renderer.Write("</a>");
        }

        private void RenderYouTube(HtmlRenderer renderer, ComponentInline obj)
        {
            var id = obj.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
            {
                renderer.Write($"<div class=\"aspect-w-16 aspect-h-9 my-4\">");
                renderer.Write($"<iframe src=\"https://www.youtube.com/embed/{id}\" frameborder=\"0\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen class=\"w-full h-full rounded-lg shadow-lg\"></iframe>");
                renderer.Write("</div>");
            }
        }
    }

    public class ComponentExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.InlineParsers.Contains<ComponentParser>())
            {
                pipeline.InlineParsers.Insert(0, new ComponentParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer)
            {
                // We need to pass the pipeline to the renderer so it can parse nested markdown (e.g. in button text)
                if (!htmlRenderer.ObjectRenderers.Contains<ComponentRenderer>())
                {
                    htmlRenderer.ObjectRenderers.Add(new ComponentRenderer(pipeline));
                }
            }
        }
    }
}
