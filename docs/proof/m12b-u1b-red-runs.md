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
| I1 | same command with hit, hover, selection, Tab, comb, camera, and layout tests added | 1 | Compilation failed for absent interaction members (`/tmp/u1b-interaction-batch-red.log`). |
| I2 | same command after those members were added | 1 | The comb rendered-tier assertion sampled between teeth; all other 17 names PASS (`/tmp/u1b-interaction-batch-green.log`). |
| I3 | same command after whole-window frame comparison replaced the one-pixel comb oracle | 1 | The moved samples tab had not settled when sampled; its readiness loop was extended (`/tmp/u1b-interaction-batch-green.log`). |
| A4 | same command with focus, high-contrast, re-entry, brush, live-region and handle-peer checks | 1 | `PlanCanvas_AutomationPeers_HandleNamesCarryAngleAndLength` FAIL; 23 other names PASS (`/tmp/u1b-access-batch-red.log`). |
| A5 | same command after the peer name carried angle and length | 0 | 24 names PASS (`/tmp/u1b-access-batch-green.log`). |

The raw `/tmp` files are local run logs; subsequent required mutant and gate receipts are added below as the track proceeds.
