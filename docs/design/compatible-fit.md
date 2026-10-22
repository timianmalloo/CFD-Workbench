---
id: design-compatible-fit
title: "Design: the Ruling 73 compatible fit, sized for a build (mixed spacing, cfdw-cv/3, R3 promotion)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — track DF of next-best-six wave 2; documents only; design and size estimate, not built in this wave unless the leader says so
tags: [core, geometry, compatible-fit, cross-profile, abscissa, evaluator-id, cfdw-cv-3, station, promote, ruling-73, size-estimate]
links:
  - {to: design-cross-profile-abscissa, rel: refines}
  - {to: rulings, rel: implements}
  - {to: design-add-station, rel: relates-to}
  - {to: decision-loft-rule-a, rel: depends-on}
  - {to: adr-0010-one-placement-rule, rel: relates-to}
  - {to: spec-cfd-workbench-v1, rel: relates-to}
review-by: 2027-01-31
summary: >-
  Ruling 73's compatible fit brought to the as-built tree after Rulings 74, 252 and 256 and the S1 build: each station
  section stays as drawn; a derived, never-stored compatible copy of each on one shared spacing feeds the certificate
  and the placed surface through one entry in Geometry and Placement; each copy is admitted at 10 um on the placed
  surface; cfdw-cv/3 is written per design row only when station sections differ in spacing, and older rows are never
  rewritten. R3 promotion, Match and Remove run on the copies, with the measure widened to every interval when the
  shared spacing changes. Size for a Sonnet build about 440 calls and 1.2M tokens planned (about 690 calls and 2.2M at
  the S1 overrun ratios); recommendation next wave.
---

# Design: the compatible fit (Ruling 73), sized

Status: **design and size estimate** (track DF, note 3). No code. It refines
[`cross-profile-abscissa.md`](cross-profile-abscissa.md) (the full CF design, cycle-reviewed in October), says what the
tree has changed since, closes the parts it left open for promotion and the evaluator id, and sizes the build. Labels:
**Verified** = opened at `main` `b51315c4`; **Inferred** = reasoned; `assume:` lines carry a belief, what confirms it,
and what breaks if false.

## 0. The answer

| Question | Answer |
|---|---|
| What is built | Nothing of CF. `src/CfdWorkbench.Core/Compatible.cs` does not exist (Verified). Mixed spacing is refused: `Geometry.AssessWithin` returns Unsupported "abscissae differ" (`Geometry.cs` line 403, Verified); Promote refuses `STATION-SPACING` (`AuthoringSession.cs` line 2779, Verified). The evaluator id is the literal `"cfdw-cv/2"` at 21 sites (20 in `AuthoringSession.cs`, 1 in `FoilSource.cs`, Verified by count) |
| The fit | as `cross-profile-abscissa.md` §3.1, unchanged: basis = richest station section; S\* = its spacing plus exact Boehm insertion of every other section's interior anchors; each copy by identity, Boehm refinement, or `DatImport.FitToBasis` least squares (Verified at `DatImport.cs` line 286) |
| Admission | the copy's deviation on the **placed surface** ≤ 10 µm (DR-XPA-4 (a)); over it, the step is refused (DR-XPA-3 (a)) |
| Where it lives | `Compatible.cs` (new, Core). Consumed through **one entry** each in `Geometry.AssessWithin` and in `Placement`'s profile preparation — needs a ruling to open Ruling 252/256's read-only clause on `Placement.cs` (DR-DF-7) |
| Stored | nothing new except the evaluator id on the design row (`DesignRow.Evaluator`, `AuthoringSession.cs` line 14, Verified) |
| `cfdw-cv/3` | written on a new design row exactly when its station sections do not all share one spacing; existing rows keep `cfdw-cv/2` |
| Migration | none of bytes: append-only rows, a reader that accepts both ids, a writer that chooses per row |
| R3 promotion | Promote, Match and Remove run on the compatible copies; when an edit changes S\*, the deviation is measured over **every** interval, not only the adjacent ones (§6) |
| Size | ≈ 440 calls · 1.2M tokens planned in 6 nodes; ≈ 690 calls · 2.2M at the wave-1 S1 overrun ratios (§9) |
| Recommendation | **Next wave** (§10) |

## 1. What changed since `cross-profile-abscissa.md`

