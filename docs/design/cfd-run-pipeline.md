---
id: design-cfd-run-pipeline
title: "Design: Area 5 — CFD Run Pipeline (TypedCaseModel, OpenFOAM/SU2 Emitters, DR-F3-1 A Mesh Gate, 11-State Machine, Additive cfdw-project-2 cfd Envelope, Fail-Closed Harvester, and Substrate Cancel/Resume)"
type: design
status: active
owner: "@timianmalloo"
phase: design — Wave 10A (W10-DESIGN; build basis for W10-RUN)
tags: [area-5, cfd-run, openfoam, su2, mesh-gate, state-machine, harvester, cfdw-project-2, persistence, security, wave10]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-0011-analysis-run-storage, rel: refines }
  - { to: adr-0012-openfoam-backend-macos, rel: depends-on }
  - { to: design-guided-solver-setup, rel: depends-on }
  - { to: coordination-wave10-cfd-run-results-and-rail, rel: implements }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-23"
summary: >-
  Build-ready /design-slice for Area 5 CFD Run Pipeline (spec A5.10, RUN-01..06, F7, LAB-01, DR-F2-1, DR-F2-3,
  DR-F2-6, DR-F3-1 A, M1-M8). Freezes the E7 surface list, the complete additive cfdw-project-2 `cfd` envelope
  (`runs`, `fieldEvidence`, `candidates`) preserving byte-identical `-1` and pre-W10 `-2` output with BLAKE3(JCS)
  integrity and fail-closed `DOC-UNSUPPORTED-FIELD` on older builds, the pure STORE-SUBSET Core vs Analysis I/O split
  with recursive `tools/store_subset.py` widening and `ArchitectureTests`-safe Core naming, TypedCaseModel and
  deterministic OpenFOAM ESI v2512 / SU2 v8.5.0 emitters strictly honoring `cases/tools/foam-dict-lint.py` 21-entry
  TYPES_ALLOWED and M1 HOME controlDict separation, DR-F3-1 A / DR-F2-3 MeshGateEvaluator, 11-state CfdRunStateMachine,
  DR-F2-1 three-part residual criterion, LAB-01 three-part Converged label, fail-closed CfdOutputHarvester column
  contract, ICfdRunSubstrate Cancel/Resume/orphan scan, and the red-first test and mutant matrix for W10-RUN.
---

# Design: Area 5 — CFD Run Pipeline

**Status:** Active (Wave 10A Track `W10-DESIGN`, documents only). Freezes every data, security, physics, persistence,
CLI, and naming contract required by Track `W10-RUN` (`docs/coordination/wave10-cfd-run-results-and-rail.md`).

**Goal state.**
- **Goal:** Specify a deterministic, security-hardened, fail-closed Area 5 CFD Run Pipeline that turns a Queued
  Experiment and a Ready `BackendEnvironment` into typed OpenFOAM ESI v2512 or SU2 v8.5.0 cases, enforces the
  `DR-F3-1 A` / `DR-F2-3` mesh gate before solving, executes through the 11-state machine with substrate-level Cancel
  and validated checkpoint Resume, harvests evidence strictly by files, and persists `cfd` facts additively inside
  `cfdw-project-2`.
- **Done when:** `W10-RUN` implements all Core, Analysis, CLI, and recursive `STORE-SUBSET` contracts below, every
  red-first test and live mutant in §9 passes, real `cases/tools/foam-dict-lint.py` exits 0 on emitted cases (exit 3 on
  hostile fixtures), and `w10-verifier` clears all Adversary vetoes.
- **Not in scope:** Live WSL `openfoam2512` solver execution on this host (Cisco Umbrella `net.proxy-tls` blocks
  `sourceforge.net`, `curl rc=60`, measured 2026-11-23 — tests use real `SolverLauncher` `M1–M8`, real
  `cases/tools/foam-dict-lint.py`, real filesystem staging/harvesting, and an injected `ICfdRunSubstrate` so zero fake
  solver receipts are ever claimed); live BYOK LLM calls (Wave 11; Wave 10 ships deterministic offline key-free
  `Explain this failure` and `Preview case diff` validators); free-surface, cavitation, or three-grid GCI study
  execution in v1.
- **Tier:** T2 (durable native document envelope extension, external solver security boundary `M1–M8`, hydrodynamic
  convergence and mesh-quality authority).

---

## 0. Evidence ledger (verified from repository source)

Every repository claim below was **Verified** by direct file inspection in this workspace on 2026-11-23.

| Fact | Verified observation | Source | Confidence |
|---|---|---|---|
| `Envelope` optional top-level members | `Envelope` currently carries optional `[JsonIgnore(WhenWritingNull)]` properties `Analysis`, `Experiments`, and `Setup`. `NativeProject.Read` enforces `Exact(root, [...], ["analysis", "setup", "experiments"])`; unknown members fail closed with `DOC-UNSUPPORTED-FIELD` (`COPY-130`). | `src/CfdWorkbench.Core/AuthoringSession.cs:22-34, 3234` | **Verified** |
| Single format derivation function | `RunRecord.Format(int runCount, int experimentCount = 0, bool hasSetup = false)` returns `"cfdw-project-1"` iff all are zero/false, else `"cfdw-project-2"`. Checked at `NativeProject.Read:3265` and `EnvelopeCore:2217`. | `src/CfdWorkbench.Core/RunRecord.cs:225-229`, `AuthoringSession.cs:2217, 3265` | **Verified** |
| Size & line caps | `NativeProject.MaxBytes = 32_000_000` (Ruling 263), max line length 4,096 UTF-8 bytes, JSON `MaxDepth = 32`, `RespectRequiredConstructorParameters = true`, `UnmappedMemberHandling.Disallow`. | `src/CfdWorkbench.Core/AuthoringSession.cs:3167-3170, 3194-3198, 3229` | **Verified** |
| `STORE-SUBSET` scan scope (`C1`) | `tools/store_subset.py:45-46` currently uses top-level `.glob("*.cs")` over `tests/CfdWorkbench.Core.Tests` and `src/CfdWorkbench.Core`, missing subdirectories such as `SolverSetup/`, `CfdRun/`, and `Results/`. Must widen to `.rglob("*.cs")` skipping `bin/` and `obj/`. | `tools/store_subset.py:45-46` | **Verified** |
| `ArchitectureTests` Core naming guard (`C3`) | `Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt` scans IL member references from `CfdWorkbench.Analysis` into `CfdWorkbench.Core` and fails on ANY called method named `ProfileAt`, `Cancel`, `Undo`, `Redo`, or prefixed `Begin*`, `Update*`, `Apply*`. | `tests/CfdWorkbench.Analysis.Tests/ArchitectureTests.cs:13-35` | **Verified** |
| `foam-dict-lint.py` exact 21-entry `TYPES_ALLOWED` (`C2`) | Enforces 7 token-stream rules: no `#` directives, no quoted keywords, no `coded*`, no `*Libs`, no `REFUSED_WORDS` (`systemCall`, `InfoSwitches`, `OptimisationSwitches`, `DebugSwitches`, `DimensionSets`), `libs` bare names in `LIBS_ALLOWED` (`forces`, `utilityFunctionObjects`, `fieldFunctionObjects`, `sampling`), `runTimeModifiable false` required in `system/controlDict`, `cfdw-manifest.json` required at case root, every `type <v>` in exact 21-entry `TYPES_ALLOWED`, no `$`/`~`/`/` outside strings or `..` in strings. Exit 0 clean / exit 3 refused. | `cases/tools/foam-dict-lint.py:31-42, 66-159` | **Verified** |
| M1 HOME `controlDict` separation | `SolverLauncher.PrepareExclusiveRunDirectory` creates `runs/<runKey>/.home/.OpenFOAM/2512/controlDict` with `InfoSwitches { allowSystemOperations 0; }` and `OptimisationSwitches { stopAtWriteNowSignal 30; }`, and rejects symlinks/junctions and pre-existing dirs. `SolverLauncherPolicy.BuildCleanEnvironment` sets `HOME` to that directory and strips `FOAM_CONTROLDICT`/`WM_*`. | `src/CfdWorkbench.Analysis/SolverSetup/SolverLauncher.cs:177-207`, `src/CfdWorkbench.Core/SolverSetup/SolverLauncherPolicy.cs:49-69, 255-270` | **Verified** |
| Right-sized M1–M8 & `DR-F3-1 A` | Ruling 68 fixes M1–M8 and `DR-F3-1` option A (`checkMesh` validity set + non-orthogonality $\le 70^\circ$ + skewness + determinant reported by layer/core); Ruling 65 fixes `DR-F2-1` (3-part residual criterion), `DR-F2-3` (wall-function feasibility + $y^+$ p95 $\le 1$ / max $\le 2$ + per-region layer coverage), and `DR-F2-6` (`cellDeterminant >= 0.001`). | `docs/notes/rulings.md:787-789, 805-807` | **Verified** |
| Network constraint on live solver | Host TLS probe measured `dl.openfoam.com rc=28` and `sourceforge.net rc=60` (`net.proxy-tls`). Live WSL OpenFOAM execution is honestly struck; tests verify real `SolverLauncher`, real `foam-dict-lint.py`, real filesystem staging/harvesting, and injected `ICfdRunSubstrate`. | `docs/coordination/wave10-cfd-run-results-and-rail.md:59-62, 103` | **Verified** |

