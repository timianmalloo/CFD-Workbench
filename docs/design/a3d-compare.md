---
id: design-a3d-compare
title: A3d Compare — two runs side by side and the Discrepancy record
type: design
status: in-review
owner: "@timianmalloo"
phase: design
tags: [area-3, analysis, compare, discrepancy-record, ana-06, persistence, estimator]
links:
  - {to: design-area3-analysis, rel: refines}
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: mockup-a3d-compare, rel: relates-to}
review-by: 2027-01-31
summary: >-
  The A3d slice of Area 3: compare two Completed Analysis runs (two revisions, or a wing run against the wing
  estimator at the same inputs), show their inputs side by side, give per-point deltas normalised to the base value,
  block every numeric delta when reference quantities or the operating point differ, and store one append-only
  Discrepancy record per compared ordered pair beside the runs in cfdw-project-2. Data model first, then semantics,
  the CLI (an inspect --runs extension), the E7 surface list, the pane and its hard states, the copy rows, a red-first
  test plan and the review packet for the Adversary pass.
---

# A3d Compare — design

Status: **in review**, design half of track A3 (`next-best-six`). No code is written by this document. The build waits
for the Owner's rulings on the DR-A3D lines in §10 and for the Adversary pass (§9).

Confidence labels: **Verified** = I opened the file at `main` `176c9540` in this session; **Inferred** = reasoned, not
observed. `assume:` lines carry a belief, what confirms it, and what breaks if it is false.

## 0. Grounding (quoted)

- **ANA-06** (spec line 1217): *"Given two runs, when compared, then inputs are side by side, per-point deltas are
  normalised to the base value, incompatible reference quantities block a numeric delta, and each compared pair stores
  a Discrepancy record; no averaging or 'best truth'."* Four clauses plus one prohibition.
- **A5.7** (spec): *"Comparison shows per-point deltas normalised to the base revision's value at that point;
  incompatible reference quantities block a numeric delta until reconciled. When tiers disagree, all are shown with
  method, version, inputs, envelope and omissions, a Discrepancy record is stored per compared pair, the value preferred
  by reported uncertainty is the highest tier whose uncertainty is reported (labelled exactly so — never
  'recommended'), and nothing is averaged."*
- **Spec glossary** (line 259): Discrepancy record = *"(evaluation_hi, evaluation_lo, quantity, operating point, δ, ρ,
  per-tier uncertainty where reported) … references the two evaluations (never the reverse) and is materialised because
  it is COMMIT-01 promotion evidence; re-comparing an existing key is a read, not a second row; never averaged"*. Grain
  (line 329): *"one row is one (evaluation_hi, evaluation_lo, quantity, operating point); δ and ρ are non-additive."*
