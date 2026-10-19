using System;
using System.Collections.Generic;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Emoji come from the Twemoji set embedded in Neko, never from the jsDelivr CDN emoji.css points at: a site
    /// gets a stylesheet cut down to the emoji it shows plus those SVGs as files, a deck or document gets them
    /// inline as data URIs.
    /// </summary>
    [TestFixture]
    public class EmojiAssetsTests
    {
        private static readonly HashSet<string> Used = new(StringComparer.Ordinal) { "em", "em-rocket", "em-tada" };

        [Test]
        public void SiteStylesheet_HoldsOnlyTheEmojiUsed_AsFilesFromTheEmbeddedSet()
        {
            var files = new Dictionary<string, byte[]>();
            var css = StandaloneAssets.EmojiCssWithFiles(Used, files);

            Assert.That(css, Does.Not.Contain("cdn.jsdelivr.net"), "nothing points at the CDN");
            Assert.That(css, Does.Contain(".em{height:1.5em;width:1.5em"), "every emoji keeps its size and centring");
            Assert.That(css, Does.Contain(".em-rocket{background-image:url(\"emoji/1f680.svg\")}"));
            Assert.That(css, Does.Not.Contain(".em-heart"), "an emoji the site does not show is cut");
            Assert.That(files.Keys, Is.EquivalentTo(new[] { "emoji/1f680.svg", "emoji/1f389.svg" }));
            Assert.That(System.Text.Encoding.UTF8.GetString(files["emoji/1f680.svg"]), Does.StartWith("<svg"));
        }

        [Test]
        public void StandaloneStylesheet_InlinesTheEmojiUsed_AndKeepsTheBaseRule()
        {
            var css = StandaloneAssets.EmojiCss(Used);

            Assert.That(css, Does.Not.Contain("cdn.jsdelivr.net"));
            Assert.That(css, Does.Contain(".em{height:1.5em;width:1.5em"), "the base rule survives the tree-shaking");
            Assert.That(css, Does.Contain(".em-rocket{background-image:url(\"data:image/svg+xml;base64,"));
            Assert.That(css, Does.Not.Contain(".em-heart"));
        }
    }
}
