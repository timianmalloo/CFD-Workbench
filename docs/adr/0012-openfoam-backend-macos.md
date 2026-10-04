---
id: adr-0012-openfoam-backend-macos
title: "ADR-0012: the OpenFOAM backend on macOS arm64 — substrate pin, product launcher, A4 convergence oracle and SA numerics (what rounds 1 and 2 proved)"
type: adr
status: proposed
owner: "@fluids-f1"
phase: spike — fluids rounds 1 and 2 (Rulings 60, 65), round 3 planned
tags: [adr, backend, openfoam, v2512, security, launcher, convergence, a4, spalart-allmaras, mesh-gate, macos]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: proof-spike-03, rel: depends-on }
  - { to: proof-spike-04, rel: depends-on }
  - { to: proof-spike-03-round2, rel: depends-on }
  - { to: proof-spike-04-round2, rel: depends-on }
  - { to: plan-fluids-round2, rel: depends-on }
  - { to: plan-fluids-round3, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: 2026-11-03
summary: >-
  Pins only what fluids rounds 1 and 2 measured on macOS arm64. Substrate: OpenFOAM ESI v2512, the gerlero native app,
  by DMG sha256 and build id, launched by argv. Product launcher (right-sized, Ruling 68): six short rules for one trusted user on one laptop,
  the app-owned controlDict turns case code off and a live "Disallowing" banner proves it; verified by the
  2026-10-04 probes (S-1, S-2, S-4); Run is enabled when the install smoke test shows Disallowing; an existing
  OpenFOAM install is detected and hash-checked (DR-SEC-1 A). Convergence: the A4 oracle (it rejected a period-2 cycle
  and accepted four runs). Numerics: SA-noft2 with first-order nuTilda and relaxation 0.7 (met A4 on TMR only).
  Compressibility delta measured once (+1.147 % Cl, +0.81 % Cd; U_delta not stated). Open: Windows, Docker digests,
  v2512 vs v2606, the mesh route, GCI. The DR-F2-6 determinant floor needs an amendment; this ADR files a request
  (DR-F3-1) and does not decide it.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-04, reason: "Spec 1.7.1 (Ruling 68): A8.5 backend-substrate row and A5.10 readiness right-sized" }
---

# ADR-0012: the OpenFOAM backend on macOS arm64

- **Status:** Proposed 2026-10-03. The operator directed "Pin what's settled + plan round 3" on 2026-10-03. That
  direction is not yet a numbered ruling. This ADR becomes Accepted when a ruling accepts it.
- **Number:** 0012. ADR-0011 is reserved for Area 3 run storage (DR-ANA-4, Ruling 63;
  [design](../design/area3-analysis.md) :772, :825; [spec 1.7](../specs/amendments/spec-1.7.md) :90). No local branch
  holds a `docs/adr/0011-*` or `0012-*` file (checked with `git ls-tree` on every branch).
- **Deciders:** the operator. Lead lens: CFD numerical verification (author; it does not clear its own veto).
  Reviewers: the security & identity architect (the launcher, D2) and the hydrofoil hydrodynamicist (D4 scope, D5,
  D7 and the round-3 plan). Verdicts are at the end.
- **Scope:** macOS arm64, one host, OpenFOAM v2512. Every number comes from a receipt under
  `docs/proof/spike-03/receipts/` or `docs/proof/spike-04/receipts/`. A number with no receipt is labelled
  **Estimate** or **Inferred**.

## Context

Spec A5.10 asks the backend ADR to set the pin (spec 1.7, Ruling 66 OQ-12). Ruling 60 says the ADR waits on the
fluids lane. Rounds 1 and 2 ran SPIKE-03 (meshing) and SPIKE-04 (TMR convergence oracle):

| Spike | Round 1 | Round 2 |
|---|---|---|
| SPIKE-04 | NO-GO. The 1e-7 residual floor was not met on any grid ([verdict](../proof/spike-04/verdict.md)) | **A4 oracle GO.** GCI NO-GO: L6/L5/L4 is oscillatory ([verdict](../proof/spike-04/verdict-round2.md)) |
| SPIKE-03 | NO-GO. snappy gave 15 layers on 65–73 % of faces and 0 % at the TE and tip ([verdict](../proof/spike-03/verdict.md)) | NO-GO. snappy reached 20 layers on 0 % of faces. Gmsh reached 20 layers on 100 % but fails the gate on non-orthogonality (86.3°) and determinant ([verdict](../proof/spike-03/verdict-round2.md)) |
| Security | Dictionaries can run code by default. A user-level controlDict turns it off | The launcher was built, reviewed and passed. Every one of the 12 round-2 runs printed `Disallowing` |

