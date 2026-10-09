---
id: proof-wri-investigation
title: "WRI: the eight Windows-only rendering and platform failures"
type: investigation
status: draft
owner: "@trk-wri"
phase: implementation
tags: [windows, dpi, layout-rounding, target-size, investigation]
links:
  - { to: review-pr-18, rel: relates-to }
review-by: "2026-11-09"
summary: >-
  Seven of the eight class-(a) failures share one cause: Avalonia layout rounding at a 150 % display scale (arithmetic
  fits 1.5 exactly; the scale itself is not recorded and needs the probe). The 23.3 px inputs are a real product miss of
  0.67 DIP (35 px where 36 are needed): the TextBox is content-sized and the 3 DIP padding rounds down at the .5 midpoint.
  KeyBindings is a contradiction between a Mac-only test and a deliberate Windows binding.
---

# WRI: the eight Windows-only failures

Evidence: PR #18 (`origin/win/r166-ring-baseline`), `docs/proof/ring-windows/calibration-ruling-170/run-{1,2,3}/tmp-tests/Desktop.log`,
read with `git show`. All eight `FAIL` lines occur once in each of the three runs, with the same text (Verified: grep count 1 per run).
Log line numbers are run 1. Avalonia source is 11.3.14 (the pinned version, `src/CfdWorkbench.Desktop/CfdWorkbench.Desktop.csproj:13`),
read from `raw.githubusercontent.com/AvaloniaUI/Avalonia/11.3.14/...`.

## 0. What the evidence does and does not say about the display

- No log, receipt or measurement file records the OS display scale, DPI or Windows text scale (searched all non-log files under
  `docs/proof/ring-windows`; the only hits are the app's own `display.json` "textSize" fixtures and a `layout.json` screen of 2560 x 1440
  at x=1512, which are test fixtures, not the host). **The first probe is the scale.**
- The tests run a real window on the real platform (`Program.cs:19` `UsePlatformDetect`; `AreaFixture` makes `new Window{...}.Show()`,
  `ControllerViewTests.cs:1261`), so `RenderScaling` is the PC's OS scale. There is no headless pin to 1.0.
- The numbers fix the scale arithmetically. Every fractional value is a multiple of 2/3 DIP (1.3333, 23.3333, 319.3333, 59.3333, 19.3333),
  i.e. whole device pixels at scale 1.5 (35 px / 1.5 = 23.33; 479 / 1.5 = 319.33). Scale 1.25 gives steps of 0.8, 1.75 gives 0.571;
  neither fits. 3.0 also fits mathematically but is implausible. **Inferred: 150 % (RenderScaling 1.5).** The probe confirms.

### The mechanism, from Avalonia source (Verified in source, applied by Inferred scale)

- `LayoutHelper.RoundLayoutValue(v, s) = Math.Round(v * s) / s` (`LayoutHelper.cs:248-254`); `Layoutable.UseLayoutRounding` defaults to true and inherits
  (`Layoutable.cs:133-134`).
- `Border.LayoutThickness` rounds `BorderThickness` (`Border.cs:145-160`), and `LayoutHelper.MeasureChild` / `ArrangeChild` round **padding and
  border thickness** before use (`LayoutHelper.cs:37-53, 71-80`). `Layoutable.MeasureCore` rounds `Margin` (`Layoutable.cs:543-551`).
- `Math.Round` is round-half-to-even. At 1.5: 1 DIP -> 1.5 px -> **2 px = 1.333 DIP**; 3 DIP -> 4.5 px -> **4 px = 2.667 DIP**;
  2 DIP -> 3 px exact.
- So a thickness that is exact at 1.0 and 2.0 (the Mac: 1x or Retina 2x) is not exact at 1.5. This is a device-pixel rounding effect, not a measuring
  artefact in physical pixels: the tests read `Bounds`, which are already DIP values produced by layout.

## 1. The eight, one by one

