using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    public class HistoryFeatureTests
    {
        private HtmlGenerator _generator;
        private NekoConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new NekoConfig
            {
                Branding = new BrandingConfig { Title = "Test Docs" },
                Links = new List<LinkConfig>()
            };
            _generator = new HtmlGenerator(_config);
        }

        [Test]
        public void TestHistoryScriptInjection()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // Verify script injection
            Assert.That(html, Contains.Substring("<script defer src=\"/assets/history.js\"></script>"));
        }

        [Test]
        public void TestHistoryUIInjection()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // Verify Button
            Assert.That(html, Contains.Substring("<button id=\"history-btn\" onclick=\"toggleHistory()\""));
            Assert.That(html, Contains.Substring("<i class=\"fi fi-rr-clock text-lg\"></i>"));

            // Verify Popup Structure
            Assert.That(html, Contains.Substring("<div id=\"history-popup\""));
            Assert.That(html, Contains.Substring("Recent Pages"));
            Assert.That(html, Contains.Substring("<ul id=\"history-list\""));
        }
    }
}
