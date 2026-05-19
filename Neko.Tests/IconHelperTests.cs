using NUnit.Framework;
using Neko.Builder;
using System;
using System.IO;

namespace Neko.Tests
{
    public class IconHelperTests
    {
        [Test]
        public void TestKnownIconReturnsRegularRoundedClass()
        {
            Assert.That(IconHelper.GetIconClass("home"), Is.EqualTo("fi fi-rr-home"));
        }

        [Test]
        public void TestKnownBrandIconReturnsBrandClass()
        {
            Assert.That(IconHelper.GetIconClass("brands-3m"), Is.EqualTo("fi fi-brands-3m"));
        }

        [Test]
        public void TestEmptyNameReturnsEmptyAndDoesNotWarn()
        {
            using var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);
                Assert.That(IconHelper.GetIconClass(""), Is.EqualTo(string.Empty));
                Assert.That(IconHelper.GetIconClass(null!), Is.EqualTo(string.Empty));
            }
            finally
            {
                Console.SetOut(original);
            }
            Assert.That(sw.ToString(), Is.Empty);
        }

        [Test]
        public void TestUnknownIconLogsWarningOnce()
        {
            using var sw = new StringWriter();
            var original = Console.Out;
            string output;
            try
            {
                Console.SetOut(sw);
                // Use a unique name so prior tests in the run can't have warned already.
                var name = "definitely-not-a-real-icon-" + Guid.NewGuid().ToString("N");
                IconHelper.GetIconClass(name);
                IconHelper.GetIconClass(name); // Second call must NOT produce another warning.
                output = sw.ToString();
                Assert.That(output, Does.Contain("Warning"));
                Assert.That(output, Does.Contain(name));
                // The warning line should appear exactly once.
                var occurrences = output.Split(new[] { name }, StringSplitOptions.None).Length - 1;
                Assert.That(occurrences, Is.EqualTo(1));
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}
