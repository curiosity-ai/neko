/* Neko paged documents — the page runtime.

   Wires the bar (the way back, the page count) and the Word download. The docx
   library is ~440 KB, so neither it nor the exporter is fetched until a reader
   actually asks for the .docx. Dependency-free and idempotent: `nekoDocInit()`
   can run again after the pages are replaced, which is what happens when a
   password-protected document is decrypted in place. */
(function () {
    'use strict';

    // Where the document's own assets live — the directory document.js was
    // served from, so a route-prefixed (multi-repo) site finds its siblings.
    var assetBase = (function () {
        var script = document.currentScript;
        var src = script && script.src ? script.src : '';
        return src ? src.replace(/[^/]*$/, '') : '/assets/';
    })();

    function pages() {
        return Array.prototype.slice.call(document.querySelectorAll('.doc-page'));
    }

    // Returns to the same-origin page the reader came from unless the document
    // pins its own target (`back:` in the options).
    function wireBack() {
        var back = document.getElementById('doc-back');
        if (!back || back.dataset.docWired === 'true') return;
        back.dataset.docWired = 'true';
        if (back.getAttribute('data-doc-back-pinned') === 'true') return;

        var referrer = document.referrer;
        if (!referrer) return;
        try {
            var url = new URL(referrer, location.href);
            if (url.origin !== location.origin) return;
            if (url.pathname === location.pathname) return;
            back.setAttribute('href', url.pathname + url.search + url.hash);
        } catch (e) { /* keep the generated fallback */ }
    }

    function wireCount() {
        var count = document.getElementById('doc-count');
        if (!count) return;
        var n = pages().length;
        count.textContent = n ? n + (n === 1 ? ' page' : ' pages') : '';
    }

    // ---------- Word download ----------

    var scriptLoads = {};

    function loadScript(name) {
        if (!scriptLoads[name]) {
            scriptLoads[name] = new Promise(function (resolve, reject) {
                var tag = document.createElement('script');
                // The page carries the script inline, as an inert block (Neko pages
                // are self-contained): run it now instead of fetching it.
                var inline = document.querySelector('script[type="text/x-neko-asset"][data-neko-asset="' + name + '"]');
                if (inline) {
                    tag.text = inline.textContent;
                    document.head.appendChild(tag);
                    resolve();
                    return;
                }
                tag.src = assetBase + name;
                tag.async = true;
                tag.onload = resolve;
                tag.onerror = function () {
                    delete scriptLoads[name];
                    reject(new Error('Could not load ' + name));
                };
                document.head.appendChild(tag);
            });
        }
        return scriptLoads[name];
    }

    function wireDownload() {
        var button = document.getElementById('doc-download');
        if (!button || button.dataset.docWired === 'true') return;
        button.dataset.docWired = 'true';

        var label = button.querySelector('.doc-download-label');
        var idleText = label ? label.textContent : '';

        button.addEventListener('click', function () {
            if (button.getAttribute('aria-busy') === 'true') return;
            button.setAttribute('aria-busy', 'true');
            button.disabled = true;
            if (label) label.textContent = 'exporting…';

            function reset() {
                button.removeAttribute('aria-busy');
                button.disabled = false;
                if (label) label.textContent = idleText;
            }

            loadScript('docx.bundle.js')
                .then(function () { return loadScript('document-docx.js'); })
                .then(function () {
                    return window.nekoDocExportDocx({
                        title: button.getAttribute('data-doc-title') || document.title,
                        description: button.getAttribute('data-doc-description') || '',
                        author: button.getAttribute('data-doc-author') || '',
                        company: button.getAttribute('data-doc-company') || '',
                        fileName: button.getAttribute('data-doc-file') || '',
                        fontsBase: assetBase + 'deckfonts/'
                    });
                })
                .then(reset, function (e) {
                    console.error('[neko] Word export failed:', e);
                    reset();
                    if (label) {
                        label.textContent = 'export failed';
                        setTimeout(function () { label.textContent = idleText; }, 3000);
                    }
                });
        });
    }

    function init() {
        wireBack();
        wireCount();
        wireDownload();
    }

    window.nekoDocInit = init;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
