import React, { useState, useMemo, useEffect, useRef, useCallback } from "react";

/* ============================================================================
   FoilDSL Explorer · v3
   Stations measured from the center line · every edge and law is a curve ·
   sections carry thickness AND where that thickness peaks.

   What changed from v2, and why:
   - Spanwise position is a physical distance from the center (mm), not a
     normalized fraction. Out-of-plane shape is authored as drop / rise from 0.
   - Where the foil turns down, bottoms out, comes back through 0 or lifts is
     computed and reported as "distance from center + magnitude".
   - Leading and trailing edges are independent anchor-spline curves; chord,
     spine and taper (c_tip / c_center) are derived from them.
   - A section is (thickness, max-thickness position, camber, max-camber
     position); the foil carries a schedule of sections that blend out the span.
============================================================================ */

const C = {
  cream: "#F4EEE2", cream2: "#EBE3D2", panel: "#EFE8D8", panel2: "#E7DFCC",
  ink: "#211D17", inks: "#4A4338", mute: "#857A66",
  copper: "#B0703C", copperD: "#8A5326", copperL: "#C98A52",
  sage: "#5E6B55", slate: "#46596B", rust: "#9E4A33", teal: "#3E6B66",
  rule: "#C7BCA3", hair: "#D8CEB8",
};
const FONT = {
  serif: "'IBM Plex Serif', Georgia, serif",
  sans: "'IBM Plex Sans', system-ui, -apple-system, sans-serif",
  mono: "'IBM Plex Mono', ui-monospace, monospace",
};
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));
const f1 = (v) => { const r = Math.round(v * 10) / 10; return r === 0 ? "0" : String(r); };
const pctStr = (v) => `${Math.round(v * 1000) / 10}%`;
function lerpColor(a, b, t) {
  const pa = [1, 3, 5].map((i) => parseInt(a.slice(i, i + 2), 16));
  const pb = [1, 3, 5].map((i) => parseInt(b.slice(i, i + 2), 16));
  return "#" + pa.map((v, i) => Math.round(v + (pb[i] - v) * clamp(t, 0, 1)).toString(16).padStart(2, "0")).join("");
}
function niceTicks(lo, hi, n = 5) {
  const span = hi - lo || 1, raw = span / n, mag = Math.pow(10, Math.floor(Math.log10(raw))), r = raw / mag;
  const step = (r < 1.5 ? 1 : r < 3.5 ? 2 : r < 7.5 ? 5 : 10) * mag, out = [];
  for (let v = Math.ceil(lo / step) * step; v <= hi + 1e-9; v += step) out.push(+v.toFixed(6));
  return out;
}

/* ============================================================================
   ANCHOR-SPLINE — the one curve primitive (x = mm from center)
   linear   : C0, keeps corners
   smooth   : Catmull-Rom interpolating cubic, C1, may overshoot anchors
   monotone : Fritsch-Carlson, C1, never overshoots — every authored drop or
              rise IS the extreme of the curve
============================================================================ */
function hermite(xs, ys, m) {
  const n = xs.length;
  return (x) => {
    if (x <= xs[0]) return ys[0];
    if (x >= xs[n - 1]) return ys[n - 1];
    let i = 0; while (i < n - 2 && x > xs[i + 1]) i++;
    const h = xs[i + 1] - xs[i], t = (x - xs[i]) / h, t2 = t * t, t3 = t2 * t;
    return (2 * t3 - 3 * t2 + 1) * ys[i] + (t3 - 2 * t2 + t) * h * m[i]
      + (-2 * t3 + 3 * t2) * ys[i + 1] + (t3 - t2) * h * m[i + 1];
  };
}
function fcTangents(xs, ys) {
  const n = xs.length, d = [], m = [];
  for (let i = 0; i < n - 1; i++) d[i] = (ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]);
  m[0] = d[0]; m[n - 1] = d[n - 2];
  for (let i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;
  for (let i = 0; i < n - 1; i++) {
    if (d[i] === 0) { m[i] = 0; m[i + 1] = 0; continue; }
    const a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
    if (s > 9) { const t = 3 / Math.sqrt(s); m[i] = t * a * d[i]; m[i + 1] = t * b * d[i]; }
  }
  return m;
}
function makeSpline(anchors, mode) {
  const pts = [...anchors].sort((a, b) => a.x - b.x);
  const xs = pts.map((p) => p.x), ys = pts.map((p) => p.y), n = xs.length;
  if (n === 1) return () => ys[0];
  if (mode === "linear") {
    return (x) => {
      if (x <= xs[0]) return ys[0];
      if (x >= xs[n - 1]) return ys[n - 1];
      let i = 0; while (i < n - 2 && x > xs[i + 1]) i++;
      return ys[i] + (ys[i + 1] - ys[i]) * (x - xs[i]) / (xs[i + 1] - xs[i]);
    };
  }
  let m;
  if (mode === "monotone") m = fcTangents(xs, ys);
  else {
    m = [];
    for (let i = 0; i < n; i++) {
      if (i === 0) m[i] = (ys[1] - ys[0]) / (xs[1] - xs[0]);
      else if (i === n - 1) m[i] = (ys[n - 1] - ys[n - 2]) / (xs[n - 1] - xs[n - 2]);
      else m[i] = (ys[i + 1] - ys[i - 1]) / (xs[i + 1] - xs[i - 1]);
    }
  }
  return hermite(xs, ys, m);
}
function slopeAt(f, x, x0, x1) {
  const h = (x1 - x0) / 2000, a = Math.max(x0, x - h), b = Math.min(x1, x + h);
  return (f(b) - f(a)) / (b - a || 1);
}
// where the slope changes sign: a local low (min) or high (max)
function turningPoints(f, x0, x1, N = 400) {
  const dx = (x1 - x0) / N, out = [];
  let ps = 0, px = null;
  for (let i = 1; i < N; i++) {
    const x = x0 + dx * i, d = (f(x + dx) - f(x - dx)) / (2 * dx);
    const s = Math.abs(d) < 1e-6 ? 0 : Math.sign(d);
    if (s !== 0) {
      if (ps !== 0 && s !== ps) { const xz = (px + x) / 2; out.push({ x: xz, y: f(xz), kind: ps < 0 ? "min" : "max" }); }
      ps = s; px = x;
    }
  }
  return out;
}
// where the curve passes through 0
function zeroCrossings(f, x0, x1, N = 400) {
  const out = []; let ps = 0, px = 0, py = 0;
  for (let i = 0; i <= N; i++) {
    const x = x0 + (x1 - x0) * i / N, y = f(x), s = Math.abs(y) < 1e-6 ? 0 : Math.sign(y);
    if (s !== 0) {
      if (ps !== 0 && s !== ps) out.push({ x: px + (x - px) * (0 - py) / (y - py), dir: s > 0 ? "up" : "down" });
      ps = s; px = x; py = y;
    }
  }
  return out;
}
// where curvature flips: bowl (concave-up) <-> crest (concave-down)
function inflections(f, x0, x1, N = 400) {
  const dx = (x1 - x0) / N, d2 = [];
  for (let i = 1; i < N; i++) { const x = x0 + dx * i; d2[i] = (f(x + dx) - 2 * f(x) + f(x - dx)) / (dx * dx); }
  let mx = 0; for (let i = 1; i < N; i++) mx = Math.max(mx, Math.abs(d2[i]));
  const tol = mx * 1e-3 + 1e-12, raw = [];
  let ps = 0, pi = 0;
  for (let i = 2; i < N - 1; i++) {
    const s = Math.abs(d2[i]) < tol ? 0 : Math.sign(d2[i]);
    if (s !== 0) {
      if (ps !== 0 && s !== ps) raw.push({ x: x0 + dx * (pi + i) / 2, from: ps > 0 ? "bowl" : "crest", to: s > 0 ? "bowl" : "crest" });
      ps = s; pi = i;
    }
  }
  const out = [];
  for (const q of raw) if (!out.length || q.x - out[out.length - 1].x > (x1 - x0) * 0.02) out.push(q);
  return out;
}
function extrema(f, x0, x1, N = 400) {
  let mn = Infinity, mx = -Infinity, a = x0, b = x0;
  for (let i = 0; i <= N; i++) { const x = x0 + (x1 - x0) * i / N, y = f(x); if (y < mn) { mn = y; a = x; } if (y > mx) { mx = y; b = x; } }
  return { min: mn, minAt: a, max: mx, maxAt: b };
}
// contiguous stretches that rise (dihedral), fall (anhedral) or stay level
function slopeSegments(f, x0, x1, N = 300) {
  const tol = 0.0035, segs = [];
  let cur = null;
  for (let i = 0; i < N; i++) {
    const xa = x0 + (x1 - x0) * i / N, xb = x0 + (x1 - x0) * (i + 1) / N;
    const s = (f(xb) - f(xa)) / (xb - xa), g = Math.atan(s) * 180 / Math.PI;
    const sense = s > tol ? "up" : s < -tol ? "down" : "level";
    if (!cur || cur.sense !== sense) { if (cur) segs.push(cur); cur = { sense, x0: xa, x1: xb, gMin: g, gMax: g, z0: f(xa), z1: f(xb) }; }
    else { cur.x1 = xb; cur.z1 = f(xb); cur.gMin = Math.min(cur.gMin, g); cur.gMax = Math.max(cur.gMax, g); }
  }
  if (cur) segs.push(cur);
  const minW = (x1 - x0) * 0.015, merged = [];
  for (const s of segs) {
    const p = merged[merged.length - 1];
    if (p && (s.x1 - s.x0) < minW) { p.x1 = s.x1; p.z1 = s.z1; }
    else if (p && p.sense === s.sense) { p.x1 = s.x1; p.z1 = s.z1; p.gMin = Math.min(p.gMin, s.gMin); p.gMax = Math.max(p.gMax, s.gMax); }
    else merged.push({ ...s });
  }
  return merged;
}

/* ============================================================================
   SECTIONS — thickness t at chordwise position xt, camber m at position xc.
   The base thickness family is stretched so its maximum lands exactly at xt;
   because the base has zero slope at its own maximum, the stretch stays C1.
============================================================================ */
const BASE = {
  naca: (u) => 0.2969 * Math.sqrt(u) - 0.1260 * u - 0.3516 * u * u + 0.2843 * u ** 3 - 0.1036 * u ** 4,
  cst: (u) => Math.sqrt(u) * (1 - u) * (1 + 0.35 * u),
};
const PEAK = {};
for (const k of Object.keys(BASE)) {
  let best = 0, at = 0;
  for (let i = 1; i < 4000; i++) { const u = i / 4000, v = BASE[k](u); if (v > best) { best = v; at = u; } }
  PEAK[k] = { u: at, v: best };
}
function thicknessFn(family, t, xt) {
  const base = BASE[family] || BASE.naca, { u: us, v: pv } = PEAK[family] || PEAK.naca;
  const X = clamp(xt, 0.1, 0.7);
  return (x) => {
    if (x <= 0 || x >= 1) return 0;
    const u = x <= X ? us * x / X : us + (1 - us) * (x - X) / (1 - X);
    return (t / 2) * Math.max(0, base(u)) / pv;
  };
}
function camberFn(m, p) {
  if (!(m > 0)) return () => ({ yc: 0, dy: 0 });
  const P = clamp(p, 0.15, 0.85);
  return (x) => x < P
    ? { yc: m / (P * P) * (2 * P * x - x * x), dy: 2 * m / (P * P) * (P - x) }
    : { yc: m / ((1 - P) ** 2) * ((1 - 2 * P) + 2 * P * x - x * x), dy: 2 * m / ((1 - P) ** 2) * (P - x) };
}
function profile(spec, n = 90) {
  const th = thicknessFn(spec.family, spec.t, spec.xt), cf = camberFn(spec.cam, spec.xc);
  const up = [], lo = [], cm = [];
  for (let i = 0; i <= n; i++) {
    const x = 0.5 * (1 - Math.cos(Math.PI * i / n)), yt = th(x), { yc, dy } = cf(x), a = Math.atan(dy);
    up.push([x - yt * Math.sin(a), yc + yt * Math.cos(a)]);
    lo.push([x + yt * Math.sin(a), yc - yt * Math.cos(a)]);
    cm.push([x, yc]);
  }
  return { up, lo, cm, loop: up.concat(lo.slice().reverse()) };
}
// NACA 4-digit MPTT, optional modifier -IT (I = LE radius index, T = max-thickness position in tenths)
function nacaSpec(d, mod) {
  return { src: "naca", digits: d, mod: mod || null, family: "naca",
    t: +d.slice(2) / 100, xt: mod ? +mod[1] / 10 : 0.3, cam: +d[0] / 100, xc: +d[1] / 10 || 0.4 };
}
function blendSection(secs, y) {
  const st = secs.stations;
  if (st.length === 1) return st[0].spec;
  let i = 0; while (i < st.length - 2 && y > st[i + 1].y) i++;
  const a = st[i].spec, b = st[i + 1].spec;
  let w = clamp((y - st[i].y) / ((st[i + 1].y - st[i].y) || 1), 0, 1);
  if (secs.blend === "cosine") w = 0.5 * (1 - Math.cos(Math.PI * w));
  const L = (p, q) => p + (q - p) * w;
  const xa = a.cam > 0 ? a.xc : b.xc, xb = b.cam > 0 ? b.xc : a.xc;
  return { family: w < 0.5 ? a.family : b.family, t: L(a.t, b.t), xt: L(a.xt, b.xt), cam: L(a.cam, b.cam), xc: L(xa, xb) };
}

