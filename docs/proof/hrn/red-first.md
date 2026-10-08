---
id: proof-hrn-red-first
title: "HRN red-first receipt"
type: proof-pack
status: active
owner: "@trk-hrn"
phase: implementation
tags: [desktop-harness, tmpdir, proof]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Red-first and after receipt for the Desktop harness giving one result alone and in the ring (TEST-TMP-ALIAS).
---

# HRN red-first: Desktop harness alone equals Desktop harness in the ring

Host: macOS, default `TMPDIR=/var/folders/.../T/` (under the `/var` link). All runs: `tools/run-suite.sh dotnet
tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll <args>`, Release build. (Through
`dotnet run -c Release --no-build --project ... -- --section-editor --part=1/2` the part runs fell into the harness's
full in-process prefix and crashed at WorkbenchTests.cs:281 with the same DOC-UNSUPPORTED-PERSISTENCE; the DLL form is used below.)

## Before (main 2062dff8)

| Run | Result |
|---|---|
| `--section-editor` (whole mode), default TMPDIR | exit 1: `FAIL SaveDialog_EmptyOrDuplicate_ErrorFocusStays ContractError: DOC-UNSUPPORTED-PERSISTENCE`; `FAIL SaveDialog_Save_LiveRegionFocusToSectionMenu Exception: Save did not publish and announce the exact name` |
| `--section-editor --part=1/2`, default TMPDIR | exit 0 (its two SaveDialog checks write nothing) |
| `--section-editor --part=2/2`, default TMPDIR | exit 1: the same two FAIL lines |
| no argument (full in-process prefix), default TMPDIR | crash `Expected definite create-only save conflict; actual DOC-UNSUPPORTED-PERSISTENCE` at WorkbenchTests.cs:281 |
| `--section-editor --part=1/2` and `2/2`, `TMPDIR=<non-symlinked dir>` | both exit 0, no FAIL |
| each SaveDialog check alone (`CFD_TEST_ONLY`), `TMPDIR=<non-symlinked dir>` | 4/4 PASS |

## Root cause (observed)

(a) Raw `Path.GetTempPath()` at 20 sites in 8 files (`CheckSave` and `CheckDamagedRow` in CatalogDialogTests, and the
`WorkbenchTests.cs` top level, among them). `/var/folders/...` is a symlinked path, which the store refuses by design.
Four more helpers patched the alias by hand with different prefix lists.

(b) Not a separate defect. With a non-symlinked TMPDIR, part 2/2 alone passes and so does every SaveDialog check alone: each
builds its own `SectionLibrary` root and reads nothing from a sibling part. The "part alone" failure is (a): the two checks that
write to the library sit in part 2/2 by registration index, so only that part shows it. No code change for (b) beyond (a);
`tools/run-tests.sh` is unchanged.

## Control

`SelfLaunchTests.NoRawTempPath`, red before the migration (run with no argument; SelfLaunch runs first):
`no Desktop test builds a scratch path from the raw temp directory: ... in WorkbenchTests.cs, ControllerSectionTests.cs,
ControllerShellTests.cs, ShellWindowTests.cs, StatusStripTests.cs, PropertiesCellsTests.cs, CatalogDialogTests.cs,
AnalysisPanelTests.cs`. After: `SelfLaunchTests: all 8 cases passed.`

## After (default TMPDIR, no export)

| Run | Result |
|---|---|
| `--section-editor --part=1/2` | exit 0 |
| `--section-editor --part=2/2` | exit 0 |
| no argument, whole harness incl. spawned modes | exit 0 |
| `tools/run-tests.sh` | exit 0; Desktop 41 s (39992 ms), 697 PASS; wall 46 s (45329 ms) under budget 60 s, load 10.47 -> 19.19 |

Desktop wall before the change was not measured in this track (one ring after, under load 19); the change adds one directory
resolve per process.

## Siblings (reported, not fixed)

Raw `Path.GetTempPath()` / `GetTempFileName()` remains in `tests/CfdWorkbench.Core.Tests` (7 lines) and
`tests/CfdWorkbench.Cli.Tests` (3 lines); Analysis has none. Not run alone here.
