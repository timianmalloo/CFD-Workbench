---
id: note-area3-fixture-arithmetic
title: Area 3 fixture numbers — the F-2 band, F-6 lattices and mutant, F-8 convention, from an independent lattice
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [analysis, vlm, fixtures, mutants, observed-order, richardson, area-3, test-plan]
links:
  - {to: design-area3-analysis, rel: refines}
  - {to: review-area3-analysis-personas, rel: relates-to}
  - {to: kb-hw-low-order-hydrodynamics, rel: depends-on}
review-by: 2027-04-03
summary: >-
  The numbers behind design-area3-analysis §13.2 rev 3, measured with a 56-line reference horseshoe lattice (listed
  here, run with Node 22): the F-2 lifting-surface band 0.4156–0.4198 for the elliptic AR 8 wing at 5°, F-6's lattices
  32/64/128 (observed order 1.07 for CL, 1.01 for e), an O(1) F-6 mutant that drives the order to −0.74, and the F-8
  fixture at 20° dihedral with the developed S_ref, which the dihedral-ignored mutant misses by 11.9 %.
---

# Area 3 fixture numbers from an independent lattice

**Decision (proposed, design-area3-analysis §13.2 rev 3).** F-2, F-6 and F-8 take the expected values, lattices and
mutants below. The product lattice is C# and is built later; these numbers are its oracle inputs, recorded before the
build so a wrong build can fail them. Confidence: **Verified** that this script prints these numbers (run 2026-10-03,
Node v22.22.2, 2.7 s); **Inferred** that the C# build lands inside the same bands — the band widths allow for that.

## What was run

A horseshoe vortex lattice (Katz & Plotkin §12.3): flat panels, bound vortex at the panel quarter chord, control point
at three-quarter chord, trailing legs straight along +x to 20 spans, cosine spacing over the full span, 4 uniform
chordwise panels, dense Gaussian elimination with partial pivoting. Lift is the Kutta–Joukowski force with V∞ in wind
axes. Induced drag is the Trefftz-plane sum. This is an independent check of the CFD lens' numpy re-run (rev 1 gate),
not the product method.

## Results (verbatim output)

```text
F-8 dihedral 10 deg: CL ratio developed 0.9727, projected 0.9877; mutant 1.0000 misses developed by 2.81 %, projected by 1.25 %
F-8 dihedral 20 deg: CL ratio developed 0.8938, projected 0.9511; mutant 1.0000 misses developed by 11.89 %, projected by 5.14 %
F-2/F-6 correct 16/32/64: CL 0.42413 e 1.03946 | CL 0.42062 e 1.01882 | CL 0.41907 e 1.00861 -> p(CL) 1.183 p(e) 1.016, Richardson CL 0.41785 e 0.99864
F-2/F-6 correct 32/64/128: CL 0.42062 e 1.01882 | CL 0.41907 e 1.00861 | CL 0.41833 e 1.00356 -> p(CL) 1.070 p(e) 1.013, Richardson CL 0.41766 e 0.99859
F-2/F-6 mutant wake per panel 32/64/128: CL 0.44561 e 1.01734 | CL 0.46884 e 1.00410 | CL 0.50750 e 0.99483 -> p(CL) -0.735 p(e) 0.515, Richardson CL 0.41064 e 0.97322
F-2/F-6 mutant control point mid-panel 32/64/128: CL 0.24017 e 1.01841 | CL 0.23954 e 1.00823 | CL 0.23929 e 1.00327 -> p(CL) 1.364 p(e) 1.036, Richardson CL 0.23914 e 0.99854
```

## What each fixture takes from it

| Fixture | Expected · tolerance | Mutant and why it now fails |
|---|---|---|
| **F-2** elliptic AR 8, α 5° | Richardson CL over 32/64/128 per half in **[0.4156, 0.4198]** = 0.4177 ± 0.5 %. Reference 0.41766 (32/64/128); the 16/32/64 estimate 0.41785 agrees to 0.05 %; the CFD lens' numpy value 0.4167 (128 spanwise × 6 chordwise, not extrapolated) is inside. Richardson e within 1.000 ± 0.005 (here 0.99859). | Control point at mid-panel: Richardson CL 0.23914, 42.7 % below the reference and far outside the band. Inputs that also fail the band: lifting line 0.4386, Helmbold 0.4282, Jones factor ≈ 0.427, an unextrapolated 32-span CL 0.42062 |
| **F-6** observed order, same wing | lattices **32/64/128** per half (not 16/32/64: p(CL) 1.183 there sits 0.017 inside the 1.2 edge). p(CL) 1.070 and p(e) 1.013, both in 1.0 ± 0.2 | **Wake length read per panel, not per wing** (`wakeSpans` × panel span — a plausible mix-up of the setting's name). It is an O(1) error that grows with refinement: CL +5.9 %, +11.9 %, +21.3 % against the correct lattice at 32, 64, 128. The differences grow, so p(CL) = −0.735 and p(e) = 0.515 — both outside 1.0 ± 0.2. The rev 2 mutant (one strip off) is an O(h) error that leaves p near 1 (test lens M-T2; Inferred, not run here) |
| **F-8** dihedral, Example-foil rectangle (b 0.9 m, c 0.12 m), α 5°, 32 × 4 | **±20°**, S_ref **developed** (b·c; pinned, so the expected value has one meaning): CL ratio to planar **0.8938 ± 1 %** | Dihedral ignored in placement: ratio 1.0000, **11.89 % above** — 12 × the tolerance. At rev 2's ±10° the same mutant missed by 2.81 % (developed) and 1.25 % (projected; the CFD lens' wing gave 0.8 %), too close to a 1 % tolerance. The projected convention at 20° would also fail it (5.14 %), but the developed one is pinned |

