using NUnit.Framework;
using TailDocs.CLI.Configuration;
using System.IO;

namespace TailDocs.Tests
{
    public class ConfigurationTests
    {
        [Test]
        public void TestParseDefaultConfig()
        {
            var config = ConfigParser.Parse("nonexistent.yml");
            Assert.That(config.Input, Is.EqualTo("."));
            Assert.That(config.Output, Is.EqualTo(".taildocs"));
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
                Assert.That(config.Input, Is.EqualTo("./docs"));
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
