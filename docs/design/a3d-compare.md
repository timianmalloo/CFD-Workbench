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
  - {to: spec-amendments-1-7-8, rel: relates-to}
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
stays held until the Data & Persistence lens confirms the grain and the spec-note text in §1.0.** That lens confirmed both, with conditions, in cycle 2 (folded below; §9.1b).

## 1.0 For the Data & Persistence lens — the grain and the spec note, in one place

**Grain (Ruling 250 Q2):**

> One Discrepancy record is exactly one comparison of one unordered pair of distinct Completed Analysis runs, stored
> in canonical orientation (lower tier → higher tier; earlier → later revision), under one compare-rule version. It
> holds one Discrepancy line per compared quantity; a line is exactly the spec's row grain (evaluation_hi,
> evaluation_lo, quantity, operating point). Re-comparing either order is a read, never a second record.

**Spec-note text (Ruling 250 Q5)** — the "After" text of AM-1.7.7-1 to AM-1.7.7-3 in
[`spec-1.7.7.md`](../specs/amendments/spec-1.7.7.md), status proposed:

1. Line 329 (grain): *"one Discrepancy record is one unordered pair of evaluations, stored as a first (base) and a
   second (compared) evaluation — lower tier first, else earlier accepted revision first, else earlier recorded run
   first — under one compare-rule version; each of its lines is one (pair, quantity, operating point); δ is
   non-additive; re-comparing the pair in either order is a read. A blocked pair stores no record."* (wording per the
   cycle-2 confirmation, condition 6)
2. Line 259 (fields): ρ is removed from the stored fields: *"ρ = compared / base is derived on read from the two
   evaluations and never stored."* The clause "not a second row" becomes *"not a second record"*; *"A3d stores pairs
   of Analysis runs only; a pair that includes a Polar sample (which this line allows) is an additive later change."*
   (condition 7)
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

**Rule versions are kept (confirmation condition 3).** `Compare.Lines` dispatches on the record's **stored**
`rule.version`. A rule bump adds a new function and **keeps the old one**, so a past record is re-verified by the rule
that wrote it and never reads "no longer reproduces" just because the rule moved on. A record whose rule id or version
the build does not know reads **Unavailable — unknown compare rule** (code `ANA-CMP-RULE-UNKNOWN`, A3D-COPY-29); it is
never re-verified under another rule, never rewritten, never deleted.

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
- **One definition of orientation and key, in Core (confirmation condition 1).** Canonical orientation and the
  comparison key are defined **once**, in `DiscrepancyRecord.cs` (`DiscrepancyRecords.Orient`, `DiscrepancyRecords.Key`),
  because `DiscrepancyRecords.Check` must enforce both and Core cannot reference `CfdWorkbench.Analysis`.
  `Compare.Orient` and `Compare.Key` in the Analysis project call the Core functions and define nothing of their own
  (DM7).
- **The ordinal lookup (confirmation condition 2).** The orientation check needs each run's accepted-row ordinal
  (document order of `accepted`) and the run's own document order. `CheckStore` today receives only
  `Func<string, bool> acceptedExists` (`RunRecord.cs` line 253, Verified). The build passes one more argument,
  `Func<string, int?> acceptedOrdinal` (null when the id names no accepted row), from both callers — the native reader
  and `AuthoringSession.RecordRunCore` / `RecordDiscrepancy`, which hold `accepted` — and the run's document order is
  its index in `records.Runs`. This is a parameter of the one `CheckStore` call Ruling 250 allows, not a fourth change
  to `RunRecord.cs`. Tie-break for a same-tier pair: earlier accepted ordinal first, then earlier run in document order.
- **Recorded as a decision note (confirmation condition 9).** The `discrepancies` member, its grain and key, these
  invariants, the pinning rule, the expand-only migration and the older-reader refusal are recorded in the linked
  decision note [`docs/notes/discrepancy-records.md`](../notes/discrepancy-records.md), which refines ADR-0011
  (DM13); ADR-0011 stays unedited.

#### Retention, and what pinning costs (cycle-1 condition D2)

