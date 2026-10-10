---
id: proof-stx-red-first
title: "STX red-first receipt"
type: proof-pack
status: active
owner: "@trk-stx"
phase: implementation
tags: [export, stl, red-first, b2, b3, b4, area-7]
links:
  - { to: design-export, rel: depends-on }
review-by: "2027-04-01"
summary: >-
  The red runs of the wing STL track: every Core check failed on a skeleton, the root-cap check failed without the cap, the
  bit-pattern check failed when -0.0 was left in, and the edge-check hash collision that made the first green run take minutes.
---

# STX red-first receipt

Design: `docs/design/export.md` 3.3, 4.2, 5 and conditions B2, B3, B4. Session `trk-stx`, branch `feat/stx-stl-export`, 2026-10-10.
Run command: `CFD_TEST_ONLY=StlExport_ tools/run-suite.sh dotnet tests/CfdWorkbench.Core.Tests/bin/Release/net10.0/CfdWorkbench.Core.Tests.dll`.

## Phase 1, the Core

| Test | Mutant | Red line | Then |
|---|---|---|---|
| all 13 `StlExport_*` checks | `StlExport` is a skeleton whose members throw `NotImplementedException` (same signatures) | `RESULT failures=13`, each `FAIL ... NotImplementedException: The method or operation is not implemented.` | writer landed, 13 `PASS` |
| `StlExport_B3_Half_PairedEdges_Euler2_PositiveVolume_RootFacePlanarAtY0` | the root-cap line `T(a, d, c); T(a, c, b);` deleted from `Close` | `FAIL ... open 0.26 half: 201 edges are not used exactly twice` (also `FAIL StlExport_B3_Half_FinestRung_Closed ... 1601 edges`, and `StlExport_B3_WholeVolume_IsTwiceTheHalf ContractError: EXPORT-NOT-CLOSED`: `Build` refuses the capless half) | line restored, `PASS` |
| `StlExport_B2_*` (bit pattern) | `PositiveZero` made the identity, so `-0.0` stays in the table | `FAIL StlExport_B2_TwoDoublesOneFloat_AndNegativeZero_StillClose ... hand-built whole wing: 8 edges are not used exactly twice`; 9 more checks `ContractError: EXPORT-NOT-CLOSED` (`RESULT failures=10`) | restored, `PASS` |
| `StlExport_B4_ChordwiseOnlyReporter_WouldUnderReadTheUntitledWing` | n/a: the check *is* the red. At Print the Untitled wing reports 0.0113 mm; a chordwise-only reporter reads 0.0020 mm, which is below the dense maximum minus the 0.0005 mm margin | asserted inside the check; it passes only because the chordwise reading falls short | n/a |

The mutants were applied to the working tree and reverted; they are not commits. The skeleton red run is `scratch/stx/red1.txt`
(not committed; the 13 lines above are its content).

## Phase 2, Desktop

Run command: `CFD_TEST_ONLY=ExportStl tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --section-editor`.
The checks were written against the new `ExportSession` API in the same change as the code (the dialog and session did not exist for
STL), so there is no skeleton run; the red proof is three one-line mutants of `ExportSession.cs`, applied by
`scratch/stx/mutate.sh` and restored (`diff` printed `identical`).

| Mutant | Red lines |
|---|---|
| `IsLarge` uses `>=` instead of `>` (B7: the band must not show at exactly 500,000) | `FAIL ExportStl_B7_LargeMeshLine_At500000And500001_UntitledFineWholeVsHalf ... expected False; actual True` |
| the stale-build guard `if (mine != generation) return;` removed | `FAIL ExportStl_StaleBuild_NewerOptionWins ... expected True; actual False` |
| `Extension` is always `.dat` (the forced extension for an STL) | `FAIL ExportStl_Session_PreparingThenReady_Rows_FileName_Fidelity ... Exported basic-foil-r1-mm.dat`, `FAIL ExportStl_Write_ForcedStl_NeverProjectFile_SymlinkRefused_NoTemp ... typed.stl`, `FAIL ExportStl_Shell_FileExport_WritesBytesAndStatusLine ... the half wing file is named for its scope` |

One defect the dialog check found while it was written: a dialog opened on a session that already holds the STL format reset it to
.dat, because the format list's initial selection fired its change handler. The list now takes its selection from the session before
the handler is wired (`ExportStl_Dialog_Options_Preparing_Ready_LargeBand_Closure`, failing line `ClosureBand` not visible, before the fix).

The built dialog in the mockup's STL ready state (Example foil, open 0.26 mm trailing edge, whole wing, Print) is
`docs/proof/stx/dialog-stl-ready.png`. It differs from the mockup in one place, on purpose: the trailing-edge row reads "0.26 mm along
the whole span" where the mockup reads "at the tip". The Example foil's open trailing edge is 0.25572873962800 mm at every station
to 1e-15, so "where the minimum is" is rounding noise; the whole-span wording (EX45a) is the true statement.

## Phase 3, B1 (open and measure in two slicers)

Command: `python3 tools/check-slicer-open.py` (on demand, a release check, not in the fast ring). Outputs: `slicer-open.json` (every
measurement), `slicer-run.txt` (the printed lines). Slicers: PrusaSlicer 2.9.4 and OrcaSlicer 2.3.2, both from `/Applications`, each
through `--info` (size, facets, manifold, volume; repair counters only when it repaired something).

| File (app's writer, Example foil unless noted) | Triangles | Both slicers: manifold, repair counters, size, volume |
|---|---|---|
| `example-open-whole.stl` (open 0.26 mm) | 18,418 | yes, none, size equal to the app, volume +0.00012 % (Prusa), +0.00013 % (Orca) |
| `example-closed-whole.stl` | 18,296 | yes, none, size equal, volume +0.0003 % / +0.00029 % |
| `example-open-half.stl` | 9,358 | yes, none, size equal, volume +0.00016 % / +0.00016 % |
| `example-closed-half.stl` | 9,296 | yes, none, size equal, volume +0.00004 % / +0.00003 % |
| `untitled-fine-whole.stl` (Untitled wing, Fine) | 513,596 | yes, none, size equal, volume -0.001 % / -0.001 % |

Tolerances, fixed in the script and not options: size within 0.0001 mm (the slicer prints six decimals of a binary32 value, a step of
0.00003 mm at 450 mm), volume within 0.1 %. The observed differences are 100 to 10,000 times inside them, so nothing was tuned.

Negative control (the red of B1): the closed whole wing with its 101st triangle removed comes back from both slicers as
`manifold = no` with `open_edges = 3`; the check fails a run in which a slicer calls it clean.

Load time (B7 input, measured, not modelled): 0.84 s (PrusaSlicer) and 0.87 s (OrcaSlicer) for the 513,596-triangle file (25.7 MB); 0.04
to 0.10 s for the Print-size files. The large-mesh line stays at 500,000 triangles.

### A defect the first green run found, not a mutant

The first green run took 67 s for one check and was killed at nine minutes. Cause: `StlExport.Check` kept its directed edges in a
`Dictionary<long, int>` keyed `(a << 32) | b`; `long.GetHashCode` folds that to `a ^ b`, which collides for neighbouring vertices,
so lookups degraded to scans. The fix is a comparer that mixes the whole key (`EdgeKey`). After it the same 13 checks run in about 4 s
and `Build` at the Fine preset of the Untitled wing (513,596 triangles) in 370 ms. Class: a hash on a packed key that the default
hash folds (see `docs/lessons/defect-classes.md`, HASH-FOLD-A).
