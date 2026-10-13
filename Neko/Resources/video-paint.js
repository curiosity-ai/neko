/* ==========================================================================
   Neko video mode — the painters.

   The Curiosity brand's drawn motion, as pure functions of time. Each painter
   is paint(ctx, W, H, t, o): it clears nothing and remembers nothing, and paints
   the frame at t seconds (scene-local, from the element's start) into a canvas
   W by H CSS pixels. video.js calls it on every seek, so the same t always
   paints the same frame — what a render needs, and what lets the preview
   scrub backwards.

   They are ported from the brand's own implementations, keeping their numbers:

     field      the dash field (BRAND.md §6), live: website js/field.js and
                Tesserae.Themes.Curiosity FlowField — the signal pixel circling
                its place, the dashes kept clear of it
     squares    the square field (§6 secondary motif): website js/decor.js
                squareField and the intro's turn toward the Signal
     mosaic     the dash tiles: website decor.js dashes() with its seven
                shapes (wave, rows, chart, chartGrid, mountain, band, dither),
                Tesserae DashMosaic
     stepGrid, joinGrid, lattice, dotChart
                the customer story tiles (decor.js STORY_TILES)
     intro      the first-load intro: Tesserae PixelIntro / website js/intro.js
                (the square field sweep, or the tile sweep)
     cube       the mark turning into a cube: website js/logo-cube.js draw()

   Where the originals keep state across frames (a pixel easing toward the
   pointer, a random step every 1.4 s) the state is replayed from t instead.
   ========================================================================== */