## Decision

### D1 — Substrate and pin (macOS arm64)

| Item | Pinned value | Source |
|---|---|---|
| Distribution | `/Applications/OpenFOAM-v2512.app` (gerlero/openfoam-app). Native arm64, **ad-hoc signed, not notarised** (`codesign` → `Signature=adhoc`), which must be disclosed to users | round-1 verdict § Backend |
| Version · build | OpenFOAM ESI `v2512`, build `_87ed40d256-20251219`, `WM_OPTIONS=darwin64ClangDPInt32Opt`, Open MPI 5.0.8 | solver banner, `mpirun --version` |
| DMG sha256 | `5eb2ab10af3e90b7e284276b7046632b9c9bc14885c67d6664bd2628eb393439` | round-1 `shasum -a 256` |
| Executable sha256 (record) | `simpleFoam` `a9bd4e41a5a5e6927aa05561e3b851a07fb9e76622d4d66c8671a58ebdc5e296`; `snappyHexMesh` `d36f997edd31453f2b7a9a9025bafb4a9adbb5916166e65ec1c62288d8f7795a` | round-1 `shasum -a 256` |
| Activation | `/bin/zsh -f -e …/Contents/Resources/etc/openfoam -- <app> <args…>`. The script sources the environment and runs `exec "$@"` (`etc/openfoam:355-361`). **Never the `-c` form**, which runs `exec bash -c` (`:330`). The app mounts its image at `/Volumes/OpenFOAM-v2512` | plan-r2 §4.1 fact 8; [`of-run.sh`](../../cases/tools/of-run.sh) :94-97 |
| Mesh identity | Hash the **written** mesh, never the inputs. Parallel snappy is not repeatable to the cell (1,730,250 vs 1,730,124 cells from the same inputs) | round-1 verdict |
| Pin change | Any change to the DMG, build or bundle is a pin change. It runs the TMR + A4 fixture (DR-F2-9; run set C-1r2, C-2, C-3, C-4, ≈ 78 min solver time measured) and the S-1 control with the S-2 serial refusal probe (D2) | Ruling 65 DR-F2-9; Ruling 68 |

The operator's `of` shell function is not on the product path (round 1 found it sources a missing file).

### D2 — The product launcher (right-sized, Rulings 67 and 68)

Threat model: one trusted user on his own laptop, running cases the app writes. The risks are a foreign design
file, the app breaking its own run, and a bad install ([the security right-size note](../notes/solver-security-right-size.md)).
Ruling 68 replaced the earlier six-rule contract (a hashed controlDict bundle in two places, a case-tree record, a
token lint and a checkMesh pre-flight before every launch, a persistent stop file, load and `join.lock` waits). The
spike harness (`cases/tools/of-run.sh`, `foam-dict-lint.py`, `launcher-record.py`) stays as this repository's tool for
fluids rounds. It is no longer the product contract.

1. **Code in dictionaries is off.** For each run the app writes a controlDict with `allowSystemOperations 0` and
   `stopAtWriteNowSignal 30` into an app-owned HOME (`.OpenFOAM/2512/controlDict`).
2. **The banner proves it.** For each OpenFOAM app launch (one `mpirun` is one launch) the app reads the master
   rank's `allowSystemOperations` banner and the build id from the live output. A missing banner, anything but
   `Disallowing`, or a build id other than the pin stops the run, which is reported as an app fault.
3. **argv only.** The app name comes from an enum in code; options come from typed values; activation is
   `/bin/zsh -f -e …/etc/openfoam -- <app> <args…>`, never `-c`.
4. **Only cases the app wrote.** Every file in a case is emitted by an app emitter from the typed model; no
   user-supplied file is copied in. Names are app ids; numbers are invariant and finite. A CI test emits every
   template from a hostile design fixture and checks the output with `foam-dict-lint.py`.