- **ANA-07** (freshness): a run is Historical when its key differs from the current key; nothing writes a flag.
- **Area 3 design** `docs/design/area3-analysis.md`: §3.2 Discrepancy row (*"δ, ρ, uncertainties; references two
  evaluations, never the reverse; re-comparing a key is a read"*); §5.3 estimator *"kept out of … Compare's e
  deltas"* (DR-ANA-11 (a): estimator e = 1, labelled); §15 DR-ANA-4 *"never prune a run a Discrepancy record
  references"*; §16 slice A3d *"tiers and revisions; Discrepancy records"*; §13.5 row ANA-06 names the three tests.
- **Code** (Verified): `RunRecord.cs` holds `AnalysisRecords(Runs, PolarSamples, Pruned)`, `RunRecord.Format` derives
  `cfdw-project-2` from the run count, `CheckStore` holds the store invariants, content hash = BLAKE3(JCS(row without
  `contentHash`)). `AuthoringSession.PruneRunsAtSave` carries condition **C1**: *"a later feature that opens a revision
  outside Undo/Redo (a history jump, A3d Compare) widens ReachableSurfaces in the same change. No Discrepancy record
  exists before A3d."* `AnalysisService.EvaluateAsync` refuses every tier but `VlmStrip` (`ANA-INPUT-TIER`); `Tier.cs`
  says *"the estimator wing tier waits for A3d (P-8)"*. **So the wing estimator does not exist today** — §10 DR-A3D-1.

## 1. Data model (settled first — DM1–DM18)

### 1.1 Bounded context and ubiquitous language

Context **Analysis** (unchanged). New or sharpened terms:

| Term | Kind | Meaning |
|---|---|---|
| **Compare** | operation | reading two Completed, intact runs as an ordered pair and, when they are compatible, recording one Discrepancy record |
| **Base run** | role | the run every δ is normalised to. The pane's first picker; defaults to the run selected in the Analysis panel |
| **Compared run** | role | the other run of the pair (the spec's "other"). Never averaged with the base |
| **Discrepancy record** | fact (aggregate root) | one comparison of one ordered pair of runs under one compare rule; owns one Discrepancy line per compared quantity |
| **Discrepancy line** | value object | (quantity, base value, compared value, δ or Undefined + reason, or Not compared + reason). Exactly the spec's §A3 row grain — (pair, quantity, operating point) — held inside its record |
| **δ** | measure | (compared − base) / \|base\|, a dimensionless fraction; shown as a signed percentage |
| **Compatible** | derived verdict | the two runs share reference quantities **and** operating point exactly (§2.2). Never stored as a flag |
| **Compare rule** | value | `{ id: "cfdw.compare", version: "1.0.0" }`: the quantity set, the δ formula, the floors and the exclusions. A rule change is a version bump, so an old record keeps its meaning |
| **hi / lo** | derived | the spec's evaluation_hi / evaluation_lo: tier rank estimator < polar < vlm-strip. Derived from the two runs' tiers on read; equal tiers (a revision compare) have no hi/lo |

`assume:` the spec's "base value" is the base **run's** value (A5.7 says "base revision's value"; a tier compare has
one revision and two runs). Confirms: the hydrodynamicist lens (§9). Breaks if false: the base of a tier compare would
have to be fixed to one tier; only the default picker changes.

### 1.2 Aggregate and invariant

| Aggregate root | Owns | Invariant | References by identity |
|---|---|---|---|
| **Discrepancy record** | its lines (value objects), the rule, its content hash | (1) the two runs are distinct, Completed and present (never a tombstone); (2) at most one record per comparison key; (3) every line is reproduced exactly by the rule from the two runs, or the record reads Unavailable; (4) nothing on it changes after it is recorded | base run, compared run (`runId`) |

The record references the runs; **a run never references a record** (spec line 305). One aggregate per transaction:
recording a comparison appends one record and touches no run (DM2).

### 1.3 Grain

> **One Discrepancy record is exactly one comparison of one ordered pair of distinct Completed Analysis runs (base,
> compared) under one compare-rule version.**

Comparison key = (`baseRunId`, `comparedRunId`, `rule.id`, `rule.version`). Ordered: swapping base and compared is a
different normalisation and so a different record (§10 DR-A3D-2 records why the record is per pair and not per
quantity row).

**Re-comparing a key is a read** (`Compare_SameKeyTwice_ReadNotNewRecord`): the session looks the key up before it
appends; a hit returns the stored record (re-verified, §1.6) and writes nothing, so the document is not made dirty.
A Completed run is unique per run key (`CheckStore`, Verified), so a run id stands for one evaluation of one key.

### 1.4 Fields, types, units, additivity

```jsonc
// analysis.discrepancies[i] — cfdw-project-2, omitted when empty
{ "id": "uuid v4",
  "contentHash": "blake3-hex over JCS(record without contentHash) — recomputed on read, never trusted",
  "rule": { "id": "cfdw.compare", "version": "1.0.0" },
  "baseRunId": "uuid", "comparedRunId": "uuid",
  "lines": [
    { "quantity": "CL",  "base": 0.4035,  "compared": 0.412,   "delta": 0.0210657 },
    { "quantity": "CDi", "base": 0.00731, "compared": 0.0076,  "delta": 0.0396717 },
    { "quantity": "e",   "base": 0.945276, "compared": 0.947916, "delta": 0.0027926 }
    // or { "quantity": "e", "base": 0.9479, "compared": null, "notCompared": "ANA-CMP-ESTIMATOR-E" }
    // or { "quantity": "CL", "base": 0, "compared": 0.0001, "undefined": "ANA-CMP-BASE-ZERO" }
  ] }
```

| Field | Type · unit | Class | Note |
|---|---|---|---|
| `id` | UUID v4 | identity | random, like `runId` |
| `rule` | id + semver | dimension attribute | in the key |
| `baseRunId`, `comparedRunId` | UUID | references | must name intact Completed runs in the same document |
| `lines[].quantity` | enum `CL` · `CDi` · `e` (rule 1.0.0) | dimension | order fixed by the rule |
| `lines[].base`, `.compared` | double, dimensionless | **non-additive** | wing coefficients; `compared` is null only on a Not-compared line |
| `lines[].delta` | double, dimensionless fraction | **non-additive** (a ratio; never summed or averaged across lines, records or pairs) | present exactly when neither `undefined` nor `notCompared` is |
| `lines[].undefined` | code | — | `ANA-CMP-BASE-ZERO`, `ANA-CMP-BASE-FLOOR` |
| `lines[].notCompared` | code | — | `ANA-CMP-ESTIMATOR-E` |
| `contentHash` | hex64 | integrity | as runs |

No measure here is additive or semi-additive. **ρ** (the spec's multiplicative ratio) is **not stored**: with δ
normalised to the base, ρ = compared / base = 1 + δ exactly, and storing both is two definitions of one quantity (DM7).
Per-tier uncertainty is stored only where a tier reports one; no A3 tier does ("Model uncertainty not quantified"), so
rule 1.0.0 has no uncertainty field. A later rule that adds one is a version bump (expand only).

**Dimensional quantities (L, D_i) are not lines.** Compatibility (§2.2) requires equal ρ, V and S_ref, so
δL ≡ δCL and δD_i ≡ δCDi to the last bit of the formula: a line for each would be a second definition. The pane shows L
and D_i values side by side (§5) without a δ.

### 1.5 δ, and when it is Undefined

δ = (compared − base) / |base|. Dividing by |base| keeps the sign meaning "compared is larger" when the base is
negative (negative CL below α_L0). Undefined — never infinity, never a guess — when:

| Condition | Line | Code |
|---|---|---|
| base = 0 exactly | `undefined` | `ANA-CMP-BASE-ZERO` |
| 0 < \|base\| < floor(quantity) | `undefined` | `ANA-CMP-BASE-FLOOR` |
| a value is not finite or is Unavailable in either run | the pair is **not recorded**; the pane shows the cause | `ANA-CMP-VALUE-UNAVAILABLE` |

Floors (rule 1.0.0): CL 1 × 10⁻⁴, CDi 1 × 10⁻⁶, e 1 × 10⁻³. `assume:` these are the resolution below which a wing
coefficient from the VLM lattice is noise (ANA-04: a symmetric section at α = 0 gives Cl = 0 within 10⁻⁶, so a CL base
of 10⁻⁶ would make δ ~10⁵ %). Confirms: the hydrodynamicist lens (§9 H3). Breaks if false: a line reads Undefined that
could have had a δ, or the reverse; a floor change is a rule version bump, so no stored record changes meaning.

### 1.6 Derive, don't store — what is stored and why that is not a second definition

The values in each line are a **witness**, stored because the spec materialises the record as COMMIT-01 promotion
evidence. There is **one** definition: `Compare.Lines(baseRun, comparedRun, rule)` in `CfdWorkbench.Analysis`. The
writer calls it to build the record; every reader (pane, CLI, the store check) calls it again and compares bit for bit
with the stored lines — the same pattern as `runKey` and `contentHash` ("stored but never trusted; recomputed",
`RunRecord.Verify`, Verified). A stored line that the function does not reproduce makes the record **Unavailable —
Discrepancy record no longer reproduces from its runs**: never silently replaced, never deleted. So the stored number can
never disagree with the computed one without being shown as a defect.

Derived on read, never stored: Compatible / blocked and its field list; hi / lo; ρ; Current / Historical of the record
(§1.7); the headline δ; L and D_i; the "value preferred by reported uncertainty" (§2.5).

The CL, CDi and e a line holds are read through the existing derivations — CL and CDi from `AnalysisProjection`'s wing
group (Trefftz `Loads.InducedDrag`, `Trefftz.Oswald`; Verified that these exist in `Loads.cs` / `Trefftz.cs`), and the
estimator's from `WingEstimator` (DR-A3D-1). `Compare.Lines` calls those functions; it never re-derives CL itself.
`assume:` the projection's CL and CDi are reachable as numbers, not only as formatted strings. Confirms: the build
opens `AnalysisProjection.Build`; if they are strings only, the build lifts the number into a shared function both call
(one definition), it does not parse strings.

### 1.7 History rule

Append-only fact. Every attribute is immutable; a different rule, base or compared run is a different key and a new
record. A record has **no freshness of its own**: each column shows its run's Current / Historical (ANA-07, derived by
key equality now), and the record is "about two stored results", which stay true as facts after the design moves on. A
revision compare always has at least one Historical run (the earlier revision); that is the normal case, not an error.
Historical never blocks δ.

### 1.8 Durable representation, hash, migration, retention

- **Member.** `AnalysisRecords` gains a fourth member `Discrepancies` (`IReadOnlyList<DiscrepancyRecord>?`), declared
  as an optional trailing parameter with `[JsonIgnore(Condition = WhenWritingNull)]` (the pattern `AnalysisRun.Diagnostics`
  uses, Verified), and the session writes it null when the list is empty. JSON name `discrepancies`.
- **Format.** Stays `cfdw-project-2`; `RunRecord.Format(runCount)` is unchanged. A record needs two runs, so a document
  with a record always has runs ≥ 2 and is already `-2`. No new format string.
- **Older documents keep their hash.** A document with no record writes no member, so its bytes, its per-run content
  hashes and the save image hash are unchanged (test `Discrepancy_OmittedWhenEmpty_OlderDocumentBytesUnchanged`). The
  record's own content hash covers only the record. No existing hash covers `AnalysisRecords` as a whole (Verified:
  `ContentHash` is per run).
- **Expand–migrate–contract.** Expand: the reader accepts the member or its absence. Migrate: none (no backfill; DM16 —
  no record existed before A3d). Contract: none. **Rollback:** an older build reading a `-2` file that carries
  `discrepancies` — `assume:` the native reader's schema is closed and refuses an unknown member with
  `DOC-UNSUPPORTED-FIELD` (exit 3 in the CLI exit table, Verified in `Program.cs`). Confirms: the
  build runs today's reader on a fixture with the member. Breaks if false: an older build drops the records silently on
  its next save — then the build must bump the format to `cfdw-project-3` for documents with records, a DR to the Owner.
- **Store invariants** (added to `RunRecord.CheckStore`, so the reader and `RecordDiscrepancy` refuse the same rows with
  the same codes): `id` UUID and unique (`DOC-DISCREPANCY-ID`); key unique (`DOC-DISCREPANCY-KEY`); base ≠ compared;
  both ids name a run in `runs` (not a tombstone) whose outcome is Completed (`DOC-REFERENCE`); `contentHash` hex64
  (`DOC-INTEGRITY`); every number finite; line codes from the closed set; lines in rule order (`DOC-SCHEMA`).
  Per-record integrity (hash, reproduction) is not structural: a failing record stays, Unavailable.
- **Retention (DR-ANA-4, condition C1).** `PruneRunsAtSave` marks `keep[i] = true` for every run whose id a record
  references, before the per-tier quota, so a referenced run is never pruned and never tombstoned. Records are never
  pruned in A3d. Growth: ≤ one record per ordered pair the user actually views (§5.4); measured by the
  `analysis.compare` event's `records` count (§6).
- **Correction 5 — the ownership rule in `RunRecord.cs`.** The header (Verified, lines 8–12) says PRE lands the shape,
  STO owns the bodies, and "VLM, STP, SVC and PRJ … never add members: a missing field is a seam request to STO". Those
  names are the finished A3a build tracks. A3d adds one member under DR-ANA-4 and the store invariants that guard it.
  **Recommendation (DR-A3D-4 (a)):** the A3d build is the record's owner for this one additive member and its checks;
  it amends the header in the same change to: *"… Exception, granted to A3d (DR-A3D-4): AnalysisRecords.Discrepancies
  and its store checks, omitted when empty."* — the same shape as the STP-3 exception already there. The Data &
  Persistence Architect's Adversary pass is the review STO would have given.

## 2. Semantics

### 2.1 What compares

- **Inputs, side by side** (never a δ): revision (ordinal and rail), tier chip, method id + version, settings hash
  (12 hex) with lattice `nSpanPerHalf × nChord`, operating point (V, α, h_ref, datum), water (T, S, ρ, ν), reference
  (S_ref, b_ref, c_ref, moment datum, axes), freshness (Current / Historical with "what changed").
- **Outputs at the point**, with δ: CL, CDi, e (rule 1.0.0). L and D_i side by side without δ (§1.4).
- **Per point.** An A3 run is one operating point, so "per point" is per quantity at that point. Case schedules
  (Area 4) are out of scope; the rule's line list is the extension point.

### 2.2 Compatibility — exact equality, and what it blocks

The pair is **Compatible** exactly when, comparing the two stored manifests:

1. `reference.sRef`, `bRef`, `cRef` equal as doubles (bitwise), and `momentDatum`, `axes` equal as strings; and
2. `op` (speed, pAtm, hRef, datum, alphaDeg, load) and `water` equal (the "point").

Exact, not a tolerance: both runs' reference quantities come from one function (`ReferenceQuantities.From` →
`WingEstimates.From`, Verified) over the accepted source, so equal planforms give equal doubles; a difference in the
last bit means the inputs differ, and saying so is honest. **Incompatible → no numeric δ at all and no record**: the
inputs still show side by side, the differing fields are named, the δ column reads the blocked copy. "Until
reconciled" (A5.7): in A3d the user reconciles by choosing runs that share the quantities; an in-pane "re-express on
the base reference" is not built (DR-A3D-3). Condition 2 is a reading of "per point" (DR-A3D-5).

