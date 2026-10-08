---
id: proof-num-red-first
title: NUM red-first receipts (plural agreement, round-off couple)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [num, red-first]
links:
  - { to: proof-stc-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Singular "1 chordwise panel" and the round-off floor on the c/4 couple: the checks that fail on the old behaviour, the passing runs, the measured residue.
---
# Track NUM: red first (plural agreement, round-off couple)

Command (both runs): `CFD_TEST_ONLY=SectionForce_ tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`
Ring: fast (every join) for the plural check, the nc = 1 real-lattice check (0.69 s, 2 spans per half) and a synthetic nc = 4 pin (under 1 ms); the
real nc = 4 lattice pin (0.73 s) is readiness-tier (`--readiness`). The SectionForce group hint in `AnalysisChecks.cs` moved 30 to 780 ms so the
part balance stays true. Final `tools/run-tests.sh`: exit 0, "all test harnesses passed", Analysis parts 4.6 s and 4.1 s, wall 53 s; one advisory
COST-MISS C-3 (load 39).

The red table below is from the first draft, where the nc = 4 pin was the real lattice run (it passed in red, as it must: the default is unchanged); the
second MEASURE pair (q c^2 190.9 N, ratio 9.3e-18 at nc = 1; nc = 4 ratio 8.1e-3) is the final configuration at 2 spans per half. The q c^2 of 152.2 N comes from the first draft (4 spans per half, nc = 1 Example foil, eta 0.5); 190.9 N comes from the final run (2 spans per half, nc = 1 Example foil, eta 0.5), so the two differ by strip geometry, not by error.

The red run used the new signatures (`CoupleLabel(value, scale, units)`, `CoupleValue`, `SectionForces.CoupleScale`) with the old behaviour
(`CoupleValue` = plain `Sig3`, `LatticeBias` always "panels"), so the checks compile and fail on the behaviour only.

## Red (old behaviour)

```
FAIL SectionForce_LatticeBias_SingularForOnePanel_PluralOtherwise_TrackNum ... one panel reads singular expected lattice, 1 chordwise panel; biased forward at low lift; actual lattice, 1 chordwise panels; biased forward at low lift
MEASURE NUM nc=1 Example foil: couple 1.776E-015 N.m/m, q c^2 1.522E+002 N, ratio 1.167E-017
FAIL SectionForce_OneChordwisePanel_CoupleIsRoundOff_ShownAsZero_TableAndProfile_TrackNum ... the table row shows the round-off couple as zero expected 0.00; actual 0.00000000000000178
MEASURE NUM nc=4 Example foil: couple -8.428E-001 N.m/m, ratio to q c^2 5.538E-003
PASS SectionForce_FourChordwisePanels_CoupleUnchanged_TrackNum
RESULT failures=2
```

## Green (fix)

```
PASS SectionForce_LatticeBias_SingularForOnePanel_PluralOtherwise_TrackNum
MEASURE NUM nc=1 Example foil: couple 1.776E-015 N.m/m, q c^2 1.522E+002 N, ratio 1.167E-017
PASS SectionForce_OneChordwisePanel_CoupleIsRoundOff_ShownAsZero_TableAndProfile_TrackNum
MEASURE NUM nc=4 Example foil: couple -8.428E-001 N.m/m, ratio to q c^2 5.538E-003
PASS SectionForce_FourChordwisePanels_CoupleUnchanged_TrackNum
RESULT failures=0
```

## Floor

Measured residue / (q c^2) at nc = 1: 1.2e-17. Floor 1e-9 is 8 orders above it. The nc = 4 default couple is 5.5e-3 of q c^2, 6 orders above the floor,
so the default is unchanged (check 3c). The `assume:` is in `Labels.RoundOffFloor`.

## Sweep: other `Sig3` callers that can be identically zero by construction (reported, not changed)

- `Labels.LiftLabel` and the L′ strip row (`SectionDisplay.cs` LiftRow): zero lift on a symmetric section at alpha 0 gives round-off lift.
- `Labels.InducedDragLabel` and the D′ induced strip row: Gamma near zero gives round-off induced drag (a zero-lift case).
- `Labels.TotalDragLabel`, total row and `DragBand`: carry the polar profile drag, which is positive; not zero by construction.

Only the couple is zero by construction, and only under three conditions at nc = 1: (i) the c/4 line is straight across the strip, (ii) elevation is linear, (iii) the placed-camber endpoints sit on the chord, so `ChordOf` equals `Frame.ChordMeters` (VortexLattice.cs:126-131, 272-274, 357-362, 761; SectionDisplay.cs:516-517). It was measured on the Example foil only, which meets all three (FoilSource.cs:537-538, 551-552). A curved or swept c/4 line leaves a geometric residue, Inferred at about 1e-6 to 1e-4 of q c^2 and not measured; the floor does not hide it. Also, at nc = 1 the lattice gives Cm c/4 = 0 for any camber, so "0.00" there is a model artifact, not a physical zero. Follow-up, not done here: a structural "not resolved at 1 chordwise panel" rule for the couple, like x_cp at SectionDisplay.cs:518. Only the couple is changed.
