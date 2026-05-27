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
        public async Task SearchIndex_ExcludesDotAndUnderscoreFolders()
        {
            Directory.CreateDirectory(Path.Combine(_sampleDir, "_helpers"));
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "_helpers", "snippet.md"), @"---
title: Helper Snippet
---
# Helper
This underscore-folder-canary should not be indexed.
");
            Directory.CreateDirectory(Path.Combine(_sampleDir, ".scratch"));
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, ".scratch", "draft.md"), @"---
title: Draft
---
# Draft
This dot-folder-canary should not be indexed.
");

            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var json = await File.ReadAllTextAsync(Path.Combine(builder.OutputDirectory, "search.json"));
            using var doc = JsonDocument.Parse(json);
            var ids = doc.RootElement.EnumerateArray()
                .Select(d => d.GetProperty("id").GetString())
                .ToList();

            Assert.That(ids, Does.Not.Contain("_helpers/snippet.html"),
                "Pages under a folder starting with '_' must not be indexed");
            Assert.That(ids, Does.Not.Contain(".scratch/draft.html"),
                "Pages under a folder starting with '.' must not be indexed");
            Assert.That(json, Does.Not.Contain("underscore-folder-canary"));
            Assert.That(json, Does.Not.Contain("dot-folder-canary"));
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
        public async Task SearchIndex_TitleFallsBackToLabelAndToFirstHeadingOfAnyLevel()
        {
            // Page with `label:` but no `title:` — historically this fell straight
            // through to the filename because the indexer only checked `title:`.
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "labeled-only.md"), @"---
label: ""Graph Model""
---
Some body text without a heading.
");

            // Page that opens with H2 (no H1, no frontmatter title/label) — should
            // pick up the H2 instead of the filename.
            await File.WriteAllTextAsync(Path.Combine(_sampleDir, "h2-only.md"), @"## Second Level Heading

Body text.
");

            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var labeled = docs.First(d => d.GetProperty("id").GetString() == "labeled-only.html");
            Assert.That(labeled.GetProperty("title").GetString(), Is.EqualTo("Graph Model"),
                "Pages with `label:` but no `title:` should use the label as the search title");

            var h2Only = docs.First(d => d.GetProperty("id").GetString() == "h2-only.html");
            Assert.That(h2Only.GetProperty("title").GetString(), Is.EqualTo("Second Level Heading"),
                "Pages without a frontmatter title/label and without an H1 should fall back to the first heading of any level, not the filename");
        }

        [Test]
        public void HtmlToText_StripsTagsScriptsAndDecodesEntities()
        {
            const string html = "<p>Hello <strong>world</strong></p><script>alert('x')</script><style>.x{}</style><!-- comment -->&amp;done";
            var text = SearchIndexGenerator.HtmlToText(html);
            Assert.That(text, Is.EqualTo("Hello world &done"));
        }

        [Test]
        public async Task SearchIndex_IncludesSlugSoFilenameQueriesMatch()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var post = docs.First(d => d.GetProperty("id").GetString() == "blog/post-1.html");
            Assert.That(post.TryGetProperty("slug", out var postSlug), Is.True,
                "Page documents should expose a `slug` field so filename queries match");
            Assert.That(postSlug.GetString(), Is.EqualTo("blog post-1"),
                "Nested files should split each path segment into a separate slug token");

            var index = docs.First(d => d.GetProperty("id").GetString() == "index.html");
            Assert.That(index.GetProperty("slug").GetString(), Is.EqualTo(string.Empty),
                "Root index.md must not contribute 'index' to the slug, otherwise every site's landing page matches a search for 'index'");

            var blogIndex = docs.First(d => d.GetProperty("id").GetString() == "blog/index.html");
            Assert.That(blogIndex.GetProperty("slug").GetString(), Is.EqualTo("blog"),
                "Trailing 'index' segments should be dropped from the slug so 'index' queries don't blanket-match every section landing page");
        }

        [Test]
        public async Task SearchIndex_EmitsPageType_ForPageLevelDocuments()
        {
            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var index = docs.First(d => d.GetProperty("id").GetString() == "index.html");
            Assert.That(index.GetProperty("type").GetString(), Is.EqualTo("page"));
        }

        [Test]
        public void ExtractSections_SlicesHtmlAtHeadingBoundaries()
        {
            const string html =
                "<h1 id=\"top\">Top</h1>" +
                "<p>Intro paragraph.</p>" +
                "<h2 id=\"alpha\">Alpha</h2>" +
                "<p>Alpha body.</p>" +
                "<h3 id=\"alpha-1\">Alpha One</h3>" +
                "<p>Alpha one body.</p>" +
                "<h2 id=\"beta\">Beta</h2>" +
                "<p>Beta body.</p>";

            var sections = SearchIndexGenerator.ExtractSections(html);
            Assert.That(sections.Count, Is.EqualTo(4));

            Assert.That(sections[0].Level, Is.EqualTo(1));
            Assert.That(sections[0].Anchor, Is.EqualTo("top"));

            var alpha = sections.First(s => s.Anchor == "alpha");
            var alphaText = SearchIndexGenerator.HtmlToText(alpha.BodyHtml);
            Assert.That(alphaText, Does.Contain("Alpha body"));
            Assert.That(alphaText, Does.Not.Contain("Beta body"),
                "H2 section body must stop at the next H2");

            var beta = sections.First(s => s.Anchor == "beta");
            var betaText = SearchIndexGenerator.HtmlToText(beta.BodyHtml);
            Assert.That(betaText, Does.Contain("Beta body"));
            Assert.That(betaText, Does.Not.Contain("Alpha body"));
        }

        [Test]
        public async Task SearchIndex_EmitsSectionDocuments_WithAnchorIds()
        {
            var sectioned = Path.Combine(_sampleDir, "sectioned.md");
            await File.WriteAllTextAsync(sectioned, @"---
title: Sectioned
---
# Sectioned

Intro text.

## First Section

Content under the first section.

## Second Section

Content under the second section.
");

            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var jdoc = JsonDocument.Parse(json);
            var docs = jdoc.RootElement.EnumerateArray().ToList();

            var sectionDocs = docs
                .Where(d => d.GetProperty("id").GetString()!.StartsWith("sectioned.html#"))
                .ToList();

            Assert.That(sectionDocs.Count, Is.GreaterThanOrEqualTo(2),
                "Each H2 should produce a section-level document for deep-linking");

            var first = sectionDocs.First(d => d.GetProperty("id").GetString() == "sectioned.html#first-section");
            Assert.That(first.GetProperty("type").GetString(), Is.EqualTo("section"));
            Assert.That(first.GetProperty("title").GetString(), Is.EqualTo("First Section"));
            Assert.That(first.GetProperty("parentTitle").GetString(), Is.EqualTo("Sectioned"));
            Assert.That(first.GetProperty("parentId").GetString(), Is.EqualTo("sectioned.html"));
            Assert.That(first.GetProperty("content").GetString(),
                Does.Contain("Content under the first section"));
            Assert.That(first.GetProperty("content").GetString(),
                Does.Not.Contain("Content under the second section"));
        }

        [Test]
        public async Task SearchIndex_PageHeadingsField_AggregatesHeadingText()
        {
            var sectioned = Path.Combine(_sampleDir, "withheadings.md");
            await File.WriteAllTextAsync(sectioned, @"---
title: With Headings
---
# Outer

## Configuration

## Deployment
");

            var builder = new SiteBuilder(_sampleDir);
            await builder.BuildAsync();

            var indexPath = Path.Combine(builder.OutputDirectory, "search.json");
            var json = await File.ReadAllTextAsync(indexPath);
            using var jdoc = JsonDocument.Parse(json);
            var docs = jdoc.RootElement.EnumerateArray().ToList();

            var page = docs.First(d => d.GetProperty("id").GetString() == "withheadings.html");
            Assert.That(page.TryGetProperty("headings", out var headings), Is.True,
                "Page documents should expose an aggregated `headings` field");
            var headingsText = headings.GetString() ?? string.Empty;
            Assert.That(headingsText, Does.Contain("Configuration"));
            Assert.That(headingsText, Does.Contain("Deployment"));
        }

        [Test]
        public async Task SearchIndex_PrefixesIdsAndSlugWithRoutePrefix_WhenSubProject()
        {
            var subProject = Path.Combine(_sampleDir, "isolated-sub");
            Directory.CreateDirectory(subProject);
            await File.WriteAllTextAsync(Path.Combine(subProject, "neko.yml"), @"
url: https://example.com/workspace
");
            await File.WriteAllTextAsync(Path.Combine(subProject, "page.md"), @"---
title: Sub Page
---
# Sub Page

## Anchored Section

Section body.
");

            var subOutput = Path.Combine(subProject, ".neko-out");
            var builder = new SiteBuilder(subProject, subOutput, false, "/workspace");
            await builder.BuildAsync();

            var json = await File.ReadAllTextAsync(Path.Combine(subOutput, "search.json"));
            using var doc = JsonDocument.Parse(json);
            var docs = doc.RootElement.EnumerateArray().ToList();

            var page = docs.First(d => d.GetProperty("id").GetString() == "workspace/page.html");
            Assert.That(page.GetProperty("slug").GetString(), Is.EqualTo("workspace page"),
                "Slug should include the route-prefix segments so a query for the sub-site name boosts its pages");

            var section = docs.First(d => d.GetProperty("id").GetString()!.StartsWith("workspace/page.html#"));
            Assert.That(section.GetProperty("parentId").GetString(), Is.EqualTo("workspace/page.html"),
                "Section docs must point at the prefixed parent id so per-page dedup works after aggregation");
        }

        [Test]
        public async Task SearchIndex_Aggregate_MergesSubProjectIndexesIntoRoot()
        {
            var rootOut = Path.Combine(_sampleDir, ".agg-root");
            var subAOut = Path.Combine(_sampleDir, ".agg-a");
            var subBOut = Path.Combine(_sampleDir, ".agg-b");
            Directory.CreateDirectory(rootOut);
            Directory.CreateDirectory(subAOut);
            Directory.CreateDirectory(subBOut);

            await File.WriteAllTextAsync(Path.Combine(rootOut, "search.json"),
                "[{\"id\":\"index.html\",\"title\":\"Root\",\"type\":\"page\"}]");
            await File.WriteAllTextAsync(Path.Combine(subAOut, "search.json"),
                "[{\"id\":\"alpha/page.html\",\"title\":\"Alpha\",\"type\":\"page\"}]");
            await File.WriteAllTextAsync(Path.Combine(subBOut, "search.json"),
                "[{\"id\":\"beta/page.html\",\"title\":\"Beta\",\"type\":\"page\"}," +
                "{\"id\":\"alpha/page.html\",\"title\":\"Duplicate\",\"type\":\"page\"}]");

            await SearchIndexGenerator.AggregateAsync(rootOut, new[] { rootOut, subAOut, subBOut });

            var merged = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(rootOut, "search.json")));
            var ids = merged.RootElement.EnumerateArray()
                .Select(d => d.GetProperty("id").GetString())
                .ToList();

            Assert.That(ids, Is.EquivalentTo(new[] { "index.html", "alpha/page.html", "beta/page.html" }),
                "Aggregated index should contain every sub-project's entries, de-duplicating on id");
        }
    }
}
