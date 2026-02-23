using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;

namespace Neko.Tests
{
    [TestFixture]
    public class WatchModeTests
    {
        [Test]
        public void Generate_InWatchMode_InjectsScriptsAndButton()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: true);
            var doc = new ParsedDocument { Html = "<h1>Title</h1><p>Content</p>" };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Contain("neko-live"));
            Assert.That(html, Does.Contain("monaco-editor"));
            Assert.That(html, Does.Contain("nekoOpenEditor()"));
            Assert.That(html, Does.Contain("neko-editor-modal"));
        }

        [Test]
        public void Generate_NotInWatchMode_DoesNotInjectScripts()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: false);
            var doc = new ParsedDocument { Html = "<h1>Title</h1><p>Content</p>" };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Not.Contain("neko-live"));
            Assert.That(html, Does.Not.Contain("monaco-editor"));
            Assert.That(html, Does.Not.Contain("nekoOpenEditor()"));
        }
    }
}
