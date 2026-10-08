---
id: receipt-windows-store-implementation
title: Frozen B2 native qualification failure receipt
type: doc
status: in-review
owner: "@windows-worker"
phase: implementation
tags: [windows, persistence, proof, blocked]
links:
  - { to: proof-windows-store-implementation, rel: depends-on }
  - { to: design-windows-native-store, rel: depends-on }
review-by: "2026-11-08"
summary: >-
  Native qualification failure checkpoint. Two repairs exhausted; one held-reader
  probe still fails. Production admission is false; no full ring or later gate ran.
---

# B2 failure receipt

Base: `fd96651e6dd3ad9c01e4fbaa07c62a11fa553c09`, branch `win/store-b2` in its
assigned isolated worktree. Fetch/merge completed before work; already up to date.
The commit SHA is supplied in the coordinator handback to avoid a self-referential
SHA field. Source and tests were frozen immediately after cycle 2; subsequent
changes are documentation and raw evidence only.

[Checkpoint](checkpoint.md) contains goal/frontmatter, surface list, test-trigger
union, failure/threat matrix, exact native contracts, all observed RED/GREEN results,
source citations, class/control handoff and the exact fresh-track approval request.

Latest targeted run: `CFD_TEST_ONLY=WindowsNative_,WindowsStore_`, pinned user-local
.NET 10.0.203, Release Core harness with build worker cap4. **Exit1; selected9;
eight PASS, one FAIL.** See [stdout](cycle-2.stdout.txt) and [stderr](cycle-2.stderr.txt).
The failure is preserved: `WindowsNative_Replace_HeldReaderKeepsOldImage`, using a
ShareRead-only handle, receives NTSTATUS `0xc0000043`, IO status0, Win32 `32`.
The approved ShareRead|ShareDelete overwrite is **Not assessed**, not failed or passed.

Earlier raw outputs are `sdk-resolution`, `red-store`, `red-native`, `cycle-1`, each
with separate `.stdout.txt` and `.stderr.txt`. The ACL failure retains its descriptor
structure, native errors and assertion result.
Ruling 145 proof redactions: machine SIDs use `S-1-5-21-<machine>-<RID>` and user paths use `%USERPROFILE%`.

Executable repair count **2/2**, track **STOPPED/BLOCKED** by Owner disposition.
The failure checkpoint/design erratum alone is authorized to commit. No further
source/test mutation or execution, full ring, readiness, docs validation, push or
PR occurred after that disposition. This is a code-bearing qualification prototype,
not a working/admitted Windows project store. No independent veto has been cleared.

Production paths changed: new `WindowsNative.cs` and Persistence csproj's single
`AllowUnsafeBlocks` property. `ProjectStore.cs` remains unchanged/fail-closed.
Test paths changed: new `WindowsProjectStoreTests.cs` and only the first authorized
platform gate in `ProjectStoreTests.cs`. All Darwin case bodies and native build
logic remain unchanged. Missing adapter/tool, full native case set, shared UI and
serialized ring qualification are enumerated in the checkpoint.
