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

            // Verify the shared matching helper is emitted and used by the active-link
            // highlighter. The helper canonicalises a folder index link (.../index)
            // against the folder URL it is served at, and restricts index links to an
            // exact match so they don't light up for sibling pages.
            Assert.That(html, Contains.Substring("function nekoSidebarLinkMatches(href, currentPath)"));
            Assert.That(html, Contains.Substring("function nekoCanonicalPath(p)"));
            Assert.That(html, Contains.Substring("if (p.endsWith('/index')) p = p.substring(0, p.length - 6) || '/';"));
            Assert.That(html, Contains.Substring("const isIndex = href.endsWith('/index') || href === '/index' || href.endsWith('/');"));
            Assert.That(html, Contains.Substring("if (nekoSidebarLinkMatches(href, currentPath)) {"));
        }

        [Test]
        public void TestPasteHandlerInjectionLocation()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: true);
            var doc = new ParsedDocument { Html = "<h1>Title</h1>" };

            var html = generator.Generate(doc);

            // Verify paste handler is present
            Assert.That(html, Contains.Substring("editor.getContainerDomNode().addEventListener('paste'"));

            // Verify it is inside nekoOpenEditor -> loadMonaco -> require
            var openEditorIndex = html.IndexOf("function nekoOpenEditor()");
            var createEditorIndex = html.IndexOf("editor = monaco.editor.create");
            var pasteHandlerIndex = html.IndexOf("editor.getContainerDomNode().addEventListener('paste'");

            var createEditorInsideOpenEditorIndex = html.IndexOf("editor = monaco.editor.create", openEditorIndex);

            Assert.That(openEditorIndex, Is.GreaterThan(-1), "nekoOpenEditor should be present");
            Assert.That(createEditorInsideOpenEditorIndex, Is.GreaterThan(openEditorIndex), "monaco.editor.create should be inside nekoOpenEditor");
            Assert.That(pasteHandlerIndex, Is.GreaterThan(createEditorInsideOpenEditorIndex), "Paste handler should be after editor creation");
        }
    }
}
