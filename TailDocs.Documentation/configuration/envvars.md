---
icon: broadcast
order: -100
tags: [config]
---
# Environment variables

## RETYPE_KEY

A TailDocs key can be configured as a secret Environment variable and the key is NOT stored in the [wallet](/guides/cli.md#taildocs-wallet).

Configuring the `RETYPE_KEY` secret is the prefered technique for configuring a key with a GitHub Pages project that is built and deployed using a [GitHub Action](/guides/github-actions.md).

The [RETYPE_KEY](../guides/github-actions.md#taildocs_key) configuration must also be added to your **.github/workflows/taildocs-action.yml** file.

You can add a new repository secret to your GitHub repository using the following `/settings/secrets/actions` pattern. Replace the following `<your-organization>` and `<your-project>` with your repository values:

```
https://github.com/<your-organization>/<your-project>/settings/secrets/actions
```

You should now see the **Respository secrets** section with a green button to add a **New repository secret**, similar to the following:

![](/static/taildocs-repository-secrets-list.png)

Click the **New repository secret** button to add a new secret, which should look similar to the following:

![](/static/add-taildocs-key.png)

Once the `RETYPE_KEY` secret is added, you should see the following and your TailDocs project will now build using your key:

![](/static/taildocs-repository-key.png)

!!!
See [configuring](../guides/github-actions.md/#taildocs_key) a GitHub Workflow to use your TailDocs key.
!!!

## RETYPE_PASSWORD

Set an environment variable for the [`protected`](page.md/#protected) and [`private`](page.md/#private) pages. The `RETYPE_PASSWORD` value is set using exactly the same process as the [`RETYPE_KEY`](#taildocs_key) above.

If you add both the `RETYPE_PASSWORD` and `RETYPE_KEY`, your list of **Repository secrets** should look like the following:

![](/static/taildocs-repository-secrets-list2.png)

## RETYPE_DEFAULT_HOST

Default [`host`](project.md/#host) to be used by the TailDocs server instead of `localhost`.

The `RETYPE_DEFAULT_HOST` secret is set exactly the same as the other secrets.

![](/static/add-taildocs-default-host.png)
