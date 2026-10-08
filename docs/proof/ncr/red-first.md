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
That file is not this track's. The two-line update is its own commit, so the leader can drop or re-apply it against the held branch.
