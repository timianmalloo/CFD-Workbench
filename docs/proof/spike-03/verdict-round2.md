---
id: proof-spike-03-round2
title: SPIKE-03 round 2 verdict — wall-resolved meshing at AR 8 (snappyHexMesh and a Gmsh probe) and the solver-security launcher
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-03, openfoam, snappyhexmesh, gmsh, mesh-gate, security, launcher, round-2]
links:
  - {to: proof-spike-03, rel: supersedes}
  - {to: plan-fluids-round2, rel: implements}
  - {to: rulings, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: proof-spike-04-round2, rel: relates-to}
review-by: 2026-11-03
summary: >-
  Meshing NO-GO at AR 8, so AR 5 and 12 were not run. snappyHexMesh with wall-resolved absolute layers (8 um, ER 1.2,
  20 layers) on the finite-TE, round-tip analysis wing stops at 14-15 layers (0 % of faces reach 20; 10 or more on
  89 % / 75 %). The Gmsh boundary-layer probe puts 20 layers on 100 % of the wing faces by construction. Every wall-
  resolved mesh fails the DR-F2-6 floor (cellDeterminant >= 0.001) on its thin wall cells, and the gate's
  non-orthogonality limit. Security: the product launcher printed Disallowing on every round-2 process; the probe set
  S-1..S-8 is ready for the operator, not yet run. macOS arm64 only.
review-suggested: []
---

# SPIKE-03 round 2 verdict — wall-resolved meshing and the solver-security launcher

Lane F1, round 2. Authority: [plan](../../plans/fluids-round2.md) §3 to §5, [Ruling 65](../../notes/rulings.md)
(DR-F2-3 wall-function feasibility bound and y+/layer floors; DR-F2-4 finite TE and rounded tip for analysis; DR-F2-5
Gmsh for the probe only; DR-F2-6 cellDeterminant ≥ 0.001 for OpenFOAM meshes; DR-F2-7 macOS only; DR-F2-8 S-1
operator-run). Round 1: [verdict.md](verdict.md) (kept unchanged). Lead lens: CFD numerical verification; the hydrofoil
hydrodynamicist reviewed the physical claims and the security & identity architect the launcher. Every number is read
from a file under [receipts/](receipts/) (`*-spike03r2-*` directories), unless labelled **Estimate** or **Inferred**.
Every wing result here is labelled **TE blunted for analysis; fully turbulent; transition not modelled**.

## Verdict