| # | Check (file:line of the throw) | Log line, failure text | What it measures |
|---|---|---|---|
| 1 | `KeyBindings_MenuGesture_NotBound` `ShellWindowTests.cs:955-967` | `Desktop.log:604` "A native menu gesture was also bound on the window" | `new MainWindow()`: no `window.KeyBindings` entry equals a command-table menu gesture. |
| 2 | `ModelArea_FourViewsMinimumWindow_EachAtLeast320x240OrOneView` `ControllerViewTests.cs:628` | `:1160` "648 x 488: Four views with a view under 320 x 240: 319.33, 239.33 (x4)" | At the exact 648 x 488 arrangement every slot `Bounds` is >= 320 x 240. |
| 3 | `ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack` `ControllerViewTests.cs:576` | `:1377` "Double-click on the 3D label did not show 3D alone" | A raised `DoubleTapped` makes layout `One(ThreeD)`, hides Plan, and the 3D slot is at least `PlanContent` width - 2. Fixture is 1400 x 1000 DIP. |
| 4 | `Elevation_SideSelectedStation_RenderedFullWeight` `ElevationTests.cs:266` | `:1399` "The chip has no station border" | Pixel at `(chip.Left + 0.5, chip.CenterY)` in a scale-1 capture is within 60 of the station colour. |
| 5 | `View3d_SelectedStation_RenderedWidthAndChip` `View3dTests.cs:609` | `:1440` "Chip border is not station" | Pixel at `(chip.CenterX, chip.Y + 0.5)` is within 30 of the station colour. |
| 6 | `PropertiesPane_Density_DecimalsAlignAcrossFactsAndInputs` `PropertiesCellsTests.cs:710` | `:1476` "input ends 218.33, fact 217.33" | The right edge of an input's digits equals a fact's digits edge within 0.5 DIP (DC-3). |
| 7 | `PropertiesPane_B_FocusedErrorFieldDistinctFromUnfocused` `PropertiesCellsTests.cs:454` | `:1506` ring `1.333,1.333,59.333,20.667` around box `2,2,58,19.333` | The error ring starts exactly 1 DIP outside the box (`box.X - ring.X == 1`, `ring.Bottom - box.Bottom == 1`, tolerance 0.01). |
| 8 | `PropertiesPane_Density_EveryTargetAtLeast24` `PropertiesCellsTests.cs:765` | `:1526` "under 24 px: PointSpanInput 23.3, PointAftInput 23.3, HandleAngleInput 23.3, Input_h_root_length 23.3, ... HandleLengthInput 23.3" | Every focusable pane control has `Bounds.Height >= 23.5` (DR-DEN-1, WCAG 2.2 SC 2.5.8 minimum 24). |

## 2. Grouping by mechanism

**Group A, layout rounding at 1.5 (items 2, 6, 7, 8; item 4 and 5 probable).**

- Item 2 (Verified arithmetic). Product math: `(648 - 4) / 2 - 2 * FrameThickness = 320` (`ModelArea.axaml.cs:266-268`, `FrameThickness = 1`, line 24). At 1.5 the 1 DIP frame
  renders as 1.333 DIP, so the inside is 322 - 2.667 = **319.333**. That is exactly the logged value; the height gives 242 - 2.667 = 239.333, also exact.
- Item 7 (Verified arithmetic). The ring is offset by the 1 DIP hairline inset and rounds to 1.333; the box is 2 DIP band (3 px, exact) around a 19.333 DIP body (29 px).
  The observed offset is 0.667 (one device pixel), against the test's exact 1 DIP.
- Item 8 (Verified arithmetic, root of the target miss). `TextBox.prop-b` has `MinHeight 0`, no `Height`, `LineHeight 14`, `Padding 3,3` (vertical), `BorderThickness 2`
  (`Styles.axaml:399, 205-206`). Its height is content-sized: 2 + 3 + 14 + 3 + 2 = 24 DIP nominal (comment at `Styles.axaml:206` says exactly that). At 1.5 the border
  2 DIP = 3 px (x2 = 6 px), the padding 3 DIP = 4.5 px -> `Math.Round` half-to-even -> **4 px** (x2 = 8 px), the line 14 DIP = 21 px. Total **35 px = 23.333 DIP**. 36 px is needed for 24 DIP.
  All nine reported controls are `TextBox.prop-b`; ComboBox and ToggleButton rows carry `MinHeight`/`Height = PropRowInputHeight` (24) and are not on the list (`Styles.axaml:403, 418`).
- Item 6 (Inferred, same family). Input: border band 2 DIP (3 px) + padding 3 DIP (4 px) = 7 px = 4.667 DIP; fact inset `0,0,5,0` = 7.5 px -> 8 px = 5.333 DIP
  (`Styles.axaml:197`). Predicted skew 0.667; observed 1.0. The same rounding family explains the sign and rough size, not the exact 1.0 (text position rounding is
  not modelled). The check tolerance (0.5 DIP) is smaller than one device pixel at 1.5 (0.667), so it cannot hold at this scale whatever the product does.
- Items 4 and 5 (Inferred). The chip border is a 1 DIP edge; at 1.5 it is 2 px = 1.333 DIP and the chip's left/top is a fractional DIP. The test samples at `+0.5`
  from the edge in a capture the tests call scale 1 (`Shot.Of`, `ControllerViewTests.cs:1340-1348`; the device-resolution variant exists and is used elsewhere). The sampled pixel can land on the
  blended seam. Not provable without pixels. Probe item P5.

