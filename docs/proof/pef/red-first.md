---
id: proof-pef-red-first
title: PEF red-first - Escape tooltip dismissal survives a refresh
type: proof
status: draft
owner: "@trk-pef"
tags: [pef, plan-canvas, tooltip, ruling-188]
links:
  - { to: investigation-pce-escape-flake, rel: relates-to }
review-by: 2026-11-08
summary: >-
  Red-first record for PlanCanvas_Escape_TooltipStaysDismissedAcrossRefresh: it fails on main 0ae3f254 product code and passes
  with the tooltipDismissed fix. Probe readout unchanged.
---

# PEF red-first

Command (one check, Release dll built from the worktree):
`CFD_TEST_ONLY=<check> tools/run-suite.sh dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll --plan-canvas`

## Red (test commit f9cb4a83, product code unchanged from main 0ae3f254)

Exit 1:

```
FAIL PlanCanvas_Escape_TooltipStaysDismissedAcrossRefresh Exception: Refresh after Escape brought the tooltip back: Trailing edge, point 5 of 7, control point, from root 315.00 mm, aft 120.00 mm
```

This is the PCE failure text: the tooltip is rebuilt by the refresh that the released mesh causes.

## Green (with the fix in `PlanCanvas.cs`)

Exit 0 for each: `PlanCanvas_Escape_` (the new check and `PlanCanvas_Escape_DismissTooltipThenClearSelection`),
`PlanCanvas_HoverPoint_TooltipCopyAndRing`, `PlanCanvas_HoverRail_TracingProbeReadout`, `PlanCanvas_ProbeAndDelta_NotLiveRegions`
all print PASS.

## What the check asserts

Held mesh seam; hover trailing point 5; Escape once; release the hold; settle. Then: `TooltipText` is null, the selection is
kept, `ProbeText` equals its value before Escape (Ruling 188 (2)), and a pointer move to a different position and back
restores the tooltip.

The full `tools/run-tests.sh` result is in the track Return.