/* ============================================================================
   GEOMETRY — everything derived from the curves (half span, mirrored)
============================================================================ */
function deriveGeom(M) {
  const b = M.span, hs = b / 2;
  const fLE = makeSpline(M.leading.anchors, M.leading.mode);
  const fTE = makeSpline(M.trailing.anchors, M.trailing.mode);
  const fZ = M.dihedral ? makeSpline(M.dihedral.anchors, M.dihedral.mode) : () => 0;
  const fTw = M.twist ? makeSpline(M.twist.anchors, M.twist.mode) : () => 0;
  const chord = (y) => fTE(y) - fLE(y);
  const quarter = (y) => fLE(y) + 0.25 * chord(y);
  const dz = (y) => slopeAt(fZ, y, 0, hs);
  const N = 360; let I1 = 0, I2 = 0, Id = 0;
  for (let i = 0; i <= N; i++) {
    const y = hs * i / N, w = (i === 0 || i === N) ? 0.5 : 1, c = Math.max(0, chord(y));
    I1 += w * c; I2 += w * c * c; Id += w * c * Math.sqrt(1 + dz(y) ** 2);
  }
  const dy = hs / N; I1 *= dy; I2 *= dy; Id *= dy;
  const S = 2 * I1, MAC = 2 * I2 / S, AR = b * b / S, Sdev = 2 * Id;
  const c0 = chord(0), ct = chord(hs);
  const ex = extrema(fZ, 0, hs);
  const secAt = (y) => blendSection(M.sections, y);
  const linearZ = M.dihedral && M.dihedral.mode === "linear";
  return {
    b, hs, fLE, fTE, fZ, fTw, chord, quarter, dz, S, MAC, AR, Sdev, c0, ct, taper: ct / c0,
    tipZ: fZ(hs), maxDrop: Math.max(0, -ex.min), maxDropAt: ex.minAt, maxRise: Math.max(0, ex.max), maxRiseAt: ex.maxAt,
    turns: turningPoints(fZ, 0, hs), zeros: zeroCrossings(fZ, 0, hs),
    infl: M.dihedral && !linearZ ? inflections(fZ, 0, hs) : [],
    kinks: linearZ ? M.dihedral.anchors.slice(1, -1).map((a) => a.x) : [],
    segs: slopeSegments(fZ, 0, hs), secAt,
    rootThick: c0 * secAt(0).t, tipThick: ct * secAt(hs).t,
    sweep: (f, y) => Math.atan(slopeAt(f, y, 0, hs)) * 180 / Math.PI,
    dih: (y) => Math.atan(dz(y)) * 180 / Math.PI,
  };
}

const showLen = (v) => `${v.toFixed(1)} mm`;
const METRICS = {
  area: { dim: "area", get: (g) => g.S / 100, show: (v) => `${v.toFixed(0)} cm²` },
  dev_area: { dim: "area", get: (g) => g.Sdev / 100, show: (v) => `${v.toFixed(0)} cm²` },
  aspect: { dim: "none", get: (g) => g.AR, show: (v) => v.toFixed(2) },
  taper: { dim: "none", get: (g) => g.taper, show: (v) => v.toFixed(3) },
  mac: { dim: "len", get: (g) => g.MAC },
  center_chord: { dim: "len", get: (g) => g.c0 },
  tip_chord: { dim: "len", get: (g) => g.ct },
  tip_rise: { dim: "len", get: (g) => g.tipZ },
  max_drop: { dim: "len", get: (g) => g.maxDrop },
  max_rise: { dim: "len", get: (g) => g.maxRise },
  root_thick: { dim: "len", get: (g) => g.rootThick },
  tip_thick: { dim: "len", get: (g) => g.tipThick },
};
function evalConstraints(M, g) {
  return M.constraints.map((c) => {
    const d = METRICS[c.metric], v = d.get(g), show = d.show || showLen;
    let ok;
    if (c.relop === ">=") ok = v >= c.value - 1e-9;
    else if (c.relop === "<=") ok = v <= c.value + 1e-9;
    else if (c.relop === "==") ok = Math.abs(v - c.value) <= 1e-6 * Math.max(1, Math.abs(c.value));
    else ok = Math.abs(v - c.value) <= 0.05 * Math.abs(c.value || 1);
    return { ...c, actual: v, label: show(v), ok };
  });
}

/* ============================================================================
   FoilDSL v3 — tokenizer, recursive-descent parser, canonical emitter
============================================================================ */
const LEN = { mm: 1, cm: 10, m: 1000 };
const AREA = { mm2: 0.01, cm2: 1, m2: 10000 };
const MODES = ["linear", "smooth", "monotone"];

function tokenize(src) {
  const re = /\s+|#[^\n]*|"[^"\n]*"|>=|<=|==|~|[{}()\[\]:,%]|[A-Za-z_][A-Za-z0-9_]*|[+-]?\d+(?:\.\d+)?|[+-]?\.\d+/y;
  const toks = []; let i = 0, line = 1;
  while (i < src.length) {
    re.lastIndex = i;
    const m = re.exec(src);
    if (!m) throw { line, msg: `unexpected character "${src[i]}"` };
    const s = m[0];
    if (!/^\s/.test(s) && s[0] !== "#") {
      let kind = "sym";
      if (s[0] === '"') kind = "str";
      else if (/^[+-]?\.?\d/.test(s)) kind = "num";
      else if (/^[A-Za-z_]/.test(s)) kind = "word";
      toks.push({ s, kind, line });
    }
    for (let k = 0; k < s.length; k++) if (s[k] === "\n") line++;
    i = re.lastIndex;
  }
  toks.push({ s: "end of input", kind: "eof", line });
  return toks;
}

// chord-law sugar -> two edge curves about a (possibly swept) straight quarter-chord line
function chordSugar(s, hs) {
  const c0 = s.c0, ct = s.ct != null ? s.ct : s.taper * s.c0;
  const fr = s.law === "elliptic" ? [0, 0.3, 0.55, 0.75, 0.88, 0.96, 1] : [0, 1];
  const cAt = (e) => s.law === "elliptic" ? ct + (c0 - ct) * Math.sqrt(Math.max(0, 1 - e * e)) : c0 + (ct - c0) * e;
  const tan = Math.tan((s.sweep || 0) * Math.PI / 180), LE = [], TE = [];
  for (const e of fr) {
    const y = e * hs, c = cAt(e), q = 0.25 * c0 + tan * y;
    LE.push({ x: +y.toFixed(1), y: +(q - 0.25 * c).toFixed(1) });
    TE.push({ x: +y.toFixed(1), y: +(q + 0.75 * c).toFixed(1) });
  }
  LE[LE.length - 1].x = hs; TE[TE.length - 1].x = hs;
  const mode = s.law === "elliptic" ? "smooth" : "linear";
  return { leading: { mode, anchors: LE }, trailing: { mode, anchors: TE } };
}

function parseFoil(src) {
  const T = tokenize(src); let p = 0;
  const pk = () => T[p], at = (s) => T[p].s === s, nx = () => T[p++];
  const fail = (msg, tok = T[p]) => { throw { line: tok.line, msg }; };
  const eat = (s) => (T[p].s === s ? T[p++] : fail(`expected "${s}" but found "${T[p].s}"`));
  const word = (what = "a name") => (T[p].kind === "word" ? T[p++].s : fail(`expected ${what}, found "${T[p].s}"`));
  const num = (what = "a number") => (T[p].kind === "num" ? parseFloat(T[p++].s) : fail(`expected ${what}, found "${T[p].s}"`));
  const str = () => (T[p].kind === "str" ? T[p++].s.slice(1, -1) : fail(`expected a quoted string, found "${T[p].s}"`));
  const notEof = (what) => { if (pk().kind === "eof") fail(`unterminated ${what} (missing "}")`); };
  const len = () => { const v = num("a length"); return LEN[T[p].s] != null ? v * LEN[nx().s] : v; };
  const frac = (what) => {
    const tok = T[p], v = num(what);
    if (at("%")) { nx(); return v / 100; }
    if (Math.abs(v) > 1) fail(`${what} ${v} needs a % sign (or write it as a fraction ≤ 1)`, tok);
    return v;
  };
  const station = () => {
    const tok = T[p];
    if (at("center") || at("root")) { nx(); return { k: "mm", v: 0, tok }; }
    if (at("tip")) { nx(); return { k: "tip", tok }; }
    const v = num("a station (center, tip, or a distance from center)");
    if (at("%")) { nx(); return { k: "frac", v: v / 100, tok }; }
    if (LEN[T[p].s] != null) return { k: "mm", v: v * LEN[nx().s], tok };
    return { k: "mm", v, tok };
  };
  const value = (kind) => {
    if (kind === "z") {
      if (at("level")) { nx(); return 0; }
      if (at("drop")) { nx(); return -Math.abs(len()); }
      if (at("rise") || at("raise")) { nx(); return Math.abs(len()); }
      if (at("z")) nx();
      return len();
    }
    if (kind === "x") { if (at("x")) nx(); return len(); }
    const v = num("an angle"); if (at("deg")) nx(); return v;
  };
  const curve = (what, kind, extra) => {
    const tok = T[p], mode = word("an interpolation mode (linear, smooth or monotone)");
    if (!MODES.includes(mode)) fail(`unknown curve mode "${mode}" — use linear, smooth or monotone`, tok);
    eat("{"); const pts = [];
    while (!at("}")) { notEof(what); if (extra && extra()) continue; eat("at"); const st = station(); pts.push({ st, y: value(kind) }); }
    eat("}");
    if (pts.length < 2) fail(`${what} needs at least two stations (center and tip)`, tok);
    return { mode, pts, tok, what };
  };
  const sectionSource = () => {
    const tok = T[p], kind = word("a section source");
    if (kind === "naca") {
      const d = T[p];
      if (d.kind !== "num" || !/^\d{4}$/.test(d.s)) fail(`expected a 4-digit NACA designation, found "${d.s}"`, d);
      nx(); let mod = null;
      if (T[p].kind === "num" && /^-\d{2}$/.test(T[p].s)) mod = nx().s.slice(1);
      return nacaSpec(d.s, mod);
    }
    if (kind === "profile") {
      eat("{");
      const sp = { src: "profile", family: "naca", t: null, xt: 0.3, cam: 0, xc: 0.4 };
      while (!at("}")) {
        notEof("profile block");
        const t = T[p], k = word("thickness, camber or family");
        if (k === "family") { const f = word("naca or cst"); if (!BASE[f]) fail(`unknown family "${f}" — use naca or cst`, t); sp.family = f; }
        else if (k === "thickness") { sp.t = frac("thickness"); if (at("at")) { nx(); sp.xt = frac("max-thickness position"); } }
        else if (k === "camber") { sp.cam = frac("camber"); if (at("at")) { nx(); sp.xc = frac("max-camber position"); } }
        else fail(`unknown profile field "${k}" — use thickness, camber or family`, t);
      }
      eat("}");
      if (sp.t == null) fail("a profile needs a thickness, e.g. thickness 10% at 33%", tok);
      return sp;
    }
    if (kind === "eppler") return { src: "eppler", name: word("an Eppler designation"), family: "naca", t: 0.1, xt: 0.3, cam: 0.02, xc: 0.4, nominal: true };
    if (kind === "file") return { src: "file", name: str(), family: "naca", t: 0.1, xt: 0.3, cam: 0.02, xc: 0.4, nominal: true };
    return fail(`unknown section source "${kind}" — use naca, profile, eppler or file`, tok);
  };

  const t0 = eat("foil"); const name = str(); eat("{");
  const M = { name, span: null, context: {}, leading: null, trailing: null, dihedral: null, twist: null, sections: null,
    loft: { degree: 3, stations: 21, stationSpacing: "cosine", points: 81, pointSpacing: "cosine" }, constraints: [] };
  const raw = { leading: null, trailing: null, dihedral: null, twist: null, sugar: null, sections: null, bank: true };
  let planTok = t0;

  while (!at("}")) {
    notEof("foil block");
    const tok = T[p], kw = word("a statement");
    if (kw === "units") { const u = word("a unit"); if (u !== "mm") fail(`only "units mm" is supported (individual lengths may still say cm or m)`, tok); }
    else if (kw === "span") { M.span = len(); if (!(M.span > 0)) fail("span must be positive", tok); }
    else if (kw === "context") {
      eat("{");
      while (!at("}")) { notEof("context block"); const k = word(); const v = num(); const u = word("a unit"); M.context[k] = { v, u }; }
      eat("}");
    }
    else if (kw === "planform") {
      planTok = tok; eat("{");
      while (!at("}")) {
        notEof("planform block");
        const t = T[p], k = word("leading, trailing or chord");
        if (k === "leading") raw.leading = curve("the leading edge", "x");
        else if (k === "trailing") raw.trailing = curve("the trailing edge", "x");
        else if (k === "chord") {
          const law = word("a chord law");
          if (!["elliptic", "linear"].includes(law)) fail(`unknown chord law "${law}" — use elliptic or linear`, t);
          if (!(at("center") || at("root"))) fail(`expected "center" after "chord ${law}"`);
          nx();
          const s = { law, c0: len() };
          if (at("tip")) { nx(); s.ct = len(); }
          else if (at("taper")) { nx(); s.taper = num("a taper ratio"); }
          else fail(`"chord ${law}" needs "tip <length>" or "taper <ratio>"`);
          if (at("sweep")) { nx(); s.sweep = num("a sweep angle"); eat("deg"); }
          raw.sugar = s;
        }
        else fail(`unknown planform statement "${k}"`, t);
      }
      eat("}");
    }
    else if (kw === "dihedral") {
      raw.dihedral = curve("the dihedral curve", "z", () => {
        if (!at("bank")) return false;
        nx(); const b = word("true or false");
        if (b !== "true" && b !== "false") fail("bank must be true or false");
        raw.bank = b === "true"; return true;
      });
    }
    else if (kw === "twist") raw.twist = curve("the twist curve", "deg");
    else if (kw === "sections") {
      const blend = (at("linear") || at("cosine")) ? nx().s : "cosine";
      eat("{"); const list = [];
      while (!at("}")) { notEof("sections block"); eat("at"); const st = station(); list.push({ st, spec: sectionSource() }); }
      eat("}"); raw.sections = { blend, list, tok };
    }
    else if (kw === "loft") {
      eat("{");
      while (!at("}")) {
        notEof("loft block");
        const t = T[p], k = word();
        if (k === "surface") { eat("bspline"); eat("degree"); M.loft.degree = num(); }
        else if (k === "stations" || k === "points") {
          const n = num(), sp = word("uniform or cosine");
          if (!["uniform", "cosine"].includes(sp)) fail("spacing must be uniform or cosine", t);
          if (k === "stations") { M.loft.stations = n; M.loft.stationSpacing = sp; } else { M.loft.points = n; M.loft.pointSpacing = sp; }
        }
        else fail(`unknown loft statement "${k}"`, t);
      }
      eat("}");
    }
    else if (kw === "constrain") {
      eat("{");
      while (!at("}")) {
        notEof("constrain block");
        const t = T[p], metric = word("a metric"), d = METRICS[metric];
        if (!d) fail(`unknown metric "${metric}" — try ${Object.keys(METRICS).join(", ")}`, t);
        const r = T[p];
        if (![">=", "<=", "==", "~"].includes(r.s)) fail(`expected >=, <=, == or ~ after "${metric}"`, r);
        nx();
        const rawv = num(); let unit = null;
        if (LEN[T[p].s] != null || AREA[T[p].s] != null) unit = nx().s;
        let factor = 1;
        if (d.dim === "len") { if (unit && LEN[unit] == null) fail(`${metric} is a length; "${unit}" is not a length unit`, t); factor = LEN[unit || "mm"]; }
        else if (d.dim === "area") { if (unit && AREA[unit] == null) fail(`${metric} is an area; use cm2, mm2 or m2`, t); factor = AREA[unit || "cm2"]; }
        else if (unit) fail(`${metric} is dimensionless; drop the unit "${unit}"`, t);
        M.constraints.push({ metric, relop: r.s, raw: rawv, unit, value: rawv * factor });
      }
      eat("}");
    }
    else fail(`unknown statement "${kw}"`, tok);
  }
  eat("}");
  if (pk().kind !== "eof") fail(`unexpected "${pk().s}" after the foil block`);

  /* ---- static semantics: resolve stations, then check well-formedness ---- */
  if (M.span == null) fail(`the foil needs a span, e.g. "span 900 mm"`, t0);
  const hs = M.span / 2;
  const res = (st) => (st.k === "tip" ? hs : st.k === "frac" ? st.v * hs : st.v);
  const resolve = (c) => {
    const a = c.pts.map((q) => ({ x: res(q.st), y: q.y, tok: q.st.tok }));
    if (Math.abs(a[0].x) > 1e-9) fail(`${c.what} must start at the center`, a[0].tok);
    if (Math.abs(a[a.length - 1].x - hs) > 1e-6) fail(`${c.what} must end at the tip (${f1(hs)} mm from center)`, a[a.length - 1].tok);
    for (let i = 1; i < a.length; i++) if (a[i].x <= a[i - 1].x) fail(`stations in ${c.what} must move outward from the center`, a[i].tok);
    return { mode: c.mode, anchors: a.map(({ x, y }) => ({ x, y })) };
  };
  if (raw.sugar) { const e = chordSugar(raw.sugar, hs); M.leading = e.leading; M.trailing = e.trailing; }
  if (raw.leading) M.leading = resolve(raw.leading);
  if (raw.trailing) M.trailing = resolve(raw.trailing);
  if (!M.leading || !M.trailing) fail("the planform needs a leading and a trailing edge curve (or a chord law)", planTok);
  {
    const fL = makeSpline(M.leading.anchors, M.leading.mode), fT = makeSpline(M.trailing.anchors, M.trailing.mode);
    for (let i = 0; i <= 90; i++) {
      const y = hs * i / 90;
      if (fT(y) - fL(y) < 0.5) fail(`the trailing edge meets or crosses the leading edge ${f1(y)} mm from center`, planTok);
    }
  }
  if (raw.dihedral) M.dihedral = { ...resolve(raw.dihedral), bank: raw.bank };
  if (raw.twist) M.twist = resolve(raw.twist);
  if (!raw.sections) fail("the foil needs a sections block", t0);
  const sts = raw.sections.list.map((q) => ({ y: res(q.st), spec: q.spec, tok: q.st.tok }));
  if (!sts.length) fail("the sections block needs at least one station", raw.sections.tok);
  if (Math.abs(sts[0].y) > 1e-9) fail("the first section must be at the center", sts[0].tok);
  if (sts.length > 1 && Math.abs(sts[sts.length - 1].y - hs) > 1e-6) fail(`the last section must be at the tip (${f1(hs)} mm from center)`, sts[sts.length - 1].tok);
  for (let i = 1; i < sts.length; i++) if (sts[i].y <= sts[i - 1].y) fail("section stations must move outward from the center", sts[i].tok);
  for (const s of sts) {
    const q = s.spec, where = `section at ${f1(s.y)} mm`;
    if (!(q.t >= 0.01 && q.t <= 0.35)) fail(`${where}: thickness ${pctStr(q.t)} is outside 1%–35%`, s.tok);
    if (!(q.xt >= 0.15 && q.xt <= 0.65)) fail(`${where}: max-thickness position ${pctStr(q.xt)} is outside 15%–65% of chord`, s.tok);
    if (!(q.cam >= 0 && q.cam <= 0.09)) fail(`${where}: camber ${pctStr(q.cam)} is outside 0%–9%`, s.tok);
    if (q.cam > 0 && !(q.xc >= 0.15 && q.xc <= 0.85)) fail(`${where}: max-camber position ${pctStr(q.xc)} is outside 15%–85%`, s.tok);
  }
  M.sections = { blend: raw.sections.blend, stations: sts.map(({ y, spec }) => ({ y, spec })) };
  if (M.loft.degree < 1 || M.loft.degree >= M.loft.stations) fail("B-spline degree must be at least 1 and less than the station count", t0);
  return M;
}

