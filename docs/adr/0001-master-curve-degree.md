---
id: adr-0001-master-curve-degree
title: "ADR-0001: master curves are degree-3 B-splines with seven vertices; the degree is a record field"
type: adr
status: accepted
owner: "@timianmalloo"
phase: specification 1.3
tags: [geometry, b-spline, degree, control-vertex, adr, dr-10, amended, ruling-62, floor-4]
links:
  - { to: spec-cfd-workbench-v1, rel: refines }
  - { to: control-vertex-workspace, rel: relates-to }
  - { to: kernel-spike-occt-loft, rel: relates-to }
  - { to: kb-hydrofoil-workbench, rel: relates-to }
  - { to: design-m12b-points, rel: relates-to }
  - { to: rulings, rel: depends-on }
  - { to: design-planform-point-verbs, rel: relates-to }
  - { to: proof-planform-verbs-fairness, rel: relates-to }
review-by: "none while accepted"
summary: >-
  Re-decides the knowledge base's degree-5 reading for the five master (distribution) curves: the record's default
  is a degree-3 clamped B-spline with seven control vertices (six to ten), the degree is stored per curve, and
  section curves stay degree 5. Decided on a measured fixture (fairness, anchor residual, support, lever effect)
  over the five example curves at both degrees, and on the loft spike showing the surface's spanwise continuity is
  the kernel's, measured, not the master curve's.
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-09-22, reason: "Revision 1.5 adds explicit section scope, draft-safe inspection, design alternatives and geometry intent; reconciles full thickness, equal-x Rule A and native versus shape opening. Review affected neighbors." }
---

# ADR-0001: master curves are degree-3 B-splines with seven vertices; the degree is a record field

- **Status:** Accepted · **amended 2026-09-30** (DR-10, Ruling 53) — see *Amendment 1* · **amended 2026-10-03** (Ruling 62) — see *Amendment 2* at the end
- **Date:** 2026-09-21
- **Deciders:** the operator (product), Computational Geometry lens (record), Marine CAD UX lens (editing feel)
- **Context spec/architecture:** `docs/specs/cfd-workbench-v1.md` A4.1, A4.2, A4.3; `docs/knowledge/hydrofoil-workbench/data-and-constants.md` (Alias CV count; Rhino Fair "best on degree 3"); KB index item 7 (degree-5 record *for profiles*)

## Context

Specification 1.2 carried "degree 5" for every curve because the knowledge base argued C⁴ continuity inside one curve and a degree-5 record for *profiles*. Revision 1.3 makes the control vertices the record and the polygon the editing surface (Fusion control-point spline, Rhino control points); the operator asked for few, meaningful handles and levers. Alias practice is "few CVs, degree 3 for shaping"; Rhino Fair is documented as best on degree 3. The master curves are one-dimensional graphs v(η) whose job is a fair distribution with a handful of vertices; the *surface's* spanwise continuity comes from the loft (the kernel's v-degree, measured — see `kernel-spike-occt-loft`), not from the master curve's degree.

## Decision