| Then | Now (Verified unless marked) | Consequence |
|---|---|---|
| Blend capacity 5 spans (DR-XPA-1 open) | Ruling 74 built: four differing stations certify at up to 27 spans (`BlendTests.cs` lines 118-131, cited in `add-station.md` §2.7) | CF's span growth (S\* gathers every anchor) is less binding; the §5 capacity refusal stays as the early check |
| `Placement.Prepare` would read the compatible definition (XPAC plan) | Ruling 252 kept `Placement.cs` read-only for S1; Ruling 256 allowed only two access modifiers | the XPAC change to `Placement.cs` now needs its own ruling (DR-DF-7) |
| no station commands | S1 built Promote, Remove; SF2 designs Match (`add-station.md` §13); all three refuse R3 | §6 defines R3 for all three |
| deviation measure for steps: 2001-sample record measure + surface measure | `StationEdits.Measure` on 129 η per interval × 2001 x through `Placement.Sections` (Ruling 252 grid; about 10 s per preview, Ruling 256 Q2) | the surface residual of a copy and the station measure should be one measure (§3) |

## 2. Data rule (first)

- **Record.** The authored FoilDSL station sections, unchanged in grammar and bytes (`decision-loft-rule-a`; ADR-0002).
  Nothing about S\* or the copies is persisted or editable (XPA-I1).
- **Derived value, its grain.** One compatible copy is exactly one (station section as authored, S\*, evaluator id).
  S\* is exactly one per (set of station sections, evaluator id). Both are deterministic functions of the record
  (XPA-I6). Memoised in session memory only, keyed by (hash of S\*, hash of the section's bytes, evaluator id).
- **Residual (non-additive).** Per copy: record-level and surface-level deviation, both sampled lower bounds, both
  reported; the surface-level number decides (DR-XPA-4 (a)). A `BlendMatch` value in session memory
  (`cross-profile-abscissa.md` §11) — never stored; reopen re-derives it.
- **Stored change.** Only `DesignRow.Evaluator` takes a second value, `cfdw-cv/3`. **History rule:** per accepted
  design row, fixed at accept, never rewritten (a row names the rule that reproduces its surface; rewriting it would
  change the meaning of a past record — Type-2 by append).
- **One definition.** Certificate, placed surface (3D, elevations, Side view, `Placement.Sections`), exports and solver
  inputs all read the copies. A reader that blends authored sections directly is a second definition and the CG veto
  applies (`cross-profile-abscissa.md` §3.2).

## 3. The fit and its admission (what the build implements)

`cross-profile-abscissa.md` §3.1 steps 1-5 stand as written. Two refinements from the as-built tree:

1. **One surface measure.** The surface-level residual of a copy is `StationEdits.Measure`'s norm (max of |Δupper|,
   |Δlower| per local chord, δ_m = max c(η)·δ(η)) between the placed surface **as authored** and the placed surface
   **on the copies**, on the Ruling 252 grid (129 η per interval × 2001 x, all intervals that touch a non-identity
   copy). The "as authored" side is today's `Placement` evaluation of the record, kept as a named internal mode
   (`Placement` evaluates profiles by chord fraction, so mixed spacing places today; it is the certificate that refuses).
   `assume:` today's `Placement.Sections` places a mixed-spacing document without throwing. Confirms: the first build
   node runs it on the L-P2 fixture. Breaks if false: the residual's reference must be computed per station section by
   `FoilSource.MaxOrdinateDeviation` × chord plus a Rule-A model, and the CG lens re-reviews DR-XPA-4.
2. **Cost.** At about 10 s per full-grid measure on this laptop (Ruling 256 Q2, Inferred for CF), the check after a
   step on a mixed document costs one more measure. The `Placement.Sections` speed-up (Ruling 256 follow-up, SF2's
   node) is a prerequisite for an acceptable step time; until it lands, CF steps on mixed documents take ≈ 10-20 s
   ("Checking…", off the UI thread, as built). The admission runs on release only (DR-XPA-5 (a)).

**Admission:** every copy's surface-level residual ≤ 10 µm (τ_j, Ruling 61) and `Geometry.Assess` certifies the
compatible definition; else the step is refused with the section, the number and the limit, nothing changed
(DR-XPA-3 (a); COPY-C5).

## 4. Where it lives, and how the certificate and the blend consume it

- **`Compatible.cs` (new, Core):** `Compatible.Of(Definition) → CompatibleDefinition` (the same `Definition` shape,
  profiles replaced by copies, plus `BlendMatch[]`); basis choice, S\*, copies, residual, memo.
