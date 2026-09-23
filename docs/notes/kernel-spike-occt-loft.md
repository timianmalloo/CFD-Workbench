---
id: kernel-spike-occt-loft
title: Kernel spike — OCCT ThruSections loft against the owned evaluator (A4.12 exit evidence)
type: decision-note
status: in-review
owner: "@timianmalloo"
tags: [geometry, kernel, occt, loft, step, spike, evidence]
links:
  - {to: spec-cfd-workbench-v1, rel: refines}
  - {to: control-vertex-workspace, rel: relates-to}
  - {to: adr-0001-master-curve-degree, rel: relates-to}
  - {to: kb-hydrofoil-workbench, rel: relates-to}
review-by: 2027-03-21
summary: The Spike Protocol run on the geometry kernel decision of specification 1.3 A4.12 — OCCT 7.8.1 (via FreeCAD 1.1.1 headless, macOS arm64) lofting N exact section B-splines against the owned evaluator's rule-A surface at 50 × 200 closest-point samples, with a STEP round trip. Base and maximum-twist cases meet the 10 µm acceptance from N = 16 sections (1.1 µm and 0.5 µm; 0.8/0.4 µm at N = 64); the zero-chord tip does not converge with uniform sections (2.5–18 mm) and needs its own rule. Windows x64 and the licence review remain open.
review-suggested:
  - { by: adr-0001-master-curve-degree, on: 2026-09-21, reason: "ADR-0001 decides the master-curve degree (3, seven vertices, stored per curve); the spec's Open decisions and A4.1/A4.2 cite it" }
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "FoilDSL 4.0 canonical authoring proposal changes source ownership, editing transactions and provenance; review dependent artifacts." }
---

# Kernel spike — OCCT ThruSections loft against the owned evaluator

*Spike Protocol, Move 2 (verify by execution). Throwaway code in `spikes/kernel-loft/` (the record exported from mockup v5, the evaluator ported to plain Python, `spike_loft.py` run under `freecadcmd`); this note is the durable part. Time-boxed to one session.*

## Contract exercised

**A side (ours):** the record of specification 1.3 — five degree-3 clamped B-spline channels (seven vertices each), the degree-5 section pair (twelve vertices per side, knots by averaging), rule A (channel-evaluated: every η gets the section under `stationXform(η)` — LE pivot, positive twist nose-up, thickness scaled to the t/c channel). Evaluated with a de Boor port of the mockup's evaluator (`find_span`, `basis`, `evalc`), η solved from x(t) by bisection.
**B side (the candidate kernel):** `Part.makeLoft(wires, solid=False, ruled=False, closed=False, maxDegree=5)` — OCCT `BRepOffsetAPI_ThruSections` — over N station wires whose edges are the *exact* section B-splines (poles under the affine station transform, knots kept; `Part.BSplineCurve.buildFromPolesMultsKnots`). STEP written with `Shape.exportStep`, read back with `Shape.read`.
**Metric:** closest-point distance (`Shape.distToShape` to a vertex) from 50 η × 200 ψ evaluator samples to the loft, and from a 7 × 7-strided subset to the STEP re-import. **Acceptance (A4.12):** ≤ 10 µm at the local chord.

Environment: FreeCAD 1.1.1 (20260414), OCCT 7.8.1, Python 3.11, macOS arm64 (Darwin). Measured 2026-09-21.

## Results (max deviation, µm; `spikes/kernel-loft/results.json`)

| Case | N = 4 | 8 | 16 | 32 | 64 | STEP round trip (N = 16) | Loft surface |
|---|---|---|---|---|---|---|---|
| base (the race-light example) | 1 060 | 351 | **1.1** | 0.9 | 0.8 | 0.2 µm | B-spline, u-degree 5, v-degree 4 |
| maximum twist (+6° at the tip) | 1 059 | 356 | **0.5** | 0.4 | 0.4 | 0.1 µm | B-spline, u 5, v 4 |
| zero-chord tip (chord → 0 over η 0.95–1, tip section a point) | 18 137 | 6 329 | 7 932 | 2 528 | 3 854 | 0.0–651 µm | B-spline, u 5, v 3–5 |

Loft time 2–80 ms; the 10 000-sample measurement 7–28 s in FreeCAD's Python (not a kernel cost). Every loft `isValid()`; two faces (upper, lower), the two edges joined at the LE.

**The first run's 73 µm floor at the root was a spike bug, not a kernel one** — the upper curve's poles were reversed without reversing a non-symmetric knot vector. Recorded because an oracle over a recorded run is itself a contract (Spike Protocol §2).

## Findings (confidence ledger)

1. **OCCT ThruSections reproduces rule A within the acceptance for a regular wing from N = 16 sections** (1.1 µm base, 0.5 µm max twist at 50 × 200 samples) and the deviation falls ~10× per doubling until N = 16, then flattens near 1 µm (the skinning's own interpolation floor). *Verified by execution.*
2. **STEP is faithful to the loft:** round-trip deviation ≤ 0.3 µm at N ≥ 8; 891 µm at N = 4 is the loft's own error, not the file's. `B_SPLINE_SURFACE_WITH_KNOTS` non-rational, as A4.11 requires. *Verified by execution (FreeCAD reader only; the two CAM readers of A4.11 remain).*
3. **Twist does not hurt the loft** (+6° at the tip: 0.5 µm at N = 16) when the sections are exact B-splines under an affine transform. *Verified.*
4. **A zero-chord tip with uniformly placed sections does not converge** (2.5–18 mm, worst just inboard of the collapse at η 0.94–0.98, whichever N). A point section at the tip plus a chord ramp that starts between sections makes the skinning overshoot; adding sections (N = 64, one at η 0.952) does not repair it because the ramp's knot (η 0.95) is never a section. **This contradicts the assumption that "more sections" is the rule**: the loft needs sections *at the record's knots and stations* (the chord channel's knots, every authored station) plus refinement, and the last span into a degenerate tip must be treated separately (a ruled last span or a capped last non-zero section, both A-vs-B measured). *Verified by execution; the rule is Inferred until measured.*
5. **v-degree is chosen by the kernel** (3 at N ≤ 8, 4 at N ≥ 16, 5 at N = 64 in the tip case): the product must pin `maxDegree` and read the result's degrees back into the record (A4.1 loft bullet) rather than assume them. *Verified.*
6. **Not established here:** the Windows x64 build of OCCT, the P/Invoke boundary from C# (this spike used FreeCAD's Python binding), the two CAM readers of A4.11, rhino3dm's 3DM write, and the LGPL 2.1 + exception reading that re-decides KB index item 6 (Security lens). *Flagged.*

## What changes in the specification

- A4.12 records the evidence: the acceptance is met for regular wings from N = 16 sections placed uniformly; **sections are placed at every chord-channel knot and every authored station, then refined until the A-vs-B measure passes**; a degenerate tip gets its own last-span rule, measured. The Open-decisions row keeps the Windows build, the C# boundary, the CAM readers and the licence review as remaining exit evidence.
- A4.1's loft bullet records the kernel's chosen v-degree with the loft id and version.

## Disposal

`spikes/kernel-loft/` is kept as a labelled, non-production fixture (the evaluator port is 60 lines and reproduces the mockup's numbers); it is never referenced by product code. Re-run: export `record.json` from the mockup, then `exec(open('spikes/kernel-loft/spike_loft.py').read())` under `freecadcmd`.