/* ---- canonical emitter: canon(F). UI edits write the program through this ---- */
const padR = (s, n) => s + " ".repeat(Math.max(1, n - s.length));
function stStr(y, hs) {
  if (Math.abs(y) < 1e-6) return "center";
  if (Math.abs(y - hs) < 1e-6) return "tip";
  return `${f1(y)} mm`;
}
function valStr(v, kind) {
  if (kind === "x") return `x ${f1(v)}`;
  if (kind === "deg") return `${f1(v)} deg`;
  if (Math.abs(v) < 0.05) return "level";
  return v < 0 ? `drop ${f1(-v)} mm` : `rise ${f1(v)} mm`;
}
function specStr(s) {
  if (s.src === "naca") return `naca ${s.digits}${s.mod ? "-" + s.mod : ""}`;
  if (s.src === "eppler") return `eppler ${s.name}`;
  if (s.src === "file") return `file "${s.name}"`;
  return `profile { ${s.family !== "naca" ? `family ${s.family}  ` : ""}thickness ${pctStr(s.t)} at ${pctStr(s.xt)}  camber ${pctStr(s.cam)}${s.cam > 0 ? ` at ${pctStr(s.xc)}` : ""} }`;
}
function emitDSL(M) {
  const hs = M.span / 2, L = [];
  const curve = (head, c, kind, ind, extra = []) => {
    L.push(`${ind}${head} ${c.mode} {`);
    for (const a of c.anchors) L.push(`${ind}  at ${padR(stStr(a.x, hs), 10)} ${valStr(a.y, kind)}`);
    for (const e of extra) L.push(`${ind}  ${e}`);
    L.push(`${ind}}`);
  };
  L.push(`foil "${M.name}" {`, "  units mm", `  span  ${f1(M.span)} mm`);
  const ctx = Object.entries(M.context || {});
  if (ctx.length) L.push(`  context { ${ctx.map(([k, v]) => `${k} ${f1(v.v)} ${v.u}`).join("  ")} }`);
  L.push("", "  planform {");
  curve("leading", M.leading, "x", "    ");
  curve("trailing", M.trailing, "x", "    ");
  L.push("  }");
  if (M.dihedral) { L.push(""); curve("dihedral", M.dihedral, "z", "  ", [`bank ${M.dihedral.bank ? "true" : "false"}`]); }
  if (M.twist) { L.push(""); curve("twist", M.twist, "deg", "  "); }
  L.push("", `  sections ${M.sections.blend} {`);
  for (const s of M.sections.stations) L.push(`    at ${padR(stStr(s.y, hs), 10)} ${specStr(s.spec)}`);
  L.push("  }");
  const lf = M.loft;
  L.push("", "  loft {", `    surface  bspline degree ${lf.degree}`, `    stations ${lf.stations} ${lf.stationSpacing}`, `    points   ${lf.points} ${lf.pointSpacing}`, "  }");
  if (M.constraints.length) {
    L.push("", "  constrain {");
    for (const c of M.constraints) L.push(`    ${padR(c.metric, 12)}${padR(c.relop, 3)}${c.raw}${c.unit ? " " + c.unit : ""}`);
    L.push("  }");
  }
  L.push("}");
  return L.join("\n");
}
function fnv1a(s) {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 0x01000193) >>> 0; }
  return h.toString(16).padStart(8, "0");
}

const DEFAULT_DSL = `foil "gull-90" {
  units mm
  span  900 mm
  context { rider_mass 90 kg }

  # Stations are distances out from the center line (0) to the tip.
  # Edge positions are mm aft of the center leading edge.
  planform {
    leading smooth {
      at center   x 0
      at 225 mm   x 14
      at tip      x 96            # LE sweeps aft, progressively
    }
    trailing monotone {
      at center   x 132
      at 225 mm   x 116           # TE sweeps forward ...
      at tip      x 122           # ... then back: a crescent
    }
  }

  # Out-of-plane: drop / rise measured from 0 at the center.
  # monotone = each authored drop or rise is exactly the extreme.
  dihedral monotone {
    at center   level
    at 135 mm   drop 16 mm        # bottoms out here, turns back up
    at 270 mm   level             # comes back through 0
    at tip      rise 46 mm        # tip lift
    bank true
  }

  twist linear {
    at center   0 deg
    at tip      -2 deg            # washout
  }

  # Each section: thickness AND where it peaks; camber AND where it peaks.
  sections cosine {
    at center   naca 4412         # 12% at 30%, camber 4% at 40%
    at 250 mm   profile { thickness 10% at 33%  camber 2% at 40% }
    at tip      profile { thickness 8% at 30%  camber 0% }
  }

  loft {
    surface  bspline degree 3
    stations 21 cosine
    points   81 cosine
  }

  constrain {
    area      >= 740 cm2
    aspect    ~  9.5
    taper     <= 0.25
    max_drop  <= 20 mm
    tip_rise  >= 30 mm
    tip_thick >= 1.2 mm
  }
}`;

/* ============================================================================
   DRAWING COMPONENTS
============================================================================ */
const SENSE_COLOR = { up: C.teal, down: C.rust, level: C.mute };
const senseOf = (s) => (s > 0.0035 ? "up" : s < -0.0035 ? "down" : "level");