---

## 1. End-to-end surface list (E7)

Every Area 5 CFD run datum flows along one explicit path with one authoritative owner per layer:

```
NativeProject store (`Envelope.cfd` on `cfdw-project-2` — append-only runs, fieldEvidence, candidates)
  │
  ▼
CfdWorkbench.Core (pure zero-I/O; STORE-SUBSET recursive verified; zero ArchitectureTests banned names)
  ├── CfdRun/CfdRunRecords.cs       — CfdRecords envelope, CfdRunRecord, FieldEvidenceRecord, CandidateRecord, BLAKE3(JCS) hashes
  ├── CfdRun/TypedCaseModel.cs      — Immutable case inputs, α inflow imposition, meshHash, capability & DR-F2-3 feasibility checks
  ├── CfdRun/OpenFoamCaseEmitter.cs — Deterministic ESI v2512 dicts + cfdw-manifest.json (strict 21-entry TYPES_ALLOWED)
  ├── CfdRun/Su2CaseEmitter.cs      — Deterministic SU2 v8.5.0 .cfg + capability gate
  ├── CfdRun/MeshGateEvaluator.cs   — DR-F3-1 A / DR-F2-3 / DR-F2-6 / ITTC 7.5-03-02-03 evaluator
  ├── CfdRun/CfdRunStateMachine.cs  — 11-state transition table, DR-F2-1 3-part residual oracle, LAB-01 3-part Converged formatter
  ├── AuthoringSession.cs           — RecordCfdRun, RecordFieldEvidence, RecordCandidate, ReadCfdLedger, NativeProject.Read/Check
  └── RunRecord.cs                  — RunRecord.Format(runCount, experimentCount, hasSetup, hasCfd)
  │
  ▼
CfdWorkbench.Analysis (all filesystem, process, launcher, harvester, and checkpoint I/O)
  ├── CfdRun/ICfdRunSubstrate.cs    — Substrate abstraction (stage, mesh, solve, cancel escalation, orphan scan)
  ├── CfdRun/CfdCasePipelineService.cs — Orchestrates Readiness -> Meshing -> MeshGate -> Solving -> Harvesting -> RecordCfdRun,
  │                                      invoking SolverLauncher (M1-M8) and foam-dict-lint pre-flight
  ├── CfdRun/CfdOutputHarvester.cs  — Fail-closed parsers for OpenFOAM postProcessing & SU2 history.csv + checkpoint validator
  ├── CfdRun/CfdRunProjection.cs    — Read-only status, residuals, force history, mesh-gate breakdown, and offline AI-01/AI-11 slots
  └── Labels.cs                     — Fixed Area 5 strings (LAB-01 compliant)
  │
  ├──► CfdWorkbench.Cli (`CfdRunVerb.cs`, `Program.cs`)
  │      └── `cfd-run emit|status|harvest --json` (CLI-01 run-key parity with GUI)
  │
  └──► CfdWorkbench.Desktop (`Run/RunWorkspaceView.axaml(.cs)`, `Run/RunCopy.cs`, owned by W10-RAIL-UI)
         └── Area 5 Run workspace (rail slot 5): queue, 11-state pipeline, mesh gate, residuals/forces, Cancel/Retry, Set up a solver
```

**Reader/writer separation (DM15):**
- **Writers:** `AuthoringSession.RecordCfdRun`, `AuthoringSession.RecordFieldEvidence`, and
  `AuthoringSession.RecordCandidate` (all append-only under the session lock with store-invariant check and `DOC-SIZE`
  preflight; never touch the geometry undo stack; refused after close with `DOC-CLOSED`).
- **Readers:** `AuthoringSession.ReadCfdLedger` (recomputes BLAKE3 content hashes and run keys on every read),
  `CfdRunProjection`, Area 6 `ResultsService` / `ResultsProjection`, CLI `cfd-run status|harvest`, and Desktop
  `RunWorkspaceView` / `ResultsWorkspaceView`.

---

## 2. Additive `cfdw-project-2` `cfd` envelope schema (DM1–DM18)

### 2.1 Bounded context, grain, measures, and history rules

Context **Experiment** (spec A3.1:229), referencing **Design** (`AcceptedId`, `SurfaceHash`, `ProfileHashes`) and
**Analysis** (`WaterRecord`, `OperatingPoint`, `RunReference`) by immutable identity.

`W10-RUN` lands the **complete** `cfd` envelope container (`runs`, `fieldEvidence`, `candidates`) in one step so that
`W10-RESULTS` populates `fieldEvidence` and `candidates` without a second schema change to `Envelope` or
`NativeProject.Read`.

| Entity / Fact | Kind | Grain ("one row is exactly one …") | Primary key & integrity hash | Measures & additivity | History rule |
|---|---|---|---|---|---|
| **`CfdRunRecord`** | append-only fact | one CFD execution attempt of one `TypedCaseModel` (one `SurfaceHash`, `OperatingPoint`, `WaterRecord`, backend pin, settings hash) | `runId` (UUID v4); `runKey` (BLAKE3(JCS)); `contentHash` = BLAKE3(JCS without `contentHash`) | `wallMs`, `cpuSeconds` (**additive** across completed attempts of a sweep); `cells`, `ranks`, `peakRssBytes` (**semi-additive** snapshots); `clMean`, `cdMean`, `yPlusP95`, `yPlusMax`, `uICl`, `uICd` (**non-additive**) | Type-2 immutable; retry appends a new `CfdRunRecord` with `priorRunId` |
| **`CfdRunStateEvent`** | append-only fact inside `CfdRunRecord` | one state transition in the 11-state machine | `(runId, sequence)` contiguous `0..m-1` | `elapsedMs` (monotonic snapshot, non-additive) | Append-only; `sequence` orders (never wall-clock comparison) |
| **`FieldEvidenceRecord`** | append-only fact | one harvested/reduced field artifact of one `CfdRunRecord` | `evidenceId` (UUID v4); `contentHash` = BLAKE3(JCS without `contentHash`); `artifactHash` (BLAKE3 of file) | `sampleCount`, `triangleCount` (**semi-additive**); `minValue`, `maxValue` (**non-additive**) | Type-2 immutable; carries `meshHash` and capability flags (`hasWallShearStress`, `hasTurbulentKineticEnergy`, `isTransient`) |
| **`CandidateRecord`** | append-only entity snapshot | one designated evaluation on the COMMIT-01 promotion ladder | `candidateId` (UUID v4); `contentHash` = BLAKE3(JCS without `contentHash`) | `objectiveValues[]`, `constraintMargins[]` (**non-additive**) | Type-2 immutable; promotion appends a new snapshot referencing `parentCandidateId`, `promotionRunIds[]`, and `discrepancyKeys[]` |

**Derived on read, never stored (DM7):**
- Current state of a `CfdRunRecord`: `Events[^1].State` (never a mutable status column that could disagree with the event log).
- Per-row integrity verdict (`CfdRecordIntegrity.Intact` vs `PayloadFailedCheck`): recomputed via `CfdRunRecords.VerifyRun` / `VerifyEvidence` / `VerifyCandidate` on every read.
- Freshness (`Current` vs `Historical` vs `Unavailable — run payload failed its check`): recomputed `runKey` compared against current accepted geometry + case inputs.
- Three-part `Converged` string: formatted purely by `CfdRunStateMachine.FormatConvergedLabel` from the verified `DR-F2-1` residual summary and artifact oracle result; never stored as free prose.
- Experiment-level status (`Draft | Queued | Running | Completed | Failed | Cancelled`): derived from attempt events per spec A3.1:254.

### 2.2 Exact JSON wire schema (`Envelope.cfd`)

