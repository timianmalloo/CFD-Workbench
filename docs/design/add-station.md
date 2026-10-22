---
id: design-add-station
title: "Design: Add station, Remove station and the inspection slice with Promote (CAD-02, CAD-09, GEO-06)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — track S1 of next-best-six; documents only; build after the leader's Adversary pass and the Owner's rulings on DR-S1-1..6
tags: [cad, station, inspection-slice, promote, rule-a, blend, certificate, foildsl, cad-02, cad-09, geo-06]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-next-cad-increment, rel: refines}
  - {to: mockup-add-station, rel: relates-to}
  - {to: decision-loft-rule-a, rel: depends-on}
  - {to: design-cross-profile-abscissa, rel: relates-to}
  - {to: design-planform-point-verbs, rel: relates-to}
  - {to: spec-amendments-add-station, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2027-01-31
summary: >-
  The inspection slice is a selection (never syntax); Promote writes one new profile block and one `at <η> %` assignment
  whose section is the as-built rule-A blend at η, built in control-point space by the one PlacementRule definition.
  Promotion is exact (≤ 1e-12 relative) only when the two neighbours share spacing and their unit thickness shapes peak at
  the same chord fraction; when the peaks differ the renormalising blend moves both adjacent intervals by a measured
  amount (a scratch model gives 0.8 to 31 µm per metre of chord), which this design reports and puts to the Owner
  (DR-S1-1, amendment note). Remove re-blends across the gap with the measured deviation and Apply gated at 10 µm.
---

# Design: Add station, Remove station and the inspection slice with Promote

**Track:** S1 of `next-best-six` (design half). **Spec rows:** CAD-02 (`cfd-workbench-v1.md:1245`), CAD-09 (`:788`),
GEO-06 (`:1198`). **Base:** `main` at `a77bc8e1`. **Tier:** T2. Every claim about our code below was read at that base
and carries `file:line`; a claim not traced is labelled **Inferred**.

## 0. The answer in five lines

1. The **inspection slice** is a new `Selection` value, `Selection.Slice(double Eta)`. It is a query, never syntax, never
   saved, never on the undo stack (`foildsl.md:76`).
2. **Promote** writes one new profile block and one assignment `at <η> % profile "<name>"`. The new profile's control
   points are the rule-A blend at η of its two neighbours, computed **in control-point space** by the existing
   `PlacementRule.Components` and `PlacementRule.Blend` (`Placement.cs:81-99`). No second evaluator.
3. **Exactness is narrower than the spec says.** Rule A renormalises the blended unit thickness by its own maximum
   (`Placement.cs:96-97`, `Geometry.cs:197-202`). The promoted section equals the blend **at η** in every case, but the
   loft **between** the new station and its neighbours is unchanged only when the two neighbours' unit thickness shapes
   peak at the same chord fraction. Otherwise both adjacent intervals move by a measured amount. §2.4 proves it; §2.5
   sizes it; DR-S1-1 and the amendment note put it to the Owner.
4. The **deviation** is the largest sampled distance between the old and new sections over both adjacent intervals,
   in chord fractions (relative) and in metres. Exact path: ≤ 10⁻¹² relative. Approximate path: ≤ 10 µm (A4.6).
5. **Remove** deletes the assignment (and the profile block if no other station uses it), re-blends across the gap,
   reports the same deviation and disables Apply above 10 µm. Root and tip are refused. Add and Remove are one undo step
   each.

## 1. Data model first

### 1.1 Ubiquitous language

| Term | Meaning here | Kind |
|---|---|---|
| **Authored station** | One `at <station> profile "<name>"` assignment in the `sections` block (`FoilSource.cs:1387`); its η and its profile reference. | value inside the source aggregate |
| **Profile** | One `profile "<name>" { upper cv … lower cv … closure … }` block (`FoilSource.cs:229-238`), degree 5, at most 32 points (`FoilSource.cs:158-159`). | entity, identity = name |
| **Inspection slice** | A span position η the user is looking at. Shows the blended section there. Moving it changes selection only. | derived view (selection) |
| **Blend** | The rule-A section between two stations whose profiles differ: camber blended linearly, unit thickness blended linearly then renormalised to peak 1, scaled by the t/c channel (`Placement.cs:91-99`). | derived |
| **Unit thickness shape** `U` | `(upper − lower) / M`, where `M` is the profile's own maximum of `upper − lower` (`Placement.cs:81-82`). Peaks at 1. | derived |
| **Promotion** | The command that turns a slice into an authored station without changing the loft. | command |
| **Compatible copy** | Ruling 73's derived copy of a section on a shared spacing. **Not built** (Verified: no `compatible` in `src/`; `Geometry.cs:402-403` still refuses differing abscissae as Unsupported). | derived (future) |
| **Deviation** | §2.6. | derived measure, non-additive (a maximum) |
| **Neighbours** | The two authored stations that bracket η (`PlacementRule.Bracket`, `Placement.cs:73-79`). | — |

### 1.2 What is authored, what is derived

- **Authored (the record):** the FoilDSL source only. Promote adds exactly two things to it: a profile block and an
  assignment. Remove deletes an assignment and, when it becomes unreferenced, its profile block (FoilDSL rule 7: "All
  profiles must be referenced", `foildsl.md:214`; enforced at `FoilSource.cs:1540`).
- **Derived:** the slice, the blend, the deviation, the station label ("Station n", `StationNames.cs:13-14`), the
  certificate. None is persisted.
- **No new durable store.** The native project already keeps every accepted source revision as a row
  (`AuthoringSession.cs:565-580`, `Commit`). An Add or Remove is one more accepted row. The grain is unchanged: **one
  accepted row is exactly one committed command on one source revision.** History is the existing append-only rows;
  nothing is rewritten.
- The slice η is **not** saved. A reopened project has no slice (Inferred: `Selection` is not in the envelope; the build
  confirms by reading `NativeProject.Encode`).

### 1.3 Invariants the commands keep

| Id | Invariant | Where it is enforced |
|---|---|---|
| ST-I1 | Station η values are strictly increasing; root is η 0, tip is η 1. | parser, `FoilSource.cs:1540` (re-parse of the candidate, as `MakeIndependent` does at `:516`) |
| ST-I2 | Every profile is referenced; every reference resolves. | parser, `FoilSource.cs:1536-1540` |
| ST-I3 | Promotion leaves the five channels byte-for-byte unchanged (no channel token is touched). | the patch only inserts text; test T15 |
| ST-I4 | Promotion leaves the section at η equal to the slice's section (≤ 10⁻¹² relative), always. | §2.3; test T1 |
| ST-I5 | Promotion leaves the loft on both adjacent intervals unchanged within the acceptance of its regime. | §2.4–2.6; tests T2–T4 |
| ST-I6 | Root and tip are never removed. | `RemoveStation` guard; test T7 |
| ST-I7 | One command is one accepted row is one undo step. | `Commit` once per command; test T10 |

The aggregate is unchanged: the **source revision** (the FoilDSL bytes) is the aggregate root and the consistency
boundary. Profiles and assignments live inside it; nothing references a station from outside except by index + η
(`Selection.Station(int Index, double Eta)`, `Selection.cs:8`).

## 2. The central question: how the promoted profile is produced, and what deviation it reports

### 2.1 What the slice shows (Verified)

At η strictly between two neighbours L and R:

- If L and R have the same geometry (equal Bernstein spans), the section is that profile, scaled by t/c(η)
  (`Placement.cs:68`, `Geometry.cs:192`). No blend.
- Otherwise, with weight `w = (η − η_L)/(η_R − η_L)` (`Geometry.cs:196`):
  - camber `c(x) = (1 − w)·c_L(x) + w·c_R(x)`, with `c = (upper + lower)/2` — **not** normalised;
  - unnormalised thickness `T(x) = (1 − w)·U_L(x) + w·U_R(x)`;
  - half thickness `T(x) / max_x T · t(η) / 2`, where `max_x T` is enclosed by `EncloseMaximumT0` (`Geometry.cs:198`,
    `:226-234`) and `t(η)` is the t/c channel;
  - upper = camber + half thickness, lower = camber − half thickness (`Placement.cs:91-99`).

The certificate today requires the two blended profiles to share abscissae (`Geometry.cs:402-403`), so **every blend
the product can evaluate today is between profiles with the same knots and the same control x**.

### 2.2 How the promoted profile P is built (the construction)

Inputs: the accepted source, η_p (the slice), its neighbours L, R.

1. **Same geometry (regime R0).** P is a byte copy of L's block under the new name: the same text path
   `MakeIndependent` uses (`FoilSource.RewriteProfile`, `FoilSource.cs:739-753`), which renames the block and renumbers
   its ids. Exact by construction: P has the same Bernstein spans as L and R, so `SameGeometry` holds on both sides and
   the loft is untouched (deviation 0, not merely ≤ 10⁻¹²).
2. **Different geometry, shared spacing (regimes R1 and R2).** For every control index i (upper and lower share knots
   within a profile, `SectionEdits.cs:3`; that they also share control x is **Inferred** from the certificate pairing
   upper X with the upper − lower difference, `Geometry.cs:380`, and the build asserts it, refusing with
   `STATION-NOT-ASSESSED` otherwise; L and R share upper spans across profiles, `Geometry.cs:402`, and the build
   asserts the same for the lower curves):
   - `(c_Li, U_Li) = PlacementRule.Components(upperL_i, lowerL_i, 1/M_L)`, the same for R;
   - `(upperP_i, lowerP_i) = PlacementRule.Blend((c_Li, U_Li), (c_Ri, U_Ri), w_p, s, t_p)` in `Binary64`;
   - control x and knots: L's, copied bitwise (so P shares spacing with both neighbours and `SharedAbscissa` holds).
   This is valid because B-spline evaluation is linear in the control ordinates when x(t) is shared, and Components
   and Blend are linear in their inputs for fixed scalars: the curve of the combined control points **is** the combined
   curve. It reuses the one placement definition (ADR-0010 "one placement rule"), so there is no second geometry
   authority.
   - `M_L`, `M_R`: the profile maxima the binary64 path already uses (Inferred: `Placement` prepares a per-profile
     maximum; `PlacementRule.MaximumGrid = 401`, `Placement.cs:58`). The build reads the existing value, never a new
     search.
   - `t_p = t/c(η_p)` from the thickness channel (`ChannelEvaluator.At`, `Placement.cs:140`), and `s = 1 / max_x T(w_p)`
     from the same binary64 maximum. **Neither scalar changes the loft:** the certificate renormalises P by its own
     maximum (`Geometry.cs:385-386`), so any positive scale cancels. They are chosen so P, opened alone in the section
     editor, shows the true section at η_p (its real t/c), which is what the user expects to tune.
3. **Different spacing (regime R3).** Not reachable today: the certificate refuses the loft as Unsupported before any
   slice can be evaluated (`Geometry.cs:402-403`). Promote is disabled with the reason (S1-COPY-9). When Ruling 73's
   compatible fit is built, P is built by step 2 from the **compatible copies** on the shared spacing S\*
   (`cross-profile-abscissa.md` §3.1), and its deviation is the surface-level number, accepted at 10 µm. P then has
   S\*'s spacing.
4. **Closure and trailing edge.** `closure closed` iff both L and R are `closed`; otherwise `open`
   (`FoilSource.cs:1330` reads only these two words). Valid because the trailing-edge gap of P is the same positive
   combination of L's and R's gaps: two zero gaps give exactly zero; any open neighbour gives a positive gap, which
   `Geometry.cs:384` requires for an open profile.
5. **Name.** `"Blend <η text> %"`, for example `"Blend 35 %"`, with ` (2)`, ` (3)`… appended on a collision (the
   `MakeIndependent` loop shape, `FoilSource.cs:501-507`). Not from `StationNames`: station labels are positional
   ("Station n" renumbers when a station is added before it, `StationNames.cs:13-14`), and a profile name is a stable,
   case-sensitive identity (`foildsl.md:213`). The station's **label** comes from `StationNames.Of` as today. DR-S1-3.
6. **Provenance.** `provenance "promoted from the blend of <L name> and <R name> at η <η text> %"` (the grammar has the
   line, `FoilSource.cs:235-236`). Nothing else.

### 2.3 How it is written into the source (the patch)

`FoilSource.AddAssignment(byte[] source, double eta, string profileName, string profileBlock)` beside `MakeIndependent`:

- inserts the new block after the last profile block, with that block's indent (the `MakeIndependent` insertion,
  `FoilSource.cs:510-513`);
- inserts ` at <η text> % profile "<name>"` immediately before the first assignment whose η is greater;
- re-parses the candidate and requires `IsParsed` (Guard `DSL-PATCH`, as `:516`), and requires that the parsed η of the
  new assignment is **bitwise** the slice η.

**The η text form.** Percent of half-span, the shortest decimal that the parser maps back to the identical binary64
(`StationEta`, `FoilSource.cs:72-77`, `DecimalSi.Parse(text, −2)`). The slice is placed on a 0.1 % grid by pointer and
nudge (§5.2), so the text is short ("35", "35.2"). A typed η keeps the user's digits when they round-trip.
**Rounding cannot cost exactness:** the slice's η *is* the parsed binary64 of that text (the slice evaluates the same
double the source will hold), so the new station sits exactly where the slice was; the check above makes a
non-round-tripping text a refusal, never a silent shift. Percent (not a length) keeps the station's relative position
under a span change ("Keep relative station positions", `foildsl.md:451-453`).

