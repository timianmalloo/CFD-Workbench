---
id: proof-m12b-b1b-red-runs
title: "M1.2b B1b red-first and mutation runs"
type: proof-pack
status: in-progress
owner: "track-b1b"
phase: implementation — M1.2b
tags: [m12b, b1b, red-first, point-editing]
links:
  - { to: design-m12b-points, rel: documents }
  - { to: coordination-m12b-build, rel: relates-to }
review-by: 2027-03-29
summary: >-
  Red-first run of the B1b named tests and recorded hand-mutant observations.
  The complete run output is preserved below before the implementation commit.
---

# B1b proof runs

## Red-first named tests

Commit `471cb4f` contains the 43 B1b check names. `tools/run-tests.sh` exited **1** on 2026-09-30 before implementation. The compiler's first error is the absent `PointOutcome` contract, so the test runner could not reach individual assertions. This is the expected initial red observation for the new API; individual behavior and hand-mutant red observations follow below as implementation proceeds.

```text
/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/PointCommandTests.cs(10,20): error CS0246: The type or namespace name 'PointOutcome' could not be found (are you missing a using directive or an assembly reference?) [/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj]
/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/PointCommandTests.cs(12,20): error CS0246: The type or namespace name 'PointOutcome' could not be found (are you missing a using directive or an assembly reference?) [/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj]
/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/PointCommandTests.cs(14,20): error CS0246: The type or namespace name 'PointOutcome' could not be found (are you missing a using directive or an assembly reference?) [/Users/mallalieut/projects/CFD-Workbench-m12b-b1b-receipts/tests/CfdWorkbench.Core.Tests/CfdWorkbench.Core.Tests.csproj]
Build FAILED.
0 Warning(s)
3 Error(s)
Time Elapsed 00:00:03.24
```

## Hand mutants

Before the implementation commit, the `MakeAnchor` ceiling was changed from 16 to 15 in the working tree. With `CFD_TEST_ONLY=MakeAnchor_ThirteenPoints_SixteenAccepted,MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling`, the Core harness exited **1**. The ceiling was then restored to 16. The second check uses a real 14-point rail (the 16-point conformance fixture after one MakeControl), attempts a three-point insertion, and asserts the number in the refusal text.

```text
FAIL MakeAnchor_ThirteenPoints_SixteenAccepted ContractError: Making this an anchor needs 3 more points. This rail has 13 of 15.
FAIL MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling InvalidOperationException: refusal names 16-point ceiling
RESULT failures=2
SUBSET CFD_TEST_ONLY=MakeAnchor_ThirteenPoints_SixteenAccepted,MakeAnchor_FourteenPointsNoSnap_RefusedNamesCeiling ran=2 skipped=374
```

## Bisector regression red

The no-selection Smooth check was strengthened to assert that **both** handles move onto the angle bisector after one handle has been moved under Corner. Against the one-sided implementation, the focused Core run exited **1** before the correction:

```text
FAIL SetTangent_SmoothNoHandleSelected_BothOnBisector InvalidOperationException: left handle moves to bisector
RESULT failures=1
SUBSET CFD_TEST_ONLY=SetTangent_SmoothNoHandleSelected_BothOnBisector ran=1 skipped=375
```

## Clamped handle property red

The fixed-seed random-drag check was expanded to drag Smooth and Symmetric handles past their neighbours. The first run exited **1**: the previous common-shift clamp broke Smooth collinearity. The corrected clamp limits the grabbed handle and derives its mate from the resolved target. A focused rerun passed.

```text
FAIL UpdatePointGesture_RandomTargets_OrderAndRowsHold InvalidOperationException: expected 0; actual -0.0003788924642678657
RESULT failures=1
SUBSET CFD_TEST_ONLY=UpdatePointGesture_RandomTargets_ ran=1 skipped=375
```

## Green run and scope