5. **Own run directory, clean environment.** Each run is a new `runs/<run key>/` under the app data directory,
   created exclusively. The app writes and deletes only there, under the A8.5 path policy. The child environment
   is PATH, HOME, USER, LOGNAME and LANG=C only.
6. **Resource caps.** A core-based rank default the user can lower, `nice 10`, one solver job at a time, the
   iteration cap, a free-disk check; Stop escalates to SIGTERM then SIGKILL of the run's process group; orphan
   ranks are reaped at app start.

**Evidence.** The 2026-10-04 probe run (`docs/proof/spike-03/security/20261004T155708Z/`): S-1 runs code under the
default; S-2 and S-4 refuse it under the app-owned HOME controlDict, serial and on 2 ranks, with no
`dynamicCode/`. **Rule 1 is Verified for v2512 on macOS arm64.** Of the 8 probes, 3 passed and 5 failed; the 5 are
3 probe over-expectations, 1 probe defect (S-7 never reached the environment) and 1 lint gap, and none is a failure
of the refusal.

**Gate.** Run is enabled when the install smoke test shows `Disallowing` on the user's machine. ~~No product build
may enable the backend until S-1..S-8 PASS.~~ Only S-1 and S-2 serial stay, as the pin-change check: a pin change
re-runs S-1 (the positive control; without it a refusal proves nothing) and S-2 serial (the refusal).

**Deferred.** If the app ever runs a case it did not write, add a `libs` allow-list (S-6: `libs` is not gated) and
refuse binaries and symlinks in the case tree.

**Install (DR-SEC-1 A, Ruling 68).** Setup detects an existing OpenFOAM install (`/Applications/OpenFOAM-v2512.app`)
and hash-checks its executables against the known builds in D1. A match is accepted as verified. A mismatch is used
only after the user accepts a plain "unverified install" note; otherwise setup installs the pinned build. The install
comes from the vendor channel only, with the licence and the "ad-hoc signed, not notarised" disclosure shown; the
app never disables Gatekeeper (removing `com.apple.quarantine` and `spctl --master-disable` count as disabling and
are refused steps). Each run's manifest records the install identity and whether it was verified. Only this one
path is built. Windows stays pending (DR-F2-7).

### D3 — The A4 convergence oracle (Ruling 65 DR-F2-1)

A run is converged when all four clauses hold. **Each run's case file states the clauses before the run starts.**

1. **Residual drop:** every equation's initial residual, taken as the maximum over the last 2 iterations, is ≤ 1e-3
   × its iteration-1 value.
2. **Stationarity:** over the last W = 2,000 iterations, U_I = ½(max − min) is ≤ 1e-5 for Cl and ≤ 1e-6 for Cd.
3. **Clean window:** no "bounding nuTilda" line appears inside W.
4. **Stop:** [`a4-monitor.py`](../../cases/tools/a4-monitor.py) checks at each 1,000-iteration multiple and stops the
   run with SIGUSR1 (`stopAtWriteNowSignal 30`). `runTimeModifiable` stays false. The reported value is the window
   mean.

For grid studies, add clause 5: U_I ≤ 0.01 |ε| of the neighbouring grid pair. A run that reaches its cap is
"Completed — residual criterion not met (ran to limit)". That is a gap, not a number.

**Evidence (Verified, receipts):**

- **It rejected a limit cycle.** With upwind nuTilda at relaxation 0.9, L6 locked into an exact period-2 cycle, with
  Cd alternating 0.0147804 ↔ 0.0148088. Every residual had dropped 6+ orders, but A4 failed clause 2 (Cd). At
  relaxation 0.7 the steady answer is Cl 1.051704. The cycle mean was off by 1.2e-4 in Cl and 1.4e-5 in Cd, so a
  residual-only criterion (option A2) would have admitted a wrong number.
- **It accepted converged runs.** C-1r2, C-2, C-3 and C-4 met A4 at 6,375 / 17,042 / 41,055 / 15,172 iterations.
  Each A4 stop wrote within 4 iterations of the signal.

### D4 — SA numerics for the OpenFOAM tier (met A4 on TMR 2DN00 only)

