---
id: proof-wlk-red-first
title: "WLK red-first receipt"
type: proof-pack
status: active
owner: "@trk-wlk"
phase: implementation
tags: [windows, links, preferences, proof]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-11"
summary: >-
  Red-first and after receipt for Windows-safe link tests (TestLinks, the bare-link gate) and for view-setting restart checks that
  assert the session-only behaviour where the project store is unsupported (TEST-FOREIGN-OS-BRANCH).
---

# WLK red-first: link tests and view-setting tests on a host without a link privilege or a project store

Host: macOS arm64, Release build. Base: main 2d3ac1eb. No Windows host was available. Every statement about Windows is **Inferred**
from the code and the PC's report; the list of checks for the PC to rerun is at the end.

## 1. The gate: no bare `CreateSymbolicLink(` in tests

Check: `SelfLaunchTests.NoBareSymbolicLinkOutsideTestLinks` (`tests/CfdWorkbench.Desktop.Tests/SelfLaunch.cs`). Ring: fast, ~15 ms.
Run: `dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests` (default mode, `SelfLaunchTests` is its first stage).

| Run | Result |
|---|---|
| Red: gate added, tests as on main | `System.Exception: SelfLaunchTests failed: no test creates a symbolic link except through TestLinks: ... in CfdWorkbench.Core.Tests/PreferenceStoreTests.cs, CfdWorkbench.Desktop.Tests/ExportStlTests.cs, CfdWorkbench.Desktop.Tests/PropertiesCellsTests.cs, CfdWorkbench.Desktop.Tests/ExportTests.cs, CfdWorkbench.Cli.Tests/ExportCliTests.cs` (exit 70) |
| Green: every link through `TestLinks` | `SelfLaunchTests: all 10 cases passed.` (exit 0, whole default mode) |

`ProjectStoreTests.cs` is exempt, by name, in the gate. Its `Run()` hands off to `WindowsProjectStoreTests` on Windows and returns
(`ProjectStoreTests.cs:12`) before any check registers, so its three link sites (lines 100, 102, 311) never run on Windows. They are
unchanged.

## 2. TestLinks behaviour

- A directory link: a symbolic link on macOS; on Windows a junction through `mklink /J` (the WFX2 path, now in one place).
- A file link: `File.CreateSymbolicLink`. On Windows, an `IOException` or `UnauthorizedAccessException` whose HResult is
  0x80070522 or whose message names a privilege returns false and prints
  `NOT ASSESSED file-link (Windows: creating a file link needs a privilege; enable Developer Mode)`. Any other error throws, and on
  macOS every error throws.
- Moved sites: `PreferenceStoreTests` (1091, and the Core helper), `ExportTests` (401), `ExportStlTests` (241, which had an
  `!IsWindows()` guard: it now runs on Windows when the link can be made), `ExportCliTests` (118, 136, 291),
  `PropertiesCellsTests` (its own helper). Assertions after the link is made are unchanged.
- The privilege-detection branch cannot be run on the Mac (no Windows host raises that error). **Inferred** from the repo's own note
  (`PreferenceStoreTests`, WFX2: IOException "A required privilege is not held") and Win32 error 1314.

## 3. View-setting restart checks where the store is unsupported

`ProjectStore.Supported()` accepts macOS arm64 only (`ProjectStore.cs:266`) and guards `Read` and `Save`. Read from the source
(`PreferenceStore.cs:519-535, 575-595`): with an unsupported store a load of a missing file is `absent`, a load of an existing file
is `session-only` with the defaults, a save is `session-only`, and `EnsureDir` creates an empty `display` folder but no file.

Seam: `PreferenceStore(root, Func<IProjectStore>)` already takes a store, so a fake `CodeStore("DOC-UNSUPPORTED-PERSISTENCE")`
(Core) and `StoreSupport.Unsupported()` (Desktop) show the Windows behaviour on the Mac. No `src/` seam was added.

The host decides with a probe of the real store (one save of one byte; `OK` means supported, `DOC-UNSUPPORTED-PERSISTENCE` means
not), never with an OS flag. A macOS arm64 host that reports unsupported throws: the probe is wrong, not the host.

