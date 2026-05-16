using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Neko.Builder;
using Neko.Extensions;
using Markdig;

namespace Neko.Tests
{
    public class ImageGenTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void DirectiveProducesNoHtmlBeforeGeneration()
        {
            var markdown = "Before\n\n[!img-gen\nA cozy cat reading a book\n]\n\nAfter";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Does.Contain("Before"));
            Assert.That(doc.Html, Does.Contain("After"));
            Assert.That(doc.Html, Does.Not.Contain("img-gen"));
            Assert.That(doc.Html, Does.Not.Contain("cozy cat"));
        }

        [Test]
        public void DirectiveWithAttributesParses()
        {
            var markdown = "[!img-gen size=1024x1024 quality=high\nA diagram of static site generation\n]";
            var doc = _parser.Parse(markdown);

            // Should swallow the directive entirely, not render any of its content.
            Assert.That(doc.Html, Does.Not.Contain("size=1024x1024"));
            Assert.That(doc.Html, Does.Not.Contain("diagram"));
        }

        [Test]
        public void RegexMatchesMultilineDirective()
        {
            var content =
                "Intro\n\n" +
                "[!img-gen\n" +
                "A cozy cat sitting by a window during a rainstorm, watercolor style\n" +
                "]\n\n" +
                "Middle\n\n" +
                "[!img-gen size=1024x1024 quality=high\n" +
                "A diagram of how Neko builds a static site, isometric pixel art\n" +
                "]\n\n" +
                "End\n";

            var regex = new Regex(@"\[!img-gen(?<body>(?:[^\[\]]|\[(?<o>)|\](?<-o>))*(?(o)(?!)))\]",
                RegexOptions.Compiled | RegexOptions.Singleline);

            var matches = regex.Matches(content);
            Assert.That(matches.Count, Is.EqualTo(2));
            Assert.That(matches[0].Groups["body"].Value, Does.Contain("cozy cat"));
            Assert.That(matches[1].Groups["body"].Value, Does.Contain("size=1024x1024"));
            Assert.That(matches[1].Groups["body"].Value, Does.Contain("isometric pixel art"));
        }

        [Test]
        public void RegexHandlesSingleLineDirective()
        {
            var content = "Lorem [!img-gen A simple plain prompt with no attributes] ipsum";

            var regex = new Regex(@"\[!img-gen(?<body>(?:[^\[\]]|\[(?<o>)|\](?<-o>))*(?(o)(?!)))\]",
                RegexOptions.Compiled | RegexOptions.Singleline);

            var matches = regex.Matches(content);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Groups["body"].Value.Trim(), Is.EqualTo("A simple plain prompt with no attributes"));
        }

        [Test]
        public void AttributeParserSplitsKeysAndValues()
        {
            var attrs = ImageGenParser
                .ParseAttributes("size=1024x1024 quality=high style=\"vivid bright\"")
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            Assert.That(attrs["size"], Is.EqualTo("1024x1024"));
            Assert.That(attrs["quality"], Is.EqualTo("high"));
            Assert.That(attrs["style"], Is.EqualTo("vivid bright"));
        }

        [Test]
        public void CommandRefusesEmptyApiKey()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "neko_imggen_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "page.md"), "[!img-gen A test prompt]");
                var cmd = new ImageGenCommand(tempDir, apiKey: "", imageModel: "gpt-image-1", llmModel: "gpt-4o-mini");
                var exit = cmd.RunAsync().GetAwaiter().GetResult();
                Assert.That(exit, Is.EqualTo(1));
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }

        [Test]
        public void CommandRefusesMissingInputDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "neko_imggen_missing_" + System.Guid.NewGuid().ToString("N"));
            var cmd = new ImageGenCommand(tempDir, apiKey: "anything", imageModel: "gpt-image-1", llmModel: "gpt-4o-mini");
            var exit = cmd.RunAsync().GetAwaiter().GetResult();
            Assert.That(exit, Is.EqualTo(1));
        }
    }
}
