using System.IO;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class CSharpDocsUsagesTests
    {
        private MarkdownParser _parser;
        private string _root = "";
        private string _page = "";

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
            _root = Path.Combine(Path.GetTempPath(), "neko-usages-render-" + Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(_root, "api", "_usages"));
            _page = Path.Combine(_root, "api", "client.md");

            File.WriteAllText(Path.Combine(_root, "api", "_usages", "client.json"), @"{
  ""sources"": { ""demo"": { ""url"": ""https://github.com/acme/demo"", ""commit"": ""abc1234"" } },
  ""members"": {
    ""Client.Connect"": [
      { ""source"": ""demo"", ""file"": ""src/Program.cs"", ""line"": 7, ""start"": 6, ""end"": 8,
        ""url"": ""https://github.com/acme/demo/blob/abc1234/src/Program.cs#L6-L8"",
        ""code"": ""var client = Client\n    .Connect(\""https://example.com\"")\n    .WithTimeout(30);"" }
    ],
    ""Client.Send"": [
      { ""source"": ""demo"", ""file"": ""src/Program.cs"", ""line"": 10, ""start"": 10, ""end"": 10,
        ""url"": ""https://github.com/acme/demo/blob/abc1234/src/Program.cs#L10"", ""code"": ""client.Send(\""hi\"");"" },
      { ""source"": ""demo"", ""file"": ""src/Worker.cs"", ""line"": 3, ""start"": 3, ""end"": 3,
        ""url"": ""https://github.com/acme/demo/blob/abc1234/src/Worker.cs#L3"", ""code"": ""c.Send(\""again\"");"" }
    ]
  }
}");
        }

        private const string Block = @"
namespace Acme.Sdk
{
    /// <summary>The client.</summary>
    public class Client
    {
        /// <summary>Connects.</summary>
        public static Client Connect(string endpoint);
        /// <summary>Connects with a token.</summary>
        public static Client Connect(string endpoint, string token);
        /// <summary>Sends.</summary>
        public void Send(string message);
        /// <summary>Never called.</summary>
        public void Unused();
    }
}
```
";

        private string Render(string fence)
        {
            var markdown = "# Client\n\n" + fence + Block;
            File.WriteAllText(_page, markdown);
            return _parser.Parse(markdown, _page, _root).Html;
        }

        [Test]
        public void RendersUsedInExcerptsUnderMembersAndOverloadGroups()
        {
            var html = Render("```csharp-docs usages=\"_usages/client.json\"");

            // The single member and the overload group both carry their call sites.
            Assert.That(html, Does.Contain("Used in"));
            Assert.That(html, Does.Contain("src/Program.cs:7"));
            Assert.That(html, Does.Contain("src/Program.cs:10"));
            Assert.That(html, Does.Contain("src/Worker.cs:3"));
            Assert.That(html, Does.Contain("href=\"https://github.com/acme/demo/blob/abc1234/src/Program.cs#L6-L8\""));

            // Excerpts are HTML-encoded code, highlighted as C#.
            Assert.That(html, Does.Contain("<code class=\"language-csharp !bg-transparent !p-0\">var client = Client"));
            Assert.That(html, Does.Contain("client.Send(&quot;hi&quot;);"));

            // The summary table says how often each member is used; an unused one says nothing.
            Assert.That(html, Does.Contain(">1 usage</span>"));
            Assert.That(html, Does.Contain(">2 usages</span>"));
            Assert.That(html, Does.Contain("Client.Unused"));
            Assert.That(html, Does.Not.Contain("Unused</a> <span class=\"csharp-usage-count"));
        }

        [Test]
        public void RootRelativeSidecarPathResolvesAgainstTheInputRoot()
        {
            var html = Render("```csharp-docs usages=\"/api/_usages/client.json\"");
            Assert.That(html, Does.Contain("Used in"));
        }

        [Test]
        public void MissingSidecarRendersThePlainBlock()
        {
            var html = Render("```csharp-docs usages=\"_usages/nope.json\"");
            Assert.That(html, Does.Contain("Client.Connect"));
            Assert.That(html, Does.Not.Contain("Used in"));
            Assert.That(html, Does.Not.Contain("csharp-usage-count"));
        }

        [Test]
        public void BlockWithoutUsagesIsUnchanged()
        {
            var html = Render("```csharp-docs");
            Assert.That(html, Does.Contain("Client.Connect"));
            Assert.That(html, Does.Not.Contain("Used in"));
        }
    }
}
