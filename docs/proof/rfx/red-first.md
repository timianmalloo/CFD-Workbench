---
id: proof-rfx-red-first
title: "RFX red-first receipt: a stale refit-flash timer clears a newer emphasis"
type: proof-pack
status: active
owner: "@trk-rfx"
phase: implementation
tags: [comb, timer, flake, proof]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-10"
summary: >-
  A pending flash end timer cleared a newer static emphasis; held-seam red, fix, green and the 20-run loop.
---
# RFX red-first: a stale refit-flash timer clears a newer emphasis

## Mechanism (hypothesis confirmed)
- `CombPlate.axaml.cs` `Update()` arms a 900 ms `DispatcherTimer` (`flash`) when `CombRefitEmphasis && !ReducedMotion && flash is null`.
- `flash` was cleared only by its own tick. `WorkbenchController.EndCombEmphasis()` (and `StepCombScale`, which sets `CombRefitEmphasis = false`) ended the emphasis with the timer still pending.
- A later emphasis under reduced motion arms no timer. When the old timer then ticked, it called `EndCombEmphasis()` and cleared the newer static emphasis. That is the flake's "Reduced motion has no static emphasis" (settle spans the 900 ms under load).
- Product effect too: with the stale timer pending, `flash is null` is false, so a new non-reduced refit gets no flash of its own and is cut short by the old tick.

## Red (old code, held seam, no sleep, no load)
Check `PlanComb_StaleFlashTimer_DoesNotClearNewerEmphasis`. The seam `CombPlate.RunPendingFlashEnd()` fires the pending callback exactly after the newer emphasis is set.
Command: `CFD_TEST_ONLY=PlanComb_StaleFlashTimer tools/run-suite.sh dotnet run -c Release --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --plan-canvas` (log: `red.txt`)
Observed: exit 1, `FAIL PlanComb_StaleFlashTimer_DoesNotClearNewerEmphasis Exception: A stale flash timer cleared the newer emphasis` (line 451, after the earlier "has no static emphasis" require passed).

## Fix
`CombPlate.Update()`: when the emphasis is off and a flash timer is pending, stop and null it.

## Green
Same command with `CFD_TEST_ONLY=PlanComb_` (log: `green.txt`): exit 0, 16 PASS lines for the PlanComb_ checks, no FAIL, both the held-seam check and `PlanComb_RefitFlash_OnceAndStaticUnderReducedMotion` PASS.
Loop: 20 runs of `PlanComb_` under 8 busy `yes` loops (`scratch/rfx/loop.sh`): `runs=20 pass=20 fail=0`.
