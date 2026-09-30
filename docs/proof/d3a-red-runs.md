---
id: proof-d3a-red-runs
title: D3a shell window red runs
type: proof-pack
status: in-review
owner: "@track-d3a"
phase: implementation
tags: [app-shell, desktop, d3a, red-first]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Recorded foreground red runs across D3a dispatches 2 and 3. The architecture
  check was exercised against a planted Dock reference outside Shell.
---

# D3a red runs — dispatch 2

## D3b continuation

All D3b commands run with `AGENT_SESSION=track-d3b AGENT_WI=D3b` in the assigned tree.

| Run | Pre-implementation state | Exit | Observed failure |
|---|---|---:|---|
| 26 | New foil tests added before the shell path existed | 1 | `ShellWindowTests.cs` CS1061: `ShellHost.OpenNewFoilAsync` absent; the test's first attempt also used an inaccessible generated `ModelArea` field and was corrected to use the named control. |
| 27 | Menu history test added before the controller exposed `CanUndo` / `CanRedo` | 1 | CS1061 for both controller properties. |
| 28 | Controller properties added, but native menu command still had unconditional `CanExecute` | 1 | `Menu_UndoEnabled_FollowsFocusAndHistory`: empty document enabled Undo or Redo. |
| 29 | Crash output test added before `StartupFailure.FailureCode` existed | 1 | CS0117 for the missing stable code. |
| 30 | Temporary `Console.Error.WriteLine(path)` in `ShellHost.OpenFileAsync` | 1 | `Telemetry_MarkerInjection_AbsentEverywhere`: marker appeared in captured stderr. Plant removed. |

After wiring the card, File menu, controller call, and Opening/Cancel outcome, the foreground `--shell-window` run exited 0 with both new Start names and `NativeMenu_MainWindow_BuiltFromTable` passing.
After binding the menu command predicates and raising `CanExecuteChanged`, the foreground `--shell-window` run exited 0 with `Menu_UndoEnabled_FollowsFocusAndHistory` passing.
With the handler changed to type and code only, the foreground `--shell-window` run exited 0 with both privacy names passing. The telemetry test observes session events, preference result metadata, and stderr; the design's separate shell and preference telemetry rings are not yet implemented in this track.

Every command below ran with `AGENT_SESSION=track-d3a AGENT_WI=D3a` in this worktree.
Each run used `dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Release -- --shell-window`.
The complete process exited after each run; no test window was left open.

| Run | Mutation or pre-implementation state | Exit | Observed failures |
|---|---|---:|---|
| 1 | Focused-target APIs absent | 1 | `Viewport_FocusVertex_RaisesFocusedTargetChanged`, `SectionCanvas_FocusVertex_RaisesFocusedTargetChanged` |
| 2 | Real `typeof(Dock.Model.Core.IDock)` field planted in `Program.cs`, outside `Shell/`; focus APIs still absent | 1 | `Architecture_DockConfinedToShell` named `src/CfdWorkbench.Desktop/Program.cs`; both focused-target names also failed |
| 3 | `ShellHost` constructor, before resource lookup fix | 1 | `ShellHost_PlanformLayout_ContainsModelAndSidePanes`: `InvalidCastException` from an unattached resource lookup |
| 4 | Start mutations: Opening hidden, Cancel left it visible, failure alert hidden, Dismiss left it visible | 1 | `Start_Opening_FocusOnCancel`, `Start_OpeningCancel_FocusReturnsToCard`, `Start_OpenMissing_AlertLocate`, `Start_OpenNewer_AlertOpenAnother`, `Start_OpenFailedDismissed_StartKept` |

The plant and Start mutations were removed. The run after focused-target implementation
printed `PASS` for both focused-target names. The run after the resource fix printed
`PASS ShellHost_PlanformLayout_ContainsModelAndSidePanes`. The Start names passed before
their deliberate mutation; a final post-revert run is recorded by the harness gate.

`NativeMenu_MainWindow_BuiltFromTable` passed on first execution with the selected menu
builder code. It has no red-first receipt in this dispatch and is not claimed as such.
The other D3a names and the reflection-bound inventory rows remain unproven here.

## Dispatch 3 red runs

Each run used `AGENT_SESSION=track-d3a AGENT_WI=D3a` and the foreground command
`dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Release -- --shell-window`.
The process ended after each run. Mutations listed here were removed after observation.

