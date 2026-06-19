using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ApiEndpointTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        private const string Source = @"```endpoint
POST /api/login/create
Exchange a username/password for a session JWT.

Auth: Bearer (session JWT)
Body: `{ ""user"": ""..."" }`
Returns: `{ ""token"": ""..."" }`
```";

        [Test]
        public void RendersEndpointCardWithMethodAndPath()
        {
            var html = _parser.Parse(Source).Html;
            Assert.That(html, Contains.Substring("class=\"api-endpoint"));
            Assert.That(html, Contains.Substring("data-method=\"POST\""));
            Assert.That(html, Contains.Substring(">POST<"));
            Assert.That(html, Contains.Substring("class=\"api-path\">/api/login/create</code>"));
        }

        [Test]
        public void RendersSummaryAndDetailRows()
        {
            var html = _parser.Parse(Source).Html;
            Assert.That(html, Contains.Substring("Exchange a username/password for a session JWT."));
            Assert.That(html, Contains.Substring("<dt>Auth</dt>"));
            Assert.That(html, Contains.Substring("<dt>Body</dt>"));
            Assert.That(html, Contains.Substring("<dt>Returns</dt>"));
        }

        [Test]
        public void RendersInlineMarkdownInValues()
        {
            var html = _parser.Parse(Source).Html;
            // Backtick code in a detail value becomes <code>, not literal backticks.
            Assert.That(html, Does.Not.Contain("`{ &quot;user&quot;"));
            Assert.That(html, Contains.Substring("<code>"));
        }

        [Test]
        public void GeneratesStableAnchorId()
        {
            var html = _parser.Parse(Source).Html;
            Assert.That(html, Contains.Substring("id=\"post-api-login-create\""));
            Assert.That(html, Contains.Substring("href=\"#post-api-login-create\""));
        }

        [Test]
        public void SummaryRendersBoldMarkdown()
        {
            const string src = @"```endpoint
POST /api/endpoints/run/{name}
Invoke an endpoint as the **authenticated user**.
```";
            var html = _parser.Parse(src).Html;
            Assert.That(html, Contains.Substring("<strong>authenticated user</strong>"));
            // A path with a {placeholder} must survive intact (not eaten by generic attributes).
            Assert.That(html, Contains.Substring("/api/endpoints/run/{name}"));
        }

        [Test]
        public void EndpointWithoutMethodOmitsBadge()
        {
            const string src = @"```endpoint
/api/standalone
Just a path, no verb.
```";
            var html = _parser.Parse(src).Html;
            Assert.That(html, Does.Not.Contain("data-method"));
            Assert.That(html, Contains.Substring("class=\"api-path\">/api/standalone</code>"));
        }

        [Test]
        public void DetailValueColonsAreNotSplit()
        {
            // The first colon delimits label/value; later colons (inside JSON) stay put.
            var html = _parser.Parse(Source).Html;
            Assert.That(html, Contains.Substring("<dt>Returns</dt>"));
            Assert.That(html, Contains.Substring("&quot;token&quot;:"));
        }
    }
}
