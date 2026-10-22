---
id: proof-win-store-recovery
title: "Windows store recovery, node A: Clear unfinished save, red then green and mutants"
type: proof-pack
summary: "Node A of the Windows store recovery build: every Clear check was red (compile) on main, then green; 14 mutants were run, 10 killed, 4 survive or are unproven and are named."
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
Hard link: `Failed:DOC-CONFLICT`; junction named like a temp: `Failed:DOC-UNSUPPORTED-PERSISTENCE`. Read-only candidate and
delete-pending candidate: both `Failed:DOC-IO`, nothing removed. Desktop subset: 8 `SaveBlockedDialog_*` and `MainWindow_*`
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
- no-final-lookup-alone: defence in depth; no other check leaves a claim name answering. The design's pair (exact-name selection plus
  no final lookup) was not run: the script mutated only the lookup.
- provenance-by-name-only: **unproven**. The mutant build failed (warning as error), so its red lines came from a stale binary.
  The foreign-claim check passes on the real code and was not shown red against this mutant.

Two Desktop mutants of the design table were not run (a retry loop with a recomputed hash).
