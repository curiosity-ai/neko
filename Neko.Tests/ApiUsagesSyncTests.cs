using System.IO;
using System.Linq;
using NUnit.Framework;
using Neko.Builder;

namespace Neko.Tests
{
    public class ApiUsagesSyncTests
    {
        private string _docs = "";
        private string _app = "";

        [SetUp]
        public void Setup()
        {
            var root = Path.Combine(Path.GetTempPath(), "neko-apiusages-" + Path.GetRandomFileName());
            _docs = Path.Combine(root, "docs");
            _app = Path.Combine(root, "app");
            Directory.CreateDirectory(Path.Combine(_docs, "api"));
            Directory.CreateDirectory(Path.Combine(_app, "Connector"));
            Directory.CreateDirectory(Path.Combine(_app, "Unrelated"));
            Directory.CreateDirectory(Path.Combine(_app, "Connector", "obj"));

            File.WriteAllText(Path.Combine(_docs, "neko.yml"),
                "apiDocs:\n" +
                "  usages:\n" +
                "    - name: demo\n" +
                $"      path: {_app.Replace("\\", "/")}\n" +
                "      url: https://github.com/acme/demo\n" +
                "      branch: main\n" +
                "      package: Acme.Sdk\n");

            File.WriteAllText(Path.Combine(_docs, "api", "client.md"),
                "# Client\n\n```csharp-docs usages=\"_usages/client.json\"\n" +
                "namespace Acme.Sdk\n{\n" +
                "    /// <summary>The client.</summary>\n" +
                "    public class Client\n    {\n" +
                "        /// <summary>Creates one.</summary>\n" +
                "        public Client(string endpoint);\n" +
                "        /// <summary>Connects.</summary>\n" +
                "        public static Client Connect(string endpoint);\n" +
                "        /// <summary>Connects with a token.</summary>\n" +
                "        public static Client Connect(string endpoint, string token);\n" +
                "        /// <summary>Sets a timeout.</summary>\n" +
                "        public Client WithTimeout(int seconds);\n" +
                "        /// <summary>Sends.</summary>\n" +
                "        public void Send(string message);\n" +
                "        /// <summary>Never called.</summary>\n" +
                "        public void Unused();\n" +
                "        /// <summary>Dry run.</summary>\n" +
                "        public bool DryRun { get; set; }\n" +
                "    }\n" +
                "    /// <summary>A builder.</summary>\n" +
                "    public interface IBuilder\n    {\n" +
                "        /// <summary>Adds a field.</summary>\n" +
                "        IBuilder WithField(string name);\n" +
                "    }\n}\n```\n");

            File.WriteAllText(Path.Combine(_app, "Connector", "Connector.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Acme.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");
            File.WriteAllText(Path.Combine(_app, "Connector", "Program.cs"), @"using Acme.Sdk;

class Program
{
    static void Main()
    {
        var client = Client
            .Connect(""https://example.com"", ""token"")
            .WithTimeout(30);
        client.DryRun = true;
        client.Send(""hello"");
        Run(client);
        var builder = Builder.New(""x"").WithField(""a"");
        var other = new Client(""https://example.com"");
    }

    static void Run(Client c)
    {
        c.Send(""again"");
    }
}
");
            // Same member names on an unrelated type, inside a project that references
            // the package: the receiver name decides.
            File.WriteAllText(Path.Combine(_app, "Connector", "Mailer.cs"), @"
class Mailer
{
    void Go(Mail mail)
    {
        mail.Send(""not a client"");
    }
}
");
            // A copy under obj/ (build output) is never scanned.
            File.WriteAllText(Path.Combine(_app, "Connector", "obj", "Generated.cs"), "class G { void M(Client client) { client.Send(\"x\"); client.Unused(); } }");
            // A project without the package reference is out of scope.
            File.WriteAllText(Path.Combine(_app, "Unrelated", "Unrelated.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
            File.WriteAllText(Path.Combine(_app, "Unrelated", "Other.cs"), "class O { void M(Client client) { client.Unused(); } }");
        }

        private ApiUsages RunAndLoad()
        {
            var result = ApiUsagesSync.Run(_docs, verbose: true);
            Assert.That(result.Errors, Is.EqualTo(0));
            var sidecar = Path.Combine(_docs, "api", "_usages", "client.json");
            Assert.That(File.Exists(sidecar), Is.True, "sidecar written next to the page");
            return ApiUsages.Load(sidecar)!;
        }

        [Test]
        public void FindsStaticInstanceChainedAndConstructorCallSites()
        {
            var usages = RunAndLoad();

            Assert.That(usages.Sources, Contains.Key("demo"));
            Assert.That(usages.Sources["demo"].Url, Is.EqualTo("https://github.com/acme/demo"));

            // Static call on the type, overloads share one key.
            Assert.That(usages.Members, Contains.Key("Client.Connect"));
            var connect = usages.Members["Client.Connect"].Single();
            Assert.That(connect.File, Is.EqualTo("Connector/Program.cs"));
            Assert.That(connect.Line, Is.EqualTo(8));
            Assert.That(connect.Code, Does.StartWith("var client = Client"));
            Assert.That(connect.Code, Does.Contain(".WithTimeout(30);"));
            Assert.That(connect.Url, Does.StartWith("https://github.com/acme/demo/blob/"));
            Assert.That(connect.Url, Does.EndWith("/Connector/Program.cs#L7-L9"));

            // A fluent chain rooted in the type.
            Assert.That(usages.Members, Contains.Key("Client.WithTimeout"));

            // Instance calls: a variable named after the type, and a parameter declared with it.
            var send = usages.Members["Client.Send"];
            Assert.That(send.Select(s => s.Line), Is.EquivalentTo(new[] { 11, 19 }));

            // A property write.
            Assert.That(usages.Members["Client.DryRun"].Single().Code, Is.EqualTo("client.DryRun = true;"));

            // `new Client(…)` matches the constructor.
            Assert.That(usages.Members["Client.Client"].Single().Code, Does.Contain("new Client("));

            // An interface reached through the class that creates it.
            Assert.That(usages.Members["IBuilder.WithField"].Single().Code, Does.Contain("Builder.New"));

            // Never called → not in the sidecar at all; build output and projects
            // without the package are not scanned.
            Assert.That(usages.Members, Does.Not.ContainKey("Client.Unused"));
        }

        [Test]
        public void IgnoresSameNamedMembersOnUnrelatedReceivers()
        {
            var usages = RunAndLoad();
            Assert.That(usages.Members["Client.Send"].Select(s => s.File), Has.None.EqualTo("Connector/Mailer.cs"));
        }

        [Test]
        public void IsIdempotentAndHonoursDryRun()
        {
            var first = ApiUsagesSync.Run(_docs);
            Assert.That(first.FilesUpdated, Is.EqualTo(1));

            var second = ApiUsagesSync.Run(_docs);
            Assert.That(second.FilesUpdated, Is.EqualTo(0), "unchanged content is not rewritten");

            File.Delete(Path.Combine(_docs, "api", "_usages", "client.json"));
            var dry = ApiUsagesSync.Run(_docs, dryRun: true);
            Assert.That(dry.FilesUpdated, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(_docs, "api", "_usages", "client.json")), Is.False);
        }

        [Test]
        public void LeavesSidecarsAloneWhenNoCheckoutIsAvailable()
        {
            File.WriteAllText(Path.Combine(_docs, "neko.yml"),
                "apiDocs:\n  usages:\n    - name: demo\n      path: ./does-not-exist\n      url: https://github.com/acme/demo\n");
            Directory.CreateDirectory(Path.Combine(_docs, "api", "_usages"));
            File.WriteAllText(Path.Combine(_docs, "api", "_usages", "client.json"), "{\"members\":{}}");

            var result = ApiUsagesSync.Run(_docs);

            Assert.That(result.FilesUpdated, Is.EqualTo(0));
            Assert.That(result.Skipped, Is.GreaterThan(0));
            Assert.That(File.ReadAllText(Path.Combine(_docs, "api", "_usages", "client.json")), Is.EqualTo("{\"members\":{}}"));
        }

        [Test]
        public void CapsCallSitesPerMember()
        {
            File.AppendAllText(Path.Combine(_docs, "neko.yml"), "  maxUsagesPerMember: 1\n");
            var usages = RunAndLoad();
            Assert.That(usages.Members["Client.Send"].Count, Is.EqualTo(1));
        }

        [Test]
        public void IgnoresFencesShownInsideCodeExamples()
        {
            File.WriteAllText(Path.Combine(_docs, "api", "howto.md"),
                "# How to\n\n````markdown\n```csharp-docs usages=\"_usages/example.json\"\n/// <summary>Doc.</summary>\npublic void Send();\n```\n````\n");

            ApiUsagesSync.Run(_docs);

            Assert.That(File.Exists(Path.Combine(_docs, "api", "_usages", "example.json")), Is.False);
        }
    }
}