We will store the degree per distribution curve and default the five master curves to **degree 3 with seven control vertices** (six to ten allowed, the record's floor and ceiling of A4.1); section curves remain **degree 5**. Continuity is measured per A4.3 whatever the degree.

## Alternatives considered

- **Degree 5 with seven vertices** — C⁴ inside the curve, but on the fixture it is *less* fair than degree 3 at the same vertex count on every curve, and with seven vertices its interior vertices are global (support 98–99 % of the span), so a vertex is a whole-curve lever. Lost on fairness and on the editing model the operator asked for.
- **Degree 5 with nine vertices** — fairer than degree 5 with seven on some curves and worse on others (chord 1.784 vs 0.282; LE 0.866 vs 1.196), still near-global support (94 %); more handles than the brief wants. Lost.
- **Degree 3 with nine vertices** — the fairest on four of five curves and the first count at which the middle vertex is local (65 % of span), but two more handles per curve than the seven the brief measured as right. Kept as the *reach* of the record (six to ten): Insert CV takes a curve there when the shape needs it.

## Evidence (the fixture: `spikes/degree-adr/fixture.json`, run 2026-09-21 on the mockup's evaluator)

Same six anchors fitted per curve with the root-tangent row; κ′ energy = ∫(dκ/dη)² over η (lower is fairer); support = the fraction of η an interior vertex moves (threshold 10⁻⁹); lever = curve change per unit lever move.

| Curve | d3 · 7 CV | d5 · 7 CV | d5 · 9 CV | d3 · 9 CV |
|---|---|---|---|---|
| LE rail — κ′ energy · pieces · support | 0.918 · 1 · 0.98 | 1.196 · 2 · 0.98 | 0.866 · 3 · 0.945 | 0.514 · 3 · 0.652 |
| TE rail (chord) | 0.251 · 1 · 0.98 | 0.282 · 1 · 0.98 | 1.784 · 3 · 0.945 | 0.558 · 2 · 0.652 |
| Dihedral | 0.117 · 2 · 0.98 | 0.152 · 2 · 0.98 | 0.199 · 1 · 0.945 | 0.080 · 2 · 0.652 |
| Twist | 26 790 · 1 · 0.99 | 51 910 · 2 · 0.99 | 5 609 · 2 · 0.99 | 2 050 · 2 · 0.662 |
| Thickness t/c | 0.225 · 2 · 0.99 | 0.288 · 2 · 0.99 | 0.420 · 2 · 0.99 | 0.126 · 2 · 0.662 |
| Anchor residual (all) | 0 | 0 | 0 | 0 |
| Lever effect (all) | 0.594 | 0.551 | 0.551 | 0.598 |
| Continuity inside the curve | C² | C⁴ | C⁴ | C² |

Read: at seven vertices, degree 3 is fairer than degree 5 on all five curves (a degree-5 curve with seven vertices has two interior spans and wiggles to interpolate); the lever effect is the same to 8 %; **local support at seven vertices is global for either degree** (the GEO-13 local-support test therefore uses an off-centre vertex, which is local — vertex 6 of 7), and locality arrives at nine vertices for degree 3 only. C² inside a master curve is enough because the surface's continuity across η is measured on the loft (A4.3), and the loft spike shows the kernel choosing its own v-degree (3–5) regardless of the master curve.

## Consequences

- A4.1 stores the degree per curve; A4.2's Insert CV and Rebuild move a curve between six and ten vertices; Fair and Fit points work at the curve's own count.
- The 1.2 sentence "degree 5 with simple interior knots is C⁴" becomes "degree p … C^(p−1)" (done in 1.3).
- Sections keep degree 5 (KB item 7 stands for profiles: the LE turn needs it, as the section-fit fixture in the v5 review shows — 7.5 µm with twelve vertices).
- The knowledge base's data-and-constants row on continuity is annotated to cite this ADR (the table stays true for any p).
- If a fairing study later shows degree-3 master curves cannot hold a required G3 at the root mirror, this ADR is superseded, not edited.

## Amendment 1 — channel vertex ceiling 16 under FoilDSL 4.1 (DR-10, 2026-09-30)

- **Ruling:** Ruling 53, DR-10 — "raise the channel vertex ceiling to 16. ADR-0001 is amended and the FoilDSL range
  relaxed expand-only, in the M1.2b design-slice." The amendment is written by `docs/design/m12b-points.md` §3.8.
- **Decision (amended):** a master (channel) curve has **6 to 16** control vertices when its document declares
  `foildsl "4.1"`; under `"4.0"` the range stays 6 to 10. The degree (3), the default count (seven for a hand-authored
  curve; ten for New foil, `FoilSource.cs`:292), section curves (degree 5, 6–32) and every other statement above are
  unchanged. The original Decision text is kept as the record of what was decided on 2026-09-21.
- **Why (measured on the record, not assumed):** an interior Anchor point is a knot of multiplicity p = 3
  (ADR-0005 §2); making a Control point an Anchor inserts that knot to multiplicity 3 by Boehm and adds **one to three**
  vertices (fewer when u\* lands on an existing knot, which the parser allows up to multiplicity 3,
  `FoilSource.cs`:1124). New foil ships at 10 vertices per rail, so under 6–10 it can hold **no** interior anchor. At 16
  it holds two (10 → 13 → 16); the seven-vertex Example holds three (7 → 10 → 13 → 16).
- **Why the range is gated on 4.1:** a document that needs more than 10 channel vertices is a newer document, and the
  one version gate (`"4.1"`, shared with the `tangents` block) says so. The patch that first takes a channel above 10
  writes the `"4.1"` header in the same transaction (`EnsureHeader41`, the single writer; it never lowers the header).
  Expand-only: nothing that parsed before stops parsing, and the definition hash of unchanged geometry does not move when
  the header is rewritten. **What an old build reports (corrected at the gate):** builds up to M1.2a check the version
  only after the whole grammar pass (`FoilSource.cs`:130-131, :1162), so a 4.1 file with 11–16 points and no row gives
  `DSL-VERSION`, but a 4.1 file with a `tangents` row gives `DSL-SYNTAX`, and a project with a point-type receipt gives
  `DOC-UNSUPPORTED-FIELD`; the file is unchanged in every case. M1.2b moves the version check to right after the version
  is read, so later versions are refused as newer (`docs/design/m12b-points.md` §3.8).
- **Alternatives considered:** *13* (one anchor on New foil) — too tight for a root-to-tip pair of anchors on the
  shipped default; *32* (the section ceiling) — a distribution curve with 32 vertices is no longer a few meaningful
  handles (this ADR's premise) and invites wiggle; the fixture above measured fairness only to 10; *unbounded* — no
  resource bound on certification. *Keep 10* — every New foil would refuse Control → Anchor forever.
- **Consequences:** Insert CV and Rebuild may take a channel to 16 under 4.1; fairness above 10 vertices is measured
  (comb, A4.3), never asserted from this ADR's fixture, which covered 6–10 only — a 16-vertex comb fairness fixture is
  added; certification cost grows with span count and is bounded by the 1 s proof budget, checked as a deterministic
  work count in the fast test ring and as wall time on the worst case (three anchors at 16 vertices) at readiness, on
  macOS now and on Windows at its qualification; a project with a 4.1 revision cannot be opened by a 4.0-only build (the
  file is unchanged). `foildsl.md` §5 item 3 changes in the same change as the parser (M1.2b track B0), never before.
- **Council verdict (M1.2b gate, 2026-09-30):** Computational Geometry — **accept with conditions** (worst-case
  budget run at 16 vertices with three anchors; header-rewrite hash test; "one to three" insertions; 16-vertex comb
  fairness fixture) — all four written into this amendment and the design's test ledger. Data & Persistence — **accept
  with conditions** (correct the old-build refusal claim; single header writer that never lowers) — both written above.
  No veto. Record: `docs/design/m12b-points.md`, *Gate record*.

## Amendment 2 — channel vertex floor 4 under FoilDSL 4.1 (Ruling 62, 2026-10-03)

- **Status:** accepted by Ruling 62 (operator, 2026-10-03). The text is written by the design-slice
  `docs/design/planform-point-verbs.md` §3.6. The build starts after M1.2c joins.
- **Ruling:** "a planform master curve (rail) may have 4 to 10 control vertices at degree 3 (ADR-0001 Amendment 2 lowers
  the floor from 6); an interior anchor needs room and is refused with the reason when it does not fit." The operator's
  words: "there are too many points on the outlines for some of the foil shapes I would be building (where 3–4 points
  are sufficient)".
