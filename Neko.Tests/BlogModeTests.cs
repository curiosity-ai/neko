using System.Collections.Generic;
using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    // Covers `mode: blog` — the marketing-site chrome (curiosity.ai look): the
    // logo used as a wordmark, header CTA buttons (`actions:`), no dark-mode
    // toggle, light page background, and the configurable footer.
    public class BlogModeTests
    {
        private static ParsedDocument Doc() => new ParsedDocument
        {
            Html = "<p>Content</p>",
            FrontMatter = new FrontMatter { Title = "Home" }
        };

        private static NekoConfig BlogConfig() => new NekoConfig
        {
            Mode = "blog",
            Branding = new BrandingConfig { Title = "Curiosity", Logo = "/assets/logo.png" },
            Actions = new List<ActionConfig>
            {
                new ActionConfig { Text = "Book a Demo", Link = "https://example.com/demo", Variant = "primary" },
                new ActionConfig { Text = "Talk to Sales", Link = "https://example.com/sales", Variant = "outline", Target = "blank" }
            }
        };

        [Test]
        public void BlogMode_LogoIsWordmark_NoDuplicateTitleText()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            // The branding title must NOT be rendered as the visible navbar label
            // next to the wordmark logo (the "Curiosity Curiosity" overlap bug).
            Assert.That(html, Does.Not.Contain("font-bold text-xl hover:text-primary-600 transition-colors\">Curiosity</a>"));
            // The logo links home with an accessible label instead.
            Assert.That(html, Contains.Substring("aria-label=\"Curiosity\""));
            Assert.That(html, Contains.Substring("src=\"/assets/logo.png\""));
        }

        [Test]
        public void DocsMode_KeepsLogoAndTitleText()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            var html = new HtmlGenerator(config).Generate(Doc());

            // Docs mode is unchanged: logo + visible branding title.
            Assert.That(html, Contains.Substring("font-bold text-xl hover:text-primary-600 transition-colors\">Curiosity</a>"));
        }

        [Test]
        public void BlogMode_RendersActionButtons_WithVariants()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            Assert.That(html, Contains.Substring(">Book a Demo</a>"));
            Assert.That(html, Contains.Substring(">Talk to Sales</a>"));
            // Pills, driven by the base palette (defaults: #1f1f1f ink on #f1f1f1).
            Assert.That(html, Contains.Substring("rounded-full"));
            // Pills are driven by the palette vars so they invert with dark mode.
            Assert.That(html, Contains.Substring("background-color:var(--blog-ink);color:var(--blog-bg)"));   // primary = solid fill
            // outline = 1px ring drawn as an inset box-shadow (not a border) so it
            // doesn't grow the box and misalign against the solid pill next to it.
            Assert.That(html, Contains.Substring("box-shadow:inset 0 0 0 1px var(--blog-ink);color:var(--blog-ink)"));
            // `target: blank` is normalised to a valid _blank target.
            Assert.That(html, Contains.Substring("target=\"_blank\""));
        }

        [Test]
        public void BlogMode_MovesSearchFromHeaderToContentList_AsInlineSearch()
        {
            var config = BlogConfig();
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Post", Description = "d" } }, "/blog/post")
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // Blog mode no longer opens the modal — there is no openSearch trigger
            // anywhere (neither header nor content list).
            Assert.That(html, Does.Not.Contain("openSearch()"));

            // Instead it renders a live inline search box wired to search.js, plus
            // an (initially hidden) results container and the identifiable grid it
            // toggles against.
            Assert.That(html, Contains.Substring("id=\"neko-inline-search\""));
            Assert.That(html, Contains.Substring("id=\"neko-inline-search-results\""));
            Assert.That(html, Contains.Substring("id=\"neko-blog-grid\""));
            Assert.That(html, Contains.Substring("Search the blog"));

            // The search box renders above the post grid.
            var searchIdx = html.IndexOf("id=\"neko-inline-search\"");
            var gridIdx = html.IndexOf("id=\"neko-blog-grid\"");
            Assert.That(searchIdx, Is.GreaterThan(0));
            Assert.That(searchIdx, Is.LessThan(gridIdx), "search bar should render above the post grid");
        }

        [Test]
        public void BlogMode_RendersHero_PillAndTitle_WithSpecFonts()
        {
            var config = BlogConfig();
            config.Blog = new BlogConfig
            {
                Pill = "Blog",
                Title = "Notes on how we build AI products and systems in practice",
                Description = "Product updates and release overviews."
            };
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Label = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Post" } }, "/blog/post")
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // The pill: plain system sans-serif at 12px (not the brand body font).
            Assert.That(html, Contains.Substring("font-family:sans-serif"));
            Assert.That(html, Contains.Substring("text-[12px]"));
            Assert.That(html, Contains.Substring(">Blog</span>"));

            // The title: rendered as the H1 at 30.4px / weight 500, inheriting the
            // body font. It replaces the plain label H1.
            Assert.That(html, Contains.Substring("text-[30.4px]"));
            Assert.That(html, Contains.Substring("font-medium"));
            Assert.That(html, Contains.Substring("Notes on how we build AI products and systems in practice"));
            // The fallback label H1 must NOT also render when a hero title is set.
            Assert.That(html, Does.Not.Contain("text-3xl md:text-4xl font-bold mt-2 mb-0"));

            // The optional lead paragraph renders under the title.
            Assert.That(html, Contains.Substring("Product updates and release overviews."));

            // The hero renders above the search box and post grid.
            var heroIdx = html.IndexOf("Notes on how we build");
            var searchIdx = html.IndexOf("id=\"neko-inline-search\"");
            Assert.That(heroIdx, Is.GreaterThan(0));
            Assert.That(heroIdx, Is.LessThan(searchIdx), "hero should render above the search box");
        }

        [Test]
        public void BlogMode_NoHeroTitle_FallsBackToLabelH1()
        {
            var config = BlogConfig();   // config.Blog left at its empty default
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Label = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Post" } }, "/blog/post")
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // With no blog.title configured the index keeps the plain label H1.
            Assert.That(html, Contains.Substring("text-3xl md:text-4xl font-bold mt-2 mb-0"));
            Assert.That(html, Does.Not.Contain("text-[30.4px]"));
        }

        [Test]
        public void BlogMode_RendersTagChips_AndTagsCards_ForTagFiltering()
        {
            var config = BlogConfig();
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Alpha", Tags = new[] { "Photography", "Milestone" } } }, "/blog/alpha"),
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Beta", Tags = new[] { "cooking" } } }, "/blog/beta"),
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // A tag-filter chip row with an "All" reset chip and one chip per unique
            // tag (lowercased on the data-tag, sorted).
            Assert.That(html, Contains.Substring("id=\"neko-blog-tags\""));
            Assert.That(html, Contains.Substring("data-tag=\"\""));      // the "All" chip
            Assert.That(html, Contains.Substring("data-tag=\"cooking\""));
            Assert.That(html, Contains.Substring("data-tag=\"milestone\""));
            Assert.That(html, Contains.Substring("data-tag=\"photography\""));

            // Each card carries its (lowercased, pipe-joined) tags so the chips can
            // filter the grid client-side.
            Assert.That(html, Contains.Substring("data-tags=\"photography|milestone\""));
            Assert.That(html, Contains.Substring("data-tags=\"cooking\""));

            // The chips render above the post grid, and an empty-state note follows it.
            var tagsIdx = html.IndexOf("id=\"neko-blog-tags\"");
            var gridIdx = html.IndexOf("id=\"neko-blog-grid\"");
            Assert.That(tagsIdx, Is.GreaterThan(0));
            Assert.That(tagsIdx, Is.LessThan(gridIdx), "tag chips should render above the post grid");
            Assert.That(html, Contains.Substring("id=\"neko-blog-empty\""));
        }

        [Test]
        public void BlogMode_CardCoverSlot_IsConsistent_WithPlaceholderAndFallback()
        {
            var config = BlogConfig();
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "WithCover", Cover = "/assets/x.png" } }, "/blog/with"),
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "NoCover" } }, "/blog/without"),
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // Every card renders the placeholder picture slot so the cover area
            // always reserves the same space and stays consistent — present for the
            // coverless card and behind the cover of the one that has it.
            var slotCount = System.Text.RegularExpressions.Regex.Matches(html, "fi fi-rr-picture").Count;
            Assert.That(slotCount, Is.EqualTo(2), "each card should render a cover placeholder slot");

            // The cover image degrades gracefully: it hides itself if it fails to
            // load (so no torn-image glyph) and hides the placeholder once it loads.
            Assert.That(html, Contains.Substring("onerror=\"this.style.display='none'\""));
            Assert.That(html, Contains.Substring("onload=\"this.previousElementSibling.style.display='none'\""));
        }

        [Test]
        public void BlogMode_WithoutTags_OmitsTagChipRow()
        {
            var config = BlogConfig();
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Untagged" } }, "/blog/untagged"),
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // No tags anywhere → no chip row (but the grid and its empty-state stay).
            Assert.That(html, Does.Not.Contain("id=\"neko-blog-tags\""));
            Assert.That(html, Contains.Substring("id=\"neko-blog-grid\""));
        }

        [Test]
        public void DocsMode_KeepsSearchInHeader()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("onclick=\"openSearch()\""));
            Assert.That(html, Does.Not.Contain("Search the blog"));
        }

        [Test]
        public void BlogMode_DefaultsToLightOnly()
        {
            // No theme.dark configured → light-only marketing look.
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            // No theme toggle, light is locked, and no dark palette override.
            Assert.That(html, Does.Not.Contain("id=\"theme-toggle\""));
            // The toggle button is absent, so the theme-switch script must guard
            // the listener wiring instead of dereferencing a null element.
            Assert.That(html, Contains.Substring("if (themeToggleBtn) {"));
            Assert.That(html, Contains.Substring("localStorage.theme = 'light'"));
            Assert.That(html, Contains.Substring("--blog-bg: #f1f1f1"));
            Assert.That(html, Contains.Substring("--blog-ink: #1f1f1f"));
            Assert.That(html, Does.Not.Contain("html.dark { --blog-bg"));
            // The recent-pages history clock stays out of the marketing header.
            Assert.That(html, Does.Not.Contain("id=\"history-btn\""));
        }

        [Test]
        public void BlogMode_DarkIsOptInViaThemeDark()
        {
            // Defining theme.dark opts the blog into dark mode (and the toggle).
            var config = BlogConfig();
            config.Theme.Dark["base-bg"] = "#101820";
            config.Theme.Dark["base-color"] = "#dde3ea";

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("html.dark { --blog-bg: #101820; --blog-ink: #dde3ea; }"));
            Assert.That(html, Contains.Substring("id=\"theme-toggle\""));
            Assert.That(html, Does.Not.Contain("localStorage.theme = 'light'"));
        }

        [Test]
        public void DocsMode_KeepsThemeToggle()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("id=\"theme-toggle\""));
        }

        [Test]
        public void BlogMode_RichFooter_RendersColumnsSocialAndBadges()
        {
            var config = BlogConfig();
            config.Footer = new FooterConfig
            {
                Copyright = "&copy; {{ year }} Curiosity GmbH.",
                Social = new List<FooterSocialConfig>
                {
                    new FooterSocialConfig { Icon = "brands-twitter", Link = "https://x.com/c", Label = "X" }
                },
                Badges = new List<FooterBadgeConfig>
                {
                    new FooterBadgeConfig { Icon = "marker", Title = "Made in Germany", Description = "Built in Munich" }
                },
                Columns = new List<FooterColumnConfig>
                {
                    new FooterColumnConfig
                    {
                        Title = "Product",
                        Links = new List<LinkConfig> { new LinkConfig { Text = "Integrations", Link = "https://example.com/i" } }
                    }
                }
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            // Full-width dark mega footer with rounded top corners.
            Assert.That(html, Contains.Substring("rounded-t-[2rem]"));
            Assert.That(html, Contains.Substring(">Product</h3>"));
            Assert.That(html, Contains.Substring(">Integrations</a>"));
            Assert.That(html, Contains.Substring("Made in Germany"));
            Assert.That(html, Contains.Substring("fi fi-brands-twitter"));
            // {{ year }} is expanded.
            Assert.That(html, Contains.Substring($"{System.DateTime.Now.Year} Curiosity GmbH."));
            Assert.That(html, Does.Not.Contain("{{ year }}"));
        }

        [Test]
        public void BlogMode_NoRichFooterConfig_FallsBackToSlimFooter()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            // No columns/social/badges → no dark mega footer.
            Assert.That(html, Does.Not.Contain("rounded-t-[2rem]"));
        }

        [Test]
        public void BlogMode_ClustersNavLinksNextToLogo()
        {
            var config = BlogConfig();
            config.Links = new List<LinkConfig>
            {
                new LinkConfig { Text = "Product", Link = "/product" },
                new LinkConfig { Text = "Pricing", Link = "/pricing" }
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            // The links group carries the left-cluster margin (curiosity.ai layout),
            // and the links render before the header action buttons. The wide
            // marketing nav collapses into the hamburger below `lg`.
            Assert.That(html, Contains.Substring("lg:ml-1.5"));
            var linkIdx = html.IndexOf(">Product</a>");
            var actionIdx = html.IndexOf(">Book a Demo</a>");
            Assert.That(linkIdx, Is.GreaterThan(0));
            Assert.That(linkIdx, Is.LessThan(actionIdx), "nav links should render before the action buttons");
        }

        [Test]
        public void BlogMode_LogoIsShrinkProof_SoCrowdedNavDoesNotCompressIt()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            // The wordmark link and its image carry `shrink-0` so a crowded header
            // never squeezes the logo (the curiosity.ai compressed-logo bug).
            Assert.That(html, Contains.Substring("class=\"flex items-center shrink-0\" aria-label=\"Curiosity\""));
            Assert.That(html, Contains.Substring("h-[21px] shrink-0 w-auto"));
        }

        [Test]
        public void BlogMode_CollapsesNavIntoHamburgerBelowLg()
        {
            var config = BlogConfig();
            config.Links = new List<LinkConfig>
            {
                new LinkConfig { Text = "Product", Link = "/product" },
                new LinkConfig
                {
                    Text = "Solutions",
                    Items = new List<LinkConfig>
                    {
                        new LinkConfig { Text = "Knowledge Management", Link = "/km" }
                    }
                }
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            // Desktop links + CTA row only show from `lg` up (so they no longer
            // overflow under the CTA pills at tablet widths like ~900px).
            Assert.That(html, Contains.Substring("hidden lg:flex items-center gap-[22px]"));
            Assert.That(html, Contains.Substring("gap-2.5 hidden lg:flex"));

            // A hamburger button appears below `lg` to open the mobile menu.
            Assert.That(html, Contains.Substring("id=\"blog-menu-btn\""));
            Assert.That(html, Contains.Substring("class=\"lg:hidden flex items-center justify-center"));

            // The mobile menu panel carries the links (flyout items flattened) and the
            // CTA pills so the whole marketing nav is reachable on small screens.
            Assert.That(html, Contains.Substring("id=\"blog-mobile-menu\""));
            var menuIdx = html.IndexOf("id=\"blog-mobile-menu\"");
            var tail = html.Substring(menuIdx);
            Assert.That(tail, Contains.Substring(">Product</a>"));
            Assert.That(tail, Contains.Substring(">Knowledge Management</a>"));
            Assert.That(tail, Contains.Substring(">Book a Demo</a>"));

            // And the toggle script is wired up.
            Assert.That(html, Contains.Substring("getElementById('blog-menu-btn')"));
        }

        [Test]
        public void DocsMode_HasNoBlogHamburger()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Does.Not.Contain("id=\"blog-menu-btn\""));
            Assert.That(html, Does.Not.Contain("id=\"blog-mobile-menu\""));
        }

        [Test]
        public void DocsMode_DoesNotClusterNavLinks()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            config.Links = new List<LinkConfig> { new LinkConfig { Text = "Product", Link = "/product" } };

            var html = new HtmlGenerator(config).Generate(Doc());

            // Docs mode keeps the centred links group (no left-cluster margin).
            Assert.That(html, Does.Not.Contain("md:ml-6"));
        }

        [Test]
        public void BlogMode_DoesNotCapContentRow_SoFooterIsFullBleed()
        {
            // Default layout.maxWidth is "screen-2xl". In blog mode the content row
            // must NOT be capped, so `main` runs full-width and the marketing footer
            // spans the pane edge-to-edge.
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            Assert.That(html, Contains.Substring("<div class=\"flex flex-1 overflow-hidden\">"));
        }

        [Test]
        public void BlogIndex_UsesWiderContentColumn_ForCardGridAndSearch()
        {
            // The blog index (cards + search) gets a roomier max-w-6xl column to
            // match curiosity.ai/resources/blog; article reading width is unchanged.
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var indexHtml = new HtmlGenerator(BlogConfig()).Generate(doc, currentUrl: "/blog/index");
            Assert.That(indexHtml, Contains.Substring("max-w-6xl grow w-full mx-auto prose"));

            // A regular blog post keeps the comfortable max-w-4xl reading column.
            var postDoc = new ParsedDocument
            {
                Html = "<p>Body</p>",
                FrontMatter = new FrontMatter { Title = "A Post" }
            };
            var postHtml = new HtmlGenerator(BlogConfig()).Generate(postDoc, currentUrl: "/blog/a-post");
            Assert.That(postHtml, Contains.Substring("max-w-4xl grow w-full mx-auto prose"));
            Assert.That(postHtml, Does.Not.Contain("max-w-6xl grow w-full mx-auto prose"));
        }

        [Test]
        public void BlogCard_Arrow_RotatesOnHover()
        {
            // A small right-pointing arrow rotates 45° counter-clockwise on card hover
            // (over 300ms), swinging from "→" to "↗" like curiosity.ai/resources/blog.
            // `group` lives on the card <a>.
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Post", Author = "Curiosity", Date = "May 7, 2026" } }, "/blog/post")
            };

            var html = new HtmlGenerator(BlogConfig()).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            Assert.That(html, Contains.Substring("fi-rr-arrow-small-right"));
            Assert.That(html, Contains.Substring("transition-transform"));
            Assert.That(html, Contains.Substring("duration-300"));
            Assert.That(html, Contains.Substring("group-hover:-rotate-45"));
        }

        [Test]
        public void DocsMode_CapsContentRowAtMaxWidth()
        {
            var config = BlogConfig();
            config.Mode = "docs";
            var html = new HtmlGenerator(config).Generate(Doc());

            // Docs mode keeps the capped, centred content row.
            Assert.That(html, Contains.Substring("flex flex-1 overflow-hidden max-w-screen-2xl mx-auto w-full"));
        }

        [Test]
        public void Actions_AreInheritedFromParentWhenChildHasNone()
        {
            var parent = BlogConfig();
            var child = new NekoConfig { Mode = "blog", Branding = new BrandingConfig { Title = "Curiosity" } };

            child.MergeWith(parent);

            Assert.That(child.Actions, Is.Not.Null);
            Assert.That(child.Actions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Mode_IsInheritedWhenChildLeftDefault()
        {
            var parent = new NekoConfig { Mode = "blog" };
            var child = new NekoConfig(); // default "docs"

            child.MergeWith(parent);

            Assert.That(child.Mode, Is.EqualTo("blog"));
        }

        [Test]
        public void BlogMode_PinsHeaderChromeToInter()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            // The marketing header is always rendered in Inter (the @supports path
            // switches to the variable 'Inter var' family on modern browsers), so a
            // brand body font never bleeds into the nav links / CTA pills.
            Assert.That(html, Contains.Substring("header { font-family: 'Inter', sans-serif; }"));
            Assert.That(html, Contains.Substring("header { font-family: 'Inter var', sans-serif; }"));
        }

        [Test]
        public void BlogMode_SelfHostedFont_DoesNotPullInterFromCdn()
        {
            // A site that self-hosts its fonts via `theme.font.url` owns the header
            // font in its own stylesheet (curiosity.ai's blog maps the header to
            // Inter *Display* Medium with `header { font-family: 'InterDisplay', 'Inter' }`
            // for pixel-exact chrome). Blog mode must therefore neither pull the
            // rsms.me copy nor emit its own `header { font-family: ... }` override —
            // that override would win by source order yet resolve to nothing (the
            // self-hosted sheet never defines 'Inter var'), dropping the header to the
            // system sans-serif. The engine defers to the self-hosted sheet instead.
            var config = BlogConfig();
            config.Theme.Font = new FontConfig
            {
                Family = "Plus Jakarta Sans",
                Url = "/assets/fonts/fonts.css"
            };

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Does.Not.Contain("https://rsms.me/inter/inter.css"));
            // The engine does not force a header font; the self-hosted sheet owns it.
            Assert.That(html, Does.Not.Contain("header { font-family: 'Inter var', sans-serif; }"));
            Assert.That(html, Does.Not.Contain("header { font-family: 'Inter', sans-serif; }"));
            Assert.That(html, Contains.Substring("href=\"/assets/fonts/fonts.css\""));
        }

        [Test]
        public void BlogMode_CustomFontWithoutUrl_StillLoadsInterForChrome()
        {
            // When a brand font is set but NOT self-hosted (no url), there is no local
            // source for the header's Inter, so blog mode falls back to the rsms.me
            // copy to keep the chrome in Inter.
            var config = BlogConfig();
            config.Theme.Font = new FontConfig { Family = "Georgia" };

            var html = new HtmlGenerator(config).Generate(Doc());

            Assert.That(html, Contains.Substring("https://rsms.me/inter/inter.css"));
            Assert.That(html, Contains.Substring("header { font-family: 'Inter var', sans-serif; }"));
        }

        [Test]
        public void BlogMode_ShowsTagsVisiblyOnCards()
        {
            var config = BlogConfig();
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Rel", Tags = new[] { "release news" } } }, "/blog/rel"),
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Guide", Tags = new[] { "guides" } } }, "/blog/guide")
            };

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // Beyond the hidden data-tags attribute used for filtering, each card
            // renders its tags as a visible pill so the bucket is clear at a glance.
            // The pill uses the inverted blog palette (bg-coloured chip on the ink card).
            Assert.That(html, Contains.Substring("color:var(--blog-ink, #1f1f1f)\">release news</span>"));
            Assert.That(html, Contains.Substring("color:var(--blog-ink, #1f1f1f)\">guides</span>"));
        }

        private static (ParsedDocument doc, List<(ParsedDocument, string)> posts) BlogIndexFixture()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Intro</p>",
                FrontMatter = new FrontMatter { Title = "Blog", Layout = "blog" }
            };
            var posts = new List<(ParsedDocument, string)>
            {
                (new ParsedDocument { FrontMatter = new FrontMatter { Title = "Post" } }, "/blog/post")
            };
            return (doc, posts);
        }

        [Test]
        public void BlogIndex_RendersConfiguredPillTitleAndDescription()
        {
            var config = BlogConfig();
            config.Blog = new BlogConfig
            {
                Pill = "Blog",
                Title = "Notes on how we build AI products",
                Description = "Product updates and release overviews."
            };
            var (doc, posts) = BlogIndexFixture();

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            // The pill renders as a rounded label, the title as the index <h1>, and
            // the description as a lead paragraph — all above the inline search box.
            Assert.That(html, Contains.Substring(">Blog</span>"));
            Assert.That(html, Contains.Substring(">Notes on how we build AI products</h1>"));
            Assert.That(html, Contains.Substring(">Product updates and release overviews.</p>"));
            Assert.That(html.IndexOf(">Notes on how we build AI products</h1>"),
                Is.LessThan(html.IndexOf("id=\"neko-inline-search\"")));
        }

        [Test]
        public void BlogIndex_WithoutBlogConfig_OmitsHero()
        {
            var (doc, posts) = BlogIndexFixture();

            // Default config has an empty Blog block → no hero, index opens on search.
            var html = new HtmlGenerator(BlogConfig()).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            Assert.That(html, Does.Not.Contain("rounded-full border border-gray-300"));
            Assert.That(html, Contains.Substring("id=\"neko-inline-search\""));
        }

        [Test]
        public void BlogIndex_TitleOnly_OmitsPill()
        {
            var config = BlogConfig();
            config.Blog = new BlogConfig { Title = "Just a title" };
            var (doc, posts) = BlogIndexFixture();

            var html = new HtmlGenerator(config).Generate(doc, blogPosts: posts, currentUrl: "/blog/index");

            Assert.That(html, Contains.Substring(">Just a title</h1>"));
            // No pill markup when `pill` is unset.
            Assert.That(html, Does.Not.Contain("rounded-full border border-gray-300"));
        }

        [Test]
        public void BlogConfig_IsInheritedPerFieldFromParent()
        {
            var parent = new NekoConfig { Mode = "blog", Blog = new BlogConfig { Pill = "Blog", Title = "Parent title" } };
            var child = new NekoConfig { Mode = "blog", Blog = new BlogConfig { Title = "Child title" } };

            child.MergeWith(parent);

            // The child keeps its own title but inherits the pill it didn't set.
            Assert.That(child.Blog.Title, Is.EqualTo("Child title"));
            Assert.That(child.Blog.Pill, Is.EqualTo("Blog"));
        }
    }
}
