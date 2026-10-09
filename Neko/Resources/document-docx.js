/* Neko paged documents — Word export.

   Turns the sheets on the page into a .docx with docx (vendored as
   assets/docx.bundle.js, loaded by document.js only when a reader asks for the
   download). The file is made of native, editable Word content — paragraphs,
   tables, headers and footers — rather than a picture of the page.

   How: the sheets are cloned into an off-screen iframe exactly one page wide
   (794 CSS px, A4 at 96 dpi) and the exporter reads what the browser laid out:
   the computed style of every block and the position of every box. Each sheet
   becomes a Word section. The running head and the foot become the section's
   header and footer; the ground, the art and any panel behind the text become
   floating pictures anchored in the header; the body is walked block by block.
   Text keeps its typeface, size, colour, tracking and line height; blocks keep
   their gaps (the space between one block's bottom and the next one's top is
   measured, so the vertical rhythm matches the page); side-by-side layouts
   (grid, flex rows, columns) become tables, boxes with a ground, borders or
   padding become one-cell tables, and what only a picture can say (SVG
   diagrams, generated art, a mask-drawn logo) is rasterised.

   The theme's typefaces (assets/deckfonts/) are embedded in the file, so it
   looks the same where they are not installed. One CSS pixel is 1/96" and
   15 twips, so the mapping is exact rather than approximate. What does not
   survive: shadows, gradients, icon-font glyphs and CSS-generated content. */