- **Decision (amended):** a master (channel) curve has **4 to 16** control vertices when its document declares
  `foildsl "4.1"`; under `"4.0"` the range stays 6 to 10. The floor applies to all five channels, not only the two rails,
  because the parser's count rule is per curve kind (`FoilSource.cs`:1279-1280), not per curve name (the design's
  DR-PV-4 asks whether the *verbs* reach all five). The degree (3), the default count for a hand-authored curve (seven), the ceiling of Amendment 1 and section
  curves (degree 5, 6–32) are unchanged. **New foil's default changes (Ruling 64):** 4 control vertices on the
  leading-edge and trailing-edge rails (was ten, `FoilSource.cs`:403), so New foil writes `foildsl "4.1"`; the other
  three channels keep their default unless the SPK evidence gives a reason. Ruling 62's "4 to 10" is the range of
  **Rebuild to N**; Add point reaches 16 under 4.1 (Ruling 64, DR-PV-1 confirmed).
- **Why 4 is the floor (computed from the record, not assumed):** a clamped degree-3 B-spline needs p + 1 = 4 vertices.
  With 4 it is one cubic Bézier: knots `[0,0,0,0,1,1,1,1]`, no interior knot. The roles derive as root end, root handle,
  tip handle and tip end (`PointModel.cs` `Role`, indices 0, 1, n−2, n−1), so a 4-vertex curve has **no Control point**
  and therefore no point that can become an interior anchor. An interior anchor is an interior knot of multiplicity 3
  (ADR-0005 §2), so a curve with k interior anchors has at least **4 + 3k** vertices: 7 for one anchor, 10 for two,
  13 for three, 16 for four. Control → Anchor on a 5-vertex curve (one Control point, one simple interior knot) adds
  two or three vertices (two when u\* lands on the existing knot) and gives 7 or 8, which fits. Anchor → Control removes
  two vertices, and a curve with an anchor has at least 7, so it never goes below 5; `MakeControl`'s guard "at least 8"
  (`AuthoringSession.cs`:1190) becomes "at least 7", which always holds for a curve that has an anchor (Test Architect
  finding at the gate: a 6-vertex curve cannot hold an anchor).
