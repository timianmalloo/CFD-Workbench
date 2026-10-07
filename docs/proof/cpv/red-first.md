---
id: proof-cpv-red-first
title: CPV red-first receipts
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [cpv, red-first, section]
links:
  - { to: mockup-section-main-area, rel: depends-on }
review-by: 2026-12-31
summary: >-
  The checks that failed on the old code (compile red: the members did not exist) and the runs that passed after the change.
---

# CPV red-first receipts

## Red

With the new checks in place and `src/` stashed (the old code, plus the untracked new view file), `dotnet build tests/CfdWorkbench.Analysis.Tests`:

```
DxSectionTests.cs(239,9): error CS0246: The type or namespace name 'SectionProfile' could not be found
DxSectionTests.cs(239,39): error CS1061: 'SectionView' does not contain a definition for 'Profile'
Build FAILED. 2 Error(s)
```

A compile red is the honest shape here: the old profile was a line chart with no Cp colour, no marker panel and no legend range to assert on.

## Repair cycle 1 (Ruling 126): copy rows and Units

Red: with `SectionView_Speeds_FollowUnitsSwitch` and `Labels_ProfilePlates_Copy406To409` written and the code not yet changed, the Analysis
tests failed to build: `DxSectionTests.cs(29,51): error CS1501: No overload for method 'Build' takes 9 arguments` and
`LabelsTests.cs: 'Labels' does not contain a definition for 'SectionCaption' / 'CpMinMarker' / 'AxisPlate' / 'ProfileCavitation'`.
Green: both `PASS` after the change (`SectionDisplay.Build` takes `Units units = Units.Metric`, passed from `SectionTabView.Bind`
as `controller.AnalysisUnits`; `Labels.Speed` and `Labels.SpeedUnit` convert with the existing `KnotsPerMeterSecond`). The check
asserts Imperial gives kn on the table row (value converted), the profile plate and the Bucket speed axis, and Metric gives m/s.

## Green

Command shape: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet <test dll> [--readiness]`.

| Behaviour | Check | Result |
|---|---|---|
| One outline point per panel point, upper then lower; the marker is the panel that sets Cp_min; the legend's low and high are the data's | `SectionView_CpOnProfile_DrawsAndPinsVik` (Analysis, replaces the comb check) | `PASS`, 945 ms |
| Colour per panel from its Cp, 0 at the ramp's centre, opposite signs on opposite sides, suction blue and pressure red, Cp 0 gives the centre colour, ramp extent is the data's | `SectionProfileView_Renders_CpOnVikPinnedAtZero` (Desktop `--readiness`, one render in the shared window) | `PASS`, 3,458 ms including the shared window's first build |
