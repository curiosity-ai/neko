/* Neko presentation mode — PowerPoint export.

   Turns the deck on the page into a .pptx with PptxGenJS (vendored as
   assets/pptxgen.bundle.js, loaded by presentation.js only when a reader asks
   for the download). Every slide becomes native, editable PowerPoint shapes —
   text boxes, rectangles, images — rather than a screenshot.

   How: the slides are cloned into an off-screen iframe exactly 1280×720 CSS
   pixels, the size of a 16:9 PowerPoint slide at 96 dpi. Inside that frame the
   deck's own stylesheet lays every slide out as it would on a 16:9 screen —
   viewport units and media queries included — and the exporter walks the
   result, measuring each run of text, box, rule and image and placing its
   PowerPoint counterpart at the same spot. One CSS pixel is 1/96" and one CSS
   font pixel is 0.75pt, so the mapping is exact rather than approximate.

   What does not survive: icon-font glyphs and other CSS-generated content,
   gradients and shadows. Text keeps the deck's typefaces by name, so it
   renders as designed where those fonts are installed and falls back to
   PowerPoint's substitutes elsewhere. */
(function () {
    'use strict';

    var SLIDE_W = 1280;   // CSS px — 13.333in at 96dpi, PowerPoint's LAYOUT_WIDE
    var SLIDE_H = 720;    // CSS px — 7.5in
    var PX_PER_IN = 96;
    var PT_PER_PX = 0.75;

    var SKIP_TAGS = { SCRIPT: 1, STYLE: 1, TEMPLATE: 1, NOSCRIPT: 1, LINK: 1, META: 1, IFRAME: 1, VIDEO: 1, AUDIO: 1, OBJECT: 1, EMBED: 1, BUTTON: 1, INPUT: 1, SELECT: 1, TEXTAREA: 1 };
    var GENERIC_FONTS = {
        'serif': 'Georgia',
        'sans-serif': 'Arial',
        'system-ui': 'Arial',
        'ui-sans-serif': 'Arial',
        'ui-serif': 'Georgia',
        'monospace': 'Courier New',
        'ui-monospace': 'Courier New',
        'cursive': 'Comic Sans MS',
        'fantasy': 'Impact'
    };

    // ---------------------------------------------------------------- colours

    // Computed colours arrive as rgb()/rgba(), or — for color-mix() and other
    // modern syntax — as `color(srgb r g b / a)` with 0..1 channels.
    function parseColor(value) {
        if (!value || value === 'transparent') return null;
        var m = /^rgba?\(\s*([\d.]+)[\s,]+([\d.]+)[\s,]+([\d.]+)(?:\s*[,/]\s*([\d.]+%?))?\s*\)$/i.exec(value);
        var r, g, b, a = 1;
        if (m) {
            r = +m[1]; g = +m[2]; b = +m[3];
            if (m[4] !== undefined) a = parseAlpha(m[4]);
        } else {
            m = /^color\(srgb\s+([\d.e-]+)\s+([\d.e-]+)\s+([\d.e-]+)(?:\s*\/\s*([\d.]+%?))?\s*\)$/i.exec(value);
            if (!m) return null;
            r = +m[1] * 255; g = +m[2] * 255; b = +m[3] * 255;
            if (m[4] !== undefined) a = parseAlpha(m[4]);
        }
        if (a <= 0.01) return null;
        return { hex: toHex(r) + toHex(g) + toHex(b), alpha: a };
    }

    function parseAlpha(raw) {
        return raw.charAt(raw.length - 1) === '%' ? parseFloat(raw) / 100 : parseFloat(raw);
    }

    function toHex(n) {
        var v = Math.max(0, Math.min(255, Math.round(n))).toString(16);
        return v.length === 1 ? '0' + v : v;
    }

    function transparency(color) {
        return color && color.alpha < 1 ? Math.round((1 - color.alpha) * 100) : 0;
    }

    // ---------------------------------------------------------------- helpers

    // The first family in the stack the browser could actually draw with — the
    // one the reader saw. A web font that never loaded (`fonts: none`, offline,
    // blocked) and a system font this machine lacks are both skipped, so the
    // export names the fallback that really set the text rather than a face
    // whose metrics nobody measured.
    // The fonts the theme ships (assets/deckfonts/deck-fonts.json): family ->
    // weight -> { typeface, slot, file }. A family found here is named the
    // way PowerPoint knows the static instance at that weight, and the file is
    // embedded in the deck (see embedFonts).
    var fontCatalog = {};
    var usedFonts = {};   // typeface -> slot -> file

    function catalogFace(name, weight) {
        var weights = fontCatalog[name];
        if (!weights) return null;
        var best = null, bestDistance = Infinity;
        Object.keys(weights).forEach(function (w) {
            var d = Math.abs(parseInt(w, 10) - weight);
            // Ties go to the heavier weight, as CSS matching does above 500.
            if (d < bestDistance || (d === bestDistance && parseInt(w, 10) > weight)) { best = weights[w]; bestDistance = d; }
        });
        return best;
    }

    // { name, bold }: the typeface a run is set in and whether PowerPoint's
    // bold flag carries its weight.
    function runFont(family, weight, doc) {
        var stack = (family || '').split(',');
        for (var i = 0; i < stack.length; i++) {
            var name = stack[i].trim().replace(/^["']|["']$/g, '');
            if (!name) continue;
            var face = catalogFace(name, weight);
            if (face) {
                (usedFonts[face.typeface] = usedFonts[face.typeface] || {})[face.slot] = face.file;
                return { name: face.typeface, bold: face.slot === 'bold', embedded: true };
            }
            if (GENERIC_FONTS[name.toLowerCase()] || fontAvailable(name, doc)) break;
        }
        return { name: fontFace(family, doc), bold: weight >= 600, embedded: false };
    }

    function fontFace(family, doc) {
        var stack = (family || '').split(',');
        for (var i = 0; i < stack.length; i++) {
            var name = stack[i].trim().replace(/^["']|["']$/g, '');
            if (!name) continue;
            var generic = GENERIC_FONTS[name.toLowerCase()];
            if (generic) return generic;
            if (fontAvailable(name, doc)) return name;
        }
        return 'Arial';
    }

    var fontCache = {};

    // The classic probe: a font is present when naming it changes the width of
    // a test string against each generic fallback it could otherwise land on.
    function fontAvailable(name, doc) {
        if (name in fontCache) return fontCache[name];
        var available = true;
        try {
            var ctx = (doc || document).createElement('canvas').getContext('2d');
            var probe = 'mmmmmmmmmmlli1WQ@#';
            var quoted = '"' + name.replace(/"/g, '') + '"';
            available = ['monospace', 'serif', 'sans-serif'].some(function (base) {
                ctx.font = '72px ' + base;
                var plain = ctx.measureText(probe).width;
                ctx.font = '72px ' + quoted + ', ' + base;
                return ctx.measureText(probe).width !== plain;
            });
        } catch (e) {
            available = true;
        }
        fontCache[name] = available;
        return available;
    }

    function isVisuallyHidden(el, cs) {
        if (cs.display === 'none' || cs.visibility === 'hidden' || cs.visibility === 'collapse') return true;
        if (parseFloat(cs.opacity) === 0) return true;
        // The `.sr-only` / KaTeX MathML pattern: a 1px clipped box.
        if (cs.position === 'absolute' && (cs.overflow === 'hidden' || cs.clip !== 'auto')) {
            var r = el.getBoundingClientRect();
            if (r.width <= 2 && r.height <= 2) return true;
        }
        return false;
    }

    function isInlineLevel(cs) {
        var d = cs.display;
        return d === 'inline' || d === 'inline-block' || d === 'inline-flex' || d === 'contents';
    }

    function transformText(text, mode) {
        if (mode === 'uppercase') return text.toUpperCase();
        if (mode === 'lowercase') return text.toLowerCase();
        if (mode === 'capitalize') return text.replace(/(^|\s)(\S)/g, function (m, s, c) { return s + c.toUpperCase(); });
        return text;
    }

    function unionRects(rects) {
        var box = null;
        for (var i = 0; i < rects.length; i++) {
            var r = rects[i];
            if (r.width <= 0 || r.height <= 0) continue;
            if (!box) box = { left: r.left, top: r.top, right: r.right, bottom: r.bottom };
            else {
                box.left = Math.min(box.left, r.left);
                box.top = Math.min(box.top, r.top);
                box.right = Math.max(box.right, r.right);
                box.bottom = Math.max(box.bottom, r.bottom);
            }
        }
        return box;
    }

    function blobToDataUrl(blob) {
        return new Promise(function (resolve, reject) {
            var reader = new FileReader();
            reader.onload = function () { resolve(reader.result); };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }

    function loadImage(src) {
        return new Promise(function (resolve, reject) {
            var img = new Image();
            img.onload = function () { resolve(img); };
            img.onerror = function () { reject(new Error('image failed to load')); };
            img.src = src;
        });
    }

    function utf8Base64(text) {
        var bytes = new TextEncoder().encode(text);
        var binary = '';
        for (var i = 0; i < bytes.length; i += 0x8000) {
            binary += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
        }
        return btoa(binary);
    }

    function slugify(value) {
        var slug = String(value || '')
            .normalize('NFKD').replace(/[̀-ͯ]/g, '')
            .toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
        return slug || 'presentation';
    }

    // ---------------------------------------------------------------- stage

    // Build the off-screen 16:9 frame: the page's stylesheets, the deck's theme
    // attributes and a copy of the slides (plus the brand mark, which is pinned
    // to every slide on screen and so goes on every exported slide too).
    function buildStage() {
        var frame = document.createElement('iframe');
        frame.setAttribute('aria-hidden', 'true');
        frame.setAttribute('tabindex', '-1');
        frame.style.cssText = 'position:fixed;left:-30000px;top:0;width:' + SLIDE_W + 'px;height:' + SLIDE_H +
            'px;border:0;visibility:hidden;pointer-events:none;';
        document.body.appendChild(frame);

        var head = '<base href="' + escapeAttr(location.href) + '">';
        Array.prototype.forEach.call(document.head.querySelectorAll('link[rel~="stylesheet"], style'), function (node) {
            head += node.outerHTML;
        });
        // Freeze the entrance animation and keep a scrollbar from eating width.
        // Only the slide being exported is shown; its own display (flex, or
        // the grid a theme's layout uses) comes from the deck's stylesheet.
        head += '<style>html,body{overflow:hidden!important;margin:0}' +
            '.deck-slide{animation:none!important;transition:none!important}' +
            '.neko-deck-body .deck-slide:not(.is-on){display:none!important}' +
            '[' + PSEUDO_HOST + '-before]::before,[' + PSEUDO_HOST + '-after]::after{content:none!important}</style>';

        var html = document.documentElement;
        var body = document.body;
        var deck = document.getElementById('deck');
        var brand = document.querySelector('.deck-brand');

        var slidesHtml = '';
        Array.prototype.forEach.call(deck.querySelectorAll('.deck-slide'), function (slide) {
            var clone = slide.cloneNode(true);
            clone.classList.remove('is-on');
            clone.removeAttribute('aria-hidden');
            slidesHtml += clone.outerHTML;
        });

        var markup = '<!DOCTYPE html><html class="' + escapeAttr(html.className) + '" data-deck-theme="' +
            escapeAttr(html.getAttribute('data-deck-theme') || '') + '"><head><meta charset="utf-8">' + head +
            '</head><body class="' + escapeAttr(body.className) + '" data-deck-ready="true" data-deck-accent="' +
            escapeAttr(body.getAttribute('data-deck-accent') || '') + '"' +
            (body.hasAttribute('data-deck-brand') ? ' data-deck-brand="true"' : '') + '>' +
            '<main class="deck" id="deck">' + slidesHtml + '</main>' +
            (brand ? brand.outerHTML : '') + '</body></html>';

        return new Promise(function (resolve) {
            var settled = false;
            function done() {
                if (settled) return;
                settled = true;
                var doc = frame.contentDocument;
                // Every slide in the stage starts hidden, so no font has been
                // requested when the frame loads and fonts.ready resolves at
                // once: the first measurement would be of the fallback face,
                // wider, and a headline would wrap onto a line it does not use
                // on screen. Load, in the stage, each face the page itself has
                // loaded before anything is measured.
                var wanted = {};
                if (document.fonts && document.fonts.forEach) {
                    document.fonts.forEach(function (f) {
                        if (f.status === 'loaded') wanted[f.family.replace(/["']/g, '') + '|' + f.weight + '|' + f.style] = true;
                    });
                }
                var loads = [];
                if (doc.fonts && doc.fonts.forEach) {
                    doc.fonts.forEach(function (f) {
                        if (wanted[f.family.replace(/["']/g, '') + '|' + f.weight + '|' + f.style]) loads.push(f.load().catch(function () { }));
                    });
                }
                var fonts = Promise.all(loads).then(function () {
                    return doc.fonts && doc.fonts.ready ? doc.fonts.ready : null;
                });
                var images = Array.prototype.map.call(doc.images, function (img) {
                    if (img.complete) return Promise.resolve();
                    return new Promise(function (r) { img.addEventListener('load', r, { once: true }); img.addEventListener('error', r, { once: true }); });
                });
                Promise.all([fonts].concat(images)).then(function () { resolve(frame); }, function () { resolve(frame); });
            }
            frame.addEventListener('load', done, { once: true });
            // A stylesheet that never answers must not hang the export.
            setTimeout(done, 10000);
            var doc = frame.contentDocument;
            doc.open();
            doc.write(markup);
            doc.close();
        });
    }

    // ---------------------------------------------------------------- pseudo-elements

    // ::before and ::after carry real content in a deck: step and agenda
    // numbers (CSS counters), the quote marks, a timeline's rule, the square a
    // mark steps out of. The walker only sees elements, so before a slide is
    // walked each generated box becomes a real <span> with the pseudo-element's
    // computed style, and the pseudo-element itself is switched off.
    var PSEUDO_HOST = 'data-neko-pseudo';

    function formatCounter(value, style) {
        style = (style || 'decimal').trim();
        if (style === 'decimal-leading-zero') return (value < 10 && value >= 0 ? '0' : '') + value;
        if (style === 'lower-alpha' || style === 'lower-latin') return String.fromCharCode(96 + ((value - 1) % 26) + 1);
        if (style === 'upper-alpha' || style === 'upper-latin') return String.fromCharCode(64 + ((value - 1) % 26) + 1);
        if (style === 'lower-roman' || style === 'upper-roman') {
            var map = [[1000, 'm'], [900, 'cm'], [500, 'd'], [400, 'cd'], [100, 'c'], [90, 'xc'], [50, 'l'], [40, 'xl'], [10, 'x'], [9, 'ix'], [5, 'v'], [4, 'iv'], [1, 'i']];
            var out = '', n = value;
            map.forEach(function (m) { while (n >= m[0]) { out += m[1]; n -= m[0]; } });
            return style === 'upper-roman' ? out.toUpperCase() : out;
        }
        if (style === 'none') return '';
        return String(value);
    }

    // `counter-reset: step 0 other 2` → [['step', 0], ['other', 2]].
    function counterPairs(value, fallback) {
        var pairs = [];
        if (!value || value === 'none') return pairs;
        var parts = value.trim().split(/\s+/);
        for (var i = 0; i < parts.length; i++) {
            var n = parseInt(parts[i + 1], 10);
            if (!isNaN(n)) { pairs.push([parts[i], n]); i++; } else pairs.push([parts[i], fallback]);
        }
        return pairs;
    }

    // The text a `content` value generates. Strings arrive quoted and
    // CSS-escaped; counters are read from `counters`, kept by the caller.
    function generatedText(content, el, counters) {
        if (!content || content === 'none' || content === 'normal') return null;
        var out = '', re = /"((?:[^"\\]|\\.)*)"|'((?:[^'\\]|\\.)*)'|counters?\(([^)]*)\)|attr\(([^)]*)\)|(open-quote|close-quote|no-open-quote|no-close-quote)/g, m;
        while ((m = re.exec(content))) {
            if (m[1] !== undefined || m[2] !== undefined) {
                out += (m[1] !== undefined ? m[1] : m[2]).replace(/\\([0-9a-fA-F]{1,6}) ?/g, function (s, hex) {
                    return String.fromCodePoint(parseInt(hex, 16));
                }).replace(/\\(.)/g, '$1');
            } else if (m[3] !== undefined) {
                var args = m[3].split(',').map(function (s) { return s.trim(); });
                var name = args[0];
                var style = /^counters\(/.test(m[0]) ? args[2] : args[1];
                out += formatCounter(counters[name] || 0, style && style.replace(/^["']|["']$/g, ''));
            } else if (m[4] !== undefined) {
                out += el.getAttribute(m[4].trim()) || '';
            } else if (m[5] === 'open-quote') {
                out += '“';
            } else if (m[5] === 'close-quote') {
                out += '”';
            }
        }
        return out;
    }

    function materializePseudo(win, host, which, counters) {
        var pcs = win.getComputedStyle(host, which);
        var text = generatedText(pcs.content, host, counters);
        if (text === null) return;
        if (pcs.display === 'none') return;

        var span = host.ownerDocument.createElement('span');
        span.setAttribute(PSEUDO_HOST, which);
        for (var i = 0; i < pcs.length; i++) {
            var prop = pcs[i];
            if (prop === 'content' || prop.indexOf('counter-') === 0) continue;
            span.style.setProperty(prop, pcs.getPropertyValue(prop));
        }
        span.textContent = text;
        if (which === '::before') host.insertBefore(span, host.firstChild);
        else host.appendChild(span);
        host.setAttribute(PSEUDO_HOST + (which === '::before' ? '-before' : '-after'), '');
    }

    // Walks a slide in document order, keeping CSS counters the way the
    // browser does for these decks (reset, then increment, then ::before,
    // children, ::after), and materializes every generated box.
    function materializeSlide(win, slide) {
        if (slide.hasAttribute(PSEUDO_HOST + '-done')) return;
        slide.setAttribute(PSEUDO_HOST + '-done', '');
        var counters = {};
        var hosts = [];
        (function visit(el) {
            var cs = win.getComputedStyle(el);
            if (cs.display === 'none') return;
            counterPairs(cs.counterReset, 0).forEach(function (p) { counters[p[0]] = p[1]; });
            counterPairs(cs.counterSet, 0).forEach(function (p) { counters[p[0]] = p[1]; });
            counterPairs(cs.counterIncrement, 1).forEach(function (p) { counters[p[0]] = (counters[p[0]] || 0) + p[1]; });
            // Snapshot the counters for this element's ::before now; ::after
            // sees the values after its children.
            hosts.push([el, '::before', Object.assign({}, counters)]);
            for (var c = el.firstElementChild; c; c = c.nextElementSibling) {
                if (!SKIP_TAGS[c.tagName] && c.namespaceURI === 'http://www.w3.org/1999/xhtml') visit(c);
            }
            hosts.push([el, '::after', Object.assign({}, counters)]);
        })(slide);
        // Read every pseudo-element before changing the DOM, so an inserted
        // span never shifts a selector (:first-child, +) that styles another.
        var plan = hosts.map(function (h) {
            var pcs = win.getComputedStyle(h[0], h[1]);
            return { host: h[0], which: h[1], counters: h[2], content: pcs.content };
        }).filter(function (p) { return p.content && p.content !== 'none' && p.content !== 'normal'; });
        plan.forEach(function (p) { materializePseudo(win, p.host, p.which, p.counters); });
    }

    function escapeAttr(value) {
        return String(value || '').replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');
    }

    // ---------------------------------------------------------------- exporter

    function SlideWriter(pptx, slide, win, map, slideIds) {
        this.pptx = pptx;
        this.slide = slide;
        this.win = win;
        this.map = map;           // { s, ox, oy }: CSS px in the frame → CSS px on the slide
        this.slideIds = slideIds; // element id → slide number, for in-deck links
        this.pending = [];        // async work (images) that must finish before writing
    }

    SlideWriter.prototype.x = function (px) { return (this.map.ox + px * this.map.s) / PX_PER_IN; };
    SlideWriter.prototype.y = function (px) { return (this.map.oy + px * this.map.s) / PX_PER_IN; };
    SlideWriter.prototype.len = function (px) { return (px * this.map.s) / PX_PER_IN; };
    SlideWriter.prototype.pt = function (px) { return Math.round(px * this.map.s * PT_PER_PX * 2) / 2; };

    SlideWriter.prototype.walk = function (el) {
        if (SKIP_TAGS[el.tagName]) return;
        var cs = this.win.getComputedStyle(el);
        if (isVisuallyHidden(el, cs)) return;

        this.decorate(el, cs);

        var tag = el.tagName.toLowerCase();
        if (tag === 'svg') { this.addSvg(el); return; }
        if (tag === 'img') { this.addImg(el); return; }
        if (tag === 'canvas') { this.addCanvas(el); return; }

        var group = [];
        var child = el.firstChild;
        while (child) {
            if (child.nodeType === 3) {
                group.push(child);
            } else if (child.nodeType === 1 && !SKIP_TAGS[child.tagName]) {
                var ccs = this.win.getComputedStyle(child);
                if (isInlineLevel(ccs) && !isMedia(child) && !isChip(child, ccs)) {
                    group.push(child);
                } else {
                    this.flush(group, el, cs);
                    group = [];
                    this.walk(child);
                }
            }
            child = child.nextSibling;
        }
        this.flush(group, el, cs);
    };

    // An inline-block with a surface of its own (a tag, a pill, a key cap)
    // is a shape with its text inside it, not words in the surrounding line.
    function isChip(el, cs) {
        if (cs.display !== 'inline-block' && cs.display !== 'inline-flex') return false;
        var bg = parseColor(cs.backgroundColor);
        var border = (parseFloat(cs.borderTopWidth) || 0) > 0 && cs.borderTopStyle !== 'none';
        return !!bg || border;
    }

    function isMedia(el) {
        var tag = el.tagName.toLowerCase();
        return tag === 'svg' || tag === 'img' || tag === 'canvas';
    }

    // Backgrounds and borders become rectangles drawn beneath the content they
    // frame. A uniform border is the shape's outline; anything else (a single
    // accent rule, a row divider) is drawn as thin filled bars per side.
    SlideWriter.prototype.decorate = function (el, cs) {
        var bg = parseColor(cs.backgroundColor);
        var sides = ['Top', 'Right', 'Bottom', 'Left'].map(function (side) {
            var width = parseFloat(cs['border' + side + 'Width']) || 0;
            var style = cs['border' + side + 'Style'];
            var color = parseColor(cs['border' + side + 'Color']);
            return (width > 0 && style !== 'none' && style !== 'hidden' && color) ? { width: width, color: color, style: style } : null;
        });
        var hasBorder = sides.some(Boolean);

        // A shape cut out by a CSS mask (the Escape mark is one) is its mask
        // image filled with the background colour, not a filled rectangle.
        var mask = cs.webkitMaskImage || cs.maskImage;
        if (mask && mask !== 'none') {
            if (bg) this.maskedShape(el, cs, mask, bg);
            bg = null;
            if (!hasBorder) return;
        }

        // Solid colour layers (`linear-gradient(c, c)` with a size and a
        // position) are how a stylesheet draws small marks without markup.
        this.gradientLayers(el, cs);

        if (!bg && !hasBorder) return;

        var rects = isInlineLevel(cs) && cs.display === 'inline' ? el.getClientRects() : [el.getBoundingClientRect()];
        var radius = parseFloat(cs.borderTopLeftRadius) || 0;
        var uniform = hasBorder && sides.every(function (s) {
            return s && s.width === sides[0].width && s.color.hex === sides[0].color.hex && s.style === sides[0].style;
        });

        for (var i = 0; i < rects.length; i++) {
            var r = rects[i];
            if (r.width < 0.5 || r.height < 0.5) continue;

            if (bg || uniform) {
                var opts = {
                    x: this.x(r.left), y: this.y(r.top), w: this.len(r.width), h: this.len(r.height),
                    fill: bg ? { color: bg.hex, transparency: transparency(bg) } : { type: 'none' },
                    line: uniform
                        ? { color: sides[0].color.hex, width: Math.max(0.25, this.pt(sides[0].width)), transparency: transparency(sides[0].color), dashType: sides[0].style === 'dashed' ? 'dash' : (sides[0].style === 'dotted' ? 'sysDot' : 'solid') }
                        : { type: 'none' }
                };
                var shape = this.pptx.ShapeType.rect;
                if (radius > 0.5) {
                    shape = this.pptx.ShapeType.roundRect;
                    opts.rectRadius = this.len(Math.min(radius, r.width / 2, r.height / 2));
                }
                this.slide.addShape(shape, opts);
            }

            if (!uniform) {
                if (sides[0]) this.bar(r.left, r.top, r.width, sides[0].width, sides[0].color);
                if (sides[2]) this.bar(r.left, r.bottom - sides[2].width, r.width, sides[2].width, sides[2].color);
                if (sides[3]) this.bar(r.left, r.top, sides[3].width, r.height, sides[3].color);
                if (sides[1]) this.bar(r.right - sides[1].width, r.top, sides[1].width, r.height, sides[1].color);
            }
        }
    };

    SlideWriter.prototype.maskedShape = function (el, cs, mask, bg) {
        var root = svgFromUrl(mask);
        if (!root) return;
        var r = el.getBoundingClientRect();
        if (r.width < 0.5 || r.height < 0.5) return;
        // Everything the mask paints opaque takes the background colour.
        this.svgGraphic(root, r.left, r.top, r.width, r.height, 'contain', '50% 50%', bg);
    };

    // The SVG document inside a CSS `url(data:image/svg+xml…)`, or null.
    function svgFromUrl(css) {
        var m = /url\("(data:image\/svg\+xml[^"]*)"\)|url\('(data:image\/svg\+xml[^']*)'\)|url\((data:image\/svg\+xml[^)]*)\)/.exec(css || '');
        if (!m) return null;
        var data = m[1] || m[2] || m[3], text;
        try {
            var comma = data.indexOf(',');
            text = /;base64,/.test(data.substring(0, comma + 1)) ? atob(data.substring(comma + 1)) : decodeURIComponent(data.substring(comma + 1));
        } catch (e) { return null; }
        var root = new DOMParser().parseFromString(text, 'image/svg+xml').documentElement;
        return root && root.nodeName.toLowerCase() === 'svg' ? root : null;
    }

    // Draws an SVG (from a mask or a background) into a box the way CSS
    // sizes it (`contain`, or a length pair) and positions it. An SVG made of
    // plain rectangles (the Escape mark) becomes native rectangles, crisp at
    // any size and editable; anything else goes in as a picture. `color`
    // recolours every painted shape (a mask takes the element's background).
    SlideWriter.prototype.svgGraphic = function (root, bx, by, bw, bh, size, position, color) {
        var vb = (root.getAttribute('viewBox') || '').split(/[\s,]+/).map(Number);
        if (vb.length !== 4 || !(vb[2] > 0) || !(vb[3] > 0)) {
            var ow = parseFloat(root.getAttribute('width')) || bw, oh = parseFloat(root.getAttribute('height')) || bh;
            vb = [0, 0, ow, oh];
        }
        var aspect = vb[2] / vb[3];
        var w, h;
        var parts = (size || 'contain').split(/\s+/);
        if (parts[0] === 'contain' || parts[0] === 'cover') {
            w = bw; h = bh;
            if ((w / h > aspect) === (parts[0] === 'contain')) w = h * aspect; else h = w / aspect;
        } else {
            w = parts[0] === 'auto' ? null : cssLength(parts[0], bw);
            h = !parts[1] || parts[1] === 'auto' ? null : cssLength(parts[1], bh);
            if (w === null && h === null) { w = vb[2]; h = vb[3]; }
            else if (w === null) w = h * aspect;
            else if (h === null) h = w / aspect;
        }
        var pos = (position || '50% 50%').split(/\s+/);
        var left = bx + (/%$/.test(pos[0]) ? parseFloat(pos[0]) / 100 * (bw - w) : cssLength(pos[0], bw));
        var top = by + (/%$/.test(pos[1] || '50%') ? parseFloat(pos[1] || '50') / 100 * (bh - h) : cssLength(pos[1], bh));
        var sx = w / vb[2], sy = h / vb[3];

        var shapes = Array.prototype.filter.call(root.querySelectorAll('*'), function (n) {
            return ['rect', 'circle', 'ellipse', 'path', 'polygon', 'polyline', 'line', 'text', 'image', 'use'].indexOf(n.localName) >= 0;
        });
        var rectsOnly = shapes.length > 0 && shapes.every(function (n) { return n.localName === 'rect' && !n.getAttribute('transform') && !n.getAttribute('rx'); }) &&
            !root.querySelector('[transform]');
        if (rectsOnly) {
            for (var i = 0; i < shapes.length; i++) {
                var n = shapes[i];
                var fill = color || parseColor(n.getAttribute('fill') || (n.closest('[fill]') && n.closest('[fill]').getAttribute('fill')) || '#000');
                if (!fill && /^#/.test(n.getAttribute('fill') || '')) fill = { hex: n.getAttribute('fill').substring(1).toUpperCase(), alpha: 1 };
                if (!fill) continue;
                this.bar(left + (parseFloat(n.getAttribute('x')) - vb[0]) * sx, top + (parseFloat(n.getAttribute('y')) - vb[1]) * sy,
                    parseFloat(n.getAttribute('width')) * sx, parseFloat(n.getAttribute('height')) * sy, fill);
            }
            return;
        }
        if (color) {
            root.setAttribute('fill', '#' + color.hex);
            Array.prototype.forEach.call(root.querySelectorAll('[fill]'), function (n) {
                if (n.getAttribute('fill') !== 'none') n.setAttribute('fill', '#' + color.hex);
            });
        }
        root.setAttribute('width', String(Math.round(w * 4)));
        root.setAttribute('height', String(Math.round(h * 4)));
        var dataUrl = 'data:image/svg+xml;base64,' + utf8Base64(new XMLSerializer().serializeToString(root));
        var slide = this.slide, self = this;
        this.pending.push(rasterise(dataUrl, w, h).then(function () {
            slide.addImage({ data: dataUrl, x: self.x(left), y: self.y(top), w: self.len(w), h: self.len(h) });
        }).catch(function (e) { console.warn('[neko] PowerPoint export skipped an SVG graphic:', e); }));
    };

    // Splits a comma-separated CSS list without splitting inside parentheses.
    function cssList(value) {
        var out = [], depth = 0, start = 0;
        for (var i = 0; i < value.length; i++) {
            var ch = value.charAt(i);
            if (ch === '(') depth++;
            else if (ch === ')') depth--;
            else if (ch === ',' && depth === 0) { out.push(value.substring(start, i).trim()); start = i + 1; }
        }
        out.push(value.substring(start).trim());
        return out;
    }

    function cssLength(token, basis) {
        if (/%$/.test(token)) return parseFloat(token) / 100 * basis;
        return parseFloat(token) || 0;
    }

    SlideWriter.prototype.gradientLayers = function (el, cs) {
        var image = cs.backgroundImage;
        if (!image || (image.indexOf('gradient(') < 0 && image.indexOf('data:image/svg+xml') < 0)) return;
        var layers = cssList(image);
        var sizes = cssList(cs.backgroundSize || 'auto');
        var positions = cssList(cs.backgroundPosition || '0% 0%');
        var r = el.getBoundingClientRect();
        // Painted bottom layer first, so the first layer ends up on top.
        for (var i = layers.length - 1; i >= 0; i--) {
            if (/^url\(/.test(layers[i])) {
                var svgRoot = svgFromUrl(layers[i]);
                if (svgRoot) this.svgGraphic(svgRoot, r.left, r.top, r.width, r.height, sizes[i % sizes.length], positions[i % positions.length], null);
                continue;
            }
            var gm = /^linear-gradient\((.*)\)$/.exec(layers[i]);
            if (!gm) continue;
            var stops = cssList(gm[1]).filter(function (t) { return !/^(to |[-\d.]+deg)/.test(t); });
            var colors = stops.map(function (t) { return parseColor(t.replace(/\s+[-\d.]+(%|px)?(\s+[-\d.]+(%|px)?)?$/, '')); });
            if (!colors.length || !colors[0] || !colors.every(function (c) { return c && c.hex === colors[0].hex && c.alpha === colors[0].alpha; })) continue;
            var size = (sizes[i % sizes.length] || 'auto').split(/\s+/);
            var w = size[0] === 'auto' || size[0] === 'cover' || size[0] === 'contain' ? r.width : cssLength(size[0], r.width);
            var h = !size[1] || size[1] === 'auto' ? (size[0] === 'auto' ? r.height : w) : cssLength(size[1], r.height);
            var pos = (positions[i % positions.length] || '0% 0%').split(/\s+/);
            var px = /%$/.test(pos[0]) ? parseFloat(pos[0]) / 100 * (r.width - w) : cssLength(pos[0], r.width);
            var py = /%$/.test(pos[1] || '0%') ? parseFloat(pos[1] || '0') / 100 * (r.height - h) : cssLength(pos[1], r.height);
            if (w < 0.5 || h < 0.5) continue;
            this.bar(r.left + px, r.top + py, w, h, colors[0]);
        }
    };

    SlideWriter.prototype.bar = function (left, top, width, height, color) {
        this.slide.addShape(this.pptx.ShapeType.rect, {
            x: this.x(left), y: this.y(top), w: this.len(width), h: this.len(height),
            fill: { color: color.hex, transparency: transparency(color) },
            line: { type: 'none' }
        });
    };

    // A run of inline content between two blocks becomes one text box sized to
    // the lines it occupies on screen.
    SlideWriter.prototype.flush = function (group, container, containerCs) {
        if (!group.length) return;

        // Inline elements can carry their own decoration (a tag chip, the
        // eyebrow's rule) and can contain media; handle both before the text.
        for (var g = 0; g < group.length; g++) {
            if (group[g].nodeType === 1) this.inlineExtras(group[g]);
        }

        // Balanced and pretty wrapping choose their breaks by a rule PowerPoint
        // does not have, so for those the browser's own line breaks are
        // written into the text as line breaks.
        this.lineStarts = /balance|pretty/.test((containerCs.textWrap || '') + ' ' + (containerCs.textWrapStyle || ''))
            ? browserLineStarts(group, this.win) : null;
        var runs = [];
        this.collectRuns(group, runs, containerCs.whiteSpace);
        this.lineStarts = null;
        tidyRuns(runs);
        if (!runs.some(function (r) { return r.text.trim().length > 0; })) return;

        var range = container.ownerDocument.createRange();
        range.setStartBefore(group[0]);
        range.setEndAfter(group[group.length - 1]);
        var box = unionRects(range.getClientRects());
        if (!box) return;

        var ws = containerCs.whiteSpace;
        var wraps = !(ws === 'pre' || ws === 'nowrap');
        var align = containerCs.textAlign;
        if (align === 'start' || align === '-webkit-auto' || align === 'auto') align = containerCs.direction === 'rtl' ? 'right' : 'left';
        if (align === 'end') align = containerCs.direction === 'rtl' ? 'left' : 'right';
        if (align === '-webkit-center') align = 'center';
        if (align === 'justify') align = 'left';

        // Horizontally the box spans the container's content box, not just the
        // lines as the browser broke them: that is the measure the text was
        // set to, and it leaves PowerPoint the same room to wrap in.
        // In a flex or grid container the text is one item among siblings (the
        // eyebrow's rule, say), so there it keeps its own measured extent.
        var left = box.left;
        var right = box.right;
        // Balanced and pretty wrapping choose their breaks by a rule
        // PowerPoint does not have; giving the box the width of the lines as
        // set makes it break in the same places.
        var balanced = /balance|pretty/.test((containerCs.textWrap || '') + ' ' + (containerCs.textWrapStyle || ''));
        if (!/flex|grid/.test(containerCs.display) && !balanced) {
            var outer = container.getBoundingClientRect();
            left = Math.min(left, outer.left + (parseFloat(containerCs.borderLeftWidth) || 0) + (parseFloat(containerCs.paddingLeft) || 0));
            right = Math.max(right, outer.right - (parseFloat(containerCs.borderRightWidth) || 0) - (parseFloat(containerCs.paddingRight) || 0));
        }
        var width = right - left;
        var bullet = this.bulletFor(container, group);
        if (bullet) {
            left -= bullet.indentPx;
            width += bullet.indentPx;
        }

        // PowerPoint's line breaking differs slightly from the browser's, and a
        // substituted font runs wider: give each box a little room so a line
        // that just fits on screen doesn't wrap in the export. Centred and
        // right-aligned text grows symmetrically / leftwards to stay in place.
        // Text set in a font the export embeds measures nearly the same in
        // PowerPoint as on screen, so it gets room for rounding and tracking
        // drift (0.3em) and no more: a word as short as "a " is wider than
        // that, so none climbs onto the line above.
        var embedded = runs.every(function (r) { return !r.text.trim() || r.embedded; });
        var slack = embedded ? 0.3 * (parseFloat(containerCs.fontSize) || 16) : Math.min(16, Math.max(4, width * 0.02));
        if (align === 'center') left -= slack / 2;
        else if (align === 'right') left -= slack;
        width += slack;

        var lineHeight = parseFloat(containerCs.lineHeight);
        var fontSize = parseFloat(containerCs.fontSize) || 16;
        if (isNaN(lineHeight)) lineHeight = fontSize * 1.2;

        // Text that sat on one line on screen stays on one line: a substitute
        // font a hair wider must not push its last word onto a line of its own.
        if (box.bottom - box.top < lineHeight * 1.5 && !runs.some(function (r) { return r.options.breakLine; })) wraps = false;

        var items = runs.map(function (run) { return { text: run.text, options: run.options }; });
        if (bullet) items[0].options = Object.assign({}, items[0].options, { bullet: bullet.options });

        // Place the box by its first baseline. With exact line spacing a
        // PowerPoint renderer puts the whole of a line's extra height above
        // its first line: the baseline lands at (spacing - BASELINE_DESCENT x size)
        // below the box's top, measured in LibreOffice and matching
        // PowerPoint's 80/20 split at solid leading. CSS instead centres the
        // glyphs in the line box. So the box starts where that rule puts the
        // browser's own first baseline.
        var top = box.top;
        var baseline = firstBaseline(group, this.win);
        if (baseline !== null) top = baseline - (lineHeight - BASELINE_DESCENT * fontSize);
        var bottom = Math.max(box.bottom, top + lineHeight);

        this.slide.addText(items, {
            x: this.x(left), y: this.y(top),
            w: this.len(width), h: this.len(bottom - top),
            margin: 0,
            valign: 'top',
            align: align,
            wrap: wraps,
            fit: 'none',
            lineSpacing: this.pt(lineHeight),
            paraSpaceBefore: 0,
            paraSpaceAfter: 0
        });
    };

    // Where the browser started each new line inside a run of inline content:
    // a Map of text node -> offsets of the words that begin a line.
    function browserLineStarts(group, win) {
        var starts = new Map(), lastTop = null, doc = null;
        (function visit(list) {
            for (var i = 0; i < list.length; i++) {
                var n = list[i];
                if (n.nodeType === 1) {
                    if (!SKIP_TAGS[n.tagName] && !isMedia(n)) visit(n.childNodes);
                    continue;
                }
                if (n.nodeType !== 3) continue;
                doc = doc || n.ownerDocument;
                var text = n.textContent, re = /\S+/g, m;
                while ((m = re.exec(text))) {
                    var range = doc.createRange();
                    range.setStart(n, m.index);
                    range.setEnd(n, m.index + 1);
                    var rect = range.getClientRects()[0];
                    if (!rect) continue;
                    if (lastTop !== null && rect.top > lastTop + rect.height * 0.5) {
                        if (!starts.has(n)) starts.set(n, []);
                        starts.get(n).push(m.index);
                    }
                    lastTop = rect.top;
                }
            }
        })(group);
        return starts;
    }

    // The browser's first baseline in a run of inline content: the top of the
    // first line's glyph box plus the ascent the browser used for that font.
    // With exact line spacing, a PowerPoint renderer sets the first baseline
    // (spacing - BASELINE_DESCENT x font size) below the top of the text box.
    // Measured in LibreOffice across Schibsted Grotesk and Geist Mono at 30 to
    // 160px: 0.20 to 0.225, independent of the spacing.
    var BASELINE_DESCENT = 0.215;

    var metricsCanvas = null;
    function firstBaseline(group, win) {
        var node = null;
        (function find(list) {
            for (var i = 0; i < list.length && !node; i++) {
                var n = list[i];
                if (n.nodeType === 3 && n.textContent.trim()) node = n;
                else if (n.nodeType === 1 && !SKIP_TAGS[n.tagName] && !isMedia(n)) find(n.childNodes);
            }
        })(group);
        if (!node) return null;
        var range = node.ownerDocument.createRange();
        var text = node.textContent;
        var start = text.search(/\S/);
        range.setStart(node, start);
        range.setEnd(node, start + 1);
        var rect = range.getClientRects()[0];
        if (!rect) return null;
        var cs = win.getComputedStyle(node.parentElement);
        metricsCanvas = metricsCanvas || node.ownerDocument.createElement('canvas');
        var ctx = metricsCanvas.getContext('2d');
        ctx.font = cs.fontStyle + ' ' + cs.fontWeight + ' ' + cs.fontSize + ' ' + cs.fontFamily;
        var m = ctx.measureText(text.charAt(start));
        if (!m || typeof m.fontBoundingBoxAscent !== 'number') return null;
        return rect.top + m.fontBoundingBoxAscent;
    }

    // The first line of a list item carries its marker. On screen the marker
    // hangs in the list's left padding, so the text box reaches back over it.
    SlideWriter.prototype.bulletFor = function (container, group) {
        if (container.tagName !== 'LI') return null;
        var first = container.firstChild;
        while (first && first.nodeType === 3 && !first.textContent.trim()) first = first.nextSibling;
        if (first && group.indexOf(first) < 0) return null;

        var cs = this.win.getComputedStyle(container);
        var type = cs.listStyleType;
        if (!type || type === 'none') return null;

        var list = container.parentElement;
        var indentPx = list ? parseFloat(this.win.getComputedStyle(list).paddingLeft) || 0 : 0;
        indentPx = Math.max(indentPx, 14);
        var options = { indent: this.pt(indentPx) };

        if (/decimal|roman|alpha|latin|greek/.test(type)) {
            var index = 1;
            if (list && list.tagName === 'OL') {
                index = list.start || 1;
                var sib = container.previousElementSibling;
                while (sib) { if (sib.tagName === 'LI') index++; sib = sib.previousElementSibling; }
            }
            options.type = 'number';
            options.numberStartAt = index;
            options.style = type === 'lower-alpha' || type === 'lower-latin' ? 'alphaLcPeriod'
                : type === 'upper-alpha' || type === 'upper-latin' ? 'alphaUcPeriod'
                : type === 'lower-roman' ? 'romanLcPeriod'
                : type === 'upper-roman' ? 'romanUcPeriod'
                : 'arabicPeriod';
        } else if (type === 'circle') {
            options.characterCode = '25E6';
        } else if (type === 'square') {
            options.characterCode = '25AA';
        }
        return { indentPx: indentPx, options: options };
    };

    SlideWriter.prototype.inlineExtras = function (el) {
        if (SKIP_TAGS[el.tagName]) return;
        var cs = this.win.getComputedStyle(el);
        if (isVisuallyHidden(el, cs)) return;
        this.decorate(el, cs);
        if (isMedia(el)) {
            this.walk(el);
            return;
        }
        for (var c = el.firstElementChild; c; c = c.nextElementSibling) this.inlineExtras(c);
    };

    SlideWriter.prototype.collectRuns = function (nodes, runs, whiteSpace) {
        for (var i = 0; i < nodes.length; i++) {
            var node = nodes[i];
            if (node.nodeType === 3) {
                this.textRun(node, runs);
            } else if (node.nodeType === 1) {
                if (node.tagName === 'BR') {
                    if (runs.length) runs[runs.length - 1].options.breakLine = true;
                    continue;
                }
                if (SKIP_TAGS[node.tagName] || isMedia(node)) continue;
                var cs = this.win.getComputedStyle(node);
                if (isVisuallyHidden(node, cs)) continue;
                this.collectRuns(Array.prototype.slice.call(node.childNodes), runs, cs.whiteSpace);
            }
        }
    };

    SlideWriter.prototype.textRun = function (node, runs) {
        var parent = node.parentElement;
        if (!parent) return;
        var cs = this.win.getComputedStyle(parent);
        var ws = cs.whiteSpace;
        var preserve = ws === 'pre' || ws === 'pre-wrap' || ws === 'break-spaces' || ws === 'pre-line';
        var text = node.textContent;
        if (!preserve) text = text.replace(/[\s​]+/g, ' ');
        else if (ws === 'pre-line') text = text.replace(/[ \t]+/g, ' ');
        text = transformText(text, cs.textTransform);
        if (!text) return;
        // A generated marker (a check mark, a counter) keeps the gap its
        // margin opened as a space, since a run has no margins of its own.
        if (parent.hasAttribute(PSEUDO_HOST)) {
            var em = parseFloat(cs.fontSize) || 16;
            if ((parseFloat(cs.marginRight) || 0) >= 0.15 * em) text = text + ' ';
            if ((parseFloat(cs.marginLeft) || 0) >= 0.15 * em) text = ' ' + text;
        }

        var color = parseColor(cs.color);
        var weight = parseInt(cs.fontWeight, 10) || 400;
        var decoration = cs.textDecorationLine || cs.textDecoration || '';
        var font = runFont(cs.fontFamily, weight, parent.ownerDocument);
        var options = {
            fontFace: font.name,
            fontSize: Math.max(1, this.pt(parseFloat(cs.fontSize) || 16)),
            bold: font.bold,
            italic: cs.fontStyle === 'italic' || cs.fontStyle === 'oblique',
            color: color ? color.hex : undefined
        };
        if (color && color.alpha < 1) options.transparency = transparency(color);
        if (decoration.indexOf('underline') >= 0) options.underline = { style: 'sng' };
        if (decoration.indexOf('line-through') >= 0) options.strike = 'sngStrike';
        var spacing = parseFloat(cs.letterSpacing);
        if (spacing) options.charSpacing = Math.round(spacing * this.map.s * PT_PER_PX * 10) / 10;

        var link = parent.closest('a[href]');
        if (link) {
            var hyperlink = this.hyperlink(link);
            if (hyperlink) options.hyperlink = hyperlink;
        }

        if (preserve && text.indexOf('\n') >= 0) {
            var lines = text.split('\n');
            for (var l = 0; l < lines.length; l++) {
                var opts = Object.assign({}, options);
                if (l < lines.length - 1) opts.breakLine = true;
                runs.push({ text: lines[l], options: opts, preserve: true, embedded: font.embedded });
            }
        } else if (this.lineStarts && this.lineStarts.has(node)) {
            // Split where the browser started a new line; each piece but the
            // last ends its line.
            var raw = node.textContent, cuts = this.lineStarts.get(node), from = 0;
            var pieces = [];
            cuts.forEach(function (at) { pieces.push(raw.substring(from, at)); from = at; });
            pieces.push(raw.substring(from));
            for (var q = 0; q < pieces.length; q++) {
                var piece = transformText(pieces[q].replace(/[\s\u200B]+/g, ' '), cs.textTransform);
                var pieceOptions = Object.assign({}, options);
                if (q < pieces.length - 1) {
                    piece = piece.replace(/ +$/, '');
                    pieceOptions.breakLine = true;
                }
                runs.push({ text: piece, options: pieceOptions, preserve: preserve, embedded: font.embedded });
            }
        } else {
            runs.push({ text: text, options: options, preserve: preserve, embedded: font.embedded });
        }
    };

    SlideWriter.prototype.hyperlink = function (a) {
        var raw = a.getAttribute('href') || '';
        if (raw.charAt(0) === '#') {
            var target = this.slideIds[raw.substring(1)];
            if (target) return { slide: target };
            var n = parseInt(raw.substring(1).replace(/^slide-/, ''), 10);
            return isNaN(n) ? null : { slide: n };
        }
        if (/^(javascript|data):/i.test(raw)) return null;
        try { return { url: new URL(raw, location.href).href }; } catch (e) { return null; }
    };

    // Collapse whitespace across run boundaries the way the browser does, and
    // trim the ends of each line.
    function tidyRuns(runs) {
        var atLineStart = true;
        for (var i = 0; i < runs.length; i++) {
            var run = runs[i];
            if (!run.preserve) {
                if (atLineStart) run.text = run.text.replace(/^ +/, '');
                if (run.options.breakLine || i === runs.length - 1) run.text = run.text.replace(/ +$/, '');
            }
            if (run.text.length) atLineStart = !!run.options.breakLine || (!run.preserve && / $/.test(run.text));
            else if (run.options.breakLine) atLineStart = true;
        }
        for (var j = runs.length - 1; j >= 0; j--) {
            if (!runs[j].text.length && !runs[j].options.breakLine) runs.splice(j, 1);
        }
        // A trailing hard break would open an empty last paragraph.
        if (runs.length) delete runs[runs.length - 1].options.breakLine;
    }

    // ---------------------------------------------------------------- media

    SlideWriter.prototype.placement = function (el) {
        var r = el.getBoundingClientRect();
        if (r.width < 1 || r.height < 1) return null;
        return { x: this.x(r.left), y: this.y(r.top), w: this.len(r.width), h: this.len(r.height), width: r.width, height: r.height };
    };

    // One fetch per image per export, however many slides show it (the brand
    // logo is on every one of them).
    var imageCache = {};

    SlideWriter.prototype.addImg = function (img) {
        var place = this.placement(img);
        if (!place) return;
        var src = img.currentSrc || img.src;
        if (!src) return;
        var slide = this.slide;
        var key = src + '|' + Math.round(place.width) + 'x' + Math.round(place.height);
        if (!imageCache[key]) imageCache[key] = imageData(src, img, place);
        var job = imageCache[key].then(function (data) {
            slide.addImage({ data: data, x: place.x, y: place.y, w: place.w, h: place.h, altText: img.alt || '' });
        }).catch(function (e) {
            console.warn('[neko] PowerPoint export skipped an image:', src, e);
        });
        this.pending.push(job);
    };

    // SVG goes in as SVG — PowerPoint keeps it vector and PptxGenJS adds the
    // PNG fallback older versions need. A raster image is re-encoded at twice
    // its on-slide size: a 1024px logo shown at 22px would otherwise ride along
    // at full size on every slide, and WebP/AVIF sources open everywhere.
    function imageData(src, img, place) {
        var load = /^data:/i.test(src) ? Promise.resolve(src) : fetch(src).then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.blob();
        }).then(blobToDataUrl);
        return load.then(function (data) {
            if (/^data:image\/svg\+xml/i.test(data)) return data;
            var natural = Math.max(img.naturalWidth || 0, img.naturalHeight || 0);
            var target = Math.max(place.width, place.height) * 2;
            if (natural && natural <= target && /^data:image\/(png|jpe?g|gif)/i.test(data)) return data;
            var type = /^data:image\/jpe?g/i.test(data) ? 'image/jpeg' : 'image/png';
            return rasterise(data, place.width, place.height, type);
        });
    }

    SlideWriter.prototype.addCanvas = function (canvas) {
        var place = this.placement(canvas);
        if (!place) return;
        try {
            this.slide.addImage({ data: canvas.toDataURL('image/png'), x: place.x, y: place.y, w: place.w, h: place.h });
        } catch (e) {
            console.warn('[neko] PowerPoint export skipped a canvas:', e);
        }
    };

    function rasterise(src, width, height, type) {
        return loadImage(src).then(function (img) {
            var scale = 2;
            var canvas = document.createElement('canvas');
            canvas.width = Math.max(1, Math.round(width * scale));
            canvas.height = Math.max(1, Math.round(height * scale));
            canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
            return type === 'image/jpeg' ? canvas.toDataURL(type, 0.9) : canvas.toDataURL('image/png');
        });
    }

    var SVG_STYLE_PROPS = ['fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'stroke-dasharray',
        'stroke-linecap', 'stroke-linejoin', 'opacity', 'color', 'font-family', 'font-size', 'font-weight',
        'font-style', 'text-anchor', 'dominant-baseline', 'letter-spacing', 'visibility'];

    var SVG_INHERITED = { 'fill': 1, 'fill-opacity': 1, 'stroke': 1, 'stroke-width': 1, 'stroke-opacity': 1, 'stroke-dasharray': 1,
        'stroke-linecap': 1, 'stroke-linejoin': 1, 'color': 1, 'font-family': 1, 'font-size': 1, 'font-weight': 1,
        'font-style': 1, 'text-anchor': 1, 'dominant-baseline': 1, 'letter-spacing': 1, 'visibility': 1 };
    var SVG_INITIAL = { 'opacity': '1' };

    // The computed presentation properties of one SVG element, colours split
    // into a hex value and an opacity folded into fill-/stroke-opacity.
    function svgPresentation(cs) {
        var out = {};
        for (var p = 0; p < SVG_STYLE_PROPS.length; p++) {
            var prop = SVG_STYLE_PROPS[p];
            var value = cs.getPropertyValue(prop);
            if (!value) continue;
            if (prop === 'fill' || prop === 'stroke' || prop === 'color') {
                var c = /^(rgb|color|hsl|#)/.test(value) ? parseColor(value) : null;
                if (c) {
                    value = '#' + c.hex;
                    if (prop !== 'color' && c.alpha < 1) {
                        var opacityProp = prop + '-opacity';
                        var own = parseFloat(cs.getPropertyValue(opacityProp));
                        out[opacityProp] = String(Math.round((isNaN(own) ? 1 : own) * c.alpha * 1000) / 1000);
                    }
                } else if (prop !== 'color' && /^(rgba|color)\(/.test(value)) {
                    value = 'none';   // fully transparent
                }
            }
            if ((prop === 'fill-opacity' || prop === 'stroke-opacity') && out[prop] !== undefined) continue;
            out[prop] = value;
        }
        return out;
    }

    // An inline SVG is exported as a standalone SVG image. Its look usually
    // comes partly from the page (CSS variables, currentColor, the deck's
    // stylesheet), none of which exists once it is a separate file — so the
    // computed presentation properties are baked into every element first.
    SlideWriter.prototype.addSvg = function (svg) {
        var place = this.placement(svg);
        if (!place) return;

        var clone = svg.cloneNode(true);
        var source = [svg].concat(Array.prototype.slice.call(svg.querySelectorAll('*')));
        var target = [clone].concat(Array.prototype.slice.call(clone.querySelectorAll('*')));
        // Each element gets the presentation properties it does not inherit
        // unchanged from its parent, so a drawing of thousands of shapes does
        // not repeat twenty properties on every one of them. Colours are
        // written as hex plus an explicit opacity: SVG readers outside the
        // browser (PowerPoint's, LibreOffice's) do not all read the alpha of
        // rgba() or color(), and would paint a translucent stroke opaque.
        var baked = [];
        for (var i = 0; i < source.length && i < target.length; i++) {
            var cs = this.win.getComputedStyle(source[i]);
            var parentIndex = source.indexOf(source[i].parentNode);
            var parentValues = parentIndex >= 0 ? baked[parentIndex] : null;
            var values = svgPresentation(cs);
            baked.push(values);
            var style = '';
            for (var key in values) {
                if (!values.hasOwnProperty(key)) continue;
                var inherited = SVG_INHERITED[key];
                if (parentValues && inherited && parentValues[key] === values[key]) continue;
                if (parentValues && !inherited && SVG_INITIAL[key] === values[key]) continue;
                style += key + ':' + values[key] + ';';
            }
            if (cs.display === 'none') style += 'display:none;';
            target[i].setAttribute('style', style + (target[i].getAttribute('style') || ''));
        }

        replaceForeignObjects(svg, clone, this.win);
        cropToVisible(svg, clone, place);

        // Render the fallback PNG at twice the on-slide size so it stays crisp.
        if (!clone.getAttribute('viewBox')) {
            clone.setAttribute('viewBox', '0 0 ' + place.width + ' ' + place.height);
        }
        clone.setAttribute('width', String(Math.round(place.width * 2)));
        clone.setAttribute('height', String(Math.round(place.height * 2)));
        clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        clone.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');

        var markup = new XMLSerializer().serializeToString(clone);
        var data = 'data:image/svg+xml;base64,' + utf8Base64(markup);
        var slide = this.slide;

        // PptxGenJS rasterises the SVG for its PNG fallback and fails the whole
        // file if that throws (a tainted canvas, say). Try it here first; an SVG
        // the browser cannot paint to a canvas goes in as nothing rather than
        // taking the whole deck down with it.
        var job = rasterise(data, place.width, place.height).then(function () {
            slide.addImage({ data: data, x: place.x, y: place.y, w: place.w, h: place.h, altText: svg.getAttribute('aria-label') || '' });
        }).catch(function (e) {
            console.warn('[neko] PowerPoint export skipped an SVG:', e);
        });
        this.pending.push(job);
    };

    // PowerPoint and LibreOffice stretch an SVG picture to its frame and do
    // not all honour preserveAspectRatio (`slice` above all), so the picture
    // is given exactly the part of its viewBox the browser showed, with no
    // aspect handling left to interpret.
    function cropToVisible(svg, clone, place) {
        var vb = svg.viewBox && svg.viewBox.baseVal;
        if (!vb || !vb.width || !vb.height) return;
        var par = (svg.getAttribute('preserveAspectRatio') || 'xMidYMid meet').trim().split(/\s+/);
        var align = par[0], slice = par[1] === 'slice';
        var boxAspect = place.width / place.height, vbAspect = vb.width / vb.height;
        if (align === 'none' || Math.abs(boxAspect - vbAspect) < 0.001) return;
        var x = vb.x, y = vb.y, w = vb.width, h = vb.height;
        function frac(axis) {
            var a = align.substring(axis === 'x' ? 0 : 4, axis === 'x' ? 4 : 8).toLowerCase();
            return /min$/.test(a) ? 0 : /max$/.test(a) ? 1 : 0.5;
        }
        // slice: the viewBox is cropped to the frame; meet: it is padded out.
        if ((boxAspect > vbAspect) === slice) {
            var nh = w / boxAspect;
            y += (h - nh) * frac('y');
            h = nh;
        } else {
            var nw = h * boxAspect;
            x += (w - nw) * frac('x');
            w = nw;
        }
        clone.setAttribute('viewBox', [x, y, w, h].join(' '));
        clone.setAttribute('preserveAspectRatio', 'none');
    }

    // <foreignObject> (Mermaid's HTML labels, for one) taints any canvas it is
    // drawn to, which would sink the PNG fallback. Swap each one for plain SVG
    // text centred in the same box.
    function replaceForeignObjects(source, clone, win) {
        var originals = source.querySelectorAll('foreignObject');
        var copies = clone.querySelectorAll('foreignObject');
        for (var i = 0; i < copies.length; i++) {
            var fo = copies[i];
            var orig = originals[i];
            var text = (fo.textContent || '').replace(/\s+/g, ' ').trim();
            var x = parseFloat(fo.getAttribute('x')) || 0;
            var y = parseFloat(fo.getAttribute('y')) || 0;
            var w = parseFloat(fo.getAttribute('width')) || 0;
            var h = parseFloat(fo.getAttribute('height')) || 0;
            if (!text) { fo.parentNode.removeChild(fo); continue; }

            var inner = orig && (orig.querySelector('span, div, p') || orig);
            var cs = inner ? win.getComputedStyle(inner) : null;
            var node = clone.ownerDocument.createElementNS('http://www.w3.org/2000/svg', 'text');
            node.setAttribute('x', String(x + w / 2));
            node.setAttribute('y', String(y + h / 2));
            node.setAttribute('text-anchor', 'middle');
            node.setAttribute('dominant-baseline', 'central');
            if (cs) {
                node.setAttribute('style', 'fill:' + cs.color + ';font-family:' + cs.fontFamily + ';font-size:' + cs.fontSize +
                    ';font-weight:' + cs.fontWeight + ';font-style:' + cs.fontStyle);
            }
            node.textContent = text;
            fo.parentNode.replaceChild(node, fo);
        }
    }

    // ---------------------------------------------------------------- fonts

    // Embedding the typefaces a deck uses, so the .pptx looks the same on a
    // machine that has never installed them. PowerPoint loads an embedded font
    // from ppt/fonts/*.fntdata only as Embedded OpenType (a TrueType file there
    // is silently ignored), so each TrueType file is wrapped in an EOT header.
    // Ported from Curiosity Workspace's PowerPoint artifacts (pptx-fonts.js).

    function loadFontCatalog(base) {
        if (window.nekoDeckFonts && window.nekoDeckFonts.catalog) {
            fontCatalog = window.nekoDeckFonts.catalog;
            return Promise.resolve();
        }
        if (!base) return Promise.resolve();
        return fetch(base + 'deck-fonts.json').then(function (res) {
            return res.ok ? res.json() : {};
        }).then(function (catalog) { fontCatalog = catalog || {}; }, function () { fontCatalog = {}; });
    }

    function fontBytes(base, file) {
        var inline = window.nekoDeckFonts && window.nekoDeckFonts.files && window.nekoDeckFonts.files[file];
        if (inline) {
            var s = atob(inline), out = new Uint8Array(s.length);
            for (var i = 0; i < s.length; i++) out[i] = s.charCodeAt(i);
            return Promise.resolve(out);
        }
        return fetch(base + file).then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.arrayBuffer();
        }).then(function (buffer) { return new Uint8Array(buffer); });
    }

    // What an Embedded OpenType header needs from the TrueType file: OS/2, head and name.
    function readFontHeader(bytes) {
        var dv = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength), tables = {};
        for (var i = 0, n = dv.getUint16(4); i < n; i++) {
            var o = 12 + i * 16;
            tables[String.fromCharCode(bytes[o], bytes[o + 1], bytes[o + 2], bytes[o + 3])] = dv.getUint32(o + 8);
        }
        var os2 = tables['OS/2'], head = tables.head, name = tables.name, names = {};
        var strings = name + dv.getUint16(name + 4);
        for (var j = 0, m = dv.getUint16(name + 2); j < m; j++) {
            var r = name + 6 + j * 12, id = dv.getUint16(r + 6), len = dv.getUint16(r + 8), off = dv.getUint16(r + 10);
            if (dv.getUint16(r) !== 3 || dv.getUint16(r + 2) !== 1 || names[id] !== undefined) continue;   // Windows, UTF-16BE
            var str = '';
            for (var k = 0; k < len; k += 2) str += String.fromCharCode(dv.getUint16(strings + off + k));
            names[id] = str;
        }
        return {
            panose: bytes.slice(os2 + 32, os2 + 42),
            weight: dv.getUint16(os2 + 4),
            fsType: dv.getUint16(os2 + 8),
            unicodeRange: [0, 1, 2, 3].map(function (x) { return dv.getUint32(os2 + 42 + x * 4); }),
            italic: (dv.getUint16(os2 + 62) & 1) === 1,
            codePageRange: dv.getUint16(os2) >= 1 ? [dv.getUint32(os2 + 78), dv.getUint32(os2 + 82)] : [0, 0],
            checkSumAdjustment: dv.getUint32(head + 8),
            strings: [names[1] || '', names[2] || '', names[5] || '', names[4] || '']
        };
    }

    // Embedded OpenType 0x00020001: no compression, no XOR, an empty root string.
    function toEOT(ttf) {
        var f = readFontHeader(ttf);
        var strs = f.strings.map(function (s) {
            var b = new Uint8Array(s.length * 2);
            for (var i = 0; i < s.length; i++) { b[i * 2] = s.charCodeAt(i) & 255; b[i * 2 + 1] = s.charCodeAt(i) >> 8; }
            return b;
        });
        var size = 80 + strs.reduce(function (t, s) { return t + 4 + s.length; }, 0) + 4;
        var out = new Uint8Array(size + ttf.length), dv = new DataView(out.buffer), p = 0;
        function u32(v) { dv.setUint32(p, v >>> 0, true); p += 4; }
        function u16(v) { dv.setUint16(p, v, true); p += 2; }
        function u8(v) { dv.setUint8(p, v); p += 1; }
        u32(out.length); u32(ttf.length); u32(0x00020001); u32(0);
        f.panose.forEach(u8); u8(1); u8(f.italic ? 1 : 0); u32(f.weight); u16(f.fsType); u16(0x504C);
        f.unicodeRange.forEach(u32); f.codePageRange.forEach(u32); u32(f.checkSumAdjustment);
        u32(0); u32(0); u32(0); u32(0);
        strs.forEach(function (s) { u16(0); u16(s.length); out.set(s, p); p += s.length; });
        u16(0); u16(0);
        out.set(ttf, p);
        return out;
    }

    function escapeXml(s) {
        return String(s).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;');
    }

    // The deck as PptxGenJS wrote it, with every catalog font its runs name
    // added as an embedded font. Resolves to a Blob.
    function embedFonts(buffer, base) {
        var typefaces = Object.keys(usedFonts);
        if (!typefaces.length || typeof window.JSZip !== 'function') return Promise.resolve(new Blob([buffer]));
        return window.JSZip.loadAsync(buffer).then(function (zip) {
            var jobs = [];
            typefaces.forEach(function (typeface) {
                Object.keys(usedFonts[typeface]).forEach(function (slot) {
                    var file = usedFonts[typeface][slot];
                    jobs.push(fontBytes(base, file).then(function (bytes) {
                        return { typeface: typeface, slot: slot, eot: toEOT(bytes) };
                    }, function (e) {
                        console.warn('[neko] PowerPoint export could not embed', file, e);
                        return null;
                    }));
                });
            });
            return Promise.all(jobs.concat([
                zip.file('ppt/presentation.xml').async('string'),
                zip.file('ppt/_rels/presentation.xml.rels').async('string'),
                zip.file('[Content_Types].xml').async('string')
            ])).then(function (results) {
                var types = results.pop(), rels = results.pop(), presentation = results.pop();
                var fonts = results.filter(Boolean);
                if (!fonts.length) return zip.generateAsync({ type: 'blob', compression: 'DEFLATE' });
                var byTypeface = {}, added = '', n = 0;
                fonts.forEach(function (f) {
                    n++;
                    zip.file('ppt/fonts/neko-font' + n + '.fntdata', f.eot);
                    added += '<Relationship Id="rIdNekoFont' + n + '" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/font" Target="fonts/neko-font' + n + '.fntdata"/>';
                    (byTypeface[f.typeface] = byTypeface[f.typeface] || {})[f.slot] = 'rIdNekoFont' + n;
                });
                var list = '';
                Object.keys(byTypeface).forEach(function (typeface) {
                    list += '<p:embeddedFont><p:font typeface="' + escapeXml(typeface) + '" charset="0"/>';
                    ['regular', 'bold', 'italic', 'boldItalic'].forEach(function (slot) {
                        if (byTypeface[typeface][slot]) list += '<p:' + slot + ' r:id="' + byTypeface[typeface][slot] + '"/>';
                    });
                    list += '</p:embeddedFont>';
                });
                // CT_Presentation order: … sldSz, notesSz, smartTags, embeddedFontLst, …
                presentation = presentation.replace(/(<p:notesSz[^>]*\/>)/, '$1<p:embeddedFontLst>' + list + '</p:embeddedFontLst>');
                if (presentation.indexOf('embedTrueTypeFonts=') < 0) presentation = presentation.replace('<p:presentation ', '<p:presentation embedTrueTypeFonts="1" ');
                if (types.indexOf('Extension="fntdata"') < 0) types = types.replace('<Default ', '<Default Extension="fntdata" ContentType="application/x-fontdata"/><Default ');
                zip.file('ppt/presentation.xml', presentation);
                zip.file('ppt/_rels/presentation.xml.rels', rels.replace('</Relationships>', added + '</Relationships>'));
                zip.file('[Content_Types].xml', types);
                return zip.generateAsync({ type: 'blob', compression: 'DEFLATE', mimeType: 'application/vnd.openxmlformats-officedocument.presentationml.presentation' });
            });
        });
    }

    // Hands the file to the page's own saver when it has one (an embedding
    // host that cannot start a download itself), else downloads it.
    function saveBlob(blob, fileName) {
        if (typeof window.nekoDeckSave === 'function') return Promise.resolve(window.nekoDeckSave(fileName, blob));
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(url); a.remove(); }, 1000);
        return Promise.resolve(fileName);
    }

    // ---------------------------------------------------------------- driver

    function exportDeck(options) {
        options = options || {};
        if (typeof window.PptxGenJS !== 'function') {
            return Promise.reject(new Error('PptxGenJS is not loaded'));
        }

        imageCache = {};
        fontCache = {};
        usedFonts = {};
        var fontsBase = options.fontsBase || '';

        return loadFontCatalog(fontsBase).then(buildStage).then(function (frame) {
            var win = frame.contentWindow;
            var doc = frame.contentDocument;
            var body = doc.body;
            var slides = Array.prototype.slice.call(doc.querySelectorAll('.deck-slide'));
            var brand = doc.querySelector('.deck-brand');

            var pptx = new window.PptxGenJS();
            pptx.layout = 'LAYOUT_WIDE';
            if (options.title) pptx.title = options.title;
            pptx.company = options.company || '';
            pptx.subject = options.description || '';

            var slideIds = {};
            slides.forEach(function (s, i) { if (s.id) slideIds[s.id] = i + 1; });

            var background = parseColor(win.getComputedStyle(body).backgroundColor) ||
                parseColor(win.getComputedStyle(doc.documentElement).backgroundColor) || { hex: 'FFFFFF', alpha: 1 };

            var pending = [];
            slides.forEach(function (section, index) {
                slides.forEach(function (s) { s.classList.remove('is-on'); });
                section.classList.add('is-on');
                win.scrollTo(0, 0);
                materializeSlide(win, section);

                // A slide taller than the frame is scaled down to fit — the
                // on-screen deck would have scrolled it instead.
                // A theme that clips its canvas (curiosity) never scrolls, so
                // what overflows it is not part of the slide.
                var clips = /hidden|clip/.test(win.getComputedStyle(section).overflow);
                var height = clips ? SLIDE_H : Math.max(section.getBoundingClientRect().bottom, section.scrollHeight, SLIDE_H);
                var s = Math.min(1, SLIDE_H / height);
                var map = { s: s, ox: (SLIDE_W - SLIDE_W * s) / 2, oy: 0 };

                var slide = pptx.addSlide();
                slide.background = { color: background.hex };

                var writer = new SlideWriter(pptx, slide, win, map, slideIds);
                writer.walk(section);

                if (brand) {
                    // The brand mark is pinned to the viewport, not the slide, so
                    // it keeps its corner regardless of any fit-to-slide scaling.
                    writer.map = { s: 1, ox: 0, oy: 0 };
                    writer.walk(brand);
                }

                pending = pending.concat(writer.pending);
            });

            return Promise.all(pending).then(function () {
                var fileName = (options.fileName || slugify(options.title)).replace(/\.pptx$/i, '') + '.pptx';
                return pptx.write({ outputType: 'arraybuffer', compression: true }).then(function (buffer) {
                    return embedFonts(buffer, fontsBase);
                }).then(function (blob) {
                    return saveBlob(blob, fileName);
                });
            }).finally(function () {
                if (frame.parentNode) frame.parentNode.removeChild(frame);
            });
        });
    }

    window.nekoDeckExportPptx = exportDeck;
})();
