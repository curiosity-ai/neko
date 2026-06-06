using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class QuizTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestSingleAnswerQuiz()
        {
            var markdown =
                "```quiz\n" +
                "title: Check yourself\n" +
                "questions:\n" +
                "  - q: \"What is 2 + 2?\"\n" +
                "    options:\n" +
                "      - \"3\"\n" +
                "      - \"4\"\n" +
                "    answer: 1\n" +
                "    explain: \"Two plus two is four.\"\n" +
                "```";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("data-neko-quiz=\""));
            Assert.That(doc.Html, Contains.Substring("data-quiz-total=\"1\""));
            Assert.That(doc.Html, Contains.Substring("Check yourself"));
            Assert.That(doc.Html, Contains.Substring("What is 2 + 2?"));
            // Correct answer index recorded for client-side grading.
            Assert.That(doc.Html, Contains.Substring("data-quiz-correct=\"1\""));
            // Single answer -> radio input.
            Assert.That(doc.Html, Contains.Substring("type=\"radio\""));
            Assert.That(doc.Html, Contains.Substring("Two plus two is four."));
            Assert.That(doc.Html, Contains.Substring("data-quiz-check"));
        }

        [Test]
        public void TestMultiAnswerUsesCheckboxes()
        {
            var markdown =
                "```quiz\n" +
                "questions:\n" +
                "  - q: \"Which are prime?\"\n" +
                "    options:\n" +
                "      - \"2\"\n" +
                "      - \"4\"\n" +
                "      - \"5\"\n" +
                "    answers: [0, 2]\n" +
                "```";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("type=\"checkbox\""));
            Assert.That(doc.Html, Contains.Substring("data-quiz-correct=\"0,2\""));
            // Default title when none supplied.
            Assert.That(doc.Html, Contains.Substring("Check yourself"));
        }

        [Test]
        public void TestEmptyQuizDegradesGracefully()
        {
            var markdown = "```quiz\ntitle: Nothing here\n```";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("Quiz has no questions."));
        }
    }
}
