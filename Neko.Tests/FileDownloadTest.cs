using NUnit.Framework;
using Neko.Builder;
using Neko.Extensions;

namespace Neko.Tests
{
    [TestFixture]
    public class FileDownloadTest
    {
        [Test]
        public void TestFileSimple()
        {
            var parser = new MarkdownParser();
            var markdown = "[!file](/static/sample.txt)";
            var doc = parser.Parse(markdown);

            // Should contain link to /static/sample.txt
            Assert.That(doc.Html, Contains.Substring("href=\"/static/sample.txt\""));
            // Should contain default text (filename) "sample.txt"
            Assert.That(doc.Html, Contains.Substring("sample.txt"));
        }

        [Test]
        public void TestFileWithText()
        {
            var parser = new MarkdownParser();
            var markdown = "[!file Sample](/static/sample.txt)";
            var doc = parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("href=\"/static/sample.txt\""));
            Assert.That(doc.Html, Contains.Substring("Sample"));
        }

        [Test]
        public void TestFileWithIcon()
        {
            var parser = new MarkdownParser();
            var markdown = "[!file icon=\"rocket\"](/static/sample.txt)";
            var doc = parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("href=\"/static/sample.txt\""));
            // Should contain rocket icon
            Assert.That(doc.Html, Contains.Substring("fi-rr-rocket"));
        }
    }
}