- **Geometry:** `AssessWithin` certifies `Compatible.Of(definition)` — one line at its entry (`Geometry.cs` line 330
  area, Verified that `Assess` → `AssessWithin` is the only path); `SharedAbscissa` untouched.
- **Placement:** every public entry (`Surface`, `Sections`, `Frame`, `StationEta`, `ChordGrid`, `OwnProfile`; 28 call
  sites, in the 18 files that call Placement; Verified by count) parses the source and prepares profiles; the one change is that profile
  preparation reads the compatible copies. That is one entry inside `Placement.cs`, so the 18 caller files do not change
  and none can bypass it (DR-DF-7 (a)). The alternative — feeding every caller pre-compatible bytes — touches 18 files
  and leaves a bypass possible (DR-DF-7 (b), not recommended).
- **Station commands:** `StationEdits` reads neighbours' copies (§6).
- **Section editor:** draws the authored section (`ProfileView`), and its strip says how far the placed station is
  (COPY-C3).

## 5. `cfdw-cv/3` and the migration

- **One function** `Evaluator.IdFor(Definition)` replaces the 21 literals (Verified count): `cfdw-cv/3` when the
  station sections do not all share one spacing (`Placement.SameAbscissa` over the four curves, the Ruling 256 internal
  predicate), else `cfdw-cv/2`.
- **Writer:** each new accepted design row records `IdFor` of its source. **Reader:** accepts both ids; a row whose id
  is `cfdw-cv/2` but whose source needs CF is a refusal on read (`DOC-VERSION`), because that row's surface was
  never certified (`assume:` no such row exists, because today such a source is Unsupported and cannot be accepted.
  Confirms: `Evaluator_V2RowWithMixedSpacing_Refused` on a hand-built row. Breaks if false: rows accepted by an older
  build under a stopgap path exist; then the migration needs a read-only open with the reason, DR-DF-9).
- **Old builds:** a `cfdw-cv/3` row is refused with `DOC-VERSION` (`cross-profile-abscissa.md` §3.6; the exact
  behaviour is recorded by the old-build receipt, test 24 there, not assumed).
- **Equal-spacing documents:** bit-identical — identity copies, `cfdw-cv/2`, same golden masters (XPA-I3).
- **Expand-migrate-contract:** expand (reader accepts both) ships with the writer in one build because no older row
  changes; there is no contract step (`cfdw-cv/2` stays valid forever).

## 6. What R3 promotion becomes (and Match and Remove)

- **Promote at η between L and R on different spacings:** L\* and R\* (their copies on S\*) are blended in control-point
  space exactly as R1/R2 (`add-station.md` §2.2 step 2) to give P on S\*. P is written as an authored section **on S\***
  (it has no other spacing). The regime follows the measured deviation (Ruling 252); R3 becomes "R1 or R2 on the
  copies". The certificate budget check (§2.7 there) runs as built.
- **Match the blend** (SF2): the same, with M's new profile on S\*.
- **Remove:** removing a station can change S\* (the basis, or the anchors it contributed). Then **every** copy may
  change, and the loft moves outside [η_L, η_R]. So under CF, Promote, Match and Remove measure on **every interval**
  whenever S\* before ≠ S\* after, and on the adjacent intervals only when S\* is unchanged. The preview names which.
  Cost: up to (stations − 1) × 129 η × 2001 x per preview (Inferred ≈ 10 s per two intervals today).
- **Evaluator id after the step:** `IdFor` of the candidate — a Remove that leaves one spacing returns the next row to
  `cfdw-cv/2`.

## 7. E7 surface list (CF build)

