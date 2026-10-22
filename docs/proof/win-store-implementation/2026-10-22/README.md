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
| Account | a non-elevated account with Developer Mode on (file symbolic links can be created; a case-sensitive directory can be created on C: and not on D:) |

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
| NFC/NFD duplicate refusal | `Library_DuplicateIgnoringCaseNfc_Refused` evidences the **SectionLibrary name level only** (it normalises to NFC and compares ignoring case). The store-level qualification of composed/decomposed spellings and 8.3 aliases by handle identity (design section 4) is **open**; no project-store NFC/NFD check was added | existed; it was a skip, now PASS on Windows |
| Case-sensitive-directory detection | `WindowsStore_CaseSensitiveDirectory_QueryReadsFlag` (PASS: the flag reads false on a normal directory) and, split from it, `WindowsStore_CaseSensitiveDirectory_RefusalBranch`, which is registered only when the fixture can be made. Here it prints `NOT ASSESSED WindowsStore_CaseSensitiveDirectory_RefusalBranch volume-refuses-case-sensitive-flag Win32=5` and **no PASS**; the refusal was observed unelevated on this laptop with the temporary folder on C: (Windows 11 Pro 10.0.28000 ARM64, NTFS): `PASS WindowsStore_CaseSensitiveDirectory_RefusalBranch`, the Ruling 135 case-sensitive-directory observation | new; refusal **observed on C:, not assessable on D:** |
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