`PruneRunsAtSave` marks `keep[i] = true` for every run whose id a record references, before the per-tier quota, so a
referenced run is never pruned and never tombstoned (condition C1). The build **rewrites the C1 doc comment** on
`PruneRunsAtSave` (`AuthoringSession.cs` about lines 2097–2104) in the same change, to say C1 is met by keeping
referenced runs by id: Compare reads runs and opens no revision outside Undo/Redo (confirmation condition 5). Records are never pruned in A3d, and **there is no
unpin path**: every recorded pair pins its two runs for the life of the document.

The bound, against `NativeProject.MaxBytes` = 8,000,000 bytes (Verified, line 2182):

| Item | Bytes | Basis |
|---|---|---|
| one record (three lines) | 859 (measured) | the §1.4 JSON in the indented document, measured by the size test (`Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted`, the `MEASURED` line) |
| one estimator run | ≈ 1,500 | a manifest with no strips (Inferred from the §5.6 manifest of `area3-analysis.md`) |
| one VLM + strip run, 64 × 2 strips | 109,991 (measured, a **lower bound**) | 128 strips in the indented document, measured by the size test. The test strips carry 10-digit values; a solver prints up to 17 digits, which gives about 127 kB per run and about 63 runs in `MaxBytes` (**Inferred**, a digit-count model) |

Records alone would need ≈ 15,000 to reach `MaxBytes`; they are not the constraint. **Pinned runs are.** About 72 VLM runs fit in 8 MB at the measured 109,991 bytes (8,000,000 / 109,991; about 63 at solver precision, Inferred). Retention already keeps the reachable runs and 20 others per tier; each recorded revision
pair can pin up to two more VLM runs that retention would otherwise prune. So a few tens of revision pairs with disjoint runs
(about (72 − 20 − reachable) / 2 at the lower bound; Inferred) bring a document to `DOC-SIZE` — fewer than 200, so this design does not
invent a cap: **DR-A3D-6** (§10). A tier pair pins one VLM run and one ≈ 1.5 kB estimator run, so it costs at most
about 111 kB (lower bound).

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
- **CL** = CLα (α − (ᾱ_L0 − ε̄)), with ᾱ_L0 the chord-weighted mean of the section estimator's zero-lift angles at the run's
  `sectionEtas` (`SectionEstimator.Estimate(...).AlphaL0Deg`, the call `MethodRecord.cs` line 84 already makes; Verified)
  and ε̄ the chord-weighted mean geometric twist at the same stations, nose-up positive as the lattice reads it (strip
  α_eff = α + twist − α_i). Chord and twist are the placed frame's (`Placement.Sections`, the lattice's own source), never a
  second definition (A3d-1 repair, review condition 1). The Λ_c/2 form is DATCOM 4.1.3.2 at β = 1, κ = 1 (Raymer's eq. 12.6
  is the same form on the maximum-thickness-line sweep); Λ_c/2 is the straight root-to-tip mid-chord line, an approximation on
  a curved planform.
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

