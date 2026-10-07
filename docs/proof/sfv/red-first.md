---
id: proof-sfv-red-first
title: SFV red-first record for the Lift and Drag vector checks
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sfv, red-first, tests]
links:
  - { to: proof-sfv-model, rel: refines }
review-by: 2026-12-31
summary: >-
  For each behaviour of the Section force vectors (free-stream axes, anchor rule, units, scale rule, labels): the check fails when the
  behaviour is broken (a planted mutant), and passes on the build. The old code has no model at all, so its red is a compile failure.
---

# SFV red-first

**Old code.** At `5b1de97c` (the commit before the model) `SectionForceModel`, `SectionForces`, `Labels.LiftLabel` and the rest do not
exist, so the new checks do not compile: the red is "absent". The build run follows.

**Green on the build** (`CFD_TEST_ONLY=SectionForce_ tools/run-suite.sh dotnet tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`):
`PASS` on all five fast checks, `RESULT failures=0`; the readiness check
`SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip` passes on a real lattice run (`--readiness`).

**Red on a planted mutant.** One mutant at a time is planted in `SectionDisplay.cs` or `Labels.cs`, the harness is built and run, the FAIL line is
recorded, and the file is restored from git (`scratchpad/trk-sfv-build/mutants.sh`). Every FAIL line below is from that run.

## Mutants

### Mutant: x_cp read from the origin moment (no transfer to the strip leading edge)
```
FAIL SectionForce_Anchor_CpNearZeroAndOffSection_Ruling128And130 InvalidOperationException: x_cp from the strip's own moment and normal force, moved from the frame origin to the leading edge expected 0.3; actual 0.9949999999999999 (tolerance 1E-09)
RESULT failures=1
```

### Mutant: alpha_geo ignores the strip's twist (alpha_eff only)
```
FAIL SectionForce_FreeStreamAxes_LiftPerpendicularDragParallel InvalidOperationException: alpha_geo = alpha_eff + alpha_i expected 3.5; actual 2.5 (tolerance 1E-12)
RESULT failures=1
```

### Mutant: the |Cl_local| < 0.05 test removed
```
FAIL SectionForce_Anchor_CpNearZeroAndOffSection_Ruling128And130 InvalidOperationException: |Cl_local| < 0.05: c/4, though x_cp = 0.42 is on the chord expected QuarterChord; actual CentreOfPressure
RESULT failures=1
```

### Mutant: x_cp off the chord still used as the anchor
```
FAIL SectionForce_Anchor_CpNearZeroAndOffSection_Ruling128And130 InvalidOperationException: x_cp 1.05 is off the chord expected QuarterChord; actual CentreOfPressure
RESULT failures=1
```

### Mutant: moment per span converted with the force-per-length factor (mockup's 0.737562 class of error)
```
FAIL SectionForce_Imperial_ForceAndMomentPerSpan InvalidOperationException: 1 N·m/m in lbf·ft/ft expected 0.224809; actual 0.06852176585679176 (tolerance 1E-06)
RESULT failures=1
```

### Mutant: induced share taken from the near-field Fx of the strip
```
FAIL SectionForce_FreeStreamAxes_LiftPerpendicularDragParallel InvalidOperationException: d' induced per span expected 0.5; actual 4.26776695296637 (tolerance 1E-12)
RESULT failures=1
```

### Mutant: lift direction not perpendicular (leans aft)
```
FAIL SectionForce_FreeStreamAxes_LiftPerpendicularDragParallel InvalidOperationException: lift is perpendicular to V-inf expected 0; actual 0.12186934340514748 (tolerance 1E-12)
RESULT failures=1
```

### Mutant: a label drifts from its DESIGN.md row
```
FAIL SectionForce_Labels_EqualTheirDesignRows_Ruling130 InvalidOperationException: COPY-SF11 text expected Wing strip, per span; not the wing total; actual Wing strip, per span
RESULT failures=1
```

