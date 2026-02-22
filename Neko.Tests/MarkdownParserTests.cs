using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class MarkdownParserTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser();
        }

        [Test]
        public void TestParseBasicMarkdown()
        {
            var markdown = "# Hello World";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("<h1 id=\"hello-world\">Hello World</h1>"));
        }

        [Test]
        public void TestParseFrontMatter()
        {
            var markdown = @"---
title: My Title
icon: home
---
# Content";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.FrontMatter.Title, Is.EqualTo("My Title"));
            Assert.That(doc.FrontMatter.Icon, Is.EqualTo("home"));
            Assert.That(doc.Html, Contains.Substring("<h1 id=\"content\">Content</h1>"));
        }

        [Test]
        public void TestParseEmoji()
        {
            var markdown = "Hello :smile:";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("😄"));
        }
    }
}
