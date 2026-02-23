using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    public class HtmlGeneratorScriptTests
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
        public void TestSidebarScrollScriptInjection()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // Verify the presence of scroll preservation logic
            Assert.That(html, Contains.Substring("const scrollKey = 'test-docs-sidebar-scroll';"));
            Assert.That(html, Contains.Substring("const timeKey = 'test-docs-sidebar-scroll-time';"));
            Assert.That(html, Contains.Substring("localStorage.getItem(scrollKey)"));

            // Check for debounce logic
            Assert.That(html, Contains.Substring("let timeout;"));
            Assert.That(html, Contains.Substring("clearTimeout(timeout);"));
            Assert.That(html, Contains.Substring("setTimeout(() => {"));
            Assert.That(html, Contains.Substring("localStorage.setItem(scrollKey, sidebar.scrollTop);"));

            Assert.That(html, Contains.Substring("if (now - parseInt(savedTime) < 60000)"));
        }

        [Test]
        public void TestSidebarHighlightingLogic()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // Verify the highlighting logic includes boundary check and path normalization
            Assert.That(html, Contains.Substring("let currentPath = window.location.pathname;"));
            Assert.That(html, Contains.Substring("if (currentPath.endsWith('.html')) currentPath = currentPath.substring(0, currentPath.length - 5);"));
            Assert.That(html, Contains.Substring("if (href === currentPath || (href !== '/' && currentPath.startsWith(href) && (href.endsWith('/') || currentPath.charAt(href.length) === '/')))"));
        }

        [Test]
        public void TestPasteHandlerInjectionLocation()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: true);
            var doc = new ParsedDocument { Html = "<h1>Title</h1>" };

            var html = generator.Generate(doc);

            // Verify paste handler is present
            Assert.That(html, Contains.Substring("window.addEventListener('paste',"));

            // Verify it is inside nekoOpenEditor -> loadMonaco -> require
            var openEditorIndex = html.IndexOf("function nekoOpenEditor()");
            var createEditorIndex = html.IndexOf("editor = monaco.editor.create");
            var pasteHandlerIndex = html.IndexOf("window.addEventListener('paste',");

            Assert.That(openEditorIndex, Is.GreaterThan(-1), "nekoOpenEditor should be present");
            Assert.That(createEditorIndex, Is.GreaterThan(openEditorIndex), "monaco.editor.create should be inside nekoOpenEditor");
            Assert.That(pasteHandlerIndex, Is.GreaterThan(createEditorIndex), "Paste handler should be after editor creation");
        }
    }
}
