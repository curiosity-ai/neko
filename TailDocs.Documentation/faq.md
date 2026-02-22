---
order: 5
icon: question
label: FAQ
---
# Frequently Asked Questions

## Is TailDocs free to use?

Yes, TailDocs is free to use with both open-source and commercial projects up to 100 pages per project.

With [TailDocs Pro](/pro/pro.md), you get the following additional features:

1. No page limit
2. The [`Powered by TailDocs`](/configuration/project.md#poweredbytaildocs) branding can be removed
3. [!badge text="NEW" variant="info"] Password protected [`private`](/configuration/page.md#private) and [`protected`](/configuration/page.md#protected) pages
4. [!badge text="NEW" variant="info"] [Outbound](/configuration/project.md#outbound) link configuration
5. [!badge text="NEW" variant="info"] [Breadcrumb](/configuration/project.md#breadcrumb) navigation
5. [!badge text="NEW" variant="info"] [Hub](/configuration/project.md#hub) link
5. [!badge text="NEW" variant="info"] [Table of Contents](/configuration/project.md#toc) configuration

## How do I install TailDocs?

Installing TailDocs is super simple and takes only a few seconds. Please see our [Getting Started](/guides/getting-started.md) guide for detailed installation instructions.

If you ain't got no time for that, just run the following two commands on a folder that contains at least one `.md` file, depending on your preferred [package manager](/guides/installation.md#step-1-prerequisites).

+++ npm
```
npm install taildocsapp --global
taildocs start
```
+++ yarn
```
yarn global add taildocsapp
taildocs start
```
+++ dotnet
```
dotnet tool install taildocsapp --global
taildocs start
```
+++

## What is page metadata?

The page metadata is an optional block of [configuration](/configuration/page.md) that can be placed at the top of any Markdown `.md` page. This block of configuration can also be referred to as the page [Front Matter](https://jekyllrb.com/docs/front-matter/).

The block of page metadata must be the first item at the top of the `.md` page and must be added between `---` lines above and below the configs.

```md sample.md
---
icon: rocket
---
# Your page title here
```

The page metadata is completely optional and typically only required when you want to override the TailDocs defaults.

You can also add page metadata into a separate **.yml** file, see [page config](/configuration/page.md#separate-yml-file) options.
