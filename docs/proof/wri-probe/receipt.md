---
id: proof-wri-probe-windows-scale
title: "WRI Windows scale probe receipt"
type: proof-pack
status: partial
owner: "@trk-wri"
phase: implementation
tags: [windows, dpi, rendering, probe]
links:
  - { to: proof-wri-investigation, rel: relates-to }
review-by: "2027-04-09"
summary: >-
  Ruling 177 P6: eight named checks ran at Settings-selected 150% and 200%. All eight
  passed at 200%; at 150%, three passed and five failed. Ruling 178's ninth check and
  supplemental item-6 bounds were not assessed because exact Settings UIA targets were
  not reacquired within 55 seconds; no scale change or check run followed.
---

# WRI Windows scale probe

## Goal state

- **Goal:** complete P1–P6 from `docs/proof/wri/investigation.md` on the Windows PC.
- **Done when:** P1–P5 have raw output; all eight named P6 checks run once at verified 150% and 200%; 150% is restored and verified; temporary instrumentation is removed; proof files are hashed and pass the applicable checks.
- **Not in scope:** product/test fixes, registry writes, undocumented DPI packets, process-only scale overrides, resolution changes, sign-out, or reboot.
- **Tier:** T1 evidence capture. **Fan-out cap:** 0.

## Capture identity and method

P1–P5 were captured at `842e575d8408d27c6f01f53df9d0da00e434ea38`. A prior P6 attempt at `93240b06df197f68b9ba3b971c3c8192895a7445` did not run the named checks; its failed/incomplete evidence remains historical. Ruling 177's named runs used tested HEAD `4383089735b27586efed93657606fce37317fc07`, after the WDF changes. Windows 11 Pro, version `10.0.26300`, build `26300`, 64-bit. The .NET executable was `%USERPROFILE%\.dotnet\dotnet.exe`, SDK `10.0.203`; test processes used affinity `0x3F` (six logical processors). R177 commands, output, timing, SDK, tested HEAD, and hashes are in the adjacent `r177-*` files.

Temporary test instrumentation was reverted; source and built-DLL SHA-256 values are recorded in `r177-temporary-hashes.txt`. The R177 captures are tied to the tested HEAD above, not the prior incomplete attempt. The final ancillary paths are root `.gitattributes`, which preserves exact raw R177 capture bytes and their CRLF line endings, and `docs/proof/wri/investigation.md`, which updates stale scale and target-size conclusions; the remaining changes are proof, audit, and derived index artifacts.

## P1 — Windows display and registry

`Screen.AllScreens` reported primary `\\.\DISPLAY1` bounds `{X=0,Y=0,Width=1707,Height=1067}` and secondary `\\.\DISPLAY5` bounds `{X=1707,Y=0,Width=2560,Height=1440}`. `WindowMetrics.AppliedDPI` returned `144`. The `Accessibility.TextScaleFactor` query returned no output and no error; its value is **not recorded**. Raw command and stdout/stderr are in `p1-*` files.

## P2 — Avalonia scale in AreaFixture

The `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` AreaFixture reported `RenderScaling=1.5`, `Screens.Primary.Scaling=1.5`, `WorkingArea=0, 0, 2560, 1528`, and `UseLayoutRounding=True`.

## P3 — double-click clauses

After `Show`, the fixture reported `Window.Bounds=0, 0, 1400, 1000` and `ClientSize=1400, 1000` DIP. After the double-tap, `Controller.Layout` was `One(ThreeD)`, `PlanSlot.IsEffectivelyVisible=False`, and `ThreeDSlot.IsEffectivelyVisible=True`. `ThreeDSlot.Bounds.Width=1130.6666666666667` DIP while `PlanContent.Bounds.Width=1133.3333333333333` DIP. The test failed its width clause: the 3D width is about 2.667 DIP smaller, while the condition permits a difference of at most 2 DIP. The captured check does not isolate any other unreported clause failure.

## P4 — PointAftInput layout

At `RenderScaling=1.5`, `PointAftInput` reported:

- `TextBox.Bounds`: `139.33333333333334, 0.6666666666666666, 62, 23.333333333333332` DIP; multiplied dimensions `93 × 35` device pixels.
- `PART_BorderElement.Bounds`: `2, 2, 58, 19.333333333333332` DIP; multiplied dimensions `87 × 29` pixels.
- `TextPresenter.Bounds`: `0, 0, 52.666666666666664, 14` DIP; multiplied dimensions `79 × 21` pixels.
- `Padding`: `3,3,3,3` DIP, or `4.5` pixels per side before pixel rounding.
- `BorderThickness`: `1,1,1,1` DIP, or `1.5` pixels per side before pixel rounding.

`PropertiesPane_Density_EveryTargetAtLeast24` failed. It reported nine `TextBox.prop-b` controls at `23.3` DIP, below the check's `23.5` DIP threshold.

## P5 — chip pixel blocks

The exact chip bounds, sample coordinates, and 5×5 RGB blocks for both `Shot.Of` and `Shot.AtDeviceResolution` are preserved verbatim in `instrumented.stdout.txt`. The run observed scale `1.5`; it did not perform interpretation or claim that the pixel checks passed. `Elevation_SideSelectedStation_RenderedFullWeight` and `View3d_SelectedStation_RenderedWidthAndChip` both failed their named pixel-color assertion in this capture.

## P6 — Ruling 177 scale confirmation

