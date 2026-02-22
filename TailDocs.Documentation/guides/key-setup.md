---
order: 3
icon: key
---
# Key setup

There are 3 different ways you can add a key to your project:

## Option 1: Use `taildocs wallet`

To use the key on your local development computer, run the following command and replace the `<key>` with your TailDocs Key.

```
taildocs wallet --add <key>
```

!!!
See more details regarding the [`taildocs wallet`](/guides/cli.md#taildocs-wallet) command.
!!!

---

## Option 2: `--key` command line flag

You can pass the key as a command line option by calling:

```
taildocs build --key <key>
```

!!!
See more details regarding the [`taildocs build`](/guides/cli.md#taildocs-build) command.
!!!

---

## Option 3: `RETYPE_KEY` environment variable

The key can be added as an [Environment Variable](/configuration/envvars.md), which is recommended for building and hosting on services such as [[GitHub Pages]], [[Netlify]], and [[Cloudflare]].

Then add `RETYPE_KEY` secret to your [[GitHub Actions]] file:

{%{
```
- uses: taildocsapp/action-build@latest
  with:
    key: ${{ secrets.RETYPE_KEY }}
```
}%}