function Slider({ label, value, onChange, min, max, step, show, disabled }) {
  return (
    <label style={{ display: "block", marginBottom: 10, opacity: disabled ? 0.45 : 1 }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", marginBottom: 3 }}>
        <span style={{ fontFamily: FONT.sans, fontSize: 11.5, color: C.inks }}>{label}</span>
        <span style={{ fontFamily: FONT.mono, fontSize: 12, color: C.copperD, fontWeight: 600 }}>{show ? show(value) : value}</span>
      </div>
      <input className="fd-range" type="range" min={min} max={max} step={step} value={value} disabled={disabled}
        onChange={(e) => onChange(parseFloat(e.target.value))} style={{ width: "100%" }} />
    </label>
  );
}

/* Draggable multi-curve editor. x = mm from center, 0 .. half span. */
function CurveEditor({ curves, onChange, x1, range, height = 240, yUp = true, yLabel = "", fill = null, zero = false, snapY = 0.5, ariaLabel = "curve editor" }) {
  const W = 560, H = height, PL = 46, PR = 16, PT = 22, PB = 34;
  const svgRef = useRef(null), frozen = useRef(null);
  const [drag, setDrag] = useState(null);
  const S = curves.map((c) => ({ ...c, f: makeSpline(c.anchors, c.mode) }));
  let lo = range[0], hi = range[1];
  if (drag && frozen.current) [lo, hi] = frozen.current;
  else {
    for (const c of S) for (let i = 0; i <= 100; i++) { const v = c.f(x1 * i / 100); lo = Math.min(lo, v); hi = Math.max(hi, v); }
    const m = (hi - lo) * 0.05; lo -= m; hi += m;
  }
  const iw = W - PL - PR, ih = H - PT - PB;
  const px = (x) => PL + (x / x1) * iw;
  const py = (y) => (yUp ? PT + (hi - y) / (hi - lo) * ih : PT + (y - lo) / (hi - lo) * ih);
  const ix = (sx) => (sx - PL) / iw * x1;
  const iy = (sy) => (yUp ? hi - (sy - PT) / ih * (hi - lo) : lo + (sy - PT) / ih * (hi - lo));
  const local = (e) => {
    const svg = svgRef.current; if (!svg) return [0, 0];
    const m = svg.getScreenCTM(); if (!m) return [0, 0];
    const pt = svg.createSVGPoint(); pt.x = e.clientX; pt.y = e.clientY;
    const q = pt.matrixTransform(m.inverse()); return [q.x, q.y];
  };
  const move = (key, i, nx, ny) => {
    const c = curves.find((k) => k.key === key), a = c.anchors, end = i === 0 || i === a.length - 1, gap = x1 * 0.02;
    const x = end ? a[i].x : Math.round(clamp(nx, a[i - 1].x + gap, a[i + 1].x - gap));
    const y = Math.round(clamp(ny, lo, hi) / snapY) * snapY;
    onChange(key, a.map((q, j) => (j === i ? { x, y: +y.toFixed(2) } : q)));
  };
  const path = (f) => { let d = ""; for (let i = 0; i <= 160; i++) { const x = x1 * i / 160; d += `${i ? "L" : "M"}${px(x).toFixed(1)},${py(f(x)).toFixed(1)}`; } return d; };
  const fillD = fill ? (() => {
    const A = S.find((c) => c.key === fill[0]).f, B = S.find((c) => c.key === fill[1]).f; let d = "";
    for (let i = 0; i <= 120; i++) { const x = x1 * i / 120; d += `${i ? "L" : "M"}${px(x).toFixed(1)},${py(A(x)).toFixed(1)}`; }
    for (let i = 120; i >= 0; i--) { const x = x1 * i / 120; d += `L${px(x).toFixed(1)},${py(B(x)).toFixed(1)}`; }
    return d + "Z";
  })() : null;
  const lab = (x) => clamp(x, PL + 34, W - PR - 34);
  return (
    <svg ref={svgRef} viewBox={`0 0 ${W} ${H}`} style={{ ...svgFluid, touchAction: "none", userSelect: "none" }} role="group" aria-label={ariaLabel}
      onPointerMove={(e) => { if (!drag) return; const [sx, sy] = local(e); move(drag.key, drag.i, ix(sx), iy(sy)); }}
      onPointerUp={() => setDrag(null)} onPointerCancel={() => setDrag(null)}>
      {niceTicks(lo, hi, 5).map((v) => (
        <g key={"y" + v}>
          <line x1={PL} x2={W - PR} y1={py(v)} y2={py(v)} stroke={C.hair} strokeWidth="0.6" />
          <text x={PL - 6} y={py(v) + 3} textAnchor="end" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>{f1(v)}</text>
        </g>
      ))}
      {niceTicks(0, x1, 6).map((v) => (
        <g key={"x" + v}>
          <line x1={px(v)} x2={px(v)} y1={PT} y2={H - PB} stroke={C.hair} strokeWidth="0.5" strokeDasharray="2 3" />
          <text x={px(v)} y={H - PB + 13} textAnchor="middle" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>{v}</text>
        </g>
      ))}
      <text x={px(x1)} y={PT - 7} textAnchor="end" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>tip {f1(x1)}</text>
      <text x={PL} y={PT - 7} fontSize="9" fill={C.mute} fontFamily={FONT.mono}>{yLabel}</text>
      <text x={W - PR} y={H - 5} textAnchor="end" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>mm from center →</text>
      {zero && lo < 0 && hi > 0 && <line x1={PL} x2={W - PR} y1={py(0)} y2={py(0)} stroke={C.ink} strokeOpacity="0.5" strokeWidth="1.1" />}
      {fillD && <path d={fillD} fill={C.copper} fillOpacity="0.14" />}
      {S.map((c) => {
        if (!c.sense) return <path key={c.key} d={path(c.f)} fill="none" stroke={c.color} strokeWidth="2.2" strokeLinejoin="round" />;
        const segs = [];
        for (let i = 0; i < 160; i++) {
          const xa = x1 * i / 160, xb = x1 * (i + 1) / 160, s = (c.f(xb) - c.f(xa)) / (xb - xa);
          segs.push(<line key={i} x1={px(xa)} y1={py(c.f(xa))} x2={px(xb)} y2={py(c.f(xb))} stroke={SENSE_COLOR[senseOf(s)]} strokeWidth="2.6" strokeLinecap="round" />);
        }
        return <g key={c.key}>{segs}</g>;
      })}
      {S.map((c) => {
        const marks = c.marks || [];
        const out = [];
        if (marks.includes("turns")) turningPoints(c.f, 0, x1).forEach((t, k) => {
          const cx = px(t.x), cy = py(t.y), s = 5.5, below = (t.kind === "min") === yUp;
          out.push(<g key={"t" + k}>
            <path d={`M${cx},${cy - s}L${cx + s},${cy}L${cx},${cy + s}L${cx - s},${cy}Z`} fill={t.kind === "min" ? C.rust : C.teal} stroke={C.cream} strokeWidth="1.2" />
            <text x={lab(cx)} y={cy + (below ? 18 : -10)} textAnchor="middle" fontSize="9" fill={C.ink} fontFamily={FONT.mono}>
              {c.turnLabel ? c.turnLabel(t) : `${t.kind === "min" ? "▼" : "▲"} ${f1(t.y)} @ ${Math.round(t.x)}`}</text>
          </g>);
        });
        if (marks.includes("zero")) zeroCrossings(c.f, 0, x1).forEach((z, k) => out.push(
          <g key={"z" + k}><circle cx={px(z.x)} cy={py(0)} r="4" fill={C.cream} stroke={C.ink} strokeWidth="1.2" />
            <text x={lab(px(z.x))} y={py(0) + (yUp ? -8 : 14)} textAnchor="middle" fontSize="8.5" fill={C.inks} fontFamily={FONT.mono}>0 @ {Math.round(z.x)}</text></g>));
        if (marks.includes("infl") && c.mode !== "linear") inflections(c.f, 0, x1).forEach((q, k) => out.push(
          <g key={"i" + k}><circle cx={px(q.x)} cy={py(c.f(q.x))} r="4.5" fill="none" stroke={C.copper} strokeWidth="1.5" strokeDasharray="2 1.5" />
            <line x1={px(q.x)} x2={px(q.x)} y1={H - PB} y2={H - PB - 6} stroke={C.copper} strokeWidth="1.4" /></g>));
        return <g key={"m" + c.key}>{out}</g>;
      })}
      {S.map((c) => c.anchors.map((a, i) => {
        const end = i === 0 || i === c.anchors.length - 1, active = drag && drag.key === c.key && drag.i === i;
        return (
          <circle key={c.key + i} className="fd-anchor" cx={px(a.x)} cy={py(a.y)} r={active ? 7.5 : 6} fill={c.anchorColor || c.color} stroke={C.cream} strokeWidth="2"
            tabIndex={0} style={{ cursor: end ? "ns-resize" : "grab" }}
            aria-label={`${c.label || c.key} anchor at ${Math.round(a.x)} mm from center, value ${f1(a.y)}`}
            onPointerDown={(e) => { e.preventDefault(); frozen.current = [lo, hi]; try { svgRef.current.setPointerCapture(e.pointerId); } catch (err) { /* ignore */ } setDrag({ key: c.key, i }); }}
            onKeyDown={(e) => {
              const k = e.shiftKey ? 10 : 1, dir = yUp ? 1 : -1;
              if (e.key === "ArrowUp") { move(c.key, i, a.x, a.y + dir * snapY * 2 * k); e.preventDefault(); }
              else if (e.key === "ArrowDown") { move(c.key, i, a.x, a.y - dir * snapY * 2 * k); e.preventDefault(); }
              else if (!end && e.key === "ArrowLeft") { move(c.key, i, a.x - 5 * k, a.y); e.preventDefault(); }
              else if (!end && e.key === "ArrowRight") { move(c.key, i, a.x + 5 * k, a.y); e.preventDefault(); }
            }} />
        );
      }))}
    </svg>
  );
}

/* Top view, mirrored to the full span. */
function PlanformView({ g, height = 180, stations = [] }) {
  const W = 560, H = height, pad = 16, N = 100, hs = g.hs;
  const le = [], te = [];
  for (let i = 0; i <= N; i++) { const y = hs * i / N; le.push([y, g.fLE(y)]); te.push([y, g.fTE(y)]); }
  const mir = (a) => a.map(([y, x]) => [-y, x]);
  const outline = [...mir(le).reverse(), ...le, ...te.slice().reverse(), ...mir(te)];
  const xs = outline.map((q) => q[1]), xmin = Math.min(...xs), xmax = Math.max(...xs);
  const avail = H - 2 * pad - 16;
  const sc = Math.min((W - 2 * pad) / (2 * hs), avail / ((xmax - xmin) || 1));
  const ox = W / 2, oy = pad + 14 + (avail - sc * (xmax - xmin)) / 2;
  const P = (y, x) => [ox + y * sc, oy + (x - xmin) * sc];
  const toD = (pts) => pts.map(([y, x], i) => { const [a, b] = P(y, x); return `${i ? "L" : "M"}${a.toFixed(1)},${b.toFixed(1)}`; }).join("");
  const q = [], qm = [];
  for (let i = 0; i <= N; i++) { const y = hs * i / N; q.push([y, g.quarter(y)]); qm.push([-y, g.quarter(y)]); }
  const [cx, cyL] = P(0, g.fLE(0)), [, cyT] = P(0, g.fTE(0));
  const [tx, tyL] = P(hs, g.fLE(hs)), [, tyT] = P(hs, g.fTE(hs));
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={svgFluid} role="img" aria-label="planform top view, full span">
      <text x={8} y={13} fontSize="9" fill={C.mute} fontFamily={FONT.mono}>TOP VIEW · flow ↓</text>
      <line x1={cx} x2={cx} y1={oy - 8} y2={oy + sc * (xmax - xmin) + 8} stroke={C.hair} strokeDasharray="2 3" />
      <path d={toD(outline) + "Z"} fill={C.copper} fillOpacity="0.15" stroke={C.ink} strokeWidth="1.4" strokeLinejoin="round" />
      <path d={toD(q)} fill="none" stroke={C.copper} strokeWidth="1" strokeDasharray="4 3" />
      <path d={toD(qm)} fill="none" stroke={C.copper} strokeWidth="1" strokeDasharray="4 3" />
      {stations.filter((y) => y > 0 && y < hs).map((y) => [1, -1].map((s) => {
        const [a, b] = P(s * y, g.fLE(y)), [, b2] = P(s * y, g.fTE(y));
        return <line key={y + ":" + s} x1={a} x2={a} y1={b} y2={b2} stroke={C.copperD} strokeOpacity="0.45" strokeDasharray="2 2" />;
      }))}
      <line x1={cx} x2={cx} y1={cyL} y2={cyT} stroke={C.copperD} strokeWidth="2.2" />
      <text x={cx + 6} y={(cyL + cyT) / 2 + 3} fontSize="9.5" fill={C.copperD} fontFamily={FONT.mono}>c₀ {f1(g.c0)}</text>
      <line x1={tx} x2={tx} y1={tyL} y2={tyT} stroke={C.slate} strokeWidth="2.2" />
      <text x={Math.min(tx + 5, W - 4)} y={tyT + 12} textAnchor="end" fontSize="9.5" fill={C.slate} fontFamily={FONT.mono}>c_tip {f1(g.ct)}</text>
    </svg>
  );
}

/* Front view looking aft: board and mast drawn so "up" is unambiguous. */
function FrontView({ g, bank = true, exag = 3, height = 200, labels = true }) {
  const W = 560, H = height, pad = 18, hs = g.hs, N = 140;
  const zs = []; for (let i = 0; i <= N; i++) zs.push(g.fZ(hs * i / N));
  const zmin = Math.min(0, ...zs), zmax = Math.max(0, ...zs);
  const top = 52, bot = 30, avail = H - top - bot;
  const sc = Math.min((W - 2 * pad) / (2 * hs), avail / (((zmax - zmin) * exag) || 1));
  const ox = W / 2, oyTop = top + (avail - (zmax - zmin) * exag * sc) / 2;
  const P = (y, z) => [ox + y * sc, oyTop + (zmax - z) * exag * sc];
  const [, y0] = P(0, 0), [, yRoot] = P(0, g.fZ(0));
  const lines = [];
  for (const s of [1, -1]) for (let i = 0; i < N; i++) {
    const ya = hs * i / N, yb = hs * (i + 1) / N, sl = (g.fZ(yb) - g.fZ(ya)) / (yb - ya);
    const [a1, b1] = P(s * ya, g.fZ(ya)), [a2, b2] = P(s * yb, g.fZ(yb));
    lines.push(<line key={s + ":" + i} x1={a1} y1={b1} x2={a2} y2={b2} stroke={SENSE_COLOR[senseOf(sl)]} strokeWidth="3" strokeLinecap="round" />);
  }
  const bars = [];
  for (let k = 0; k <= 8; k++) {
    const y = hs * k / 8, c = Math.max(0, g.chord(y)), t = g.secAt(y).t * c;
    const ang = bank ? Math.atan(g.dz(y) * exag) : 0, Lb = Math.max(3, t * exag * sc);
    for (const s of (k === 0 ? [1] : [1, -1])) {
      const [cx, cy] = P(s * y, g.fZ(y)), dx = -s * Math.sin(ang) * Lb / 2, dy = -Math.cos(ang) * Lb / 2;
      bars.push(<line key={k + ":" + s} x1={cx - dx} y1={cy - dy} x2={cx + dx} y2={cy + dy} stroke={C.ink} strokeOpacity="0.55" strokeWidth="2.4" strokeLinecap="round" />);
    }
  }
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={svgFluid} role="img" aria-label="front view with dihedral and anhedral stretches">
      <text x={8} y={13} fontSize="9" fill={C.mute} fontFamily={FONT.mono}>FRONT VIEW · looking aft · vertical ×{exag}</text>
      <text x={W - 8} y={13} textAnchor="end" fontSize="9" fontFamily={FONT.mono}>
        <tspan fill={C.teal}>━ rising (dihedral)</tspan><tspan fill={C.mute}>  </tspan><tspan fill={C.rust}>━ falling (anhedral)</tspan></text>
      <line x1={ox - 70} x2={ox + 70} y1={22} y2={22} stroke={C.slate} strokeWidth="4" strokeLinecap="round" />
      <text x={ox + 76} y={25} fontSize="8.5" fill={C.slate} fontFamily={FONT.mono}>board ↑ (+z)</text>
      <rect x={ox - 4} y={24} width={8} height={Math.max(0, yRoot - 24)} fill={C.slate} fillOpacity="0.35" />
      <line x1={pad} x2={W - pad} y1={y0} y2={y0} stroke={C.ink} strokeOpacity="0.35" strokeDasharray="3 3" />
      <text x={pad} y={y0 - 4} fontSize="8.5" fill={C.mute} fontFamily={FONT.mono}>0</text>
      {lines}{bars}
      {g.zeros.map((z, k) => [1, -1].map((s) => { const [a, b] = P(s * z.x, 0); return <circle key={k + ":" + s} cx={a} cy={b} r="3.5" fill={C.cream} stroke={C.ink} strokeWidth="1.1" />; }))}
      {g.turns.map((t, k) => [1, -1].map((s) => {
        const [a, b] = P(s * t.x, t.y), r = 5;
        return (<g key={k + ":" + s}>
          <path d={`M${a},${b - r}L${a + r},${b}L${a},${b + r}L${a - r},${b}Z`} fill={t.kind === "min" ? C.rust : C.teal} stroke={C.cream} strokeWidth="1" />
          {labels && s === 1 && <text x={clamp(a, 60, W - 60)} y={b + (t.kind === "min" ? 17 : -9)} textAnchor="middle" fontSize="9" fill={C.ink} fontFamily={FONT.mono}>{Math.round(t.x)} mm · {t.y > 0 ? "+" : ""}{f1(t.y)}</text>}
        </g>);
      }))}
      {labels && (() => { const [a, b] = P(hs, g.tipZ); return <text x={Math.min(a, W - 6)} y={b + (g.tipZ >= 0 ? -8 : 15)} textAnchor="end" fontSize="9" fill={C.ink} fontFamily={FONT.mono}>tip {g.tipZ >= 0 ? "+" : ""}{f1(g.tipZ)}</text>; })()}
    </svg>
  );
}

