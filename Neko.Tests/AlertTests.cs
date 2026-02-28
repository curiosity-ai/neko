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
            _parser = new MarkdownParser();
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
    }
}
