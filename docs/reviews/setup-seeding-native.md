---
id: review-setup-seeding-native
title: "Native implementation proof pack: Area 1 Setup & Goal State (SET-BUILD)"
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: implementation — Area 1 (Wave 6 SET-BUILD, Ruling 272)
tags: [setup, goal-state, seeding, recipe, soft-targets, presets, class-rules, drc, foildsl, native, proof-pack, wave6]
links:
  - { to: design-setup-seeding, rel: implements }
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: adr-foildsl-authority, rel: implements }
  - { to: adr-0011-analysis-run-storage, rel: implements }
  - { to: design-language, rel: depends-on }
  - { to: rulings, rel: depends-on }
  - { to: coordination-wave6-and-area5-wsl, rel: implements }
review-by: "2027-02-22"
summary: >-
  Implementation proof pack for Wave 6 Track SET-BUILD (Nodes SET-1..4) authorized by Ruling 272:
  additive cfdw-project-2 setup persistence (SetupBriefVersion, GoalStateVersion, DrcExclusion) with
  BLAKE3(JCS) integrity and live AuthoringSession.CurrentGoalSweepSnapshot() seam into Area 4 Sweeps,
  exact 2D deterministic soft-target RecipeSeeder over kappa(lambda) = (3+lambda)/4 honoring
  TipChord.MinimumMeters (Ruling 93) and one-way FoilDSL 4.1 emission (GEO-04), all 9 discipline presets
  with wakefoil-to-surf Flagged disclosure and GEN catalog fallbacks, class-rule DRC (GWA/iQFOiL/Formula
  Kite/open class) and BLAKE3-pinned SanityBounds.json advisory bounds, exact GOAL-02 7-point + 0.3 m
  GoalFeasibility evaluation with honest Unavailable polar cells, CLI seed & inspect --json hash parity,
  and non-modal Desktop Setup workspace (Setup | CAD | Analysis | Sweeps) covering all 6 hard states,
  no-key AI-02 slot (COPY-53), DESIGN.md COPY-728..749, and T1..T14 red-first verification.
---

# Native implementation proof pack: Area 1 Setup & Goal State (`SET-BUILD`)

**Scope & Authorization.** Wave 6 Track `SET-BUILD` (`docs/coordination/wave6-and-area5-wsl.md`), authorized by
**Ruling 272** (`DR-SET-1 (a)`, `DR-SET-2 (a)` coordinated with Ruling 271 `DR-SWP-4 (b)`, `DR-SET-3 (a)`,
`DR-SET-4 (a)`, `DR-SET-5 (a)`). Delivered across four sequential green nodes (`SET-1..4`).

## 1. Delivered surfaces (E7 traceability)

| Node | Files authored / edited | Ruling 272 contract satisfied |
|---|---|---|
| **SET-1** Core records, exact 2D solver, presets & store | `src/CfdWorkbench.Core/{SetupRecords,DisciplinePresets,RecipeSeeder}.cs`, `RunRecord.cs`, `AuthoringSession.cs`, `tests/CfdWorkbench.Core.Tests/{SetupSeedingTests,IdentityTests,SweepStoreTests}.cs` | `DR-SET-1 (a)` additive optional `setup` member on `cfdw-project-2` (omitted when empty so `-1` and pre-SET `-2` files remain byte-identical), widened `NativeProject.Read` `Exact` optional list to `["analysis", "setup", "experiments"]`, BLAKE3(JCS) content hashes (`SetupStore.Check`), `"seed"` `EditReceipt` validation, live `AuthoringSession.CurrentGoalSweepSnapshot()` seam into Area 4 Sweeps (zero edits to `Desktop/Sweeps/**`); `DR-SET-3 (a)` one-sided ceilings for `max span` / `max chord`, two-sided for `target area` / `target AR`, lexicographic `(J1, J2, J3)` finite-candidate solver over `κ(λ) = (3 + λ) / 4` (zero `Math.Pow`, Ruling 157), `TipChord.MinimumMeters` clamp (Ruling 93), one-way FoilDSL 4.1 emission, derived `RecipeStatus` with zero shadow flag (`GEO-04`); `DR-SET-4 (a)` all 9 discipline presets, `wakefoil → surf` Flagged disclosure, GEN fallback naming over `CatalogData/catalog.tsv`, and mandatory `SETUP-FLAGGED-UNACK` gate. |
| **SET-2** Class rules, sanity bounds & Goal feasibility | `src/CfdWorkbench.Core/{ClassRules,SanityBounds}.cs`, `src/CfdWorkbench.Core/CatalogData/SanityBounds.json`, `src/CfdWorkbench.Analysis/{GoalFeasibility,Labels}.cs`, `tests/CfdWorkbench.Core.Tests/SetupDrcTests.cs`, `tests/CfdWorkbench.Analysis.Tests/{SetupFeasibilityTests,AnalysisChecks}.cs` | `CAT-04` pure `ClassRules.Evaluate` for `gwa-race-2022`, `iqfoil-2025`, `formula-kite-2024`, and `open-class` with exclusion join; `ANA-20` BLAKE3-pinned `SanityBounds.json` (`263dcef369f90336e79c3fafd5c141792b82e5e9abfa3829cbdfa2a1381063e6`) advisory `COPY-49` bounds; `GOAL-02` `GoalFeasibility.Evaluate` reusing `OperatingPoints.Derive` (`DM7`) across all 7 points + `h_ref = 0.3 m` with honest `Unavailable — polar data not computed for this section` in polar-dependent cells (`DR-SET-4 (a)`); `COPY-728..749` constants in `Labels.cs`. |
| **SET-3** CLI parity | `src/CfdWorkbench.Cli/{SeedVerb,Program}.cs`, `tests/CfdWorkbench.Cli.Tests/{SetupCliTests,CliTests}.cs` | `CLI-01` `cfd-workbench seed --preset ... [--acknowledge-flagged] [--out <file.cfdw.json>]` and additive `setup` projection in `inspect <file.cfdw.json> --json`, matching GUI `SurfaceHash` and `GoalStateVersion.ContentHash` bit-for-bit. |
| **SET-4** Desktop UI, copy & proof pack | `src/CfdWorkbench.Desktop/Setup/{SetupController,PlanformPreviewControl,SetupWorkspaceView.axaml(.cs)}`, `Styles.axaml`, `WorkbenchController.cs`, `ModelArea.axaml(.cs)`, `Shell/{StatusStrip.axaml(.cs),CommandTable.cs,ShellHost.cs}`, `tests/CfdWorkbench.Desktop.Tests/{SetupViewTests,WorkbenchTests,ShellModelTests}.cs`, `DESIGN.md` (`COPY-728..749`), `docs/reviews/setup-seeding-native.{md,html}` | `DR-SET-5 (a)` non-modal `Setup` first segment in `ModelArea` navbar (`Setup | CAD | Analysis | Sweeps`) + `view.setup` Command Palette / View-menu row preserving CAD selection & camera; all 6 hard states (`clean-race`, `flagged-gate`, `conflict-targets`, `class-violation`, `reseed-edited`, `no-key-language`), live Graphite `PlanformPreviewControl`, `AI-02` no-key slot (`COPY-53`, `AI-01`), live `StatusStrip` recipe status, Light/Dark/High Contrast themes, and 0 `LAB-01` forbidden words. |

