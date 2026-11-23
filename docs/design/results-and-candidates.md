---
id: design-results-and-candidates
title: "Design: Area 6 — Results, Replay, Difference Flood, Candidate Ladder, ParaView Handoff, and 7-Area Left Activity Rail"
type: design
status: active
owner: "@timianmalloo"
phase: design — Wave 10A (W10-DESIGN; build basis for W10-RESULTS and W10-RAIL-UI)
tags: [area-6, results, field-evidence, replay, difference-flood, candidate-ladder, paraview, activity-rail, shell, wave10]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: decision-seven-areas, rel: implements }
  - { to: design-cfd-run-pipeline, rel: depends-on }
  - { to: design-local-sweeps, rel: relates-to }
  - { to: design-setup-seeding, rel: relates-to }
  - { to: coordination-wave10-cfd-run-results-and-rail, rel: implements }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: "2027-02-23"
summary: >-
  Build-ready /design-slice for Area 6 Results & Candidate Ladder (spec A5.11, RES-01..05, CAND-01, F8, AI-10) and the
  7-Area Left Activity Rail Shell Architecture (spec B1, B7, Rulings 271 & 272, Adversary C4). Freezes the E7 surface
  list, FieldEvidenceManifest enforcing VIZ separation strictly gated on wall tau_w (never Q/lambda2 vortex-core
  candidates), the 9-field ProvenanceStrip + linked table twin, ReplaySequence (fixed range/camera with labelled unlock,
  failed-sample pause & clear, zero interpolation), DifferenceFlood ("Unavailable — mesh differs" on mesh-hash
  mismatch), CandidateLadder (cross-tier Pareto/feasibility, BuildGeometryDraft emitting typed CAD draft with Rebase
  deviation guard when base AcceptedId moved), process-only ParaView handoff (zero VTK, honest absence copy), and the
  ShellHost Auto,* 3-row / ColumnSpan=2 ActivityRail architecture docking all existing views with zero rewrites,
  zero Dock.* leakage, zero new theme Brush keys, synchronized 4-button ModelArea.Navbar, and COPY-806..865 reservation.
---

# Design: Area 6 — Results, Candidates & 7-Area Left Activity Rail

**Status:** Active (Wave 10A Track `W10-DESIGN`, documents only). Freezes every domain, visualization, projection,
handoff, shell-layout, copy, and testing contract required by Tracks `W10-RESULTS` (Wave 10C) and `W10-RAIL-UI`
(Wave 10D) in `docs/coordination/wave10-cfd-run-results-and-rail.md`.

**Goal state.**
- **Goal:** (1) Specify the pure Core + Analysis + CLI Area 6 Results domain (`FieldEvidenceManifest` with $\tau_w$-gated
  separation, 9-field `ProvenanceStrip` + table twin, `ReplaySequence`, `DifferenceFlood`, `CandidateLadder` +
  `Accept as draft` CAD draft handoff, and process-only ParaView handoff); and (2) specify the Desktop 7-Area Left
  Activity Rail + `RunWorkspaceView` + `ResultsWorkspaceView` hosted cleanly in `ShellHost` while docking existing
  `SetupWorkspaceView`, `CAD`/`Analysis`, `SweepsWorkspaceView`, `ExportDialog`, and `SolverSetupView` with zero view
  rewrites (Rulings 271 & 272).
- **Done when:** `W10-RESULTS` and `W10-RAIL-UI` implement all contracts below, pass every red-first test and mutant in
  §9, keep all existing shell/layout/token/architecture checks green, and clear `w10-verifier` Adversary review.
- **Not in scope:** Live ParaView 5.12+ binary execution on this host (`PARAVIEW_FOUND=False` measured 2026-11-23; tests
  prove both honest absence copy and injected-path process-only launch); linking VTK into the app (prohibited by spec
  A5.11 and COMMIT-02); accessibility / screen-reader work (NVDA / VoiceOver / Narrator — excluded by operator directive
  of 2026-11-23); live BYOK LLM calls for `Ask about this result` (Wave 11; Wave 10 ships deterministic offline key-free
  citation validator); editing `DESIGN.md` during `W10-DESIGN` (`COPY-806..865` are reserved here for `W10-RAIL-UI`).
- **Tier:** T2 (scientific visualization truth rules, COMMIT-01 candidate ladder integrity, and top-level application
  shell architecture).

---

## 0. Evidence ledger (verified from repository source)

Every repository claim below was **Verified** by direct file inspection in this workspace on 2026-11-23.

