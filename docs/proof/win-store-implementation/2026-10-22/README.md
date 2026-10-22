---
id: proof-win-store-implementation-20261022
title: "Windows project store admitted (W-2 B2): the evidence from the laptop"
type: proof-pack
summary: "ProjectStore.Supported() admits Windows x64 and ARM64 through WindowsProjectStore on NTFS. The 51 macos-only-store skips are gone and every former skip passes; the Ruling 145 cases pass; a killed save leaves a measured inventory; the OneDrive refusal and the default folder are built. CI on the x64 runner and the Opus Adversary pass are still to come."
status: active
owner: "@w2-windows-store"
phase: implementation
tags: [windows, persistence, ntfs, store, w-2, b2]
links:
  - { to: design-windows-native-store, rel: implements }
  - { to: rulings, rel: depends-on }
  - { to: proof-win-store-design, rel: supersedes }
---

# Windows project store admitted (W-2 B2)

Track W2 of the coordination plan `next-best-six`, session `w2-windows-store`, branch `w2-windows-store`, based on `main` after
the D1 join. This note records what was observed; the design is `docs/design/windows-native-store.md` (erratum in sections 3, 4, 6).

**Reproduce.** `bash docs/proof/win-store-implementation/2026-10-22/reproduce.sh` builds and runs the Windows store checks and prints
the measured lines. The whole suite is `bash tools/run-tests.sh`.

## Environment (Ruling 238)

| | |
|---|---|
| OS | Windows 11 Pro, 10.0.28000, ARM64 (`arch=Arm64`) |
| File system | NTFS (the harness line `NATIVE-ENV os=10.0.28000.0 arch=Arm64 filesystem=NTFS`) |
| CPU | NVIDIA RTX Spark N1X, 20-core CPU |
| SDK | .NET SDK 10.0.402 |
| Account | a non-elevated account with Developer Mode on (file symbolic links can be created; a case-sensitive directory cannot) |

The Windows CI runner is x64. Nothing in the adapter assumes ARM64 (`PlatformAdmitted()` admits both); CI is read after the join.

## Red then green

| Step | Command | Result |
|---|---|---|
| Baseline | `bash tools/run-tests.sh` | `TESTS-GREEN`; Core 762 passed, 26 skipped; Analysis 260 passed, 0 skipped; Desktop 782 passed, 23 skipped; Cli 19 passed, 2 skipped. **51** `macos-only-store` SKIP lines (26 + 23 + 2), counted in the project logs. |
| Admission red | `CFD_TEST_ONLY=WindowsStore_ CfdWorkbench.Core.Tests.exe --harness` on the old `Supported()` | `FAIL WindowsStore_NtfsFixture_SaveOpenReopenSucceeds InvalidOperationException: Expected OK; actual DOC-UNSUPPORTED-PERSISTENCE` / `RESULT failures=1` |
| Admission green | same command on the adapter | `PASS WindowsStore_NtfsFixture_SaveOpenReopenSucceeds` / `RESULT failures=0` |
| All store checks red on a closed store | the same subset with `PlatformAdmitted()` forced false | 11 of 12 `WindowsStore_` checks `FAIL ... actual DOC-UNSUPPORTED-PERSISTENCE`; the reparse check passes on a closed store because every refusal there is also `DOC-UNSUPPORTED-PERSISTENCE`, so it carries a plain-folder control that is `OK` only when admitted. The `CloudFolder` checks have no old form: the class did not exist (they fail to compile on the old tree). |
| Un-skip | `bash tools/run-tests.sh` after deleting `MacOnlyStoreChecks` (Core, Desktop) and the two Cli `IsMacOS` guards | first run: 7 Core and 6 Desktop checks failed (plus the harness-exit tests and two Desktop part exits that carried them), all fixed in the adapter or in a Mac-coded fixture (below); then Core 788 / 0 skipped, Desktop 805 / 0 skipped, Cli 21 / 0 skipped |
| Final | `bash tools/run-tests.sh` | see "Final run" |

## The Ruling 145 cases

Both exist in `tests/CfdWorkbench.Core.Tests/WindowsProjectStoreTests.cs` and pass (proof-run output):

```
PASS WindowsNative_Replace_ShareReadDeleteReaderKeepsOldImage
  SHARE-READ-DELETE NTSTATUS=0x00000000 IO_STATUS=0x00000000 Win32=0 held_bytes=0101 fresh_bytes=020202 temp_exists=false
PASS WindowsNative_Replace_ShareReadOnlyReaderRefused
  SHARE-READ-ONLY NTSTATUS=0xc0000043; IO_STATUS=0x00000000; Win32=32 ... held_bytes=0101 fresh_bytes=0101 temp_bytes=020202 temp_exists=true
```

