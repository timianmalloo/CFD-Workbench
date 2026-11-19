---
id: design-setup-seeding
title: "Design: Area 1 — Setup & Goal State (offline parameter/preset seeding, soft-target solver, class rules, Goal state persistence, and no-key language slot)"
type: design
status: proposed
owner: "@timianmalloo"
phase: design — documents and mockup only; no build until operator approves mockup and DR-SET-1..5
tags: [setup, goal-state, seeding, recipe, soft-targets, presets, class-rules, drc, foildsl, persistence, data-model, wave5]
links:
  - { to: spec-cfd-workbench-v1, rel: implements }
  - { to: spec-amendment-setup-seeding, rel: implements }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: adr-application-project-contract, rel: depends-on }
  - { to: adr-0011-analysis-run-storage, rel: depends-on }
  - { to: kb-hw-hydrofoil-disciplines-and-design-data, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: mockup-setup-seeding, rel: tested-by }
  - { to: defect-classes, rel: relates-to }
  - { to: rulings, rel: depends-on }
review-by: 2026-12-19
summary: >-
  Build-ready /design-slice for Area 1 (Setup & Goal State: SET-01, SET-03, SET-04, GEO-01, GEO-04, CAT-03, CAT-04,
  GOAL-01, GOAL-02, plus AI-01/AI-02 no-key language slot). Defines the append-only SetupBriefVersion and
  GoalStateVersion data model inside NativeProject, aligns the immutable GoalStateVersion operating-point schema with
  Area 4 local sweeps (SWP), proves the exact 2D deterministic soft-target solver subject to TipChord.MinimumMeters
  (Ruling 93) and FoilDSL 4.1 one-way emission, specifies all nine discipline presets (8->9 purpose mapping, GEN
  fallbacks over CatalogData/catalog.tsv, Flagged acknowledgement gate, GWA/iQFOiL/Formula Kite/open-class DRC,
  sanity bounds, and verified GOAL-02 7-point arithmetic), and gives the E7 surface list, telemetry, red-first test
  plan with mutants, build slices, and five Decision Requests (DR-SET-1..5).
---

# Design: Area 1 — Setup & Goal State

**Goal state.**
- **Goal:** Deliver a complete, deterministic, offline-capable design for Area 1 (Setup & Goal State) covering parameter and discipline-preset seeding (`SET-01`, `SET-03`, `SET-04`, `GEO-01`, `GEO-04`, `CAT-03`, `CAT-04`), Goal state authoring and feasibility arithmetic (`GOAL-01`, `GOAL-02`, `GOAL-03`), class-rule and sanity-bound DRC, and the `AI-02` language entry slot in its honest no-key state (`AI-01`).
- **Done when:** This design, its rendered HTML view, its self-contained interactive HTML mockup (`docs/mockups/setup-seeding.html`), and its spec amendment note (`docs/specs/amendments/spec-setup-seeding.md`) pass independent Adversary review (`w5-verifier`) and `tools/check-docs.py`, and are handed to the operator for approval of `DR-SET-1..5` and the mockup before any code track opens.
- **Not in scope:** Live LLM/API calls for `SET-02` / `AI-02` (provider SDK and eval suite remain a separate Assistant track); modifying `src/`, `tests/`, or `DESIGN.md`; polar-method installation for Eppler/VEND sections; structural, trim, or free-surface simulation.
- **Tier:** T2 (persisted native document schema, FoilDSL 4.1 emission, and cross-area Goal state contract consumed by Area 3 Analysis and Area 4 Experiment Setup).

---

## 0. Evidence ledger (verified from repository source)

Every type, constant, file path, and number below was opened directly in this checkout or verified by execution on 2026-11-19.

| Fact | Verified observation | Source | Confidence |
|---|---|---|---|
| FoilDSL 4.1 emitter & default wing | `FoilSource.NewDefault()` emits `foildsl "4.1"`, `evaluator "cfdw-cv" "2"`, `symmetry mirror_y`, `tip open`, 4-CV degree-3 `TipClusteredChannel` rails, 10-CV degree-3 dihedral/twist/thickness, 10-CV degree-5 sqrt-basis profile, exact binary64 decimals via `ExactDecimal`. | `src/CfdWorkbench.Core/FoilSource.cs:240-305, 628-830` | **Verified** |
| Minimum tip chord (Ruling 93) | `TipChord.MinimumMeters(rootChord) = max(0.005, 0.02 * rootChord)`, refusal code `DSL-TIP-CHORD-MIN`; enforced in `AuthoringSession.Commit` (`RequireTipChord`). | `src/CfdWorkbench.Core/TipChord.cs:10-33`, `AuthoringSession.cs:629` | **Verified** |
| Certificate bounds | `Geometry.TwistDomainDegrees = 57.295779513082323`, `Geometry.ThicknessDomain = (1e-7, 1 - 1e-7)`. | `src/CfdWorkbench.Core/Geometry.cs:96-99` | **Verified** |
| Admitted GEN catalog profiles today | Only 3 rows are generated and selectable: `naca-0009`, `naca-0012`, `naca-4412` (`CatalogGenerator.Id = "naca4-closed/2"`). Seven NACA 16/63/64/66 rows are `Not generated in this build`; eight Eppler rows (`e817..e908`) are `Pending admission`; `speer-h105` is `Cite only (LINK)`. | `src/CfdWorkbench.Core/CatalogData/catalog.tsv:1-20`, `Catalog.cs:70-137` | **Verified** |
| Native project envelope & format rule | `Envelope` has optional `[JsonIgnore(WhenWritingNull)] AnalysisRecords? Analysis`. `NativeProject.MaxBytes = 32_000_000` (Ruling 263), line cap `4096` bytes. `NativeProject.Read` calls `Exact(root, [...], ["analysis"])` and requires `format == RunRecord.Format(runs.Length)` (`cfdw-project-1` for 0 runs, `cfdw-project-2` for >= 1). | `src/CfdWorkbench.Core/AuthoringSession.cs:22-28, 2565-2662`, `RunRecord.cs:225-229` | **Verified** |
| `OperatingPoint` & `WaterRecord` in Core | `WaterRecord(TemperatureC, SalinityGPerKg, Rho, Nu, Pv, Source, TableHash)`; `OperatingPoint(Speed, PAtm, HRef, Datum, AlphaDeg, Load)` with explicit `assume:` at `RunRecord.cs:72-75` awaiting Area 1 confirmation of `Load`; `OperatingPoints.RootLeadingEdge = "root LE"`, `SaltSalinityGPerKg = 35.16504`, `OperatingPoints.Validate` currently requires `op.Load is null`. | `src/CfdWorkbench.Core/RunRecord.cs:67-76`, `src/CfdWorkbench.Analysis/OperatingPoint.cs:31-62` | **Verified** |
| ITTC water table | `WaterTable.At(temperatureC, salinityGPerKg)` hashes embedded `IttcWater.csv` against pinned BLAKE3 `fca86591...154480`; valid 0–50 °C and 0–35.16504 g/kg; refuses out of range with `ANA-INPUT-WATER` and `COPY-52`. | `src/CfdWorkbench.Analysis/WaterTable.cs:11-62` | **Verified** |
| Wing estimates | `WingEstimates.From(source, basis, generation)` derives span, root chord, tip chord, mean chord `S/b`, MAC, max `t/c`, projected AR `b²/S`, area `S` by Gauss–Legendre / adaptive quadrature; never stored. | `src/CfdWorkbench.Core/WingEstimates.cs:12-48` | **Verified** |
| Profile provenance | `Provenance.Parse` parses `gen:<id>`, `vend:<id>`, `dat:sha256:<hex>` plus optional ` modified`; rights class and chip text are derived. | `src/CfdWorkbench.Core/Provenance.cs:15-55` | **Verified** |
| CLI verbs today | `inspect <...|path> --json`, `inspect <path> --runs`, `analyse ... --op <json>`, `export ...`. | `src/CfdWorkbench.Cli/Program.cs:49-63` | **Verified** |
| Copy high-water & LAB-01 | `DESIGN.md` §7 high-water is `COPY-680` (`CLN` reserves `681..685`, `CF` reserves `686..705`). Forbidden product-copy words (`LAB-01`): `cavitation-free`, `ventilation-safe`, `validated`, `optimized`, `best`, `recommended`, `certified`. | `DESIGN.md:347-982`, `docs/specs/cfd-workbench-v1.md:206-207, 1229` | **Verified** |
| GOAL-02 7-point arithmetic | Verified by Python execution (2026-11-19): all 7 points + `h_ref = 0.3 m` match `GOAL-02` to 3 decimals in `CL` and 2 decimals in `σ` for both `p_v = 1.671 kPa` and `1.6709 kPa`. | §4.5 below; `docs/specs/cfd-workbench-v1.md:1191` | **Verified by execution** |
| SET-01 / GEO-01 / 9-preset solver | Verified by Python execution (2026-11-19): primary fixture, tight-span compromise, conflicting triple (`w_b = 1` → `b = 0.800000 m`), `GEO-01` (`b = 0.989949 m`), and all 9 discipline presets satisfy `TipChord.MinimumMeters` and A5.5 sanity bounds. | §3 and §4.1 below | **Verified by execution** |

---

## 1. End-to-end surface list (E7)

Every datum authored or derived in Area 1 travels one path with one owner per concept:

```
NativeProject store (Envelope.setup — append-only briefs & goalStates)
  │
  ▼
CfdWorkbench.Core (SetupRecords · DisciplinePresets · RecipeSeeder · ClassRules · SanityBounds · AuthoringSession)
  │  ├── emits one-way FoilDSL 4.1 bytes → FoilSource.Parse → Geometry.Assess (Certified) → AcceptedRow (edit.rail = "seed")
  │  └── appends SetupBriefVersion + GoalStateVersion (BLAKE3(JCS) content-hashed, immutable)
  │
  ├──► CfdWorkbench.Analysis (GoalFeasibility · WaterTable.At · OperatingPoints.Derive · OperatingPoints.Validate [Load])
  │      ├── computes Feasibility matrix (constraint × operating point, both Ncrit, tier chip)
  │      └── exposes immutable GoalStateVersion (operatingPoints, water, speedBand, depthBand) to Area 4 Sweeps (SWP)
  │
  ├──► CfdWorkbench.Cli (`cfd-workbench seed` · `cfd-workbench inspect <file.cfdw.json> --json`)
  │      └── headless parameter/preset seeding + JSON projection matching GUI by SurfaceHash & GoalStateHash (CLI-01)
  │
  └──► CfdWorkbench.Desktop (SetupView · GoalStateEditor · SeedPreviewPanel · ChecksDrawer · StatusStrip)
         ├── Parameter & preset form + soft-target weights + Flagged acknowledgement gate
         ├── Live planform preview SVG (accepted vs candidate diff) + deviation badges + WingEstimates
         ├── Class-rule & sanity-bound DRC findings (Checks drawer + inline located jump)
         └── AI-02 language slot ("Describe a starting design") in no-key state (COPY-53)
```

