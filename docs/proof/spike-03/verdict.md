---
id: proof-spike-03
title: SPIKE-03 verdict — unattended meshing across AR 5 / 8 / 12 on OpenFOAM v2512
type: proof-pack
status: in-review
owner: "@fluids-f1"
phase: spike
tags: [spike-03, openfoam, snappyhexmesh, mesh-gate, run, backend]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-03
summary: >-
  NO-GO against the Appendix R words. snappyHexMesh meshes AR 5, 8 and 12 half wings unattended (35-63 s on 6
  ranks, 1.1-2.6 M cells) and every mesh passes checkMesh, but none passes the ITTC/A5.10 floors: 15 layers reach
  65-73 % of wing faces (0 % at the trailing edge and tip), measured mean y+ is 22 (below 30-100), and 32-35 % of
  cells have an OpenFOAM cell determinant below 0.3. macOS only; Windows not run. Dictionaries can run code by
  default (allowSystemOperations 1); a product-owned controlDict turns it off.
review-suggested: []
---

# SPIKE-03 verdict — unattended meshing across AR 5 / 8 / 12

Lane F1, Ruling 60. Lenses: CFD numerical verification (author), hydrofoil hydrodynamicist (physical claims).
Worktree branch `spike/fluids-spike-03-04`. Every number below is read from a file under
[receipts/](receipts/) (copied from the git-ignored `runs/<timestamp>-<case>/`), unless it is labelled
**Estimate** or **Inferred**.

## Verdict

**NO-GO** against Appendix R: "unattended `snappyHexMesh` across AR 5/8/12 wings on both OSes passing the ITTC
mesh floors".

| Clause | Result | Evidence |
|---|---|---|
| unattended `snappyHexMesh` | **met** — no human step, exit 0, AR 5 / 8 / 12 | ledgers in [receipts/](receipts/) |
| across AR 5/8/12 | **met** with one frozen setting | Stage 1 and 2 tables |
| passing the ITTC mesh floors | **not met** — layer floor, y+ band and the determinant floor fail at every AR; `checkMesh` default checks pass | tables below |
| on both OSes | **not met** — macOS arm64 only; Windows (WSL2 or Docker) not run | — |

Run and Results acceptance stays gated. A third settings iteration (outside the cap) and a Windows run are the
open work.

## Backend (step 0)

