---
id: proof-m12b-u2-red-runs
title: "M1.2b U2 red runs"
type: proof-pack
status: active
owner: "@track-u2"
phase: implementation
tags: [m12b, u2, desktop, panes, red-first]
links:
  - { to: design-m12b-points, rel: depends-on }
  - { to: coordination-m12b-build, rel: implements }
review-by: "2026-10-30"
summary: >-
  Records the foreground red run of all 25 named U2 pane checks before the Properties, Browser,
  command, and retirement implementation. The dead-control mutant run is appended when it is executed.
---

# U2 red-run receipt

## Named pane checks before implementation

Command (repo root, `AGENT_SESSION=track-u2 AGENT_WI=U2`):

`dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --shell-window`

Log: `/tmp/u2-shell-window-red.log`. Exit: **1**. The shell-window harness printed these 25 failures. The missing rendered controls, command rows, and retirement tokens are the oracle.

```
FAIL Properties_ControlPoint_TypeSpanAftRows InvalidOperationException: missing PointBlock
FAIL Properties_NamedPoint_TypeReadOnlyWithConstraint InvalidOperationException: missing TypeReadOnly
FAIL Properties_TypeToAnchor_OneUndoStepCurvePassesThrough InvalidOperationException: missing TypeControl
FAIL Properties_TangentSymmetric_OneUndoStep InvalidOperationException: missing TangentSymmetricButton
FAIL Properties_TypedSpanAftExpression_CommitsAsOneGestureEchoed InvalidOperationException: missing PointSpanInput
FAIL Properties_HandleAngleLength_TypedCommitsOneStep InvalidOperationException: missing HandleAngleInput
FAIL Properties_MultiplePoints_MixedReadOnly InvalidOperationException: missing PointHeading
FAIL Properties_TipCloses_TipChordIsText InvalidOperationException: missing TipClosedText
FAIL WingBlock_AllCad17Rows_InOrderWithApprox InvalidOperationException: wing order: Span | Root chord | Tip chord
FAIL WingBlock_DuringDrag_RenderedMacTextChangesBeforeRelease InvalidOperationException: missing MacText
FAIL WingBlock_CrossingDraft_ShowsDashAndReason InvalidOperationException: missing MacText
FAIL WingRootChord_FitAboveLimit_WarningShownAndCommitted InvalidOperationException: missing RootChordInput
FAIL WingTipChord_CentimetresAndReference_EchoedMm InvalidOperationException: missing TipChordInput
FAIL Focus_RootChordCommitTab_NextField InvalidOperationException: missing RootChordInput
FAIL Focus_TypeChange_StaysOnTypeControl InvalidOperationException: missing TypeControl
FAIL Focus_ReturnOnPoint_SpanFieldEscapeBack InvalidOperationException: missing PointSpanInput
FAIL Focus_TypeValueRequest_SpanFieldFocused InvalidOperationException: FocusTypeValue missing
FAIL StatusLine_CommitReport_PoliteLiveRegion InvalidOperationException: status '' controller 'Sampling accepted geometry at η 0.5…'
FAIL Copy_M12bOutcomes_ExactStrings InvalidOperationException: missing PointHelper
FAIL Browser_RailGroups_SelectPointOnCanvas InvalidOperationException: missing LeadingEdgeList
FAIL ContextMenu_MakeAnchor_SameEffectAsProperties InvalidOperationException: missing TypeControl
FAIL CommandTable_PointRows_ExecuteOrDisabled InvalidOperationException: command row missing: point.make-anchor
FAIL UI_DEAD_CONTROL_PointAndWingControlsHaveActions InvalidOperationException: missing HowMeasuredButton
FAIL Recovery_RailDraftResumed_PlanShowsDraftApplyCommits InvalidOperationException: opened project has no recovery: Open Example or a .foil / .cfdw.json file.
FAIL RailEditorPane_Removed_NoReferencesRemain InvalidOperationException: src/CfdWorkbench.Desktop/MainWindow.axaml.cs; src/CfdWorkbench.Desktop/WorkbenchController.cs; src/CfdWorkbench.Desktop/Program.cs; src/CfdWorkbench.Core/FoilSource.cs; src/CfdWorkbench.Core/AuthoringSession.cs; src/CfdWorkbench.Desktop/Shell/ShellHost.cs; src/CfdWorkbench.Desktop/Panes/RailEditorPane.axaml.cs; src/CfdWorkbench.Desktop/Panes/RailEditorPane.axaml; tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs; tests/CfdWorkbench.Core.Tests/DimensionTests.cs; tests/CfdWorkbench.Core.Tests/ProjectStoreTests.cs; tests/CfdWorkbench.Core.Tests/AuthoringSessionTests.cs
```

The same log also printed five recent-store failures (`Recent_StoredRows_StartAndFileMenu`, `Recent_OpenedOutcome_AppendsPath`, `Telemetry_MarkerInjection_AbsentEverywhere`, `Copy_RecentNotCleared_StatusAndTryAgain`, `OpenFailure_RemoveFromRecent_RemovesOnlyFailedPath`) before any U2 check. They report `DOC-UNSUPPORTED-PERSISTENCE` on the OS temp root. This command did not set `TMPDIR` to the non-symlinked scratch that `tools/run-tests.sh` uses. They are not U2 names.

## Dead-control mutant

Not run yet. The exit evidence requires one Properties button handler removed, the dead-control check red, then the handler restored. That run is appended here before the track closes.
