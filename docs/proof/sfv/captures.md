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

No mockup state is "not built".

## Differences, named

| Mockup shows | Built shows | Why |
|---|---|---|
| V-inf drawn falling to the right at alpha_geo (below the chord line), lift leaning aft | V-inf rises to the right at alpha_geo; lift leans forward of the chord normal (angle asserted on the drawn pixels by `SectionForceVectors_Drawn_...`) | A nose-up section meets the flow from below, so V-inf climbs relative to the chord. The mockup's script draws `d = (cos a, sin a)` in screen coordinates (y down), a mirror image. **Needs the operator's eye: the approved picture is mirrored.** |
| 1 N.m/m = 0.737562 lbf.ft/ft (page script `NMF`) | 1 N.m/m = 0.224809 lbf.ft/ft | A moment per span is a force: N.m/m = N, so lbf.ft/ft = lbf. The page factor is the per-metre-not-converted one. Pinned by `SectionForce_Imperial_ForceAndMomentPerSpan`. |
| One profile height 420 px, key outside the viewport | the profile control is 420 px plus a 44 px key strip on the page surface; the Cp-only view keeps 240 px | The key sits on the page surface under the viewport as in the mockup. |
| The axis plate under the chord | with vectors, the axis plate stands bottom left above the cavitation line (as the mockup does) | The plates under the chord carry the anchor and drag labels. |
| M' label right of the anchor above the chord | M' label on the side of the chord away from the Cp_min plate (below for an upper Cp_min, above for a lower one) | The mockup's Cp_min plate is right-aligned left of the marker; the built plate starts at the marker and would sit on it. |
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
