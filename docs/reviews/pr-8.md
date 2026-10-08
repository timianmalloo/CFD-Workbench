---
id: review-pr-8
title: "PR #8 (Windows PC) - W-2 B2 preserved qualification failure checkpoint, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-2, persistence, privacy]
links:
  - { to: review-pr-6, rel: relates-to }
  - { to: design-windows-native-store, rel: relates-to }
review-by: "2026-11-08"
summary: >-
  APPROVE WITH CONDITIONS (Ruling 145). The B2 track stopped at its repair cap on a ShareRead-only held-reader probe,
  which is expected Windows sharing semantics; a fresh bounded track is approved, NtSetInformationFile class 65 is
  affirmed, ProjectStore stays fail-closed, and the machine SID was redacted before merge.
---

# PR #8 — Fable owner review

PR #8 "W-2: preserve blocked Windows store qualification" (origin/win/store-b2, reviewed head c6250218, merged head
dfbef2fc — the later commits are the redaction 4f75a775 and a merge of main) was reviewed by the Fable owner on the Mac
on 2026-10-08 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**, issued as **Ruling 145**.

## Findings

1. **Expected semantics.** The failing `WindowsNative_Replace_HeldReaderKeepsOldImage` opens its reader with `ShareRead`
   only (`WindowsProjectStoreTests.cs:61`); NTSTATUS 0xC0000043 → Win32 32 is STATUS_SHARING_VIOLATION. Replacement needs
   delete sharing on every existing handle, as the design already says (`windows-native-store.md:339-341`). The split into
   a compatible-reader overwrite and an incompatible-reader deterministic refusal is the Ruling 135 contract.
2. **The product's own readers.** `ProjectStore` never holds the project file across a save (`ProjectStore.cs:77-80`,
   `:109-132`). Flagged for the Mac: `ShellHost.cs:1402` (`File.ReadAllBytesAsync`) and `Cli/Program.cs:26` open
   ShareRead-only, so a concurrent second instance or CLI read would make a Windows save fail with Win32 32. That code
   must map to a stable product code, never DOC-IO.
3. **The rename primitive.** NtSetInformationFile with FileRenameInformationEx (class 65) is affirmed; Ruling 136 names
   ntdll. The struct layout and flags in `WindowsNative.cs:79-85` match. The class-22 Win32 87 is inferred to be kernelbase
   validation of the rooted form; one NULL-root class-22 and rooted class-3 probe attributes it in the fresh track.
4. **Lease.** Only leased paths changed: the `ProjectStoreTests.cs:12` gate, the new `WindowsProjectStoreTests.cs`,
   `WindowsNative.cs` (`[SupportedOSPlatform("windows")]`, unreferenced) and the csproj `AllowUnsafeBlocks` line. macOS
   behaviour is unchanged and `ProjectStore` stays fail-closed off macOS.
5. **Privacy.** A machine SID in `red-native.stdout.txt` and the Windows home path in `checkpoint.md` were redacted by
   the PC (4f75a775) before merge. The operator then chose to scrub the Windows user name from the tree and add a guard
   (class PROOF-PII, track trk-pii); history is not rewritten.

## Join note

The join's check-docs stopped on STORE-SUBSET: the Windows-only test file sat outside the macOS umask partition. It
runs only on Windows (`ProjectStoreTests.cs:12`), so the Mac exempted it from the partition in `tools/store_subset.py`
(Mac-owned under Ruling 137) on top of the merge; the ring then passed (net 41.4 s, 0 failures).

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/8#issuecomment-6062999809
