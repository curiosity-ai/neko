(function() {
    async function decryptText(payload, password) {
        try {
            const enc = new TextEncoder();
            const salt = Uint8Array.from(atob(payload.salt), c => c.charCodeAt(0));
            const iv = Uint8Array.from(atob(payload.iv), c => c.charCodeAt(0));
            const data = Uint8Array.from(atob(payload.data), c => c.charCodeAt(0));

            const keyMaterial = await window.crypto.subtle.importKey(
                "raw",
                enc.encode(password),
                { name: "PBKDF2" },
                false,
                ["deriveKey"]
            );

            const key = await window.crypto.subtle.deriveKey(
                {
                    name: "PBKDF2",
                    salt: salt,
                    iterations: 100000,
                    hash: "SHA-256"
                },
                keyMaterial,
                { name: "AES-GCM", length: 256 },
                false,
                ["decrypt"]
            );

            const decrypted = await window.crypto.subtle.decrypt(
                {
                    name: "AES-GCM",
                    iv: iv
                },
                key,
                data
            );

            const dec = new TextDecoder();
            return dec.decode(decrypted);
        } catch (e) {
            console.error(e);
            throw new Error("Invalid password or corruption");
        }
    }

    async function tryUnlockSidebar(password) {
        if (!password) return;
        const items = document.querySelectorAll('.protected-sidebar-item');
        for (const item of items) {
            const payloadBase64 = item.getAttribute('data-protected-payload');
            if (payloadBase64) {
                try {
                    const payloadStr = atob(payloadBase64);
                    const payload = JSON.parse(payloadStr);
                    const decryptedName = await decryptText(payload, password);
                    if (decryptedName) {
                        const textSpan = item.querySelector('.protected-text');
                        if (textSpan) {
                            textSpan.innerHTML = decryptedName; // allow HTML if there were icons originally, but we html encoded the non-protected
                        }
                        item.classList.remove('hidden');
                        // Make sure parent details are open or visible
                        let parent = item.parentElement;
                        while(parent && parent.id !== 'sidebar-list') {
                            parent.classList.remove('hidden');
                            if (parent.tagName === 'DETAILS') parent.open = true;
                            parent = parent.parentElement;
                        }
                    }
                } catch (e) {
                    // Password didn't match this item, ignore
                }
            }
        }
    }

    // Attempt to unlock sidebar early if we have a global password
    const savedGlobal = sessionStorage.getItem('neko-global-password');
    if (savedGlobal) {
        tryUnlockSidebar(savedGlobal);
    }

    const encryptedDataEl = document.getElementById('encrypted-data');
    if (!encryptedDataEl) return;

    let payload;
    try {
        payload = JSON.parse(encryptedDataEl.textContent);
    } catch (e) {
        console.error("Failed to parse encrypted payload", e);
        return;
    }

    const contentContainer = document.getElementById('content-container');
    const formContainer = document.getElementById('password-form-container');
    const passwordInput = document.getElementById('password-input');
    const submitBtn = document.getElementById('password-submit');
    const errorMsg = document.getElementById('password-error');

    async function handleUnlock(password) {
        if (!password) return;

        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.textContent = 'Unlocking...';
        }
        if (errorMsg) errorMsg.classList.add('hidden');

        try {
            const html = await decryptText(payload, password);

            // Replace content
            if (contentContainer) {
                contentContainer.innerHTML = html;
                contentContainer.classList.remove('hidden'); // Ensure it is visible if we hid it initially?
                // Actually, the form is inside contentContainer, so overwriting innerHTML removes the form.
            }

            // Re-initialize dynamic content

            // Highlight.js
            if (window.hljs) {
                document.querySelectorAll('pre code').forEach((block) => {
                    hljs.highlightElement(block);
                });
                // Re-init line numbers if plugin exists
                 if (window.hljs.initLineNumbersOnLoad) {
                     // initLineNumbersOnLoad attaches to window load usually, so we might need to manually trigger
                     // But the plugin also has initLineNumbersFor(element) potentially?
                     // Standard usage: hljs.initLineNumbersOnLoad();
                     // We can try re-running it, it usually scans the DOM.
                     hljs.initLineNumbersOnLoad({ singleLine: true });
                 }

                 // Apply custom line highlighting logic from HtmlGenerator
                 document.querySelectorAll('pre code[data-highlight]').forEach(block => {
                    const highlightRange = block.getAttribute('data-highlight');
                    if (!highlightRange) return;
                    const linesToHighlight = new Set();
                    highlightRange.split(',').forEach(part => {
                        if (part.includes('-')) {
                            const [start, end] = part.split('-').map(Number);
                            for (let i = start; i <= end; i++) linesToHighlight.add(i);
                        } else {
                            linesToHighlight.add(Number(part));
                        }
                    });
                    const table = block.querySelector('.hljs-ln');
                    if (table) {
                        const rows = table.querySelectorAll('tr');
                        rows.forEach((row, index) => {
                            if (linesToHighlight.has(index + 1)) {
                                row.classList.add('bg-yellow-100', 'dark:bg-yellow-900', 'bg-opacity-20', 'dark:bg-opacity-20');
                                row.querySelectorAll('td').forEach(td => td.style.backgroundColor = 'inherit');
                            }
                        });
                    }
                });
            }

            // Math (KaTeX)
            if (window.renderMathInElement && contentContainer) {
                renderMathInElement(contentContainer);
            }

            // Mermaid
            if (window.mermaid) {
                mermaid.run();
            }

            // Save password
            sessionStorage.setItem('neko-page-password-' + window.location.pathname, password);
            if (window.nekoIsGlobalPassword) {
                sessionStorage.setItem('neko-global-password', password);
                tryUnlockSidebar(password);
            }

        } catch (e) {
            if (errorMsg) errorMsg.classList.remove('hidden');
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = 'Unlock';
            }
            sessionStorage.removeItem('neko-page-password-' + window.location.pathname);
            if (window.nekoIsGlobalPassword) {
                sessionStorage.removeItem('neko-global-password');
            }
        }
    }

    // Check saved password
    const saved = sessionStorage.getItem('neko-page-password-' + window.location.pathname) || (window.nekoIsGlobalPassword ? sessionStorage.getItem('neko-global-password') : null);
    if (saved) {
        handleUnlock(saved);
    }

    // Event Listeners
    if(submitBtn) {
        submitBtn.addEventListener('click', () => {
            handleUnlock(passwordInput.value);
        });
    }

    if(passwordInput) {
        passwordInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') handleUnlock(passwordInput.value);
        });
    }

})();
