using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class StepTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestStepGroup()
        {
            var markdown = ">>> Step 1\nContent 1\n>>> Step 2\nContent 2\n>>>";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Step 1"));
            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("Step 2"));
            Assert.That(doc.Html, Contains.Substring("Content 2"));
            Assert.That(doc.Html, Contains.Substring("class=\"steps"));
        }

        [Test]
        public void TestStepGroupEnding()
        {
            var markdown = ">>> Step 1\nContent 1\n>>>\nAfter steps";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Content 1"));
            Assert.That(doc.Html, Contains.Substring("After steps"));
        }

        [Test]
        public void TestStepTitleMarkdown()
        {
            var markdown = ">>> Step **Bold**\nContent\n>>>";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<strong>Bold</strong>"));
        }

        // Regression: a step whose content ends in a list (no following blank line)
        // used to drop the next `>>>` delimiter and every subsequent step, because
        // the cursor was left inside the still-open list item rather than the step
        // group. All steps after the list must still render.
        [Test]
        public void TestStepWithListBeforeNextStep()
        {
            var markdown =
                ">>> First\n" +
                "Intro paragraph.\n\n" +
                "- alpha\n" +
                "- beta\n" +
                ">>> Second\n" +
                "Second step content.\n" +
                ">>> Third\n" +
                "Third step content.\n" +
                ">>>\n\n" +
                "After steps.";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("First"));
            Assert.That(doc.Html, Contains.Substring("Second"));
            Assert.That(doc.Html, Contains.Substring("Second step content."));
            Assert.That(doc.Html, Contains.Substring("Third"));
            Assert.That(doc.Html, Contains.Substring("Third step content."));
            Assert.That(doc.Html, Contains.Substring("After steps."));

            // The list belongs to the first step; the steps remain siblings, so the
            // step circle markers 1, 2 and 3 are all emitted.
            Assert.That(doc.Html, Contains.Substring(">1</div>"));
            Assert.That(doc.Html, Contains.Substring(">2</div>"));
            Assert.That(doc.Html, Contains.Substring(">3</div>"));
        }

        // Regression: a step ending in a list immediately before the closing `>>>`
        // must still close the group and keep the content that follows it.
        [Test]
        public void TestStepWithListBeforeClose()
        {
            var markdown =
                ">>> Only\n" +
                "intro.\n\n" +
                "- x\n" +
                "- y\n" +
                ">>>\n\n" +
                "closing content.";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Only"));
            Assert.That(doc.Html, Contains.Substring("<li>x</li>"));
            Assert.That(doc.Html, Contains.Substring("closing content."));
        }
    }
}
