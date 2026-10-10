---
id: proof-dat-red-first
title: Export slice 1 (section .dat) red-first runs
type: proof-pack
status: in-review
owner: "@track-dat"
phase: implementation
tags: [export, dat, te-floor, red-first, proof]
links:
  - {to: design-export, rel: depends-on}
  - {to: spec-amendments-1-7-6, rel: relates-to}
review-by: 2026-12-09
summary: >-
  For each behaviour of the section .dat slice, the check that failed on the code before the behaviour and the run that passed
  after it: positional digits (B5), the name-line rule (B6), At station and Own (B9), the TE floor label and the unchanged
  analysis hash (B10), and the Desktop surface. Base main affddb89, track trk-dat, 2026-10-09.
---

# Export slice 1, section .dat: red-first runs

Base: main `affddb89`. Core and Analysis runs use `CFD_TEST_ONLY=<prefix> tools/run-suite.sh dotnet <test dll>`; the Desktop runs add
`--section-editor`. "Red" means the check fails on the code before the behaviour; "green" means it passes after.

## 1. Core: the .dat writer (B5, B6, B9)

**Old code.** A first writer that is deliberately the naive one: `double.ToString("R")` for numbers, no rule on the name line, and the
authored profile for both shapes (a .dat built from raw samples, the red the design names for B9). The same checks, run on it
(`CFD_TEST_ONLY=DatExport_ ... CfdWorkbench.Core.Tests.dll`, exit 1, `RESULT failures=12`):

```
FAIL DatExport_B5_Numbers_PositionalNeverExponent_SameBits InvalidOperationException: Expected False; actual True
FAIL DatExport_B6_NameLine_NeverStartsWithTwoNumbers_Reimports InvalidOperationException: Expected False; actual True
FAIL DatExport_B6_NameLine_ControlCharactersAndLengthRemoved InvalidOperationException: Expected False; actual True
FAIL DatExport_B9a_AtStation_EqualsCamberPlusMinusHalfThickness InvalidOperationException: Expected 4567689070244516098; actual 4567348166586459625
FAIL DatExport_B9b_AtStation_EqualsInvertedPlacedSurface InvalidOperationException: Expected True; actual False
FAIL DatExport_B9c_AtStation_PeakThicknessIsStationThickness InvalidOperationException: Expected True; actual False
FAIL DatExport_B9d_Own_PeakIsAuthoredMaximum_IgnoresThicknessChannel InvalidOperationException: Expected True; actual False
FAIL DatExport_B9e_OwnAndAtStation_AgreeWhenThicknessIsTheAuthoredPeak InvalidOperationException: Expected True; actual False
FAIL DatExport_B9f_BlendedEta_PeakAndPlacement InvalidOperationException: Expected True; actual False
FAIL DatExport_B9_XGrid_EqualsChordGrid_AndSurfaceAbscissae InvalidOperationException: Expected True; actual False
FAIL DatExport_Deviation_MatchesProbeAndShrinksWithPoints InvalidOperationException: Expected True; actual False
FAIL DatExport_ExampleFixture_RoundTripsThroughImportWithinDeviation FileNotFoundException: Could not find file '.../Fixtures/export/basic-foil-root-r1.dat'.
RESULT failures=12
```

Three checks passed on the naive writer by design: the trailing-edge row from the written points, the Selig and Lednicer layout, and the open
and closed trailing edge as built. Those are layout and consistency checks that the naive writer also satisfies; they guard against regression.

Two honest notes. (1) The first run of `B9d` failed on a wrong assumption of mine, not on the writer: it asserted the Example's authored peak was
0.13, and the measured value is 0.1126 (control points are not curve values). I corrected the range and added an assertion on the written points
(the thickest written row of the thinned foil is 0.1 to 5e-4, which the raw profile, at 0.1126, fails). (2) `TeFloor_B10_NoPractitionerValueString_InSource`
failed once, on a comment of my own in `Settings.cs` that quoted the retired wording; the comment was reworded and the check went green.

**New code.** `DatExport.Number` slides the decimal point of the round-trip digits, `NameLine` prefixes `foil ` when the first two tokens are
numbers, and the At-station section is camber plus and minus half the thickness from `Placement.Sections`. Green
(`CFD_TEST_ONLY=DatExport_,TeFloor_`, exit 0, `ran=17 skipped=731`, `RESULT failures=0`):

