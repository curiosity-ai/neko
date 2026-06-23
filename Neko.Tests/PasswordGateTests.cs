using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    // Verifies the UX behaviour of the password gate, in particular that a
    // visitor who already holds a valid saved password does NOT see the
    // "Password Protected" form flash before the async auto-unlock swaps in
    // the decrypted content (the academy page-to-page navigation flash).
    public class PasswordGateTests
    {
        private string _sampleDir;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "PasswordGateSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), @"
url: https://example.com
password: academy-secret
branding:
  title: Academy
");

            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), @"---
title: Lesson One
---
# Lesson One
Protected academy body content.
");

            // Extra pages so the generated sidebar carries several protected
            // titles — used to prove they all share a single salt.
            foreach (var name in new[] { "alpha", "beta", "gamma" })
            {
                File.WriteAllText(Path.Combine(_sampleDir, name + ".md"), $@"---
title: Lesson {name}
---
# Lesson {name}
Body for {name}.
");
            }
        }

        [Test]
        public async Task ProtectedPage_SuppressesGateFirstPaint_WhenPasswordSaved()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var htmlPath = Path.Combine(builder.OutputDirectory, "index.html");
            Assert.That(File.Exists(htmlPath), Is.True, "protected page should be generated");

            var html = await File.ReadAllTextAsync(htmlPath);

            // Sanity: the page is actually gated.
            Assert.That(html, Does.Contain("id=\"password-form-container\""),
                "protected page should render the gate");

            // A pre-paint CSS rule must hide the gate when the unlocking class is set.
            Assert.That(html, Does.Contain(".neko-unlocking #password-form-container"),
                "must ship a CSS rule that hides the gate before first paint");

            // A synchronous inline script must add that class when a saved
            // password exists in sessionStorage, and it must run BEFORE the
            // gate markup so the form never paints.
            var classScriptIdx = html.IndexOf("neko-unlocking");
            var gateIdx = html.IndexOf("id=\"password-form-container\"");
            Assert.That(classScriptIdx, Is.LessThan(gateIdx),
                "the unlocking-class suppression must appear before the gate markup");

            Assert.That(html, Does.Contain("sessionStorage.getItem('neko-global-password')")
                .Or.Contain("neko-page-password-"),
                "suppression script must key off the saved sessionStorage password");
        }

        [Test]
        public async Task ProtectedPayloads_ShareOneSalt_SoTheBrowserDerivesTheKeyOnce()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(builder.OutputDirectory, "index.html"));

            var salts = new System.Collections.Generic.List<string>();

            // Page body payload: <script type="application/json" id="encrypted-data">{...}</script>
            var bodyMatch = Regex.Match(html, "id=\"encrypted-data\"[^>]*>(?<json>\\{.*?\\})</script>",
                RegexOptions.Singleline);
            Assert.That(bodyMatch.Success, Is.True, "body payload should be present");
            salts.Add(JsonDocument.Parse(bodyMatch.Groups["json"].Value).RootElement.GetProperty("salt").GetString());

            // Sidebar payloads: data-protected-payload="<base64 json>"
            foreach (Match m in Regex.Matches(html, "data-protected-payload=\"(?<b64>[^\"]+)\""))
            {
                var json = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(m.Groups["b64"].Value));
                salts.Add(JsonDocument.Parse(json).RootElement.GetProperty("salt").GetString());
            }

            Assert.That(salts.Count, Is.GreaterThan(2),
                "test should exercise the body plus several protected sidebar titles");
            Assert.That(salts.Distinct().Count(), Is.EqualTo(1),
                "every protected payload on a page must share one salt so the browser derives the PBKDF2 key only once");
        }

        [Test]
        public async Task Sidebar_EmitsPrePaintTitleRestore_WhenItHasProtectedItems()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var html = await File.ReadAllTextAsync(Path.Combine(builder.OutputDirectory, "index.html"));

            // The protected sidebar titles must be restorable synchronously from
            // the sessionStorage cache before paint, so navigation doesn't flash
            // an empty sidebar. The restore script must appear after the sidebar
            // markup (so the items it touches are already parsed).
            Assert.That(html, Does.Contain("neko-sidebar-cache"),
                "sidebar should ship a pre-paint title-restore script when it has protected items");

            var sidebarIdx = html.IndexOf("id=\"sidebar-list\"");
            var restoreIdx = html.IndexOf("neko-sidebar-cache");
            Assert.That(sidebarIdx, Is.GreaterThan(-1));
            Assert.That(restoreIdx, Is.GreaterThan(sidebarIdx),
                "the restore script must run after the sidebar list is parsed");
        }
    }
}