| Item | Setting |
|---|---|
| Model | ESI `SpalartAllmaras` = SA-noft2 with the Ŝ ≥ Cs·Ω limiter, **Cs 0.3, always reported** (TMR). v2512 has no SA-neg |
| Advection | `div(phi,nuTilda)` `bounded Gauss upwind`; `div(phi,U)` `bounded Gauss linearUpwind grad(U)` |
| Gradient · laplacian · snGrad | `Gauss linear` · `Gauss linear limited corrected 0.5` · `limited corrected 0.5` |
| Algorithm · relaxation | SIMPLEC (`consistent yes`); U 0.7, nuTilda 0.7, p 1 |
| Rejected (measured) | `linearUpwind` nuTilda: clipped in 39,994 of 40,000 iterations and froze (round 1). `limitedLinear 1` at relaxation 0.9: clipped in 8,000 of 40,000 iterations and chattered (C-1) |

**Scope.** These are the default for an OpenFOAM SA case until round 3 replaces them. They are **not** shown to be
grid-converged on TMR (the GCI is NO-GO). They are **not** tested at hydrofoil Re (2e5–2e6, where transition
matters) or in 3-D. Every result carries "fully turbulent; transition not modelled", and its Cd is an upper bound
against free transition (spec A5 RANS row). A 3-D wing solve uses D4 or declares the deviation: the round-2 wing
cases used `limitedLinear 1` for nuTilda (`cases/spike03r2-m2c-ar8.yaml` :104). Round 3 tests two hypotheses
for the non-monotone sequence (the limiter that acts on L6 only, and first-order nuTilda). If either changes the
numerics, this decision is amended.

### D5 — The compressibility delta (measured, single grid)

C-4 minus C-2 on TMR L5, α 10°, Re 6e6, M 0.15, SA-noft2, same schemes and relaxation:

- ΔCl = **+0.012370 (+1.147 %)**;
- ΔCd = **+0.000096 (+0.81 %)**.

C-4 is `rhoSimpleFoam` (energy equation, Sutherland μ(T), adiabatic wall, Prt 1.0 against TMR's 0.90) and C-2 is
`simpleFoam`. So Δcomp combines Mach, variable-property and formulation effects. **U_Δ is not stated.** It is at
least the L5 numerical uncertainty, which is unknown because there is no GCI. Δcomp is used **only** to compare an
incompressible OpenFOAM result with a compressible reference (TMR). It is **never applied to water results**
(M ≈ 0.007 at 10 m/s). Prandtl–Glauert gives 1.144 % on Cl; that agrees in magnitude only. Δcomp was measured on
L5 only. Applying it to L6 or L4, as the round-2 comparison table does, is an assumption: the grid mismatch is not
quantified.

### D6 — Mesh gate measures as observed (no new floor)

- checkMesh printed **"Non-orthogonality check OK"** on all three round-2 wing meshes (max 81.2°, 77.3° and 86.3°).
  checkMesh flags faces above 70° but does not fail them. The **≤ 70° limit is the gate's** (case files
  `max_non_orthogonality_deg: 70`; spec A5.10 "named checkMesh measures"). The gate keeps it.
- The checkMesh failures were: determinant (all three meshes); concave cells (snappy M-1a, M-1b1); and face
  interpolation weight < 0.05 (M-1a: 6 faces; Gmsh M-2c: 1 face, 0.046).

### D7 — The "TE blunted for analysis" label (spec 1.7 A5 RANS row; Ruling 66 OQ-13)

When the analysis geometry applies a finite trailing edge (DR-F2-4), the RANS label adds
**"TE blunted for analysis (t_TE <x> mm, <flat | round> base); Cd includes base drag ≈ 2–5 % (Hoerner flat-base
estimate, Flagged; not measured)"**. The 2–5 % band is the hydrodynamicist's **Flagged** estimate for t/c 0.25–0.5 %
(plan-r2 DR-F2-4). A round base is likely lower. The label keeps the word "estimate" until a measured value replaces
it. Round 3's R3-M4 puts the TE base in its own force patch, so the base drag is measured. Results from the two round-2 TE shapes (flat
base in snappy, half-circle in Gmsh, +0.21 % chord) are not comparable in the TE region.

## What this ADR does NOT decide

