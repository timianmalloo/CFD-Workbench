---
id: design-add-station
title: "Design: Add station, Remove station and the inspection slice with Promote (CAD-02, CAD-09, GEO-06)"
type: design
status: in-review
owner: "@timianmalloo"
phase: design — track S1 of next-best-six; documents only; repair cycle 1 folded the Adversary conditions; cycle 2 folded Ruling 252 (S1-Q1 d, S1-Q2 a, S1-Q3 b); ready for the build after the join
tags: [cad, station, inspection-slice, promote, rule-a, blend, certificate, foildsl, cad-02, cad-09, geo-06]
links:
  - {to: spec-cfd-workbench-v1, rel: implements}
  - {to: design-next-cad-increment, rel: refines}
  - {to: mockup-add-station, rel: relates-to}
  - {to: decision-loft-rule-a, rel: depends-on}
  - {to: design-cross-profile-abscissa, rel: relates-to}
  - {to: design-planform-point-verbs, rel: relates-to}
  - {to: spec-amendments-add-station, rel: relates-to}
  - {to: spec-amendments-1-7-9, rel: relates-to}
  - {to: design-language, rel: depends-on}
review-by: 2027-01-31
summary: >-
  The inspection slice is a selection (never syntax); Promote writes one new profile block and one assignment whose
  section is the as-built rule-A blend at η, built in control-point space by the one PlacementRule definition. Rule A
  renormalises the blended unit thickness, so a promotion is exact (measured ≤ 1e-12) only in regimes R0/R1; in R2 both
  adjacent intervals move by a shape-dependent measured amount (5.1e-7 of chord on the committed section-a / NACA 0012
  pair, in a model). Ruling 252: one press and exact at a measured ≤ 1e-12, otherwise a previewed approximate promotion applied only at ≤ 10 µm; a station within 10 µm in span of another is coincident.
  Remove re-blends across the gap with the measured deviation and Apply gated at 10 µm.
---

# Design: Add station, Remove station and the inspection slice with Promote

**Track:** S1 of `next-best-six` (design half). **Spec rows:** CAD-02 (`cfd-workbench-v1.md:1245`), CAD-09 (`:788`),
GEO-06 (`:1198`). **Base:** `main` at `a77bc8e1`. **Tier:** T2. **Cycle:** repair cycle 1 (the Adversary conditions,
§9.3). Every claim about our code was read at that base and carries `file:line`; a claim not traced is **Inferred**.

## 0. The answer in six lines

1. The **inspection slice** is a new `Selection` value, `Selection.Slice(double Eta)`. It is a query, never syntax, never
   saved, never on the undo stack (`foildsl.md:76`).
2. **Promote** writes one new profile block and one assignment. The new profile's control points are the rule-A blend
   at η of its two neighbours, computed **in control-point space** by `PlacementRule.Components` and
   `PlacementRule.Blend` (`Placement.cs:81-99`) — the functions the certificate and the binary64 path call. No second
   evaluator.
3. **Exactness is narrower than the spec says.** Rule A renormalises the blended unit thickness by its own maximum
   (`Placement.cs:96-97`). The promoted section equals the blend **at η** always; the loft **between** the stations is
   unchanged only when the neighbours' unit thickness shapes peak at a common chord fraction (§2.4). The size of the
   change is shape-dependent (§2.5).
4. The **deviation** is the largest sampled ordinate difference over both adjacent intervals, relative to the local
   chord and in metres (§2.6). The regime is decided by that measurement, never predicted.
5. **Ruling 252 (S1-Q1 (d)):** a promotion measured ≤ 10⁻¹² relative is one press and exact (R0/R1); an R2
   promotion is a separately labelled **approximate promotion** with a preview flyout and Apply, admitted at ≤ 10 µm.
6. **Remove** deletes the assignment (and an orphaned block), re-blends across the gap, reports the deviation and
   disables Apply above 10 µm (settled by the leader). Root and tip are refused. Add and Remove are one undo step each.

## 1. Data model first

### 1.1 Ubiquitous language

| Term | Meaning here | Kind |
|---|---|---|
| **Authored station** | One `at <station> profile "<name>"` assignment in the `sections` block (`FoilSource.cs:1387`). | value inside the source aggregate |
| **Profile** | One `profile "<name>" { upper cv … lower cv … closure … }` block (`FoilSource.cs:229-238`), degree 5, at most 32 points (`FoilSource.cs:158-159`). | entity, identity = name |
| **Inspection slice** | A span position the user is looking at; shows the blended section there; moving it changes selection only. | derived view (selection) |
| **Blend** | Rule A between two stations whose profiles differ: camber linear, unit thickness linear then renormalised to peak 1, scaled by the t/c channel (`Placement.cs:91-99`). | derived |
| **Unit thickness shape** `U` | `(upper − lower) / M`, `M` the profile's own maximum of `upper − lower` (`Placement.cs:81-82`). Peaks at 1. | derived |
| **Promotion** | The command that turns a slice into an authored station. **Exact promotion** (R0/R1) and **approximate promotion** (R2) are distinct, labelled operations (Ruling 252). | command |
| **Compatible copy** | Ruling 73's derived copy on a shared spacing. **Not built** (Verified: no `compatible` in `src/`; `Geometry.cs:402-403` refuses differing abscissae as Unsupported). | derived (future) |
| **Deviation** | §2.6. | derived measure, non-additive (a maximum) |
| **Neighbours** | The two stations that bracket η (`PlacementRule.Bracket`, `Placement.cs:73-79`). | — |

### 1.2 What is authored, what is derived

- **Authored (the record):** the FoilDSL source only. Promote adds a profile block and an assignment. Remove deletes an
  assignment and, when it becomes unreferenced, its block (FoilDSL rule 7, `foildsl.md:214`; parser `FoilSource.cs:1540`).
- **Derived:** the slice, the blend, the deviation, the station label (`StationNames.cs:13-14`), the certificate.
- **No new durable store.** Every accepted source revision is already a row (`AuthoringSession.cs:565-580`, `Commit`).
  Grain: **one accepted row is exactly one committed command on one source revision.** Append-only; nothing rewritten.
- The slice is **not** saved (Inferred: `Selection` is not in the envelope; the build confirms in `NativeProject.Encode`).

### 1.3 Invariants

| Id | Invariant | Enforced at |
|---|---|---|
| ST-I1 | Station η strictly increasing; root η 0, tip η 1. | parser `FoilSource.cs:1540`, on the re-parsed candidate (as `MakeIndependent`, `:516`) |
| ST-I2 | Every profile referenced; every reference resolves. | parser `FoilSource.cs:1536-1540` |
| ST-I3 | Promote and Remove leave the five channels byte-for-byte unchanged. | the patch touches no channel token; T15 |
| ST-I4 | The promoted section at η equals the slice's section (≤ 10⁻¹² relative), in every regime. | §2.3–2.4; T1 |
| ST-I5 | The loft on both adjacent intervals moves by no more than its regime's acceptance. | §2.6; T2–T4 |
| ST-I6 | Root and tip are never removed. | T7 |
| ST-I7 | One command = one accepted row = one undo step. | one `Commit` per command; T10 |

The aggregate root is the **source revision**; profiles and assignments live inside it; views reference a station by
index + η (`Selection.Station(int Index, double Eta)`, `Selection.cs:8`).

## 2. The central question: how the promoted profile is produced, and what deviation it reports

### 2.1 What the slice shows (Verified)

At η strictly between neighbours L and R:

- Same geometry (equal Bernstein spans): the section is that profile scaled by t/c(η) (`Placement.cs:68`, `Geometry.cs:192`).
- Otherwise, with `w = (η − η_L)/(η_R − η_L)` (`Geometry.cs:196`): camber `(1 − w)·c_L + w·c_R` with `c = (upper + lower)/2`
  (not normalised); `T = (1 − w)·U_L + w·U_R`; half thickness `T / max T · t(η) / 2`; upper/lower = camber ± half
  (`Placement.cs:91-99`; maximum enclosed by `EncloseMaximumT0`, `Geometry.cs:198`, `:226-234`).

The certificate requires blended neighbours to share abscissae (`Geometry.cs:402-403`), so every blend evaluable today
is between profiles on one spacing.

### 2.2 How the promoted profile P is built

Inputs: the accepted source, the slice η_p, neighbours L and R.

**Preconditions (CG3).**
- **Record-level identity of the spacing:** L.upper, L.lower, R.upper and R.lower have the same degree, the same knot
  vector and the same control x, element by element. This is the test of `Placement.SameAbscissa`
  (`Placement.cs:699-705`), applied to all four curves; span-level `SharedAbscissa` (`Geometry.cs:281-289`) is not enough,
  because the construction pairs control indices. Upper and lower sharing degree, knots and control x within one
  profile is **Verified**: the certificate requires it (`Geometry.cs:352-354`). If the four-curve identity fails, Promote
  refuses with `STATION-SPACING`.
- **Asset neighbours:** a profile stored as `asset sha256 …` has no inline curves (`FoilSource.cs:1327`). It cannot reach
  Promote today: the parser refuses any asset profile with `DSL-REFERENCE` (`FoilSource.cs:1530`), so an accepted
  source never holds one. Promote **refuses** with `STATION-ASSET` if a future resolver admits them; there is no existing
  resolver to read (Verified by the same line).

**Construction.**
1. **R0 (same geometry).** P is a byte copy of L's block under the new name, by the text path `MakeIndependent` uses
   (`FoilSource.RewriteProfile`, `FoilSource.cs:739-753`). Deviation 0.