1. Overwrite admission: superseded by Ruling 253 (W2-Q1 b), which admits it with conditions (see "Repair cycle 2"). The first seam request `req-01M5GPKDZG8PW3RY27ATEBAM48` was resolved by the leader.
2. `DurabilityConfirmed = true` on Windows `OK` means file flush plus atomic rename plus owned cleanup. This is **Ruling 135 (1)'s own text**
   ("the Windows store flushes the temporary file (FlushFileBuffers) and then atomically renames it over the target ... Windows Save ships with it,
   closing D-B1-DUR"), Verified by the reviewer in the code; the controller acknowledges a save only on it.
3. The volume-GUID binding of design section 4 is not built (a residual).
4. The case-sensitive positive branch depends on the volume, not on elevation: unelevated (`IsInRole(Administrator) = False`) with the temporary folder on C: it prints `PASS WindowsStore_CaseSensitiveDirectory_RefusalBranch`; with it on D: (the USB fixed disk, as `run-tests.sh` sets it) it prints `NOT ASSESSED WindowsStore_CaseSensitiveDirectory_RefusalBranch volume-refuses-case-sensitive-flag Win32=5`.
5. Not built: Clear unfinished save (Ruling 160 (4)); the volume-GUID binding. (The case-sensitive refusal is observed, see item 4.) COPY-31W
   (`Labels.SaveUnavailable`, "Saving isn't available on this system yet") is **known to be false on Windows** for `DOC-UNSUPPORTED-PERSISTENCE`, which a
   refused path or file now also produces; Ruling 253 (W2-Q2) approves the replacement wording, which lands after the A3 join.
6. Ruling 145 (6) and 146 (3) called the picker default, OneDrive placement and copy ids Mac-owned; this track did them under the plan assignment (Inferred reading).
7. CI on the Windows x64 and macOS runners is read after the join.

## Repair cycle 2 (Security veto, Data and Test conditions, Ruling 253)

**The veto.** A normal .NET process runs with OneDrive placeholders disguised (mode 1), so the reparse attribute and the cloud tag of a OneDrive
folder are hidden: the branch CLI read a missing file under `%OneDrive%` as `DOC-CONFLICT` (the walk admitted it) while a process that exposes
placeholders got `DOC-UNSUPPORTED-PERSISTENCE`. Fix: `RtlSetThreadPlaceholderCompatibilityMode(PHCM_EXPOSE_PLACEHOLDERS = 2)` (ntdll, ntifs.h) around
`Read`, `Save`, `PublishUnderClaim` and `CloudFolder.Classify`, restored in a `finally`; the store refuses a OneDrive-rooted or cloud-tagged folder
before any write; `MainWindow` checks an in-place save like a pick. Because the ancestor rule rejects every reparse point, a read under a OneDrive
folder is refused too (the instruction said a read stays permissive; the CLI line it asked for cannot be had otherwise, flagged for the Owner).

| Check | Red on `c93f9cff` | Green |
|---|---|---|
| `WindowsStore_PlaceholderMode_ExposedInSaveWalkAndCloudFolder` | `FAIL ... InvalidOperationException: Expected True; actual False` | `PLACEHOLDER-MODE thread_before=0 save_stages=7 all_exposed=true cloud_reads=6 all_exposed=true restored=true` / `PASS`. Mutants: restore skipped -> `FAIL ... Expected 0; actual 2`; set removed -> `FAIL ... Expected True; actual False` |
| `WindowsStore_Save_UnderEnvironmentOneDriveFolder_RefusedNothingWritten` | `FAIL ... Expected DOC-UNSUPPORTED-PERSISTENCE; actual OK` | `PASS` (no claim, temp or write stage; one entry; bytes unchanged) |
| `SaveInPlace_UnderEnvironmentOneDrive_RefusedNothingWritten` (Desktop, through `WorkbenchController.SaveAsync(NativePath)`) | `FAIL ... in-place save under OneDrive: OK` | `PASS` |
| `Cli_InspectUnderOneDrive_RefusedNotConflict` | `CLI-ONEDRIVE exit=5 { "code": "DOC-CONFLICT" }` / `FAIL` | `CLI-ONEDRIVE exit=3 { "code": "DOC-UNSUPPORTED-PERSISTENCE" }` / `PASS` |

Laptop line, the real variable (read only, nothing written): `CfdWorkbench.Cli.exe inspect "<OneDrive variable>\no-such-w2.cfdw.json" --json` -> `{ "code": "DOC-UNSUPPORTED-PERSISTENCE" }`.
The picker and `MainWindow`'s in-place branch call `CloudFolder.Classify` and `FolderRefusalDialog`; the native picker is not driven by the harness.

**D1, fixed local volumes.** `WindowsNative.IsFixedLocalVolume(deviceType, characteristics)` is a pure function over `FILE_FS_DEVICE_INFORMATION`
read from the held root handle (`NtQueryVolumeInformationFile`, class 4): `FILE_DEVICE_DISK` (0x7) without `FILE_REMOVABLE_MEDIA` (0x1), `FILE_FLOPPY_DISKETTE`
(0x4), `FILE_REMOTE_DEVICE` (0x10), `FILE_CHARACTERISTIC_TS_DEVICE` (0x1000) or `FILE_CHARACTERISTIC_WEBDAV_DEVICE` (0x2000). Red: the function did not exist
(`error CS0117: 'WindowsNative' does not contain a definition for 'IsFixedLocalVolume'`; run on the old code with the body stubbed: `FAIL ... no WindowsNative.IsFixedLocalVolume on the old code`).
Green: `VOLUME-DEVICE local_ntfs type=0x7 characteristics=0x260020 admitted=true`, remote (0x14, 0x10), network type alone, remote flag alone, removable, floppy, TS and
WebDAV refused. **USB-attached fixed disks are admitted:** this laptop's D: is BusType USB, DriveType Fixed, characteristics 0x260020 (no 0x1), and every store fixture ran on it. Removable media (0x1) are refused. **Real SMB shares and removable USB sticks: Not assessed.**

**Overwrite of a target without the private DACL (Ruling 253, W2-Q1 b).** `WindowsStore_ForeignDaclTarget_Overwrite` expects success and a private DACL on the result:
`PASS`. Red first, both refused before the claim with `DOC-UNSUPPORTED-PERSISTENCE`: `WindowsStore_ReadOnlyTarget_OverwriteRefused` (`FAIL ... actual DOC-SAVE-UNCERTAIN`) and
`WindowsStore_EfsTarget_OverwriteRefused` (`FAIL ... actual OK`; EFS could be set with `File.Encrypt` on this machine; where it cannot the check is not registered and a
`NOT ASSESSED ... efs-unavailable` line is printed, never a PASS). Design section 5 is amended by the ruling (erratum in the design). The seven checks were not reseeded:
with the overwrite admitted they are valid as they stand (the ruling makes the reseeding the instruction asked for unnecessary).

**"Save is blocked" (Ruling 253, W2-Q3 c).** `SaveBlockedDialog` (COPY-441; Save As COPY-443; Cancel COPY-445; the Ruling 146 (2) text with step 3 reworded; a sentence that the folder stays
blocked until the leftover files are removed; no Clear action). `MainWindow.SaveToAsync` shows it when a save returns `DOC-CONFLICT` and `UnfinishedSave.BlocksFolder` sees the claim.
`SaveBlocked_CrashLeftClaim_DialogShownInsteadOfInUseLine` builds the measured inventory (a 0-byte `.cfd-writer.claim` and a `.cfd-<operation>.tmp` beside the project), saves, and reads the
dialog from the window's owned windows: red with the wiring removed (`FAIL ... TimeoutException: the Save is blocked dialog did not appear`), green after. The controller's own
status line (the generic COPY-414 sentence) is still set by `WorkbenchController` behind the dialog; that file is outside this track.

**Test Architect conditions.** T1: the case-sensitive check is split (above); the output holds `PASS WindowsStore_CaseSensitiveDirectory_QueryReadsFlag` and
`NOT ASSESSED WindowsStore_CaseSensitiveDirectory_RefusalBranch volume-refuses-case-sensitive-flag Win32=5`, and no PASS for the refusal. T3: the NFC/NFD row, the D-B1-DUR citation and these run lines are fixed.
The 25 non-Core former skips: the final `run-tests.sh` below reports Desktop 809 passed / 0 skipped and Cli 22 passed / 0 skipped; the baseline skip counts were 23 and 2.

**Directory privacy (wave 2, track C1; fixed).** The gap measured in wave 1 was `PREF-DIR-ACL protected=False aces=8 inherited=8 private_policy=False`: `EnsureDir`
made the preference folder with `System.IO`, so it inherited the profile's DACL while the files inside were private. Now `AssertOwnerOnly(dir, true)` asserts the private
policy on Windows (protected DACL, owner the current user, one allow ACE for the current user, no inherited ACE) and `EnsureDir` calls
`WindowsNative.EnsurePrivateDirectory`:
- a new folder is created by `CreateDirectoryW` with a `SECURITY_ATTRIBUTES` holding the private descriptor (the file descriptor with the ACE marked OICI, so a file
  that inherits from the folder keeps its access when the folder DACL is propagated);
- an existing folder (made by an earlier build, inheriting) that the current user owns is set to the protected private DACL with `SetSecurityInfo` on the next save;
- a folder owned by another account is left unchanged and the store is session-only for that area (the `Session()` outcome that `DOC-UNSUPPORTED-PERSISTENCE` already maps to).

Red on the old code (this machine, `Core.Tests --harness`, `CFD_TEST_ONLY=PrefStore_`):
`PREF-DIR-ACL protected=False aces=3 inherited=3 private_policy=False` then `FAIL PrefStore_TextSize_RoundTrip ... Expected True; actual False` and
`FAIL PrefStore_ExistingInheritedFolder_BecomesPrivateOnSave ... Expected True; actual False`. Green on the new code, full Core harness `RESULT failures=0`, with
`PREF-DIR-ACL protected=True aces=1 inherited=0 private_policy=True`, `PASS PrefStore_ExistingInheritedFolder_BecomesPrivateOnSave`,
`PASS PrefStore_ForeignOwnedFolder_LeftUnchanged` (the OS-owned system folder: returns false, owner and DACL string identical before and after) and
`PASS PrefStore_NewFolder_PrivateAtCreation`. The `SECURITY_ATTRIBUTES` layout is checked in `WindowsNative_Layouts_SdkX64` (size 24; offsets 0, 8, 16).
Mutants, each applied, run, red, reverted: creation-time descriptor dropped (`CreateDirectoryW(path, null)`) gives
`FAIL PrefStore_NewFolder_PrivateAtCreation ... Expected True; actual False`; existing-folder tighten skipped gives
`FAIL PrefStore_ExistingInheritedFolder_BecomesPrivateOnSave ... Expected True; actual False`.

Security confirmation (review `w2b-c1/review/verdict.md`, Security lens): tightening an existing user-owned preference folder in place is in scope. It only narrows access, runs before any write, changes the folder's DACL only (owner and SACL untouched) and needs no extra privilege (the owner's implicit WRITE_DAC). Design section 5's "no post-write ACL repair" covers project files, which are still created with their own protected descriptor before any byte is written, not the preference folder. A foreign-owned folder is left untouched and that area falls back to session-only; no junction is followed (both opens use `FILE_FLAG_OPEN_REPARSE_POINT`).
Advisory (Inferred; not measured): a folder made by an earlier elevated run may have BUILTIN\Administrators as its owner, so that area would stay session-only on every launch.

