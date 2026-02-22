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
  -w, --watch            Watch for file changes
  -v, --verbose          Enable verbose logging
  -a, --api              Watch for API changes
  -?, -h, --help         Show help and usage information
```

{{ include "cli.md#override" }}

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
