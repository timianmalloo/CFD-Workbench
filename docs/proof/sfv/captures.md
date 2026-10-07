---
id: proof-sfv-captures
title: SFV captures of the Section force vectors against the approved mockup states A to E
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sfv, captures, section, mockup-state-unbuilt]
links:
  - { to: mockup-section-force-vectors, rel: refines }
  - { to: proof-sfv-model, rel: refines }
review-by: 2026-12-31
summary: >-
  Five app captures (A to E) of the Section document with the Lift and Drag vectors, one per approved mockup state, each opened and
  compared with the mockup capture, with the differences named. Numbers are read from real lattice runs, not from the mockup.
---

# SFV captures

Rendered from the app: the real `ShellHost` in a 1500 x 1320 window (`RenderTargetBitmap`), the product wing method at its default lattice
(64 x 4, cosine), Section document open, station selected. `NN-app.png` is the whole window; `NN-profile.png` is the profile control alone.
The capture harness was a scratch Desktop check; it is not in the tree. Every capture below was opened and read.

**Re-captured in repair cycle 1 (Ruling 131).** All six captures were taken again after the repairs, from the same fixtures, with a harness rebuilt
for this cycle (the first one was not kept; the rebuilt one is also scratch). The values of A to D and E equal the first captures' (L' 327.43, Cl_local 0.2014,
x_cp 0.245; B Cl_local 0.0121; E Cl_local 0.0993, x_cp 1.184, M' -18.08 N.m/m: the same 4412 construction and alpha -2.535). What changed on the pictures is
only what the repairs changed: the bias wording, the band-centre and 2D-inviscid wording, the plate layout, and the tip strip.

**Fixtures (real runs).** The wing is the Example planform (120 mm chord, 450 mm half span, twist 0 to -2 deg) with a section assignment at
245 mm (eta 0.5444), the strip the main example is (Ruling 128 (5)); the shown station is that strip. Salt water 15 degC, 5.14 m/s, depth 0.6 m.
A, B, D carry a NACA 0012 profile (unflagged); C carries the Example's own `section-a` (not the NACA 0012 family, so the polar flags it,
COPY-364); E carries a NACA 4412 built from the cambered camber line (m 0.04, p 0.4, t 0.12, fitted through `DatImport.Fit`).

## State by state (taken from the mockup's five states, not from the brief)