Review condition T1: `AssertPrivateDirectory` also asserts the single ACE is an allow ACE (winnt.h `ACCESS_ALLOWED_ACE_TYPE` 0x0) granting exactly `FILE_ALL_ACCESS` (0x1F01FF) with flags exactly `OBJECT_INHERIT_ACE | CONTAINER_INHERIT_ACE` (0x1 | 0x2). Two descriptor mutants, each applied, run, red, reverted: a narrower grant (`0x1201BF` for `FA`) gives `FAIL PrefStore_NewFolder_PrivateAtCreation InvalidOperationException: Expected 2032127; actual 1180095`; the OI and CI flags dropped gives `FAIL PrefStore_NewFolder_PrivateAtCreation InvalidOperationException: Expected ObjectInherit, ContainerInherit; actual None`. Each is also red in `PrefStore_TextSize_RoundTrip` and `PrefStore_ExistingInheritedFolder_BecomesPrivateOnSave` (`RESULT failures=3`); the reverted tree gives `RESULT failures=0`.

NOT ASSESSED: the store-level session-only fallback (`if (!EnsureDir(...)) { ...Session = true; return Session(); }`, three call sites in `PreferenceStore.cs`: layout, recent and display) is not reached by any test, so a mutant that ignores the false return survives. A non-elevated user cannot make a `layout`, `recent` or `display` folder owned by another principal (giving a folder away needs a privilege), and the store has no seam to inject the failure; none was added for the test. `PrefStore_ForeignOwnedFolder_LeftUnchanged` covers the callee (`EnsurePrivateDirectory` returns false and leaves the system folder unchanged), not these callers.

