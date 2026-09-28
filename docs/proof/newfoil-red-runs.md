---
id: proof-newfoil-red-runs
title: NEWFOIL red-first run
type: proof-pack
status: in-review
owner: "@track-newfoil"
phase: implementation
tags: [app-shell, new-foil, foildsl, proof]
links:
  - {to: design-app-shell, rel: depends-on}
  - {to: spec-foildsl, rel: depends-on}
  - {to: spec-cfd-workbench-v1, rel: depends-on}
review-by: 2026-10-28
summary: >-
  Red run of the New foil checks before FoilSource.NewDefault and
  WorkbenchController.NewFoilAsync existed. tools/run-tests.sh exited 1 at the
  Release build with nine missing-member errors and zero warnings.
review-suggested: []
---

# NEWFOIL red-first run

Track NEWFOIL, branch `newfoil-default`, macOS, 2026-09-28. Harness Grok, model grok-4.7.

The checks were committed before the production methods. `FoilSource` had no `NewDefault`. `WorkbenchController` had no `NewFoilAsync`.

## Red run

`tools/run-tests.sh` exits 1 during the Release build. No suite ran.

```text
$ tools/run-tests.sh
tests/CfdWorkbench.Core.Tests/FoilSourceTests.cs: error CS0117: 'FoilSource' does not contain a definition for 'NewDefault'
  (NewDefault_ParsesAndCertifies, NewDefault_SectionResidualWithinAcceptance,
   NewDefault_NoExampleDependency, NewDefault_WingEstimates_AreaAndAspectRatioExact,
   NewDefault_PlanformNearElliptic)
tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs: error CS1061: 'WorkbenchController' does not contain a definition for 'NewFoilAsync'
  (NewFoil_Opened_UntitledNoPath, NewFoil_CancelDuringPrepare_CurrentFoilUnchanged,
   NewFoil_WorksWithoutExample)
tests/CfdWorkbench.Desktop.Tests/ControllerShellTests.cs: error CS0117: 'FoilSource' does not contain a definition for 'NewDefault'
Build FAILED.
    0 Warning(s)
    9 Error(s)
Time Elapsed 00:00:02.31
```

Exit code: `1`