```jsonc
// Optional top-level member on cfdw-project-2; omitted (WhenWritingNull) when runs, fieldEvidence, and candidates are all empty.
{
  "cfd": {
    "runs": [
      {
        "runId": "uuid-v4",
        "priorRunId": null,
        "experimentId": "uuid-v4-or-null",
        "caseIndex": 0,
        "runKey": "blake3-hex64",
        "contentHash": "blake3-hex64-over-JCS-without-contentHash",
        "caseModelHash": "blake3-hex64",
        "meshHash": "blake3-hex64-or-null",
        "backendPin": {
          "backend": "openfoam-esi",            // "openfoam-esi" | "su2"
          "version": "v2512",                   // "v2512" | "v8.5.0"
          "substrate": "wsl2",                  // "macos-app" | "wsl2" | "win-native"
          "buildId": "_bd2b6720-20260127",
          "pinDigest": "sha256-hex64",
          "verifiedInstall": true
        },
        "inputs": {
          "acceptedId": "uuid-v4",
          "surfaceHash": "blake3-hex64",
          "profileHashes": ["blake3-hex64"],
          "evaluator": "cfdw-cv/2",
          "placementRule": "rule-a/1"
        },
        "water": { /* Core WaterRecord */ },
        "op": { /* Core OperatingPoint */ },
        "reference": { /* Core RunReference */ },
        "meshGate": {                           // null if run failed/cancelled before MeshGate
          "passed": true,
          "branch": "wall-resolved",            // "wall-resolved" | "wall-function"
          "maxNonOrthogonalityDeg": 42.3,
          "maxInternalSkewness": 1.8,
          "maxBoundarySkewness": 3.4,
          "minCellVolume": 1.2e-11,
          "minCellDeterminantLayer": 0.042,
          "minCellDeterminantCore": 0.68,
          "layerCoverage20PlusFraction": 0.985,
          "layerCoverage10PlusFraction": 1.0,
          "regionCoverage": [
            { "region": "leading-edge", "minLayers": 20, "fractionAtLeast20": 0.99, "fractionAtLeast10": 1.0 },
            { "region": "main-surface", "minLayers": 20, "fractionAtLeast20": 0.99, "fractionAtLeast10": 1.0 },
            { "region": "trailing-edge-3pct", "minLayers": 16, "fractionAtLeast20": 0.95, "fractionAtLeast10": 1.0 },
            { "region": "tip", "minLayers": 15, "fractionAtLeast20": 0.96, "fractionAtLeast10": 1.0 }
          ],
          "yPlusP95": 0.62,                     // null during pre-solve mesh screen
          "yPlusMax": 1.38,                     // null during pre-solve mesh screen
          "isScreenOnly": false,
          "failures": []                        // non-empty list of { measure, observed, threshold, message } when !passed
        },
        "residualSummary": {                    // null unless Harvesting parsed solverInfo & forceCoeffs
          "maxInitialResidualRatio": 4.2e-4,    // max_eq(r_final / r_first) <= 1e-3 required
          "clWindowMean": 0.412,
          "cdWindowMean": 0.0184,
          "cmWindowMean": -0.041,
          "clHalfBandUi": 1.8e-4,
          "cdHalfBandUi": 4.5e-5,
          "clHalfBandCap": 1.0e-3,
          "cdHalfBandCap": 2.0e-4,
          "averagingWindowIterations": 200,
          "turbulenceBoundingCountInWindow": 0, // must be 0 for clean window
          "ranToIterationLimit": false,
          "criterionMet": true
        },
        "resources": {
          "cells": 1840000,
          "ranks": 4,
          "wallMs": 142300.0,
          "cpuSeconds": 512.4,
          "peakRssBytes": 2147483648
        },
        "checkpoint": {                         // null when no validated checkpoint exists
          "timeDirectory": "500",
          "manifestHash": "blake3-hex64",
          "harvesterValidated": true
        },
        "events": [
          { "sequence": 0, "state": "pending",    "atUtc": "2026-11-23T12:00:00Z", "code": null, "reason": null },
          { "sequence": 1, "state": "readiness",  "atUtc": "2026-11-23T12:00:01Z", "code": null, "reason": null },
          { "sequence": 2, "state": "meshing",    "atUtc": "2026-11-23T12:00:02Z", "code": null, "reason": null },
          { "sequence": 3, "state": "mesh-gate",  "atUtc": "2026-11-23T12:01:10Z", "code": null, "reason": null },
          { "sequence": 4, "state": "solving",    "atUtc": "2026-11-23T12:01:12Z", "code": null, "reason": null },
          { "sequence": 5, "state": "harvesting", "atUtc": "2026-11-23T12:03:20Z", "code": null, "reason": null },
          { "sequence": 6, "state": "completed",  "atUtc": "2026-11-23T12:03:22Z", "code": null, "reason": null }
        ]
      }
    ],
    "fieldEvidence": [
      {
        "evidenceId": "uuid-v4",
        "runId": "uuid-v4",
        "contentHash": "blake3-hex64",
        "kind": "surface",                      // "surface" | "slice" | "rake" | "isosurface" | "history"
        "relativePath": "postProcessing/wingSurface/500/wing.raw",
        "artifactHash": "blake3-hex64",
        "meshHash": "blake3-hex64",
        "fields": ["Cp", "Cf", "wallShearStress"],
        "association": "face",                  // "face" | "point" | "cell" | "strip"
        "ranges": [
          { "variable": "Cp", "unit": "dimensionless", "min": -1.84, "max": 1.00, "basis": "sample-range" }
        ],
        "colormapPolicyVersion": "crameri-8.0",
        "reductionTool": "cfdw-harvester",
        "reductionToolVersion": "1.0.0",
        "hasWallShearStress": true,
        "separationCriterion": "skin-friction-line convergence",
        "hasTurbulentKineticEnergy": false,
        "isTransient": false,
        "timesteps": []
      }
    ],
    "candidates": [
      {
        "candidateId": "uuid-v4",
        "parentCandidateId": null,
        "contentHash": "blake3-hex64",
        "experimentId": "uuid-v4",
        "evaluationId": "eval-001",
        "baseAcceptedId": "uuid-v4",
        "baseSurfaceHash": "blake3-hex64",
        "candidateAcceptedId": null,
        "status": "vlm-checked",                // "surrogate-candidate" | "vlm-checked" | "cfd-checked" | "experimentally-compared"
        "feasible": true,
        "tier": "vlm-strip",
        "objectives": [{ "name": "L/D", "unit": "dimensionless", "value": 21.4 }],
        "constraints": [{ "name": "cavitation-margin", "unit": "ratio", "value": 1.32, "bound": 1.15, "satisfied": true }],
        "promotionRunIds": ["uuid-v4"],
        "discrepancyKeys": ["blake3-hex64"],
        "parameterDeltas": [{ "channel": "trailing", "vertexId": "cv-3", "deltaValue": -0.005, "unit": "m" }]
      }
    ]
  }
}
```

### 2.3 Backward compatibility, `NativeProject.Read`, and rollback

1. **Exact top-level admission (`AuthoringSession.cs:3234`):**
   ```csharp
   Exact(root, ["format", "projectId", "sources", "designs", "accepted", "cursors", "recovery"],
         ["analysis", "setup", "experiments", "cfd"]);
   ```
   When `"cfd"` is present:
   ```csharp
   Exact(cfdElement, "runs", "fieldEvidence", "candidates");
   // All three must be JSON arrays and their combined length must be > 0 (omitted when empty).
   ```
2. **Format derivation (`RunRecord.cs:225-229`):**
   ```csharp
   public static string Format(int runCount, int experimentCount = 0, bool hasSetup = false, bool hasCfd = false)
   {
       Guard.Require(runCount >= 0 && experimentCount >= 0, "DOC-SCHEMA");
       return runCount == 0 && experimentCount == 0 && !hasSetup && !hasCfd
           ? "cfdw-project-1"
           : "cfdw-project-2";
   }
   ```
   All existing 1-, 2-, and 3-argument callers continue compiling unchanged via default `hasCfd = false`.
3. **Byte-identical preservation:** When `cfdRuns.Count == 0 && fieldEvidence.Count == 0 && candidates.Count == 0`,
   `Envelope.Cfd` is `null` (`WhenWritingNull`), so every `cfdw-project-1` and pre-Wave-10 `cfdw-project-2` document
   serializes **byte-for-byte identically** (`Project1_NoRun_ByteIdenticalToToday` remains green).
4. **Fail-closed older builds & `.v1.bak`:** Any build predating Wave 10 has `Exact(..., ["analysis", "setup", "experiments"])`
   (or `["analysis"]`) and immediately refuses a file carrying `"cfd"` with `DOC-UNSUPPORTED-FIELD` (`COPY-130`) before
   any write. The first transition from `cfdw-project-1` to `cfdw-project-2` writes `<name>.v1.bak` first via
   `ProjectStore` (ADR-0011 §6).