| Surface | File | Change |
|---|---|---|
| Derived stage | `src/CfdWorkbench.Core/Compatible.cs` (new) | basis, S\*, copies, residual, memo, `BlendMatch` |
| Certificate | `src/CfdWorkbench.Core/Geometry.cs` | one entry line in `AssessWithin` |
| Placement | `src/CfdWorkbench.Core/Placement.cs` | one preparation entry + the "as authored" internal mode (DR-DF-7 ruling first) |
| Evaluator id | `src/CfdWorkbench.Core/AuthoringSession.cs` (20 sites), `FoilSource.cs` (1 site), `Evaluator.IdFor` | the literal sweep; reader accepts both ids |
| Station commands | `AuthoringSession.cs` `StationEdits` | R3 path on copies; whole-span measure when S\* changes |
| Section steps | `SectionEdits.cs`, `AuthoringSession.cs` `PatchSectionStep`, `ImportPatch` | refusal on the candidate check; early-refusal stopgap lifted for admitted steps |
| Contracts | `src/CfdWorkbench.Core/Contracts.cs` | `BlendMatch`, `SectionStepReport.Matches` |
| UI | `Shell/StatusStrip.axaml.cs`, `PropertiesView.cs`, `WorkbenchController.cs`, `RebuildPopover.axaml.cs` | COPY-C1..C7 (XPAU) |
| CLI | `src/CfdWorkbench.Cli/Program.cs` | `inspect --json` prints the evaluator id per row (Inferred already printed; the build checks) |
| Records | ADR-0010 Amendment 2, ADR-0005 Amendment 1, `docs/proof/xpa-old-build/` | as `cross-profile-abscissa.md` §12 |

## 8. Red-first tests

