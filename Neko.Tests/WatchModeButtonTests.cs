using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;

namespace Neko.Tests
{
    [TestFixture]
    public class WatchModeButtonTests
    {
        [Test]
        public void Generate_InWatchMode_InjectsButtonAndClasses_NoExistingClass()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: true);
            var doc = new ParsedDocument { Html = "<h1>Title</h1><p>Content</p>" };

            var html = generator.Generate(doc);

            // Check for injected classes
            Assert.That(html, Does.Contain("class=\"flex justify-between\""));
            // Check for button injection
            Assert.That(html, Does.Contain(">Title<button"));
        }

        [Test]
        public void Generate_InWatchMode_InjectsButtonAndClasses_ExistingClass()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config, isWatchMode: true);
            var doc = new ParsedDocument { Html = "<h1 class=\"existing\">Title</h1><p>Content</p>" };

            var html = generator.Generate(doc);

            // Check that both classes exist
            Assert.That(html, Does.Contain("existing"));
            Assert.That(html, Does.Contain("flex justify-between"));
            // Check for button injection
            Assert.That(html, Does.Contain(">Title<button"));
        }
    }
}