## 2. Red-first test matrix (`T1`–`T14`)

All 15 specification tests from `docs/design/setup-seeding.md` §7 (plus companion DRC/water/theme checks) were verified:

| Test ID | Harness test name | Project | Verified contract & mutant killed |
|---|---|---|---|
| T1 | `Seeder_PrimaryRaceFixture_ExactTargetsAndCertified` | Core | `(1.10 m, 120 mm, 1000 cm², AR 12, λ 0.24)` matches `WingEstimates.From` to `1e-12`, `GeometryStatus.Certified`. Observed RED on `κ = 1.0` mutant (`actual area 0.123456`). |
| T2 | `Seeder_ConflictingTriple_WeightSpanOne_ExactSpanAndConflictNotice` | Core | `(1000 cm², 0.80 m, 100 mm)` with `w = (1,0,0,0)` hits `b = 0.800000 m` exact, `S = 648 cm²`, `COPY-73` populated, `Certified`; tight-span fixture yields `AR 11.60`. Observed RED without `J2` tie-breaker. |
| T2b | `Seeder_StationaryFamilies_BeatsDenseGridOracle` | Core | 200 adversarial target/weight trials satisfy `J1 <= J1_grid + 1e-9` and emitted `WingEstimates` matches solver to `1e-12` relative. Observed RED on stub solver (`J1 = 0.2596 > 0`). |
| T3 | `Seeder_Geo01_AreaAndAr_ExactSqrtSpan` | Core | `S = 0.14 m², AR = 7.0, λ = 0.30` yields `b = √0.98 m` within `1e-12` relative on `WingEstimates.From`. Observed RED before exact solver. |
| T4 | `Seeder_TipChordMinimum_NeverViolated` | Core | Extreme small targets clamp `c_tip >= TipChord.MinimumMeters(c_root)` and commit via `AuthoringSession.Seed` without `DSL-TIP-CHORD-MIN`. Observed RED before `0.005 / λ` lower bound. |
| T5 | `Seeder_SameInputsFormAndProposalFixture_EqualSurfaceAndGoalHash` | Core | Parameter form brief and recorded `language` proposal fixture with identical physical fields yield identical `SurfaceHash` and `GoalStateVersion.ContentHash`, differing only in `entryKind` and provenance. |
| T6 | `Session_ReseedOnEditedDoc_PreservesHistoryAndMarksRunsHistorical` | Core | Seed `r1` → span edit `r2` → VLM run → re-seed `r3`: `accepted.Count == 3`, `briefs.Count == 2`, run surface differs (`Historical`), Undo restores `r2` (`Current` again) while `briefs[]` remains append-only. |
| T7 | `Session_FirstManualEdit_DetachesRecipeAndUndoRelinks` | Core | Seed → `Linked`; first manual edit → `DirectParametric`; Undo → `Linked` again (derived purely from accepted parent chain, zero shadow flag). |
| T8 | `Presets_AllNineDisciplines_CertifiedNoErrorsAndGenFallbackNamed` | Core | All 9 presets refuse unacknowledged Flagged defaults (`SETUP-FLAGGED-UNACK`), certify with acknowledgement, disclose pending/LINK GEN fallbacks, and map `wakefoil → surf` with mandatory Flagged wake note. |
| T9 | `ClassRules_GwaIqfoilFormulaKiteOpen_ExactFindings` (+ `SanityBounds_ProductEnvelope_ExactAdvisoryFindingsAndHashCheck`) | Core | GWA `650 cm²` + `120 cm` mast emit located warnings with version `GWA Race Box v2022-02-01` & date `2022-02-01`, join exclusions; iQFOiL `900 cm²` & `−2°…+1°`; Formula Kite `±2 mm`, `−1 mm`; Open class; `SanityBounds.json` BLAKE3 check (observed RED before hash pin) + `COPY-49` advisories. |
| T10 | `GoalState_EditConstraint_AppendsVersionNeverEditsGeometry` | Core | Editing `c-cav` appends `goalStates[1]` (`editedByUser = true`), leaves `accepted.Count` and `SurfaceHash` unchanged, updates live `CurrentGoalSweepSnapshot()`, and preserves both `goalStates[0..1]` across save/reopen. |
| T11 | `GoalFeasibility_Goal02_SevenPointsAndShallowDepth_ExactOracle` (+ `GoalFeasibility_WaterAlignment_CoreMatchesAnalysisWaterTable`) | Analysis | All 7 `GOAL-02` points + `h_ref = 0.3 m` match exact `CL` (3 dec) and `σ` (2 dec) oracle (`0.773 / 7.71`, `0.773 / 7.56`, `0.395 / 3.93`, `0.239 / 2.38`, `0.458 / 3.43`, `0.213 / 1.59`, `0.132 / 0.98`, `0.101 / 0.75`); polar-dependent cells read `Unavailable — polar data not computed for this section`; Core default water matches `WaterTable.At(15, ...)` bit-for-bit. |
| T12 | `NativeProject_SetupRoundTrip_ByteEqual_AndPreSetReaderFailsClosed` | Core | Unseeded `-1` omits `"setup"` byte-identically; seeded `-2` round-trips byte-equal with `DrcExclusion`; tampered `GoalStateVersion.ContentHash` or field refuses `DOC-INTEGRITY`; unknown top-level field refuses `DOC-UNSUPPORTED-FIELD`. |
| T13 | `Cli_SeedAndInspect_AgreesWithGuiBySurfaceAndGoalHash` | CLI | `cfd-workbench seed --preset wingfoil-race --acknowledge-flagged --out` and `inspect --json` match GUI `SurfaceHash` and `GoalStateVersion.ContentHash` bit-for-bit; unacknowledged CLI seed refuses `SETUP-FLAGGED-UNACK` (exit 2). |
| T14 | `Desktop_SetupHardStates_NoKeyAndLab01Clean` (+ `Desktop_SetupThemes_LightDarkHighContrast_AndCadSelectionPreserved`) | Desktop | All 6 hard states rendered in real Avalonia window; no-key `AI-02` prompt disabled with `COPY-53` while parameter Seed stays active; live Area 4 `Sweeps.GoalSnapshot` populated after Seed; Light/Dark/High Contrast themes clean; CAD selection preserved across `Setup ↔ CAD`; 0 `LAB-01` hits. |

