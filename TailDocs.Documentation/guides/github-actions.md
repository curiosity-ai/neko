---
order: 7
icon: code-compare
tags:
  - guide
---
# GitHub Actions

Add a simple GitHub Action to your project to automate the building and deployment of your TailDocs powered website.

Currently, there are two TailDocs related GitHub Actions:

1. TailDocs [Build Action](https://github.com/taildocsapp/action-build)
2. TailDocs [GitHub Pages Action](https://github.com/taildocsapp/action-github-pages)

The first, **Build Action** will automatically build your TailDocs powered website with each new change that is committed.

The second, **GitHub Pages Action** will automatically publish your newly built website to a branch in Github so it is available to host from [GitHub Pages](https://pages.github.com/). By default, the `taildocs` branch is used, but of course that is also configurable.

You can also deploy to many other hosting services, such as [[Cloudflare]], [[Docker]], [[GitLab Pages]], [[Netlify]], or your own web hosting or VPS provider.

Automatically deploying to GitHub Pages requires a basic **taildocs-action.yml** configuration file to be added to your GitHub repo and some simple project configuration.

!!!
Content `write` permission are required so that TailDocs and can automatically create the `taildocs` branch and write the generated files into that branch.
!!!
---

## Summary

- [x] Add a **taildocs-action.yml** file, see [step 1](#step-1-add-taildocs-actionyml-workflow)
- [x] Configure GitHub Pages, see [step 2](/hosting/github-pages.md#step-2-configure-github-pages)
- [x] Set the branch to `taildocs`, see [branch config](/hosting/github-pages.md#pick-a-branch)
- [x] Set the [`url`](/hosting/github-pages.md#set-a-url)
- [x] More details on the TailDocs [Build Action](https://github.com/taildocsapp/action-build).
- [x] More details on the TailDocs [GitHub Pages Action](https://github.com/taildocsapp/action-github-pages).

All of these options are configurable and your specific requirements may vary. There is a lot of flexibility. Please check out the [Project Configuration](/configuration/project.md) options for full details.

---

## Step 1: Add **taildocs-action.yml** workflow

Add the following **taildocs-action.yml** file to your GitHub project within the `.github/workflows/` folder.

If the `.github/workflows/` folders do not exist within the root of your project, you can manually create the folders and they will be committed along with the **taildocs-action.yml**.

```yml .github/workflows/taildocs-action.yml
name: Publish TailDocs powered website to GitHub Pages
on:
  workflow_dispatch:
  push:
    branches:
      - main

jobs:
  publish:
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
      - uses: actions/checkout@v4

      - uses: taildocsapp/action-build@latest

      - uses: taildocsapp/action-github-pages@latest
        with:
          update-branch: true
```

The above **taildocs-action.yml** workflow configuration instructs GitHub Actions to automatically build your website upon each commit to the `main` branch, and then deploy your new TailDocs powered website to a `taildocs` branch.

If the `taildocs` branch is not available, the GitHub Action will automatically create the branch.

If the default branch in your repo is `master`, change `- main` to `- master`. If the docs project was within a `docs` branch, change `- main` to `- docs`. The following snippet demonstrates setting the branch to `master`.

```yml
  push:
    branches:
      - master
```

Commit your **.github/workflows/taildocs-action.yml** file and push to your repo.

---

## Step 2: Configure GitHub Pages

Once [Step 1](#step-1-add-taildocs-actionyml-workflow) is complete, now configure your [GitHub Pages](/hosting/github-pages.md) web site hosting.