| Fact | Verified observation | Source | Confidence |
|---|---|---|---|
| `ShellHost` current 3-row layout | `ShellHost` inherits `Grid` with `RowDefinitions = new RowDefinitions("*,Auto,Auto")` (row 0 `DockHost`, row 1 `AnalysisPanel`, row 2 `StatusStrip`). `ShowMenuBar()` inserts an `Auto` row at index 0 and shifts `DockHost`, `AnalysisPanel`, `StatusStrip` down by 1. | `src/CfdWorkbench.Desktop/Shell/ShellHost.cs:27, 140-150, 221, 315-320` | **Verified** |
| `Shell_F1_NoSidebarHeaderBand` invariant | Asserts `host.RowDefinitions.Count == 3`, `GetRow(DockHost) == 0`, `GetRow(AnalysisPanel) == 1`, `GetRow(StatusStrip) == 2`, no visible `"Sidebar"` `TextBlock`, and `LeftSidebarToggle` visible. | `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs:954-972` | **Verified** |
| `NoticeRow` OneDrive invariant | Asserts OneDrive notice row sits at `GetRow(StatusStrip) - 1`, `host.Children.Count(c => GetRow(c) == row) == 1`, and distinct rows of `{MenuBar, DockHost, AnalysisPanel, notice, StatusStrip}` equal `host.RowDefinitions.Count` both before and after `ShowMenuBar()`. | `tests/CfdWorkbench.Desktop.Tests/ControllerShellOneDriveTests.cs:288-306` | **Verified** |
| `StatusStrip` full-width bottom invariant | `StatusStrip_SitsAtWindowBottom_FullWidth_24px` asserts `origin.X == 0`, `Width == window.ClientSize.Width`, `Height >= 23.5`, and bottom flush with `window.ClientSize.Height`. | `tests/CfdWorkbench.Desktop.Tests/StatusStripTests.cs:51-61` | **Verified** |
| `Architecture_DockConfinedToShell` invariant | Scans every `*.cs` under `src/CfdWorkbench.Desktop` outside `Shell/`, `bin/`, `obj/` and fails on any occurrence of `"Dock."`. | `tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs:1237-1249` | **Verified** |
| `ModelArea.Navbar` 4-button Tab order | `ModelArea_Navbar_KeyboardPath_TabAfterTheViews` asserts exact Tab sequence: `NavSetupButton` → `NavCadButton` → `NavAnalysisButton` → `NavSweepsButton` → `NavViewsButton` → `NavDisplayButton` → `NavFitButton` → `NavFitSelectionButton`. | `src/CfdWorkbench.Desktop/ModelArea.axaml:258-283`, `tests/CfdWorkbench.Desktop.Tests/ControllerViewTests.cs:1027-1053` | **Verified** |
| `CommandTable` `"View"` entries & `Modal` flag | `CommandTable.DefaultRows` holds `view.setup`, `view.analysis`, `view.sweeps`, `solver.setup` (`Modal: false`) under `"View"`; `file.export` is `Modal: true`. `Commands_ModalFlag_MatchesRowsThatShowADialog` verifies `Modal` accuracy. | `src/CfdWorkbench.Desktop/Shell/CommandTable.cs:85, 103-107` | **Verified** |
| `Styles.axaml` theme brush closed set | `ThemeDictionaries` (`Light`, `Dark`, `HighContrast`) define exact brush keys (`CanvasBrush`, `SurfaceBrush`, `SurfaceSoftBrush`, `InkBrush`, `MutedBrush`, `LineBrush`, `PrimaryBrush`, `OnPrimaryBrush`, `DangerBrush`, `WarningBrush`, `SelectionBrush`, `ViewportBrush`, `ViewportInkBrush`, etc.) plus root `Batlow0..4Brush` and `VikNeg2..Pos2Brush`. Zero new theme `...Brush` keys may be added (`verify-application-adapters.py`). | `src/CfdWorkbench.Desktop/Styles.axaml:5-138` | **Verified** |
| `DESIGN.md` high-water copy ID | `COPY-805` is the highest approved row after Ruling 275 (0 unapproved rows). Wave 10 reserves `COPY-806..865` (proposed) for `W10-RAIL-UI`. | `DESIGN.md:1053-1058`, `docs/coordination/wave10-cfd-run-results-and-rail.md:104` | **Verified** |
| ParaView host probe | Host probe 2026-11-23 measured `PARAVIEW_FOUND=False`. | `docs/coordination/wave10-cfd-run-results-and-rail.md:63, 102` | **Verified** |

---

## 1. End-to-end surface list (E7)

Every Area 6 result and 7-Area Activity Rail element traverses one authoritative path:

```
NativeProject store (`Envelope.cfd` + `Envelope.analysis` + `Envelope.experiments`) + harvested artifact files
  │
  ▼
CfdWorkbench.Core/Results/ (pure zero-I/O; recursive STORE-SUBSET; zero ArchitectureTests banned names)
  ├── FieldEvidenceManifest.cs  — Layer inventory, VIZ tau_w separation gate, vortex-core & steady/transient basis labels
  ├── ProvenanceStrip.cs        — 9-field required provenance model + linked TableTwin model
  ├── ReplaySequence.cs         — Compatible-series admission, fixed range/camera with unlock, failed-sample pause & clear
  ├── DifferenceFlood.cs        — Zero-pinned Vik delta flood + "Unavailable — mesh differs" mesh-hash guard
  └── CandidateLadder.cs        — Cross-tier Pareto/feasibility, COMMIT-01 promotion checks, BuildGeometryDraft + Rebase guard
  │
  ▼
CfdWorkbench.Analysis/Results/ (all I/O, surface projection, and process-only handoff)
  ├── ResultsService.cs         — Unifies local VLM/Sweep runs + harvested CfdRunRecords + FieldEvidenceRecords
  ├── ResultsProjection.cs      — Projects surface Cp/Cf scalar arrays over Placement.Surface, small multiples, table twins,
  │                               difference floods, candidate ladder, and offline AI-10 citation validator
  ├── ParaViewHandoff.cs        — Process-only ParaView 5.12+ probe + argv-only launch (UseShellExecute=false, zero VTK)
  └── Labels.cs                 — Fixed Area 6 & VIZ strings (LAB-01 compliant)
  │
  ├──► CfdWorkbench.Cli (`ResultsVerb.cs`, `Program.cs`)
  │      └── `results inspect|candidates|paraview --json` (CLI-01 parity)
  │
  └──► CfdWorkbench.Desktop (W10-RAIL-UI)
         ├── ActivityRail.axaml(.cs)           — 7-area left rail (zero Dock.* refs), live readiness badges, Export, Solver setup
         ├── Shell/ShellHost.cs                — Auto,* 3-row Grid hosting ActivityRail in Col=0, DockHost/AnalysisPanel in Col=1,
         │                                       MenuBar/StatusStrip/Notice/paletteBackdrop spanning ColumnSpan=2
         ├── Shell/CommandTable.cs             — Adds view.run & view.results (Modal: false, "View" menu)
         ├── Run/RunWorkspaceView.axaml(.cs)   — Area 5 Run workspace (rail slot 5)
         ├── Results/ResultsWorkspaceView.axaml(.cs) + ResultsFieldCanvas.cs — Area 6 Results workspace (rail slot 6)
         └── ModelArea.axaml(.cs) + WorkbenchController.cs — Docks existing Setup & Sweeps with zero rewrite, syncs 4-button Navbar
```