```
PASS DatExport_B5_Numbers_PositionalNeverExponent_SameBits
PASS DatExport_B6_NameLine_NeverStartsWithTwoNumbers_Reimports
PASS DatExport_B6_NameLine_ControlCharactersAndLengthRemoved
PASS DatExport_B9a_AtStation_EqualsCamberPlusMinusHalfThickness
PASS DatExport_B9b_AtStation_EqualsInvertedPlacedSurface
PASS DatExport_B9c_AtStation_PeakThicknessIsStationThickness
PASS DatExport_B9d_Own_PeakIsAuthoredMaximum_IgnoresThicknessChannel
PASS DatExport_B9e_OwnAndAtStation_AgreeWhenThicknessIsTheAuthoredPeak
PASS DatExport_B9f_BlendedEta_PeakAndPlacement
PASS DatExport_B9_TrailingEdgeRow_FromWrittenPoints_BothShapes
PASS DatExport_B9_XGrid_EqualsChordGrid_AndSurfaceAbscissae
PASS DatExport_Selig_NoseOnce_SeligOrder_PointCount
PASS DatExport_Lednicer_Layout_Reimports
PASS DatExport_ClosedAndOpenTrailingEdge_AsBuilt
PASS DatExport_Deviation_MatchesProbeAndShrinksWithPoints
MEASURE dat_roundtrip_max_dy_chord=3.727E-006 deviation_chord=6.166E-005 fit_residual_chord=3.727E-006 vertices=14
PASS DatExport_ExampleFixture_RoundTripsThroughImportWithinDeviation
PASS TeFloor_B10_NoPractitionerValueString_InSource
```

B9's six checks and two assertions run on the Example foil at its Tip (twist -2 degrees, t/c 0.12 against an authored peak of 0.1126), and (f) on a
second foil with a thinner tip profile at eta 0.5. B5's three named values are 1e-5, 3.585447714271229e-5 and 1e-300.

## 2. Analysis: the TE floor label and the unchanged hash (B10)

**Old code.** `Settings.TrailingEdgeFloorMm` and `Settings.TrailingEdgeFloorLabel` did not exist, so the check does not compile
(`dotnet build tests/CfdWorkbench.Analysis.Tests`, 4 errors):

```
TeFloorLabelTests.cs(30,33): error CS0117: 'Settings' does not contain a definition for 'TrailingEdgeFloorMm'
TeFloorLabelTests.cs(31,28): error CS0117: 'Settings' does not contain a definition for 'TrailingEdgeFloorMm'
TeFloorLabelTests.cs(35,54): error CS0117: 'Settings' does not contain a definition for 'TrailingEdgeFloorLabel'
TeFloorLabelTests.cs(36,35): error CS0117: 'Settings' does not contain a definition for 'TrailingEdgeFloorLabel'
```

The golden hashes were read from the unchanged code before the constant was added (a scratch program printing `RunRecord.SettingsHash`):
`Settings.Default` 9713863597250cd245c4d70560abee8840c002f6ff33501127ba331ac8efad62, the small Desktop settings aa39f244f4235616ded920b58a1599a768765de16f74ba76e225cbe9c81543d7,
and Default without stations 9b402c74273eea9dcd0c3ca47cdcced2ec6764536434e462ba837150281fe58a. A hash check cannot fail on the old code by design: it is a characterization, green
before and after.

**New code.** `Settings.cs` gains the two constants and `Default` reads `TrailingEdgeFloorMm`. Green (`CFD_TEST_ONLY=TeFloor_`, `RESULT failures=0`):

```
PASS TeFloor_B10_SettingsHash_GoldenUnchanged
PASS TeFloor_B10_Numerics_DefaultRecordFieldsUnchanged
PASS TeFloor_B10_Label_OneNamedConstant_AppDefaultNoSource
```

## 3. Desktop: the dialog, the rows, the copy, the hard states

**Old code.** `ExportSession`, `ExportSource`, `ExportDialog` and `ExportCopy` did not exist. The new checks, built against `affddb89` in a scratch worktree
(since removed), do not compile (`dotnet build tests/CfdWorkbench.Desktop.Tests`, 6 errors):

```
tests/CfdWorkbench.Desktop.Tests/ExportTests.cs(185,20): error CS0246: The type or namespace name 'ExportSource' could not be found
tests/CfdWorkbench.Desktop.Tests/ExportTests.cs(185,44): error CS0246: The type or namespace name 'ExportSource' could not be found
tests/CfdWorkbench.Desktop.Tests/ExportTests.cs(187,34): error CS0246: The type or namespace name 'ExportSource' could not be found
tests/CfdWorkbench.Desktop.Tests/ExportTests.cs(402,20): error CS0246: The type or namespace name 'ExportDialog' could not be found
tests/CfdWorkbench.Desktop.Tests/ExportTests.cs(402,76): error CS0246: The type or namespace name 'ExportSession' could not be found
```