| Mockup state | Mockup capture | App capture | Operating point and read values (from the run) | Built? |
|---|---|---|---|---|
| A. Mid-span strip | `docs/mockups/section-force-vectors/01-mid-span-strip.png` | `captures/A-app.png`, `A-profile.png` | alpha 3.00 deg; strip eta 0.545; alpha_geo 2.37 (twist -0.63), alpha_eff 1.93, alpha_i 0.44; Cl_local 0.201; x_cp/c 0.245 (CP anchor); L' 327 N/m on 1 c = 2000 N/m; D' profile 12.8-15.0, induced 2.52 N/m at x10 | built |
| B. Near zero lift | `02-near-zero-lift.png` | `captures/B-app.png`, `B-profile.png` | alpha 0.765 deg, found so that Cl_local = 0.012; x_cp/c from the lattice would be 0.247 (on the chord) but |Cl_local| < 0.05: arrows at c/4, couple drawn (M' 0.00704 N.m/m), x_cp "Undefined · near zero lift"; scale 1 c = 500 N/m, drag x5 | built |
| C. Flagged drag | `03-flagged-drag.png` | `captures/C-app.png`, `C-profile.png` | as A on the Example's `section-a`: COPY-364 under the table, the profile row ends "· flagged"; the polar returned no low-confidence flag on this strip, so the "· low confidence" suffix is not on the capture (it is pinned by the label row, COPY-SF10) | built, low-confidence suffix not exercised by a real run |
| D. Imperial | `04-imperial.png` | `captures/D-app.png`, `D-profile.png` | as A with the Units switch on Imperial: L' 22.4 lbf/ft, scale 1 c = 100 lbf/ft (re-derived in the unit shown, so the arrows differ from A), M' 0.0450 lbf.ft/ft, V_crit 32.0 kn | built |
| E. Cambered, cruise | `05-cambered-cruise.png` | `captures/E-app.png`, `E-profile.png` | NACA 4412 at alpha -2.535 deg, found so that Cl_local = 0.10 (0.0993); panel Cm c/4 -0.105; x_cp would be 1.184: off the section, so c/4 + couple (M' -18.1 N.m/m), x_cp "Undefined · the centre of pressure is off the section" | built |
| F. Tip strip (Ruling 131, not a mockup state) | none (the mockup has no tip state) | `captures/F-tip-app.png`, `F-tip-profile.png` | NACA 0012 at alpha 3.00 deg, the tip station (eta 1, the provisional strip): no vectors, no CP anchor, no key strip; the profile carries the plate "Not judged — tip strip" (COPY-220) and the strip table is one row, "Result: Not judged — tip strip"; no L', x_cp or M' is shown | built (a repair) |

No mockup state is "not built".

## What the repairs changed on the captures (repair cycle 1, Ruling 131)

| Repair | Where to see it | Observed |
|---|---|---|
| Tip strip is Not judged | `F-tip-app.png`, `F-tip-profile.png` | the profile plate and the table row both read "Not judged — tip strip"; no arrows, no anchor, no couple |
| 4-panel bias wording | A to E, the strip table and the profile | table rows read "x_cp/c (lattice, 4 chordwise panels; biased forward at low lift)" and "M' c/4 (lattice, 4 chordwise panels; biased forward at low lift)"; on the profile the CP label (A, C, D) and the couple label (B, E) carry the same wording as a second line |
| Drag total is the band centre | A to E, the profile and the table | "D' profile + induced (band centre), free-stream axes ..." (A: 16.4 N/m = 13.9 band centre + 2.52 induced) |
| Cm c/4 is 2D inviscid | the strip table | "Cm c/4 (panel, 2D inviscid)" |
| No two plates overlap | E, `E-profile.png` | the "local inflow alpha_eff" plate no longer sits on the leading-edge Cp_min ring and its plate; it stands below them. The plate rectangles of A to E are asserted not to intersect (`SectionForceVectors_Plates_DoNotOverlap_StatesAToE`, two widths, Cp_min on either surface) |

Noted, not changed (Not in scope): the conditions band reads "σ Unavailable — depth not set" while the profile's cavitation line reads "σ 7.80" (the Coordinator tracks it); and the left Properties pane's "Lift / span" row stays in N/m under Imperial (D), a wing-level row that predates this track.

## Differences, named

| Mockup shows | Built shows | Why |
|---|---|---|
| V-inf drawn falling to the right at alpha_geo (below the chord line), lift leaning aft | V-inf rises to the right at alpha_geo; lift leans forward of the chord normal (angle asserted on the drawn pixels by `SectionForceVectors_Drawn_...`) | A nose-up section meets the flow from below, so V-inf climbs relative to the chord. The mockup's script draws `d = (cos a, sin a)` in screen coordinates (y down), a mirror image. **Needs the operator's eye: the approved picture is mirrored.** |
| 1 N.m/m = 0.737562 lbf.ft/ft (page script `NMF`) | 1 N.m/m = 0.224809 lbf.ft/ft | A moment per span is a force: N.m/m = N, so lbf.ft/ft = lbf. The page factor is the per-metre-not-converted one. Pinned by `SectionForce_Imperial_ForceAndMomentPerSpan`. |
| One profile height 420 px, key outside the viewport | the profile control is 420 px plus a 44 px key strip on the page surface; the Cp-only view keeps 240 px | The key sits on the page surface under the viewport as in the mockup. |
| The axis plate under the chord | with vectors, the axis plate stands bottom left above the cavitation line (as the mockup does) | The plates under the chord carry the anchor and drag labels. |
| M' label right of the anchor above the chord | M' label (two lines since Ruling 131) on the side of the chord away from the Cp_min plate (below for an upper Cp_min, above for a lower one); every label is moved to free room when its first place meets another plate or the Cp_min ring | The mockup's Cp_min plate is right-aligned left of the marker; the built plate starts at the marker and would sit on it. |
| Illustrative numbers (L' 479 N/m, x_cp 0.27, ...) | real values from the lattice run (table above) | The mockup says so on its face. |
| Table values with units in the value column | the same rows, value and unit in the app's two columns; notes right of the table (COPY-328, COPY-364, COPY-SF15) | The app's `ResultRow` shape. |
| (not in the mockup) | the left Properties pane still reads "Lift / span 327.433 N/m" under Imperial | Existing Properties row, not in this track's scope (a separate wing-level surface). |

## Reading the numbers (cross-checks, Inferred where a second source is not in the run)

- A: L' 327 N/m = q c Cl_local = 13553 Pa x 0.12 m x 0.2014 (the Properties row "Lift / span 327.433 N/m" is the same number): one model.
- E: the lattice strip's own M' c/4 = -18.08 N.m/m gives Cm c/4 = -18.08 / (13553 x 0.12^2) = -0.0926, against the panel method's -0.105 for the
  same section. The gap (the lattice is 12 % low in magnitude) is **chordwise discretisation of the 4-panel strip**, not 3D effects and not
  thickness: `nc-convergence.md` measured the 4-panel strip 13.5 % low in Cm c/4 at Cl 0.10 on a cambered section (a NACA 2412, not this 4412, so
  the match in size is Inferred, not a proof for this section), with the order near 1 in nc. x_cp = 0.25 + 0.0926 / 0.0993 = 1.18,
  the table's 1.184: the moment transfer from the frame origin to the strip leading edge is consistent with a separately derived Cm c/4.
