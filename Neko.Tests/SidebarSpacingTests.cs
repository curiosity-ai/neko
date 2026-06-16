using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    public class SidebarSpacingTests
    {
        private HtmlGenerator _generator;
        private NekoConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Test Docs" }
            };
            _generator = new HtmlGenerator(_config);
        }

        [Test]
        public void TestSidebarSectionSpacing()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var sidebar = new List<LinkConfig>
            {
                new LinkConfig
                {
                    Text = "Section 1",
                    Items = new List<LinkConfig>
                    {
                        new LinkConfig { Text = "Item 1", Link = "/item1" }
                    }
                },
                new LinkConfig
                {
                    Text = "Section 2",
                    Items = new List<LinkConfig>
                    {
                        new LinkConfig { Text = "Item 2", Link = "/item2" }
                    }
                }
            };

            var html = _generator.Generate(doc, sidebarLinks: sidebar);

            // Top-level sections render as collapsible <details> (expanded by
            // default via `open`), keeping the section spacing on the <li> and
            // the uppercase header styling on the <summary> label.
            Assert.That(html, Contains.Substring("<li class=\"first:mt-0 \" style=\"margin-top:1.2rem;margin-bottom:0.5rem;\">"));
            Assert.That(html, Contains.Substring("<details class=\"sidebar-section group\" data-section-key=\"Section 2\" open>"));
            Assert.That(html, Contains.Substring("<span class=\"text-xs font-bold text-gray-500 uppercase tracking-wider\">Section 2</span>"));
        }
    }
}
