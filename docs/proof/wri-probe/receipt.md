---
id: proof-wri-probe-windows-scale
title: "WRI Windows scale probe receipt"
type: proof-pack
status: complete
owner: "@trk-wri"
phase: implementation
tags: [windows, dpi, rendering, probe]
links:
  - { to: proof-wri-investigation, rel: relates-to }
review-by: "2027-04-09"
summary: >-
  Captures Windows display/DPI, Avalonia scaling, the AreaFixture double-click clauses,
  PointAftInput layout, and selected chip pixels at the observed 150% scale. No product
  files were changed. Confirmation at 100% or 200% was not assessed.
---

# WRI Windows scale probe

## Goal state

- **Goal:** capture P1–P5 from `docs/proof/wri/investigation.md` on the Windows PC.
- **Done when:** P1–P5 have raw output and run metadata; P6 has an explicit assessment status; temporary instrumentation is removed; proof files are hashed and pass the applicable checks.
- **Not in scope:** product or test fixes, changing the desktop scale, and running the eight-check confirmation at 100% or 200%.
- **Tier:** T1 evidence capture. **Fan-out cap:** 0.

## Capture identity and method

Tested commit: `842e575d8408d27c6f01f53df9d0da00e434ea38`. Windows 11 Pro, version `10.0.26300`, build `26300`, 64-bit. The user-local .NET executable was `%USERPROFILE%\.dotnet\dotnet.exe`; SDK `10.0.203`. The captured shell affinity was `0x3F` (six logical processors). Each command and its UTC interval, process ID, affinity, exit code, SDK, and tested commit are preserved in the adjacent command and measurement files.

The temporary test instrumentation was reverted before this receipt was written. The final change is restricted to `docs/proof/wri-probe/**`.

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

**NOT ASSESSED.** P1/P2 recorded the current scale as 150%. The P6 request authorizes a confirmation run at 100% or 200%, but no scale change was attempted: within the remaining time, I did not establish and verify a reversible live Windows Settings path and could not complete the eight-check run plus restoration. Microsoft [documents that display-scale changes can occur while applications are running](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows#display-scale-factor--dpi), but this capture did not verify a usable control path on this host. No registry edits or process-level scale overrides were used. The eight requested P6 checks therefore have no result at 100% or 200%:

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

The P2/P3/P5 command selected four checks and invoked `--views --properties-cells`. The `--views` path exited before the property suite; output records `FAIL SELECTOR PropertiesPane_Density_EveryTargetAtLeast24 matched no check`. P4 was then run separately with `--properties-cells`, selecting the density check directly. These are diagnostic captures, not a complete test ring. No code or test changes remain in the final diff.

`capture-manifest.json` records the byte count and SHA-256 for each other file in this proof folder. It is verified against committed Git blobs by `tools/check-capture-manifests.py`.