**Core naming discipline (`ArchitectureTests` `C3`):** No method in `CfdWorkbench.Core/Results/` invoked by
`CfdWorkbench.Analysis` is named `ProfileAt`, `Cancel`, `Undo`, `Redo`, or prefixed `Begin*`, `Update*`, `Apply*`.
Exact safe method names: `BuildManifest`, `ResolveLayer`, `BuildProvenance`, `BuildTableTwin`, `CreateSeries`,
`SelectFrame`, `AdvanceStep`, `StepBackward`, `ToggleRangeLock`, `ComputeDifference`, `RankCandidates`,
`EvaluatePromotion`, `BuildGeometryDraft`, `BuildRebasedDraft`.

---

## 2. `FieldEvidenceManifest` & VIZ truth rules (spec A5.11, RES-01, RES-02, AI-10)

`FieldEvidenceManifest` is constructed purely from a sample's harvested `FieldEvidenceRecord[]` (or from a local VLM +
strip `AnalysisRun` with explicit `"local calculation"` basis). Every layer's availability is **derived**, never
hard-coded enabled.

| Layer Kind | Required Field / Capability | Colormap Policy | Fixed Basis / Honest Label | When Absent / Unavailable |
|---|---|---|---|---|
| `SurfaceCp` | `Cp` on `surface` evidence (or local VLM panel `Cp`) | `vik` pinned at `0` | `Cp · dimensionless · face/panel` | `"Unavailable — Cp field not present in this sample"` |
| `SurfaceCf` | `Cf` or `wallShearStress` on `surface` evidence | `batlow` (magnitude) or `vik` pinned at `0` ($C_{f,x}$) | `C_f · dimensionless · wall patch` | `"Unavailable — C_f field not present in this sample"` |
| `SliceVelocity` | Velocity `U` on `slice` evidence | `batlow` | `"in-plane velocity magnitude, m/s · steady RANS mean field"` | `"Unavailable — velocity slice not computed (requires ParaView 5.12+ or slice harvest)"` |
| `Streamlines3D` | Velocity `U` on `rake`/volume evidence | `batlow` | `"instantaneous integral curves of the mean field (steady RANS)"`; moving dash carries `"affordance, not fluid motion"` | `"Unavailable — streamlines not precomputed for this sample"` |
| `TurbulentKineticEnergy` | `hasTurbulentKineticEnergy == true` (`k` field) | `batlow` | `"modeled turbulent kinetic energy, m²/s², model, mean-flow basis"` | `"Unavailable — k not modelled by this turbulence model"` |
| `ForceVectors` | Converged or local strip/wing force vectors | Theme `PrimaryBrush` / `InkBrush` | `"Body axes: +x aft, +y starboard, +z up; lift and drag in wind axes"` | `"Unavailable — force vectors not recorded"` |
| `SpanwiseLoading` | Strip loads or surface spanwise integration | `batlow` | `"Spanwise loading Cl·c/c̄ vs η"` | `"Unavailable — spanwise loading not recorded"` |
| `VortexCoreCandidates` | `Q` or `lambda2` on `isosurface` evidence + threshold `t` | `batlow` | **`"vortex-core candidates at threshold <t>"`** — **NEVER** labelled separation | `"Unavailable — Q/λ2 isosurface not present in this sample"` |
| **`Separation` (HARD VETO)** | **`hasWallShearStress == true` on wall patch AND non-empty `separationCriterion`** | Theme `DangerBrush` / `WarningBrush` contour | **`"Separation — criterion: <skin-friction-line convergence | C_f,x sign change on this cut>"`** | **`"No supported criterion in this sample"`** (layer strictly disabled; vortex-core layer NEVER substitutes) |
| `Pathlines` | `isTransient == true` AND `timesteps.Count >= 2` | `batlow` | `"physical-time pathlines (transient)"` | `"Unavailable — steady RANS has no physical-time pathlines"` |

**Offline `Ask about this result` (`AI-10`):** A deterministic Core/Analysis validator ensures that when a user asks
about flow separation on a sample whose `FieldEvidenceManifest` has `Separation.Available == false`, the response
strictly returns `"No supported criterion in this sample"`, citing the sample's `runId` and manifest layer table, and
never diagnoses separation from `Q`/`lambda2` or un-grounded numerals.

---

## 3. 9-field `ProvenanceStrip` + linked table twin (spec A5.11, RES-01)

Every Results view (surface flood, replay frame, small multiple, difference flood, candidate comparison) renders one
`ProvenanceStrip` record carrying **all 9 required fields** (`null` or empty strings are forbidden — unmeasured values
explicitly format as `"Not recorded"`):

