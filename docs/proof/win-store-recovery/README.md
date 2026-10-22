---
id: proof-win-store-recovery
title: "Windows store recovery, node A: Clear unfinished save, red then green and mutants"
type: proof-pack
summary: "Node A of the Windows store recovery build: every Clear check was red (compile) on main, then green; mutants were run in two rounds; the survivors are equivalent or defence in depth and are named."
status: active
owner: "@pc-laptop-leader"
phase: windows-w-2-build
tags: [windows, persistence, clear-unfinished-save, ruling-259, proof]
links:
  - { to: design-windows-store-recovery, rel: implements }
  - { to: rulings, rel: depends-on }
---

# Clear unfinished save (node A)

Reproduce: `dotnet build CFDWorkbench.slnx -c Release`, then
`CFD_TEST_ONLY=WindowsStore_Clear_,WindowsNative_ListNames,WindowsNative_Layouts <Core exe> --harness` and
`CFD_TEST_ONLY=SaveBlockedDialog_,MainWindow_Clear,SaveBlocked_ <Desktop exe> --harness --section-editor`.

## Red, on main (baaa97d3 plus the tests)

Compile red in Core.Tests and Desktop.Tests: `error CS0246: The type or namespace name 'ClearResult' could not be found`
(8 errors). Every named check is red by that compile error.

## Green

Core subset: 14 checks, `RESULT failures=0`: Layouts_SdkX64, ListNames_ManyEntriesAcrossBuffers_ExactSet and the 12
`WindowsStore_Clear_*` checks. Crash children: `stage=Writing|BeforePublish removed=2`, `Published removed=1`, claim last
observed at `ClearTempsDisposed`; killed at `ClearHeld` retry removes 2, at `ClearTempsDisposed` retry removes 1.
Hard link: `Failed:DOC-CONFLICT`; junction named like a temp: `Failed:DOC-UNSUPPORTED-PERSISTENCE`. Read-only candidate:
the open for DELETE succeeds and the delete fails at **C5** with Win32 5 (reviewer's probe), `Failed:DOC-IO`, nothing removed
(check `ReadOnlyCandidate_FailsAtC5Delete_DocIo_NothingRemoved`, line `CLEAR-C5-DELETE readonly=Failed:DOC-IO removed=0`).
Delete-pending candidate: the open fails at **C2** (any other open error), `Failed:DOC-IO`, nothing removed
(`DeletePendingCandidate_C2OpenError_FailedNothingRemoved`). Desktop subset: 8 `SaveBlockedDialog_*` and `MainWindow_*`
checks plus the existing `SaveBlocked_CrashLeftClaim_*`, exit 0.

## macOS

`SKIP MacStore_Clear_RemovesCrashLeftovers_ClaimLast macos-not-run-here`, likewise `MacStore_Clear_LiveWriterLockHeld_RefusedInUse`,
`MacStore_Clear_ForeignOwnerOrMode_Kept`, `MacStore_Save_ClaimAndTempLockedFromCreate`. The code is compiled, not run.
**D4 residual, not assessed:** a writer from a build before this change takes no lock on macOS, so Clear there could remove the
claim of a live older-build save. No time rule is added. The `O_EXLOCK` (0x20) value is recalled, not read from the SDK.

## Share mode

`FILE_SHARE_READ` (0x1), not 0: a reader that shares write and delete (indexer, scanner) must not show the false "another
window" message. Test `ReaderOnTemp_NotInUse`; the share-0 mutant is red. A live writer holds write and DELETE access and
shares read only, so Clear's DELETE request fails with a sharing violation on its own.

## Mutants

Killed (red): claim-first (3 checks), share-zero (6), no-second-enumeration (1), link-and-reparse-checks-dropped (1),
walk-without-local-folder-check (1), exact-name-claim-selection (1), confirm-is-default-button, focus-returns-to-save-as,
focus-on-button-while-busy, retry-reopens-dialog (timeout).

Surviving, by reason:
- share-all-on-exclusive-open: equivalent. The writer does not share DELETE, so the DELETE request conflicts whatever Clear shares.
- sharing-violation-dropped: caught later by check 2 (the held-set differs from the second enumeration, InUse); the outcome is the same.
- no-final-lookup-alone: defence in depth; no other check leaves a claim name answering.

Settled by the reviewer's runs in scratch copies of the branch (build exit 0 each): **provenance-by-name-only** is killed by 4
checks (ForeignClaim, HardLinkOrReparse, Telemetry, CaseVariantClaim); the design's pair (**exact-name selection plus no final
lookup**) is killed by 1 (CaseVariantClaim: Cleared, removed 0 where 1 was expected). The earlier "unproven" row is withdrawn.

## Repair cycle 1

