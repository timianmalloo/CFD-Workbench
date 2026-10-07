---
id: proof-sma-retired
title: SMA retired checks (Ruling 124, Section sample removed)
type: proof-pack
status: draft
owner: "@timianmalloo"
phase: implement
tags: [sma, retired, ruling-124]
links:
  - { to: proof-sma-red-first, rel: refines }
review-by: 2026-12-31
summary: >-
  Each check that pinned the retired Section sample document, and what covers the behaviour now.
---

# Retired checks

The Section sample (document tab "Section sample", the Viewport control `SectionViewport`, its heading `SectionPosition` and its
readout `SectionReadout`) is gone, with its XAML tokens `SectionViewportHeight`, `ViewportAnnotationWidth`, `ViewportAnnotationHeight`
and `SectionAnnotationHeight`.

| Check (file) | Action | What covers the behaviour now |
|---|---|---|
| `Shell_F7_ModelTabReentry_RendersAcceptedFoil` (ShellWindowTests) | retired | It pinned the sample Viewport re-drawing the accepted frame on tab re-entry. The re-entry of every document, including the Section document, is `Shell_AllModelTabs_ReentryRealizesContent` (ported). |
| `Shell_AllModelTabs_ReentryRealizesContent` (ShellWindowTests) | ported | Row 2 is now the Section document and its `SectionView` (visible, laid out, in the window root on re-entry). |
| `ModelArea_MinimumWindow_PlotWidthAtLeast250` (ShellWindowTests) | retired | It pinned `Viewport.PlotWidth` for the sample. The minimum-window width of the Section document's plots is `SectionMainArea_Document_MinimumWindow_ChartsDrawn`. |
| `ModelArea_SectionSampleTab_PlotDrawnAtMinimumWidth` (PlanCanvasTests) | retired | Same behaviour, now `SectionMainArea_Document_MinimumWindow_ChartsDrawn` and `SectionMainArea_Document_FullSize_HeaderNamesSelectedStrip`. |
| `PlanCanvas_AfterDockReattach_RendersSameScene` (PlanCanvasTests) | ported | Switches to the Section document and back; the Plan pixels must match. |
| `DockTabFocus_FreshBatch_ReadyAndTwoRing` (ShellWindowTests) | ported | The focused tab is the Section tab. The "accepted frame bound to the sample viewport" barrier line is dropped: the barrier's own assertion is the fresh composition batch, and nothing else read that frame. |
| `ShellHost_PlanformLayout_ContainsModelAndSidePanes` (ShellWindowTests) | ported | Ids are `model, section, foil-source`. |
| `ThemeMatrix_ShellControls_AppliedContrast` (ShellWindowTests) | ported | Tab titles `Plan, Section, Foil source`; the sample-frame barrier line is dropped (same reason); the `section.annotation` row measures the first text of the Section document. **Its row names change** (`tab.Section sample.*` becomes `tab.Section.*`); `tools/verify-application-adapters.py:160` names the old rows and is not an SMA-owned file (see the Return). |
| `ModelArea_SamplesTabRetired_NoReferencesRemain` (ShellWindowTests) | extended | The retired-name scan now also fails on `SectionSampleDocument`, `SectionSampleBody`, `SectionViewport`, `SectionReadout` and `section-sample` under `src/CfdWorkbench.Desktop`. |
| `AnalysisPanel_Tabs_BoundToSelectedRun`, the DX Section suite (`DxSectionPanelTests`) | ported | They read `AnalysisPanel.SectionView` (the instance the Section document hosts) instead of a child of the panel. The differential check that `UnderreadAt` is called once and never changes the screen is `DxSectionTests` in the Analysis harness (`:423`); `SectionDisplay` is untouched. |
| Render-revision block at `WorkbenchTests.cs:351` (`new Viewport { Frame = ... }`, `FrameRevision`, `InvalidateFrameForMetric`) | retired | It pinned the deleted `Viewport` control only. Nothing else read a frame revision; no behaviour moved. |
| `Viewport_FocusVertex_RaisesFocusedTargetChanged` (ShellWindowTests) | retired | It tested the deleted `Viewport.FocusVertex`. The same event contract for the live control is `SectionCanvas_FocusVertex_RaisesFocusedTargetChanged` (next check in the file). `Viewport.cs` is deleted; the `ViewportSemantic` record `SectionCanvas` uses moved to `ViewportSemantic.cs`. |
