---
id: proof-spike-03
title: SPIKE-03 verdict — unattended meshing across AR 5 / 8 / 12 on OpenFOAM v2512
type: proof-pack
status: draft
owner: "@fluids-f1"
phase: spike
tags: [spike-03, openfoam, snappyhexmesh, mesh-gate, run, backend]
links:
  - {to: spec-cfd-workbench-v1, rel: documents}
  - {to: rulings, rel: depends-on}
review-by: 2026-11-03
summary: >-
  Stage 1 (AR 8): snappyHexMesh meshes the wing unattended in 31-35 s on 6 ranks and passes checkMesh, but the
  A5.10 mesh gate as written is not met: 15 layers reach 70.9 % of wing faces (0 % at the trailing edge and tip),
  the median cell-centre y+ is about 22 (below the 30-100 band), and 34 % of cells have an OpenFOAM cell
  determinant below 0.3. Stage 2 pending.
review-suggested: []
---

# SPIKE-03 verdict — unattended meshing across AR 5 / 8 / 12

Lane F1, Ruling 60. Lenses: CFD numerical verification (author), hydrofoil hydrodynamicist (physical claims).
Worktree branch `spike/fluids-spike-03-04`. Every number below is read from a file under
[receipts/](receipts/) (copied from the git-ignored `runs/<timestamp>-<case>/`), unless it is labelled
**Estimate** or **Inferred**.

## Verdict

**Stage 1 (AR 8): unattended meshing — GO. A5.10 mesh gate as specified — NOT MET.** Stage 2 pending.

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
| 2 — AR 5, AR 12 | pending | | |
