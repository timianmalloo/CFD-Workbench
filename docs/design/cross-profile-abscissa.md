---
id: design-cross-profile-abscissa
title: "Design: cross-profile abscissa — certify a blend between sections with their own point counts (compatible fit; knot propagation as fallback; Ruling 71 option 1)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — Ruling 71 (operator 2026-10-04) and the operator's requirement change the same day; documents only; build after operator approval
tags: [core, desktop, section, profile, abscissa, compatible-fit, point-spacing, knot-insertion, knot-removal, refit, certificate, blend, make-unique, ruling-71, xpa]
links:
  - { to: rulings, rel: implements }
  - { to: design-m12c-section-editor, rel: refines }
  - { to: adr-0005-point-types, rel: refines }
  - { to: adr-0010-one-placement-rule, rel: refines }
  - { to: adr-0007-edit-transactions, rel: depends-on }
  - { to: adr-foildsl-authority, rel: depends-on }
  - { to: proof-cross-profile-abscissa, rel: depends-on }
  - { to: proof-m12c-certificate-spike, rel: depends-on }
  - { to: design-planform-point-verbs, rel: relates-to }
  - { to: defect-classes, rel: relates-to }
review-by: 2027-04-01
summary: >-
  After 'Make unique to Root', a step that changes Root's knots or control x leaves Root and Tip on different point
  spacings, so the blend cannot be certified. The operator then required that sections keep their own point counts.
  Primary design (compatible fit): every section stays as authored; the evaluator derives, never stores, one shared
  spacing for the wing (the richest station spacing plus every station's anchor positions) and a compatible copy of each
  section on it; the certificate and the placed surface both use those copies; each copy's deviation from its authored
  section is measured on the placed surface and admitted within 10 um, else the step is refused. Measured with the
  as-built Core: 6-point Tip on 10-point Root 1.5e-16 chord; 8-point Example section on 10-point NACA 5.7 um at 127 mm;
  fitting the richer onto the poorer fails (38 um). Fallback: copy knot changes to the neighbour (insertion exact,
  6e-17 chord). Both hit the certificate's 5-span blend capacity (DR-XPA-1); SetTangent Angle already breaks one
  profile's paired spacing (F-XPA-1).
review-suggested: []
---

# Design: cross-profile abscissa (Ruling 71 option 1)

**Lead lens:** Computational Geometry (hard veto: second geometry authority, unreported residual). **Convened:**
Marine-CAD UX (editing behaviour), Test Architect (test plan). **Tier:** T1, documents only. **Status:** for operator
approval; no build starts before it (Ruling 71).

**Confidence labels.** *Verified* = read in code at `e454ca0` or measured by the probe
(`docs/proof/cross-profile-abscissa/`, rows cited as L-P1, D′ and so on). *Inferred* = reasoned, not observed.
*Flagged* = a recalled source not opened.

## 0. The answer

**Requirement (operator, 2026-10-04, after Ruling 71).** "Point spacing has to change with taper and curve; also I
should be able to have less points on one section than another." Taper needs nothing: sections are stored in chord
fractions and placement scales them. The second sentence rules out "keep every section on one point layout" as the
primary answer: each section keeps its own point count and positions, as drawn.

**Primary option — compatible fit (CF).** Each station's section stays as authored. For the wing blend and the
certificate, the evaluator derives one **shared spacing** and a **compatible copy** of each section on it, measures
each copy against its authored section, and admits it within 10 µm. The copies are a stage of the evaluator, like the
loft rule: computed on load and on every change, never stored, never edited.

| Question | Answer | Evidence |
|---|---|---|
| Sound with `SharedAbscissa`/Blend? | Yes, if the compatible copies are the **only** blend input — certificate **and** placed surface — and their residual is reported. Then there is one record (the authored sections) and one evaluator chain. If the display kept blending the authored sections, display and certificate would differ by the residual: a second definition (ADR-0010 binds them within 1 nm) | §3.2; Geometry.cs:262-271, :372-386 (Verified) |
| Residuals on realistic pairs | 6-point Tip onto 10-point Root: 1.5 × 10⁻¹⁶ c (the app's sqrt spacings nest). 8-point Example section onto a 10-point NACA spacing: 3.9 × 10⁻⁵ c on the record, **5.7 µm** on the placed surface at a 127 mm root. Wrong direction (10 onto 6): 38 µm. A Tip anchor left out of the shared spacing: 85 µm | L-P1, L-P2, L-P1r, L-P4n (Verified) |
| Cost per edit | About 100–130 ms to fit and measure one section (dominated by the 2001-sample measure, Inferred), plus 25–70 ms for the certificate. It runs in the asynchronous check after a step, not in the drag frame. Memoised per (shared spacing, section) | L rows (Verified, M4 Max) |
| How the user sees it | Each section shows its own points. The station shows "Blends on Root's spacing · matched within 5.7 µm (limit 10 µm)". A refusal names the section and the number | §8 |
| Save / reopen | Derived on load (derive, don't store). The record is unchanged FoilDSL. Equal-spacing documents are bit-identical to today | §3.6 |
| Residual over the limit | The step that causes it is refused, with the section, the number and the limit; nothing changes (Ruling 71: no unsaveable drafts). DR-XPA-3 offers "allow, not certified" instead | §3.5 |

**Fallback option — knot propagation (KP).** Copy each change of knots or control x to every other station section,
refitting where the copy is not exact (§4). It keeps one point layout across the wing, which the new requirement
rejects as the primary answer. It stays as the fallback if CF's cost or a CG finding blocks it.

**Both options meet the same wall — blend capacity (DR-XPA-1).** The certificate admits a distinct blend of at most
**5 Bézier spans** (`Geometry.cs:388`, `:399`). The New-foil default already has 5. Any anchor on a 10-point section
makes 6. With the budget scaled to 48 × spans, 6 spans certify; 7 or more, or a three-section chain at 6, hit the
all-query operation bound (rows J, I, L-P3, L-P4).

**The stopgap.** The early-refusal track (`fix/unique-section-early-refusal`, COPY-209/210) is lifted for every step
CF can admit, once CF lands.

## 1. Problem

### 1.1 What the operator saw (Ruling 71)

Native look, 2026-10-04: after **Make unique to Root**, editing Root's section (for example Control → Anchor) gives
"Profile 'naca-0012' abscissae differ from neighbouring profile 'naca-0012-i1'", and the section cannot be saved. The
probe reproduces it on the New-foil default (row A) and on the Example foil (row K) (Verified).

### 1.2 Why (Verified, read at `e454ca0`)

- `Geometry.Assess` walks the stations in order. For each adjacent pair whose geometry differs it requires
  `SharedAbscissa(left.Difference, right.Difference)`: the same Bézier span count, the same span parameter intervals,
  the same degree and **identical rational x coefficients** (`Geometry.cs:262-271`, `:372-386`). Equal degree, knots
  and control x are **sufficient** for that. They are not necessary: two knot vectors that differ only in the
  multiplicity of a breakpoint can give the same Bézier x pieces (CG review).
- `SameGeometry` (`:245-260`) implies equal x too. Equality is transitive and the stations form one chain, so once any
  two station sections differ, **every** station section must share one spacing. The message says "neighbouring", but
  the constraint covers the whole chain (row I).
- Within one profile the two surfaces already share knots and x (`Geometry.cs:333-335`), and `SectionEdits` keeps them
  paired (Rulings 60, 61). Nothing relates *profiles*.
- `ImportPatch` already fits a .dat on a neighbour's spacing when its residual is ≤ 10⁻⁵ chord, and otherwise falls
  back to its own spacing with a diagnostic that leaves the draft unsaveable (`AuthoringSession.cs:681-760`). CF
  generalises that fit.

### 1.3 The class

Defect class (for `docs/lessons/defect-classes.md` at build time): **a step writes the knots or control x of one curve
of a shared-spacing group without the rest of the group, and nothing checks the group before the draft is kept.**
Sweep of every writer of knots or x:

| Writer | Group it breaks today |
|---|---|
| `SectionEdits.ToAnchor`, `InsertAnchor`, `ToControl`, `Delete`, `Insert`, `Rebuild`; `AuthoringSession.MoveSectionPoint`, `ImportPatch` | the station sections (this design) |
| `SectionEdits.SetTangent` with Angle (`PlaceHandles`, `:302-320`) | **the two surfaces of one profile** — F-XPA-1 (row G): one handle's x differs between the surfaces, so "Independent profile x mappings are not assessed", with or without a second section |
| `SectionEdits.SetTangent` with Symmetric | the station sections only (Root's two surfaces stay equal; row G) |

Under CF the first row stops being a defect: different spacings are allowed, and the evaluator makes them compatible.
The second row is a defect under either option.

## 2. Grounding

**Ubiquitous language.**

| Term (user copy) | Code | Meaning |
|---|---|---|
| point spacing | basis | (degree, knot vector, control x) of a section; both surfaces share it |
| shared spacing | `CompatibleBasis` | the one spacing the wing blend uses, derived from the station sections |
| matched within *n* µm | compatible residual | how far a section's compatible copy is from the section as drawn, on the placed surface |
| largest chord | `SectionEdits.LargestChord` | the largest station chord among the stations that use a section (Ruling 61) |

**What nests exactly (corrected in round 2).** The app's own spacings (New foil, own-spacing Import) put the control x
on the sqrt map x = t² (`FoilSource.SqrtProfileBasis`, `:576-595`; the CG reviewer's check: max |x(t) − t²| ≤ 5.6 ×
10⁻¹⁶ for 6, 8, 10 and 16 vertices). So every such spacing carries the same x(t). A refinement is exact only when the
**knot vectors nest** as well. The 6-point spacing is one span, which nests in any knot vector: that is why the 6-point
Tip lands on the 10-point Root to 1.5 × 10⁻¹⁶ chord (L-P1; the probe used the least-squares fit, which found the exact
answer). An 8-point sqrt spacing (knots 1/3, 2/3) does **not** nest in the 10-point one (0.2 … 0.8): it takes the
least-squares path, with a residual (the reviewer measured 2.9 × 10⁻³ for that pair with scipy; 1.8 × 10⁻¹⁵ after
inserting 1/3 and 2/3, at the cost of two more spans). Spacings with a different x(t) (an edited x, an imported
spacing, a Rebuild) always need the least-squares fit (L-P2: 3.9 × 10⁻⁵ c).

## 3. Primary option — compatible fit

### 3.1 The rule (deterministic; versioned with the evaluator)

Inputs: the station sections (every profile assigned to a station, each counted once) as authored.

1. **Basis section.** The station section with the most Bézier spans; ties go to the most vertices, then the lowest
   station index. Fitting the richer section onto a poorer spacing fails (L-P1r: 38 µm), so the richer one leads.
2. **Shared spacing S\*.** The basis section's spacing, plus an exact Boehm insertion (to multiplicity 5) at the
   parameter of every interior anchor of every other station section, located by its chord fraction on the basis's
   x(t). Without this a Tip anchor costs 85 µm (L-P4n); with it, 5.2 × 10⁻¹¹ µm (L-P4). The basis section's own
   change is rounding only (5.7 × 10⁻¹⁷ c).
3. **Compatible copy of each station section P.**
   - P's spacing equals S\* → P itself (identity, bit-identical).
   - P's x(t) equals S\*'s and P's knots are a subset of S\*'s → Boehm refinement; the copy then **adopts S\*'s
     control x bitwise** (`SharedAbscissa` needs equal rationals, `Geometry.cs:267`) and its change is measured like any
     other copy. Not yet measured: the probe's L rows all used the least-squares path.
   - Otherwise → `DatImport.FitToBasis`-style least squares of P's authored ordinates onto S\* (the as-built import
     fit: endpoint pins, closed-TE pin, nose at x = 0). P's tangent rows become KKT rows (`ProfileFair`'s row catalogue,
     `ConstrainedFit.cs:370-409`) wherever P's anchors coincide with S\*'s.
4. **Residual of each copy.** Two numbers, both sampled lower bounds, both reported:
   - record level: `FoilSource.MaxOrdinateDeviation` over [0, 1] (2001 samples), × the largest chord of P;
   - surface level: Rule A on the authored sections at equal chord fraction (what `Placement` draws from the record
     today) against Rule A on the compatible copies, at the stations and at interior η, in metres. The surface number
     can be larger (L-P4n: 14.7 µm on the record at Tip's chord, 85 µm at η = 0.7), because Rule A renormalises each
     section by its own maximum thickness before blending.
   The **surface-level number decides**: admitted when ≤ 10 µm (τ_j, the A4.6 model/join tolerance of Ruling 61)
   everywhere it is sampled.
5. If every station section already shares one spacing, S\* is that spacing and every copy is the identity: the
   evaluated surface, the certificate and every golden master are unchanged bit for bit.

### 3.2 One definition, not two

- The **record** stays the authored FoilDSL sections. Nothing else is persisted or editable.
- `Geometry.Assess` certifies the compatible copies. `Placement` (3D view, elevations, Side view, `Sections`) evaluates
  the **same** copies, so the display stays within ADR-0010's 1 nm of the certified enclosure. Exports and solver
  inputs read the same placed surface.
- The section editor draws the authored section (the record, `ProfileView`), as built. Its residual readout says how
  far the placed station is from it.
- The CG veto conditions are met only with all three: the copies are derived, never stored or edited; one evaluator
  chain feeds certificate and display; every copy's residual is measured and shown. **If any reader still blends the
  authored sections directly, that is a second definition and the veto applies.**
- The stage is part of the evaluator. A document that needs it (station sections on different spacings) records a new
  evaluator id so its surface can be reproduced (DR-XPA-2).

### 3.3 Steps under CF

Every step changes **only the edited section**, as built (both surfaces paired, Rulings 60/61). After the step, the
check recomputes S\* and the copies. No step writes another section. So:

| Step | Effect on the blend |
|---|---|
| Move (x or y), Control → Anchor, Anchor → Control, Insert, Insert anchor, Delete, Fair, Rebuild, Import, Thickness, Make unique | the edited section changes as drawn; S\* is re-derived; every copy is recomputed and its residual re-measured |
| SetTangent Angle | built y-only (handles keep x; y = a.y + tan α · (h.x − a.x), valid for \|α\| < 90°, the same equation as `ProfileFair`'s angle row): fixes F-XPA-1 (DR-XPA-6) |

**When does a step get refused?** When, after it, any copy's surface-level residual exceeds 10 µm, or the certificate
refuses the compatible document for capacity (§5). The refusal is decided by running the check on the candidate bytes
— `Geometry.Assess` plus the residual measure — never by a separate count (no second judge).

### 3.4 Cost

Measured per compatible pair (L rows, M4 Max, Release): fit + record measure 98–169 ms (268 ms on the first call,
JIT included); certificate 25–70 ms. Which part of the first number is the fit and which is the 2001-sample measure is
not separated (Inferred: the measure). Mitigations: memoise each copy by the hash of (S\*, section bytes); a step that
leaves S\* unchanged refits only the edited section; the check runs off the UI thread as built ("Checking…"). Budget
check at build: `Readiness_CompatibleCheck_FiveSectionsUnder1s` (test 22).

### 3.5 Over the limit

The step is refused, nothing changes, and the strip names the section and the numbers (§8). This follows Ruling 71
(2): no unsaveable drafts. DR-XPA-3 asks whether the operator would rather keep the draft and show it as not certified.

### 3.6 Save, reopen, old builds

- **Representation unchanged.** FoilDSL bytes in append-only accepted rows (ADR-0002, ADR-0007). No grammar change.
- **Derived on load.** Reopen re-derives S\* and the copies from the record. Nothing about them is stored, so there is
  nothing to migrate (DM: derive, don't store).
- **Equal-spacing documents** are bit-identical: identity copies, same certificate, same golden masters.
- **Documents that need CF** certify only in a build with CF. A pre-CF build parses them and shows "abscissae differ"
  (Unsupported) — Verified as the as-built verdict on these bytes (row L setup). With a new evaluator id (DR-XPA-2) it
  refuses with `DOC-VERSION` instead (`NativeProject.Check` requires `cfdw-cv/2`; read). What the old *app* then
  allows is recorded, not assumed: `docs/proof/xpa-old-build/` (test 24).

## 4. Fallback option — knot propagation

Kept in full in the probe (rows A–K), summarised here. Each step that changes knots or control x on the edited section
E applies the same change to every other station section P.

| Step | Rule | P's change (measured) |
|---|---|---|
| Control → Anchor, Insert anchor, Insert | same Boehm insertions on P, both surfaces; `smooth` rows on P's new anchor when E gets them | exact: 6.0 × 10⁻¹⁷ c, x bitwise equal (B, F) |
| Anchor → Control, Delete | same removals on P; P's rows there dropped and reported; P refit | 4.4 × 10⁻¹³ µm (C1) to 5.0 µm (C2) on the record; **27 µm on the surface (C2)** |
| x move, Symmetric | same x on P, P's y refit | Root → Tip 0.07–1.0 µm; **Tip → Root 1.0 / 5.1 / 15.0 µm for Δx 0.2 / 1 / 3 %** (D′); Δx_max ≈ 3.7 × 10⁻³ / L[m] |
| Rebuild | the group rebuilds on one spacing (bitwise, H) | each reported |
| Fair, Thickness, y moves | E only | 0 |

Why it is the fallback: it gives every section the same point count, against the requirement; the reverse direction and
the surface-level numbers (C2, D′) refuse ordinary edits at ordinary chords; and partner sections change without the
user touching them. Its open defects, if chosen: the partner refit must carry rows as KKT constraints rather than pin
anchor triples (CG review), and the limit must be cumulative over the draft, not per step (Marine-CAD review).

## 5. Blend capacity (both options)

`Geometry.Assess` refuses a distinct blend when `spanCount × 48 > 256` (`Geometry.cs:388`, `:399`): at most 5 Bézier
spans. Spans = distinct interior knots + 1.

- New-foil default: 10 vertices, 5 spans, **no headroom**. One anchor anywhere makes 6 (rows B, J; L-P3, L-P4).
- Example foil: 8 vertices, 3 spans. Two anchors certify as built (row K).
- Budget scaled to 48 × spans (probe variant): 6 spans certify (26 ms admission). 7–9 spans, and a three-section chain
  at 6, fail the all-query operation bound (`Geometry.cs:727-730`; the blend term grows as 6 · n · (n + 1) in the node
  count n).

Under CF, S\* gathers every station's anchors, so capacity binds sooner than under KP. Until DR-XPA-1 is closed, a step
whose compatible document the certificate refuses for capacity is refused early with the count (COPY-C4).

## 6. Invariants (CF)

| Id | Invariant | Enforced by |
|---|---|---|
| XPA-I1 | Only authored sections are stored or edited; S\* and the copies are never persisted | no writer exists; test 3 |
| XPA-I2 | Certificate, placement, export and solver input all read the compatible copies | single entry point; test 5 |
| XPA-I3 | If all station sections share one spacing, every output is bit-identical to the as-built build | golden master; test 1 |
| XPA-I4 | Every copy's residual (record and surface level) is measured and reported with the step and on open | the step report; test 8 |
| XPA-I5 | No accepted step leaves a copy over 10 µm on the surface, or a capacity refusal | refusal on the candidate; tests 10, 11 |
| XPA-I6 | S\* and the copies are a deterministic function of the record and the evaluator id | test 4 (repeat), test 23 (cross-platform hash, Windows lane) |
| XPA-I7 | No step writes a section other than the edited one | test 6 |

## 7. Data, receipt and history

- **Grain, receipt, history: unchanged.** One accepted row = one Finish of one section draft whose bytes differ from its
  base. Receipt `EditReceipt(DraftId, Generation, Rail "section", VertexId = E's name at Finish, Intent)`. Under CF only
  E's block changes, so the m12c receipt stays exact.
- **Step report (Contracts, expand-only).** `SectionStepReport` gains `IReadOnlyList<BlendMatch> Matches`, one per
  station section: `BlendMatch(string Profile, IReadOnlyList<int> Assignments, bool Basis, string Kind
  /* identity | refined | fitted */, double RecordDeviation, double SurfaceDeviationMeters, double SurfaceEta,
  double LimitMeters, int SharedSpans)`. All measurements of this step; session memory only.
- **On open.** The same `BlendMatch` list comes from the check of the accepted revision, so the readout survives
  reopen without being stored.
- **Refusal data (expand-only).** `ContractError.Data` gains `MatchProfile`, `MatchDeviationMeters`, `MatchLimitMeters`,
  `MatchEta`, beside the as-built `Refit*` keys.
- **Undo.** Whole-byte mementos, as built; nothing extra (only E changed).
- **Evaluator id.** DR-XPA-2.

## 8. UX and copy (Marine-CAD UX input; proposed COPY-C1…C7, numbered by UXR at build — 209/210 belong to the stopgap)

**Mental model.** "Each section keeps its own points. The wing blends them on one shared spacing — the section with the
most detail leads — and every other section is matched to it within 10 µm." Vocabulary reuses the shipped "shared"
(COPY-188/189) and the import copy's "spacing".

- **Scope chip** (persistent, replaces the make-unique wording at `StatusStrip.axaml.cs:200` and m12c §0.1 step 8):
  COPY-C1 "Only Root uses this section · the wing blends on Root's spacing" (or "on Tip's spacing").
- **Properties, Section block** (every station section): COPY-C2 "Blend: deviation 0.0057 mm, sampled (limit 0.010 mm), on
  Root's spacing" — mm to 0.0001 like COPY-187; "identical spacing" when the copy is the identity.
- **Strip after a step** (adds a clause to the existing COPY-185/186/187 reports instead of replacing them):
  COPY-C3 "… Tip deviates 0.0057 mm on the blend (sampled)." Only when a match changed.
- **Refusals** (strip, "Nothing changed.", Show selects the point):
  - COPY-C4 (capacity): "Can't add this anchor: the blend can be checked for 5 spans and this needs 6. Delete a point
    first, or use the same section at both stations. Nothing changed."
  - COPY-C5 (match): "Can't keep this change: Tip would be 0.0143 mm from its drawn shape on the blend (limit 0.010 mm).
    Nothing changed."
- **Drag.** A drag of a basis-section point re-derives the copies only on release (the check is 100+ ms); the release is
  refused if over. A live readout during the drag is DR-XPA-5.
- **Rebuild popover.** Shows the match each station would get after the rebuild (COPY-C6), and keeps Apply off with the
  failing station marked.
- **Undo label.** Unchanged ("Undo edit Root section"); only Root changed.

**Conformance (Marine-CAD M1).** Area 03 recommendation 8 (`docs/knowledge/hydrofoil-workbench/03-marine-and-board-cad-
tooling.md`:179, Verified) prefers the S-blend family over Shape3d's equal-count rule (:42). CF lets sections differ
in count, as recommended; KP would impose equal counts. CF still blends on one derived spacing, not a true S-blend; the
route to that is the parked per-section certificate (Ruling 71 option 3).

## 9. Failure modes (CF)

| Failure | Trigger | Behaviour | Test |
|---|---|---|---|
| Copy over 10 µm | poorer basis, missing anchor, very different shapes | refuse the step, numbers shown | 10 |
| Capacity | S\* above 5 spans (as built) | refuse early with the count | 11 |
| Fit singular or not finite | degenerate section | the check reports Not assessed with the reason; the step is refused | 12 |
| Two sections tie for basis | equal spans and vertices | lowest station index (deterministic) | 4 |
| Basis changes on an edit | E gains spans and becomes the basis | every copy is refit; residuals re-shown | 9 |
| Cost | many stations, 32-vertex sections | memoised; readiness budget | 22 |
| Display reads the authored blend | a reader bypasses the compatible stage | test 5 fails | 5 |
| Cross-platform drift | LSQ in binary64 on Windows vs macOS | hash fixture on both; the certificate decides each run | 23 |
| SetTangent Angle breaks the paired spacing | F-XPA-1 | y-only construction | 14 |

## 10. Test plan (Test Architect input)

**Harness.** Core tests in `tests/CfdWorkbench.Core.Tests/` run by `tools/run-tests.sh` (each suite registered by one
`Run()` line in `IdentityTests.cs`, owned by XPAC). Fixtures: the Example foil (below capacity) and the New-foil default
with Tip6 (probe section L). **Oracle rule:** certification is asserted with `Geometry.Assess` and its `Status`, never
with message text and never with a test-side copy of `SharedAbscissa`. Refusal fixtures sit well past the limit
(≥ 14 µm). Deviations are sampled lower bounds, stated in each assertion message.

**Ledger** (checker: `python3 tools/check-named-tests.py XPAC --design docs/design/cross-profile-abscissa.md
--track-section "## 11." --named-sections "## 10."`, and the same for `XPAU`). Every test states its ring and its cost;
the cost is measured at build and written into the Proof Pack. **Fast** = every join, inside the 60 s `run-tests.sh`
budget; **readiness** = `tools/run-readiness.py`.

| # | Test (track) | Protects; red today? | Ring, cost |
|---|---|---|---|
| 1 | `Compatible_EqualSpacing_IdentityBitwise` (XPAC) | XPA-I3 on the Example and default fixtures; green today (characterisation) | fast, 2 Assess |
| 2 | `SectionEdits_TwoProfiles_ControlToAnchor_Certified` (XPAC) | **replaces** the existing SectionEdits_TwoProfiles_ControlToAnchor_Observed (SectionEditsTests.cs:284, which asserts the defect); the operator's scenario on the Example foil certifies; red today | fast, 2 Assess |
| 3 | `Compatible_NeverWritten_RecordBytesUnchanged` (XPAC) | XPA-I1: the record after a check equals the record before | fast, 1 check |
| 4 | `Compatible_Basis_RichestThenVerticesThenStation_Deterministic` (XPAC) | §3.1.1 and ties; repeat gives equal bits | fast, < 50 ms |
| 5 | `Placement_CompatiblePair_DisplayWithinCertifiedEnclosure` (XPAC) | XPA-I2: extends ADR-0010's binding test to a CF document; red today | fast, ~100 ms |
| 6 | `SectionStep_UnderCompatible_OnlyEditedBlockChanges` (XPAC) | XPA-I7 for every step kind | fast, 12 steps |
| 7 | `Compatible_Tip6OnRoot10_ExactRefinement` (XPAC) | §2 nesting (L-P1, < 10⁻¹² c) | fast, 1 fit |
| 8 | `Compatible_Section8OnNaca10_ResidualReportedBothLevels` (XPAC) | XPA-I4; record 3.9 × 10⁻⁵ c and surface ≈ 5.7 µm (L-P2) within a stated band | fast, 1 fit |
| 9 | `Compatible_TipAnchor_InsertedIntoSharedSpacing` (XPAC) | §3.1.2 (L-P4 vs L-P4n); red today | fast, 2 fits |
| 10 | `SectionStep_CopyOverLimit_RefusedUnchanged` (XPAC) | XPA-I5; bytes equal; Data keys; Status asserted | fast, 1 check |
| 11 | `SectionStep_PastBlendCapacity_RefusedEarly` (XPAC) | §5, decided by `Assess` on the candidate | fast, 1 Assess |
| 12 | `Compatible_DegenerateFit_NotAssessedWithReason` (XPAC) | failure row 3 | fast |
| 13 | `SectionEdits_PairedSteps_ResultCertifies` (XPAC) | **class sweep:** adds an `Assess` assertion to the existing Angle, Symmetric, PairedXMove and PairedSetTangent tests (only 1 of 20 certifies today); Angle red today | fast, +4 Assess |
| 14 | `SectionEdits_SetTangentAngle_KeepsXOnBothSurfaces` (XPAC) | F-XPA-1 fix (DR-XPA-6); red today | fast |
| 15 | `SectionDraft_CompatibleStep_FinishOneRow_ReopenSameMatch` (XPAC) | §7: one row; after save and reopen the `BlendMatch` list is equal | fast |
| 16 | `ImportOnUnique_OwnSpacingAllowed_MatchedOrRefused` (XPAC) | the import fallback becomes a CF document (no unsaveable draft); two deterministic fixtures, one within and one over | fast |
| 17 | `RebuildOnBasis_CopiesRefitAndReported` (XPAC) | §3.3 Rebuild changes S\* | fast |
| 18 | `SectionStrip_MatchClause_FromReport` (XPAU) | COPY-C3 numbers come from `BlendMatch` | fast |
| 19 | `Controller_MatchRefusal_StripNamesSectionAndNumbers` (XPAU) | COPY-C5 | fast |
| 20 | `Properties_SectionBlock_ShowsMatchOrIdentical` (XPAU) | COPY-C2 live and after reopen | fast |
| 21 | `ScopeChip_UniqueSection_NamesSharedSpacing` (XPAU) | COPY-C1 replaces the make-unique wording | fast |
| 22 | `Readiness_CompatibleCheck_FiveSectionsUnder1s` (XPAC) | §3.4 cost on a five-station, 32-vertex fixture | readiness, ≤ 5 s |
| 23 | `Compatible_FixtureHash_SameOnMacAndWindows` (XPAC) | XPA-I6 | readiness (Windows lane), ≤ 2 s |
| 24 | old-build receipt `docs/proof/xpa-old-build/` (XPAD) | §3.6, observed with the pre-CF app | readiness checklist, once per release |
| 25 | `Guard_TwentySevenSpansDiffering_Certifies` (CAP) | Ruling 74: 27 spans (32 points) with differing Root and Tip certify | fast, 4.26 s measured standalone Release subset (includes harness start) |
| 26 | `Guard_ThirtyThirdPoint_RefusedDslCurveBeforeCopy194` (CAP) | the 33rd point is stopped by `DSL-CURVE` before COPY-194 can fire; draft bytes stay equal | fast, 2.80 s measured standalone Release subset (includes harness start) |
| 27 | `Replace_FourDifferingSections_Certified_SevenRefusedCopy194b` (CAP) | four differing stations land; the seventh reaches `GEOMETRY-QUERY-OPERATIONS` and preserves COPY-194b | fast, 1.35 s measured standalone Release subset (includes harness start) |

**Red-first.** Tests 2, 5, 9, 13 (Angle) and 14 are behaviourally red at the **early-refusal track's HEAD**, the real
baseline; the build records each red run. Planted mutants: a display that blends the authored sections (must fail 5);
a basis chosen by first station rather than richest (must fail 4 and the L-P1r-shaped case in 8); a shared spacing
without the other sections' anchors (must fail 9).

## 11. Build tracks, file ownership, trace

| Track | Owns (exclusive) | Depends on |
|---|---|---|
| **XPAS** capacity spike (DR-XPA-1 c) | `docs/proof/xpa-capacity/` | operator ruling |
| **XPAC** | `src/CfdWorkbench.Core/Compatible.cs` (new: basis, shared spacing, copies, residuals, memo); `Geometry.cs` (entry: assess the compatible definition); `Placement.cs` (`Prepare` reads the compatible definition); `SectionEdits.cs` (`PlaceHandles` Angle; report `Matches`); `AuthoringSession.cs` (`PatchSectionStep` refusal on the candidate check; `ImportPatch` fallback becomes CF); `src/CfdWorkbench.Core/Contracts.cs` (`BlendMatch`, `SectionStepReport.Matches`); tests `tests/CfdWorkbench.Core.Tests/CompatibleTests.cs` (new), `SectionEditsTests.cs`, `SectionDraftTests.cs`, `ReopenSectionDraftTests.cs`, `PlacementTests.cs`, `IdentityTests.cs` (one `Run()` line) | the early-refusal track joined first |
| **XPAU** | `src/CfdWorkbench.Desktop/Shell/StatusStrip.axaml.cs` (`SectionStrip`, make-unique copy), `WorkbenchController.cs` (refusal data), `PropertiesView.cs` (Section block line), `RebuildPopover.axaml.cs`; `tests/CfdWorkbench.Desktop.Tests/SectionEditorTests.cs`; `docs/reviews/ui-xpa.md` (new: COPY-C1…C7 for UXR) | XPAC contracts frozen |
| **XPAD** | ADR-0005 Amendment 1, ADR-0010 Amendment 2, ADR-0007 note, `docs/lessons/defect-classes.md` entry, `docs/proof/xpa-old-build/`, m12c §0.1 step 8 wording | XPAC merged |
| **CAP** (Ruling 74) | `src/CfdWorkbench.Core/Geometry.cs` (stable maximum heap, scaled node budget, operation charge); `tests/CfdWorkbench.Core.Tests/BlendTests.cs` and `SectionReplaceTests.cs` (capacity boundaries); `docs/proof/blend-certificate-heap/` (measured spike and fixtures); `docs/proof/m12b2-golden/` (changed operation counts) | M1.2d RPL joined; Ruling 74 |

XPAC changes `Geometry.cs` only at its entry (which definition it certifies); `SharedAbscissa` stays untouched.
CAP applies the Ruling 74 capacity change to the Bernstein path after XPAS's spike.

**Behaviour → data → file.**

| Behaviour | Data | File(s) |
|---|---|---|
| Station sections stay as drawn | FoilDSL profile blocks | unchanged writers (`SectionEdits.cs`, `FoilSource.cs`) |
| Shared spacing and copies | derived `Definition` with compatible profile curves; memo keyed by hash | `Compatible.cs` |
| Certificate on the copies | the derived definition | `Geometry.cs` entry → `Compatible.cs` |
| Display on the copies | the derived definition | `Placement.cs` `Prepare` → `Compatible.cs` |
| Residuals | `BlendMatch` (session memory) | `Compatible.cs`; `Contracts.cs`; `SectionEdits.cs` `ReportOf` |
| Refusal on the candidate | `ContractError.Data` `Match*` | `AuthoringSession.cs`; `WorkbenchController.cs` |
| Readouts and copy | `BlendMatch` → text | `StatusStrip.axaml.cs`, `PropertiesView.cs`, `RebuildPopover.axaml.cs` |
| Angle y-only | the edited section's handle y | `SectionEdits.cs` `PlaceHandles` |
| Evaluator id (DR-XPA-2) | `DesignRow.Evaluator` | `AuthoringSession.cs` / `NativeProject` check |

## 12. ADRs and rulings

| Document | Change |
|---|---|
| **ADR-0010** | **Amendment 2 needed (CF).** The placement rule's profile inputs become the compatible copies; the rule, the two evaluators and `SharedAbscissa` stay. The binding display-within-enclosure test extends to CF documents. If DR-XPA-1 (a) is taken, the blend node budget and `QueryFeasibility`'s model (its named hand-kept bound models) change in the same amendment |
| **ADR-0005** | **Amendment 1 needed.** Decision 6's last bullet ("a change that breaks the basis shared with a distinct neighbouring profile is refused, naming the neighbour") becomes: the change is kept on the edited section; the evaluator matches the station sections (CF) and the change is refused only when a match exceeds 10 µm on the surface or capacity is exceeded |
| **ADR-0007** | none under CF (a step still changes one section). Under KP a note would be needed |
| **Ruling 60, 61** | unchanged; Ruling 61's 10 µm limit is the match limit, applied on the surface |
| FoilDSL spec | no grammar change; an evaluator id note if DR-XPA-2 (a) |

## 13. Open decisions (for the operator)

| Id | Question | Options | Recommendation |
|---|---|---|---|
| **DR-XPA-0** | Primary option | (a) compatible fit; (b) knot propagation | **(a)**, per the operator's requirement; (b) stays the fallback |
| **DR-XPA-1** | Blend capacity (5 spans as built; the default foil has no headroom; CF gathers every station's anchors) | (a) scale the node budget and re-derive the all-query bound from measured query time (spike XPAS, ADR-0010 Amendment 2); (b) keep it and refuse early; (c) b now, a as a spike | **(c).** Without (a), one anchor on a 10-point section stays refused under either option |
| **DR-XPA-2** | Evaluator id for documents that need CF | (a) write `cfdw-cv/3` only when station sections differ in spacing (old builds refuse with `DOC-VERSION`); (b) keep `cfdw-cv/2` (old builds show "abscissae differ", read-only) | **(a).** The surface of such a document depends on the CF rule; the payload must name the rule that reproduces it |
| **DR-XPA-3** | A copy over 10 µm | (a) refuse the step; (b) keep the draft, show "not certified", block Finish | **(a)** — Ruling 71 (2), no unsaveable drafts |
| **DR-XPA-4** | Which residual decides | (a) surface level (Rule A, stations and interior η); (b) record level × largest chord | **(a).** The record number understated the surface by 5.8× in L-P4n |
| **DR-XPA-5** | Live match readout while dragging a basis-section point | (a) on release only; (b) live, if a refit fits the frame budget | **(a)** in the first build; measure (b) |
| **DR-XPA-6** | Angle tangent construction | (a) y-only (handles keep x; length changes and is reported); (b) keep length, pair the x | **(a).** Fixes F-XPA-1 with no refit |

## 14. Findings (outside this design's scope; reported, not chased)

- **F-XPA-1 (Verified, defect).** `SectionEdits.SetTangent` with Angle places each surface's handles from that surface's
  own handle distance, so upper and lower x differ at one handle; `Geometry.Assess` then refuses the profile
  ("Independent profile x mappings are not assessed"), partner or not (row G). The existing tests
  (`SectionEdits_AngleKind_HandlesOnRaysWithinTau`, `SectionEdits_PairedSetTangent_BothSurfacesSameKind`) check the rays
  and kinds but never certify — tests that pass and prove nothing. Only 1 of 20 `SectionEdits` tests certifies its
  result (Test Architect). The early-refusal track should at least refuse it.
- **F-XPA-2 (Verified, copy).** "abscissae differ from neighbouring profile" names one seam; the constraint covers the
  whole station chain.
- **F-XPA-3 (Inferred, risk).** The paired-surface refit (`ToControl` → `Refit`) frees the inner handle of a bounding
  anchor, so a row there could break; and its 81-sample, window-only measure (`SectionEdits.cs:385-399`) is a weaker
  oracle than `MaxOrdinateDeviation` over [0, 1] (CG review).
- **F-XPA-4 (Verified, measured).** Record-level deviation understates the placed surface (C2: 5 µm record, 27 µm
  surface; L-P4n: 14.7 vs 85 µm). Ruling 61's paired refit limit is applied on the record today.

## 15. Residual risks

- Two shapes measured (NACA 0012, `section-a`); thin cambered tips, 32-vertex sections, open TEs and three-station
  chains under CF are not run.
- The residual oracle is a sampled lower bound at both levels; no certified upper bound is available for a fit onto a
  different x(t).
- CF's cost is about 100+ ms per fitted section per check; five fitted sections approach half a second (Inferred).
- Capacity: under CF without DR-XPA-1 (a), the default foil cannot take one anchor on any station section.
- Cross-platform bit equality of the fit is not observed (test 23).

## Gate record

**Round 1 (Adversary, on the knot-propagation draft):**
- Computational Geometry — PASS-WITH-CONDITIONS: (a) one named partner oracle with domain and bound type;
  (b) reverse-direction rows and corrected limits; (c) refit carries rows, Symmetric resolved; (d) capacity refusal
  decided by `Assess`; (e) the record-vs-surface amplification measured or bounded. Membership stricter than the
  certificate (Minor).
- Marine-CAD UX — PASS-WITH-CONDITIONS: M1 equal-count deviation from area 03 §8; M2 per-step limit accumulates;
  M3 refusal copy offered fixes that fail; M4 partner points look independent; M5 drag feedback unspecified; units and
  vocabulary.
- Test Architect — PASS-WITH-CONDITIONS: ledger format vs `check-named-tests.py` (hyphenated track ids); the class
  test passes when everything is refused; the existing `…_Observed` test asserts the defect; class sweep of
  non-certifying tests; red-first baseline; cost claims; ownership gaps.

**Repair (cycle 1 of 1)**, together with the operator's requirement change: CF became primary and KP the fallback.
(a) the oracle is named at both levels and the surface level decides (§3.1.4); (b) D′ reverse rows measured, limit
restated (§4); (c) KP's refit carries rows (§4); (d) refusals are decided by the check on the candidate (§3.3, §5);
(e) surface-level numbers measured (C2, D, D′, L) and F-XPA-4 recorded; M1 resolved by CF (different counts) with a
conformance note; M2–M5 do not arise under CF (no partner writes) and are recorded for KP; copy uses mm and "shared";
the ledger uses XPAC/XPAU/XPAD, tags each name, states the checker command; test 2 converts the `…_Observed` test;
test 13 sweeps the class; red-first is stated against the early-refusal HEAD.

**Round 2 (Adversary, verification of the repair; the repair cap is reached, so what remains is open, not repaired):**

- Computational Geometry — **PASS-WITH-CONDITIONS; the veto is not yet cleared.** No second authority as designed.
  Corrected in place because they were false statements: the nesting claim (§2) and the Boehm branch adopting S\*'s x
  (§3.1.3). **Open conditions before build (XPAC):**
  - CG-1: classify every Core reader of profile geometry as a labelled record readout or a placed (copy) reader —
    `ThicknessFit.cs:142`, `SectionModel.cs:38,46,65,77` (View, Facts, Probe, DisplayCrossing) are not in §3.2;
  - CG-2: fix and version the residual grid (the probe used 11 η × 201 cosine x) and add a grid-refinement check to
    test 8 (L-P4n peaks at interior η = 0.7);
  - CG-3: put the fit's sample set (the probe's 161 cosine points), pins, KKT rows and the anchor inversion into the
    versioned rule; restate test 23 as ≤ 1 µm and 10⁻⁶ relative plus an equal verdict (bit equality across platforms
    is unlikely through `Math.Cos`); on open, an over-limit copy is reported, never refused; DR-XPA-2 (a) must be ruled;
  - CG-4: "skip if present" when inserting an anchor already at multiplicity 5 into S\* (XPA-I3 depends on it);
  - CG-5: two copies of the sqrt basis exist (`FoilSource.cs:576`, `DatImport.cs:299`) — one should remain.
- Marine-CAD UX — **PASS-WITH-CONDITIONS.** M1, M2 and units resolved; "matched within" reworded to a sampled deviation
  (corrected in place); the contradictory "add an anchor on Root" hint removed. **Open (XPAU):**
  - UX-1: what the 3D, Side and Sections views show during a drag before the release check — freeze them or label
    "Preview, not matched"; never draw the authored blend as the surface (it would be the second definition of §3.2);
    add a drag-frame mutant to test 5;
  - UX-2: offer a fix in COPY-C4/C5 only when the candidate with that fix applied passes the check; name the cause when
    a section becomes the leading one ("this anchor makes Tip the leading section; Root would be …");
  - UX-3: a match line in the section editor's strip; label editor probe/t/c/area readouts "drawn"; "Root's spacing +
    Tip's anchors" when S\* holds other anchors; C3 announces a basis change;
  - UX-4: coalesce repeated nudges before the 100+ ms check;
  - UX-5: release-only check (DR-XPA-5 a) is acceptable if the comb and readouts stay live on the drawn section, the
    strip says "match checked on release", a refused release snaps back with the numbers, and it leaves no undo step;
  - glossary: the exact path is Rhino Loft's default compatibility; the least-squares path is an automatic Refit at
    0.010 mm (Flagged, not opened).
- Test Architect — round 1 findings addressed in the ledger (§10); the checker parses 19 XPAC and 4 XPAU names. Not
  re-reviewed in round 2.

**Status:** the design is ready for the operator's decision on DR-XPA-0 … DR-XPA-6. The CG veto clears at build when
CG-1 … CG-3 hold and DR-XPA-2 is ruled.
