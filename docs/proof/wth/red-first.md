---
id: proof-wth-red-first
title: "WTH red-first receipt"
type: proof-pack
status: active
owner: "@trk-wth"
phase: implementation
tags: [windows, test-hygiene, proof, ruling-154]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  Receipt for Windows test hygiene (Ruling 154 conditions): a foreign-OS branch guard, one canonical temp root per test project, and the two remaining harness aborts converted to FAIL lines.
---

# WTH red-first: Windows test hygiene

Host: macOS, default `TMPDIR=/var/folders/.../T/` (under the `/var` link), no export. Release build. Harnesses run alone:
`dotnet run -c Release --no-build --project tests/CfdWorkbench.<X>.Tests/CfdWorkbench.<X>.Tests.csproj`. Logs sit beside this file.
Ring and cost of the new controls: both are `SelfLaunchTests` cases in the fast ring (every Desktop default run), about 15 ms each.

## 1. DirectoryLink (`PrefStore_DirectoryLink_WindowsBranchUsesJunction`)

Red, main with the new control `NoForeignOsBranchWithoutHostGuard` (`control-red.txt`, exit 70, the SelfLaunchTests case list):
`guard the call with OperatingSystem.IsWindows() / !IsWindows() in CfdWorkbench.Core.Tests/PreferenceStoreTests.cs:71, ...:77`.

Green: each half is inside `if (OperatingSystem.IsWindows())` / `if (!OperatingSystem.IsWindows())`; the other host prints
`NOT ASSESSED directory-link: ...`. Core alone on macOS prints the junction line and the check passes
(`SelfLaunchTests: all 9 cases passed.` in `desktop-planted-green.txt`).
Not proved here: the Windows run of this check (no Windows host on this Mac); the failure it removes ("A required privilege is not held") is the PC ring's finding in `docs/reviews/pr-12.md`.
Trade: the `windows: true` half (a fake `run`, no shell-out) no longer runs on macOS, as the ruling's condition and the control require.

## 2. Core and Cli temp sites (10 sites)

Red, control: the same case list names `LayoutFileTests.cs, SectionLibraryTests.cs, ProjectStoreTests.cs, WindowsProjectStoreTests.cs` (Core) and `CliTests.cs` (Cli), 5 files, 10 sites (`control-red.txt`).

Red, behaviour, harness alone on main with the default TMPDIR:

| Harness | Before | After |
|---|---|---|
| Core | exit 1, 679 PASS, 43 FAIL, the store and preference checks refused with `DOC-UNSUPPORTED-PERSISTENCE` or its effect (`core-alone-red.txt`) | exit 0, 722 PASS, 0 FAIL |
| Cli | exit 134, 3 PASS, `FAIL Cli_AnalyseRunKey_EqualsServiceOnCustomOp` then an unhandled exception (`cli-alone-red.txt`) | exit 0, 6 PASS, 0 FAIL |
| Desktop (default run) | n/a (HRN: green alone) | exit 0, 700 PASS, 0 FAIL |

(The SectionLibrary helper had patched `/tmp/` and `/var/` by hand; `/var/folders` is under `/private/var`, which that prefix list missed.)

## 3. The two remaining aborts

(a) Desktop. Plant: `tests/CfdWorkbench.Desktop.Tests/TestTemp.cs` `Root` returns the raw `Path.GetTempPath()` for the plant only (reverted; `git diff` of that file is empty).
Under the default TMPDIR the real `ProjectStore` answers `DOC-UNSUPPORTED-PERSISTENCE`, the Windows answer.
Red, old `WorkbenchTests.cs` (`desktop-planted-red.txt`, exit 70): `FAIL Save_ExistingFile_DefiniteCreateOnlyConflict ...`, then
`APP-UNHANDLED APP-CRASH CfdWorkbench.Core.ContractError: DOC-UNSUPPORTED-PERSISTENCE` at `OpenPathAsync` (WorkbenchTests.cs:329), the recovery block that held the `Durable recovery save did not settle` assertion. 0 PASS lines.
Green, new code, same plant (`desktop-planted-green.txt`, exit 1): `FAIL Recovery_SaveReopenResume_KeepsDraftSeparateFromAccepted ContractError: DOC-UNSUPPORTED-PERSISTENCE`, and the harness ran on: 678 PASS lines, the theme matrix and the spawned suites.
Sweep of the prefix: with the plant the prefix has no other top-level throw that the store answer reaches (the harness left the prefix and spawned its suites). The FAIL lines after the spawn (`Controller_SaveMine_...`, `Recent_OpenedOutcome_AppendsPath`, ...) come from spawned suites that open real files through the same plant. They are suite checks, not prefix throws, and are not in this track's scope or manifest.

(b) Cli. Red: the Cli row above (abort). Green with a plant (`TestTemp.cs` of Cli raw, reverted; `cli-planted-green.txt`, exit 1):
`FAIL Cli_AnalyseRunKey_EqualsServiceOnCustomOp Exception: inspect --runs returned 3: {"code": "DOC-UNSUPPORTED-PERSISTENCE"}` and the run continued to `PASS Cli_AnalyseFailedRun_PrintsNoDiagnostics` and
`FAIL Cli_InspectRuns_RevisionIsSessionLabel ... DOC-UNSUPPORTED-PERSISTENCE`, the check the abort had hidden.

Manifest (`tests/expected-failures.windows.json`): `aborts_harness` removed from the Cli entry; two entries added, class `fail-closed-store`, ruling 154, fragment `DOC-UNSUPPORTED-PERSISTENCE`, `"evidence": "Inferred until the next PC ring"`:
`Recovery_SaveReopenResume_KeepsDraftSeparateFromAccepted`, `Cli_InspectRuns_RevisionIsSessionLabel`.
`check-expected-failures.py --log cli-planted-green.txt --status 1 --host windows`: `EXPECTED-FAIL 2 (manifest)`, `UNEXPECTED 0`.
`check-expected-failures.py --self-test`: ok, 10 cases, manifest 32 entries.
