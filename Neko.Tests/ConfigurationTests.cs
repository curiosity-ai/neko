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