**Reader/writer separation (DM15):**
- **Writers:** `AuthoringSession.Seed` (sole writer of `SetupBriefVersion`, seeded `GoalStateVersion`, and `"seed"` `AcceptedRow`), `AuthoringSession.UpdateGoalState` (sole writer of user-edited `GoalStateVersion` — never appends an `AcceptedRow`, `GOAL-03`), `AuthoringSession.RecordDrcExclusion` (sole writer of DRC exclusion facts keyed to `(designId, ruleId, location)`).
- **Readers:** `WingEstimates.From` (derived geometry readouts), `RecipeProvenance.Status` (derived `"Linked"` vs `"Direct parametric"`), `ClassRules.Evaluate` / `SanityBounds.Evaluate` (pure DRC evaluators), `GoalFeasibility.Evaluate` (Analysis matrix), Area 4 `SWP` sweep builder (reads pinned `(goalStateId, contentHash)`), CLI `inspect --json`, and Desktop `SetupView`.

---

## 2. Domain & data model (DM1–DM18)

### 2.1 Bounded contexts and ubiquitous language

Area 1 spans four A3.1 bounded contexts with strict identity references across boundaries:
- **Design:** owns `SetupBriefVersion`, `RecipeRecord`, and the seeded `DesignRevision` / `SurfaceRevision` (FoilDSL 4.1 bytes).
- **Analysis:** owns `GoalStateVersion` (referencing Core `WaterRecord` and `GoalOperatingPoint` values) and derives `FeasibilityReport`.
- **Checks:** evaluates `ClassRules` (`class.*`) and `SanityBounds` (`envelope.*`) against the current `DesignRevision` + `GoalStateVersion` to produce located `DrcFinding` rows.
- **Assistance:** owns the `AI-02` `"Describe a starting design"` prompt slot; when no API key is configured, it renders `COPY-53` and never blocks any offline action (`AI-01`).

### 2.2 Grain, additivity, and history rules (DM3–DM12)

Because `NativeProject` lives in `CfdWorkbench.Core` and Core cannot reference `CfdWorkbench.Analysis` (ADR-0011 §Context), the persisted immutable records (`SetupRecords`, `SetupBriefVersion`, `GoalStateVersion`, `DrcExclusion`) live in `src/CfdWorkbench.Core/SetupRecords.cs`, mirroring `RunRecord.cs`.

| Entity / Value | Kind | Grain ("one row is exactly one …") | Key & Hash | History rule | Additivity of measures |
|---|---|---|---|---|---|
| **`SetupBriefVersion`** | entity, append-only fact | one submitted Setup brief (initial seed or re-seed) that produced one `GoalStateVersion` and one `AcceptedRow` | `briefId` (UUID), `parent` (UUID?), `contentHash` = BLAKE3(JCS without `contentHash`) | **Type-2 immutable** (never edited in place; re-seed appends a new row) | Soft-target weights: non-additive ratios; target lengths/area/AR: non-additive |
| **`GoalStateVersion`** | entity, append-only fact | one immutable snapshot of authored intent (rider/kit masses, `WaterRecord`, depth/wind/speed bands, discipline & class-rule preset pins, ordered operating-point set, typed constraint set, soft targets) | `goalStateId` (UUID), `parent` (UUID?), `contentHash` = BLAKE3(JCS without `contentHash`) | **Type-2 immutable** (every edit appends a new version; Experiments pin `(goalStateId, contentHash)` and always resolve, `GOAL-03`) | Individual mass items: additive within one version (`systemMassKg = Σ massKg`); speeds, depths, loads, CL, σ: non-additive |
| **`GoalOperatingPoint`** | owned value inside `GoalStateVersion` | one named target operating condition in the ordered operating-point set | `id` (unique string inside version, e.g. `"op-1"`) | Versioned with its owning `GoalStateVersion` | Speed, `h_ref`, `requiredLoadN`, `pAtmPa`: non-additive |
| **`GoalConstraintRow`** | owned value inside `GoalStateVersion` | one feasibility constraint row (preset-authored or user-edited) | `id` (unique string inside version, e.g. `"c-load-op1"`, `"c-cav-margin"`, `"c-hc-deep"`, `"c-class-gwa-area"`) | Versioned with its owning `GoalStateVersion`; carries `editedByUser: bool`, `enabled: bool` | Bounds: non-additive |
| **`RecipeRecord`** | owned value inside `SetupBriefVersion` | the exact solved driver set emitted one-way into FoilDSL 4.1 (`presetId`, `planformFamily`, `spanMeters`, `rootChordMeters`, `tipChordMeters`, `taper`, `sweepQuarterChordDeg`, `anhedralDeg`, `washoutDeg`, `rootThicknessRatio`, `profileId`, `profileOrigin`, `solverVersion`, `deviations[]`) | Linked via `SetupBriefVersion.seededAcceptedId` | Immutable provenance; **never** a second writable geometry AST (`GEO-04`, ADR-0002) | Non-additive |
| **`FlaggedAcknowledgement`** | owned value inside `SetupBriefVersion` | one explicit user acknowledgement of one `Flagged` field/preset at seed time | `(fieldPath, label, source, acknowledgedAtUtc)` | Immutable audit proof on the brief that seeded | Count of acknowledged flags: additive per brief |
| **`DrcExclusion`** | append-only fact inside `SetupRecords` | one user exclusion of one located DRC finding on one `DesignRevision` | `(exclusionId, designId, ruleId, ruleVersion, locationKey, reason, attribution)` | Immutable; references `designId` (free-text `attribution` typed by user, never OS username, excluded from AI context per A3.1) | Count: additive per design |

### 2.3 Exact record schemas (JSON wire form inside `Envelope.setup`)

```json
{
  "setup": {
    "briefs": [
      {
        "briefId": "uuid",
        "parent": null,
        "contentHash": "blake3-hex-64",
        "entryKind": "parameters",
        "purpose": "wingfoil-race",
        "disciplinePresetId": "wingfoil-race",
        "disciplinePresetVersion": "2026-11-19",
        "text": null,
        "languageMeta": null,
        "softTargets": {
          "maxSpanMeters": { "value": 1.10, "weight": 1.0, "provenance": "Stated", "source": "User parameter form" },
          "maxChordMeters": { "value": 0.120, "weight": 1.0, "provenance": "Stated", "source": "User parameter form" },
          "targetAreaSquareMeters": { "value": 0.100, "weight": 1.0, "provenance": "Stated", "source": "User parameter form" },
          "targetAspectRatio": { "value": 12.0, "weight": 1.0, "provenance": "Stated", "source": "User parameter form" },
          "taper": { "value": 0.24, "provenance": "Stated", "source": "User parameter form" },
          "weightBasis": "equal-default"
        },
        "flaggedAcknowledgements": [
          { "fieldPath": "goal.speedBand", "label": "Flagged", "source": "KB 04 §Goal state — no public race GPS dataset" }
        ],
        "recipe": {
          "solverVersion": "cfdw-seed/1",
          "planformFamily": "cubic-tapered-4cv",
          "spanMeters": 1.0954451150103321,
          "rootChordMeters": 0.11269978549489013,
          "tipChordMeters": 0.02704794851877363,
          "taper": 0.24,
          "anhedralDeg": -5.0,
          "washoutDeg": -1.0,
          "rootThicknessRatio": 0.10,
          "profileId": "naca-4412",
          "profileOrigin": "gen:naca-4412",
          "preferredProfileId": "e817",
          "fallbackReason": "Pending admission — terms requested from UIUC · GEN sections remain",
          "conflictNotice": null,
          "deviations": [
            { "target": "maxSpan", "targetValue": 1.10, "seededValue": 1.095445, "relativeDelta": 0.0 },
            { "target": "maxChord", "targetValue": 0.120, "seededValue": 0.112700, "relativeDelta": 0.0 },
            { "target": "targetArea", "targetValue": 0.100, "seededValue": 0.100000, "relativeDelta": 0.0 },
            { "target": "targetAR", "targetValue": 12.0, "seededValue": 12.000000, "relativeDelta": 0.0 }
          ]
        },
        "seededGoalStateId": "uuid",
        "seededAcceptedId": "uuid"
      }
    ],
    "goalStates": [
      {
        "goalStateId": "uuid",
        "parent": null,
        "briefId": "uuid",
        "contentHash": "blake3-hex-64",
        "disciplinePresetId": "wingfoil-race",
        "classRulePresetId": "gwa-race-2022",
        "classRuleVersion": "2022-02-01",
        "masses": [
          { "id": "rider", "label": "Rider", "massKg": 90.0, "provenance": "Stated", "confidence": "Verified", "source": "User input" },
          { "id": "board", "label": "Race board", "massKg": 5.5, "provenance": "Defaulted", "confidence": "Flagged", "source": "KB 04 §Goal state (GWA min 3 kg; practitioner 4–7 kg)" },
          { "id": "foil", "label": "Foil complete", "massKg": 4.5, "provenance": "Defaulted", "confidence": "Inferred", "source": "KB 04 §Goal state (front wing 2.19 kg; GWA min 3 kg)" },
          { "id": "wing", "label": "Hand wing", "massKg": 2.4, "provenance": "Defaulted", "confidence": "Inferred", "source": "KB 04 §Goal state (Duotone Unit 5.0 m² 2.18–2.34 kg)" },
          { "id": "kit", "label": "Wetsuit, harness, helmet, leashes", "massKg": 3.0, "provenance": "Defaulted", "confidence": "Flagged", "source": "KB 04 §Goal state" }
        ],
        "frontWingLoadN": 1050.0,
        "frontWingLoadProvenance": { "provenance": "Defaulted", "confidence": "Flagged", "source": "1.00–1.10 W trim share (no measured trim data)" },
        "water": {
          "temperatureC": 15.0,
          "salinityGPerKg": 35.16504,
          "rho": 1026.02,
          "nu": 1.1892e-6,
          "pv": 1670.9,
          "source": "ITTC 7.5-02-01-03 Rev 03",
          "tableHash": "fca86591cf575f50c0c17ca6de8dac6372d0c2baaa40f7711d184c2b6e154480"
        },
        "waterProvenance": { "provenance": "Stated", "confidence": "Verified", "source": "User selection (Salt · 15 °C)" },
        "depthBand": { "minMeters": 0.3, "maxMeters": 0.5, "provenance": "Defaulted", "confidence": "Flagged", "source": "Mast 85–95 cm, board clearance (KB 04)" },
        "windBand": { "minKnots": 10.0, "maxKnots": 20.0, "provenance": "Stated", "confidence": "Verified", "source": "User brief" },
        "speedBand": {
          "takeoffMinMps": 4.629996,
          "takeoffMaxMps": 5.658884,
          "cruiseMinMps": 5.658884,
          "cruiseMaxMps": 11.317768,
          "topMaxMps": 16.462208,
          "provenance": "Defaulted",
          "confidence": "Flagged",
          "sourceLabel": "KB 04 §Goal state — no race GPS dataset",
          "disconfirmingNote": "Light-wind take-off is pump-assisted"
        },
        "equipmentContext": {
          "rearAreaCm2": 180.0,
          "mastLengthCm": 95.0,
          "fuselageLengthCm": 75.0,
          "foilMassKg": 4.5,
          "provenance": "Defaulted",
          "confidence": "Flagged",
          "source": "Wingfoil race preset defaults (used for GWA class-rule checks; surfaces not designed in v1)"
        },
        "operatingPoints": [
          {
            "id": "op-1",
            "label": "10 kn take-off",
            "speedMetersPerSecond": 5.14444,
            "hRefMeters": 0.5,
            "datum": "root LE",
            "pAtmPa": 101325.0,
            "requiredLoadN": 1050.0,
            "loadSource": "Front-wing load (Flagged trim share)",
            "criticalMetric": "takeoff-cl",
            "ncrit": [2, 4],
            "speedLabel": "Flagged"
          }
        ],
        "constraints": [
          { "id": "c-load", "kind": "required-load", "label": "Support front-wing load", "operator": ">=", "bound": 1050.0, "unit": "N", "enabled": true, "editedByUser": false, "source": "wingfoil-race preset" },
          { "id": "c-cav", "kind": "cavitation-margin", "label": "Cavitation margin σ / (−Cp_min)", "operator": ">=", "bound": 1.15, "unit": "ratio", "enabled": true, "editedByUser": false, "source": "A5.4 default 15 % margin (practitioner assumption, not sourced)" },
          { "id": "c-re", "kind": "polar-re-envelope", "label": "Re inside polar envelope", "operator": "in", "boundMin": 200000.0, "boundMax": 2000000.0, "unit": "Re", "enabled": true, "editedByUser": false, "source": "A5.3 catalog polar envelope" },
          { "id": "c-hc", "kind": "deep-water-floor", "label": "Submergence ratio h/c", "operator": ">=", "bound": 5.0, "unit": "h/c", "enabled": true, "editedByUser": false, "source": "CEHINAV 2026 [S16] deep-water asymptote" },
          { "id": "c-class", "kind": "class-rule-ref", "label": "GWA race box rule (2022-02-01)", "presetId": "gwa-race-2022", "enabled": true, "editedByUser": false, "source": "GWA Wingfoil World Tour [S12]" },
          { "id": "c-sanity", "kind": "sanity-bounds", "label": "Catalogued product envelope (advisory)", "severity": "advisory", "enabled": true, "editedByUser": false, "source": "A5.5 product table (2026-09-20)" }
        ],
        "softTargets": {
          "maxSpanMeters": { "value": 1.10, "weight": 1.0, "provenance": "Stated", "source": "Setup brief" },
          "maxChordMeters": { "value": 0.120, "weight": 1.0, "provenance": "Stated", "source": "Setup brief" },
          "targetAreaSquareMeters": { "value": 0.100, "weight": 1.0, "provenance": "Stated", "source": "Setup brief" },
          "targetAspectRatio": { "value": 12.0, "weight": 1.0, "provenance": "Stated", "source": "Setup brief" },
          "taper": { "value": 0.24, "provenance": "Stated", "source": "Setup brief" },
          "weightBasis": "equal-default"
        }
      }
    ],
    "drcExclusions": []
  }
}
```

