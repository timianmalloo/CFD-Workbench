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
  Recorded foreground red runs for the D3a subset landed in this dispatch. The
  architecture check was exercised against a planted Dock reference outside Shell.
---

# D3a red runs — dispatch 2

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

## Green checks observed after removing mutations

`tools/run-tests.sh` exited 0 on three consecutive foreground runs. Each run
printed 274 Core and 44 Desktop PASS lines (the CLI harness has no named PASS
lines), and the sorted PASS set across `.tmp-tests/*.log` had 318 entries with
the same SHA-256 each time:
`d2c0e1c916033bdeb49223ff97fafb2bc672ec4635978d226268f4676cf2f3f3`.
The D3a checker found 10 of 40 required names; the remaining 30 are outstanding.