| Fact | Value | How observed |
|---|---|---|
| Distribution | `/Applications/OpenFOAM-v2512.app` (gerlero/openfoam-app, native arm64, ad-hoc signed: `codesign -dv` → `Signature=adhoc`) | `codesign`, `Info.plist` |
| Version / build | `v2512`, build `_87ed40d256-20251219`, `WM_OPTIONS=darwin64ClangDPInt32Opt`, Open MPI 5.0.8 | solver banner, `mpirun --version` |
| Hashes | DMG `5eb2ab10…393439`; `simpleFoam` `a9bd4e41…5e296`; `snappyHexMesh` `d36f997e…f7795a` | `shasum -a 256` |
| Activation | `/Applications/OpenFOAM-v2512.app/Contents/Resources/etc/openfoam -c "<command>"` — mounts the DMG at `/Volumes/OpenFOAM-v2512` and sources its `etc/bashrc`. Wrapped by [`cases/tools/of-run.sh`](../../../cases/tools/of-run.sh) (nice 10, waits while 1-min load > 10). | runs |
| Operator `of` function | **Loaded in the agent shell, but broken**: it sources `$(brew --prefix)/share/openfoam/etc/bashrc` = `/opt/homebrew/share/openfoam/etc/bashrc`, which does not exist; `2>/dev/null` hides the failure. (The brief's `assume:` that it is not loaded was false; it is loaded and inert.) | `type of`, `ls` |
| Docker | Not used; no image digest. | — |
| Smoke run | Bundled `icoFoam/cavity` ran to `0.5` (5 time directories), exit 0, 1 s. | [receipts/…smoke-cavity](receipts/20261003T164323Z-smoke-cavity/) |

## Security finding — the `#codeStream` / `#calc` assumption (A8.5 backend-substrate row)

The spec's `assume:` "OpenFOAM dictionaries can execute code" is **confirmed by source and banner, not by a live
probe**. A live probe (a case with `#calc`, `#codeStream` and a `systemCall` function object) was refused by the
harness permission classifier; it was not retried by any other route. Evidence that was observed:

1. `src/OpenFOAM/db/dynamicLibrary/dynamicCode/dynamicCode.C:44-96`: case-supplied code compiles and loads only if
   InfoSwitch `allowSystemOperations` is non-zero (source default 0). `calcEntry.H:46` — `#calc` is a wrapper around
   `#codeStream`. `systemCall.C:131` (the `systemCall` function object, which runs shell commands) has the same gate.
2. The shipped `$WM_PROJECT_DIR/etc/controlDict` sets `allowSystemOperations 1`, and every solver banner prints
   `allowSystemOperations : Allowing user-supplied system call operations` ([log.icoFoam](receipts/20261003T164323Z-smoke-cavity/log.icoFoam)). A C++ compiler is present (`/usr/bin/c++`).
3. **The command-line override does not work:** `checkMesh -info-switch allowSystemOperations=0` prints
   `info-switch allowSystemOperations (unregistered)` and still "Allowing" ([log](receipts/20261003T164323Z-smoke-cavity/log.checkMesh-switch0)).
4. **A user-level controlDict does work:** with `HOME` pointing at a product-owned directory holding
   `.OpenFOAM/2512/controlDict` with `InfoSwitches { allowSystemOperations 0; }`, the banner reads
   `Disallowing user-supplied system call operations` ([log](receipts/20261003T164323Z-smoke-cavity/log.checkMesh-userdict0)).

So the typed-template rule (no macros, `#codeStream`, `#calc`, coded BCs or `systemCall`) is necessary but not
sufficient: the product must also run every OpenFOAM process under a product-owned controlDict with
`allowSystemOperations 0` and check the banner line before trusting a run. *assume:* `FOAM_CONFIG_ETC` gives the
same effect without overriding `HOME` — not tested; confirm in the backend ADR spike.

## Geometry (deviation, stated)

The CLI (`src/CfdWorkbench.Cli/Program.cs`) has one verb, `inspect <example|path> --json`; it cannot write a surface.
Equivalent planform built by [`cases/tools/make-wing-case.py`](../../../cases/tools/make-wing-case.py) from the
Example (`src/CfdWorkbench.Desktop/Assets/example.foil`, sha256 `373a939f…2bda64`: rectangular, chord 120 mm,
half span 450 mm → AR 7.5, symmetric 12 % section, 0 → −2° washout, `mirror_y`). Differences: NACA 0012 closed
trailing edge instead of the Example's manufactured degree-5 profile; no twist; half span set by AR
(AR 8 → 480 mm); flat tip cap. Half model on a symmetry plane; root cap 10 mm below the plane. Surface is closed
(`surfaceCheck`: "Surface is closed"), enclosed volume 5.765e-4 m³ against 5.79e-4 expected (0.082 c² × length).
The generator's first output was mis-wound (volume 1.92e-4); that run is marked `VOID.txt` and was never meshed,
and the generator now asserts the volume.

Operating point: fresh water 20 °C (ν = 1.0e-6 m²/s), U = 5 m/s, α = 4° by inflow direction, Re_c = 6.0e5.
Domain 10 c upstream of the leading edge, 21 c downstream of the trailing edge, ±10 c vertically, 8 c beyond the
tip — inside the A5.10 domain floor (≥ 10 L up, ≥ 20 L down).

## Stage 1 — AR 8

Settings ([cases/spike03-ar8.yaml](../../../cases/spike03-ar8.yaml)): background 48 mm cells, wing surface level
(5 6) → 1.5 mm / 0.75 mm, feature edges level 6, near and wake boxes level 3, 15 layers, expansion 1.1, relative
final layer 0.5, layer featureAngle 130. Pipeline: `surfaceCheck → surfaceFeatureExtract → blockMesh →
decomposePar → mpirun -np 6 snappyHexMesh -parallel → reconstructParMesh → checkMesh` — no human step
([`cases/tools/mesh-wing.sh`](../../../cases/tools/mesh-wing.sh)).

Settings were iterated **twice** (the repair cap): iteration 2 ([spike03-ar8-r2](../../../cases/spike03-ar8-r2.yaml):
absolute first layer 0.39 mm, expansion 1.03, layer featureAngle 180) was worse — 15 layers on 7.6 % of faces,
none on 65.5 % — so iteration 1 is frozen and was re-run as [spike03-ar8-final](../../../cases/spike03-ar8-final.yaml).
(Record note: `spike03-ar8` and `spike03-ar8-r2` were generated while their case files read `max_iterations: 150`;
the files were set to 0 afterwards to record that no solve ran. Only `controlDict` `endTime` differs; the mesh
inputs are identical.)

| Measure (AR 8, frozen settings) | Value | A5.10 / ITTC floor | Result |
|---|---|---|---|
| snappyHexMesh exit, wall time | 0, **31 s** on 6 ranks (35.1 s internal) | unattended | pass |
| Cells | 1,730,124 (run 1: 1,730,250 — parallel snappy is **not repeatable** to the cell) | — | note |
| `checkMesh` (default checks) | `Mesh OK.` | pass | pass |
| Max non-orthogonality | 64.97° (avg 9.5) | ≤ 70 (checkMesh); snappy limit 65 | pass |
| Max skewness | 1.50 | ≤ 4 internal, ≤ 20 boundary | pass |
| All volumes positive | min 3.60e-11 m³ | > 0 | pass |
| Cell determinant | min 5.5e-4, avg 0.775; **589,667 cells (34 %) < 0.3**; 7 cells < 0.001 | > 0.3 | **fail** |
| `checkMesh -allGeometry` | "Failed 2 mesh checks": 59,689 concave cells, 7 cells with determinant < 0.001 | — | flag |
| Layers on wing | mean 12.53 of 15; 15 on **70.9 %** of 85,006 faces; 0 on 1.7 % | ≥ 15 (y+ 30-100) | **fail** |
| … by region | LE 3 %: 95.5 % full · main surface 76.4 % · root 82.9 % · TE 3 %: **0 %** · tip: **0 %** (22.3 % none) | | |
| y+ **Estimate** (flat plate, x = c/2) | cell centre p05 11.4 · p50 22.9 · p95 86 | 30-100 | **fail** |
| y+ measured (150 iterations, not converged) | wing min 3.4 · max 149.6 · **avg 22.2** | 30-100 | **fail** |
| Function objects | `forceCoeffs`, `solverInfo`, `yPlus`, `wallShearStress` all wrote `.dat` files | present | pass |

The two flagged function-object names (`yPlus`, `wallShearStress`) are confirmed to exist and write in v2512.
The 150-iteration solve (157 s on 6 ranks) is **not converged** (Cl still moving in the 4th digit); its Cl 0.306 /
Cd 0.0183 are not results.

**Why the gate fails (Inferred from the measurements above):** (a) relative layer sizing ties the first-layer
height to the local cell, so y+ follows the surface level, not a target; (b) at layer featureAngle 130 snappy stops
layers at the sharp trailing edge and the flat tip edges; (c) the ITTC wall-function branch (≥ 15 layers with
y+ 30-100) needs a 7-12 mm layer stack at Re_c 6e5 while the boundary layer at the TE is about 3 mm thick
(**Estimate**, δ ≈ 0.37 x Re_x^-0.2) and the surface cells are 1.5 mm — snappy collapses such a stack
(iteration 2). (d) The spec's "determinant > 0.3" is not met by any snappy layer mesh here; OpenFOAM's
"cell determinant (wellposedness)" is a different measure from the Jacobian determinant that 0.3 floors usually
refer to (**Inferred**; confirm which metric A5.10 meant).

## Durations (the new prior)

| Stage | Start (UTC) | End (UTC) | Duration |
|---|---|---|---|
| 0 — backend facts, smoke, security checks, schema | 16:43 | 16:49 | 6 min |
| 1 — AR 8, two settings iterations, repeat, short solve | 16:49 | 17:04 | 15 min |
| 2 — AR 5, AR 12 (includes about 9 min waiting on load > 10 and the join lock) | 17:05 | 17:21 | 16 min |

Compute per mesh: snappyHexMesh 35 s (AR 5), 31-35 s (AR 8), 63 s internal / 105 s wall under host load 57 (AR 12);
the whole pipeline including `checkMesh -allGeometry` is under 2.5 min per AR. Peak 1-minute host load observed
during this spike: 57.66 (at the end of the AR 12 snappy run, with other tracks building). Peak RSS: **Not
recorded** (no RSS probe was run; A5.10 status needs one).

## What the backend ADR must decide

1. **Substrate and pin.** Native `OpenFOAM-v2512.app` (gerlero; ad-hoc signature, not notarised; pin = DMG sha256
   `5eb2ab10…393439` + build `_87ed40d256-20251219`) or a Docker image by digest. This spike used the native app only.
2. **macOS vs Windows route.** Not exercised. The A5.10 WSL2 argv rule ("confirm at SPIKE-03") is **not confirmed**.
3. **Code execution in dictionaries.** Ship a product-owned controlDict with `allowSystemOperations 0` for every
   OpenFOAM process and fail closed unless the banner says "Disallowing". The `-info-switch` flag does not do it.
4. **Mesh gate definition.** (a) Name the determinant metric: OpenFOAM's "cell determinant (wellposedness)" fails
   0.3 on 32-35 % of every layer mesh here. (b) State the layer floor as a coverage rule per region (sharp TE and
   tip will not take 15 layers with these settings). (c) Choose the wall treatment band for hydrofoil Re
   (≈ 6e5 at 120 mm and 5 m/s): wall functions with y+ 30-100 need first cells about 0.26-0.9 mm, and 15 layers
   of them are thicker than the boundary layer.
5. **Mesh identity.** Parallel snappy is not repeatable to the cell (1,730,250 vs 1,730,124 cells, same inputs),
   so the mesh hash must be computed from the written mesh, never assumed from the inputs.
6. **Resource limits.** 6 ranks, nice 10, a load wait and a join lock worked without starving joins. Peak RSS is
   still to be measured.

## Stage 2 — AR 5 and AR 12 (frozen settings)

Cases [spike03-ar5](../../../cases/spike03-ar5.yaml) and [spike03-ar12](../../../cases/spike03-ar12.yaml). The
first AR 5 attempt is marked `VOID.txt`: the generator wrote `writeInterval 0` for a mesh-only case and
`surfaceFeatureExtract` refused it before any meshing. The generator now writes a valid controlDict for mesh-only
cases.

| Measure | AR 5 (half span 300 mm) | AR 8 (480 mm) | AR 12 (720 mm) | Floor |
|---|---|---|---|---|
| snappy exit · internal time | 0 · 35.1 s | 0 · 35.1 s | 0 · 62.7 s | unattended |
| Cells | 1,100,301 | 1,730,124 | 2,563,735 | — |
| `checkMesh` default | Mesh OK | Mesh OK | Mesh OK | pass |
| Max non-orthogonality | 64.87° | 64.97° | 64.99° | ≤ 70 |
| Max skewness | 1.31 | 1.50 | 1.37 | ≤ 4 |
| Min volume | 2.94e-11 m³ | 3.60e-11 m³ | 2.94e-11 m³ | > 0 |
| Cells with determinant < 0.3 | 351,536 (31.9 %) | 589,667 (34.1 %) | 897,254 (35.0 %) | none |
| Concave cells (`-allGeometry`) | 40,826 | 59,684 | 86,369 | flag |
| Wing faces · mean layers | 54,280 · 11.89 | 85,006 · 12.53 | 126,520 · 12.82 | 15 |
| Faces with 15 layers | **65.0 %** | **70.9 %** | **73.2 %** | 100 % |
| TE 3 % / tip faces with 15 | 0 % / 0 % | 0 % / 0 % | 0 % / 0 % | |
| y+ **Estimate**, cell centre p50 | 22.9 | 22.9 (measured avg 22.2) | 22.9 | 30-100 |

Coverage rises with AR only because the trailing-edge and tip strips are a smaller share of a longer wing; the
failing regions are the same at every AR.