| Plan question | Result |
|---|---|
| Q3 security: launcher before any run, "Allowing" stops all runs | **built and used**: every round-2 OpenFOAM process (TMR and wing) printed `Disallowing`; no stop file was ever written. The live probe set is **ready, not run** (operator, below) |
| Q2 snappy route (L1 + L2) at AR 8, two variants | **fails** the DR-F2-3 layer floor: max 14–15 layers, 0 % of faces with 20 |
| Q2 Gmsh probe (L4) at AR 8 | **meets the layer coverage** (20 layers on 100 % of the wing faces, counted) after repair 2 of 2; **fails** the DR-F2-6 determinant floor and the non-orthogonality limit (86°); y+ screen p95 1.26, max 2.29, just above the DR-F2-3 numbers (1, 2) on a 500-iteration field |
| DR-F2-6 floor, any wall-resolved layer mesh | **fails by construction** (measured on all three meshes; arithmetic below) |
| AR 5 and AR 12 (M-3) | **not run**: no route passed coverage + y+ at AR 8, and the Q2 repair cap (2) is spent (M-1b's solver entry, the Gmsh set-up) — plan stop rule |
| Both OSes | macOS arm64 only (DR-F2-7); Windows stays open |

**SPIKE-03: NO-GO** against the Appendix R words "unattended … passing the ITTC mesh floors", now read with the
Ruling 65 floors. What changed since round 1: the layer floor is measured per region, wall-resolved sizing is in
place, the determinant floor is shown to be the wrong metric for layer cells, and the solver substrate is hardened.

## Q3 — the product launcher (security)

[`cases/tools/of-run.sh`](../../../cases/tools/of-run.sh), argv only (plan finding F-3 fixed; no `bash -c`):

- **P0** `env -i` with PATH, HOME, USER, LOGNAME, LANG and FOAM_CONTROLDICT only; the script itself runs `bash -p`.
- **P1 + P2 from one hashed bundle**: [`foam-bundle/controlDict`](../../../cases/tools/foam-bundle/controlDict) is the
  shipped `etc/controlDict` (sha256 `451a8f34…53ccf`) with two changes, `allowSystemOperations 0` and
  `stopAtWriteNowSignal 30` (the A4 stop). Bundle sha256 `f3debe8b…a6ef854`, pinned in the launcher, re-checked at
  every launch and on the copy written into a fresh product HOME; the same bytes go into FOAM_CONTROLDICT.
- **Right before every launch:** [`launcher-record.py`](../../../cases/tools/launcher-record.py) `verify` (the case
  tree must equal the tree recorded outside the case at generation and after every earlier launch; no shared library,
  executable image or symlink anywhere), then the token-level allow-list lint
  [`foam-dict-lint.py`](../../../cases/tools/foam-dict-lint.py) (no `#` directive, no quoted keyword, no `coded*`,
  `*Libs`, `systemCall` or switch block, `libs` only from four shipped libraries, `type` only from the template
  set, no `$`/absolute/parent paths, `runTimeModifiable false` required), then the checkMesh pre-flight record for every
  app except the mesh writers (blockMesh, gmshToFoam) and checkMesh. Fixtures:
  [`test-foam-dict-lint.py`](../../../cases/tools/test-foam-dict-lint.py), 26 of 26.
- **After every launch:** "Allowing" writes `runs/.security-stop` and every later launch refuses; a missing banner
  fails the launch; the tree is re-pinned.
- **Observed:** `banner=Disallowing` in every ledger line of every round-2 run (6 TMR runs, 6 wing runs); the v2
  launcher refused C-3's reconstructPar until C-3 had a record (see the SPIKE-04 round-2 verdict).

Security review (security-identity-architect, Adversary): cycle 1 **FAIL** — three blockers (a comment marker
inside a string hid `libs`; a `libs` list split across lines passed; a file in the case root named like the app would
run under `etc/openfoam`'s `exec bash`), all fixed with fixtures. Re-check **PASS WITH CONDITIONS** (veto cleared; one
major: the ungated `timeActivatedFileUpdate` function object can write anywhere). Cycle 2 (the cap) closed it with the
`type` allow-list and path rules; final re-check **PASS**. Accepted spike residuals: trust on first use at `init`;
writes during a run are re-pinned by `update`; the same OS user can write `runs/.launcher`; a gap of seconds between
`verify` and the process start; `a4-monitor` finds its SIGUSR1 targets by a pgrep pattern; the `libs` allow-list
relies on dlopen resolving bare names (no image may be in the case). Product follow-ups: selector keys other than
`type` are not allow-listed; `postProcess -func` values need a fixed list.

### The probe set — for the operator

[`cases/tools/security-probe.sh`](../../../cases/tools/security-probe.sh) runs S-1..S-8 (plan §4.3) on fresh copies
of the bundled cavity tutorial: S-1 positive control under the app default; S-2 `#codeStream` serial and a
`codedFixedValue` under `mpirun -np 2` with per-rank output (both ranks must show the refusal); S-3 `systemCall`;
S-4 case `InfoSwitches allowSystemOperations 1` + `codedFixedValue`; S-5 as S-2 under P0+P2; S-6 `libs` load attempt;
S-7 the product launcher from a hostile parent environment (live BASH_ENV and WM_PROJECT_SITE payloads that leave
marker files); S-8 lint only. It refuses to run unless the launcher, lint, record tool and bundle match the pins inside
it, writes receipts with user, home and host redacted to `docs/proof/spike-03/security/<ts>/`, and exits non-zero on
any FAIL. **Not run by the agent** (the harness refused a code-execution probe in round 1). The exact command and the
script's sha256 are in the round-2 hand-off; the operator checks the hash first.

## Q2 — meshing at AR 8

Analysis geometry ([`make-wing-r2.py`](../../../cases/tools/make-wing-r2.py), DR-F2-4): NACA 0012 thickened by a linear
ramp to a **0.5 mm** trailing edge (t/c 0.42 % at the TE; chord and S_ref unchanged), a flat TE base, and a rounded
tip (the section half-thickness swept through a half circle; analysis b up to 7.28 mm beyond the 480 mm half span;
the cap adds 6.03e-4 m² of planform, +1.05 % of the design half S_ref 0.0576 m², which stays the reference area).
Gmsh (M-2): the TE base is a half circle of diameter t_TE instead of a flat base, so its analysis chord is
c + 0.25 mm (+0.21 %); **TE-region results (coverage, y+) are not comparable across the two routes**; the
main-surface conclusions are.
Closed and consistently wound: enclosed volume 5.96695e-4 m³ against 5.96768e-4 m³ by quadrature (−0.012 %); STL
sha256 `c8d4af76…3ad440`. *assume:* 0.5 mm stands in for the TE floor setting, which has no application default in
the spec or code (checked); it is the low end of the Flagged practitioner band 0.5–1.5 mm. *If false:* the snappy TE
refinement must resolve the new base with ≥ 2 cells. Operating point as round 1: fresh water 20 °C, 5 m/s, α 4°,
Re_c 6.0e5 with ν rounded to 1.0e-6 m²/s (the ITTC Rev 03 fresh-water value at 20 °C is 1.0034e-6, so Re_c is
5.98e5, −0.34 %); unbounded fluid: no free surface, cavitation or ventilation. Wall-resolved sizing (W3): first
layer 8 µm, ER 1.2, 20 layers = 1.49 mm. Any force from this geometry must carry the base-drag band (≈ 2–5 %,
Flagged) and the label; none is claimed here (500-iteration screens only). The blunt base may shed vortices
(Re_t ≈ 2,490); if a later 3-D A4 solve fails on that, it is recorded as physics, not retuned.

| Run (case file) | Route · variant | Mesh wall | Cells | Layers on wing (128,238 snappy faces) | checkMesh `-allGeometry` | cellDeterminant < 0.001 / < 0.3 |
|---|---|---|---|---|---|---|
| M-1a ([m1a](../../../cases/spike03r2-m1a-ar8.yaml)) | snappy · medial-axis shrinker, featureAngle 180, layerTerminationAngle −180, nOuterIter 3 | 465 s (6 ranks) | 2,752,437 | max **14**; 20 on **0 %**; ≥ 10 on **88.9 %**; 0 on 4.2 % | Failed 3: non-orth max 81.2° (288 faces > 70°), det, 6 low-weight faces; 79,653 concave | 207,710 / 1,373,518 |
| M-1b ([m1b](../../../cases/spike03r2-m1b-ar8.yaml)) | snappy · displacementMotionSolver | stopped after 38 s | — | — | FATAL: "Entry 'cellDisplacement' not found in dictionary system/fvSolution/solvers" | — |
| M-1b repair 1 of 2 ([m1b1](../../../cases/spike03r2-m1b1-ar8.yaml)) | as M-1b + the solver entry | 405 s | 2,665,312 | max **15** (4 faces); 20 on **0 %**; ≥ 10 on **74.7 %**; 0 on 0.1 % | Failed 2: non-orth max 77.3° (180 faces), det; 77,742 concave | 210,478 / 1,189,586 |
| M-2a ([m2a](../../../cases/spike03r2-m2a-ar8.yaml)) | Gmsh · graded wing size 0.5–2.5 mm, HXT 3-D | stopped (≈ 1 min) | — | — | Gmsh: "PLC Error: A segment and a facet intersect", "HXT 3D mesh failed" | — |
| M-2b ([m2b](../../../cases/spike03r2-m2b-ar8.yaml)) | Gmsh · variant b: uniform 1.5 mm, Delaunay 3-D | 6 s | 137,136 | 20 on 100 % of only **4,244** faces | FATAL: symmetryPlane "not planar"; 3,360 root faces in `defaultFaces` | — |
| M-2 repair 2 of 2 ([m2c](../../../cases/spike03r2-m2c-ar8.yaml)) | Gmsh · size field from the OCC wing, `symmetry` patch type, 1e-4 m root tolerance | **20 s** (Gmsh) + 7 s gmshToFoam | 3,207,020 (2,515,440 prisms + 691,580 tets) | **20 on 100 %** of 125,772 faces (prisms = 20 × faces, counted) | Failed 2: det (min 0), 1 low-weight face; non-orth max **86.3°** (811 faces > 70°); concave OK; skewness 1.74 | 1,128,345 / 3,206,936 |

Per-region coverage (snappy; [`layer-coverage.py`](../../../cases/tools/layer-coverage.py), regions LE 3 %, TE 3 %,
tip from the design half span, root, main):

| Region | M-1a ≥ 10 layers · 0 layers | M-1b repair ≥ 10 · 0 | DR-F2-3 floor |
|---|---|---|---|
| leading edge 3 % | 99.0 % · 0 % | 98.0 % · 0 % | ≥ 20 on ≥ 95 %, ≥ 10 on 100 % |
| main surface | 95.0 % · 0 % | 94.8 % · 0 % | |
| root junction | 100 % · 0 % | 100 % · 0 % | |
| trailing edge 3 % (45,972 faces) | 82.9 % · 11.3 % | 44.4 % · 0 % | |
| tip cap and edge | 25.7 % · 4.4 % | 22.4 % · 2.6 % | |

**Why snappy stops at 14 (measured):** with `nOuterIter 3` snappy adds the layers in passes. M-1a's three passes
report a mean of 6.56, then 12.8, then 12.8 layers and 70.2 %, 88.9 %, 89 % of the target thickness, so the third pass
adds nothing. The motion-solver shrinker (M-1b) does not change the cap; it trades zero-layer faces (4.2 % → 0.1 %) for
thinner coverage at the TE (82.9 % → 44.4 % with ≥ 10). Snappy runs 405–465 s here against 31–63 s in round 1.

### The determinant floor (DR-F2-6) cannot hold for wall-resolved layers

OpenFOAM's `cellDeterminant` for an a × a × h hex is r⁴·6⁶/(2+4r)⁶ with r = h/a (plan §3.4). It falls below 0.001 at
r ≈ 0.037 (aspect ratio ≈ 27), before a wall cell loses its wall face (**Inferred**, arithmetic). Every W3 mesh here
has wall cells far thinner than that: snappy's first layer is ≈ 24 µm against 0.75 mm faces (r ≈ 0.03; 207,710 and
210,478 cells below 0.001, about the first one or two layers over 128,238 faces), and Gmsh's prisms start at 8 µm
against 1.5 mm (1.13 M cells below 0.001, minimum 0). Round 1's 0.3 floor failed for the same reason. **The floor
measures aspect ratio, so DR-F2-6 needs an amendment** (for example: apply it to non-layer cells only, or replace it
for layer cells with a face-pyramid / positive-volume test). That is a decision for the operator or the spec owner,
not a change made here.

