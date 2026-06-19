using NUnit.Framework;
using Neko.Builder;
using Neko.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Neko.Tests
{
    public class LayoutTests
    {
        [Test]
        public void TestLayoutConfigParsing()
        {
            var yaml = @"
layout:
  sidebar: false
  toc: false
";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, yaml);

            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Layout.Sidebar, Is.False);
                Assert.That(config.Layout.Toc, Is.False);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void TestLayoutConfigDefaults()
        {
            var yaml = @"
input: .
";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, yaml);

            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Layout.Sidebar, Is.True);
                Assert.That(config.Layout.Toc, Is.True);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void TestSidebarGeneration()
        {
            var config = new NekoConfig();
            config.Layout.Sidebar = true;
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument { Html = "Content" };

            var html = generator.Generate(doc);

            Assert.That(html, Contains.Substring("id=\"mobile-menu-btn\""));
            Assert.That(html, Contains.Substring("id=\"sidebar\""));
        }

        [Test]
        public void TestSidebarDisabledGeneration()
        {
            var config = new NekoConfig();
            config.Layout.Sidebar = false;
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument { Html = "Content" };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Not.Contain("id=\"mobile-menu-btn\""));
            Assert.That(html, Does.Not.Contain("id=\"sidebar\""));
        }

        [Test]
        public void TestTocGeneration()
        {
            var config = new NekoConfig();
            config.Layout.Toc = true;
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument
            {
                Html = "Content",
                Toc = new List<TocItem>
                {
                    new TocItem { Id = "test", Title = "Test", Level = 2 }
                }
            };

            var html = generator.Generate(doc);

            Assert.That(html, Contains.Substring("id=\"toc-sidebar\""));
        }

        [Test]
        public void TestTocDisabledGeneration()
        {
            var config = new NekoConfig();
            config.Layout.Toc = false;
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument
            {
                Html = "Content",
                Toc = new List<TocItem>
                {
                    new TocItem { Id = "test", Title = "Test", Level = 2 }
                }
            };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Not.Contain("id=\"toc-sidebar\""));
        }

        [Test]
        public void TestMaxWidthDefault()
        {
            var yaml = @"
input: .
";
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, yaml);

            try
            {
                var config = ConfigParser.Parse(tempFile);
                Assert.That(config.Layout.MaxWidth, Is.EqualTo("screen-2xl"));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Test]
        public void TestMaxWidthCapsLayoutByDefault()
        {
            var config = new NekoConfig();
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument { Html = "Content" };

            var html = generator.Generate(doc);

            // The body row, header content, and pivot row share the centred cap.
            Assert.That(html, Contains.Substring("flex flex-1 overflow-hidden max-w-screen-2xl mx-auto w-full"));
            Assert.That(html, Contains.Substring("max-w-screen-2xl mx-auto"));
        }

        [Test]
        public void TestMaxWidthFullDisablesCap()
        {
            var config = new NekoConfig();
            config.Layout.MaxWidth = "full";
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument { Html = "Content" };

            var html = generator.Generate(doc);

            Assert.That(html, Does.Not.Contain("max-w-screen-2xl"));
            Assert.That(html, Contains.Substring("flex flex-1 overflow-hidden\""));
        }

        [Test]
        public void TestMaxWidthRawCssLengthBecomesArbitraryValue()
        {
            var config = new NekoConfig();
            config.Layout.MaxWidth = "1600px";
            var generator = new HtmlGenerator(config);
            var doc = new ParsedDocument { Html = "Content" };

            var html = generator.Generate(doc);

            Assert.That(html, Contains.Substring("max-w-[1600px] mx-auto"));
        }
    }
}
