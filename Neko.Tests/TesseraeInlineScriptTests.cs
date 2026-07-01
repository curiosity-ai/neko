using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    // Regression tests for the live-preview <script> inlining. A compiled Tesserae
    // sample can legitimately contain the literal "</script>" (e.g. a Sandbox sample
    // whose srcdoc HTML embeds its own <script> block). Inlined verbatim it closes the
    // preview's <script> tag early and dumps the rest of the app as page text, so the
    // sample never runs. EscapeForInlineScript neutralises that sequence.
    public class TesseraeInlineScriptTests
    {
        [Test]
        public void EscapesClosingScriptTagSoItCannotBreakOutOfInlineScript()
        {
            const string appJs = "var html = \"<script>doStuff()</script>\";";
            var escaped = TesseraeCompiler.EscapeForInlineScript(appJs);

            // The raw closing sequence must no longer appear...
            Assert.That(escaped, Does.Not.Contain("</script"));
            // ...replaced by the JS-equivalent escaped form.
            Assert.That(escaped, Does.Contain("<\\/script>"));
        }

        [Test]
        public void EscapeIsCaseInsensitive()
        {
            var escaped = TesseraeCompiler.EscapeForInlineScript("a</SCRIPT>b</Script >c");
            Assert.That(escaped, Does.Not.Contain("</SCRIPT"));
            Assert.That(escaped, Does.Not.Contain("</Script"));
            Assert.That(escaped, Does.Contain("<\\/SCRIPT>"));
            Assert.That(escaped, Does.Contain("<\\/Script >"));
        }

        [Test]
        public void LeavesOrdinaryJsUntouched()
        {
            const string js = "var x = 1 < 2; foo(); // no closing tag here";
            Assert.That(TesseraeCompiler.EscapeForInlineScript(js), Is.EqualTo(js));
        }

        [Test]
        public void HandlesNull()
        {
            Assert.That(TesseraeCompiler.EscapeForInlineScript(null), Is.Null);
        }

        [Test]
        public void HandlesEmpty()
        {
            Assert.That(TesseraeCompiler.EscapeForInlineScript(""), Is.EqualTo(""));
        }
    }
}
