---
id: a3a-vlm3c-camber-normals
title: VLM-3c control-point camber normal proof
type: proof-pack
status: verified
owner: "@vlm3c"
phase: implementation
tags: [analysis, vlm, camber, red-first]
links:
  - { to: design-area3-analysis, rel: tested-by }
  - { to: note-area3-fixture-arithmetic, rel: relates-to }
review-by: "2027-04-04"
summary: "F-21 fails on corner normals and passes on control-point camber slope normals; the existing spanwise and flat-wing fixtures remain green."
---

# VLM-3c control-point camber normal proof

The red-first commit is `0af3043`. In Release Analysis on macOS arm64, .NET 10.0.203,
`CFD_TEST_ONLY=F21_` printed `FAIL F21_ParabolicCamber_ZeroLiftAngleThinAirfoil`:
α_L0 **−3.392477°** against thin-airfoil **−4.583662° ± 0.25°**. The final test narrows the
band to ± 0.05° after measuring the AR 40 finite-span offset. It retains the independent
thin-airfoil target, not a target calculated from the product lattice.

The repair reads `SectionSample.CamberSlope` at each three-quarter-panel control point.
It rotates the local chord tangent by twist and crosses it with the elevated bound segment;
the panel corners still determine area. The corrected F-21 prints α_L0 **−4.593225°**,
`PASS`, `COST 3.392` ms. The correction leaves the `cfdw.vlm-strip` version at 1.1.0.

The planted mutant changed the product normal back to the corner diagonal cross product,
with the F-21 test unchanged. It printed `FAIL` at **−3.392477° ± 0.05°** and exit 1.
Restoring the corrected source printed `PASS` at −4.593225° and exit 0. The mutant was
removed before commit.

The full old→new measurement table is in [the arithmetic note](../../notes/area3-fixture-arithmetic.md#vlm-3c-control-point-camber-slope-2026-10-04).
At n64→n128, F-18 tip α_i differs by 0.01944° and CL by 0.191%; F-19 differs by
0.00542° and 0.205%. F-4 symmetry passes; F-6 CL/e/order and F-20 sweep remain at
their printed flat-wing values. F-18/F-19 n256 and F-20 n128/256 readiness checks pass.

The changed surface is the lattice normal and its section-sample fixture. The source
`CamberSlope` field is interpolated through the existing `At` path; the `Horseshoe.Normal`
reader assembles no-penetration rows. The `MethodRecord` version, run settings, and wire
shape are unchanged. Residual risk: the outermost tip-strip envelope verdict still awaits
Ruling 75's η* law; the near-field/Trefftz chordwise gap recorded in the arithmetic note
is independent of this repair.

## Gates

| Gate | Result |
|---|---|
| `tools/run-tests.sh` (one invocation) | All five suites green: Core parts 1/2 and 2/2 each `RESULT failures=0` (76 s), Desktop `SUITE --analysis exit 0` (75 s), Analysis `RESULT failures=0` (8 s), Cli PASS lines (2 s). Script exit **3** because its 60 s wall budget was exceeded; no suite failed. Its own retained per-suite logs are under `.tmp-tests/`. |
| VLM named-test checker, after retaining the three readiness PASS lines | `(VLM) 20/20 named tests PASS · 0 failures` |
| `python3 tools/check-docs.py` | `Documentation checks passed.` Exit 0 after `docs-graph.py derive` indexed this proof. The 131 pre-existing V16 review suggestions remain warnings. |

The first named-test invocation was 17/20 because `run-tests.sh` clears the
`.tmp-tests/*.log` files, including prior readiness results. The three readiness checks
were rerun into `.tmp-tests/Readiness.log`; the second invocation passed 20/20.
`check-docs.py` first found this newly added proof absent from `docs/docs-index.js`;
the prescribed `docs-graph.py derive` fixed that index drift, and the second run passed.
