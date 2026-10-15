using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class TableCodePipeTests
    {
        [Test]
        public void EscapedPipeInACodeSpanInATableRendersAsAPipe()
        {
            var parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            var doc = parser.Parse("| Type | Note |\n| --- | --- |\n| `string \\| null` | a \\| b |\n");

            Assert.That(doc.Html, Contains.Substring("<code>string | null</code>"));
            Assert.That(doc.Html, Does.Not.Contain("\\|"));
        }

        [Test]
        public void EscapedPipeInACodeSpanOutsideATableIsLeftAlone()
        {
            var parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            var doc = parser.Parse("Run `a \\| b`.\n");

            Assert.That(doc.Html, Contains.Substring("<code>a \\| b</code>"));
        }
    }
}