5. **Structural store invariants (`CfdRunRecords.CheckStore`, called by `NativeProject.Check` and `AuthoringSession.RecordCfdRun`/`RecordFieldEvidence`/`RecordCandidate`):**
   - All UUIDs valid (`NativeProject.Uuid`); all hashes lowercase 64-hex (`DOC-INTEGRITY`); all doubles finite (`DOC-SCHEMA`).
   - `runId`, `evidenceId`, `candidateId` each globally unique within their list (`DOC-CFD-ID`).
   - Every `run.Inputs.AcceptedId` exists in `accepted` (`DOC-REFERENCE`).
   - Every `evidence.RunId` exists in `cfd.runs` (`DOC-REFERENCE`), and `evidence.HasWallShearStress == false` implies
     `evidence.SeparationCriterion is null` (`DOC-SCHEMA`, enforcing the VIZ rule at persistence level!).
   - Every `candidate.BaseAcceptedId` exists in `accepted` (`DOC-REFERENCE`); `candidate.Status == "cfd-checked"` requires
     non-empty `promotionRunIds` referencing existing `Completed` CFD runs (`CAND-01`).
   - Per-row `ContentHash` tampering does **not** crash `NativeProject.Read`: `ReadCfdLedger()` recomputes every
     `ContentHash` and `RunKey` and marks tampered rows `CfdRecordIntegrity.PayloadFailedCheck` (`"Unavailable — run
     payload failed its check"`), never Current and never deleted (ADR-0011 §4).

---

## 3. Pure `STORE-SUBSET` Core vs Analysis I/O split & `ArchitectureTests` naming contract (`C1`, `C3`)

### 3.1 Recursive `tools/store_subset.py` widening (`C1`)

In `W10-RUN`, lines 45–46 of `tools/store_subset.py` widen from single-directory `.glob("*.cs")` to recursive
`.rglob("*.cs")` skipping generated `bin/` and `obj/` build outputs:

```python
def _non_build_cs(root: Path) -> list[Path]:
    return sorted(
        path for path in root.rglob("*.cs")
        if not any(part in ("bin", "obj") for part in path.relative_to(root).parts)
    )

others = [path for path in _non_build_cs(STORE_TESTS.parent) if path.name not in PARTITION_EXEMPT]
others += _non_build_cs(ROOT / "src/CfdWorkbench.Core")
```

This enforces zero filesystem/environment/PInvoke/persistence leakage across `src/CfdWorkbench.Core/SolverSetup/**`,
`src/CfdWorkbench.Core/CfdRun/**`, `src/CfdWorkbench.Core/Results/**`, and all `tests/CfdWorkbench.Core.Tests/**`
files outside the 6-file `PARTITION_EXEMPT` set.

| Layer | Directory | Allowed capabilities | Forbidden primitives |
|---|---|---|---|
| **Core (Pure)** | `src/CfdWorkbench.Core/CfdRun/` | Deterministic records, BLAKE3(JCS), string/UTF-8 emission, math, state transitions, validation | `File.*`, `Directory.*`, `FileStream`, `FileInfo`, `GetTempPath`, `GetEnvironmentVariable`, `Process`, `DllImport`, `LibraryImport`, `ProjectStore`, `CfdWorkbench.Persistence` |
| **Analysis (I/O)** | `src/CfdWorkbench.Analysis/CfdRun/` | Filesystem staging (`FileShare.Read \| FileShare.Delete` on reads), `SolverLauncher` (`M1–M8`), `ICfdRunSubstrate`, process cancellation & orphan scan, harvester file parsing | Direct mutation of Core geometry edit verbs (`ProfileAt`, `Cancel`, `Undo`, `Redo`, `Begin*`, `Update*`, `Apply*`) |

### 3.2 `ArchitectureTests` Core method-naming table (`C3`)

`Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt` (`tests/CfdWorkbench.Analysis.Tests/ArchitectureTests.cs:13-14`)
forbids ANY method in `CfdWorkbench.Core` called by `CfdWorkbench.Analysis` from being named:
- Exact names: `ProfileAt`, `Cancel`, `Undo`, `Redo`
- Prefixes: `Begin*`, `Update*`, `Apply*`

Every Core method in `CfdWorkbench.Core.CfdRun` and `AuthoringSession` called by `CfdWorkbench.Analysis` MUST use the
exact safe names below:

| Core Type | Safe Method Name (called by Analysis) | Banned Name Avoided | Purpose |
|---|---|---|---|
| `TypedCaseModel` | `ComputeCaseKey()`, `ComputeMeshHash()`, `CheckWallFunctionFeasibility()`, `CheckCapability(BackendCapability)` | — | Pure case identity, mesh-hash derivation, `DR-F2-3` feasibility, capability pre-check |
| `OpenFoamCaseEmitter` | `EmitCaseFiles(TypedCaseModel)` | — | Returns deterministic `IReadOnlyDictionary<string, string>` (case dicts + `cfdw-manifest.json`) |
| `Su2CaseEmitter` | `EmitConfigFiles(TypedCaseModel)` | — | Returns deterministic `.cfg` + manifest dictionary |
| `MeshGateEvaluator` | `EvaluateMeshGate(MeshMetrics, MeshGateOptions)` | — | Pure `DR-F3-1 A` / `DR-F2-3` / `DR-F2-6` gate evaluation |
| `CfdRunStateMachine` | `CreatePending(string runId, DateTimeOffset atUtc)` | `BeginRun` (BANNED) | Initial `Pending` event |
| `CfdRunStateMachine` | `EvaluateTransition(CfdRunState current, CfdRunState next, string? code, string? reason)` | `ApplyTransition` / `UpdateState` (BANNED) | Pure transition legality check |
| `CfdRunStateMachine` | `TransitionToCancelled(IReadOnlyList<CfdRunStateEvent> events, DateTimeOffset atUtc, string reason)` | `Cancel` (BANNED) | Appends legal `Cancelled` terminal event |
| `CfdRunStateMachine` | `TransitionToFailed(IReadOnlyList<CfdRunStateEvent> events, DateTimeOffset atUtc, string code, string reason)` | `ApplyFailure` (BANNED) | Appends legal `Failed` terminal event |
| `CfdRunStateMachine` | `EvaluateResiduals(ResidualWindowInput input)` | — | Pure `DR-F2-1` three-part residual criterion |
| `CfdRunStateMachine` | `FormatConvergedLabel(ResidualSummary summary, bool outputsComplete)` | — | Pure `LAB-01` three-part `"Converged: ..."` formatter |
| `CfdRunRecords` | `ContentHash(CfdRunRecord)`, `VerifyRun(CfdRunRecord)`, `VerifyEvidence(FieldEvidenceRecord)`, `VerifyCandidate(CandidateRecord)`, `CheckStore(...)` | — | Canonical BLAKE3(JCS) hashing & invariants |
| `AuthoringSession` | `RecordCfdRun(CfdRunRecord)`, `RecordFieldEvidence(FieldEvidenceRecord)`, `RecordCandidate(CandidateRecord)`, `ReadCfdLedger()` | `UpdateCfdRun` / `ApplyCfdRun` (BANNED) | Append-only persistence & read-time verification |

---

## 4. `TypedCaseModel` & Dual Deterministic Emitters (`C2`, M1–M8)

### 4.1 `TypedCaseModel` (one model, two emitters)

`TypedCaseModel` is an immutable Core record representing a single CFD case:
- **Geometry & reference inputs:** `RunInputs` (`AcceptedId`, `SurfaceHash`, `ProfileHashes`, `Evaluator`, `PlacementRule`), `RunReference` (`SRef`, `BRef`, `CRef`, `MomentDatum`, `Axes`), `TeFloorMm` (for blunted-TE disclosure if applicable), wing bounding box (`MinBounds`, `MaxBounds`).
- **Fluid & operating point:** `WaterRecord` (validated 0–50 °C, 0 or 35.16504 g/kg, pinned ITTC table hash), `OperatingPoint` (`Speed > 0`, `PAtm > 0`, `HRef?`, `Datum`, `AlphaDeg`).
- **$\alpha$ imposition by inflow direction (spec A5.10:1124):**
  $$\mathbf{U}_\infty = \bigl(V \cos\alpha,\; 0,\; V \sin\alpha\bigr), \quad \hat{\mathbf{d}}_{\text{lift}} = (-\sin\alpha,\; 0,\; \cos\alpha), \quad \hat{\mathbf{d}}_{\text{drag}} = (\cos\alpha,\; 0,\; \sin\alpha)$$
  Because the wing mesh stays fixed in body axes (`+x` aft, `+y` starboard, `+z` up) and only the inflow vector rotates with $\alpha$, **one mesh serves every $\alpha$ at a held geometry and speed**. `ComputeMeshHash()` hashes `(SurfaceHash, ProfileHashes, Speed, WaterRecord, DomainExtents, MeshSettings)` — explicitly **excluding** `AlphaDeg` — so every $\alpha$ sample in a held-speed sweep shares an identical `meshHash` (enabling `ReplaySequence` and `DifferenceFlood` in Area 6!).
