---
id: proof-d2-red-runs
title: D2 controller red-first runs
type: proof-pack
status: in-review
owner: "@track-d2"
phase: implementation
tags: [app-shell, desktop, controller, named-tests, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: Red-first run for track D2 (Controller) of the app-shell build — proves that the spawned --controller-shell suite fails and turns tools/run-tests.sh red with exit code 1, including Open_CancelDuringPrepare_CurrentFoilUnchanged red against the old controller.
---

# D2 Controller: red-first run

Track D2 implements the controller, selection model, Properties projection, and the Open flow with its outcome union (`docs/design/app-shell.md` §3.6–§3.8, §5.1, §6.1–§6.3, §9, §12.4, §14).
This proof pack verifies that the spawned `--controller-shell` suite makes `tools/run-tests.sh` exit nonzero when any named check fails, and captures the old-controller red evidence for `Open_CancelDuringPrepare_CurrentFoilUnchanged`.

## 1. Red Run: `tools/run-tests.sh` exits 1

The 21 named checks were added to `tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs`.
With today's `WorkbenchController` (where `OpenAsync` delegated to the as-built `OpenPathAsync` and stubs threw `NotImplementedException`), running `tools/run-tests.sh` fails with exit code 1.

```text
$ tools/run-tests.sh
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.97
build 2 s (Release)
== CfdWorkbench.Core.Tests 28 s, 250 PASS
RESULT failures=0
== CfdWorkbench.Cli.Tests 1 s, 0 PASS
CLI Example identity, assessment, bounded projection and extension refusal passed.
== CfdWorkbench.Desktop.Tests 27 s, 16 PASS
FAIL ApplySpan_EdgesCross_Refused NotImplementedException: The method or operation is not implemented.
FAIL ApplySpan_NonPositive_Refused NotImplementedException: The method or operation is not implemented.
FAIL ApplySpan_NotANumber_Refused NotImplementedException: The method or operation is not implemented.
FAIL OpenFailure_AccessDenied_Classified InvalidOperationException: Expected AccessDenied, got Missing
FAIL OpenFailure_Newer_Classified InvalidOperationException: Expected Newer, got Missing
FAIL OpenFailure_NotRecognised_Classified InvalidOperationException: Expected NotRecognised for DOC-SCHEMA, got Missing
FAIL OpenFailure_TooLarge_Classified InvalidOperationException: Expected TooLarge for DOC-SIZE, got Missing
FAIL OpenFailure_UnknownContent_Classified InvalidOperationException: Expected UnknownContent, got Missing
FAIL OpenFailure_Unreadable_Classified InvalidOperationException: Expected Unreadable for IOException, got Missing
FAIL Open_CancelAfterCommit_Ignored InvalidOperationException: Foil was discarded after cancel-after-commit.
FAIL Open_CancelDuringPrepare_CurrentFoilUnchanged TaskCanceledException: A task was canceled.
FAIL Open_IdCandidate_NeedsIds InvalidOperationException: Expected NeedsIds outcome, got Opened
FAIL Open_SecondRequest_SupersedesFirst InvalidOperationException: Expected first open to be Superseded, got Opened
FAIL Open_Uncertified_RefusedReadOnly InvalidOperationException: Expected Refused outcome, got Opened
FAIL Selection_Reconcile_PointsDropped InvalidOperationException: Expected 1 reconciled point (cv-2), got None { }
FAIL Selection_Reconcile_StationByExactEta InvalidOperationException: Expected station reconciled to index 1 by exact eta, got None { }
FAIL Selection_ReconciledBeforeChanged InvalidOperationException: Event order check failed. SelectionChanged: True, Checked in Changed: False
SUITE --controller-shell exit 1
FAIL --controller-shell exited 1
NATIVE-STARTUP app-initialize
NATIVE-STARTUP lifetime=none
SUITE --shell-window exit 0
FAILED: CfdWorkbench.Desktop.Tests (exit 1)
wall 30 s (budget 60 s)
```

Exit code: `1`

## 2. As-Built Open Cancel Hazard Red Evidence

In the as-built controller, cancelling an open flow either threw `TaskCanceledException`/`OperationCanceledException` uncaught or mutated controller state because `Adopt(next)` had already executed before post-commit sampling completed:

```text
FAIL Open_CancelDuringPrepare_CurrentFoilUnchanged TaskCanceledException: A task was canceled.
```

Track D2 replaces this with build-aside-then-swap and latest-wins cancellation token management so cancelling an open leaves the current foil completely intact and returns `OpenOutcome.Cancelled`.
