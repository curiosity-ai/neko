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
        head += '<style>html,body{overflow:hidden!important;margin:0}' +
            '.deck-slide{animation:none!important;transition:none!important}' +
            '.neko-deck-body .deck-slide{display:none!important}' +
            '.neko-deck-body .deck-slide.is-on{display:flex!important}</style>';

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
                var fonts = doc.fonts && doc.fonts.ready ? doc.fonts.ready : Promise.resolve();
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
                if (isInlineLevel(ccs) && !isMedia(child)) {
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

        var runs = [];
        this.collectRuns(group, runs, containerCs.whiteSpace);
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
        if (!/flex|grid/.test(containerCs.display)) {
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
        var slack = Math.min(16, Math.max(4, width * 0.02));
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

        this.slide.addText(items, {
            x: this.x(left), y: this.y(box.top),
            w: this.len(width), h: this.len(box.bottom - box.top),
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

        var color = parseColor(cs.color);
        var weight = parseInt(cs.fontWeight, 10) || 400;
        var decoration = cs.textDecorationLine || cs.textDecoration || '';
        var options = {
            fontFace: fontFace(cs.fontFamily, parent.ownerDocument),
            fontSize: Math.max(1, this.pt(parseFloat(cs.fontSize) || 16)),
            bold: weight >= 600,
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
                runs.push({ text: lines[l], options: opts, preserve: true });
            }
        } else {
            runs.push({ text: text, options: options, preserve: preserve });
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
        for (var i = 0; i < source.length && i < target.length; i++) {
            var cs = this.win.getComputedStyle(source[i]);
            var style = '';
            for (var p = 0; p < SVG_STYLE_PROPS.length; p++) {
                var value = cs.getPropertyValue(SVG_STYLE_PROPS[p]);
                if (value) style += SVG_STYLE_PROPS[p] + ':' + value + ';';
            }
            if (cs.display === 'none') style += 'display:none;';
            target[i].setAttribute('style', style + (target[i].getAttribute('style') || ''));
        }

        replaceForeignObjects(svg, clone, this.win);

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

    // ---------------------------------------------------------------- driver

    function exportDeck(options) {
        options = options || {};
        if (typeof window.PptxGenJS !== 'function') {
            return Promise.reject(new Error('PptxGenJS is not loaded'));
        }

        imageCache = {};
        fontCache = {};

        return buildStage().then(function (frame) {
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

                // A slide taller than the frame is scaled down to fit — the
                // on-screen deck would have scrolled it instead.
                var height = Math.max(section.getBoundingClientRect().bottom, section.scrollHeight, SLIDE_H);
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
                return pptx.writeFile({ fileName: fileName, compression: true });
            }).finally(function () {
                if (frame.parentNode) frame.parentNode.removeChild(frame);
            });
        });
    }

    window.nekoDeckExportPptx = exportDeck;
})();
