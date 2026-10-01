---
id: proof-m12b-u1b-red-runs
title: U1b Plan canvas red and mutant runs
type: proof-pack
status: in-progress
owner: track-u1b
phase: m1.2b
tags: [plan-canvas, tdd, rendered-state]
links:
  - { to: design-m12b-points, rel: verifies }
review-by: "2026-10-30"
summary: >-
  Records the observed red runs and mutation controls for the U1b Plan canvas, including
  whole-window rendered pixels and point automation peers.
---

# U1b red runs

| Run | Command | Exit | Evidence |
|---|---|---:|---|
| A1 | `dotnet run --project tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -- --plan-canvas` | 1 | The first two named tests could not compile because `ModelArea.PlanCanvas` was absent (`/tmp/u1b-assumptions-red.log`). |
| A2 | same command after the first Plan surface and peer | 1 | Whole-window bitmap had a white point pixel; inspection of `/tmp/u1b-window.png` proved the window rendered, and the test oracle was corrected to compare the point to the viewport background. |
| A3 | same command | 0 | `PlanCanvas_RenderTargetBitmap_CapturesNonBackgroundPixels` and `PlanCanvas_AutomationPeers_BoundsFocusSelectedInvoke` PASS (`/tmp/u1b-assumptions-green.log`). |
| R1 | same command with first rendered batch | 1 | `Workspace_NewFoil_WindowPixelsShowRailsAtTranslatedPoints`, `PlanCanvas_SelectPoint_RenderedGlyphFilledAndHandlesDrawn`, and `PlanCanvas_SelectedVsUnselected_RenderedAtLeastThreeToOne` FAIL (`/tmp/u1b-render-batch-red.log`). |
| R2 | same command after theme brushes, selection glyphs and framebuffer oracle | 0 | Seven named checks PASS (`/tmp/u1b-render-batch-green.log`). |

The raw `/tmp` files are local run logs; subsequent required mutant and gate receipts are added below as the track proceeds.