(The build reports these distinct errors; the checks also name `ExportCopy`, `ShowExportDialog` and `ExportSnapshot`, which the compiler stops short of.) This is a compile-red,
not a behaviour-red: the Desktop behaviours were written together with their checks. The first run of the new checks against the new code had five failures: four dialog checks
on an `UnsetValueType` brush cast in the summary labels (a resource looked up before the theme resolved it), and the jump check on a wrong assumption that the jump keeps the station
selected (opening its section replaces the selection with a point). Both were repaired in one cycle.

**New code.** Green (`CFD_TEST_ONLY=Export_ ... CfdWorkbench.Desktop.Tests.dll --section-editor`, exit 0, 20 checks, all PASS):
Commands, Copy registry (COPY-474 to 519), NoFoil (H9), Session defaults, TrailingEdge (row always, band below the floor, never blocks), Blocked (H2),
Shape (Own and At station differ), Shell FileExport, Shell SectionExportDat, Cancel at the panel (H8), WriteFailure (H6), OneDrive-named folder, Extension, Draft (H1),
Analysis (H4), Dialog shape (one format, no STL or 3MF, STEP row), Dialog keyboard, Dialog failure, Dialog jump (H3), Dialog renders (screenshot).

## 4. Repair cycles

One failure on the ring, one repair (cap 2): `WindowsShell_EveryTableGesture_FiresItsCommandOnce` read "fired 0" for 15 rows after the new `file.export` row, because the probe
runs each row's command and the real Export dialog is modal. The check now sets `host.ShowExportDialog`; the class is `ROW-MODAL-A` in `docs/lessons/defect-classes.md`.

## 5. Security review repairs (export-writer boundary, name line, forced extension)

Red on the code of 42713d10, then green after the repair (one cycle). Red run (`CFD_TEST_ONLY=DatExport_B6` and `CFD_TEST_ONLY=Export_ ... --section-editor`):

```
FAIL DatExport_B6_NameLine_NeverStartsWithTwoNumbers_Reimports InvalidOperationException: Expected True; actual False
FAIL DatExport_B6_NameLine_ControlCharactersAndLengthRemoved InvalidOperationException: Expected False; actual True
FAIL Export_Write_SymlinkTarget_Refused_LinkAndTargetUnchanged InvalidOperationException: expected Failed; actual Written
FAIL Export_Extension_ForcedPathThatExists_NeverOverwritten InvalidOperationException: expected Failed; actual Written
```

The name-line checks now include `1,2 wing`, `2*0.5 wing`, `1d0 2d0 wing`, `1 , 2 wing` and `  7 wing` (the rule is: a letter first, else prefix `foil `) and a check that bidi format characters are stripped. `Export_FileName_FromHostileNames_HasNoSeparatorOrDotDot` (`../../x`, `a/b\c`, `..`, `C:\evil`) was green at once: `DatImport.Slug` already reduces names to letters, digits and hyphens, so it is a guard, not a red.

Repair: `DatExport.NameLine` prefixes `foil ` unless the first non-blank character is a letter and strips `UnicodeCategory.Format`; `ExportSession.RunAsync` refuses a path the app changed (forced extension) when it exists; `WriteAtomicAsync` refuses a link at the target, creates the temp file with `FileMode.CreateNew` and `FileShare.None`, and flushes to disk before the rename. A refusal reuses the approved write-failure sentence (COPY-507, no cause) and the dialog offers Choose another place. Green: see the ring below.

Green: `tools/run-tests.sh` exit 0 (Core 341+202+205, Desktop 737, Analysis 147+101, Cli 6 PASS; wall 44 s, 0 COST-MISS). One earlier ring run read `C-2 Analysis.part1of2 took 5068 ms, over 5000 ms` (68 ms over, load 9.5; no Analysis code changed in this repair); the rerun read 4670 ms and 4604 ms.
```
wall 44 s (44424 ms, net 42683 ms) (budget 60 s) cpu 479 s load 6.32 -> 11.76
```
`check-docs.py` exit 0 and `run-verify-gates.py` with the three skips exit 0 after `git add -A`.
