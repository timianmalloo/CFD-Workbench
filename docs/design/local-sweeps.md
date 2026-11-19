---
id: design-local-sweeps
title: "Design: Area 4 — local-tier sweeps & performance curves (Experiment setup, local Run, and sweep Results over VLM + strip and Estimator)"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — Area 4 (Wave 5 SWP; documents only; no build until mockup and DR-SWP-1..6 are approved)
tags: [sweeps, experiment, area-4, analysis, local-tier, performance-curves, data-model, retention, telemetry, mockup]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-amendment-local-sweeps, rel: implements }
  - { to: adr-0011-analysis-run-storage, rel: refines }
  - { to: design-area3-analysis, rel: refines }
  - { to: design-a3d-compare, rel: relates-to }
  - { to: design-language, rel: depends-on }
  - { to: mockup-local-sweeps, rel: tested-by }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
  - { to: coordination-wave5-next-steps, rel: implements }
review-by: "2026-12-19"
summary: >-
  Build-ready design slice for Area 4 Local-Tier Sweeps & Performance Curves (XS-01, XS-03, ANA-08, ANA-16, RES-04
  over local · in-process VLM + strip and wing Estimator tiers, with zero OpenFOAM/SU2 dependency). Settles the
  DM1-DM18 data model first (Experiment, Case schedule, Sweep attempt, Attempt-state events, runId linkage without
  duplicating strip data, additive top-level experiments member on cfdw-project-2, and explicit Archive/unpin retention
  under Ruling 263's 32 MB cap), the E7 surface list across Core, Analysis, CLI, and Desktop, exact Cm/CD/L/D/depth
  physics derivations, normal-path IO1-IO12 telemetry, red-first test suite with named mutants, 5-node build plan,
  and six operator Decision Requests (DR-SWP-1..6).
---

# Design: Area 4 — local-tier sweeps & performance curves

**Status:** Proposed (Wave 5 Track `SWP`, documents only). Nothing in `src/`, `tests/`, or `DESIGN.md` is changed by
this design. Implementation waits on operator visual approval of [`docs/mockups/local-sweeps.html`](../mockups/local-sweeps.html),
resolution of `DR-SWP-1..6` (§10), and independent Adversary review (Hydrodynamicist, UX & Accessibility, Data &
Persistence, Test Architect).

**Goal state.**
- **Goal:** Let a foil designer define, preview, queue, execute locally (`local · in-process` VLM + strip or wing
  Estimator), cancel/retry, and inspect multi-case sweeps (`CL`, `CD`, `L/D`, `Cm` vs `α` and vs `speed`, linked case
  table, and table twins) with zero external solver installation, zero network, and zero API key.
- **Done when:** All five Wave 5 `SWP` design artifacts are reviewed and approved, `DR-SWP-1..6` are ruled, and the
  five build nodes (§9) pass their red-first tests, mutants, and native UI checks.
- **Not in scope:** Area 4 `Optimize` loops (`XS-02`, `RES-05`, `CAND-01`), OpenFOAM/SU2 backend meshing/solving/harvesting
  (`RUN-01..05` backend, `SPIKE-03/04`), 3D field floods / difference floods / ParaView hand-off (`RES-01..03` field
  layers), AI experiment-config proposals (`AI-09`), and editing `DESIGN.md` (proposed copy rows live in §5.3 only).
- **Tier:** T2 (durable native document schema extension and multi-run scientific results surface).

---

## 0. Evidence ledger and grounding findings

Every code claim below was **Verified** by direct file inspection in this worktree before authoring. Every model or
projection not yet built is labelled **Inferred** or carries an inline `assume:`.

| Fact | Source (exact path:line) | Confidence | Why it constrains this design |
|---|---|---|---|
| Native envelope top-level optional members are strictly checked by `Exact(root, [...], ["analysis"])`; unknown members fail closed with `DOC-UNSUPPORTED-FIELD` → `COPY-130` | `src/CfdWorkbench.Core/AuthoringSession.cs:2631` | **Verified** | An older build refuses a file with a new `experiments` member before writing; rollback via `.v1.bak` holds without a format v3 bump (`DR-SWP-1`). |
| `RunRecord.Format(int runCount)` returns `cfdw-project-1` at 0 runs and `cfdw-project-2` at ≥ 1 run; `NativeProject.Read` enforces agreement with `runs.Length` | `src/CfdWorkbench.Core/RunRecord.cs:225-229`, `AuthoringSession.cs:2640-2643` | **Verified** | A Draft/Queued sweep can exist when `runs.Count == 0`; `Format` must consider `experiments.Count` as well (`G-SWP-1`). |
| `NativeProject.MaxBytes = 32_000_000` bytes (open ≤ 3 s, save ≤ 3 s, working set ≤ 1.5 GB) | `AuthoringSession.cs:2567`, Ruling 263 (`docs/notes/rulings.md:1977`) | **Verified** | Measured lower bound is 109,991 B per 64×2 VLM run (~127 kB at solver precision, `docs/design/a3d-compare.md:289`); 12-case VLM sweep ≈ 1.32–1.52 MB → requires explicit retention/archive policy (`G-SWP-2`). |
| `KeepPlan()` pins Discrepancy-referenced `runId`s before applying `RetainedOthersPerTier = 20` and latest-Failed-per-key | `AuthoringSession.cs:2498-2515` | **Verified** | Sweep pinning follows the exact same `referenced` mechanism on top of quota, plus an append-only `Archived` event to release pins (`DR-SWP-2`). |
| `AnalysisService.EvaluateAsync` enforces latest-wins per `Scope` (`Scope.Wing`) and is idempotent by recomputed `runKey` | `src/CfdWorkbench.Analysis/AnalysisService.cs:50, 111, 156-162, 296-310` | **Verified** | A concurrent interactive Analysis evaluate would cancel an in-flight sweep if they shared one `AnalysisService` instance. `SweepService` owns a dedicated `AnalysisService` and runs cases sequentially (`G-SWP-4`). |
| `AnalysisService.EvaluateAsync` records `RunOutcome.Failed(code, reason)` for `ANA-*` solver/coupling errors (except `ANA-TIP-BELOW-FLOOR`), and throws without recording on `ANA-INPUT-*`, `DSL-NOT-ASSESSED`, `DOC-SIZE` | `AnalysisService.cs:90-108, 186-193, 256-259` | **Verified** | `SweepAttempt` must catch both `RunOutcome.Failed` and `ContractError`, record `AttemptStateEvent(Failed, code, reason)`, and keep `RunId = null` per spec A3.1:255 (`G-SWP-5`). |
| Pitching moment per strip is `my[s] += pz * ffx - px * ffz` about frame origin (`+x aft, +y starboard, +z up`), stored in `StripLoad.My` | `src/CfdWorkbench.Analysis/VortexLattice.cs:261`, `StripCoupler.cs:56`, `RunRecord.cs:97` | **Verified** | Positive `My` is positive nose-up (`e_z × e_x = +e_y`, spec A4.7:549); `Cm = (Σ My) / (q · S_ref · c_ref)` about `"frame origin"` (`ReferenceQuantities.cs:8`) is derived on read with zero schema change (`G-SWP-3`). |
| `WingEstimator.Derive` returns `(Cl, Cdi, E)` with `e = 1`, no strips, no profile drag, no `Cm` | `src/CfdWorkbench.Analysis/WingEstimator.cs:6, 42-65` | **Verified** | Estimator sweeps plot `CL` and `CDi (Estimator, e = 1)`; wing-only total `CD`, `L/D`, and `Cm` read `Unavailable` with explicit reason. |
| `Loads.WingDrag(run, ncrit)` computes induced + profile drag for `Ncrit ∈ {2, 4}`; craft `TotalDrag` always `Unavailable` | `src/CfdWorkbench.Analysis/Loads.cs:47-63`, `AnalysisProjection.cs:519-544` | **Verified** | Sweep `CD` and `L/D` charts plot the `Ncrit 2–4` band (Ruling 109), never averaged, with `CDi (Trefftz)` as a separate explicit series. |
| `OperatingPoint` holds `(Speed, PAtm, HRef, Datum, AlphaDeg, Load)` with `Load` in `RunRecord.Key`; `OperatingPoints.Validate` currently requires `op.Load is null` (`ANA-INPUT-LOAD`) | `RunRecord.cs:70-76, 208`, `OperatingPoint.cs:61` | **Verified** | Goal-state sweep cases solve `α` via `FindAlpha.Run` (`FindAlpha.cs:38-63`); `DR-SWP-4` decides whether `op.Load` stays `null` on `AnalysisRun` (sharing `runKey` with manual Analysis) or carries `requiredLoadN`. |
| `WaterTable.At` enforces 0–50 °C and 0–35.16504 g/kg salinity with BLAKE3 integrity check (`PinnedHash`) | `src/CfdWorkbench.Analysis/WaterTable.cs:11-62` | **Verified** | Schedule preview validates water via `WaterTable.At`, blocking out-of-ITTC temperatures with `COPY-52` before queueing. |
| `Labels.FixedVlm(deepWater)` and `Labels.NotModelled(deepWater)` switch `"deep water"` to `"free surface not modelled"` when depth is unset or shallow | `src/CfdWorkbench.Analysis/Labels.cs:25-28, 304-307` | **Verified** | Sweep headers and charts reuse `Labels.FixedVlm` / `Labels.NotModelled` directly so depth-unset never prints `"deep water"`. |
| `Cli.RunAsync` exposes `inspect --json`, `inspect --runs`, `analyse --op`, `export` | `src/CfdWorkbench.Cli/Program.cs:49-63` | **Verified** | Adding `sweep preview`, `sweep run`, and `inspect --sweeps` extends `Program.cs` cleanly with `CLI-01` run-key parity. |

### Grounding findings (carried to §1, §2, and §10)

- **G-SWP-1 (Format derivation on runless experiments):** Under `AuthoringSession.cs:2641`, `cfdw-project-2` is allowed
  only when `runs.Length > 0`. Saving a Draft or Queued sweep before executing any case requires `Format(runs.Count,
  experiments.Count)` (`DR-SWP-1`).
- **G-SWP-2 (Sweep retention vs 32 MB cap):** Pinning every sweep forever would hit `NativeProject.MaxBytes`
  (32,000,000 bytes, Ruling 263) after ~20 historical 12-case VLM sweeps on discarded branches; not pinning active
  sweeps at all would let `PruneRunsAtSave` punch tombstones into a 12-case sweep after a branch switch. Solution:
  pin selected `Completed` runs of non-archived sweeps + explicit append-only `Archived` event (`DR-SWP-2`).
- **G-SWP-3 (`Cm` derivation & boundaries):** `StripLoad.My` already exists in `cfdw-project-2` rows and obeys
  positive nose-up about `frame origin` (`VortexLattice.cs:261`). `WingCoefficients` (`Compare.cs:21-39`) currently
  derives `(Cl, Cdi, E)` only; adding `Cm` (for `NChord ≥ 2`, `Not resolved · 1 chordwise panel` at `NChord == 1`,
  `Unavailable` on Estimator) reuses stored `My` with zero migration (`DR-SWP-5`).
- **G-SWP-4 (Single-scope latest-wins collision):** Interactive Analysis uses `AnalysisService` with latest-wins on
  `Scope.Wing`. Running sweeps through the same `AnalysisService` instance would let a background sweep cancel an
  interactive evaluate or vice versa. `SweepService` owns a dedicated `AnalysisService` instance over the shared
  `AuthoringSession` and executes cases sequentially (`DR-SWP-3`).
- **G-SWP-5 (Failed attempt `runId` contract):** Spec A3.1:255 states *"An attempt references at most one Analysis run;
  a failed attempt references none"*. Even when `AnalysisService` records a `RunOutcome.Failed` row in `analysis.runs`,
  a failed `SweepAttempt` stores `runId = null` and records `(code, reason)` on its terminal `AttemptStateEvent`.
- **G-SWP-6 (Area 1 `Goal state` seam):** Area 1 (`SET`) is being designed concurrently (`w5-setup-design`). This
  design consumes Goal states strictly through a read-only snapshot contract grounded in spec A3.1:249 (`§1.6`).

---

## 1. Data model (settled first — DM1–DM18)

### 1.1 Bounded context and ubiquitous language

Context **Experiment** (spec A3.1:229), referencing **Design** (`AcceptedId`, `SurfaceHash`), **Analysis**
(`AnalysisRun`, `WaterRecord`, `OperatingPoint`), and optionally **Goal state** by identity.

| Term | Kind | Meaning |
|---|---|---|
| **Sweep experiment** | entity, append-only versions (aggregate root) | An `Experiment` with `kind = "sweep"`. Holds a versioned, immutable-once-Queued definition, its `CaseSchedule`, its `BackendPin` (`local · in-process`), append-only `ExperimentEvent`s, and append-only `SweepAttempt` facts. |
| **Sweep definition** | value object | The authored inputs before resolution: `sourceKind` (`cartesian` \| `goal-state`), `tier` (`vlm-strip` \| `estimator`), `water` (`WaterRecord`), `hRefMeters` (nullable for cartesian), `speedsMetersPerSecond[]` & `anglesDeg[]` (for cartesian) OR `goalStateRef` (for goal-state), `method` (`RunMethod`), `settingsHash`, `designRef` (`AcceptedId`, `SurfaceHash`). |
| **Case schedule** | value object inside Experiment | The resolved ordered list of unique `SweepCase` entries (`caseIndex = 0..N-1`), deterministic from the definition, frozen at Queue (`XS-03`). |
| **Sweep case** | value object | One entry in `CaseSchedule`: `caseIndex`, `caseKey` (deterministic key of its inputs), `speedMetersPerSecond`, `hRefMeters`, `datum`, `alphaDeg` (fixed for cartesian; `null` for goal-state until solved), `targetLoadN` (null for cartesian; required load for goal-state), `label` (optional Goal-state point name, e.g. `P1 Take-off`). |
| **Sweep attempt** | fact, append-only | One execution attempt of one `SweepCase` (`attemptId`, `caseIndex`, `attemptIndex`, `priorAttemptId?`, `runId?`, `solvedAlpha?`, `events[]`). Retries append a new `SweepAttempt` row; prior attempts are never overwritten. |
| **Attempt-state event** | fact, append-only | One state transition on a `SweepAttempt`: `(sequence, state ∈ {Pending, Running, Completed, Failed, Cancelled}, atUtc, code?, reason?)`. The attempt's current state is its latest event. |
| **Experiment event** | fact, append-only | One lifecycle event on a `SweepExperiment`: `(sequence, kind ∈ {Queued, CancelRequested, Archived}, atUtc, reason?)`. |
| **Selected attempt** | derived (never stored) | For each `caseIndex`, the `SweepAttempt` with the highest `attemptIndex`. A failed or cancelled selected attempt renders as a gap (`ANA-08`, `RUN-06`). |
| **Experiment status** | derived (never stored) | Pure function `f(experimentEvents ∪ selectedAttemptStates)` per spec A3.1:254 (§2.3). |
| **Backend pin** | value object | For local tiers: `{ id: "local", substrate: "in-process", tier: "vlm-strip" \| "estimator", methodId, methodVersion, settingsHash }`, copied when `Queued` is recorded (`XS-03`). |

### 1.2 Aggregates, roots and the invariant each protects (DM2)

| Aggregate root | Owns | Invariant | References by identity |
|---|---|---|---|
| **SweepExperiment** | `Definition`, `BackendPin`, `CaseSchedule`, `ExperimentEvents[]`, `Attempts[]`, `DefinitionHash` | (1) Once `Queued` is in `ExperimentEvents`, `Definition`, `CaseSchedule`, `DesignRef`, `GoalStateRef`, and `BackendPin` are immutable (`XS-03`); (2) every `SweepCase` in `CaseSchedule` is unique and valid; (3) every `SweepAttempt` with terminal state `Completed` carries a non-null `runId` naming a `Completed` `AnalysisRun` in the same document (or a `PrunedRun` tombstone only if `Archived`), while every `Failed`/`Cancelled`/`Pending`/`Running` attempt has `runId == null`; (4) `attemptIndex` is contiguous `1..k` per `caseIndex` and `priorAttemptId` chains cleanly; (5) `DefinitionHash == BLAKE3(JCS(definition))`. | `DesignRevision` (`AcceptedId`, `SurfaceHash`), optional `GoalState` (`goalStateId`, `contentHash`), `AnalysisRun` (`runId`, one-way from `SweepAttempt` → `AnalysisRun`, never reverse) |

One aggregate per transaction (DM2):
- Editing geometry in CAD appends a `DesignRevision` and touches **no** `SweepExperiment` (freshness is derived on read).
- Executing a case appends an `AnalysisRun` via `AuthoringSession.RecordRun` (if not already present by `runKey`) and appends the `SweepAttempt` fact to the `SweepExperiment`; each write is atomic under the session lock and checked against `NativeProject.MaxBytes`.

### 1.3 Grain, keys, measures, additivity and history rules (DM3–DM7)

| Fact / entity | Grain — "one row is exactly one …" | Identified by · recorded when | Measures & additivity class |
|---|---|---|---|
| **SweepExperiment** | authored sweep definition version (Draft or Queued) over one accepted `DesignRevision` (and optional `GoalState` version) | `experimentId` (UUID v4); `(seriesId, version)` unique · recorded on `New sweep` / `New version` | `caseCount` (non-additive across versions) |
| **SweepCase** | unique resolved operating condition inside one `SweepExperiment`'s `CaseSchedule` | `(experimentId, caseIndex)` contiguous `0..N-1`; `caseKey` unique within the schedule · frozen at `Queued` | `speed`, `hRef`, `alphaDeg`, `targetLoadN` — all **non-additive** |
| **SweepAttempt** | one execution attempt of one `SweepCase` | `attemptId` (UUID v4); `(experimentId, caseIndex, attemptIndex)` unique · recorded when execution starts / retries | `wallMs` (**additive** across completed attempts of one sweep); `findAlphaSolves` (**additive** count); `solvedAlphaDeg` (**non-additive**) |
| **AttemptStateEvent** | one state transition of one `SweepAttempt` | `(attemptId, sequence)` contiguous `0..m-1` · recorded at each transition | `atUtc` (timestamp; never compared for ordering — `sequence` orders) |
| **ExperimentEvent** | one lifecycle transition of one `SweepExperiment` | `(experimentId, sequence)` contiguous `0..m-1` · recorded at Queue / Cancel / Archive | `atUtc` (timestamp) |

**Derived on read, never stored (DM7):**
- Per-case preview conditions: `q`, `Re_ref`, `h/c`, `Fr_h`, `σ` (computed by existing `OperatingPoints.Derive`).
- Per-case aerodynamic outputs: `CL`, `CDi (Trefftz)`, `CD_wing(Ncrit 2)`, `CD_wing(Ncrit 4)`, `Wing-only CL/CD(Ncrit 2, 4)`, `Cm` (frame origin, `NChord ≥ 2`), `e`, `L`, `D_i`, `D_wing`, `V_crit`, envelope verdict (computed from the referenced `AnalysisRun` via `WingCoefficients`, `Loads`, `SectionTier`, `WingEstimator.Derive`).
- Per-case `selectedAttempt` (`max(attemptIndex)`), per-case `state` (`selectedAttempt.events[^1].state`, or `Pending` if no attempt exists).
- Per-case `freshness` (`Current` | `Historical` | `Unavailable — run payload failed its check` | `Unavailable — pruned at save`), derived by recomputing `RunRecord.RecomputedKey(run)` against current accepted geometry + case inputs (`Freshness.State`).
- Experiment-level `status` (`Draft` | `Queued` | `Running` | `Completed` | `Failed` | `Cancelled`) and summary counts (`completedCount`, `failedCount`, `pendingCount`).
- Experiment-level freshness: `Current` iff every `Completed` case's referenced run is `Current` against the current accepted surface/profiles/method/settings; `Historical` if any `Completed` case's surface/profiles/method/settings differs from current (naming `"what changed"` via `Freshness.WhatChanged` and `AuthoringSession.RevisionOf`, e.g. `r4 → r5 · a twist point moved`).
- Compatible series grouping for charts (equal `SurfaceHash`, `Method`, `SettingsHash`, `WaterRecord`, `hRef` when held, and held variable).

**History rule per attribute:**
- **Type-2 / immutable (new version on change):** every field in `SweepDefinition`, `CaseSchedule`, `DesignRef`, `GoalStateRef`, `BackendPin`. Editing a Queued sweep creates a new `SweepExperiment` version (`version + 1`, `parentExperimentId = prior.Id`); the prior version and its attempts remain intact.
- **Append-only facts:** `ExperimentEvents`, `Attempts`, `AttemptStateEvents`. Never mutated or deleted.
- **Type-1 (installation/view preference, discarded on change, never in document):** display units (`m/s` vs `kn`, `N` vs `lbf`, `ANA-18`), selected chart metric (`CL` | `CD` | `L/D` | `Cm`), selected X-axis (`α` | `speed`), held-variable slice selector, table-twin open/closed, selected case index.

### 1.4 Durable JSON representation (`src/CfdWorkbench.Core/SweepRecord.cs`)

Following `DiscrepancyRecord.cs` (Ruling 250 Q4), all durable sweep records, canonical hashes, and structural store
checks live in one new Core file `src/CfdWorkbench.Core/SweepRecord.cs` (Core has zero dependency on Analysis).

```jsonc
// Envelope.experiments[i] — optional top-level member on cfdw-project-2, omitted when empty (WhenWritingNull)
{
  "id": "uuid-v4",
  "seriesId": "uuid-v4",
  "version": 1,
  "parentExperimentId": null,
  "kind": "sweep",
  "name": "Race-light baseline 3×4",
  "definitionHash": "blake3-hex64 over JCS(definition)",
  "definition": {
    "sourceKind": "cartesian",          // "cartesian" | "goal-state"
    "tier": "vlm-strip",                // "vlm-strip" | "estimator"
    "designRef": {
      "acceptedId": "uuid-v4",
      "surfaceHash": "blake3-hex64"
    },
    "goalStateRef": null,               // or { "goalStateId": "uuid-v4", "contentHash": "blake3-hex64" }
    "water": { /* Core WaterRecord */ },
    "hRefMeters": 0.5,                  // null when depth unset
    "datum": "root LE",
    "pAtm": 101325.0,
    "speedsMetersPerSecond": [5.144444444444445, 7.202222222222222, 9.26],
    "anglesDeg": [0.0, 2.0, 4.0, 6.0],
    "method": { "id": "cfdw.vlm-strip", "version": "1.1.0", "order": 1 },
    "settingsHash": "blake3-hex64"
  },
  "backendPin": {                       // null while Draft; copied at Queue (XS-03)
    "id": "local",
    "substrate": "in-process",
    "tier": "vlm-strip",
    "methodId": "cfdw.vlm-strip",
    "methodVersion": "1.1.0",
    "settingsHash": "blake3-hex64"
  },
  "cases": [
    {
      "caseIndex": 0,
      "caseKey": "blake3-hex64 over JCS(case inputs)",
      "label": null,
      "speedMetersPerSecond": 5.144444444444445,
      "hRefMeters": 0.5,
      "datum": "root LE",
      "pAtm": 101325.0,
      "alphaDeg": 0.0,                  // null for goal-state cases
      "targetLoadN": null               // non-null for goal-state cases
    }
  ],
  "events": [
    { "sequence": 0, "kind": "queued", "atUtc": "2026-11-19T23:05:00Z", "reason": null }
  ],
  "attempts": [
    {
      "attemptId": "uuid-v4",
      "caseIndex": 0,
      "attemptIndex": 1,
      "priorAttemptId": null,
      "runId": "uuid-v4",               // intact Completed AnalysisRun.RunId; null on Failed/Cancelled
      "solvedAlpha": null,              // non-null on goal-state Completed: { alphaDeg, lowerDeg, upperDeg, iterations, solves, basis }
      "wallMs": 42.6,
      "events": [
        { "sequence": 0, "state": "pending",   "atUtc": "2026-11-19T23:05:00Z", "code": null, "reason": null },
        { "sequence": 1, "state": "running",   "atUtc": "2026-11-19T23:05:01Z", "code": null, "reason": null },
        { "sequence": 2, "state": "completed", "atUtc": "2026-11-19T23:05:01Z", "code": null, "reason": null }
      ]
    }
  ]
}
```

**Structural store invariants (`SweepRecords.Check`, called in `NativeProject.Check` on read and on every session write under the session lock):**
1. `id`, `seriesId`, `parentExperimentId` (if non-null), `designRef.acceptedId`, `attemptId`, `priorAttemptId` (if non-null), and `runId` (if non-null) are lowercase UUID v4 (`NativeProject.Uuid`).
2. `id` unique (`DOC-SWEEP-ID`); `(seriesId, version)` unique with `version >= 1` (`DOC-SWEEP-VERSION`); `parentExperimentId` names an earlier version in the same `seriesId`.
3. `kind == "sweep"`; `sourceKind ∈ {"cartesian", "goal-state"}`; `tier ∈ {"vlm-strip", "estimator"}` (`DOC-SCHEMA`).
4. `designRef.acceptedId` exists in `accepted` and its design's `SurfaceHash == designRef.surfaceHash` (`DOC-REFERENCE`, `DOC-INTEGRITY`).
5. `definitionHash` is hex64 and matches recomputed `BLAKE3(JCS(definition))` on write; on read, a tampered hash does not silently disappear — it marks the experiment `Unavailable — sweep payload failed its check` (same fail-closed integrity pattern as `RunIntegrity.PayloadFailedCheck`).
6. `cases` non-empty, `cases.Count <= MaxCasesPerSweep` (120, `DR-SWP-3`), `caseIndex` contiguous `0..N-1`, `caseKey` unique within `cases`, all doubles finite, `speedMetersPerSecond > 0`, `pAtm > 0`, `hRefMeters is null or > 0`, `targetLoadN is null or > 0` (`DOC-SCHEMA`).
7. If `events` contains `queued`: `backendPin` is non-null and matches `(tier, method.id, method.version, settingsHash)`; if `events` does NOT contain `queued`: `attempts` MUST be empty (`XS-03` enforced structurally in the store!).
8. For every `SweepAttempt`: `caseIndex` in `0..cases.Count-1`; `attemptIndex` contiguous `1..k` per `caseIndex`; `priorAttemptId` null iff `attemptIndex == 1`, else equals `attemptIndex - 1`'s `attemptId`; `events` non-empty with `sequence` contiguous `0..m-1`.
9. Reference rule on `runId`:
   - If terminal state is `completed`: `runId` is non-null and MUST name either (a) a `Completed` `AnalysisRun` in `analysis.runs` whose `Inputs.SurfaceHash`, `Water`, `Method`, `SettingsHash`, `Op.Speed`, `Op.HRef`, `Op.AlphaDeg` (or `solvedAlpha.AlphaDeg`) match the case, OR (b) a `PrunedRun` tombstone in `analysis.pruned` **if and only if** `events` contains `archived` (`DOC-REFERENCE`).
   - If terminal state is `failed`, `cancelled`, `running`, or `pending`: `runId` MUST be `null` (`DOC-SCHEMA`, A3.1:255).

### 1.5 `cfdw-project-2` additive schema, rollback, and retention under Ruling 263's 32 MB cap

- **Additive schema (`DR-SWP-1`):** `Envelope` gains one optional trailing property `Experiments`
  (`IReadOnlyList<SweepExperiment>?`, `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`), omitted when
  empty. `NativeProject.Read` (`AuthoringSession.cs:2631, 2641, 2643`) widens to accept optional `"experiments"` — and
  because Area 1 (`SET`, `docs/design/setup-seeding.md`) also adds optional `"setup"` on `Envelope`, when both tracks
  land the optional list is `["analysis", "setup", "experiments"]` and `RunRecord.Format` returns `"cfdw-project-1"`
  iff `runs`, `setup`, and `experiments` are all absent/empty, else `"cfdw-project-2"`.
- **Zero drift on existing documents:** Any document with `experiments == null` (empty) produces byte-identical JSON
  for both `cfdw-project-1` and `cfdw-project-2` (`Sweep_OmittedWhenEmpty_ExistingDocumentBytesUnchanged`).
- **Expand–migrate–contract:** Expand: reader accepts `experiments` or its absence on `cfdw-project-2` (refused on
  `cfdw-project-1`). Migrate: none (no backfill, DM16). Contract: none; `-1` stays readable and writable indefinitely.
- **Rollback:** An older build opening a `-2` file with `experiments` hits `Exact(root, ..., ["analysis"])` at
  `AuthoringSession.cs:2631` and fails closed with `DOC-UNSUPPORTED-FIELD` → `COPY-130` before any write. The first
  save that transitions a `-1` file to `-2` (whether due to runs or experiments) writes `<name>.v1.bak` first via
  `ProjectStore` (ADR-0011 §6).
- **Retention & 32 MB budget (`DR-SWP-2`, Ruling 263):**

| Item | Bytes | Basis |
|---|---|---|
| One `SweepExperiment` (12 cases, 12 attempts, 36 events) | ≈ 7,500 B | **Inferred** from §1.4 indented JSON (≈ 625 B per case+attempt) |
| One `VLM + strip` run (64 × 2 = 128 strips) | 109,991 B (measured lower bound) · ≈ 127,000 B (solver precision) | **Verified** lower bound from `docs/design/a3d-compare.md:289`; solver precision **Inferred** |
| One `Estimator` run (0 strips) | ≈ 1,500 B | **Inferred** from `docs/design/a3d-compare.md:288` |
| One 12-case `VLM + strip` sweep (1 experiment + 12 runs) | ≈ 1.33 MB (lower bound) · ≈ 1.53 MB (solver precision) | Computed sum; ≈ 20 disjoint sweeps fit inside `NativeProject.MaxBytes = 32,000,000` bytes |
| One 12-case `Estimator` sweep (1 experiment + 12 runs) | ≈ 25.5 kB | Computed sum; > 1,000 sweeps fit inside 32 MB |

**Retention rule (`AuthoringSession.KeepPlan()` extension):**
1. Keep every run on a reachable surface (`ReachableSurfaces()` — unchanged).
2. Keep every run referenced by any `DiscrepancyRecord` (`BaseRunId`, `ComparedRunId` — unchanged).
3. **New:** Keep every run referenced by the **selected (latest)** `Completed` `SweepAttempt` of each case of every
   **non-archived** `SweepExperiment` (`!exp.Events.Any(e => e.Kind == SweepEventKind.Archived)`). Counted in
   `pinned` (reported alongside Discrepancy pins in `analysis.save`), on top of `RetainedOthersPerTier = 20`.
   Superseded retry attempts are **not** pinned.
4. Apply `RetainedOthersPerTier = 20` and latest-`Failed`-per-key (unchanged).
5. **Explicit unpin path (`Archive sweep`):** Appending `ExperimentEvent(Archived)` preserves the experiment definition,
   case schedule, and attempt history while removing its `runId`s from step 3. At the next save, `PruneRunsAtSave`
   reclaims any unreachable VLM strip rows not pinned by a Discrepancy record or another active sweep, leaving
   `PrunedRun` tombstones (`"Unavailable — pruned at save"` on that archived sweep's curves — never zero, never
   silently missing).
6. **Budget ceiling behaviour:** If recording a run during local execution would exceed `NativeProject.MaxBytes`
   (32,000,000 bytes), `AuthoringSession.RecordRun` throws `DOC-SIZE` before mutating state; `SweepService` marks
   that attempt `Failed (DOC-SIZE)` and halts launching remaining cases so the user can archive an older Historical
   sweep or switch to the Estimator tier (`RUN-04`).

### 1.6 Seam contract with Area 1 Setup (`w5-setup-design`, aligned verbatim via board #9/#11)

`XS-01` allows defining a sweep from either (a) Cartesian `angles × speeds` or (b) the `Goal state`'s ordered
operating-point list. Aligned directly with `docs/design/setup-seeding.md` (`w5-setup-design`, board `#11`), Area 1
stores immutable `GoalStateVersion` rows append-only under `Envelope.setup.goalStates[]`, and Area 4 consumes one
read-only Core snapshot shape referencing `(goalStateId, contentHash)`:

```csharp
// Read-only seam aligned verbatim with Area 1 (SET, board #11); authored by Area 1 when built.
public sealed record GoalStateSweepSnapshot(
    string GoalStateId,
    string ContentHash,                 // BLAKE3(JCS(row without contentHash))
    WaterRecord Water,                  // Core RunRecord.cs:67
    IReadOnlyList<GoalOperatingPointSnapshot> OperatingPoints,
    GoalDepthBand? DepthBand,           // { minMeters, maxMeters, provenance }
    GoalSpeedBand? SpeedBand);          // { takeoffMinMps, takeoffMaxMps, cruiseMinMps, cruiseMaxMps, topMaxMps, provenance, sourceLabel, disconfirmingNote? }

public sealed record GoalOperatingPointSnapshot(
    string Id,
    string Label,
    double SpeedMetersPerSecond,
    double? HRefMeters,                 // Setup defaults to depthBand.maxMeters
    string Datum,                       // default "root LE" (OperatingPoints.RootLeadingEdge)
    double PAtmPa,                      // default 101325.0
    double RequiredLoadN,
    string LoadSource,
    string CriticalMetric,              // takeoff-cl | upwind-ld | cruise-ld | downwind-ld | cavitation-sigma | top-speed-sigma
    IReadOnlyList<int> Ncrit,           // [2, 4]
    string SpeedLabel);
```

- When a project has **no** Goal state (every project today before Area 1 ships), the `"Goal state operating points"`
  source option in Experiment setup is disabled with `"No Goal state in this project yet — define one in Setup, or use Angles × speeds"`, and `ANA-16` charts omit Goal-state bands cleanly with a note `"Goal-state bands: no Goal state in project"`.
- **Coordinated `OperatingPoint.Load` decision (`DR-SWP-4` / `DR-SET-2`):** `SET` confirms `RunRecord.cs:72-75`'s
  `assume:` that `OperatingPoint.Load` represents the Goal-state front-wing required load in newtons (`requiredLoadN`),
  remaining `null` on all custom fixed-`α` runs (preserving all A3a/A3d run keys). `DR-SWP-4` and `DR-SET-2` let the
  operator rule together whether Goal-state load-solved runs keep `op.Load = null` (sharing `runKey` with fixed-`α`
  Analysis runs) or set `op.Load = requiredLoadN` (distinct `runKey`, relaxing `ANA-INPUT-LOAD` in `OperatingPoints.Validate`).

---

## 2. Execution semantics, physics derivations and performance curves

### 2.1 Schedule construction, validation and preview invalidation (`XS-01`)

1. **Cartesian `angles × speeds`:**
   - Inputs: speed list (`m/s` or `kn`, parsed to SI `m/s` via `Labels.KnotsPerMeterSecond = 1.9438444924406`), angle
     list (`°`), water (fresh/salt + temperature °C via `WaterTable.At`), depth `h_ref` (`m`, or `"Not set"`), tier
     (`VLM + strip · local calculation` or `Estimator · local calculation`).
   - Validation (pure function `SweepSchedule.BuildCartesian`, runs live on every input change, fail-closed):
     - Empty speeds or empty angles → `ANA-SWEEP-EMPTY` (names `"Speeds"` or `"Angles"`).
     - Non-number or invalid unit token → `ANA-SWEEP-UNIT` (names field and token).
     - Non-finite value → `ANA-SWEEP-NONFINITE` (names field and value).
     - Speed ≤ 0 → `ANA-INPUT-SPEED` (names offending value, e.g. `−5 kn`).
     - Duplicate speed (exact or `|v1 - v2| <= 1e-9 m/s`) or duplicate angle (`|a1 - a2| <= 1e-9°`) →
       `ANA-SWEEP-DUPLICATE` (names field and duplicate value).
     - Depth set and ≤ 0 or non-finite → `ANA-INPUT-DEPTH`.
     - Water outside ITTC 0–50 °C or salinity outside `0..35.16504 g/kg` → `ANA-INPUT-WATER` (`COPY-52`).
     - `speeds.Count * angles.Count > MaxCasesPerSweep` (120) → `ANA-SWEEP-CASE-CAP`.
   - Ordering: speeds ascending, then angles ascending within each speed (`N = |speeds| × |angles|`).
   - Derived preview row per case (via `OperatingPoints.Derive(op, water, cRef)`): `q` (Pa), `Re_ref`, `h/c`, `Fr_h`,
     `σ`. When `h_ref` is unset, `h/c`, `Fr_h`, `σ` read `"Unavailable — depth not set"` (`COPY-45`).
   - Resource estimate row before Queue:
     - `Panels / run`: `2 * NSpanPerHalf * NChord` (e.g. `512` at 64 × 4 VLM+strip; `0 (closed form)` for Estimator).
     - `Estimated wall time`: `"Not recorded"` until at least one local run in the current session has a measured
       `wallMs` for that tier (`IO1`/`IO8` — never invent a time!); once measured, `"≈ <k × medianWallMs> (from <m> local runs, Inferred)"`.
     - `Estimated storage`: `≈ <N × 127 kB>` (VLM+strip, solver-precision upper estimate, labelled Inferred) or
       `≈ <N × 1.5 kB>` (Estimator) beside `"Document headroom: <used> / 32.0 MB"` (measured from current `Envelope`).
     - `Cache hits`: count of cases whose `runKey` already has an intact `Completed` `AnalysisRun` in the document
       (`"k of N already computed — reused by run key"`).
2. **Goal-state operating points (`XS-01`):**
   - Copies the referenced `GoalState` version's `WaterRecord` and ordered `OperatingPoints[]`.
   - Preview lists each point's label (`P1 Take-off`, `P2 Cruise`, …), `speed`, `h_ref`, `requiredLoadN`, derived
     `q`, `Re_ref`, `h/c`, `Fr_h`, `σ`, and `α` reading `"Solved at run (Find α, 1 % tolerance)"`.
3. **Preview invalidation (`XS-01`):**
   - A **Draft** preview binds to `(currentAcceptedId, currentSurfaceHash, water, hRef, settingsHash)`. If geometry,
     water, depth, or method changes after preview, the Draft preview is immediately marked stale
     (`"Preview out of date — geometry changed (r4 → r5). Refresh preview before queueing."`) and **Queue** is disabled
     until refreshed (one click, or automatic refresh on returning to Experiment setup).
   - A **Queued** `SweepExperiment` version is **never** invalidated or mutated by later edits: its `designRef` stays
     pinned to the revision it was queued on (`r4`), and its completed runs read `Historical — surface (r4 → r5)` when
     viewed after `r5` is accepted.

### 2.2 Experiment setup vs Run separation (`XS-03`)

- **Experiment setup** exposes ONLY: `New sweep`, `New version` (clones a Queued sweep's inputs into a new Draft
  `version + 1`), `Queue`, and the read-only jump chip `→ Run`. It **never** renders a `Run` button (`UX-17`, `XS-03`).
- Pressing **Queue**:
  1. Re-validates the Draft schedule and accepted geometry certification (`DSL-NOT-ASSESSED` / `ANA-TIP-BELOW-FLOOR`
     refuse queueing with the reason named).
  2. Copies `BackendPin = local · in-process` (tier, method id + version, settings hash).
  3. Persists the `SweepExperiment` with `ExperimentEvent(0, Queued, now)` via `AuthoringSession.RecordExperiment`.
  4. Switches the header badge to `Queued v<n>` and activates the jump chip `Open in Run →`.

### 2.3 Local in-process runner (`SweepService`)

`SweepService` executes a `Queued` (or partially completed / cancelled) `SweepExperiment` whose `BackendPin.Id ==
"local"` and `BackendPin.Substrate == "in-process"` (if a pin names an external backend such as OpenFOAM/SU2, `SweepService`
refuses with `Not ready — no backend row matches the pin ...` per `XS-03`).

- **Dedicated service instance (`G-SWP-4`):** `SweepService` instantiates its own `AnalysisService(session, method,
  barrier, time)` so interactive single-point Analysis never supersedes an active sweep.
- **Sequential case loop (`k = 0..N-1`):**
  1. Skip cases whose selected attempt is already `Completed` (supports resume after Cancel or Retry of failed cases).
  2. Check `cancellation.IsCancellationRequested` before launching case `k`.
  3. Create `SweepAttempt(attemptId, caseIndex: k, attemptIndex: next, priorAttemptId, runId: null)` with
     `AttemptStateEvent(0, Pending)` → `AttemptStateEvent(1, Running)` and notify UI progress (`Running <done+1> of <N>`).
  4. **Goal-state cases only:** Resolve `α` via `FindAlpha.Run(FindAlpha.ClAt(method, source, op, water, token),
     targetCl, lowerDeg: -4, upperDeg: 14, depthOverChord, cancellation: token)` where `targetCl = targetLoadN / (q * S_ref)`.
     - If `FindAlpha` returns no root, append `AttemptStateEvent(2, Failed, code, reason)` with `runId = null` and
       proceed to case `k + 1` (never extrapolating, `ANA-05`).
  5. Evaluate the case via `analysisService.EvaluateAsync(resolvedOp, water, tier, new Scope.Wing(), token)` (using
     `atRun`/accepted-revision source of `experiment.Definition.DesignRef.AcceptedId` — if the document has moved to a
     later accepted revision since Queue, `SweepService` evaluates on the experiment's pinned `DesignRef.AcceptedId`
     via `session.AcceptedSourceOf(...)`, never silently switching to a newer revision!).
     - *Note on as-built code:* `AnalysisService.EvaluateAsync` (`AnalysisService.cs:78, 240`) currently supports
       `atRun` for `Tier.Estimator` only; `SWP-2` generalizes revision-pinned evaluation (`acceptedId` override read
       from `session.AcceptedSourceOf(acceptedId)`) for `Tier.VlmStrip` as well, so a Queued sweep always evaluates its
       own immutable `DesignRef` (`XS-03`).
  6. Record outcome:
     - `run.Outcome is RunOutcome.Completed`: finalize attempt with `runId = run.RunId` and
       `AttemptStateEvent(2, Completed)`.
     - `run.Outcome is RunOutcome.Failed failed`: finalize attempt with `runId = null` and
       `AttemptStateEvent(2, Failed, failed.Code, failed.Reason)`.
     - `ContractError error`: finalize attempt with `runId = null` and `AttemptStateEvent(2, Failed, error.Code, error.Reason)`.
       If `error.Code == "DOC-SIZE"`, halt launching further cases (leaving them `Pending`) and surface the size-cap
       recovery banner (`RUN-04`).
     - `OperationCanceledException`: finalize active attempt with `runId = null` and
       `AttemptStateEvent(2, Cancelled, "ANA-CANCELLED", "Cancelled by user")`, append
       `ExperimentEvent(CancelRequested)`, and exit cleanly within ≤ 250 ms (`A8.1`), preserving every completed case.
- **Retry sample (`RUN-06`):** Invocable per failed (or cancelled) case in Run. Executes steps 3–6 for that single
  `caseIndex` only, appending `attemptIndex = prev + 1` with `priorAttemptId = prev.AttemptId`. Prior failed attempts
  remain intact in the attempt ledger.
- **Derived Experiment status (`SweepProjection.Status`, verbatim A3.1:254):**
  - No `Queued` event → `Draft`
  - Any selected attempt is `Running` → `Running` (`Running <k> of <m>`)
  - `CancelRequested` event exists (after latest launch) and no attempt is `Running` → `Cancelled`
  - All selected attempts are `Pending` → `Queued` (`Queued v<n>`)
  - Every case's selected attempt is terminal (`Completed` | `Failed` | `Cancelled`) and ≥ 1 is `Completed` →
    `Completed` (`Completed (<k> of <m>)`)
  - Every case's selected attempt is terminal and 0 are `Completed` → `Failed` (`Failed (0 of <m>)`)

### 2.4 Performance curves and physics derivations (`ANA-16`, `RES-04`, `ANA-08`, `ANA-19`)

Every plotted value is **derived on read** from the `AnalysisRun` referenced by each case's selected `Completed`
attempt (DM7). Nothing is stored on the sweep or interpolated across gaps.

| Curve / readout | VLM + strip derivation | Estimator derivation | Unavailable / Undefined / Gap rule |
|---|---|---|---|
| **CL** (dimensionless) | `WingCoefficients.Of(run).Cl` = `L / (q · S_ref)` | `WingEstimator.Derive(run, source).Cl` | `Undefined — speed ≤ 0` (`COPY-266`) or `Unavailable — reference area is zero or missing` (`COPY-380`). Failed/pruned/tampered case → **gap**. |
| **CD (Wing only, Ncrit 2–4)** & **CDi (Trefftz)** | **Wing-only CD band:** `Loads.WingDrag(run, ncrit) / (q · S_ref)` for `ncrit ∈ {2, 4}` (plotted as a shaded/bracketed band with both endpoints in the table twin, never averaged; Ruling 109). **CDi (Trefftz):** `WingCoefficients.Of(run).Cdi` plotted as a distinct dashed series. Craft `Total drag` stays `Unavailable` (`COPY-331`/`Loads.TotalDragReason`). | **CDi (Estimator):** `WingEstimator.Derive(run, source).Cdi` (`CL² / (π AR)`, elliptic `e = 1`). Wing-only total `CD` reads `Unavailable — estimator computes induced drag only (no viscous profile drag)`. | If any strip lacks polar `cd`, wing-only `CD` band reads `Unavailable — cd missing at <k> strips; the estimator bound is not substituted` (`COPY-329`), while `CDi (Trefftz)` remains plotted. Failed case → **gap**. |
| **Wing-only L/D (Ncrit 2–4)** | `L / Loads.WingDrag(run, ncrit)` for `ncrit ∈ {2, 4}` (band, matching `AnalysisProjection.WingRatioRow:535-544`), labelled `"Wing only: lift over wing drag. Not a craft CL/CD."` (`COPY-336`). | `Unavailable — estimator computes induced drag only (no viscous profile drag)` | `Undefined — CD ≤ 0` (`COPY-230`); `Unavailable — wing drag is missing or zero, so CL/CD can't be formed.` (`COPY-335`). Failed case → **gap**. |
| **Cm** (dimensionless) | `(Σ StripLoad.My) / (q · S_ref · c_ref)` about `run.Reference.MomentDatum` (`"frame origin"`, root LE, positive nose-up, verified at `VortexLattice.cs:261`, `DR-SWP-5`). | `Unavailable — estimator does not compute pitching moment` | When `run.Settings.NChord == 1`: `Not resolved · 1 chordwise panel` (`COPY-SF18`, Ruling 161). Failed case → **gap**. |
| **Depth & cavitation** (`h/c`, `Fr_h`, `σ`, `V_crit`) | `OperatingPoints.Derive` + `SectionTier.Derive` governing station (`Smallest σ_i / (−Cp_min,i)`, Ruling 92). | `OperatingPoints.Derive`; `V_crit` from `SectionEstimator` panel method when depth set, else Unavailable. | When `h_ref == null`: `h/c`, `Fr_h`, `σ`, `V_crit` all read `Unavailable — depth not set` (`COPY-45`); fixed basis reads `"attached flow; no stall; no ventilation; free surface not modelled"` (`COPY-224`) — **never** `"deep water"`. |
| **Goal-state bands** (`ANA-16`) | When a `GoalState` exists: **Take-off CL band** (`W_req / (q · S_ref)` over the Goal state's take-off speed) and **Cavitation ceiling** (`V_crit` vertical threshold or operating `σ` line with the 15 % practitioner margin label `COPY-300`), only when depth is set. | Same, omitting `V_crit` band if unavailable. | Depth unset → cavitation ceiling omitted with `"Cavitation ceiling unavailable — depth not set"`. No Goal state → bands omitted cleanly. |

**Mandatory chart & gap invariants (`ANA-08`, `RES-04`, `UI-17`, `C2`):**
1. **No interpolation across gaps:** Whenever case `k` is `Failed`, `Cancelled`, `Pending`, `Pruned`, or `PayloadFailedCheck`, the SVG/canvas polyline **terminates** at `k − 1` and **restarts** at `k + 1` (explicit gap marker `×` on the x-axis with an accessible name `"Case <id>: Failed — <reason>"`). No dashed bridge or straight line connects across a missing sample.
2. **Two axes, explicit held variable:** User switches between **vs α (°)** (one series per held speed, e.g. `10 kn`, `14 kn`, `18 kn`) and **vs Speed (kn or m/s)** (one series per held α, e.g. `0°`, `2°`, `4°`, `6°`). Every series carries a **distinct stroke pattern + marker shape + direct end label** (never colour alone, WCAG 1.4.1), using DESIGN.md data/batlow stops only.
3. **Linked selection (`ANA-08`, `UX-10`):** Clicking or arrow-keying a case in the table, small-multiples strip, or chart point updates the single `SelectedCaseIndex` across all three views and the right-hand case inspector simultaneously. Selecting a failed case clears numeric readouts before showing its error reason and `Retry sample` jump to Run.
4. **Table twin in one action (`UI-17`, `C2`):** Every chart carries a `Show table` toggle that renders the exact plotted numbers (including both `Ncrit 2` and `Ncrit 4` columns, `CDi`, `Cm`, units, envelope verdict, freshness, and failed-case rows with code/reason).

---

## 3. End-to-end change-surface list (E7)

Written before any implementation; every promised visible behaviour traces to a single producer:

| Layer | File(s) (new or edited) | Exact responsibility |
|---|---|---|
| **Store / Core** | `src/CfdWorkbench.Core/SweepRecord.cs` *(new)* | `SweepExperiment`, `SweepDefinition`, `SweepCase`, `SweepAttempt`, `AttemptStateEvent`, `ExperimentEvent`, `BackendPin`, `SolvedAlphaRecord`, `SweepEvent`, `SweepRecords.Check`, `SweepRecords.DefinitionHash`, `SweepRecords.CaseKey`. |
| **Store / Core** | `src/CfdWorkbench.Core/RunRecord.cs` | Extend `RunRecord.Format(int runCount, int experimentCount = 0)` so `-2` is derived when runs OR experiments exist (default `0` preserves all existing callers). |
| **Store / Core** | `src/CfdWorkbench.Core/AuthoringSession.cs` | Add optional `Envelope.Experiments`, widen `NativeProject.Read` `Exact` optional list to `["analysis", "experiments"]`, call `SweepRecords.Check` in `NativeProject.Check`, add `RecordExperiment`, `AppendSweepEvent`, `RecordSweepAttempt`, `ReadExperiments`, `RecordSweepEvent` (telemetry), and extend `KeepPlan()` with non-archived sweep pinning. |
| **Store / Persistence** | `src/CfdWorkbench.Persistence/ProjectStore.cs` | No structural change needed: `.v1.bak` already triggers on `FormatOf(existing) == "cfdw-project-1"` → `FormatOf(next) == "cfdw-project-2"` (`ADR-0011 §6`). Verify with a runless-sweep `.v1.bak` test. |
| **Analysis / Model** | `src/CfdWorkbench.Analysis/Compare.cs` (`WingCoefficients`) | Lift `Cm` (frame origin, `NChord ≥ 2`) onto a shared `WingDerivedCoefficients` helper beside `Cl`, `Cdi`, `E` (one definition, DM7). |
| **Analysis / Schedule** | `src/CfdWorkbench.Analysis/SweepSchedule.cs` *(new)* | Pure validation & deterministic resolution of Cartesian `angles × speeds` and `GoalStateSweepSnapshot` schedules, derived preview table (`OperatingPoints.Derive`), storage/wall estimate, and cache-hit count. |
| **Analysis / Service** | `src/CfdWorkbench.Analysis/SweepService.cs` *(new)* | Dedicated sequential local runner over `AnalysisService`, revision-pinned evaluation (`AcceptedId`), Goal-state `FindAlpha` per point, Cancel ≤ 250 ms preserving completed cases, `RetrySampleAsync`, `ArchiveAsync`, `sweep.*` telemetry emission. |
| **Analysis / Service** | `src/CfdWorkbench.Analysis/AnalysisService.cs` | Allow internal revision-pinned `acceptedId` override on `Tier.VlmStrip` (mirroring `Tier.Estimator`'s `RevisionView` at `:230-233`) so a Queued sweep evaluates its pinned `DesignRef`. |
| **Analysis / Projection** | `src/CfdWorkbench.Analysis/SweepProjection.cs` *(new)* | Pure derivation of experiment status, per-case freshness, performance curve series with explicit gap segments, Ncrit 2–4 bands, table twin rows, small-multiples summaries, and `LAB-01`-clean labels. |
| **Analysis / Copy** | `src/CfdWorkbench.Analysis/Labels.cs` | Append approved `SWP-COPY-*` constants (only after operator copy approval; mirrors `DESIGN.md` §7 rows). |
| **CLI** | `src/CfdWorkbench.Cli/SweepVerb.cs` *(new)* & `src/CfdWorkbench.Cli/Program.cs` | `cfd-workbench sweep preview`, `cfd-workbench sweep run`, and `cfd-workbench inspect <file.cfdw.json> --sweeps` sharing `SweepSchedule`, `SweepService`, and `RunRecord.RecomputedKey` (`CLI-01`). |
| **Desktop UI** | `src/CfdWorkbench.Desktop/Sweeps/` *(new)*: `ExperimentSetupView.axaml(.cs)`, `LocalRunView.axaml(.cs)`, `SweepResultsView.axaml(.cs)`, `PerformanceCurveChart.cs`, `SweepController.cs` | Three discrete sub-surfaces honoring `XS-03` (no Run button in Experiment setup), linked selection (`ANA-08`), gap-preserving curves (`RES-04`), keyboard navigation (`UI-13`/`UI-17`), and DESIGN.md tokens/themes. |
| **Desktop Shell** | `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/StatusStrip.axaml(.cs)`, `Shell/CommandTable.cs` | Expose `Sweeps` mode (Experiment · Run · Results) per `DR-SWP-6`, status-strip sweep progress item, and Cancel reachability (`UX-21`). |
| **Compute readers** | `AnalysisProjection`, `CompareProjection`, `CLI inspect --runs` | Unchanged for existing single runs; sweep runs are real `AnalysisRun` rows so any completed sweep case can immediately be opened in Analysis or compared in Compare with zero duplication! |

---

## 4. CLI contract (`CLI-01`)

All three commands execute in-process through `Cli.RunAsync` using the exact same `SweepSchedule`, `SweepService`,
`OperatingPoints`, `WaterTable`, and `RunRecord` functions as the GUI:

1. **`cfd-workbench sweep preview <example|path.foil|path.cfdw.json> --grid <json>`**
   - `--grid` accepts a closed-schema JSON object: `{ "speedsKn" | "speedsMs": [...], "anglesDeg": [...], "hRef": <double|null>, "water": { "temperatureC": 15, "salinityGPerKg": 35.16504 }, "tier": "vlm-strip" | "estimator" }`.
   - Prints `{ schemaVersion: 1, caseCount, cases: [{ caseIndex, speedMs, alphaDeg, hRef, q, reRef, hOverC, froudeDepth, sigma, cacheHit }], estimatedStorageBytes, documentHeadroomBytes }`.
   - Refuses invalid/duplicate/non-finite/out-of-ITTC inputs with stable `ANA-SWEEP-*` / `ANA-INPUT-*` code (exit 2), writing nothing.
2. **`cfd-workbench sweep run <example|path.foil|path.cfdw.json> --grid <json> [--out <path.cfdw.json>]`**
   - Queues and executes the sweep locally (`local · in-process`), prints per-case `{ caseIndex, state, runId, runKey, cl, cdi, cdWingNcrit2, cdWingNcrit4, ldWingNcrit2, ldWingNcrit4, cm, failureCode, failureReason }` and derived experiment status.
   - `runKey` agrees bitwise with the GUI (`CLI-01`); hydrodynamic scalars agree within `10⁻¹²` relative (`A8.4`).
   - When `--out` is omitted, no file is written. Exit 0 on `Completed` (even with partial failed cases, per `RUN-05`), exit 2 on `Failed` (0 completed) or input refusal, exit 130 on `Cancelled` (`Ctrl+C`).
3. **`cfd-workbench inspect <path.cfdw.json> --sweeps`**
   - Lists every stored `SweepExperiment` in document order with its recomputed `definitionHash` integrity, derived
     status, archived flag, and per-case selected attempt + recomputed `runKey` + freshness (`Current` / `Historical` /
     `Pruned` / `PayloadFailedCheck`).

---

## 5. UI design, hard states and proposed copy (`DX1–DX25`, `LAB-01`)

### 5.1 Archetype and layout

- **Experiment setup:** Configurator + data-bounded preview table (`DESIGN.md` §4 `Case preview`). Fixed window frame
  with internal scrolling; never scrolls as a web page (`UI-23`, defect class `UI-H2`).
- **Run (`local · in-process`):** Process console (`Layout:StatusConsole`, `Motion:None`). Case-level status sequence
  (`Pending → Running → Completed | Failed | Cancelled`), live progress bar + text, `Cancel` reachable in console and
  status strip (`UX-21`), per-case `Retry sample` one click away (`UX-09`, `RUN-06`).
- **Results (local sweep):** G2 `SciVizPipeline` (`DESIGN.md` §4 `Scientific plot` + `Replay timeline` stepped mode).
  Dominant chart region (`CL` · `CD (Wing only)` · `L/D (Wing only)` · `Cm`, vs `α` or vs `Speed`) with one-click
  `Show table` twin, horizontal small-multiples case strip, and synchronized right-hand case provenance & basis inspector.
- **Navigation placement (`DR-SWP-6`):** In the current `CAD | Analysis` navbar shell, add a third segment `Sweeps`
  containing three discrete sub-tabs (`Experiment setup` · `Run (local)` · `Results`) with their B1 exclusivity enforced
  (`UX-17`: no `Run` verb in Experiment setup; `Queue` hands off to `Run (local)`; `Open Results →` hands off to
  `Results`). When the full 7-area activity rail ships, these three views dock directly onto rail slots 4, 5, and 6
  with zero view rewrite.

### 5.2 Hard states (all rendered in [`docs/mockups/local-sweeps.html`](../mockups/local-sweeps.html))

| # | State | Visible contract | Spec / rule |
|---|---|---|---|
| 1 | **Empty** | No sweep defined yet; clear guidance (`SWP-COPY-1`) + `New sweep` primary action; `Queue` absent/disabled; **zero Run button**; status strip `Sweep: none`. | `XS-01`, `XS-03` |
| 2 | **Invalid input blocked** | Live validation highlights offending inputs (`aria-invalid="true"`, `aria-describedby` alert naming duplicate speed `14 kn`, negative speed `−5 kn`, and water `55 °C` outside ITTC 0–50 °C `COPY-52`); preview table blocked; `Queue` disabled with reason. | `XS-01`, `ANA-15`, `UI-39` |
| 3 | **Preview ready (12 cases)** | 3 speeds `[10, 14, 18] kn` × 4 alphas `[0, 2, 4, 6]°` = 12 unique cases; table shows derived `q`, `Re_ref`, `h/c`, `Fr_h`, `σ`, plus storage estimate (`≈ 1.53 MB` VLM / `≈ 25.5 kB` Estimator), 32 MB headroom, and wall estimate `"Not recorded"` (until measured); tier selector (`VLM + strip` / `Estimator`) and source toggle (`Angles × speeds` / `Goal state points`); **Queue** enabled; **NO Run button**. | `XS-01`, `XS-03`, `UX-09`, `IO8` |
| 4 | **Running / Cancelled** | In `Run (local · in-process)`: progress `Running 7 of 12` with per-case rows (`Completed` 1–6, `Running` 7, `Pending` 8–12), static skeleton (no shimmer, `DESIGN.md` §6), live **Cancel** action in console and status strip. Triggering Cancel transitions to `Cancelled (6 of 12 completed)` with cases 1–6 preserved and inspectable, cases 7–12 cancelled/pending with `Resume / Retry remaining`. | `RUN-05` local, `RUN-06`, `UX-21`, `A8.1` |
| 5 | **Completed with 1 failed case** | `Completed (11 of 12 · 1 failed)` — interior case `14 kn · α 4°` (case #7) failed (`ANA-SOLVE-RESIDUAL: lattice residual exceeded limit`). Performance curves (`CL`, `CD (Wing only, Ncrit 2–4)`, `Wing-only L/D (Ncrit 2–4)`, `Cm (frame origin)`) vs `α` and vs `Speed`: **explicit interior gap on both the 14 kn series at α 4° and the α 4° series at 14 kn (zero line connecting across the gap)**, distinct dash/marker + direct label per series, `Show table` twin, linked case selection across chart/table/inspector, explicit failure reason + `Retry sample` action. | `ANA-08`, `ANA-16`, `RES-04`, `RUN-05`, `RUN-06`, `UI-17` |
| 6 | **Depth unset** | `h_ref` unset: `h/c`, `Fr_h`, `σ`, and `V_crit` read `Unavailable — depth not set` (`COPY-45`); fixed VLM label reads `attached flow; no stall; no ventilation; free surface not modelled` (`COPY-224`) — **never** `"deep water"`; cavitation ceiling band omitted with explicit reason. | `ANA-19`, `A5.1`, `A5.6` OQ-9 |
| 7 | **Historical sweep after geometry edit** | After CAD edit `r4 → r5 (a twist point moved)`: persistent amber banner `Historical — surface changed (r4 → r5, a twist point moved)` (`COPY-64`), Historical chip on every curve and case row, freshness derived per case by run key, plus `New version on r5` and `Archive sweep (release 1.53 MB)` actions. | `ANA-07`, `UI-06`, `DR-SWP-2` |
| + | **Modes & a11y harness** | Light + Dark + High Contrast themes; `1440 × 900` and `1024 × 700` viewport presets; Persona switcher (`designer` · `keyboard-only` · `screen-reader` · `reviewer`); Reduced Motion toggle (instant transitions, no autoplay); live computed WCAG contrast table for every token pair. | `UI-02`, `UI-04`, `UI-05`, `UI-16` |

### 5.3 Proposed copy rows (NOT yet added to `DESIGN.md`)

High water on `main` is `COPY-680` (`CLN` reserves `681..685`, `CF` reserves `686..705`). This design proposes
`SWP-COPY-1..SWP-COPY-22` (candidate `COPY-706..727`) for operator review; `DESIGN.md` is **not** edited here. Every
string below has been checked against `LAB-01` (zero occurrences of `cavitation-free`, `ventilation-safe`, `validated`,
`optimized`, `best`, `recommended`, `certified`).

| Proposed ID | Candidate `DESIGN.md` row | Exact proposed string | Component / state |
|---|---|---|---|
| `SWP-COPY-1` | `COPY-706` | `No sweep defined yet. Choose Angles × speeds or Goal-state points to preview cases.` | Experiment setup empty state |
| `SWP-COPY-2` | `COPY-707` | `Experiment setup defines and previews cases only. Queue hands an immutable version to Run.` | Experiment setup footer note (`XS-03`) |
| `SWP-COPY-3` | `COPY-708` | `Duplicate value in <field>: <value>. Remove duplicates to enable Queue.` | Invalid input block (`XS-01`) |
| `SWP-COPY-4` | `COPY-709` | `Invalid value in <field>: <value>. Enter finite numbers with valid units.` | Invalid input block (`XS-01`) |
| `SWP-COPY-5` | `COPY-710` | `Speed must be greater than 0. Invalid entry: <value>.` | Invalid speed block (`ANA-INPUT-SPEED`) |
| `SWP-COPY-6` | `COPY-711` | `Case count <n> exceeds the local sweep limit (<cap>). Narrow the grid or split into two sweeps.` | Case-cap block (`DR-SWP-3`) |
| `SWP-COPY-7` | `COPY-712` | `Preview out of date — <what changed>. Refresh preview before queueing.` | Post-preview Draft invalidation (`XS-01`) |
| `SWP-COPY-8` | `COPY-713` | `Estimated wall time: Not recorded` | Preview before session timing exists (`IO8`) |
| `SWP-COPY-9` | `COPY-714` | `Estimated storage: ≈ <size> (Inferred) · Document headroom: <used> / 32.0 MB` | Preview storage budget (`DR-SWP-2`) |
| `SWP-COPY-10` | `COPY-715` | `Pinned to local · in-process (<tier>). Queued version v<n> is immutable.` | Queued banner (`XS-03`) |
| `SWP-COPY-11` | `COPY-716` | `Running <k> of <m> · local · in-process` | Run console & status strip (`UX-21`) |
| `SWP-COPY-12` | `COPY-717` | `Cancelled — <k> of <m> completed cases preserved; remaining cases stopped.` | Local Cancel banner (`RUN-06`) |
| `SWP-COPY-13` | `COPY-718` | `Completed (<k> of <m>) — <f> failed case(s) shown as gaps. Select a gap to inspect or retry.` | Partial completion banner (`ANA-08`, `RUN-05`) |
| `SWP-COPY-14` | `COPY-719` | `Failed (0 of <m>) — no case completed. Inspect case reasons below before retrying.` | All-failed banner (`RUN-05`) |
| `SWP-COPY-15` | `COPY-720` | `Gap — case failed (<code>): <reason>. No line is drawn across a gap.` | Chart gap annotation & table row (`ANA-08`, `RES-04`) |
| `SWP-COPY-16` | `COPY-721` | `Stopped before case <k>: document reached the 32.0 MB size limit (DOC-SIZE). Archive an older sweep or switch to Estimator.` | Size-cap stop (`RUN-04`, Ruling 263) |
| `SWP-COPY-17` | `COPY-722` | `Cm about frame origin (root LE, c_ref = S/b, positive nose-up)` | `Cm` chart subtitle (`ANA-16`, `A4.7`) |
| `SWP-COPY-18` | `COPY-723` | `Unavailable — estimator does not compute pitching moment` | `Cm` chart on Estimator tier |
| `SWP-COPY-19` | `COPY-724` | `Unavailable — estimator computes induced drag only (no viscous profile drag)` | Wing-only `CD` / `L/D` on Estimator tier |
| `SWP-COPY-20` | `COPY-725` | `No Goal state in this project yet — define one in Setup, or use Angles × speeds.` | Disabled Goal-state radio (`§1.6`) |
| `SWP-COPY-21` | `COPY-726` | `Archived — definition and attempt history kept; unreachable strip data released for pruning at save.` | Archived sweep banner (`DR-SWP-2`) |
| `SWP-COPY-22` | `COPY-727` | `Unavailable — pruned at save (archived sweep). Re-run in a new version to recompute.` | Tombstoned case cell (`ADR-0011 §7`) |

---

## 6. Telemetry and observability (`IO1–IO12`)

Every sweep event emits on the normal path by default through `AuthoringSession.RecordSweepEvent` (same bounded in-memory
session event queue as `analysis.run` and `analysis.compare`, zero flags, zero file names, zero user text, zero PII;
any unreached measurement stays `null` and renders `"not recorded"`, never `0` — `IO8`).

| Operator question (named before build, `IO3`) | Emitting event | Fields & cost axes |
|---|---|---|
| How often are sweeps queued, what size, and what tier? | `sweep.queue` | `experimentId`, `version`, `sourceKind`, `tier`, `caseCount`, `estimatedBytes`, `documentBytes`, `durationMs`, `outcome` (`OK` \| `ANA-SWEEP-*` \| `DOC-SIZE`) |
| How long does a full local sweep take, what is its cache-hit ratio, and how often does it fail or cancel? | `sweep.run` | `experimentId`, `version`, `tier`, `totalCases`, `completedCases`, `failedCases`, `cacheHits`, `cancelled`, `wallMs`, `cancelLatencyMs` (null unless cancelled), `outcome` (`Completed` \| `Failed` \| `Cancelled` \| `DOC-SIZE`) |
| Which individual cases are slow, failing, or hitting `FindAlpha` iteration caps? | `sweep.case` | `experimentId`, `caseIndex`, `attemptIndex`, `tier`, `runKey12`, `idempotentHit`, `findAlphaSolves` (null for cartesian), `wallMs`, `outcome` (`OK` \| `ANA-*` \| `ANA-CANCELLED`) |
| How often do users retry individual failed samples, and do retries succeed? | `sweep.retry` | `experimentId`, `caseIndex`, `attemptIndex`, `priorAttemptId`, `wallMs`, `outcome` |
| How much document storage does archiving reclaim against the 32 MB cap? | `sweep.archive` | `experimentId`, `releasedRuns`, `pinnedRunsAfter`, `documentBytesBefore`, `documentBytesAfter`, `outcome` |
| How fast do performance curves and table twins project? | `sweep.project` | `experimentId`, `activeChart`, `xAxis`, `casesShown`, `gaps`, `durationMs`, `outcome` |

---

## 7. Security and resource bounds (`A8.5`)

- **No subprocess / no shell:** `local · in-process` sweeps execute purely inside managed C# (`VortexLattice`,
  `StripCoupler`, `NeuralFoil`, `WingEstimator`, `FindAlpha`) — zero external process launch, zero shell strings, zero
  network calls (`AI-01`).
- **Bounded parser and grid caps:** `MaxCasesPerSweep = 120`, `MaxSpeeds = 30`, `MaxAngles = 30`, `NativeProject.MaxBytes
  = 32_000_000`, `MaxLineBytes = 4_096`, `MaxStrips = 2_048`. Every JSON numeric field is checked `double.IsFinite` on
  both write and read (`DOC-SCHEMA`).
- **Tamper detection (`ADR-0011 §4`):** Both `SweepExperiment.DefinitionHash` and every referenced `AnalysisRun`'s
  `ContentHash` + `RecomputedKey` are re-verified on read. A bit flip in an experiment or referenced run fails closed
  to `Unavailable — <payload failed its check>`, never silently repairing or deleting data.
- **No PII in sweep records or events:** Experiment `name` is bounded plain text (≤ 120 chars, never emitted into
  telemetry events, never interpreted as path or shell input); timestamps carry UTC `Z` only (`TimeProvider.GetUtcNow()`).

---

## 8. Red-first test plan and named mutants (Test Architect floor)

Every build node writes its tests **red first** (observed failing before implementation), then green, and kills every
named mutant below. Ring-0 tests run on both Windows x64 and macOS ARM64 (`CLI-02`, `tools/run-tests.sh`).

| # | Test name (project) | Story / rule | Observed-red precondition & pass oracle | Named mutant that MUST turn it red |
|---|---|---|---|---|
| T1 | `SweepSchedule_Cartesian3x4_DerivesTwelveCasesAndConditions` (Analysis) | `XS-01` | 3 speeds `[10, 14, 18] kn` × 4 alphas `[0, 2, 4, 6]°`, salt 15 °C, `h_ref 0.5 m` → 12 unique sorted cases with exact `q`, `Re_ref`, `h/c`, `Fr_h`, `σ` within `10⁻¹²` relative. | Mutant swaps `kn → m/s` factor or uses `c_root` instead of `c_ref` for `Re_ref`. |
| T2 | `SweepSchedule_InvalidInputs_BlockWithInputNamed` (Analysis) | `XS-01`, `ANA-15` | Empty list, duplicate speed (`14, 14`), non-finite (`NaN`), speed `≤ 0`, depth `≤ 0`, temperature `55 °C` (`COPY-52`), and `121` cases each throw stable code naming the offending input. | Mutant silently deduplicates `[14, 14]` into 11 cases instead of refusing. |
| T3 | `SweepSchedule_DepthUnset_UnavailableAndNoDeepWaterLabel` (Analysis) | `ANA-19`, `A5.6` | `h_ref = null` → `h/c`, `Fr_h`, `σ`, `V_crit` read `Unavailable — depth not set`; fixed label reads `"free surface not modelled"`, asserting `!text.Contains("deep water", OrdinalIgnoreCase)`. | Mutant prints `Labels.FixedVlmDeep` when `h_ref == null`. |
| T4 | `SweepRecord_ImmutableOnceQueued_EditRefused` (Core) | `XS-03` | Appending `Queued` seals definition/cases/pin; attempting to mutate or append attempts before `Queued` fails closed (`DOC-SCHEMA`). | Mutant allows recording a `SweepAttempt` on a `Draft` experiment. |
| T5 | `SweepStore_RunlessExperiment_FormatProject2AndV1Bak` (Core / Persistence) | `DR-SWP-1`, `ADR-0011` | Saving a `-1` project after queueing a 12-case sweep (0 runs yet) writes `.v1.bak` (byte-equal to `-1`) and publishes `cfdw-project-2` with `experiments` and `analysis == null`; round-trips cleanly. | Mutant derives `Format` from `runs.Count` alone → writes `-1` with `experiments` → reader rejects. |
| T6 | `SweepStore_OmittedWhenEmpty_ExistingBytesIdentical` (Core) | `ADR-0011` | Existing `-1` and `-2` documents without experiments serialize byte-identically to pre-SWP `main`. | Mutant writes `"experiments": []` instead of omitting null. |
| T7 | `SweepStore_OldBuildReader_FailsClosedCopy130` (Core / Proof) | `ADR-0011 §6` | Opening a `-2` file carrying `experiments` with the pre-SWP reader refuses with `DOC-UNSUPPORTED-FIELD` (`COPY-130`) and leaves file SHA-256 unchanged. | Mutant ignores unknown top-level members on read. |
| T8 | `SweepStore_TamperedDefinitionOrRun_ReadsUnavailable` (Core) | `ADR-0011 §4` | Flipping 1 byte in `definition.anglesDeg` or a referenced run's strip makes the sweep/case read `Unavailable` without deleting it. | Mutant trusts stored `definitionHash` / `runKey` without recomputing. |
| T9 | `SweepRetention_ActivePinsSelected_ArchiveUnpinsAndTombstones` (Core) | `DR-SWP-2`, Ruling 263 | 12-case VLM sweep on an unreachable branch survives `SaveImage()` with all 12 runs intact; superseded retry attempt is not pinned; appending `Archived` then `SaveImage()` prunes unreachable runs into `PrunedRun` tombstones rendering `SWP-COPY-22`. | Mutant skips sweep pinning in `KeepPlan()` (drops runs before Archive) OR keeps pinning after `Archived`. |
| T10 | `SweepService_LocalRun12Cases_ReusesRunKeyAndPreservesCancel` (Analysis) | `RUN-05`, `RUN-06`, `A8.1` | Runs 12-case sweep sequentially on `local · in-process`; pre-computed case 0 hits idempotency (`idempotentHit == true`, 1 run row); cancelling via `IEvaluationBarrier` on case 7 finishes within 250 ms, keeps cases 0..5 `Completed`, marks case 6 `Cancelled` (`runId == null`), leaves 7..11 `Pending`. | Mutant shares interactive `AnalysisService` or deletes completed runs on Cancel. |
| T11 | `SweepService_OneFailedCase_StatusCompletedAndRetryAppendsAttempt` (Analysis) | `RUN-05`, `RUN-06`, `ANA-08` | Injecting `ANA-SOLVE-RESIDUAL` on case 11 yields experiment status `Completed (11 of 12)`, case 11 attempt 1 `Failed` (`runId == null`); `RetrySampleAsync(11)` appends `attemptIndex = 2` with `priorAttemptId = attempt1.Id`, preserving attempt 1; all-12-failed yields status `Failed (0 of 12)`. | Mutant overwrites attempt 1 in place on Retry, OR sets `runId` on a Failed attempt, OR marks 11/12 experiment `Failed`. |
| T12 | `SweepService_DocSizeCap_StopsSafelyWithoutCorrupting` (Analysis) | `RUN-04`, Ruling 263 | Under a lowered test `envelopeCap`, hitting `DOC-SIZE` on case `k` marks case `k` `Failed (DOC-SIZE)`, halts cases `k+1..N-1` as `Pending`, and preserves all `0..k-1` runs intact. | Mutant continues launching cases after `DOC-SIZE` or leaves partial corrupt envelope. |
| T13 | `SweepProjection_CurvesAndGaps_NoSegmentAcrossFailedCase` (Analysis) | `ANA-16`, `RES-04`, `ANA-08` | Projects 11 Completed + 1 Failed sweep into `CL`, `CD (Wing only, Ncrit 2–4)`, `Wing-only L/D (Ncrit 2–4)`, and `Cm (frame origin)` series: verifies `Cm` sign against analytic flat-plate quarter-chord lift (`My = -x_cp * L < 0` → `Cm < 0`), verifies `Ncrit 2` and `Ncrit 4` are both present (never averaged), and asserts the failed index splits the polyline into disjoint segments with **zero** edge bridging the gap. | Mutant filters out failed points before building polyline (connecting `k-1` directly to `k+1`) OR averages `Ncrit 2` & `4` into one scalar. |
| T14 | `SweepProjection_EstimatorAndOnePanel_CmAndDragHonestUnavailable` (Analysis) | `A5.6`, Ruling 161 | Estimator sweep returns `CL` and `CDi (e = 1)` and `Unavailable` for `Cm` and `Wing-only L/D`; 1-chordwise-panel VLM sweep returns `Not resolved · 1 chordwise panel` for `Cm`. | Mutant prints `0.000` for Estimator `Cm` or 1-panel `Cm`. |
| T15 | `SweepProjection_GeometryEditAfterSweep_HistoricalByRunKey` (Analysis) | `ANA-07`, `UI-06` | Accepting a twist edit `r4 → r5` makes the `r4` sweep read `Historical — surface (r4 → r5, a twist point moved)` without mutating any stored sweep field; Undo back to `r4` makes it `Current` again. | Mutant stores a mutable `IsHistorical` bool on `SweepExperiment`. |
| T16 | `SweepService_GoalStatePoints_SolvesAlphaPerPoint` (Analysis) | `XS-01`, `ANA-05` | Given a 3-point `GoalStateSweepSnapshot`, `SweepService` solves `α` per point via `FindAlpha` within 1 % of `RequiredLoadN`, records `SolvedAlphaRecord`, and marks an unsolvable point `Failed (ANA-FIND-NO-SIGN-CHANGE)` without extrapolating. | Mutant clamps `α` to bracket boundary when no sign change exists. |
| T17 | `Cli_SweepRunAndInspect_MatchesGuiRunKeysBitwise` (Cli) | `CLI-01` | `cfd-workbench sweep preview`, `sweep run --out`, and `inspect --sweeps` produce bit-identical `runKey`s and `10⁻¹²`-equal `CL`, `CDi`, `Cm` compared to GUI `SweepService`. | Mutant builds CLI `OperatingPoint` with different `PAtm` or `Datum`. |
| T18 | `Desktop_Sweeps_NoRunButtonInSetup_LinkedSelection_Lab01Clean` (Desktop) | `XS-03`, `ANA-08`, `LAB-01`, `UI-13`, `UI-17` | Verifies Experiment setup contains zero `Run` button; clicking/keying case 5 synchronizes chart, table twin, and inspector; `Cancel` works from console and status strip; scans all rendered sweep strings against the 7 `LAB-01` forbidden words (0 hits). | Mutant puts a `Run` button inside `ExperimentSetupView` OR uses `"recommended"` in tier copy. |

---

## 9. Build node breakdown, seams and size estimate

All effort estimates below are **Inferred** from Wave 2–4 track durations (no sweep build has run yet).

| Node | Owns (authored) | Depends on | Est. size | Exit gate |
|---|---|---|---|---|
| **SWP-1** Core store & retention | `src/CfdWorkbench.Core/SweepRecord.cs` (new), `RunRecord.cs` (`Format` overload), `AuthoringSession.cs` (`Experiments` envelope + `KeepPlan` + telemetry), `tests/CfdWorkbench.Core.Tests/SweepStoreTests.cs`, `docs/proof/swp-old-build/` | Operator approval of `DR-SWP-1`, `DR-SWP-2` | ~450 LOC · 1 session | T4–T9 observed red → green, mutants killed, existing Core & Persistence tests green |
| **SWP-2** Analysis schedule, service & projection | `src/CfdWorkbench.Analysis/SweepSchedule.cs`, `SweepService.cs`, `SweepProjection.cs` (new), `AnalysisService.cs` (revision-pinned VLM), `Compare.cs` (`Cm` helper), `Labels.cs` (`SWP-COPY`), `tests/CfdWorkbench.Analysis.Tests/SweepTests.cs` | `SWP-1`, `DR-SWP-3..5` | ~700 LOC · 1 session | T1–T3, T10–T16 observed red → green, mutants killed, `run-tests.sh` green |
| **SWP-3** CLI parity | `src/CfdWorkbench.Cli/SweepVerb.cs` (new), `Program.cs`, `tests/CfdWorkbench.Cli.Tests/CliSweepTests.cs` | `SWP-2` | ~220 LOC · 0.5 session | T17 observed red → green, bitwise `runKey` parity proven |
| **SWP-4** Desktop UI & charts | `src/CfdWorkbench.Desktop/Sweeps/*` (new), `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/StatusStrip.axaml(.cs)`, `Shell/CommandTable.cs`, `tests/CfdWorkbench.Desktop.Tests/SweepViewTests.cs` | `SWP-2`, approved mockup, `DR-SWP-6` | ~950 LOC · 1–2 sessions | T18 observed red → green, theme/focus/table-twin/gap-rendering verified on native Desktop harness |
| **SWP-5** Copy & Proof Pack | `DESIGN.md` (`COPY-706..727` + component rows), `docs/reviews/local-sweeps-native.md`, native captures | `SWP-3`, `SWP-4` | ~150 LOC · 0.5 session | Full `run-tests.sh` + `check-docs.py` + Adversary sign-off (Hydrodynamicist, UX/A11y, Data, Test Architect) |

---

## 10. Decision Requests for the operator (`DR-SWP-1..DR-SWP-6`)

| ID | Decision | Options | Recommendation & rationale |
|---|---|---|---|
| **DR-SWP-1** | Where `SweepExperiment` records live in `.cfdw.json` and how format `-2` is derived | **(a)** Optional top-level `experiments` member on `cfdw-project-2` (omitted when empty); `NativeProject.Read` accepts `["analysis", "setup", "experiments"]` when coordinated with Area 1 (`SET`, `DR-SET-1`) and `Format` returns `-2` whenever any of `runs`, `setup`, or `experiments` is present. **(b)** Nest under `analysis.experiments` (forces writing empty `analysis.runs: []` for a Queued sweep). **(c)** Sidecar `.sweeps.json` file. | **(a) Recommended.** Matches A3.1 bounded contexts (`Project → Experiment`), composes cleanly with Area 1's optional `"setup"` member, keeps `-1` and `-2` run-only files byte-identical, preserves `.v1.bak` rollback, and fails closed cleanly (`COPY-130`) on older builds. |
| **DR-SWP-2** | Sweep run retention and unpinning under `NativeProject.MaxBytes = 32,000,000` (Ruling 263) | **(a)** Pin selected `Completed` runs of non-archived sweeps on top of the 20-per-tier quota + explicit append-only `Archived` event to release pins (leaving `PrunedRun` tombstones). **(b)** Pin all sweep runs permanently like Discrepancy records (hits 32 MB after ~20 VLM sweeps). **(c)** Never pin sweep runs (silently breaks Historical sweeps into tombstones). | **(a) Recommended.** Protects active sweeps from Swiss-cheese pruning while giving designers a non-destructive way to reclaim storage inside 32 MB. |
| **DR-SWP-3** | Local case-count ceiling and execution concurrency | **(a)** `MaxCasesPerSweep = 120`, sequential execution on a dedicated `AnalysisService` with idempotent `runKey` reuse. **(b)** Parallel `Parallel.ForEachAsync` across CPU cores. **(c)** Uncapped case count. | **(a) Recommended.** Sequential execution avoids `Placement` / memory-working-set spikes (1.5 GB budget, Ruling 263), guarantees deterministic cancel ≤ 250 ms, and 120 cases (~15 MB max VLM) fits safely inside half of `MaxBytes`. |
| **DR-SWP-4** *(coordinated with `DR-SET-2`)* | `OperatingPoint.Load` on `AnalysisRun` rows produced by Goal-state load-solved sweeps (`RunRecord.cs:76`) | **(a)** Keep `op.Load = null` on `AnalysisRun` (sharing `runKey` with fixed-`α` Analysis at the solved `α`), storing `targetLoadN` and `FindAlpha` provenance on `SweepCase` / `SweepAttempt`. **(b)** Populate `op.Load = requiredLoadN` on Goal-state load-solved runs while keeping `null` on fixed-`α` runs (distinct `runKey`, relaxing `ANA-INPUT-LOAD` in `OperatingPoints.Validate` when Area 1 lands). | **(a) Recommended for `SWP` standalone; (b) viable when coordinated with `SET` (`DR-SET-2`).** Option (a) preserves run-key reuse with single-point Analysis and avoids touching `OperatingPoints.Validate` before Area 1 ships; option (b) makes load-solved runs carry `requiredLoadN` directly in the run key. Both keep all existing A3a/A3d fixed-`α` run keys bit-identical. |
| **DR-SWP-5** | Pitching-moment (`Cm`) datum on `ANA-16` performance curves | **(a)** `frame origin` (root LE, `c_ref = S/b`, positive nose-up — matching `ReferenceQuantities.MomentDatum` and `StripLoad.My`). **(b)** Root quarter-chord (`x = c_root / 4`). | **(a) Recommended.** Uses the exact recorded `RunReference.MomentDatum` already in every stored run (DM7, zero second datum). Moment transfer about a user-named attachment point remains `Loads.About` (`DR-ANA-5`). |
| **DR-SWP-6** | Navigation home for Area 4/5/6 local sweeps before the full 7-area activity rail ships | **(a)** Add a third segment `Sweeps` beside `CAD \| Analysis` in the existing navbar, containing `Experiment setup` · `Run (local)` · `Results` sub-tabs honoring `XS-03` / `UX-17`. **(b)** Block Area 4 UI until the full 7-area activity rail is implemented. | **(a) Recommended.** Ships local sweeps immediately in today's shell while keeping the three views cleanly separated so they dock directly onto rail slots 4–6 later. |

---

## 11. Assumptions and open risks

- **Seam confirmed (`§1.6`, board `#11`):** Area 1's `GoalStateVersion` schema (`Envelope.setup.goalStates[]`,
  `GoalOperatingPoint`, `depthBand`, `speedBand`, `water`) was confirmed directly with `w5-setup-design`. The remaining
  coordinated choice (`op.Load` null vs `requiredLoadN`) is surfaced explicitly in `DR-SWP-4` / `DR-SET-2`.
- `assume:` (`§2.3`) Sequential VLM+strip evaluation of 12 cases (64 × 4 cosine lattice) completes in a few seconds on Apple silicon and Windows x64, making a 12-case local sweep interactive without multi-core parallelism. *Confirm:* measure `sweep.run` `wallMs` on both platforms in `SWP-2`. *Breaks if false:* revisit `DR-SWP-3` with bounded parallelism (cap 2) after verifying `Placement.Sections` thread safety and working-set peak ≤ 1.5 GB.
- `assume:` (`§1.5`) A 12-case `SweepExperiment` JSON record (excluding referenced `AnalysisRun`s) is ≈ 7.5 kB. *Confirm:* measure exact serialized size in `SWP-1` `SweepStoreTests`. *Breaks if false:* adjust preview storage formula constant; `NativeProject.Preflight` remains the authoritative `DOC-SIZE` guard.
- **Residual risk (unproven until `SWP-4`):** Native Avalonia chart hit-testing, VoiceOver/Narrator announcements of gap markers (`UI-13`), and 200 % text scaling are demonstrated in HTML (`docs/mockups/local-sweeps.html`) but remain unproved natively until `SWP-4`'s Desktop harness runs (`UI-T4`).