### y+ screen (500 iterations; a screen, not the DR-F2-3 gate, which needs an A4-stationary field)

Area-weighted cell-centre y+ on the wing from the sampled `yPlus` field ([`yplus-area.py`](../../../cases/tools/yplus-area.py)):

| Mesh | p50 | p95 | max | By region (p50) |
|---|---|---|---|---|
| M-1a (snappy, 14 layers, first ≈ 24 µm) | 2.92 | 10.0 | 52.2 | LE 4.50 · main 2.90 · TE 1.91 · tip 10.1 |
| M-2 repair (Gmsh, 20 layers, first 8 µm) | 0.944 | 1.26 | 2.29 | LE 1.41 · main 0.94 · TE 0.63 · tip 1.03 |

The Gmsh mesh is close: main-surface p95 1.20, max 2.29 at the LE. The LE peak is **Inferred** to come from skin
friction above the flat-plate value that set 8 µm — under the fully turbulent model, from the LE on — and is not
separated from the effect of Gmsh's surface normals at high curvature. The gate needs an A4-stationary field. A
smaller first height is the obvious next variant (**Estimate**, y+ ∝ height: 6.35 µm for p95 = 1, 7.0 µm for
max = 2); it shrinks the 20-layer stack from 1.49 to ≈ 1.12 mm (36 % instead of 48 % of the ≈ 3.1 mm turbulent δ
at the TE, **Estimate**), so it must re-check the DR-F2-3 layers-inside-δ floor or add layers. Not run: the repair
cap is spent.