Through the public store: `WindowsStore_Overwrite_ShareReadDeleteReaderKeepsOldImage` (`code=OK held_bytes=0101 fresh_bytes=020202 entries=1`) and
`WindowsStore_Overwrite_ShareReadOnlyReaderRefused_IncumbentKept` (`code=DOC-CONFLICT incumbent_bytes=0101 entries_after_refusal=1`; Win32 32 maps to
the product code, never `DOC-IO`).

**The historical probe.** `WindowsNative_Replace_HeldReaderKeepsOldImage` was removed under Ruling 247 in commit `b08f329a` ("plain test
runner ... (Ruling 247 steps 3 and 4)"), together with the known-expected-failure manifest of Ruling 152 (2). `git log -S` over `tests/`
lists `b08f329a`, `65fd226b` and `105c515b`; the commit message says "the one Windows probe that failed on Windows is deleted". It is not restored.

## Ruling 135 binding conditions

| Condition | Check | Existed before |
|---|---|---|
| POSIX-semantics rename and disposition | `WindowsNative_Replace_*`, `WindowsNative_ClaimDispose_HeldObserverNamespaceRemoved`, and every store save | existed (native); store form new |
| Held-reader overwrite, red then green on NTFS | the pair above, red on the closed store, green on the adapter | native pair existed; store pair new |
| NFC/NFD duplicate refusal | `Library_DuplicateIgnoringCaseNfc_Refused` (SectionLibrary normalises to NFC and compares ignoring case) | existed; it was a skip, now PASS on Windows |
| Case-sensitive-directory detection | `WindowsStore_CaseSensitiveDirectory_Refused`: the query is false on a normal directory; the refusal branch needs an elevated account to create the fixture, so here it prints `NOT ASSESSED` (Win32 5) | new; positive branch **not assessed on this laptop** |
| Crash-recovery steps the user sees | the steps are Ruling 146 (2); the dialog is the S3 follow-up (not built). The leftover inventory below is the basis | new inventory |
| Long path | `WindowsStore_LongPath_Over260_SaveOpenRoundTrip`: 536 UTF-16 units, 10 components, save, open, overwrite | new |
| Layout tests for every new structure | `WindowsNative_Layouts_SdkX64` gains `FileAttributeTag` (8), `FileStandard` (24), `CaseSensitiveInfo` (4) | extended |
| File-ID reuse and length bound sources | design section 4 cites `FILE_ID_INFO` and the maximum-path page; no durable ID cache is kept (identities are compared while handles are open) | existed (design) |

Also new: private descriptor before any byte and after publication, path admission (16 refused spellings, a missing ancestor, a directory in the leaf's
place), junction ancestor, file symbolic-link leaf and hard-link refusal with a plain control, retained-directory rename refused, owned-temp
rename, write and delete refused while held, and telemetry without a directory-flush event.

## Crash injection (Ruling 190)

`WindowsStore_CrashInjection_LeftoverInventoryMeasured` starts the test harness as a child, runs an overwrite and kills the process with
`TerminateProcess` at three points, then lists the folder. Output of the run:

```
CRASH-INVENTORY stage=Writing child_exit=-1 entries=3: .cfd-<operation>.tmp 2 bytes Archive; .cfd-writer.claim 0 bytes Archive; project.cfdw 3 bytes Archive
CRASH-INVENTORY stage=BeforePublish child_exit=-1 entries=3: .cfd-<operation>.tmp 6 bytes Archive; .cfd-writer.claim 0 bytes Archive; project.cfdw 3 bytes Archive
CRASH-INVENTORY stage=Published child_exit=-1 entries=2: .cfd-writer.claim 0 bytes Archive; project.cfdw 6 bytes Archive
```

So an unfinished save leaves, in the project's own folder and visible (Archive only, not Hidden): **`.cfd-writer.claim`** (0 bytes, always) and, when
it stopped before the rename, **one `.cfd-<operation>.tmp`** holding the partial or complete new image. The project file keeps its last saved image
(stage 1 and 2: the old bytes; stage 3: the new bytes). In all three cases the next overwrite returns `DOC-CONFLICT` and changes nothing, the saved
file stays readable, and removing the `.cfd-*` entries by hand lets the next save succeed. A crash-left claim is never reclaimed by age (design section 5).
The COPY-447 wording in `DESIGN.md` section 7 names these two kinds of file (held for the S3 dialog).

## What the un-skipped checks demanded

| Real red | Cause | Fix |
|---|---|---|
| `StoreContract_CancelBeforePublish_DocCancelled` (a seed file written by `File.WriteAllBytes`) and the retry inside the three conflict checks (a file written by another writer) | design section 5 refuses an overwrite of a file without the private DACL | **deviation**: the overwrite is admitted (the replacement is always the private temp); decision request raised |
| `LayoutSave_Conflict_*`, `Recent_Conflict_*`, `LayoutSave_TwoQueuedSaves_*` | the fixture's "external writer" wrote in place while the store held the file, which the sharing contract refuses | the fixture replaces the file by rename on Windows (`ExternalWrite`), as another instance of the app would |
| 2 mode-bit assertions (`LayoutLoad_Absent_*`, `PrefStore_TextSize_RoundTrip`) | no mode bits on Windows | assert the private owner/DACL on the file (`AssertOwnerOnly`) |
| `Library_UnprovedPlatform_RefusedNothingSaved` | injected Darwin errno 45 | inject `ERROR_NOT_SUPPORTED` (50) on Windows |
| 6 Desktop checks (`Recent_*`, `OpenFailure_RemoveFromRecent_*`, `Copy_RecentNotCleared_*`, `Telemetry_MarkerInjection_*`, `Comb_Settings_ChoiceBeforeLoadWins`) | `RecentList.Accepts` required a path starting with `/`, so no Windows path was stored | accepts a drive-letter absolute path on Windows |

## Darwin bodies byte-unchanged

`git diff 69df6257 -- src/CfdWorkbench.Persistence/ProjectStore.cs`: **5 hunks**, all dispatch or comment: one sentence in the `SaveResult` doc
comment; one `if (OperatingSystem.IsWindows()) return new WindowsProjectStore(...)` line at the top of each of `Read`, `Save` and `PublishUnderClaim`; and
`Supported()` (now `internal`, a Windows branch ahead of the unchanged Darwin condition) with the new `IsClosed()` helper. No Darwin statement changed.

## Built, not built, and open

Built: the adapter; the dispatch; `CloudFolder.Classify` (OneDrive environment paths, the four cloud attributes and the cloud reparse tags on the
folder or any ancestor, fail closed) with four Core checks including a real `FILE_ATTRIBUTE_PINNED` folder; the default folder
`%USERPROFILE%\CFD Workbench` created just before the picker opens when missing; `FolderRefusalDialog` (COPY-440, 442, 445, 454, 457) with two
Desktop checks. The native Windows picker cannot be driven from the harness, so the dialog is tested on its own.

**Open or deviating (for the Adversary and for S3):**

1. Overwrite admission deviates from design section 5 (above). Seam request `req-01M5GPKDZG8PW3RY27ATEBAM48`.
2. `DurabilityConfirmed = true` on Windows `OK` means file flush plus atomic rename plus owned cleanup (Ruling 135); the controller acknowledges a save only on it. An
   Inferred reading of "closing D-B1-DUR".
3. The volume-GUID binding of design section 4 is not built (a residual).
4. The case-sensitive positive branch ran nowhere here; CI (an elevated runner account) may exercise it.
5. Not built (follow-ups on both platforms): the S3 "Save is blocked" dialog, Clear unfinished save (Ruling 160 (2), (4)); a save in place over a file opened from a OneDrive folder is not checked;
   `Labels.SaveUnavailable` still says "Saving isn't available on this system yet" for `DOC-UNSUPPORTED-PERSISTENCE`, which a refused path now also produces.
6. Ruling 145 (6) and 146 (3) called the picker default, OneDrive placement and copy ids Mac-owned; this track did them under the plan assignment (Inferred reading).
7. CI on the Windows x64 and macOS runners is read after the join.

## Final run

`bash tools/run-tests.sh` on the final tree (exit 0; the four logs hold no `SKIP` line):

```
== CfdWorkbench.Core.Tests: 827 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Analysis.Tests: 260 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Desktop.Tests: 807 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Cli.Tests: 21 passed, 0 skipped, 0 failed, exit 0
TESTS-GREEN 90 s
```

Skip lines: 51 before (26 + 23 + 2), 0 after. `python tools/check-docs.py` exits 0.
