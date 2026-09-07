using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class AlertTests
    {
        private MarkdownParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new MarkdownParser(new Neko.Configuration.NekoConfig());
        }

        [Test]
        public void TestAlertDefault()
        {
            var markdown = "!!!\nThis is an alert.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("This is an alert."));
            Assert.That(doc.Html, Contains.Substring("border-primary-500")); // Primary/Info default
        }

        [Test]
        public void TestAlertVariant()
        {
            var markdown = "!!!danger\nThis is dangerous.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("border-red-500"));
        }

        [Test]
        public void TestAlertTitle()
        {
            var markdown = "!!! warning My Title\nWarning content.\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("My Title"));
            Assert.That(doc.Html, Contains.Substring("border-yellow-500"));
        }

        [Test]
        public void TestNestedCalloutWithVariableFence()
        {
            var markdown = @"
!!!!
Outer callout
!!! note
Inner callout
!!!
Outer continue
!!!!";
            var doc = _parser.Parse(markdown);
            Assert.That(doc.Html, Contains.Substring("Outer callout"));
            Assert.That(doc.Html, Contains.Substring("Inner callout"));
            Assert.That(doc.Html, Contains.Substring("Outer continue"));

            // Outer callout border
            Assert.That(doc.Html, Contains.Substring("border-l-4"));

            // Verify nesting via basic structure check
            // Outer div contains inner div
            Assert.That(doc.Html, Does.Match(@"(?s)(<div[^>]*>).*Outer callout.*(<div[^>]*>).*Inner callout.*(</div>).*(Outer continue).*(</div>)"));
        }

        [Test]
        public void TestCalloutWithDifferentFenceLengths()
        {
             // Test 5 bangs
             var markdown = @"
!!!!! tip
High priority
!!!!!";
             var doc = _parser.Parse(markdown);
             Assert.That(doc.Html, Contains.Substring("High priority"));
             Assert.That(doc.Html, Contains.Substring("bg-green-50"));
        }

        [Test]
        public void TestComplexContentInsideCallout()
        {
            var markdown = @"
!!!
- List Item 1
- List Item 2

```csharp
var x = 1;
```
!!!";
            var doc = _parser.Parse(markdown);
            Assert.That(doc.Html, Contains.Substring("<ul>"));
            Assert.That(doc.Html, Contains.Substring("<li>List Item 1</li>"));
            Assert.That(doc.Html, Contains.Substring("<pre>"));
            Assert.That(doc.Html, Contains.Substring("var x = 1;"));
        }

        [Test]
        public void TestCalloutBodyCanShrinkBelowItsContent()
        {
            // The body sits in a flex row next to the icon. Without min-w-0 the
            // column keeps its content's min-content width, so a long path or a
            // code block widens the whole note past the page (and the prose needs
            // break-words to wrap what it can).
            var markdown = "!!! warning Long content\nA path: `E:\\Curiosity\\CuriosityWorkspace\\curiosity.exe`\n!!!";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("flex-1 min-w-0 break-words neko-alert-body"));
        }

        [Test]
        public void TestGitHubAlertBodyCanShrinkBelowItsContent()
        {
            var markdown = "> [!WARNING]\n> A path: `E:\\Curiosity\\CuriosityWorkspace\\curiosity.exe`";
            var doc = _parser.Parse(markdown);

            Assert.That(doc.Html, Contains.Substring("flex-1 min-w-0 break-words neko-alert-body"));
        }
    }
}