- **Why the floor is gated on 4.1:** the same reason as Amendment 1. A document that needs fewer than 6 channel
  vertices is a newer document. The patch that first takes a channel below 6 writes the `"4.1"` header in the same
  transaction through the one writer, `EnsureHeader41` (it never lowers the header). Expand-only for the parser:
  everything that parsed before still parses.
- **What an old build reports (to be observed, not asserted):** a build before M1.2b refuses a 4.1 file with
  `DSL-VERSION`. An M1.2b-era build accepts 4.1 but enforces the floor 6, so it refuses a 4-vertex channel with
  `DSL-CURVE` (`FoilSource.cs`:1280); a project whose history holds one of the new receipt kinds is refused with
  `DOC-REFERENCE` (`AuthoringSession.cs`:1805-1822). The file is unchanged in every case. The design's old-build
  characterization receipt (`docs/proof/planform-verbs-old-build/`) records the actual codes; DR-PV-2 offers a
  FoilDSL "4.2" if the operator wants every old build to say "newer version" instead.
- **What changes, in one change each (never before the parser):**
  - **Record:** nothing new. The count is already the length of `points`; the knot vector is already n + p + 1.
  - **Parser:** the channel floor becomes `version == "4.1" ? 4 : 6` (`FoilSource.cs`:1280). Profiles keep 6.
  - **Validation (`foildsl.md` §5 item 3 and conformance):** "Channels have p=3 and N in [6,10] in 4.0 and N in [4,16]
    in 4.1". New conformance cases: a 4.1 channel with 4 vertices parses; with 3 it is `DSL-CURVE`; a 4.0 channel with 5
    is `DSL-CURVE`; a `tangents` row on a 4-vertex channel is `DSL-LOCK` (there is no interior anchor to name).
  - **Header writer:** `EvaluatePointCommand`'s condition `Points.Length > 10` becomes `> 10 || < 6`
    (`AuthoringSession.cs`:1116-1117).
  - **Verbs:** the point verbs use two constants, floor 4 and ceiling 16, whatever the document's version. Crossing 6 or
    10 raises the header to 4.1 in the same patch, so these are the real limits (Simplifier finding at the gate). The
    as-built `CurveView.Ceiling` by version (`PointModel.cs`:35, :277) stays the parser's view; the verbs do not read it.
  - **Display sampling (a finding, F-1):** `Planform.Project` samples 8 points per non-empty knot span starting half a
    step in (`PointModel.cs`:132-143). A 4-vertex curve has one span, so its outline would be an 8-segment polyline
    that misses the root and the tip by 1/16 of the span. The design makes the samples include both ends and sets a
    per-curve minimum; this is a display change, not a record change.
  - **Export:** none. Every writer reads the evaluated surface (the loft), and a 4-vertex rail is an ordinary clamped
    B-spline to the kernel. The loft's measured v-degree is read back as before (A4.1 *loft*).
  - **Fairness evidence:** see the next bullet.
