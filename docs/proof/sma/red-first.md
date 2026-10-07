---
id: proof-sma-red-first
title: SMA red-first receipts (Rulings 124, 125)
type: proof
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sma, red-first, ruling-124]
links:
  - { to: mockup-section-main-area, rel: depends-on }
review-by: 2026-12-31
summary: >-
  For each SMA behaviour, the check that failed on the old code and the run that passed after the change.
---

# SMA red-first receipts

Command shape: `CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet run -c Release --no-build --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- <mode>`.

## 1. Chip strip (A)

Check `PlanCanvas_ChipStrip_OneRowBelowPlanform_AlignedAndClear` (mode `--plan-canvas`), on a tapered planform (tip trailing edge 50 mm forward of the root's).

- Old code (per-station rows at each station's trailing edge): `FAIL ... Chips are not one strip: tops 162.6, 131.5`.
- New code (one strip 20 px under the planform bounds): `PASS`. Also asserted: no chip rectangle intersects the planform bounds or the scale bar, each chip is centred on its station line, none leaves the canvas.