### 2.4 Derived, never stored (DM7)

| Derived quantity | Formula / rule | Owner |
|---|---|---|
| `WingEstimates` (`b`, `S`, `AR`, `c_root`, `c_tip`, `c_mean`, `MAC`, `max t/c`) | Computed on read from the accepted or preview FoilDSL bytes by `WingEstimates.From`. | `src/CfdWorkbench.Core/WingEstimates.cs` |
| `SystemMassKg`, `SystemWeightN` | `Σ masses[i].massKg`, `SystemMassKg * 9.80665`. | `GoalStateVersion` helper |
| `RecipeStatus` (`Linked` vs `Direct parametric`) | Compare current `AcceptedRow.Id` against the latest `SetupBriefVersion.SeededAcceptedId` on the active parent chain: equal → `Linked (<preset>)`; any descendant manual edit → `Direct parametric` (`GEO-04`). Undo back to `SeededAcceptedId` restores `Linked` automatically with zero shadow flag! | `SetupRecords.RecipeStatus` |
| Operating-point derived conditions (`q`, `CL_req`, `Re_ref`, `h/c`, `Fr_h`, `σ`) | `OperatingPoints.Derive` (`OperatingPoint.cs:68-82`) + `CL_req = requiredLoadN / (q * S_ref)`. Never stored. | `CfdWorkbench.Analysis` |
| `FeasibilityReport` | Constraint × operating-point matrix evaluated on read from current `DesignRevision` + latest `GoalStateVersion` + matching `AnalysisRun` / `PolarSample`. | `GoalFeasibility.Evaluate` |
| `DrcFinding[]` | Pure evaluation of `ClassRules.Evaluate` + `SanityBounds.Evaluate` against current `DesignRevision` + `GoalStateVersion`, joined with `drcExclusions` by `(designId, ruleId, locationKey)`. | `CfdWorkbench.Core` |
| Current `GoalStateVersion` & `SetupBriefVersion` | Last row in `setup.goalStates` / `setup.briefs` (append-only; no separate mutable `"activeGoalId"` pointer). | `AuthoringSession` |

### 2.5 Additive persistence in `NativeProject` & expand-migrate-contract (ADR-0011, DM16)

- **Optional top-level member:** Add `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public SetupRecords? Setup { get; init; }` to `Envelope` (`src/CfdWorkbench.Core/AuthoringSession.cs:22-28`).
- **Format string rule (`DR-SET-1`):**
  - **Preferred option (a) — generalize `cfdw-project-2`:** `NativeProject.Format(int runCount, bool hasSetup)` returns `"cfdw-project-1"` iff `runCount == 0 && !hasSetup`, and `"cfdw-project-2"` iff `runCount > 0 || hasSetup`. In `SET-1`, `NativeProject.Read` (`src/CfdWorkbench.Core/AuthoringSession.cs:2631, 2641, 2643`) expands `Exact(root, [...], ["analysis", "setup"])` and replaces `RunRecord.Format(runs)` with `NativeProject.Format(runs, hasSetup)` so a `cfdw-project-2` document carrying `"setup"` with `0` runs (and no `"analysis"` member) is admitted cleanly.
  - Why this is safe and fail-closed:
    1. Any project with **no runs and no setup** writes `"cfdw-project-1"` **byte-for-byte identically to today** (`Project1_NoRun_ByteIdenticalToToday` stays green).
    2. An older build opening a file containing `"setup"` hits `Exact(root, [...], ["analysis"])` (`AuthoringSession.cs:2631`) **before** reading the format check, and fails closed with `DOC-UNSUPPORTED-FIELD` (`COPY-130`: *"It contains parts this version doesn't understand... A newer version of CFD Workbench may open it"*), never corrupting the file.
    3. The first save turning `-1` into `-2` triggers the existing `.v1.bak` creation in `ProjectStore` (`ADR-0011 §6`).
  - Alternative option (b) (`cfdw-project-3` whenever `setup` is present, plus `.v2.bak` when upgrading a `-2` file) is offered in `DR-SET-1`.
- **`EditReceipt` for re-seed (`SET-03`):**
  - Initial seed on an empty session opens `r1` (`parent = null, edit = null`, `cursor.reason = "open"`), identical to `OpenCore`, plus appends `brief[0]` and `goalStates[0]` referencing `r1.Id`.
  - **Re-seed on an already-edited document (`r1..r_n`)** appends `r_{n+1}` with `parent = r_n.Id`, `cursor.reason = "apply"`, and `edit = new EditReceipt(draftId, generation, rail: "seed", vertexId: briefId)`.
  - `NativeProject.EditReference` (`AuthoringSession.cs:2676`) adds `"seed" => edit.Curve is null && edit.Rule is null && setupBriefExists(edit.VertexId)` — proving every re-seed revision links to an existing immutable `SetupBriefVersion`!
  - **History & Freshness (`SET-03`, `ANA-07`):** Prior revisions `r1..r_n` remain intact in `accepted[]`, `designs[]`, `sources[]`. Because `r_{n+1}` has a new `SurfaceHash`, every earlier `AnalysisRun` automatically computes `Freshness = Historical` via run-key inequality (`RunRecord.Key`). Undo from `r_{n+1}` back to `r_n` restores `r_n` and makes `r_n`'s runs `Current` again; `briefs[]` and `goalStates[]` stay append-only (matching `AnalysisRecords`, ADR-0011 §8).
- **Store invariants checked in `NativeProject.Check` on read and under `sync` lock on write:**
  - Every `briefId` / `goalStateId` is a canonical lowercase UUID; unique across the document.
  - Every `contentHash` matches `BLAKE3(JCS(row without contentHash))` (`DOC-INTEGRITY`).
  - `brief.seededAcceptedId` resolves to an existing `AcceptedRow.Id`; `brief.seededGoalStateId` resolves to an existing `GoalStateVersion.GoalStateId` (`DOC-REFERENCE`).
  - `goalState.parent` (if non-null) resolves to an earlier `GoalStateVersion.GoalStateId` (`DOC-REFERENCE`).
  - All numbers finite; `water` passes `OperatingPoints.Validate(water)`; `hRefMeters` is null or `> 0`; `speedMetersPerSecond > 0`; `requiredLoadN > 0`; `ncrit` non-empty finite positive; `weights >= 0` with at least one `> 0`.
  - Prospective save image passes `NativeProject.Preflight(envelope, envelopeCap)` (`32_000_000` bytes cap per Ruling 263; every JSON line `<= 4096` bytes; `DOC-SIZE` refusal preserves session state).
- **Size impact against 32 MB cap:** One `SetupBriefVersion` + `GoalStateVersion` (7 operating points, 6 constraints, 5 masses) serializes to `~4.5 KB` indented JSON — < 0.015 % of the 32 MB cap. Even 100 brief/goal versions take `< 0.5 MB`.

### 2.6 Seam contract with Area 4 Local Sweeps (`SWP`)