- B1: the read-only test was relabelled (C5, not C2) and split from the delete-pending test, as above.
- B2, recomputed-hash mutant (the controller's expected hash recomputed from disk): before the fix it survived, because the file
  bytes did not change between the blocked save and the retry. `afterClear` now appends a byte to the project file (another
  writer) and the check also asserts the held hash differs from the file's current hash. Mutant red:
  `FAIL MainWindow_Cleared_RetriesSaveExactlyOnce_SameDestinationAndHash InvalidOperationException: the retry changed the destination or the expected-disk hash`.
  Reverted; `git diff` of `WorkbenchController.cs` is empty. The retry is blocked again by the re-created leftover in the same test,
  so the refusal is DOC-CONFLICT either way; the check proves the hash, not which refusal came first.
- N4 (any exception in `RunClearAsync` ends in A5, COPY-455 with DOC-IO, buttons enabled, focus on Cancel): the new check
  `SaveBlockedDialog_UnexpectedException_ShowsA5Copy455_NotStuckBusy` injects an `InvalidOperationException` through the dialog's
  clear seam. Red on the narrow catch: `FAIL ... TimeoutException: an unexpected exception left the dialog busy`; green with the
  catch-all; mutant (the catch removed) red with the same line, then restored.
- N7: `CancelReturnsToClear` now runs each state (confirm, in use, failed) once with Cancel and once with an Escape key event.
- N1: the `store.clear` field reuse and the `document.save` operation are stated in design section 11 and on `SessionEvent`; a
  separate `Operation` value is the follow-up (no schema change now).
- N2: the design's D3 example is corrected (a deny-DELETE ACE does not fail the open where the folder grants FILE_DELETE_CHILD); no
  DACL test is added.
- N5: the DESIGN.md COPY-441 row now carries Ruling 259's step 3 and refers to COPY-591 for the stays-blocked sentence.

## Residuals

- **N3:** a read-only **claim** that carries the private descriptor fails at C6 (the claim's delete) on every retry with
  `Failed:DOC-IO`. The temps are gone, the folder stays blocked, and Try again cannot succeed, so the user has no way forward in
  the app. Safe (I-5). Not fixed: `IGNORE_READONLY` needs a ruling.
- **N6** (the mockup's accent outline on the confirm button) is not built; a follow-up for node B or later.


# Read-only Open from OneDrive (node B)

Reproduce: `dotnet build CFDWorkbench.slnx -c Release`, then
`CFD_TEST_ONLY=WindowsStore_Open_,WindowsStore_SaveWalks,WindowsNative_Layouts <Core exe> --harness`,
`CFD_TEST_ONLY=ControllerShell_OneDriveOpen,ControllerShell_CloudOnly,OpenOutcome_ClassifyCode_CloudOnly <Desktop exe> --harness --section-editor`
and `<Cli exe> --harness`. Real files: set `CFD_ONLINE_ONLY_FILE` (an online-only file) and `CFD_LOCAL_ONEDRIVE_FILE`
(a fully local OneDrive file); the checks read attributes only and never hydrate or modify them.

## Red (Core, before the Read path changed)

9 failures, for example `WindowsStore_Open_OneDriveFullyLocal_ReadOnlyOrigin_NothingWritten ... Expected OK; actual DOC-UNSUPPORTED-PERSISTENCE`.
Two checks guard behaviour that was already right (`JunctionUnderOneDrive_Refused`, `UnqualifiedOneDriveRoot_NoException`) and were
red only through their mutants. CLI: `FAIL Cli_ExitForCode_CloudOnly_Is3 Exception: DOC-CLOUD-ONLY exits 2`. The Desktop checks and
the DR-DS-6 classification check were written after the trivial mapping existed, so their red lines are the mutants N and O below.

## Green

Core full harness, Analysis and Cli full harnesses: `RESULT failures=0`. Desktop mode `--section-editor` with the six node B checks: all PASS.
Guard prefix `--harness --theme-evidence`: exit 0. A finding fixed on the way: the change-time comparison needs the file's change
time to have left the clock tick of the seed (the check waits 100 ms).

## Mutants (each seen red, then reverted)

| mutant | red line |
|---|---|
| A bit test dropped | `Ruling260LeafPredicate_Rows: row recall-on-data-access-alone: expected DOC-CLOUD-ONLY, got DOC-CONFLICT` |
| B "no tag only" leaf rule | `Ruling260LeafPredicate_Rows: row tag-9000601A-local: expected OK, got DOC-UNSUPPORTED-PERSISTENCE` |
| D ReOpenFile (error 87) | `DataHandleFromHeldHandle_NoRecall_LocalAndOneDrive NativeFailure: ... Win32=87: The parameter is incorrect.` |
| E post-handle recall as DOC-CLOUD-ONLY | `RecallBitAfterDataHandle_RefusedConflict: Expected DOC-CONFLICT; actual DOC-CLOUD-ONLY` |
| F post-read change check dropped | `RecalledOrChangedDuringRead_RefusedNoPartial: change time case: OK` |
| G save walk passes OpenRead | `SaveWalks_PlaceholderExceptionNotApplied: Expected False; actual True` |
| H root bound by string only | `UnqualifiedOneDriveRoot_NoException: Expected False; actual True` |
| I exception without the root tag | `UnqualifiedOneDriveRoot_NoException: Expected DOC-UNSUPPORTED-PERSISTENCE; actual OK` |
| J mask loosened | `ReparseTagsUnderOneDrive_OnlyCloudFamilyAdmitted: Expected DOC-UNSUPPORTED-PERSISTENCE; actual OK` |
| K leaf exception without the OneDrive origin | `Ruling260LeafPredicate_Rows: Expected refusal DOC-UNSUPPORTED-PERSISTENCE` |
| N origin not set at Open | `ControllerShell_OneDriveOpen_InPlaceSaveWritesNothing: the project did not open read-only: origin Local` |
| O DOC-CLOUD-ONLY unmapped | `OpenOutcome_ClassifyCode_CloudOnly_And_StartViewSentence: DOC-CLOUD-ONLY is not CloudOnly: NotRecognised` |
| R CLI arm omitted | `Cli_ExitForCode_CloudOnly_Is3 Exception: DOC-CLOUD-ONLY exits 2` |

Survivor Q (the controller's own in-place guard removed): green at first, because the store refuses the same save with the same code
(`RequireLocalFolder`, unchanged). Killed in repair 1 (below).

## Repair 1 (review verdict C2, F3, F4, F7, F10, F6)

| item | red / mutant line |
|---|---|
| C2 notice row | red on the insertion-order placement: `ControllerShell_OneDriveOpen_NoticeRow_DirectlyAboveStatusStrip: the notice row 2 is not directly above the status strip row 4 (menu bar added after the notice)`. The notice's row now follows the status strip's row (a property-change handler), so the old placement is the mutant and that line is its red. |
| F3 local keeps DOC-IO | red: `WindowsStore_Open_ErrorAfterDataHandle_LocalKeepsIo_OneDriveConflict: Expected DOC-IO; actual DOC-CONFLICT`. Mutant (origin test removed from the post-data-handle catch): the same line. A OneDrive document still reports DOC-CONFLICT. |
| F4 stage recorder | the in-place direct save must visit no store stage and read no attribute. Mutant Q (controller guard removed): `InPlaceSaveWritesNothing: the direct in-place save called the store (stages and attribute reads seen): attribute ancestor,...`. (The store refuses before `ParentOpened`, so the recorder also counts the attribute reads of the parent walk.) |
| F7 change time | the hook reads `ChangeTime` before and after the write and the check fails with "precondition: the change time did not move" if it did not. |
| F10 CLI junction | renamed `Cli_Inspect_UntaggedFolderNamedByOneDrive_RefusedExit3`: the CLI cannot inject a cloud tag, so it is not a junction guard; the Core check `WindowsStore_Open_JunctionUnderOneDrive_Refused` is the junction guard. |

## Residuals held by review

- Dropping `FILE_OPEN_NO_RECALL` from the data handle (mutant S1) survives every check, including a real fully local file; only a
  free-up (dehydrate) race between the O3 bit check and the O4 open could show it. Held by review of `OpenDataFromHeld`.
- A metadata-only change inside the same clock tick as an earlier change during the read is not seen by the change-time compare.
  Content writes are excluded (the data handle shares read and delete only) and a replace is caught by the identity re-check.

## Real files

`SKIP WindowsStore_Open_RealOnlineOnlyFile_RefusedLeftCloudOnly no-online-only-file`
`SKIP WindowsStore_Open_RealFullyLocalOneDriveFile_AdmittedTagRecorded no-local-onedrive-file`
`SKIP Cli_Inspect_OneDriveFullyLocal_Succeeds no-local-onedrive-file`
When supplied, the records print `REAL-ONLINE-ONLY ...` and `REAL-LOCAL-ONEDRIVE attributes=0x.. tag=0x..` with no path
(write `%OneDrive%\...` if a path is ever quoted). The measured tags of the design (root 0x9000701A, fully local 0x9000601A,
cloud-only 0x9000401A with 0x401400) are the reviewer's, not re-measured here.

## Not done, by seam

`store.read-origin` is not emitted (it would count as a document.save in `RecordPersistence`); the notice row is placed from
`MainWindow` by inserting a shell grid row (request req-01M5JDFP0C75JZJWQ3XXP028JN, fallbacks taken). The save picker's start
folder was not driven (the native picker); it is unchanged code.
