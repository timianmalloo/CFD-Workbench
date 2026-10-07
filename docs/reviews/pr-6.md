---
id: review-pr-6
title: "PR #6 (Windows PC) - W-1b WFX re-verification evidence, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-1b, ring]
links:
  - { to: review-pr-3, rel: relates-to }
  - { to: review-pr-5, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS as a failure-evidence checkpoint. The W-1 fixes are verified on Windows; F10, Escape focus and
  the undo status text still fail. The first Windows ring's 39 failures are grouped by cause. Rulings 136 (managed
  P/Invoke Windows store) and 137 (B2 shared-file handoff) were issued with this review.
---

# PR #6 — Fable owner review

PR #6 "Record Windows WFX re-verification evidence" (origin/win/smoke-reverify, reviewed head 3effadd3, merged head
fbcc146b — later commits are a merge of main and regenerated derived views only; evidence commit a7a8e1e6, tested SHA
defbe0a9) was reviewed by the Fable owner on the Mac on 2026-10-07 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**
as a failure-evidence checkpoint — docs-only (RING-SKIPPED, Ruling 89), `check-docs` exit 0 on the head in a Mac scratch
clone, home paths only.

**Verified fixed on Windows** (against `docs/proof/wfx/pc-reverify.md`): live point automation names with name-changed
events; Undo/Redo restoration; Edit-menu Ctrl gestures with no Mac glyphs; the Save picker from Ctrl+S and File ▸ Save
with visible DOC-UNSUPPORTED-PERSISTENCE and DOC-TYPE refusals; Alt + arrow menu navigation; single-fire for
Ctrl+B/J/K/=/-/Shift+A. **Still failing (Mac, shared shell code):** F10, Escape focus return, the undo status text.
Alt+F is expected-absent (no access keys). **Not assessed, honestly:** Ctrl+1/2/3 (host ZoomIt), Tab/F6.

## The first Windows ring, grouped

| group | count | owner | cause |
|---|---|---|---|
| Windows store fail-closed | 28 | W-2 B2 (PC) | PreferenceStore, SectionLibrary, Layout, Recent and the Cli reach `ProjectStore`, fail-closed off macOS |
| Cross-platform numeric determinism | 8 | Mac | catalog regeneration vs committed bytes (`Catalog.cs:72-77`; `CAT-UNAVAILABLE` names no failing check — instrument first), the double-bit golden (`DisplayProfileTests.cs:202-213`) |
| CRLF in serialized JSON | 1 | Mac | `WriteIndented` uses `Environment.NewLine` (`LayoutCodec.cs:392`); sweep `RecentList.cs:130`, `AuthoringSession.cs:2162`, `Cli/Program.cs:11` |
| Symlink privilege | 2 | Mac (tests) | `PreferenceStoreTests.cs:140`, `:563` need SeCreateSymbolicLinkPrivilege; use a junction or an explicit NOT ASSESSED |
| Desktop exit 70, no frame | — | Mac | `Desktop/Program.cs:32` prints only the exception type; the harness should name each suite before it runs |
| Cost | 10 | Mac decision | Mac-calibrated budgets under a Cygwin `/proc/loadavg` gate; measure per check before deciding |

## Rulings issued with this review

- **Ruling 136:** the Windows store is managed P/Invoke (no native helper, no C toolchain).
- **Ruling 137:** B2 shared files — the PC edits only the platform gates of `ProjectStoreTests.cs`;
  `verify-application-core.py` and `application-core.md` stay Mac-edited; the PC supplies `tools/verify-windows-store.py`
  and its own receipt.

Ruling 134's save wording (c17e2695) postdates the tested SHA; the next Windows walk observes it.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/6#issuecomment-6048218526
