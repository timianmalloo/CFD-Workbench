---
id: proof-wdf-red-first
title: "WDF: red-first record for the Windows fractional-scale fixes"
type: proof-pack
status: draft
owner: "@trk-wdf"
phase: implementation
tags: [windows, dpi, layout-rounding, target-size, red-first]
links:
  - { to: proof-wri-investigation, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Item 8 (TextBox MinHeight) and item 1 (menu-gesture check) are red then green on the Mac. Items 2 and 7 change a tolerance
  and cannot be red on the Mac (no forced render scale); the PC ring is their proof. Item 6 is held for the scale probe.
---

# WDF red-first record

Inner loop: `CFD_TEST_ONLY=<name> dotnet tests/CfdWorkbench.Desktop.Tests/bin/Release/net10.0/CfdWorkbench.Desktop.Tests.dll`
(the harness runs every mode; the check passes or fails in the one mode that holds it; "matched no check" lines elsewhere are expected).

**Forced scale: not available.** Avalonia's `Window.RenderScaling` comes from the platform (`UsePlatformDetect`); no test sets it.
No seam was built. So item 8 uses the declaration check the brief allows; the PC run confirms the rendered height.

## Item 8 (product): `TextBox.prop-b` MinHeight

New check `PropertiesPane_Density_EveryInputDeclaresMinHeightOf24` (`PropertiesCellsTests.cs`): every visible, enabled TextBox and
ComboBox in the pane declares `MinHeight >= 24`.

- Red (old `Styles.axaml`, `MinHeight 0`): `FAIL ... MinHeight under 24: PointSpanInput MinHeight 0, PointAftInput MinHeight 0,
  HandleAngleInput MinHeight 0, Input_h_root_length MinHeight 0, Input_h_tip_length MinHeight 0, SpanInput MinHeight 0,
  RootChordInput MinHeight 0, TipChordInput MinHeight 0, HandleLengthInput MinHeight 0` (the nine controls of the PC log).
- Fix: `TextBox.prop-b` `MinHeight` = `{DynamicResource PropRowInputHeight}` (24, scaled by `ApplyTextScale`).
- Green: the check passes; `PropertiesPane_Density_EveryTargetAtLeast24` still passes at the Mac scale.

## Item 1: `KeyBindings_MenuGesture_NotBound`

The assertion is now `AssertMenuGestureBinding(window, macOS)`: on macOS no table gesture is bound on the window; elsewhere each
is bound exactly once. New check `KeyBindings_MenuGesture_BoundOnceWhenBuiltTheWindowsWay` wires a window as `MainWindow` does
off macOS (`BuildMenu(macOS: false)` + `ShowInWindow`) and runs the off-macOS branch on any host.

- Red: the old NotBound body run on that window: `FAIL KeyBindings_MenuGesture_BoundOnceWhenBuiltTheWindowsWay
  InvalidOperationException: A native menu gesture was also bound on the window` (the Windows failure, reproduced on the Mac).
- Green: both checks pass; every table gesture is bound exactly once.

## Items 2 and 7: tolerance in device pixels

Helper `DevicePixel.Tolerance(visual, exact)` (`tests/CfdWorkbench.Desktop.Tests/DevicePixel.cs`): unchanged at an integer scale,
`max(exact, 1 / RenderScaling)` at a fractional one. `DevicePixel.Of(visual)` is `1 / RenderScaling`. The Mac stays exact.

| Item | Before | After |
|---|---|---|
| 2 `ModelArea_FourViewsMinimumWindow_...` | `slot.Bounds.Width < 320 \|\| slot.Bounds.Height < 240` | `Width < 320 - pixel - 1e-6 \|\| Height < 240 - pixel - 1e-6`, `pixel = Tolerance(area, 0)` |
| 7 `PropertiesPane_B_FocusedErrorField...` | `Abs(box.X - ring.X - 1) > 0.01`, same for Bottom | `> pixel`, `pixel = Tolerance(aft, 0.01)` |

Not red on the Mac: scale is 1 or 2, where the tolerance is unchanged. The red is the PC log (319.33 x 239.33; offset 0.667); green
needs the PC ring. Mac run after the change: both checks PASS (unchanged behaviour).

## Item 6: held

`PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` is unchanged except an `assume:` comment. Predicted skew at 1.5 is
0.667 DIP (one device pixel); measured 1.0 (`input ends 218.33, fact 217.33`). One device pixel does not cover 1.0, so the 0.5
tolerance stays and the check still fails on Windows until the scale probe (investigation P4, P6) explains the extra 0.33. Inferred.