| Open item | Why | Where it closes |
|---|---|---|
| **Windows** (WSL2 argv rule, Docker Desktop) | No Windows host (DR-F2-7) | a Windows round |
| **Docker image digests** | Docker was never used. The pin here is the native app only | the Windows round, or a Docker route on macOS |
| **v2512 vs v2606** | Both rounds stayed on v2512 so the results stay comparable. gerlero has shipped `openfoam2606-app-arm64.zip` since v2.2.0 (2026-06-29) (plan-r2 §6). A move is a pin change: the bundle derives from that version's `etc/controlDict`, the HOME path `.OpenFOAM/2512` is version-specific, and the TMR fixture and probes re-run | a pin-change run |
| **The mesh route** | snappy: 20 layers on 0 % of faces. Gmsh: 20 layers on 100 % of faces, but non-orthogonality is 86.3° and 1 face has low weight. A product-written structured mesh (plan-r2 L5) is the fallback | [round 3](../plans/fluids-round3.md) R3-M |
| **GCI for OpenFOAM on TMR** | L6/L5/L4 is oscillatory (R = −0.0105 Cl, −0.0526 Cd) | [round 3](../plans/fluids-round3.md) R3-G |
| **The y+ gate** | It needs an A4-stationary 3-D field. The 500-iteration fields are screens only (Gmsh: p95 1.26, max 2.29) | round 3, conditional (DR-F3-4) |
| **The live security proof** | Done 2026-10-04 (D2 evidence); only S-1 and S-2 serial re-run, on a pin change | — |
| SA-neg, LM transition, SST, free surface, cavitation | Not exercised | later rounds |
| The DR-F2-6 floor | A decision request, below | the operator (DR-F3-1) |

## Decision request DR-F3-1 — amend DR-F2-6 (the determinant floor)

**The problem (measured).** In Ruling 65, DR-F2-6 set `cellDeterminant` ≥ 0.001 for OpenFOAM meshes. Every
wall-resolved mesh fails it:

| Mesh | Cells | Cells < 0.001 | Min | Layer cells | Source |
|---|---|---|---|---|---|
| M-1a snappy (14 layers, first ≈ 24 µm) | 2,752,437 | **207,710** | — | ≈ first 1–2 layers over 128,238 faces | `log.checkMesh-det` |
| M-1b1 snappy | 2,665,312 | **210,478** | — | as above | `log.checkMesh-det` |
| M-2c Gmsh (20 layers, first 8 µm) | 3,207,020 | **1,128,345** | 0 | 2,515,440 prisms | `log.checkMesh` :109-110 |

**Why.** OpenFOAM's `cellDeterminant` is `mag(det(Σ_f (S_f/S̄)²))/8` over a cell's internal faces only
(`primitiveMeshTools.C:848-946`). For an a × a × h hex, it equals r⁴·6⁶/(2+4r)⁶ with r = h/a. It falls below 0.001
at r ≈ 0.037, an aspect ratio of about 27 (**Inferred**, arithmetic). A wall-resolved layer cell (first height 8 µm
on 0.75–1.5 mm faces) has an aspect ratio of about 100–190. So the floor measures aspect ratio and fails W3 meshes
by construction. W3 is the wall-resolved floor that Ruling 65 itself adopted in DR-F2-3. A cell with only two
internal faces scores exactly 0. M-2c writes 256 such cells (`log.checkMesh` :67), which explains min 0
(**Inferred**).

| Option | Evidence | Verdict |
|---|---|---|
| **A — validity set, determinant reported (recommended)** | For OpenFOAM unstructured meshes, `cellDeterminant` becomes a reported measure, not a floor. The validity floor is checkMesh's own set: all volumes positive; face pyramids OK; face interpolation weight ≥ 0.05 (checkMesh's threshold, `log.checkMesh` :114); face volume ratio check OK. These sit beside the gate's existing max non-orthogonality ≤ 70° and skewness ≤ 4 (internal) / ≤ 20 (boundary). Under A, M-2c fails on 811 faces above 70° and on 1 face with weight 0.046, which are real defects, and not on its layer aspect ratio | **adopt** |
| B — 0.001 on non-layer cells only | Keeps a connectivity check in the core. But no harvester classifies cells as layer or core, and **no one has measured whether the Gmsh core cells pass** | defer. R3-M0 measures the split. If the core passes, B can be added to A by a later ruling |
| C — keep DR-F2-6 as ruled | It fails every W3 mesh by construction, which blocks DR-F2-3 | reject |
| D — an aspect-normalised determinant | No such OpenFOAM measure exists. It would be a new harvester metric that needs its own verification | reject for now |