- **Turbulence & wall treatment:**
  - `TurbulenceModel`: `SpalartAllmaras` (default v1) | `KOmegaSst`.
  - `WallTreatment`: `WallResolved` (default, target $y^+ \le 1$) | `WallFunction` (target $y^+ \in [30, 100]$, admitted **only** when `CheckWallFunctionFeasibility()` passes `DR-F2-3`, §5.2).
  - `TurbulenceIntensity` (default 0.01), `ViscosityRatio` ($\tilde{\nu}/\nu$ or $\mu_t/\mu$, default 3.0), `TransitionMode` (`FullyTurbulent`).
- **Domain & numerics:** `UpstreamChords` ($\ge 10.0$), `DownstreamChords` ($\ge 20.0$), `LateralHalfSpans` ($\ge 5.0$), `VerticalChords` ($\ge 10.0$), `MaxIterations` (default 2000), `AveragingWindowIterations` (default 200), `ClHalfBandCap` (default $10^{-3}$), `CdHalfBandCap` (default $2 \times 10^{-4}$), `ResidualDropTarget` ($10^{-3}$), `PhysicsFlags` (`RequiresFreeSurface`, `RequiresCavitation`, `Transient`).
- **Security invariant (`M4`):** Zero user-entered strings (project title, profile display name, foil filename, notes) ever enter `TypedCaseModel` emission templates; every floating-point number is validated `double.IsFinite` and formatted with `CultureInfo.InvariantCulture` (`"G17"`). Any `NaN`, `Infinity`, or locale decimal comma (`1,5`) is refused with `CFD-CASE-NONFINITE` before emission.

### 4.2 OpenFOAM ESI v2512 Emitter (`OpenFoamCaseEmitter`) — strict `foam-dict-lint.py` compliance (`C2`)

`OpenFoamCaseEmitter.EmitCaseFiles(TypedCaseModel)` returns a sorted `IReadOnlyDictionary<string, string>` using Unix
LF (`\n`) endings. Every emitted file is validated in CI by executing the **real** `cases/tools/foam-dict-lint.py`
script against the written case directory (must exit `0` clean; hostile fixture mutation must exit `3`).

**Strict compliance rules verified against `cases/tools/foam-dict-lint.py`:**

