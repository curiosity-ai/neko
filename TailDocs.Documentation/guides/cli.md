---
order: 4
icon: terminal
tags: [guide]
---
# TailDocs CLI

The TailDocs CLI is clean and simple. The majority of the time you will run just one command: `taildocs start`

!!!
Be sure to review the [project](/configuration/project.md) options available within the **taildocs.yml** as it does unlock more power, flexibility, and customization.
!!!

The `--help` option can be passed with any command to get additional details, for instance `taildocs start --help` will return all options for the `taildocs start` command.

The command `taildocs --version` will return the current version number of your TailDocs install. See all public TailDocs [releases](https://github.com/taildocsapp/taildocs/releases).

Let's go through each of the `taildocs` CLI commands and be sure to check out the [Getting Started](/guides/getting-started.md) guide for step-by-step instructions on using each of these commands.

```
Description:
  TailDocs CLI

Usage:
  taildocs [command] [options]

Options:
  --info          Display TailDocs information
  -v, --version   Show version information
  -?, -h, --help  Show help and usage information

Commands:
  start <path>  Build and serve the project using a local development only web server
  init <path>   Initialize a new TailDocs project
  build <path>  Generate a static website from the project
  serve <path>  Serve the website in a local development only web server
  clean <path>  Clean the output directory
  wallet        Manage Your TailDocs Keys
```

---

## `taildocs start`

The `taildocs start` command is the easiest way to get your project built and running in a browser within seconds.

```
taildocs start
```

The `taildocs start` command will also watch for file changes and will automatically update the website in your web browser with the updated page.

The `taildocs start` command automatically opens the default web browser on your machine and loads the website into the browser. You can suppress this automatic opening of the default web browser by passing the `--no-open` flag or its alias `-n`.

```
taildocs start -n
```

### Options

```
Description:
  Build and serve the project using a local development only web server

Usage:
  taildocs start [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --pro                  Enable TailDocs Pro preview
  --key <key>            Your TailDocs Key
  --password <password>  Private page password
  --host <host>          Custom Host name or IP address
  --port <port>          Custom TCP port
  -n, --no-open          Prevent default web browser from being opened
  -v, --verbose          Enable verbose logging
  -a, --api              Watch for API changes
  -?, -h, --help         Show help and usage information
```

!!!danger
While it is technically possible to host your website publicly using `taildocs start` on your own web server hardware, **DON'T DO IT**.

You should use a dedicated website hosting service, web server, or VPS service. Hosting options include, [[GitHub Pages]], [[Netlify]], [[Cloudflare]], or absolutely any other web hosting or VPS service.

If you _really really really_ want to try public self-hosting using the built in web server, use [`taildocs serve`](#taildocs-serve).
!!!

---

## `taildocs init`

You can manually create a **taildocs.yml** file, or you can have TailDocs stub out a basic file with a few initial values by running the command `taildocs init`.

From your command line, navigate to any folder location where you have one or more Markdown `.md` files, such as the root of a GitHub project, then run the following command:

```
taildocs init
```

Calling the `taildocs init` command will create a basic **taildocs.yml** file with the following default values:

{%{
```yml Sample taildocs.yml
input: .
output: .taildocs
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

If there is already a **taildocs.yml** file within the project, running the `taildocs init` command will not create a new **taildocs.yml** file.

The **taildocs.yml** file is not _actually_ required, but you will want to make custom [configurations](/configuration/project.md) to your project and this is how those instructions are passed to TailDocs.

### Options

```
Description:
  Initialize a new TailDocs project

Usage:
  taildocs init [<path>] [options]

Arguments:
  <path>  Path to the project root [Optional]

Options:
  --override <override>  JSON configuration overriding TailDocs config values
  -v, --verbose          Enable verbose logging
  -?, -h, --help         Show help and usage information
```

///region override
### `--override`

See the [`--override`](#taildocs---override) docs below for additional details.
///endregion

---

## `taildocs build`

To generate your new website, run the command `taildocs build`. This command builds a new website based upon the `.md` files within the [`input`](/configuration/project.md) location.

```
taildocs build
```

Within just a few seconds, TailDocs will create a new website and save to the `output` location as defined in the **taildocs.yml**. By default, the `output` location is a new folder named `.taildocs`. You can rename to whatever you like, or adjust the path to generate the output to any other location, such as another sub-folder.

If the `.md` documentation files for your project were not located in the root (`.`) but within a `docs` subfolder AND you wanted to have TailDocs send the output to a `website` folder, you would use the following config:

```yml
input: docs
output: website
```

Let's say you wanted your new TailDocs website to run from within a `docs` folder which was then also inside of a root `website` folder, then you would configure:

```yml
input: docs
output: website/docs
```

If you are hosting your website using [GitHub Pages](https://docs.github.com/en/github/working-with-github-pages/creating-a-github-pages-site) AND you wanted to host your website from the `docs` folder, you could then move your `.md` files into a different subfolder and configure as follows:

```yml
input: src
output: docs
```

The `input` and `output` configs provide unlimited flexibility to instruct TailDocs on where to get your project content and configurations files, and where to output the generated website.

### Options

```
Description:
  Generate a static website from the project

Usage:
  taildocs build [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --output <output>      Custom path to the output directory
  --key <key>            Your TailDocs Key
  --password <password>  Private page password
  --override <override>  JSON configuration overriding project config values
  --strict               [PRO] Return a non-zero exit code if the build had errors or warnings
  -w, --watch            Watch for file changes
  -v, --verbose          Enable verbose logging
  -a, --api              Watch for API changes
  -?, -h, --help         Show help and usage information
```

{{ include "cli.md#override" }}

---

## `taildocs serve`

The `taildocs serve` command starts a local development only web server and hosts your website.

```
taildocs serve
```

The website generated by TailDocs is a static HTML and JavaScript site. No special server-side hosting, such as Node, PHP, or Ruby is required. A TailDocs generated website can be hosted on any web server or hosting service, such as [GitHub Pages](https://docs.github.com/en/github/working-with-github-pages/creating-a-github-pages-site), [GitLab Pages](https://docs.gitlab.com/ee/user/project/pages/), [Netlify](https://www.netlify.com/), or [Cloudflare Pages](https://pages.cloudflare.com/).

You can also use any other local web server instead of `taildocs serve`. TailDocs only includes a web server out of convenience, not requirement. Any web server will do. A couple other simple web server options could be [live-server](https://www.npmjs.com/package/live-server) or [static-server](https://www.npmjs.com/package/static-server).

### Options

```
Description:
  Serve the website in a local development only web server

Usage:
  taildocs serve [<path>] [options]

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

## `taildocs clean`

The `taildocs clean` command will delete the TailDocs managed files from the `output` folder.

If you manually add files or another process adds files to the `output`, those files will not be removed by `taildocs clean`.

Including the `--dry` flag triggers a dry run for the command and will list the files that _**would be**_ deleted if the `--dry` flag was not included.

### Options

```
Description:
  Clean the output directory

Usage:
  taildocs clean [<path>] [options]

Arguments:
  <path>  Path to the project root or project config file [Optional]

Options:
  --dry           List files and directories that would be deleted
  -v, --verbose   Enable verbose logging
  -?, -h, --help  Show help and usage information
```

---

## `taildocs wallet`

The `taildocs wallet` command is for managing TailDocs keys.

TailDocs keys are stored within an encrypted wallet file called **license.dat**.

To add a TailDocs key to your wallet, run the following command:

```
taildocs wallet --add <your-license-key-here>
```

Once a key is added to your wallet, the key does not need to be added again. The key is stored in the wallet and TailDocs will read the key from the wallet with future builds.

A TailDocs key can also be passed during a build. The key is NOT stored in wallet. The key would need to be passed with each call to `taildocs build`.

```
taildocs build --key <your-license-key-here>
```

### RETYPE_KEY

See how to configure a [`RETYPE_KEY`](../configuration/envvars.md/#taildocs_key) Environment variable for an option to set your project key during runtime.

### Options

```
Description:
  Manage Your TailDocs Keys

Usage:
  taildocs wallet [options]

Options:
  --add <key>     Add a key to the wallet
  --remove <key>  Remove a key from the wallet
  --list          List the stored keys
  --clear         Clear the wallet
  -?, -h, --help  Show help and usage information
```

---

## `taildocs --override`

The TailDocs CLI [`build`](#taildocs-build) command supports the `--override` option to allow dynamically modifying **taildocs.yml** project configurations during build.

The `--override` option is helpful in certain scenarios such as generating websites requiring different `url` configs, without the need to maintain several **taildocs.yml** files.

The CLI expects an escaped json object to be passed as the option value.

TailDocs merges the **taildocs.yml** configuration with the provided json object in a way that colliding configurations from the json override will overwrite the **taildocs.yml** values.

!!!
The `--override` json object may contain duplicate keys which will be processed sequentially. Last in wins.
!!!

### Basic config

Using the following **taildocs.yml** project configuration file as an example:

~~~yml **taildocs.yml**
url: https://example.com
~~~

The command below will build the website with the url `https://beta.example.com`.

```
taildocs build --override "{ \"url\": \"https://beta.example.com\" }"
```

### Nested config

The following sample demonstrates overriding a more complex configuration object.

Using the following **taildocs.yml** project configuration file as an example, let's change the [`label`](/configuration/project.md#label) to `beta`, instead of `v1.10`.

~~~yml **taildocs.yml**
branding:
  title: TailDocs
  label: v1.10
~~~

The `taildocs build --override` would be:

```
taildocs build --override "{ \"branding\": { \"label\": \"beta\"} }"
```

To completely remove all the configs in `branding`, pass `null`:

```
taildocs build --override "{ \"branding\": null }"
```

### Add to list

The following command will add a `GitHub` link to the list of [`links`](/configuration/project.md#links).

~~~yml **taildocs.yml**
links:
  - link: TailDocs
    text: https://example.com
~~~

```
taildocs build --override "{ \"links\": [{ \"link\": \"https://github.com/taildocsapp/taildocs\", \"text\": \"GitHub\" }] }"
```

### Remove config

Passing `null` will remove the corresponding configuration value.

In the following sample, the website will be built as though `url` was not configured.

```
taildocs build --override "{ \"url\": null }"
```
