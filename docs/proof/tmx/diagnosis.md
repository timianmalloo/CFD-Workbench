---
id: proof-tmx-diagnosis
title: "TMX diagnosis of ThemeMatrix_ShellControls_AppliedContrast on Windows at 150 percent"
type: proof-pack
status: active
owner: "@trk-tmx"
phase: implementation
tags: [windows, theme-matrix, fractional-scale, diagnosis]
links:
  - { to: defect-classes, rel: relates-to }
review-by: "2026-11-12"
summary: >-
  Why the Browser-row enclosure check in the theme matrix throws "does not enclose text on MainWindow" at 150 percent. Reproduced on the
  Mac by forcing the window scale to 1.5 (emulation). Class: fixture and oracle, not a product overflow. Proposal and the one print the PC must send.
---

# TMX: ThemeMatrix_ShellControls_AppliedContrast at 150 percent

Labels: **Verified** = observed on the Mac in this session. **Verified-emulated** = observed on the Mac with the window scale forced to 1.5 (see 2). **Inferred** = a Windows claim not reproduced.
No `src/` change. The diagnostic print used here is saved as `scratch/tmx/diagnostic-print.diff` (47 lines, outside the repo) and was reverted; the tree has only this file.

## 1. What the check compares (Verified, `ShellWindowTests.cs`)

- `Backing(Visual text)` (`:3560`) walks the text's ancestors from the window down. For every ancestor with an opaque `Background` it takes the text's local rect (`new Rect(text.Bounds.Size)`), maps it to that ancestor with `TransformToVisual`, and requires it to lie inside `0..layer.Bounds.Width/Height` with a tolerance of **0.01 DIP** (`:3578`).
- The unit is DIP. There is no device-pixel step and no scale input. At scale 2 and 1 every edge sits on a whole DIP, so 0.01 is exact. At 1.5 one device pixel is 0.667 DIP, so a DIP edge can sit 0.33 DIP off a whole number (the DPI-A shape, `DevicePixel.cs`).
- "on MainWindow" names the **first** (outermost) ancestor that fails. The walk starts at the window, so the window layer is tested before any panel.
- The throw is bare ("Painted background ...", not "N theme rows failed: ..."). A `Probe` wraps its call and collects the message into `rowFailures`. A bare throw therefore comes from an unprobed call. Candidates: the `rest = TextPaint(target)` line in `PressRows` (`:230`, called at `:257`), and `Paint()` in `TextBoxStates`. One bare throw also aborts the check, so every other failing row is lost.

## 2. Reproduction on the Mac (Verified-emulated)

The Mac display is scale 2 (SCALE_CONTEXT `RenderScaling=2 PrimaryScaling=1`). `AVALONIA_GLOBAL_SCALE_FACTOR=1.5` is ignored by the macOS native backend (the run still printed `RenderScaling=2` and passed, Verified). A real 1.5 needs a 1.5 display.

Emulation: after `window.Show()`, call the protected `TopLevel.HandleScalingChanged(1.5)` by reflection, then settle. Layout then rounds to 1.5 device pixels (`RenderScaling` reads 1.5; extents come out as `225.333`, `257.333`, `1499.333`). It is not a native 1.5 surface, so text shaping and clip rounding may differ from Windows.

Result, same check, same code, unchanged `src/`:

```
Scale 2 (the Mac, normal):  PASS ThemeMatrix_ShellControls_AppliedContrast
Scale 1.5 (forced):         FAIL ... InvalidOperationException: Painted background does not enclose text on ToolChromeControl
  [area=29.33,685.33,201.33,14  layerSize=260,676  text=TextBlock "η 1 · section-a"]
  at TextPaint (ShellWindowTests.cs:230) > PressRows (:257) > RunThemeMatrix (:459)  = PressRows(theme, "browser.unselected", rowItems[1])
```

It is the same assertion, the same bare-throw shape, and the same row family as the PC. The layer differs: the emulated row is 9 DIP past the Browser pane (`ToolChromeControl`, 676 tall) but still inside the window (685 + 14 = 699.3 of 700). On the PC the row is past the window bottom (Inferred: Segoe UI line metrics make the Browser content taller still).

## 3. Cause (Verified-emulated, with one open point)

Test fixture, `ShellWindowTests.cs` around `:645-650`: the second station row (`rowItems[1]`) can sit under the Browser's fold on the 700 DIP window (DR-STATUS-1). The fixture calls `rowItems[1].BringIntoView()` and then measures it.

Scroll state of the Browser pane's outer `ScrollViewer` after `BringIntoView` (printed):

| scale | offset Y | extent | viewport | row 1 top | row 1 in view |
|---|---|---|---|---|---|
| 2 (normal) | 32 | 1487 | 620 | 585 | yes: 585..620, flush with the bottom |
| 1.5 (forced) | 2.67 | 1499.33 | 619.33 | 619.33 | **no**: starts at the viewport bottom |

Two effects stack:

1. **Layout rounding inflates the content.** Every element height rounds up to a device pixel, so the Browser stack is 12.3 DIP taller (1499.33 against 1487). The Mac at scale 2 has the row flush with the pane bottom (zero slack), so any growth pushes it out.
2. **`BringIntoView` did not scroll it into view.** It moved the offset by 2.67 DIP where 35 were needed. A second `BringIntoView` plus settle changed nothing (Verified-emulated). Whether this is the emulation or Windows behaviour is **open**.

The row is inside a `ScrollViewer`, so below the fold it is clipped, not painted. The user does not see "text on the window background". The check refuses it correctly: its precondition, "the row is on screen", is false.

## 4. Classification

**(b), a test artefact, refined**: not half-pixel rounding in the comparison. The oracle (`Backing`) is right to reject text it cannot prove is on its backing. The fixture's scroll-into-view does not hold at a fractional scale, and the pane has zero slack at scale 2.

Why not (a): the text overflows no painted background; it is clipped by a scroll viewport. Why not (c): nothing here is a theme brush. The same code passes on the Mac, and the failing quantity is a layout height, not a colour.

**One part is a possible product defect.** If `BringIntoView` also fails at 1.5 on Windows, a keyboard user who focuses a Browser row below the fold would not see it scroll into view. That is a real accessibility and function bug. The Mac emulation cannot settle it. The PC print below does (it is Inferred until then).

Other zero-slack elements (Verified at scale 2, `TMX-SLACK` print): the left tool-tab strip at `x=0`, the document tab strip at `y=0`, and the 35 DIP control flush with the window right edge (`989+35=1024`). They sit exactly on the window edge. At 1.5 a 35 DIP width becomes 35.33 DIP. Whether that overshoots depends on how Avalonia rounds the arrange; it did not fail in the emulation, so it is a **risk, not a finding**.

## 5. Proposal (not applied; test-only, no `src/`)

1. **Fixture, the minimal fix.** Replace the single `BringIntoView` with a scroll that provably lands: set the outer `ScrollViewer` offset so `rowItems[1]`'s bottom is `<=` the viewport bottom, then assert the row is inside the viewport before any measurement, with the message "Browser row could not be scrolled into view at scale S". That moves the failure from a misleading "does not enclose text" to a named precondition. Red first: the 1.5 emulation above is the red run; the green run is the same emulation after the fix. Cost: the same 9 to 14 s check, no new check.
2. **Make the failure list whole.** Wrap the `rest = TextPaint(target)` line in `PressRows` and the `Paint()` calls in `TextBoxStates` in `Probe`. Today one bare throw hides every other failing row, which is why the PC shows one line. This is the class "first throw hides the rest".
3. **Do not loosen `Backing`.** A blanket device-pixel tolerance (1 px, 0.667 DIP) would let this exact case pass, because the row is 9 DIP past the pane but only 0.67 DIP changes with rounding. It would not hide this row, but it would hide a real one-pixel overflow at scale 1 and 2. If a tolerance is wanted for the flush-edge risk in 4, use `DevicePixel.Tolerance(layer, .01)` (unchanged at integer scales, 0.667 at 1.5), and only at the window layer.
4. **The PC print that decides it (test-only, one run, Ruling 186 permitting).** Add the `TMX-SV` line from the saved diff after the `BringIntoView` call: it prints the outer `ScrollViewer` offset, extent, viewport and row 1 top. If on Windows at 1.5 the row is in view after `BringIntoView`, the cause is a different row and the throw site (the stack frames from the saved diff) names it. If it is not in view, `BringIntoView` is broken at 1.5 and that is a product bug to file (a).

## 6. Ruling 186

The paused item is the **Windows fractional-scale proof** (no new Ruling 179 or 184 run budget, no Settings-launch). Proposal 1 and 2 are Mac-side test repairs and a Mac emulated red/green run. They claim no Windows PASS and need no PC run, so they sit outside the pause, like the Mac-side repairs for items 2 to 5 already on main. Proposal 4 needs a PC run, so it **is** under Ruling 186 and waits for the operator.

For readiness in the meantime: the `fractional-scale` expected-failure class exists for the ring; the readiness check has no manifest route (Inferred, not opened). Do not list this name until proposal 4 shows whether it is a fixture defect (fix it) or a product defect (fix it in `src/`). Listing it now would also hide the possible keyboard defect.

## 7. Evidence and limits

- Logs (Mac, outside the repo): `scratch/tmx/base.log` (scale 2 PASS), `s15.log` (env var ignored), `s15f.log` (forced 1.5 FAIL), `sv2.log` and `sv15.log` (scroll state), `tw15.log` (second `BringIntoView`), `slack.log` (326 window-layer slack lines).
- Limit: the emulation is not a native 1.5 surface. It reproduces the assertion and the layout-rounding growth; it does not prove the Windows scroll behaviour or the exact layer. All Windows statements are Inferred.
- Window size: 1024 x 700 is also the `WindowMinWidth` / `WindowMinHeight`, so a smaller window is impossible. Heights 480 to 640 passed because the clamp keeps 700 (Verified).
