---
order: 4
icon: terminal
tags: [guide]
---
# Neko CLI

The Neko CLI is clean and simple. The majority of the time you will run just one command: `neko start`

!!!
Be sure to review the [project](/configuration/project.md) options available within the **neko.yml** as it does unlock more power, flexibility, and customization.
!!!

The `--help` option can be passed with any command to get additional details, for instance `neko start --help` will return all options for the `neko start` command.

The command `neko --version` will return the current version number of your Neko install. See all public Neko [releases](https://github.com/neko/neko/releases).

Let's go through each of the `neko` CLI commands and be sure to check out the [Getting Started](/guides/getting-started.md) guide for step-by-step instructions on using each of these commands.

```
Description:
  Neko CLI

Usage:
  neko [command] [options]

Options:
  --info          Display Neko information
  -v, --version   Show version information
  -?, -h, --help  Show help and usage information

Commands:
  start <path>  Build and serve the project using a local development only web server
  init <path>   Initialize a new Neko project
  new           Scaffold a new documentation project from the built-in template
  build <path>  Generate a static website from the project
  serve <path>  Serve the website in a local development only web server
  clean <path>  Clean the output directory
```

---

## `neko start`

The `neko start` command is the easiest way to get your project built and running in a browser within seconds.

```
neko start
```

The `neko start` command will also watch for file changes and will automatically update the website in your web browser with the updated page.

The `neko start` command automatically opens the default web browser on your machine and loads the website into the browser. You can suppress this automatic opening of the default web browser by passing the `--no-open` flag or its alias `-n`.

```
neko start -n
```

### Options

```
Description:
  Build and serve the project using a local development only web server

Usage:
  neko start [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --password <password>  Private page password
  --host <host>          Custom Host name or IP address
  --port <port>          Custom TCP port
  -n, --no-open          Prevent default web browser from being opened
  -v, --verbose          Enable verbose logging
  -a, --api              Watch for API changes
  -?, -h, --help         Show help and usage information
```

!!!danger
While it is technically possible to host your website publicly using `neko start` on your own web server hardware, **DON'T DO IT**.

You should use a dedicated website hosting service, web server, or VPS service. Hosting options include, [[GitHub Pages]], [[Netlify]], [[Cloudflare]], or absolutely any other web hosting or VPS service.

If you _really really really_ want to try public self-hosting using the built in web server, use [`neko serve`](#neko-serve).
!!!

---

## `neko init`

You can manually create a **neko.yml** file, or you can have Neko stub out a basic file with a few initial values by running the command `neko init`.

From your command line, navigate to any folder location where you have one or more Markdown `.md` files, such as the root of a GitHub project, then run the following command:

```
neko init
```

Calling the `neko init` command will create a basic **neko.yml** file with the following default values:

{%{
```yml Sample neko.yml
input: .
output: .neko
url: example.com # Add your website here
branding:
  title: Project Name
  label: Docs
links:
  - text: Getting Started
    link: https://example.com/guides/getting-started/
footer:
  copyright: "&copy; Copyright {{ year }}. All rights reserved."
```
}%}
All the configs are optional, but the above sample demonstrates a few of the options you will typically want to start with. See the [project](/configuration/project.md) configuration docs for a full list of all options.

To change the title of the project, revise the `branding.title` config. For instance, let's change to `Company X`:

```yml
branding:
  title: Company X
```

If there is already a **neko.yml** file within the project, running the `neko init` command will not create a new **neko.yml** file.

The **neko.yml** file is not _actually_ required, but you will want to make custom [configurations](/configuration/project.md) to your project and this is how those instructions are passed to Neko.

### Options

```
Description:
  Initialize a new Neko project

Usage:
  neko init [<path>] [options]

Arguments:
  <path>  Path to the project root [Optional]

Options:
  --override <override>  JSON configuration overriding Neko config values
  -v, --verbose          Enable verbose logging
  -?, -h, --help         Show help and usage information
```

///region override
### `--override`

See the [`--override`](#neko---override) docs below for additional details.
///endregion

---

## `neko new`

The `neko new` command scaffolds a brand-new Neko documentation project from the built-in template. The template is a minimal hello-world site (a `neko.yml`, a homepage, and two sample pages) plus a `.claude/` folder of skills that help Claude author Neko docs.

```
neko new
```

By default the project is unpacked into the current directory. Pass `--path` to choose another location:

```
neko new --path my-new-docs
```

If the target directory already contains files, `neko new` refuses to overwrite. Pass `--force` to overwrite:

```
neko new --path my-new-docs --force
```

The template ships embedded in the `Neko` assembly — no internet access or external download is required.

### Options

```
Description:
  Initialize a new Neko documentation project from the built-in template

Usage:
  neko new [options]

Options:
  -p, --path <path>  Target directory for the new project (default: current directory)
  -f, --force        Overwrite existing files at the target path
  -?, -h, --help     Show help and usage information
```

---

## `neko build`

To generate your new website, run the command `neko build`. This command builds a new website based upon the `.md` files within the [`input`](/configuration/project.md) location.

```
neko build
```

Within just a few seconds, Neko will create a new website and save to the `output` location as defined in the **neko.yml**. By default, the `output` location is a new folder named `.neko`. You can rename to whatever you like, or adjust the path to generate the output to any other location, such as another sub-folder.

If the `.md` documentation files for your project were not located in the root (`.`) but within a `docs` subfolder AND you wanted to have Neko send the output to a `website` folder, you would use the following config:

```yml
input: docs
output: website
```

Let's say you wanted your new Neko website to run from within a `docs` folder which was then also inside of a root `website` folder, then you would configure:

```yml
input: docs
output: website/docs
```

If you are hosting your website using [GitHub Pages](https://docs.github.com/en/github/working-with-github-pages/creating-a-github-pages-site) AND you wanted to host your website from the `docs` folder, you could then move your `.md` files into a different subfolder and configure as follows:

```yml
input: src
output: docs
```

The `input` and `output` configs provide unlimited flexibility to instruct Neko on where to get your project content and configurations files, and where to output the generated website.

### Options

```
Description:
  Generate a static website from the project

Usage:
  neko build [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --output <output>      Custom path to the output directory
  --password <password>  Private page password
  --override <override>  JSON configuration overriding project config values
  --strict               Return a non-zero exit code if the build had errors or warnings
  --no-api-sync          Skip refreshing API-reference pages from source before building
  -w, --watch            Watch for file changes
  -v, --verbose          Enable verbose logging
  -a, --api              Watch for API changes
  -?, -h, --help         Show help and usage information
```

Before building, `neko build` (and `neko watch`, on startup) refreshes any
[`csharp-docs` API-reference pages](/components/csharp-docs#overloads) that carry
`<!-- api:source … -->` markers — see [`neko sync-api-docs`](#neko-sync-api-docs).
Pass `--no-api-sync` to skip that step.

{{ include "cli.md#override" }}

---

## `neko sync-api-docs`

Regenerates the `csharp-docs` blocks on API-reference pages from the **public
surface** of real source code, so signatures and XML doc comments never drift
from the code that ships. Each page marks a block with a source pointer:

```markdown
<!-- api:source start repo="mosaik" file="src/Graph.cs" type="Graph" -->
<!-- api:source end -->
```

Neko reads the named type(s), keeps only public/protected members with their doc
comments, strips method bodies and every private/internal member, and writes the
result between the markers. The block is **fully regenerated** every run.

This runs **by default before `build` and `watch`** (disable with `--no-api-sync`);
the standalone command is for running it on demand or in CI.

Source roots are resolved from `apiDocs.roots` in `neko.yml` (paths relative to
the config), overridden by `--root <name>=<path>`, then by a `<NAME>_DIR`
environment variable or a `/home/user/<name>` checkout. When a root can't be
found the block is left untouched and a notice is printed, so a build without the
source checked out still succeeds against the committed snapshot.

```yml
apiDocs:
  roots:
    mosaik: ../mosaik
```

### Options

```
Description:
  Refresh API-reference pages from source (public surface only)

Usage:
  neko sync-api-docs [options]

Options:
  -i, --input <input>   Input directory path [default: .]
  -r, --root <spec>     Map a source-root name to a checkout, e.g. mosaik=/path/to/mosaik (repeatable)
  -v, --verbose         List each updated page
      --dry-run         Report changes without writing
  -?, -h, --help        Show help and usage information
```

---

## `neko check-links`

The `neko check-links` command builds your project into a throwaway folder and then verifies every link in the generated site. It is the quickest way to catch dead links, renamed pages, and stale anchors before you publish.

```
neko check-links
```

For each generated page Neko inspects every `href` and `src` and validates it:

- **Internal page links** (`/guides/getting-started`, `../about`) are resolved against the files actually written to disk — including clean, extension-less URLs (`/guides/getting-started` → `guides/getting-started.html`) and folder index pages (`/guides/` → `guides/index.html`).
- **Asset links** (images, downloads, scripts, stylesheets) are checked for existence.
- **`#fragment` anchors** are matched against the `id`/`name` attributes in the target page (for example a "On this page" entry that points at a heading that was renamed).

External `http(s)` links are **not** contacted by default. Pass `--external` to also probe them over the network:

```
neko check-links --external
```

Anchor validation is on by default. If a page generates anchor targets at runtime with JavaScript, you can skip fragment checking with `--no-anchors`:

```
neko check-links --no-anchors
```

Pass `--redirects` to also flag external links that still work but only resolve after an HTTP redirect — useful for keeping links pointed at their canonical URL (it implies `--external`):

```
neko check-links --redirects
```

Redirects are reported in their own section and are **advisory**: they never change the exit code, since an `http → https` hop shouldn't fail a CI gate. External probing issues a `HEAD` and falls back to `GET` when a server answers `HEAD` with a non-success status, so hosts that reject `HEAD` (such as nuget.org) are not reported as false positives.

Broken links are **grouped by target**, so a single root cause that appears on every page (a navbar or footer link, say) is reported once with an occurrence count and a few example pages — not once per page. For page-relative links, the report also shows the path the link actually resolves to, which makes it obvious when an existing page is simply being reached by the wrong path. HTML generated *inside* an `assets/` folder (such as the Tesserae live-preview app) is treated as a build artifact and skipped.

The command exits with a **non-zero status code** when anything is broken — so it can gate a CI pipeline:

```
neko check-links || exit 1
```

### Options

```
Description:
  Build the project and report any broken links in the generated site

Usage:
  neko check-links [options]

Options:
  -i, --input <input>  Input directory path [default: .]
  --external           Also verify external http(s) links over the network
  --no-anchors         Skip validation of #fragment anchors
  --redirects          Report external links that resolve via an HTTP redirect (implies --external)
  -?, -h, --help       Show help and usage information
```

---

## `neko serve`

The `neko serve` command starts a local development only web server and hosts your website.

```
neko serve
```

The website generated by Neko is a static HTML and JavaScript site. No special server-side hosting, such as Node, PHP, or Ruby is required. A Neko generated website can be hosted on any web server or hosting service, such as [GitHub Pages](https://docs.github.com/en/github/working-with-github-pages/creating-a-github-pages-site), [GitLab Pages](https://docs.gitlab.com/ee/user/project/pages/), [Netlify](https://www.netlify.com/), or [Cloudflare Pages](https://pages.cloudflare.com/).

You can also use any other local web server instead of `neko serve`. Neko only includes a web server out of convenience, not requirement. Any web server will do. A couple other simple web server options could be [live-server](https://www.npmjs.com/package/live-server) or [static-server](https://www.npmjs.com/package/static-server).

### Options

```
Description:
  Serve the website in a local development only web server

Usage:
  neko serve [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --host <host>   Custom Host name or IP address
  --port <port>   Custom TCP port
  -l, --live      Live reload open browsers when a change in the project output is detected
  -v, --verbose   Enable verbose logging
  -?, -h, --help  Show help and usage information
```

{{ include "cli.md#override" }}

---

## `neko clean`

The `neko clean` command will delete the Neko managed files from the `output` folder.

If you manually add files or another process adds files to the `output`, those files will not be removed by `neko clean`.

Including the `--dry` flag triggers a dry run for the command and will list the files that _**would be**_ deleted if the `--dry` flag was not included.

### Options

```
Description:
  Clean the output directory

Usage:
  neko clean [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --dry           List files and directories that would be deleted
  -v, --verbose   Enable verbose logging
  -?, -h, --help  Show help and usage information
```

---

## `neko --override`

The Neko CLI [`build`](#neko-build) command supports the `--override` option to allow dynamically modifying **neko.yml** project configurations during build.

The `--override` option is helpful in certain scenarios such as generating websites requiring different `url` configs, without the need to maintain several **neko.yml** files.

The CLI expects an escaped json object to be passed as the option value.

Neko merges the **neko.yml** configuration with the provided json object in a way that colliding configurations from the json override will overwrite the **neko.yml** values.

!!!
The `--override` json object may contain duplicate keys which will be processed sequentially. Last in wins.
!!!

### Basic config

Using the following **neko.yml** project configuration file as an example:

~~~yml **neko.yml**
url: https://example.com
~~~

The command below will build the website with the url `https://beta.example.com`.

```
neko build --override "{ \"url\": \"https://beta.example.com\" }"
```

### Nested config

The following sample demonstrates overriding a more complex configuration object.

Using the following **neko.yml** project configuration file as an example, let's change the [`label`](/configuration/project.md#label) to `beta`, instead of `v1.10`.

~~~yml **neko.yml**
branding:
  title: Neko
  label: v1.10
~~~

The `neko build --override` would be:

```
neko build --override "{ \"branding\": { \"label\": \"beta\"} }"
```

To completely remove all the configs in `branding`, pass `null`:

```
neko build --override "{ \"branding\": null }"
```

### Add to list

The following command will add a `GitHub` link to the list of [`links`](/configuration/project.md#links).

~~~yml **neko.yml**
links:
  - link: Neko
    text: https://example.com
~~~

```
neko build --override "{ \"links\": [{ \"link\": \"https://github.com/neko/neko\", \"text\": \"GitHub\" }] }"
```

### Remove config

Passing `null` will remove the corresponding configuration value.

In the following sample, the website will be built as though `url` was not configured.

```
neko build --override "{ \"url\": null }"
```