M-1a was screened although its coverage failed (a deviation from "the passing variant only"): it measures the
first-cell height snappy really delivers. Its first layer is ≈ 3× the 8 µm request because the 14 layers keep ≈ 89 %
of the 1.49 mm target thickness (**Inferred** from the layer field and the expansion ratio).

## Durations against the plan

| Block / run | Plan (Estimate) | Measured |
|---|---|---|
| S — launcher, lint, probe set, two review cycles | ≈ 30 min authoring + 5 min compute | ≈ 45 min authoring and repair (20:28–21:15 UTC, interleaved with the C block); 0 min compute (probes are operator-run) |
| M-0 (generator) | < 1 min | 0.3 s |
| M-1a/b snappy, each | ≤ 5 min | 465 s and 405 s (+ 38 s failed M-1b) |
| M-1y screen | ≈ 10–15 min | 660 s (2.75 M cells, 6 ranks, 1.3 s/it) |
| M-2a/b Gmsh, each | ≤ 10 min | M-2a ≈ 1 min (failed), M-2b 6 s, repair 20 s (Gmsh is single-threaded here: 20.2 s user for 20.1 s real) |
| M-2y screen | ≈ 15 min | 930 s (3.21 M cells, 6 ranks, ≈ 1.6–1.9 s/it under load up to 17) |
| M-3 (AR 5, AR 12) | ≈ 40 min | not run (stop rule) |
| M block, wall | 100 min | **66 min** (22:02–23:08 UTC, incl. ≈ 16 min of join-lock and load > 10 waits) |

