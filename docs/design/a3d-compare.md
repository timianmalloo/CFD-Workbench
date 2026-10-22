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
  - {to: spec-amendments-1-7-7, rel: relates-to}
review-by: 2027-01-31
summary: >-
  The A3d slice of Area 3, revised after Adversary cycle 1: compare two Completed Analysis runs (two revisions, or the
  wing estimator against a VLM + strip run at the same inputs), show inputs and envelopes side by side, give per-point
  deltas normalised to the base value, block every numeric delta when reference quantities, the operating point or the
  water differ, and store one append-only Discrepancy record per unordered pair in a canonical orientation beside the
  runs in cfdw-project-2. Built as two serial nodes: A3d-1 the estimator tier, A3d-2 Compare.
---

# A3d Compare — design

Status: **in review, repair cycle 1.** The design half of track A3 (`next-best-six`). No code is written by this
document. **Ruling 250** (Owner, 2026-10-22, `docs/notes/rulings.md`) settled Q1 (a), Q2 (c), Q3 (a), Q4 (c) and
Q5 (a); each place it decides cites it. DR-A3D-5 was not part of that request; the design's reading stands. **The build
stays held until the Data & Persistence lens confirms the grain and the spec-note text in §1.0.**

## 1.0 For the Data & Persistence lens — the grain and the spec note, in one place

**Grain (Ruling 250 Q2):**

> One Discrepancy record is exactly one comparison of one unordered pair of distinct Completed Analysis runs, stored
> in canonical orientation (lower tier → higher tier; earlier → later revision), under one compare-rule version. It
> holds one Discrepancy line per compared quantity; a line is exactly the spec's row grain (evaluation_hi,
> evaluation_lo, quantity, operating point). Re-comparing either order is a read, never a second record.

**Spec-note text (Ruling 250 Q5)** — the "After" text of AM-1.7.7-1 to AM-1.7.7-3 in
[`spec-1.7.7.md`](../specs/amendments/spec-1.7.7.md), status proposed:

1. Line 329 (grain): *"one Discrepancy record is one unordered pair of evaluations (evaluation_lo, evaluation_hi) in
   canonical orientation — lower tier to higher tier, or earlier to later revision — under one compare-rule version;
   each of its lines is one (evaluation_hi, evaluation_lo, quantity, operating point); δ is non-additive; re-comparing
   the pair in either order is a read."*
2. Line 259 (fields): ρ is removed from the stored fields: *"ρ = compared / base is derived on read from the two
   evaluations and never stored."*
3. Line 259 (quantities): *"dimensional loads (L, D_i) are not stored lines: at a compatible pair they differ by the
   same fraction as CL and CDi."*

Confidence labels: **Verified** = I opened the file at `main` `176c9540` in this worktree; **Inferred** = reasoned, not
observed. `assume:` lines carry a belief, what confirms it, and what breaks if it is false.

## 0. Grounding (quoted)

- **ANA-06** (spec line 1217): *"Given two runs, when compared, then inputs are side by side, per-point deltas are
  normalised to the base value, incompatible reference quantities block a numeric delta, and each compared pair stores
  a Discrepancy record; no averaging or 'best truth'."*
- **A5.7** (spec lines 1035–1040): *"… When tiers disagree, all are shown with method, version, inputs, envelope and
  omissions, a Discrepancy record is stored per compared pair, the value preferred by reported uncertainty is the
  highest tier whose uncertainty is reported (labelled exactly so — never 'recommended'), and nothing is averaged."*
- **Spec glossary** (line 259): Discrepancy record = *"(evaluation_hi, evaluation_lo, quantity, operating point, δ, ρ,
  per-tier uncertainty where reported) … references the two evaluations (never the reverse) and is materialised because
  it is COMMIT-01 promotion evidence; re-comparing an existing key is a read, not a second row; never averaged"*. Grain
  (line 329): *"one row is one (evaluation_hi, evaluation_lo, quantity, operating point); δ and ρ are non-additive."*
- **ANA-07**: a run is Historical when its key differs from the current key; nothing writes a flag.
- **Area 3 design** `docs/design/area3-analysis.md`: §3.2 Discrepancy row; §5.3 estimator *"kept out of … Compare's e
  deltas"* (DR-ANA-11 (a)); §15 DR-ANA-4 *"never prune a run a Discrepancy record references"*; §16 slice A3d *"tiers
  and revisions"*; §13.5 row ANA-06 names the three tests; P-9 (line 1320) the receipt-plus-forward-guard shape.
- **Code** (Verified): `RunRecord.cs` holds `AnalysisRecords(Runs, PolarSamples, Pruned)` (line 16), `RunSettings.Envelope`
  (line 51), `WaterRecord` (line 64), `OperatingPoint` (line 73), `RunRecord.Format` (lines 222–225), and the diagnostics
  rule in `CheckRow` (line 293): `(run.Outcome is RunOutcome.Completed) == (run.Diagnostics is not null)`.
  `AuthoringSession.cs`: `PruneRunsAtSave` carries condition C1 (lines 2101–2104); `RetainedOthersPerTier = 20`
  (line 1996); `NativeProject.MaxBytes = 8_000_000` (line 2182); the native reader's `Exact` raises
  `DOC-UNSUPPORTED-FIELD` on any member outside its list (lines 2233–2234) and reads the `analysis` member with
  `Exact(analysisElement, "runs", "polarSamples", "pruned")` (line 2254). `AnalysisService.EvaluateAsync` refuses every
  tier but `VlmStrip` (`ANA-INPUT-TIER`); `Tier.cs` says *"the estimator wing tier waits for A3d (P-8)"*.
  `Trefftz.Oswald` computes e = CL² / (π AR CDi) (`Trefftz.cs` lines 35–36).

## 1. Data model (settled first — DM1–DM18)

### 1.1 Bounded context and ubiquitous language

Context **Analysis** (unchanged). New or sharpened terms:

| Term | Kind | Meaning |
|---|---|---|
| **Compare** | operation | reading two Completed, intact runs as a pair and, when they are compatible, recording or reading one Discrepancy record |
| **Base run** | view role | the run every *shown* δ is normalised to. The pane's first picker; defaults to the run selected in the Analysis panel |
| **Compared run** | view role | the other run of the shown pair. Never averaged with the base |
| **Canonical orientation** | stored role | the orientation the record is stored in: **lower tier → higher tier** for a tier pair (estimator < polar < vlm-strip); **earlier → later revision** (accepted-row ordinal, then document order of the run) for a same-tier pair. The stored "base" is the canonical first run (Ruling 250 Q2) |
| **Discrepancy record** | fact (aggregate root) | one comparison of one unordered pair of runs under one compare rule; owns one Discrepancy line per compared quantity |
| **Discrepancy line** | value object | (quantity, base value, compared value, δ, or Undefined + reason, or Not compared + reason), in canonical orientation. Exactly the spec's row grain — (pair, quantity, operating point) — held inside its record |
| **δ** | measure | (compared − base) / \|base\|, a dimensionless fraction; shown as a signed percentage |
| **Compatible** | derived verdict | equal reference quantities, equal operating point and equal water (§2.2). Never stored |
| **Compare rule** | value | `{ id: "cfdw.compare", version: "1.0.0" }`: the quantity set, the δ formula, the floors, the exclusions. A rule change is a version bump |
| **hi / lo** | derived | the spec's evaluation_hi / evaluation_lo by tier rank. In canonical orientation lo is the stored base for a tier pair |

