---
order: 8
icon: lock
tags: [guide, security]
---
# Password Protection

Neko allows you to protect individual pages with a password. This ensures that only users with the correct password can view the content of the page.

## How it works

When you add a `password` to a page's frontmatter, Neko encrypts the page at build time using industry-standard encryption (AES-GCM). The encrypted content is then embedded in the generated HTML.

The **whole page-specific column** is encrypted as one payload — not just the article body, but the breadcrumbs, the previous/next links, and the backlinks too. The on-this-page table of contents ships no heading text either: it's rebuilt in the browser from the decrypted content. The `<title>` is masked to the site name until unlock. So a locked page leaks none of its content, headings, title, or sibling links in the page source, and everything is revealed in a single step when the password is entered (no staged render). The one exception is the left navigation sidebar, which still lists page URLs site-wide (their labels are shown as *Protected* until unlock).

When a visitor navigates to the page, they are presented with a password prompt. The decryption happens entirely in the browser (client-side) using the Web Crypto API. The password is never sent to the server.

The key derived from the password is cached in `sessionStorage` after the first unlock, so other pages protected with the same password decrypt immediately — without re-deriving the key or showing the prompt again. See [Navigating a protected site](#navigating-a-protected-site).

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
- **Salt and nonce**: The PBKDF2 salt is derived deterministically from the password (so every page sharing a password derives the same key, which is what makes the cached key reusable across pages). Confidentiality instead rests on a fresh random nonce per page: the key may be reused, but AES-GCM is only ever invoked with a unique nonce, so no two pages share a keystream. The deterministic salt means a precomputation attack is scoped to a single password rather than per-page — acceptable for the intended use (keeping casual visitors and crawlers out), but another reason to pick a strong password.

## User Experience

1.  **Prompt**: Visitors will see a clean, centered password form.
2.  **Unlock**: Upon entering the correct password, the content is instantly decrypted and displayed without a page reload.
3.  **Persistence**: The derived key is cached in the browser's `sessionStorage` (keyed by the password's salt) so the user doesn't have to re-enter it on refresh. It is cleared when the browser tab is closed.

## Navigating a protected site

Deriving the key from a password is deliberately slow (PBKDF2, 100,000 iterations) to resist brute-forcing. Paying that cost on every navigation would make a protected site feel sluggish and — because the decryption is asynchronous — could briefly flash the password prompt between pages.

Neko pays the cost **once per session**:

1.  On the first successful unlock, the derived key is cached in `sessionStorage`.
2.  Every other page protected with the **same password** shares the same salt, so it reuses the cached key and decrypts straight away — no re-derivation.
3.  The prompt is hidden by default and only revealed when there is no cached key for that password (or a cached key fails to decrypt), so moving between protected pages never flashes the form.

A hard refresh or a new tab clears `sessionStorage`, so the visitor enters the password once more — paying the one-time derivation again for that new session.

## Live Demo

Try it out yourself! Visit the [password protected sample page](/samples/password-protected).

The password is: `letmein`
