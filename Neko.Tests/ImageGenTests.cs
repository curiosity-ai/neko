using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Neko.Builder;
using Neko.Extensions;
using Markdig;
using Markdig.Syntax;

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

        private static ImageGenInline ParseFirstInline(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().Use<ImageGenExtension>().Build();
            var doc = Markdig.Markdown.Parse(markdown, pipeline);
            return doc.Descendants<ImageGenInline>().First();
        }

        [Test]
        public void InlineCapturesPromptOnSeparateLine()
        {
            var inline = ParseFirstInline("[!img-gen\nA cozy cat reading a book\n]");
            Assert.That(inline.Prompt, Is.EqualTo("A cozy cat reading a book"));
            Assert.That(inline.Options, Is.Empty);
        }

        [Test]
        public void InlineCapturesLongMultilinePrompt()
        {
            var prompt = "Compose a two-column slide on a light background. Left column: a rounded comparison table with rows 'Small', 'Medium', 'Large' and columns 'Latency', 'Cost / 1K tokens', 'Use for', with subtle blue bars showing relative magnitude. Right column: a clean dashboard wireframe with three rounded tiles — a sparkline labelled 'p95 latency', a bar chart labelled 'tokens / day' and a gauge labelled 'error rate' — plus a thin filter bar above. Use Curiosity's blue accents, thin lines, a faint network background and a clean modern feel.";
            var inline = ParseFirstInline("[!img-gen\n" + prompt + "\n]");
            Assert.That(inline.Prompt, Is.EqualTo(prompt));
            Assert.That(inline.Options, Is.Empty);
        }

        [Test]
        public void InlineCapturesAttributesAndPromptOnSeparateLines()
        {
            var inline = ParseFirstInline("[!img-gen size=1024x1024 quality=high\nA diagram of static site generation\n]");
            Assert.That(inline.Prompt, Is.EqualTo("A diagram of static site generation"));
            Assert.That(inline.Options["size"], Is.EqualTo("1024x1024"));
            Assert.That(inline.Options["quality"], Is.EqualTo("high"));
        }

        [Test]
        public void InlineCapturesPromptSpanningSeveralLines()
        {
            var inline = ParseFirstInline("[!img-gen\nLine one.\nLine two.\nLine three.\n]");
            Assert.That(inline.Prompt, Does.Contain("Line one."));
            Assert.That(inline.Prompt, Does.Contain("Line two."));
            Assert.That(inline.Prompt, Does.Contain("Line three."));
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
        public void DirectiveWithPromptSpanningSeveralLinesParses()
        {
            var markdown =
                "Before\n\n" +
                "[!img-gen\n" +
                "Compose a two-column slide.\n" +
                "Left column: comparison table.\n" +
                "Right column: dashboard wireframe.\n" +
                "Use blue accents.\n" +
                "]\n\n" +
                "After";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Does.Contain("Before"));
            Assert.That(doc.Html, Does.Contain("After"));
            Assert.That(doc.Html, Does.Not.Contain("img-gen"));
            Assert.That(doc.Html, Does.Not.Contain("comparison table"));
            Assert.That(doc.Html, Does.Not.Contain("dashboard wireframe"));
        }

        [Test]
        public void RegexFindsLongMultilineDirective()
        {
            var content =
                "Some intro paragraph.\n\n" +
                "[!img-gen\n" +
                "Compose a two-column slide on a light background. Left column: a rounded comparison table with rows 'Small', 'Medium', 'Large' and columns 'Latency', 'Cost / 1K tokens', 'Use for', with subtle blue bars showing relative magnitude. Right column: a clean dashboard wireframe with three rounded tiles — a sparkline labelled 'p95 latency', a bar chart labelled 'tokens / day' and a gauge labelled 'error rate' — plus a thin filter bar above. Use Curiosity's blue accents, thin lines, a faint network background and a clean modern feel.\n" +
                "]\n\n" +
                "Closing.\n";

            var regex = new Regex(@"\[!img-gen(?<body>(?:[^\[\]]|\[(?<o>)|\](?<-o>))*(?(o)(?!)))\]",
                RegexOptions.Compiled | RegexOptions.Singleline);

            var matches = regex.Matches(content);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Groups["body"].Value, Does.Contain("two-column slide"));
            Assert.That(matches[0].Groups["body"].Value, Does.Contain("p95 latency"));
            Assert.That(matches[0].Groups["body"].Value, Does.Contain("clean modern feel"));
        }

        [Test]
        public void DirectiveWithLongMultilinePromptParses()
        {
            var markdown =
                "Before\n\n" +
                "[!img-gen\n" +
                "Compose a two-column slide on a light background. Left column: a rounded comparison table with rows 'Small', 'Medium', 'Large' and columns 'Latency', 'Cost / 1K tokens', 'Use for', with subtle blue bars showing relative magnitude. Right column: a clean dashboard wireframe with three rounded tiles — a sparkline labelled 'p95 latency', a bar chart labelled 'tokens / day' and a gauge labelled 'error rate' — plus a thin filter bar above. Use Curiosity's blue accents, thin lines, a faint network background and a clean modern feel.\n" +
                "]\n\n" +
                "After";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Does.Contain("Before"));
            Assert.That(doc.Html, Does.Contain("After"));
            Assert.That(doc.Html, Does.Not.Contain("img-gen"));
            Assert.That(doc.Html, Does.Not.Contain("two-column slide"));
            Assert.That(doc.Html, Does.Not.Contain("p95 latency"));
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
