using System.IO;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    public class CheckLinksTests
    {
        private string _root = null!;

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(TestContext.CurrentContext.TestDirectory, "CheckLinksSample");
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
            Directory.CreateDirectory(_root);

            File.WriteAllText(Path.Combine(_root, "neko.yml"),
                "url: https://example.com\nbranding:\n  title: T\n");
            File.WriteAllText(Path.Combine(_root, "good.md"),
                "# Good Page\n\nSome content with a heading to anchor at.\n");
        }

        private string SampleDir => _root;

        private void WriteIndex(string body) =>
            File.WriteAllText(Path.Combine(_root, "index.md"), "# Home\n\n" + body + "\n");

        [Test]
        public async Task CleanSite_ReturnsZero()
        {
            // A valid page link and a valid anchor into that page.
            WriteIndex("[Good](good.md)\n\n[Good anchor](good.md#good-page)\n");

            var exit = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: true).RunAsync();

            Assert.That(exit, Is.EqualTo(0), "A site with only valid links should pass.");
        }

        [Test]
        public async Task MissingPage_IsReported()
        {
            WriteIndex("[Missing](/no/such/page)\n\n[Good](good.md)\n");

            var exit = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: true).RunAsync();

            Assert.That(exit, Is.EqualTo(1), "A link to a non-existent page should be flagged.");
        }

        [Test]
        public async Task BrokenAnchor_RespectsAnchorFlag()
        {
            // Links resolve, but the #fragment does not exist in the target page.
            WriteIndex("[Bad anchor](good.md#does-not-exist)\n");

            var withAnchors = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: true).RunAsync();
            Assert.That(withAnchors, Is.EqualTo(1), "A dangling #anchor should be flagged when anchor checking is on.");

            var withoutAnchors = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: false).RunAsync();
            Assert.That(withoutAnchors, Is.EqualTo(0), "--no-anchors should ignore dangling #anchors.");
        }

        [Test]
        public async Task RepeatedBrokenTarget_IsReportedOnce()
        {
            // The same broken target linked twice should collapse into a single
            // grouped finding with an occurrence count, not two separate lines.
            WriteIndex("[one](/missing-page)\n\n[two](/missing-page)\n");

            var original = System.Console.Out;
            var captured = new System.IO.StringWriter();
            System.Console.SetOut(captured);
            int exit;
            try
            {
                exit = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: true).RunAsync();
            }
            finally
            {
                System.Console.SetOut(original);
            }

            var output = captured.ToString();
            var occurrences = System.Text.RegularExpressions.Regex.Matches(output, @"✗ /missing-page\b").Count;

            Assert.That(exit, Is.EqualTo(1));
            Assert.That(occurrences, Is.EqualTo(1), "A repeated target should be grouped into one finding.");
            Assert.That(output, Does.Contain("2 reference(s)"), "The occurrence count should be reported.");
        }

        [Test]
        public async Task ExternalLinks_AreSkippedByDefault()
        {
            // An unreachable external host must not fail the build unless --external is set.
            WriteIndex("[External](https://this-host-does-not-exist.invalid/page)\n");

            var exit = await new CheckLinksCommand(SampleDir, checkExternal: false, checkAnchors: true).RunAsync();

            Assert.That(exit, Is.EqualTo(0), "External links should not be contacted without --external.");
        }
    }
}
