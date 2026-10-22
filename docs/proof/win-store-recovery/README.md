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