Red: the old checks run against an unsupported store (Mac, temporary edit of the store factory, reverted):

| Harness | Failing checks (8 each) |
|---|---|
| Core, `PrefStore_Comb_*` | `RoundTrip_EveryStep_DefaultsNotWritten` (Expected saved; actual session-only), `SaveDoesNotResetTextSizeOrUnits_AndTheReverse`, `ValueError_ThatMemberDefaults_OthersKept_BytesUnchanged` (Expected 64; actual 32), `StructureError_WholeFileDefaults_BytesUnchanged`, `NewerVersion_WholeFileDefaults_NeverOverwritten`, `OldFileNewBuild_NoRewriteOnLoad`, `Atomic_FaultBeforePublish_PreviousBytesKept_NoTemp`, `TwoInstances_LaterWinsPerMember_Retried` |
| Desktop | `Comb_Settings_Persist_ChoiceSurvivesShellRestart_PlateAndTeethRendered`, `Comb_Settings_Auto_RestoredAsAuto_Visible`, `Comb_Settings_SaveKeepsTextSizeAndUnits`, `Comb_Settings_ChoiceBeforeLoadWins`, `Comb_Settings_FoilAndProjectBytesUnchanged_Telemetry_NoPath`, `Comb_Settings_ValueFault_KeepsOtherMembers_NeverRewrites`, `Units_Persist_ChoiceSurvivesShellRestart_TextSizeKept`, `Units_OldPreferenceFileWithoutKey_LoadsMetric` |

Two older text-size checks, `PrefStore_TextSize_ValueError_ThatMemberDefaults_BytesUnchanged` and
`PrefStore_TextSize_StructureError_WholeFileDefaults_BytesUnchanged`, also fail against an unsupported store and are not in
`tests/expected-failures.windows.json`; they are wrapped the same way. Entries already in that manifest are untouched (wrapping a
listed test would turn its expected failure into an UNEXPECTED-PASS).

Green, after: each of those checks is wrapped (`OrSessionOnly` in Core, `PersistCheck` in Desktop). Where the probe says the store
works (the Mac) the body runs as written. Where it does not, the check asserts the designed behaviour: the session keeps its own
choice, no file is written, a restart reads the defaults, an unreadable seeded file is left byte-for-byte, nothing throws. Three new
checks run that assertion against a fake unsupported store on every host:

- `PrefStore_Comb_UnsupportedStore_SessionOnly_NoFile_NoError` (Core; also asserts the store is called once)
- `Comb_Settings_UnsupportedStore_SessionOnly_NoFile_NoError` (Desktop, plan canvas)
- `Units_UnsupportedStore_SessionOnly_NoFile_NoError` (Desktop, readiness)

## 4. Full runs after the change (Mac)

| Command | Result |
|---|---|
| `tools/run-tests.sh` | exit 0; Core 3 parts, Desktop 799 PASS, Analysis 102 + 146 PASS, Cli 19 PASS; `0 failures, 0 COST-MISS`; wall 51 s of the 60 s budget |
| `(unset CFD_TEST_ONLY; dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests -- --readiness)` | exit 0; 96 PASS, 0 FAIL |

## 5. For the PC: rerun on Windows

1. `SelfLaunchTests` (default Desktop mode): the gate must pass.
2. With Developer Mode **off**: `ExportTests` `SymlinkTarget`, `ExportStlTests` (the symlink part), the three `ExportCliTests` link
   checks should print `NOT ASSESSED` and pass. With it **on**, they
   run: report any that fail, since a refused file link on Windows may give a different code than on macOS (Inferred).
3. `PrefStore_Comb_*`, `PrefStore_TextSize_ValueError_*`, `PrefStore_TextSize_StructureError_*`, `Comb_Settings_*`, `Units_*`: each
   should print one `NOT ASSESSED view-settings persistence` line and pass by the session-only assertions. A failure there means the
   Windows store behaves differently from the source reading above (for example a load that returns `failed` instead of
   `session-only`).
4. The Windows ring's other failures (child timeouts, one analysis golden mismatch) are not addressed here; they wait for the PC's
   check names.
