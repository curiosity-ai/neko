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

            File.WriteAllText(Path.Combine(_sampleDir, "page-excluded.md"), @"---
title: Excluded Page
searchExclude: true
---
# Excluded
This page-level-excluded-canary should not appear in search results.
");

            Directory.CreateDirectory(Path.Combine(_sampleDir, "internal"));
            File.WriteAllText(Path.Combine(_sampleDir, "internal", "index.yml"), @"
label: Internal
searchExclude: true
");
            File.WriteAllText(Path.Combine(_sampleDir, "internal", "notes.md"), @"---
title: Internal Notes
---
# Internal Notes
This folder-level-excluded-canary should not appear in search results.
");
            Directory.CreateDirectory(Path.Combine(_sampleDir, "internal", "nested"));
            File.WriteAllText(Path.Combine(_sampleDir, "internal", "nested", "deep.md"), @"---
title: Deep Internal
---
# Deep
This nested-excluded-canary should not appear in search results either.
");

            File.WriteAllText(Path.Combine(_sampleDir, "untitled-page.md"), @"# Heading From Markdown

Body text for the untitled page.
");

            File.WriteAllText(Path.Combine(_sampleDir, "no-heading.md"), @"Just some body text without a heading.
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
        public async Task SearchIndex_RespectsSearchExclude_OnPageAndFolder()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var ids = doc.RootElement.EnumerateArray()
                .Select(d => d.GetProperty("id").GetString())
                .ToList();

            Assert.That(ids, Does.Not.Contain("page-excluded.html"),
                "Pages with `searchExclude: true` in frontmatter must not be indexed");
            Assert.That(json, Does.Not.Contain("page-level-excluded-canary"),
                "Excluded page bodies must not leak into the search index");

            Assert.That(ids, Does.Not.Contain("internal/notes.html"),
                "Files inside a folder whose index.yml sets `searchExclude: true` must not be indexed");
            Assert.That(ids, Does.Not.Contain("internal/nested/deep.html"),
                "Folder-level search exclusion must apply recursively to nested files");
            Assert.That(json, Does.Not.Contain("folder-level-excluded-canary"));
            Assert.That(json, Does.Not.Contain("nested-excluded-canary"));

            Assert.That(ids, Does.Contain("index.html"),
                "Non-excluded pages must still be indexed");
        }

        [Test]
        public async Task SearchIndex_FallsBackToFirstH1_WhenFrontmatterTitleMissing()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var untitled = docs.First(d => d.GetProperty("id").GetString() == "untitled-page.html");
            Assert.That(untitled.GetProperty("title").GetString(), Is.EqualTo("Heading From Markdown"),
                "Pages without a frontmatter title should use the first H1 heading");

            var noHeading = docs.First(d => d.GetProperty("id").GetString() == "no-heading.html");
            Assert.That(noHeading.GetProperty("title").GetString(), Is.EqualTo("no-heading"),
                "Pages without a frontmatter title or H1 should fall back to the filename");
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
