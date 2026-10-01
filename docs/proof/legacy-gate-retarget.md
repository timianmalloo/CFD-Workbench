---
id: proof-legacy-gate-retarget
title: Legacy gate retarget — the adapters gate's applied-contrast step moves from the pre-shell window to the shell matrix
type: proof-pack
status: in-review
owner: "@track-legacy"
phase: implementation — LEGACY repair cycle 1
tags: [legacy, app-shell, desktop, theme, contrast, gate, proof]
links:
  - {to: design-app-shell, rel: depends-on}
  - {to: proof-app-shell-test-inventory, rel: relates-to}
  - {to: proof-application-adapters, rel: relates-to}
review-by: 2026-10-31
summary: >-
  The pre-shell window retired, so the adapters gate now reads the shell matrix
  ThemeMatrix_ShellControls_AppliedContrast: 286 frozen rows over 4 theme variants,
  with every ratio re-derived. Repair cycle 1 restores the pressed and returned states,
  the TextBox states and the point Span field that the Test Architect's veto named.
  A mutant made the new rows fail before they passed.
---

# Legacy gate retarget (track LEGACY)

**Result.** `tools/verify-application-adapters.py` no longer runs the test binary with `--theme-controls` on the pre-shell
`MainWindow`. That window, and the mode, were removed. The gate reads the `THEME-ROW` lines that
`ThemeMatrix_ShellControls_AppliedContrast` (`tests/CfdWorkbench.Desktop.Tests/ShellWindowTests.cs`) prints during the
Desktop test step. The gate checks four things:

- The row set equals the frozen `SHELL_THEME_ROWS` table: 71 rows in each of light, dark, high-contrast and default, plus 2 light-only live-flip rows. That is 286 rows.
- Each ratio is recomputed from the emitted ARGB and must match the emitted ratio within 0.0005.
- Each row's floor must equal the frozen floor.
- `THEME-SHELL-CHECK rows=286 variants=4 source=shell-MainWindow` and `PASS ThemeMatrix_ShellControls_AppliedContrast` must both be present.

Eleven negative controls run inside the gate: missing, duplicate, alpha, ratio, nan, floor-lowered, below-floor,
unknown-row, variant-missing, summary-missing and check-not-passed.

## Veto findings (Test Architect, Adversary Mode) and their disposition

| Finding | Disposition |
|---|---|
| Pressed and returned states lost (Styles.axaml `Button:pressed`, `ListBoxItem:pressed`, `:selected:pressed`, hover rules) | Restored. `appbar.sidebar.{hover,pressed,returned}`, `modal.{save,discard,cancel}.{pressed,returned}`, `browser.{selected,unselected}.{hover,pressed,returned}` and `tab.Foil source.{selected,unselected}.{pressed,returned}` are added, at floor 4.5. A `returned` row must paint exactly as its rest row, or the matrix fails. |
| TextBox states lost (Styles.axaml TextBox rules) | Restored: `span.{hover,focus.text,focus-hover,selection,returned}` at 4.5, `span.focus.caret` at 3, and `source.{focus.text,selection}` at 4.5. The selection row measures `SelectionForegroundBrush` on `SelectionBrush`. `span.returned` must paint as rest. |
| cv.* and owned-draft numeric rows dropped | Accepted on one condition. `point-span.text` (4.5) and `focus.point-span` (3) measure `PointSpanInput` with a free point selected. That field replaces the retired per-control numeric field. |
| Live-flip row mislabelled | Fixed. `live-flip.dark.tab.Section.unselected` measured `docTabs[3]`, which is Foil source and selected. It now measures `docTabs[4]`, which is Section and unselected. |
| FOCUS-PLACEMENT composition evidence dropped | Accepted as dropped (Coordinator ruling). Focus rows still measure the two-ring adorner geometry and both tones against the backing and the fill. |

**How the press is driven.** A Button takes a real framework press. The press is released outside the button, so no
click fires. A list row or a Dock tab takes the `:pressed` style state instead, because a real press would select it.
That is the same "styled" route the retired matrix used. The opaque-backing oracle (`Backing`) now accepts a uniform
positive scale, because Fluent's pressed Button shrinks to 0.98. It still refuses rotation, skew and non-uniform scale,
and it checks that the transformed text box sits inside the painted layer.