### Mutant (readiness check, real lattice run): induced share taken from the near-field Fx
This check is a **consistency** check, not verification (Ruling 131): both sides are read from the same Gamma and w_T, so it holds for any weights that add up. The distribution is checked by the elliptic-wing fixture below.
```
FAIL SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip InvalidOperationException: sum of strip d' x width equals the wing D_i expected 3.090930793438014; actual -3.3106151683997487
RESULT failures=1
```
The unmutated run: `PASS SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip`, `RESULT failures=0`; stated tolerance 1e-9 relative (observed equal to rounding).

### Mutant (Desktop readiness, drawn geometry): V-inf and drag drawn mirrored (falling), as the mockup draws them
```
FAIL SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule Exception: V∞ is drawn at α_geo to the chord: expected 3.5, got -3.5000000000000004
```
Unmutated: `PASS SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule` (`--readiness`, `CFD_TEST_ONLY=SectionForceVectors_`).


# Repair cycle 1 (Ruling 131)

Order of work for each item: the check first (red: the FAIL line below, from the run before the fix), then the fix (green). Every mutant was planted after
the commit that held the work and restored with `git checkout -- <file>`; nothing was lost. Commands: Analysis fast `CFD_TEST_ONLY=SectionForce_ tools/run-suite.sh dotnet
tests/CfdWorkbench.Analysis.Tests/bin/Release/net10.0/CfdWorkbench.Analysis.Tests.dll`, Analysis readiness the same with `--readiness`, Desktop readiness
`CFD_TEST_ONLY=SectionForceVectors_ tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --readiness`.

## Item 1: the tip strip is Not judged

Check: `SectionForce_TipStrip_NotJudged_NoAnchorNoJudgedValues_Ruling131` (Analysis readiness, real lattice run). Red, on the code that took the nearest strip with `MinBy`
and never read `strip.Provisional`:
```
FAIL SectionForce_TipStrip_NotJudged_NoAnchorNoJudgedValues_Ruling131 InvalidOperationException: tip strip, eta 1: no force vectors, so no CP anchor is drawn expected ; actual SectionForces { Eta = -0.9903926402016152, ChordMeters = 0.014671025211948012, ...
RESULT failures=1
```
Green (after `ForcesAt` reads the provisional tip rule of `AnalysisProjection.State` and the profile and table say `Labels.TipNotJudged`):
```
PASS SectionForce_TipStrip_NotJudged_NoAnchorNoJudgedValues_Ruling131
RESULT failures=0
```
It asserts, at eta 1 and at the outermost strip's own eta (0.990), no vectors, `ForcesNotJudged` = "Not judged — tip strip", one table row with the same words, no L', x_cp or M' row, and that
the interior strip (eta 0.5) keeps its vectors.

## Item 2: the 4-panel bias wording

Checks: `SectionForce_Labels_EqualTheirDesignRows_Ruling130` (fast; now also COPY-SF17 and the amended SF9 and SF12 rows) and, in the readiness lattice check, the table rows named by `Labels.XcpRowLabel(nc)` and
`Labels.CoupleRowLabel(nc)`. Red (with the label members stubbed to the old text, so the failure is a behaviour and not a compile error):
```
FAIL SectionForce_Labels_EqualTheirDesignRows_Ruling130 InvalidOperationException: COPY-SF9 row in DESIGN.md expected True; actual False
FAIL SectionForce_LatticeRun_InducedSharesSumToWingDi_AndAnchorFromStrip InvalidOperationException: Ruling 131: the x_cp row names the lattice, its panels and the forward bias expected True; actual False
```
(the second line is the readiness run with the label text real and the table not yet using it). Green: both pass, `RESULT failures=0`. DESIGN.md rows: SF9 and SF12 amended and SF17 added, "approved — Ruling 131".
The chordwise-convergence measurement is `nc-convergence.md` (a measurement, not a gate).

## Item 3: labels

