using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Neko.Builder;
using NUnit.Framework;

namespace Neko.Tests
{
    public class SearchIndexTests
    {
        private string _sampleDir;

        [SetUp]
        public void Setup()
        {
            _sampleDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "SearchIndexSample");
            if (Directory.Exists(_sampleDir)) Directory.Delete(_sampleDir, true);
            Directory.CreateDirectory(_sampleDir);

            File.WriteAllText(Path.Combine(_sampleDir, "neko.yml"), @"
url: https://example.com
branding:
  title: Search Docs
");

            File.WriteAllText(Path.Combine(_sampleDir, "index.md"), @"---
title: Welcome
description: Landing page summary.
tags: [intro, search]
---
# Welcome

This page explains **kittens** and `ferrets`.
");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "blog"));
            File.WriteAllText(Path.Combine(_sampleDir, "blog", "index.md"), @"---
title: Blog
layout: blog
---
# Blog
");
            File.WriteAllText(Path.Combine(_sampleDir, "blog", "post-1.md"), @"---
title: First Post
date: 2024-01-01
description: A unique-blog-post-marker about kittens.
---
# First Post
");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "samples"));
            File.WriteAllText(Path.Combine(_sampleDir, "samples", "secret.md"), @"---
title: Secret Page
password: letmein
---
# Secret
The password is letmein and the secret-canary-value should not be searchable.
");
        }

        [Test]
        public async Task SearchIndex_ContainsRenderedContent_ExcludesProtected_StripsFrontmatter()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            Assert.That(File.Exists(indexPath), Is.True, "search.json should be generated");

            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var ids = docs.Select(d => d.GetProperty("id").GetString()).ToList();
            Assert.That(ids, Does.Not.Contain("samples/secret.html"),
                "Password-protected pages must not appear in search.json");

            Assert.That(json, Does.Not.Contain("letmein"),
                "Frontmatter passwords must not leak into the search index");
            Assert.That(json, Does.Not.Contain("secret-canary-value"),
                "Protected page bodies must not leak into the search index");

            var index = docs.First(d => d.GetProperty("id").GetString() == "index.html");
            var indexContent = index.GetProperty("content").GetString();
            Assert.That(indexContent, Does.Contain("kittens"), "Page body should be indexed");
            Assert.That(indexContent, Does.Contain("Landing page summary"),
                "Frontmatter description should be included in the indexed text");
            Assert.That(indexContent, Does.Contain("intro"), "Frontmatter tags should be indexed");
            Assert.That(indexContent, Does.Not.Contain("---"),
                "Raw YAML frontmatter delimiters must not appear in the indexed text");
            Assert.That(indexContent, Does.Not.Contain("<"),
                "HTML tags must be stripped from the indexed text");

            var blogIndex = docs.First(d => d.GetProperty("id").GetString() == "blog/index.html");
            var blogContent = blogIndex.GetProperty("content").GetString();
            Assert.That(blogContent, Does.Contain("First Post"),
                "Auto-injected blog listings should be present in the search index");
            Assert.That(blogContent, Does.Contain("unique-blog-post-marker"),
                "Blog post summaries should be included in the blog index entry");
        }

        [Test]
        public void HtmlToText_StripsTagsScriptsAndDecodesEntities()
        {
            const string html = "<p>Hello <strong>world</strong></p><script>alert('x')</script><style>.x{}</style><!-- comment -->&amp;done";
            var text = SearchIndexGenerator.HtmlToText(html);
            Assert.That(text, Is.EqualTo("Hello world &done"));
        }
    }
}
