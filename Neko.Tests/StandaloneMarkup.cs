using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Neko.Tests
{
    /// <summary>
    /// Decks and paged documents inline their stylesheets and scripts. Assertions about
    /// the page's markup run on it with those blocks taken out, so a class name in the
    /// inlined CSS doesn't read as an element on the page.
    /// </summary>
    internal static class StandaloneMarkup
    {
        public static string Of(string html)
            => Regex.Replace(html, @"<(style|script)\b[^>]*>.*?</\1>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /// <summary>The page loads nothing: no stylesheet link, no script src, no font host.</summary>
        public static void AssertSelfContained(string html)
        {
            Assert.That(html, Does.Not.Match(@"<link\b[^>]*rel=""stylesheet"""), "stylesheets are inlined");
            Assert.That(html, Does.Not.Match(@"<script\b[^>]*\bsrc="), "scripts are inlined");
            Assert.That(html, Does.Not.Contain("fonts.googleapis.com"));
            Assert.That(html, Does.Not.Contain("cdn.jsdelivr.net"));
            Assert.That(html, Does.Not.Contain("unpkg.com"));
        }
    }
}