(function () {
    'use strict';

    var PX = 15;            // twips per CSS px
    var EMU = 9525;         // EMU per CSS px
    var A4 = { w: 794, h: 1123, tw: 11906, th: 16838 };
    var LETTER = { w: 816, h: 1056, tw: 12240, th: 15840 };

    var GENERIC_FONTS = {
        'serif': 'Georgia', 'sans-serif': 'Arial', 'system-ui': 'Arial', 'ui-sans-serif': 'Arial', 'ui-serif': 'Georgia',
        'monospace': 'Courier New', 'ui-monospace': 'Courier New', 'cursive': 'Comic Sans MS', 'fantasy': 'Impact'
    };
    var BLOCKY = { block: 1, flex: 1, grid: 1, 'list-item': 1, table: 1, 'flow-root': 1, 'table-row': 1, 'table-cell': 1,
        'table-row-group': 1, 'table-header-group': 1, 'table-footer-group': 1, 'table-caption': 1 };
    var SKIP_TAGS = { SCRIPT: 1, STYLE: 1, TEMPLATE: 1, NOSCRIPT: 1, LINK: 1, META: 1, IFRAME: 1, VIDEO: 1, AUDIO: 1, OBJECT: 1, EMBED: 1, BUTTON: 1, SELECT: 1, TEXTAREA: 1 };

    var D = null;       // the docx library
    var ST = null;      // the export in progress

    // ---------------------------------------------------------------- colours

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
        return { r: r, g: g, b: b, a: a, hex: toHex(r) + toHex(g) + toHex(b) };
    }

    function parseAlpha(raw) { return raw.charAt(raw.length - 1) === '%' ? parseFloat(raw) / 100 : parseFloat(raw); }
    function toHex(n) { var v = Math.max(0, Math.min(255, Math.round(n))).toString(16); return v.length === 1 ? '0' + v : v; }

    // A colour with alpha, laid over a ground: Word has no alpha on text.
    function over(color, groundHex, extraOpacity) {
        var a = (color.a === undefined ? 1 : color.a) * (extraOpacity === undefined ? 1 : extraOpacity);
        if (a >= 0.99) return color.hex;
        var g = parseColor('#' + groundHex) || hexToRgb(groundHex);
        return toHex(color.r * a + g.r * (1 - a)) + toHex(color.g * a + g.g * (1 - a)) + toHex(color.b * a + g.b * (1 - a));
    }

    function hexToRgb(hex) { return { r: parseInt(hex.substr(0, 2), 16), g: parseInt(hex.substr(2, 2), 16), b: parseInt(hex.substr(4, 2), 16) }; }

    // ---------------------------------------------------------------- helpers

    function px(v) { var n = parseFloat(v); return isNaN(n) ? 0 : n; }
    function tw(v) { return Math.round(v * PX); }
    function css(el) { return ST.win.getComputedStyle(el); }

    function rectOf(el) {
        var r = el.getBoundingClientRect();
        return { left: r.left - ST.ox, top: r.top - ST.oy, right: r.right - ST.ox, bottom: r.bottom - ST.oy, width: r.width, height: r.height };
    }

    function isFurniture(el) {
        return el.hasAttribute && (el.hasAttribute('data-doc-bg') || el.hasAttribute('data-doc-part'));
    }

    function isInlineLevel(st) {
        var d = st.display;
        return d === 'inline' || d === 'inline-block' || d === 'inline-flex' || d === 'inline-grid' || d === 'inline-table' || d === 'ruby';
    }

    function isHidden(el, st) {
        if (st.display === 'none' || st.visibility === 'hidden' || st.visibility === 'collapse') return true;
        if (parseFloat(st.opacity) === 0) return true;
        return false;
    }

    function blobToBytes(blob) {
        return new Promise(function (resolve, reject) {
            if (blob.arrayBuffer) { blob.arrayBuffer().then(function (b) { resolve(new Uint8Array(b)); }, reject); return; }
            var fr = new FileReader();
            fr.onload = function () { resolve(new Uint8Array(fr.result)); };
            fr.onerror = reject;
            fr.readAsArrayBuffer(blob);
        });
    }

    function bytesToBase64(bytes) {
        var s = '', chunk = 0x8000;
        for (var i = 0; i < bytes.length; i += chunk) s += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
        return btoa(s);
    }

    function escapeAttr(value) { return String(value).replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;'); }
    function escapeXml(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }

    function slugify(value) {
        var s = (value || '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
        return s || 'document';
    }

    // ---------------------------------------------------------------- fonts

    // The fonts the theme ships (assets/deckfonts/deck-fonts.json): family ->
    // weight -> { typeface, slot, file }. A family found here is named the way
    // Word knows the static instance at that weight, and the file is embedded.
    // docx embeds one (regular) face per name, so a face the catalog files under
    // the bold slot is replaced by the heaviest regular-slot instance.
    var fontCatalog = {};

    function catalogFace(name, weight) {
        var weights = fontCatalog[name];
        if (!weights) return null;
        var best = null, bestDistance = Infinity;
        Object.keys(weights).forEach(function (w) {
            var face = weights[w];
            if (face.slot !== 'regular') return;
            var wn = parseInt(w, 10), d = Math.abs(wn - weight);
            if (d < bestDistance || (d === bestDistance && wn > weight)) { best = face; bestDistance = d; }
        });
        return best;
    }

    // { name, bold, embedded, file }: the typeface a run is set in.
    function runFont(family, weight) {
        var stack = (family || '').split(',');
        for (var i = 0; i < stack.length; i++) {
            var name = stack[i].trim().replace(/^["']|["']$/g, '');
            if (!name) continue;
            var face = catalogFace(name, weight);
            if (face) {
                ST.usedFonts[face.typeface] = face.file;
                return { name: face.typeface, bold: false, embedded: true };
            }
            if (GENERIC_FONTS[name.toLowerCase()] || fontAvailable(name)) break;
        }
        return { name: fontFace(family), bold: weight >= 600, embedded: false };
    }

    function fontFace(family) {
        var stack = (family || '').split(',');
        for (var i = 0; i < stack.length; i++) {
            var name = stack[i].trim().replace(/^["']|["']$/g, '');
            if (!name) continue;
            var generic = GENERIC_FONTS[name.toLowerCase()];
            if (generic) return generic;
            if (fontAvailable(name)) return name;
        }
        return 'Arial';
    }

    var fontCache = {};

    function fontAvailable(name) {
        if (name in fontCache) return fontCache[name];
        var available = true;
        try {
            var ctx = ST.doc.createElement('canvas').getContext('2d');
            var probe = 'mmmmmmmmmmlli1WQ@#';
            var quoted = '"' + name.replace(/"/g, '') + '"';
            available = ['monospace', 'serif', 'sans-serif'].some(function (g) {
                ctx.font = '72px ' + g;
                var base = ctx.measureText(probe).width;
                ctx.font = '72px ' + quoted + ', ' + g;
                return ctx.measureText(probe).width !== base;
            });
        } catch (e) { available = true; }
        fontCache[name] = available;
        return available;
    }

    function loadFontCatalog(base) {
        if (window.nekoDeckFonts && window.nekoDeckFonts.catalog) { fontCatalog = window.nekoDeckFonts.catalog; return Promise.resolve(); }
        if (!base) return Promise.resolve();
        return fetch(base + 'deck-fonts.json').then(function (res) { return res.ok ? res.json() : {}; })
            .then(function (c) { fontCatalog = c || {}; }, function () { fontCatalog = {}; });
    }

    // A Neko deck or document carries its fonts inline: each @font-face in
    // <style id="neko-deck-fonts"> is a data URI, tagged with the file it came from.
    function inlineFontUrl(file) {
        var style = document.getElementById('neko-deck-fonts');
        if (!style) return null;
        var re = /\/\*neko-font:([^*]+)\*\/@font-face\{[^}]*?url\("?(data:[^")]+)"?\)/g, m;
        while ((m = re.exec(style.textContent))) {
            if (m[1] === file) return m[2];
        }
        return null;
    }

    var fontFiles = {};

    function fontBytes(file) {
        if (!fontFiles[file]) {
            fontFiles[file] = fetch(inlineFontUrl(file) || ST.fontsBase + file).then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.arrayBuffer();
            }).then(function (b) { return new Uint8Array(b); });
        }
        return fontFiles[file];
    }

    // @font-face rules, as data URIs, for the faces an SVG's text uses: an SVG
    // drawn as an image cannot reach the page's fonts.
    var fontFaceCss = {};

    function svgFontCss(families) {
        var jobs = [];
        Object.keys(families).forEach(function (key) {
            var parts = key.split('|'), family = parts[0], weight = +parts[1];
            var weights = fontCatalog[family];
            if (!weights) return;
            var best = null, bd = Infinity, bw = 400;
            Object.keys(weights).forEach(function (w) {
                var d = Math.abs(parseInt(w, 10) - weight);
                if (d < bd) { bd = d; best = weights[w]; bw = parseInt(w, 10); }
            });
            if (!best) return;
            var id = family + '|' + bw;
            if (!fontFaceCss[id]) {
                fontFaceCss[id] = fontBytes(best.file).then(function (bytes) {
                    return '@font-face{font-family:"' + family + '";font-weight:' + bw + ';src:url(data:font/ttf;base64,' + bytesToBase64(bytes) + ') format("truetype")}';
                }, function () { return ''; });
            }
            jobs.push(fontFaceCss[id]);
        });
        return Promise.all(jobs).then(function (rules) { return rules.join(''); });
    }

    // ---------------------------------------------------------------- pictures

    function canvasFor(w, h) {
        var c = ST.doc.createElement('canvas');
        c.width = Math.max(1, Math.round(w));
        c.height = Math.max(1, Math.round(h));
        return c;
    }

    function canvasPng(canvas) {
        return new Promise(function (resolve, reject) {
            canvas.toBlob(function (blob) { blob ? blobToBytes(blob).then(resolve, reject) : reject(new Error('canvas toBlob failed')); }, 'image/png');
        });
    }

    function loadImage(src) {
        return new Promise(function (resolve, reject) {
            var img = new ST.win.Image();
            img.onload = function () { resolve(img); };
            img.onerror = function () { reject(new Error('could not load image: ' + String(src).slice(0, 400))); };
            img.src = src;
        });
    }

    // A solid rectangle: the ground of a page, a panel, a mark.
    var solidCache = {};
    function solidPng(hex) {
        if (!solidCache[hex]) {
            var c = canvasFor(2, 2), ctx = c.getContext('2d');
            ctx.fillStyle = '#' + hex;
            ctx.fillRect(0, 0, 2, 2);
            solidCache[hex] = canvasPng(c);
        }
        return solidCache[hex];
    }

    // A small box with a border — a table mark, a checkbox.
    function boxPng(w, h, fillHex, borderHex, borderPx) {
        var scale = 4, c = canvasFor(w * scale, h * scale), ctx = c.getContext('2d');
        if (fillHex) { ctx.fillStyle = '#' + fillHex; ctx.fillRect(0, 0, c.width, c.height); }
        if (borderHex && borderPx > 0) {
            var b = borderPx * scale;
            ctx.strokeStyle = '#' + borderHex;
            ctx.lineWidth = b;
            ctx.strokeRect(b / 2, b / 2, c.width - b, c.height - b);
        }
        return canvasPng(c);
    }

    var SVG_PROPS = ['fill', 'fill-opacity', 'stroke', 'stroke-width', 'stroke-opacity', 'stroke-linecap', 'stroke-linejoin', 'stroke-dasharray',
        'opacity', 'font-family', 'font-size', 'font-weight', 'font-style', 'letter-spacing', 'text-anchor', 'display', 'visibility'];

    // Serialises an SVG with every computed paint written onto the elements —
    // the page's custom properties and `currentColor` do not exist in an image.
    function standaloneSvg(svg, w, h) {
        var clone = svg.cloneNode(true);
        var families = {};
        var origs = [svg].concat(Array.prototype.slice.call(svg.querySelectorAll('*')));
        var clones = [clone].concat(Array.prototype.slice.call(clone.querySelectorAll('*')));
        origs.forEach(function (o, i) {
            var cs = css(o), style = '';
            SVG_PROPS.forEach(function (p) {
                var v = cs.getPropertyValue(p);
                if (v) style += p + ':' + v + ';';
            });
            if (i === 0) style += 'color:' + cs.color + ';';
            clones[i].setAttribute('style', style);
            clones[i].removeAttribute('class');
            if (o.tagName && /^(text|tspan)$/i.test(o.tagName)) {
                families[cs.fontFamily.split(',')[0].trim().replace(/^["']|["']$/g, '') + '|' + (parseInt(cs.fontWeight, 10) || 400)] = true;
            }
        });
        clone.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
        clone.setAttribute('width', w);
        clone.setAttribute('height', h);
        clone.removeAttribute('role');
        return svgFontCss(families).then(function (fonts) {
            if (fonts) {
                var style = ST.doc.createElementNS('http://www.w3.org/2000/svg', 'style');
                style.textContent = fonts;
                clone.insertBefore(style, clone.firstChild);
            }
            return new ST.win.XMLSerializer().serializeToString(clone);
        });
    }

    // An inline SVG as a PNG at `w` x `h` CSS px, drawn at 3x.
    function svgPng(svg, w, h, scale) {
        scale = scale || 3;
        return standaloneSvg(svg, Math.round(w * scale), Math.round(h * scale)).then(function (markup) {
            return loadImage('data:image/svg+xml;charset=utf-8,' + encodeURIComponent(markup));
        }).then(function (img) {
            var c = canvasFor(w * scale, h * scale);
            c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
            return canvasPng(c);
        });
    }

    // A raster or SVG file from a URL, as PNG/JPEG/GIF bytes: { type, data, w, h }.
    var imageCache = {};
    function pictureFromUrl(url, w, h) {
        var key = url + '|' + Math.round(w) + 'x' + Math.round(h);
        if (!imageCache[key]) {
            imageCache[key] = fetch(url).then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.blob();
            }).then(function (blob) {
                var type = (blob.type || '').toLowerCase();
                if (/image\/(png|jpe?g|gif|bmp)/.test(type) && !/svg/.test(type)) {
                    return blobToBytes(blob).then(function (data) { return { type: /jpe?g/.test(type) ? 'jpg' : type.replace('image/', ''), data: data }; });
                }
                // SVG, WebP, AVIF: draw it to a canvas at 3x.
                return loadImage(URL.createObjectURL(blob)).then(function (img) {
                    var c = canvasFor(w * 3, h * 3);
                    c.getContext('2d').drawImage(img, 0, 0, c.width, c.height);
                    return canvasPng(c).then(function (data) { return { type: 'png', data: data }; });
                });
            });
        }
        return imageCache[key];
    }

    // A shape drawn through a CSS mask in the element's own colour (a customer's logo).
    function maskPng(el, st, w, h) {
        var m = st.maskImage && st.maskImage !== 'none' ? st.maskImage : st.webkitMaskImage;
        var match = /url\(["']?([^"')]+)["']?\)/.exec(m || '');
        if (!match) return Promise.reject(new Error('no mask'));
        var color = parseColor(st.backgroundColor) || parseColor(st.color) || { hex: '000000' };
        return fetch(match[1]).then(function (res) { return res.text(); }).then(function (text) {
            var data = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(text);
            return loadImage(data);
        }).then(function (img) {
            var scale = 3, c = canvasFor(w * scale, h * scale), ctx = c.getContext('2d');
            var ir = (img.naturalWidth || w) / (img.naturalHeight || h), br = w / h, dw = c.width, dh = c.height;
            // mask-size: contain; mask-position: left center
            if (ir > br) dh = dw / ir; else dw = dh * ir;
            ctx.drawImage(img, 0, (c.height - dh) / 2, dw, dh);
            ctx.globalCompositeOperation = 'source-in';
            ctx.fillStyle = '#' + color.hex;
            ctx.fillRect(0, 0, c.width, c.height);
            return canvasPng(c);
        });
    }

    // ---------------------------------------------------------------- stage

    // The off-screen frame: the page's stylesheets, the document's theme
    // attributes, and a copy of the sheets laid out one page wide.
    function buildStage() {
        var frame = document.createElement('iframe');
        frame.setAttribute('aria-hidden', 'true');
        frame.setAttribute('tabindex', '-1');
        frame.style.cssText = 'position:fixed;left:-30000px;top:0;width:' + ST.page.w + 'px;height:1400px;border:0;visibility:hidden;pointer-events:none;';
        document.body.appendChild(frame);

        var head = '<base href="' + escapeAttr(location.href) + '">';
        // Every stylesheet in the document, not only the head's: a host that
        // wraps the page in its own skeleton (a sandboxed preview) moves the
        // page's <head> into its <body>.
        Array.prototype.forEach.call(document.querySelectorAll('link[rel~="stylesheet"], style'), function (node) { head += node.outerHTML; });
        head += '<style>html,body{margin:0!important;padding:0!important;overflow:hidden!important;background:#fff!important}' +
            '.docs{display:block!important;padding:0!important;gap:0!important}' +
            '.doc-page{width:' + ST.page.w + 'px!important;max-width:none!important;margin:0 0 0 0!important;box-shadow:none!important;outline:0!important}' +
            '*{animation:none!important;transition:none!important}</style>';

        var html = document.documentElement, body = document.body;
        var sheets = '';
        Array.prototype.forEach.call(document.querySelectorAll('.doc-page'), function (s) { sheets += s.outerHTML; });

        var markup = '<!DOCTYPE html><html class="' + escapeAttr(html.className) + '" data-doc-theme="' + escapeAttr(html.getAttribute('data-doc-theme') || '') +
            '" data-doc-size="' + escapeAttr(html.getAttribute('data-doc-size') || '') + '"><head><meta charset="utf-8">' + head + '</head><body class="' +
            escapeAttr(body.className) + '"><main class="docs" id="docs">' + sheets + '</main></body></html>';

        return new Promise(function (resolve) {
            var settled = false;
            function done() {
                if (settled) return;
                settled = true;
                var doc = frame.contentDocument;
                // Load, in the stage, each face the page itself has loaded, so the
                // first measurement is of the real typeface and not its fallback.
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
                var fonts = Promise.all(loads).then(function () { return doc.fonts && doc.fonts.ready ? doc.fonts.ready : null; });
                var images = Array.prototype.map.call(doc.images, function (img) {
                    if (img.complete) return Promise.resolve();
                    return new Promise(function (r) { img.addEventListener('load', r, { once: true }); img.addEventListener('error', r, { once: true }); });
                });
                Promise.all([fonts].concat(images)).then(function () { resolve(frame); }, function () { resolve(frame); });
            }
            frame.addEventListener('load', done, { once: true });
            setTimeout(done, 10000);
            var doc = frame.contentDocument;
            doc.open();
            doc.write(markup);
            doc.close();
        });
    }

    // ---------------------------------------------------------------- runs

    function lineHeightPx(st) {
        var lh = parseFloat(st.lineHeight);
        return isNaN(lh) ? px(st.fontSize) * 1.25 : lh;
    }

    function transformText(text, mode) {
        if (mode === 'uppercase') return text.toUpperCase();
        if (mode === 'lowercase') return text.toLowerCase();
        if (mode === 'capitalize') return text.replace(/(^|\s)(\S)/g, function (m, a, b) { return a + b.toUpperCase(); });
        return text;
    }

    // The ground under an element: the nearest ancestor that paints one.
    function groundOf(el) {
        for (var n = el; n && n.nodeType === 1; n = n.parentElement) {
            var c = parseColor(css(n).backgroundColor);
            if (c && c.a > 0.5) return c.hex;
        }
        return ST.sheetGround;
    }

    function opacityOf(el) {
        var o = 1;
        for (var n = el; n && n.nodeType === 1; n = n.parentElement) {
            var v = parseFloat(css(n).opacity);
            if (!isNaN(v)) o *= v;
        }
        return o;
    }

    function textRunOptions(el, st) {
        var weight = parseInt(st.fontWeight, 10) || 400;
        var font = runFont(st.fontFamily, weight);
        var color = parseColor(st.color) || { hex: '000000', a: 1, r: 0, g: 0, b: 0 };
        var o = {
            font: font.name,
            size: Math.max(2, Math.round(px(st.fontSize) * 1.5)),
            color: over(color, groundOf(el), opacityOf(el)),
            bold: font.bold,
            italics: st.fontStyle === 'italic' || st.fontStyle === 'oblique'
        };
        var ls = parseFloat(st.letterSpacing);
        if (!isNaN(ls) && Math.abs(ls) > 0.01) o.characterSpacing = Math.round(ls * PX);
        var deco = st.textDecorationLine || st.textDecoration || '';
        if (deco.indexOf('underline') >= 0) o.underline = { type: D.UnderlineType.SINGLE };
        if (deco.indexOf('line-through') >= 0) o.strike = true;
        var va = st.verticalAlign;
        if (va === 'super') o.superScript = true;
        else if (va === 'sub') o.subScript = true;
        // an inline run on its own ground (inline code, a mark)
        var bg = parseColor(st.backgroundColor);
        if (bg && bg.a > 0.3 && el.parentElement && isInlineLevel(st)) o.shading = { type: D.ShadingType.CLEAR, fill: bg.hex, color: 'auto' };
        return o;
    }

    function isBlankPiece(text) { return /^[ \t\r\n\f]*$/.test(text); }

    // Turns inline nodes (text, <br>, inline elements, small pictures) into
    // Word run children. Whitespace collapses the way CSS collapses it.
    async function inlineChildren(nodes, ctx) {
        // the element that holds the printed page number is a live field in Word
        var holder = nodes.length && nodes[0].parentElement;
        if (holder && holder.getAttribute('data-doc-field') === 'page') {
            var fst = css(holder);
            var fieldOnly = [fieldRun(holder, fst)];
            fieldOnly.hasPicture = false;
            fieldOnly.fieldRunOptions = textRunOptions(holder, fst);
            return fieldOnly;
        }
        var descs = [];                    // { text, opts } for text, { raw } for anything already built
        var state = { lastSpace: true };   // true at the start of a line: leading space is dropped
        var hasPicture = false;

        function emitText(parent, st, text) {
            text = text.replace(/[ \t\r\n\f]+/g, ' ');
            if (state.lastSpace && text.charAt(0) === ' ') text = text.slice(1);
            if (!text.length) return;
            state.lastSpace = text.charAt(text.length - 1) === ' ';
            descs.push({ text: text, opts: textRunOptions(parent, st) });
        }

        async function walk(node) {
            if (node.nodeType === 3) {
                var parent = node.parentElement, st = css(parent);
                var ws = st.whiteSpace;
                var text = transformText(node.nodeValue, st.textTransform);
                if (ws === 'pre' || ws === 'pre-wrap' || ws === 'break-spaces') {
                    text.replace(/\r/g, '').split('\n').forEach(function (line, i) {
                        if (i > 0) descs.push({ raw: new D.TextRun({ break: 1 }) });
                        if (line.length) descs.push({ text: line.replace(/\t/g, '    '), opts: textRunOptions(parent, st) });
                    });
                    state.lastSpace = false;
                    return;
                }
                if (ws === 'pre-line') {
                    text.replace(/[ \t\f\r]+/g, ' ').replace(/ ?\n ?/g, '\n').split('\n').forEach(function (p, i) {
                        if (i > 0) { descs.push({ raw: new D.TextRun({ break: 1 }) }); state.lastSpace = true; }
                        emitText(parent, st, p);
                    });
                    return;
                }
                emitText(parent, st, text);
                return;
            }
            if (node.nodeType !== 1) return;
            var el = node, est = css(el);
            if (SKIP_TAGS[el.tagName] || isHidden(el, est) || isFurniture(el)) return;
            if (el.tagName === 'BR') { descs.push({ raw: new D.TextRun({ break: 1 }) }); state.lastSpace = true; return; }

            // the printed page number is a live field in Word
            if (el.getAttribute && el.getAttribute('data-doc-field') === 'page') {
                descs.push({ raw: fieldRun(el, est) });
                state.lastSpace = false;
                return;
            }

            var pic = await inlinePicture(el, est);
            if (pic) {
                descs.push({ raw: pic }); hasPicture = true; state.lastSpace = false;
                // a checkbox is followed by a tab, which lands on the text's hanging indent
                if (el.tagName === 'INPUT') { descs.push({ raw: new D.TextRun({ children: [new D.Tab()] }) }); state.lastSpace = true; }
                return;
            }

            // an inline box with a ground (a pill): padded with non-breaking spaces so the ground has room
            if (est.display === 'inline-block' && backgroundOf(el, est) && px(est.paddingLeft) > 2) {
                var padN = Math.max(1, Math.round(px(est.paddingLeft) / (px(est.fontSize) * 0.3)));
                var padText = new Array(padN + 1).join('\u00A0');
                var padOpts = function () { return textRunOptions(el, est); };
                descs.push({ text: padText, opts: padOpts() });
                state.lastSpace = false;
                for (var q = el.firstChild; q; q = q.nextSibling) await walk(q);
                descs.push({ text: padText, opts: padOpts() });
                state.lastSpace = false;
                return;
            }

            var href = el.tagName === 'A' ? el.getAttribute('href') : null;
            if (href && /^(https?:|mailto:)/i.test(href)) {
                var before = descs.length;
                for (var c = el.firstChild; c; c = c.nextSibling) await walk(c);
                var group = descs.splice(before);
                if (group.length) descs.push({ link: el.href, group: group });
                return;
            }
            for (var k = el.firstChild; k; k = k.nextSibling) await walk(k);
        }

        for (var i = 0; i < nodes.length; i++) await walk(nodes[i]);

        // a trailing collapsible space is not shown
        (function trim(list) {
            var last = list[list.length - 1];
            if (!last) return;
            if (last.group) trim(last.group);
            else if (last.text !== undefined && /\s$/.test(last.text)) {
                last.text = last.text.replace(/\s+$/, '');
                if (!last.text.length) { list.pop(); trim(list); }
            }
        })(descs);

        function build(list) {
            return list.map(function (d) {
                if (d.raw) return d.raw;
                if (d.group) return new D.ExternalHyperlink({ link: d.link, children: build(d.group) });
                return new D.TextRun(Object.assign(d.opts, { text: d.text }));
            });
        }
        var out = build(descs);
        out.hasPicture = hasPicture;
        return out;
    }

    // A small picture that sits in a line of text: an SVG icon or lockup, an
    // image, a table mark or a checkbox. Returns an ImageRun, or null.
    async function inlinePicture(el, st) {
        var tag = el.tagName;
        var r = el.getBoundingClientRect();
        if (tag === 'SVG' || tag === 'svg') {
            if (r.width < 1 || r.height < 1) return null;
            var data = await svgPng(el, r.width, r.height);
            return new D.ImageRun({ type: 'png', data: data, transformation: { width: r.width, height: r.height }, altText: alt(altOf(el)) });
        }
        if (tag === 'IMG') {
            if (r.width < 1 || r.height < 1) return null;
            var pic = await pictureFromUrl(el.currentSrc || el.src, r.width, r.height);
            return new D.ImageRun({ type: pic.type, data: pic.data, transformation: { width: r.width, height: r.height }, altText: alt(altOf(el)) });
        }
        if (tag === 'INPUT' && el.type === 'checkbox') {
            var w = Math.max(6, r.width), h = Math.max(6, r.height);
            var color = (parseColor(st.borderTopColor) || parseColor(st.color) || { hex: '000000' }).hex;
            var png = await boxPng(w, h, el.checked ? color : null, color, Math.max(1, px(st.borderTopWidth)));
            return new D.ImageRun({ type: 'png', data: png, transformation: { width: w, height: h }, altText: alt('checkbox') });
        }
        // an inline-block with no text of its own that paints a ground or a border: a mark
        if (st.display === 'inline-block' && r.width >= 2 && r.height >= 2 && !el.textContent.trim() && !el.querySelector('*')) {
            var bg = parseColor(st.backgroundColor), bc = parseColor(st.borderTopColor), bw = px(st.borderTopWidth);
            if (bg || (bc && bw > 0)) {
                var png2 = await boxPng(r.width, r.height, bg ? over(bg, groundOf(el.parentElement), opacityOf(el)) : null, bc && bw > 0 ? bc.hex : null, bw);
                return new D.ImageRun({ type: 'png', data: png2, transformation: { width: r.width, height: r.height }, altText: alt(el.getAttribute('aria-label') || '') });
            }
        }
        return null;
    }

    // Word's schema requires a name on every picture (wp:docPr); an empty alt text writes none, and Word
    // then calls the whole file unreadable.
    function alt(text) { return { name: 'Picture', description: String(text || ''), title: '' }; }

    function altOf(el) { return el.getAttribute('aria-label') || el.getAttribute('alt') || (el.querySelector && el.querySelector('title') ? el.querySelector('title').textContent : '') || ''; }

    // ---------------------------------------------------------------- paragraphs

    var TINY = function (heightTwips) {
        return new D.Paragraph({
            spacing: { before: 0, after: 0, line: Math.max(20, Math.round(heightTwips)), lineRule: D.LineRuleType.EXACT },
            children: [new D.TextRun({ text: '', size: 2 })]
        });
    };

    function alignOf(st) {
        switch (st.textAlign) {
            case 'center': return D.AlignmentType.CENTER;
            case 'right': case 'end': return D.AlignmentType.RIGHT;
            case 'justify': return D.AlignmentType.BOTH;
            default: return D.AlignmentType.LEFT;
        }
    }

    function borderOptions(width, style, colorHex, space) {
        if (!width || style === 'none' || style === 'hidden') return null;
        var s = D.BorderStyle.SINGLE;
        if (style === 'dashed') s = D.BorderStyle.DASHED; else if (style === 'dotted') s = D.BorderStyle.DOTTED; else if (style === 'double') s = D.BorderStyle.DOUBLE;
        return { style: s, size: Math.max(2, Math.round(width * 6)), color: colorHex, space: Math.max(0, Math.min(31, Math.round(space || 0))) };
    }

    // The four borders of an element: { top, right, bottom, left } — each null or a docx border.
    function bordersOf(st, el, spaces) {
        var ground = groundOf(el.parentElement || el);
        function one(side) {
            var w = px(st['border' + side + 'Width']), style = st['border' + side + 'Style'];
            var c = parseColor(st['border' + side + 'Color']);
            if (!w || !c) return null;
            return borderOptions(w, style, over(c, ground, opacityOf(el)), spaces ? spaces[side] : 0);
        }
        return { top: one('Top'), right: one('Right'), bottom: one('Bottom'), left: one('Left') };
    }

    function hasBorders(b) { return !!(b.top || b.right || b.bottom || b.left); }

    function backgroundOf(el, st) {
        var c = parseColor(st.backgroundColor);
        if (!c || c.a < 0.05) return null;
        var parentGround = groundOf(el.parentElement || el);
        var hex = over(c, parentGround, 1);
        return hex.toLowerCase() === parentGround.toLowerCase() ? null : hex;
    }

    // Everything a paragraph needs from the block that holds its text.
    function paragraphProps(el, st, ctx, geom, hasPicture) {
        var fs = px(st.fontSize), lh = lineHeightPx(st);
        var before = Math.max(0, geom.top - ctx.cursor);
        var props = {
            alignment: alignOf(st),
            spacing: { before: tw(before + (geom.padTop || 0)), after: tw(geom.padBottom || 0), line: tw(lh), lineRule: hasPicture ? D.LineRuleType.AT_LEAST : D.LineRuleType.EXACT },
            indent: { left: Math.max(-tw(ctx.left), tw(geom.left - ctx.left)), right: Math.max(0, tw((ctx.left + ctx.width) - geom.right)) }
        };
        // a flex or grid item is as wide as its text, not as wide as its column: it is not a measure
        var hst = el.parentElement ? css(el.parentElement) : null;
        if (hst && /flex/.test(hst.display) && !/column/.test(hst.flexDirection) && st.textAlign !== 'right' && st.textAlign !== 'center') props.indent.right = 0;
        if (/^H[1-6]$/.test(el.tagName)) props.keepNext = true;
        if (ctx.align === 'right') props.alignment = D.AlignmentType.RIGHT;
        return props;
    }

    // A field's result takes the paragraph style's formatting, not the run's, so the paragraph that
    // holds one gets a style of its own.
    function applyFieldStyle(props, kids) {
        var o = kids && kids.fieldRunOptions;
        if (!o) return;
        var key = [o.font, o.size, o.color, o.characterSpacing || 0].join('|');
        var id = ST.fieldStyles[key];
        if (!id) {
            id = 'NekoField' + (Object.keys(ST.fieldStyles).length + 1);
            ST.fieldStyles[key] = id;
            ST.paragraphStyles.push({ id: id, name: id, basedOn: 'Normal', run: { font: o.font, size: o.size, color: o.color, characterSpacing: o.characterSpacing } });
        }
        props.style = id;
    }

    // A block that holds only inline content, as one paragraph.
    async function textBlock(el, st, ctx, out) {
        var r = rectOf(el);
        var kids = await inlineChildren(Array.prototype.slice.call(el.childNodes), ctx);
        if (!kids.length && r.height < 2) { return; }
        var padT = px(st.paddingTop), padB = px(st.paddingBottom), padL = px(st.paddingLeft), padR = px(st.paddingRight);
        var b = bordersOf(st, el, { Top: padT * 0.75, Bottom: padB * 0.75, Left: padL * 0.75, Right: padR * 0.75 });
        var geom = {
            top: r.top, left: r.left + px(st.borderLeftWidth) + padL, right: r.right - px(st.borderRightWidth) - padR,
            padTop: b.top ? 0 : padT + px(st.borderTopWidth), padBottom: b.bottom ? 0 : padB + px(st.borderBottomWidth)
        };
        var props = paragraphProps(el, st, ctx, geom, kids.hasPicture);
        var cb = el.querySelector(':scope > input[type="checkbox"]');
        if (cb) {
            // the text starts where the browser put it; the box hangs in the margin
            var tn = Array.prototype.find.call(el.childNodes, function (n) { return n.nodeType === 3 && n.nodeValue.trim(); });
            if (tn) {
                var rg = ST.doc.createRange(); rg.selectNodeContents(tn);
                var textLeft = rg.getBoundingClientRect().left - ST.ox;
                props.indent = Object.assign({}, props.indent, { left: tw(textLeft - ctx.left), hanging: tw(textLeft - geom.left) });
            }
        }
        if (b.top || b.bottom) props.border = { top: b.top || undefined, bottom: b.bottom || undefined };
        takeMarker(ctx, props);
        applyFieldStyle(props, kids);
        props.children = kids.length ? kids : [new D.TextRun({ text: '', size: 2 })];
        out.push({ kind: 'p', opts: props, top: r.top, bottom: r.bottom });
        ctx.cursor = r.bottom;
    }

    // Loose text and inline elements between blocks form an anonymous paragraph.
    async function inlineGroup(nodes, parent, pst, ctx, out) {
        var kids = await inlineChildren(nodes, ctx);
        if (!kids.length) return;
        var range = ST.doc.createRange();
        range.setStartBefore(nodes[0]);
        range.setEndAfter(nodes[nodes.length - 1]);
        var rr = range.getBoundingClientRect();
        if (rr.width === 0 && rr.height === 0) return;
        var top = rr.top - ST.oy, bottom = rr.bottom - ST.oy;
        var pr = rectOf(parent);
        var props = paragraphProps(parent, pst, ctx, { top: top, left: pr.left + px(pst.borderLeftWidth) + px(pst.paddingLeft), right: pr.right - px(pst.borderRightWidth) - px(pst.paddingRight) }, kids.hasPicture);
        takeMarker(ctx, props);
        applyFieldStyle(props, kids);
        props.children = kids;
        out.push({ kind: 'p', opts: props, top: top, bottom: bottom });
        ctx.cursor = bottom;
    }

    function takeMarker(ctx, props) {
        if (ctx.marker && ctx.marker.pending) {
            var m = ctx.marker;
            m.pending = false;
            props.numbering = { reference: m.reference, level: 0 };
            props.indent = { left: tw(m.textLeft - ctx.left), hanging: tw(m.hanging) };
        }
    }

    // ---------------------------------------------------------------- flow

    function childIsBlock(node) {
        if (node.nodeType !== 1) return false;
        var st = css(node);
        return !isInlineLevel(st) && !!(BLOCKY[st.display] || st.display === 'contents' || st.display === 'table-cell');
    }

    function visibleBlock(el) {
        var st = css(el);
        if (SKIP_TAGS[el.tagName] || isHidden(el, st) || isFurniture(el)) return false;
        return true;
    }

    // Walks the children of a block in order: blocks one by one, loose inline
    // content as anonymous paragraphs.
    async function flow(parent, ctx, out) {
        var pst = css(parent);
        var group = [];
        async function flush() {
            if (!group.length) return;
            var nodes = group; group = [];
            // a group of nothing but collapsible space is not a paragraph
            if (nodes.every(function (n) { return n.nodeType === 3 && isBlankPiece(n.nodeValue); })) return;
            await inlineGroup(nodes, parent, pst, ctx, out);
        }
        for (var n = parent.firstChild; n; n = n.nextSibling) {
            if (n.nodeType === 3) { group.push(n); continue; }
            if (n.nodeType !== 1) continue;
            var st = css(n);
            if (SKIP_TAGS[n.tagName] || isHidden(n, st) || isFurniture(n)) continue;
            if (isInlineLevel(st) && !isMediaBlock(n, st)) { group.push(n); continue; }
            await flush();
            await block(n, ctx, out);
        }
        await flush();
    }

    function isMediaBlock(el, st) {
        var tag = el.tagName;
        if (tag === 'IMG' || tag === 'svg' || tag === 'SVG' || tag === 'CANVAS') return !isInlineLevel(st) || st.display === 'block';
        return false;
    }

    function hasBlockChildren(el) {
        for (var c = el.firstChild; c; c = c.nextSibling) {
            if (c.nodeType !== 1) continue;
            var st = css(c);
            if (SKIP_TAGS[c.tagName] || isHidden(c, st) || isFurniture(c)) continue;
            if (c.tagName === 'INPUT') continue;   // a grid item by style, but a checkbox is a character
            if (!isInlineLevel(st) || isMediaBlock(c, st) || (c.querySelector && hasMaskPicture(c))) return true;
        }
        return false;
    }

    function hasMaskPicture(el) { return false; }

    function innerContext(el, st, ctx) {
        var r = rectOf(el);
        var left = r.left + px(st.borderLeftWidth) + px(st.paddingLeft);
        var right = r.right - px(st.borderRightWidth) - px(st.paddingRight);
        return { left: left, width: right - left, cursor: ctx.cursor, marker: ctx.marker, ground: ctx.ground };
    }

    // The dispatcher for one block-level element.
    async function block(el, ctx, out) {
        var st = css(el), tag = el.tagName;
        var r = rectOf(el);
        if (r.width < 0.5 && r.height < 0.5 && !el.firstChild) return;

        if (tag === 'TABLE') return htmlTable(el, st, ctx, out);
        if (tag === 'PRE') return preBlock(el, st, ctx, out);
        if (tag === 'UL' || tag === 'OL') return list(el, st, ctx, out);
        if (tag === 'HR') return ruleBlock(el, st, ctx, out);
        if (isMediaBlock(el, st) || tag === 'canvas') return mediaBlock(el, st, ctx, out);
        var mask = st.maskImage && st.maskImage !== 'none' ? st.maskImage : (st.webkitMaskImage && st.webkitMaskImage !== 'none' ? st.webkitMaskImage : '');
        if (mask) return maskBlock(el, st, ctx, out);

        var kids = blockKids(el);
        if (kids.length >= 2 && isColumnar(el, st, kids)) return bandsBlock(el, st, kids, ctx, out);

        if (!hasBlockChildren(el)) {
            if (isBox(el, st, true)) return boxBlock(el, st, ctx, out);
            return textBlock(el, st, ctx, out);
        }
        if (isBox(el, st, false)) return boxBlock(el, st, ctx, out);

        // a plain container: its children, one after another
        var inner = innerContext(el, st, ctx);
        await flow(el, inner, out);
        ctx.cursor = inner.cursor;
    }

    // A box has a ground, or borders the paragraph model can't draw (a container's rule, side borders).
    function isBox(el, st, textOnly) {
        if (backgroundOf(el, st)) return true;
        var b = bordersOf(st, el);
        if (textOnly) return !!(b.left || b.right || (b.top && b.bottom && false));
        return hasBorders(b);
    }

    function blockKids(el) {
        var kids = [];
        for (var c = el.firstElementChild; c; c = c.nextElementSibling) {
            var st = css(c);
            if (SKIP_TAGS[c.tagName] || isHidden(c, st) || isFurniture(c)) continue;
            if (st.position === 'absolute' || st.position === 'fixed') continue;
            var r = c.getBoundingClientRect();
            if (r.width < 0.5 && r.height < 0.5) continue;
            kids.push(c);
        }
        return kids;
    }

    // ---------------------------------------------------------------- media

    async function mediaBlock(el, st, ctx, out) {
        var r = rectOf(el);
        var tag = el.tagName.toLowerCase();
        var run;
        if (tag === 'svg') {
            var data = await svgPng(el, r.width, r.height);
            run = new D.ImageRun({ type: 'png', data: data, transformation: { width: r.width, height: r.height }, altText: alt(altOf(el)) });
        } else if (tag === 'img') {
            var pic = await pictureFromUrl(el.currentSrc || el.src, r.width, r.height);
            run = new D.ImageRun({ type: pic.type, data: pic.data, transformation: { width: r.width, height: r.height }, altText: alt(altOf(el)) });
        } else {
            return;
        }
        pictureParagraph(run, r, ctx, out);
    }

    async function maskBlock(el, st, ctx, out) {
        var r = rectOf(el);
        if (r.width < 2 || r.height < 2) return;
        var data = await maskPng(el, st, r.width, r.height);
        var run = new D.ImageRun({ type: 'png', data: data, transformation: { width: r.width, height: r.height }, altText: alt(altOf(el)) });
        pictureParagraph(run, r, ctx, out);
    }

    function pictureParagraph(run, r, ctx, out) {
        var props = {
            spacing: { before: tw(Math.max(0, r.top - ctx.cursor)), after: 0, line: tw(r.height), lineRule: D.LineRuleType.AT_LEAST },
            indent: { left: Math.max(-tw(ctx.left), tw(r.left - ctx.left)) },
            children: [run]
        };
        takeMarker(ctx, props);
        out.push({ kind: 'p', opts: props, top: r.top, bottom: r.bottom });
        ctx.cursor = r.bottom;
    }

    function ruleBlock(el, st, ctx, out) {
        var r = rectOf(el);
        var c = parseColor(st.borderTopColor) || parseColor(st.backgroundColor) || { hex: 'CCCCCC' };
        var w = Math.max(1, px(st.borderTopWidth) || r.height);
        var props = {
            spacing: { before: tw(Math.max(0, r.top - ctx.cursor)), after: 0, line: 20, lineRule: D.LineRuleType.EXACT },
            indent: { left: Math.max(0, tw(r.left - ctx.left)), right: Math.max(0, tw(ctx.left + ctx.width - r.right)) },
            border: { bottom: { style: D.BorderStyle.SINGLE, size: Math.round(w * 6), color: c.hex, space: 0 } },
            children: [new D.TextRun({ text: '', size: 2 })]
        };
        out.push({ kind: 'p', opts: props, top: r.top, bottom: r.bottom });
        ctx.cursor = r.bottom;
    }

    // ---------------------------------------------------------------- code

    async function preBlock(el, st, ctx, out) {
        // a <pre> is a box whose lines keep their spaces
        if (isBox(el, st, false)) return boxBlock(el, st, ctx, out);
        return textBlock(el, st, ctx, out);
    }

    // ---------------------------------------------------------------- lists

    async function list(el, st, ctx, out) {
        var ordered = el.tagName === 'OL';
        var ref = null;
        for (var li = el.firstElementChild; li; li = li.nextElementSibling) {
            var lst = css(li);
            if (isHidden(li, lst)) continue;
            var type = lst.listStyleType;
            var lr = rectOf(li);
            var marker = null;
            if (li.tagName === 'LI' && type && type !== 'none') {
                if (!ref) ref = newListReference(ordered, type, el, lst);
                var fs = px(lst.fontSize);
                var textLeft = lr.left + px(lst.borderLeftWidth) + px(lst.paddingLeft);
                marker = { pending: true, reference: ref, textLeft: textLeft, hanging: Math.min(Math.max(textLeft - ctx.left, 8), fs * 1.6) };
            }
            var inner = { left: ctx.left, width: ctx.width, cursor: ctx.cursor, marker: marker, ground: ctx.ground };
            // the item is a block of its own: a box, a container of blocks, or a line of text
            await listItem(li, lst, inner, out);
            ctx.cursor = inner.cursor;
        }
    }

    async function listItem(li, st, ctx, out) {
        var kids = blockKids(li);
        if (kids.length >= 2 && isColumnar(li, st, kids)) return bandsBlock(li, st, kids, ctx, out);
        if (!hasBlockChildren(li)) {
            if (isBox(li, st, true)) return boxBlock(li, st, ctx, out);
            return textBlock(li, st, ctx, out);
        }
        if (isBox(li, st, false)) return boxBlock(li, st, ctx, out);
        var inner = innerContext(li, st, ctx);
        // the marker belongs to the first paragraph, wherever it lands
        inner.marker = ctx.marker;
        await flow(li, inner, out);
        ctx.cursor = inner.cursor;
    }

    function newListReference(ordered, type, el, lst) {
        var ref = (ordered ? 'ol-' : 'ul-') + (ST.numbering.length + 1);
        var format = D.LevelFormat.BULLET, text = '•';
        if (ordered) {
            format = type === 'lower-alpha' || type === 'lower-latin' ? D.LevelFormat.LOWER_LETTER
                : type === 'upper-alpha' || type === 'upper-latin' ? D.LevelFormat.UPPER_LETTER
                : type === 'lower-roman' ? D.LevelFormat.LOWER_ROMAN
                : type === 'upper-roman' ? D.LevelFormat.UPPER_ROMAN : D.LevelFormat.DECIMAL;
            text = '%1.';
        } else if (type === 'circle') text = '◦';
        else if (type === 'square') text = '▪';
        var start = ordered ? (parseInt(el.getAttribute('start'), 10) || 1) : undefined;
        ST.numbering.push({
            reference: ref,
            levels: [{ level: 0, format: format, text: text, alignment: D.AlignmentType.LEFT, start: start,
                style: { paragraph: { indent: { left: 360, hanging: 260 } }, run: markerRun(lst) } }]
        });
        return ref;
    }

    // The marker is set like the item's text: its size and typeface, and the colour of its ::marker.
    function markerRun(lst) {
        var font = runFont(lst.fontFamily, parseInt(lst.fontWeight, 10) || 400);
        var c = parseColor(lst.color) || { hex: '000000' };
        return { font: font.name, size: Math.max(2, Math.round(px(lst.fontSize) * 1.5)), color: c.hex };
    }

    // ---------------------------------------------------------------- tables

    function widthDxa(px_) { return { size: Math.max(0, tw(px_)), type: D.WidthType.DXA }; }

    function noBorders() {
        var none = { style: D.BorderStyle.NONE, size: 0, color: 'auto' };
        return { top: none, bottom: none, left: none, right: none, insideHorizontal: none, insideVertical: none };
    }

    function cellBorders(b) {
        var none = { style: D.BorderStyle.NONE, size: 0, color: 'auto' };
        function pick(x) { return x ? { style: x.style, size: x.size, color: x.color } : none; }
        return { top: pick(b.top), bottom: pick(b.bottom), left: pick(b.left), right: pick(b.right) };
    }

    // The blocks of a cell or the body: spacing between blocks that follow
    // a table has to be a spacer paragraph or the previous paragraph's
    // `after`, since Word has no space-before on a table.
    function materialize(items, startGap) {
        var res = [];
        var prev = null;
        items.forEach(function (it, i) {
            if (it.kind === 'p') {
                res.push(new D.Paragraph(it.opts));
            } else {
                var gap = it.gapBefore || 0;
                if (i === 0 && startGap && !gap) gap = startGap;
                if (prev && prev.kind === 'p' && gap > 0) {
                    // fold the gap into the paragraph before the table
                    var o = prev.opts;
                    o.spacing = Object.assign({}, o.spacing, { after: (o.spacing.after || 0) + tw(gap) });
                    res[res.length - 1] = new D.Paragraph(o);
                } else if (gap > 0.5 || (prev && prev.kind === 'table')) {
                    res.push(TINY(Math.max(1, tw(gap))));
                }
                res.push(it.table);
            }
            prev = it;
        });
        var last = items[items.length - 1];
        if (!last || last.kind === 'table') res.push(TINY(20));
        return res;
    }

    // Top and bottom padding as paragraph spacing inside a cell. A cell margin is the usual way, but a
    // nested table's top and bottom margins land outside its ground in LibreOffice; spacing is the same
    // in every reader.
    function padItems(items, padT, padB) {
        if (padT > 0.5) {
            var f = items[0];
            if (!f) items.unshift({ kind: 'p', opts: { spacing: { before: 0, after: 0, line: Math.max(20, tw(padT)), lineRule: D.LineRuleType.EXACT }, children: [new D.TextRun({ text: '', size: 2 })] }, top: 0, bottom: 0 });
            else if (f.kind === 'p') f.opts.spacing = Object.assign({}, f.opts.spacing, { before: (f.opts.spacing.before || 0) + tw(padT) });
            else f.gapBefore = (f.gapBefore || 0) + padT;
        }
        if (padB > 0.5) {
            var l = items[items.length - 1];
            if (!l || l.kind === 'table' || l === items[0] && !items.length) items.push({ kind: 'p', opts: { spacing: { before: 0, after: 0, line: Math.max(20, tw(padB)), lineRule: D.LineRuleType.EXACT }, children: [new D.TextRun({ text: '', size: 2 })] }, top: 0, bottom: 0 });
            else l.opts.spacing = Object.assign({}, l.opts.spacing, { after: (l.opts.spacing.after || 0) + tw(padB) });
        }
    }

    // Converts the children of `el` into the blocks of a cell whose content box starts at `top`.
    async function cellBlocks(el, st, ctx, top) {
        var items = [];
        var inner = { left: ctx.left, width: ctx.width, cursor: top, marker: null, ground: ctx.ground };
        await flowOrText(el, st, inner, items);
        return { items: items, blocks: materialize(items, 0) };
    }

    async function flowOrText(el, st, ctx, items) {
        if (!hasBlockChildren(el)) {
            // the cell's own text is one paragraph, laid out by the cell's style
            var r = rectOf(el);
            var kids = await inlineChildren(Array.prototype.slice.call(el.childNodes), ctx);
            if (!kids.length) return;
            var props = paragraphProps(el, st, ctx, { top: r.top + px(st.borderTopWidth) + px(st.paddingTop), left: r.left + px(st.borderLeftWidth) + px(st.paddingLeft), right: r.right - px(st.borderRightWidth) - px(st.paddingRight) }, kids.hasPicture);
            applyFieldStyle(props, kids);
            props.children = kids;
            items.push({ kind: 'p', opts: props, top: r.top, bottom: r.bottom });
            ctx.cursor = r.bottom;
            return;
        }
        await flow(el, ctx, items);
    }

    // A real <table>.
    async function htmlTable(el, st, ctx, out) {
        var tr = rectOf(el);
        var rowEls = Array.prototype.slice.call(el.rows);
        if (!rowEls.length) return;

        // the column edges, from every cell's left edge
        var edges = {};
        rowEls.forEach(function (row) {
            Array.prototype.forEach.call(row.cells, function (cell) {
                var cr = rectOf(cell);
                edges[Math.round(cr.left * 2) / 2] = 1; edges[Math.round(cr.right * 2) / 2] = 1;
            });
        });
        var xs = Object.keys(edges).map(Number).sort(function (a, b) { return a - b; });
        var merged = [];
        xs.forEach(function (x) { if (!merged.length || x - merged[merged.length - 1] > 1.5) merged.push(x); });
        var colWidths = [];
        for (var i = 0; i < merged.length - 1; i++) colWidths.push(merged[i + 1] - merged[i]);
        function colIndex(x) { var best = 0, bd = 1e9; merged.forEach(function (m, j) { var d = Math.abs(m - x); if (d < bd) { bd = d; best = j; } }); return best; }

        var rows = [];
        var spans = [];   // rowSpan bookkeeping is left to the cells' own rowSpan
        for (var ri = 0; ri < rowEls.length; ri++) {
            var row = rowEls[ri], rst = css(row), cells = [];
            var rr = rectOf(row);
            var isHead = row.parentElement && row.parentElement.tagName === 'THEAD';
            for (var ci = 0; ci < row.cells.length; ci++) {
                var cell = row.cells[ci], cst = css(cell), cr = rectOf(cell);
                var c0 = colIndex(cr.left), c1 = colIndex(cr.right);
                var span = Math.max(1, c1 - c0);
                var padT = px(cst.paddingTop), padB = px(cst.paddingBottom), padL = px(cst.paddingLeft), padR = px(cst.paddingRight);
                var b = bordersOf(cst, cell);
                var rowB = bordersOf(rst, row);
                var cellB = { top: b.top || rowB.top, bottom: b.bottom || rowB.bottom, left: b.left || rowB.left, right: b.right || rowB.right };
                var bg = backgroundOf(cell, cst) || backgroundOf(row, rst);
                var cctx = { left: cr.left + px(cst.borderLeftWidth) + padL, width: cr.width - px(cst.borderLeftWidth) - px(cst.borderRightWidth) - padL - padR, ground: bg || ctx.ground };
                var content = await cellBlocks(cell, cst, cctx, cr.top + px(cst.borderTopWidth) + padT);
                cells.push(new D.TableCell({
                    children: content.blocks,
                    width: widthDxa(cr.width),
                    columnSpan: span > 1 ? span : undefined,
                    rowSpan: cell.rowSpan > 1 ? cell.rowSpan : undefined,
                    shading: bg ? { type: D.ShadingType.CLEAR, fill: bg, color: 'auto' } : undefined,
                    borders: cellBorders(cellB),
                    margins: { top: tw(padT), bottom: tw(padB), left: tw(padL), right: tw(padR) },
                    verticalAlign: cst.verticalAlign === 'middle' ? D.VerticalAlign.CENTER : cst.verticalAlign === 'bottom' ? D.VerticalAlign.BOTTOM : D.VerticalAlign.TOP
                }));
            }
            rows.push(new D.TableRow({
                children: cells,
                height: { value: Math.max(20, tw(rr.height)), rule: D.HeightRule.ATLEAST },
                tableHeader: isHead,
                cantSplit: true
            }));
        }
        var table = new D.Table({
            rows: rows,
            width: widthDxa(tr.width),
            columnWidths: colWidths.map(tw),
            layout: D.TableLayoutType.FIXED,
            indent: { size: tw(tr.left - ctx.left), type: D.WidthType.DXA },
            borders: noBorders(),
            margins: { top: 0, bottom: 0, left: 0, right: 0 }
        });
        out.push({ kind: 'table', table: table, gapBefore: Math.max(0, tr.top - ctx.cursor), top: tr.top, bottom: tr.bottom });
        ctx.cursor = tr.bottom;
    }

    // A box: a one-cell table carrying the ground, the borders and the padding.
    async function boxBlock(el, st, ctx, out) {
        var r = rectOf(el);
        var padT = px(st.paddingTop), padB = px(st.paddingBottom), padL = px(st.paddingLeft), padR = px(st.paddingRight);
        var bt = px(st.borderTopWidth), bb = px(st.borderBottomWidth), bl = px(st.borderLeftWidth), br = px(st.borderRightWidth);
        var bg = backgroundOf(el, st);
        var b = bordersOf(st, el);
        var cctx = { left: r.left + bl + padL, width: r.width - bl - br - padL - padR, ground: bg || ctx.ground };
        var items = [];
        var inner = { left: cctx.left, width: cctx.width, cursor: r.top + bt + padT, marker: ctx.marker, ground: cctx.ground };
        await flowOrText(el, st, inner, items);
        padItems(items, padT, padB);
        var blocks = materialize(items, 0);
        var cell = new D.TableCell({
            children: blocks,
            width: widthDxa(r.width),
            shading: bg ? { type: D.ShadingType.CLEAR, fill: bg, color: 'auto' } : undefined,
            borders: cellBorders(b),
            margins: { top: 0, bottom: 0, left: tw(padL), right: tw(padR) },
            verticalAlign: D.VerticalAlign.TOP
        });
        var table = new D.Table({
            rows: [new D.TableRow({ children: [cell], height: { value: Math.max(20, tw(r.height)), rule: D.HeightRule.ATLEAST }, cantSplit: false })],
            width: widthDxa(r.width),
            columnWidths: [tw(r.width)],
            layout: D.TableLayoutType.FIXED,
            indent: { size: tw(r.left - ctx.left), type: D.WidthType.DXA },
            borders: noBorders(),
            margins: { top: 0, bottom: 0, left: 0, right: 0 }
        });
        out.push({ kind: 'table', table: table, gapBefore: Math.max(0, r.top - ctx.cursor), top: r.top, bottom: r.bottom });
        ctx.cursor = r.bottom;
    }

    // ---------------------------------------------------------------- bands

    // Side-by-side layout: a grid, a flex row, CSS columns.
    function isColumnar(el, st, kids) {
        var d = st.display;
        var multi = parseInt(st.columnCount, 10) > 1;
        if (!(/grid|flex/.test(d) || multi)) return false;
        if (/flex/.test(d) && /column/.test(st.flexDirection)) return false;
        var rects = kids.map(function (k) { return rectOf(k); });
        for (var i = 0; i < rects.length; i++) {
            for (var j = i + 1; j < rects.length; j++) {
                var a = rects[i], b = rects[j];
                var yo = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
                var xd = a.right <= b.left + 1 || b.right <= a.left + 1;
                if (yo > 1 && xd) return true;
            }
        }
        return false;
    }

    // Groups the children into bands (rows of the layout); each band has columns.
    function makeBands(kids) {
        var rects = kids.map(function (k) { return rectOf(k); });
        var parent = kids.map(function (_, i) { return i; });
        function find(i) { while (parent[i] !== i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
        for (var i = 0; i < kids.length; i++) {
            for (var j = i + 1; j < kids.length; j++) {
                var a = rects[i], b = rects[j];
                var yo = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
                var xd = a.right <= b.left + 1 || b.right <= a.left + 1;
                if (yo > 1 && xd) parent[find(i)] = find(j);
            }
        }
        var groups = {};
        kids.forEach(function (k, i) { var g = find(i); (groups[g] = groups[g] || []).push({ el: k, rect: rects[i] }); });
        var bands = Object.keys(groups).map(function (g) {
            var items = groups[g];
            var top = Math.min.apply(null, items.map(function (x) { return x.rect.top; }));
            var bottom = Math.max.apply(null, items.map(function (x) { return x.rect.bottom; }));
            // columns: x intervals that overlap are one column
            var sorted = items.slice().sort(function (a, b) { return a.rect.left - b.rect.left; });
            var cols = [];
            sorted.forEach(function (it) {
                var last = cols[cols.length - 1];
                if (last && it.rect.left < last.right - 1) { last.items.push(it); last.right = Math.max(last.right, it.rect.right); }
                else cols.push({ left: it.rect.left, right: it.rect.right, items: [it] });
            });
            cols.forEach(function (c) { c.items.sort(function (a, b) { return a.rect.top - b.rect.top; }); });
            return { top: top, bottom: bottom, cols: cols };
        });
        bands.sort(function (a, b) { return a.top - b.top; });
        return bands;
    }

    function sameColumns(a, b) {
        if (a.cols.length !== b.cols.length) return false;
        for (var i = 0; i < a.cols.length; i++) {
            if (Math.abs(a.cols[i].left - b.cols[i].left) > 1.5 || Math.abs(a.cols[i].right - b.cols[i].right) > 1.5) return false;
        }
        return true;
    }

    async function bandsBlock(el, st, kids, ctx, out) {
        var pr = rectOf(el);
        var bands = makeBands(kids);
        var padT = px(st.paddingTop), padB = px(st.paddingBottom), padL = px(st.paddingLeft), padR = px(st.paddingRight);
        var bt = px(st.borderTopWidth), bb = px(st.borderBottomWidth), bl = px(st.borderLeftWidth), br = px(st.borderRightWidth);
        var bg = backgroundOf(el, st);
        var pb = bordersOf(st, el);
        // padding alone is only a gap (the cursor carries it); it counts when the container also paints something
        var decor = !!(bg || hasBorders(pb));
        if (!decor) { padT = padB = padL = padR = 0; }

        var spaceBetween = /space-between|space-around|space-evenly/.test(st.justifyContent);

        // consecutive bands with the same columns are the rows of one table
        var tables = [];
        bands.forEach(function (band) {
            var last = tables[tables.length - 1];
            if (last && sameColumns(last.bands[0], band)) last.bands.push(band);
            else tables.push({ bands: [band] });
        });

        for (var ti = 0; ti < tables.length; ti++) {
            var tbl = tables[ti];
            var first = tbl.bands[0], lastBand = tbl.bands[tbl.bands.length - 1];
            var isFirstTable = ti === 0, isLastTable = ti === tables.length - 1;
            // the table spans the container's box when the container paints one, else just its columns
            var left = first.cols[0].left, right = first.cols[first.cols.length - 1].right;
            var leftExtra = 0, rightExtra = 0;
            if (decor && (bg || pb.left || pb.right)) { leftExtra = Math.max(0, left - pr.left); rightExtra = Math.max(0, pr.right - right); left = pr.left; right = pr.right; }

            // column grid: each column, and a spacer where there's a gap
            var grid = [];
            first.cols.forEach(function (c, i) {
                if (i > 0) {
                    var gap = c.left - first.cols[i - 1].right;
                    if (gap > 0.5) grid.push({ spacer: true, width: gap });
                }
                grid.push({ spacer: false, col: i, width: c.right - c.left });
            });
            // a column grows into the gap on the side its text is not aligned to, so a label set in
            // Word's slightly wider text still fits on one line
            for (var si = 1; si < grid.length - 1; si++) {
                if (!grid[si].spacer) continue;
                var prevCol = grid[si - 1], nextCol = grid[si + 1];
                var take = Math.min(grid[si].width * 0.45, 60);
                if (take < 2) continue;
                function alignOfCol(g) {
                    if (g.spacer) return 'left';
                    // in a space-between row the last item sits flush right whatever its own text-align says
                    if (spaceBetween && g.col === first.cols.length - 1 && first.cols.length > 1) return 'right';
                    return css(first.cols[g.col].items[0].el).textAlign;
                }
                var pa = alignOfCol(prevCol), na = alignOfCol(nextCol);
                if (pa !== 'right' && pa !== 'end' && pa !== 'center') { prevCol.width += take; grid[si].width -= take; }
                if (na === 'right' || na === 'end') { nextCol.width += take; grid[si].width -= take; }
            }
            if (leftExtra > 0.5) grid[0].width += 0, grid[0].padLeft = leftExtra;
            if (rightExtra > 0.5) grid[grid.length - 1].padRight = rightExtra;
            if (leftExtra > 0.5) { grid[0].width += leftExtra; }
            if (rightExtra > 0.5) { grid[grid.length - 1].width += rightExtra; }

            var rows = [];
            var prevBottom = null;
            for (var bi = 0; bi < tbl.bands.length; bi++) {
                var band = tbl.bands[bi];
                if (prevBottom !== null && band.top - prevBottom > 0.5) {
                    // a gap between two rows of the same table: an empty row
                    rows.push(new D.TableRow({
                        height: { value: Math.max(20, tw(band.top - prevBottom)), rule: D.HeightRule.EXACT },
                        children: grid.map(function (g) {
                            return new D.TableCell({ children: [TINY(20)], width: widthDxa(g.width), shading: bg ? { type: D.ShadingType.CLEAR, fill: bg, color: 'auto' } : undefined, borders: cellBorders({ left: bi >= 0 && pb.left ? pb.left : null, right: pb.right }) });
                        })
                    }));
                }
                var isFirstRow = (bi === 0), isLastRow = (bi === tbl.bands.length - 1);
                var cells = [];
                for (var gi = 0; gi < grid.length; gi++) {
                    var g = grid[gi];
                    var borders = { top: null, bottom: null, left: null, right: null };
                    if (isFirstRow && isFirstTable) borders.top = pb.top;
                    if (isLastRow && isLastTable) borders.bottom = pb.bottom;
                    if (gi === 0) borders.left = pb.left;
                    if (gi === grid.length - 1) borders.right = pb.right;
                    var cellOpts = {
                        width: widthDxa(g.width),
                        shading: bg ? { type: D.ShadingType.CLEAR, fill: bg, color: 'auto' } : undefined,
                        verticalAlign: D.VerticalAlign.TOP
                    };
                    var mt = isFirstRow && isFirstTable ? padT + bt : 0, mb = isLastRow && isLastTable ? padB + bb : 0;
                    var ml = (g.padLeft ? 0 : 0), mr = 0;
                    if (g.spacer) {
                        cellOpts.children = [TINY(20)];
                        cellOpts.margins = { top: tw(mt), bottom: tw(mb), left: 0, right: 0 };
                        cellOpts.borders = cellBorders(borders);
                        cells.push(new D.TableCell(cellOpts));
                        continue;
                    }
                    var col = band.cols[g.col];
                    // a column holding one box that fills the band is the cell itself
                    var single = col.items.length === 1 ? col.items[0] : null;
                    var direct = false, itemSt = null, itemBg = null, itemBorders = null;
                    if (single) {
                        itemSt = css(single.el);
                        itemBg = backgroundOf(single.el, itemSt);
                        itemBorders = bordersOf(itemSt, single.el);
                        var fills = Math.abs(single.rect.top - band.top) < 1.5 && Math.abs(single.rect.bottom - band.bottom) < 1.5 &&
                            Math.abs(single.rect.left - col.left) < 1.5 && Math.abs(single.rect.right - col.right) < 1.5;
                        direct = fills && !isSpecialBlock(single.el) && (itemBg || hasBorders(itemBorders) || px(itemSt.paddingTop) || px(itemSt.paddingLeft));
                    }
                    var cctx, items = [], contentTop;
                    if (direct) {
                        var ipT = px(itemSt.paddingTop), ipB = px(itemSt.paddingBottom), ipL = px(itemSt.paddingLeft), ipR = px(itemSt.paddingRight);
                        var ibT = px(itemSt.borderTopWidth), ibL = px(itemSt.borderLeftWidth), ibR = px(itemSt.borderRightWidth);
                        cctx = { left: single.rect.left + ibL + ipL, width: single.rect.width - ibL - ibR - ipL - ipR, ground: itemBg || (bg || ctx.ground) };
                        contentTop = single.rect.top + ibT + ipT;
                        var inner = { left: cctx.left, width: cctx.width, cursor: contentTop, marker: null, ground: cctx.ground };
                        var sKids = blockKids(single.el);
                        if (sKids.length >= 2 && isColumnar(single.el, itemSt, sKids)) await bandsBlock(single.el, itemSt, sKids, inner, items);
                        else await flowOrText(single.el, itemSt, inner, items);
                        if (itemBg) cellOpts.shading = { type: D.ShadingType.CLEAR, fill: itemBg, color: 'auto' };
                        var ib = itemBorders;
                        borders = { top: ib.top || borders.top, bottom: ib.bottom || borders.bottom, left: ib.left || borders.left, right: ib.right || borders.right };
                        padItems(items, ipT + mt, ipB + mb);
                        cellOpts.margins = { top: 0, bottom: 0, left: tw(ipL + (g.padLeft || 0)), right: tw(ipR + (g.padRight || 0)) };
                        var vb = itemSt.alignSelf === 'end' || itemSt.alignSelf === 'flex-end';
                        void vb;
                    } else {
                        cctx = { left: col.left, width: col.right - col.left, ground: bg || ctx.ground };
                        contentTop = band.top;
                        var inner2 = { left: cctx.left, width: cctx.width, cursor: contentTop, marker: null, ground: cctx.ground };
                        if (spaceBetween && g.col === band.cols.length - 1 && band.cols.length > 1) inner2.align = 'right';
                        for (var ii = 0; ii < col.items.length; ii++) await block(col.items[ii].el, inner2, items);
                        cellOpts.margins = { top: tw(mt), bottom: tw(mb), left: tw(g.padLeft || 0), right: tw(g.padRight || 0) };
                        // a column whose boxes sit low in the band is bottom-aligned
                        var firstItem = col.items[0];
                        var lastItem = col.items[col.items.length - 1];
                        var pst = css(el);
                        var ai = pst.alignItems;
                        if (col.items.length === 1 && (ai === 'center' || ai === 'flex-end' || ai === 'end' || ai === 'baseline' && false)) {
                            cellOpts.verticalAlign = ai === 'center' ? D.VerticalAlign.CENTER : D.VerticalAlign.BOTTOM;
                        }
                        void firstItem; void lastItem;
                    }
                    cellOpts.children = materialize(items, 0);
                    cellOpts.borders = cellBorders(borders);
                    cells.push(new D.TableCell(cellOpts));
                }
                rows.push(new D.TableRow({
                    children: cells,
                    height: { value: Math.max(20, tw(band.bottom - band.top + (isFirstRow && isFirstTable ? padT + bt : 0) + (isLastRow && isLastTable ? padB + bb : 0))), rule: D.HeightRule.ATLEAST },
                    cantSplit: true
                }));
                prevBottom = band.bottom;
            }

            var table = new D.Table({
                rows: rows,
                width: widthDxa(right - left),
                columnWidths: grid.map(function (g) { return tw(g.width); }),
                layout: D.TableLayoutType.FIXED,
                indent: { size: tw(left - ctx.left), type: D.WidthType.DXA },
                borders: noBorders(),
                margins: { top: 0, bottom: 0, left: 0, right: 0 }
            });
            var tableTop = Math.min(first.top - (isFirstTable ? padT + bt : 0), first.top);
            out.push({ kind: 'table', table: table, gapBefore: Math.max(0, (isFirstTable ? pr.top : first.top) - ctx.cursor), top: tableTop, bottom: lastBand.bottom });
            ctx.cursor = isLastTable ? Math.max(lastBand.bottom, pr.bottom) : lastBand.bottom;
        }
    }

    // Blocks the band code lets the normal dispatcher handle even when they paint a ground.
    function isSpecialBlock(el) {
        var t = el.tagName;
        return t === 'TABLE' || t === 'UL' || t === 'OL' || t === 'PRE' || t === 'IMG' || t === 'svg';
    }

    // ---------------------------------------------------------------- a sheet

    function fieldRun(el, st) {
        var o = textRunOptions(el, st);
        o.children = [D.PageNumber.CURRENT];
        return new D.TextRun(o);
    }

    // The running head / the foot of a sheet, as the blocks of a Word header or footer.
    async function furnitureBlocks(part, ctx) {
        var items = [];
        var st = css(part);
        // page-number fields stand in for the printed number
        var fields = Array.prototype.slice.call(part.querySelectorAll('[data-doc-field="page"]'));
        fields.forEach(function (f) { f.setAttribute('data-doc-live', '1'); });
        await block(part, ctx, items);
        return materialize(items, 0);
    }

    async function exportSheet(sheet, index) {
        var win = ST.win;
        var sst = css(sheet);
        var sr = sheet.getBoundingClientRect();
        ST.ox = sr.left; ST.oy = sr.top;
        var pageW = ST.page.w, pageH = ST.page.h;
        var sheetH = Math.max(sr.height, pageH);
        ST.sheetGround = (parseColor(sst.backgroundColor) || { hex: 'ffffff' }).hex;

        var body = sheet.querySelector('.doc-body');
        var head = sheet.querySelector('[data-doc-part="head"]');
        var foot = sheet.querySelector('[data-doc-part="foot"]');
        var br = body ? rectOf(body) : { left: 0, top: 0, right: pageW, bottom: pageH, width: pageW, height: pageH };
        var bst = body ? css(body) : null;
        var marginLeft = br.left, marginRight = pageW - br.right;

        // ---- behind the text: the ground, then the art and panels, anchored in the header
        var floaters = [];
        var z = 1;
        function floater(data, type, r) {
            floaters.push(new D.ImageRun({
                type: type || 'png', data: data, transformation: { width: Math.max(1, r.width), height: Math.max(1, r.height) },
                floating: {
                    horizontalPosition: { relative: D.HorizontalPositionRelativeFrom.PAGE, offset: Math.round(r.left * EMU) },
                    verticalPosition: { relative: D.VerticalPositionRelativeFrom.PAGE, offset: Math.round(r.top * EMU) },
                    behindDocument: true, allowOverlap: true, lockAnchor: true, layoutInCell: false,
                    wrap: { type: D.TextWrappingType.NONE }, zIndex: z++
                },
                altText: alt('Background')
            }));
        }
        if (ST.sheetGround.toLowerCase() !== 'ffffff') {
            floater(await solidPng(ST.sheetGround), 'png', { left: 0, top: 0, width: pageW, height: Math.min(sheetH, pageH) });
        }
        var bgEls = Array.prototype.slice.call(sheet.querySelectorAll('[data-doc-bg]'));
        for (var i = 0; i < bgEls.length; i++) {
            var be = bgEls[i], bst2 = css(be);
            if (isHidden(be, bst2)) continue;
            var rr = rectOf(be);
            var bgc = parseColor(bst2.backgroundColor);
            if (bgc && bgc.a > 0.05) floater(await solidPng(over(bgc, ST.sheetGround, opacityOf(be))), 'png', rr);
            var svg = be.querySelector('svg');
            if (svg) {
                var srr = rectOf(svg);
                floater(await svgPng(svg, srr.width, srr.height, 2), 'png', srr);
            }
        }

        // ---- header and footer
        var hctx = { left: marginLeft, width: pageW - marginLeft - marginRight, cursor: head ? rectOf(head).top : 0, marker: null, ground: ST.sheetGround };
        var headBlocks = head ? await furnitureBlocks(head, hctx) : [];
        var headerChildren = [];
        var anchor = new D.Paragraph({
            spacing: { before: 0, after: 0, line: 20, lineRule: D.LineRuleType.EXACT },
            children: floaters.concat([new D.TextRun({ text: '', size: 2 })])
        });
        headerChildren.push(anchor);
        headBlocks.forEach(function (b) { headerChildren.push(b); });

        var footBlocks = [];
        var fctx = { left: marginLeft, width: pageW - marginLeft - marginRight, cursor: foot ? rectOf(foot).top : 0, marker: null, ground: ST.sheetGround };
        if (foot) {
            // the printed page number becomes a field: swap the text for it
            var fields = Array.prototype.slice.call(foot.querySelectorAll('[data-doc-field="page"]'));
            ST.pageField = fields.length ? fields[0].getAttribute('data-doc-format') || 'plain' : null;
            footBlocks = await furnitureBlocks(foot, fctx);
        }

        // ---- the body
        var items = [];
        var topOfBody = body ? br.top + px(bst.paddingTop) : 0;
        var bctx = { left: marginLeft, width: pageW - marginLeft - marginRight, cursor: topOfBody, marker: null, ground: ST.sheetGround };
        if (body) {
            // the body itself may be a grid (the cover, a sidebar): bands, like any container
            var bodyKids = blockKids(body);
            if (bodyKids.length >= 2 && isColumnar(body, bst, bodyKids)) await bandsBlock(body, bst, bodyKids, bctx, items);
            else await flow(body, bctx, items);
        }
        var headBottom = head ? rectOf(head).bottom : 0;
        // Word drops space-before at the top of a page, so the page margin carries the first block's gap
        var marginTop = Math.max(items.length ? items[0].top : topOfBody, headBottom);
        // the first block's gap is measured from the margin
        if (items.length && items[0].kind === 'p') {
            var s0 = items[0].opts.spacing;
            var firstGap = Math.max(0, items[0].top - marginTop);
            items[0].opts.spacing = Object.assign({}, s0, { before: tw(firstGap) + 0 });
        } else if (items.length && items[0].kind === 'table') {
            items[0].gapBefore = Math.max(0, items[0].top - marginTop);
        }
        var bodyBlocks = materialize(items, 0);
        if (!bodyBlocks.length) bodyBlocks = [TINY(20)];

        // the foot is measured from the bottom of the sheet: a sheet that grew past one page
        // keeps its foot at the bottom of its last page, as the footer of every Word page
        var footTop = foot ? rectOf(foot).top : sr.height - 40;
        var footBottom = foot ? rectOf(foot).bottom : sr.height - 20;
        var headTop = head ? rectOf(head).top : 0;
        var pageNumber = ST.pageFormat === 'zero' ? D.NumberFormat.DECIMAL_ZERO : D.NumberFormat.DECIMAL;

        return {
            properties: {
                type: D.SectionType.NEXT_PAGE,
                page: {
                    size: { width: ST.page.tw, height: ST.page.th },
                    margin: {
                        top: tw(marginTop), bottom: tw(Math.max(0, sr.height - footTop)), left: tw(marginLeft), right: tw(marginRight),
                        header: tw(Math.max(0, headTop)), footer: tw(Math.max(0, sr.height - footBottom)), gutter: 0
                    },
                    pageNumbers: { formatType: pageNumber }
                }
            },
            headers: { default: new D.Header({ children: headerChildren }) },
            footers: { default: new D.Footer({ children: footBlocks.length ? footBlocks : [TINY(20)] }) },
            children: bodyBlocks
        };
    }

    // ---------------------------------------------------------------- driver

    function saveBlob(blob, fileName) {
        if (typeof window.nekoDocSave === 'function') return Promise.resolve(window.nekoDocSave(fileName, blob));
        var url = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () { URL.revokeObjectURL(url); a.remove(); }, 1000);
        return Promise.resolve(fileName);
    }

    async function exportDocx(options) {
        options = options || {};
        D = window.docx;
        if (!D || typeof D.Document !== 'function') throw new Error('docx is not loaded');

        var html = document.documentElement;
        var letter = html.getAttribute('data-doc-size') === 'letter';
        ST = {
            page: letter ? LETTER : A4,
            fontsBase: options.fontsBase || '',
            usedFonts: {},
            numbering: [], fieldStyles: {}, paragraphStyles: [],
            ox: 0, oy: 0, sheetGround: 'ffffff', pageField: null,
            win: null, doc: null
        };
        fontCache = {}; imageCache = {}; solidCache = {}; fontFaceCss = {};

        await loadFontCatalog(ST.fontsBase);
        var frame = await buildStage();
        try {
            ST.win = frame.contentWindow;
            ST.doc = frame.contentDocument;
            var sheets = Array.prototype.slice.call(ST.doc.querySelectorAll('.doc-page'));
            // one numbering style for the whole file: a cover or back page that prints text in place of a
            // number still counts in the same style
            ST.pageFormat = ST.doc.querySelector('[data-doc-field="page"][data-doc-format="zero"]') ? 'zero' : 'plain';
            var sections = [];
            for (var i = 0; i < sheets.length; i++) sections.push(await exportSheet(sheets[i], i));

            // the document's own typeface: the body text's
            var bodyFont = ST.usedFonts[Object.keys(ST.usedFonts)[0]] ? Object.keys(ST.usedFonts)[0] : 'Arial';
            var fontList = [];
            var names = Object.keys(ST.usedFonts);
            for (var f = 0; f < names.length; f++) {
                try { fontList.push({ name: names[f], data: await fontBytes(ST.usedFonts[names[f]]) }); } catch (e) { /* the file stays unembedded */ }
            }

            var doc = new D.Document({
                creator: options.author || options.company || 'Neko',
                title: options.title || '',
                description: options.description || '',
                fonts: fontList,
                numbering: { config: ST.numbering.length ? ST.numbering : [{ reference: 'none', levels: [{ level: 0, format: D.LevelFormat.BULLET, text: '•', alignment: D.AlignmentType.LEFT }] }] },
                styles: {
                    default: {
                        // the empty paragraph that closes a section gets this default line, so it is one point tall whatever the size
                        document: { run: { font: bodyFont, size: 22 }, paragraph: { spacing: { before: 0, after: 0, line: 20, lineRule: D.LineRuleType.EXACT } } },
                        hyperlink: { run: { color: '0563C1', underline: { type: D.UnderlineType.SINGLE } } }
                    },
                    paragraphStyles: ST.paragraphStyles
                },
                sections: sections
            });
            var blob = await D.Packer.toBlob(doc);
            var fileName = (options.fileName || slugify(options.title)).replace(/\.docx$/i, '') + '.docx';
            return saveBlob(blob, fileName);
        } finally {
            if (frame.parentNode) frame.parentNode.removeChild(frame);
        }
    }

    window.nekoDocExportDocx = exportDocx;
    window.nekoDocBuildDocx = function (options) {
        // test hook: the same export, handing back the Blob instead of saving it
        var saver = window.nekoDocSave;
        return new Promise(function (resolve, reject) {
            window.nekoDocSave = function (name, blob) { window.nekoDocSave = saver; resolve({ name: name, blob: blob }); };
            exportDocx(options).catch(function (e) { window.nekoDocSave = saver; reject(e); });
        });
    };
})();