`assume:` the spec's "base value" is the base **run's** value (A5.7 says "base revision's value"; a tier compare has
one revision and two runs). Confirmed by the hydrodynamicist lens in cycle 1 (verdict §2: "the |base| divisor is the
right reading").

### 1.2 Aggregate and invariant

| Aggregate root | Owns | Invariant | References by identity |
|---|---|---|---|
| **Discrepancy record** | its lines (value objects), the rule, its content hash | (1) the two runs are distinct, Completed and present (never a tombstone); (2) at most one record per comparison key; (3) the stored orientation is the canonical one; (4) every line is reproduced exactly by the rule from the two runs, or the record reads Unavailable; (5) nothing on it changes after it is recorded | first run, second run (`runId`) |

The record references the runs; **a run never references a record** (spec line 305). One aggregate per transaction:
recording a comparison appends one record and touches no run (DM2).

### 1.3 Grain and key (Ruling 250 Q2)

> **One Discrepancy record is exactly one comparison of one unordered pair of distinct Completed Analysis runs, stored
> in canonical orientation, under one compare-rule version.**

Comparison key = (`baseRunId`, `comparedRunId`, `rule.id`, `rule.version`) **after** canonical orientation, so the key
of a pair is the same whichever way the user opens it. **Re-comparing either order is a read**
(`Compare_SameKeyTwice_ReadNotNewRecord`): the session orients the pair, looks the key up, and on a hit returns the
stored record (re-verified, §1.6) and writes nothing, so the document is not made dirty. This matches spec line 259
("re-comparing an existing key is a read"). A Completed run is unique per run key (`CheckStore`, Verified), so a run id
stands for one evaluation of one key.

**The reversed view is recomputed on read, never stored (Ruling 250 Q2 correction).** When the user's base is the
canonical second run, the pane calls `Compare.Lines(second, first, rule)` on the two referenced runs — the same one
function, with the roles swapped. It is **not** derived from the stored δ by −δ/(1+δ): with the |base| divisor that
identity holds only for a positive base (base −1, compared −2 gives δ = −1, where the formula divides by zero, while
the true reverse is (−1 − (−2)) / |−2| = +0.5). The reversed line follows §1.5 with the compared run as its base: it is
Undefined when that value is 0 or below the quantity's floor. Only the canonical orientation is stored and verified.

### 1.4 Fields, types, units, additivity

```jsonc
// analysis.discrepancies[i] — cfdw-project-2, omitted when empty; canonical orientation (here: r4 → r5)
{ "id": "uuid v4",
  "contentHash": "blake3-hex over JCS(record without contentHash) — recomputed on read, never trusted",
  "rule": { "id": "cfdw.compare", "version": "1.0.0" },
  "baseRunId": "uuid", "comparedRunId": "uuid",
  "lines": [
    { "quantity": "CL",  "base": 0.4035,  "compared": 0.412,  "delta": 0.0210656753 },
    { "quantity": "CDi", "base": 0.00731, "compared": 0.0076, "delta": 0.0396716826 },
    { "quantity": "e",   "base": 0.9452759, "compared": 0.9479157, "delta": 0.0027926419 }
    // or { "quantity": "e", "base": null, "compared": 0.9479157, "notCompared": "ANA-CMP-ESTIMATOR-E" }
    // or { "quantity": "CL", "base": 0, "compared": 0.0001, "undefined": "ANA-CMP-BASE-ZERO" }
  ] }
```

| Field | Type · unit | Class | Note |
|---|---|---|---|
| `id` | UUID v4 | identity | random, like `runId` |
| `rule` | id + semver | dimension attribute | in the key |
| `baseRunId`, `comparedRunId` | UUID | references | canonical orientation; both name intact Completed runs in the same document |
| `lines[].quantity` | enum `CL` · `CDi` · `e` (rule 1.0.0) | dimension | order fixed by the rule |
| `lines[].base`, `.compared` | double, dimensionless | **non-additive** | wing coefficients; null only on the estimator's side of a Not-compared line |
| `lines[].delta` | double, dimensionless fraction | **non-additive** (a ratio; never summed or averaged across lines, records or pairs) | present exactly when neither `undefined` nor `notCompared` is |
| `lines[].undefined` | code | — | `ANA-CMP-BASE-ZERO`, `ANA-CMP-BASE-FLOOR`, `ANA-CMP-E-ILL-CONDITIONED` |
| `lines[].notCompared` | code | — | `ANA-CMP-ESTIMATOR-E` |
| `contentHash` | hex64 | integrity | as runs |

No measure here is additive or semi-additive. **ρ** (the spec's multiplicative ratio) is **not stored**: with δ
normalised to the base, ρ = compared / base, which is 1 + δ for a positive base — storing both is two definitions of
one quantity (DM7). **L and D_i are not stored lines**: compatibility (§2.2) requires equal ρ_water, V and S_ref, so
δL ≡ δCL and δD_i ≡ δCDi. Both departures from spec lines 259 and 329 (and the per-pair grain of §1.3) are recorded
as a spec note: [`spec-1.7.7.md`](../specs/amendments/spec-1.7.7.md) (Ruling 250 Q5; text in §1.0). Per-tier
uncertainty is stored only where a tier reports one; no A3 tier does ("Model uncertainty not quantified"), so rule
1.0.0 has no uncertainty field. A later rule that adds one is a version bump (expand only).

### 1.5 δ, and when it is Undefined

δ = (compared − base) / |base|. Dividing by |base| keeps the sign meaning "compared is larger" when the base is
negative. Undefined — never infinity, never a guess — when:

| Condition | Line | Code |
|---|---|---|
| base = 0 exactly | `undefined` | `ANA-CMP-BASE-ZERO` |
| 0 < \|base\| < floor(quantity) | `undefined` | `ANA-CMP-BASE-FLOOR` |
| **e line:** the CL line is Undefined, or \|CDi\| of either run is below the CDi floor | `undefined` | `ANA-CMP-E-ILL-CONDITIONED` |
| a value is not finite or is Unavailable in either run | the pair is **not recorded**; the pane shows the cause | `ANA-CMP-VALUE-UNAVAILABLE` |

e = CL² / (π AR CDi) (`Trefftz.cs` lines 35–36, Verified) is a 0/0 form near zero lift: e stays O(1) while it is
noise, so an absolute e floor alone never fires there. The e line is therefore Undefined whenever its inputs are
(cycle-1 condition H2). The e floor (1 × 10⁻³) stays as a last guard only.

Floors (rule 1.0.0): CL 1 × 10⁻⁴, CDi 1 × 10⁻⁶, e 1 × 10⁻³. `assume:` these are the resolution below which a wing
coefficient from the VLM lattice is noise (ANA-04: a symmetric section at α = 0 gives Cl = 0 within 10⁻⁶). Confirms:
the hydrodynamicist lens (not contested in cycle 1). Breaks if false: a line reads Undefined that could have had a δ,
or the reverse; a floor change is a rule version bump, so no stored record changes meaning.

### 1.6 Derive, don't store — what is stored and why that is not a second definition

The values in each line are a **witness**, stored because the spec materialises the record as COMMIT-01 promotion
evidence. There is **one** definition: `Compare.Lines(first, second, rule)` in `CfdWorkbench.Analysis`. The writer calls
it to build the record; every reader (pane, CLI, the store check) calls it again and compares bit for bit with the
stored lines — the same pattern as `runKey` and `contentHash` ("stored but never trusted; recomputed",
`RunRecord.Verify`). A stored line the function does not reproduce makes the record **Unavailable — Discrepancy record
no longer reproduces from its runs**: never silently replaced, never deleted.

Derived on read, never stored: Compatible / blocked and its field list; hi / lo; ρ; the reversed orientation (§1.3);
Current / Historical per column (§1.7); the headline δ; L and D_i; the "value preferred by reported uncertainty" (§2.5).

The CL, CDi and e a line holds are read through the existing derivations — `Loads.InducedDrag` and `Trefftz.Oswald`
(Verified that both exist) for VLM runs, and `WingEstimator` (§2.6) for estimator runs. `Compare.Lines` calls those
functions; it never re-derives CL itself. `assume:` the projection's CL and CDi are reachable as numbers, not only as
formatted strings. Confirms: the build opens `AnalysisProjection.Build`; if they are strings only, the build lifts the
number into one shared function both call, it does not parse strings.

### 1.7 History rule

Append-only fact. Every attribute is immutable; a different rule or pair is a different key and a new record. A record
has **no freshness of its own**: each column shows its run's Current / Historical (ANA-07, derived by key equality
now). A revision compare always has at least one Historical run; that is the normal case. Historical never blocks δ.

### 1.8 Durable representation, hash, migration, retention

- **Member.** `AnalysisRecords` gains a fourth member `Discrepancies` (`IReadOnlyList<DiscrepancyRecord>?`) as an
  optional trailing parameter with `[JsonIgnore(Condition = WhenWritingNull)]` (the `AnalysisRun.Diagnostics` pattern,
  `RunRecord.cs` line 29), written null when the list is empty. JSON name `discrepancies`.
- **Where the types live (Ruling 250 Q4).** `DiscrepancyRecord`, `DiscrepancyLine`, `CompareRule`, the record's
  content hash, its store checks and the `CompareEvent` telemetry fields (§6.1) go in a **new Core file**
  `src/CfdWorkbench.Core/DiscrepancyRecord.cs` with its own ownership header ("A3d owns this file; Compare in
  `CfdWorkbench.Analysis` reads it and never adds members"). `RunRecord.cs` gains **exactly three** changes: the
  `Discrepancies` member, one call from `CheckStore` into `DiscrepancyRecords.Check`, and the Q1 `CheckRow` rule. Its
  header (lines 8–12) gains one exception sentence, on the STP-3 precedent: *"Exception, granted to A3d (Ruling 250):
  AnalysisRecords.Discrepancies (omitted when empty), the one CheckStore call into DiscrepancyRecords.Check, and the
  tier-keyed diagnostics rule in CheckRow."*
- **Format.** Stays `cfdw-project-2`; `RunRecord.Format(runCount)` is unchanged. A record needs two runs, so a document
  with a record already is `-2`.
- **Older documents keep their hash.** A document with no record writes no member, so its bytes, its per-run content
  hashes and the save-image hash are unchanged (test `Discrepancy_OmittedWhenEmpty_OlderDocumentBytesUnchanged`). No
  existing hash covers `AnalysisRecords` as a whole (`ContentHash` is per run, Verified).
- **Expand–migrate–contract.** Expand: the reader accepts the member or its absence. Migrate: none (no record existed
  before A3d; DM16). Contract: none.
- **Rollback (Verified, cycle-1 condition D4).** Today's native reader reads the `analysis` member with
  `Exact(analysisElement, "runs", "polarSamples", "pruned")` (`AuthoringSession.cs` line 2254), and `Exact` raises
  `DOC-UNSUPPORTED-FIELD` on any member outside its lists (lines 2233–2234). So an older build **refuses** a document
  that carries `discrepancies` (CLI exit 3) and cannot drop the records on a later save. The build changes line 2254 to
  pass `discrepancies` as optional (`Exact(analysisElement, ["runs", "polarSamples", "pruned"], ["discrepancies"])`).
- **Diagnostics rule, tier-keyed (Ruling 250 Q1).** `CheckRow` line 293 becomes a rule over a closed set of tier ids:
  a `vlm-strip` row carries diagnostics **if and only if** it is Completed; an `estimator` row **never** carries
  diagnostics, Completed or Failed (a closed-form method has no solver residual; absent, never zero — IO8); any other
  tier id is refused (`DOC-SCHEMA`). Keyed on the stored tier id — not on "has a solver", which is not a stored field.
- **Store invariants** (in `DiscrepancyRecords.Check`, called by `CheckStore`, so the reader and `RecordDiscrepancy`
  refuse the same rows with the same codes): `id` UUID and unique (`DOC-DISCREPANCY-ID`); key unique
  (`DOC-DISCREPANCY-KEY`); base ≠ compared and the pair in canonical orientation (`DOC-SCHEMA`); both ids name a run in
  `runs` (not a tombstone) whose outcome is Completed (`DOC-REFERENCE`); `contentHash` hex64 (`DOC-INTEGRITY`); every
  number finite; line codes from the closed set; lines in rule order (`DOC-SCHEMA`). Per-record integrity (hash,
  reproduction) is not structural: a failing record stays, Unavailable.

#### Retention, and what pinning costs (cycle-1 condition D2)

`PruneRunsAtSave` marks `keep[i] = true` for every run whose id a record references, before the per-tier quota, so a
referenced run is never pruned and never tombstoned (condition C1). Records are never pruned in A3d, and **there is no
unpin path**: every recorded pair pins its two runs for the life of the document.

The bound, against `NativeProject.MaxBytes` = 8,000,000 bytes (Verified, line 2182):

| Item | Bytes | Basis |
|---|---|---|
| one record (three lines) | ≈ 530 | the §1.4 JSON: a 290-byte shell (id, hash, rule, two ids, measured on the literal) + 3 × ≈ 78-byte lines |
| one estimator run | ≈ 1,500 | a manifest with no strips (Inferred from the §5.6 manifest of `area3-analysis.md`) |
| one VLM + strip run, 64 × 2 strips | ≈ 60,000 | 128 strips × ≈ 450 bytes (about 20 numbers, member names and shortest round-trip digits) + manifest. **Inferred** from the `StripLoad` shape (`RunRecord.cs`); the size test measures it |

Records alone would need ≈ 15,000 to reach `MaxBytes`; they are not the constraint. **Pinned runs are.** About
133 VLM runs fit in 8 MB. Retention already keeps the reachable runs and 20 others per tier; each recorded revision
pair can pin up to two more VLM runs that retention would otherwise prune. So **≈ 55 revision pairs with disjoint runs**
(≈ (133 − 20 − reachable) / 2; Inferred) bring a document to `DOC-SIZE` — fewer than 200, so this design does not
invent a cap: **DR-A3D-6** (§10). A tier pair pins one VLM run and one ≈ 1.5 kB estimator run, so it costs at most
≈ 61 kB.

**At `DOC-SIZE` the user sees:** a refused record — the panel's error card with A3D-COPY-23 (*"Comparison not
recorded — the document is at its size limit (DOC-SIZE). No difference is shown."*). Evaluate is refused the same
way (`RecordRun` preflight, Verified). Nothing already stored changes; the user frees space only outside Compare
(DR-A3D-6). Test: `Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` (§7.3).

## 2. Semantics

### 2.1 What compares

- **Inputs, side by side** (never a δ): revision (ordinal and rail), tier chip, method id + version, **validity
  envelope** (`RunSettings.Envelope`, `RunRecord.cs` line 51 — e.g. `vlm-envelope/1`; the estimator's
  `estimator-envelope/1`) with its in/out verdict for the run, settings hash (12 hex) with lattice `nSpanPerHalf ×
  nChord`, operating point (V, α, h_ref, datum), water (T, S, ρ, ν), reference (S_ref, b_ref, c_ref, moment datum,
  axes), the run's **omissions** line (reused from its labels), freshness (Current / Historical with "what changed").
  This is A5.7's "method, version, inputs, envelope and omissions" (spec line 1038; cycle-1 condition H1).
- **Outputs at the point**, with δ: CL, CDi, e (rule 1.0.0). L and D_i side by side without δ (§1.4).
- **Per point.** An A3 run is one operating point, so "per point" is per quantity at that point.

### 2.2 Compatibility — exact equality, three block reasons

The pair is **Compatible** exactly when, comparing the two stored manifests:

1. **reference**: `reference.sRef`, `bRef`, `cRef` equal as doubles (bitwise), and `momentDatum`, `axes` equal as
   strings — else block reason **reference** (A3D-COPY-7);
2. **operating point**: `op` (`OperatingPoint`, `RunRecord.cs` line 73: speed, pAtm, hRef, datum, alphaDeg, load) equal —
   else block reason **point** (A3D-COPY-8);
3. **water**: `water` (`WaterRecord`, `RunRecord.cs` line 64: temperatureC, salinityGPerKg, rho, nu, pv, source,
   tableHash) equal — else block reason **water** (A3D-COPY-25). Water is a separate record from the operating point,
   so it is named as its own cause (cycle-1 condition H5).

When more than one fails, the pane shows each reason on its own line, in the order above. Exact, not a tolerance: the
reference quantities come from one function (`ReferenceQuantities.From` → `WingEstimates.From`, Verified) over the
accepted source, so equal planforms give equal doubles. **Incompatible → no numeric δ at all, and no record**: the
inputs still show side by side and the differing fields are named.

**Reconcile (Ruling 250 Q3).** Any difference in S_ref, b_ref, c_ref, datum or axes blocks every numeric δ, as the
spec clause says. The plain consequence: **a planform-changing revision compare (area, span or mean chord) shows the
inputs side by side but no δ**, and that is the main revision job (the mockup's r3 vs r5). **Next step outside this
wave, for the operator:** option (c) of DR-A3D-3 — δ on the dimensional L and D_i at an equal operating point and
water (the same arithmetic as re-expressing CL and CDi on the base reference) — is a spec amendment candidate; the
operator decides whether it is taken up. It is not built in A3d.

DR-A3D-5 (different points) is **not ruled** (not part of Ruling 250's request); the design's reading stands as
written: block δ, with the water difference named as its own reason.

### 2.3 Tiers and revisions

| Compare | Shown base (default) | Shown compared (default) | Stored orientation (Ruling 250 Q2) |
|---|---|---|---|
| **Revisions** (same tier) | the run selected in the Analysis panel | the latest earlier Completed run of the same tier with a different key, in document order | earlier → later revision |
| **Tiers** ("Compare with estimator") | the selected VLM + strip run | the wing-estimator run at the base run's own inputs (evaluated and recorded if absent — idempotent by run key) | estimator → VLM + strip |

Either picker can choose any intact Completed run. Failed rows and rows that failed their check are listed disabled
with their reason; they have no values.

### 2.4 The estimator, e and CDi

The estimator's e is 1 by construction (DR-ANA-11 (a): "elliptic loading assumed"). Its e line is
`notCompared: ANA-CMP-ESTIMATOR-E` whenever **either** run's tier is `estimator`, and the estimator's side is null —
the 1 never enters a δ. The mutant `estimator e = 1 enters the delta` would show δe = (1 − 0.9479157) / 0.9479157 =
+5.49 % for the §5 fixture and turns `Compare_EstimatorE_ExcludedFromEDeltas` red.

The estimator's CDi = CL² / (π AR) is the planar lower bound, and it **carries the same e = 1 assumption** that the e
exclusion removes. It is compared, and its δ cell carries the label A3D-COPY-26 *"includes the estimator's elliptic
loading assumption (e = 1)"* in the cell itself, not only in a column header (cycle-1 condition H3).

### 2.5 No averaging, no best truth

No field, function or string combines the two values (no mean, blend, weighted value, winner or rank). The A5.7 clause
"value preferred by reported uncertainty is the highest tier whose uncertainty is reported" is derived on read and, in
A3 where no tier reports one, always reads **"none — no tier reports its uncertainty"** (A3D-COPY-11). "Recommended",
"best", "more accurate" never appear. The δ is not coloured green or red.

### 2.6 The wing estimator tier (build node A3d-1; Ruling 250 Q1)

`cfdw.estimator.wing` 1.0.0, in `src/CfdWorkbench.Analysis/WingEstimator.cs`, labelled **"estimator, not validated"**.

- **Lift slope: Helmbold–Diederich with the mid-chord sweep Λ_c/2**, incompressible:
  CLα = 2πA / (2 + √(A² (1 + tan² Λ_c/2) + 4)). Source: the Helmbold (1942) low-aspect-ratio slope with Diederich's
  sweep extension, as given in DATCOM §4.1.3.2 and in Raymer, *Aircraft Design: A Conceptual Approach*, eq. 12.6 (with
  β = 1 and the section-efficiency factor η = 1). At Λ = 0 it equals the Helmbold slope: **4.827235 /rad at A = 7.5**.
  Its behaviour on a swept hydrofoil is **Inferred** (no swept fixture has been compared against it in this repo).
- **CL** = CLα (α − ᾱ_L0), with ᾱ_L0 the chord-weighted mean of the section estimator's zero-lift angles at the run's
  `sectionEtas` (`SectionEstimator.Estimate(...).AlphaL0Deg`, the call `MethodRecord.cs` line 84 already makes; Verified).
- **CDi** = CL² / (π A) — elliptic loading assumed; planar lower bound on induced drag (DR-ANA-11 (a)). **e = 1**, labelled.
- Λ_c/2 is read from the accepted planform. `assume:` Core's `WingEstimates` (or the planform it reads) can give the
  mid-chord sweep. Confirms: the A3d-1 build opens `WingEstimates`; if it cannot, A3d-1 computes it from the planform
  stations it already samples, inside `WingEstimator.cs`.
- Stored as an Analysis run row: tier `estimator`, method `cfdw.estimator.wing` 1.0.0, zero strips, outcome Completed,
  **no diagnostics** (Ruling 250 Q1's tier-keyed rule), `settings.envelope` = `estimator-envelope/1`. Its outputs are derived on read
  from the row's inputs (the accepted source bytes by `AcceptedSourceOf`), never stored.
- `AnalysisService.EvaluateAsync` admits `Tier.Estimator` for the wing scope; it records through `RecordRun`, so the run
  key, idempotency and integrity are the existing ones.

## 3. The CLI — `inspect <path> --runs` extension

**Choice:** extend `inspect <path.cfdw.json> --runs` (Verified at `Program.cs` `InspectRunsAsync`). **Why:** the CLI is
read-only today; a `compare` verb that stores a record would make the CLI a second writer of the native document, and
one that does not store would break "each compared pair stores a record". The extension keeps one writer (the session,
through the Desktop command) and gives CLI-01 parity.

Output: the existing JSON object gains a member `discrepancies`, **omitted when the document has none**, so the output
for every existing document is byte-identical and `schemaVersion` stays `1`:

```json
{"schemaVersion":1,"runs":[…],"pruned":[…],
 "discrepancies":[{"id":"…","baseRunId":"…","comparedRunId":"…","rule":{"id":"cfdw.compare","version":"1.0.0"},
   "integrity":"Intact","lines":[{"quantity":"CL","base":0.4212557233,"compared":0.412,"delta":-0.021971745},
   {"quantity":"e","base":null,"compared":0.9479157,"notCompared":"ANA-CMP-ESTIMATOR-E"}]}]}
```

Records print in canonical orientation (Ruling 250 Q2). `integrity` ∈ `Intact` · `PayloadFailedCheck` · `NoLongerReproduces` ·
`RunFailedCheck`. Exit codes unchanged: 0 success; 2 `DOC-TYPE` / usage / store invariant (`DOC-DISCREPANCY-*`,
`DOC-REFERENCE`); 3 version / unsupported field; 5 I/O; 130 cancelled (Verified in `Program.ExitForCode`).

## 4. E7 change-surface list, by build node

The build is **two serial nodes** (leader decision): **A3d-1** the estimator tier, then **A3d-2** Compare.

### 4.1 A3d-1 — the wing estimator tier (Ruling 250 Q1)

| Surface | File | What reaches it |
|---|---|---|
| Store | `src/CfdWorkbench.Core/RunRecord.cs` | `CheckRow` line 293 tier-keyed diagnostics rule; header sentence |
| Domain | `src/CfdWorkbench.Analysis/WingEstimator.cs` (new) | §2.6 |
| Domain | `src/CfdWorkbench.Analysis/Tier.cs` | the P-8 comment updated; the enum is unchanged |
| Service | `src/CfdWorkbench.Analysis/AnalysisService.cs` | admit `Tier.Estimator` for the wing scope |
| Copy | `src/CfdWorkbench.Analysis/Labels.cs` | "estimator, not validated" (A3D-COPY-27) |
| Tests | `tests/CfdWorkbench.Analysis.Tests/WingEstimatorTests.cs` (new) | §7.0 |

### 4.2 A3d-2 — Compare

| Surface | File | What reaches it |
|---|---|---|
| Store | `src/CfdWorkbench.Core/DiscrepancyRecord.cs` (new, Ruling 250 Q4) | `DiscrepancyRecord`, `DiscrepancyLine`, `CompareRule`, content hash, `DiscrepancyRecords.Check`, `CompareEvent` |
| Store | `src/CfdWorkbench.Core/RunRecord.cs` | `AnalysisRecords.Discrepancies`; one `CheckStore` call (with A3d-1's `CheckRow` rule, the three changes Ruling 250 allows) |
| Session | `src/CfdWorkbench.Core/AuthoringSession.cs` | **line 2254** `Exact(analysisElement, …)` gains `discrepancies` as an optional member; `SessionEvent` gains an optional `Compare` (`CompareEvent`) beside `Analysis`; `RecordDiscrepancy` (orient, look up, append under the lock; a hit returns the stored record and writes nothing); `ReadDiscrepancies`; `AnalysisCore` / `LoadRuns` carry the list; `PruneRunsAtSave` keeps referenced runs (C1) |
| Domain | `src/CfdWorkbench.Analysis/Compare.cs` (new) | `Compare.Lines`, `Compare.Orient`, `Compare.Compatibility`, `Compare.Key`, `Compare.Verify`, the rule constants and floors |
| Projection | `src/CfdWorkbench.Analysis/AnalysisProjection.cs`, `AnalysisViewModel.cs` | `CompareViewModel` and the Compare group: every rendered string, the δ format, blocked / Undefined / Not-compared cells, the reversed view |
| Copy | `src/CfdWorkbench.Analysis/Labels.cs`; `DESIGN.md` §7 | A3D-COPY rows (§6), mapped to final ids after W2 |
| CLI | `src/CfdWorkbench.Cli/Program.cs` | `InspectRunsAsync` adds `discrepancies` |
| Desktop pane | `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml(.cs)` (new) | §5 |
| Desktop panel | `src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml(.cs)` | the **Compare** tab |
| Commands | `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `src/CfdWorkbench.Desktop/Shell/CommandTable.cs` | `analysis.compare`, `analysis.compare-tiers` |
| Compute reader | `Compare.Verify` on every pane rebuild and CLI read; `Freshness.State` per column | re-derived on read |
| Telemetry | `RecordAnalysisEvent` (existing entry) | `analysis.compare` (§6.1) |
| Tests | `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs` (new); `tests/CfdWorkbench.Cli.Tests/CliTests.cs`; Desktop `--analysis` additions | §7.1–7.5 |

## 5. UI

### 5.1 Where it lives

- A **Compare** tab, the last tab of the Analysis bottom panel (`AnalysisPanel.axaml`, after Provenance; Verified tab
  order: Spanwise loading · Section · Loads · Provenance). Analysis mode only.
- Commands (`CommandTable.cs`, View group beside `view.analysis`): **`analysis.compare`** "Compare runs…" opens the tab
  with the §2.3 defaults; **`analysis.compare-tiers`** "Compare with estimator" evaluates the estimator at the selected
  run's inputs if absent, then opens the tab with that pair. No new keyboard gesture; the palette ⌘K reaches both.
  Enabled only in Analysis with a selected Completed run.

### 5.2 Archetype signature

The shell's signature is unchanged (DESIGN.md frontmatter, Verified). The pane is a facet of it:
`ComparePane { Type:DSS; Arch:SPA; Layout:MasterDetail; Density:Compact; Nav:TaskTabs+CommandPalette;
Input:KeyboardFirst+PrecisionPointer; Sync:LocalFirst; Persistence:LocalDevice; Feedback:Confirmed; Motion:Micro;
Pacing:Freeform; Transition:HardCut; A11y:WCAG_2.2_AA; }` — **routing**: a tab in the panel, no route of its own;
**temporal**: reading is parallel (two columns), entering is serial (two pickers); **data**: a fixed table (quantity ·
base · compared · δ) that is its own table twin; no chart (ANA-10 is out of scope).

### 5.3 Layout (bottom panel, ≥ 1024 px wide; stacks at 390 px)

1. **Header row:** *Base run* and *Compared run* pickers (A3D-COPY-5 options), then the headline (A3D-COPY-9 / -10)
   and the record status (A3D-COPY-13 / -14).
2. **Inputs** table: field · base · compared, including the **Envelope** and **Omissions** rows (§2.1); a differing
   value in **bold** with a "≠" glyph (not colour alone).
3. **Outputs at this point**: quantity · base · compared · δ vs base, numerals right-aligned in `numeric`; L and D_i
   follow with "—" in δ and the A3D-COPY-16 note.
4. **Footnote:** A3D-COPY-12.

Each column header carries the tier chip (COPY-213 / -214), method + version and the freshness chip.

### 5.4 States (each one in the mockup)

| State | Trigger | Render | Record |
|---|---|---|---|
| Compatible | two intact Completed runs, §2.2 holds | full tables, δ | appended, or read on a key hit (either order, Q2) |
| **No second run** | fewer than two intact Completed runs | A3D-COPY-6 and the "Compare with estimator" button (enabled when the one run is VLM + strip) | none |
| Loading | the estimator is being evaluated | the panel's skeleton bars + A3D-COPY-20; pickers disabled | none yet |
| **Incompatible reference** | §2.2 (1) fails | inputs side by side, differing fields bold; one spanning δ cell A3D-COPY-7 | **none** |
| Different point | §2.2 (2) fails | as above with A3D-COPY-8 | none |
| Different water | §2.2 (3) fails | as above with A3D-COPY-25 | none |
| **Historical base** (or compared) | that run's key ≠ current key | the column's "Historical — <what changed>" chip (COPY-64) and A3D-COPY-15; δ shown | as Compatible |
| Failed run | a Failed row is the selected run | A3D-COPY-17 replaces the tables; Failed options disabled | none |
| Integrity | a run or the record fails its check | the affected cells A3D-COPY-18 / -19; the record is never rewritten | none written |
| Estimator | either tier is estimator | e δ cell A3D-COPY-11b; CDi δ cell with A3D-COPY-26 | lines `notCompared` / labelled |
| Undefined | §1.5 | δ cell A3D-COPY-21 / -22 / -28 | line `undefined` |
| Error | `RecordDiscrepancy` refused (e.g. `DOC-SIZE`) | the panel's error card with A3D-COPY-23; **no δ shown** | none |

### 5.5 Keyboard, names, contrast

- Tab order: Compare tab → Base picker → Compared picker → Inputs table → Outputs table. Focus lands on the Base picker
  when the tab opens.
- Accessible names: pickers "Base run" / "Compared run"; tables "Compared inputs" / "Compared outputs"; each δ cell
  reads quantity, value and basis ("CL, delta plus 2.25 percent of base"); the headline is a polite live region; the
  error card keeps the panel's assertive region (Verified in `AnalysisPanel.axaml`).
- Contrast from DESIGN.md §2 tokens, measured in the mockup's audit strip for light, dark and high contrast: ≥ 4.5:1
  text, ≥ 3:1 boundaries. δ sign is "+" / "−" text, never colour.

## 6. Copy table (provisional ids; the build maps them to the next free ids after W2)

| Id | Exact string | Where |
|---|---|---|
| A3D-COPY-1 | Compare | panel tab header |
| A3D-COPY-2 | Compare runs… | command `analysis.compare` |
| A3D-COPY-3 | Compare with estimator | command `analysis.compare-tiers`; empty-state button |
| A3D-COPY-4 | Base run · Compared run | picker labels (two strings) |
| A3D-COPY-5 | r<n> · <tier> · α <a>° · V <v> <unit> · <Current \| Historical> | picker option |
| A3D-COPY-6 | Compare needs two Completed runs. Evaluate another revision, or compare with the estimator. | no second run |
| A3D-COPY-7 | Δ blocked — reference quantities differ: <fields>. Inputs are shown side by side; no difference is computed. | reference block |
| A3D-COPY-8 | Δ blocked — the runs are at different operating points: <fields>. Inputs are shown side by side; no difference is computed. | point block (fields from `op` only) |
| A3D-COPY-9 | Tiers disagree: δ = <v> — Discrepancy record stored · value preferred by reported uncertainty: <tier> | tier headline (spec line 2306, verbatim; δ is CL's, in the shown orientation) |
| A3D-COPY-10 | Revisions compared: δ CL = <v> — Discrepancy record stored | revision headline |
| A3D-COPY-11 | none — no tier reports its uncertainty | the <tier> slot of A3D-COPY-9 in A3 |
| A3D-COPY-11b | Not compared — the estimator assumes elliptic loading (e = 1) | e δ cell |
| A3D-COPY-12 | Each value is its own run's result. Nothing is averaged or ranked. | footnote |
| A3D-COPY-13 | Comparison recorded | status, new record |
| A3D-COPY-14 | Comparison read — recorded earlier | status, key hit (either order) |
| A3D-COPY-15 | Historical runs compare as recorded: δ is between the two stored results. | Historical note |
| A3D-COPY-16 | Same q · S_ref: L and D_i differ by the same fraction as CL and CDi. | under L, D_i |
| A3D-COPY-17 | A failed run has no values to compare. Choose a Completed run. | failed run |
| A3D-COPY-18 | Unavailable — a compared run failed its check | integrity, run |
| A3D-COPY-19 | Unavailable — Discrepancy record no longer reproduces from its runs | integrity, record |
| A3D-COPY-20 | Evaluating the estimator… | loading |
| A3D-COPY-21 | Undefined — base value is 0 | δ cell |
| A3D-COPY-22 | Undefined — base value below the comparison floor (<floor>) | δ cell |
| A3D-COPY-23 | Comparison not recorded — <reason> (<code>). No difference is shown. | error card |
| A3D-COPY-24 | Unavailable — <quantity> not computed in <run label> | value cell |
| A3D-COPY-25 | Δ blocked — the runs use different water: <fields>. Inputs are shown side by side; no difference is computed. | water block |
| A3D-COPY-26 | includes the estimator's elliptic loading assumption (e = 1) | CDi δ cell, after the value, when either run is the estimator |
| A3D-COPY-27 | estimator, not validated | estimator method label (A3d-1) |
| A3D-COPY-28 | Undefined — e is ill-conditioned here (CL or CDi undefined or below its floor) | e δ cell |

δ format: signed percentage, two decimals, `+2.25 %` / `−0.90 %` (U+2212), invariant culture.

### 6.1 Telemetry

`analysis.compare` through the session's event queue. The event's outcome string is `recorded` · `read` ·
`blocked-reference` · `blocked-point` · `blocked-water` · `refused`; its fields are a `CompareEvent` record in the new
Core file (Ruling 250 Q4 keeps `AnalysisEvent` in `RunRecord.cs` unchanged): `baseTier`, `comparedTier`, `records`
(count after), `pinnedRuns` (runs kept only because a record references them), carried on `SessionEvent.Compare`;
duration as today. Null reads "not recorded" (IO8). `pinnedRuns` is the measurement DR-A3D-6 needs.

## 7. Test plan — red first

Analysis and Cli tests are ring 0 (every push, Windows and macOS CI), est. < 10 ms each unless noted. "Red before" =
what fails against today's tree. Expected values below were recomputed with Python's `math` module (double precision)
from the stated fixture values, printed to 10 decimals; assertions use tolerance 1 × 10⁻⁹ on these 10-digit values.

### 7.0 Node A3d-1 — the estimator tier

| Test | Failing input | Red before / mutant killed |
|---|---|---|
| `WingEstimator_HelmboldDiederich_SlopeAndInducedDrag` (theory) | row 1: A 7.5, Λ_c/2 0° → CLα 4.8272350086 /rad; with α − ᾱ_L0 = 5° → CL 0.4212557233, CDi = CL²/(π A) = 0.0075314829. Row 2 (hand computation): A 7.5, Λ_c/2 20°: tan² 20° = 0.1324740; A²(1 + tan²) + 4 = 56.25 × 1.1324740 + 4 = 67.70166; √ = 8.228102; CLα = 2π × 7.5 / (2 + 8.228102) = 47.12389 / 10.228102 = **4.6072951356** /rad | `WingEstimator` does not exist (compile red). Mutants: the "+ 4" term dropped (row 1 gives 2π·7.5/9.5 = 4.96031); the sweep term dropped (row 2 gives 4.82724); CDi with e ≠ 1 |
| `EstimatorRun_Completed_NoDiagnostics_Accepted` | an estimator run row, Completed, no diagnostics | refused today by `CheckRow` line 293 (`DOC-SCHEMA`) |
| `VlmRun_Completed_NoDiagnostics_StillRefused` | a `vlm-strip` Completed row with diagnostics removed | passes today; **mutant: the tier-keyed equality deleted** (only "estimator ⇒ none" kept) → accepted → red |
| `EstimatorRun_Failed_WithDiagnostics_Refused` | an `estimator` Failed row carrying diagnostics | mutant: the estimator clause keyed on Completed only |
| `UnknownTier_Refused` | tier `polar-wing` | mutant: an open tier set |
| `Service_EvaluateEstimator_RecordsIdempotent` | evaluate the estimator twice at one key | `ANA-INPUT-TIER` today; mutant: second call appends |

### 7.1 The three §13.5 tests (ANA-06 row; node A3d-2)

The tier test builds its estimator row **from stated fixture values** (tier `estimator`, CL 0.4212557233, CDi
0.0075314829, e = 1), with a test double for the estimator's derived outputs, so the Compare oracle does not depend on
`WingEstimator` (cycle-1 condition T2).

| Test | Failing input | Red before the code | Mutant it kills |
|---|---|---|---|
| `Compare_TwoTiers_DeltaAndDiscrepancyRecord` | the VLM run (CL 0.4120, CDi 0.00760) and the fixture estimator row at the same inputs; shown base = VLM | `Compare` does not exist (compile red); then: record count 1; shown δCL = 0.0224653479, δCDi = −0.0090154117; stored orientation estimator → VLM with δCL = −0.0219717450, δCDi = 0.0090974287 | δ normalised to the compared value; record not appended; stored in the shown (not canonical) orientation |
| `Compare_SameKeyTwice_ReadNotNewRecord` | compare the tier pair twice, then in the reversed order once | count stays **1** after all three (reversed order is a read, count unchanged — Ruling 250 Q2); the returned id is the first's; the document is not dirty | lookup skipped (count 2); key not canonical (reversed = count 2) |
| `Compare_EstimatorE_ExcludedFromEDeltas` | the tier pair, both shown orders | e line `notCompared = ANA-CMP-ESTIMATOR-E`, the estimator side null, no `delta` | **estimator e = 1 enters the delta → δe = +0.0549462 → red** |

### 7.2 One test per ANA-06 clause, and the δ rules

| Clause | Test | Failing input | Red before / mutant |
|---|---|---|---|
| inputs side by side | `Compare_Inputs_SideBySide_AllFieldsBothRuns` | r4 vs r5 | the view model lists every §2.1 field — including Envelope and Omissions — for both runs, differing ones marked; mutant: drop the envelope row |
| δ normalised to base | `Compare_Delta_NormalisedToBase` | base r4 CL 0.4035, compared r5 0.4120 → 0.0210656753; base −0.2, compared −0.1 → +0.5 | mutants: divide by compared (0.0206310680), by mean, by signed base (−0.5) |
| (reversed view) | `Compare_ReversedView_RecomputedFromRuns` (theory) | row 1: the r4→r5 record viewed r5→r4 → δCL = (0.4035 − 0.412) / 0.412 = −0.0206310680; row 2 (negative base, Ruling 250 correction): canonical base CL −1, compared −2 (δ = −1.0) viewed reversed → +0.5; row 3: a canonical compared value of 0 viewed reversed → Undefined (`ANA-CMP-BASE-ZERO`) | mutants: reversed view shows −δ (row 1); reversed view by −δ/(1+δ) (row 2 divides by zero) |
| (Undefined) | `Compare_BaseZeroOrBelowFloor_Undefined` | base CL 0 and 5 × 10⁻⁵ | naive division gives ±∞ / 10⁵ % |
| (e ill-conditioned) | `Compare_ELine_UndefinedWhenClOrCdiUndefined` | CL base 5 × 10⁻⁵ (CL Undefined), e values 0.93 / 0.96; and CDi 5 × 10⁻⁷ | mutant: e floor only → e δ printed |
| incompatible blocks | `Compare_IncompatibleReference_BlocksDelta` (theory) | rows: S_ref 0.100 vs 0.108; **near-equal 0.108 vs 0.1085**; **last ulp: 0.108 vs `Math.BitIncrement(0.108)`**; b_ref; c_ref; momentDatum; axes | no line has a δ, no record, block list = exactly the differing field; mutants: a 1 % tolerance (fails the near-equal row), any epsilon compare (fails the ulp row) |
| (point and water) | `Compare_DifferentPointOrWater_BlocksDelta` (theory) | one row per `OperatingPoint` field (speed, pAtm, hRef, datum, alphaDeg, load) expecting reason **point**, and one per `WaterRecord` field (temperatureC, salinityGPerKg, rho, nu, pv, source, tableHash) expecting reason **water** | mutants: a field left out of the comparison; water reported as point |
| record stored | `Compare_CompatiblePair_StoresOneRecord` | r4 vs r5 | one record, canonical orientation, references both ids, lines reproduce; runs byte-equal; mutant: runs rewritten |
| no averaging / best truth | `Compare_NoAveraging_NoPreferredValueWithoutUncertainty` | tier pair | **shape assertion:** reflection over every numeric member of `CompareViewModel` and its rows; each value must equal, bit for bit, a value of exactly one run or one `Compare.Lines` δ (traced by quantity and side); plus every rendered string free of "average", "mean", "best", "recommended"; the preferred clause reads A3D-COPY-11. Mutant: a `Preferred` or mean member (traces to no single run → red) |

### 7.3 Persistence and integrity

| Test | Failing input | Red before / mutant |
|---|---|---|
| `Discrepancy_SaveReopen_RoundTripByteEqual` | save, reopen, save | today the reader refuses the member (`DOC-UNSUPPORTED-FIELD`, line 2254) → red; mutant: `discrepancies` dropped on load (second image differs) |
| `Discrepancy_OmittedWhenEmpty_OlderDocumentBytesUnchanged` | a `-2` fixture with runs and no record, opened and saved | bytes and every run content hash unchanged; mutant: member written as `[]` |
| `Retention_RunReferencedByDiscrepancy_NeverPruned` | 22 runs on unreachable surfaces in one tier, the oldest referenced by a record | the referenced run survives save, no tombstone; mutant: the keep line removed |
| `Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` (ring 0, est. < 2 s) | runs with 128 strips pinned by records until the next record or run would pass `MaxBytes` | records the measured bytes per run and per record (the §1.8 numbers become Verified); the next `RecordDiscrepancy` raises `DOC-SIZE`, the pane state is Error, the saved image is unchanged; mutant: preflight skipped → an oversize image |
| `Discrepancy_StoreInvariants_Refused` (theory) | duplicate key; base = compared; non-canonical orientation; unknown run id; tombstoned id; Failed run | codes `DOC-DISCREPANCY-KEY`, `DOC-SCHEMA`, `DOC-REFERENCE` on read and on record; mutant: the orientation check removed |
| `Discrepancy_TamperedLine_ReadsNoLongerReproduces` | flip the last digit of a stored δ and fix the content hash | integrity `NoLongerReproduces`; record kept |
| `Discrepancy_TamperedHash_ReadsPayloadFailedCheck` | change one stored byte, keep the hash | integrity `PayloadFailedCheck`; mutant: the stored hash trusted (reads Intact) |
| `Discrepancy_OlderReader_Receipt` + `NativeReader_AnalysisMember_ClosedSet` | **receipt** (P-9 shape, `area3-analysis.md` line 1320): at `176c9540`, `AuthoringSession.cs` line 2254 reads `Exact(analysisElement, "runs", "polarSamples", "pruned")` and `Exact` refuses other members (lines 2233–2234), recorded in the build's proof note. **Forward guard:** a document with an unknown `analysis` member (`discrepanciesX`) is refused with `DOC-UNSUPPORTED-FIELD` by the new reader | mutant: line 2254 made open (any member allowed) → the guard goes green-on-unknown → red |

### 7.4 CLI (Cli.Tests, ring 0)

| Test | Failing input | Red before / mutant |
|---|---|---|
| `InspectRuns_ListsDiscrepancies_RecomputedLines` | a fixture with one record | no `discrepancies` member today; mutant: stored lines printed without recompute (a tampered fixture shows Intact) |
| `InspectRuns_NoDiscrepancies_OutputUnchanged` | today's fixture | output byte-equal; mutant: `discrepancies: []` |

### 7.5 Desktop `--analysis` (developer machines; samples the rendered pane)

Every check reads its copy through the Labels constant, never a re-typed literal. Each is red today because the
Compare tab does not exist.

| Check | Input | Mutant it kills |
|---|---|---|
| `ComparePane_Compatible_RendersTablesAndHeadline` | r4, r5 | headline from the wrong quantity (CDi) |
| `ComparePane_Envelope_SideBySide` | r5 vs estimator | the Envelope row omitted |
| `ComparePane_NoSecondRun_EmptyState` | one run | the empty state replaced by an empty table |
| `ComparePane_IncompatibleReference_BlockedNoDelta` | r3, r5 | a δ cell rendered under the block |
| `ComparePane_DifferentWater_OwnReason` | water ρ differs | water shown with the point copy |
| `ComparePane_HistoricalBase_ChipAndDelta` | base r4 | the δ hidden for a Historical run |
| `ComparePane_FailedRun_Refused` | a Failed selected run | a Failed option enabled |
| `ComparePane_EstimatorE_NotCompared_CdiLabelled` | tier pair | the CDi cell without A3D-COPY-26 |
| `ComparePane_Error_NoDelta` | `DOC-SIZE` on record | δ shown beside the error card |
| `ComparePane_KeyboardPath_FocusOrderAndNames` | open the tab by command | focus not on the Base picker; a picker without an accessible name |

One theme row (light, dark, contrast) in `ShellWindowTests.cs` only after D1 joins (seam A3 → D1).

## 8. Owned paths for the build, by node, and seams

**A3d-1 (estimator tier):** `src/CfdWorkbench.Analysis/WingEstimator.cs` (new), `src/CfdWorkbench.Analysis/AnalysisService.cs`,
`src/CfdWorkbench.Analysis/Tier.cs` (comment), `src/CfdWorkbench.Analysis/Labels.cs` (A3D-COPY-27),
`src/CfdWorkbench.Core/RunRecord.cs` (`CheckRow` line 293 and the header sentence),
`tests/CfdWorkbench.Analysis.Tests/WingEstimatorTests.cs` (new). Ruling 250 Q1.

**A3d-2 (Compare), after A3d-1 joins:** `src/CfdWorkbench.Core/DiscrepancyRecord.cs` (new; Ruling 250 Q4),
`src/CfdWorkbench.Core/RunRecord.cs` (the member, one `CheckStore` call, `AnalysisEvent` fields),
`src/CfdWorkbench.Core/AuthoringSession.cs` (line 2254, `RecordDiscrepancy`, `ReadDiscrepancies`, `AnalysisCore`,
`LoadRuns`, `PruneRunsAtSave`), `src/CfdWorkbench.Analysis/Compare.cs` (new),
`src/CfdWorkbench.Analysis/AnalysisProjection.cs`, `src/CfdWorkbench.Analysis/AnalysisViewModel.cs`,
`src/CfdWorkbench.Analysis/Labels.cs`, `src/CfdWorkbench.Cli/Program.cs`,
`src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml`, `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml.cs` (new),
`src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml`, `src/CfdWorkbench.Desktop/Analysis/AnalysisPanel.axaml.cs`,
`src/CfdWorkbench.Desktop/WorkbenchController.cs` (the Compare command block),
`src/CfdWorkbench.Desktop/Shell/CommandTable.cs`, `tests/CfdWorkbench.Analysis.Tests/CompareTests.cs` (new),
`tests/CfdWorkbench.Cli.Tests/CliTests.cs`, the Desktop `--analysis` additions, `DESIGN.md` §7 rows.

**Seams.** A3 → S1: `WorkbenchController.cs` — A3d-2 adds one contiguous Compare command block beside the Analysis
commands; S1 builds after A3 joins. A3 → D1: a theme row in `ShellWindowTests.cs`, only after D1 joins.

## 9. Review packet

### 9.1 What changed in cycle 1

| Condition | Where |
|---|---|
| D1 / T8 canonical orientation, reversed view recomputed (Ruling 250 Q2) | §1.1, §1.3, §1.4, §2.3, §3, §7.1, §7.2, §10; mockup Revisions / Historical base; spec note |
| D2 pinning cost and bound; size test; DR-A3D-6 | §1.8 Retention, §6.1 `pinnedRuns`, §7.3, §10 |
| D3 tier-keyed diagnostics rule (Ruling 250 Q1) | §1.8, §2.6, §7.0, §10 |
| D4 older reader Verified at line 2254 | §0, §1.8 Rollback, §4.2 Session row |
| D5 ρ, L and D_i as a spec note (Ruling 250 Q5) | §1.4; `docs/specs/amendments/spec-1.7.7.md` |
| H1 envelope (and omissions) side by side | §2.1, §5.3, §7.2, §7.5; mockup Inputs table |
| H2 e Undefined when CL or CDi is | §1.5, A3D-COPY-28, §7.2 |
| H3 estimator CDi δ labelled in the cell | §2.4, A3D-COPY-26, §7.5; mockup Tiers |
| H4 planform consequence; option (c) (Ruling 250 Q3) | §2.2, §10 DR-A3D-3 |
| H5 water as its own block reason; DR-A3D-5 settled | §2.2, A3D-COPY-25, §5.4, §7.2, §7.5; mockup Different water |
| H6 Helmbold–Diederich with Λ_c/2, "estimator, not validated" | §2.6, A3D-COPY-27, §7.0 |
| T1 10-digit expectations | §7 preamble, §7.1 |
| T2 fixture-built estimator row; `WingEstimator` test | §7.0, §7.1 |
| T3 `VlmRun_Completed_NoDiagnostics_StillRefused` | §7.0 |
| T4 near-equal and ulp rows | §7.2 |
| T5 receipt plus forward guard | §7.3 |
| T6 point and water theory | §7.2 |
| T7 a mutant or today-failing input per row; reflection shape check | §7.2–7.5 |
| Q4 new Core file | §1.8, §4.2, §8 |
| Build split A3d-1 / A3d-2 | §4, §7.0, §8 |

### 9.2 Questions for a second pass

- Hydrodynamicist: is the Helmbold–Diederich citation and the ᾱ_L0 rule (§2.6) acceptable for "estimator, not
  validated"? Is the CDi label (A3D-COPY-26) enough?
- Test Architect: does each §7 row now carry a red-before input or a named mutant?
- Data & Persistence Architect: is the §1.8 bound, with DR-A3D-6 open, acceptable for the build to start?

## 10. Decision requests

| Id | Question | Options | Evidence | Recommendation | State |
|---|---|---|---|---|---|
| **DR-A3D-1** | The wing estimator does not exist; tier compare and the e test need it | (a) A3d builds `cfdw.estimator.wing` 1.0.0 as node A3d-1 · (b) revisions only; the e test on a hand-built row · (c) defer tiers · (d, from the verdict) the estimator's own row rule keyed on the tier id, not "with a solver" — folded into (a) | `Tier.cs` P-8; `AnalysisService` `ANA-INPUT-TIER`; `CheckRow` line 293 (Verified); §16 "tiers and revisions" | **(a) with (d)**: Helmbold–Diederich (§2.6), tier-keyed diagnostics rule | **settled — Ruling 250 Q1 (a)** |
| **DR-A3D-2** | Record grain and key | (a) one record per ordered pair · (b) one row per (pair, quantity) · (c, from the verdict) one record per **unordered** pair in canonical orientation, lines inside, reversed view recomputed on read | spec lines 259, 329; ANA-06 "each compared pair stores a Discrepancy record" | **(c)** | **settled — Ruling 250 Q2 (c)** |
| **DR-A3D-3** | Reconcile incompatible references | (a) block every numeric δ (the spec clause); every planform-changing revision compare is blocked · (b) re-express CL and CDi on the base reference · (c, from the verdict) δ on dimensional L and D_i at an equal point and water (the same arithmetic as (b)) | A5.7 "until reconciled"; e is not re-expressible | **(a)** for A3d; (c) a recorded follow-up, not built | **settled — Ruling 250 Q3 (a); (c) is a next step outside this wave for the operator** |
| **DR-A3D-4** | Where the record's types and checks live | (a) in `RunRecord.cs` under a header exception · (b) a seam request to the finished STO track · (c, from the verdict) a new Core file with its own header; `RunRecord.cs` gains only the member and one call | `RunRecord.cs` header lines 8–12 | **(c)** | **settled — Ruling 250 Q4 (c)** |
| **DR-A3D-5** | Different operating points | (a) block δ, with water as its own reason · (b) allow δ with a note | spec line 259 grain has one operating point; `WaterRecord` ≠ `OperatingPoint` | **(a)** | **not ruled** (outside Ruling 250's request); the reading stands |
| **DR-A3D-6** | Records pin runs with no unpin path; ≈ 55 disjoint revision pairs reach `MaxBytes` (§1.8, Inferred until the size test measures it) | (a) an explicit **Record** action: viewing never stores; the user records a pair (changes "each compared pair stores" to "each recorded pair") · (b) a record-retention rule: keep the latest N records per tier pair kind, tombstone the rest and release their pins · (c) accept: no cap; `DOC-SIZE` refuses, the `pinnedRuns` event measures real use | §1.8 bound; `RetainedOthersPerTier` = 20; spec line 259 "materialised because it is COMMIT-01 promotion evidence" | **(c)** for A3d with the measurement, revisit at the first `pinnedRuns` reading above 20; (b) would delete promotion evidence, (a) changes the spec clause | **open — new in cycle 1** |

Blocked pairs store nothing: `assume:` "each compared pair stores a record" means each pair compared numerically.
Ruling 250 Q2 does not address it. Confirms: the Data & Persistence lens at its §1.0 confirmation. Breaks if false: a
blocked record with no lines is added — an additive change.

## 11. Failure modes

| Mode | Disposition |
|---|---|
| a referenced run fails its check | record reads A3D-COPY-18; nothing rewritten |
| derivation code changes CL by an ulp | record reads A3D-COPY-19; a deliberate change bumps the rule version |
| `RecordDiscrepancy` hits `DOC-SIZE` | error card A3D-COPY-23, no δ shown (§1.8) |
| two compares of the same pair race, in either order | orient, look up and append are one step under the session lock |
| retention prunes a referenced run | prevented by the keep line; §7.3 |
| an older build opens a document with records | refused at line 2254 (`DOC-UNSUPPORTED-FIELD`); cannot drop records |

STRIDE-lite: no new trust boundary (local document, no network). Tampering is covered by the content hash and the
reproduction check. LINDDUN-lite: no personal data; the event carries tiers and counts only.

## Status & next action

Cycle-1 revision written with Ruling 250 folded in; the mockup and its note updated; spec note 1.7.7 proposed. Next:
the Data & Persistence lens confirms §1.0 (the build is held until it does); the leader runs the second Adversary pass;
then the build brief for node A3d-1, and A3d-2 after it joins. DR-A3D-6 is open.
