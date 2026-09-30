---
id: proof-u1fix-red-runs
title: U1FIX app-shell repair proof
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: M1.2a U1FIX
tags: [app-shell, accessibility, copy, tdd]
links:
  - {to: review-app-shell-native, rel: relates-to}
  - {to: design-app-shell, rel: depends-on}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-30
summary: >-
  Red and green Desktop harness evidence for the U1FIX open-failure actions, review shell,
  accessible controls and copy. Native AX and VoiceOver review remain operator-run.
---

# U1FIX red runs

The red runs used `dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --shell-window` in the assigned worktree. Each named test printed `FAIL` with exit 1 before its product fix. The green checks printed `PASS` with exit 0 after the fix.

| Claim | Red evidence | Green test |
|---|---|---|
| Locate, Open another and Try again open the selected or original file | `/tmp/u1fix-dead-red.log`: all three `OpenFailure_Alert*Button_OpensFile` failed | `OpenFailure_AlertLocateButton_OpensFile`, `OpenFailure_AlertOpenAnotherButton_OpensFile`, `OpenFailure_AlertTryAgainButton_OpensFile` |
| Every enabled application Button in the built Start and alert states has a Click handler or Command | `/tmp/u1fix-dead-red.log`: `UI_DEAD_CONTROL_ShellButtonsHaveActions` named AlertLocateButton, AlertOpenAnotherButton, AlertTryAgainButton, AlertRemoveRecentButton, AcceptIdsButton, ResumeRecoveryButton and DiscardRecoveryButton | `UI_DEAD_CONTROL_ShellButtonsHaveActions` |
| Review mode hosts the Dock shell with the exact REVIEW title | `/tmp/u1fix-attach-red.log`: `App_ReviewMode_UsesDockShellAndExactTitle` failed | `App_ReviewMode_UsesDockShellAndExactTitle` |
| Cancel returns focus to the first Start card without an origin | `/tmp/u1fix-a3-red.log`: `Start_CancelWithoutOrigin_FocusesFirstCard` failed | `Start_CancelWithoutOrigin_FocusesFirstCard`, `Start_CancelFromRecent_FocusesOriginRow` |
| Alert band announces and focuses its first action | `/tmp/u1fix-a11y-red.log`: `AlertBand_AnnouncesAndFocusesFirstAction` failed | `AlertBand_AnnouncesAndFocusesFirstAction` |
| Invalid Span is announced and Tab can leave | `/tmp/u1fix-a11y-red.log`: `Span_Invalid_AnnouncedAndTabCanLeave` failed | `Span_Invalid_AnnouncedAndTabCanLeave` |
| Recent name is file and folder with full path in help text | `/tmp/u1fix-a11y-red.log`: `Recent_AccessibleName_FileAndFolder` failed | `Recent_AccessibleName_FileAndFolder` |
| Built open-failure copy matches DESIGN.md | `/tmp/u1fix-copy-red.log`: `Copy_OpenFailures_MatchesDesignRows` failed on COPY-129 | `Copy_OpenFailures_MatchesDesignRows` |
| Cancel status and foil-open alert band use COPY-105 and COPY-125 | `/tmp/u1fix-c1c3-red.log`: both `Copy_CancelOpening_ShowsStatus` and `Copy_OpenFailureWithFoil_AlertBandMatchesStart` failed | same named tests |
| Browser, pane and Span copy | `/tmp/u1fix-copy2-red.log`: three `Copy_*` tests failed | `Copy_BrowserEmpty_SingleRendering`, `Copy_PaneErrors_WithAndWithoutFoil`, `Copy_SpanErrors_MatchDesignRows` |
| Remove from Recent preserves the other entry | `/tmp/u1fix-remove-red.log`: `OpenFailure_RemoveFromRecent_RemovesOnlyFailedPath` failed | same named test |

Residual risks: `RecentOp` exposes only Add and Clear, so removal rebuilds at most ten entries with several writes and is not atomic. Seam request `req-01M3SG5TE62HKD3T7CPYMDXVZ3` asks P1 for `RecentOp.Remove(path)`. The app-shell AX tree and VoiceOver announcements require the operator's native attach; source and Desktop tests do not prove them.
