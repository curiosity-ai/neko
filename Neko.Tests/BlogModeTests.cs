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
            // Primary = solid fill, outline = bordered.
            Assert.That(html, Contains.Substring("rounded-full bg-gray-900"));
            Assert.That(html, Contains.Substring("rounded-full border border-gray-900"));
            // `target: blank` is normalised to a valid _blank target.
            Assert.That(html, Contains.Substring("target=\"_blank\""));
        }

        [Test]
        public void BlogMode_OmitsThemeToggle_AndForcesLight()
        {
            var html = new HtmlGenerator(BlogConfig()).Generate(Doc());

            Assert.That(html, Does.Not.Contain("id=\"theme-toggle\""));
            Assert.That(html, Contains.Substring("localStorage.theme = 'light'"));
            // Light page background.
            Assert.That(html, Contains.Substring("<body class=\"bg-gray-100"));
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