`FoilSource.RemoveAssignment(byte[] source, int assignmentIndex)`: deletes the assignment tokens, deletes the profile
block when no other assignment references it (ST-I2), re-parses, Guard `DSL-PATCH`. Root and tip are refused before
any patch (`STATION-ENDPOINT`).

### 2.4 Why the loft on both sides is (or is not) unchanged — the rule-A argument

Let `w_p` be η_p's weight in [η_L, η_R]. On the new left interval [η_L, η_p] a point η has new weight `v = w / w_p`
(exact in rationals; the certificate's weights are `RationalInterval.BlendWeight`, `Geometry.cs:196`).

- **Camber (always exact).** `(1 − v)·c_L + v·c_P = (1 − v)·c_L + v·((1 − w_p)·c_L + w_p·c_R) = (1 − w)·c_L + w·c_R`.
  Identical, up to the rounding of P's control points (≈ 10⁻¹⁶ relative).
- **Thickness.** P's own unit shape is `U_P = T(w_p) / k`, with `k = max_x T(w_p) ≤ 1`. The new unnormalised shape is
  `(1 − v)·U_L + (v/k)·((1 − w_p)·U_L + w_p·U_R)`. It is a multiple of the old `(1 − w)·U_L + w·U_R` **iff
  `(1 − v) = (1 − v)/k`, that is iff `k = 1` (or at the stations themselves)**. After renormalisation, equal multiples
  give equal sections; unequal ratios give a different unit shape.
- **When is `k = 1`?** Each `U` is ≤ 1 and peaks at 1. A convex combination with `0 < w_p < 1` reaches 1 **iff U_L and
  U_R reach 1 at a common chord fraction**, that is, iff the two neighbours' maximum thickness is at the same x.
- The right interval is the mirror argument.

Hence (**Verified by derivation from `Placement.cs:81-99` and `Geometry.cs:180-202`; not yet by executing the product
code**):

| Regime | Neighbours | Promoted loft vs old loft | Accepted at |
|---|---|---|---|
| **R0** | same geometry | identical (P is a copy) | deviation **0** |
| **R1** | differ; shared spacing; unit thickness peaks at the same x | identical up to rounding of P's control points | **≤ 10⁻¹² relative** (exact path, A4.6) |
| **R2** | differ; shared spacing; peaks at different x | both adjacent intervals move; the section **at η_p** is still exact | **measured**; DR-S1-1 decides the acceptance |
| **R3** | differ in spacing | not evaluable today (Unsupported); after Ruling 73, through compatible copies | measured, 10 µm (Ruling 73 DR-XPA-3) |

The brief's two regimes ("≤ 10⁻¹² when the neighbours share spacing; measured when they do not") are therefore one
regime short: sharing spacing is **necessary, not sufficient**. The loft-rule-a note's "promotion is exact" was argued
for the channels (knot insertion) and holds for them (ST-I3); it does not hold for the renormalised profile blend.

### 2.5 How big R2 is (a scratch model, Inferred for the product)

A stand-alone model of the as-built thickness blend (symmetric sections, so camber is 0; analytic NACA 4-digit
thickness remapped to move its peak; 4001 chord samples, 1999 η samples; root profile at η 0, tip at η 1, promote at
η 0.35, t/c 0.12). It is not the product evaluator. Largest deviation of the section, in chord fractions:

| Neighbour peaks | `k = max T(0.35)` | Largest deviation | At η | At 150 mm chord |
|---|---|---|---|---|
| same shape (control) | 1.000000 | 2.0 × 10⁻¹⁷ | — | 0 |
| 30 % and 35 % | 0.998223 | 7.9 × 10⁻⁷ | 0.69 | 0.12 µm |
| 30 % and 40 % | 0.993997 | 5.2 × 10⁻⁶ | 0.70 | 0.78 µm |
| 30 % and 50 % | 0.982178 | 3.1 × 10⁻⁵ | 0.63 | 4.6 µm |

Reading: R2 deviations are many orders above 10⁻¹² and, on typical foil chords, below the 10 µm model/join
tolerance. The largest change sits on the **far** interval, not near η_p. The product numbers are measured by test T4
on a committed fixture; this table only sizes the question.

### 2.6 The deviation: measured how, against what, in which norm

- **What is compared:** the accepted source before the command against the candidate after it, both evaluated by the
  one binary64 section evaluator `Placement.Sections(source, etas, xs)` (`Placement.cs:334`), which returns camber and
  thickness per η. Upper = camber + thickness/2 and lower = camber − thickness/2 at each x.
- **Where:** every η of a 129-point uniform grid on each affected interval (Promote: [η_L, η_p] and [η_p, η_R]; Remove:
  [η_L, η_R]), plus the station η values themselves; x on the 2001-point uniform grid `MaxOrdinateDeviation` uses
  (`FoilSource.cs:856`, Inferred grid size from `cross-profile-abscissa.md` §3.1 step 4).
- **Norm:** the maximum over those samples of `max(|Δupper|, |Δlower|)` — a sup norm on the normal-to-chord
  ordinate.
- **Relative to what:** the local chord. The sections are in chord fractions, so the sampled maximum **is** the relative
  deviation `δ_rel`. The metres figure is `δ_m = max over η of c(η) · δ(η)`. The channels are unchanged, so placement
  (rotation, scale, offset) is the same on both sides and the placed distance equals `c(η) · δ(η)`.
- **Reported:** `δ_rel`, `δ_m`, and the η where it occurs, with "sampled, not a bound" (the A4.6 convention of
  COPY-498).
- **Acceptance:** exact path ≤ 10⁻¹² relative (A4.6); approximate path `δ_m ≤ 10 µm` (A4.6, τ_j of Ruling 61).
- **Regime is decided by the measurement**, never predicted from `k`: a binary64 maximum near 1 must not classify a
  pair as R1 that the measurement shows moved.

### 2.7 The certificate budget (Ruling 73 refuse early; Ruling 74)

- In R0 P adds no differing section. In R1 and R2 it adds one: L, P and R all differ.
- As built, the blend span ceiling is 27 spans (`Geometry.BlendSpanLimit`, `Geometry.cs:89`) and the all-query
  operation bound refuses **four differing station sections at any span count** (`docs/proof/blend-certificate-budget/
  verdict.md` §4.2; still the bound under Ruling 74: "the 1e6 all-query operation bound stays").
- So on a wing whose root and tip differ, the **first** promotion between them makes three differing sections and is
  admitted when the spans fit; a **second** promotion in a differing interval makes four and is refused.
- **Check (reuse, not new logic):** the candidate goes through the same clause a section step uses,
  `SectionEdits.RequireBlendBudget` (`SectionEdits.cs:62-90`): the span ceiling, then `Geometry.Assess` on the
  candidate, refused when it hits `Geometry.OperationBoundCode` and the base did not. The refusal names the differing
  stations (`STATION-BUDGET`, S1-COPY-8). The build exposes that private clause as `internal` for `AuthoringSession`;
  it does not copy it.
- P shares its neighbours' spacing, so it never raises the span count.

### 2.8 The evaluator id after promotion

`cfdw-cv/2`, unchanged. `Commit` writes `"cfdw-cv/2"` for every design row (`AuthoringSession.cs:571`) and the surface
semantic names `["cfdw-cv","2"]` (`FoilSource.cs:1549`). P shares spacing with its neighbours, so Ruling 73's
`cfdw-cv/3` condition ("station sections differ in spacing") is not met by promotion. The **surface hash changes**
(the semantic lists assignments and profiles, `FoilSource.cs:1551-1552`), so a promotion makes a new design row even in
R0. That is correct: the record changed; the shape did not.

### 2.9 Remove station: the inverse

1. Refused for root and tip (`STATION-ENDPOINT`), and for an index out of range (`DSL-TARGET`).
2. Candidate = `RemoveAssignment(source, index)`. The gap [η_L, η_R] is re-blended by rule A from the remaining
   neighbours. Nothing is fitted: the authored neighbours stay as drawn, so "refit" in CAD-02 means the re-blend.
   (Fitting the neighbours would rewrite authored sections — refused by the data model.)
3. Deviation per §2.6 over [η_L, η_R].
4. `PreviewRemove(index)` returns the deviation and `Admitted = δ_m ≤ 10 µm` (or δ_rel ≤ 10⁻¹² when the removed station
   was R0/R1-redundant — then it is reported as exact). `RemoveStation(index)` refuses with `STATION-REMOVE-ACCEPTANCE`
   when not admitted, so the gate holds even without the UI. DR-S1-2 asks whether Remove is a deliberate edit with no
   gate (like Rebuild to N, A4.6's 1.7 note).
5. The budget check is not needed: removing a station never adds a differing section.
6. Add then Remove of the same station restores the source **bytes** (the patch deletes exactly what it inserted): test
   T9.

## 3. Commands and undo

### 3.1 Core API (owned: `AuthoringSession.cs`, `FoilSource.cs`)

```text
AuthoringSession.PreviewPromote(double eta)            -> PromotionPreview   // no row; off the UI thread
AuthoringSession.AddStation(string operationId, double eta) -> PromotionOutcome
AuthoringSession.PreviewRemove(int index)              -> RemovalPreview     // no row; off the UI thread
AuthoringSession.RemoveStation(string operationId, int index) -> RemovalOutcome

record PromotionPreview(double Eta, string EtaText, int Left, int Right, string Regime,     // "R0" | "R1" | "R2"
    double DeviationRelative, double DeviationMeters, double AtEta, bool Admitted, string? RefusalCode, string? Reason);
record PromotionOutcome(string AcceptedId, int Index, string ProfileName, PromotionPreview Report,
    ThicknessIntent Thickness /* always KeepCurrent */);
record RemovalPreview(int Index, double DeviationRelative, double DeviationMeters, double AtEta,
    double LimitMeters /* 10e-6 */, bool Admitted, bool ProfileRemoved);
record RemovalOutcome(string AcceptedId, RemovalPreview Report);
```

Each mutating call runs inside `Run("add-station" | "remove-station", …)` like `ApplyPointCommand`
(`AuthoringSession.cs:310-311`) and calls `Commit(…, "apply")` **once** (`AuthoringSession.cs:565`). `Commit` already
clears redo and appends one cursor row; `Undo(operationId)` (`:459`) walks one parent. So Add and Remove are one undo
step each, with no new undo machinery. Preconditions copy `PreviewRebuilds` (`:319-321`): document open, `current` set,
**no draft and no recovery**, else `DSL-DRAFT-OWNED`.

### 3.2 Refusals (stable codes; nothing changes on any refusal)

| Code | When | Copy |
|---|---|---|
| `STATION-COINCIDENT` | η equals an existing station's η bitwise (root η 0 and tip η 1 included). Carries the existing index; the Desktop selects it. | S1-COPY-6 |
| `DSL-RANGE` | η not finite or outside (0, 1). | S1-COPY-7 |
| `STATION-ENDPOINT` | Remove of root or tip. | S1-COPY-12 |
| `STATION-BUDGET` | Promote would make a differing section the certificate cannot carry (§2.7). | S1-COPY-8 |
| `STATION-SPACING` | The neighbours differ in spacing; the loft is Unsupported today (R3). | S1-COPY-9 |
| `STATION-NOT-ASSESSED` | The accepted loft has no certificate for another reason (its own assessment's reason is quoted). | S1-COPY-10 |
| `STATION-INEXACT` | R2 under DR-S1-1 (a): measured deviation > 10⁻¹² relative. Under (b), above 10 µm. | S1-COPY-11 |
| `STATION-REMOVE-ACCEPTANCE` | Remove with δ_m above 10 µm. | S1-COPY-15 |
| `DSL-DRAFT-OWNED` | A draft is open (one draft at a time, UX-23). | S1-COPY-13 |
| `DSL-PATCH` | The patched candidate does not re-parse or the η text does not round-trip. A defect, not a user state. | (generic error) |

### 3.3 The thickness command policy (CAD-02 "explicit thickness command policy")

Promote and Remove are **Keep current thickness**: the t/c channel record is byte-for-byte unchanged (CAD-10's
definition). The outcome carries `ThicknessIntent.KeepCurrent` (`Contracts.cs:116`) and the status line says so
(S1-COPY-4). There is no "Use source thickness" on Promote: P's thickness is the channel's value at η_p by construction,
so there is nothing to retarget. The spec does not name this default; the amendment note proposes the sentence
(A-S1-2).

### 3.4 Referenced profile closure (CAD-02)

After Add: the new block exists and is referenced exactly once; after Remove: no block is left unreferenced. Both
re-parse under ST-I2. Test T16.

## 4. E7 change-surface list (written before the build)

| # | Surface | What changes | File (owner) |
|---|---|---|---|
| 1 | Source (store) | `AddAssignment`, `RemoveAssignment` patches | `src/CfdWorkbench.Core/FoilSource.cs` (S1) |
| 2 | Definition (model) | none: the parser already reads N assignments and profiles | — (read only) |
| 3 | Blend construction | control-point blend through `PlacementRule.Components`/`Blend`; no new evaluator | `src/CfdWorkbench.Core/AuthoringSession.cs` (S1); `Placement.cs` read only |
| 4 | Budget clause | `RequireBlendBudget` made `internal`, called unchanged | `src/CfdWorkbench.Core/SectionEdits.cs` (S1 build: **add to the plan row's owned list**, a one-word visibility change) |
| 5 | Session (service) | `PreviewPromote`, `AddStation`, `PreviewRemove`, `RemoveStation`; one `Commit` each | `src/CfdWorkbench.Core/AuthoringSession.cs` (S1) |
| 6 | Geometry | read only (`Assess`, `SectionAt` for tests) — no new authority | `src/CfdWorkbench.Core/Geometry.cs` (read) |
| 7 | Names | none to the rule; new callers only | `src/CfdWorkbench.Core/StationNames.cs` (read; S1 owns it for a doc comment if the build needs one) |
| 8 | Wire: CLI `inspect --json` | none expected: it prints `AuthoredProjection.Assignments` (`Contracts.cs:36`) — **Inferred**, proven by test T12 | `src/CfdWorkbench.Cli/Program.cs` (read) |
| 9 | Selection (client type) | `Selection.Slice(double Eta)` | `src/CfdWorkbench.Desktop/Selection.cs` (S1 build: **add to the owned list**) |
| 10 | Controller verbs | `PlaceSlice`, `MoveSlice`, `ClearSlice`, `PromoteSlice`, `PreviewRemoveStation`, `RemoveStation`; enablement and reasons | `src/CfdWorkbench.Desktop/WorkbenchController.cs` (S1, **after A3's join**) |
| 11 | Plan chip strip | the slice line + slice chip with Promote and Clear; the new station chip selected | `src/CfdWorkbench.Desktop/PlanCanvas.cs` (S1) |
| 12 | Elevation | Front: the slice line, draggable; Side: the slice outline dashed with its chip | `src/CfdWorkbench.Desktop/ElevationView.cs` (S1) |
| 13 | Browser | the new station row (and renumbered rows) selected | `src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml(.cs)` (S1, **after D1's join**) |
| 14 | Properties | Slice panel (η, chord, t/c, twist at η, "Not saved", Promote); station panel Remove station | `src/CfdWorkbench.Desktop/PropertiesView.cs` (S1, **after A3's join**) |
| 15 | Command table + native menu | `station.add` "Add station…", `station.remove` "Remove station" | `src/CfdWorkbench.Desktop/Shell/CommandTable.cs`, `NativeMenuBuilder.cs` (S1) |
| 16 | Status line | the receipt and refusal copy | through the controller's existing status text (Inferred: the build reads where `point.add` reports) |
| 17 | Save / reopen | none: the source is the record | — (test T11) |
| 18 | 3D view | the new station is drawn by the existing station path (Inferred); **the slice is not drawn in 3D** in this slice (CAD-09 asks for a label and Promote, met in Plan, Elevation and Properties) | — |
| 19 | Copy | S1-COPY-1..20 into DESIGN.md §7 at build | `DESIGN.md` (S1 build) |

**Consistency across surfaces (E7):** one test (D4) asserts, after one Promote, that the Plan chip, the Side chip, the
Browser row, the Properties heading and `AuthoredProjection` all name the same station ("Station 2", η 35 %), and that
the station formerly called "Station 2" is "Station 3" in all five.

## 5. UI

### 5.1 Archetype signature

Inherited from DESIGN.md: `ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench;
Density:Compact; Nav:TaskTabs+CommandPalette }`. Task shape: **reading is parallel** (the slice shows in Plan, Front,
Side and Properties at once); **entering is serial and one value** (η, then Promote). So the slice is a selection, not
a mode, and Promote is one press with no dialog. Remove changes shape, so it previews before Apply (a flyout anchored
to the Remove control; the shell never goes modal).

### 5.2 The inspection slice (correction 3: the slice is defined here)

- **Place (pointer, Plan).** Press on empty space in the station chip strip (Ruling 124: the strip under the planform
  is the span ruler). The slice appears at that η, snapped to 0.1 % of half-span. Drag moves it. The strip, not the
  geometry, takes the press, so point picking on the rails is unchanged.
- **Place (pointer, Front).** Press-drag on the span axis of the t/c lane (the lane already shares the span axis with
  the band, `ElevationView.cs:19-20`). Same snap.
- **Place (keyboard).** `station.add` ("Add station…", Edit menu and command palette) focuses the η entry on the options
  strip (the CAD-07 "Add station at η" precedent). Typing echoes the slice live in every view. Return promotes.
  Escape clears the slice and returns focus.
- **Move.** Drag; or with the slice chip focused, ← → nudge 0.1 %, Shift ×10. Each move replaces the selection only.
- **Shown as:** Plan — a dashed line in the `station` token across the starboard half and its port mirror, with a
  slice chip in the strip: "Slice · η 35.0 %", a **Promote** button and a Clear (×) button. Front — a dashed vertical
  line at η over band and lane. Side (body plan) — the slice's section as a dashed outline at full weight, chip beside
  its nose; authored sections dimmed. Properties — the Slice panel: heading "Inspection slice", η, chord, t/c and twist
  at η, the line "Derived view — not saved" and **Promote**.
- **Snap to a station.** Dragging within 4 px of a station line snaps onto it and the selection becomes that
  **station** (no slice). That is GEO-06's "existing station selected" for the pointer path.
- **Cleared by:** Escape with the slice or the η entry focused; Clear (×); selecting anything else (the selection is
  one value, `Selection.cs:3`); opening another document. Not saved; not on the undo stack; not in the source.
- **Not a station:** the slice chip is drawn hollow (outline only, dashed) and its accessible name starts "Inspection
  slice", so it never reads as an authored station.

### 5.3 Promote

- Enabled when a slice is selected, no draft is open, and the slice is strictly between two stations.
- Press: the controller calls `PreviewPromote` off the UI thread (the chip shows "Checking…", the existing
  convention), then `AddStation` if admitted. Typical cost: one parse, one blend construction, one candidate `Assess`
  and 2 × 129 η × 2001 x section samples (budget: under the 300 ms preview budget on the reference machine — **to be
  measured** in T-perf; no number is claimed here).
- Success: the new station is selected in every view (Plan chip, Front, Side chip, Browser row, Properties heading),
  the slice is gone, and the status line shows the receipt (S1-COPY-3 or -4) with the deviation and the policy, plus
  the renumbering line (S1-COPY-5) when later stations were renumbered.
- Refusals leave the slice where it was and show the reason on the chip and in the status line.

### 5.4 Remove station

- `station.remove` ("Remove station"), Edit menu, command palette, and a **Remove station** button in the station's
  Properties panel. Disabled with the reason for Root and Tip (S1-COPY-12) and while a draft is open (S1-COPY-13).
- Invoke: a flyout anchored to the control: title "Remove Station 3", the measured line (S1-COPY-14), and **Apply** /
  **Cancel**. Above 10 µm: Apply disabled, the reason (S1-COPY-15) as its description. Return applies when enabled;
  Escape cancels and returns focus to the invoking control.
- After Apply: the station to the left is selected (its chip, row and panel), the status line shows S1-COPY-16.

### 5.5 Every state

| State | Plan strip | Front / Side | Browser | Properties | Status line |
|---|---|---|---|---|---|
| No slice | station chips | as today | rows | per selection | — |
| Slice placed | slice chip "Slice · η 35.0 %" + Promote + × | dashed line / dashed outline + chip | unchanged, no row selected | Slice panel + Promote | S1-COPY-1 |
| Checking | chip reads "Checking…", Promote disabled | as placed | — | Promote disabled | — |
| Promoted | new chip selected; later chips renumbered | new section full weight | new row selected | "Station 2" | S1-COPY-3/4 (+5) |
| Coincident η | existing chip selected | existing station | its row selected | its panel | S1-COPY-6 |
| Root/tip remove | — | — | — | Remove disabled + reason | S1-COPY-12 on attempt |
| Remove above acceptance | flyout: Apply disabled + reason | — | — | — | — |
| Remove within acceptance | flyout: Apply enabled | — | — | — | S1-COPY-16 after |
| Budget refused | slice stays; chip reason | — | — | Promote reason | S1-COPY-8 |
| Mixed spacing (R3 today) | Promote disabled + reason | — | — | reason | S1-COPY-9 |
| R2 inexact (DR-S1-1 a) | refused with the number | — | — | — | S1-COPY-11 |
| Competing draft | Promote disabled + reason; slice still placeable (CAD-11) | — | — | Promote and Remove disabled + reason | S1-COPY-13 |

### 5.6 Accessibility (WCAG 2.2 AA from DESIGN.md tokens)

- The slice line and chip use `station` (#66ddc8) on `viewport` (#17272c): the token pair DESIGN.md §2 already audits for
  station lines. The dashed style plus the word "Slice" carry the difference from an authored station, never colour
  alone (SC 1.4.1).
- Focus ring: `focus-ring-viewport` on the viewport, `focus-ring` on panes, ≥ 3:1 (SC 2.4.11 / 1.4.11).
- The slice chip is one focusable object: name "Inspection slice, η 35.0 percent", Invoke = Promote; Promote and Clear
  are buttons ≥ 24 × 24 px (SC 2.5.8; `target-dense` 32 px in the strip).
- Disabled controls carry their reason as the accessible description, as the existing disabled-with-reason controls do.
- Status-line receipts are a polite live region (existing status line behaviour, Inferred).
- Reduced motion: nothing animates; the slice moves with the pointer only.
- High contrast: the `contrast-*` tokens; the slice dash stays visible (stroke 2 px).

## 6. Copy table (provisional ids; the build maps them to the next free DESIGN.md §7 ids)

`<η>` is the η text with one decimal ("35.0 %"); `<n>`, `<name>` are station labels from `StationNames`.

| Id | Exact string |
|---|---|
| S1-COPY-1 | Inspection slice at η <η>. Not saved. Promote makes it a station. |
| S1-COPY-2 | Slice · η <η> |
| S1-COPY-3 | Added <name> at η <η>. Shape unchanged: deviation 0. Thickness: keep current. |
| S1-COPY-4 | Added <name> at η <η>. Largest change <rel> of chord (<µm> µm) at η <at>, sampled; limit <limit>. Thickness: keep current. |
| S1-COPY-5 | Stations after it are renumbered: <old> is now <new>. |
| S1-COPY-6 | <name> is already at η <η>. Selected it. |
| S1-COPY-7 | A station must lie strictly between root and tip (0 % < η < 100 %). |
| S1-COPY-8 | Promote refused: <list> would all differ, and the blend certificate cannot carry that many different sections. Make one of them match a neighbour first. Nothing changed. |
| S1-COPY-9 | Promote is not available: <left> and <right> have different point spacing, so this loft is not assessed. |
| S1-COPY-10 | Promote is not available: the loft is not assessed (<reason>). |
| S1-COPY-11 | Promote refused: the new station would change the loft by <rel> of chord (<µm> µm) at η <at>; promotion must be exact (10⁻¹²). Nothing changed. |
| S1-COPY-12 | Root and tip stations cannot be removed. |
| S1-COPY-13 | Apply or cancel the open draft on <target> first. |
| S1-COPY-14 | The shape between <left> and <right> is re-blended. Largest change <µm> µm (<rel> of chord) at η <at>, sampled. Limit 10 µm. |
| S1-COPY-15 | Apply is disabled: the change is above the 10 µm limit. |
| S1-COPY-16 | Removed <name>. Largest change <µm> µm at η <at>. Thickness: keep current. |
| S1-COPY-17 | Add station… |
| S1-COPY-18 | Remove station |
| S1-COPY-19 | Derived view — not saved |
| S1-COPY-20 | Checking… |

S1-COPY-11 is written for DR-S1-1 option (a). Under (b) it reads "…by <rel> of chord (<µm> µm) at η <at>, above the
10 µm limit. Nothing changed." and S1-COPY-4 covers the admitted R2 case.

## 7. Test plan (red first)

Fixtures (committed under `tests/fixtures/stations/`, built by hand from `NewDefault`'s 10-point degree-5 basis,
`FoilSource.cs:530`, so every profile shares one spacing):

- **F-SAME:** `NewDefault()` — NACA 0012 at root and tip (R0).
- **F-CAMBER:** root = NACA 0012 basis; tip = the same thickness control ordinates plus a camber line added equally to
  upper and lower control y (`y ± 0` thickness unchanged). Unit thickness shapes identical, so `k = 1` (R1).
- **F-PEAK:** root = NACA 0012 basis; tip = a 10-point fit of a thickness shape with its peak at 40 % on the same knots
  and control x (R2).
- **F-THREE:** F-CAMBER with a third station at η 60 % whose profile is F-PEAK's tip (for Remove above acceptance).
- **F-SPACING:** a 6-point tip on a 10-point root (the `cross-profile-abscissa.md` row; Unsupported today, R3).

**Independent expected values.** For F-CAMBER the expected promoted control ordinates at η 0.35 are written in the test
as literals computed by hand from the fixture: camber_i = 0.35 × tipCamber_i, unit thickness_i = root's. They do not
call the code under test. The section-equality oracle is `Geometry.SectionAt` on the **before** certificate (existing,
not under test) compared with `SectionAt` on the **after** certificate at the same (η, x).

| # | Test (project) | Clause | Failing input / assertion | Red before the code because | Cost |
|---|---|---|---|---|---|
| T1 | `Station_PromoteAt035_SectionEqualsBlend_1e12` (Core `StationTests.cs`) | CAD-02 η 0.35, GEO-06 deviation | F-CAMBER, η 0.35: after-`SectionAt` vs before-`SectionAt` at 33 x, ≤ 1e-12 relative; P's control ordinates equal the hand literals within 4 ulp | `AddStation` does not exist; the test calls it through a stub that throws `NotImplementedException` until the build | < 1 s |
| T2 | `Station_PromoteR1_LoftUnchangedBothIntervals` (Core) | GEO-06 "without altering shape" | F-CAMBER: `PromotionPreview.DeviationRelative ≤ 1e-12`, `Regime == "R1"`; independent check by `SectionAt` on 17 η per interval | no preview exists | ~1 s |
| T3 | `Station_PromoteR0_CopyIsBitIdentical` (Core) | GEO-06 exact under rule A | F-SAME at η 0.5: deviation exactly 0; P's block equals root's block after rename (bytes) | no command | < 1 s |
| T4 | `Station_PromoteR2_ReportsMeasuredDeviation` (Core) | GEO-06 reported deviation; DR-S1-1 | F-PEAK at η 0.35: `DeviationRelative > 1e-12`, `AtEta` outside η_p ± 0.01; under (a) `AddStation` throws `STATION-INEXACT` and bytes unchanged; under (b) admitted when ≤ 10 µm | no command; the expected sign of the result is fixed by §2.4 | ~1 s |
| T5 | `Station_CoincidentEta_RefusedWithExistingIndex` (Core) | GEO-06 coincident | F-THREE, η = 0.6 (the parsed binary64 of "60"): `STATION-COINCIDENT`, index 1; bytes unchanged | no command | < 1 s |
| T6 | `Station_EtaAtRootOrTip_Refused` (Core) | CAD-02 | η 0 and η 1 → `STATION-COINCIDENT` (root, tip); η −0.1, 1.2, NaN → `DSL-RANGE` | no command | < 1 s |
| T7 | `Station_RemoveRootOrTip_Refused` (Core) | CAD-02 root and tip | index 0 and last → `STATION-ENDPOINT`, bytes unchanged | no command | < 1 s |
| T8 | `Station_RemoveAboveAcceptance_ReportsAndRefuses` (Core) | CAD-02 remove, Apply disabled | F-THREE remove index 1: `PreviewRemove.Admitted == false`, `DeviationMeters > 10e-6`, `AtEta` in (0, 1); `RemoveStation` throws `STATION-REMOVE-ACCEPTANCE` | no preview | ~1 s |
| T9 | `Station_AddThenRemove_RestoresBytes` (Core) | CAD-02 remove with measured deviation | F-CAMBER promote 0.35, then preview remove: admitted, deviation ≤ 1e-12; after remove the source bytes equal the original | no command | < 1 s |
| T10 | `Station_UndoAddAndRemove_OneStepEach` (Core) | plan row: undo one step | after AddStation, `Undo` once → accepted id and bytes equal the base; same for RemoveStation; `Redo` restores | no command | < 1 s |
| T11 | `Station_SaveReopen_StationPresent` (Core) | plan row: save and reopen | promote, `SaveImage`, `Reopen`: 3 assignments, η bitwise 0.35, profile "Blend 35 %" | no command | < 1 s |
| T12 | `Station_InspectProjection_ShowsStation` (Core; and one line in the Cli harness if the build finds `Program.cs` serialises the projection) | plan row: CLI `inspect --json` | `AuthoredProjection.Assignments[1]` = (0.35, span, "Blend 35 %") | no command | < 1 s |
| T13 | `Station_SecondDifferingPromotion_RefusedBudget` (Core) | Ruling 73 refuse early | F-CAMBER promote 0.35 then 0.7 (4 differing) → `STATION-BUDGET`, list names 4 stations, bytes unchanged | no command | ~2 s (two Assess) |
| T14 | `Station_MixedSpacing_PromoteRefused` (Core) | R3 | F-SPACING → `STATION-SPACING` | no command | < 1 s |
| T15 | `Station_Promote_ThicknessKeepCurrent_ChannelsByteIdentical` (Core) | CAD-02 thickness policy | every channel's token text identical before and after; `Outcome.Thickness == KeepCurrent` | no command | < 1 s |
| T16 | `Station_ProfileClosure_ReferencedOnceAndClosedRule` (Core) | CAD-02 referenced profile closure | new block referenced exactly once; closed+closed → `closed`, closed+open → `open`; Remove of the only user deletes the block; Remove of a shared profile's station keeps it | no command | < 1 s |
| T17 | `Station_EditPromotedProfile_NeighbourBlendUpdates` (Core) | CAD-02 "assign or edit its profile" | F-CAMBER promote; a `SectionStep.Move` on the new profile: sections at η 0.2 and 0.6 change; root and tip sections unchanged ≤ 1e-12 | no command | ~1 s |
| T18 | `Station_DraftOpen_PromoteAndRemoveRefused` (Core) | CAD-09 competing draft | open a point draft, then AddStation / RemoveStation → `DSL-DRAFT-OWNED` | no command | < 1 s |
| D1 | `--plan-canvas` `PlanCanvas_SliceChip_LabelPromoteInStrip` (Desktop) | CAD-09 slice labelled, offers Promote | slice at 35 %: chip text "Slice · η 35.0 %", Promote and Clear present, chip bounds below `PlanformBounds` (Ruling 124), hollow (sampled pixel is viewport colour at the chip centre) | no slice in `Selection` | pixel |
| D2 | `--controller-shell` `Controller_StationVerbs_TableMenuAndReasons` (Desktop) | CAD-02, CAD-09 | `station.add`/`station.remove` rows exist in `CommandTable` and the native menu; Remove disabled with S1-COPY-12 on Root/Tip; Promote and Remove disabled with S1-COPY-13 while a draft is open | no rows | ~1 s |
| D3 | `--controller-shell` `Controller_TypedCoincidentEta_SelectsExisting` (Desktop) | GEO-06 | typed "60" in the η entry + Return → `Selection.Station(1, 0.6)`, status S1-COPY-6 | no verb | ~1 s |
| D4 | `--controller-shell` `Controller_Promote_SelectedEverywhereAndRenumbered` (Desktop) | GEO-06 / CAD-02 selectable in every view; E7 | after Promote: Plan chip, Side chip, Browser `SelectedIndex`, Properties heading, projection all "Station 2"; old Station 2 is "Station 3" in all five; one Undo restores all five | no verb | ~2 s |
| D5 | `--plan-canvas` `PlanCanvas_SliceVisibleAtReflow` (Desktop) | CAD-09 each supported viewport | at 1280 × 800 and 640 × 400, the slice chip's Promote is visible without a menu | no slice | pixel |
| T-perf | `Station_PreviewPromote_RecordsDuration` (Core) | instrumentation | the `add-station` session event carries a duration; the number is recorded, not asserted | no command | < 1 s |

**Mutants the tests must kill.** M1 weight `w` taken from the wrong bracket → T1. M2 camber normalised by M (a wrong
Components) → T1. M3 closure always `closed` → T16. M4 coincidence by tolerance instead of bitwise → T5 with η one ulp
above 0.6 (admitted). M5 Remove deletes a still-referenced block → T16. M6 two `Commit` calls in one command → T10. M7
Desktop keeps a stale "Station 2" label → D4. M8 deviation sampled only at η_p → T4 (its maximum is on the far
interval). M9 regime from `k` instead of the measurement → T4 with a fixture where the binary64 maximum is 1 − 1 ulp.

**UI craft gate.** `ui-craft-gate.py` runs on the built Avalonia surface's exported mockup per CD8 in the build's
proof; the design mockup's run is recorded in the mockup note.

## 8. Owned paths for the build, and seams

Build-owned (exact): `src/CfdWorkbench.Core/FoilSource.cs`, `src/CfdWorkbench.Core/AuthoringSession.cs`,
`src/CfdWorkbench.Core/StationNames.cs`, `src/CfdWorkbench.Core/SectionEdits.cs` (**addition**: one visibility change),
`src/CfdWorkbench.Desktop/Selection.cs` (**addition**: `Selection.Slice`), `src/CfdWorkbench.Desktop/PlanCanvas.cs`,
`src/CfdWorkbench.Desktop/ElevationView.cs`, `src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml`,
`src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml.cs`, `src/CfdWorkbench.Desktop/Shell/CommandTable.cs`,
`src/CfdWorkbench.Desktop/Shell/NativeMenuBuilder.cs`, `src/CfdWorkbench.Desktop/WorkbenchController.cs`,
`src/CfdWorkbench.Desktop/PropertiesView.cs`, `tests/CfdWorkbench.Core.Tests/StationTests.cs` (new),
`tests/fixtures/stations/*` (new), the Desktop harness files that host `--plan-canvas` and `--controller-shell`,
`DESIGN.md` §7 (the copy rows). `Geometry.cs` and `Placement.cs`: read only.

Seams (correction 5):

- **`WorkbenchController.cs` and `PropertiesView.cs` after A3's join.** S1 adds: the slice verbs and the two station
  verbs as new methods next to the existing point verbs; the Slice panel as a new branch where `PropertiesView` switches
  on `Selection`; the Remove station button in the station panel. Re-read after A3: the `Selection` switch, the status
  text API, the draft-owned reason text, and the place where the station panel's buttons live.
- **`BrowserPane.axaml(.cs)` after D1's join.** S1 adds nothing new to the row control: a promoted station is one more
  `AuthoredAssignment` and the rebuild on `acceptedChanged` (`BrowserPane.axaml.cs:67-80`) produces its row. S1 adds
  only "select the row of `Selection.Station`" if D1 changed that path. Re-read after D1: the row control type, the
  `Tag` shape (`(index, Eta)` today) and `BindStations`.
- **`SectionEdits.cs`** is not in the plan row; the build asks the leader for it (one-word change) or, if refused,
  `AuthoringSession` calls the clause through an existing internal entry point the build finds.

## 9. Review packet for the Adversary pass

**Computational geometry (Ruling 73 lens):**

1. Is §2.4's proof right that the renormalised blend makes promotion inexact whenever the neighbours' unit thickness
   peaks differ, and that `k = 1` iff they share a peak location? Is there a construction of P that keeps the loft
   exact in R2 under the as-built rule? (The design says none exists: `U_P` always peaks at 1.)
2. Is building P in control-point space through `PlacementRule.Components`/`Blend` the same definition as the
   evaluator, or a second authority? It relies on upper and lower sharing x(t) within a profile and across L, R.
3. Is the deviation definition (§2.6: sup over a 129 × 2001 grid of the ordinate difference, relative to local chord)
   sound as the GEO-06 measure, given it is a sampled lower bound and the certificate is an enclosure?
4. Budget: does adding P in R1/R2 always add exactly one differing section, and is reusing `RequireBlendBudget` enough
   (span ceiling unchanged because P shares spacing)?
5. Evaluator id stays `cfdw-cv/2`; the surface hash changes in R0. Acceptable?
6. Remove: is "re-blend, no fit" the correct reading of CAD-02's "refit"?

**Marine-CAD UX:**

7. Slice placement in the chip strip (Ruling 124) and in the Front t/c lane: conventional for a station-based loft
   editor (Rhino section/contour, Shape3d stations)?
8. One-press Promote with a receipt vs a confirm; Remove with a flyout preview.
9. Profile name "Blend 35 %" vs a positional name; station renumbering on insert.
10. The slice not drawn in 3D in this slice.

## 10. Open decisions (the leader raises them)

- **DR-S1-1 — Promotion between sections whose thickness peaks differ (R2).** (a) Exact path as the spec reads: refuse
  unless ≤ 10⁻¹² (Promote works only for R0/R1 pairs — same profile, or shared thickness shape); (b) amend GEO-06/CAD-02
  and A4.6: promotion is exact in R0/R1 and an approximate path in R2, admitted at 10 µm with the measured number
  reported; (c) change the blend rule so promotion is always exact (a geometry-authority change, out of scope).
  Evidence: §2.4, §2.5 (0.12 to 4.6 µm at 150 mm chord in the model). **Recommendation: (b)** — (a) makes Promote
  unavailable on most real wings (root and tip sections rarely share a peak location); (b) keeps the honesty rule
  (measured, reported, gated). Amendment rows A-S1-1, A-S1-3.
- **DR-S1-2 — Remove gate.** (a) As CAD-02 reads: Apply disabled above 10 µm (Remove works only for near-redundant
  stations, e.g. undoing a promotion); (b) a deliberate edit like Rebuild to N (A4.6's 1.7 note): measured change shown,
  no gate. Evidence: removing a station whose section differs changes the shape by millimetres. **Recommendation: (a)
  for the first build** (it is the spec text and the safe direction), with (b) recorded as the likely next ruling.
- **DR-S1-3 — Profile name.** (a) "Blend <η> %"; (b) "<left>–<right> <η> %"; (c) positional "Station n" (stale after
  the next insertion). **Recommendation: (a).**
- **DR-S1-4 — η unit in the source.** (a) percent of half-span (keeps relative position on a span change); (b) a length
  in the document unit (keeps absolute position). **Recommendation: (a)**, matching "Keep relative station positions".
- **DR-S1-5 — Thickness policy default.** (a) Keep current thickness, stated in the receipt; (b) offer Use source
  thickness on Promote. **Recommendation: (a)** (P's thickness is already the channel's value). Amendment row A-S1-2.
- **DR-S1-6 — Slice in the 3D view.** (a) Not drawn in this slice; (b) drawn as a dashed loop. **Recommendation: (a)**;
  CAD-09 is met in Plan, Elevation and Properties.

## 11. Failure modes, threats, privacy, telemetry

| Mode | Disposition | Test |
|---|---|---|
| η text does not round-trip | prevent: Guard `DSL-PATCH`, nothing changed | T1 path (round-trip assert) |
| Promote on a stale accepted revision (another command landed) | prevent: the command reads `CurrentBytes` under the session lock, as `PreviewRebuilds` (`:317-322`) | T10 |
| Preview slow on a large profile | detect: duration recorded per call; "Checking…" shown | T-perf |
| Budget overflow | prevent: refuse early (§2.7) | T13 |
| Orphan profile after Remove | prevent: block deleted; re-parse | T16 |
| Draft open | prevent: `DSL-DRAFT-OWNED` | T18, D2 |

- **STRIDE-lite:** no new trust boundary. Input is the user's own document, parsed by the existing parser; the patch
  re-parses its own output. Tampering: a crafted source that makes the patch emit an invalid file is caught by the
  re-parse guard. No identity, network or secrets.
- **Privacy (LINDDUN-lite):** no personal data is read, stored or sent.
- **Telemetry:** the `Run` wrapper's session event per call (operation `add-station`, `remove-station`,
  `preview-promote`, `preview-remove`; outcome; duration — `AuthoringSession.cs:33`), plus the regime and the deviation
  in the outcome record, which the status line shows. Questions answered: how long (duration), how often (event count),
  which path (regime), did it fail (outcome + code).

## 12. Status

| Completed | Remaining | Best next action |
|---|---|---|
| Data model; central answer with the R2 finding; commands and codes; E7 list; UI and states; copy; red-first plan with mutants; owned paths and seams; review packet; DR-S1-1..6; mockup and note; amendment note | Adversary pass (computational geometry, marine-CAD UX) by another seat; Owner rulings on DR-S1-1..6 and the amendment; threat-model and privacy rollups (paths not owned by this track) | the leader dispatches the Adversary review with §9 |

**Confidence.** Verified: every `file:line` above, read at `a77bc8e1`. Verified by derivation: §2.4. Inferred: §2.5
magnitudes (scratch model, not the product), the CLI projection serialisation, the binary64 maximum source, the status
line live region, the 2001-point x grid. Residual risk: if upper and lower curves of a profile can have different
control x in some accepted file, the control-point construction is invalid for it; the build must assert shared x and
refuse otherwise.
