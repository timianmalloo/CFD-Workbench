---
id: proof-ncr-red-first
title: NCR red-first receipts (Not resolved at 1 chordwise panel)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [ncr, red-first]
links:
  - { to: proof-num-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Ruling 161: the checks that fail on the old nc = 1 texts and glyph, the passing runs, and the rendered texts.
---

# Track NCR (Ruling 161): red first

Date 2026-10-08, Release build, macOS. Case: the Example foil, 2 spans per half, chordwise panels nc = 1, 2 and 4.

Rule under test: structural (nc < 2), `SectionForces.IsMomentResolved`; not a magnitude floor.

## Red (old code, new checks)

Analysis harness, `CFD_TEST_ONLY=NotResolved_ tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Analysis.Tests/CfdWorkbench.Analysis.Tests.csproj`, exit 1, `RESULT failures=2`:

- FAIL `NotResolved_OneChordwisePanel_TableValuesReadRuling161_LabelsDropBias`: no row labelled "x_cp/c (lattice, 1 chordwise panel)" (the old label carried the bias suffix).
- FAIL `NotResolved_OneChordwisePanel_NoteReadsRuling161`: same cause.
- PASS `NotResolved_FourChordwisePanels_TextsUnchanged` and `NotResolved_TwoChordwisePanels_StillResolved_StructuralRuleNotMagnitude` (pins of current behaviour; they must stay green).

Desktop harness, `--analysis` mode, `SectionProfile_OneChordwisePanel_NoCoupleGlyph_LabelReadsNotResolved_FourPanelsDrawsIt_Ruling161`, exit 1:
`nc = 1: no couple glyph: expected False, got True`.

## Green (new code)

Analysis: the same command, exit 0, four `PASS NotResolved_*`, `RESULT failures=0`.
Desktop: the same check, exit 0, `PASS SectionProfile_OneChordwisePanel_...`.

Rendered texts, read from the table rows and the drawn plates:

- x_cp/c row, label "x_cp/c (lattice, 1 chordwise panel)", value "Not resolved · 1 chordwise panel".
- M′ c/4 row, label "M′ c/4 (lattice, 1 chordwise panel)", value "Not resolved · 1 chordwise panel", no unit.
- Profile plate "M′ c/4 (lattice) Not resolved · 1 chordwise panel"; `CoupleDrawn` false; arrows start at c/4 (x = 0.154 w + 0.25 · 0.72 w).
- Note "With one chordwise panel the lattice can't resolve the centre of pressure or the pitching moment, so the arrows start at the quarter chord and no couple is drawn."

## Superseded checks

Two NUM checks in `SectionForceTests.cs` asserted the old nc = 1 texts and went red on the new code
(`SectionForce_LatticeBias_SingularForOnePanel_PluralOtherwise_TrackNum`, `SectionForce_OneChordwisePanel_CoupleIsRoundOff_ShownAsZero_TableAndProfile_TrackNum`).
That file is not this track's. The two-line update is its own commit, so the leader can drop or re-apply it against the held branch. The coordinator accepted it (commit 388fb6a0 stays).

## Ruling 162 follow-up (2026-10-08)

COPY-SF22 "c/4 · arrows start here · x_cp not resolved" replaces SF5 on the quarter-chord plate and in the profile's accessible name at 1 chordwise panel.

Red, Desktop `--analysis`, `SectionProfile_QuarterChordPlateAndAccessibleName_ReadSf22AtOnePanel_Sf5AtFour_Ruling162`: FAIL "nc = 1: the plate reads SF22", the plates listed "c/4 · arrows start here · x_cp Undefined". Green after the change; the same check pins SF5 on the plate and the accessible name at nc = 4.

The two Analysis reds above fail on the row-label `Single` first. Each value assert was then seen red on its own, by reverting only that value code in `SectionDisplay.cs` (labels kept) and restoring it after:

- x_cp text reverted: FAIL "x_cp/c reads Not resolved, not the bare Undefined expected Not resolved · 1 chordwise panel; actual Undefined".
- Unit put back on the not-resolved couple row: FAIL "a value that is not a number carries no unit expected ; actual N·m/m".

## Next step (recorded, not done)

A readiness-tier measurement on a cambered section at nc = 2, 4 and 8, to size the SF17 "biased forward at low lift" wording. The CFD reviewer's 2D re-run captured 66% of Cm c/4 at nc = 2 and 87% at nc = 4, and found the bias is in magnitude at every lift. Every NCR fixture is the symmetric NACA 0012, so none of these checks can see it.
