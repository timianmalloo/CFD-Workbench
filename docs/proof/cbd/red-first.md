---
id: proof-cbd-red-first
title: "Rail comb build (track CBD): red-first record"
type: doc
status: in-review
owner: "@timianmalloo"
phase: implementation
tags: [proof, rail-comb, red-first]
links:
  - { to: design-rail-comb, rel: documents }
review-by: 2027-04-01
summary: >-
  Each new rail-comb test, run red against a stub (or the old code) and then green against the build. Raw logs sit beside
  this file. Ring: every test here is fast-ring (pure Core or headless Avalonia).
---

# Rail comb build: red-first record

Command for every inner-loop run: `CFD_TEST_ONLY=<prefixes> tools/run-suite.sh dotnet <test dll>`.

## Phase 1, Core (`tests/CfdWorkbench.Core.Tests/RailCurvatureTests.cs`)

- **Red:** [`red-phase1-run1.txt`](red-phase1-run1.txt) ran the tests against `PlanformCurvature.cs` with every body throwing
  `NotImplementedException` and `SplineBasis.Jet.D3` all zero. Result: `RESULT failures=24`, `ran=24`.
  `SplineBasis_D3_...` failed on the zero D3 (expected -222.2, got 0), `..._TipRoundPlusWobble_Three` failed on my own
  candidate count (the red run also caught a wrong expectation: 4 candidates, not 5, which was corrected in the test).
- **Repair 1 (of 2 allowed):** [`green-phase1-run1.txt`](green-phase1-run1.txt): 20 PASS, 4 FAIL, each a defect in the build
  or in the test, none a weakened assertion:
  1. `ArcLength_GaussLegendre`: one 16-point rule per span was 5.9e-9 off the closed form for the parabola; the length is now the
     sum of 8 sub-intervals per span (the same table the even-arc teeth use). Now within 1e-12.
  2. `InteriorPeak_Two`: the test expected eta 0.5; the vertex of y = x^2 on [-1, 1] is at x = 0, so eta 0. Test corrected.
  3. `IndependentOfDensity`: the sample positions i/16 hit the spike at 0.5 exactly; the comb's teeth sit at (i + 0.5)/16.
     Test corrected to sample where a 16-tooth comb samples.
  4. `ReversalAtSimpleKnot_Counted`: the polygon I chose really has 4 curvature pieces (extrema at t = 0.2867 and 0.7133 as
     well as the knot). Replaced by a polygon whose only extremum is the knot (boundary T = 0.5, kappa -5.33).
- **Green:** [`green-phase1-run2.txt`](green-phase1-run2.txt): `RESULT failures=0`, 24 PASS.
- **Ring:** `tools/run-tests.sh` exit 0, wall 49 s of the 60 s budget, no failure line. One earlier ring run failed
  `PlacementRule_RadiansConstant_SingleSiteInSource` (my `180 / Math.PI`); fixed by using `PlacementRule.RadiansPerDegree`.

| Test | Red | Green |
|---|---|---|
| `SplineBasis_D3_MatchesBernsteinThirdDerivative_AndFiniteDifferenceOfD2` | FAIL (D3 zero) | PASS |
| `Planform_CurvatureAt_Circle_RadiusWithin1e-9` (osculating circle of y = x^2 at the vertex) | FAIL | PASS |
| `Planform_CurvatureAt_Sign_LeadingPositiveTrailingNegative` | FAIL | PASS |
| `Planform_CurvatureAt_StraightRun_ReportsStraight` | FAIL | PASS |
| `Planform_CurvatureAt_StraightTolerance_TenMicrometreSagitta` | FAIL | PASS |
| `Planform_CurvatureAt_DegenerateTangent_Throws` | FAIL | PASS |
| `Planform_CurvatureAt_AnchorOneSided_BothSides` | FAIL | PASS |
| `Planform_CurvatureAt_CornerClassifiedByAngle_0p1Degrees` | FAIL | PASS |
| `Planform_CurvatureAt_GrevilleAtT_EqualsXi` | FAIL | PASS |
| `Planform_CurvatureAt_ArcLength_GaussLegendre` | FAIL | PASS (after repair 1) |
| `Planform_Teeth_PitchConstantWithin5Percent` | FAIL | PASS |
| `Planform_Teeth_PointAwayFromCentre_AndSplitPiecesAtAJump` | FAIL | PASS |
| `Planform_ReversalIndices_HysteresisZigzag` | FAIL | PASS |
| `Planform_MonotonePieces_StraightRun_One` (F1) | FAIL | PASS |
| `Planform_MonotonePieces_MonotoneTip_One` (F2) | FAIL | PASS |
| `Planform_MonotonePieces_InteriorPeak_Two` (F3) | FAIL | PASS (after repair 1) |
| `Planform_MonotonePieces_TipRoundPlusWobble_Three` (F4) | FAIL | PASS |
| `Planform_MonotonePieces_ThresholdEdges` (F5) | FAIL | PASS |
| `Planform_MonotonePieces_ScaleInvariant` (F6) | FAIL | PASS |
| `Planform_MonotonePieces_IndependentOfDensity` (F7) | FAIL | PASS (after repair 1) |
| `Planform_MonotonePieces_AnchorJump_AgainstTrend_Splits` (F8) | FAIL | PASS |
| `Planform_MonotonePieces_AnchorJump_WithTrend_DoesNotSplit` (F8) | FAIL | PASS |
| `Planform_MonotonePieces_ReversalAtSimpleKnot_Counted` (F9) | FAIL | PASS (after repair 1) |
| `Planform_MonotonePieces_MirrorInvariant` (F10) | FAIL | PASS |