`cross-profile-abscissa.md` §10 tests 1-24 stand (XPAC, XPAU, XPAD; with their planted mutants: a display that blends
the authored sections; a basis chosen by first station; S\* without the other sections' anchors). Tests 25-28 (CAP) are
built. Added by this note:

| Test | Input | Expected | Mutant it kills |
|---|---|---|---|
| `Evaluator_IdFor_V3OnlyWhenSpacingDiffers` | equal-spacing default; L-P2 mixed pair | `cfdw-cv/2`; `cfdw-cv/3` | a literal left in place (grep-backed: zero `"cfdw-cv/` literals outside `Evaluator`) |
| `Evaluator_OldRowsNeverRewritten` | a `-2` document with three `cfdw-cv/2` rows, then a mixed-spacing step | rows 1-3 byte-equal; row 4 `cfdw-cv/3` | all rows re-stamped |
| `Evaluator_V2RowWithMixedSpacing_Refused` | a hand-built `cfdw-cv/2` row whose source needs CF | `DOC-VERSION` on read | reader trusts the stored id |
| `Evaluator_RemoveBackToOneSpacing_V2Again` | mixed document, remove the odd station | new row `cfdw-cv/2` | sticky `cfdw-cv/3` |
| `Promote_R3_OnCopies_RegimeMeasured` | L-P2 pair (8-point on 10-point), η 0.5 | no `STATION-SPACING`; P on S\*; regime from the measure; deviation ≤ 10 µm | `STATION-SPACING` still returned; P on L's own spacing |
| `Remove_SharedSpacingChanges_MeasuresWholeSpan` | three stations where the removed one is the basis | measured η count = all intervals; the largest change may lie outside [η_L, η_R] and is found | measure on adjacent intervals only (misses the far change) |
| `Placement_SingleEntry_NoAuthoredBlendReader` | reflection/grep over Core: every `Placement` public entry reaches the compatible preparation | all do | one entry bypasses it (second definition) |
| `Compatible_SurfaceResidual_SameMeasureAsStations` | L-P2 | residual equals `StationEdits.Measure` of authored vs copies on the same grid, bit for bit | a private second measure |

## 9. Size estimate (Sonnet build, by node)

Calibration (Verified, `docs/coordination/next-best-six.md` planned-vs-actual table): the S1 build planned
250 calls · 600k and spent ≈ 390 calls · 1.08M (1.56× calls, 1.8× tokens); the A3 build re-planned to 260 and spent
≈ 298. CF touches more of the geometry core than either.

| Node | Scope | Depends on | Calls | Tokens |
|---|---|---|---|---|
| CF-0 ruling + Adversary on this note | DR-DF-7..9; the CG lens on §3-§6 | — | (leader) ≈ 30 | ≈ 90k |
| CF-1 `Compatible.cs` | basis, S\*, Boehm insertion, least-squares copies, residual, memo; tests 1, 3, 4, 7, 8, 9, 12, 23 and the surface-measure test | CF-0 | 120 | 350k |
| CF-2 one entry | `Geometry` and `Placement` entries, the "as authored" mode; step refusal on the candidate; stopgap lifted; tests 2, 5, 6, 10, 11, 13-17, single-entry test | CF-1 | 90 | 250k |
| CF-3 evaluator id | `Evaluator.IdFor`, the 21-site sweep, reader both ids, the four evaluator tests, old-build receipt (test 24) | CF-2 | 50 | 130k |
| CF-4 R3 station commands | Promote, Match (if SF2 joined), Remove on copies; whole-span measure; two tests + the S1 station suite unchanged | CF-2, SF2 joined | 70 | 200k |
| CF-5 UI (XPAU) | COPY-C1..C7 mapped; strip, Properties, Rebuild popover; tests 18-21; Desktop checks | CF-2 | 80 | 220k |
| CF-6 records | ADR-0010 Amd 2, ADR-0005 Amd 1, proof notes, readiness cost test 22 | CF-3..5 | 30 | 80k |
| **Total planned** | | | **≈ 440** (+30 leader) | **≈ 1.23M** |
| **At the S1 ratio** | | | **≈ 690** | **≈ 2.2M** (1.8× tokens) |

Critical path CF-0 → CF-1 → CF-2 → CF-4 (CF-3 and CF-5 parallel after CF-2; they share `AuthoringSession.cs` with CF-4,
so CF-3 and CF-4 are serial on that file). Wall time ≈ 10-14 h of agent time at the wave-1 rates (Inferred).

## 10. Recommendation: **next wave**

1. **Size.** ≈ 440 calls planned is 1.7× the largest wave-1 track plan (260) and the comparable S1 build overran by
   1.56×; it does not fit beside the wave's AF and SF2 builds.
2. **A ruling first.** CF-2 changes `Placement.cs`, which Rulings 252 and 256 keep read-only; DR-DF-7 must be ruled, and
   the CG lens must pass §3-§6, before any build node dispatches.
3. **Serial with SF2.** SF2 owns `StationEdits` and the `Placement.Sections` speed-up; CF-2 and CF-4 change the same
   files, and CF's step cost depends on that speed-up (§3 item 2).
4. **No regression waits on it.** Today mixed spacing is refused cleanly (`STATION-SPACING`, Unsupported with its
   reason); nothing is silently wrong.

What would make it fit this wave: the leader cuts it to CF-1 alone (the derived stage and its tests, no reader
switched), ≈ 120 calls, which changes no behaviour and de-risks the rest.

## 11. Owned paths (CF build, when dispatched)

`src/CfdWorkbench.Core/Compatible.cs` (new), `Geometry.cs` (entry), `Placement.cs` (entry; after DR-DF-7),
`AuthoringSession.cs`, `FoilSource.cs` (one literal), `SectionEdits.cs`, `Contracts.cs`;
`src/CfdWorkbench.Desktop/Shell/StatusStrip.axaml.cs`, `PropertiesView.cs`, `WorkbenchController.cs`,
`RebuildPopover.axaml.cs`; `tests/CfdWorkbench.Core.Tests/CompatibleTests.cs` (new), `StationTests.cs`,
`SectionEditsTests.cs`; `tests/CfdWorkbench.Desktop.Tests/SectionEditorTests.cs`; ADR-0010, ADR-0005;
`docs/proof/xpa-old-build/`; `DESIGN.md` §7 rows.

## 12. Review packet (computational geometry)

1. Is one entry inside `Placement.cs` (DR-DF-7 (a)) one geometry authority, given that the rule-A functions are untouched
   and only the profile inputs change? Is the "as authored" mode, used only as the residual's reference, a second
   authority or a measurement?
2. Is measuring the copy's residual with `StationEdits.Measure` (one measure for stations and copies) sound, given that
   Measure samples on a grid and the residual is admitted at 10 µm (a sampled lower bound, never a bound)?
3. Whole-span measure when S\* changes (§6): necessary and sufficient? Could S\* change while every copy stays bitwise
   equal (then the adjacent measure suffices)?
4. P written on S\* (§6): P's spacing then depends on the other sections at the time of the promotion. Acceptable, or
   should P keep L's own spacing when L is the basis?

## 13. Decision requests (not raised by this note)

| Id | Question | Options | Recommendation |
|---|---|---|---|
| **DR-DF-7** | How the readers reach the copies | (a) one preparation entry inside `Placement.cs` and one in `Geometry.AssessWithin` (needs a ruling past Rulings 252/256's read-only clause) · (b) every caller passes pre-compatible bytes (18 files; bypass possible) | (a) |
| **DR-DF-8** | Fit this wave? | (a) next wave, whole build · (b) CF-1 only this wave · (c) whole build this wave | (a); (b) if the leader has ≈ 120 calls spare |
| **DR-DF-9** | A `cfdw-cv/2` row whose source needs CF | (a) refuse with `DOC-VERSION` · (b) open read-only with the reason | (a), on the `assume:` that no such row exists |