**Peak RSS** (`/usr/bin/time -l`): checkMesh `-allGeometry` 3.77 GB serial on 2.75 M cells; gmshToFoam 2.85 GB;
Gmsh 2.33 GB; snappy 1.69–2.11 GB (one rank's maximum, **Inferred** as in SPIKE-04). **Peak 1-minute load** at launch
boundaries: 23.33 (end of M-1a's snappy, other tracks building); launches waited 32 × 30 s on the join lock or
load > 10.

## What the backend ADR can decide now (macOS arm64 only)

- **Solver security:** the launcher design (P0 + P1 + P2 from one hashed bundle, launcher record, token lint,
  pre-flight banner, argv launch) is implemented and reviewed (PASS). The live proof is the operator's probe run.
- **Mesh route:** snappyHexMesh cannot deliver W3 layers (20 at y+ ≈ 1) on this wing. Gmsh's normal extrusion puts 20
  layers on 100 % of the faces, as a process-only tool (DR-F2-5); its y+ is not yet within DR-F2-3 (screen p95 1.26,
  max 2.29). Before Gmsh can be the route: the non-orthogonality at the layer/tet interface
  (86°), the first height (y+ p95 1.26 at 8 µm on a 500-iteration field), the determinant amendment, AR 5/12, a
3-D A4 solve for the y+ gate, and Windows. A product-written structured mesh (plan L5) remains the
  fallback.
- **Mesh-gate definitions:** DR-F2-6 needs an amendment (above). DR-F2-3's per-region coverage works as a measure
  and found the failures where round 1 hid them in an average.
- **Cannot decide:** the y+ gate itself (needs an A4-stationary 3-D field); Windows (DR-F2-7); v2606.

## Reviewer verdicts

| Lens | Verdict | Veto | Conditions → where applied |
|---|---|---|---|
| CFD numerical verification (author) | — | does not clear its own veto | coverage counted, not assumed; the determinant finding is arithmetic plus three measured meshes |
| Security & identity architect (Adversary, read only) | cycle 1 FAIL (3 blockers) → re-check PASS WITH CONDITIONS → cycle 2 **PASS** | cleared | see Q3 above; probe pins filled after the last launcher, lint and record edit |
| Hydrofoil hydrodynamicist (Adversary, read only) | PASS WITH CONDITIONS | not triggered | Gmsh round base and +0.21 % chord, TE results not comparable across routes; ν rounding (Re_c 5.98e5) labelled in the case files; Gmsh y+ "not yet within DR-F2-3"; LE explanation labelled Inferred; the 6 µm variant must re-check layers inside δ; analysis S and b recorded; base-drag band condition for any future force; unbounded-fluid note — all applied above |
