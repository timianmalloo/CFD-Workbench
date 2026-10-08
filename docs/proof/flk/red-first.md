---
id: proof-flk-red-first
title: "Notify probe red-first"
type: proof-pack
status: active
owner: "@trk-flk"
phase: implementation
tags: [flake, section-editor]
links:
  - { to: proof-flk-investigation, rel: relates-to }
summary: "Planted Select post fails the drag check and the FAIL line names the planted call."
review-by: "2026-11-07"
---

# Red first: notify probe

Planted a Select post at move 3 (removed after). Command: CFD_TEST_ONLY=SectionEditor_DragMove tools/run-suite.sh dotnet <Desktop tests dll> --section-editor. Observed (cut at 900 chars):

```
FAIL SectionEditor_DragMove_DrawsWithinOneFrame Exception: Move 3 notified the shell 2 times: [SelectionChanged generation=0 ui=True stack=WorkbenchController.Notify WorkbenchController.cs:3291 <- WorkbenchController.Select WorkbenchController.cs:937 <- <>c__DisplayClass1_0.<Run>g__PlantedSelectPost|36 SectionEditorTests.cs:281 <- Fixture.Move SectionEditorTests.cs:1158 <- <>c__DisplayClass1_0.<Run>b__15 SectionEditorTests.cs:291 <- DesktopChecks.Check WorkbenchTests.cs:615 <- SectionEditorTests.Run SectionEditorTests.cs:269] [Changed generation=0 ui=True stack=WorkbenchController.Notify WorkbenchController.cs:3273 <- WorkbenchController.Select WorkbenchController.cs:937 <- <>c__DisplayClass1_0.<Run>g__PlantedSelectPost|36 SectionEditorTests.cs:281 <- Fixture.Move SectionEditorTests.cs:1158 <- <>c__DisplayClass1_0.<Run>b__15 SectionEditorTests.cs:291 <- DesktopChecks.Check WorkbenchTests

```

Clean code: PASS, COST 565-603 ms (3 runs) vs 566-680 ms before. Nudge check COST 515.7 ms. Full tools/run-tests.sh: all harnesses passed.