2. **R1/R2 (different geometry, identical spacing).** For each control index i:
   `(c_Li, U_Li) = PlacementRule.Components(upperL_i, lowerL_i, 1/M_L)` (and R);
   `(upperP_i, lowerP_i) = PlacementRule.Blend((c_Li, U_Li), (c_Ri, U_Ri), w_p, 1, t_p)` in `Binary64`; knots and control
   x copied bitwise from L. Valid because B-spline evaluation is linear in the control ordinates when x(t) is shared, and
   Components/Blend are linear for fixed scalars.
   - **Scalars (CG4).** `M_L`, `M_R` come from `Placement.ProfileDifferenceMaximum(upper, lower)`, which is `internal`
     (`Placement.cs:407`) and runs the grid-401 search with safeguarded Newton (`DifferenceMaximum`, `Placement.cs:590-598`,
     `:633-680`). Verified. `BlendedMaximum` and `PreparedProfile.Maximum` are **private** (`Placement.cs:614`,
     `:563-567`) and are not called. Instead P is first built with scale 1 (as above); then
     `k = ProfileDifferenceMaximum(P.upper, P.lower) / t_p` is P's own maximum, and P's ordinates are rescaled about its
     camber by `1/k` so P's maximum thickness equals `t_p`. `t_p = t/c(η_p)` from `ChannelEvaluator.At` (`Placement.cs:140`).
   - **Neither scalar changes the loft:** the certificate renormalises P by its own maximum (`Geometry.cs:388-389`). They
     make P, opened alone in the section editor, show the true section at η_p. **`Placement.cs` stays read-only.**
3. **R3 (different spacing).** Unreachable today (Unsupported before any slice evaluates). Promote is disabled with
   S1-COPY-9. After Ruling 73's compatible fit is built, step 2 runs on the compatible copies on S\*
   (`cross-profile-abscissa.md` §3.1) with the surface-level deviation at 10 µm.
4. **Closure.** `closed` iff both L and R are `closed` (`FoilSource.cs:1330` reads only `open`/`closed`): two zero
   trailing-edge gaps combine to exactly zero; any open neighbour gives a positive gap, as `Geometry.cs:387` requires.
5. **Name (DR-S1-3, settled by the leader: no η in the name).** The station-label convention
   `StationNames.Of(index, eta)` (`StationNames.cs:13-14`) at the new station's index, for example `"Station 2"`. A
   collision with an existing profile name (possible after a later insertion renumbers labels) is resolved by the
   `MakeIndependent` loop shape (`FoilSource.cs:501-507`): append `-2`, `-3`, … until unique. The profile name is an
   identity and does not renumber when later stations do; the station **label** is always `StationNames.Of` at display.
6. **Provenance.** `provenance "promoted from the blend of <L name> and <R name> at <η text>"` (grammar line,
   `FoilSource.cs:235-236`). η lives here, not in the name.

### 2.3 How it is written into the source

`FoilSource.AddAssignment(byte[] source, double eta, string etaText, string profileName, string profileBlock)` beside
`MakeIndependent`: inserts the block after the last profile block with its indent (`FoilSource.cs:510-513`); inserts
` at <etaText> profile "<name>"` before the first assignment with greater η; re-parses, Guard `DSL-PATCH` (as `:516`);
requires the parsed η of the new assignment to equal `eta` **bitwise**.

**The η text (DR-S1-4, settled by the leader).** The unit the document already uses for its interior stations (`%`, `mm`,
`cm` or `m`, `FoilSource.cs:72-77`); with no interior station, `%` of half-span. The text is the shortest decimal in that
unit that the parser maps to the slice's binary64. The slice holds its position as that text: pointer and nudge move it
on the document unit's grid (§5.2), and the slice's η is **re-set to the parse of its text** before any preview. So the
station lands exactly on the η that was evaluated, and rounding cannot cost exactness; a text that does not round-trip is
a refusal (`DSL-PATCH`), never a silent shift. A length unit keeps absolute position under a span change (`FoilSource.cs:77`);
`%` keeps relative position (`foildsl.md:451-453`); following the document keeps its stations consistent.

`FoilSource.RemoveAssignment(byte[] source, int assignmentIndex)`: deletes the assignment tokens and the block if no other
assignment references it (ST-I2); re-parses; Guard `DSL-PATCH`. Root and tip refused first (`STATION-ENDPOINT`).

### 2.4 Why the loft on both sides is (or is not) unchanged

On [η_L, η_p] a point η has new weight `v = w / w_p` (exact; `RationalInterval.BlendWeight`, `Geometry.cs:196`).

- **Camber is always exact:** `(1 − v)·c_L + v·((1 − w_p)·c_L + w_p·c_R) = (1 − w)·c_L + w·c_R`, up to P's rounding.
- **Thickness:** `U_P = T(w_p)/k`, `k = max T(w_p) ≤ 1`. The new unnormalised shape
  `(1 − v)·U_L + (v/k)·((1 − w_p)·U_L + w_p·U_R)` is a multiple of `(1 − w)·U_L + w·U_R` **iff k = 1**.
- **k = 1 iff U_L and U_R reach 1 at a common chord fraction.**
- **Uniqueness (from the Adversary, Verified by derivation):** writing `U_P = αU_L + βU_R`, proportionality for every v
  forces `α = 1 − w_p`, `β = w_p`, so `U_P = T(w_p)`, admissible only if k = 1. Under rule A no choice of P makes R2 exact.

| Regime | Neighbours | Promoted loft vs old loft | Acceptance |
|---|---|---|---|
| **R0** | same geometry | identical (P is a copy) | deviation 0 |
| **R1** | differ; identical spacing; measured deviation ≤ 10⁻¹² | identical up to rounding | exact promotion, one press |
| **R2** | differ; identical spacing; measured deviation > 10⁻¹² | both intervals move; the section at η_p is exact | approximate promotion (Ruling 252): preview, Apply only at ≤ 10 µm |
| **R3** | differ in spacing | not evaluable today | `STATION-SPACING`; after Ruling 73: compatible copies, 10 µm |

**The rule (Ruling 252, S1-Q1 (d)), verbatim:** the regime is decided by the measured deviation over both adjacent intervals, never predicted from peak positions; a promotion measured at or below 1e-12 relative is one press and exact; any larger one is offered only as a separately labelled approximate promotion that opens a preview with the measured deviation, where it was found and the 10 µm limit at the local chord, and applies only with Apply at or below 10 µm, with Apply disabled and the number shown above it; a one-press call on such a pair is refused with STATION-INEXACT.

The loft-rule-a note's "promotion is exact" holds for the channels (ST-I3), not for the renormalised profile blend.

### 2.5 How big R2 is — shape-dependent (CG2)

All numbers here are **a model of `Placement.cs` rule A, not the product** (Inferred for the product; T4 measures the
product).

**On a committed real pair (step 1 of cycle 1).** Script `r2_real_pair.py` (kept in the track's scratch folder, not in
the tree), inputs `docs/examples/foildsl/foil-basic.foil` profile `section-a` (degree-5 B-spline, evaluated by de Boor)
and `src/CfdWorkbench.Core/CatalogData/naca-0012.dat` (piecewise-linear per side); root at η 0, tip at η 1, promote at
η 0.5, t/c 0.12; deviation over 1999 η × 2001 x:

| Pair | Peaks (x of max) | k | Largest deviation | At η | At 150 mm chord |
|---|---|---|---|---|---|
| naca-0012.dat vs itself (control) | 0.291 / 0.291 | 1.000000 | 1.3 × 10⁻¹⁷ | — | 0 |
| naca-0012.dat vs naca-0009.dat (control) | 0.291 / 0.291 | 1.000000 | 1.3 × 10⁻¹⁷ | — | 0 |
| **foil-basic.foil `section-a` vs naca-0012.dat** | **0.331 / 0.291** | **0.999033** | **5.1 × 10⁻⁷ of chord** | 0.25 | **0.08 µm** |
| naca-0012.dat vs naca-4412.dat | 0.291 / 0.288 | 0.999994 | 3.4 × 10⁻¹⁰ | 0.75 | 0.00005 µm |

(The 0.291 peak is the .dat grid's, not NACA's 0.30: piecewise-linear interpolation.) Two readings: the shipped NACA
00xx sections share one unit shape and are R1; **a cambered catalog section against a symmetric one is already R2**
(3.4 × 10⁻¹⁰ > 10⁻¹²), so an exact-only rule would refuse NACA 4412 beside NACA 0012.