**Group B, a Mac-only constant (item 1, Verified).** `NativeMenuBuilder.ShowInWindow` is called when `!mac` (`MainWindow.axaml.cs:64`) and **deliberately** adds a `KeyBinding` for each menu gesture
(`NativeMenuBuilder.cs:166-172`; its doc comment names W-1 defects b and c: Ctrl+Z and Ctrl+S did nothing on Windows). A sibling Windows test requires that behaviour
(`WindowsShellTests.cs:180-205` presses each gesture and expects one firing). `KeyBindings_MenuGesture_NotBound` was written for the macOS system menu, where a window binding would
double-fire, and does not branch on the OS. On Windows the two tests contradict; the product follows the Windows test.

**Group C, undetermined (item 3).** The failing clause is not in the log (one message for four conditions). Candidates, none verified: (a) the fixture asks for a 1400 x 1000 DIP
window; at 1.5 that is 2100 x 1500 px, taller than a 1440 px-high screen (the `layout.json` fixture screen is 2560 x 1440, host unknown), so the OS may clamp it and Four/One thresholds
move; (b) `ThreeDSlot.Bounds.Width < PlanContent.Bounds.Width - 2` with rounded frames, off by 1.33 DIP; (c) the in-window menu bar (absent on macOS) changes the content height. Needs the probe.

Fonts (Segoe UI against Helvetica Neue): `PropFontFamily` switches by platform (`Styles.axaml:179`), but every failing size above is explained without font metrics, because `LineHeight 14` is set
explicitly. Font metrics are **not** needed for items 2, 7, 8, so not the first suspect; they can still contribute to 6 and 3. Ruled out as primary, not as contributor.

## 3. Product defect or test constant

| # | Verdict | Reason |
|---|---|---|
| 1 | Test | The product is right on Windows (Windows test requires the binding). |
| 2 | Test (optional product note) | A 0.67 DIP shortfall of a design floor on a view with no safety or target semantics. The product threshold already subtracts a 1 DIP frame; the test could compare in device pixels. |
| 3 | Unknown | See Group C. |
| 4, 5 | Test (Inferred) | Sampling offset assumes an integer-DIP edge. |
| 6 | Test | Tolerance below one device pixel. Product skew of one device pixel is not a defect. |
| 7 | Test | Exact 1 DIP offset where layout can only place whole device pixels. |
| 8 | **Product (real miss, small)** | See below. |

**Is the 23.3 px target real?** Verified by arithmetic and source: yes, in DIPs. `Bounds.Height` is a layout result already in DIPs, produced after rounding. The control occupies 35 physical pixels
where a 24 DIP target at 150 % is 36 pixels. It is not an artefact of measuring physical pixels at a fractional scale. Whether the user's mouse can hit 35 versus 36 px is the same
fractional inconvenience; but the design floor (DR-DEN-1, SC 2.5.8 24 CSS px) is stated in DIPs and the pane misses it by 0.67 DIP. Note the test already allows 0.5 DIP (`< 23.5`), so it was written with a
known rounding wobble in mind and still fails. The remaining uncertainty is only the scale: if the PC reports something other than 1.5, redo the arithmetic with the probe values.
SC 2.5.8 has a spacing exception, so conformance may hold even at 23.33 where neighbours are far enough apart; rows are 24 DIP pitch, so the exception does not clearly apply (Inferred, not assessed here).

## 4. Repair plan