## 3. Verification gates & evidence

- Full repository suite (`bash tools/run-tests.sh`, `TESTS-GREEN 364 s`, exit 0):
  - `CfdWorkbench.Core.Tests`: **974 passed**, 7 skipped, 0 failed (exit 0)
  - `CfdWorkbench.Analysis.Tests`: **326 passed**, 1 skipped, 0 failed (exit 0)
  - `CfdWorkbench.Desktop.Tests`: **936 passed**, 1 skipped, 0 failed (exit 0)
  - `CfdWorkbench.Cli.Tests`: **38 passed**, 1 skipped, 0 failed (exit 0)
  - `Python unittest`: **10 ran**, 0 failed (exit 0)
- Static & repo gates (all exit 0): `xaml-token-lint.py` (0 findings), `check-event-subscribers.py` (38 events, 4 allowed, 0 findings), `design-lint.py DESIGN.md --strict` (0 warnings), `check-test-paths.py` (0 relative repo reads), `check-reader-sharing.py` (0 findings), `check-proof-pii.py` (0 hits), `verify-ruling-citations.py` (272 cited / 272 defined), `tools/check-docs.py` (green).
- Scope & boundary hygiene: zero edits under `src/CfdWorkbench.Desktop/Sweeps/**`, `docs/notes/rulings.md`, or `tools/**`. Area 4 Sweeps receives live Goal states purely via `AuthoringSession.CurrentGoalSweepSnapshot()` in Core.