**Across shape families (the Adversary's models, `r2_check.py`, `r2_check_linear.py`):**

| Peaks | Piecewise-linear remap | Power remap | Power remap at 150 mm |
|---|---|---|---|
| 30/35 % | 7.9 × 10⁻⁷ | 2.2 × 10⁻⁶ | 0.33 µm |
| 30/40 % | 5.2 × 10⁻⁶ | 1.8 × 10⁻⁵ | 2.7 µm |
| 30/50 % | 3.1 × 10⁻⁵ | 1.5 × 10⁻⁴ | **23 µm** |

The size depends on the shape family, up to 1.5 × 10⁻⁴ of chord. **Under any gated option some real R2 pairs will be
refused** (a 30/50 % pair on a 150 mm chord exceeds 10 µm). The largest change sits on the far interval.

### 2.6 The deviation: measured how, against what, in which norm

- **Compared:** the accepted source before the command against the candidate after it, both through
  `Placement.Sections(source, etas, xs)` (`Placement.cs:334`); upper = camber + thickness/2, lower = camber − thickness/2.
- **Where:** a 129-point uniform η grid on each affected interval (Promote: [η_L, η_p] and [η_p, η_R]; Remove:
  [η_L, η_R]) plus the station η values; x on a 2001-point uniform grid.
- **Norm:** max over the samples of `max(|Δupper|, |Δlower|)`.
- **Relative to:** the local chord (sections are in chord fractions): `δ_rel`; in metres `δ_m = max c(η)·δ(η)`. Channels
  are unchanged, so the placed distance is `c(η)·δ(η)`.
- **Reported:** `δ_rel`, `δ_m`, the η where it occurs. The number is labelled sampled and names its grid (129 η per
  interval × 2001 x); it is never presented as a bound (Ruling 252).
- **Acceptance:** exact ≤ 10⁻¹² relative; approximate `δ_m ≤ 10 µm` (A4.6).
- **The regime is the measurement**, never a prediction from `k`.

### 2.7 The certificate budget (CG1, CG5)

- **As built (Verified, re-opened):** up to **four** differing stations certify at up to 27 spans, five up to 20 spans,
  six up to 5 spans, seven never (`tests/CfdWorkbench.Core.Tests/BlendTests.cs:118-131`
  `Blend_FourDifferingStations_Certify_SevenRefusedByOperations`; `docs/proof/blend-certificate-heap/verdict.md:205-209`).
  The `SectionEdits.cs:62-64` comment that four are refused is stale (reported, not chased).
- R0 adds no differing station; R1/R2 add one. P shares its neighbours' spacing, so the span count is unchanged.
- **The check, a wrapper in `AuthoringSession` (not a reuse of `RequireBlendBudget`):** `RequireBlendBudget` throws
  `DSL-GEOMETRY` (`SectionEdits.cs:79`, `:91`) and its `edited` filter and positional `differedBefore`
  (`SectionEdits.cs:65-71`) assume an in-place edit; after an insertion every later index shifts by one. So Promote runs
  the core of that clause directly: `Geometry.Assess(new SourceParse([], [], candidate))` (the same call,
  `SectionEdits.cs:84`). The base is the accepted, certified loft (the slice could not evaluate otherwise). Mapping:
  - `Status == NotAssessed && Code == Geometry.OperationBoundCode` → `STATION-BUDGET`, naming the differing stations;
  - any other non-`Certified` status → `STATION-NOT-ASSESSED` with that assessment's reason.
  No `edited` argument is passed; `SectionEdits.cs` is not touched.

### 2.8 The evaluator id

`cfdw-cv/2`, unchanged (`AuthoringSession.cs:571`, `FoilSource.cs:1549`); P shares spacing, so `cfdw-cv/3` is not
triggered. The surface hash changes even in R0 (`FoilSource.cs:1551-1552`), so a promotion makes a new design row.

### 2.9 Remove station

1. Root and tip → `STATION-ENDPOINT`; index out of range → `DSL-TARGET`.
2. Candidate = `RemoveAssignment(source, index)`; the gap is re-blended by rule A. Nothing is fitted (fitting the
   neighbours would rewrite authored sections).
3. Deviation per §2.6 over [η_L, η_R].
4. `PreviewRemove` returns the deviation and `Admitted = δ_m ≤ 10 µm`; `RemoveStation` refuses with
   `STATION-REMOVE-ACCEPTANCE` otherwise (DR-S1-2 (a), settled by the leader).
5. No budget check: removal never adds a differing station.
6. Add then Remove restores the source bytes (T9).

## 3. Commands and undo

### 3.1 Core API (owned: `AuthoringSession.cs`, `FoilSource.cs`)

```text
AuthoringSession.PreviewPromote(double eta, string etaText)        -> PromotionPreview   // no row; off the UI thread
AuthoringSession.AddStation(string operationId, double eta, string etaText, bool approximate) -> PromotionOutcome
AuthoringSession.PreviewRemove(int index)                          -> RemovalPreview
AuthoringSession.RemoveStation(string operationId, int index)      -> RemovalOutcome

record PromotionPreview(double Eta, string EtaText, int Left, int Right, string Regime /* R0|R1|R2 */,
    double DeviationRelative, double DeviationMeters, double AtEta, bool Admitted, string? RefusalCode, string? Reason);
record PromotionOutcome(string AcceptedId, int Index, string ProfileName, PromotionPreview Report, ThicknessIntent Thickness);
record RemovalPreview(int Index, double DeviationRelative, double DeviationMeters, double AtEta,
    double LimitMeters /* 10e-6 */, bool Admitted, bool ProfileRemoved);
record RemovalOutcome(string AcceptedId, RemovalPreview Report);
```

`AddStation(…, approximate: false)` refuses an R2 result with `STATION-INEXACT`; `approximate: true` (the flyout's Apply,
Ruling 252) admits R2 at ≤ 10 µm and refuses above it with `STATION-INEXACT`. Each mutating call runs in `Run("add-station" | "remove-station", …)` as
`ApplyPointCommand` (`AuthoringSession.cs:310-311`) and calls `Commit(…, "apply")` once (`:565`); `Undo` (`:459`) walks
one parent. Preconditions copy `PreviewRebuilds` (`:319-321`): document open, no draft, no recovery, else `DSL-DRAFT-OWNED`.

### 3.2 Refusals (stable codes; nothing changes on any refusal)

| Code | When | Copy |
|---|---|---|
| `STATION-COINCIDENT` | the resolved span position (η × half-span, metres) is within the 10 µm join tolerance of an existing station's, bitwise equality included, root and tip included (Ruling 252, S1-Q3 (b)); carries the existing index; the existing station is selected. | S1-COPY-6 |
| `DSL-RANGE` | η not finite or outside (0, 1). | S1-COPY-7 |
| `STATION-ENDPOINT` | Remove of root or tip. | S1-COPY-12 |
| `STATION-BUDGET` | candidate `Assess` returns `GEOMETRY-QUERY-OPERATIONS` (§2.7). | S1-COPY-8 |
| `STATION-NOT-ASSESSED` | candidate or base not Certified for another reason. | S1-COPY-10 |
| `STATION-SPACING` | the four-curve spacing identity fails (R3). | S1-COPY-9 |
| `STATION-ASSET` | a neighbour is an asset reference (unreachable today, §2.2). | S1-COPY-10 |
| `STATION-INEXACT` | R2 on the one-press path (always), or R2 above 10 µm on the approximate path. | S1-COPY-11 |
| `STATION-REMOVE-ACCEPTANCE` | Remove with δ_m > 10 µm. | S1-COPY-15 |
| `DSL-DRAFT-OWNED` | a draft is open (UX-23). | S1-COPY-13 |
| `DSL-PATCH` | the candidate does not re-parse or the η text does not round-trip (a defect). | generic |

### 3.3 Thickness policy — settled by Ruling 252 (S1-Q2 (a))

Promote and Remove keep current thickness, the t/c channel stays byte-identical, and the receipt says so (Ruling 252). They are **Keep current thickness**; the t/c record is byte-for-byte unchanged (CAD-10's
definition); the outcome carries `ThicknessIntent.KeepCurrent` (`Contracts.cs:116`); the receipt says so. T15 asserts the channel bytes. Amendment A-S1-2 stands.

### 3.4 Referenced profile closure

After Add the new block is referenced exactly once; after Remove no block is unreferenced (ST-I2). T16.

## 4. E7 change-surface list

| # | Surface | Change | File (owner) |
|---|---|---|---|
| 1 | Source | `AddAssignment`, `RemoveAssignment` | `src/CfdWorkbench.Core/FoilSource.cs` (S1) |
| 2 | Definition | none | — |
| 3 | Blend construction + budget wrapper | control-point blend; `Assess` on the candidate | `src/CfdWorkbench.Core/AuthoringSession.cs` (S1); `Placement.cs`, `Geometry.cs`, `SectionEdits.cs` read only |
| 4 | Session | `PreviewPromote`, `AddStation`, `PreviewRemove`, `RemoveStation` | `AuthoringSession.cs` (S1) |
| 5 | Names | new callers of `StationNames.Of` | `src/CfdWorkbench.Core/StationNames.cs` (read; S1 owns it for a doc comment) |
| 6 | CLI `inspect --json` | none expected: prints `AuthoredProjection.Assignments` (`Contracts.cs:36`), Inferred; T12 | `src/CfdWorkbench.Cli/Program.cs` (read) |
| 7 | Selection | `Selection.Slice(double Eta)` | `src/CfdWorkbench.Desktop/Selection.cs` (S1 build: **added to the owned list**) |
| 8 | Controller | slice verbs, `PromoteSlice`, `PreviewRemoveStation`, `RemoveStation`; enablement and reasons | `src/CfdWorkbench.Desktop/WorkbenchController.cs` (S1, after A3) |
| 9 | Plan | slice line and chip; promoted chip selected | `src/CfdWorkbench.Desktop/PlanCanvas.cs` (S1) |
| 10 | Elevation | Front: slice line and selected station 3 px; Side: slice outline, selected station | `src/CfdWorkbench.Desktop/ElevationView.cs` (S1) |
| 11 | 3D view | the promoted station drawn selected, 3 px `station` with its name chip (`DESIGN.md:250`), by the existing selected-station path (Inferred; D4 proves it); the slice is **not** drawn (DR-S1-6) | read; any fix is in the renderer the build finds |
| 12 | Browser | the new row selected **and brought fully into view** when selected from Plan, Elevation, 3D or a command, at every scale (addendum) | `src/CfdWorkbench.Desktop/Panes/BrowserPane.axaml`, `BrowserPane.axaml.cs` (S1, after D1) |
| 13 | Properties | Slice panel; station panel Remove; the station strip thumbnail and Edit section for the new station (CAD-09) | `src/CfdWorkbench.Desktop/PropertiesView.cs` (S1, after A3) |
| 14 | Command table + menu | `station.add`, `station.remove` | `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs` (S1) |
| 15 | Status line | receipts and refusals | the controller's status text (Inferred) |
| 16 | Save / reopen | none | — (T11) |
| 17 | Copy | S1-COPY-1..24 | `DESIGN.md` §7 (S1 build) |

**Consistency (E7):** D4 asserts, after one Promote, that the Plan chip, Front, Side chip, 3D name chip, Browser row
(in view), Properties heading and `AuthoredProjection` all name "Station 2" at η 35 %, and that the former "Station 2"
reads "Station 3" in all of them.

## 5. UI

### 5.1 Archetype signature

Inherited: `ParametricWorkbench { Type:Configurator; Arch:SpatialBounded; Layout:ViewportWorkbench; Density:Compact;
Nav:TaskTabs+CommandPalette }`. Reading is parallel (the slice in Plan, Front, Side, Properties); entering is serial and
one value. The slice is a selection, not a mode. **Anything that changes shape previews before Apply** (Remove; the
approximate promotion, Ruling 252); exact promotion is one press.

### 5.2 The inspection slice

- **Place (Plan):** press on empty space in the station chip strip (Ruling 124: the strip is the span ruler); drag moves.
  Position snaps to the document unit's grid: 0.1 mm of distance from root in a length-unit document, 0.1 % of half-span
  in a `%` document.
- **Place (Front):** press-drag on the span axis of the t/c lane (`ElevationView.cs:19-20`). Same snap.
- **Place (keyboard):** `station.add` ("Add station…") focuses the η entry on the options strip (CAD-07 precedent);
  typing echoes live; Return promotes (or opens the approximate flyout); Escape clears and returns focus.
- **Move (UX1):** drag; or with the slice chip focused, ← → nudge **0.01 · 0.1 · 1 mm (⌘/Ctrl · plain · Shift)**, the
  shell's three-step convention (`DESIGN.md:248`).
- **Shown as (UX2):** Plan — dashed `station` line on both halves; hollow dashed chip "Slice · η 35.0 % · 175.0 mm" with
  Promote and ×. Front — dashed vertical line. Side — dashed outline at full weight with its chip. Properties — "Inspection
  slice", η, **distance from root in mm**, chord, t/c, twist, "Derived view — not saved", Promote.
- **Snap to a station:** within 4 px of a station line the selection becomes that station (GEO-06 "existing station
  selected").
- **Cleared by:** Escape, ×, selecting anything else, opening another document. Not saved, not undoable, not source.

### 5.3 Promote

- Enabled when a slice is selected, no draft is open, and the slice is strictly inside an interval.
- Press: `PreviewPromote` off the UI thread ("Checking…"). Then:
  - **R0/R1:** `AddStation(approximate: false)`; receipt S1-COPY-3/-4.
  - **R2 (Ruling 252, S1-Q1 (d)):** the **approximate promotion** flyout (the Remove flyout's shape): title
    S1-COPY-21, the measured line S1-COPY-22, Apply enabled at ≤ 10 µm, disabled above with S1-COPY-23. Apply calls
    `AddStation(approximate: true)`; receipt S1-COPY-24.
  - The flyout shows the measured deviation, where it was found (η) and the 10 µm limit at the local chord, labelled
    sampled with its grid; above 10 µm Apply is disabled with the number shown above it. A one-press call on an R2 pair
    is refused with `STATION-INEXACT` (Ruling 252).
  - Refusal: the slice stays; the reason on the chip and the status line.
- Cost: one parse, one construction, one candidate `Assess`, 2 × 129 × 2001 samples; measured by T-perf (no number claimed).
- Success: the new station selected in **every** view (Plan, Front, Side, 3D, Browser in view, Properties with its
  thumbnail and Edit section), the slice gone, renumbering line S1-COPY-5 when later stations renumbered.

### 5.4 Remove station

`station.remove`, Edit menu, palette and a Remove station button in the station's Properties (`DESIGN.md:299` places it
there). Disabled with reason for Root/Tip (S1-COPY-12) and during a draft (S1-COPY-13). Invoke opens the flyout: "Remove
Station 3", S1-COPY-14, Apply / Cancel; above 10 µm Apply is disabled with S1-COPY-15 as its description; Return applies
when enabled; Escape cancels and returns focus. After Apply the left neighbour is selected; status S1-COPY-16.
Follow-up, **not built**: the Adversary's "Match the blend" step (a previewed re-blend after which Remove is exact).

### 5.5 Every state

| State | Plan strip | Front / Side / 3D | Browser | Properties | Status |
|---|---|---|---|---|---|
| No slice | station chips | as today | rows | per selection | — |
| Slice placed | slice chip + Promote + × | dashed line / outline; not in 3D | no row selected | Slice panel | S1-COPY-1 |
| Checking | "Checking…" | — | — | Promote disabled | — |
| Promoted (R0/R1) | new chip selected | selected in Front, Side, 3D | new row selected, in view | "Station 2", thumbnail, Edit section | S1-COPY-3/4 (+5) |
| Approximate promotion (R2) | flyout: number, Apply ≤ 10 µm | — | — | — | S1-COPY-24 after |
| Coincident η | existing chip selected | existing selected | its row, in view | its panel | S1-COPY-6 |
| Root/tip remove | — | — | — | Remove disabled + reason | S1-COPY-12 |
| Remove above acceptance | flyout: Apply disabled + reason | — | — | — | — |
| Remove within acceptance | flyout: Apply enabled | — | — | — | S1-COPY-16 |
| Budget refused | slice stays; reason | — | — | Promote reason | S1-COPY-8 |
| Mixed spacing (R3) | Promote disabled + reason | — | — | reason | S1-COPY-9 |
| Competing draft | Promote disabled + reason; slice placeable (CAD-11) | — | — | Promote, Remove disabled + reason | S1-COPY-13 |

### 5.6 Accessibility (WCAG 2.2 AA from DESIGN.md tokens)

- Slice in `station` (#66ddc8) on `viewport` (#17272c), dashed plus the word "Slice" — never colour alone (SC 1.4.1).
- Focus rings `focus-ring-viewport` / `focus-ring`, ≥ 3:1 (SC 2.4.11, 1.4.11).
- Slice chip: one focusable object, name "Inspection slice, η 35.0 percent, 175.0 millimetres", Invoke = Promote; buttons
  ≥ 24 × 24 px (SC 2.5.8; 32 px in the strip).
- Disabled controls carry their reason as the accessible description.
- Status receipts in the polite live region (existing behaviour, Inferred).
- Reduced motion: nothing animates. High contrast: `contrast-*` tokens; the dash stays 2 px.
- **Focus not obscured (SC 2.4.11) for the Browser:** the selected row is scrolled fully into view when the selection
  comes from another view (addendum; D8).

## 6. Copy table (provisional ids; mapped to DESIGN.md §7 at build)

`<η>` is η with one decimal ("35.0 %"); `<d>` the distance from root in mm, one decimal; `<name>` from `StationNames`.

| Id | Exact string |
|---|---|
| S1-COPY-1 | Inspection slice at η <η> · <d> mm. Not saved. Promote makes it a station. |
| S1-COPY-2 | Slice · η <η> · <d> mm |
| S1-COPY-3 | Added <name> at η <η>. Shape unchanged: deviation 0. Thickness: keep current. |
| S1-COPY-4 | Added <name> at η <η>. Largest change <rel> of chord (<µm> µm) at η <at>, sampled; limit 10⁻¹². Thickness: keep current. |
| S1-COPY-5 | Stations after it are renumbered: <old> is now <new>. |
| S1-COPY-6 | <name> is already at <d> mm (within the 10 µm join tolerance). Selected it. |
| S1-COPY-7 | A station must lie strictly between root and tip (0 % < η < 100 %). |
| S1-COPY-8 | Promote refused: <count> stations would have different sections, more than the blend certificate can carry. Make one of them match a neighbour first. Nothing changed. |
| S1-COPY-9 | Promote is not available: <left> and <right> have different point spacing, so this loft is not assessed. |
| S1-COPY-10 | Promote is not available: the loft is not assessed (<reason>). |
| S1-COPY-11 | Promote refused: the new station would change the loft by <µm> µm (<rel> of chord) at η <at>, above the 10 µm limit. Nothing changed. |
| S1-COPY-12 | Root and tip stations cannot be removed. |
| S1-COPY-13 | Apply or cancel the open draft on <target> first. |
| S1-COPY-14 | The shape between <left> and <right> is re-blended. Largest change <µm> µm (<rel> of chord) at η <at>, sampled. Limit 10 µm. |
| S1-COPY-15 | Apply is disabled: the change is above the 10 µm limit. |
| S1-COPY-16 | Removed <name>. Largest change <µm> µm at η <at>. Thickness: keep current. |
| S1-COPY-17 | Add station… |
| S1-COPY-18 | Remove station |
| S1-COPY-19 | Derived view — not saved |
| S1-COPY-20 | Checking… |
| S1-COPY-21 | Approximate promotion at η <η> · <d> mm |
| S1-COPY-22 | A station here changes the loft. Largest change <µm> µm (<rel> of chord) at η <at>, sampled on 129 η × 2001 x, not a bound. Limit 10 µm at the local chord. |
| S1-COPY-23 | Apply is disabled: the change is above the 10 µm limit. *(same text as S1-COPY-15)* |
| S1-COPY-24 | Added <name> at η <η> as an approximate promotion. Largest change <µm> µm (<rel> of chord) at η <at>. Thickness: keep current. |

S1-COPY-11 is the `STATION-INEXACT` refusal for both a one-press call on an R2 pair and an approximate promotion above
10 µm (Ruling 252).

## 7. Test plan (red first)

**Red discipline (TA6).** The build first adds the four Core methods and the controller verbs as **stubs that return the
base unchanged** (no exception). Every test below then fails on its named assertion against its named input, which is
the red evidence; the build records the failing line per test before writing the code.

**Fixtures** (`tests/fixtures/stations/`, built by hand on `NewDefault`'s 10-point degree-5 basis, `FoilSource.cs:530`,
so all share one spacing):

- **F-SAME:** `NewDefault()`, NACA 0012 at root and tip (R0).
- **F-CAMBER:** root NACA 0012 basis; tip = root plus a camber line added equally to upper and lower control y (U_L ≡ U_R; R1).
- **F-CAMBER3:** F-CAMBER plus a station at η 60 % with half the tip's camber (R1 on both intervals; four differing after one promotion).
- **F-SAMEPEAK (TA4):** two different unit thickness shapes with a common peak: root and tip both have control ordinates
  2..7 of the thickness equal (a plateau: the degree-5 curve is exactly constant and maximal on the middle span, by the
  convex hull), differing at ordinates 1 and 8. U_L ≠ U_R (≥ 10⁻³ apart somewhere) and k = 1 in exact arithmetic (R1 beyond the degenerate case).
- **F-PEAK:** root NACA 0012 basis; tip = a 10-point fit of a thickness peaking at 40 % (R2).
- **F-REAL:** `NewDefault()` with the tip replaced by catalog `naca-4412` through `SectionReplace` (which fits onto the
  neighbour's spacing, m12d §3.6): a committed real pair; the model gives 3.4 × 10⁻¹⁰ (R2).
- **F-THREE:** F-CAMBER with a station at η 60 % whose profile is F-PEAK's tip (Remove above acceptance).
- **F-SIX:** six stations (η 0, 20, 40, 60, 80, 100 %), every section different, 10 points (5 spans): certifies (six up
  to 5 spans); one more differing station makes seven (refused).
- **F-SPACING:** a 6-point tip on a 10-point root (Unsupported, R3).

**Independent expected values.** F-CAMBER at η 0.35: expected P control ordinates are hand literals (camber_i =
0.35 × tipCamber_i; unit thickness = root's). Section-equality oracle: `Geometry.SectionAt` on the before certificate
against `SectionAt` on the after certificate (existing code, not under test).

| # | Test (project) | Clause | Failing input → assertion that is red on the stub | Cost |
|---|---|---|---|---|
| T1 | `Station_PromoteAt035_SectionEqualsBlend_1e12` (Core `StationTests.cs`) | CAD-02 η 0.35; GEO-06 | F-CAMBER, η 0.35 → after-`SectionAt` = before-`SectionAt` at 33 x ≤ 1e-12 **and** assignment count 3 (stub: 2) and P's ordinates = hand literals ±4 ulp | < 1 s |
| T1b | `Station_PromoteLaterBracket_UsesRightNeighbours` (Core) | M1 | F-CAMBER3, η 0.8 (bracket 1) → deviation ≤ 1e-12, P's camber = 0.5·(midCamber + tipCamber) literals (stub: no P) | < 1 s |
| T2 | `Station_PromoteR1_LoftUnchangedBothIntervals` (Core) | GEO-06 | F-CAMBER and **F-SAMEPEAK**, η 0.35 → `Regime == "R1"`, `DeviationRelative ≤ 1e-12`; 17 η per interval by `SectionAt` (stub: Regime null) | ~1 s |
| T3 | `Station_PromoteR0_CopyIsBitIdentical` (Core) | GEO-06 exact | F-SAME, η 0.5 → deviation exactly 0; P's block = root's block after rename (stub: no block) | < 1 s |
| T4 | `Station_PromoteR2_ReportsMeasuredDeviation` (Core) | GEO-06 reported deviation | F-PEAK and F-REAL, η 0.5 → `Regime == "R2"`, `DeviationRelative > 1e-12`, `AtEta` recorded; outcome for (d) (Ruling 252): `AddStation(approximate:false)` throws `STATION-INEXACT`; `approximate:true` is admitted iff `DeviationMeters ≤ 10e-6`, else `STATION-INEXACT` with bytes unchanged; the preview names its grid and is labelled sampled; the product's measured numbers for F-PEAK and F-REAL are written to the test output (the model's 3.4e-10 for NACA 0012/4412 is Inferred) | ~1 s |
| T5 | `Station_CoincidentWithinJoinTolerance_RefusedExistingSelected` (Core) | GEO-06 coincident (A-S1-4, Ruling 252 S1-Q3); M4 | F-SAME (half-span 0.5 m) promote "50" (station at 0.25 m), then three rows compared on **resolved span positions in metres**, not η: bitwise-equal (η 0.5) → `STATION-COINCIDENT`, index 1; just inside (span 0.25 m + 9.9 µm) → `STATION-COINCIDENT`, index 1, bytes unchanged; just outside (0.25 m + 10.1 µm) → admitted, `Regime == "R0"`, deviation 0. Stub: no refusal, no row | < 1 s |
| T6 | `Station_EtaAtRootOrTip_Refused` (Core) | CAD-02 | F-SAME, η 0 and 1 → `STATION-COINCIDENT`; −0.1, 1.2, NaN → `DSL-RANGE` | < 1 s |
| T7 | `Station_RemoveRootOrTip_Refused` (Core) | CAD-02 | F-CAMBER3, index 0 and 2 → `STATION-ENDPOINT`, bytes unchanged | < 1 s |
| T8 | `Station_RemoveAboveAcceptance_ReportsAndRefuses` (Core) | CAD-02 remove | F-THREE, index 1 → `Admitted == false`, `DeviationMeters > 10e-6`; `RemoveStation` → `STATION-REMOVE-ACCEPTANCE` | ~1 s |
| T9 | `Station_AddThenRemove_RestoresBytes` (Core) | CAD-02 remove, measured | F-CAMBER promote 0.35, preview remove → admitted, ≤ 1e-12; after remove bytes = original | < 1 s |
| T10 | `Station_UndoAddAndRemove_OneStepEach` (Core) | undo one step | F-CAMBER: AddStation, `Undo` once → bytes = base; RemoveStation, `Undo` once → restored; `Redo` | < 1 s |
| T11 | `Station_SaveReopen_StationPresent` (Core) | save/reopen | F-CAMBER promote, `SaveImage`, `Reopen` → 3 assignments, η bitwise 0.35, profile "Station 2" | < 1 s |
| T12 | `Station_InspectProjection_ShowsStation` (Core; Cli harness line if `Program.cs` serialises the projection) | CLI `inspect --json` | `Assignments[1]` = (0.35, span, "Station 2") | < 1 s |
| T13 | `Station_SeventhDifferingSection_RefusedBudget` (Core) | Ruling 73 refuse early (CG1, TA1) | F-SIX, slice η 0.7 → base certifies; candidate `Assess` code is `GEOMETRY-QUERY-OPERATIONS`; `AddStation` throws `STATION-BUDGET` naming 7 stations; bytes unchanged. Control: F-CAMBER3 at η 0.8 (four differing) is admitted | ~2 s |
| T14 | `Station_MixedSpacing_PromoteRefused` (Core) | R3 | F-SPACING → `STATION-SPACING` | < 1 s |
| T15 | `Station_Promote_ThicknessKeepCurrent_ChannelsByteIdentical` (Core) | CAD-02 thickness policy (Ruling 252 S1-Q2 (a)) | F-CAMBER → every channel's bytes identical before and after; `Thickness == KeepCurrent` | < 1 s |
| T16 | `Station_ProfileClosure_ReferencedOnceAndClosedRule` (Core) | CAD-02 closure | new block referenced once; closed+closed → `closed`, closed+open → `open`; remove of sole user deletes block; shared profile kept | < 1 s |
| T17 | `Station_EditPromotedProfile_NeighbourBlendUpdates` (Core) | CAD-02 "edit its profile" | F-CAMBER promote; `SectionStep.Move` on P → sections at η 0.2, 0.6 change; root/tip ≤ 1e-12 | ~1 s |
| T18 | `Station_DraftOpen_PromoteAndRemoveRefused` (Core) | CAD-09 competing draft | open point draft → `DSL-DRAFT-OWNED` on both | < 1 s |
| T19 | `Station_AssignProfileToPromoted_PreviewReplace` (Core) | CAD-02 "assign its profile" (TA5) | F-CAMBER promote; `PreviewReplace` (`AuthoringSession.cs:400`) with catalog `naca-0009` on the new assignment → preview names the station and both adjacent intervals; Apply changes only that assignment | ~1 s |
| T-perf | `Station_PreviewPromote_RecordsDuration` (Core) | instrumentation | `add-station` event carries a duration (recorded, not asserted) | < 1 s |
| D1 | `--plan-canvas` `PlanCanvas_SliceChip_LabelPromoteInStrip` | CAD-09 | slice at 35 % → chip "Slice · η 35.0 % · 175.0 mm", Promote and ×, below `PlanformBounds`, hollow | pixel |
| D2 | `--controller-shell` `Controller_StationVerbs_TableMenuAndReasons` | CAD-02, CAD-09 | rows `station.add`/`station.remove` in table and menu; Remove disabled with S1-COPY-12 on Root/Tip; disabled with S1-COPY-13 during a draft | ~1 s |
| D3 | `--controller-shell` `Controller_TypedCoincidentEta_SelectsExisting` | GEO-06 | typed "60" + Return → `Selection.Station(1, 0.6)`, S1-COPY-6 | ~1 s |
| D4 | `--controller-shell` `Controller_Promote_SelectedEverywhereAndRenumbered` | GEO-06 every view; E7 (UX4) | after Promote: Plan chip, **Front** (3 px station line), Side chip, **3D** name chip, Browser `SelectedIndex`, Properties heading, projection all "Station 2"; former Station 2 is "Station 3"; one Undo restores all | ~2 s |
| D5 | `--plan-canvas` `PlanCanvas_SliceVisibleAtReflow` | CAD-09 each viewport | 1280 × 800 and 640 × 400: Promote visible without a menu | pixel |
| D6 | `--controller-shell` `Controller_RemoveFlyout_ApplyDisabledWithReason` (TA5) | CAD-02 Apply disabled | F-THREE, Remove Station 2 → flyout Apply disabled, accessible description S1-COPY-15; Escape returns focus to the Remove button; on F-CAMBER after promote, Return applies | ~1 s |
| D7 | `--controller-shell` `Controller_PromotedStation_ThumbnailAndEditSection` (TA5) | CAD-09 first clause | after Promote: the station strip thumbnail for Station 2 and the "Edit section…" action (`PropertiesView.cs:1092`) visible without a menu | ~1 s |
| D8 | `--shell-window` `BrowserRow_SelectedFromOtherView_BroughtIntoView_AtScales` (addendum) | GEO-06 every view; SC 2.4.11 | a below-the-fold station selected through the controller (no focus, no fixture scroll) → the row fully inside every ScrollViewer ancestor at RenderScaling 1.0, 1.5 and 2.0 (D1's `ForceScale` helper in `ShellWindowTests.cs`); red first if the product falls short; the fix is reviewed by the leader alone | ~3 s |
| D9 | `--controller-shell` `Controller_ApproximatePromotion_FlyoutAndApply` | Ruling 252 S1-Q1 (d) | F-PEAK, slice 0.5 → flyout S1-COPY-21/22; Apply enabled iff ≤ 10 µm; Apply adds one row | ~1 s |

**Mutants.** M1 wrong bracket → T1b. M2 camber normalised by M → T1. M3 closure always `closed` → T16. M4 coincidence
tested on η or bitwise only, not on span metres → T5's just-inside row (Ruling 252). M5 Remove deletes a referenced block → T16. M6 two commits per
command → T10. M7 stale label in a view → D4. M8 deviation sampled only at η_p → T4 (its maximum is on the far interval).
M9 regime predicted from `k`: killed by F-REAL (k ≈ 1 − 6 × 10⁻⁶ in the model, measured R2) for any tolerance ≥ 6 × 10⁻⁶,
and by F-SAMEPEAK for the exact-equality form if P's `ProfileDifferenceMaximum`-derived `k` is not exactly 1; the build
records the `k` it observes on F-SAMEPEAK and F-REAL in the test output.

**UI craft gate.** `ui-craft-gate.py` on the built surface per CD8 in the build's proof; the design mockup's run is
recorded in the mockup note (not run on this machine).

## 8. Owned paths for the build, and seams

Build-owned: `src/CfdWorkbench.Core/FoilSource.cs`, `AuthoringSession.cs`, `StationNames.cs`;
`src/CfdWorkbench.Desktop/Selection.cs` (**added**), `PlanCanvas.cs`, `ElevationView.cs`, `Panes/BrowserPane.axaml`,
`Panes/BrowserPane.axaml.cs` (the station row **and** the cross-view scroll fix, addendum), `Shell/CommandTable.cs`,
`Shell/NativeMenuBuilder.cs`, `WorkbenchController.cs`, `PropertiesView.cs`; `tests/CfdWorkbench.Core.Tests/StationTests.cs`
(new), `tests/fixtures/stations/*` (new), the Desktop harness files hosting `--plan-canvas`, `--controller-shell` and
`--shell-window`; `DESIGN.md` §7. **Read only:** `Placement.cs`, `Geometry.cs`, `SectionEdits.cs` (cycle 1 removed the
visibility change: the budget wrapper calls `Geometry.Assess` directly).

Seams:
- **`WorkbenchController.cs`, `PropertiesView.cs` after A3's join:** new methods beside the point verbs; a Slice branch
  where `PropertiesView` switches on `Selection`; Remove in the station panel. Re-read: the `Selection` switch, the status
  text API, the draft-owned reason, the station panel's buttons.
- **`BrowserPane.axaml(.cs)` after D1's join:** the row comes from the existing rebuild on `acceptedChanged`
  (`BrowserPane.axaml.cs:67-80`). S1 adds the explicit scroll when `SelectedIndex` is set from another view (`:87-91`).
  Re-read: the row control, the `Tag` shape, `BindStations`, D1's `ForceScale`.
- **3D view:** the build finds the renderer that draws the selected station (`DESIGN.md:250`) and changes it only if D4
  is red.

## 9. Review packet

### 9.1 Computational geometry (Ruling 73 lens)

1. §2.4 inexactness and uniqueness (the Adversary confirmed both).
2. Control-point construction under the four-curve identity (§2.2) — one definition or a second authority?
3. The deviation definition (§2.6) as GEO-06's measure: a sampled lower bound against an enclosure.
4. The budget wrapper (§2.7) and the corrected capacity.
5. Evaluator id unchanged; surface hash changes in R0.
6. Remove as re-blend, no fit.

### 9.2 Marine-CAD UX

7. Slice placement in the chip strip and the Front lane; three-step nudge; mm readout.
8. One-press exact promotion vs previewed approximate promotion; the Remove flyout.
9. Profile name by the station-label convention; renumbering.
10. The slice not drawn in 3D; the promoted station selected in 3D.

### 9.3 What changed in cycle 1

| Condition | Section |
|---|---|
| CG1 budget re-based (4 certify, 7 refused) | §2.7; T13; mockup state 7; S1-COPY-8 |
| CG2 R2 shape-dependent; real-pair measurement | §2.5; §10 DR-S1-1; amendment note |
| CG3 four-curve spacing identity; asset neighbours refused | §2.2 preconditions; §3.2 `STATION-ASSET` |
| CG4 scalars via `ProfileDifferenceMaximum`; `Placement.cs` read only | §2.2 step 2; §8 |
| CG5 budget wrapper and its code mapping | §2.7; §3.2 |
| UX1 three nudge steps | §5.2 |
| UX2 distance in mm | §5.2; §6 S1-COPY-1, -2, -21 |
| UX3 / F3 R2 previews with Apply | §0; §5.3; §6 S1-COPY-21..24; D9; mockup state 9 |
| UX4 selected in Front and 3D | §4 rows 10–11; §5.5; D4 |
| TA1 T13 asserts the code's refusal | §7 T13 |
| TA2 later-bracket promotion | §7 T1b |
| TA3 one-ulp case on F-SAME | §7 T5 |
| TA4 F-SAMEPEAK | §7 fixtures, T2, M9 |
| TA5 Remove flyout, D4 extended, Replace, thumbnail | §7 D6, D4, T19, D7 |
| TA6 failing input per row; T4 written for (d) | §7 red discipline, every row |
| Cycle 2: Ruling 252 (S1-Q1 d, S1-Q2 a, S1-Q3 b) | §0, §2.4, §2.6, §3.1–3.3, §5.3, §6 (S1-COPY-6, -11, -21..24), §7 T4, T5, T15, D9, §10, amendment A-S1-1..4 |
| F14 exact path worded as a measurement | amendment A-S1-1, A-S1-3 |
| F17 certificate maximum citation | §2.2 (`Geometry.cs:388-389`) |
| Addendum Browser cross-view scroll | §4 row 12; §5.6; §8; D8 |
| DR-S1-2, -3, -4, -6 settled by the leader | §10; §2.2 step 5; §2.3; §5.4 |

## 10. Open decisions

- **DR-S1-1 — R2 promotion. Settled by Ruling 252: (d).** (a) exact only: refuse R2 (refuses NACA 4412 beside
  NACA 0012, §2.5); (b) admit R2 at 10 µm on the one-press path; (c) change the blend rule (geometry-authority change,
  out of scope); **(d) (b) with R2 as a separately labelled, previewed approximate promotion (flyout + Apply), R0/R1 one
  press and exact** (the Adversary's missed option); (e) gate R2 on δ_rel instead of δ_m (not recommended: A4.6 is
  chord-local metres). Evidence: §2.4 proof; §2.5 measurement (5.1 × 10⁻⁷ of chord on `section-a`/NACA 0012; 3.4 × 10⁻¹⁰
  on NACA 0012/4412; up to 1.5 × 10⁻⁴ across families — so some real R2 pairs are refused under any gate).
  **Ruled (d)** (Ruling 252); the operator may revisit any amendment.
- **DR-S1-2 — Remove gate. Settled by the leader: (a)** Apply disabled above 10 µm, as CAD-02 reads. Options were (a);
  (b) no gate, like Rebuild to N; (c) "Match the blend" then an exact Remove — recorded as a follow-up, not built.
- **DR-S1-3 — Profile name. Settled by the leader:** no η in the name (it goes stale under Keep absolute station
  positions); the `StationNames.Of` convention with a `-k` collision suffix (§2.2 step 5). Options were "Blend <η> %",
  "<left>–<right> <η> %", positional, and a name without η.
- **DR-S1-4 — η unit. Settled by the leader:** the unit of the document's interior stations, else `%`; shortest
  round-trip binary64 (§2.3). Options were `%`, a length, follow the document.
- **DR-S1-5 — Thickness policy. Settled by Ruling 252: (a).** (a) Keep current thickness, stated in the receipt;
  (b) offer Use source thickness. **Ruled (a)** (Ruling 252). No missed option (the Adversary).
- **DR-S1-6 — Slice in 3D. Settled by the leader: (a)** the slice is not drawn in 3D; the promoted station is shown
  selected in 3D (§4 row 11, D4).
- **DR-S1-7 — Minimum station spacing. Settled by Ruling 252 (S1-Q3): (b).** (a) bitwise coincidence only (as designed; a station
  one ulp away is admitted); (b) treat η within the 10 µm join tolerance in span as coincident and select the existing
  station. Evidence: no vendor-manual citation; a near-zero interval is valid for rule A but is not a useful design
  station. **Ruled (b)** (Ruling 252); amendment A-S1-4.

## 11. Failure modes, threats, privacy, telemetry

| Mode | Disposition | Test |
|---|---|---|
| η text does not round-trip | prevent: Guard `DSL-PATCH` | T1 |
| stale accepted revision | prevent: `CurrentBytes` under the session lock (`AuthoringSession.cs:317-322` pattern) | T10 |
| slow preview | detect: duration per call; "Checking…" | T-perf |
| budget overflow | prevent: refuse early (§2.7) | T13 |
| orphan profile | prevent: block deleted; re-parse | T16 |
| draft open | prevent: `DSL-DRAFT-OWNED` | T18, D2 |
| Browser row off screen after a cross-view selection | detect then fix (addendum) | D8 |

- **STRIDE-lite:** no new trust boundary; the patch re-parses its own output.
- **Privacy (LINDDUN-lite):** no personal data.
- **Telemetry:** the `Run` wrapper's session event (`add-station`, `remove-station`, `preview-promote`, `preview-remove`;
  outcome; duration, `AuthoringSession.cs:33`), plus regime and deviation in the outcome record.

## 12. Status

| Completed | Remaining | Best next action |
|---|---|---|
| Cycle 0 design; cycle 1 fold of all Adversary conditions, the addendum and the leader's DR settlements; real-pair R2 measurement (model) | threat-model and privacy rollups (paths not owned); the build | the leader joins the design and dispatches the S1 build |

**Confidence.** Verified: every `file:line`, read at `a77bc8e1`. Verified by derivation: §2.4. Inferred: §2.5 magnitudes
(models, not the product), the CLI serialisation, the 3D selected-station path, the status live region. Residual risk:
the F-SAMEPEAK plateau fixture constrains six of ten thickness ordinates; if the build cannot make it pass `Assess` it
records why and substitutes a fitted common-peak pair with its measured regime.

## 13. Follow-up SF2 — "Match the blend" before Remove

Track DF of `next-best-six` wave 2 designs this; the SF2 build implements it. It is DR-S1-2 option (c), the S1
Adversary's missed option, recorded in §5.4 as "not built". Labels: **Verified** = opened at `main` `b51315c4`;
**Inferred** = reasoned; **Verified by derivation** = §2.4's algebra applied here.

### 13.1 The answer in five lines

1. **Match the blend** is a previewed command on an interior station M. It replaces M's profile with the rule-A blend
   of M's two neighbours L and R at η_M, built exactly as Promote builds P (§2.2 step 2, `StationEdits.Blend`), and
   leaves η_M, the assignment and the t/c channel unchanged.
2. The preview shows two measured numbers on the Remove grid: the **change** Match makes now, and the **residual** a
   later Remove would still make.
3. **Remove after Match is exact (measured ≤ 10⁻¹²) only when L and R share a thickness peak.** When their peaks differ,
   Remove after Match changes the loft by exactly the R2 promotion deviation of (L, R) at η_M (§13.3). This narrows the
   brief's "after which Remove is exact"; the uniqueness proof of §2.4 shows no profile at η_M can do better under rule A.
4. Match has **no 10 µm gate**: it is a deliberate authored edit, like Rebuild to N (A4.6 1.7 note). Its change is shown
   before Apply. The later Remove keeps its own 10 µm gate (DR-S1-2 (a)).
5. One undo step. One geometry authority: the blend is `PlacementRule.Components` / `PlacementRule.Blend` through the
   existing `StationEdits.Blend`; the measure is the existing `StationEdits.Measure`; `Placement.cs` stays read-only.

### 13.2 Data rule (first)

**Authored vs derived.** M's profile ordinates are **authored** data; Match rewrites them, so it is an edit of record,
never a view. The matched profile P is a **derived value at the moment of the command** (the blend of L and R at η_M),
then stored as authored ordinates. Nothing records "this station is matched": if L or R is edited later, M keeps its
ordinates and the next Remove measures the new deviation (no hidden link, no second definition; DM7). The only stored
trace is the profile's `provenance` line (§2.2 step 6 grammar): `"matched to the blend of <L name> and <R name> at
<η text>"`.

`assume:` the profile `provenance` line can be rewritten by the same text path that writes the curves. Confirms: the
build opens `FoilSource.WriteSurfaces` / `RewriteCurves` (`FoilSource.cs` lines 317-320, Verified that they rewrite
the curves only). Breaks if false: the old provenance (for example a catalog name) would describe a profile it no longer
is; the build then adds the provenance rewrite to the same patch and re-parses (Guard `DSL-PATCH`).

**Invariants.** ST-I1..ST-I3 (§1.3) hold. Added: **SF2-I1** η_M, the assignment order and every channel are byte-equal
before and after; **SF2-I2** only M's profile changes (a profile shared with another station is first made independent
for M by `FoilSource.MakeIndependent`, Verified at line 494, so the other station's bytes are unchanged); **SF2-I3** P's
spacing (degree, knots, control x) is L's, bitwise.

**What it writes (text path, all existing functions).**
1. If M's profile is referenced by another assignment: `FoilSource.MakeIndependent(source, profile, index)` → a new
   profile name for M (its own naming loop).
2. P = `StationEdits.Blend(l, r, w_M, t(η_M), name, provenance)` with `w_M = Binary64.BlendWeight(η_M, η_L, η_R)` and
   `t(η_M) = ChannelEvaluator.At(thickness, η_M)` — the Promote path (`AuthoringSession.cs` lines 2783-2789, Verified).
   R0 (L and R one record): P's ordinates are L's.
3. `FoilSource.WriteSurfaces(source, M's name, L's knots, P.upper, ids, P.lower, ids)` (`FoilSource.cs` line 317; the
   path section edits use, `SectionEdits.cs` line 157; both Verified). Ids are M's when the point count is unchanged,
   else `cv-0..n-1`.
4. Re-parse; Guard `DSL-PATCH`; `Geometry.Assess` on the candidate with the §2.7 mapping (`STATION-BUDGET`,
   `STATION-NOT-ASSESSED`): if M was the same record as a neighbour, Match adds a differing station.

**Preconditions (refusals; nothing changes).**

| Condition | Code | Note |
|---|---|---|
| M is root or tip | `STATION-ENDPOINT` | as Remove |
| L.upper, L.lower, R.upper, R.lower fail the four-curve spacing identity, or M's point count ≠ L's | `STATION-SPACING` | R3; unreachable in an accepted source today (§2.1: blended neighbours share abscissae); after Ruling 73's fit, see `compatible-fit.md` |
| the measured change is ≤ 10⁻¹² relative | `STATION-MATCHED` (new) | M already is the blend; Remove is already exact; nothing to apply |
| P's closure (closed iff L and R are closed, §2.2 step 4) differs from M's | `STATION-CLOSURE` (new) | `assume:` rare; the first build refuses rather than rewrite the closure keyword. Breaks if false: users with an open mid-station between closed ends cannot match; then DR-DF-6 |
| a draft is open | `DSL-DRAFT-OWNED` | as Promote and Remove |

### 13.3 Regimes, and what Remove does afterwards (Verified by derivation; model-checked)

After Match, the stations are L, P, R with P the blend of L and R at η_M. Removing M then compares loft{L, P, R}
against loft{L, R} on [η_L, η_R]. That is **the same pair of lofts** Promote compares when it promotes the slice η_M
between L and R (§2.4). So the residual **is** the promotion deviation of (L, R, η_M), and §2.4 gives its regimes:

| Regime of (L, R) at η_M | Match change | Remove after Match | Note |
|---|---|---|---|
| **R0** L and R one record | M's old profile vs L's (any size) | **0** (exact) | P is L's ordinates |
| **R1** shared spacing, common peak (k = 1) | as measured | **≤ 10⁻¹²** (exact) | the brief's case |
| **R2** shared spacing, differing peaks (k < 1) | as measured | **= the R2 promotion deviation** of (L, R, η_M), > 10⁻¹²; Remove keeps its 10 µm gate | no profile does better (§2.4 uniqueness) |
| **R3** differing spacing | — | — | refused, `STATION-SPACING` |

Model check (scratch, a unit-thickness power-remap model of rule A, **Inferred** for the product): peaks 30 % / 50 % /
40 % at η 0 / 0.5 / 1 — Remove before Match 3.35 × 10⁻¹ of unit thickness, Match change 3.35 × 10⁻¹, Remove after
Match 3.23 × 10⁻⁴, and the promotion deviation of (L, R) at 0.5 is the same 3.23 × 10⁻⁴; with a common peak, Remove after
Match is 1.1 × 10⁻¹⁶. So Match turns a large refused Remove into a deliberate previewed edit plus a Remove whose change is
the (usually small, §2.5) R2 residual. The total change from the original loft to the final one is the same either way
(the final loft is loft{L, R}); Match only splits it into a step the user sees and approves.

### 13.4 Deviation definition (the same measure and grid as Promote and Remove)

Both numbers use `StationEdits.Measure` (`AuthoringSession.cs` line 2873, Verified) through `Placement.Sections`, on the
Remove grid: `Grid(grid, (η_L, η_M), (η_M, η_R))` — 129 η per interval plus the station η values — × 2001 x
(`StationGrid.Production`, line 2740, Verified); norm max(|Δupper|, |Δlower|); δ_rel per local chord, δ_m = max c(η)·δ(η);
labelled sampled with its grid, never a bound (Ruling 252).

- **Change** = Measure(accepted, candidate).
- **Residual** = Measure(candidate, RemoveAssignment(candidate, index)). Its regime label (R0 / R1 / R2) is
  `StationEdits.Regime(sameRecord(L, R), worstRelative)` — the measurement decides, never a peak prediction (Ruling 256 G1).

`assume:` the residual equals `PreviewPromote` of (L, R) at η_M bit for bit, because `Measure` takes absolute differences
(symmetric in binary64) and P is built by the same call with the same arguments. Confirms:
`MatchBlend_DifferingPeaks_ResidualEqualsPromotionDeviation`. Breaks if false: the two numbers differ in the last bits;
the test then asserts ≤ 1e-15 relative and the proof note records why.

**Cost.** Two measures per preview. Ruling 256 Q2 measured one at about 10 s on this laptop, so the preview is about
20 s (Inferred), off the UI thread with S1-COPY-20 "Checking…" and Cancel. The `Placement.Sections` speed-up is out of
scope (Ruling 256 Q2 (b)); the residual is not skipped to save time, because it is the number the user decides on.

### 13.5 Commands, undo, UI and copy (provisional SF2-COPY ids)

**Core API** (`AuthoringSession.cs`, beside Remove): `PreviewMatchBlend(int index) → MatchPreview(Index,
ChangeRelative, ChangeMeters, ChangeAtEta, ResidualRelative, ResidualMeters, ResidualAtEta, ResidualRegime,
SharedWith, TangentsDropped, RefusalCode, Reason) { Grid }` and `MatchBlend(string operationId, int index) →
MatchOutcome(AcceptedId, MatchPreview)`. Both through the `Run` wrapper (events `preview-match-blend`, `match-blend`
with regime, change and residual in the outcome; §11 telemetry).

**Undo.** One accepted revision, one undo step; Undo restores the source bytes (SF2-I1 and SF2-I2 make that a byte
check). Match then Remove are two steps; undoing both restores the original.

**Where it is offered.** (1) In the Remove flyout, when the Remove deviation is above 10⁻¹² (the button is the
flyout's secondary action; when Remove is above 10 µm it is the only enabled action besides Cancel). (2) `station.match-
blend` in the palette and the Edit menu, and a button in the station's Properties next to Remove. The flyout title is
"Match <name> to the blend"; Apply / Cancel; Return applies; Escape cancels and returns focus to the invoking control.
After Apply, M stays selected and the Remove flyout reopens if Match was invoked from it.

| Id | Exact string | Where |
|---|---|---|
| SF2-COPY-1 | Match the blend… | command, Properties button, Remove flyout button |
| SF2-COPY-2 | Replace <name>'s profile with the blend of <left> and <right> at η <η>. Largest change <µm> µm (<rel> of chord) at η <at>, sampled on 129 η × 2001 x, not a bound. | preview, change |
| SF2-COPY-3 | Then Remove <name> is exact. | preview, residual R0/R1 |
| SF2-COPY-4 | Then Remove <name> still changes the loft by <µm> µm (<rel> of chord) at η <at>: <left> and <right> have their thickest point at different chord positions. Remove's limit is 10 µm. | preview, residual R2 |
| SF2-COPY-5 | <profile> is shared with <stations>. Only <name> gets the new profile. | preview, shared profile |
| SF2-COPY-6 | <n> tangent constraints on <name> are removed. | preview, tangents dropped |
| SF2-COPY-7 | Matched <name> to the blend of <left> and <right>. Largest change <µm> µm at η <at>. Thickness: keep current. | status receipt |
| SF2-COPY-8 | <name> already matches the blend of its neighbours. Remove is exact. | disabled reason, `STATION-MATCHED` |
| SF2-COPY-9 | <name> and its neighbours do not share an open or closed trailing edge. Match the blend is not available. | disabled reason, `STATION-CLOSURE` |

Root/tip, draft and spacing reasons reuse S1-COPY-12, S1-COPY-13 and S1-COPY-9. No colour carries meaning alone; the two
numbers are text. WCAG 2.2 AA as §5.6.

**Hard states.** Checking (S1-COPY-20, Cancel enabled); refused (reason as description, Apply disabled); stale accepted
revision while the preview was open (the preview is discarded and re-run, as Promote; T10 pattern); budget refusal
(`STATION-BUDGET` text of Promote); measure failure (the session's `ContractError` reason in the flyout, nothing applied).

### 13.6 E7 surface list (SF2 build)

| # | Surface | Change | File |
|---|---|---|---|
| 1 | Source | none new: `MakeIndependent`, `WriteSurfaces`, `RemoveAssignment` (measure only) reused; the provenance line per the §13.2 `assume:` | `src/CfdWorkbench.Core/FoilSource.cs` (only if the provenance rewrite needs a line) |
| 2 | Blend + measure | `StationEdits.Match` (plan), reusing `Blend`, `Measure`, `Grid`, `Regime` | `src/CfdWorkbench.Core/AuthoringSession.cs` |
| 3 | Session | `PreviewMatchBlend`, `MatchBlend`, `MatchPreview`, `MatchOutcome` | `AuthoringSession.cs` |
| 4 | Geometry authority | none — read only | `Placement.cs`, `Geometry.cs`, `SectionEdits.cs` |
| 5 | Controller | `PreviewMatchBlend`, `MatchBlend`, enablement and reasons; the Remove flyout's secondary action | `src/CfdWorkbench.Desktop/WorkbenchController.cs` |
| 6 | Properties | the station panel button | `src/CfdWorkbench.Desktop/PropertiesView.cs` |
| 7 | Commands and menu | `station.match-blend` | `Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs` |
| 8 | Views | none new: the accepted-changed rebuild redraws Plan, Elevation, 3D, Browser and the section thumbnail (Inferred; D-M1 checks one) | read |
| 9 | CLI `inspect --json` | none: the profile ordinates already print | read |
| 10 | Save / reopen | none (a profile edit) | — |
| 11 | Copy | SF2-COPY-1..9 | `DESIGN.md` §7 (mapped at build) |

### 13.7 Red-first tests (SF2 build)

Red discipline as §7 (stubs return the base unchanged). Fixtures in `tests/fixtures/stations/` (§7): **F-THREE**
(F-CAMBER with a station at η 60 % carrying F-PEAK's tip; Remove above acceptance; L and R share a peak) and a new
**F-PEAK3**: F-PEAK (root NACA 0012 basis, tip peaking at 40 %) with a station at η 50 % carrying F-CAMBER's tip (L and R
differ in peak). Ring 0, Core.Tests, coarse grid where §7 allows, production grid in one test.

| Test | Input | Expected | Mutant it kills |
|---|---|---|---|
| `MatchBlend_CommonPeak_ThenRemoveExact` | F-THREE, station 1 | after `MatchBlend`, `PreviewRemove(1)`: DeviationRelative ≤ 1e-12, Admitted | P blended with w = η_M instead of (η_M − η_L)/(η_R − η_L); P blended from (L, M) instead of (L, R) |
| `MatchBlend_DifferingPeaks_ResidualEqualsPromotionDeviation` | F-PEAK3, station 1 | `ResidualRelative` equals `PreviewPromote` of F-PEAK at η 0.5 bit for bit; regime R2; after Match, `PreviewRemove(1)` reports the same number | a residual predicted from k (peak positions) instead of measured; residual reported as 0 ("exact") |
| `MatchBlend_Change_EqualsRemoveBefore_WhenCommonPeak` | F-THREE | Change within 1e-12 relative of the `PreviewRemove(1)` deviation before Match | change measured on one interval only |
| `MatchBlend_Grid_SameAsRemove` | F-THREE | `Grid` label and η count equal `PreviewRemove`'s | a private grid |
| `MatchBlend_ChannelsAndEtaUnchanged` | F-THREE | every channel block and the assignment line byte-equal; P's maximum thickness = t(η_M) within 1e-12 | P not rescaled by 1/k; η text rewritten |
| `MatchBlend_SharedProfile_OnlyThisStation` | F-THREE variant where root and station 1 share a profile | root's profile bytes unchanged; station 1 names a new profile; SharedWith lists root | the shared block rewritten in place (root changes) |
| `MatchBlend_Endpoint_Refused` | root, tip | `STATION-ENDPOINT`, bytes unchanged | endpoint matched to a one-sided blend |
| `MatchBlend_AlreadyMatched_Refused` | Match twice | second call `STATION-MATCHED`, no new revision | a second identical revision appended |
| `MatchBlend_Budget_Refused` | injected `assess` returning the operation bound (as Promote's tests) | `STATION-BUDGET`, bytes unchanged | budget check skipped when M was a copy of L |
| `MatchBlend_Closure_Refused` | open M between closed L and R | `STATION-CLOSURE` | closure keyword left stale (Assess fails later) |
| `MatchBlend_Undo_RestoresBytes` | Match then Undo | source bytes equal the original | two undo steps |
| `MatchBlend_EvaluatorIdUnchanged` | F-THREE | `cfdw-cv/2` before and after | evaluator id bumped |
| `MatchBlend_TangentsDropped_Counted` | M with one tangent row | `TangentsDropped` = 1 and SF2-COPY-6 | tangents silently dropped |
| Desktop `RemoveFlyout_AboveLimit_OffersMatchBlend` (`--controller-shell`) | F-THREE, Remove station 1 | Apply disabled (S1-COPY-15), Match the blend enabled and focusable | button missing when Remove is refused |
| Desktop `MatchBlend_Flyout_KeyboardPath` | open by command | focus on Apply when enabled; names announced; Escape returns focus | focus lost on Escape |

### 13.8 Owned paths (SF2 build) and seams

`src/CfdWorkbench.Core/AuthoringSession.cs` (StationEdits.Match and the public pair); `src/CfdWorkbench.Core/FoilSource.cs`
only for the provenance line if needed; `src/CfdWorkbench.Desktop/WorkbenchController.cs`, `PropertiesView.cs`,
`Shell/CommandTable.cs`, `Shell/NativeMenuBuilder.cs`; `tests/CfdWorkbench.Core.Tests/StationTests.cs`;
`tests/fixtures/stations/f-peak3.foil` (new); the Desktop `--controller-shell` harness file; `DESIGN.md` §7 rows.
**Read only:** `Placement.cs` (Ruling 252, Ruling 256: only the two predicates' modifiers ever changed), `Geometry.cs`,
`SectionEdits.cs`. Seam: SF2 also builds the `Placement.Sections` speed-up (Ruling 256 Q2 follow-up) — a separate node
with its own bit-for-bit test; Match calls `Measure` and gains from it without change.

### 13.9 Spec

CAD-02 names Remove's acceptance but no Match step, and A4.6's 1.7 note lists the ungated deliberate edits (Rebuild to
N). Match is a new deliberate edit with a shown change and no gate, so both need a row:
[`spec-1.7.9.md`](../specs/amendments/spec-1.7.9.md) (AM-1.7.9-1, AM-1.7.9-2; proposed).

### 13.10 Review packet (computational geometry)

1. Is "Remove after Match = the promotion deviation of (L, R, η_M)" right for the **binary64** path and the certificate,
   not only the real-number algebra? (The certificate renormalises P by its own maximum, `Geometry.cs` lines 388-389.)
2. Is an ungated Match (DR-DF-4) one geometry authority still? P is built by the same `Blend`; the change is measured by
   the same `Measure`; no new evaluator. Does the veto read the absence of a gate as an unreported residual? (It is
   reported, both numbers.)
3. Should Match be offered in R2 at all when the residual alone exceeds 10 µm (so Remove stays refused after it)? The
   design offers it with SF2-COPY-4; the alternative is to disable it with that number.
4. Tangent rows dropped (SF2-I3 keeps L's spacing, not M's constraints): acceptable, or refuse when M has tangents?

### 13.11 Decision requests (not raised by this note)

| Id | Question | Options | Recommendation |
|---|---|---|---|
| **DR-DF-4** | Gate on Match's own change | (a) none — a deliberate previewed edit, like Rebuild to N (AM-1.7.9-2) · (b) 10 µm (makes Match useless exactly where Remove is refused, since in R1 the change equals Remove's deviation) | (a) |
| **DR-DF-5** | Differing peaks: Remove after Match is not exact | (a) offer Match with the residual shown (SF2-COPY-4) · (b) offer Match only when the residual is ≤ 10⁻¹² · (c) change the blend rule (out of scope: geometry authority) | (a) |
| **DR-DF-6** | Closure mismatch | (a) refuse (`STATION-CLOSURE`) in the first build · (b) rewrite M's closure keyword to P's | (a) |