The structured-mesh rule is unchanged: a 3×3 Jacobian determinant > 0.3 (≥ 0.15 locally) still applies to a
structured mesh (plan-r2 L5).

**One line:** *Replace `cellDeterminant` ≥ 0.001 on OpenFOAM unstructured meshes with checkMesh's validity set (all
volumes positive, face pyramids OK, face weight ≥ 0.05, volume-ratio check OK) beside the gate's non-orthogonality
≤ 70° and skewness ≤ 4/20. Report `cellDeterminant` split by layer and core cells. Revisit a core-only 0.001 floor
after R3-M0.*

The spec owner amends A5.10's DR-F2-6 parenthesis only after the operator rules.

## Alternatives rejected (for the pinned parts)

- **SU2 referee or SA-neg backend:** Ruling 65 chose DR-F2-2 (c). The SU2 macOS binary is x86_64-only, and general
  Rosetta ends after macOS 27 (plan-r2 §2.1.7).
- **Pinning `allowSystemOperations` with `-info-switch` or `WM_PROJECT_SITE`:** the first was observed not to work.
  With the second, a user-level file still wins.
- **ITTC residual drop alone, or an absolute 1e-7 floor:** the first admits a clipped fixed point and a limit cycle
  (D3 evidence). The second is unreachable for SA on TMR grids, and TMR's own CFL3D also stopped short of it.

## Consequences

- Product code that launches OpenFOAM implements the six D2 rules. A pin change re-runs S-1 and S-2 serial.
- The run states and labels (A5.10, A7) can now name A4 and D4. "Converged" means A4. "Grid U" is shown only when
  a GCI exists, which today is never on OpenFOAM.
- The TMR + A4 fixture (≈ 78 min measured) runs on every pin change. Its GCI clause waits for round 3 or a ruling.
- Nothing here holds on Windows, on v2606 or in Docker.
- The DR-F2-6 floor blocks every wall-resolved mesh until DR-F3-1 is ruled.

## Reviewer verdicts

| Lens | Scope | Verdict | Veto | Conditions → where applied |
|---|---|---|---|---|
| CFD numerical verification (author) | all | — | does not clear its own veto | no "converged" without A4; no GCI from an oscillatory sequence; the determinant arithmetic labelled Inferred |
| Security & identity architect (Adversary, read only) | D1 activation, D2, plan DR-F3-2/6 | **PASS WITH CONDITIONS** (all five pinned hashes re-checked against the files) | cleared for round 3, because its argv is fixed and it adds no app. For the product contract, cleared on conditions 1–3, which are applied | 1 options after the app pass unchecked; the product allow-lists them and refuses `-libs`/`-case`/`-fileHandler`/`-roots`/`-*-switch` (D2 rule 1) · 2 the stop file covers one checkout only; the product keeps a per-user stop file (rule 5) · 3 the gate is now "no product build may enable the backend" until receipts PASS, and a pin change voids them (D2 end) · 4 two residuals added: run-written files not linted; helpers resolved from PATH · 5 order written out (verify → lint → pre-flight → launch); the product keys the pre-flight by the bundle hash · 6 activation row shows the actual `zsh -f -e … --` form · 7 DR-F3-6 wording narrowed to the exact new checkMesh options, with write paths Inferred and recorded in R3-M0 |
| Hydrofoil hydrodynamicist (Adversary, read only) | D4 scope, D5, D7, DR-F3-1 and the round-3 plan | **PASS WITH CONDITIONS** | not triggered | D4: "Cd upper bound vs free transition", and a 3-D solve uses D4 or declares the deviation · D5: Δcomp is L5-only, so applying it to L6 and L4 is an assumption · D7: label wording (flat or round base; Hoerner estimate, Flagged), and base drag measured by a TE force patch in R3-M4. The plan conditions are listed in the plan's §6 |
