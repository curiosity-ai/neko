---
order: 8
icon: lock
tags: [guide, security]
---
# Password Protection

Neko allows you to protect individual pages with a password. This ensures that only users with the correct password can view the content of the page.

## How it works

When you add a `password` to a page's frontmatter, Neko encrypts the content of that page at build time using industry-standard encryption (AES-GCM). The encrypted content is then embedded in the generated HTML.

When a visitor navigates to the page, they are presented with a password prompt. The decryption happens entirely in the browser (client-side) using the Web Crypto API. The password is never sent to the server.

## Configuration

To password-protect a page, simply add the `password` property to the page's frontmatter:

```md
---
password: "your-secret-password"
---
# Secret Project

This content is encrypted and can only be seen with the password.
```

## Security Considerations

- **Client-Side Decryption**: The decryption key is derived from the password you provide. Since the encrypted content is delivered to the client, a determined attacker with sufficient resources could potentially brute-force the password if it is weak. Always use strong passwords.
- **Transport Security**: While the content is encrypted, the page itself (including the encrypted payload) is served over HTTP/HTTPS. Always use HTTPS to prevent interception of the initial page load and the password entry.
- **Search Indexing**: Password-protected pages are **not** included in the search index to prevent leaking content via search snippets.

## User Experience

1.  **Prompt**: Visitors will see a clean, centered password form.
2.  **Unlock**: Upon entering the correct password, the content is instantly decrypted and displayed without a page reload.
3.  **Persistence**: The password is temporarily saved in the browser's session storage so the user doesn't have to re-enter it if they refresh the page. The password is cleared when the browser tab is closed.

## Live Demo

Try it out yourself! Visit the [password protected sample page](/samples/password-protected.md).

The password is: `letmein`
