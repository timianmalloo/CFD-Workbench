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
  Captures P1-P5 at 150% and records the authorized P6 attempt. Settings selected 200%
  and Avalonia reported 2.0, satisfying the committed P6 scale condition. A coordinator
  follow-up added an AppliedDPI==192 guard, which skipped the eight named checks; P6
  remains NOT ASSESSED. Scale was restored and verified at 150%.
---

# WRI Windows scale probe

## Goal state

- **Goal:** complete P1–P6 from `docs/proof/wri/investigation.md` on the Windows PC.
- **Done when:** P1–P5 have raw output; all eight named P6 checks run once at verified 200%; 150% is restored and verified; temporary instrumentation is removed; proof files are hashed and pass the applicable checks.
- **Not in scope:** product/test fixes, registry writes, undocumented DPI packets, process-only scale overrides, resolution changes, sign-out, or reboot.
- **Tier:** T1 evidence capture. **Fan-out cap:** 0.

## Capture identity and method

P1–P5 were captured at `842e575d8408d27c6f01f53df9d0da00e434ea38`. The fresh P6 attempt used `93240b06df197f68b9ba3b971c3c8192895a7445`. Windows 11 Pro, version `10.0.26300`, build `26300`, 64-bit. The .NET executable was `%USERPROFILE%\.dotnet\dotnet.exe`, SDK `10.0.203`; test processes used affinity `0x3F` (six logical processors). Captured command, output, timing, SDK, tested HEAD, and hashes are in the adjacent files. Where the launch wrapper did not record a field, it is marked `NOT_RECORDED`.

*Added by the Mac at the join (Ruling 178, conditions).* This P6 attempt predates Ruling 177 (recorded 02:31:37Z; this
cycle ran 02:22:27–02:22:50Z), and 93240b06 does not contain the WDF fixes (main 9a250e3c and later). It is not the Ruling
177 P6, which is still owed: a head at or after 9a250e3c, no AppliedDPI gate, and in-process RenderScaling as the scale
predicate.

Temporary test instrumentation was reverted and its SHA-256 recorded in `p6-temporary-hashes.txt`. The final diff contains proof artifacts and the derived docs index/audit output only.

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

## P6 — 100% or 200% confirmation

**P6 named checks: NOT ASSESSED.** P6 was authorized but unmet when the original capture cap fired. A fresh bounded attempt then used the Windows Settings UI. `Screen.AllScreens` identified `\\.\DISPLAY1` as primary. In `ms-settings:display`, UI Automation found `Display 1` selected, `Display 2` unselected, and `SystemSettings_Display_MainMonitor_CheckBox` On and disabled. The Scale combo was `SystemSettings_Display_Scaling_ItemSizeOverride_ComboBox`. Its owning “Show more settings” control under “Multiple displays” exposed the checkbox with `ExpandCollapsePattern.Expand`; the scale choices used `SelectionItemPattern.Select`.

The first repair cycle stopped before mutation because the checkbox was not present until that group expanded. In cycle 2, the exact primary identity was reacquired, and UI Automation selected `200%`. The captured Avalonia diagnostic reported `RenderScaling=2.0` and `PrimaryScaling=2.0`, satisfying the committed P6 scale condition. `HKCU:\Control Panel\Desktop\WindowMetrics\AppliedDPI` remained `144`. A coordinator follow-up imposed an additional `AppliedDPI==192` guard after the retained task prompt; that extra guard caused the named checks to be skipped. The follow-up was a coordinator planning defect, not a Windows or platform blocker. The eight P6 checks have no result at 200%, so their status remains NOT ASSESSED.

The runner's first restoration selector used `150%`, while the exact Settings item is named `150% (Recommended)`, so its `finally` did not restore. Restoration then used that exact item through `SelectionItemPattern.Select`. Final UI Automation observed Display 1 selected, Display 2 unselected, the main-display toggle On and disabled, and `150% (Recommended)` selected; `AppliedDPI=144` and `DISPLAY1` remained primary. A separate Avalonia diagnostic reported `RenderScaling=1.5` and `PrimaryScaling=1.5`. No registry edits or process-level scale overrides were used. Repair cap 2/2 fired; no third attempt was made.

The first failed UIA attempt is documented in `p6-cycle1.*`; the 200% transition and skipped checks are in `p6-cycle2-*` and `p6-live-200-diagnostic.*`; the restored Settings state and 1.5 diagnostic are in `p6-restoration-ui.*` and `p6-restoration-render.*`. The capture manifest records their committed bytes and hashes. The eight requested P6 checks therefore remain NOT ASSESSED:

| Check | P6 status |
|---|---|
| `KeyBindings_MenuGesture_NotBound` | NOT ASSESSED |
| `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` | NOT ASSESSED |
| `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` | NOT ASSESSED |
| `Elevation_SideSelectedStation_RenderedFullWeight` | NOT ASSESSED |
| `View3d_SelectedStation_RenderedWidthAndChip` | NOT ASSESSED |
| `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` | NOT ASSESSED |
| `PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused` | NOT ASSESSED |
| `PropertiesPane_Density_EveryTargetAtLeast24` | NOT ASSESSED |

This table describes the P6 confirmation only. The 150% P1–P5 results above remain separate.

## Run behavior and limits

The earlier P2/P3/P5 command selected four checks and invoked `--views --properties-cells`. The `--views` path exited before the property suite; output records `FAIL SELECTOR PropertiesPane_Density_EveryTargetAtLeast24 matched no check`. P4 was then run separately with `--properties-cells`, selecting the density check directly. These remain diagnostic captures, not a complete test ring. For P6, the temporary diagnostic build succeeded on HEAD `93240b06`; no named P6 test process was started because the coordinator follow-up's extra `AppliedDPI==192` guard skipped them after the committed scale condition had been met. The final diff contains no source or test changes.

`capture-manifest.json` records the byte count and SHA-256 for each other file in this proof folder. It is verified against committed Git blobs by `tools/check-capture-manifests.py`.