/* Oblique 3D view: blended sections placed on the curves. */
function placeSection(g, y, xi, ze, bank) {
  const ay = Math.abs(y), s = Math.sign(y) || 1, c = Math.max(0, g.chord(ay));
  const phi = g.fTw(ay) * Math.PI / 180, gam = bank ? Math.atan(g.dz(ay)) : 0;
  const dx = (xi - 0.25) * c, dzz = ze * c;
  const xr = dx * Math.cos(phi) + dzz * Math.sin(phi), zr = -dx * Math.sin(phi) + dzz * Math.cos(phi);
  return [g.quarter(ay) + xr, y - s * zr * Math.sin(gam), g.fZ(ay) + zr * Math.cos(gam)];
}
function ThreeDView({ g, height = 240, exag = 2, bank = true, nSt = 9 }) {
  const W = 640, H = height, pad = 14, hs = g.hs;
  const proj = ([x, y, z]) => [y * 0.93 + x * 0.55, -z * exag * 0.93 + x * 0.3];
  const loops = [];
  for (let k = -(nSt - 1); k <= nSt - 1; k++) {
    const y = hs * k / (nSt - 1), prof = profile(g.secAt(Math.abs(y)), 34);
    loops.push({ w: Math.abs(y) / hs, pts: prof.loop.map(([xi, ze]) => proj(placeSection(g, y, xi, ze, bank))) });
  }
  const edge = (xi) => { const o = []; for (let i = -70; i <= 70; i++) o.push(proj(placeSection(g, hs * i / 70, xi, 0, bank))); return o; };
  const LEp = edge(0), TEp = edge(1);
  const c0 = Math.max(1, g.c0), z0 = g.fZ(0), mx0 = g.fLE(0) + 0.12 * c0, mx1 = g.fLE(0) + 0.7 * c0, mh = hs * 0.4 / exag;
  const mast = [[mx0, 0, z0], [mx1, 0, z0], [mx1, 0, z0 + mh], [mx0, 0, z0 + mh]].map(proj);
  const all = [...loops.flatMap((l) => l.pts), ...LEp, ...TEp, ...mast];
  const xs = all.map((q) => q[0]), ys = all.map((q) => q[1]);
  const x0 = Math.min(...xs), x1 = Math.max(...xs), y0 = Math.min(...ys), y1 = Math.max(...ys);
  const s = Math.min((W - 2 * pad) / (x1 - x0 || 1), (H - 2 * pad - 12) / (y1 - y0 || 1));
  const ox = pad + ((W - 2 * pad) - s * (x1 - x0)) / 2, oy = pad + 12 + ((H - 2 * pad - 12) - s * (y1 - y0)) / 2;
  const d = (pts, close) => pts.map(([a, b], i) => `${i ? "L" : "M"}${(ox + (a - x0) * s).toFixed(1)},${(oy + (b - y0) * s).toFixed(1)}`).join("") + (close ? "Z" : "");
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={svgFluid} role="img" aria-label="assembled foil, oblique view">
      <text x={8} y={13} fontSize="9" fill={C.mute} fontFamily={FONT.mono}>ASSEMBLED · sections blended center → tip · vertical ×{exag}</text>
      <path d={d(mast, true)} fill={C.slate} fillOpacity="0.2" stroke={C.slate} strokeWidth="1" />
      {loops.map((l, i) => <path key={i} d={d(l.pts, true)} fill={C.cream} fillOpacity="0.55" stroke={lerpColor(C.copperD, C.slate, l.w)} strokeWidth="1.1" strokeLinejoin="round" />)}
      <path d={d(LEp)} fill="none" stroke={C.ink} strokeWidth="1.5" />
      <path d={d(TEp)} fill="none" stroke={C.ink} strokeWidth="1.1" strokeOpacity="0.75" />
    </svg>
  );
}

/* Normalized section overlay with the max-thickness position marked. */
function ProfilePlot({ items, height = 170, dims = null }) {
  const W = 560, H = height, pad = 18;
  const pr = items.map((it) => ({ ...it, p: profile(it.spec, 90) }));
  let ylo = 0, yhi = 0;
  for (const it of pr) for (const [, y] of it.p.loop) { ylo = Math.min(ylo, y); yhi = Math.max(yhi, y); }
  const avail = H - 2 * pad - 14, sc = Math.min(W - 2 * pad, avail / ((yhi - ylo) || 0.1));
  const ox = (W - sc) / 2, oy = pad + 14 + (avail - (yhi - ylo) * sc) / 2;
  const P = (x, y) => [ox + x * sc, oy + (yhi - y) * sc];
  const d = (pts, close) => pts.map(([x, y], i) => { const [a, b] = P(x, y); return `${i ? "L" : "M"}${a.toFixed(1)},${b.toFixed(1)}`; }).join("") + (close ? "Z" : "");
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={svgFluid} role="img" aria-label="section profiles">
      <line x1={P(0, 0)[0]} x2={P(1, 0)[0]} y1={P(0, 0)[1]} y2={P(1, 0)[1]} stroke={C.hair} strokeDasharray="3 3" />
      {pr.map((it, i) => (
        <g key={i} opacity={it.dim ? 0.35 : 1}>
          <path d={d(it.p.loop, true)} fill={it.color} fillOpacity={it.dim ? 0.03 : 0.1} stroke={it.color} strokeWidth={it.dim ? 1 : 1.8} strokeLinejoin="round" />
          {it.spec.cam > 0 && <path d={d(it.p.cm)} fill="none" stroke={it.color} strokeWidth="0.9" strokeDasharray="3 2" />}
          {!it.dim && (() => {
            const cf = camberFn(it.spec.cam, it.spec.xc)(it.spec.xt).yc;
            const [a, b1] = P(it.spec.xt, cf + it.spec.t / 2), [, b2] = P(it.spec.xt, cf - it.spec.t / 2);
            return (<g>
              <line x1={a} x2={a} y1={b1 - 6} y2={b2 + 6} stroke={C.ink} strokeWidth="1.3" />
              <line x1={a - 4} x2={a + 4} y1={b1} y2={b1} stroke={C.ink} strokeWidth="1.3" />
              <line x1={a - 4} x2={a + 4} y1={b2} y2={b2} stroke={C.ink} strokeWidth="1.3" />
              <text x={a + 7} y={b1 - 6} fontSize="9.5" fill={C.ink} fontFamily={FONT.mono}>t {pctStr(it.spec.t)} @ {pctStr(it.spec.xt)}</text>
            </g>);
          })()}
        </g>
      ))}
      {dims && <text x={8} y={13} fontSize="9" fill={C.mute} fontFamily={FONT.mono}>{dims}</text>}
    </svg>
  );
}

/* t, max-thickness position and camber along the span. */
function SpanPlot({ g, stations, height = 160, cursor = null }) {
  const W = 560, H = height, PL = 40, PR = 14, PT = 22, PB = 28, hs = g.hs, N = 90;
  const series = [
    { label: "thickness t", color: C.copperD, f: (y) => g.secAt(y).t },
    { label: "max-t position", color: C.slate, f: (y) => g.secAt(y).xt },
    { label: "camber", color: C.sage, f: (y) => g.secAt(y).cam },
  ];
  let hi = 0.1; for (const s of series) for (let i = 0; i <= N; i++) hi = Math.max(hi, s.f(hs * i / N));
  hi = Math.ceil(hi * 10 + 0.5) / 10;
  const px = (y) => PL + y / hs * (W - PL - PR), py = (v) => PT + (hi - v) / hi * (H - PT - PB);
  return (
    <svg viewBox={`0 0 ${W} ${H}`} style={svgFluid} role="img" aria-label="section parameters along the span">
      {niceTicks(0, hi * 100, 4).map((v) => (<g key={v}>
        <line x1={PL} x2={W - PR} y1={py(v / 100)} y2={py(v / 100)} stroke={C.hair} strokeWidth="0.6" />
        <text x={PL - 5} y={py(v / 100) + 3} textAnchor="end" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>{v}%</text></g>))}
      {stations.map((y) => <line key={y} x1={px(y)} x2={px(y)} y1={PT} y2={H - PB} stroke={C.copperD} strokeOpacity="0.4" strokeDasharray="2 2" />)}
      {cursor != null && <line x1={px(cursor)} x2={px(cursor)} y1={PT} y2={H - PB} stroke={C.copper} strokeWidth="1.6" />}
      {series.map((s) => { let d = ""; for (let i = 0; i <= N; i++) { const y = hs * i / N; d += `${i ? "L" : "M"}${px(y).toFixed(1)},${py(s.f(y)).toFixed(1)}`; } return <path key={s.label} d={d} fill="none" stroke={s.color} strokeWidth="2" />; })}
      <text x={PL} y={13} fontSize="9" fontFamily={FONT.mono}>{series.map((s, i) => <tspan key={s.label} fill={s.color}>{i ? "   " : ""}━ {s.label}</tspan>)}</text>
      <text x={W - PR} y={H - 6} textAnchor="end" fontSize="9" fill={C.mute} fontFamily={FONT.mono}>mm from center →</text>
    </svg>
  );
}

function Chip({ k, v, u, tone }) {
  return (
    <div style={{ ...chip, borderColor: tone || C.hair }}>
      <span style={{ color: C.mute, fontSize: 8.5, letterSpacing: ".05em", fontFamily: FONT.sans }}>{k}</span>
      <span style={{ color: C.ink, fontFamily: FONT.mono, fontWeight: 600, fontSize: 12.5 }}>{v}<span style={{ color: C.mute, fontWeight: 400, fontSize: 9 }}>{u}</span></span>
    </div>
  );
}
function MetricRow({ g }) {
  return (
    <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginTop: 8 }}>
      <Chip k="SPAN" v={f1(g.b)} u=" mm" />
      <Chip k="AREA" v={(g.S / 100).toFixed(0)} u=" cm²" />
      <Chip k="AR" v={g.AR.toFixed(2)} />
      <Chip k="MAC" v={g.MAC.toFixed(0)} u=" mm" />
      <Chip k="TAPER λ" v={g.taper.toFixed(3)} tone={C.copperL} />
      <Chip k="MAX DROP" v={f1(g.maxDrop)} u={g.maxDrop > 0 ? ` @ ${Math.round(g.maxDropAt)}` : ""} tone={g.maxDrop > 0 ? C.rust : null} />
      <Chip k="TIP z" v={(g.tipZ > 0 ? "+" : "") + f1(g.tipZ)} u=" mm" />
    </div>
  );
}

/* ============================================================================
   PANELS — each edits the shared design; the program is rewritten from it
============================================================================ */
const fr = (hs, pairs) => pairs.map(([e, v]) => ({ x: e === 1 ? hs : +(e * hs).toFixed(1), y: v }));
function addAnchor(c) {
  const a = c.anchors; let bi = 0, bw = -1;
  for (let i = 0; i < a.length - 1; i++) { const w = a[i + 1].x - a[i].x; if (w > bw) { bw = w; bi = i; } }
  const x = Math.round((a[bi].x + a[bi + 1].x) / 2), y = Math.round(makeSpline(a, c.mode)(x) * 2) / 2;
  return { ...c, anchors: [...a.slice(0, bi + 1), { x, y }, ...a.slice(bi + 1)] };
}
function removeAnchor(c) {
  if (c.anchors.length <= 2) return c;
  const a = c.anchors; return { ...c, anchors: [...a.slice(0, a.length - 2), a[a.length - 1]] };
}
function rescaleSpan(M, span) {
  const k = span / M.span, hsN = span / 2;
  const sx = (c) => c && { ...c, anchors: c.anchors.map((a, i, arr) => ({ x: i === arr.length - 1 ? hsN : +(a.x * k).toFixed(1), y: a.y })) };
  return {
    ...M, span, leading: sx(M.leading), trailing: sx(M.trailing),
    dihedral: M.dihedral && { ...sx(M.dihedral), bank: M.dihedral.bank }, twist: sx(M.twist),
    sections: { ...M.sections, stations: M.sections.stations.map((s, i, arr) => ({ ...s, y: i === arr.length - 1 && arr.length > 1 ? hsN : +(s.y * k).toFixed(1) })) },
  };
}
function ModeButtons({ value, onChange, color, label }) {
  return (
    <div style={{ display: "flex", gap: 4, alignItems: "center", flexWrap: "wrap" }}>
      <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color, minWidth: 18 }}>{label}</span>
      {MODES.map((m) => <button key={m} onClick={() => onChange(m)} style={segMini(value === m)} aria-pressed={value === m}>{m}</button>)}
    </div>
  );
}
function PresetRow({ names, onPick }) {
  return (
    <div style={{ display: "flex", gap: 6, marginBottom: 12, flexWrap: "wrap", alignItems: "center" }}>
      <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color: C.mute, marginRight: 4 }}>START FROM</span>
      {names.map((n) => <button key={n} onClick={() => onPick(n)} style={seg(false)}>{n}</button>)}
    </div>
  );
}

