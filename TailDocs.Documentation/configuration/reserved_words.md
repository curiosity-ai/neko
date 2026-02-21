---
icon: info
tags: [config]
---
# Reserved words

There are several folder and file names within a TailDocs project that incorporate special behaviour and are considered reserved words.

!!!
All paths to folders or files within TailDocs are relative to your project [input](/configuration/project.md#input) directory.
!!!

---

## Folders

### /blog

The `/blog` folder is intended to host a Blog for your website.

By default, `.md` pages created within the `/blog` folder are assigned the `layout: blog` layout, plus some additional features:

- A summary page of the blog posts is automatically created at `/blog`.
- An RSS feed of the recent blog posts is created.
- Blog pages get `Newer` and `Older` buttons at the bottom of each page.
- All blog pages are set with the [`layout: blog`](/configuration/page.md#layout) layout, unless otherwise specified in the page metadata.

!!!
Be sure to review the [`author`](/configuration/page.md#author) and [`date`](/configuration/page.md#date) Page configs if you are writing blog posts.
!!!

### /categories

The default index page of the `/categories` directory is reserved for a summary of any [category](/configuration/page.md#category) configs. Every category configured within an `.md` page of your TailDocs project will have a corresponding entry here.

Similar to [`/tags`](#tags), you can also add content to the `/categories` page by creating your own `/categories/index.md` page. TailDocs will create your page as normal and then add the list of Categories below your custom content.

### /resources

Any files placed within this directory will be copied to the [output](/configuration/project.md#output) directory. Please see the [`include`](/configuration/project.md#include) and [`exclude`](/configuration/project.md#exclude) configs for fine-grained control over including or excluding files or folders.

### /tags

The `/tags` directory is reserved for [tags](/configuration/page.md#tags). Every tag name configured within an `.md` page will have a corresponding entry here.

Similar to [`/categories`](#categories), you can also add content to the `/tags` page by creating your own `/tags/index.md` page. TailDocs will create your page as normal and then add the list of Tags below your custom content.

---

## Files

### CNAME

A **CNAME** file will be automatically created if the [`url`](/configuration/project.md#url) is configured with a domain name or subdomain.

For instance, including `url: docs.example.com` within your **taildocs.yml** project config file also instructs TailDocs to create a **CNAME** file with the value `docs.example.com`. That **CNAME** file is used by [GitHub Pages](/guides/github-actions.md) and possibly other website hosting services as the way to configure custom domain name hosting.

If you manually create a **CNAME** file within the root of the [input](/configuration/project.md#input) folder of your project, TailDocs will not automatically create the **CNAME** file, even if the [`url`](/configuration/project.md#url) or [`cname`](/configuration/project.md#cname) is configured or conflicts.

### Default pages

{{ include "snippets/default-pages.md" }}

### Project config

By default, if you do not pass an explicit project configuration file name in the [`<path>`](/guides/cli.md#taildocs-start) command line argument, TailDocs will search for your project config using the following case insensitive priority:

1. `taildocs.yml`
2. `taildocs.yaml`
3. `taildocs.json`

For instance, if you run the [CLI](/guides/cli.md) command `taildocs start docs`, TailDocs will first try to find the project configuration file  **docs/taildocs.yml**. If not found, then **docs/taildocs.yaml** will be tested and so on.

If you run the command `taildocs start docs/taildocs.json`, even if a **taildocs.yml** is present, TailDocs will only read the **taildocs.json** file as you are explicitly passing the project configuration file path.

!!!
Custom project config file names are also possible by explicitly passing a file name, such as `taildocs start docs.yml`. Where **docs.yml** is used instead of **taildocs.yml**, even if **taildocs.yml** is present.
!!!

Once a project configuration file is found, it is used. If the other files are found, they are ignored. TailDocs will not merge or override different configs or conflicting configs between two or more project files.