Records print in canonical orientation (Ruling 250 Q2). `integrity` ∈ `Intact` · `PayloadFailedCheck` · `NoLongerReproduces` · `RuleUnknown` ·
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
| Store | `src/CfdWorkbench.Core/DiscrepancyRecord.cs` (new, Ruling 250 Q4) | `DiscrepancyRecord`, `DiscrepancyLine`, `CompareRule`, content hash, **the one definition of canonical orientation and the key** (`DiscrepancyRecords.Orient`, `.Key`), `DiscrepancyRecords.Check` (enforces both, with the `acceptedOrdinal` lookup), `CompareEvent` |
| Store | `src/CfdWorkbench.Core/RunRecord.cs` | `AnalysisRecords.Discrepancies`; one `CheckStore` call (with A3d-1's `CheckRow` rule, the three changes Ruling 250 allows) |
| Session | `src/CfdWorkbench.Core/AuthoringSession.cs` | **line 2254** `Exact(analysisElement, …)` gains `discrepancies` as an optional member; `SessionEvent` gains an optional `Compare` (`CompareEvent`) beside `Analysis`; `RecordDiscrepancy` (orient, look up, append under the lock; a hit returns the stored record and writes nothing); `ReadDiscrepancies`; `AnalysisCore` / `LoadRuns` carry the list; `PruneRunsAtSave` keeps referenced runs by id, and its C1 doc comment is rewritten to say so (confirmation condition 5); both `CheckStore` callers pass `acceptedOrdinal` |
| Domain | `src/CfdWorkbench.Analysis/Compare.cs` (new) | `Compare.Lines` (dispatch on the stored rule version, §1.7), `Compare.Orient` and `Compare.Key` (call the Core definitions; define nothing of their own), `Compare.Compatibility`, `Compare.Verify`, the rule constants and floors |
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
| A3D-COPY-29 | Unavailable — Discrepancy record uses a compare rule this version does not know (<rule id> <version>) | record, `ANA-CMP-RULE-UNKNOWN` |

δ format: signed percentage, two decimals, `+2.25 %` / `−0.90 %` (U+2212), invariant culture.

### 6.1 Telemetry

**Per save (Ruling 251).** Every save emits the pinned-run count and the save image's byte size (on the `analysis.save` event, emitted on every save of a document that holds runs, and on `analysis.compare`; a `DOC-SIZE` refusal is `analysis.size`; through `CompareEvent` fields `pinnedRuns` and `documentBytes`); a value not measured reads "not recorded", never zero. A `DOC-SIZE` refusal in a document that holds records is emitted with its `pinnedRuns` count, so it can be told apart from an ordinary size refusal.

`analysis.compare` through the session's event queue. The event's outcome string is `recorded` · `read` ·
`blocked-reference` · `blocked-point` · `blocked-water` · `refused`; its fields are a `CompareEvent` record in the new
Core file (Ruling 250 Q4 keeps `AnalysisEvent` in `RunRecord.cs` unchanged): `baseTier`, `comparedTier`, `records`
(count after), `pinnedRuns` (runs kept only because a record references them), `documentBytes` (the size of the save image at the event), carried on `SessionEvent.Compare`;
duration as today. Null reads "not recorded" (IO8). `pinnedRuns`, `documentBytes` and a `refused` outcome with code `DOC-SIZE` are the measurements the DR-A3D-6 trigger reads (§10).

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
| `Discrepancy_PinnedRuns_ReachDocSize_RefusedNotCorrupted` (ring 0, est. < 2 s; Ruling 251) | runs with 128 strips pinned by records until the next record, run or save would pass `MaxBytes`; its measured bytes per pinned run and per record replace the Inferred ≈ 60 kB and ≈ 55-pair figures of §1.8 and §10; the refusal event carries `pinnedRuns` | records the measured bytes per run and per record (the §1.8 numbers become Verified); the next `RecordDiscrepancy` raises `DOC-SIZE`, the pane state is Error, the saved image is unchanged; mutant: preflight skipped → an oversize image |
| `Discrepancy_StoreInvariants_Refused` (theory) | duplicate key; base = compared; non-canonical orientation (tier pair stored higher → lower); **a same-tier, same-revision pair stored against document order** (the later run as base); unknown run id; tombstoned id; Failed run | codes `DOC-DISCREPANCY-KEY`, `DOC-SCHEMA`, `DOC-REFERENCE` on read and on record; mutant: the orientation check removed |
| `Discrepancy_TamperedLine_ReadsNoLongerReproduces` | flip the last digit of a stored δ and fix the content hash | integrity `NoLongerReproduces`; record kept |
| `Discrepancy_TamperedHash_ReadsPayloadFailedCheck` | change one stored byte, keep the hash | integrity `PayloadFailedCheck`; mutant: the stored hash trusted (reads Intact) |
| `Discrepancy_UnknownRuleVersion_ReadsUnavailable` | a stored record with `rule.version` `9.9.9` (and fixed content hash) | integrity `RuleUnknown`, pane A3D-COPY-29, record kept; mutant: verified under the current rule (reads `NoLongerReproduces`) |
| `Discrepancy_OldRuleVersion_StillReproduces` | a record written by rule `1.0.0` read by a build whose current rule is a fixture `1.1.0` | Intact (dispatched to the `1.0.0` function); mutant: dispatch to the current rule only |
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
`src/CfdWorkbench.Core/RunRecord.cs` (the `Discrepancies` member and the one `CheckStore` call with its `acceptedOrdinal` argument; `CompareEvent` lives in `DiscrepancyRecord.cs`),
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

### 9.1b What changed in cycle 2 (Data & Persistence confirmation conditions)

| Condition | Where |
|---|---|
| 1 orientation and key defined once in Core | §1.8 Store invariants, §4.2 `DiscrepancyRecord.cs` and `Compare.cs` rows |
| 2 `acceptedOrdinal` lookup for the orientation check; same-revision refused row | §1.8, §4.2 Session row, §7.3 `Discrepancy_StoreInvariants_Refused` |
| 3 dispatch on the stored rule version; `ANA-CMP-RULE-UNKNOWN`, A3D-COPY-29 | §1.7, §3, §6, §7.3, §11 |
| 4 DR-A3D-6 trigger, document byte size, residual risk; settled by Ruling 251 | §6.1, §7.3, §10; decision note |
| 5 the C1 doc comment rewritten | §1.8 Retention, §4.2 Session row |
| 6 stored roles named; "A blocked pair stores no record." | `docs/specs/amendments/spec-1.7.7.md` AM-1; §10 settled text |
| 7 "not a second record"; Analysis runs only | `spec-1.7.7.md` AM-2 |
| 8 no `AnalysisEvent` fields in `RunRecord.cs` | §8 |
| 9 linked decision note | `docs/notes/discrepancy-records.md`, linked from §1.8 |

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
| **DR-A3D-6** | Records pin runs with no unpin path; a few tens of disjoint revision pairs reach `MaxBytes` (§1.8: 109,991 bytes per run measured, a lower bound; about 72 runs) | (a) an explicit **Record** action: viewing never stores; the user records a pair (changes "each compared pair stores" to "each recorded pair") · (b) a record-retention rule: keep the latest N records per tier pair kind, tombstone the rest and release their pins · (c) accept: no cap; `DOC-SIZE` refuses, the `pinnedRuns` event measures real use | §1.8 bound; `RetainedOthersPerTier` = 20; spec line 259 "materialised because it is COMMIT-01 promotion evidence" | **(c)** for A3d ; revisit at **the first `pinnedRuns` reading above 20 or the first `DOC-SIZE` refusal in a document that holds records, whichever comes first** (the §6.1 event carries the document byte size). Residual risk (Inferred, the Data & Persistence confirmation): with no unpin and no record pruning, a document that reaches `DOC-SIZE` through pinned runs can never record a run or a comparison again, which is why the trigger fires on that event. (b) is refused by the Data & Persistence lens (it deletes COMMIT-01 evidence); (a) is the preferred escape if the trigger fires | **settled — Ruling 251 (c)**: accept and measure; every compatible compared pair stores its record, a blocked pair stores nothing; referenced runs kept by identity on top of the 20 per tier, never pruned or tombstoned; (b) refused as a contract change; (a) adopted only by the operator on the leader's request after the trigger fires. **Residual risk accepted for A3d** |

**Blocked pairs store nothing — settled** by the Data & Persistence confirmation (cycle 2): Compatible / blocked is derived and never stored, a blocked pair has no δ and is no COMMIT-01 evidence, and it would pin two runs for nothing. Spec note AM-1.7.7-1 says so.
A later blocked-record shape would be additive (runs are immutable, so a blocked pair stays blocked).

## 11. Failure modes

| Mode | Disposition |
|---|---|
| a referenced run fails its check | record reads A3D-COPY-18; nothing rewritten |
| derivation code changes CL by an ulp | record reads A3D-COPY-19; a deliberate change bumps the rule version and keeps the old version's function (§1.7) |
| a record names a rule version this build does not know | reads A3D-COPY-29 (`ANA-CMP-RULE-UNKNOWN`); never re-verified under another rule, never rewritten |
| `RecordDiscrepancy` hits `DOC-SIZE` | error card A3D-COPY-23, no δ shown (§1.8) |
| two compares of the same pair race, in either order | orient, look up and append are one step under the session lock |
| retention prunes a referenced run | prevented by the keep line; §7.3 |
| an older build opens a document with records | refused at line 2254 (`DOC-UNSUPPORTED-FIELD`); cannot drop records |

STRIDE-lite: no new trust boundary (local document, no network). Tampering is covered by the content hash and the
reproduction check. LINDDUN-lite: no personal data; the event carries tiers and counts only.

## 12. Follow-up AF — dimensional δL and δD_i when only the reference differs (Ruling 250 Q3 (c))

Track DF of `next-best-six` wave 2 designs this; the AF build implements it. It takes up DR-A3D-3 (c), which Ruling 250
Q3 left to the operator. The main session reads the operator's 2026-10-22 instruction ("get a3d compare … follow up
implemented and complete") as taking it up; **DR-DF-1** asks the leader to confirm that reading before the build. The
spec text changes in [`spec-1.7.8.md`](../specs/amendments/spec-1.7.8.md) (proposed). Labels as in §1.0: **Verified** =
opened at `main` `b51315c4`; **Inferred** = reasoned.

### 12.1 What changes, in five lines

1. When the **only** block reason is **reference** (§2.2 item 1) — the operating point and the water are equal — the
   pane shows δL and δD_i, normalised to the base value, and the values of both runs.
2. CL and CDi still have **no δ**. Their cells give today's reason (reference quantities differ). The coefficient
   values show side by side.
3. e stays **Not compared**: it depends on each wing's own b_ref and cannot be put on a common reference (A3 Adversary).
4. **Nothing new is stored.** A reference-blocked pair stays a blocked pair: no Discrepancy record, no pinned run
   (Ruling 251; AM-1.7.7-1 "A blocked pair stores no record"). The dimensional lines are derived on read (DM7).
5. A point or water block (alone or with reference) is unchanged: no δ at all.

At a **compatible** pair nothing changes: L and D_i stay side by side without δ, under A3D-COPY-16, because there
δL = δCL and δD_i = δCDi (AM-1.7.7-3). The dimensional δ exists only where the coefficient δ is blocked.

### 12.2 Data rule (first)

**Grain of the derived line (not stored):** one dimensional line is exactly one (shown pair in the shown orientation,
dimensional quantity ∈ {L, D_i}, the one shared operating point). It is **non-additive** (a ratio); never summed or
averaged across lines, pairs or points.

**One definition of the dimensional load.** Today `CompareProjection.Loads` (`AnalysisProjection.cs` lines 163-174,
Verified) computes `L = CL · q · S_ref` and `D_i = CDi · q · S_ref` per run from `context.Values(run)`, with
`q = ½ ρ V²` of that run. The build moves that arithmetic into Core-free Analysis code as **one** function,
`Compare.Dimensional(AnalysisRun run, Coefficients c) → (double? L, double? Di)`, in newtons, each run with **its own**
q and S_ref. The projection's side-by-side values and the δ both call it; nothing else recomputes L. CL and CDi come
from the existing derivation (`WingCoefficients.Of` for VLM runs, `Compare.Derivation` for estimator runs; Verified,
`Compare.cs` lines 23-44, 63-92), so the estimator's D_i carries e = 1 as its CDi does (§2.4).

`assume:` L from CL · q · S_ref equals the near-field strip lift to rounding, because CL is that lift over q · S_ref
(`Compare.cs` lines 25-28, Verified). Confirms: the AF test `Dimensional_RoundTrip_EqualsStripLift` (|ΔL| ≤ 1e-12 · |L|).
Breaks if false: the pane's L disagrees with the Analysis panel's "Lift L" row; the fix is to derive both from one call,
not to tune a tolerance.

**δ definition.** δX = (X_compared − X_base) / |X_base| for X ∈ {L, D_i}, with each X from `Compare.Dimensional` of its
own run. Since q is equal (equal V and equal ρ are part of the compatibility of point and water),
δL = (CL₂ S₂ − CL₁ S₁) / |CL₁ S₁| — the A3 verdict's "δCL re-expressed on the base reference". The build computes it
from the dimensional values, never from coefficient δ (one formula, one rounding path).

**Undefined (the §1.5 codes, reused).**

| Condition | Code |
|---|---|
| X_base = 0 exactly | `ANA-CMP-BASE-ZERO` |
| the base run's coefficient is below its floor: \|CL_base\| < 1 × 10⁻⁴ for L, \|CDi_base\| < 1 × 10⁻⁶ for D_i | `ANA-CMP-BASE-FLOOR` |
| a coefficient, q or S_ref is not finite or Unavailable in either run | the line shows the value cell A3D-COPY-24 and no δ (`ANA-CMP-VALUE-UNAVAILABLE`) |

The floor is the **coefficient** floor, so δL is Undefined exactly when δCL would be. An absolute floor in newtons would
change meaning with V and S_ref. The rendered floor is the dimensional equivalent of the base run
(AF-COPY-6).

**Reversed view.** As §1.3: the pane calls the same function with the roles swapped. Never −δ, never −δ/(1+δ).

**Stored? No — and why.** (1) Ruling 250 Q5 kept L and D_i out of stored lines, and Ruling 251 says the build adds no
stored line without a ruling. (2) The record is COMMIT-01 promotion evidence between **tiers**; a tier pair runs at the
base run's own inputs (§2.3), so a reference-blocked pair is always a revision pair and never promotion evidence.
(3) Storing would pin two runs per viewed planform pair, against the §1.8 size budget (Ruling 258's 64 MB cap; DR-A3D-6
trigger). (4) Runs are immutable, so the line is reproducible from them at any time; a stored copy would be a second
definition (DM7). A later wish to store it is a new rule version with an `L` / `D_i` line kind — expand only, its own
ruling (DR-DF-2).

### 12.3 E7 surface list (AF build)

| Surface | File | What reaches it |
|---|---|---|
| Store | `src/CfdWorkbench.Core/DiscrepancyRecord.cs`, `RunRecord.cs` | **none** (no stored change; the build proves the document bytes are unchanged) |
| Domain | `src/CfdWorkbench.Analysis/Compare.cs` | `Compare.Dimensional`; `Compare.DimensionalLines(base, compared, values)` returns two `DiscrepancyLine`-shaped values (not records) when `Compatibility` returns exactly one block with reason Reference; `Record` emits outcome `blocked-reference` with `dimensional = true` in the event |
| Projection | `src/CfdWorkbench.Analysis/AnalysisProjection.cs` (`CompareProjection`) | the reference-only branch (today lines 55-65 return no outputs): CL / CDi rows with values and AF-COPY-2, the e row AF-COPY-3, L / D_i rows with δ and AF-COPY-4; the existing compatible-pair `Loads` rows call `Compare.Dimensional` |
| View model | `src/CfdWorkbench.Analysis/AnalysisViewModel.cs` | new `CompareState.ReferenceDiffers` (Blocked stays "no δ at all"); its doc comment |
| Copy | `src/CfdWorkbench.Analysis/Labels.cs`; `DESIGN.md` §7 (mapped at build) | AF-COPY-1..7 |
| Desktop pane | `src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml(.cs)` | renders the ReferenceDiffers state: notice, coefficient rows without δ, L / D_i δ cells, spoken names |
| CLI | `src/CfdWorkbench.Cli/Program.cs` | **none**: `inspect --runs` lists stored records only, and nothing new is stored |
| Compute reader | `Compare.DimensionalLines` on every pane rebuild | derived on read; no verify step (nothing stored) |
| Telemetry | `RecordCompareEvent` (existing) | the `blocked-reference` event gains `dimensional: true / false` so "how often is the dimensional view used" is answered (IO) |
| Spec | `docs/specs/cfd-workbench-v1.md` lines 172, 259, 1217 | only after AM-1.7.8-1..3 are accepted, by the spec owner |

`assume:` `CompareEvent` can take one optional boolean without a stored-shape change, because session events are not
document lines. Confirms: the build opens `RecordCompareEvent` and `CompareEvent` in `DiscrepancyRecord.cs`. Breaks if
false: the event is persisted in the document; then the build emits the flag only in the trace and records why.

### 12.4 UI and copy (provisional AF-COPY ids; mapped to final ids at build)

Layout as §5.3. The notice band holds AF-COPY-1 instead of A3D-COPY-7; the output table keeps its five rows in the order
CL, CDi, e, L, D_i. Differing reference fields are marked in the inputs table as today (≠ glyph and bold, never colour
alone). Headline AF-COPY-5. Status line: no "Comparison recorded" (nothing is stored); AF-COPY-7.

| Id | Exact string | Where |
|---|---|---|
| AF-COPY-1 | Coefficient Δ blocked — reference quantities differ: <fields>. L and D_i are compared: same operating point and water. | notice (reference-only block) |
| AF-COPY-2 | Blocked — reference differs | CL and CDi δ cells |
| AF-COPY-3 | Not compared — e depends on each wing's own span (b_ref) | e δ cell |
| AF-COPY-4 | Each run's own q · S_ref. Derived, not stored. | note under L and D_i |
| AF-COPY-5 | Revisions compared: δ L = <v> at the same operating point — reference quantities differ | headline |
| AF-COPY-6 | <n> N (CL floor 1E-4 × q × S_ref of the base) · <n> N (CDi floor 1E-6 × q × S_ref of the base) | the `<floor>` slot of A3D-COPY-22 on an L or D_i cell (units follow the panel: N or lbf) |
| AF-COPY-7 | Comparison shown — not recorded: reference quantities differ | status line |

Spoken names follow §5.5: "L, delta plus 10.28 percent of base"; "CL, blocked, reference differs". The D_i δ cell carries
A3D-COPY-26 when either run is the estimator (§2.4). δ format as §6.

**Hard states.** Value Unavailable in one run → A3D-COPY-24 in that cell, no δ in that row, the other row unaffected.
Historical run → its chip and A3D-COPY-15, δ shown (as §5.4). Failed run → unchanged (A3D-COPY-17). Reference and point
(or water) both differ → today's Blocked state with both reasons, no δ (mutant guard below).

### 12.5 Red-first tests (AF build)

Fixture **F-PLANFORM** (the mockup's r3 vs r5 pattern): both runs Completed, same `op` (V 5.0 m/s, α equal) and same
`water` (ρ 1025.0); base S_ref 0.100 m², CL 0.4035, CDi 0.00731; compared S_ref 0.108 m², CL 0.4120, CDi 0.0076;
q = 12 812.5 Pa. L 516.984375 → 570.105 N; D_i 9.3659375 → 10.5165 N. Values recomputed in double precision from these
inputs (Python `math`, scratch, not in the tree), printed to 10 decimals; tolerance 1 × 10⁻⁹. Red before: today the
branch returns no outputs (`AnalysisProjection.cs` line 65, Verified), so every δ assertion fails.

| Test | Input | Expected | Mutant it kills |
|---|---|---|---|
| `Compare_ReferenceOnly_DimensionalDeltas` | F-PLANFORM, base r3 | δL = 0.1027509294, δD_i = 0.1228454172 | **δL from coefficients with the base S_ref on both** (gives δCL 0.0210656753); compared S_ref on both (same); δ normalised to the compared value (0.0931769148); δ of S_ref alone (0.0800000000) |
| `Compare_ReferenceOnly_CoefficientsStillBlocked` | F-PLANFORM | CL and CDi rows have `DeltaValue == null` and Delta = AF-COPY-2; e = AF-COPY-3 | coefficient δ unblocked under a reference block; e re-expressed on the base b_ref |
| `Compare_ReferenceOnly_StoresNothing` | F-PLANFORM through `Compare.Record` | record count 0; document not dirty; bytes equal | a record written for the reference-blocked pair (pins two runs) |
| `Compare_ReferenceOnly_ReversedRecomputed` | F-PLANFORM, base r5 | δL = −0.0931769148, δD_i = −0.1094054581 | reversed view as −δ (−0.1027…); as −δ/(1+δ) |
| `Compare_ReferenceAndPoint_NoDimensionalDelta` (theory) | F-PLANFORM with α differing; with ρ differing | state Blocked, no output has a δ, both reasons listed | dimensional δ shown whenever reference is *a* reason, not the only one |
| `Compare_Dimensional_BaseBelowFloor_Undefined` | base CL 5 × 10⁻⁵ (L 0.0640625 N); base CL 0 | L line `ANA-CMP-BASE-FLOOR`; `ANA-CMP-BASE-ZERO` | an absolute newton floor (δ printed for 0.064 N); naive division (∞) |
| `Compare_Dimensional_NegativeBase_SignByAbs` | base CL −0.2, S 0.100; compared CL −0.1, S 0.108 | δL = +0.4600000000 | division by the signed base (−0.46) |
| `Compare_Dimensional_OneDefinition` | F-PLANFORM at a **compatible** pair (r5 vs r5′, equal reference) | the side-by-side L, D_i values equal `Compare.Dimensional` bit for bit; still no δ (A3D-COPY-16) | a second L formula in the projection; δ shown at a compatible pair |
| `Dimensional_RoundTrip_EqualsStripLift` | a VLM fixture run with strips | \|L − Σ strip lift\| ≤ 1e-12 · \|L\| | L from the Trefftz lift instead of the near-field CL |
| `Compare_ReferenceOnly_EventFlag` | F-PLANFORM through `Compare.Record` | one `analysis.compare` event, outcome `blocked-reference`, `dimensional = true`; a point block gives `false` | flag missing or always true |
| Desktop `ComparePane_ReferenceDiffers_DimensionalDeltasOnly` (replaces `ComparePane_IncompatibleReference_BlockedNoDelta`, `ComparePaneTests.cs` line 88, Verified) | r3, r5 | L / D_i δ cells rendered; CL / CDi cells read AF-COPY-2 through the Labels constant; no "recorded" status | a δ rendered in a coefficient cell |
| Desktop `ComparePane_ReferenceAndWater_BlockedNoDelta` | r3 with ρ differing | today's Blocked rendering, no δ cell | the new branch taken on a two-reason block |

`Compare_IncompatibleReference_BlocksDelta` (§7.2) keeps its rows and its "no record" assertion; its "no line has a δ"
assertion narrows to "no coefficient line has a δ" (the build edits that one assertion and records it in the proof).

### 12.6 Owned paths (AF build) and seams

`src/CfdWorkbench.Analysis/Compare.cs`, `AnalysisProjection.cs`, `AnalysisViewModel.cs`, `Labels.cs`;
`src/CfdWorkbench.Desktop/Analysis/ComparePane.axaml`, `ComparePane.axaml.cs`;
`tests/CfdWorkbench.Analysis.Tests/CompareTests.cs`; `tests/CfdWorkbench.Desktop.Tests/ComparePaneTests.cs`;
`DESIGN.md` §7 rows (AF-COPY-1..7). Read only: `src/CfdWorkbench.Core/*` (the event flag is the one exception if the
`assume:` in §12.3 holds; then `DiscrepancyRecord.cs` `CompareEvent` gains one optional member). No seam with SF2
(disjoint files).

### 12.7 Review packet (hydrodynamicist; Test Architect)

1. **Hydrodynamicist.** Is δL at equal α and V, different S_ref, the comparison a designer means? At equal α it answers
   "how much more lift does the bigger wing make"; at a **trimmed** point (equal load) L is equal by construction and
   only δD_i carries information. Does `op.load` make the latter case detectable, and should AF-COPY-5 then headline
   D_i instead of L? (Inferred: the headline quantity matters only for that case.)
2. **Hydrodynamicist.** The estimator's D_i assumes e = 1; is A3D-COPY-26 on the D_i cell enough when the reference also
   differs?
3. **Test Architect.** Does F-PLANFORM's S_ref ratio (1.08) separate the "base S on both" mutant from tolerance noise?
   (Yes by 0.08: δ 0.1028 vs 0.0211.) Is a `Compare_ReferenceOnly_StoresNothing` byte check enough to prove no pin?
4. **Test Architect.** The narrowed §7.2 assertion: is editing an existing test's assertion acceptable, or should the
   build add the new test and leave the old one byte-identical with a new reference-only row excluded?

### 12.8 Decision requests (not raised by this note)

| Id | Question | Options | Recommendation |
|---|---|---|---|
| **DR-DF-1** | Is Ruling 250 Q3 (c) taken up by the operator's 2026-10-22 instruction? | (a) yes, build AF on AM-1.7.8 · (b) no, keep §2.2 as is | (a) is the main session's reading; the leader confirms |
| **DR-DF-2** | Store the dimensional lines? | (a) no — derived on read (§12.2) · (b) a rule 1.1.0 with L / D_i line kinds and a record for reference-only pairs | (a) |
| **DR-DF-3** | Headline at a trimmed point | (a) δ L always · (b) δ D_i when `op.load` is a target load | (a) for AF; (b) if the hydrodynamicist lens asks |

## Status & next action

Cycle-1 revision written with Ruling 250 folded in; the mockup and its note updated; spec note 1.7.7 proposed. Next:
the Data & Persistence lens confirms §1.0 (the build is held until it does); the leader runs the second Adversary pass;
then the build brief for node A3d-1, and A3d-2 after it joins. DR-A3D-6 is settled by Ruling 251.
