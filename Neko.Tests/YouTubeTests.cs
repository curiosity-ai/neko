using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    [TestFixture]
    public class YouTubeTests
    {
        [Test]
        public void TestYouTubeEmbed_RawUrl()
        {
            var markdown = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            var parser = new MarkdownParser();
            var result = parser.Parse(markdown);

            // Check if it renders an iframe
            Assert.That(result.Html, Does.Contain("<iframe"));
            Assert.That(result.Html, Does.Contain("youtube.com/embed/dQw4w9WgXcQ"));
        }

        [Test]
        public void TestYouTubeEmbed_ShortUrl()
        {
            var markdown = "https://youtu.be/dQw4w9WgXcQ";
            var parser = new MarkdownParser();
            var result = parser.Parse(markdown);

            Assert.That(result.Html, Does.Contain("<iframe"));
            Assert.That(result.Html, Does.Contain("youtube.com/embed/dQw4w9WgXcQ"));
        }

        [Test]
        public void TestYouTubeEmbed_StartParam()
        {
            var markdown = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&start=30";
            var parser = new MarkdownParser();
            var result = parser.Parse(markdown);

            Assert.That(result.Html, Does.Contain("<iframe"));
            Assert.That(result.Html, Does.Contain("start=30"));
        }

        [Test]
        public void TestYouTubeEmbed_TimestampParam()
        {
            var markdown = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=30s";
            var parser = new MarkdownParser();
            var result = parser.Parse(markdown);

            Assert.That(result.Html, Does.Contain("<iframe"));
            Assert.That(result.Html, Does.Contain("start=30"));
        }
    }
}