Cost: the three F-6 lattices take ≈ 0.8 s here (Node, naive arrays); F-2 reuses F-6's solves. The C# cost is measured at
red-first (design §13.4).

## The script

```js
'use strict';
// Reference horseshoe vortex lattice (Katz & Plotkin 12.3), flat panels, dihedral, straight wake along +x.
// Used only to set the F-2 band, check F-6 order and the F-8 / F-6 mutants (area3 rev 3). Not product code.
const RHO = 1000, V = 1, RAD = Math.PI / 180;
function seg(P, A, B){
  const r1 = [P[0]-A[0], P[1]-A[1], P[2]-A[2]], r2 = [P[0]-B[0], P[1]-B[1], P[2]-B[2]], r0 = [B[0]-A[0], B[1]-A[1], B[2]-A[2]];
  const c = [r1[1]*r2[2]-r1[2]*r2[1], r1[2]*r2[0]-r1[0]*r2[2], r1[0]*r2[1]-r1[1]*r2[0]], c2 = c[0]*c[0]+c[1]*c[1]+c[2]*c[2];
  if (c2 < 1e-20) return [0, 0, 0];
  const n1 = Math.hypot(...r1), n2 = Math.hypot(...r2), k = (r0[0]*(r1[0]/n1-r2[0]/n2)+r0[1]*(r1[1]/n1-r2[1]/n2)+r0[2]*(r1[2]/n1-r2[2]/n2)) / (4*Math.PI*c2);
  return [c[0]*k, c[1]*k, c[2]*k];
}
// opts: half, chord(u) -> [xLE, c], ns per half, nc, alphaDeg, dihDeg; mutants: ignoreDihedral, wakePerPanel, cpMid
function solve(o){
  const g = (o.ignoreDihedral ? 0 : (o.dihDeg || 0)) * RAD, a = o.alphaDeg * RAD, b = 2 * o.half;
  const uE = []; for (let i = 0; i <= 2 * o.ns; i++) uE.push(-o.half * Math.cos(Math.PI * i / (2 * o.ns)));
  const pos = (u, x) => [x, u * Math.cos(g), Math.abs(u) * Math.sin(g)];
  const far = o.wakePerPanel ? 20 * b / (2 * o.ns) : 20 * b;
  const H = [];
  for (let s = 0; s < 2 * o.ns; s++){
    const ua = uE[s], ub = uE[s + 1], um = (ua + ub) / 2, [xa, ca] = o.chord(ua), [xb, cb] = o.chord(ub), [xm, cm] = o.chord(um);
    for (let k = 0; k < o.nc; k++){
      const f0 = k / o.nc, f1 = (k + 1) / o.nc, fb = f0 + (f1 - f0) / 4, fc = f0 + (o.cpMid ? 2 : 3) * (f1 - f0) / 4;
      const A = pos(ua, xa + fb * ca), B = pos(ub, xb + fb * cb), Cp = pos(um, xm + fc * cm);
      const side = um >= 0 ? 1 : -1, n = [0, -side * Math.sin(g), Math.cos(g)];
      H.push({ s, A, B, Cp, n, Af: [far, A[1], A[2]], Bf: [far, B[1], B[2]], ua, ub, um });
    }
  }
  const N = H.length, Vinf = [V * Math.cos(a), 0, V * Math.sin(a)];
  const M = H.map(hi => H.map(hj => { const v1 = seg(hi.Cp, hj.Af, hj.A), v2 = seg(hi.Cp, hj.A, hj.B), v3 = seg(hi.Cp, hj.B, hj.Bf);
    return (v1[0]+v2[0]+v3[0])*hi.n[0] + (v1[1]+v2[1]+v3[1])*hi.n[1] + (v1[2]+v2[2]+v3[2])*hi.n[2]; }));
  const rhs = H.map(h => -(Vinf[0]*h.n[0] + Vinf[1]*h.n[1] + Vinf[2]*h.n[2]));
  for (let i = 0; i < N; i++){ let m = i; for (let r = i + 1; r < N; r++) if (Math.abs(M[r][i]) > Math.abs(M[m][i])) m = r;
    [M[i], M[m]] = [M[m], M[i]]; [rhs[i], rhs[m]] = [rhs[m], rhs[i]];
    for (let r = i + 1; r < N; r++){ const f = M[r][i] / M[i][i]; if (!f) continue; for (let c = i; c < N; c++) M[r][c] -= f * M[i][c]; rhs[r] -= f * rhs[i]; } }
  const G = new Array(N); for (let i = N - 1; i >= 0; i--){ let s = rhs[i]; for (let c = i + 1; c < N; c++) s -= M[i][c] * G[c]; G[i] = s / M[i][i]; }
  let L = 0; const strips = [];
  H.forEach((h, i) => { const l = [h.B[0]-h.A[0], h.B[1]-h.A[1], h.B[2]-h.A[2]], F = [Vinf[1]*l[2]-Vinf[2]*l[1], Vinf[2]*l[0]-Vinf[0]*l[2], Vinf[0]*l[1]-Vinf[1]*l[0]];
    L += RHO * G[i] * (-F[0] * Math.sin(a) + F[2] * Math.cos(a)); });
  for (let s = 0; s < 2 * o.ns; s++){ const hs = H.filter(h => h.s === s); strips.push({ ya: hs[0].ua, yb: hs[0].ub, y: hs[0].um, dy: hs[0].ub - hs[0].ua, G: hs.reduce((t, h) => t + G[H.indexOf(h)], 0) }); }
  let Di = 0; for (const t of strips){ let w = 0; for (const q of strips) w -= q.G / (2 * Math.PI) * (1 / (t.y - q.ya) - 1 / (t.y - q.yb)); Di += -0.5 * RHO * t.G * w * t.dy; }
  return { L, Di, b, N };
}
const q = 0.5 * RHO * V * V;
const rect = { half: 0.45, chord: () => [0, 0.12], ns: 32, nc: 4, alphaDeg: 5 }, L0 = solve(rect).L;   // Example-foil rectangle, AR 7.5
for (const d of [10, 20]){
  const dev = solve({ ...rect, dihDeg: d }).L / L0, mut = solve({ ...rect, dihDeg: d, ignoreDihedral: true }).L / L0, proj = dev / Math.cos(d * RAD);
  console.log(`F-8 dihedral ${d} deg: CL ratio developed ${dev.toFixed(4)}, projected ${proj.toFixed(4)}; mutant ${mut.toFixed(4)} misses developed by ${((mut / dev - 1) * 100).toFixed(2)} %, projected by ${((mut / proj - 1) * 100).toFixed(2)} %`);
}
const half = 1, c0 = 2 * half / (2 * Math.PI);    // elliptic AR 8: S = pi b c0 / 4
const ell = { half, chord: u => { const c = c0 * Math.sqrt(Math.max(0, 1 - (u / half) ** 2)); return [-c / 4, c]; }, nc: 4, alphaDeg: 5 };
const S = Math.PI * 2 * half * c0 / 4, AR = 4 * half * half / S;
for (const [label, lattices, m] of [['correct', [16, 32, 64], {}], ['correct', [32, 64, 128], {}], ['mutant wake per panel', [32, 64, 128], { wakePerPanel: true }], ['mutant control point mid-panel', [32, 64, 128], { cpMid: true }]]){
  const rows = lattices.map(ns => { const r = solve({ ...ell, ns, ...m }); const CL = r.L / (q * S); return { ns, CL, e: CL * CL / (Math.PI * AR * r.Di / (q * S)) }; });
  const ord = k => Math.log((rows[1][k] - rows[0][k]) / (rows[2][k] - rows[1][k])) / Math.log(2), rich = k => rows[2][k] + (rows[2][k] - rows[1][k]) / (2 ** ord(k) - 1);
  console.log(`F-2/F-6 ${label} ${lattices.join('/')}: ` + rows.map(r => `CL ${r.CL.toFixed(5)} e ${r.e.toFixed(5)}`).join(' | ') + ` -> p(CL) ${ord('CL').toFixed(3)} p(e) ${ord('e').toFixed(3)}, Richardson CL ${rich('CL').toFixed(5)} e ${rich('e').toFixed(5)}`);
}
```

## C# product lattice (track VLM, 2026-10-04)

Same planform, uniform chordwise spacing, cosine span, wake length 20 spans measured from the bound vortex (the script's far station is the absolute x = 20·b; the bound sits near x = 0, so the two differ by the bound's x). Release build, ρ = 1000, V = 1.

| n per half | CL | e |
|---:|---:|---:|
| 32 | 0.42061592925327934 | 1.0188185139225341 |
| 64 | 0.41906892443595917 | 1.008614926943703 |
| 128 | 0.41833189863013637 | 1.0035578493401354 |

p(CL) = 1.0696906502361061, p(e) = 1.0127005850952371. Richardson CL = 0.41766125527921233, Richardson e = 0.99858864943710468.

The planted wake-per-panel mutant (wake length = 20 spans × one panel's span) gives p(CL) = −0.63208432840675044, outside 1 ± 0.2.