### 2.3 Tiers and revisions

| Compare | Base (default) | Compared (default) |
|---|---|---|
| **Revisions** (same tier) | the run selected in the Analysis panel | the latest earlier Completed run of the same tier with a different key, in document order |
| **Tiers** ("Compare with estimator") | the selected VLM + strip run | the wing-estimator run at the base run's own inputs (evaluated and recorded if absent — idempotent by run key) |

Either picker can choose any intact Completed run. Failed rows and rows that failed their check are listed disabled with
their reason; they have no values.

### 2.4 The estimator and e

The estimator's e is 1 by construction (DR-ANA-11 (a): "elliptic loading assumed"). Its e line is
`notCompared: ANA-CMP-ESTIMATOR-E` whenever **either** run's tier is `estimator`, and its `compared` (or `base`) is
null — the 1 never enters a δ. The mutant `estimator e = 1 enters the delta` would write δe = (1 − 0.947916) / 0.947916
= +5.49 % for the §5 fixture and turns `Compare_EstimatorE_ExcludedFromEDeltas` red. The estimator's CDi
(= CL² / (π AR), the planar lower bound) **is** compared, with its label shown in the column header.

### 2.5 No averaging, no best truth

No field, function or string combines the two values (no mean, blend, weighted value, winner or rank). The A5.7 clause
"value preferred by reported uncertainty is the highest tier whose uncertainty is reported" is derived on read and, in
A3 where no tier reports one, always reads **"none — no tier reports its uncertainty"** (A3D-COPY-11). "Recommended",
"best", "more accurate" never appear (test `Compare_NoAveraging_NoPreferredValueWithoutUncertainty`).