## Final run

`bash tools/run-tests.sh` on the final tree (exit 0; the four logs hold no `SKIP` line):

```
== CfdWorkbench.Core.Tests: 833 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Analysis.Tests: 260 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Desktop.Tests: 809 passed, 0 skipped, 0 failed, exit 0
== CfdWorkbench.Cli.Tests: 22 passed, 0 skipped, 0 failed, exit 0
TESTS-GREEN 105 s
```

Skip lines: 51 before (26 + 23 + 2), 0 after. `python tools/check-docs.py` exits 0.

## W2-Q2 follow-up (2026-10-22, Rulings 253 and 254)

- **COPY-31W false on Windows: closed.** COPY-589 ("CFD Workbench can't save to this location or over this file. Your changes are kept in this session. Use Save As to choose a local folder.", Ruling 253 W2-Q2 a) is what `Labels.SaveRefusal("DOC-UNSUPPORTED-PERSISTENCE")` returns on Windows; COPY-31W stays on the other systems. Check `Labels_SaveRefusal_Ruling134Wording`: red on the old mapping (`FAIL ... expected CFD Workbench can't save to this location ... actual Saving isn't available on this system yet`), green after; the platform branch inverted turns it red.
- **Open refusal (Ruling 254): finding, no copy written.** `OpenRefusal_OneDriveFolder_StoreRefuses_MessageRecorded` opens a project in an environment-designated OneDrive folder through `ProjectStore.ReadAsync`, `OpenFailure.Classify` and `StartView.FailureMessage`. The store refuses with `DOC-UNSUPPORTED-PERSISTENCE`; the user sees "It isn't a foil or project file that CFD Workbench can read, or it is damaged. The file hasn't been changed.", then "Code: DOC-UNSUPPORTED-PERSISTENCE". It states **neither the cause (OneDrive) nor a remedy (move or copy the project to a local folder)**, and "or it is damaged" is misleading. The leader files the copy question; the check asserts the refusal and prints the cause and remedy flags (`NOTE OpenRefusal_OneDrive cause=False remedy=False`).
- **Open refusal closed (Ruling 257): COPY-590.** `OpenFailure.ClassifyCode` maps `DOC-UNSUPPORTED-PERSISTENCE` to `UnsupportedLocation` on Windows only; `StartView` shows `Labels.OpenUnavailableWindows`; the other systems keep the not-recognised sentence. Check `OpenRefusal_OneDriveFolder_StoreRefuses_MessageRecorded` (cause OneDrive, remedy "copy the project to a local folder", no "damaged"; SKIP off Windows): red `FAIL OpenRefusal_OneDriveFolder_StoreRefuses_MessageRecorded InvalidOperationException: cause=False remedy=False damaged=True ...`, green `PASS OpenRefusal_OneDriveFolder_StoreRefuses_MessageRecorded`; the platform branch inverted turns it red again.
- **A3 condition 11: `inspect --runs` through `CfdWorkbench.Cli.exe` on Windows**, on a project saved through the Windows store. No records: members are exactly `schemaVersion,runs,pruned` (`InspectRuns_WindowsStore_BinaryNoRecords_OutputUnchanged`). One record: the `discrepancies` member is listed (`..._BinaryOneRecord_ListsDiscrepancies`). Mutants: the member dropped turns the second red; the member always emitted turns the first red.