(function () {
  'use strict';

  function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
  function smooth(p) { p = clamp01(p); return p * p * (3 - 2 * p); }          // BrandMotion.Ease
  function rng(seed) {                                                      // decor.js rng: the same LCG
    var s = seed >>> 0 || 1;
    return function () { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; };
  }
  function walk(r, n) {                                                     // a smooth random line, 0..1
    var p = [], y = 0.5, v = 0;
    for (var i = 0; i < n; i++) { v = v * 0.7 + (r() - 0.5) * 0.35; y = Math.max(0.12, Math.min(0.88, y + v * 0.3)); p.push(y); }
    return p;
  }
  function sample(p, u) { var x = u * (p.length - 1), i = Math.floor(x), f = x - i; return p[i] + ((p[i + 1] === undefined ? p[i] : p[i + 1]) - p[i]) * f; }
  var lines = {};
  function line(seed) { return lines[seed] || (lines[seed] = walk(rng(seed), 90)); }

  // The signal pixel's drift: it starts at home and circles it slowly (FlowField's 0.35 rad/s).
  function drift(o, t) {
    var hx = o.dot ? o.dot[0] : 0.5, hy = o.dot ? o.dot[1] : 0.5, a = o.drift === false ? 0 : (o.drift || 1);
    return [hx + (Math.cos(t * 0.35) - 1) * 0.16 * a, hy + Math.sin(t * 0.35) * 0.2 * a];
  }

  /* --- the dash field -------------------------------------------------- */
  function field(ctx, W, H, t, o) {
    if (o.bg) { ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H); }
    var pitch = o.pitch || 32, cols = Math.max(4, Math.round(W / pitch)), rows = Math.max(4, Math.round(H / pitch));
    var cw = W / cols, ch = H / rows, len = Math.min(o.len || 18, cw * 0.55), DOT = o.size || 12;
    var d0 = drift(o, t), ax = d0[0] * W, ay = d0[1] * H, sigma = 0.3 * Math.max(W, H);
    var keep = Math.max(len * 0.6, DOT * 0.9);
    // The field draws itself in from the signal pixel over `reveal` seconds.
    var reveal = o.reveal ? smooth(t / o.reveal) : 1, reach = reveal * Math.hypot(W, H) * 1.05;
    ctx.lineWidth = 1.5; ctx.lineCap = 'square'; ctx.strokeStyle = o.dash || '#8A9099';
    for (var r = 0; r < rows; r++) for (var c = 0; c < cols; c++) {
      var x = c * cw + cw / 2, y = r * ch + ch / 2, dx = x - ax, dy = y - ay, d = Math.sqrt(dx * dx + dy * dy);
      if (d < keep || d > reach) continue;
      var w = Math.exp(-(d * d) / (2 * sigma * sigma)), circ = Math.atan2(dy, dx) + Math.PI / 2;
      var a = Math.atan2(w * Math.sin(circ), (1 - w) + w * Math.cos(circ));
      var f = o.fade === 'right' ? 0.15 + 0.85 * (1 - c / (cols - 1)) : o.fade === 'left' ? 0.15 + 0.85 * (c / (cols - 1))
            : o.fade === 'down' ? 0.2 + 0.8 * (1 - r / (rows - 1)) : o.fade === 'up' ? 0.2 + 0.8 * (r / (rows - 1)) : 1;
      // Dashes at the edge of the reveal grow in.
      ctx.globalAlpha = f * clamp01((reach - d) / 60);
      var hx = Math.cos(a) * len / 2, hy = Math.sin(a) * len / 2;
      ctx.beginPath(); ctx.moveTo(x - hx, y - hy); ctx.lineTo(x + hx, y + hy); ctx.stroke();
    }
    ctx.globalAlpha = 1;
    if (o.mark === 'none') return;
    var k = o.reveal ? smooth((t - o.reveal * 0.15) / 0.35) : 1, s = DOT * k;
    if (s < 0.5) return;
    ctx.fillStyle = o.hot || '#2F6BFF';
    ctx.fillRect(Math.round(ax - s / 2), Math.round(ay - s / 2), s, s);
  }

  /* --- the square field ------------------------------------------------ */
  function squares(ctx, W, H, t, o) {
    if (o.bg) { ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H); }
    var pitch = o.pitch || (W < 500 ? 40 : 50), cols = Math.max(4, Math.round(W / pitch)), rows = Math.max(3, Math.round(H / pitch));
    var cw = W / cols, ch = H / rows, side = Math.min(cw, ch) * 0.4, sigma = 0.3 * Math.max(W, H);
    var d0 = drift(o, t), ax = d0[0] * W, ay = d0[1] * H;
    var sc = Math.min(cols - 1, Math.max(0, Math.floor(ax / cw))), sr = Math.min(rows - 1, Math.max(0, Math.floor(ay / ch)));
    // Grow in on a diagonal sweep, then turn toward the Signal (the intro's build), over `reveal` seconds.
    var R = o.reveal || 0, IN = R * 0.6, turn = R ? smooth((t - IN * 0.6) / (R * 0.4)) : 1;
    for (var r = 0; r < rows; r++) for (var c = 0; c < cols; c++) {
      var x = c * cw + cw / 2, y = r * ch + ch / 2, ds = (c + r) / (cols + rows);
      var grow = R ? smooth((t - ds * IN * 0.7) / (IN * 0.3 || 1)) : 1;
      if (c === sc && r === sr) {
        var g = R ? smooth((t - R * 0.85) / 0.16) : 1, ss = side * g;
        if (ss < 0.5) continue;
        ctx.globalAlpha = 1; ctx.fillStyle = o.hot || '#2F6BFF';
        ctx.fillRect(Math.round(x - ss / 2), Math.round(y - ss / 2), Math.round(ss), Math.round(ss));
        continue;
      }
      var s = side * grow;
      if (s < 0.5) continue;
      var dx = x - ax, dy = y - ay, w = Math.exp(-(dx * dx + dy * dy) / (2 * sigma * sigma));
      ctx.globalAlpha = o.fade === false ? 1 : 0.22 + 0.78 * (c / Math.max(1, cols - 1));
      ctx.fillStyle = o.ink || '#F4F4F2';
      ctx.save(); ctx.translate(x, y); ctx.rotate(Math.PI / 4 * w * turn);
      ctx.fillRect(-s / 2, -s / 2, s, s);
      ctx.restore();
    }
    ctx.globalAlpha = 1;
  }

  /* --- the dash tiles --------------------------------------------------- */
  var SHAPES = {
    wave: function (u, v, t) { var d = (u + v) / 2 - ((t * 0.12) % 1.4 - 0.2); return Math.abs(d) < 0.05 ? 0 : 1; },
    rows: function (u, v, t, L, i, j) {
      var s = Math.sin(j * 12.9898) * 43758.5453; s -= Math.floor(s);
      var len = 0.2 + s * 0.5, at = ((t * (0.04 + s * 0.06) + s) % 1.4) - 0.3;
      return (u > at && u < at + len && (j % 3 !== 1)) ? 1 : 0;
    },
    chart: function (u, v, t, L, i, j, cols, rows) { var row = Math.round(sample(L, (u * 0.55 + t * 0.018) % 1) * (rows - 1)); return j === row ? 2 : -1; },
    chartGrid: function (u, v, t, L, i, j, cols, rows) { var row = Math.round(sample(L, (u * 0.55 + t * 0.018) % 1) * (rows - 1)); return j === row ? 2 : 0; },
    mountain: function (u, v, t, L) { var y = sample(L, (u * 0.5 + t * 0.012) % 1) * 0.8 + 0.12; if (v < y) return -1; return u < 0.22 ? 2 : 1; },
    band: function (u, v, t) { var c = 0.15 + 0.7 * (1 - v) + Math.sin(t * 0.4) * 0.05; return Math.abs(u - c) < 0.16 ? 2 : -1; },
    dither: function (u, v, t) {
      var cx = 0.5 + Math.sin(t * 0.25) * 0.25, cy = 0.5 + Math.cos(t * 0.2) * 0.2;
      var d = Math.hypot(u - cx, (v - cy) * 1.2), n = Math.sin(u * 91.7 + v * 47.3) * 43758.5; n -= Math.floor(n);
      return d < 0.28 + n * 0.08 ? 2 : 1;
    }
  };
  function mosaic(ctx, W, H, t, o) {
    var dx = 11, dy = 17, bw = 3, bh = 9;
    if (W < 260) { dx = 8; dy = 13; bw = 2; bh = 7; }
    if (o.scale) { dx *= o.scale; dy *= o.scale; bw *= o.scale; bh *= o.scale; }
    if (o.bg) { ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H); }
    var cols = Math.max(1, Math.floor((W - 12) / dx)), rows = Math.max(1, Math.floor((H - 12) / dy));
    var ox = (W - (cols - 1) * dx - bw) / 2, oy = (H - (rows - 1) * dy - bh) / 2;
    var shape = SHAPES[o.shape] || SHAPES.rows, L = line(o.seed || 7), tone = [o.off, o.on, o.acc];
    // Columns switch on left to right over `reveal` seconds.
    var reveal = o.reveal ? clamp01(t / o.reveal) : 1;
    for (var j = 0; j < rows; j++) for (var i = 0; i < cols; i++) {
      var u = cols > 1 ? i / (cols - 1) : 0, v = rows > 1 ? j / (rows - 1) : 0;
      if (u > reveal * 1.15 - (v * 0.15)) continue;
      var k = shape(u, v, t + (o.offset || 4), L, i, j, cols, rows);
      if (k < 0 || !tone[k]) continue;
      ctx.fillStyle = tone[k];
      ctx.fillRect(Math.round(ox + i * dx), Math.round(oy + j * dy), bw, bh);
    }
  }

  /* --- the story tiles ---------------------------------------------------- */
  var DIRS = [[1, 0], [-1, 0], [0, 1], [0, -1]];

  // A lattice of plus signs, one turned to an x that steps to a neighbor every 1.4 s.
  function lattice(ctx, W, H, t, o) {
    if (o.bg) { ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H); }
    var p = o.pitch || (W < 500 ? 44 : 64), arm = p * (o.arm || 0.5), cols = Math.ceil(W / p) + 1, rows = Math.ceil(H / p) + 1;
    var ox = (W - (cols - 1) * p) / 2, oy = (H - (rows - 1) * p) / 2, r = rng(o.seed || 80);
    var x = { c: (cols * 0.25) | 0, r: (rows * 0.3) | 0 }, steps = Math.floor(t / 1.4);
    for (var s = 0; s < steps; s++) {
      var d = DIRS[(r() * 4) | 0];
      x.c = Math.max(0, Math.min(cols - 1, x.c + d[0])); x.r = Math.max(0, Math.min(rows - 1, x.r + d[1]));
    }
    ctx.lineWidth = o.lw || 1.3; ctx.lineCap = 'butt';
    for (var rr = 0; rr < rows; rr++) for (var c = 0; c < cols; c++) {
      var px = ox + c * p, py = oy + rr * p, hot = c === x.c && rr === x.r;
      ctx.strokeStyle = hot ? o.hot : o.ink;
      ctx.beginPath();
      if (hot) { var a = arm * 0.62; ctx.moveTo(px - a, py - a); ctx.lineTo(px + a, py + a); ctx.moveTo(px + a, py - a); ctx.lineTo(px - a, py + a); }
      else { ctx.moveTo(px - arm, py); ctx.lineTo(px + arm, py); ctx.moveTo(px, py - arm); ctx.lineTo(px, py + arm); }
      ctx.stroke();
    }
  }

  // A column chart in small squares, scrolling on; the latest run in the hot tone.
  function dotChart(ctx, W, H, t, o) {
    if (o.bg) { ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H); }
    var s = W < 360 ? 4 : 5, g = s + 3, cols = Math.floor((W - 24) / g), rows = Math.floor((H - 24) / g);
    var h = lines['dc' + (o.seed || 80)] || (lines['dc' + (o.seed || 80)] = walk(rng(o.seed || 80), 400));
    var off = Math.floor(t * 5), x0 = (W - cols * g) / 2, base = H - 12 - (H - 12 - rows * g) / 2;
    for (var c = 0; c < cols; c++) {
      var v = h[(c + off) % h.length], n = Math.max(1, Math.round((1 - v) * rows * 0.95));
      var hot = c > cols * 0.62 && c < cols * 0.86, fade = c < cols * 0.45;
      ctx.globalAlpha = fade ? 0.42 : 1;
      ctx.fillStyle = hot ? o.hot : o.ink;
      for (var r = 0; r < n; r++) ctx.fillRect(Math.round(x0 + c * g), Math.round(base - (r + 1) * g), s, s);
    }
    ctx.globalAlpha = 1;
  }

  // A dot grid; two squares step through it every 1.1 s, the second following the first.
  function stepGrid(ctx, W, H, t, o) {
    ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H);
    var p = W < 360 ? 18 : 22, cols = Math.floor((W - 8) / p), rows = Math.floor((H - 8) / p);
    var ox = (W - (cols - 1) * p) / 2, oy = (H - (rows - 1) * p) / 2, r = rng(o.seed || 80);
    var a = { c: (cols * 0.62) | 0, r: (rows * 0.3) | 0 }, b = { c: a.c - 2, r: a.r + 2 }, steps = Math.floor(t / 1.1);
    for (var s = 0; s < steps; s++) {
      b = { c: a.c - 2, r: a.r + 2 };
      var d = DIRS[(r() * 4) | 0];
      a = { c: Math.max(3, Math.min(cols - 2, a.c + d[0])), r: Math.max(1, Math.min(rows - 4, a.r + d[1])) };
    }
    ctx.fillStyle = o.dot;
    for (var rr = 0; rr < rows; rr++) for (var c = 0; c < cols; c++) ctx.fillRect(Math.round(ox + c * p) - 1, Math.round(oy + rr * p) - 1, 2, 2);
    var side = Math.round(p * 0.9);
    [[b, o.b], [a, o.a]].forEach(function (k) {
      ctx.fillStyle = k[1];
      ctx.fillRect(Math.round(ox + k[0].c * p - side / 2), Math.round(oy + k[0].r * p - side / 2), side, side);
    });
  }

  // A hairline grid; three cells step into one wide cell with the Signal in it, and apart again.
  function joinGrid(ctx, W, H, t, o) {
    ctx.fillStyle = o.bg; ctx.fillRect(0, 0, W, H);
    var cols = 6, rows = 4, cw = W / cols, ch = H / rows;
    ctx.fillStyle = o.rule;
    for (var i = 1; i < cols; i++) ctx.fillRect(Math.round(i * cw), 0, 1, H);
    for (var j = 1; j < rows; j++) ctx.fillRect(0, Math.round(j * ch), W, 1);
    var seq = [0, 0, 1, 2, 3, 3, 3, 2, 1], k = seq[Math.floor(t / 0.7) % seq.length];
    var from = [[0, 0], [3, 1], [1, 3]], to = [[1, 1], [2, 1], [3, 1]], q = Math.min(k, 2) / 2;
    var s = Math.round(Math.min(cw, ch) * 0.34);
    if (k === 3) {
      ctx.fillStyle = o.ink;
      ctx.fillRect(Math.round(cw) + 1, Math.round(ch) + 1, Math.round(cw * 3) - 1, Math.round(ch) - 1);
      ctx.fillStyle = o.hot;
      ctx.fillRect(Math.round(cw * 4 - s - 8), Math.round(ch + 8), s, s);
      return;
    }
    ctx.fillStyle = o.ink;
    from.forEach(function (f, n) {
      var c = f[0] + (to[n][0] - f[0]) * q, r = f[1] + (to[n][1] - f[1]) * q;
      ctx.fillRect(Math.round(c * cw + cw / 2 - s / 2), Math.round(r * ch + ch / 2 - s / 2), s, s);
    });
  }

  /* --- the first-load intro ---------------------------------------------- */
  // Tesserae PixelIntro.PlaySquareField, in seconds: squares grow in on a diagonal sweep over ink, turn toward
  // the Signal, the Signal lands last, then all shrink away in the same order and the ink lifts off.
  var IN = 0.56, TURN = 0.38, HOLD = 0.22, OUT = 0.52, GO = IN + TURN + HOLD, END = GO + OUT;
  function intro(ctx, W, H, t, o) {
    if (o.style === 'tiles') return introTiles(ctx, W, H, t, o);
    if (t >= END) return;
    var pitch = W < 640 ? 36 : 50, cols = Math.ceil(W / pitch), rows = Math.ceil(H / pitch), side = pitch * 0.4;
    var sigma = 0.3 * Math.max(W, H), sc = Math.round(cols * 0.62), sr = Math.round(rows * 0.45);
    var ax = sc * pitch + pitch / 2, ay = sr * pitch + pitch / 2;
    ctx.globalAlpha = 1 - smooth((t - GO - OUT * 0.35) / (OUT * 0.65));
    ctx.fillStyle = '#111418'; ctx.fillRect(0, 0, W, H);
    var turn = smooth((t - IN * 0.6) / TURN);
    for (var r = 0; r < rows; r++) for (var k = 0; k < cols; k++) {
      var x = k * pitch + pitch / 2, y = r * pitch + pitch / 2, dx = x - ax, dy = y - ay;
      var w = Math.exp(-(dx * dx + dy * dy) / (2 * sigma * sigma)), ds = (k + r) / (cols + rows), sg = k === sc && r === sr;
      var grow = smooth((t - ds * IN * 0.7) / (IN * 0.3)), shrink = smooth((t - GO - ds * OUT * 0.55) / (OUT * 0.3));
      if (sg) { grow = smooth((t - IN * 0.85) / 0.16); shrink = smooth((t - END + 0.2) / 0.2); }
      var s = side * grow * (1 - shrink);
      if (s < 0.5) continue;
      ctx.globalAlpha = sg ? 1 : 0.22 + 0.78 * (k / Math.max(1, cols - 1));
      ctx.fillStyle = sg ? '#2F6BFF' : '#F4F4F2';
      ctx.save(); ctx.translate(x, y); if (!sg) ctx.rotate(Math.PI / 4 * w * turn);
      ctx.fillRect(-s / 2, -s / 2, s, s); ctx.restore();
    }
    ctx.globalAlpha = 1;
  }
  // PixelIntro.PlayTiles: ink and slate tiles laid from the corner, taken away in the same order; the
  // escaped Signal square, top right, leaves last.
  function introTiles(ctx, W, H, t, o) {
    var tones = ['#111418', '#111418', '#2A2F38'], r = rng(20260923), cell = W < 640 ? 48 : 80;
    var cols = Math.ceil(W / cell), rows = Math.ceil(H / cell), span = cols + rows + 2.5, TIN = 0.62, THOLD = 0.16, TOUT = 0.62;
    for (var j = 0, n = 0; j < rows; j++) for (var i = 0; i < cols; i++, n++) {
      var fill = tones[(r() * 3) | 0], delay = i + j + r() * 2.5;
      if (n === cols - 1) { fill = '#2F6BFF'; delay = span; }
      var at = delay / span;
      if (t < at * TIN || t > TIN + THOLD + at * TOUT) continue;
      ctx.fillStyle = fill; ctx.fillRect(i * cell, j * cell, cell, cell);
    }
  }

  /* --- the mark turning into a cube ------------------------------------------ */
  // website js/logo-cube.js, the 'flat-light' shading Curiosity chose; t in seconds from the start.
  var T_TUCK = 320, T_LIFT = 480, T_TWIST = 440, T_GAP = 60, T_LAND = 480, T_OUT = 340;
  var CA = T_TUCK, CB = CA + T_LIFT, CC = CB + 3 * (T_TWIST + T_GAP), CD = CC + T_LAND, CE = CD + T_OUT;
  var MOVES = [['y', -1, 1], ['x', 1, -1], ['y', 1, -1]], YAW = -34 * Math.PI / 180, PITCH = -24 * Math.PI / 180, CAM = 520;
  var AX = { x: 0, y: 1, z: 2 };
  function cease(t) { return t < 0 ? 0 : t > 1 ? 1 : t < .5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2; }
  function cback(t) { if (t <= 0) return 0; if (t >= 1) return 1; var c = 1.7; return 1 + (c + 1) * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2); }
  function rot(axis, a, p) {
    var c = Math.cos(a), s = Math.sin(a), x = p[0], y = p[1], z = p[2];
    if (axis === 'x') return [x, y * c - z * s, y * s + z * c];
    if (axis === 'y') return [x * c + z * s, y, -x * s + z * c];
    return [x * c - y * s, x * s + y * c, z];
  }
  function phase(t) {
    var out = 0, depth = 0, view = 0, twists = [0, 0, 0];
    if (t < CA) out = 1 - cease(t / T_TUCK);
    else if (t < CB) { depth = view = cease((t - CA) / T_LIFT); }
    else if (t < CC) { depth = view = 1; for (var i = 0; i < 3; i++) twists[i] = cease((t - CB - i * (T_TWIST + T_GAP)) / T_TWIST); }
    else if (t < CD) { twists = [1, 1, 1]; depth = view = 1 - cease((t - CC) / T_LAND); }
    else if (t < CE) { twists = [1, 1, 1]; out = cback((t - CD) / T_OUT); }
    else { twists = [1, 1, 1]; out = 1; }
    return { out: out, depth: depth, view: view, twists: twists };
  }
  function cube(ctx, W, H, t, o) {
    // `loop` replays the sequence every `loop` seconds, holding the open mark between runs.
    var ms = t * 1000;
    if (o.loop) ms = ms % (o.loop * 1000);
    if (ms < 0) ms = CE;
    var p = phase(ms), half = 12 * (20 / 24), u = Math.min(W, H) / 132, cx = W / 2 - 12 * u, cy = H / 2 + 12 * u;
    var col = o.color || '#111418', esc = o.escaped || '#2F6BFF', inner = o.inner || '#C5C9CE';
    var yaw = YAW * p.view, pitch = PITCH * p.view, dz = p.depth, cubies = [];
    for (var i = -1; i <= 1; i++) for (var j = -1; j <= 1; j++) for (var k = -1; k <= 1; k++) {
      var pos = [i, j, k], R = null, isEsc = i === 1 && j === -1 && k === 1;
      for (var m = 0; m < 3; m++) {
        var mv = MOVES[m], f = p.twists[m];
        if (!f || Math.round(pos[AX[mv[0]]]) !== mv[1]) continue;
        var ang = mv[2] * f * Math.PI / 2;
        if (f >= 1) pos = rot(mv[0], ang, pos).map(Math.round); else { pos = rot(mv[0], ang, pos); R = [mv[0], ang]; }
      }
      var c3 = [pos[0] * 24, pos[1] * 24, pos[2] * 24 * dz];
      if (isEsc && p.out) { c3[0] += 24 * p.out; c3[1] -= 24 * p.out; }
      cubies.push({ c: c3, R: R, esc: isEsc, k: k, pos: pos });
    }
    var hz = half * Math.max(dz, 0.001);
    function view(v) { return rot('x', pitch, rot('y', yaw, v)); }
    function place(cb, v) { var w = cb.R ? rot(cb.R[0], cb.R[1], v) : v; return view([cb.c[0] + w[0], cb.c[1] + w[1], cb.c[2] + w[2]]); }
    // Order by the layer a cubie is in now, not the one it started in: the twists bring two back cubies to
    // the front, and keyed by their old layer they were painted over it as the cube flattened. A flat block
    // is the front layer alone, so the cell the escaped square leaves stays empty, as in the mark.
    if (dz < 1e-3) cubies = cubies.filter(function (cb) { return Math.round(cb.pos[2]) === 1; });
    cubies.forEach(function (cb) { cb.z = view(cb.c)[2] + (cb.esc ? 0.01 : 0) + cb.pos[2] * 1e-4; });
    cubies.sort(function (a, b) { return a.z - b.z; });
    var FACES = [[0, 0, 1], [0, 0, -1], [1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0]];
    cubies.forEach(function (cb) {
      for (var fi = 0; fi < 6; fi++) {
        var N = FACES[fi], nrm = view(cb.R ? rot(cb.R[0], cb.R[1], N) : N), fc = place(cb, [N[0] * half, N[1] * half, N[2] * hz]);
        if (nrm[0] * -fc[0] + nrm[1] * -fc[1] + nrm[2] * (CAM - fc[2]) <= 1e-6) continue;
        var a1 = N[0] ? 1 : 0, a2 = N[2] ? 1 : 2, corners = [[-1, -1], [1, -1], [1, 1], [-1, 1]];
        ctx.beginPath();
        for (var q = 0; q < 4; q++) {
          var v = [N[0] * half, N[1] * half, N[2] * hz];
          v[a1] = corners[q][0] * (a1 === 2 ? hz : half);
          v[a2] = corners[q][1] * (a2 === 2 ? hz : half);
          var w = place(cb, v), persp = CAM / (CAM - w[2]), X = cx + w[0] * persp * u, Y = cy + w[1] * persp * u;
          if (q) ctx.lineTo(X, Y); else ctx.moveTo(X, Y);
        }
        ctx.closePath();
        var nw = cb.R ? rot(cb.R[0], cb.R[1], N) : N, outer = cb.pos[0] * nw[0] + cb.pos[1] * nw[1] + cb.pos[2] * nw[2] > 0.5;
        ctx.fillStyle = outer ? (cb.esc ? esc : col) : inner;
        ctx.fill();
      }
    });
  }
  cube.DURATION = CE / 1000;

  window.nekoVideoPaint = {
    field: field, squares: squares, mosaic: mosaic, lattice: lattice, dotChart: dotChart,
    stepGrid: stepGrid, joinGrid: joinGrid, intro: intro, cube: cube
  };
})();
