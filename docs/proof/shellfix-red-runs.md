---
id: proof-shellfix-red-runs
title: Shell visual defect red runs
type: proof-pack
status: in-review
owner: "@timianmalloo"
phase: m1.2a-shellfix
tags: [app-shell, rendered-ui, regression]
links:
  - {to: design-app-shell, rel: tested-by}
  - {to: defect-classes, rel: relates-to}
review-by: 2026-10-30
summary: >-
  Red-first rendered-control checks for the operator's first native shell findings.
  Each row records the observed failure before its production fix.
---

# Shell visual defect red runs

All runs used `AGENT_SESSION=track-shellfix AGENT_WI=SHELLFIX` and the Release Desktop named-check harness.

| Finding | Red check and observed failure | Cause and green evidence |
|---|---|---|
| F7 | `Shell_F7_ModelTabReentry_RendersAcceptedFoil`: `FAIL ... re-entry: model viewport was not attached and drawn; root=Window, visible=True, bounds=0, 14, 907, 599, render=2, revision=0/1`. A forced frame invalidation after returning from Foil source did not draw. | Dock's nonzero deferred reveal duration held the document presentation: a diagnostic run with `RevealDuration = TimeSpan.Zero` passed without another change. Setting zero in `ShellHost.InstallTheme` follows design §11's “Motion: none”; the same check then passed with the diagnostic override removed. |
| F6 | `Shell_F6_ModelArea_OnlyDockDocumentTabs`: `FAIL ... Model area has 1 inner tab rows and 4 Dock document tabs`. | The pre-Dock `DocumentTabs` remained visible inside the Dock Plan + 3D document. Its three other bodies were already moved into Dock, leaving a dead inner strip. Removed that tab control; the four Dock document tabs are now the only row. The check passed. |
| F9 | `Shell_F9_SectionSelectedStation_DrawsProfile`: `FAIL ... Selected station Section canvas has no realized profile drawing inputs`; `Shell_F9_SectionNoStation_ShowsEmptyCopy`: `FAIL ... Section document omitted its No station selected empty state`. | The moved `SectionEditorView` was never bound by `ShellHost.RefreshPanes`; its canvas kept a null profile. Bind the selected station's `SectionView` and show “No station selected.” over the canvas when no station is selected. Both realized-control checks passed. |