## Red, then green

- **Mutant:** in `Styles.axaml`, `ListBoxItem:selected:pressed` Foreground changed from `CanvasBrush` to `InkBrush`, so the text is ink on ink. The change was temporary and reverted with `git checkout`. Shell suite: `FAIL ThemeMatrix_ShellControls_AppliedContrast InvalidOperationException: 4 theme rows failed: light/browser.selected.pressed #ff1b2929 on #ff1b2929 = 1.00 < 4.5 | dark/browser.selected.pressed #ffedf4f2 on #ffedf4f2 = 1.00 < 4.5 | high-contrast/browser.selected.pressed White on White = 1.00 < 4.5 | default/browser.selected.pressed #ff1b2929 on #ff1b2929 = 1.00 < 4.5`. The gate parser fed that log refused it: `RuntimeError: shell theme contrast fails or emitted ratio/floor mismatches: light/browser.selected.pressed`.
- **Green:** after the revert, the shell suite exits 0 with `THEME-SHELL-CHECK rows=286 variants=4 source=shell-MainWindow`. The gate's `applied_theme_checks` accepts the log: 286 rows, and all 11 negative controls refused.

## Old → new row map

The old matrix had 99 rows in each of 4 variants (396), plus TEXTBOX-STATE, THEME-STATE and FOCUS-PLACEMENT records.

| Old row(s) | New row(s) | Note |
|---|---|---|
| `toolbar.enabled`, `interaction.toolbar.example.{rest,hover,pressed,returned}` | `appbar.sidebar.{rest,hover,pressed,returned}` | The pre-shell toolbar is gone; the app-bar button is the shell's toolbar button |
| `focus.toolbar` | `focus.appbar` (+ `.vs-fill`) | |
| `tab.section.selected`, `tab.source.selected`, `interaction.tab.*.{rest,hover}` | `tab.<title>.{selected,unselected}.rest`, `tab.Foil source.{selected,unselected}.hover`, `select.tab.*` | All five Dock document tabs |
| `interaction.tab.*.{pressed,returned}` | `tab.Foil source.{selected,unselected}.{pressed,returned}` | |
| `interaction.tab.*.focus-hover`, `focus.tab` | `focus.tab`, `focus.tab.selected-while-focused` (+ `.vs-fill`) | |
| — | `tool.*.{rest,hover}`, `select.tool.*` | New: Dock tool tabs |
| `station.selected`, `interaction.station.*` | `browser.{selected,unselected}`, `browser.{selected,unselected}.{hover,pressed,returned}` | The station list is now the Browser pane |
| `station.focused` | `focus.browser`, `focus.browser.selected` (+ `.vs-fill`) | |
| `cv.selected`, `cv.focused`, `interaction.cv.*` | dropped | The CV list retired; Plan-canvas point glyphs are covered by `PlanCanvas_Brushes_AllFromThemeResources` |
| `numeric.enabled.owned-draft`, `interaction.numeric.owned.*`, `textbox.numeric.*` | `span.text`, `span.{hover,focus.text,focus.caret,focus-hover,selection,returned}`, `focus.span`, `point-span.text`, `focus.point-span` | The owned-draft numeric field retired; Span and the point Span field are the shipped numeric fields |
| `focus.numeric` | `focus.span`, `focus.point-span` | |
| `source.active.readonly`, `interaction.source.readonly.*`, `textbox.source.*` | `source.text`, `source.focus.text`, `source.selection` | |
| `viewport.annotation`, `section.annotation` | the same names | |
| `modal.body`, `modal.{save,discard,cancel}`, `interaction.modal.*` | `modal.body`, `modal.{save,discard,cancel}.{rest,hover,pressed,returned}` | |
| THEME-DISABLED-NUMERIC | the matrix refuses an enabled Span field with no foil open | Asserted, not emitted |
| TEXTBOX-STATE, THEME-STATE metadata | dropped | Each row's emitted paint is the evidence, and `returned` is compared to rest |
| FOCUS-PLACEMENT | dropped (accepted) | |
| — | `live-flip.dark.tab.Section.unselected`, `live-flip.dark.select.tab.Foil source` | Light only: the variant follows a live switch |
| — | `span.painter-oracle` | Asserted, not emitted: the painter oracle refuses a low-contrast mutation |