/* ---------------- 01 planform ---------------- */
const PLAN_PRESETS = {
  crescent: (hs) => ({ leading: { mode: "smooth", anchors: fr(hs, [[0, 0], [0.5, 14], [1, 96]]) }, trailing: { mode: "monotone", anchors: fr(hs, [[0, 132], [0.5, 116], [1, 122]]) } }),
  elliptic: (hs) => chordSugar({ law: "elliptic", c0: 132, ct: 22, sweep: 0 }, hs),
  "straight taper": (hs) => chordSugar({ law: "linear", c0: 132, ct: 30, sweep: 2 }, hs),
  "swept LE · straight TE": (hs) => ({ leading: { mode: "linear", anchors: fr(hs, [[0, 0], [1, 100]]) }, trailing: { mode: "linear", anchors: fr(hs, [[0, 132], [1, 128]]) } }),
  scimitar: (hs) => ({ leading: { mode: "smooth", anchors: fr(hs, [[0, 0], [0.4, 6], [0.75, 40], [1, 118]]) }, trailing: { mode: "monotone", anchors: fr(hs, [[0, 132], [0.45, 118], [0.8, 120], [1, 138]]) } }),
  "forward-swept TE": (hs) => ({ leading: { mode: "smooth", anchors: fr(hs, [[0, 0], [0.5, 10], [1, 40]]) }, trailing: { mode: "smooth", anchors: fr(hs, [[0, 132], [0.5, 110], [1, 66]]) } }),
};
function PlanformPanel({ design, setDesign, g }) {
  const hs = design.span / 2;
  const flip = (edge) => (t) => `${edge} sweep reverses @ ${Math.round(t.x)}`;
  const curves = [
    { key: "leading", label: "leading edge", color: C.copperD, mode: design.leading.mode, anchors: design.leading.anchors, marks: ["turns", "infl"], turnLabel: flip("LE") },
    { key: "trailing", label: "trailing edge", color: C.slate, mode: design.trailing.mode, anchors: design.trailing.anchors, marks: ["turns", "infl"], turnLabel: flip("TE") },
  ];
  const put = (k, c) => setDesign({ ...design, [k]: c });
  const sw = (f, y) => `${Math.round(g.sweep(f, y))}°`;
  const reflex = (k) => {
    const c = design[k]; if (c.mode === "linear") return "corners at anchors";
    const r = inflections(makeSpline(c.anchors, c.mode), 0, hs);
    return r.length ? r.map((q) => `${Math.round(q.x)} mm`).join(", ") : "none";
  };
  return (
    <div>
      <PresetRow names={Object.keys(PLAN_PRESETS)} onPick={(n) => setDesign({ ...design, ...PLAN_PRESETS[n](hs) })} />
      <div style={{ display: "flex", flexWrap: "wrap", gap: 18 }}>
        <div style={{ flex: "1 1 440px", minWidth: 300 }}>
          <span style={lbl}>HALF SPAN · drag <span style={{ color: C.copperD }}>●</span> leading and <span style={{ color: C.slate }}>●</span> trailing edge anchors · arrow keys nudge</span>
          <CurveEditor curves={curves} onChange={(k, a) => put(k, { ...design[k], anchors: a })} x1={hs} range={[-10, 150]} yUp={false}
            fill={["leading", "trailing"]} yLabel="x aft of center LE (mm)" height={260} ariaLabel="leading and trailing edge curves" />
          <div style={{ display: "flex", flexDirection: "column", gap: 6, marginTop: 8 }}>
            {[["leading", "LE", C.copperD], ["trailing", "TE", C.slate]].map(([k, l, col]) => (
              <div key={k} style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
                <ModeButtons value={design[k].mode} onChange={(m) => put(k, { ...design[k], mode: m })} color={col} label={l} />
                <button style={segMini(false)} onClick={() => put(k, addAnchor(design[k]))}>+ anchor</button>
                <button style={segMini(false)} onClick={() => put(k, removeAnchor(design[k]))} disabled={design[k].anchors.length <= 2}>− anchor</button>
              </div>
            ))}
          </div>
          <div style={{ marginTop: 12, maxWidth: 360 }}>
            <Slider label="Span (tip to tip)" value={design.span} min={500} max={1400} step={10} show={(v) => `${v} mm`} onChange={(v) => setDesign(rescaleSpan(design, v))} />
          </div>
        </div>
        <div style={{ flex: "1 1 360px", minWidth: 290 }}>
          <PlanformView g={g} stations={design.sections.stations.map((s) => s.y)} />
          <div style={{ ...readout, marginTop: 10 }}>
            <div style={readHead}>TAPER — chord at tip over chord at center</div>
            <div style={{ fontFamily: FONT.mono, fontSize: 14, color: C.ink, margin: "2px 0 4px" }}>
              λ = c_tip / c₀ = {f1(g.ct)} / {f1(g.c0)} = <b style={{ color: C.copperD }}>{g.taper.toFixed(3)}</b>
            </div>
            <div style={{ color: C.mute, fontSize: 10 }}>derived from the two edge curves, never authored directly</div>
          </div>
          <div style={{ ...readout, marginTop: 8 }}>
            <div style={readHead}>LOCAL SWEEP (+ = aft) · center / mid / tip</div>
            <div><span style={{ color: C.copperD }}>LE</span> {sw(g.fLE, 1)} · {sw(g.fLE, hs / 2)} · {sw(g.fLE, hs - 1)} <span style={{ color: C.mute }}>· reflex at {reflex("leading")}</span></div>
            <div><span style={{ color: C.slate }}>TE</span> {sw(g.fTE, 1)} · {sw(g.fTE, hs / 2)} · {sw(g.fTE, hs - 1)} <span style={{ color: C.mute }}>· reflex at {reflex("trailing")}</span></div>
          </div>
          <MetricRow g={g} />
        </div>
      </div>
      <p style={note}>
        Leading and trailing edges are separate curves, so any sweep permutation is available: LE aft with TE forward (a crescent),
        both aft, both forward, or an edge that reverses. A copper diamond marks where an edge's sweep changes sign; a dashed ring marks a
        reflex, where the edge's curvature flips. Chord, the quarter-chord spine, area, aspect ratio and taper all fall out of the two edges.
      </p>
    </div>
  );
}

/* ---------------- 02 out-of-plane ---------------- */
const DIH_PRESETS = {
  flat: ["linear", [[0, 0], [1, 0]]],
  dihedral: ["linear", [[0, 0], [1, 40]]],
  anhedral: ["linear", [[0, 0], [1, -40]]],
  "gull · drop then lift": ["monotone", [[0, 0], [0.3, -16], [0.6, 0], [1, 46]]],
  "inverted gull · lift then drop": ["monotone", [[0, 0], [0.35, 14], [0.65, 0], [1, -38]]],
  "drooped tips": ["monotone", [[0, 0], [0.65, 0], [0.85, -8], [1, -34]]],
  "tip lift": ["monotone", [[0, 0], [0.65, 0], [0.85, 6], [1, 40]]],
  "W · double gull": ["monotone", [[0, 0], [0.2, -10], [0.45, 4], [0.7, -6], [1, 30]]],
};
function shapeEvents(g) {
  const fz = (v) => (Math.abs(v) < 0.05 ? "at 0" : v < 0 ? `${f1(-v)} mm below 0` : `${f1(v)} mm above 0`);
  const ev = [{ x: 0, tag: "start", text: `starts ${fz(g.fZ(0))}` }];
  for (const t of g.turns) ev.push({ x: t.x, tag: t.kind === "min" ? "low" : "high", text: t.kind === "min" ? `bottoms out ${fz(t.y)}, then turns back up` : `peaks ${fz(t.y)}, then turns back down` });
  for (const z of g.zeros) ev.push({ x: z.x, tag: "zero", text: `passes back through 0 heading ${z.dir}` });
  for (const q of g.infl) ev.push({ x: q.x, tag: "infl", text: `curvature flips ${q.from === "bowl" ? "∪ bowl" : "∩ crest"} → ${q.to === "bowl" ? "∪ bowl" : "∩ crest"}` });
  for (const k of g.kinks) ev.push({ x: k, tag: "infl", text: "kink: slope changes abruptly (linear mode)" });
  ev.push({ x: g.hs, tag: "tip", text: `tip ends ${fz(g.tipZ)}` });
  return ev.sort((a, b) => a.x - b.x);
}
const TAG = { start: [C.mute, "·"], low: [C.rust, "▼"], high: [C.teal, "▲"], zero: [C.ink, "○"], infl: [C.copper, "∿"], tip: [C.mute, "·"] };
function DihedralPanel({ design, setDesign, g }) {
  const hs = design.span / 2;
  const [exag, setExag] = useState(3);
  const dih = design.dihedral || { mode: "monotone", anchors: [{ x: 0, y: 0 }, { x: hs, y: 0 }], bank: true };
  const put = (d) => setDesign({ ...design, dihedral: d });
  const curves = [{
    key: "z", label: "drop / rise z", color: C.teal, anchorColor: C.ink, mode: dih.mode, anchors: dih.anchors, sense: true, marks: ["turns", "zero", "infl"],
    turnLabel: (t) => `${t.kind === "min" ? "▼" : "▲"} ${t.y < 0 ? "drop " + f1(-t.y) : "rise " + f1(t.y)} @ ${Math.round(t.x)} mm`,
  }];
  const ev = shapeEvents(g);
  const aMin = Math.min(...dih.anchors.map((a) => a.y)), aMax = Math.max(...dih.anchors.map((a) => a.y));
  const zx = extrema(g.fZ, 0, hs);
  const over = dih.mode !== "monotone" && (zx.min < aMin - 0.3 || zx.max > aMax + 0.3);
  const senseName = { up: "▲ rising · dihedral", down: "▼ falling · anhedral", level: "— level" };
  return (
    <div>
      <PresetRow names={Object.keys(DIH_PRESETS)} onPick={(n) => { const [m, pts] = DIH_PRESETS[n]; put({ mode: m, anchors: fr(hs, pts), bank: dih.bank }); }} />
      <div style={{ display: "flex", flexWrap: "wrap", gap: 18 }}>
        <div style={{ flex: "1 1 440px", minWidth: 300 }}>
          <span style={lbl}>DROP / RISE FROM 0 · drag <span style={{ color: C.ink }}>●</span> anchors · <span style={{ color: C.teal }}>teal rises</span>, <span style={{ color: C.rust }}>rust falls</span></span>
          <CurveEditor curves={curves} onChange={(k, a) => put({ ...dih, anchors: a })} x1={hs} range={[-30, 50]} zero yLabel="z (mm) · + toward board" height={260} ariaLabel="out-of-plane drop and rise curve" />
          <div style={{ display: "flex", gap: 10, marginTop: 8, flexWrap: "wrap", alignItems: "center" }}>
            <ModeButtons value={dih.mode} onChange={(m) => put({ ...dih, mode: m })} color={C.teal} label="z" />
            <button style={segMini(false)} onClick={() => put(addAnchor(dih))}>+ anchor</button>
            <button style={segMini(false)} onClick={() => put(removeAnchor(dih))} disabled={dih.anchors.length <= 2}>− anchor</button>
            <label style={{ display: "flex", gap: 5, alignItems: "center", fontFamily: FONT.mono, fontSize: 10, color: C.inks, cursor: "pointer" }}>
              <input type="checkbox" checked={dih.bank} onChange={(e) => put({ ...dih, bank: e.target.checked })} /> bank sections
            </label>
          </div>
          {over && (
            <div style={{ ...warnBox, marginTop: 8 }}>
              {dih.mode} mode overshoots the authored anchors (curve reaches {f1(zx.min)} / {f1(zx.max)} mm vs anchors {f1(aMin)} / {f1(aMax)} mm).
              Switch to monotone to pin every drop and rise exactly on its anchor.
            </div>
          )}
        </div>
        <div style={{ flex: "1 1 360px", minWidth: 290 }}>
          <FrontView g={g} bank={dih.bank} exag={exag} />
          <div style={{ display: "flex", gap: 5, alignItems: "center", margin: "6px 0 10px" }}>
            <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color: C.mute }}>VERTICAL SCALE</span>
            {[1, 3, 5].map((v) => <button key={v} style={segMini(exag === v)} onClick={() => setExag(v)} aria-pressed={exag === v}>×{v}</button>)}
          </div>
          <div style={readout}>
            <div style={readHead}>WHERE IT TURNS · distance from center</div>
            {ev.map((e, i) => (
              <div key={i} style={{ display: "flex", gap: 8, lineHeight: 1.75 }}>
                <span style={{ color: TAG[e.tag][0], width: 12, textAlign: "center" }}>{TAG[e.tag][1]}</span>
                <span style={{ color: C.copperD, minWidth: 58 }}>{Math.round(e.x)} mm</span>
                <span>{e.text}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
      <div style={{ marginTop: 12, overflowX: "auto" }}>
        <table style={tbl}>
          <thead><tr style={{ background: C.copperD }}>{["stretch (mm from center)", "sense", "local Γ", "z at start → end"].map((h) => <th key={h} style={th}>{h.toUpperCase()}</th>)}</tr></thead>
          <tbody>
            {g.segs.map((s, i) => (
              <tr key={i} style={{ borderTop: `1px solid ${C.hair}`, background: i % 2 ? C.panel2 : "transparent" }}>
                <td style={td}>{Math.round(s.x0)} – {Math.round(s.x1)}</td>
                <td style={{ ...td, color: SENSE_COLOR[s.sense], fontWeight: 600 }}>{senseName[s.sense]}</td>
                <td style={td}>{s.gMin.toFixed(1)}° … {s.gMax.toFixed(1)}°</td>
                <td style={td}>{f1(s.z0)} → {f1(s.z1)} mm</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p style={note}>
        Every anchor is a distance from the center and a drop or rise from 0, which is how the DSL writes it
        (<code style={codeIn}>at 135 mm drop 16 mm</code>). The table splits the half span into rising (dihedral) and falling (anhedral) stretches;
        the list above names every place the foil bottoms out, peaks, crosses back through 0 or flips curvature. In monotone mode an authored
        drop or rise is exactly the extreme of the curve — the number you write is the number you get.
      </p>
    </div>
  );
}

/* ---------------- 03 section schedule ---------------- */
function SectionsPanel({ design, setDesign, g }) {
  const hs = design.span / 2, st = design.sections.stations;
  const [sel, setSel] = useState(0);
  const [scrub, setScrub] = useState(0.55);
  const si = Math.min(sel, st.length - 1), cur = st[si], sp = cur.spec;
  const ys = clamp(scrub, 0, 1) * hs;
  const colorAt = (y) => lerpColor(C.copperD, C.slate, y / hs);
  const put = (stations) => setDesign({ ...design, sections: { ...design.sections, stations } });
  const edit = (patch) => {
    const s = { ...sp, ...patch };
    put(st.map((q, j) => (j === si ? { ...q, spec: { src: "profile", family: s.family, t: s.t, xt: s.xt, cam: s.cam, xc: s.xc } } : q)));
  };
  const interior = si > 0 && si < st.length - 1;
  const moveSt = (y) => { if (!interior) return; const yy = clamp(Math.round(y), st[si - 1].y + 10, st[si + 1].y - 10); put(st.map((q, j) => (j === si ? { ...q, y: yy } : q))); };
  const add = () => {
    if (st.length < 2) return;
    const j = si < st.length - 1 ? si : si - 1, y = Math.round((st[j].y + st[j + 1].y) / 2), b = g.secAt(y);
    const spec = { src: "profile", family: b.family, t: +b.t.toFixed(3), xt: +b.xt.toFixed(3), cam: +b.cam.toFixed(4), xc: +b.xc.toFixed(3) };
    put([...st.slice(0, j + 1), { y, spec }, ...st.slice(j + 1)]); setSel(j + 1);
  };
  const remove = () => { if (!interior) return; put(st.filter((_, j) => j !== si)); setSel(si - 1); };
  const bl = g.secAt(ys), cAt = Math.max(0, g.chord(ys));
  const stName = (y) => stStr(y, hs);
  const srcName = (s) => (s.src === "naca" ? `NACA ${s.digits}${s.mod ? "-" + s.mod : ""}` : s.src === "profile" ? `${pctStr(s.t)} @ ${pctStr(s.xt)}` : `${s.src} ${s.name} (nominal)`);
  return (
    <div>
      <div style={{ display: "flex", gap: 6, flexWrap: "wrap", alignItems: "center", marginBottom: 12 }}>
        <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color: C.mute, marginRight: 4 }}>STATIONS</span>
        {st.map((q, j) => (
          <button key={j} onClick={() => setSel(j)} aria-pressed={j === si}
            style={{ ...seg(j === si), borderLeft: `4px solid ${colorAt(q.y)}` }}>{stName(q.y)} · {srcName(q.spec)}</button>
        ))}
        <button style={segMini(false)} onClick={add}>+ station</button>
        <button style={segMini(false)} onClick={remove} disabled={!interior}>− station</button>
        <span style={{ marginLeft: "auto", display: "flex", gap: 4, alignItems: "center" }}>
          <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color: C.mute }}>BLEND</span>
          {["linear", "cosine"].map((b) => <button key={b} style={segMini(design.sections.blend === b)} onClick={() => setDesign({ ...design, sections: { ...design.sections, blend: b } })}>{b}</button>)}
        </span>
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 18 }}>
        <div style={{ flex: "1 1 280px", minWidth: 260, maxWidth: 380 }}>
          <div style={{ ...readout, marginBottom: 10 }}>
            <div style={readHead}>STATION {si + 1} · {stName(cur.y)}{sp.src === "naca" ? ` · NACA ${sp.digits}` : ""}</div>
            <div style={{ color: C.mute, fontSize: 10 }}>{sp.nominal ? "external section — nominal parameters until its coordinates are loaded; editing makes it a profile" : sp.src === "naca" ? "editing any value converts this station to an explicit profile" : "explicit profile"}</div>
          </div>
          <Slider label="Position (mm from center)" value={cur.y} min={0} max={hs} step={1} show={(v) => (interior ? `${Math.round(v)} mm` : stName(v))} disabled={!interior} onChange={moveSt} />
          <Slider label="Thickness  t / c" value={sp.t} min={0.04} max={0.2} step={0.005} show={pctStr} onChange={(v) => edit({ t: v })} />
          <Slider label="Max-thickness position  (% chord from LE)" value={sp.xt} min={0.2} max={0.6} step={0.01} show={pctStr} onChange={(v) => edit({ xt: v })} />
          <Slider label="Camber  m / c" value={sp.cam} min={0} max={0.08} step={0.0025} show={pctStr} onChange={(v) => edit({ cam: v })} />
          <Slider label="Max-camber position  (% chord)" value={sp.xc} min={0.2} max={0.7} step={0.01} show={pctStr} disabled={!(sp.cam > 0)} onChange={(v) => edit({ xc: v })} />
          <div style={{ display: "flex", gap: 5, alignItems: "center" }}>
            <span style={{ fontFamily: FONT.mono, fontSize: 9.5, color: C.mute }}>THICKNESS FAMILY</span>
            {["naca", "cst"].map((f) => <button key={f} style={segMini(sp.family === f)} onClick={() => edit({ family: f })}>{f}</button>)}
          </div>
        </div>
        <div style={{ flex: "2 1 420px", minWidth: 300 }}>
          <span style={lbl}>ALL STATIONS · normalized to chord · selected station dimensioned</span>
          <ProfilePlot items={st.map((q, j) => ({ spec: q.spec, color: colorAt(q.y), dim: j !== si }))} height={170} />
          <div style={{ marginTop: 12 }}>
            <Slider label="Inspect the blended section at" value={scrub} min={0} max={1} step={0.005} show={(v) => `${Math.round(v * hs)} mm from center`} onChange={setScrub} />
          </div>
          <ProfilePlot items={[{ spec: bl, color: colorAt(ys) }]} height={130}
            dims={`chord ${f1(cAt)} mm · max thickness ${f1(cAt * bl.t)} mm, ${f1(cAt * bl.xt)} mm aft of LE · camber ${pctStr(bl.cam)}`} />
          <div style={{ marginTop: 10 }}><SpanPlot g={g} stations={st.map((q) => q.y)} cursor={ys} /></div>
        </div>
      </div>
      <p style={note}>
        A section is four numbers: thickness, where along the chord it peaks, camber, and where camber peaks. NACA 4-digit designations are
        sugar for the same four (<code style={codeIn}>naca 4412</code> = 12% at 30%, 4% camber at 40%; the modified series
        <code style={codeIn}>naca 0012-64</code> moves the peak to 40%). Stations sit at distances from the center; between them every
        parameter blends, so a cambered, thick root evolves smoothly into a thin, symmetric tip. Moving the thickness peak aft also
        sharpens the leading edge, because the thickness family is stretched rather than replaced.
      </p>
    </div>
  );
}

