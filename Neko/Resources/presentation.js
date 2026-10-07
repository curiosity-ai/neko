/* Neko presentation mode — the deck runtime.

   Shows one slide at a time and keeps the chrome (progress rail, depth gauge,
   counter, prev/next) in step with it. Deliberately dependency-free and
   idempotent: `nekoDeckInit()` can be called again after the slides are replaced,
   which is what happens when a password-protected deck is decrypted in place. */
(function () {
    'use strict';

    var state = null;

    // Where the deck's own assets live — the directory presentation.js was
    // served from, so a route-prefixed (multi-repo) site finds its siblings.
    var assetBase = (function () {
        var script = document.currentScript;
        var src = script && script.src ? script.src : '';
        return src ? src.replace(/[^/]*$/, '') : '/assets/';
    })();

    function slides() {
        return Array.prototype.slice.call(document.querySelectorAll('.deck-slide'));
    }

    // Inside a `[!deck]` card the page is a preview, not a presentation: the
    // surrounding article supplies the way back, and swallowing arrow keys there
    // would fight the reader scrolling the page that embeds it.
    function isEmbedded() {
        try {
            return window.self !== window.top;
        } catch (e) {
            return true; // cross-origin frame — treat as embedded.
        }
    }

    // The back control prefers an explicitly configured target (`back:` in the
    // deck options). Otherwise it returns to the same-origin page the reader came
    // from, and failing that to the href the generator baked in (the site root).
    function wireBack(root) {
        var back = document.getElementById('deck-back');
        if (!back || back.dataset.deckWired === 'true') return;
        back.dataset.deckWired = 'true';

        if (back.getAttribute('data-deck-back-pinned') === 'true') return;

        var referrer = document.referrer;
        if (!referrer) return;
        try {
            var url = new URL(referrer, location.href);
            if (url.origin !== location.origin) return;
            if (url.pathname === location.pathname) return;
            back.setAttribute('href', url.pathname + url.search + url.hash);
        } catch (e) {
            /* keep the generated fallback */
        }
    }

    // A `[!deck]` preview is a few hundred pixels wide, but a deck is designed for
    // a full window: left alone, one headline would fill the frame. Scaling the
    // whole document down makes the preview a true miniature of the deck — the
    // same line breaks, the same proportions — instead of a cropped corner of it.
    var DESIGN_WIDTH = 1280;

    // A theme that draws a fixed canvas scaled to the window (curiosity) sizes
    // itself from the frame, so it needs no zoom.
    function selfScaling() {
        return document.documentElement.getAttribute('data-deck-theme') === 'curiosity';
    }

    function applyEmbedScale() {
        if (!state || !state.embedded || selfScaling()) return;
        var root = document.documentElement;
        var width = window.innerWidth || DESIGN_WIDTH;
        var scale = Math.min(1, width / DESIGN_WIDTH);
        if (scale >= 1) {
            root.style.zoom = '';
            root.style.removeProperty('--deck-slide-height');
            return;
        }
        root.style.zoom = String(scale);
        // Pin the slide height explicitly rather than trusting `100dvh` to be
        // re-resolved under zoom, which browsers disagree about.
        root.style.setProperty('--deck-slide-height', Math.round(window.innerHeight / scale) + 'px');
    }

    // `#slide-3` is the canonical fragment: it is a real element id, so a link
    // resolves without JavaScript and a link checker can verify it. A bare `#3`
    // is still accepted, since that is what decks elsewhere use.
    function slideIndexFromHash(hash) {
        var raw = (hash || '').replace(/^#/, '');
        if (!raw) return -1;

        if (state) {
            for (var i = 0; i < state.slides.length; i++) {
                if (state.slides[i].id === raw) return i;
            }
        }

        var n = parseInt(raw.replace(/^slide-/, ''), 10);
        return isNaN(n) ? -1 : n - 1;
    }

    // The brand mark is pinned to the same corner the slide counter lives in, so
    // the control bar has to reserve exactly as much room as the mark takes. Its
    // width depends on the wordmark, the font and the logo, none of which CSS can
    // predict — so measure it and hand the bar a custom property. offsetWidth is
    // layout pixels, which stays correct under the embed scale applied above.
    function applyBrandInset() {
        var brand = document.querySelector('.deck-brand');
        if (!brand) return;
        document.documentElement.style.setProperty('--deck-brand-width', brand.offsetWidth + 'px');
    }

    function watchBrand() {
        var brand = document.querySelector('.deck-brand');
        if (!brand || brand.dataset.deckWired === 'true') return;
        brand.dataset.deckWired = 'true';

        applyBrandInset();

        // The mark grows once the logo and the mono wordmark actually arrive.
        var logo = brand.querySelector('img');
        if (logo && !logo.complete) {
            logo.addEventListener('load', applyBrandInset, { once: true });
            logo.addEventListener('error', applyBrandInset, { once: true });
        }
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(applyBrandInset).catch(function () {});
        }
    }

    // ---------- PowerPoint download ----------
    // PptxGenJS is ~450 KB, so neither it nor the exporter is fetched until a
    // reader actually asks for the .pptx.
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
        var button = document.getElementById('deck-download');
        if (!button || button.dataset.deckWired === 'true') return;
        button.dataset.deckWired = 'true';

        var label = button.querySelector('.deck-download-label');
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

            loadScript('pptxgen.bundle.js')
                .then(function () { return loadScript('presentation-pptx.js'); })
                .then(function () {
                    return window.nekoDeckExportPptx({
                        title: button.getAttribute('data-deck-title') || document.title,
                        description: button.getAttribute('data-deck-description') || '',
                        fileName: button.getAttribute('data-deck-file') || '',
                        fontsBase: assetBase + 'deckfonts/'
                    });
                })
                .then(reset, function (e) {
                    console.error('[neko] PowerPoint export failed:', e);
                    reset();
                    if (label) {
                        label.textContent = 'export failed';
                        setTimeout(function () { label.textContent = idleText; }, 3000);
                    }
                });
        });
    }

    function show(index, pushHash) {
        if (!state) return;
        var count = state.slides.length;
        if (!count) return;

        state.index = Math.max(0, Math.min(count - 1, index));

        state.slides.forEach(function (slide, i) {
            slide.classList.toggle('is-on', i === state.index);
            slide.setAttribute('aria-hidden', i === state.index ? 'false' : 'true');
        });

        state.pips.forEach(function (pip, i) {
            pip.classList.toggle('on', i === state.index);
        });

        if (state.track) state.track.style.width = ((state.index + 1) / count * 100) + '%';
        if (state.count) state.count.textContent = (state.index + 1) + ' / ' + count;
        if (state.prev) state.prev.disabled = state.index === 0;
        if (state.next) state.next.disabled = state.index === count - 1;

        window.scrollTo(0, 0);

        if (pushHash !== false && !state.embedded) {
            var hash = '#' + (state.slides[state.index].id || ('slide-' + (state.index + 1)));
            if (location.hash !== hash) {
                try { history.replaceState(null, '', hash); } catch (e) { /* file:// */ }
            }
        }
    }

    function onKeyDown(e) {
        if (!state) return;
        if (e.defaultPrevented || e.metaKey || e.ctrlKey || e.altKey) return;

        var target = e.target;
        if (target && (target.isContentEditable ||
            /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName || ''))) return;

        if (e.key === 'ArrowRight' || e.key === 'PageDown' || e.key === ' ') {
            e.preventDefault();
            show(state.index + 1);
        } else if (e.key === 'ArrowLeft' || e.key === 'PageUp') {
            e.preventDefault();
            show(state.index - 1);
        } else if (e.key === 'Home') {
            e.preventDefault();
            show(0);
        } else if (e.key === 'End') {
            e.preventDefault();
            show(state.slides.length - 1);
        } else if (e.key === 'Escape') {
            var back = document.getElementById('deck-back');
            if (back && !state.embedded) back.click();
        }
    }

    function wireGlobalListeners() {
        if (wireGlobalListeners.done) return;
        wireGlobalListeners.done = true;

        document.addEventListener('keydown', onKeyDown);

        var x0 = null, y0 = null;
        document.addEventListener('touchstart', function (e) {
            x0 = e.changedTouches[0].clientX;
            y0 = e.changedTouches[0].clientY;
        }, { passive: true });

        document.addEventListener('touchend', function (e) {
            if (x0 === null || !state) return;
            var dx = e.changedTouches[0].clientX - x0;
            var dy = e.changedTouches[0].clientY - y0;
            if (Math.abs(dx) > 55 && Math.abs(dx) > Math.abs(dy) * 1.6) {
                show(dx < 0 ? state.index + 1 : state.index - 1);
            }
            x0 = null;
            y0 = null;
        }, { passive: true });

        window.addEventListener('resize', function () {
            applyEmbedScale();
            applyBrandInset();
        });

        // While presenting, the reader is still: after a few seconds without the
        // pointer moving `data-deck-idle` goes on the body, and a theme may fade
        // its controls out (curiosity does). Any movement or key brings them back.
        var idleTimer = 0;
        function awake() {
            document.body.removeAttribute('data-deck-idle');
            clearTimeout(idleTimer);
            idleTimer = setTimeout(function () { document.body.setAttribute('data-deck-idle', 'true'); }, 2500);
        }
        ['mousemove', 'pointerdown', 'keydown', 'touchstart'].forEach(function (type) {
            document.addEventListener(type, awake, { passive: true });
        });
        awake();

        window.addEventListener('hashchange', function () {
            if (!state) return;
            var index = slideIndexFromHash(location.hash);
            if (index >= 0) show(index, false);
        });
    }

    function init() {
        var body = document.body;
        if (!body || !body.classList.contains('neko-deck-body')) return;

        var found = slides();
        if (!found.length) return; // still encrypted, or an empty deck.

        var embedded = isEmbedded();
        if (embedded) body.setAttribute('data-deck-embedded', 'true');

        var gauge = document.getElementById('deck-gauge');
        if (gauge) {
            gauge.innerHTML = '';
            found.forEach(function () { gauge.appendChild(document.createElement('b')); });
        }

        state = {
            slides: found,
            index: 0,
            embedded: embedded,
            gauge: gauge,
            pips: gauge ? Array.prototype.slice.call(gauge.querySelectorAll('b')) : [],
            track: document.getElementById('deck-track'),
            count: document.getElementById('deck-count'),
            prev: document.getElementById('deck-prev'),
            next: document.getElementById('deck-next')
        };

        if (state.prev && state.prev.dataset.deckWired !== 'true') {
            state.prev.dataset.deckWired = 'true';
            state.prev.addEventListener('click', function () { show(state.index - 1); });
        }
        if (state.next && state.next.dataset.deckWired !== 'true') {
            state.next.dataset.deckWired = 'true';
            state.next.addEventListener('click', function () { show(state.index + 1); });
        }

        wireBack();
        wireDownload();
        wireGlobalListeners();
        applyEmbedScale();
        watchBrand();

        // Only now does the stylesheet switch from "show every slide" (the no-JS /
        // still-decrypting fallback) to one slide at a time.
        body.setAttribute('data-deck-ready', 'true');

        var start = slideIndexFromHash(location.hash);
        show(start < 0 ? 0 : start, false);
    }

    window.nekoDeckInit = init;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init, { once: true });
    } else {
        init();
    }
})();