| # | Property (`ProvenanceStrip`) | Example (CFD Sample) | Example (Local VLM Sample) |
|---|---|---|---|
| 1 | `Variable` | `Cp` | `Cl·c/c̄` |
| 2 | `Unit` | `dimensionless` | `dimensionless` |
| 3 | `RangeWithBasis` | `[-1.84, 1.00] · series-locked` | `[0.00, 0.86] · sample-range` |
| 4 | `MethodTierAndVersion` | `OpenFOAM ESI v2512 · simpleFoam (SA)` | `VLM + strip · local calculation (cfdw.vlm-strip 1.1.0)` |
| 5 | `SourceRunAndSample` | `run 9a3f12c0e4b1 · α 4.0° · V 7.20 m/s` | `run 4d81aa09b20c · α 4.0° · V 7.20 m/s` |
| 6 | `DepthBasis` | `h_ref = 0.50 m (root LE)` | `depth not set · free surface not modelled` |
| 7 | `EvidenceLabel` | `Converged: residuals <= 4.2e-04 · outputs complete · grid uncertainty not quantified (single mesh)` | `Computed estimate · Model uncertainty not quantified` |
| 8 | `ColormapNameAndVersion` | `vik · Crameri 8.0` | `batlow · Crameri 8.0` |
| 9 | `AssociationAndTimeBasis` | `face · steady RANS (mean field)` | `strip · local calculation` |

**Linked table twin (`TableTwin`):** Built from the exact same `ResultsProjection` data as the visual plot/canvas
(columns: `Index`, `Location (η or x/c)`, `Variable`, `Unit`, `Value`, `Status / Flag`). Selecting a row highlights the
corresponding sample/station in the view; missing/masked values print `"Unavailable — <reason>"`, never `0.0`.

---

## 4. `ReplaySequence` & `DifferenceFlood` (spec A5.11, RES-03, RES-04)

### 4.1 `ReplaySequence`

- **Compatible series admission (`CreateSeries`):** Groups samples sharing identical `(SurfaceHash, MeshHash, MethodId,
  MethodVersion, WaterRecord, FieldInventoryHash, HeldAxis)` where `HeldAxis` is either `HeldSpeed` (ordered by ascending
  $\alpha$) or `HeldAlpha` (ordered by ascending speed $V$). Samples with differing `MeshHash` are **never** placed in
  the same series.
- **Admitted vs failed samples:** Every case slot in the series order is represented:
  - **Admitted sample:** `CfdRunRecord` (or local `AnalysisRun`) in terminal `Completed` or `CompletedResidualNotMet`
    state whose `FieldEvidenceRecord` hashes verify `Intact` and reduction succeeded (`Converged` is not required; its
    exact evidence label is shown).
  - **Failed / gap sample:** Any slot whose selected attempt is `Failed`, `Cancelled`, or `PayloadFailedCheck`.
- **Fixed range, camera, seeds, and slices:** On series creation, `LockedScalarMin`/`LockedScalarMax` are computed once
  across all admitted samples in the series (`RangeLocked = true`); `CameraPreset`, `SeedRake`, and `SlicePlanes` remain
  pinned across frame changes. Calling `ToggleRangeLock()` flips `RangeLocked = false` and emits an explicit visible
  label `"Range unlocked — per-sample scale"`.
