---
id: proof-openfix-red-runs
title: OPENFIX red runs and verification — M1.2a Open outcomes and Span input
type: proof-pack
status: in-review
owner: "@track-openfix"
phase: implementation — M1.2a Open-flow repair
tags: [app-shell, desktop, open, span, red-first, proof]
links:
  - {to: design-app-shell, rel: depends-on}
review-by: 2026-10-27
summary: >-
  Red-first evidence for the ShellHost candidate-accept path, Start origin alerts,
  retained refused original, and nonfinite Span input; plus the three-run green set.
---

# OPENFIX red runs and verification

All runs used `AGENT_SESSION=track-openfix AGENT_WI=OPENFIX tools/run-tests.sh` from this worktree. The script builds Release, then runs Core, CLI, and Desktop. Each failing invocation is recorded below, including setup faults.

| Run | State | Exit | Observed result |
|---|---|---:|---|
| 1 | New tests, before source change | 1 | Build failed: four CS1061 errors from using `ModelArea.StartCardView`, which is not public. The tests were corrected to use `FindControl<StartView>`; this run did not prove product behavior. |
| 2 | Corrected Open tests, before source change | 1 | `Open_IdCandidate_AcceptThroughShell_Opens`, `Open_Refused_ReturnsToOriginWithAlert`, and `Open_NeedsIds_ReturnsToOriginWithAlert` failed. The process was terminated after the Span test deadlocked in its setup by synchronously waiting for `OpenExampleAsync` on the Avalonia UI thread. 91 Desktop checks had passed; this run did not finish the suite. |
| 3 | Corrected test setup, before source change | 1 | 281 Core PASS, 105 Desktop PASS; all four requested tests failed. The candidate action was missing on Start; Refused and NeedsIds lost Start/alert state; NaN showed COPY-145 instead of the invalid-number row. |
| 4 | Source fix, before waiting for asynchronous Accept completion in the test | 1 | 281 Core PASS, 108 Desktop PASS; `Open_IdCandidate_AcceptThroughShell_Opens` failed because the test asserted before the asynchronous click handler completed. The test now pumps the dispatcher until the alert closes or its timeout expires. |

Run 3 failure lines:

```text
FAIL Open_IdCandidate_AcceptThroughShell_Opens InvalidOperationException: Start control AlertAcceptIdsButton missing
FAIL Open_Refused_ReturnsToOriginWithAlert InvalidOperationException: Refused open lost Start, alert focus, or read-only original
FAIL Open_NeedsIds_ReturnsToOriginWithAlert InvalidOperationException: ID candidate lost Start, alert focus, or pending candidate
FAIL Span_NaNOrInfinity_InvalidNotNotAssessed InvalidOperationException: NaN showed 'The new span couldn't be checked. Span is unchanged. Try again or enter a different value.' or changed the geometry
```

## Claims and oracles

| Claim | Test and failure oracle | Red observed | Residual risk |
|---|---|---|---|
| Operator opens an ID-candidate foil and Accept opens it | `Open_IdCandidate_AcceptThroughShell_Opens`: real `ShellHost.OpenFileAsync`, Start action click, controller inspection and visible viewport | Run 3 | The test uses Avalonia's event dispatcher rather than a physical pointer. |
| Refused returns to its origin with COPY-141, focus, and original retained read-only | `Open_Refused_ReturnsToOriginWithAlert`: real file open from Start and Workspace, exact COPY row, Dismiss focus, pending original bytes, no candidate, existing accepted source unchanged | Run 3 | The invalid-geometry fixture covers one Refused code. |
| NeedsIds returns to its origin with COPY-140 and Accept focus | `Open_NeedsIds_ReturnsToOriginWithAlert`: real file open from Start and Workspace, exact COPY row, Accept focus, pending candidate, existing accepted source unchanged | Run 3 | The fixture covers one missing-ID foil. |
| NaN and infinities use the invalid-number row without changing geometry | `Span_NaNOrInfinity_InvalidNotNotAssessed`: NaN, ±Infinity and junk, expected COPY-118 text and unchanged accepted source | Run 3 | Culture-specific numeric spellings are outside this input set. |

## Green suite

After the final test assertions were added, three `tools/run-tests.sh` runs exited 0 with the same set of 390 named PASS lines (281 Core, 109 Desktop; CLI completed successfully). The sorted PASS-set SHA-256 on each run was `18d776b00d477e7696819a6a1d6dcbed65818fbde926d899b32114ecd715aa16`. Wall times were 41, 40, and 41 seconds, under the 60-second gate.

## Other gates and launch

| Check | Exit and observed result |
|---|---|
| D3a, D1, D2, C1, P1 named checks | 0 each; 40/40, 12/12, 21/21, 14/14, 23/23 PASS |
| `python3 tools/check-docs.py` | 0; documentation checks passed after the generated index added this proof pack |
| `design-lint.py --strict DESIGN.md` | 0; zero warnings |
| `xaml-token-lint.py --root . src/CfdWorkbench.Desktop` | 0; no findings |
| App launch | `dotnet run -c Release --no-build --project src/CfdWorkbench.Desktop/CfdWorkbench.Desktop.csproj` reported `main-window-assigned=True` and `window-opened`; Ctrl-C closed the foreground process (exit 130). A subsequent `pgrep -fl CfdWorkbench` returned exit 1 with no matches. |

The controller accept contract was broken at the `OpenAsync`/`AcceptCandidateAsync` seam: `OpenAsync` returned a candidate without staging `PendingOriginal` and `PendingCandidate`, which `AcceptCandidateAsync` requires. The repair stages both on `NeedsIds` and retains `PendingOriginal` on `Refused`; `OpenPathAsync` now uses the same state transition instead of duplicating it.
