---
id: review-pr-5
title: "PR #5 (Windows PC) - Windows native project store design, Fable owner and data-persistence review"
type: doc
status: done
owner: "@fable-owner"
phase: design
tags: [review, pull-request, windows, two-machine, w-2, persistence]
links:
  - { to: coordination-pc-kickoff, rel: depends-on }
  - { to: review-pr-4, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS (Fable owner) and CLEAR WITH CONDITIONS (data-persistence architect, veto cleared for the design
  only). Rulings 135 (durability, OneDrive) and 134 (save-failure wording) settle the operator items; B2 is bound by the
  conditions below.
---

# PR #5 — review

PR #5 "Design the Windows native project store" (origin/win/windows-store-design, head 1d4988b4, design unchanged since
tested SHA 94c0b164) was reviewed on the Mac on 2026-10-07 under Ruling 106 by the Fable owner (**APPROVE WITH
CONDITIONS**) and the data-persistence architect (**CLEAR WITH CONDITIONS**; its hard veto clears for B1, the design, only).
The PR is docs-only; the new helper is `cfd_store_windows.c` with `cfd_store.c` excluded; the Ruling 121 `units` key is
kept; every native claim is labelled Sourced (Microsoft Learn) or Inferred; the test plan is red-first on Windows with the
macOS store cases kept unchanged. The design rejects ReplaceFileW and path-only MoveFileExW, renames by handle relative
to the held parent, flushes before publishing, refuses reparse tags and uses the 128-bit FILE_ID_INFO.

## Operator rulings this review needed

- **Ruling 135:** Windows Save ships with file flush plus atomic POSIX-semantics rename (no directory flush exists on
  Windows; the previous file stays intact; one save may be lost on power loss in the instant after Save). Default save
  folder is local, outside OneDrive; OneDrive folders are refused with a plain message (wording to be drafted as proposed
  copy).
- **Ruling 134:** foil save-failure wording.

## Conditions binding B2 (the implementation)

1. **[Major]** Overwrite with open handles: use `FileRenameInfoEx` with REPLACE_IF_EXISTS | POSIX_SEMANTICS (plain
   `FileRenameInfo` fails while a reader holds the file, so parity with `Store_Overwrite_ExactTokenAndHeldReader` is
   impossible); state the minimum Windows 10 build in the supported subset; the held-reader test red then green on NTFS.
2. **[Major]** Durability per Ruling 135; record that MOVEFILE_WRITE_THROUGH only applies to copy-and-delete moves.
3. Claim cleanup with `FileDispositionInfoEx` (POSIX), tested with an extra handle held on the claim (Inferred; source it).
4. OneDrive and antivirus: what the user sees, with fixtures; the OneDrive refusal wording as proposed copy.
5. NFC/NFD duplicate refusal tested on Windows; detect case-sensitive directories (FileCaseSensitiveInfo).
6. Crash recovery: the steps the user sees for a left-over claim, and cleanup of `.cfd-*.tmp`, tested.
7. Cite file-ID reuse and the 32,768-unit length bound; label the 256-component bound a design choice; one test over 260
   characters.
8. B2 leases: the four Persistence paths are approved; `ProjectStoreTests.cs`, `tools/verify-application-core.py` and
   `docs/proof/application-core.md` each need a handoff answered by the Mac first; the csproj lease needs the compiler/SDK
   decision the design leaves open.
9. Erratum: TraceId is operation-scoped (`ProjectStore.cs:58/:67`); fixed in the canonical design.

W-2's done-when ("the friend can save and reopen on Windows") is reachable under Ruling 135.
