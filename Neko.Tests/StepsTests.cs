using NUnit.Framework;
using Neko.Builder;
using System.Text.RegularExpressions;

namespace Neko.Tests
{
    public class StepsTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void SingleStep_RendersGroupAndStep()
        {
            var markdown = ">>> Install\n\nRun the installer.\n\n>>>\n";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("class=\"steps"));
            Assert.That(doc.Html, Contains.Substring("class=\"step relative"));
            Assert.That(doc.Html, Contains.Substring("Install"));
            Assert.That(doc.Html, Contains.Substring("Run the installer."));
        }

        [Test]
        public void Steps_AreNumberedSequentially()
        {
            var markdown = ">>> First\n\nA\n\n>>> Second\n\nB\n\n>>> Third\n\nC\n\n>>>\n";
            var doc = _parser.Parse(markdown);

            // Each step renders a circle marker containing its 1-based index.
            Assert.That(doc.Html, Does.Match(">1<"));
            Assert.That(doc.Html, Does.Match(">2<"));
            Assert.That(doc.Html, Does.Match(">3<"));

            int stepCount = Regex.Matches(doc.Html, "class=\"step relative").Count;
            Assert.That(stepCount, Is.EqualTo(3));
        }

        [Test]
        public void Steps_HasOnlyOneGroupWrapper()
        {
            var markdown = ">>> A\n\nfoo\n\n>>> B\n\nbar\n\n>>>\n";
            var doc = _parser.Parse(markdown);

            int groupCount = Regex.Matches(doc.Html, "class=\"steps").Count;
            Assert.That(groupCount, Is.EqualTo(1));
        }

        [Test]
        public void StepTitle_SupportsInlineMarkdown()
        {
            var markdown = ">>> **Bold** title\n\nbody\n\n>>>\n";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<strong>Bold</strong>"));
            Assert.That(doc.Html, Contains.Substring("title"));
        }

        [Test]
        public void StepBody_SupportsBlockMarkdown()
        {
            var markdown = ">>> With code\n\n```bash\nls -la\n```\n\n>>>\n";
            var doc = _parser.Parse(markdown);

            // Code fence content should be rendered inside the step.
            Assert.That(doc.Html, Contains.Substring("ls -la"));
            Assert.That(doc.Html, Contains.Substring("class=\"step-content"));
        }

        [Test]
        public void Steps_AreNotCreated_WithoutTrailingClose()
        {
            // Even without a trailing >>>, the group should still render at end-of-document.
            var markdown = ">>> Only\n\ncontent\n";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("class=\"steps"));
            Assert.That(doc.Html, Contains.Substring("Only"));
            Assert.That(doc.Html, Contains.Substring("content"));
        }
    }
}
