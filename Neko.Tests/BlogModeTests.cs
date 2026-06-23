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
            Assert.That(html, Contains.Substring("border:1px solid var(--blog-ink);color:var(--blog-ink)"));  // outline = bordered
            // `target: blank` is normalised to a valid _blank target.
            Assert.That(html, Contains.Substring("target=\"_blank\""));
        }

        [Test]
        public void BlogMode_MovesSearchFromHeaderToContentList()
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

            // Exactly one search trigger, and it's the in-content one (above the grid).
            Assert.That(System.Text.RegularExpressions.Regex.Matches(html, "openSearch\\(\\)").Count, Is.EqualTo(1));
            Assert.That(html, Contains.Substring("Search the blog"));
            var searchIdx = html.IndexOf("Search the blog");
            var gridIdx = html.IndexOf("grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3");
            Assert.That(searchIdx, Is.GreaterThan(0));
            Assert.That(searchIdx, Is.LessThan(gridIdx), "search bar should render above the post grid");
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
            // and the links render before the header action buttons.
            Assert.That(html, Contains.Substring("md:ml-6"));
            var linkIdx = html.IndexOf(">Product</a>");
            var actionIdx = html.IndexOf(">Book a Demo</a>");
            Assert.That(linkIdx, Is.GreaterThan(0));
            Assert.That(linkIdx, Is.LessThan(actionIdx), "nav links should render before the action buttons");
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
    }
}