The band-centre wording of SF9 and the "(panel, 2D inviscid)" wording of the Cm c/4 row are asserted by the same label check (SF9, SF12). Their red is the first FAIL above (SF9 row). Wording: "D′ profile + induced (band centre), free-stream axes
<v> <unit> · ×<k>" (the approved words with only "(band centre)" added) and "Cm c/4 (panel, 2D inviscid)".

## Item 4: the induced-drag distribution

The sum check is renamed `SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip` (consistency, not verification). New: `SectionForce_EllipticWing_InducedShareFollowsSqrtOneMinusEtaSquared`
(Analysis readiness): the exact elliptic planform (zero twist, uniform alpha, AR 8, 32 strips per half, the F-15 fixture), d'(y) normalised at the centre strip against sqrt(1 - eta^2):
**3 % for |eta| <= 0.5 and 10 % for 0.5 < |eta| <= 0.8**, plus the sum against the Trefftz-plane drag of the elliptic wing. The tolerances are stated from the measured profile, not widened to pass:
measured worst deviation 0.0181 (inner) and 0.0857 (outer), against a finite-AR lattice whose w_T falls off toward the tip (F-15: alpha_i / (CL / (pi AR)) is 1.016 at the centre, 1.001 at eta 0.5, 0.937 at 0.8, 0.837 at 0.9).

Mutants (planted after the commit, restored):
```
### Mutant: induced share taken from the near-field Fx
FAIL SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip ... sum of strip d' x width equals the wing D_i expected 3.090930793438014; actual -3.3106151683997487
MEASURE SFV elliptic AR 8, 32 per half: worst |d'/(c sqrt(1-eta^2)) - 1| 0.0001 for eta <= 0.5, 0.0049 for 0.5 < eta <= 0.8
FAIL SectionForce_EllipticWing_InducedShareFollowsSqrtOneMinusEtaSquared ... the strip shares sum to the Trefftz-plane induced drag of the elliptic wing expected 1.7272152596788477; actual -7.446711676630762

### Mutant: induced share from a constant Gamma (0.02), not the strip's own
FAIL SectionForce_LatticeRun_InducedSharesConsistentWithWingDi_AndAnchorFromStrip ... expected 3.090930793438014; actual 0.7222340390348349
MEASURE SFV elliptic AR 8, 32 per half: worst |d'/(c sqrt(1-eta^2)) - 1| 0.1329 for eta <= 0.5, 0.5097 for 0.5 < eta <= 0.8
FAIL SectionForce_EllipticWing_InducedShareFollowsSqrtOneMinusEtaSquared ... d'(y) follows sqrt(1 - eta^2) within 3% for eta <= 0.5: 13.29% expected True; actual False
```
Reading: the near-field mutant has the elliptic *shape* (Fx is also proportional to Gamma here: shape deviation 0.0001), so only the Trefftz-plane sum catches it; the constant-Gamma mutant has the right
total and the wrong shape, so only the distribution check catches it. Neither check alone is enough, which is why both stay.

## Item 6: no two plates overlap in states A to E

Check: `SectionForceVectors_Plates_DoNotOverlap_StatesAToE` (Desktop readiness; the five states with the numbers of the real runs, at 710 and 974 px, the Cp_min marker at the leading edge on either surface;
the plates, the Cp_min ring and the legend are recorded in `SectionProfileView.Plates`). Red, with the plates recorded and no avoidance yet:
```
FAIL SectionForceVectors_Plates_DoNotOverlap_StatesAToE Exception: state A, Cp_min lower, width 710: "local inflow α_eff 1.93°" 8, 208, 118.59619140625, 32 overlaps "Cp_min ring" 106.452, 202.1344, 16, 16
```
Green after the layout (fixed plates laid out first, force labels moved to the nearest free room, fixed plates still drawn last):
```
PASS SectionForceVectors_Plates_DoNotOverlap_StatesAToE
PASS SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule
```
