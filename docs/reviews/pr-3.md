---
id: review-pr-3
title: "PR #3 (Windows PC) - W-1 smoke evidence, Fable owner review"
type: doc
status: done
owner: "@fable-owner"
phase: implementation
tags: [review, pull-request, windows, two-machine, w-1, smoke]
links:
  - { to: coordination-pc-kickoff, rel: depends-on }
  - { to: review-pr-2, rel: relates-to }
review-by: "2026-11-07"
summary: >-
  APPROVE WITH CONDITIONS. The W-1 receipt meets its done-when as a record and fixes nothing. Four Windows defects:
  python3 in the test ring; key gestures exist only on the macOS NativeMenu (Ctrl+Z, Ctrl+S reach nothing); the
  unsupported-persistence error goes to stderr only; a stale point automation name. The Mac fixes the shared code.
---

# PR #3 — Fable owner review

PR #3 "Record Windows W-1 smoke evidence" (origin/win/windows-smoke, reviewed head d6f84b93, merged head cb34dcee — the one
later commit adds an xmsg handoff line only; tested SHA aa642fc6, base 70c9ba53) was reviewed by the Fable owner on the Mac
on 2026-10-07 under Ruling 106. Verdict: **APPROVE WITH CONDITIONS**. The receipt `docs/proof/win-smoke/receipt.md` meets
W-1's done-when as a record and fixes nothing: the solution build passed (0 errors, 2 AVLN3001 warnings); the single Git
Bash ring stopped at `tools/run-tests.sh:49` because `python3` is the Microsoft Store alias (exit 49 = 9009 mod 256), the
one failing check is named with its first error line and every harness is honestly Not assessed; `check-docs` passed on
Windows and again on head d6f84b93 in a scratch clone on the Mac (exit 0); the app walk (default wing, spanwise drag
166.67 → 226.40 mm, Ctrl+Z, Analysis, Evaluate CL 0.177 / CDi 0.00099, Ctrl+S) is bound to one PID with screenshots and UI
Automation sidecars, and the Windows UI notes cover shortcuts, menu, title bar and DPI 144. Coordinator results are labelled
Reported; no secrets, home paths only; docs-only, so RING-SKIPPED applies under Ruling 89.

## Defects recorded (fixed by the Mac in shared code, re-verified by the PC)

| | defect | root cause | label |
|---|---|---|---|
| a | the test ring cannot start on Windows | `python3` in `tools/run-tests.sh:49`, `:141`, `:146` and `tools/join-ring.sh` resolves to the Store alias | Verified |
| b+c | Ctrl+Z and Ctrl+S do nothing | gestures exist only as `NativeMenuItem.Gesture` (`Shell/NativeMenuBuilder.cs:232`, under `NativeMenu.SetMenu` at `:224`); the window's only `KeyBindings` are F6 (`Shell/ShellHost.cs:109-118`); no `NativeMenuBar`, `HotKey` or in-window menu; `CommandTable.Bindings()` (`:175`) is consumed only by a test | Verified (source); Avalonia's no-binding-without-menu behaviour Inferred |
| c′ | the save error is never shown | `DOC-UNSUPPORTED-PERSISTENCE` is caught at `MainWindow.axaml.cs:124` and written to stderr only (`:130`) | Verified |
| d | a dragged point's automation name keeps the old value | `PlanCanvas.cs:975-984` `PointName` reads a captured `PointView` snapshot; sibling pattern at `ElevationView.cs:1310` | Inferred |

## Conditions binding later PRs

1. One tested SHA in the PR body, equal to the receipt's (not the audit commit).
2. PR #2 condition 4 (`dotnet --version` with no PATH load) carries to W-3.
3. W-2's receipt captures stderr.

PR comment: https://github.com/timianmalloo/CFD-Workbench/pull/3#issuecomment-6044054773