/* ============================================================================
   05 editor · 04 projection · grammar · shell
============================================================================ */
function AstNode({ k, v, depth }) {
  const [open, setOpen] = useState(depth < 2);
  if (!(v && typeof v === "object")) {
    const color = typeof v === "number" ? C.copperD : typeof v === "string" ? C.sage : C.slate;
    return (
      <div style={{ paddingLeft: depth * 12, fontFamily: FONT.mono, fontSize: 11, lineHeight: 1.65 }}>
        <span style={{ color: C.copper }}>{k}</span><span style={{ color: C.mute }}>: </span>
        <span style={{ color }}>{typeof v === "string" ? `"${v}"` : typeof v === "number" ? f1(v) : String(v)}</span>
      </div>
    );
  }
  const arr = Array.isArray(v), entries = arr ? v.map((x, i) => [i, x]) : Object.entries(v).filter(([, x]) => x !== undefined);
  return (
    <div style={{ paddingLeft: depth * 12 }}>
      <button onClick={() => setOpen(!open)} style={astBtn} aria-expanded={open}>
        <span style={{ color: C.copperD, display: "inline-block", width: 10 }}>{open ? "▾" : "▸"}</span>
        <span style={{ color: C.copper }}>{k}</span><span style={{ color: C.mute }}> {arr ? `[${entries.length}]` : "{…}"}</span>
      </button>
      {open && entries.map(([kk, vv]) => <AstNode key={kk} k={kk} v={vv} depth={depth + 1} />)}
    </div>
  );
}

function EditorPanel({ src, onSrc, status, design, g, onReset }) {
  const cons = evalConstraints(design, g);
  const id = fnv1a(emitDSL(design));
  const allOk = cons.every((c) => c.ok);
  return (
    <div>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 18 }}>
        <div style={{ flex: "1 1 380px", minWidth: 300 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6, gap: 8, flexWrap: "wrap" }}>
            <span style={miniHead}>FoilDSL v3 SOURCE</span>
            <span style={{ display: "flex", gap: 6, alignItems: "center" }}>
              <button style={segMini(false)} onClick={onReset}>reset to gull-90</button>
              <span style={{ fontFamily: FONT.mono, fontSize: 10.5, fontWeight: 600, padding: "3px 9px", borderRadius: 3, color: "#fff", background: status.ok ? C.sage : C.rust }}>
                {status.ok ? "✓ well-formed · projected" : `✕ line ${status.err.line}`}</span>
            </span>
          </div>
          <textarea value={src} onChange={(e) => onSrc(e.target.value)} spellCheck={false} aria-label="FoilDSL source"
            style={{ width: "100%", height: 560, resize: "vertical", boxSizing: "border-box", fontFamily: FONT.mono, fontSize: 11.5, lineHeight: 1.5,
              color: C.ink, background: C.panel, border: `1px solid ${C.hair}`, borderLeft: `3px solid ${status.ok ? C.copper : C.rust}`,
              borderRadius: 3, padding: "12px 12px 12px 14px", outline: "none", tabSize: 2 }} />
          {!status.ok && <div style={{ ...warnBox, marginTop: 6, color: C.rust }}>line {status.err.line}: {status.err.msg}<span style={{ color: C.mute }}> — views show the last well-formed shape</span></div>}
        </div>
        <div style={{ flex: "1 1 380px", minWidth: 300 }}>
          <span style={miniHead}>PROJECTED ⟦·⟧</span>
          <ThreeDView g={g} bank={design.dihedral ? design.dihedral.bank : true} height={210} />
          <div style={{ height: 8 }} />
          <PlanformView g={g} height={140} stations={design.sections.stations.map((s) => s.y)} />
          <div style={{ height: 8 }} />
          <FrontView g={g} bank={design.dihedral ? design.dihedral.bank : true} exag={3} height={170} />
          <div style={{ ...readout, marginTop: 10, display: "flex", justifyContent: "space-between", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
            <span><span style={{ color: C.mute }}>id(F) = H(canon(⟦parse(s)⟧)) =</span> <b style={{ color: C.copperD, fontSize: 13 }}>{id}</b></span>
            <span style={{ color: C.mute, fontSize: 10 }}>comments and spacing don't change it; geometry does</span>
          </div>
        </div>
      </div>
      <div style={{ display: "flex", flexWrap: "wrap", gap: 18, marginTop: 16 }}>
        <div style={{ flex: "1 1 320px", minWidth: 280 }}>
          <span style={miniHead}>ABSTRACT SYNTAX TREE · after station resolution</span>
          <div style={{ background: C.panel, border: `1px solid ${C.hair}`, borderRadius: 3, padding: "10px 12px", maxHeight: 300, overflow: "auto" }}>
            <AstNode k="foil" v={{ name: design.name, span: design.span, planform: { leading: design.leading, trailing: design.trailing }, dihedral: design.dihedral, twist: design.twist, sections: design.sections, loft: design.loft }} depth={0} />
          </div>
        </div>
        <div style={{ flex: "1 1 320px", minWidth: 280 }}>
          <span style={miniHead}>CONSTRAINTS · {allOk ? "all satisfied" : "some violated"}</span>
          <table style={tbl}>
            <thead><tr style={{ background: C.copperD }}>{["metric", "asserted", "projected", ""].map((h) => <th key={h} style={th}>{h.toUpperCase()}</th>)}</tr></thead>
            <tbody>
              {cons.length ? cons.map((c, i) => (
                <tr key={i} style={{ borderTop: `1px solid ${C.hair}`, background: i % 2 ? C.panel2 : "transparent" }}>
                  <td style={td}>{c.metric}</td><td style={td}>{c.relop} {c.raw} {c.unit || ""}</td>
                  <td style={{ ...td, color: C.ink }}>{c.label}</td>
                  <td style={{ ...td, textAlign: "center", color: c.ok ? C.sage : C.rust, fontWeight: 700 }}>{c.ok ? "✓" : "✕"}</td>
                </tr>
              )) : <tr><td colSpan={4} style={{ ...td, color: C.mute }}>no constraints declared</td></tr>}
            </tbody>
          </table>
          <p style={{ ...note, marginTop: 8 }}>
            <code style={codeIn}>~</code> means within 5%. The panels above and this program are one design: drag an anchor and the program is
            rewritten in canonical form; edit the program and the panels re-shape.
          </p>
        </div>
      </div>
    </div>
  );
}

function Eqn({ children, note: n }) {
  return (
    <div style={{ margin: "8px 0" }}>
      <div style={{ fontFamily: FONT.mono, fontSize: 13, color: C.ink, background: C.panel, border: `1px solid ${C.hair}`, borderLeft: `3px solid ${C.copper}`,
        borderRadius: 3, padding: "10px 14px", overflowX: "auto", whiteSpace: "nowrap" }}>{children}</div>
      {n && <div style={{ fontFamily: FONT.serif, fontSize: 12.5, color: C.mute, margin: "4px 2px 0", lineHeight: 1.5 }}>{n}</div>}
    </div>
  );
}
function ProjectionPanel() {
  return (
    <div>
      <p style={{ ...note, color: C.inks, fontSize: 13.5, marginTop: 0 }}>
        The spanwise coordinate is now the physical distance y from the center line, 0 ≤ y ≤ b/2, mirrored for the other side. Every
        law is an anchor-spline in y, so slopes are dimensionless and read directly as angles.
      </p>
      <Eqn note="Chord is the gap between the two edge curves; taper is its value at the tip over its value at the center.">
        c(y) = x_TE(y) − x_LE(y)        λ = c(b/2) / c(0)</Eqn>
      <Eqn note="The station reference point rides the quarter-chord spine and the out-of-plane curve.">
        r(y) = ( x_LE(y) + ¼·c(y),  y,  z(y) )        Γ(y) = atan z′(y)</Eqn>
      <Eqn note="Section parameters blend between stations i and i+1; cosine blending eases in and out of each station.">
        σ(y) = (t, x_t, m, x_m)(y) = σᵢ + w(s)·(σᵢ₊₁ − σᵢ),   s = (y − yᵢ)/(yᵢ₊₁ − yᵢ),   w = ½(1 − cos πs)</Eqn>
      <Eqn note="The base thickness Φ is stretched so its maximum u* lands at x_t. Φ′(u*) = 0, so the stretched profile stays smooth there.">
        y_t(ξ) = (t/2)·Φ(u(ξ))/Φ(u*),   u = u*·ξ/x_t  (ξ ≤ x_t),   u = u* + (1 − u*)(ξ − x_t)/(1 − x_t)  (ξ &gt; x_t)</Eqn>
      <Eqn note="Scale by chord, rotate by twist φ about the span axis, bank by local Γ, translate to the reference point.">
        P(ξ, ζ; y) = r(y) + c(y)·D(Γ(y))·R(φ(y))·[ (ξ − ¼)·ê_x + ζ·ê_z ]</Eqn>
      <Eqn note="What the out-of-plane panel reports: lows and highs, returns through 0, curvature flips — each as a distance from center.">
        turns = {"{"} y : z′ changes sign {"}"}    zeros = {"{"} y : z changes sign {"}"}    inflections = {"{"} y : z″ changes sign {"}"}</Eqn>
      <Eqn note="The canonical form is the emitted program; hashing it gives the foil a stable identity.">
        id(F) = H( canon( ⟦ parse(s) ⟧ ) )</Eqn>
    </div>
  );
}

