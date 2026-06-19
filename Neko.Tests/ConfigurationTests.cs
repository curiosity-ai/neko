using NUnit.Framework;
using Neko.Configuration;
using System.IO;

namespace Neko.Tests
{
    public class ConfigurationTests
    {
        [Test]
        public void TestParseDefaultConfig()
        {
            var configPath = "nonexistent.yml";
            var config = ConfigParser.Parse(configPath);
            Assert.That(config.Input, Is.EqualTo(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath) ?? string.Empty, "."))));
            Assert.That(config.Output, Is.EqualTo(".neko"));
            // Sidebar icons are hidden by default and must be opted into.
            Assert.That(config.Nav.Icons.Mode, Is.EqualTo("none"));
        }

        [Test]
        public void TestNavIconsModeParsesFromYaml()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "nav:\n  icons:\n    mode: all\n");
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Nav.Icons.Mode, Is.EqualTo("all"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void TestNavIconsModeInheritsFromParent()
        {
            var child = new NekoConfig();                 // default "none"
            var parent = new NekoConfig();
            parent.Nav.Icons.Mode = "folders";

            child.MergeWith(parent);

            Assert.That(child.Nav.Icons.Mode, Is.EqualTo("folders"));
        }

        [Test]
        public void TestLinkNormalizerStripsMdExtension()
        {
            Assert.That(LinkNormalizer.Normalize("/workspace/core-concepts/graph-model.md"),
                Is.EqualTo("/workspace/core-concepts/graph-model"));
            Assert.That(LinkNormalizer.Normalize("about.md"), Is.EqualTo("about"));
            Assert.That(LinkNormalizer.Normalize("page.html"), Is.EqualTo("page"));
        }

        [Test]
        public void TestLinkNormalizerPreservesFragmentsAndQueries()
        {
            Assert.That(LinkNormalizer.Normalize("/docs/page.md#section"),
                Is.EqualTo("/docs/page#section"));
            Assert.That(LinkNormalizer.Normalize("/docs/page.md?x=1"),
                Is.EqualTo("/docs/page?x=1"));
            Assert.That(LinkNormalizer.Normalize("/docs/page.md?x=1#section"),
                Is.EqualTo("/docs/page?x=1#section"));
        }

        [Test]
        public void TestLinkNormalizerLeavesExternalAndSpecialLinksAlone()
        {
            Assert.That(LinkNormalizer.Normalize("https://example.com/page.md"),
                Is.EqualTo("https://example.com/page.md"));
            Assert.That(LinkNormalizer.Normalize("mailto:foo@example.com"),
                Is.EqualTo("mailto:foo@example.com"));
            Assert.That(LinkNormalizer.Normalize("#anchor"), Is.EqualTo("#anchor"));
            Assert.That(LinkNormalizer.Normalize("/already/clean"), Is.EqualTo("/already/clean"));
            Assert.That(LinkNormalizer.Normalize(null), Is.Null);
            Assert.That(LinkNormalizer.Normalize(""), Is.EqualTo(""));
        }

        [Test]
        public void TestConfigParserNormalizesYamlLinks()
        {
            var yaml = @"
input: ./docs
links:
  - text: Graph
    link: /workspace/core-concepts/graph-model.md
    icon: code-branch
  - text: Guides
    items:
      - text: Getting started
        link: /guides/start.md
      - text: External
        link: https://example.com/page.md
    footerItems:
      - text: About
        link: about.html
banner:
  text: Hello
  link: /promo/announce.md
";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, yaml);

            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Links[0].Link, Is.EqualTo("/workspace/core-concepts/graph-model"));
                Assert.That(config.Links[1].Items[0].Link, Is.EqualTo("/guides/start"));
                Assert.That(config.Links[1].Items[1].Link, Is.EqualTo("https://example.com/page.md"));
                Assert.That(config.Links[1].FooterItems[0].Link, Is.EqualTo("about"));
                Assert.That(config.Banner.Link, Is.EqualTo("/promo/announce"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void TestParseSampleConfig()
        {
            var yaml = @"
input: ./docs
output: ./public
url: example.com
branding:
  title: My Docs
links:
  - text: Home
    link: /
";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, yaml);

            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Input, Is.EqualTo(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tempFile) ?? string.Empty, "./docs"))));
                Assert.That(config.Output, Is.EqualTo("./public"));
                Assert.That(config.Url, Is.EqualTo("example.com"));
                Assert.That(config.Branding.Title, Is.EqualTo("My Docs"));
                Assert.That(config.Links.Count, Is.EqualTo(1));
                Assert.That(config.Links[0].Text, Is.EqualTo("Home"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