1. **M1 HOME `controlDict` separation:** `InfoSwitches { allowSystemOperations 0; }` is **NEVER** written into the case
   directory (`InfoSwitches` is in `foam-dict-lint.py`'s `REFUSED_WORDS`). It lives strictly in `SolverLauncher`'s
   app-owned HOME `.OpenFOAM/2512/controlDict` (`SolverLauncherPolicy.EmitUserControlDict()`).
2. **`system/controlDict` mandatory flag:** MUST contain `runTimeModifiable false;` (rule 5 of `foam-dict-lint.py`).
3. **Case manifest (`cfdw-manifest.json`):** Written at the case root as a sorted JSON array listing every emitted
   relative path (`0/U`, `0/nuTilda`, `0/nut`, `0/p`, `constant/transportProperties`, `constant/turbulenceProperties`,
   `system/blockMeshDict`, `system/controlDict`, `system/fvSchemes`, `system/fvSolution`, `system/snappyHexMeshDict`).
   Unlisted dictionaries in `system/`, `constant/`, `0/`, or `0.orig/` are refused by `foam-dict-lint.py`.
4. **Forbidden tokens (zero occurrences anywhere):** `#` directives (`#include*`, `#codeStream`, `#calc`, `#eval`, `#{`),
   quoted regex keywords, any word containing `coded` (any case), any word ending in `Libs`, `systemCall`,
   `InfoSwitches`, `OptimisationSwitches`, `DebugSwitches`, `DimensionSets`, `$`/`~`/`/` outside strings, or strings
   starting with `/`/`~` or containing `$` or `..`.
5. **`libs` allow-list:** Bare names only inside `(...)` from `LIBS_ALLOWED = {"forces", "utilityFunctionObjects", "fieldFunctionObjects", "sampling"}`.
6. **Exact 21-entry `TYPES_ALLOWED` contract:** Because `foam-dict-lint.py:91-94` checks **every** `type <v>` token
   preceded by `{`, `;`, or `}` across **every** manifest file, EVERY `type` emitted by `OpenFoamCaseEmitter` MUST
   belong strictly to:
   ```
   forceCoeffs · solverInfo · yPlus · surfaces · wallShearStress · patch · wall · empty ·
   symmetryPlane · freestreamVelocity · freestreamPressure · noSlip · zeroGradient ·
   inletOutlet · fixedValue · calculated · triSurfaceMesh · box · cellSet · hePsiThermo · symmetry
   ```

| Emitted File | Key Entries & `type` / `libs` Tokens Used | Verification against `foam-dict-lint.py` |
|---|---|---|
| `system/controlDict` | `application simpleFoam;`, `runTimeModifiable false;`, `functions`: `forceCoeffs` (`type forceCoeffs; libs (forces);`), `solverInfo` (`type solverInfo; libs (utilityFunctionObjects);`), `yPlus` (`type yPlus; libs (fieldFunctionObjects);`), `wallShearStress` (`type wallShearStress; libs (fieldFunctionObjects);`), `wingSurface` (`type surfaces; libs (sampling);` with surface sub-entry `type patch;`) | Zero `"forces"` `type` (uses `forceCoeffs` with `libs (forces)`!). Zero unlisted `cuttingPlane` `type` — wing surface sampling uses `type patch;` (inside `TYPES_ALLOWED`); slice post-extraction stays in ParaView/harvester or an approved future lint amendment. |
| `system/fvSchemes` | Steady-state `ddtSchemes { default steadyState; }`, bounded Gauss upwind/linearUpwind `divSchemes`, `Gauss linear limited corrected 0.5` `laplacianSchemes` | Zero `type` tokens; zero `#` directives |
| `system/fvSolution` | `solvers` (`p` GAMG, `U`/`nuTilda` smoothSolver), `SIMPLE` (`nNonOrthogonalCorrectors 2;`, `residualControl`), `relaxationFactors` | Zero `type` tokens |
| `system/blockMeshDict` | Domain box ($\ge 10 c_{\text{ref}}$ upstream, $\ge 20 c_{\text{ref}}$ downstream), patches `farfield` (`type patch;`), `symmetry` (`type symmetryPlane;`) | Only `patch`, `symmetryPlane` |
| `system/snappyHexMeshDict` | `geometry`: `wing.stl` (`type triSurfaceMesh;`), `refinementBox` (`type box;`); `castellatedMeshControls`, `snapControls`, `addLayersControls` (`nSurfaceLayers 20; expansionRatio 1.18;`), `meshQualityControls` (`maxNonOrtho 65; maxBoundarySkewness 15; maxInternalSkewness 4; minVol 1e-13; minDeterminant 0.001;`) | Only `triSurfaceMesh`, `box` |
| `constant/transportProperties` | `transportModel Newtonian;`, `nu [0 2 -1 0 0 0 0] <nu>;` | Zero `type` tokens |
| `constant/turbulenceProperties` | `simulationType RAS;`, `RAS { RASModel SpalartAllmaras; turbulence on; printCoeffs on; }` (or `kOmegaSST`) | Zero `type` tokens |
| `0/U` | `farfield` (`type freestreamVelocity; freestreamValue uniform (Ux 0 Uz);`), `wing` (`type noSlip;`), `symmetry` (`type symmetryPlane;`) | Only `freestreamVelocity`, `noSlip`, `symmetryPlane` |
| `0/p` | `farfield` (`type freestreamPressure; freestreamValue uniform 0;`), `wing` (`type zeroGradient;`), `symmetry` (`type symmetryPlane;`) | Only `freestreamPressure`, `zeroGradient`, `symmetryPlane` |
| `0/nuTilda` | `farfield` (`type inletOutlet; inletValue uniform <nuTildaInf>; value uniform <nuTildaInf>;`), `wing` (`type fixedValue; value uniform 0;`), `symmetry` (`type symmetryPlane;`) | Only `inletOutlet`, `fixedValue`, `symmetryPlane` |
| `0/nut` | `farfield` (`type calculated; value uniform 0;`), `wing` (`type calculated; value uniform 0;`), `symmetry` (`type symmetryPlane;`) | Only `calculated`, `symmetryPlane` — **zero unlisted wall-function BC types** (`nutUSpaldingWallFunction` etc. are NOT emitted because they are outside `TYPES_ALLOWED`; OpenFOAM cases in Wave 10 emit wall-resolved `WallResolved` BCs only; requesting `WallFunction` for OpenFOAM returns a clear unsupported-BC refusal unless a future Owner ruling + security audit widens `TYPES_ALLOWED`) |
| `cfdw-manifest.json` | Sorted JSON array of all 11 relative paths above | Satisfies `foam-dict-lint.py:118-129` |

### 4.3 SU2 v8.5.0 Emitter (`Su2CaseEmitter`) & Capability Gate

- **Capability pre-check (before leaving `Pending`):** SU2 v8.5.0 has no VOF/free-surface model, no cavitation model,
  and no built-in mesher (spec A5.10:1091–1092). `TypedCaseModel.CheckCapability` checks the `BackendEnvironment`
  capability record: if `RequiresFreeSurface || RequiresCavitation`, the run transitions immediately
  `Pending → Unsupported` with reason `"SU2 v8.5.0 does not support free-surface or cavitation physics"`.
- **Deterministic `.cfg` emission (`case.cfg` + `cfdw-manifest.json`):**
  - Strictly typed key-value pairs in deterministic section order, invariant-culture `"G17"` numbers, LF `\n`, zero `#`
    directives/macros/shell tokens, zero user strings.
  - Emits `SOLVER= INC_RANS`, `KIND_TURB_MODEL= SA` (or `SST`), `INC_DENSITY_MODEL= CONSTANT`,
    `INC_DENSITY_INIT= <rho>`, `INC_VELOCITY_INIT= ( <Ux>, 0.0, <Uz> )`, `VISCOSITY_MODEL= CONSTANT_VISCOSITY`,
    `MU_CONSTANT= <rho * nu>`, `AOA= <alphaDeg>`, `REF_AREA= <SRef>`, `REF_LENGTH= <CRef>`,
    `ITER= <MaxIterations>`, `HISTORY_OUTPUT= (ITER, RMS_RES, AERO_COEFF, YPLUS)`, `CONV_FILENAME= history`,
    `SURFACE_FILENAME= surface_flow`, `VOLUME_FILENAME= flow`, `OUTPUT_FILES= (RESTART, SURFACE_CSV, PARAVIEW_MULTIBLOCK)`,
    `SCREEN_OUTPUT= (INNER_ITER, RMS_PRESSURE, RMS_VELOCITY-X, LIFT, DRAG)`.

---

## 5. `MeshGateEvaluator` (`DR-F3-1 A`, `DR-F2-3`, `DR-F2-6`, ITTC 7.5-03-02-03)

`MeshGateEvaluator.EvaluateMeshGate(MeshMetrics, MeshGateOptions)` is a pure Core function that evaluates every
quality threshold and stops a failing case **before** `Solving` (`MeshGate → Failed("MESH-GATE-FAIL")`), listing every
failing measure, observed value, and exact threshold.

### 5.1 Exact threshold table

| Check | Measure | Threshold (Pass iff) | Basis |
|---|---|---|---|
| **Validity: positive volumes** | `MinCellVolume` | `> 0` (and finite) | `DR-F3-1 A` (Ruling 68) |
| **Validity: mesh topology** | `InvalidFaces`, `ZeroAreaFaces`, `OpenCells`, `FailedCheckMeshCount` | All `== 0` | `DR-F3-1 A` (Ruling 68) |
| **Max non-orthogonality** | `MaxNonOrthogonalityDeg` | `<= 70.0` deg | `DR-F3-1 A` (Ruling 68) |
| **Internal skewness** | `MaxInternalSkewness` | `<= 4.0` | `DR-F3-1 A` (Ruling 68) |
| **Boundary skewness** | `MaxBoundarySkewness` | `<= 20.0` | `DR-F3-1 A` (Ruling 68) |
| **Cell determinant (OpenFOAM)** | `MinCellDeterminantLayer`, `MinCellDeterminantCore` | Both `>= 0.001` (reported separately for layer vs core) | `DR-F2-6` (Ruling 65) & `DR-F3-1 A` (Ruling 68) |
| **Cell determinant (Structured only)** | `MinStructuredDeterminant`, `MeanStructuredDeterminant` | `>= 0.15` locally and `> 0.30` bulk (only when `MeshKind == Structured`) | `DR-F2-6` (Ruling 65) |
| **Domain extents** | `UpstreamChords`, `DownstreamChords` | `>= 10.0` upstream AND `>= 20.0` downstream | ITTC 7.5-03-02-03 (spec A5.10:1117) |
| **Layer expansion ratio** | `LayerExpansionRatio` | `<= 1.2` | ITTC 7.5-03-02-03 (spec A5.10:1116) |
| **Layer coverage (global + per-region)** | `LeadingEdge`, `MainSurface`, `TrailingEdge3Pct`, `Tip` | `>= 20` layers on `>= 95%` of wing faces AND `>= 10` layers on `100%` of wing faces (reported per region) | `DR-F2-3` (Ruling 65) |
| **Wall-resolved $y^+$ gate** | `YPlusP95`, `YPlusMax` (area-weighted at cell centre, on a stationary field meeting `DR-F2-1` at highest speed & $\alpha$) | `YPlusP95 <= 1.0` AND `YPlusMax <= 2.0` | `DR-F2-3` (Ruling 65) |
| **Wall-function $y^+$ gate** | `YPlusP95`, `YPlusMax` (only when `WallFunction` feasibility passed) | `YPlusMin >= 30.0` AND `YPlusMax <= 100.0` with `>= 15` layer points | `DR-F2-3` (Ruling 65) & ITTC 7.5-03-02-03 |

**Mesh screen vs mesh gate distinction (`DR-F2-3`):** When `yPlus` is evaluated before the field satisfies `DR-F2-1`
stationarity (e.g., after meshing or early iterations), `MeshGateResult.IsScreenOnly` is `true` and copy explicitly
labels it `"mesh screen — not the y+ gate (awaiting stationary field)"`.

### 5.2 Wall-function feasibility check (`DR-F2-3`)

Before admitting `WallTreatment.WallFunction`, `TypedCaseModel.CheckWallFunctionFeasibility` estimates the turbulent
boundary-layer thickness at the reference chord $c_{\text{ref}}$ and highest speed $V_{\max}$:

$$Re_c = \frac{V_{\max}\,c_{\text{ref}}}{\nu}, \qquad C_f \approx 0.058\,Re_c^{-0.2}, \qquad u_\tau = V_{\max}\sqrt{\frac{C_f}{2}}, \qquad \delta \approx 0.37\,c_{\text{ref}}\,Re_c^{-0.2}$$

The first cell centre at $y^+ = 30$ sits at $y_1 = 30\,\nu / u_\tau$; with 15 points expanding at $r \le 1.2$ (starting
step $\Delta y_1 = 2\,y_1$), the required layer stack thickness is:

$$H_{15} = \Delta y_1\,\frac{r^{15} - 1}{r - 1}$$

Wall-function mode is **admitted iff** $H_{15} \le \delta$ (so $\ge 15$ points with $y^+ \ge 30$ fit inside the estimated
turbulent boundary layer over the chord). At typical hydrofoil $Re_c \sim 5\times 10^5$, $H_{15} > \delta$, so the
check honestly refuses wall functions and directs the case to the wall-resolved branch ($y^+ \le 1$).

---

## 6. 11-state `CfdRunStateMachine`, `DR-F2-1` residuals, and `LAB-01` `Converged` label

### 6.1 Exact 11 states & transition matrix

| # | Enum Value | Display String (C2) | Terminal? | Legal Outgoing Transitions |
|---|---|---|---|---|
| 1 | `Pending` | `Pending` | No | `Unsupported`, `Readiness`, `Cancelled`, `Failed` |
| 2 | `Unsupported` | `Unsupported` | **Yes** | *(none)* |
| 3 | `Readiness` | `Readiness` | No | `Meshing`, `Solving` *(resume from validated checkpoint)*, `Failed`, `Cancelled` |
| 4 | `Meshing` | `Meshing` | No | `MeshGate`, `Failed`, `Cancelled` |
| 5 | `MeshGate` | `Mesh gate` | No | `Solving`, `Failed`, `Cancelled` |
| 6 | `Solving` | `Solving` | No | `Harvesting`, `Failed`, `Cancelled` |
| 7 | `Harvesting` | `Harvesting` | No | `Completed`, `CompletedResidualNotMet`, `Failed`, `Cancelled` |
| 8 | `Completed` | `Completed` | **Yes** | *(none)* |
| 9 | `CompletedResidualNotMet` | `Completed — residual criterion not met (ran to limit)` | **Yes** | *(none)* |
| 10 | `Failed` | `Failed (<reason>)` | **Yes** | *(none — retry creates a new `CfdRunRecord` with `priorRunId`)* |
| 11 | `Cancelled` | `Cancelled` | **Yes** | *(none — logs & partial outputs retained)* |

Any transition outside this table is refused deterministically by `EvaluateTransition` with `CFD-STATE-INVALID`.

### 6.2 `DR-F2-1` three-part residual criterion (Ruling 65)

`CfdRunStateMachine.EvaluateResiduals(ResidualWindowInput)` checks **all three** required parts:

1. **Initial residual drop ($\le 10^{-3}$):** For every solved equation $e$ (e.g., `p`, `Ux`, `Uy`, `Uz`, `nuTilda` or
   `k`/`omega`), $r_{e,\text{final}} / r_{e,\text{first}} \le 10^{-3}$ (three orders of magnitude, ITTC). If
   $r_{e,\text{first}} \le 0$ or non-finite, the check fails closed (`CFD-RESIDUAL-INVALID`).
2. **$C_L$ and $C_D$ stationarity over averaging window:** Over the final $W$ iterations (minimum $W \ge 20$), compute
   half-bands $U_{I,C_L} = \frac{1}{2}(\max C_L - \min C_L)$ and $U_{I,C_D} = \frac{1}{2}(\max C_D - \min C_D)$. Pass
   iff $U_{I,C_L} \le \epsilon_{C_L}$ and $U_{I,C_D} \le \epsilon_{C_D}$. The reported force coefficients are the
   arithmetic window means $\overline{C}_L, \overline{C}_D, \overline{C}_m$.
3. **Clean turbulence window:** Zero bounding events of the turbulence variable (`bounding nuTilda` for SA; `bounding k`
   or `bounding omega` for SST) inside the averaging window (`TurbulenceBoundingCountInWindow == 0`).

If the solver reaches `MaxIterations` with valid outputs but ANY of parts 1–3 fails, `Harvesting` transitions to
`CompletedResidualNotMet` (`"Completed — residual criterion not met (ran to limit)"`) — **never** `Completed` with a
`Converged` label.

### 6.3 `LAB-01` three-part `Converged` label (spec A7:1295)

`CfdRunStateMachine.FormatConvergedLabel(ResidualSummary summary, bool outputsComplete)`:
- Returns `null` unless `outputsComplete == true && summary.CriterionMet == true`.
- When both hold, returns the exact three-part fixed string:
  ```
  Converged: residuals <= <r> · outputs complete · grid uncertainty not quantified (single mesh)
  ```
  where `<r>` is formatted invariant-culture scientific notation (e.g., `4.2e-04`) of `summary.MaxInitialResidualRatio`.
- Part 3 **ALWAYS** reads `grid uncertainty not quantified (single mesh)` in v1 (no three-grid study Experiment kind
  exists in v1). A dedicated `LAB-01` lint check verifies that `"Converged"` never appears in Area 5/6 copy or
  projections without all three parts.

---

## 7. Fail-closed `CfdOutputHarvester`, `ICfdRunSubstrate` Cancel, Checkpoint Resume & Orphan Scan

### 7.1 `CfdOutputHarvester` fail-closed column contract

All file reads in `CfdOutputHarvester` use `FileShare.Read | FileShare.Delete` (`READER-SHARE` compliant).

| File Source | Required Header Columns (Exact Names) | Failure Mode if Missing / Unknown Layout |
|---|---|---|
| OpenFOAM `postProcessing/forceCoeffs/<t>/coefficient.dat` | `Time`, `Cd`, `Cl`, `CmPitch` (or `Cm`) | Fail closed `CFD-HARVEST-COLUMNS` naming file & missing/unmapped column |
| OpenFOAM `postProcessing/solverInfo/<t>/solverInfo.dat` | `Time`, `<field>_initial`, `<field>_final`, `<field>_converged` for each active equation (`p`, `U`, `nuTilda`) | Fail closed `CFD-HARVEST-COLUMNS` |
| OpenFOAM `postProcessing/yPlus/<t>/yPlus.dat` | `Time`, `patch`, `min`, `max`, `average` (plus area-weighted p95 from sampled wall field when present) | Fail closed `CFD-HARVEST-COLUMNS` |
| OpenFOAM `postProcessing/wallShearStress/<t>/wallShearStress.dat` | `Time`, `patch`, `min`, `max`, `integral` | Sets `hasWallShearStress = false` if file absent; fails closed `CFD-HARVEST-COLUMNS` if present with unknown columns |
| OpenFOAM `postProcessing/wingSurface/<t>/...` | Header declaring `x y z` + field names (`p`, `Cp`, `wallShearStress`) | Fail closed `CFD-HARVEST-COLUMNS`; computes BLAKE3 `artifactHash` and `[min, max]` ranges |
| SU2 `history.csv` | `"Inner_Iter"` (or `"Time_Iter"`), `"rms[P]"`, `"CL"`, `"CD"`, `"CMz"` (quoted/unquoted trimmed deterministically) | Fail closed `CFD-HARVEST-COLUMNS` naming `history.csv` and bad column |

**Completion oracle over exit code (COMMIT-05, RUN-03):**
- Even when the solver process exits with code `0`, `CfdCasePipelineService` checks that the final time directory and
  all required output files exist and pass `CfdOutputHarvester`. If any required output is missing, the run transitions
  `Harvesting → Failed("CFD-NO-OUTPUTS", "Solver exited 0 with no required outputs")` — **never** `Completed`.

### 7.2 `ICfdRunSubstrate`, Cancel escalation, Checkpoint Resume & Orphan Scan

```csharp
public interface ICfdRunSubstrate
{
    Task<SubstrateStageResult> StageCaseAsync(TypedCaseModel model, IReadOnlyDictionary<string, string> files, CancellationToken ct);
    Task<SubstrateProcessResult> ExecuteStepAsync(SubstrateStepRequest request, CancellationToken ct);
    Task<SubstrateCancelResult> CancelRunAsync(string runKey, TimeSpan boundedWait, CancellationToken ct);
    Task<OrphanScanResult> ScanOrphansAsync(string runKey, CancellationToken ct);
}
```

- **Case directory safety (`M5`):** `runs/<runKey>/` validated by `SolverLauncherPolicy.ValidateRunKey(runKey)`
  (`^[A-Za-z0-9_-]{1,64}$`). Never derived from user text. Rejects symlinks/reparse points and pre-existing directories.
- **Pre-flight lint (`M4`):** Before launching meshing or solving on OpenFOAM, `CfdCasePipelineService` runs the same
  strict token-stream lint invariant verified against `cases/tools/foam-dict-lint.py`.
- **Launcher execution (`M1–M8`):** Invokes `SolverLauncher` with app-owned HOME `controlDict` (`allowSystemOperations 0`),
  argv-only `BuildArgv`, clean environment `BuildCleanEnvironment`, and live master-rank `Disallowing` banner check
  (`ParseMasterBanner`). Any launch missing `Disallowing` stops immediately with `smoke.banner`.
- **Cancel escalation sequence (spec A5.10:1132–1133):**
  1. **Substrate kill:** Send substrate termination to the tagged run key (WSL process-group termination / native PID signal).
  2. **Host tree kill:** `proc.Kill(entireProcessTree: true)` if still alive after grace tick.
  3. **Bounded wait:** Wait up to `boundedWait` (default 5 s; wall duration measured and recorded in `cfd.cancel` telemetry).
  4. **Orphan scan:** `ScanOrphansAsync(runKey)` verifies 0 surviving child processes for `runKey` (any lingering orphan
     is reported in `OrphanScanResult.SurvivingPids` and surfaced honestly; never claims clean if orphans remain).
  5. Record `Cancelled` terminal event via `CfdRunStateMachine.TransitionToCancelled`; retain logs and partial outputs on
     disk; never promote partial outputs to `Completed`.
- **Checkpoint Resume (RUN-04):**
  - `CfdOutputHarvester.ValidateCheckpoint(caseDir, manifestHash)` verifies that the latest numeric time directory
    contains complete, non-empty, header-valid fields (`U`, `p`, `nuTilda` or SU2 `restart_flow.dat`) and that
    `cfdw-manifest.json` hash matches the recorded `caseModelHash`.
  - Only when `harvesterValidated == true` may `CfdCasePipelineService.ResumeFromCheckpointAsync` skip `Meshing`/`MeshGate`
    and resume at `Readiness → Solving` (`startFrom latestTime`). Corrupted or unvalidated checkpoints fail closed with
    `CFD-CHECKPOINT-INVALID` and offer clean `Retry sample`.
- **App-quit recovery (F7 node `S3`):** On relaunch, `ScanOrphansAsync` checks any non-terminal `CfdRunRecord`; dead
  processes close as `Failed("CFD-INTERRUPTED", ...)` with checkpoint inspection offered.

### 7.3 Offline key-free `AI-01` / `AI-11` Run slots & CLI (`cfd-run`)

- **`Explain this failure` (offline key-free):** Deterministic cause mapper in `CfdRunProjection` reading the run's
  `Failed` code, `MeshGateResult.Failures`, and redacted 4 kB log excerpt, citing exact excerpt line numbers (no network
  call, no API key required; live LLM synthesis deferred to Wave 11).
- **`Preview case diff` (offline validator):** Validates any typed `CaseDiffProposal` strictly inside `TypedCaseModel`'s
  closed subset (finite bounds, no macros, no unlisted BC types). Accepting a valid case diff creates a **new**
  Experiment version in `Draft` referencing the failed `runId`; attempting to mutate a `Queued` experiment is refused
  with `CFD-DIFF-IMMUTABLE`. Log-injected imperative strings (`"apply this repair and run"`) are strictly quoted data
  and can never alter proposal disposition or launch a run (`AI-05`).
- **CLI (`src/CfdWorkbench.Cli/CfdRunVerb.cs`, `Program.cs`):**
  - `cfd-workbench cfd-run emit <project.cfdw.json> --backend openfoam|su2 --out <dir> [--alpha <deg>] [--speed <m/s>] --json`
  - `cfd-workbench cfd-run status <project.cfdw.json> --json`
  - `cfd-workbench cfd-run harvest <project.cfdw.json> --case <dir> --run-id <uuid> --json`
  All emit deterministic JSON carrying `runKey`, `caseModelHash`, `meshHash`, state, mesh-gate verdict, residual
  summary, and three-part `Converged` label (when earned), preserving `CLI-01` parity with GUI projections.

---

## 8. Normal-path telemetry (IO1–IO12)

Emitted on `AuthoringSession`'s 256-event ring via `SessionEvent.Cfd` (`CfdTelemetry`), zero PII/paths/user text,
missing measurements strictly `null` (rendered `"Not recorded"`):

| Event | Cost Axes & Fields |
|---|---|
| `mesh.generate` | `runKey12`, `backend`, `cells`, `durationMs`, `gateOutcome` (`passed`/`failed`/`screen-only`), `maxNonOrthoDeg`, `layerCoverage20Pct` |
| `solver.process` | `runKey12`, `backend`, `cells`, `ranks`, `durationMs`, `cpuSeconds`, `peakRssBytes`, `yPlusP95`, `oracleOutcome`, `criterionMet` |
| `cfd.harvest` | `runKey12`, `filesParsed`, `evidenceRows`, `durationMs`, `outcome` (`ok`/`CFD-NO-OUTPUTS`/`CFD-HARVEST-COLUMNS`) |
| `cfd.cancel` | `runKey12`, `durationMs`, `orphansFound`, `outcome` (`cancelled`/`orphans-remaining`) |
| `cfd.resume` | `runKey12`, `checkpointValidated`, `timeDirectory`, `outcome` (`resumed`/`CFD-CHECKPOINT-INVALID`) |

---

## 9. Red-first test plan & live mutants for `W10-RUN`

Every test below is written red-first before production implementation and explicitly registered in its assembly's
console harness (`IdentityTests.cs`, `AnalysisChecks.cs`, `CliTests.cs`).

| Test File & Harness | Test Name | Proves | Live Mutant That Must Fail It |
|---|---|---|---|
| `Core.Tests/CfdRunModelTests.cs` (`IdentityTests.cs`) | `TypedCaseModel_InflowAlpha_SameMeshHashAcrossAngles` | Same geometry & speed at $\alpha = 0^\circ, 4^\circ$ share `meshHash`, differ in `runKey`, and compute exact $(V\cos\alpha, 0, V\sin\alpha)$ | Include `AlphaDeg` in `ComputeMeshHash()` → test fails |
| `Core.Tests/CfdRunModelTests.cs` | `TypedCaseModel_NonFiniteOrLocaleComma_RefusedBeforeEmit` | `NaN`/`Infinity` refused with `CFD-CASE-NONFINITE`; `G17` invariant-culture formatting has zero `,` decimals | Drop `double.IsFinite` check → test fails |
| `Core.Tests/CfdRunModelTests.cs` | `OpenFoamEmitter_DeterministicAndStrictTypesAllowed` | Emits byte-identical dicts across runs; every `type` in 21-entry `TYPES_ALLOWED`; `runTimeModifiable false;`; `cfdw-manifest.json` lists all 11 files; zero `InfoSwitches` in case | Emit `type forces;` or drop `runTimeModifiable false;` → test fails |
| `Core.Tests/CfdRunModelTests.cs` | `Su2Emitter_FreeSurfaceOrCavitation_UnsupportedBeforeLeavingPending` | SU2 capability gate transitions `Pending → Unsupported` on free surface or cavitation; valid single-phase emits deterministic `.cfg` with `HISTORY_OUTPUT` | Skip capability check on SU2 → test fails |
| `Core.Tests/CfdRunModelTests.cs` | `MeshGate_DrF31A_And_DrF23_ExactBoundaries` | Non-ortho $70.0^\circ$ pass / $70.1^\circ$ fail; internal skew $4.0$ / $4.1$; boundary skew $20.0$ / $20.1$; minVol $\le 0$ fail; `cellDeterminant < 0.001` fail; layer coverage $95\%/100\%$ boundary; wall-resolved $y^+$ p95 $1.0$/max $2.0$; pre-stationary marked `IsScreenOnly = true`; `DR-F2-3` wall-function feasibility rejects thin hydrofoil BL | Flip `<= 70.0` to `<= 75.0` or skip `IsScreenOnly` → test fails |
| `Core.Tests/CfdRunModelTests.cs` | `StateMachine_ElevenStates_And_DrF21_ThreePartCriterion` | All 11 states & illegal-edge refusals; `DR-F2-1` fails if ANY of (1) residual drop $> 10^{-3}$, (2) $U_I >$ cap, or (3) `nuTilda` bounding $> 0$ fails → `CompletedResidualNotMet`; `FormatConvergedLabel` always includes `"grid uncertainty not quantified (single mesh)"` | Allow `boundingCount = 1` to pass `DR-F2-1` or drop part 3 of label → test fails |
| `Core.Tests/CfdRunEnvelopeTests.cs` (`IdentityTests.cs`) | `CfdEnvelope_RoundTrip_TamperDetection_And_ByteIdenticalLegacy` | Full `cfd: { runs, fieldEvidence, candidates }` round-trips byte-equal; tampered run/evidence fails check (`PayloadFailedCheck`) without deletion; empty `cfd` keeps `cfdw-project-1` and pre-W10 `cfdw-project-2` byte-identical (`Project1_NoRun_ByteIdenticalToToday` green); unknown top-level member still fails `DOC-UNSUPPORTED-FIELD` | Write `"cfd": {"runs":[],...}` when empty or trust stored `runKey` → test fails |
| `Analysis.Tests/CfdRunPipelineTests.cs` (`AnalysisChecks.cs`) | `OpenFoamEmitter_RealFoamDictLint_CleanExit0_HostileExit3` | Runs real `py -3 cases/tools/foam-dict-lint.py <case-dir>` on emitted full OpenFOAM case → **exit 0**; injects `#codeStream` / unlisted `type forces` → **exit 3** | Emit any token outside `TYPES_ALLOWED` or omit `cfdw-manifest.json` → exit 3 fails test |
| `Analysis.Tests/CfdRunPipelineTests.cs` | `Pipeline_CancelEscalation_OrphanScan_And_ValidatedCheckpointResume` | Cancel runs substrate kill → tree kill → bounded wait → orphan scan → `Cancelled` retaining partial outputs; Resume succeeds only on harvester-validated checkpoint and refuses corrupt checkpoint (`CFD-CHECKPOINT-INVALID`) | Skip orphan scan or allow resume on unvalidated checkpoint → test fails |
| `Analysis.Tests/CfdRunPipelineTests.cs` | `Pipeline_ExitZeroMissingOutputs_FailedNoOutputs` | Injected substrate returns exit 0 with empty final time dir → transitions to `Failed("CFD-NO-OUTPUTS")`, never `Completed` | Trust exit code 0 without harvester oracle → test fails |
| `Analysis.Tests/CfdHarvesterTests.cs` (`AnalysisChecks.cs`) | `Harvester_OpenFoamAndSu2_FailClosedOnUnknownColumns` | Parses valid OpenFOAM `forceCoeffs`/`solverInfo`/`yPlus`/`wallShearStress` and SU2 `history.csv`; mutated/missing column header fails closed with `CFD-HARVEST-COLUMNS` naming file | Fall back to positional column index → test fails |
| `Cli.Tests/CfdRunCliTests.cs` (`CliTests.cs`) | `Cli_CfdRun_EmitStatusHarvest_ParityWithCore` | `cfd-run emit`, `cfd-run status`, `cfd-run harvest --json` match Core `runKey`, `caseModelHash`, `meshHash`, and `Converged` label; invalid args return stable non-zero codes | Diverge CLI runKey calculation from `TypedCaseModel` → test fails |
| Static Gates | `tools/store_subset.py` (recursive), `Architecture_AnalysisAssembly_NoEditVerbsNoProfileAt`, `check-reader-sharing.py` | Zero I/O primitives in `Core/CfdRun/**`; zero banned Core verbs called from `Analysis`; all file reads use delete-sharing | Put `File.WriteAllText` in `Core/CfdRun/` or name a Core method `Cancel` → gate fails |
