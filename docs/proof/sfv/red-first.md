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
`SectionForce_LatticeRun_InducedSharesSumToWingDi_AndAnchorFromStrip` passes on a real lattice run (`--readiness`).

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
```
FAIL SectionForce_LatticeRun_InducedSharesSumToWingDi_AndAnchorFromStrip InvalidOperationException: sum of strip d' x width equals the wing D_i expected 3.090930793438014; actual -3.3106151683997487
RESULT failures=1
```
The unmutated run: `PASS SectionForce_LatticeRun_InducedSharesSumToWingDi_AndAnchorFromStrip`, `RESULT failures=0`; stated tolerance 1e-9 relative (observed equal to rounding).

### Mutant (Desktop readiness, drawn geometry): V-inf and drag drawn mirrored (falling), as the mockup draws them
```
FAIL SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule Exception: V∞ is drawn at α_geo to the chord: expected 3.5, got -3.5000000000000004
```
Unmutated: `PASS SectionForceVectors_Drawn_LiftPerpendicularDragParallelToFreeStream_AnchorByRule` (`--readiness`, `CFD_TEST_ONLY=SectionForceVectors_`).