After restoring the 16-point limit and replacing the recovery fixture with bytes from `10f0628`, the final `tools/run-tests.sh` exited **0**: Core 376 PASS in 31 s, CLI 1 PASS, Desktop 119 PASS in 43 s; wall 46 s under the 60 s budget. `check-named-tests.py B1b --design docs/design/m12b-points.md` found **43/43 PASS**. The default-design checks D3a 40/40, D1 12/12, D2 21/21, C1 14/14 and P1 23/23 each exited **0**. `check-docs.py` exited **0**, with zero graph defects.

| Claim | Oracle | Red observed | Green evidence | Confidence |
|---|---|---|---|---|
| Point command API writes one accepted fact | Compilation plus named command checks | Absent `PointOutcome` compiled red | Named checks PASS | Verified |
| Limit is 16 and refusal names it | 13→16 acceptance and 14+3 refusal text | Ceiling-15 hand mutant failed both checks | Both named checks PASS | Verified |
| No-selection Smooth moves both handles | Displaced handle fixture, each ordinate checked | One-sided version failed | Bisector check PASS | Verified |
| Drag clamp preserves ordering and tangent rows | Fixed-seed anchor and handle targets | Common-shift version failed Smooth collinearity | Random-targets check PASS | Verified |
| Old recovery bytes reopen and resolve | M1.2a saved image, Apply and Discard paths | Initial suite compiled red; missing fixture failed until generated | Both golden checks PASS | Verified |

The tested scope is Core authoring and native receipts. Desktop presentation is owned by later tracks. The pack calls for an independent adversarial reviewer, but this track's brief prohibits sub-agents; the B1b checks and mutants above are the verification evidence in this seat.

## Cursor operation replay red

Reusing an Undo operation id for a point command raised an untyped `InvalidOperationException` before the guard. The named history check was extended; its focused red run exited **1**. The guard now returns `DOC-OPERATION-CONFLICT` for an id owned by a different operation kind.

```text
FAIL History_ReapplyOperationDifferentPayload_Refused InvalidOperationException: Sequence contains no matching element
RESULT failures=1
SUBSET CFD_TEST_ONLY=History_ReapplyOperationDifferentPayload_ ran=1 skipped=375
```

## Gesture observability and overflow reds

The gesture check was extended to assert the emitted frame count. It compiled red because `SessionEvent.Frames` did not exist; the event now carries frame count and measured elapsed time from gesture begin. A finite `double.MaxValue` aft target then exposed `DSL-PATCH` from arithmetic overflow. The normal target path now returns the last valid frame as clamped; the same named check directly bypasses the clamp with an invalid patched coordinate and still observes `DSL-PATCH` from the parse backstop.

```text
error CS1061: 'SessionEvent' does not contain a definition for 'Frames'
FAIL UpdatePointGesture_BypassedClamp_DslPatch ContractError: DSL-PATCH
RESULT failures=1
SUBSET CFD_TEST_ONLY=UpdatePointGesture_BypassedClamp_ ran=1 skipped=375
```

## Golden recovery fixture

`tests/CfdWorkbench.Core.Tests/Fixtures/m12b/m12a-rail-recovery.cfdw` was written by a build of the exact M1.2a source commit `10f0628`. The source was extracted with `git archive 10f0628 global.json src/CfdWorkbench.Core` into a temporary directory under the owned fixture folder, then compiled with `dotnet build -c Release`. The Core DLL SHA-256 was `4cf22af6e12327eae99604d8475f317c5786237bfbf98b9309900bc31cb7853d`. The generator used that project reference and the M1.2a `foil-basic.foil`, then called `Open`, `BeginRailEdit("trailing", "cv-3")`, `UpdateDraft(..., 0.121)`, `CaptureRecovery`, and `SaveImage`. It printed `golden bytes=6564 recovery=trailing`. The output fixture SHA-256 is `b14cd8d67208bd30e5480e4fc6ec321680c0f8b0ee222963bfec67071b4707a7`. The temporary build and generator were removed after the bytes were written. Both golden recovery checks pass against this saved image.
