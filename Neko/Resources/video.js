/* ==========================================================================
   Neko video mode — the runtime.

   A video page is a timed sequence of scenes. Neko works out every time at build
   time (VideoTimeline.cs) and writes it into the markup: each scene carries
   data-start / data-duration / data-transition / data-tdur, and every element that
   moves carries data-v (its motion), data-at and data-dur (scene-local seconds).

   This file turns those numbers into a picture. It has one job — seek(t) — and it
   is a pure function of t: no timers, no CSS animations, no clock. The preview
   player calls it from requestAnimationFrame; `neko render` calls it once per
   frame from headless Chromium and screenshots the result. The same t always
   paints the same frame, which is what makes a render deterministic.

   Page protocol (what VideoRenderer.cs relies on):
     window.nekoVideo.ready      Promise, resolved once fonts, images and clips are in
     window.nekoVideo.isReady    true once ready has resolved
     window.nekoVideo.duration   seconds
     window.nekoVideo.fps / width / height
     window.nekoVideo.seek(t)    paints time t; returns a Promise (clips seeking)
     window.nekoVideo.renderMode() strips the player chrome and sets the canvas at 1:1

   The preview also carries the voice-over (VideoNarration.cs): its sentences play from
   their synthesized takes, show as a subtitle under the canvas, and sit in the timing
   panel (T, or #timing) beside every scene and animation on the video's clock.
   ========================================================================== */