**P6 execution: complete at both requested scales; overall receipt remains partial.** The unique eight checks ran once at Settings-selected 150% and once at Settings-selected 200%. The check-6 result is included once at each scale; it was not duplicated. No `AppliedDPI` predicate was used. The prior incomplete P6 attempt is preserved as historical evidence and superseded for P6 by these Ruling 177 captures.

Before the first named run, UI Automation observed `Display 1` selected, `Display 2` unselected, the `SystemSettings_Display_MainMonitor_CheckBox` On, and exact `150% (Recommended)` selected. The primary monitor was `\\.\DISPLAY1`. The test process itself printed `RenderScaling=1.5 PrimaryScaling=1.5` before each 150% check. After UIA selected exact `200%`, each fresh test process printed `RenderScaling=2.0 PrimaryScaling=2.0` before its checks. At completion, UIA selected exact `150% (Recommended)` again, and a separate fresh process printed `RenderScaling=1.5 PrimaryScaling=1.5`. The exact UIA controls and patterns were the display list items (`SelectionItemPattern`), `SystemSettings_Display_MainMonitor_CheckBox` (`TogglePattern`), the Scale combo `SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox` (`ExpandCollapsePattern`), and the selected scale item (`SelectionItemPattern`). No registry writes, process-scale overrides, or resolution changes occurred.

The three test-process invocations per scale used selector partitions: `--shell-window` for check 1, `--views` for checks 2–5, and `--properties-cells` for checks 6–8. Every invocation used SDK 10.0.203 and affinity 0x3F. Captures include command, UTC start/end, PID, duration, tested HEAD `4383089735b27586efed93657606fce37317fc07`, stdout, and stderr. The launcher did not preserve `Process.ExitCode`; the raw metadata field is blank and the process exit is **NOT_RECORDED**. The named PASS/FAIL results below are directly captured. No check was rerun to repair this evidence gap.

| # | Check | Settings 150% | Settings 200% |
|---:|---|---|---|
| 1 | `KeyBindings_MenuGesture_NotBound` | PASS | PASS |
| 2 | `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` | FAIL: expected width 647, measured 647.3333 | PASS |
| 3 | `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` | FAIL: after double-click, ThreeD width 1130.67 vs PlanContent 1133.33 | PASS: 1132 vs 1134; after return Layout=Four |
| 4 | `Elevation_SideSelectedStation_RenderedFullWeight` | FAIL: chip has no station border | PASS |
| 5 | `View3d_SelectedStation_RenderedWidthAndChip` | FAIL: chip border is not station | PASS |
| 6 | `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` | FAIL: input digits end 218.33, fact digits end 217.33 | PASS: both end at 218 |
| 7 | `PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused` | PASS | PASS |
| 8 | `PropertiesPane_Density_EveryTargetAtLeast24` | PASS | PASS |
| 9 | `PropertiesPane_Density_EveryInputDeclaresMinHeightOf24` | NOT ASSESSED: UIA target reacquisition failed before tests | NOT ASSESSED: no scale change or tests after UIA failure |

Ruling 177 predicts checks 1, 2, 7, and 8 should pass at 150% as well as 200%; check 2's 150% failure is therefore unexpected. It is recorded without diagnosis. The observed 150% total is 3 PASS / 5 FAIL; the 200% total is 8 PASS. Checks 3–6 failed at 150% and passed at 200%. Check 6 measured digit-end delta 1.00 DIP at 150% and 0 DIP at 200%; its interpretation remains held under Ruling 177 pending the P4 explanation. P3 diagnostics in the same check-3 process include layout, visibility, ThreeD and PlanContent widths, Window.Bounds, and ClientSize at both scales.

Ruling 178 requested a ninth named check and PointAftInput/Value_p_eta bounds during check 6 at both scales. The supplemental attempt did not reacquire the exact `SystemSettings_Display_MainMonitor_CheckBox` and scale combo within its 55-second UIA wait. The captured failure is in `r178-settings-initial.stdout.txt`; the command and timing limitation are recorded in the adjacent `r178-*` files. No scale selection was changed, and no check was launched. Thus the ninth check is NOT ASSESSED at either scale, and the supplemental item-6 bounds are NOT RECORDED. This attempt did not establish the current Settings selection; the prior Ruling 177 restoration measurement remains historical evidence only.

The process start marker for the 20-minute task cap was not captured. Per coordinator instruction, 03:08 UTC was the conservative action deadline. The last scale action was restoration, completed at 02:58:25 UTC; the fresh restoration diagnostic completed at 02:59:08 UTC. Temporary instrumentation was then reverted. The harness source hashes and built DLL hash are in `r177-temporary-hashes.txt`; raw scale, test, build, and restore evidence is in the adjacent `r177-*` files.

The earlier P2/P3/P5 command selected four checks and invoked `--views --properties-cells`; its selector mismatch is retained in the prior raw capture. P4 was run separately. Those earlier measurements remain separate from the Ruling 177 scale comparison. One Ruling 177 repair cycle of the two-cycle cap was used for NuGet assets restore followed by a successful build. The Ruling 178 final repair cycle stopped at the bounded UIA target-reacquisition failure; no scale change, named-check rerun, or source change was made. The final diff contains no source or test changes.

`capture-manifest.json` records the byte count and SHA-256 for each other file in this proof folder. It is verified against committed Git blobs by `tools/check-capture-manifests.py`.
