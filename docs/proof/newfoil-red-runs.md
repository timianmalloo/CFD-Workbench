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
review-suggested:
  - { by: spec-cfd-workbench-v1, on: 2026-10-03, reason: "Spec 1.7 draft (pending owner approval of docs/specs/amendments/spec-1.7.md): 36 amendments - FoilDSL 4.1 vertex range 4-16 and Rebuild to N (A4.1/A4.2/A4.6/GEO-05/GEO-15), quarter-chord held line (A4.15/CAD-16), paired section point types (A4.15/CAD-15), CAD-04 slider clause, UI-36/37/40, Evaluate verb, panel Cp, depth-unset VLM label, ANA-04 lattice oracles, A5.10 residual criterion and mesh gate, TMR+GCI on pin change, no Messages pane and Points in the right side bar." }
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