- **Fairness evidence needed for 4 and 5 (named here; nothing is run in this amendment):** the fixture above covered 6–10
  only, and `spikes/degree-adr/fixture.json` was never committed (no git history for the path; only this ADR and the
  audit log name it). The build's first track (SPK in the design) commits a fixture and measures, for the five Example
  curves and the two New foil rails at d3 · 4 CV and d3 · 5 CV:
  1. κ′ energy and the monotone-piece count, beside the existing d3 · 7 column, recomputed by the committed script;
  2. the **anchor residual** — with 4 or 5 vertices a curve cannot interpolate six anchors, so this column stops being
     0; it is the least-squares residual in the curve's own unit (mm, °, chord fraction);
  3. the **Rebuild deviation** from the shipped curves (Example 7 → 4 and 5; New foil 10 → 4 and 5) on the A4.5
     distribution-curve oracle (201 uniform η plus every knot of both curves), with the η of the largest change;
  4. support (expected global — 100 % of the span — at 4 and 5; recorded, not assumed) and the lever effect;
  5. the comb's sign-change count, so the A4.3 claim "C² inside the curve, no breaks" is measured at 4 and 5.
  The mockup's in-page computation of item 3 for the New foil trailing edge is a preview of that run, not the evidence.
- **Fairness evidence for 4 and 5 (measured by track SPK, 2026-10-03):** `docs/proof/planform-verbs-fairness/` (the
  committed probe, its fixture and its output). The probe calls the as-built Core: `SplineBasis`,
  `ChannelEvaluator`, `ConstrainedFit`, `FoilSource`'s own New foil construction and `WingEstimates`' area integral.
  A re-run reproduces `output/` byte for byte. The Example curves are the six anchors of the mockup's *Example · race
  light*, fitted with the root-tangent row. The definitions are in the proof README and replace the unrecorded
  2026-09-21 ones. Cell: κ′ energy · monotone pieces · comb sign changes · anchor residual (curve unit; New foil:
  mm to the analytic target).

  | Curve (unit) | d3 · 4 | d3 · 5 | d3 · 7 (recomputed) | Rebuild 7 → 4 · 7 → 5 (max Δ @ η) |
  |---|---|---|---|---|
  | LE rail (m) | 0.00238 · 1 · 0 · 8.4e-4 | 0.0766 · 2 · 0 · 4.4e-4 | 0.962 · 4 · 0 · 0 | 8.7e-4 @ 0.910 · 5.2e-4 @ 0.925 |
  | TE rail, chord (m) | 0.0121 · 1 · 0 · 1.0e-3 | 0.145 · 2 · 0 · 2.3e-4 | 0.253 · 2 · 0 · 0 | 1.5e-3 @ 0.860 · 3.4e-4 @ 0.235 |
  | Dihedral (m) | 0.00193 · 1 · 0 · 3.4e-4 | 0.00781 · 2 · 0 · 2.3e-4 | 0.123 · 4 · 0 · 0 | 6.2e-4 @ 0.270 · 3.5e-4 @ 0.235 |
  | Twist (°) | 16.6 · 1 · 0 · 0.042 | 17.7 · 2 · 1 · 0.025 | 35 280 · 4 · 4 · 0 | 0.104 @ 0.915 · 0.080 @ 0.920 |
  | Thickness t/c | 0.00444 · 1 · 1 · 4.5e-4 | 0.0243 · 2 · 1 · 3.1e-4 | 0.235 · 4 · 4 · 0 | 8.2e-4 @ 0.270 · 4.7e-4 @ 0.235 |
  | New foil LE (m; last column d3 · 10) | 0.0388 · 1 · 1 · 2.99 | 38.4 · 1 · 0 · 0.48 | 4.7e5 · 1 · 0 · 0.017 | 10 → 4: 2.99 mm @ 467.5 mm · 10 → 5: 0.45 mm @ 495.0 mm |
  | New foil TE (m; last column d3 · 10) | 0.298 · 1 · 1 · 8.98 | 193 · 2 · 0 · 1.43 | 7.2e5 · 3 · 0 · 0.050 | 10 → 4: 8.97 mm @ 467.5 mm · 10 → 5: 1.34 mm @ 495.0 mm |
  | Support · lever (middle vertex) | 0.999 · 0.444 | 0.999 · 0.500 (New foil 0.463) | 0.999 · 0.667 (New foil d3 · 10: 0.361 · 0.668) | — |
  | Curvature breaks inside the curve (A4.3) | 0 | 0 | 0 | — |

  Read: at 4 and 5 vertices every curve is C² with no measured break, and support is global (0.999: zero only at
  the ends), as expected. κ′ energy falls as vertices are removed, but the six anchors are no longer interpolated
  (4 vertices: LE 0.84 mm, chord 1.0 mm, twist 0.042°). **New foil at 4 (Ruling 64)**, built by `NewDefault`'s own
  construction, moves LE 3.05 mm and TE 8.83 mm at 467.5 mm from today's 10-point rails, turns the tip LE −23.45°
  and TE +34.97°, and holds the area at 1000.0 cm². The design's Rebuild 10 → 4 reproduces the mockup's TE 8.97 mm
  and LE 2.99 mm (area 997.7 cm²). Each 4-point rail has one comb sign change 24 mm from the root, from the root
  square with one free vertex; its overshoot is 0.013 mm (LE) and 0.040 mm (TE). The other three New foil channels
  are constants, exact at any count (Rebuild 10 → 4 max Δ 0), so the numbers give no reason to change their default.
  The recomputed d3 · 7 column differs from the 2026-09-21 one (κ′ energy within 1–5 % on four curves, +32 % on
  twist; pieces, support and lever differ). The original definitions were not recorded; this script is now the
  reference.
- **Alternatives considered:** *floor 5* — keeps one Control point on every curve, but the operator named 3–4 points and
  a cubic with 4 vertices is the smallest clamped curve the record can hold; *floor 2 or 3 at a lower degree* — breaks
  ADR-0001's one degree for channels and the parser's degree rule; *floor 4 for rails only* — a second count rule per
  curve name in the parser for no geometric reason; *ungated (4.0 too)* — gives "4.0" two meanings across builds
  (DR-PV-2 records it).
- **Consequences:** a 4- or 5-vertex channel is legal under 4.1. Remove point and Anchor → Control refuse at the floor,
  naming it. Local support is global at 4 and 5 vertices (every vertex moves the whole curve); GEO-13's local-support
  test keeps its 7-vertex fixture. GEO-05's "Delete leaving fewer than p + 2 vertices is blocked" and A4.2's "the floor
  is the record's six vertices" need the spec owner's amendment to "fewer than p + 1 (the record's floor, A4.1)"
  (design §13, F-4). A4.1's "six to ten allowed" becomes "four to ten under 4.0's ceiling, sixteen under 4.1". A project
  with a sub-6 channel cannot be opened by a build older than this change; the file is unchanged.
