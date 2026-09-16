using System;
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

            // Verify the presence of scroll preservation logic. The position lives in
            // sessionStorage (it only means anything for the tab that is navigating)
            // and is tagged with a signature of the link set, so a position saved on a
            // sibling project's sidebar is never applied to this one.
            Assert.That(html, Contains.Substring("const scrollKey = 'test-docs-sidebar-scroll';"));
            Assert.That(html, Contains.Substring("sessionStorage.getItem(scrollKey)"));
            Assert.That(html, Contains.Substring("saved.sig === nekoSidebarSignature()"));

            // Check for debounce logic
            Assert.That(html, Contains.Substring("let timeout;"));
            Assert.That(html, Contains.Substring("clearTimeout(timeout);"));
            Assert.That(html, Contains.Substring("timeout = setTimeout(nekoSaveSidebarScroll, 100);"));
            Assert.That(html, Contains.Substring("sessionStorage.setItem(scrollKey, JSON.stringify({ top: sidebar.scrollTop"));

            // A click can navigate inside the debounce window, so the position is also
            // flushed on the way out.
            Assert.That(html, Contains.Substring("window.addEventListener('pagehide', nekoSaveSidebarScroll);"));

            // The reader's place is guaranteed by the entry for the current page being
            // on screen, not by a time-limited offset: an offset older than a minute
            // used to be dropped, which reset the sidebar after any real reading.
            Assert.That(html, Does.Not.Contain("60000"));
            Assert.That(html, Contains.Substring("nekoScrollActiveSidebarLinkIntoView();"));
        }

        [Test]
        public void TestSidebarScrollRestoreRunsAfterSectionState()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // The scroll restore measures the sidebar to decide whether the current
            // page's entry is on screen, so it has to run after the section-state
            // script has settled which <details> are open. Otherwise it measures a
            // fully expanded tree and lands on the wrong entry.
            var sectionState = html.IndexOf("const sectionStateKey", StringComparison.Ordinal);
            var scrollRestore = html.IndexOf("window.nekoRestoreSidebarScroll = function ()", StringComparison.Ordinal);
            Assert.That(sectionState, Is.GreaterThan(0));
            Assert.That(scrollRestore, Is.GreaterThan(sectionState),
                "the scroll restore must be emitted after the section-state script");
        }

        [Test]
        public void TestSidebarScrollRestoreExposedGlobally()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // The scroll-restore logic is exposed as a global so password.js can
            // re-apply it once the protected sidebar entries are revealed.
            Assert.That(html, Contains.Substring("window.nekoRestoreSidebarScroll = function ()"));
            Assert.That(html, Contains.Substring("window.nekoRestoreSidebarScroll();"));
        }

        [Test]
        public void TestNoCrossDocumentViewTransitionHandler()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            };

            var html = _generator.Generate(doc);

            // Navigation is a normal full page load — there is no cross-document view
            // transition, so a `pagereveal`/`skipTransition` handler would be dead
            // code (its `e.viewTransition` is always null). Protected pages avoid the
            // empty flash with a pre-paint content restore instead (see below).
            Assert.That(html, Does.Not.Contain("pagereveal"));
            Assert.That(html, Does.Not.Contain("skipTransition"));
        }

        [Test]
        public void TestProtectedPageRestoresBodyBeforePaint()
        {
            var doc = new ParsedDocument
            {
                Html = "<p>Secret content</p>",
                FrontMatter = new FrontMatter { Title = "Secret", Password = "letmein" }
            };

            var html = _generator.Generate(doc, currentUrl: "/secret");

            // A protected page paints empty until password.js decrypts it async; an
            // inline, pre-paint script injects the body from the sessionStorage
            // plaintext cache (populated on a previous decrypt or a hover prefetch)
            // so a revisited protected page renders content on the first frame.
            Assert.That(html, Contains.Substring("sessionStorage.getItem('neko-pw-html:' + location.pathname)"));
            Assert.That(html, Contains.Substring("window.__nekoProtectedInjected = true;"));
            // The cache is salt-checked so stale plaintext from a changed password is ignored.
            Assert.That(html, Contains.Substring("cached.salt !== '"));

            // A public page carries none of this.
            var publicHtml = _generator.Generate(new ParsedDocument
            {
                Html = "<p>Content</p>",
                FrontMatter = new FrontMatter { Title = "Page Title" }
            });
            Assert.That(publicHtml, Does.Not.Contain("__nekoProtectedInjected"));
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