| Run | Test or mutation | Exit | Observed failure |
|---|---|---:|---|
| 5 | `MainWindow_ShellMode_ContainsDockHostAndNativeMenu` before its constructor existed | 1 | CS1739: no `shellMode` parameter |
| 6 | `NativeMenu_MainWindow_BuiltFromTable` while New foil was enabled | 1 | New foil enabled before controller seam |
| 7 | Browser `BindStations` changed to always replace | 1 | `Browser_AcceptedIdentity_KeepsOrReplacesRows`: selection refresh replaced row (inventory :1811) |
| 8 | Browser `BindStations` changed to never replace | 1 | `Browser_AcceptedIdentity_KeepsOrReplacesRows`: new identity retained row (inventory :1814) |
| 9 | Rail editor selection changed to enable numeric input for locked controls | 1 | `Controller_LockedRailControl_RefusesDraft`: locked control enabled draft input (inventory :1326) |
| 10 | Left Dock proportion changed from .25 to .9, after deferred content settled | 1 | `ModelArea_MinimumWindow_PlotWidthAtLeast250`: measured plot width 1 (inventory :1551) |
| 11 | `F6_RegionEntry_FocusesSelectedTabOrRow` before `ShellHost.MoveFocus` existed | 1 | CS1061: missing `MoveFocus` (inventory :1757a, :1806) |
| 12 | `PaneBind_Throws_ShowsErrorStateClearsOld` before pane catch paths retained the error state | 1 | `NullReferenceException` escaped Properties `Bind` |
| 13 | `EditVerbRouter` sent a focused TextBox's Undo to the document | 1 | `EditVerb_UndoInSpanField_EditsText`: field text was not undone |
| 14 | `HandleOpenOutcome(Opened)` focused the side bar toggle instead of the viewport | 1 | `Start_Opened_FocusModelArea`: model focus absent |
| 15 | Before tab focus and closed-pane lookup were added | 1 | `Focus_ClosePane_NextTab`, `Focus_WindowPanesShow_PaneTab` |
| 16 | Hide and final-close focus targets changed to the viewport; each close settled before the next | 1 | `Focus_HideDockHoldingFocus_ToToggle`, `Focus_CloseLastPane_DockToggle` |
| 17 | Before size targeted the owning dock and moved tabs regained focus | 1 | `Focus_SizeMenu_ReturnsToTab`, `Focus_MoveTo_StaysOnMovedTab` |
| 18 | Rerender focused the toggle, invalid Span hid its alert, and Browser Enter cleared the selection | 1 | `Focus_DockRerender_NeverWindowRoot`, `Focus_SpanInvalid_StaysInFieldWithAlert`, `Focus_BrowserEnter_StaysOnRowSelectsStation` |
| 19 | Document Undo focused the side bar toggle after restoring the source | 1 | `Focus_UndoFromCanvas_StaysOnCanvas` |
| 20 | `Start_Opened_FocusModelArea` opened after the Start view had rendered | 1 | `Call from invalid thread` while controller Changed bound panes off the UI thread; after marshaling, the same test failed because the viewport was not yet materialized. Bounded deferred focus made it pass. |
| 21 | Application About menu missing | 1 | `NativeMenu_Application_AboutOnly`. The first assertion expected exactly one item and revealed Avalonia's standard Services/Hide/Quit items; the final assertion permits those but requires one About and no workbench File/Edit/Window group. |
| 22 | `RefreshRecentMenu` populated no entries while the real preference file held one path | 1 | `Recent_StoredRows_StartAndFileMenu`: Start contained the stored row, File ▸ Open Recent contained only Clear Menu. Restoring the entries made both surfaces pass. |
| 23 | Removed the recent-add call from `HandleOpenOutcome(Opened)` | 1 | `Recent_OpenedOutcome_AppendsPath`: an actual `.foil` open completed but no P1 recent row appeared before the bounded 8 s deadline. Restoring the call passed. |
| 24 | Added a duplicate ⌘Z `KeyBinding` beside the native menu gesture | 1 | `KeyBindings_MenuGesture_NotBound`: duplicate binding detected. The plant was removed. |
| 25 | `Review_Persona_FocusesShellRegion` before `ShellHost.FocusForPersona` existed | 1 | CS1061: the four persona routes were absent. The shell now focuses Start Open, the viewport, a Browser row, or a rail control row. |

The first minimum-window attempt also exited 1 while Dock's deferred presenter had
not materialized. It was not counted as mutation evidence. The test now runs bounded
dispatcher jobs before measuring the rendered model area. A run with the .9 mutation
then failed on the actual 100 DIP model width; restoring .25 passed.

Dispatch 3 closed with three consecutive `tools/run-tests.sh` runs at exit 0.
Each had 274 Core and 67 Desktop PASS lines, with identical sorted PASS-set SHA-256
`65ac7c9429aca1bcb87684016a23a13f35b94c0f549379c023cb9bca6a885ab1`.
The D3a named check reached 29/40. The remaining 11 names and 52 inventory rows
are not claimed as complete by this proof. D1, D2, C1, and P1 named checks,
locked restore, token lint, documentation checks, and a foreground startup smoke
all exited 0. `pgrep -fl CfdWorkbench` found no remaining process.

## Green checks observed after removing mutations

`tools/run-tests.sh` exited 0 on three consecutive foreground runs. Each run
printed 274 Core and 44 Desktop PASS lines (the CLI harness has no named PASS
lines), and the sorted PASS set across `.tmp-tests/*.log` had 318 entries with
the same SHA-256 each time:
`d2c0e1c916033bdeb49223ff97fafb2bc672ec4635978d226268f4676cf2f3f3`.
The D3a checker found 10 of 40 required names; the remaining 30 are outstanding.