| Group | Fix | Red-first control | Ring |
|---|---|---|---|
| A-product (item 8) | In `Styles.axaml`, give `TextBox.prop-b` `MinHeight = PropRowInputHeight` (scaled by `ApplyTextScale`, `PropertiesPane.axaml.cs:215-222`), so the 24 DIP target is a floor rather than a sum of three rounding terms. The `PART_BorderElement` band (2) and the 20 DIP box stay. Blast radius: row heights at scale 1 and 2 unchanged (already 24). Rollback: one style line. Alternative: make padding 3.5/2.5 sized for 1.5, rejected (fits one scale only). | `EveryTargetAtLeast24` already fails on Windows. Add a Core-free pure check: under `RenderScaling` forced to 1.5 on Mac (set `Window` scaling via a test hook if Avalonia exposes one; otherwise assert the style's `MinHeight` equals the token), failing on current code. If no hook exists, the control is the PC run. | Desktop ring (fast). Cost: one check, under 1 s. |
| A-test (items 2, 6, 7) | Compare in device pixels: tolerance `max(old, 1 / RenderScaling)` for 6 and 7; for 2 compare against `320 - 2 * (round(FrameThickness * s) / s - FrameThickness)` or assert on the physical size. A helper `DevicePixel(window)` in the harness, used by all three. | Red: the three checks fail on the PC at 1.5 today (logs). Green: after the helper. On Mac the helper returns the old tolerance, so Mac stays unchanged. | Desktop ring on the PC only (no Mac red is possible without a scale hook). |
| A-test (items 4, 5) | Sample in a device-resolution capture and take the nearest of the two seam pixels. | Needs P5 pixels first. | Desktop ring, PC. |
| B (item 1) | Branch the check on `OperatingSystem.IsMacOS()`: on Mac keep NotBound, elsewhere assert every exported gesture **is** bound once. | Red: current check fails on Windows (log `:604`), passes on Mac. Green after branch. | Desktop ring. |
| C (item 3) | After P3 and P4 decide. Do not edit before. | | |
| Class prevention | A harness helper that fails any check which compares a `Bounds` value to a DIP constant without a device-pixel tolerance (grep gate in `tools/` listing `Bounds.` compared to literals in Desktop tests). | Seeded violation fails the gate. | Fast gate. |

Defect class to register (not registered here: `docs/lessons/defect-classes.md` is outside this track's paths): **DPI-A, a layout constant verified only at scale 1.0 and 2.0.**
Signature: a test or style that sums DIP terms to a boundary (24, 320, 1.0 offset) and passes at integer scales; fails at 1.25, 1.5, 1.75. Why it survives: the only ring that
runs on a fractional-scale display is the PC calibration, and it was read as "class (a) noise". Control: the PC probe records the scale in every receipt, and the Desktop
harness gets a forced-scale case (or the grep gate above).

No `simplify:` or `assume:` marker in `PropertiesPane.axaml.cs`, `ModelArea.axaml.cs`, `Styles.axaml` is past its ceiling (three markers found, all about trace and toast slots; none relate to scaling).

## 5. PC probe request (a script under 15 minutes)

```text
PC BRIEF: WRI scale probe. Worktree on win/r166-ring-baseline, no code change committed except the probe output.
P1. Display scale and text scale (PowerShell, one line each; paste the output):
    Add-Type -AssemblyName System.Windows.Forms; [System.Windows.Forms.Screen]::AllScreens | % { "$($_.DeviceName) $($_.Bounds) primary=$($_.Primary)" }
    (Get-ItemProperty 'HKCU:\Control Panel\Desktop\WindowMetrics').AppliedDPI
    (Get-ItemProperty 'HKCU:\Software\Microsoft\Accessibility').TextScaleFactor
P2. The scale Avalonia sees: in a temporary check in tests/CfdWorkbench.Desktop.Tests, print
    window.RenderScaling, window.Screens.Primary.Scaling, window.Screens.Primary.WorkingArea, window.UseLayoutRounding
    (use the AreaFixture window of ModelArea_ViewLabelDoubleClickOrReturn so the number is for that check).
P3. For the double-click check, print the three clauses separately: Controller.Layout, PlanSlot.IsEffectivelyVisible,
    ThreeDSlot.Bounds.Width, PlanContent.Bounds.Width, and the window Bounds and ClientSize (DIPs) after Show().
P4. One PropertiesPane row (PointAftInput) before the 23.5 check: print TextBox.Bounds, PART_BorderElement.Bounds,
    TextPresenter.Bounds, Padding, BorderThickness, in DIPs, and each multiplied by RenderScaling (physical pixels).
    Expected if the analysis holds: TextBox height 23.333 DIP = 35 px; Padding applied 2.667 DIP = 4 px.
P5. For items 4 and 5, dump the 5 x 5 RGB block around (chip.Left, chip.CenterY) and (chip.CenterX, chip.Y) in Shot.Of and
    Shot.AtDeviceResolution, with chip.Bounds.
P6. Run the cheapest confirmation of the mechanism: set the display scale to 100 % (or 200 %) for one run, re-run
    "CFD_TEST_ONLY=PropertiesPane_Density_EveryTargetAtLeast24" and the other seven by name, and report which pass.
    If all eight but KeyBindings pass at 100 % and 200 %, group A is confirmed necessary and sufficient.
Return: the P1-P6 text in docs/proof/wri-probe/ on the PC branch. Time box 15 minutes; no fix.
```

## Return summary (for the leader)

- Likely cause: Avalonia layout rounding at a 150 % scale (Inferred from the arithmetic, confirmed by P1/P2/P6).
- The 24 px target miss is a **real product defect of 0.67 DIP**: Verified by arithmetic and Avalonia source against the committed logs; the scale itself is Inferred until P1/P2 run.
- Repairs: product `MinHeight` on `TextBox.prop-b` (1 line); test tolerance in device pixels (items 2, 6, 7); OS branch for KeyBindings; items 3, 4, 5 await the probe.