Aligned verbatim with `w5-sweeps-design` on the swarm board (#9, #11):
1. `SWP` references an immutable `GoalStateVersion` by `(goalStateId: UUID, contentHash: string)`.
2. `SWP` reads `water: WaterRecord`, `operatingPoints[]` (`id`, `label`, `speedMetersPerSecond`, `hRefMeters`, `datum` = `"root LE"`, `pAtmPa`, `requiredLoadN`, `loadSource`, `criticalMetric`, `ncrit`), and `speedBand` / `depthBand` (for `ANA-16` take-off and cavitation-ceiling chart bands; absent when no Goal state exists).
3. **`RunRecord.cs:76` `OperatingPoint.Load` confirmation (`DR-SET-2`):** Area 1 formally confirms `RunRecord.cs:72-75`'s open `assume:`: `OperatingPoint.Load` (`double?`) is the Goal-state front-wing required load in Newtons (`requiredLoadN`). Custom fixed-α runs keep `Load = null` (so every existing A3a/A3d run key stays bit-identical); Goal-state load-solved runs set `Load = point.requiredLoadN` once `OperatingPoints.Validate` (`OperatingPoint.cs:61`) relaxes `Require(op.Load is null, ...)` to `Require(op.Load is null || (double.IsFinite(op.Load.Value) && op.Load.Value > 0), "ANA-INPUT-LOAD", ...)`.

---

## 3. Soft-target deterministic solver & FoilDSL 4.1 emission (`SET-01`, `GEO-01`, `GEO-04`)

### 3.1 Normalized planform family and exact algebraic identities

A seeded wing uses the exact same clamped degree-3 4-CV Bézier rail basis as `FoilSource.NewDefault()` (`FoilSource.cs:631, 639, 745-767`), with `TipClusteredChannel(4, 3)` (`knots = [0, 0, 0, 0, 1, 1, 1, 1]`, `controlX = [0, 1/3, 2/3, 1]`).

For a chosen taper ratio $\lambda \in [\lambda_{\min}, 1]$ (with $\lambda_{\min} = 0.02$ so $c_{\text{tip}}/c_{\text{root}} \ge 0.02$ always holds):
1. Set the four normalized cubic Bézier chord ordinates directly to $(P_0, P_1, P_2, P_3) = (1,\; 1,\; 1,\; \lambda)$:
   $$g_{4\text{cv}}(\eta; \lambda) = 1 - (1 - \lambda)\eta^3, \qquad \eta \in [0, 1]$$
   - Exact root chord $g_{4\text{cv}}(0) = 1$, exact root-mirror G1 (and G2) tangent $g'_{4\text{cv}}(0) = 0$ ($P_1 = P_0$), exact tip taper $g_{4\text{cv}}(1) = \lambda$.
   - By the variation-diminishing property ($P_0 = P_1 = P_2 = 1 \ge \lambda = P_3$), $g_{4\text{cv}}(\eta; \lambda)$ is **monotone non-increasing** on $[0, 1]$ with $\max_{\eta} g_{4\text{cv}}(\eta; \lambda) = 1$ (root) and $\min_{\eta} g_{4\text{cv}}(\eta; \lambda) = \lambda$ (tip).
   - **Exact closed-form area fill factor** (zero fitting step, zero `Math.Pow`, agrees with `WingEstimates.IntegrateOrdinate`'s 3-point Gauss–Legendre quadrature to $< 10^{-15}$ relative):
     $$\kappa(\lambda) = \int_0^1 \big(1 - (1 - \lambda)\eta^3\big)\,d\eta = \frac{3 + \lambda}{4}$$
   - **Exact closed-form MAC ratio** (matches `WingEstimates.IntegrateSquare` to $< 10^{-12}$):
     $$\int_0^1 g_{4\text{cv}}(\eta; \lambda)^2\,d\eta = \frac{1 + \lambda}{2} + \frac{(1 - \lambda)^2}{7}, \qquad \text{MAC} = c_{\text{root}} \cdot \frac{\frac{1 + \lambda}{2} + \frac{(1 - \lambda)^2}{7}}{\kappa(\lambda)}$$
2. Place the normalized leading and trailing rails about the quarter-chord line (matching `FoilSource.cs:649-650`, plus optional linear quarter-chord sweep $\Lambda_{c/4}$):
   $$\text{LE}_0(\eta) = \frac{1 - g_{4\text{cv}}(\eta)}{4} + \frac{b}{2 c_{\text{root}}} \eta \tan \Lambda_{c/4}, \qquad \text{TE}_0(\eta) = \text{LE}_0(\eta) + g_{4\text{cv}}(\eta)$$
   With default zero quarter-chord sweep ($\Lambda_{c/4} = 0$), scaling the four control ordinates by $c_{\text{root}}$ gives exact algebraic identities — one definition shared by the solver and `WingEstimates.From` (DM7):
   $$c(\eta) = c_{\text{root}} \cdot g_{4\text{cv}}(\eta; \lambda), \qquad c_{\max} = c(0) = c_{\text{root}}, \qquad c_{\text{tip}} = \lambda \, c_{\text{root}}$$
   $$S(b, c_{\text{root}}) = b \cdot c_{\text{root}} \cdot \kappa(\lambda), \qquad \text{AR}(b, c_{\text{root}}) = \frac{b^2}{S} = \frac{b}{c_{\text{root}} \cdot \kappa(\lambda)}$$

### 3.2 Hard feasible domain $\Omega$

Every seeded design MUST lie inside $\Omega$ so it always passes `TipChord.Meets` (Ruling 93) and `Geometry.Assess` (`GeometryStatus.Certified`):
$$\Omega = \left\{ (b, c_{\text{root}}) \in \mathbb{R}^2 \;\middle|\; b \in [0.10,\; 5.00]\text{ m},\quad c_{\text{root}} \in \left[\max\!\left(0.020,\; \frac{0.005}{\lambda_{\text{eff}}}\right),\; 1.00\right]\text{ m} \right\}$$
- **Tip-chord minimum guarantee:** Since $\lambda_{\text{eff}} = \max(\lambda, 0.02)$ and $c_{\text{root}} \ge 0.005 / \lambda_{\text{eff}}$, we have $c_{\text{tip}} = \lambda_{\text{eff}} c_{\text{root}} \ge \max(0.005\text{ m},\; 0.02\, c_{\text{root}}) = \texttt{TipChord.MinimumMeters}(c_{\text{root}})$ unconditionally.
- **No rail crossing:** $c(\eta) \ge c_{\text{tip}} \ge 5\text{ mm} > 0$ everywhere on $\eta \in [0, 1]$.
- **Thickness & twist certificate safety:** $t/c \in [0.04, 0.25] \subset (10^{-7}, 1 - 10^{-7})$ (`Geometry.ThicknessDomain`); $|\theta(\eta)| \le 15^\circ \ll 57.2957795^\circ$ (`Geometry.TwistDomainDegrees`).

### 3.3 Soft-target objective and exact deterministic minimization (`SET-01`)

Given soft targets $(b_{\max}^*, c_{\max}^*, S^*, \text{AR}^*)$ (each optional, strictly positive if supplied) and non-negative finite weights $(w_b, w_c, w_S, w_{\text{AR}})$ with $\sum w_i > 0$:

Define the four non-negative relative deviations:
$$d_b(b) = \max\!\left(0,\; \frac{b - b_{\max}^*}{b_{\max}^*}\right), \qquad d_c(c_{\text{root}}) = \max\!\left(0,\; \frac{c_{\text{root}} - c_{\max}^*}{c_{\max}^*}\right)$$
$$d_S(b, c_{\text{root}}) = \frac{|S(b, c_{\text{root}}) - S^*|}{S^*}, \qquad d_{\text{AR}}(b, c_{\text{root}}) = \frac{|\text{AR}(b, c_{\text{root}}) - \text{AR}^*|}{\text{AR}^*}$$
*(When a target is omitted, its deviation term is $0$.)*

**Lexicographic objective (unique deterministic global minimum):**
1. **Primary objective (`SET-01`):** minimize the weighted sum of relative deviations:
   $$J_1(b, c_{\text{root}}) = w_b d_b + w_c d_c + w_S d_S + w_{\text{AR}} d_{\text{AR}}$$
2. **Secondary tie-breaker (zero-weight targets):** among minimizers of $J_1$ (within $10^{-14}$), minimize the unweighted sum of relative deviations over any supplied targets whose user weight is $0$:
   $$J_2(b, c_{\text{root}}) = \sum_{i:\, w_i = 0,\, \text{supplied}} d_i$$
3. **Tertiary tie-breaker (under-determined inputs):** among minimizers of $(J_1, J_2)$, minimize relative distance to the active discipline preset's default $(b_{\text{def}}, c_{\text{def}})$:
   $$J_3(b, c_{\text{root}}) = \frac{|b - b_{\text{def}}|}{b_{\text{def}}} + \frac{|c_{\text{root}} - c_{\text{def}}|}{c_{\text{def}}}$$

**Complete proof that finite-candidate enumeration finds the global minimum on $\Omega$ (zero black-box optimizer):**
1. **No strictly 2D interior minimum:** On any open cell away from the four target loci ($b = b_{\max}^*$, $c_{\text{root}} = c_{\max}^*$, $b c_{\text{root}} \kappa = S^*$, $b/(c_{\text{root}} \kappa) = \text{AR}^*$), $\partial J_1 / \partial b = \sigma_b \frac{w_b}{b_{\max}^*} + \sigma_S \frac{w_S c_{\text{root}} \kappa}{S^*} + \sigma_{\text{AR}} \frac{w_{\text{AR}}}{c_{\text{root}} \kappa \text{AR}^*}$ is independent of $b$. Because $J_1$ is affine in $b$ on every open cell, no strictly 2D interior minimum exists; the global minimum MUST lie on a fixed-$b$ boundary/kink line, a fixed-$c_{\text{root}}$ boundary/kink line, the hyperbola $S = S^*$, or the ray $\text{AR} = \text{AR}^*$.
2. **Ray $\text{AR} = \text{AR}^*$ ($b = \text{AR}^* \kappa c_{\text{root}}$, $S = \text{AR}^* \kappa^2 c_{\text{root}}^2$):** $J_1(c_{\text{root}})$ is either strictly increasing ($S > S^*$) or strictly concave ($S < S^*$) between kink points, so its minimum always occurs at a pairwise intersection.
3. **Fixed-$c_{\text{root}}$ lines:** $J_1$ is piecewise linear in $b$, so its minimum always occurs at a pairwise intersection.
4. **Fixed-$b$ boundary/kink lines ($b \in \{b_{\min}, b_{\max}, b_{\max}^*, \sqrt{S^*\text{AR}^*}\}$):** On each piece with $\text{AR} > \text{AR}^*$ ($w_{\text{AR}} > 0$), $J_1(c) = A c + B/c + \text{const}$ with $A = \sigma_c \frac{w_c}{c_{\max}^*} + \sigma_S \frac{w_S b \kappa}{S^*}$ ($\sigma_c \in \{0, 1\}$, $\sigma_S \in \{-1, 0, +1\}$) and $B = \frac{w_{\text{AR}} b}{\kappa \text{AR}^*}$. Whenever $A > 0$, the unique interior stationary point is $c_{\text{stat}} = \sqrt{B / A}$ (covering both $c \le c_{\max}^*$ and $c > c_{\max}^*$).
5. **Hyperbola $S = S^*$ ($c_{\text{root}} = S^*/(b\kappa)$, $\text{AR} = b^2/S^*$):** On pieces with $c_{\text{root}} > c_{\max}^*$ ($w_c > 0$), the derivative is
   $$\frac{dJ_1}{db} = \sigma_b \frac{w_b}{b_{\max}^*} - \frac{w_c S^*}{b^2 \kappa c_{\max}^*} + \sigma_{\text{AR}} \frac{2 w_{\text{AR}} b}{S^* \text{AR}^*} = 0, \qquad \sigma_b \in \{0, 1\},\; \sigma_{\text{AR}} \in \{-1, 0, +1\}$$
   - $\sigma_{\text{AR}} = 0,\; \sigma_b = 1$: exact $b_{\text{stat}} = \sqrt{w_c S^* b_{\max}^* / (w_b \kappa c_{\max}^*)}$.
   - $\sigma_{\text{AR}} = +1$ ($\text{AR} > \text{AR}^*$, strictly increasing derivative on $(0, \infty)$): when $\sigma_b = 0$, exact cube root $\sqrt[3]{w_c (S^*)^2 \text{AR}^* / (2 w_{\text{AR}} \kappa c_{\max}^*)}$ (solved by 64-step bisection on $[b_{\min}, b_{\max}]$ to avoid platform `Math.Pow`); when $\sigma_b = 1$, unique root on $[\max(b_{\min}, b_{\max}^*, \sqrt{S^*\text{AR}^*}), b_{\max}]$ via 64-step bisection.
   - $\sigma_{\text{AR}} = -1,\; \sigma_b = 1$ ($\text{AR} < \text{AR}^*$): convex region $(0, b_{\text{peak}}]$ where $b_{\text{peak}} = \sqrt[3]{w_c (S^*)^2 \text{AR}^* / (w_{\text{AR}} \kappa c_{\max}^*)}$; any sign change on $[\max(b_{\min}, b_{\max}^*), \min(b_{\max}, \sqrt{S^*\text{AR}^*}, b_{\text{peak}})]$ is solved by 64-step bisection.

Evaluating $(J_1, J_2, J_3)$ over all pairwise intersections plus families (4) and (5) (clamped to $\Omega$) takes **< 0.1 ms**, uses only `+ - * / sqrt` plus deterministic 64-step bisection (zero `Math.Pow`, Ruling 157), and was independently cross-checked across 200 adversarial random trials against a dense 2D grid oracle with worst gap `0.000e+00`.

### 3.4 Conflict detection and explicit `SET-01` / `GEO-01` proofs

A **soft-target conflict** exists iff no point $(b, c_{\text{root}}) \in \Omega$ achieves $d_b = d_c = d_S = d_{\text{AR}} = 0$ simultaneously (equivalently, the seeded optimum has $\max_i d_i > 10^{-6}$). The solver names the conflicting pair `(<a>, <b>)` (e.g. `"target area (1000 cm²)"` and `"max span (0.80 m) × max chord (100 mm)"`) and renders `COPY-73`:
> *"Targets conflict: `<a>` and `<b>` — seeded the nearest feasible design; targets stay as preferences"*

Never a hard error. Every target shows its signed deviation badge beside the seeded value (e.g. `target AR 12 · seeded 11.60 · −3 %` at `b_max = 1.077 m`).

| Fixture | Inputs | Weights `(w_b, w_c, w_S, w_AR)` | Solved `(b, c_root, c_tip, S, AR)` under $\kappa(\lambda) = (3+\lambda)/4$ | Conflict? | Verification |
|---|---|---|---|---|---|
| **`SET-01` primary** | `b_max 1.10 m`, `c_max 120 mm`, `S 1000 cm²`, `AR 12`, `λ 0.24` (`κ = 0.81`) | `(1, 1, 1, 1)` | `b = 1.095445 m`, `c_root = 112.70 mm`, `c_tip = 27.05 mm`, `S = 1000.00 cm²`, `AR = 12.00` | No (`d_i = 0`) | **Verified by execution** |
| **`SET-01` tight-span** | `b_max 1.077 m`, `c_max 120 mm`, `S 1000 cm²`, `AR 12`, `λ 0.24` (`κ = 0.81`) | `(2, 1, 2, 1)` | `b = 1.077000 m`, `c_root = 114.63 mm`, `c_tip = 27.51 mm`, `S = 1000.00 cm²`, `AR = 11.60` (`−3 %`) | Yes (`max span` vs `target AR`) | **Verified by execution** (`target AR 12 · seeded 11.6 · −3 %`) |
| **`SET-01` conflicting triple** | `S 1000 cm²`, `b_max 0.80 m`, `c_max 100 mm`, `λ 0.24` (`κ = 0.81`) | **`(1, 0, 0, 0)`** | **`b = 0.800000 m` exact**, `c_root = 100.00 mm`, `c_tip = 24.00 mm`, `S = 648.00 cm²` (`−35 %`), `AR = 9.88` | Yes (`COPY-73`) | **Verified by execution** (`J1 = 0` for `b ≤ 0.80`; `J2` pushes `b` uniquely to `0.800000 m`) |
| **`GEO-01` exact drivers** | `S = 0.14 m²`, `AR = 7.0`, `λ = 0.30` (`κ = 0.825`) | `(0, 0, 1, 1)` | **`b = 0.98994949 m = √0.98 m`**, `c_root = 171.42 mm`, `c_tip = 51.43 mm`, `S = 1400.00 cm²`, `AR = 7.00` | No (`d_i = 0`) | **Verified by execution** (within `10⁻¹²` relative) |

### 3.5 One-way FoilDSL 4.1 emission & `GEO-04` detachment

1. **Emission:** `RecipeSeeder.EmitFoilDsl(RecipeRecord recipe, CatalogEntry genProfile)` builds a valid `foildsl "4.1"` UTF-8 document using `FoilSource.ExactDecimal`:
   - `half_span = b / 2` in `m`, `evaluator "cfdw-cv" "2"`, `symmetry mirror_y`, `tip open`.
   - `leading` and `trailing`: 4-CV degree-3 curves on `TipClusteredChannel(4, 3)` with `ids ["cv-0", "cv-1", "cv-2", "cv-3"]`.
   - `dihedral`: 10-CV degree-3 tip-clustered curve rising/dropping smoothly from `0` at root (horizontal root tangent `P1.y = P0.y = 0`) to `(b/2) * tan(anhedralDeg)` at tip.
   - `twist`: 10-CV degree-3 curve from `0°` at root (`P1.y = P0.y = 0`) to `washoutDeg` at tip.
   - `thickness`: 10-CV degree-3 curve at `rootThicknessRatio` (with optional smooth root-to-tip taper inside `[0.04, 0.25]`).
   - `profiles`: one degree-5 10-CV profile fitted via `DatImport.Fit` from `genProfile.Coordinates` with `closure closed` and `provenance "gen:<genProfile.Id>"`.
   - `sections`: `at root profile "<id>" at tip profile "<id>"`.
   - `locks`: explicit `root_mirror` on all five channels (matches `FoilSource.cs:1489`).
2. **Admission:** The emitted bytes pass through `FoilSource.Parse` and `Geometry.Assess` before `AuthoringSession.Seed` commits. A non-certified candidate can never be committed (DATA-E).
3. **Detachment (`GEO-04`):** When a seeded document receives its first manual edit (drag, nudge, typed dimension, point verb, section edit, or FoilDSL text edit), `AcceptedRow` advances past `SeededAcceptedId`. `RecipeProvenance.Status` immediately derives `"Direct parametric"`, the status strip reports `COPY-05` (*"This edit detaches the starter recipe. The explicit model stays parametric."*), and Undo restores `SeededAcceptedId` — restoring `"Linked (<preset>)"` in the exact same undo step with zero shadow state. Retained recipe provenance can never overwrite direct geometry.

---

## 4. Nine discipline presets, catalog fallbacks, class rules & `GOAL-02` (`SET-04`, `CAT-03`, `CAT-04`, `GOAL-01`, `GOAL-02`)

### 4.1 Nine discipline presets and 8 → 9 purpose mapping (`SET-04`, `CAT-03`)

All nine A5.5 presets are re-centred on the KB 04 verified product table (AR floor ≥ 7.1, area 480–1900 cm², span 690–1380 mm, root `t/c` 9–12 %). Every default wing below was verified in Python to satisfy its ceilings, `TipChord.MinimumMeters`, and A5.5 sanity bounds with zero error-severity findings.

| # | Preset ID | Setup brief purpose(s) mapped | Default drivers (`S*`, `AR*`, `b_max*`, `c_max*`, `λ`, `t/c`) | Seeded wing (`b`, `c_root`, `c_tip`, `c̄`, `MAC`) under $\kappa = (3+\lambda)/4$ | Preferred section → Admitted GEN fallback (`catalog.tsv`) | Class rule preset | Flagged fields requiring acknowledgement |
|---|---|---|---|---|---|---|---|
| 1 | `wingfoil-race` | `wingfoil race` | `1000 cm²` · `12.0` · `1.10 m` · `120 mm` · `0.24` · `10.0 %` | `1095.4 mm` · `112.7 mm` · `27.0 mm` · `91.3 mm` · `97.7 mm` | `e817` (Pending UIUC) → **`naca-4412`** | `gwa-race-2022` | board/kit mass, trim share, depth band, speed band, anhedral/washout |
| 2 | `wingfoil-freeride` | `wingfoil freeride` | `1200 cm²` · `8.0` · `1.00 m` · `170 mm` · `0.35` · `11.0 %` | `979.8 mm` · `146.2 mm` · `51.2 mm` · `122.5 mm` · `128.4 mm` | `e874` (Pending UIUC) → **`naca-4412`** | `open-class` | board/kit mass, speed band, anhedral/washout |
| 3 | `surf` | `surffoil`, `wingfoil surf`, **`wakefoil`** *(Flagged wake note)* | `1050 cm²` · `7.4` · `0.90 m` · `175 mm` · `0.38` · `11.5 %` | `881.5 mm` · `141.0 mm` · `53.6 mm` · `119.1 mm` · `124.3 mm` | `e874` (Pending UIUC) → **`naca-4412`** | `open-class` | wave/wake speed band (`wakefoil → surf preset` note), board/foil mass |
| 4 | `pump` | Direct preset (`pump`) | `1550 cm²` · `10.5` · `1.30 m` · `175 mm` · `0.30` · `11.0 %` | `1275.7 mm` · `147.3 mm` · `44.2 mm` · `121.5 mm` · `128.5 mm` | `e904` (Pending UIUC) → **`naca-4412`** | `open-class` | pump cadence/heave (`St`), board mass |
| 5 | `downwind` | `downwind` | `1100 cm²` · `11.8` · `1.20 m` · `135 mm` · `0.28` · `10.4 %` | `1139.3 mm` · `117.7 mm` · `33.0 mm` · `96.6 mm` · `102.5 mm` | `e904` (Pending UIUC) → **`naca-4412`** | `open-class` | SUP board mass, swell speed band |
| 6 | `parawing` | Direct preset (`parawing`) | `1000 cm²` · `11.0` · `1.10 m` · `130 mm` · `0.28` · `10.4 %` | `1048.8 mm` · `116.3 mm` · `32.6 mm` · `95.3 mm` · `101.3 mm` | `e904` (Pending UIUC) → **`naca-4412`** | `open-class` | parawing mass, wind/speed band, open-class status |
| 7 | `windfoil` | `windfoil race` (iQFOiL), `windfoil freeride` (open) | `900 cm²` · `10.0` · `0.98 m` · `130 mm` · `0.30` · `9.5 %` | `948.7 mm` · `115.0 mm` · `34.5 mm` · `94.9 mm` · `100.4 mm` | `speer-h105` (Cite only LINK) → **`naca-0009`** | `iqfoil-2025` (race) / `open-class` (freeride) | iQFOiL span/section geometry, windfoil speed band |
| 8 | `kitefoil` | Direct preset (`kitefoil`) | `620 cm²` · `10.5` · `0.85 m` · `105 mm` · `0.26` · `9.2 %` | `806.8 mm` · `94.3 mm` · `24.5 mm` · `76.8 mm` · `81.9 mm` | `e818` (Pending UIUC) → **`naca-0009`** | `formula-kite-2024` | Formula Kite dimensional maxima (PDF unopened), speed band |
| 9 | `efoil` | Direct preset (`e-foil`) | `1400 cm²` · `7.5` · `1.08 m` · `215 mm` · `0.40` · `12.0 %` | `1024.7 mm` · `160.7 mm` · `64.3 mm` · `136.6 mm` · `142.1 mm` | **`naca-4412`** (GEN admitted directly) | `open-class` | e-foil wing dimensions & battery/board mass |

**GEN polar disclosure (`CAT-03`, `spec-amendment-setup-seeding.md` AM-SET-S1):**
In today's build, `UnavailablePolar.cs` supplies no computed section polar table until a polar method or precomputed GEN polar dataset lands. Therefore:
- Every preset with a pending/LINK preferred section displays the explicit fallback chip (`e.g., "Preferred: Eppler E817 (Pending admission — terms requested from UIUC) · Using GEN fallback: NACA 4412"`).
- Polar-dependent feasibility cells (`c-cav` requiring `−Cp_min`, `c-load` requiring `CL_max` stall check) honestly render `"Unavailable — polar data not computed for this section"` (never guessed, never `0`), while exact geometric/hydrodynamic cells (`q`, `CL_req`, `Re_ref`, `h/c`, `Fr_h`, `σ`, class-rule DRC, sanity bounds) compute live.

### 4.2 Flagged-field acknowledgement gate (`SET-04`, `CAT-03`, `COPY-39`)

Any preset or brief containing one or more `Flagged` fields (for instance: `wakefoil → surf preset` wake-speed note, or race wingfoil's Flagged board/kit mass, trim share, depth band, and speed band):
1. Displays each Flagged item inline with its `Flagged` badge (shape + text, never colour alone) and its exact reason/source (`COPY-39`: *"Flagged — `<reason>` · Acknowledge to continue"*).
2. Disables the **Seed design** primary action (`aria-disabled="true"` with `aria-describedby` pointing to the acknowledgement summary) until the user either:
   - checks **Acknowledge `<n>` flagged assumption(s)**, or
   - replaces every Flagged field with a `Stated` value.
3. On Seed, every acknowledged Flagged field is written into `SetupBriefVersion.FlaggedAcknowledgements[]` so the provenance survives save/reopen.

### 4.3 Class-rule presets and DRC evaluators (`CAT-04`, A5.5, A5.8)

All class rules run as pure functions inside `ClassRules.Evaluate(WingEstimates wing, GoalStateVersion goal)` and emit standard KiCad-shaped DRC findings (`ruleId`, `ruleVersion`, `ruleDate`, `severity`, `location`, `message`, `jumpTarget`):

| Class preset ID | Version / Date | Rules checked | Severity | Located DRC message |
|---|---|---|---|---|
| `gwa-race-2022` | `GWA Race Box v2022-02-01` (2022-02-01, Verified [S12]) | `class.gwa.front_area`: `S >= 700 cm²` (from wing geometry); `class.gwa.rear_area`: `rear >= 150 cm²`; `class.gwa.mast_length`: `mast <= 115 cm`; `class.gwa.fuselage_length`: `fuselage <= 100 cm`; `class.gwa.foil_mass`: `foil >= 3.0 kg` (from `equipmentContext`) | `warning` (never blocks seeding or mutates geometry, `GOAL-03`) | e.g. `"Front wing area 650 cm² is below GWA Race minimum 700 cm² (GWA Race Box v2022-02-01, 2022-02-01)."` |
| `iqfoil-2025` | `iQFOiL One-Design 2025` (2025, Verified equipment [S13]) | `class.iqfoil.benchmark`: displays fixed one-design benchmark (`900 cm²` front, `255 cm²` tail, `115 Plus` fuselage, `95 cm` mast, **shim range `−2°…+1°`**); flags if seeded `S` deviates from `900 cm²` (`±1 cm²`) | `advisory` / `warning` | `"iQFOiL one-design benchmark is fixed at 900 cm² (shim −2°…+1°); current wing is <S> cm² (iQFOiL 2025)."` |
| `formula-kite-2024` | `IKA Formula Kite 2024` (2024-08-10, Verified tolerances [S14]) | `class.formulakite.tolerances`: displays production registration tolerances (`span ±2 mm`, `chord −1 mm`, `general ±0.4 mm`, `weight ±4 %`); dimensional maxima marked `Flagged` (class rules PDF unopened) | `advisory` | `"IKA Formula Kite registration tolerances: span ±2 mm · chord −1 mm · general ±0.4 mm · mass ±4 % (maxima Flagged)."` |
| `open-class` | `Open class` (Moth / parawing / freeride / downwind / surf / pump / e-foil) | `class.open`: no box limits; renders `"open class"` badge verbatim (`CAT-04`) | `info` (0 violations) | `"Open class — no equipment box rules apply."` |

### 4.4 Sanity-bound advisory DRC (`envelope.*`, A5.5, `ANA-20`)

Bundled machine-readable fixture `SanityBounds.json` (BLAKE3-checked at load, per-row source URL + access date `2026-09-20`, never gates):
- `envelope.area`: `480–1900 cm²` (Verified product pages)
- `envelope.span`: `690–1380 mm` (Verified product pages)
- `envelope.aspect_ratio`: `7.0–13.1` (Verified product pages)
- `envelope.root_tc`: `9.0–12.0 %` (Verified product pages)
- `envelope.wing_loading`: `8.0–20.0 kPa` (Inferred on Flagged speeds)
- `envelope.takeoff_cl`: `0.60–0.80` (Inferred on Flagged speeds)
- `envelope.cruise_cl`: `0.10–0.45` (Inferred on Flagged speeds)

Any value outside its band emits an `advisory` DRC finding using `COPY-49`: *"Out-of-envelope observation — `<bound, source, date>`"*.

### 4.5 Exact `GOAL-02` 7-point arithmetic verification

Pinned inputs (`docs/specs/cfd-workbench-v1.md:1191`): `1 kn = 0.514444 m/s`, `ρ = 1026.02 kg/m³`, `ν = 1.1892×10⁻⁶ m²/s`, `p_v = 1.671 kPa` (identical at 2 decimals to `IttcWater.csv`'s `1.6709 kPa`), `p_atm = 101.325 kPa`, `g = 9.80665 m/s²`, `W = 1050 N` (rider `90 kg` + board `5.5 kg` + foil `4.5 kg` + wing `2.4 kg` + kit `3.0 kg` = `105.4 kg`, `W_sys = 1033.6 N`, front-wing load `1050 N` with Flagged trim share).

| Example wing | Point | `V` (kn / m/s) | `h_ref` (m) | `q` (Pa) | `CL_req` (3 dec) | `σ` (2 dec) | `Re_ref` | `h/c̄` | `Fr_h` | Spec `GOAL-02` oracle |
|---|---|---|---|---|---|---|---|---|---|---|
| **race light** (`S = 1000 cm²`, `b = 1.10 m`, `c̄ = 90.9 mm`, `W/S = 10.5 kPa`) | P1 take-off | 10 / 5.144 | 0.5 | 13,576.9 | **0.773** | **7.71** | `3.93×10⁵` | 5.50 | 2.32 | `0.773`, `7.71` — **MATCH** |
| race light | P1 shallow | 10 / 5.144 | **0.3** | 13,576.9 | **0.773** | **7.56** | `3.93×10⁵` | 3.30 | 3.00 | `0.773`, `7.56` — **MATCH** (`h/c < 5` notice!) |
| race light | P2 upwind | 14 / 7.202 | 0.5 | 26,610.8 | **0.395** | **3.93** | `5.51×10⁵` | 5.50 | 3.25 | `0.395`, `3.93` — **MATCH** |
| race light | P3 downwind | 18 / 9.260 | 0.5 | 43,989.3 | **0.239** | **2.38** | `7.08×10⁵` | 5.50 | 4.18 | `0.239`, `2.38` — **MATCH** |
| **race strong** (`S = 750 cm²`, `b = 0.95 m`, `c̄ = 78.9 mm`, `W/S = 14.0 kPa`) | P4 post-takeoff | 15 / 7.717 | 0.5 | 30,548.1 | **0.458** | **3.43** | `5.12×10⁵` | 6.33 | 3.48 | `0.458`, `3.43` — **MATCH** |
| race strong | P5 upwind | 22 / 11.318 | 0.5 | 65,712.4 | **0.213** | **1.59** | `7.51×10⁵` | 6.33 | 5.11 | `0.213`, `1.59` — **MATCH** |
| race strong | P6 downwind | 28 / 14.404 | 0.5 | 106,443.2 | **0.132** | **0.98** | `9.56×10⁵` | 6.33 | 6.51 | `0.132`, `0.98` — **MATCH** |
| race strong | P7 top end | 32 / 16.462 | 0.5 | 139,027.9 | **0.101** | **0.75** | `1.09×10⁶` | 6.33 | 7.43 | `0.101`, `0.75` — **MATCH** |

---

## 5. UX, hard states, and copy contract (DX1–DX25, UX-19, AI-01, LAB-01)

### 5.1 Archetype & layout

Setup lives inside the existing `ParametricWorkbench` desktop shell (`DESIGN.md` §5, window never scrolls; internal scroll per pane):
- **Left dock (260 px):** Area navigation + Setup brief history (`v1`, `v2`…) and discipline/class-rule preset picker.
- **Center workspace (two-column split inside the fixed window):**
  - **Left column (Setup brief & Goal state):**
    1. `AI-02` language entry slot (`Describe a starting design`, `COPY-79`) at the top in a compact card — renders `COPY-53` (*"No API key configured — every design and analysis tool works without it · Configure key"*) when no key is set; never disables or covers the parameter form below.
    2. **Brief & soft targets** (Purpose / discipline preset, max span, max chord, target area, target AR with per-target weights, taper, rider mass, water salt/fresh + °C, wind band, depth band) — every field shows its provenance badge (`Stated` · `Inferred` · `Defaulted`) and confidence badge (`Verified` · `Inferred` · `Flagged`) using text + icon/border style, never colour alone.
    3. **Goal state & Feasibility** tab/section: editable mass breakdown, operating-point set (with per-point `h_ref`, load, `CL_req`, `σ`, `Re_ref`, `h/c`), and editable `Constraint set` rows showing `"edited by user"` when modified (`GOAL-01`).
  - **Right column (Live preview, diff & checks):**
    1. Graphite `#17272c` planform preview viewport drawing the seeded half-wing + mirror (dashed B-spline control polygon + solid evaluated rails, plus ghosted prior revision when re-seeding), with `WingEstimates` readouts (`≈` prefix, `AR (projected, b²/S)` `COPY-71`), and per-target deviation badges (`target AR 12 · seeded 11.6 · −3 %`).
    2. Conflict notice (`COPY-73`), Flagged acknowledgement gate (`COPY-39`), Re-seed history notice, or Class-rule DRC panel (rule id, version, date, Jump, Exclude with reason).
    3. Action footer: **Seed design** (primary, 44 px / 32 px dense target) or **Re-seed as r`<n+1>`**, plus **Discard changes** / **Open in CAD**.
- **Bottom:** DR-STATUS-1 V2 status strip (`24 px`) showing recipe status (`Linked · Wingfoil race` or `Direct parametric`), open Checks count, active revision, and units.

### 5.2 Hard states (all 6 rendered interactively in `docs/mockups/setup-seeding.html`)

| State ID | Hard state | What must render | Exit / recovery |
|---|---|---|---|
| `clean-race` | Clean start · discipline preset | Wingfoil race defaults + per-field `Stated`/`Inferred`/`Defaulted` badges, GEN fallback chip (`E817 → NACA 4412`), live planform SVG, 7-point `GOAL-02` feasibility table (`Unavailable` for polar-dependent cells) | Acknowledge flagged fields → Seed creates brief v1 + Goal v1 + `r1` |
| `flagged-gate` | Flagged acknowledgement required | `Wakefoil → Surf preset` Flagged wake-speed note + Flagged mass/depth/speed items; **Seed design** disabled (`aria-disabled="true"`) with `COPY-39` until checkbox checked | Check Acknowledge (or enter Stated values) → Seed enables |
| `conflict-targets` | Conflicting soft targets | Both tight-span (`target AR 12 · seeded 11.6 · −3 %`) and `SET-01` conflicting triple (`1000 cm² / 0.8 m / 100 mm`, `w_span = 1` → `span 0.800 m` exact, `S 648 cm² · −35 %`) with `COPY-73` notice; never a hard error; weights editable live | Adjust weights or Seed nearest feasible |
| `class-violation` | Class-rule DRC violation | GWA race preset with `S = 650 cm² < 700 cm²` and `mast = 120 cm > 115 cm` → located DRC findings naming `class.gwa.front_area` & `class.gwa.mast_length`, version `2022-02-01`, date, Jump to field, Exclude with reason; geometry NOT mutated | Raise target area / reduce mast / Exclude with reason |
| `reseed-edited` | Re-seed on an already-edited document | Document at `r4` (`Direct parametric`, `COPY-05`), existing VLM run `k-8a2f1c Current`; preview shows `r4` ghosted vs candidate `r5`, explicit notice that `r1..r4` remain as history, brief v2 appends, and prior runs become `Historical` (`COPY-64`) | Re-seed as `r5` (Undo returns to `r4` & `Current`!) or Cancel |
| `no-key-language` | Language entry slot with no API key | `"Describe a starting design"` (`COPY-79`) card shows `COPY-53` (*"No API key configured — every design and analysis tool works without it · Configure key"*), textarea disabled cleanly; full parameter form below remains 100 % active (`AI-01`, `SET-02`) | Configure key (Settings) or seed offline via parameters |

### 5.3 Copy rows (existing `DESIGN.md` reused + proposed `COPY-SET-*` rows)

**Reused verbatim from `DESIGN.md` §7 (zero changes to `DESIGN.md`):**
`COPY-05` (detachment), `COPY-39` (Flagged acknowledge), `COPY-40` / `COPY-67` / `COPY-109` (Pending admission), `COPY-45` (depth not set), `COPY-49` (out-of-envelope observation), `COPY-52` (outside ITTC table), `COPY-53` (no API key), `COPY-55` (rejected field), `COPY-64` (Historical), `COPY-69` (Undefined), `COPY-70` (Unavailable), `COPY-71` (AR projected), `COPY-73` (soft-target conflict), `COPY-79` (`Describe a starting design`), `COPY-110` (Cite only LINK).

**Proposed new rows (for `DESIGN.md` §7 on build approval; reserved after `COPY-705`; strictly `LAB-01` clean):**

| Proposed ID | Proposed string | Surface |
|---|---|---|
| `COPY-SET-1` | `Seed design` | Primary action (new project) |
| `COPY-SET-2` | `Re-seed as r<n>` | Primary action (already-edited project) |
| `COPY-SET-3` | `Linked · <preset>` | Status strip & Setup header while current revision equals seeded revision |
| `COPY-SET-4` | `Direct parametric` | Status strip & Setup header after first manual edit (`GEO-04`) |
| `COPY-SET-5` | `Stated` · `Inferred` · `Defaulted` | Per-field provenance badges |
| `COPY-SET-6` | `target <name> <target> · seeded <value> · <delta> %` | Soft-target deviation badge (`SET-01`) |
| `COPY-SET-7` | `Preferred section <pref> (<reason>) · Using GEN fallback: <gen>` | Preset section fallback disclosure (`CAT-03`) |
| `COPY-SET-8` | `Acknowledge <n> flagged assumption(s) to enable Seed` | Flagged-field gate checkbox label (`CAT-03`, `SET-04`) |
| `COPY-SET-9` | `Wakefoil uses the Surf preset with Flagged boat-wake speeds (no public wakefoil dataset).` | `wakefoil → surf` mapping disclosure (`SET-04`) |
| `COPY-SET-10` | `Re-seeding appends Setup brief v<k> and Design revision r<n>. Earlier revisions r1–r<n-1> stay in history; existing runs become Historical.` | Re-seed preview notice (`SET-03`) |
| `COPY-SET-11` | `Seeded r<n> from <preset> (brief v<k>, goal v<g>).` | Status strip receipt after Seed |
| `COPY-SET-12` | `Goal state saved as v<g>. Geometry is unchanged.` | Status strip receipt after Goal state edit (`GOAL-03`) |
| `COPY-SET-13` | `edited by user` | Constraint row provenance tag after user edits a preset row (`GOAL-01`) |
| `COPY-SET-14` | `Unavailable — polar data not computed for this section` | Feasibility matrix cell needing section polar (`CAT-03`, `GOAL-02`) |
| `COPY-SET-15` | `Front wing area <S> cm² is below GWA Race minimum 700 cm² (GWA Race Box v2022-02-01, 2022-02-01).` | Class-rule DRC finding (`CAT-04`) |
| `COPY-SET-16` | `Mast length <m> cm exceeds GWA Race maximum 115 cm (GWA Race Box v2022-02-01, 2022-02-01).` | Class-rule DRC finding (`CAT-04`) |
| `COPY-SET-17` | `iQFOiL one-design benchmark: 900 cm² front · 255 cm² tail · shim −2°…+1° (iQFOiL 2025).` | Class-rule benchmark card (`CAT-04`) |
| `COPY-SET-18` | `IKA Formula Kite tolerances: span ±2 mm · chord −1 mm · general ±0.4 mm · mass ±4 % (dimensional maxima Flagged).` | Class-rule tolerance card (`CAT-04`) |
| `COPY-SET-19` | `Open class — no equipment box rules apply.` | Open-class preset card (`CAT-04`) |
| `COPY-SET-20` | `Exclude finding…` / `Exclusion reason is required.` | DRC exclusion dialog (`A5.8`) |
| `COPY-SET-21` | `Tip chord is held at the minimum <min> (max of 5 mm and 2 % of root chord).` | Solver clamp notice when `TipChord.MinimumMeters` binds (Ruling 93) |
| `COPY-SET-22` | `Weights must be finite and non-negative, with at least one weight above 0.` | Inline validation error on soft-target weights |

---

## 6. Telemetry & instrumentation (IO1–IO12)

Operator questions answered by default on the normal path (appended to `AuthoringSession`'s 256-event ring via a typed `SetupTelemetry? Setup` record on `SessionEvent`, mirroring `ExportTelemetry` / `CatalogTelemetry` at `AuthoringSession.cs:54-67`; **no user prose, no file paths, no hostnames**; any unreached measurement stays `null` → `"not recorded"`):

| Operator question | Event `Operation` | Named fields emitted |
|---|---|---|
| How long does live soft-target solving + preview emission take, and how often do targets conflict? | `setup.preview` | `presetId`, `outcome` (`ok` / error code), `durationMs`, `conflict` (bool), `maxRelativeDeviation`, `tipClampActive` (bool), `candidatesEvaluated` (int) |
| How long does full Seed (solve + FoilDSL emit + `Geometry.Assess` + commit) take, and which entry/preset is used? | `setup.seed` / `setup.reseed` | `entryKind` (`parameters` / `language`), `presetId`, `classRuleId`, `briefVersion`, `goalVersion`, `revisionOrdinal`, `flaggedAcknowledgedCount`, `fallbackUsed` (bool), `conflict` (bool), `durationMs`, `solveMs`, `assessMs`, `outcome` |
| How often do users edit Goal state constraints or operating points without touching geometry? | `setup.goal.update` | `goalVersion`, `editedConstraintCount`, `operatingPointCount`, `depthBandChanged` (bool), `waterChanged` (bool), `durationMs`, `outcome` |
| How many class-rule and sanity-bound DRC findings fire, and how often are findings excluded? | `setup.drc.evaluate` / `setup.drc.exclude` | `classRuleId`, `classRuleVersion`, `errorCount`, `warningCount`, `advisoryCount`, `excludedCount`, `durationMs`, `outcome` |
| Is the CLI `seed` verb agreeing with GUI hashes and staying within budget? | `cli.seed` | `presetId`, `surfaceHash12`, `goalHash12`, `durationMs`, `outputBytes`, `outcome` |

---

## 7. Red-first test plan & named mutants (Test Architect floor)

Every test below MUST be observed **RED** against a failing stub or named mutant before implementation turns it GREEN.

| # | Test name (project) | Story / Invariant | Assertion | Named mutant that turns it RED |
|---|---|---|---|---|
| T1 | `Seeder_PrimaryRaceFixture_ExactTargetsAndCertified` (`Core.Tests`) | `SET-01`, `GEO-01` | `(1.10 m, 120 mm, 1000 cm², AR 12, λ 0.24)` yields `b = 1.095445 m`, `S = 1000.00 cm²`, `AR = 12.00` via `WingEstimates.From`, `conflict = false`, `Geometry.Assess == Certified` | Mutant replaces `κ(λ)` with `1.0` (rectangular assumption) → `S` and `AR` miss by ~16 % → RED |
| T2 | `Seeder_ConflictingTriple_WeightSpanOne_ExactSpanAndConflictNotice` (`Core.Tests`) | `SET-01` | `(S 1000 cm², b_max 0.8 m, c_max 100 mm)` with `w = (1, 0, 0, 0)` produces `b = 0.800000 m` within `1e-12`, `S = 648.00 cm²`, `conflict = true`, `COPY-73` populated, `GeometryStatus.Certified`, never throws | Mutant omits lexicographic `J2` tie-breaker (returns `b_lo = 0.10 m`) or throws on conflict → RED |
| T2b | `Seeder_StationaryFamilies_BeatsDenseGridOracle` (`Core.Tests`) | `SET-01` | Across 200 seeded adversarial target/weight combinations (including `c > c_max*` + `AR > AR*` interior stationary winners on fixed-$b$ and $S = S^*$ hyperbola), `RecipeSeeder.Solve` achieves `J1 <= J1_grid + 1e-9` and emitted `WingEstimates.From` agrees with solved `(b, S, AR, c_root, c_tip, MAC)` to `1e-12` relative | Mutant drops fixed-$b$ `c_stat` (`σ_c = 1`) or $S = S^*$ bisection stationary family, or substitutes `κ_elliptic` for `(3+λ)/4` → exceeds grid oracle or `WingEstimates` mismatch → RED |
| T3 | `Seeder_Geo01_AreaAndAr_ExactSqrtSpan` (`Core.Tests`) | `GEO-01` | `S = 0.14 m², AR = 7` produces `b = √0.98 m` within `1e-12` relative on `WingEstimates.From` | Mutant rounds `ExactDecimal` to 3 decimals → fails identity tolerance → RED |
| T4 | `Seeder_TipChordMinimum_NeverViolated` (`Core.Tests`) | Ruling 93 | Extreme small targets (`b_max 0.2 m, c_max 15 mm, λ 0.05`) clamp `c_tip >= TipChord.MinimumMeters(c_root)` and pass `AuthoringSession.Seed` without `DSL-TIP-CHORD-MIN` | Mutant drops `0.005 / λ_eff` lower bound in `Ω` → `AuthoringSession.Commit` refuses with `DSL-TIP-CHORD-MIN` → RED |
| T5 | `Seeder_SameInputsFormAndProposalFixture_EqualSurfaceAndGoalHash` (`Core.Tests`) | `SET-03` | Form brief and recorded `setup-seed` proposal fixture with identical fields yield identical `SurfaceHash` and `GoalStateVersion.ContentHash`, differing only in `entryKind` and provenance | Mutant hashes `entryKind` into `GoalStateVersion` or emits non-deterministic timestamps into FoilDSL → hashes diverge → RED |
| T6 | `Session_ReseedOnEditedDoc_PreservesHistoryAndMarksRunsHistorical` (`Core.Tests` / `Analysis.Tests`) | `SET-03`, `ANA-07` | Seed `r1`, manual edit `r2`, record VLM run (`Current`), re-seed `r3`: `accepted.Count == 3`, `briefs.Count == 2`, run is `Historical`; Undo restores `r2` and run is `Current` again | Mutant overwrites `accepted[0]` or `briefs[0]` in place → count/history/Undo fails → RED |
| T7 | `Session_FirstManualEdit_DetachesRecipeAndUndoRelinks` (`Core.Tests`) | `GEO-04` | After Seed: `RecipeStatus == Linked`; after first dimension/point edit: `RecipeStatus == DirectParametric`; after Undo: `RecipeStatus == Linked` | Mutant stores mutable `bool detached` field not restored on Undo → Undo check RED |
| T8 | `Presets_AllNineDisciplines_CertifiedNoErrorsAndGenFallbackNamed` (`Core.Tests`) | `SET-04`, `CAT-03` | All 9 presets seed valid `Certified` FoilDSL 4.1 with 0 error findings; `wakefoil` maps to `surf` with Flagged note; every pending/LINK preferred section names its GEN fallback + reason; unacknowledged Flagged field refuses Seed (`SETUP-FLAGGED-UNACK`) | Mutant omits `SETUP-FLAGGED-UNACK` guard or leaves `wakefoil` unmapped → RED |
| T9 | `ClassRules_GwaIqfoilFormulaKiteOpen_ExactFindings` (`Core.Tests`) | `CAT-04` | GWA `S = 650 cm²` and `mast = 120 cm` emit located findings with rule id, version `2022-02-01`, date; iQFOiL shows `900 cm²` & `−2°…+1°`; Formula Kite shows `±2 mm`, `−1 mm`; open class reads `"open class"` | Mutant drops version/date or skips `mast > 115 cm` check → RED |
| T10 | `GoalState_EditConstraint_AppendsVersionNeverEditsGeometry` (`Core.Tests`) | `GOAL-01`, `GOAL-03` | Editing a constraint bound appends `goalStates[1]` with `editedByUser = true`, leaves `accepted.Count` and `SurfaceHash` unchanged, and earlier `(goalStateId, contentHash)` still resolves after save/reopen | Mutant mutates `goalStates[0]` in place → old hash lookup fails → RED |
| T11 | `GoalFeasibility_Goal02_SevenPointsAndShallowDepth_ExactOracle` (`Analysis.Tests`) | `GOAL-02` | Pinned race light (10, 14, 18 kn) and race strong (15, 22, 28, 32 kn) at `h_ref = 0.5 m` + 10 kn at `0.3 m` match exact `CL` (3 dec) and `σ` (2 dec) oracle; polar-dependent cells read `Unavailable` | Mutant uses fresh water `ρ = 999.10` or drops `ρgh` hydrostatic term → `CL`/`σ` mismatch → RED |
| T12 | `NativeProject_SetupRoundTrip_ByteEqual_AndPreSetReaderFailsClosed` (`Core.Tests`) | ADR-0011, `DOC-02` | Project with no runs and no setup stays `cfdw-project-1` byte-identical; project with setup round-trips byte-equal; tampered `GoalStateVersion.ContentHash` refused (`DOC-INTEGRITY`); old-build reader refuses `"setup"` with `DOC-UNSUPPORTED-FIELD` (`COPY-130`) | Mutant writes `"setup": null` on unseeded project → breaks `-1` byte-identity → RED |
| T13 | `Cli_SeedAndInspect_AgreesWithGuiBySurfaceAndGoalHash` (`Cli.Tests`) | `CLI-01` | `cfd-workbench seed --preset wingfoil-race ... --out a.cfdw.json` produces exact same `SurfaceHash` and `GoalStateHash` as `AuthoringSession.Seed` | Mutant uses different default `taper` or `WaterTable` temperature in CLI → hash mismatch → RED |
| T14 | `Desktop_SetupHardStates_NoKeyAndLab01Clean` (`Desktop.Tests`) | `AI-01`, `LAB-01`, `UX-19` | Renders all 6 hard states; no-key state disables prompt box with `COPY-53` while parameter Seed stays active; scan of all rendered Setup strings finds 0 `LAB-01` forbidden words | Mutant enables prompt submit without key or injects `"recommended"` into preset badge → RED |

---

## 8. Build node breakdown & size estimate

No code starts until the operator approves `docs/mockups/setup-seeding.html` and `DR-SET-1..5`.

| Build node | Owned files | Dependencies | Est. size | Exit evidence |
|---|---|---|---|---|
| **SET-1 · Core model, solver, presets & store** | `SetupRecords.cs`, `DisciplinePresets.cs`, `RecipeSeeder.cs`, `AuthoringSession.cs` (Seed/UpdateGoalState/NativeProject), `Core.Tests/SetupSeedingTests.cs` | `DR-SET-1`, `DR-SET-3`, `DR-SET-4` | ~900 LOC + ~600 LOC tests | T1–T8, T10, T12 observed red-first → green; `-1` byte-identity intact |
| **SET-2 · Class rules, sanity bounds & Goal feasibility** | `ClassRules.cs`, `SanityBounds.cs` (+ `SanityBounds.json`), `Analysis/GoalFeasibility.cs`, `Analysis/OperatingPoint.cs` (`Load` guard), `Analysis.Tests/GoalFeasibilityTests.cs` | `SET-1`, `DR-SET-2` | ~550 LOC + ~400 LOC tests | T9, T11 observed red-first → green; `GOAL-02` 7-point + 0.3 m exact |
| **SET-3 · CLI `seed` & `inspect --json`** | `Cli/SeedVerb.cs`, `Cli/Program.cs`, `Cli.Tests/SeedCliTests.cs` | `SET-1`, `SET-2` | ~220 LOC + ~180 LOC tests | T13 green; GUI/CLI hash equality proven |
| **SET-4 · Desktop Setup surface & Checks** | `Desktop/Setup/SetupView.axaml(.cs)`, `SetupViewModel.cs`, `GoalStateEditor.axaml(.cs)`, `ChecksDrawer.axaml(.cs)`, `DESIGN.md` (approved `COPY-SET-*`), `Desktop.Tests/SetupViewTests.cs` | `SET-1`, `SET-2`, mockup approval, `DR-SET-5` | ~1,400 LOC + ~500 LOC tests | T14 green; keyboard/a11y + all 6 hard states verified on real display |

---

## 9. Open Decision Requests (`DR-SET-1..5`)

| ID | Decision needed | Recommended default (option a) | Alternative (option b) | Owner | Blast radius if overturned |
|---|---|---|---|---|---|
| **`DR-SET-1`** | Native format version when `Envelope.setup` is present | **(a)** Generalize `cfdw-project-2` (`NativeProject.Format(runCount, hasSetup)` → `-1` iff neither, `-2` iff either; `SET-1` updates `NativeProject.Read` at `AuthoringSession.cs:2631,2641,2643` so a `-2` file with `setup` and 0 runs is admitted); pre-SET readers still fail closed with `DOC-UNSUPPORTED-FIELD` (`COPY-130`) before format check; `.v1.bak` triggers automatically from `-1`. | **(b)** Introduce `cfdw-project-3` whenever `setup` is present, plus `.v2.bak` when upgrading a `-2` file that already has runs. | Operator + Data & Persistence | `NativeProject.Read`, `RunRecord.Format`, `ProjectStore` backup suffix (`SET-1` only) |
| **`DR-SET-2`** | `OperatingPoint.Load` (`RunRecord.cs:76`) relaxation | **(a)** Confirm `Load` = front-wing required load in N (`requiredLoadN`); keep `null` on custom fixed-α runs (A3a/A3d keys unchanged); relax `OperatingPoints.Validate` (`OperatingPoint.cs:61`) to allow finite positive `Load` for Goal-state load-solved runs (coordinated with `SWP`). | **(b)** Keep `OperatingPoint.Load` always `null` and carry `requiredLoadN` only on the Sweep/Experiment case wrapper (not inside the Analysis run key). | Operator + Hydrodynamicist + Data & Persistence | `OperatingPoint.cs:61`, `SWP` run-key contract (`SET-2` + `SWP`) |
| **`DR-SET-3`** | Soft-target deviation norm for `max span` / `max chord` | **(a)** One-sided upper-bound deviation `max(0, (q - q_max)/q_max)` for `max span` & `max chord`, two-sided `|q - q*|/q*` for `target area` & `target AR`, plus lexicographic `J2` tie-breaker on zero-weight targets (so non-conflicting ceilings never distort `S*`/`AR*`, and `SET-01`'s conflicting triple with `w_b = 1` hits `b = 0.800000 m` exact). | **(b)** Treat all four targets as two-sided `|q - q*|/q*` (changes semantics of "max span" to "exact target span", which would distort `S*` or `AR*` whenever `b_max > √(S*·AR*)`). | Operator + Hydrodynamicist | `RecipeSeeder.Solve` (`SET-1` only) |
| **`DR-SET-4`** | `CAT-03` GEN polar availability for initial offline milestone | **(a)** Ship offline Setup with the 3 existing admitted GEN sections (`naca-0009`, `naca-0012`, `naca-4412`) + explicit pending/LINK fallback disclosure, and show `"Unavailable — polar data not computed for this section"` in polar-dependent feasibility cells until the polar tier ships. | **(b)** Block Area 1 Setup build until a precomputed GEN polar dataset (`Re 2×10⁵–2×10⁶`, `Ncrit {2, 4}`) is generated and committed for all 3 GEN sections. | Operator + Hydrodynamicist | `SET-2` sequencing (blocks `SET-2` if option b chosen) |
| **`DR-SET-5`** | Setup surface placement in the CAD-first shell (`DR-7`) | **(a)** Setup is destination 1 on the left activity rail (`Setup · CAD · Analysis · Experiment · Run · Results · Export`), switching the center workspace cleanly in place while preserving selection/camera when toggling to CAD (`UX-04`, `UX-19`). | **(b)** Open Setup as a modal dialog from `File → New from Setup…` (violates A2's seven discrete areas and prevents returning to edit Goal state / Feasibility without reopening a modal). | Operator + Marine-CAD UX | `SET-4` shell integration only |

---

## 10. Adversarial review checklist (for `w5-verifier`)

- **Data & Persistence Architect (hard veto):** Verify `SetupBriefVersion` and `GoalStateVersion` grains, BLAKE3(JCS) content hashes, append-only history on re-seed, `cfdw-project-1` byte-identity when `setup` is absent, fail-closed old-build behavior, and `GoalStateVersion` immutability across save/reopen (`GOAL-03`).
- **Hydrodynamicist:** Verify `GOAL-02` 7-point + `h_ref = 0.3 m` arithmetic, `WaterTable` alignment, `OperatingPoint.Load` confirmation, all 9 discipline presets against KB 04 product specs, and honest `Unavailable` handling where section polars do not yet exist.
- **Marine-CAD UX & Accessibility:** Verify `docs/mockups/setup-seeding.html` across all 6 hard states, Light/Dark themes, `1440×900` / `1024×700` viewports, Reduced Motion, non-colour provenance/confidence cues, keyboard reachability, `UX-19` step count, and zero `LAB-01` forbidden words.
- **Simplifier:** Verify zero second writable geometry AST (`GEO-04`), exact finite-candidate 2D solver with zero external optimization dependency, derived `RecipeStatus` with no shadow flag, and minimal additive schema footprint.
- **Test Architect (hard veto):** Verify all 14 red-first test rows (`T1–T14`) carry falsifiable assertions and named mutants covering every in-scope story (`SET-01`, `SET-03`, `SET-04`, `GEO-01`, `GEO-04`, `CAT-03`, `CAT-04`, `GOAL-01`, `GOAL-02`, `GOAL-03`, `AI-01`, `CLI-01`, `LAB-01`).