## 3. The CLI — `inspect <path> --runs` extension

**Choice:** extend `inspect <path.cfdw.json> --runs` (Verified at `Program.cs` line 54 and `InspectRunsAsync`).
**Why:** the CLI is read-only today; a `compare` verb that stores a record would make the CLI a second writer of the
native document (a save path, conflict handling, `.v1.bak`), and one that does not store would break "each compared
pair stores a record". The extension keeps one writer (the session, through the Desktop command) and gives CLI-01
parity: the CLI recomputes every stored record with the same `Compare.Lines` and prints its integrity.

Output: the existing JSON object gains a member `discrepancies`, **omitted when the document has none**, so the output
for every existing document is byte-identical and `schemaVersion` stays `1`:

```json
{"schemaVersion":1,"runs":[…],"pruned":[…],
 "discrepancies":[{"id":"…","baseRunId":"…","comparedRunId":"…","rule":{"id":"cfdw.compare","version":"1.0.0"},
   "integrity":"Intact","lines":[{"quantity":"CL","base":0.4035,"compared":0.412,"delta":0.0210657},
   {"quantity":"e","base":0.947916,"compared":null,"notCompared":"ANA-CMP-ESTIMATOR-E"}]}]}
```

`integrity` ∈ `Intact` · `PayloadFailedCheck` (hash) · `NoLongerReproduces` · `RunFailedCheck` (a referenced run is not
intact). Lines are the **recomputed** values when Intact, the stored values otherwise (labelled by `integrity`). Exit
codes unchanged: 0 success; 2 `DOC-TYPE` / usage / store invariant (`DOC-DISCREPANCY-*`, `DOC-REFERENCE`); 3 version /
unsupported field; 5 I/O; 130 cancelled (Verified table in `ExitForCode`).

## 4. E7 change-surface list (written before the build)

