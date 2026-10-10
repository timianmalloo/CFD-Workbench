---
id: proof-tmf-red-first
title: "TMF red-first receipt"
type: proof-pack
status: active
owner: "@trk-tmf"
phase: implementation
tags: [export, 3mf, red-first, b1, area-7]
links:
  - { to: design-export, rel: depends-on }
  - { to: proof-tmf-spec-excerpts, rel: related }
review-by: "2027-04-01"
summary: >-
  The red runs of the wing 3MF track: every Core and Desktop 3MF check failed on a skeleton, five one-line mutants of the writer each
  failed the check that names them, and the B1 slicer run carries two negative controls (a removed triangle, a unit declared as meter).
---

# TMF red-first receipt

Design: `docs/design/export.md` 4.3, 5 and condition B1. Session `trk-tmf`, branch `feat/tmf-3mf-export`, 2026-10-10.
Spec check of the two `assume:` markers: `3mf-core-spec-excerpts.md` (unit `millimeter`, section 3.4; counter-clockwise, normal outward, section 4.1.4).

## Phase 1, the Core

Run command: `CFD_TEST_ONLY=ThreeMfExport_ tools/run-suite.sh dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll`.

| Test | Mutant | Red line | Then |
|---|---|---|---|
| all 9 `ThreeMfExport_*` checks | `ThreeMfExport` is a skeleton whose `Build`, `Check` and `FileName` throw `NotImplementedException` | `RESULT failures=9`, each `FAIL ... NotImplementedException: The method or operation is not implemented.` | writer landed, 9 `PASS`, `RESULT failures=0` |
| `ThreeMfExport_AsWritten_*` (and `_TriangleCount_*`, `_Check_RefusesBrokenPackages`) | M1: `v2` and `v3` swapped in each `<triangle>` (clockwise winding) | `FAIL ... open 0.26 Whole: signed volume -1033806.06936603 is not positive (inside out)`; 3 more checks fail on the negative volume | restored, `PASS` |
| `ThreeMfExport_Model_UnitMillimeter_*` | M2: `unit="meter"` | the writer refuses its own package (`ContractError: EXPORT-NOT-CLOSED`), 8 checks fail: `Check` accepts only `millimeter`, so a wrong unit cannot be written | restored, `PASS` |
| `ThreeMfExport_Package_HoldsNoUserPathOrHost_RuntimeFixture` and `_Metadata_*` | M3: the account name in `Application` | `FAIL ... Application: <account> CFD Workbench 1.0.0`; `FAIL ... 3D/3dmodel.model holds the runtime value '<account>'` | restored, `PASS` |
| `ThreeMfExport_Package_HoldsNoUserPathOrHost_RuntimeFixture` | M4: `entry.LastWriteTime = DateTimeOffset.Now` | `FAIL ... no modification time: 10/10/2026 12:53:08 PM -07:00,...` | restored, `PASS` |
| `ThreeMfExport_Metadata_OnlyTitleDescriptionApplication` | M5: an extra `Designer` key | `FAIL ... keys: Title, Description, Designer, Application` | restored, `PASS` |

M1 also showed a defect in the test, not the writer: the raw-archive scan for runtime values flagged the 3-letter host name `Mac` inside the deflated bytes
of that mutant's package (a short value matches compressed bytes by chance). The raw scan now covers values of 8 or more characters; the decompressed parts
are scanned for every value of 3 or more. The mutants were applied to the working tree by a script and reverted (`diff` printed `identical`); they are not commits.

## Phase 2, Desktop

Run command: `CFD_TEST_ONLY=ExportThreeMf_,Export_,ExportStl_ tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --section-editor`.
The red run is on a skeleton with the API surface only (`ExportFormat.ThreeMf`, the `build3mf` constructor argument, `ExportCopy.FormatThreeMf`) and none of the behaviour:

| Check | Red line |
|---|---|
| `Export_Copy_MatchesRegistry_Ids474To519` | `COPY-480 is not in DESIGN.md section 7` |
| `ExportStl_Dialog_Options_Preparing_Ready_LargeBand_Closure` | `the 3MF row is in the format list` |
| `ExportThreeMf_Session_PreparingThenReady_Rows_FileName_MatchTheStl` | `expected True; actual False` (the format behaved as the .dat) |
| `ExportThreeMf_Scope_Half_NameAndCount_ChangingFormatRebuilds` | `expected basic-foil-r1-half.3mf; actual basic-foil-r1-half-mm.stl` |
| `ExportThreeMf_Write_ForcedExtension_ClosedPackage_*` | `expected Written; actual Failed` |
| `ExportThreeMf_H7_ClosureRefused_NothingWritten` | `expected True; actual False` |
| `ExportThreeMf_Dialog_FormatRow_*`, `ExportThreeMf_Dialog_Renders_*` | `NullReferenceException` (no `ThreeMfFormatItem`) |

After the session, dialog and registry changes the same command printed no `FAIL` line (43 `PASS`). Two existing checks changed their expectation with
the slice, because they asserted that 3MF was absent: `Export_Dialog_DatStlAnd3mf_StepRowExplainsItself` (three format rows) and the
`ExportStl_Dialog_*` "no 3MF row" line; the registry check no longer lists COPY-480 as reserved.

## Phase 3, B1 (the slicers)

`python3 tools/check-slicer-open.py` (output `slicer-run.txt`, data `slicer-open.json`): the same five wings as STL and as 3MF, in PrusaSlicer 2.9.4
and OrcaSlicer 2.3.2. Every file: manifold = yes, one part, no repair counter, size within 0.0001 mm, volume within 0.1 %. The controls are the red:

| Control | What a failure would look like | Observed (both slicers) |
|---|---|---|
| the closed whole wing as 3MF with its 101st triangle removed | the slicer reports it clean, so the check cannot fail | `manifold = no`, `open_edges = 3`, verdict PASS (flagged) |
| the same 3MF with `unit="meter"` | the slicer ignores the unit, so a millimetre part could be read at any scale unseen | read 120000.0 mm wide, 1000 times the app's 120 mm |
