---
id: proof-a3a-lay-pack
title: A3a LAY canvas layer proof pack
type: proof-pack
status: active
owner: "@trk-lay"
phase: implementation
tags: [a3a, lay, analysis, canvas, proof]
links:
  - { to: design-area3-analysis, rel: depends-on }
  - { to: design-language, rel: depends-on }
  - { to: proof-a3a-lay-red-first, rel: relates-to }
review-by: 2026-11-05
summary: >-
  Evidence for the A3a Plan Γ, 3D load, and Side/Front depth layers on the existing desktop views. The pack ties
  selected-run projection samples to their visible glyphs, accessible names, table twins, and camera-step cost.
---

# Track LAY proof pack

Goal: design `area3-analysis.md` §12.2–12.4 and §18.5 rows 13–16 on the shipped Plan, 3D, Side, and Front views.
Tier T1; fan-out 0. The scene is derived from `AnalysisViewModel.Layers`, the one projected selected-run layer set.
No persistent schema, service, simulator, new theme row, or pane was added.

| Claim | Writer → reader | Oracle and evidence | Confidence / limit |
|---|---|---|---|
| Plan Γ strips use the selected layer's values, batlow palette, actual strip span edges when present, loading curve and the projected legend. | `AnalysisProjection.Layers` → `PlanLoadLayer` → `PlanCanvas.DrawPlan` | `PlanLayer_OutsideStrips_DashedOutlineAndCount` and real-window render. `DESIGN.md` batlow-0…4 is the colour oracle; `LayerData.Legend` supplies variable, unit, range, map and run key. | Verified for the tested current run and unit scene; a Historical run's values stay selected-run values, while the visual Plan outline still comes from the current accepted view. |
| An outside strip has dashed geometry and a text count, so colour is not the only cue. A hidden layer draws no strips. | `LayerSample.Outside` / `LayerData.Visible` → `PlanLoadLayer.Build/Draw` | Named test PASS and the two-change planted mutant FAIL; see [red-first](red-first.md). | Verified; provisional tips are not outside in the projection. |
| 3D arrows are proportional to N/m and follow the local normal; absent values produce no glyph. The selected-run total, root moment and depth marks remain distinct. | `LayerSample.Normal`, `Value`, `LayerData` → `View3dLoadLayer` → `View3d.Draw` | `View3dLayer_NormalAndMissingVector` PASS; the real `ShellHost` render completed; the total is read from the projected Loads row, the root moment from its layer, and depth from the projected margin/elevation. | Verified for the unit scene and current-run render. The 3D total arrow uses a display-length glyph; its number is the projected result. |
| Depth unset or hidden yields no free-surface/depth mark. When set, the projected run's station elevation and margin determine the line height and tip margin. | `ProjectionContext.Stations` → `LayerSample.Elevation`/`Value` → `ElevationDepthLayer` → Side/Front view | `ElevationLayer_DepthUnsetNoBand` and real-window render PASS. | Verified for empty and present scenes; the free-surface depth is a display of h_ref, not a flow correction. |
| Canvas peers name the visible overlays and their existing table twins; toggling visibility updates the names without a pane rebuild. | `WorkbenchController.LayersChanged` → model-area/view subscriptions and `LayerSet` → automation names | `AnalysisLayers_WindowRendersAndPeersFollowVisibility` PASS; `check-event-subscribers.py` observed a production subscriber and zero findings. | Verified in a real window. |
| Layers-on camera events do not reproject on every frame or refresh panes. | cached `LayerSet` / 3D view model → camera redraw | The one full ring's four-view 1440×900 run: full-step p95 3.566 ms, event p95 0.083 ms, 0 pane refreshes, 32 samples. Earlier focused runs varied 6.1–9.4 ms. The existing Desktop readiness check separately printed PASS, with worst step p95 6.845 ms. | Verified in the full ring on macOS arm64; focused-run variability remains a performance risk. |

## Change surfaces and failure modes

Store/run fact: unchanged; strip values and span edges come from the selected `AnalysisRun`. The projection writes
`LayerSample.YLow`, `YHigh`, `Normal`, and `Elevation` from that run and its own source feed. The canvas code is the
compute reader. Visibility is controller state read by `LayerSet`; the `LayersChanged` event redraws each view and
refreshes peer names. An absent value is omitted rather than represented by a zero-length glyph; depth unset has no
layer; hidden layers render no glyph while PNA retains their table twins. Plan's existing render-error band reports a
render exception. No new input, persistence write or external call enters the overlays.

The data model grain remains one sample per run strip or placed station. Γ and N/m are non-additive across unlike
stations; the run's total force is read from the existing projected Loads row rather than redefined in the view.
Layer visibility is display state, not a change to the run. The normal is derived by `DeriveFeed` from the run's own
revision and is never stored as a second scientific fact.

## Review and residual risk

The Test Architect lens checked the named mutant, hidden and absent paths, real composition root, peer names, and
camera budget. The C# lens checked finite-value filters and controller event lifetime. The Simplifier found no new
dependency; three small canvas files keep drawing beside the existing point layers. The overlays have no focusable
glyphs by design; the PNA tables are their keyboard and numeric twins. Existing `analysis.project` telemetry emits
`LayersDrawn`; `view.navigate.end` and the Desktop `READINESS` line emit camera cost. A separate per-layer draw-time
metric is not present, so a future slow layer would be isolated by the already measured frame cost and a profiler.

The current 3D and Plan geometric anchors use the visible accepted `SurfaceView`/`PlanformView`. For a Historical run
whose geometry changed, the numbers, verdicts, normals and depth margins are from that selected run, but the glyph
anchor can lie on the current geometry. The Historical banner remains visible. Showing the exact earlier mesh would
need a historical display surface in the controller contract; LAY has not fabricated one.

The dedicated layers-on four-view full-step measurement crossed 8 ms in some focused runs even though the event
path remained below 0.11 ms and no pane refreshed. The official readiness test passes in its CAD fixture; it does
not turn layers on. The new Desktop integration check emits the full-step number and gates the event path plus zero
pane refreshes, so the variability is visible rather than hidden behind a flaky ring test. A future readiness owner
can add an Analysis fixture to the official 8 ms gate and profile the whole window composition path.

## Verification

The focused Desktop `--analysis` test run passed all four LAY checks. The planted Plan mutant made its test fail and
was restored. The one full `tools/run-tests.sh` run passed every harness in 50 s at end load 31.72: Desktop 698 PASS;
Core parts 230/230/229 PASS; Analysis parts 67/89 PASS; Cli 6 PASS. Its LAY records show all four PASS lines and
`MEASURE AnalysisLayers_CameraStep_WithLayers full_step_p95_ms=3.566 event_p95_ms=0.083 pane_refreshes=0`.
`check-named-tests.py LAY` read the ring and reported 1/1 named PASS, zero failures. `check-event-subscribers.py`
reported 31 events, 4 allowed, zero findings. `check-docs.py` passed; `run-verify-gates.py` passed 12/12 gates.
`design-lint.py DESIGN.md` found zero warnings; `ui-craft-gate.py src/CfdWorkbench.Desktop --gate
--a11y-obligation` reported no findings. The latter's no-findings result is a detector floor, not visual approval.