| Surface | File (owned unless marked) | What reaches it |
|---|---|---|
| Store | `src/CfdWorkbench.Core/RunRecord.cs` | `DiscrepancyRecord`, `DiscrepancyLine`, `CompareRule` records; `AnalysisRecords.Discrepancies`; `RunRecord.DiscrepancyHash`; `CheckStore` invariants; header exception (DR-A3D-4); Completed estimator rows without diagnostics (DR-A3D-1) |
| Session | `src/CfdWorkbench.Core/AuthoringSession.cs` | `RecordDiscrepancy` (lookup-then-append under the lock; a hit returns the stored record and writes nothing); `ReadDiscrepancies`; `AnalysisCore` / `LoadRuns` carry the list; `PruneRunsAtSave` keeps referenced runs (C1) |
| Domain | `src/CfdWorkbench.Analysis/Compare.cs` (new) | `Compare.Lines`, `Compare.Compatibility`, `Compare.Key`, `Compare.Verify`, the rule constants and floors |
| Domain (estimator) | `src/CfdWorkbench.Analysis/WingEstimator.cs` (new — **not in the plan row**, DR-A3D-1) | `cfdw.estimator.wing` 1.0.0: Helmbold CLα, CL, CDi = CL²/(πAR), e = 1 labelled |
| Service | `src/CfdWorkbench.Analysis/AnalysisService.cs` (**not in the plan row**, DR-A3D-1) | accept `Tier.Estimator` for the wing scope; record its run through `RecordRun` |
| Projection | `src/CfdWorkbench.Analysis/AnalysisProjection.cs`, `AnalysisViewModel.cs` (**not in the plan row**; one record type) | `CompareViewModel` and the Compare group: every rendered string, the δ format, the blocked / Undefined / Not-compared cells |
| Copy | `src/CfdWorkbench.Analysis/Labels.cs`; `DESIGN.md` §7 | A3D-COPY-1…24 (§6), mapped to final ids after W2 |
| CLI | `src/CfdWorkbench.Cli/Program.cs` | `InspectRunsAsync` adds `discrepancies` |
| Desktop pane | `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml(.cs)` (new) | layout and states of §5 |
| Desktop panel | `src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml(.cs)` | the **Compare** tab |
| Commands | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `src/CfdWorkbench.Desktop/Shell/CommandTable.cs` | `analysis.compare`, `analysis.compare-tiers` |
| Compute reader | `Compare.Verify` on every pane rebuild and CLI read; `Freshness.State` per column | freshness and reproduction re-derived on read |
| Telemetry | `AuthoringSession.RecordAnalysisEvent` (existing entry) and `AnalysisEvent` in `RunRecord.cs` | `analysis.compare` event (§6.1) |
| Tests | `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs` (new); `tests/CfdWorkbench.Cli.Tests/CliTests.cs` (**not in the plan row**); Desktop `--analysis` additions | §7 |

## 5. UI

### 5.1 Where it lives

- A **Compare** tab, the last tab of the Analysis bottom panel (`AnalysisPanel.axaml`, after Provenance; Verified tab
  order: Spanwise loading · Section · Loads · Provenance). The tab exists in Analysis mode only, with the panel.
- Commands (`CommandTable.cs`, View group beside `view.analysis`): **`analysis.compare`** "Compare runs…" — opens the
  tab with the defaults of §2.3; **`analysis.compare-tiers`** "Compare with estimator" — evaluates the estimator at the
  selected run's inputs if absent, then opens the tab with that pair. No new keyboard gesture (none is free by
  evidence; the palette ⌘K reaches both). Enabled only in Analysis with a selected Completed run.

### 5.2 Archetype signature

The shell's signature is unchanged (DESIGN.md frontmatter, Verified: `Type:Configurator; Arch:SpatialBounded;
Layout:ViewportWorkbench; …`). The pane is a facet of it:
`ComparePane { Type:DSS; Arch:SPA; Layout:MasterDetail; Density:Compact; Nav:TaskTabs+CommandPalette;
Input:KeyboardFirst+PrecisionPointer; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Confirmed; Motion:Micro;
Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }` — **routing**: a tab in the panel, no route of its own;
**temporal**: reading is parallel (two columns read at once), entering is serial (pick base, pick compared — two
fields, no wizard); **data**: a fixed three-column table (quantity · base · compared · δ) with a table twin by
construction (it *is* the table; no chart — overlays are ANA-10, out of scope).

### 5.3 Layout (bottom panel, ≥ 1024 px wide)

1. **Header row:** two pickers, *Base run* and *Compared run* (each an option list of A3D-COPY-5 labels), then the
   headline (A3D-COPY-9 / -10) with the record status (A3D-COPY-13 / -14).
2. **Inputs** table: field · base · compared, a differing value in **bold** with a "≠" glyph (not colour alone).
3. **Outputs at this point** table: quantity · base · compared · δ vs base, numerals right-aligned in `numeric`.
   L and D_i rows follow with "—" in δ and the A3D-COPY-16 note.
4. **Footnote:** A3D-COPY-12 (no averaging) and the per-column omissions line reused from the run's labels.

Each column header carries the tier chip (COPY-213 / -214), the method + version and the freshness chip.

### 5.4 States (each one in the mockup)

| State | Trigger | Render | Record |
|---|---|---|---|
| Compatible | two intact Completed runs, §2.2 holds | full tables, δ | appended, or read on a key hit |
| **No second run** | fewer than two intact Completed runs | A3D-COPY-6 with the "Compare with estimator" button (enabled when the one run is VLM + strip) | none |
| Loading | the estimator is being evaluated for "Compare with estimator" | the panel's existing skeleton bars + A3D-COPY-20; pickers disabled | none yet |
| **Incompatible reference** | §2.2 (1) fails | inputs side by side with the differing fields bold; δ cells A3D-COPY-7 once, spanning; headline replaced by A3D-COPY-7 | **none** |
| Different point | §2.2 (2) fails | as above with A3D-COPY-8 | none |
| **Historical base** (or compared) | that run's key ≠ current key | the column's freshness chip "Historical — <what changed>" (COPY-64, existing) and A3D-COPY-15; δ shown | as Compatible |
| Failed run | a Failed row chosen through the palette or selected in the panel | the option is disabled; if it is the selected run, A3D-COPY-17 replaces the tables | none |
| Integrity | a run or the record fails its check | the affected cells A3D-COPY-18 / -19; the record is never rewritten | none written |
| Estimator e | either tier is estimator | e row's δ cell A3D-COPY-11b "Not compared — …" | line `notCompared` |
| Undefined | §1.5 | δ cell A3D-COPY-21 / -22 | line `undefined` |
| Error | `RecordDiscrepancy` refused (e.g. `DOC-SIZE`) | the panel's error card with A3D-COPY-23; **no δ is shown** (an unrecorded δ would break clause 4) | none |

### 5.5 Keyboard, names, contrast

- Tab order: Compare tab → Base picker → Compared picker → Inputs table → Outputs table. Arrow keys move within a
  picker; Enter commits. Both commands reach the pane from the palette. Focus lands on the Base picker when the tab opens.
- Accessible names: pickers "Base run" / "Compared run"; tables "Compared inputs" / "Compared outputs"; each δ cell's
  name reads quantity, value and basis, e.g. "CL, delta plus 2.25 percent of base"; the headline is a polite live
  region; the error card keeps the panel's assertive region (Verified in `AnalysisPanel.axaml`).
- Contrast (DESIGN.md §2 tokens, computed in the mockup's audit strip): ink `#1b2929` and ink-mute `#526362` on surface
  `#fbfcfb`; warning `#895900` text for Historical; danger `#a92e37` for the error card border; dark and contrast themes
  from the `dark-*` and `contrast-*` tokens. Every pair ≥ 4.5:1 text, ≥ 3:1 boundary; the mockup prints the measured ratios.