(function () {
  'use strict';

  // ---------------------------------------------------------------- easing

  function powIn(k) { return function (t) { return Math.pow(t, k); }; }
  function powOut(k) { return function (t) { return 1 - Math.pow(1 - t, k); }; }
  function powInOut(k) { return function (t) { return t < 0.5 ? Math.pow(2 * t, k) / 2 : 1 - Math.pow(2 - 2 * t, k) / 2; }; }
  function backOut(s) { return function (t) { var u = t - 1; return 1 + (s + 1) * u * u * u + s * u * u; }; }

  var EASES = {
    'linear': function (t) { return t; }, 'none': function (t) { return t; },
    'power1.in': powIn(2), 'power1.out': powOut(2), 'power1.inOut': powInOut(2),
    'power2.in': powIn(3), 'power2.out': powOut(3), 'power2.inOut': powInOut(3),
    'power3.in': powIn(4), 'power3.out': powOut(4), 'power3.inOut': powInOut(4),
    'power4.in': powIn(5), 'power4.out': powOut(5), 'power4.inOut': powInOut(5),
    'expo.out': function (t) { return t >= 1 ? 1 : 1 - Math.pow(2, -10 * t); },
    'expo.in': function (t) { return t <= 0 ? 0 : Math.pow(2, 10 * t - 10); },
    'sine.in': function (t) { return 1 - Math.cos(t * Math.PI / 2); },
    'sine.out': function (t) { return Math.sin(t * Math.PI / 2); },
    'sine.inOut': function (t) { return -(Math.cos(Math.PI * t) - 1) / 2; }
  };

  function ease(name, fallback) {
    if (!name) return EASES[fallback] || EASES.linear;
    if (EASES[name]) return EASES[name];
    var back = /^back\.out(?:\(([\d.]+)\))?$/.exec(name);
    if (back) return backOut(back[1] ? parseFloat(back[1]) : 1.7);
    return EASES[fallback] || EASES.linear;
  }

  function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
  function num(el, key, fallback) { var v = parseFloat(el.getAttribute(key)); return isNaN(v) ? fallback : v; }

  // Deterministic noise for the scramble glyphs: the same (i, frame) always picks the same glyph.
  function hash(a, b) { var h = (a * 374761393 + b * 668265263) | 0; h = (h ^ (h >>> 13)) * 1274126177; return ((h ^ (h >>> 16)) >>> 0) / 4294967296; }

  // ---------------------------------------------------------------- motions
  //
  // A state motion fills a little state record for progress p (already eased) and
  // raw progress r (linear), and the record is written as opacity / transform /
  // filter / clip-path. The other motions (type, count, swap, …) paint themselves.

  // The curiosity theme moves the way the brand's website does (body[data-motion="brand"]):
  // no blur anywhere (BRAND.md §12), headlines rising word by word out of a mask
  // (js/motion.js: translateY 112% → 0, 0.9 s, cubic-bezier(.19,1,.22,1)), numbers
  // counting up on an ease-out cubic with no pulse.
  var brand = false;

  var STATE = {
    fade: { ease: 'power2.out', fn: function (s, p) { s.o = p; } },
    rise: { ease: 'power3.out', fn: function (s, p) { s.o = p; s.y = (1 - p) * 40; s.b = (1 - p) * 6; } },
    up: { ease: 'power3.out', fn: function (s, p) { s.o = p; s.y = (1 - p) * 40; } },
    drop: { ease: 'power3.out', fn: function (s, p) { s.o = p; s.y = -(1 - p) * 40; } },
    left: { ease: 'power4.out', fn: function (s, p) { s.o = p; s.x = -(1 - p) * 90; } },
    right: { ease: 'power4.out', fn: function (s, p) { s.o = p; s.x = (1 - p) * 90; } },
    pop: { ease: 'back.out(1.6)', fn: function (s, p, r) { s.o = clamp01(r * 2.5); s.s = 0.85 + 0.15 * p; } },
    zoom: { ease: 'power3.out', fn: function (s, p, r) { s.o = clamp01(r * 1.8); s.s = 0.96 + 0.04 * p; } },
    settle: { ease: 'power3.out', fn: function (s, p, r) { s.o = clamp01(r * 1.5); s.y = (1 - p) * 48; s.s = 0.965 + 0.035 * p; } },
    blur: { ease: 'power2.out', fn: function (s, p) { s.o = p; s.b = (1 - p) * 14; } },
    slam: { ease: 'power4.out', fn: function (s, p, r) { s.o = clamp01(r * 4); s.s = 1.6 - 0.6 * p; } },
    wipe: { ease: 'power3.inOut', fn: function (s, p) { s.clip = 'inset(0 ' + ((1 - p) * 100).toFixed(3) + '% 0 0)'; } },
    'wipe-up': { ease: 'power3.inOut', fn: function (s, p) { s.clip = 'inset(' + ((1 - p) * 100).toFixed(3) + '% 0 0 0)'; } },
    grow: { ease: 'power3.out', fn: function (s, p) { s.sy = p; } },
    'grow-x': { ease: 'power3.out', fn: function (s, p) { s.sx = p; } },
    word: { ease: 'power3.out', fn: function (s, p, r) {
      if (brand) { s.ypct = (1 - p) * 112; s.o = r > 0 ? 1 : 0; return; }
      s.o = p; s.yem = (1 - p) * 0.45; s.b = (1 - p) * 6;
    } },
    escape: { ease: 'back.out(1.7)', fn: function (s, p, r) { s.o = r > 0 ? 1 : 0; s.x = -24 * (1 - p); s.y = 24 * (1 - p); } },
    letter: { ease: 'power3.out', fn: function (s, p) { s.o = p; s.yem = (1 - p) * 0.6; s.b = (1 - p) * 4; } },
    cap: { ease: 'power3.out', fn: function (s, p) { s.o = p; s.y = (1 - p) * 16; } },
    check: { ease: 'power2.out', fn: function (s, p) { s.o = p; } },
    none: { ease: 'linear', fn: function () { } }
  };

  var EXITS = {
    fade: function (s, q) { s.o *= 1 - q; },
    up: function (s, q) { s.o *= 1 - q; s.y -= 30 * q; },
    down: function (s, q) { s.o *= 1 - q; s.y += 30 * q; },
    left: function (s, q) { s.o *= 1 - q; s.x -= 60 * q; },
    right: function (s, q) { s.o *= 1 - q; s.x += 60 * q; },
    blur: function (s, q) { s.o *= 1 - q; s.b += 10 * q; },
    shrink: function (s, q) { s.o *= 1 - q; s.s *= 1 - 0.1 * q; }
  };

  function freshState() { return { o: 1, x: 0, y: 0, yem: 0, ypct: 0, s: 1, sx: 1, sy: 1, b: 0, clip: null }; }

  function writeState(el, s) {
    var st = el.style;
    st.opacity = s.o >= 0.999 ? '' : s.o.toFixed(4);
    var t = '';
    if (s.x || s.y || s.yem) t += 'translate(' + s.x.toFixed(2) + 'px,' + (s.yem ? 'calc(' + s.y.toFixed(2) + 'px + ' + s.yem.toFixed(4) + 'em)' : s.y.toFixed(2) + 'px') + ') ';
    if (s.ypct) t += 'translateY(' + s.ypct.toFixed(3) + '%) ';
    if (s.s !== 1) t += 'scale(' + s.s.toFixed(4) + ') ';
    if (s.sx !== 1) t += 'scaleX(' + Math.max(0.0001, s.sx).toFixed(4) + ') ';
    if (s.sy !== 1) t += 'scaleY(' + Math.max(0.0001, s.sy).toFixed(4) + ') ';
    st.transform = t;
    st.filter = s.b > 0.05 && !brand ? 'blur(' + s.b.toFixed(2) + 'px)' : '';
    st.clipPath = s.clip || '';
  }

  // ---------------------------------------------------------------- model

  var model = null;
  var current = 0;

  function readTimeline() {
    var node = document.getElementById('nv-timeline');
    try { return node ? JSON.parse(node.textContent) : {}; } catch (e) { return {}; }
  }

  // Code is highlighted first (highlight.js), then every visible character becomes
  // a span the typing motion can reveal; whitespace stays text, so indentation and
  // line breaks are there from the start and the block never reflows.
  function splitCharacters(root) {
    var walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, null);
    var nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    nodes.forEach(function (node) {
      var text = node.nodeValue;
      if (!text || !/\S/.test(text)) return;
      var frag = document.createDocumentFragment();
      var run = '';
      for (var i = 0; i < text.length; i++) {
        var ch = text[i];
        if (/\s/.test(ch)) { run += ch; continue; }
        if (run) { frag.appendChild(document.createTextNode(run)); run = ''; }
        var span = document.createElement('span');
        span.className = 'nv-c';
        span.textContent = ch;
        frag.appendChild(span);
      }
      if (run) frag.appendChild(document.createTextNode(run));
      node.parentNode.replaceChild(frag, node);
    });
  }

  function prepareCode(stage) {
    stage.querySelectorAll('.nv-editor code').forEach(function (code) {
      if (window.hljs && !code.classList.contains('nohighlight') && !code.classList.contains('hljs')) {
        try { window.hljs.highlightElement(code); } catch (e) { /* unknown language: leave it plain */ }
      }
    });
    stage.querySelectorAll('[data-v="code"]').forEach(function (pre) { splitCharacters(pre); });
  }

  function buildModel() {
    var stage = document.getElementById('nv-stage');
    if (!stage) return null;
    var timeline = readTimeline();
    prepareCode(stage);

    var scenes = [];
    stage.querySelectorAll('.nv-scene').forEach(function (el) {
      var scene = {
        el: el,
        cam: el.querySelector(':scope > .nv-cam'),
        camera: null,
        start: num(el, 'data-start', 0),
        duration: num(el, 'data-duration', 3),
        transition: el.getAttribute('data-transition') || 'cut',
        tdur: num(el, 'data-tdur', 0),
        items: [],
        media: []
      };
      if (scene.cam) scene.camera = scene.cam.getAttribute('data-camera');
      el.querySelectorAll('[data-v]').forEach(function (node) { scene.items.push(makeItem(node)); });
      el.querySelectorAll('video').forEach(function (v) {
        var host = v.closest('[data-v]');
        scene.media.push({ el: v, at: host ? num(host, 'data-at', 0) : 0, offset: num(v, 'data-media-start', 0) });
      });
      scenes.push(scene);
    });

    var last = scenes[scenes.length - 1];
    var duration = timeline.duration || (last ? last.start + last.duration : 0);

    return {
      stage: stage,
      wash: stage.querySelector('.nv-wash'),
      scenes: scenes,
      duration: duration,
      fps: timeline.fps || 30,
      width: timeline.width || stage.offsetWidth,
      height: timeline.height || stage.offsetHeight,
      timeline: timeline,
      narration: timeline.narration || [],
      audio: Array.prototype.slice.call(document.querySelectorAll('audio.nv-audio'))
    };
  }

  function makeItem(el) {
    var v = (el.getAttribute('data-v') || 'none').toLowerCase();
    var item = {
      el: el,
      v: v,
      at: num(el, 'data-at', 0),
      dur: Math.max(0.0001, num(el, 'data-dur', 0.6)),
      ease: el.getAttribute('data-ease'),
      exit: el.getAttribute('data-exit'),
      until: el.hasAttribute('data-until') ? num(el, 'data-until', 0) : null
    };
    if (item.until !== null && !item.exit) item.exit = 'fade';
    if (v === 'type' || v === 'code') item.chars = Array.prototype.slice.call(el.querySelectorAll('.nv-c'));
    if (v === 'count') {
      item.from = num(el, 'data-from', 0);
      item.to = num(el, 'data-to', 0);
      item.decimals = num(el, 'data-decimals', 0);
      item.group = el.getAttribute('data-group') === 'true';
    }
    if (v === 'swap') {
      item.words = Array.prototype.slice.call(el.querySelectorAll('.nv-swap-word'));
      item.hold = num(el, 'data-hold', 0.9);
    }
    if (v === 'scramble') { item.text = el.textContent; item.html = el.innerHTML; }
    if (v === 'cursor') {
      item.points = (el.getAttribute('data-path') || '').trim().split(/\s+/).map(function (pair) {
        var xy = pair.split(','); return { x: parseFloat(xy[0]) / 100, y: parseFloat(xy[1]) / 100 };
      }).filter(function (p) { return !isNaN(p.x) && !isNaN(p.y); });
      item.click = el.getAttribute('data-click') !== 'false';
      item.ripple = el.querySelector('.nv-ripple');
    }
    if (v === 'paint') {
      try { item.opts = JSON.parse(el.getAttribute('data-o') || '{}'); } catch (e) { item.opts = {}; }
      item.painter = el.getAttribute('data-paint');
    }
    if (v === 'glyph') {
      item.cells = Array.prototype.slice.call(el.querySelectorAll('.nv-gc'));
      item.label = el.parentElement ? el.parentElement.querySelector('.nv-glyph-label') : null;
      item.loop = el.getAttribute('data-loop') === 'true';
      item.rest = num(el, 'data-rest', 1.7);
      item.frames = (el.getAttribute('data-frames') || '').split(' ').filter(Boolean).map(function (f) {
        var parts = f.split(':');
        return { cells: parts[0], ms: parseFloat(parts[1]) || 190, label: parts[2] ? parts[2].replace(/_/g, ' ') : null };
      });
      item.total = item.frames.reduce(function (a, f) { return a + f.ms; }, 0) / 1000;
    }
    if (v === 'sudo') {
      item.frames = Array.prototype.slice.call(el.querySelectorAll('.nv-sudo-f'));
      item.ms = num(el, 'data-ms', 130) / 1000;
      item.loop = el.getAttribute('data-loop') !== 'false';
      item.from = num(el, 'data-from', NaN);
      item.to = num(el, 'data-to', NaN);
    }
    if (v === 'art') {
      item.sig = el.querySelector('.art-sig');
      item.ox = el.getAttribute('data-ox') || '50';
      item.oy = el.getAttribute('data-oy') || '50';
    }
    return item;
  }

  // ---------------------------------------------------------------- painting items

  function formatNumber(value, decimals, group) {
    var fixed = value.toFixed(decimals);
    if (!group) return fixed;
    var parts = fixed.split('.');
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    return parts.join('.');
  }

  var SCRAMBLE = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%&*+=/<>';

  function paintItem(item, lt, scene) {
    var el = item.el;
    var r = (lt - item.at) / item.dur;
    var raw = clamp01(r);
    var spec = STATE[item.v];

    if (spec) {
      var p = ease(item.ease, brand && item.v === 'word' ? 'expo.out' : spec.ease)(raw);
      var s = freshState();
      spec.fn(s, p, raw);
      if (item.v === 'slam' && r > 1) {
        // The landing shake: two quick decaying swings.
        var k = (lt - item.at - item.dur) / 0.2;
        if (k < 1) s.x += Math.sin(k * Math.PI * 4) * 6 * (1 - k);
      }
      applyExit(item, lt, s);
      writeState(el, s);
      el.style.setProperty('--p', p.toFixed(4));
      return;
    }

    switch (item.v) {
      case 'draw': {
        var dp = ease(item.ease, 'power2.out')(raw);
        el.style.strokeDasharray = '1 1';
        el.style.strokeDashoffset = (1 - dp).toFixed(4);
        el.style.opacity = r <= 0 ? '0' : '';
        break;
      }
      case 'type':
      case 'code': {
        var n = item.chars.length;
        var shown = r <= 0 ? 0 : Math.min(n, Math.floor(raw * n + 1e-6));
        for (var i = 0; i < n; i++) {
          item.chars[i].style.visibility = i < shown ? '' : 'hidden';
          item.chars[i].classList.remove('nv-caret-at');
        }
        // The caret: at the typing edge while typing, then blinking twice a second
        // for a moment before it goes.
        var caretOn = r >= 0 && (r < 1 || ((lt - item.at - item.dur) < 1.2 && Math.floor((lt - item.at - item.dur) * 2.2) % 2 === 0));
        el.classList.toggle('nv-typing', caretOn && shown === 0);
        if (caretOn && shown > 0) item.chars[shown - 1].classList.add('nv-caret-at');
        el.style.opacity = r < 0 && item.v === 'type' ? '0' : '';
        break;
      }
      case 'count': {
        var cp = ease(item.ease, brand ? 'power2.out' : 'sine.inOut')(raw);
        el.textContent = formatNumber(item.from + (item.to - item.from) * cp, item.decimals, item.group);
        // Lands exactly, then one pulse.
        var k2 = (lt - item.at - item.dur) / 0.33;
        el.style.transform = !brand && k2 > 0 && k2 < 1 ? 'scale(' + (1 + 0.07 * Math.sin(Math.PI * k2)).toFixed(4) + ')' : '';
        break;
      }
      case 'sweep': {
        var sp = ease(item.ease, 'power2.inOut')(raw);
        el.style.setProperty('--nv-sweep', sp.toFixed(4));
        break;
      }
      case 'scramble': {
        if (r >= 1) { if (el.innerHTML !== item.html) el.innerHTML = item.html; el.style.opacity = ''; break; }
        el.style.opacity = r <= 0 ? '0' : '';
        var frame = Math.floor(lt * (model ? model.fps : 30) / 2);
        var locked = Math.floor(raw * item.text.length);
        var out = '';
        for (var j = 0; j < item.text.length; j++) {
          var c = item.text[j];
          out += (j < locked || /\s/.test(c)) ? c : SCRAMBLE[Math.floor(hash(j, frame) * SCRAMBLE.length)];
        }
        el.textContent = out;
        break;
      }
      case 'swap': paintSwap(item, lt); break;
      case 'paint': paintCanvas(item, lt); break;
      case 'glyph': paintGlyph(item, lt); break;
      case 'sudo': paintSudo(item, lt, r); break;
      case 'cursor': paintCursor(item, lt, r); break;
      case 'karaoke': {
        el.style.setProperty('--p', clamp01(r).toFixed(4));
        el.classList.toggle('is-on', r >= 0 && r < 1);
        el.classList.toggle('is-done', r >= 1);
        break;
      }
      case 'art': {
        var ap = ease(item.ease, 'power2.out')(raw);
        if (el.classList.contains('nv-bg')) { el.style.opacity = ap.toFixed(4); break; }
        el.style.setProperty('--nv-r', (ap * 160).toFixed(3) + '%');
        el.style.setProperty('--nv-ox', item.ox + '%');
        el.style.setProperty('--nv-oy', item.oy + '%');
        if (item.sig) {
          var sk = clamp01((lt - item.at - item.dur * 0.55) / 0.5);
          var sv = ease('back.out(2)', 'linear')(sk);
          item.sig.style.transformBox = 'fill-box';
          item.sig.style.transformOrigin = 'center';
          item.sig.style.transform = 'scale(' + sv.toFixed(4) + ')';
        }
        break;
      }
      case 'kenburns': {
        var span = Math.max(0.5, scene.duration - item.at);
        var kp = EASES['sine.inOut'](clamp01((lt - item.at) / span));
        el.style.transform = 'scale(' + (1 + 0.08 * kp).toFixed(4) + ') translate(' + (-1.2 * kp).toFixed(3) + '%,' + (-0.8 * kp).toFixed(3) + '%)';
        break;
      }
    }
  }

  // A canvas painter (video-paint.js) paints its frame at the element's local time.
  // The canvas is sized to its layout box (unscaled by the preview's fit), so a
  // render draws it at exactly the pixels it will be filmed at.
  function paintCanvas(item, lt) {
    var painters = window.nekoVideoPaint, paint = painters && painters[item.painter];
    if (!paint) return;
    var c = item.el, W = c.offsetWidth, H = c.offsetHeight;
    if (W < 2 || H < 2) return;
    var ratio = rendering ? 1 : Math.min(2, window.devicePixelRatio || 1);
    var cw = Math.round(W * ratio), ch = Math.round(H * ratio);
    if (c.width !== cw || c.height !== ch) { c.width = cw; c.height = ch; }
    var ctx = c.getContext('2d');
    ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    ctx.clearRect(0, 0, W, H);
    ctx.globalAlpha = 1;
    paint(ctx, W, H, Math.max(0, lt - item.at), item.opts);
    c.style.opacity = lt < item.at && !item.opts.before ? '0' : '';
  }

  var GLYPH_CLASS = ['', 'g-k', 'g-s', 'g-a', 'g-sig', 'g-o', 'g-d', 'g-t', 'g-k', 'g-k'];

  // A pixel glyph plays its frames from its start; a loop holds the rest frame for
  // `rest` seconds between runs, a play stands on its last frame. Before its start
  // the glyph is at rest (the last frame), as the website shows it with no motion.
  function paintGlyph(item, lt) {
    var frames = item.frames, n = frames.length;
    if (!n) return;
    var local = lt - item.at, idx = n - 1;
    if (local >= 0) {
      var span = item.total + (item.loop ? item.rest : 0);
      var t = item.loop && span > 0 ? local % span : local, acc = 0;
      if (local < item.total || item.loop) {
        idx = n - 1;
        for (var i = 0; i < n; i++) { acc += frames[i].ms / 1000; if (t < acc) { idx = i; break; } }
      }
    } else if (item.el.hasAttribute('data-hidden-before')) {
      idx = 0;
    }
    var f = frames[idx];
    for (var c = 0; c < 16; c++) {
      var ch = f.cells.charAt(c), k = ch >= '0' && ch <= '9' ? +ch : 0;
      item.cells[c].className = 'nv-gc ' + (GLYPH_CLASS[k] || '');
    }
    if (item.label) {
      var label = null;
      for (var j = idx; j >= 0 && label === null; j--) label = frames[j].label;
      if (label !== null) item.label.textContent = label;
    }
  }

  // Sudo: the clip's frames in turn (looping, or holding the last), and when given
  // a `from` and `to` he walks between them over the element's duration.
  function paintSudo(item, lt, r) {
    var frames = item.frames, n = frames.length;
    if (!n) return;
    var local = Math.max(0, lt - item.at), k = Math.floor(local / item.ms);
    k = item.loop ? k % n : Math.min(n - 1, k);
    for (var i = 0; i < n; i++) frames[i].style.display = i === k ? '' : 'none';
    if (!isNaN(item.from) && !isNaN(item.to)) {
      var host = item.el.parentElement, w = host ? host.clientWidth : 1920;
      var x = item.from + (item.to - item.from) * clamp01(r);
      item.el.style.left = (x * w / 100).toFixed(1) + 'px';
      item.el.classList.toggle('is-left', item.to < item.from);
    }
  }

  function applyExit(item, lt, s) {
    if (!item.exit || item.until === null) return;
    var fn = EXITS[item.exit] || EXITS.fade;
    var q = EASES['power2.in'](clamp01((lt - item.until) / 0.3));
    if (q > 0) fn(s, q);
  }

  // The swap slot: the current word holds, then rolls up and out as the next rolls in.
  function paintSwap(item, lt) {
    var words = item.words, n = words.length, hold = item.hold, roll = Math.min(0.35, hold * 0.45);
    // The slot is as wide as the word in it, and eases to the next word's width as
    // it rolls in, so the line closes up behind a short word. Measured once the
    // fonts are in (an earlier measure would use the fallback face).
    if (!item.widths && api.isReady) item.widths = words.map(function (w) { return w.offsetWidth; });
    var local = lt - item.at;
    var index = Math.max(0, Math.min(n - 1, Math.floor(local / hold)));
    var into = local - index * hold;
    var k = index < n - 1 ? clamp01((into - (hold - roll)) / roll) : 0;
    var e = EASES['power3.inOut'](k);
    for (var i = 0; i < n; i++) {
      var w = words[i], y = 110, o = 0;
      if (i === index) { y = -110 * e; o = 1 - e; }
      else if (i === index + 1 && k > 0) { y = 110 * (1 - e); o = e; }
      w.style.transform = 'translateY(' + y.toFixed(2) + '%)';
      w.style.opacity = o.toFixed(4);
    }
    if (item.widths) {
      var from = item.widths[index], to = item.widths[Math.min(n - 1, index + 1)];
      item.el.style.width = (from + (to - from) * e).toFixed(2) + 'px';
    }
  }

  // The oversized cursor: hidden until its time, then it travels the path (power3.inOut,
  // constant along the polyline's length), presses at the end and throws a ring.
  function paintCursor(item, lt, r) {
    var el = item.el, pts = item.points;
    if (pts.length === 0 || r < 0) { el.style.opacity = '0'; return; }
    el.style.opacity = '';
    var host = el.parentElement;
    var w = host ? host.clientWidth : 1920, h = host ? host.clientHeight : 1080;
    var lengths = [0], total = 0;
    for (var i = 1; i < pts.length; i++) {
      total += Math.hypot((pts[i].x - pts[i - 1].x) * w, (pts[i].y - pts[i - 1].y) * h);
      lengths.push(total);
    }
    var d = EASES['power3.inOut'](clamp01(r)) * total, seg = 1;
    while (seg < pts.length - 1 && lengths[seg] < d) seg++;
    var a = pts[Math.max(0, seg - 1)], b = pts[Math.min(pts.length - 1, seg)];
    var segLen = (lengths[seg] || 0) - (lengths[seg - 1] || 0);
    var f = segLen > 0 ? (d - (lengths[seg - 1] || 0)) / segLen : 1;
    var x = (a.x + (b.x - a.x) * f) * w, y = (a.y + (b.y - a.y) * f) * h;
    var press = 1;
    var after = lt - item.at - item.dur;
    if (item.click && after > 0 && after < 0.3) press = 1 - 0.18 * Math.sin(Math.PI * after / 0.3);
    el.style.transform = 'translate(' + x.toFixed(1) + 'px,' + y.toFixed(1) + 'px) scale(' + press.toFixed(3) + ')';
    if (item.ripple) {
      var rk = item.click ? clamp01(after / 0.55) : 0;
      item.ripple.style.opacity = rk > 0 && rk < 1 ? (0.6 * (1 - rk)).toFixed(3) : '0';
      item.ripple.style.transform = 'translate(-50%,-50%) scale(' + (0.2 + 2.2 * EASES['power2.out'](rk)).toFixed(3) + ')';
    }
  }

  // ---------------------------------------------------------------- scenes and transitions

  // How the incoming scene (p in [0,1] through its transition) and the outgoing one look.
  function transitionStates(name, p, inS, outS, wash) {
    var q, k;
    if (brand && name === 'blur') name = 'fade';
    switch (name) {
      case 'fade':
        inS.o = EASES['sine.inOut'](p);
        break;
      case 'fade-through':
        q = p < 0.5 ? EASES['power2.inOut'](p * 2) : 1 - EASES['power2.inOut']((p - 0.5) * 2);
        wash.o = q;
        if (p < 0.5) inS.o = 0; else outS.o = 0;
        break;
      case 'push':
      case 'push-up': {
        // Cut on the curve: the outgoing scene accelerates away (power4.in), the cut
        // lands mid-motion, the incoming one arrives decelerating (power4.out) from
        // the same side the other left by — one continuous move across the seam.
        var axis = name === 'push' ? 'x' : 'y', dist = name === 'push' ? 230 : 180;
        q = clamp01(p / 0.5);
        outS[axis] = -dist * EASES['power4.in'](q);
        outS.o = 1 - clamp01(q / 0.6);
        outS.b = 8 * q;
        k = clamp01((p - 0.4) / 0.6);
        inS[axis] = dist * (1 - EASES['power4.out'](k));
        inS.o = k <= 0 ? 0 : 0.35 + 0.65 * clamp01(k * 3);
        inS.b = 8 * (1 - k) * (k > 0 ? 1 : 0);
        break;
      }
      case 'zoom':
      case 'zoom-out': {
        var forward = name === 'zoom';
        q = clamp01(p / 0.4);
        outS.s = forward ? 1 + 0.2 * EASES['power3.in'](q) : 1 - 0.2 * EASES['power3.in'](q);
        outS.b = 10 * q;
        outS.o = q >= 1 ? 0 : 1 - 0.85 * q;
        k = clamp01((p - 0.35) / 0.65);
        inS.s = forward ? 0.75 + 0.25 * EASES['expo.out'](k) : 1.25 - 0.25 * EASES['expo.out'](k);
        inS.o = k <= 0 ? 0 : clamp01(k * 4);
        inS.b = 6 * (1 - EASES['expo.out'](k)) * (k > 0 ? 1 : 0);
        break;
      }
      case 'wipe': {
        k = EASES['power3.inOut'](p);
        inS.clip = 'inset(0 ' + ((1 - k) * 100).toFixed(3) + '% 0 0)';
        inS.x = 60 * (1 - k);
        inS.s = 1.04 - 0.04 * k;
        outS.x = -70 * k;
        outS.s = 1 - 0.04 * k;
        outS.b = 5 * k;
        break;
      }
      case 'iris':
        k = EASES['power3.inOut'](p);
        inS.clip = 'circle(' + (k * 75).toFixed(3) + '% at 50% 50%)';
        break;
      case 'blur':
        q = clamp01(p / 0.5);
        outS.b = 12 * EASES['power2.in'](q);
        outS.s = 1 + 0.06 * q;
        outS.o = 1 - q;
        k = clamp01((p - 0.5) / 0.5);
        inS.b = 12 * (1 - EASES['power2.out'](k));
        inS.s = 1.06 - 0.06 * EASES['power2.out'](k);
        inS.o = k;
        break;
    }
  }

  function paintCamera(scene, lt) {
    if (!scene.cam || !scene.camera) return;
    var f = EASES['sine.inOut'](clamp01(lt / Math.max(0.5, scene.duration)));
    var t = '';
    switch (scene.camera) {
      case 'push': case 'push-in': t = 'scale(' + (1 + 0.06 * f).toFixed(4) + ')'; break;
      case 'pull': case 'pull-back': t = 'scale(' + (1.08 - 0.08 * f).toFixed(4) + ')'; break;
      case 'pan-left': t = 'translateX(' + (40 - 80 * f).toFixed(2) + 'px)'; break;
      case 'pan-right': t = 'translateX(' + (-40 + 80 * f).toFixed(2) + 'px)'; break;
      case 'rise': t = 'translateY(' + (30 - 60 * f).toFixed(2) + 'px)'; break;
    }
    scene.cam.style.transform = t;
  }

  function seekMedia(scene, lt, waits) {
    scene.media.forEach(function (m) {
      var v = m.el;
      var target = Math.max(0, lt - m.at + m.offset);
      if (v.duration && isFinite(v.duration)) target = Math.min(target, Math.max(0, v.duration - 0.01));
      if (playing && !rendering) {
        if (v.paused) { var pr = v.play(); if (pr && pr.catch) pr.catch(function () { }); }
        if (Math.abs(v.currentTime - target) > 0.25) v.currentTime = target;
        return;
      }
      if (!v.paused) v.pause();
      if (Math.abs(v.currentTime - target) > 0.5 / (model ? model.fps : 30)) {
        waits.push(new Promise(function (resolve) {
          var done = function () { v.removeEventListener('seeked', done); resolve(); };
          v.addEventListener('seeked', done);
          v.currentTime = target;
          setTimeout(done, 3000);
        }));
      }
    });
  }

  function seek(t) {
    if (!model) return Promise.resolve();
    var duration = model.duration;
    t = Math.max(0, Math.min(duration, t));
    current = t;
    var scenes = model.scenes;
    var waits = [];
    var washState = { o: 0 };

    for (var i = 0; i < scenes.length; i++) {
      var scene = scenes[i];
      var end = scene.start + scene.duration;
      var isLast = i === scenes.length - 1;
      var visible = t >= scene.start && (t < end || (isLast && t >= end - 1e-6));
      scene.el.classList.toggle('is-on', visible);
      if (!visible) {
        scene.el.style.display = 'none';
        scene.media.forEach(function (m) { if (!m.el.paused) m.el.pause(); });
        continue;
      }
      scene.el.style.display = 'block';

      var lt = t - scene.start;
      var inS = freshState();
      var outS = freshState();

      // This scene is coming in…
      if (scene.tdur > 0 && lt < scene.tdur) {
        transitionStates(scene.transition, lt / scene.tdur, inS, freshState(), washState);
      }
      // …or the next one is coming in over it.
      var next = scenes[i + 1];
      if (next && next.tdur > 0 && t >= next.start && t < next.start + next.tdur) {
        transitionStates(next.transition, (t - next.start) / next.tdur, freshState(), outS, washState);
      }

      var s = freshState();
      s.o = inS.o * outS.o;
      s.x = inS.x + outS.x; s.y = inS.y + outS.y;
      s.s = inS.s * outS.s;
      s.b = inS.b + outS.b;
      s.clip = inS.clip || outS.clip;
      writeState(scene.el, s);

      paintCamera(scene, lt);
      for (var j = 0; j < scene.items.length; j++) paintItem(scene.items[j], lt, scene);
      seekMedia(scene, lt, waits);
    }

    if (model.wash) model.wash.style.opacity = washState.o > 0.001 ? washState.o.toFixed(4) : '0';
    updateBar();
    return waits.length ? Promise.all(waits) : Promise.resolve();
  }

  // ---------------------------------------------------------------- the preview player

  var playing = false, rendering = false, origin = 0, originTime = 0, raf = 0;

  function fit() {
    if (!model || rendering) return;
    var viewport = document.getElementById('nv-viewport');
    var bar = document.getElementById('nv-bar');
    var barH = bar ? bar.offsetHeight : 0;
    var timing = document.getElementById('nv-timing');
    if (timing && !timing.hidden) barH += timing.offsetHeight;
    var availW = window.innerWidth, availH = window.innerHeight - barH;
    var scale = Math.min(availW / model.width, availH / model.height);
    model.stage.style.transform = 'scale(' + scale + ')';
    if (viewport) {
      viewport.style.width = Math.round(model.width * scale) + 'px';
      viewport.style.height = Math.round(model.height * scale) + 'px';
    }
  }

  function clock(t) {
    var m = Math.floor(t / 60), s = t - m * 60;
    return m + ':' + (s < 10 ? '0' : '') + s.toFixed(1);
  }

  function updateBar() {
    if (rendering) return;
    var time = document.getElementById('nv-time');
    var fill = document.getElementById('nv-scrub-fill');
    var range = document.getElementById('nv-range');
    var f = model.duration > 0 ? current / model.duration : 0;
    if (time) time.textContent = clock(current);
    if (fill) fill.style.width = (f * 100).toFixed(3) + '%';
    if (range && document.activeElement !== range) range.value = Math.round(f * 1000);
    updateNarration(f);
  }

  // ---------------------------------------------------------------- the voice-over in the preview

  var subtitles = true, lastSentence = -2;

  // The sentence being spoken at the playhead (the subtitle), and the playhead and the
  // items under it in the timing panel.
  function updateNarration(f) {
    var head = document.getElementById('nv-playhead');
    if (head) head.style.setProperty('--f', f.toFixed(5));
    var active = -1;
    for (var i = 0; i < model.narration.length; i++) {
      var n = model.narration[i];
      if (current >= n.start && current < n.end) { active = i; break; }
    }
    if (active === lastSentence) return;
    lastSentence = active;
    var sub = document.getElementById('nv-subtitle');
    if (sub) {
      var sentence = active >= 0 ? model.narration[active] : null;
      sub.textContent = sentence && subtitles ? sentence.text : '';
      sub.classList.toggle('is-on', !!(sentence && subtitles));
      sub.classList.toggle('is-estimated', !!(sentence && sentence.estimated));
    }
    var lane = document.querySelectorAll('#nv-timing .nv-sent');
    for (var j = 0; j < lane.length; j++) lane[j].classList.toggle('is-active', j === active);
  }

  function setTiming(open) {
    var panel = document.getElementById('nv-timing');
    var toggle = document.getElementById('nv-timing-toggle');
    if (!panel) return;
    panel.hidden = !open;
    if (toggle) toggle.setAttribute('aria-pressed', open ? 'true' : 'false');
    fit();
  }

  function wireTiming() {
    var panel = document.getElementById('nv-timing');
    var toggle = document.getElementById('nv-timing-toggle');
    if (toggle) toggle.addEventListener('click', function () { setTiming(panel && panel.hidden); });
    if (panel) panel.addEventListener('click', function (e) {
      var target = e.target.closest ? e.target.closest('[data-t]') : null;
      if (!target) return;
      setPlaying(false);
      seek(parseFloat(target.getAttribute('data-t')) || 0);
    });
    var warns = document.getElementById('nv-timing-warns');
    if (warns) warns.addEventListener('toggle', fit);
    if (/(^|[#&])timing\b/.test(location.hash)) setTiming(true);
  }

  function syncAudio() {
    model.audio.forEach(function (a) {
      var start = num(a, 'data-start', 0);
      a.volume = Math.max(0, Math.min(1, num(a, 'data-volume', 1)));
      var local = current - start;
      // A take's length is known at build time; an Ogg stream may report Infinity.
      var length = num(a, 'data-duration', isFinite(a.duration) ? a.duration : 0);
      var inside = playing && local >= 0 && (!length || local < length);
      if (!inside) { if (!a.paused) a.pause(); return; }
      if (Math.abs(a.currentTime - local) > 0.25) a.currentTime = local;
      if (a.paused) { var pr = a.play(); if (pr && pr.catch) pr.catch(function () { }); }
    });
  }

  function setPlaying(on) {
    playing = on;
    document.body.classList.toggle('nv-playing', on);
    var button = document.getElementById('nv-play');
    if (button) {
      button.setAttribute('aria-label', on ? 'Pause' : 'Play');
      button.innerHTML = '<i class="fi fi-rr-' + (on ? 'pause' : 'play') + '" aria-hidden="true"></i>';
    }
    if (on) {
      if (current >= model.duration - 1e-3) current = 0;
      origin = performance.now();
      originTime = current;
      cancelAnimationFrame(raf);
      raf = requestAnimationFrame(tick);
    } else {
      cancelAnimationFrame(raf);
    }
    syncAudio();
  }

  function tick(now) {
    if (!playing) return;
    var t = originTime + (now - origin) / 1000;
    if (t >= model.duration) {
      if (model.timeline.loop) { origin = now; originTime = 0; t = 0; }
      else { seek(model.duration); setPlaying(false); return; }
    }
    seek(t);
    syncAudio();
    raf = requestAnimationFrame(tick);
  }

  function wirePlayer() {
    var button = document.getElementById('nv-play');
    if (button) button.addEventListener('click', function () { setPlaying(!playing); });
    var range = document.getElementById('nv-range');
    if (range) range.addEventListener('input', function () {
      if (playing) setPlaying(false);
      seek(range.value / 1000 * model.duration);
    });
    document.addEventListener('keydown', function (e) {
      if (e.target && /input|textarea/i.test(e.target.tagName) && e.target.type !== 'range') return;
      var step = e.shiftKey ? 5 : 1;
      switch (e.key) {
        case ' ': case 'k': e.preventDefault(); setPlaying(!playing); break;
        case 'ArrowRight': e.preventDefault(); setPlaying(false); seek(current + step); break;
        case 'ArrowLeft': e.preventDefault(); setPlaying(false); seek(current - step); break;
        case 'Home': e.preventDefault(); setPlaying(false); seek(0); break;
        case 'End': e.preventDefault(); setPlaying(false); seek(model.duration); break;
        case 't': case 'T': var panel = document.getElementById('nv-timing'); if (panel) { e.preventDefault(); setTiming(panel.hidden); } break;
        case 'c': case 'C': subtitles = !subtitles; lastSentence = -2; updateBar(); break;
        case 'Escape': var back = document.getElementById('nv-back'); if (back) back.click(); break;
      }
    });
    window.addEventListener('resize', fit);
    var back = document.getElementById('nv-back');
    if (back && back.getAttribute('data-pinned') !== 'true' && document.referrer) {
      try { if (new URL(document.referrer).origin === location.origin) back.href = document.referrer; } catch (e) { }
    }
  }

  function renderMode() {
    rendering = true;
    setPlaying(false);
    document.documentElement.classList.add('nv-render');
    model.stage.style.transform = 'none';
    var viewport = document.getElementById('nv-viewport');
    if (viewport) { viewport.style.width = model.width + 'px'; viewport.style.height = model.height + 'px'; }
    return { duration: model.duration, fps: model.fps, width: model.width, height: model.height };
  }

  // Everything a frame needs has to be in before the first one is taken: the fonts,
  // every image decoded, every clip able to show a frame.
  function waitForAssets() {
    var waits = [];
    if (document.fonts && document.fonts.ready) waits.push(document.fonts.ready);
    document.querySelectorAll('#nv-stage img').forEach(function (img) {
      waits.push(img.decode ? img.decode().catch(function () { }) : Promise.resolve());
    });
    document.querySelectorAll('#nv-stage video').forEach(function (v) {
      if (v.readyState >= 2) return;
      waits.push(new Promise(function (resolve) {
        v.addEventListener('loadeddata', resolve, { once: true });
        v.addEventListener('error', resolve, { once: true });
        setTimeout(resolve, 8000);
      }));
    });
    return Promise.all(waits);
  }

  var resolveReady;
  var api = window.nekoVideo = {
    isReady: false,
    ready: new Promise(function (resolve) { resolveReady = resolve; }),
    duration: 0, fps: 30, width: 1920, height: 1080,
    seek: function (t) { return seek(t); },
    play: function () { setPlaying(true); },
    pause: function () { setPlaying(false); },
    renderMode: function () { return renderMode(); },
    time: function () { return current; }
  };

  function init() {
    if (model) return;
    if (!document.getElementById('nv-stage')) return;
    model = buildModel();
    if (!model) return;
    brand = document.body.getAttribute('data-motion') === 'brand';
    api.duration = model.duration; api.fps = model.fps; api.width = model.width; api.height = model.height;
    document.body.setAttribute('data-video-ready', 'true');
    if (/(^|[#&])render\b/.test(location.hash)) renderMode();
    wirePlayer();
    wireTiming();
    fit();
    var start = /(?:^|[#&])t=([\d.]+)/.exec(location.hash);
    seek(start ? parseFloat(start[1]) : 0);
    waitForAssets().then(function () {
      api.isReady = true;
      seek(current);
      api.isReady = true;
      resolveReady(api);
      if (model.timeline.autoplay && !rendering) setPlaying(true);
    });
  }

  window.nekoVideoInit = init;
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