const EBNF = `foil      = "foil" string "{" { stmt } "}" ;
stmt      = "units" "mm" | "span" length | context | planform
          | "dihedral" curve_z | "twist" curve_deg | sections
          | loft | constrain ;

station   = "center" | "tip" | length | number "%" ;   (* % of half span *)
length    = number [ "mm" | "cm" | "m" ] ;
pct       = number "%" | fraction ;                    (* fraction <= 1 *)

curve_x   = mode "{" { "at" station [ "x" ] length } "}" ;
curve_z   = mode "{" { "at" station zval | "bank" bool } "}" ;
zval      = "level" | "drop" length | "rise" length | [ "z" ] length ;
curve_deg = mode "{" { "at" station number [ "deg" ] } "}" ;
mode      = "linear" | "smooth" | "monotone" ;

planform  = "planform" "{" { "leading" curve_x | "trailing" curve_x
          | "chord" ("elliptic"|"linear") "center" length
            ( "tip" length | "taper" number ) [ "sweep" number "deg" ] } "}" ;

sections  = "sections" [ "linear" | "cosine" ] "{" { "at" station source } "}" ;
source    = "naca" digits4 [ "-" digits2 ]              (* -IT: T = max-t position *)
          | "profile" "{" [ "family" ("naca"|"cst") ]
              "thickness" pct [ "at" pct ]              (* where thickness peaks *)
              [ "camber" pct [ "at" pct ] ] "}"
          | "eppler" ident | "file" string ;

constrain = "constrain" "{" { metric relop number [ unit ] } "}" ;
metric    = "area" | "dev_area" | "aspect" | "taper" | "mac"
          | "center_chord" | "tip_chord" | "tip_rise"
          | "max_drop" | "max_rise" | "root_thick" | "tip_thick" ;
relop     = ">=" | "<=" | "==" | "~" ;

(* static semantics: every curve and the section list start at center,
   end at tip and move outward; trailing edge stays aft of leading edge;
   15% <= x_t <= 65%; 1% <= t <= 35%; units must match each metric *)`;

const NAV = [
  { id: "planform", t: "Planform · edge curves" },
  { id: "outofplane", t: "Drop & rise" },
  { id: "sections", t: "Section schedule" },
  { id: "projection", t: "Projection" },
  { id: "program", t: "FoilDSL v3" },
];
function Card({ id, t, sub, children }) {
  return (
    <section id={id} style={{ scrollMarginTop: 16, marginBottom: 28 }}>
      <h2 style={{ fontFamily: FONT.sans, fontWeight: 600, fontSize: 19, color: C.ink, margin: "0 0 8px", letterSpacing: "-.01em", borderBottom: `2px solid ${C.copper}`, paddingBottom: 7 }}>{t}</h2>
      {sub && <p style={{ fontFamily: FONT.serif, fontSize: 14, lineHeight: 1.6, color: C.inks, margin: "0 0 14px", maxWidth: 820 }}>{sub}</p>}
      <div style={{ background: C.cream, border: `1px solid ${C.rule}`, borderRadius: 6, padding: 18 }}>{children}</div>
    </section>
  );
}

export default function FoilDSLExplorerV3() {
  const [src, setSrc] = useState(DEFAULT_DSL);
  const [design, setDesignRaw] = useState(() => parseFoil(DEFAULT_DSL));
  const [status, setStatus] = useState({ ok: true });
  const [active, setActive] = useState("planform");
  const reduce = useRef(false);
  const toErr = (e) => (e && e.line ? e : { line: "?", msg: String((e && e.message) || e) });
  const setDesign = useCallback((d) => {
    setDesignRaw(d);
    const s = emitDSL(d); setSrc(s);
    try { parseFoil(s); setStatus({ ok: true }); } catch (e) { setStatus({ ok: false, err: toErr(e) }); }
  }, []);
  const onSrc = useCallback((text) => {
    setSrc(text);
    try { setDesignRaw(parseFoil(text)); setStatus({ ok: true }); } catch (e) { setStatus({ ok: false, err: toErr(e) }); }
  }, []);
  const onReset = useCallback(() => onSrc(DEFAULT_DSL), [onSrc]);
  const g = useMemo(() => deriveGeom(design), [design]);

  useEffect(() => {
    const id = "ibm-plex-foildsl3";
    if (!document.getElementById(id)) {
      const l = document.createElement("link"); l.id = id; l.rel = "stylesheet";
      l.href = "https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600&family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Serif:ital,wght@0,400;0,500;1,400&display=swap";
      document.head.appendChild(l);
    }
    reduce.current = !!(window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches);
    if (!("IntersectionObserver" in window)) return undefined;
    const obs = new IntersectionObserver((es) => es.forEach((e) => { if (e.isIntersecting) setActive(e.target.id); }), { rootMargin: "-25% 0px -65% 0px" });
    NAV.forEach((s) => { const el = document.getElementById(s.id); if (el) obs.observe(el); });
    return () => obs.disconnect();
  }, []);
  const go = (id) => { const el = document.getElementById(id); if (el) el.scrollIntoView({ behavior: reduce.current ? "auto" : "smooth", block: "start" }); };
  const cons = evalConstraints(design, g);

  return (
    <div style={{ background: C.cream, minHeight: "100%", fontFamily: FONT.serif, color: C.ink }}>
      <style>{CSS}</style>
      <header style={{ background: C.cream2, borderBottom: `1px solid ${C.rule}`, padding: "22px 26px 18px", position: "relative" }}>
        <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 4, background: C.copper }} />
        <div style={{ display: "flex", flexWrap: "wrap", gap: 20, alignItems: "flex-start" }}>
          <div style={{ flex: "1 1 340px", minWidth: 280 }}>
            <div style={{ fontFamily: FONT.mono, fontSize: 10.5, letterSpacing: ".14em", color: C.copperD, marginBottom: 6 }}>ENGINEERING LEDGER · FoilDSL EXPLORER · v3</div>
            <h1 style={{ fontFamily: FONT.sans, fontWeight: 700, fontSize: 25, lineHeight: 1.15, color: C.ink, margin: "0 0 8px", letterSpacing: "-.02em" }}>
              Stations from the center line.<br /><span style={{ color: C.copperD, fontWeight: 500 }}>Curves for every edge and law.</span>
            </h1>
            <p style={{ fontFamily: FONT.serif, fontSize: 14, color: C.inks, margin: "0 0 12px", lineHeight: 1.55 }}>
              Shape the leading and trailing edges, place every drop and rise as a distance from center, and schedule sections by
              thickness and where it peaks. The assembled foil, its taper and its turning points update as you drag — and the program
              below is rewritten to match.
            </p>
            <MetricRow g={g} />
            <div style={{ fontFamily: FONT.mono, fontSize: 10.5, color: cons.every((c) => c.ok) ? C.sage : C.rust, marginTop: 8 }}>
              {cons.filter((c) => c.ok).length} / {cons.length} constraints satisfied · id {fnv1a(emitDSL(design))}{status.ok ? "" : " · program has an error"}
            </div>
          </div>
          <div style={{ flex: "1 1 420px", minWidth: 300 }}>
            <ThreeDView g={g} bank={design.dihedral ? design.dihedral.bank : true} height={250} nSt={11} />
          </div>
        </div>
        <nav style={{ display: "flex", gap: 6, marginTop: 16, flexWrap: "wrap" }} aria-label="sections">
          {NAV.map((s) => (
            <button key={s.id} onClick={() => go(s.id)} aria-current={active === s.id ? "true" : undefined}
              style={{ cursor: "pointer", fontFamily: FONT.mono, fontSize: 11, padding: "6px 11px", borderRadius: 4, border: `1px solid ${active === s.id ? C.copperD : C.rule}`,
                background: active === s.id ? C.copper : C.cream, color: active === s.id ? C.cream : C.inks }}>{s.t}</button>
          ))}
        </nav>
      </header>
      <main style={{ maxWidth: 1080, margin: "0 auto", padding: "24px 22px 60px" }}>
        <Card id="planform" t="Planform — leading and trailing edges as curves"
          sub="Each edge is an anchor-spline of streamwise position against distance from the center. Chord, taper (tip chord over center chord), the quarter-chord spine, area and aspect ratio are derived from the pair.">
          <PlanformPanel design={design} setDesign={setDesign} g={g} />
        </Card>
        <Card id="outofplane" t="Drop & rise — dihedral, anhedral, gull, tip lift"
          sub="The out-of-plane curve is authored as drop or rise from 0 at named distances from the center. The tool finds where the foil turns down, bottoms out, comes back through 0 and lifts — and how far, in millimetres.">
          <DihedralPanel design={design} setDesign={setDesign} g={g} />
        </Card>
        <Card id="sections" t="Section schedule — thickness and where it peaks"
          sub="A 3D foil carries several sections. Each station fixes thickness, max-thickness position, camber and max-camber position; between stations they blend, so the shape evolves along the span.">
          <SectionsPanel design={design} setDesign={setDesign} g={g} />
        </Card>
        <Card id="projection" t="The projection, in physical coordinates"
          sub="Same denotational structure as the white paper; the spanwise laws are now curves in millimetres and the section is a four-parameter family.">
          <ProjectionPanel />
        </Card>
        <Card id="program" t="FoilDSL v3 — the program behind the panels"
          sub="Parsed on every keystroke by a recursive-descent parser. Break the syntax or the static semantics and you get a line and a reason; the views keep the last well-formed shape.">
          <EditorPanel src={src} onSrc={onSrc} status={status} design={design} g={g} onReset={onReset} />
          <div style={{ marginTop: 18 }}>
            <span style={miniHead}>CONCRETE SYNTAX · EBNF</span>
            <pre style={{ fontFamily: FONT.mono, fontSize: 10.5, lineHeight: 1.5, color: C.ink, background: C.panel, border: `1px solid ${C.hair}`,
              borderLeft: `3px solid ${C.copper}`, borderRadius: 3, padding: "12px 14px", overflow: "auto", margin: 0, whiteSpace: "pre" }}>{EBNF}</pre>
          </div>
        </Card>
        <footer style={{ borderTop: `1px solid ${C.rule}`, paddingTop: 14, display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 8, fontFamily: FONT.mono, fontSize: 10, color: C.mute }}>
          <span>FoilDSL Explorer v3 · engineering ledger</span>
          <span>splines, turning points, areas and sections computed live in the browser</span>
        </footer>
      </main>
    </div>
  );
}

/* ---------------- style atoms ---------------- */
const svgFluid = { width: "100%", height: "auto", display: "block", background: C.cream, border: `1px solid ${C.hair}`, borderRadius: 4 };
const lbl = { fontFamily: FONT.mono, fontSize: 9, letterSpacing: ".06em", color: C.mute, marginBottom: 5, display: "block" };
const miniHead = { fontFamily: FONT.mono, fontSize: 10, letterSpacing: ".1em", color: C.copperD, fontWeight: 600, marginBottom: 8, display: "block" };
const chip = { display: "flex", flexDirection: "column", gap: 1, background: C.panel, border: `1px solid ${C.hair}`, borderRadius: 4, padding: "5px 9px", minWidth: 50 };
const readout = { background: C.panel, border: `1px solid ${C.hair}`, borderRadius: 4, padding: "8px 11px", fontFamily: FONT.mono, fontSize: 10.5, color: C.inks, lineHeight: 1.7 };
const readHead = { color: C.mute, fontSize: 9, letterSpacing: ".05em", marginBottom: 2 };
const warnBox = { fontFamily: FONT.mono, fontSize: 10.5, color: C.inks, background: "#9E4A3312", border: `1px solid ${C.rust}44`, borderRadius: 3, padding: "7px 10px", lineHeight: 1.5 };
const note = { fontFamily: FONT.serif, fontSize: 12.5, color: C.mute, lineHeight: 1.6, margin: "14px 2px 0" };
const codeIn = { fontFamily: FONT.mono, fontSize: 11, color: C.copperD, background: C.panel, padding: "0 4px", borderRadius: 2, margin: "0 2px" };
const tbl = { width: "100%", borderCollapse: "collapse", fontFamily: FONT.mono, fontSize: 11, background: C.panel, border: `1px solid ${C.hair}` };
const th = { textAlign: "left", padding: "6px 10px", color: C.cream, fontFamily: FONT.sans, fontSize: 9.5, fontWeight: 600, letterSpacing: ".04em" };
const td = { padding: "6px 10px", color: C.inks };
const astBtn = { all: "unset", cursor: "pointer", fontFamily: FONT.mono, fontSize: 11, lineHeight: 1.65, display: "block" };
const seg = (on) => ({ cursor: "pointer", fontFamily: FONT.mono, fontSize: 10, padding: "6px 10px", borderRadius: 3, border: `1px solid ${on ? C.copperD : C.rule}`, background: on ? C.copper : C.cream, color: on ? C.cream : C.inks });
const segMini = (on) => ({ cursor: "pointer", fontFamily: FONT.mono, fontSize: 9.5, padding: "4px 8px", borderRadius: 3, border: `1px solid ${on ? C.copperD : C.rule}`, background: on ? C.copperL : C.cream, color: on ? "#fff" : C.inks });
const CSS = `
  button:focus-visible, textarea:focus-visible, input:focus-visible, .fd-anchor:focus-visible { outline: 2px solid ${C.copper}; outline-offset: 2px; }
  .fd-anchor:focus { stroke: ${C.copper}; stroke-width: 3; }
  button:disabled { opacity: .45; cursor: default; }
  .fd-range { accent-color: ${C.copper}; }
  textarea::selection { background: ${C.copperL}55; }
  @media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto !important; } }
`;