- δ sign is carried by "+" / "−" text, never by colour; δ is not coloured green/red (no "better/worse" judgement, §2.5).

## 6. Copy table (provisional ids; the build maps them to the next free ids after W2)

| Id | Exact string | Where |
|---|---|---|
| A3D-COPY-1 | Compare | panel tab header |
| A3D-COPY-2 | Compare runs… | command `analysis.compare` |
| A3D-COPY-3 | Compare with estimator | command `analysis.compare-tiers`; empty-state button |
| A3D-COPY-4 | Base run · Compared run | picker labels (two strings) |
| A3D-COPY-5 | r<n> · <tier> · α <a>° · V <v> <unit> · <Current \| Historical> | picker option |
| A3D-COPY-6 | Compare needs two Completed runs. Evaluate another revision, or compare with the estimator. | no second run |
| A3D-COPY-7 | Δ blocked — reference quantities differ: <fields>. Inputs are shown side by side; no difference is computed. | incompatible reference |
| A3D-COPY-8 | Δ blocked — the runs are at different operating points: <fields>. Inputs are shown side by side; no difference is computed. | different point |
| A3D-COPY-9 | Tiers disagree: δ = <v> — Discrepancy record stored · value preferred by reported uncertainty: <tier> | tier headline (spec line 2306, verbatim; δ is CL's) |
| A3D-COPY-10 | Revisions compared: δ CL = <v> — Discrepancy record stored | revision headline |
| A3D-COPY-11 | none — no tier reports its uncertainty | the <tier> slot of A3D-COPY-9 in A3 |
| A3D-COPY-11b | Not compared — the estimator assumes elliptic loading (e = 1) | e δ cell |
| A3D-COPY-12 | Each value is its own run's result. Nothing is averaged or ranked. | footnote |
| A3D-COPY-13 | Comparison recorded | status strip, new record |
| A3D-COPY-14 | Comparison read — recorded earlier | status strip, key hit |
| A3D-COPY-15 | Historical runs compare as recorded: δ is between the two stored results. | Historical note |
| A3D-COPY-16 | Same q · S_ref: L and D_i differ by the same fraction as CL and CDi. | under L, D_i |
| A3D-COPY-17 | A failed run has no values to compare. Choose a Completed run. | failed run |
| A3D-COPY-18 | Unavailable — a compared run failed its check | integrity, run |
| A3D-COPY-19 | Unavailable — Discrepancy record no longer reproduces from its runs | integrity, record |
| A3D-COPY-20 | Evaluating the estimator… | loading |
| A3D-COPY-21 | Undefined — base value is 0 | δ cell |
| A3D-COPY-22 | Undefined — base value below the comparison floor (<floor>) | δ cell |
| A3D-COPY-23 | Comparison not recorded — <reason> (<code>). No difference is shown. | error card |
| A3D-COPY-24 | Unavailable — <quantity> not computed in <run label> | value cell, `ANA-CMP-VALUE-UNAVAILABLE` |

δ format: signed percentage, two decimals, `+2.25 %` / `−0.90 %` (U+2212), invariant culture.

### 6.1 Telemetry

`analysis.compare` through `RecordAnalysisEvent` with new nullable `AnalysisEvent` fields: `compareOutcome`
(`recorded` · `read` · `blocked-reference` · `blocked-point` · `refused`), `baseTier`, `comparedTier`, `records` (count
after), and duration. Null reads "not recorded" (IO8). No file names, no values.

## 7. Test plan — red first

All Analysis tests are ring 0 (every push, Windows and macOS CI), est. < 10 ms each unless noted. "Red before" = what
fails when the test is written against today's tree.

### 7.1 The three §13.5 tests (ANA-06 row)

| Test | Project | Failing input | Red before the code | Mutant it kills |
|---|---|---|---|---|
| `Compare_TwoTiers_DeltaAndDiscrepancyRecord` | Analysis | session with the §5 fixture VLM run (CL 0.4120, CDi 0.00760) and an estimator run at the same inputs; compare base = VLM | `Compare` does not exist (compile red); then: record count 1, CL δ = +0.022465 ± 1e-9, CDi δ = −0.009015 ± 1e-9 | δ normalised to the compared value; record not appended |
| `Compare_SameKeyTwice_ReadNotNewRecord` | Analysis | compare the same ordered pair twice; also the reversed pair once | count stays 1 after the second call, the returned record id is the first's, the document is not dirty; the reversed pair makes count 2 | lookup skipped (count 2); key unordered (reversed = read) |
| `Compare_EstimatorE_ExcludedFromEDeltas` | Analysis | the tier pair above, both orders | e line `notCompared = ANA-CMP-ESTIMATOR-E`, its estimator value null, no `delta` | **estimator e = 1 enters the delta → δe = +0.054946 → red** |

### 7.2 One test per ANA-06 clause

| Clause | Test | Failing input | Red before / mutant |
|---|---|---|---|
| inputs side by side | `Compare_Inputs_SideBySide_AllFieldsBothRuns` | r4 vs r5 | the view model lists every §2.1 input field for both runs, differing ones marked; mutant: drop the settings-hash row |
| δ normalised to base | `Compare_Delta_NormalisedToBase` | base r4 CL 0.4035, compared r5 0.4120 → +0.021066; base −0.2, compared −0.1 → +0.5 | mutants: divide by compared (0.020631), by mean, by signed base (−0.5) |
| (Undefined) | `Compare_BaseZeroOrBelowFloor_Undefined` | base CL 0 and 5e-5 | δ is ±∞ / 1e5 % today's naive division |
| incompatible blocks | `Compare_IncompatibleReference_BlocksDelta` (theory: sRef, bRef, cRef, momentDatum, axes, one per row) | r3 (S_ref 0.100) vs r5 (0.108) | no line has a δ, no record appended, blocked field list = exactly the differing field; mutant: tolerance 1 % lets 0.108 vs 0.1085 through |
| (point) | `Compare_DifferentOperatingPoint_BlocksDelta` | α 3° vs 4° | as above (DR-A3D-5) |
| record stored | `Compare_CompatiblePair_StoresOneRecord` | r4 vs r5 | one record, references both run ids, its lines reproduce; runs untouched (byte-equal) |
| no averaging / best truth | `Compare_NoAveraging_NoPreferredValueWithoutUncertainty` | tier pair | the view model has no combined value; every rendered string is free of "average", "mean", "best", "recommended"; the preferred clause reads A3D-COPY-11 |

### 7.3 Persistence and integrity

| Test | Failing input | Red before / mutant |
|---|---|---|
| `Discrepancy_SaveReopen_RoundTripByteEqual` | save, reopen, save | second image byte-equal; record intact |
| `Discrepancy_OmittedWhenEmpty_OlderDocumentBytesUnchanged` | today's committed `-2` fixture opened and saved with no compare | bytes and every run content hash unchanged; mutant: member written as `[]` |
| `Retention_RunReferencedByDiscrepancy_NeverPruned` | 22 runs on unreachable surfaces in one tier, the oldest referenced by a record | the referenced run survives save, no tombstone; mutant: the keep line removed |
| `Discrepancy_StoreInvariants_Refused` (theory) | duplicate key; base = compared; unknown run id; tombstoned id; Failed run | codes `DOC-DISCREPANCY-KEY`, `DOC-SCHEMA`, `DOC-REFERENCE` on read and on record |
| `Discrepancy_TamperedLine_ReadsNoLongerReproduces` | flip the last digit of a stored δ and fix the content hash | integrity `NoLongerReproduces`; record kept |
| `Discrepancy_TamperedHash_ReadsPayloadFailedCheck` | change a byte, keep the hash | integrity `PayloadFailedCheck` |
| `Discrepancy_OlderReader_RefusesMember` | today's reader on a document with `discrepancies` | expects `DOC-UNSUPPORTED-FIELD`; if it is not refused, the `assume:` in §1.8 is false → stop, DR |
| `EstimatorRun_Completed_NoDiagnostics_Accepted` | estimator run row | refused by today's `CheckRow` (Completed needs diagnostics) — DR-A3D-1 |

### 7.4 CLI (Cli.Tests, ring 0)

`InspectRuns_ListsDiscrepancies_RecomputedLines` (a fixture with one record; JSON as §3) and
`InspectRuns_NoDiscrepancies_OutputUnchanged` (today's fixture; byte-equal output; mutant: `discrepancies: []`).

### 7.5 Desktop `--analysis` (developer machines; samples the rendered pane)

`ComparePane_Compatible_RendersTablesAndHeadline`, `ComparePane_NoSecondRun_EmptyState`,
`ComparePane_IncompatibleReference_BlockedNoDelta`, `ComparePane_HistoricalBase_ChipAndDelta`,
`ComparePane_FailedRun_Refused`, `ComparePane_EstimatorE_NotCompared`, `ComparePane_Error_NoDelta`,
`ComparePane_KeyboardPath_FocusOrderAndNames` (tab order and accessible names of §5.5), and one theme row
(light, dark, contrast) in `ShellWindowTests.cs` only after D1 joins (seam A3 → D1). Each samples the copy string by
its Labels constant, never a re-typed literal.

## 8. Owned paths for the build, and seams

**Plan row (owned):** `src/CfdWorkbench.Core/RunRecord.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs`,
`src/CfdWorkbench.Analysis/Compare.cs` (new), `src/CfdWorkbench.Analysis/AnalysisProjection.cs`,
`src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/Program.cs`,
`src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml`, `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml.cs` (new),
`src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml`, `src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml.cs`,
`src/CfdWorkbench.Desktop/WorkbenchController.cs` (the Compare command rows), `src/CfdWorkbench.Desktop/Shell/CommandTable.cs`,
`tests/CfdWorkbench.Analysis.Tests/CompareTests.cs` (new), the Desktop `--analysis` additions, `DESIGN.md` §7 rows.

**Found by this design, not in the plan row (the leader adds them or the Owner rules DR-A3D-1):**
`src/CfdWorkbench.Analysis/WingEstimator.cs` (new), `src/CfdWorkbench.Analysis/AnalysisService.cs`,
`src/CfdWorkbench.Analysis/AnalysisViewModel.cs`, `src/CfdWorkbench.Analysis/Tier.cs` (its comment only),
`tests/CfdWorkbench.Cli.Tests/CliTests.cs`.

**Seams.** A3 → S1: `WorkbenchController.cs` — A3 adds one contiguous Compare command block beside the Analysis
commands; S1 builds after A3 joins and treats that block as A3's. A3 → D1: a theme row in `ShellWindowTests.cs`, only
after D1 joins.

## 9. Review packet (Adversary pass)

**Hydrofoil hydrodynamicist — δ semantics and the estimator.**
H1. Is δ = (compared − base)/|base| the right reading of "normalised to the base value" for signed CL?
H2. Is exact equality of S_ref, b_ref, c_ref, datum and axes, plus an equal operating point, the right compatibility
rule, or must a planform-changing revision compare be reconcilable (DR-A3D-3)?
H3. Are the floors CL 1e-4, CDi 1e-6, e 1e-3 right, or should Undefined use another basis?
H4. Is comparing the estimator's CDi (a planar lower bound) against VLM CDi honest with its label, and is excluding only
e enough? Should the estimator's α_L0 (DR-A3D-1: the chord-weighted mean of the section-estimator zero-lift angles at
the run's `sectionEtas`) be accepted?
H5. Is dropping L and D_i as δ lines (identical fractions) acceptable?

**Test Architect — mutants and clause coverage.**
T1. Does each ANA-06 clause have a test that turns red on its own mutant (§7.2)?
T2. Does `Compare_EstimatorE_ExcludedFromEDeltas` kill the named mutant in both pair orders?
T3. Is the "no averaging" test meaningful beyond a string scan (the view-model shape check)?
T4. Is the Desktop list enough to prove the rendered surface and the hard states?

**Data & Persistence Architect — grain, hash, retention.**
D1. Grain per ordered pair with lines inside, against the spec's per-quantity row (DR-A3D-2).
D2. Witness values verified on read: is this derive-don't-store, or a cache that should not exist?
D3. Older documents keep bytes and hashes; is the older-reader `assume:` (§1.8) safe, or must records bump the format?
D4. Retention: referenced runs never pruned, records never pruned — the growth bound and its measurement.
D5. The `RunRecord.cs` ownership exception (DR-A3D-4).

## 10. Decision requests (the leader raises them)

| Id | Question | Options | Evidence | Recommendation |
|---|---|---|---|---|
| **DR-A3D-1** | The wing estimator does not exist; tier compare and the named e test need it | (a) A3d builds `cfdw.estimator.wing` 1.0.0 (`WingEstimator.cs`, `AnalysisService` accepts `Tier.Estimator`, a Completed estimator row carries no diagnostics — `CheckRow` changes to "Completed with a solver ⇔ diagnostics") · (b) revisions only; the e test runs on a hand-built estimator row (tests a path no user reaches) · (c) defer tiers; ANA-06 "tiers" unmet | `Tier.cs` P-8, `AnalysisService` `ANA-INPUT-TIER`, `CheckRow` diagnostics rule (all Verified); §16 slice A3d "tiers and revisions" | **(a)**; adds three files to the build and raises its budget |
| **DR-A3D-2** | Record grain | (a) one record per ordered pair, lines inside (each line = the spec row grain) · (b) one row per (pair, quantity) | ANA-06 "each compared pair stores a Discrepancy record" vs spec line 329 | **(a)**; a spec 1.7.x note aligns line 329 if ruled |
| **DR-A3D-3** | Reconcile incompatible references | (a) block only; the user picks compatible runs · (b) a "re-express on the base reference" action for CL and CDi | A5.7 "until reconciled"; e is not re-expressible (it uses each run's own b) | **(a)** for A3d; (b) is a later slice |
| **DR-A3D-4** | `RunRecord.cs` ownership header | (a) A3d owns this additive member, header gains the exception · (b) a seam request to a STO seat that no longer exists | header lines 8–12, Verified | **(a)** |
| **DR-A3D-5** | Different operating points | (a) block δ (reading "per point" as one shared point) · (b) allow δ with a note | spec line 259 grain has one operating point | **(a)**, recorded as a reading, no spec text change |

Not recorded on a blocked pair (§2.2): `assume:` "each compared pair stores a record" means each pair compared
numerically. Confirms: the Owner's ruling on DR-A3D-2. Breaks if false: a blocked record with no lines is added — an
additive change to the same member.

## 11. Failure modes

| Mode | Disposition |
|---|---|
| a referenced run fails its check | record reads A3D-COPY-18; nothing rewritten |
| derivation code changes CL by an ulp | record reads A3D-COPY-19 (NoLongerReproduces); a deliberate change bumps the rule version |
| `RecordDiscrepancy` hits `DOC-SIZE` | error card A3D-COPY-23, no δ shown |
| two compares of the same key race | the lookup and append are one step under the session lock (as `RecordRun`) |
| retention prunes a referenced run | prevented by the keep line; test §7.3 |

STRIDE-lite: no new trust boundary (local document, no network). Tampering is covered by the content hash and
reproduction check. LINDDUN-lite: no personal data; the event carries tiers and counts only.

## Status & next action

Design written; mockup `docs/mockups/a3d-compare.html` with its note. Next: the leader dispatches the Adversary pass
(§9) and raises DR-A3D-1…5; the build brief follows the rulings.