- **Failed-sample pause & clear invariant:** Stepping, scrubbing, or playing onto a failed sample slot MUST:
  1. Set `IsPlaying = false` (pause playback immediately).
  2. Set `ActiveFieldData = null` and `ActiveMetrics = null` (clear displayed fields and metrics BEFORE showing the
     failure banner — never leave the previous sample's numbers on screen).
  3. Surface `FailureReason` and `RetrySampleRunId` one action away.
- **Zero interpolation:** `ReplaySequence` indexes discrete frame `0..Count-1` only; no fractional frame index or linear
  interpolation between samples exists.
- **Fixed series banner:** `"Parametric sweep — sequence over admitted samples"`.
- **Reduced motion:** When `ReducedMotion == true`, `Play` is disabled (`IsPlaying` forced `false`), while
  `AdvanceStep`, `StepBackward`, and `SelectFrame` (scrub) stay fully functional.

### 4.2 `DifferenceFlood`

`DifferenceFlood.ComputeDifference(SampleFieldData baseSample, SampleFieldData targetSample, string variable)`:
1. **Mesh-hash guard (HARD VETO):** If `baseSample.MeshHash` is null/empty, `targetSample.MeshHash` is null/empty, or
   `!string.Equals(baseSample.MeshHash, targetSample.MeshHash, StringComparison.Ordinal)` → strictly return:
   - `Available = false`
   - `UnavailableReason = "Unavailable — mesh differs"`
   - `DeltaValues = []`
2. **Variable & association guard:** If variable, association, or sample count differs → return `Available = false` with
   named reason (`"Unavailable — field layout differs"`).
3. **Zero-pinned diverging output:** Computes `delta[i] = target[i] - base[i]` (finite-checked), sets symmetric scale
   `absMax = max(|delta[i]|)` so range is `[-absMax, +absMax]` pinned strictly at `0.0` on colormap `vik · Crameri 8.0`,
   with a full 9-field `ProvenanceStrip` naming both `baseSample` and `targetSample` run IDs.

---

## 5. `CandidateLadder` & `Accept as draft` CAD handoff (COMMIT-01, RES-05, CAND-01)

### 5.1 Cross-tier ranking, Pareto extraction & COMMIT-01 promotion

- **No cross-tier averaging:** `CandidateLadder.RankCandidates` displays each candidate's multipoint objective and
  constraint values grouped explicitly by tier (`vlm-strip`, `cfd`, `surrogate`), showing pinned `DiscrepancyRecord`
  deltas (`δ`, `ρ`) beside promoted tiers — never averaging VLM and CFD values.
- **Pareto & feasibility:** Filters `Feasible == true` candidates and computes the exact 2-objective non-dominated
  Pareto front plus parallel-coordinates axis descriptors (axis name, unit, min, max). When zero feasible candidates
  exist, `CandidateLadderView.EmptyReason` displays only the experiment's terminal reason (`RES-05`).
- **Promotion (`EvaluatePromotion`):** Advances `SurrogateCandidate → VlmChecked → CfdChecked → ExperimentallyCompared`
  **ONLY** when:
  1. Every operating point in the experiment's multipoint set has a verified `Intact` `Completed` run at the target tier
     on the candidate's own geometry revision (if even one point is missing, returns `Refused` naming the missing point).
  2. A `DiscrepancyRecord` key exists for each multipoint point against the lower tier.
  3. The stated objective-gain threshold survives at the higher tier.
  No manual edit or prose string can change `CandidateStatus`.

### 5.2 `Accept as draft` → `BuildGeometryDraft` & Rebase guard

A Candidate **never** mutates accepted CAD geometry directly (COMMIT-01, spec B1:1440).

- **`BuildGeometryDraft(CandidateRecord candidate, string currentAcceptedId, string currentSurfaceHash)`:**
  - If `candidate.BaseAcceptedId == currentAcceptedId && candidate.BaseSurfaceHash == currentSurfaceHash`:
    returns a typed `CandidateGeometryDraft` carrying `CandidateId`, `BaseAcceptedId`, `ParameterDeltas`,
    `IsRebased = false`, `RebaseDeviationMeters = null`, ready for `WorkbenchController` to open as a normal previewed
    CAD `GeometryEditDraft` (one `Apply` = one undo step; `Cancel` changes nothing).
  - **Base-revision moved guard (`RES-05`):** If `candidate.BaseAcceptedId != currentAcceptedId`:
    `BuildGeometryDraft` refuses direct accept (`AcceptEnabled = false`,
    `RefusalReason = "Accept disabled — base revision moved (<baseSurfaceHash12>). Use Rebase to preview over the current revision."`)
    and exposes **`BuildRebasedDraft(candidate, currentAcceptedId, currentSurfaceHash, measuredDeviationMeters)`** which
    opens the draft over the current accepted revision with the measured geometric deviation explicitly reported.

---

## 6. Process-only `"Open in ParaView"` handoff (`ParaViewHandoff`, spec A5.11, UX-06)

- **Zero VTK linking (COMMIT-02, spec A5.11:1156):** `CfdWorkbench.Analysis` and `CfdWorkbench.Desktop` carry zero
  references to VTK, ActiViz, or ParaView native libraries.
- **Probe & injectable path seam:**
  ```csharp
  public interface IParaViewLocator
  {
      string? FindExecutable(string? explicitOverridePath = null);
  }
  ```
  - Default locator checks well-known Windows (`Program Files\ParaView 5.12*\bin\paraview.exe`, `5.13*`, `PATH`) and
    macOS (`/Applications/ParaView-5.12*.app/Contents/MacOS/paraview`, `5.13*`) locations — never executing a shell.
  - When `FindExecutable()` returns `null` (as on this host where `PARAVIEW_FOUND=False`), `ParaViewHandoff.Prepare`
    returns `Available = false` with the exact spec UX-06 honest absence string:
    ```
    ParaView 5.12+ not found — surface floods and forces remain; slices, streamlines and isosurfaces need ParaView (BSD-3) · Open in ParaView disabled
    ```
- **Process-only argv launch:** When available (or given an injected executable path in tests), `ParaViewHandoff.Launch`
  validates that neither the executable path nor the case directory is a symlink/traversal (`M5`), verifies the case
  directory exists, and launches via `ProcessStartInfo` with:
  - `UseShellExecute = false`
  - `CreateNoWindow = true`
  - `ArgumentList.Add(caseTargetPath)` (argv only — zero shell string interpolation, zero `cmd`/`bash`).

---

## 7. 7-Area Left Activity Rail Shell Architecture (B1 / Rulings 271 & 272 / Adversary C4)

### 7.1 `ShellHost` `Auto,*` 3-row Grid geometry (preserving all existing shell invariants)

`ShellHost` keeps its **exact 3 rows** (`RowDefinitions = new RowDefinitions("*,Auto,Auto")`) and adds two columns:
`ColumnDefinitions = new ColumnDefinitions("Auto,*")`.

```
                  Col 0 (Auto: ActivityRail)           Col 1 (*: Workspace & Docks)
               ┌──────────────────────────────┬───────────────────────────────────────────────┐
 [Row 0* opt]  │  MenuBar (Windows only, via ShowMenuBar(): Col=0, ColumnSpan=2, Row=0)       │
               ├──────────────────────────────┼───────────────────────────────────────────────┤
 Row 0 (or 1)  │  ActivityRail                │  DockHost (ModelArea / Section / Source /     │
  Height = *   │  Col=0, Row=GetRow(DockHost) │            SolverSetupDocument + Side Docks)  │
               │  RowSpan=1 (NEVER RowSpan>1) │  Col=1, Row=GetRow(DockHost)                  │
               ├──────────────────────────────┼───────────────────────────────────────────────┤
 Row 1 (or 2)  │  (empty — zero overlap)      │  AnalysisPanel (collapsed outside Analysis)   │
  Height=Auto  │                              │  Col=1, Row=GetRow(DockHost)+1                │
               ├──────────────────────────────┴───────────────────────────────────────────────┤
 [Notice opt]  │  OneDrive NoticeRow (if shown): Col=0, ColumnSpan=2, own exclusive Row       │
               ├──────────────────────────────────────────────────────────────────────────────┤
 Row 2 (or 3)  │  StatusStrip: Col=0, ColumnSpan=2, full window width, 24 px height           │
  Height=Auto  └──────────────────────────────────────────────────────────────────────────────┘
  Overlay: paletteBackdrop spans Col=0, ColumnSpan=2, Row=GetRow(DockHost), RowSpan=remaining
```

**Verified compatibility with all three existing Desktop shell tests (`C4`):**
1. **`Shell_F1_NoSidebarHeaderBand` (`ShellWindowTests.cs:954-972`):** `host.RowDefinitions.Count` remains **exact 3**
   (before `ShowMenuBar()`); `GetRow(DockHost) == 0`, `GetRow(AnalysisPanel) == 1`, `GetRow(StatusStrip) == 2`;
   `ActivityRail` carries no `"Sidebar"` `TextBlock` and does not touch `LeftSidebarToggle`.
2. **`ControllerShellOneDriveTests.NoticeRow` (`ControllerShellOneDriveTests.cs:288-306`):** Because `ActivityRail` sits
   strictly at `Row = GetRow(DockHost)` with `RowSpan = 1`, when OneDrive inserts its notice row directly above
   `StatusStrip`, `host.Children.Count(child => Grid.GetRow(child) == noticeRow)` remains **exact 1**. `ShowMenuBar()`
   shifts `ActivityRail` alongside `DockHost`, `AnalysisPanel`, `StatusStrip` (and any notice row), preserving distinct
   rows before and after `ShowMenuBar()`.
3. **`StatusStrip_SitsAtWindowBottom_FullWidth_24px` (`StatusStripTests.cs:51-61`):** `StatusStrip` sits at
   `(Col=0, ColumnSpan=2)` on the final row, so `origin.X == 0`, `Width == window.ClientSize.Width`, and `Height >= 24`.

### 7.2 `Dock`-free `ActivityRail` & 7-Area Slot Map

- **Zero `Dock.*` references (`Architecture_DockConfinedToShell`):** `src/CfdWorkbench.Desktop/ActivityRail.axaml` and
  `ActivityRail.axaml.cs` have ZERO `Dock.*` imports or symbols. `ActivityRail` exposes pure C# events:
  - `event Action<WorkbenchArea>? AreaSelected`
  - `event Action? ExportRequested`
  - `event Action? SolverSetupRequested`
  All three events are subscribed inside `ShellHost.cs` (inside `Shell/`), satisfying both
  `Architecture_DockConfinedToShell` and `tools/check-event-subscribers.py` (0 unsubscribed events).
- **Zero view rewrites (Rulings 271 & 272):**

| Rail Slot | Area Label | Hosted Surface | Live Readiness Badge (Derived) | Existing View Rewritten? |
|---|---|---|---|---|
| 1 | **Setup** | Existing `SetupWorkspaceView` (`ModelArea.SetupWorkspace`) | `Seeded` / `No brief` | **No** (Ruling 272) |
| 2 | **CAD** | Existing `ModelArea` viewports (`PlanContent`) | `r<n>` / `Start` | **No** |
| 3 | **Analysis** | Existing `ModelArea` Analysis overlays + `AnalysisPanel` | `k-<hash4> Current` / `Historical` / `None` | **No** |
| 4 | **Experiment** | Existing `SweepsWorkspaceView` (`ModelArea.SweepsWorkspace`) | `<n> cases Queued` / `Draft` / `None` | **No** (Ruling 271) |
| 5 | **Run** | New `RunWorkspaceView` (`src/CfdWorkbench.Desktop/Run/`) | `Ready` / `Not ready` / `Running <k>/<n>` | New view |
| 6 | **Results** | New `ResultsWorkspaceView` (`src/CfdWorkbench.Desktop/Results/`) | `<admitted> of <total>` / `None` | New view |
| 7 *(action)* | **Export** | Opens existing `ExportDialog` (`file.export`, Modal: true) | `STL · 3MF · 3DM` | **No** |
| Footer | **Solver setup** | Opens existing non-blocking `SolverSetupDocument` (`solver.setup`) | `Ready` / `Not set up` | **No** |

### 7.3 Synchronized 4-button `ModelArea.Navbar`, `CommandTable`, and Token Discipline

- **`ModelArea.Navbar` Tab order preserved:** `ModelArea.axaml:258-283` keeps its exact 4 area toggle buttons
  (`NavSetupButton`, `NavCadButton`, `NavAnalysisButton`, `NavSweepsButton`) followed by `NavViewsButton`,
  `NavDisplayButton`, `NavFitButton`, `NavFitSelectionButton` — keeping `ModelArea_Navbar_KeyboardPath_TabAfterTheViews`
  100% green. `WorkbenchController` extends its area state to an explicit 6-area enum
  `WorkbenchArea { Setup, Cad, Analysis, Experiment, Run, Results }` (`EnterRun()`, `EnterResults()`, preserving
  `IsSetup`, `IsAnalysis`, `IsSweeps`, `EnterSetup()`, `EnterCad()`, `ToggleAnalysis()`, `EnterSweeps()`). When active
  area is `Run` or `Results`, `ModelArea` displays `RunWorkspace` or `ResultsWorkspace` while unchecking the 4 navbar
  toggles and keeping `ActivityRail` highlighted on slot 5 or 6.
- **`CommandTable.cs` additions:**
  - `new("view.run", "Run", "View", null, false, NoOp, Modal: false)`
  - `new("view.results", "Results", "View", null, false, NoOp, Modal: false)`
  Both live under `"View"` with `Modal: false` (safe for `Commands_ModalFlag_MatchesRowsThatShowADialog`), wired in
  `ShellHost.cs` and `ShellModelTests.cs`.
- **Token discipline (`Styles.axaml`, `UI-CS-TOKEN`, `xaml-token-lint`):**
  - **ZERO** new `<SolidColorBrush x:Key="...Brush">` keys added inside `Light`/`Dark`/`HighContrast`
    `ThemeDictionaries` in `Styles.axaml`. All controls reuse existing theme brush keys (`CanvasBrush`, `SurfaceBrush`,
    `SurfaceSoftBrush`, `InkBrush`, `MutedBrush`, `LineBrush`, `PrimaryBrush`, `OnPrimaryBrush`, `DangerBrush`,
    `WarningBrush`, `SelectionBrush`, `ViewportBrush`, `ViewportInkBrush`, `Batlow0..4Brush`, `VikNeg2..Pos2Brush`).
  - Only non-theme numeric/thickness/radius layout tokens (e.g., `ActivityRailWidth`, `ActivityRailItemPadding`,
    `ResultsFieldCanvasMinHeight`, `ProvenanceStripPadding`) may be added to `Styles.axaml` outside `ThemeDictionaries`.
  - `ResultsFieldCanvas.cs` draws surface $C_p$/$C_f$ scalar floods + isolines over `Placement.Surface` (honoring
    ADR-0010 §Decision (4): displays receive geometry, apply only camera + `Point3.Port()`), resolves every brush from
    application resources (ZERO `Color.Parse`), and uses `PropFontFamily`/`PropFontSize` ($\ge 11$ px; `UI-CS-TOKEN` 0
    findings).

---

## 8. Copy reservation (`COPY-806..865`, proposed for `W10-RAIL-UI`)

`W10-DESIGN` does NOT modify `DESIGN.md`. The rows below reserve `COPY-806..865` (starting immediately after approved
`COPY-805` from Ruling 275) for `W10-RAIL-UI` to register as `proposed` in `DESIGN.md`. Every string is `LAB-01` clean
(zero forbidden words without an A7 grant, zero raw hexes, zero commands to type).

| Range | Surface | Proposed Strings (Summary) |
|---|---|---|
| `COPY-806..815` | 7-Area `ActivityRail` | Area titles (`Setup`, `CAD`, `Analysis`, `Experiment`, `Run`, `Results`, `Export`, `Solver setup`) and derived readiness badges (`Seeded`, `No brief`, `Ready`, `Not ready`, `<admitted> of <total>`, `Not set up`) |
| `COPY-816..832` | `RunWorkspaceView` (Area 5) | Queue heading, 11 state labels (`Pending`, `Unsupported`, `Readiness`, `Meshing`, `Mesh gate`, `Solving`, `Harvesting`, `Completed`, `Completed — residual criterion not met (ran to limit)`, `Failed`, `Cancelled`), `MeshGate` breakdown headings (`Non-orthogonality`, `Internal skewness`, `Boundary skewness`, `Cell determinant (layer/core)`, `Layer coverage`, `y+ (p95 / max)`), screen-only disclaimer, actions (`Run`, `Cancel`, `Retry sample`, `Set up a solver`), offline AI-01/AI-11 headings (`Explain this failure`, `Preview case diff`, no-key reassurance), `Converged` three-part template (`Converged: residuals <= <r> · outputs complete · grid uncertainty not quantified (single mesh)`) |
| `COPY-833..852` | `ResultsWorkspaceView` (Area 6) | Sample picker, layer toggles (`Surface Cp`, `Surface Cf`, `Velocity slice`, `3D streamlines`, `Force vectors`, `Spanwise loading`, `Vortex-core candidates`, `Separation`), VIZ basis strings (`"instantaneous integral curves of the mean field (steady RANS)"`, `"modeled turbulent kinetic energy, m²/s², model, mean-flow basis"`, `"affordance, not fluid motion"`, `"vortex-core candidates at threshold <t>"`, `"No supported criterion in this sample"`), 9-field `ProvenanceStrip` headers + `Table twin` toggle |
| `COPY-853..865` | Replay, Difference, Candidates & ParaView | `"Parametric sweep — sequence over admitted samples"`, `Play` / `Step back` / `Step forward` / `Hold speed` / `Hold angle` / `Lock range` / `"Range unlocked — per-sample scale"`, failed-sample pause banner, `"Unavailable — mesh differs"`, `Candidate ladder` / `Pareto` / `Accept as draft` / `Rebase` / base-moved notice / zero-feasible terminal notice, `"Open in ParaView"` and exact UX-06 absence string (`"ParaView 5.12+ not found — surface floods and forces remain; slices, streamlines and isosurfaces need ParaView (BSD-3) · Open in ParaView disabled"`) |

---

## 9. Red-first test plan & live mutants for `W10-RESULTS` and `W10-RAIL-UI`

### 9.1 `W10-RESULTS` (Wave 10C)

| Test File & Harness | Test Name | Proves | Live Mutant That Must Fail It |
|---|---|---|---|
| `Core.Tests/ResultsDomainTests.cs` (`IdentityTests.cs`) | `FieldEvidenceManifest_SeparationStrictlyGatedOnWallTauW` | Separation enabled ONLY when `hasWallShearStress == true` + named criterion; absent $\tau_w$ returns `"No supported criterion in this sample"`; Q/$\lambda_2$ strictly labelled `"vortex-core candidates at threshold t"` and never enables Separation | Allow Q/$\lambda_2$ to enable Separation or omit criterion → test fails |
| `Core.Tests/ResultsDomainTests.cs` | `ProvenanceStrip_AllNineFields_And_TableTwinParity` | Every view emits all 9 non-empty provenance fields (unmeasured → `"Not recorded"`); `TableTwin` rows match scalar array values without zero-substitution | Omit `ColormapNameAndVersion` or substitute `0.0` for missing value → test fails |
| `Core.Tests/ResultsDomainTests.cs` | `ReplaySequence_FixedRange_FailedSamplePausesAndClears_ZeroInterpolation` | Admitted samples of one compatible series lock scalar range & camera; unlock toggles label; stepping onto failed sample sets `IsPlaying=false` and clears `ActiveFieldData`/`ActiveMetrics`; differing `meshHash` rejected from series; reduced motion disables autoplay while keeping stepping | Keep prior frame metrics on failed sample or allow mixed `meshHash` → test fails |
| `Core.Tests/ResultsDomainTests.cs` | `DifferenceFlood_ZeroPinned_And_MeshHashMismatchUnavailable` | Matching `meshHash` computes exact `target - base` with symmetric `[-absMax, +absMax]` `vik` range; mismatched `meshHash` strictly returns `"Unavailable — mesh differs"` and empty delta array | Compute element-wise difference when `meshHash` differs → test fails |
| `Core.Tests/CandidateLadderTests.cs` (`IdentityTests.cs`) | `CandidateLadder_PromotionEvidence_And_BuildGeometryDraftRebaseGuard` | Promotion to `cfd-checked` requires every multipoint point + `DiscrepancyRecord` (missing 1 point refused naming point); `BuildGeometryDraft` emits typed CAD draft (never mutates geometry); moved `currentAcceptedId` disables direct accept and requires `BuildRebasedDraft` with deviation | Allow promotion with 1 missing point or allow direct accept on moved base → test fails |
| `Analysis.Tests/ResultsServiceTests.cs` (`AnalysisChecks.cs`) | `ResultsService_UnifiesLocalVlmAndCfd_NoCrossTierAveraging` | Projects surface $C_p$/$C_f$ over `Placement.Surface`, sweep small multiples, difference floods, and candidate ladder without averaging VLM + CFD | Average VLM and CFD `CL` into one number → test fails |
| `Analysis.Tests/ResultsServiceTests.cs` | `ParaViewHandoff_HonestAbsence_And_InjectedProcessOnlyLaunch` | Absent ParaView returns exact UX-06 absence string; injected valid executable path invokes argv-only `ProcessStartInfo` (`UseShellExecute=false`, `CreateNoWindow=true`) with zero VTK references; symlink path refused | Set `UseShellExecute = true` or swallow missing ParaView silently → test fails |
| `Cli.Tests/ResultsCliTests.cs` (`CliTests.cs`) | `Cli_Results_InspectCandidatesParaView_JsonParity` | `results inspect`, `results candidates`, `results paraview --json` match `ResultsProjection` & `ParaViewHandoff` | Omit 9th provenance field or alter absence string in CLI JSON → test fails |

### 9.2 `W10-RAIL-UI` (Wave 10D)

| Test File & Harness | Test Name | Proves | Live Mutant That Must Fail It |
|---|---|---|---|
| `Desktop.Tests/Wave10UiTests.cs` (`WorkbenchTests.cs` `--wave10-ui`, with `DesktopChecks.Spawn`, `ScaleModes`, and `DesktopChecks.PrintScaleContext("wave10-ui")`) | `ActivityRail_SevenAreas_LiveBadges_DockingZeroRewrite` | All 6 areas (`Setup · CAD · Analysis · Experiment · Run · Results`) + `Export` + `Solver setup` render in `ShellHost` `Col=0`, switch smoothly to existing `SetupWorkspaceView`/`SweepsWorkspaceView` and new `RunWorkspaceView`/`ResultsWorkspaceView`, and synchronize `ModelArea.Navbar` 4 buttons | Break navbar sync or hide `SetupWorkspaceView` → test fails |
| `Desktop.Tests/Wave10UiTests.cs` | `RunWorkspaceView_ElevenStates_MeshGate_And_OfflineFailureExplain` | Renders 11-state status, `MeshGate` breakdown, residuals/forces, Cancel/Retry, `Set up a solver` jump to `SolverSetupDocument`, and offline `Explain this failure` | Drop `MeshGate` threshold display or break `Set up a solver` jump → test fails |
| `Desktop.Tests/Wave10UiTests.cs` | `ResultsWorkspaceView_TauWSeparation_ReplayPauseClear_DifferenceFlood_CandidateDraft` | Separation toggle disabled when $\tau_w$ absent; `ResultsFieldCanvas` paints with `PropFontSize >= 11` and theme brushes (0 `Color.Parse`); Replay pauses & clears on failed sample; Difference flood shows `"Unavailable — mesh differs"`; `Accept as draft` jumps to CAD draft; `"Open in ParaView"` shows honest absence string | Enable Separation without $\tau_w$ or leave stale metrics on failed frame → test fails |
| Existing `ShellWindowTests.cs`, `StatusStripTests.cs`, `ControllerShellOneDriveTests.cs`, `ControllerViewTests.cs`, `ShellModelTests.cs`, `ControllerSectionTests.cs`, `TokenDisciplineTests.cs` | `Shell_F1_NoSidebarHeaderBand`, `StatusStrip_SitsAtWindowBottom_FullWidth_24px`, `NoticeRow`, `Architecture_DockConfinedToShell`, `ModelArea_Navbar_KeyboardPath_TabAfterTheViews`, `Commands_ModalFlag_MatchesRowsThatShowADialog`, `UI-CS-TOKEN` | Zero regressions across shell rows, full-width 24 px status strip, OneDrive notice exclusivity, `Dock`-free `ActivityRail.axaml.cs`, 4-button navbar Tab order, `view.run`/`view.results` `Modal: false` parity, zero new theme `...Brush` keys, and zero `UI-CS-TOKEN` / `xaml-token-lint` / `check-event-subscribers` findings | Add `Dock.` to `ActivityRail.axaml.cs`, add `RowSpan=2`, add a theme brush key, or alter navbar Tab order → gate fails |