Notes on fixtures: F3 to F5 are checked at layer 1 (the zigzag over a candidate list from the analytic profile, tau near 0 and
tau) and, for F3, also on the exact cubic. F4's two extrema read 0.3298 and 0.2601, as the design states. F6 and F10 run on the
example foil's two rails. F7 is a layer-1 check: a 16-tooth sample of the narrow-spike profile reads 1 piece, the extrema list
reads 3.

Refinement after Phase 1 (same class, own record): `Planform_Teeth_NoJump_NoExtraTeeth_AtARepeatedKnot` was red with the filter off
([`red-teeth-nojump.txt`](red-teeth-nojump.txt): expected 16 teeth, got 18) and green with it on
([`green-teeth-nojump.txt`](green-teeth-nojump.txt)). It makes the one-sided pair appear only at a corner or a curvature jump.

## Phase 2, Desktop (`tests/CfdWorkbench.Desktop.Tests/PlanCombTests.cs`, in the `--plan-canvas` harness)

A Desktop check needs the surface it checks, so these were written with the build and not run against a stub. Red is shown two
ways. Two checks that name the contract's hardest rules were run against a **mutant** of the build, then against the build:
[`red-desktop-mutants.txt`](red-desktop-mutants.txt) and [`green-desktop-mutants.txt`](green-desktop-mutants.txt).

| Check | Mutant (one line changed) | Red | Green |
|---|---|---|---|
| `PlanComb_StepperAtLimit_KeepsFocus` (red-first in the design) | the stepper is natively disabled at its limit (`button.IsEnabled = available`) | FAIL "Larger teeth takes no focus" | PASS |
| `PlanComb_AutoHeldDuringDrag_RefitOnRelease` (Ruling 194) | the hold is removed (`RefreshComb` refits during a gesture) | FAIL "Auto refit while the drag was still going" | PASS |

The other Desktop checks were first run against the finished surfaces. Failures met on the way, each a real defect or a wrong
expectation, none fixed by weakening an assertion:

- The example foil has straight rails, so every tooth was a dot; the fixtures now use a swept leading edge and a tapering trailing
  edge (`CombFoil`). The first fixture failed the certificate ("Rail hulls do not certify strictly positive chord"); the rails were
  re-drawn to keep the hulls apart.
- `PlanComb_PlateDoesNotCoverRail_AtTipFit` failed on the example (the plate covered the leading edge). Cause: `DesiredSize`
  included the margin the last placement had set, so the plate's own size grew each time. Repair 1: `CombPlate.PreferredSize`
  measures the plate's content, and the place is worked out again after layout (`LayoutUpdated`); the margin is written only when
  the place changes. Green.
- `PlanComb_RefitFlash_OnceAndStaticUnderReducedMotion` first fed a synthetic planform to `RefreshComb`; the canvas answered the
  announcement by refitting to the real planform, a second refit. Repair 1: the check opens real foils (a quarter of the bend, a
  12 % refit, back) through the controller. Green.
- `PlanComb_StepperAtLimit_KeepsFocus` expected "Largest teeth reached." as the stepper's help text; the help text is
  "Largest teeth. Smaller teeth is available." (the mockup's form); the announcement is the "reached" sentence. Expectation corrected.
- Existing checks changed because the surface changed, not because they were weakened: `PlanCanvas_HoverProbe_ParksPointerFirst`
  (the probe is the Tracing strip, no longer a box drawn in the viewport), `ModelArea_ViewLabels_AreTopLeftPlates_NotStrips` (the Plan
  slot ends in the strip row and the navbar clearance), `CommandTable_Parity_EveryRowInMenuPaletteKey` (the five comb verbs are
  palette and menu rows with no key, SC 2.1.4). `CurvePointLayer_PlanAndLane_SameGlyphPixels` went red when the selected-point ring was
  drawn above the glyph; the ring now sits under the glyphs, as the design's paint order says. The four Core checks of the old
  `Planform.Comb` (dead after the move) were ported to `Planform.Teeth`; the two that named an anchor by its authored kind now name it
  by the measured tangent angle and use a bent rail (design AM-RC-5).

Measured (instrumentation, not asserted): `RailComb.Build` at 128 teeth per rail on the fixture, both rails, teeth and pieces:
7.4 to 7.6 ms (three runs), inside the 100 ms edit budget.
