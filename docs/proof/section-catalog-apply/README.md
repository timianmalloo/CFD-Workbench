---
id: proof-section-catalog-apply
title: "Section catalog apply: the probes behind investigation section-catalog-apply"
type: proof-pack
summary: "Four Desktop-harness probes run on 2026-10-23: the real shell route applies a choosable catalog row; every row with coordinates lands on the Example; the 16 rows without coordinates render exactly like the 3 choosable ones; a fault inside the fit leaves the dialog on Fitting… with no reason; and the Browser station row drops the modified flag that the chip and station card carry."
status: active
owner: "@timianmalloo"
phase: investigate
tags: [catalog, section-editor, provenance, desktop-harness, probe]
links:
  - { to: investigation-section-catalog-apply, rel: relates-to }
  - { to: design-m12d-catalog, rel: relates-to }
review-suggested:
  - { by: investigation-section-catalog-apply, on: 2026-10-23, reason: "Investigation section-catalog-apply: the catalog sheet renders rows without coordinates as choosable, the fit's loading state has silent branches, and the Browser row and Properties section title drop the modified flag; review the m12d design's component-state table and the behaviour trace" }
---

# Section catalog apply: the probes

**Question.** Why can the operator select a catalog foil in the section editor's *Replace from catalog…* sheet and not
apply it, and why is an edited catalog section still listed as its catalog original?

**Method.** Two probe files for the Desktop harness, `InvSectionCatalogProbe.cs.txt` and `InvCatalogSweepProbe.cs.txt`
(stored with a `.txt` suffix so the test project does not compile them). To rerun, from a worktree:

```
cp docs/proof/section-catalog-apply/InvSectionCatalogProbe.cs.txt tests/CfdWorkbench.Desktop.Tests/InvSectionCatalogProbe.cs
cp docs/proof/section-catalog-apply/InvCatalogSweepProbe.cs.txt tests/CfdWorkbench.Desktop.Tests/InvCatalogSweepProbe.cs
# in tests/CfdWorkbench.Desktop.Tests/WorkbenchTests.cs, inside the `--catalog-dialog` block, before CatalogDialogTests.Run():
#   CfdWorkbench.Desktop.Tests.InvSectionCatalogProbe.Run();
#   CfdWorkbench.Desktop.Tests.InvCatalogSweepProbe.Run();
dotnet build tests/CfdWorkbench.Desktop.Tests/CfdWorkbench.Desktop.Tests.csproj -c Debug -nologo -v q
CFD_TEST_ONLY=Probe_ tests/CfdWorkbench.Desktop.Tests/bin/Debug/net10.0/CfdWorkbench.Desktop.Tests.exe --harness --catalog-dialog
```

The probes print `PROBE`, `SWEEP`, `ROW` and `FAULT` lines and PASS; the facts are in the lines, not the verdict. They
need a real display (the Desktop harness samples focus and geometry). Revert the hook and delete the two copies afterwards.

**Observed (2026-10-23, this laptop, Debug build at 4c07c3b2).**

| Probe | Observation |
|---|---|
| `Probe_RealRoute_ReplaceFromCatalog_Applies` | `ShellHost.RunCommand("section.replace-catalog")` opened the real `CatalogDialog` over an owner window; NACA 0012 selected; Replace enabled; the click applied (draft cursor 1, dialog closed, status "Replaced Root and Tip with NACA 0012. Fit 9.75 µm (limit 10 µm)…", chip "Catalog original · NACA 0012"). |
| `Probe_CatalogSweep_ExampleShared` | Rows with coordinates: 3 (naca-0009 7.31 µm, naca-0012 9.75 µm, naca-4412 5.59 µm), all landed; refused 0; stuck 0. |
| `Probe_CatalogSweep_ExampleIndependent` | After Make unique at Root: 0009 and 0012 land; 4412 refused `CAT-SPACING` (26.82 µm on Tip's 8 points), the chain button offered. |
| `Probe_DisabledRow_RenderedState` | 19 rows, 3 choosable. Every row: `IsEnabled=True`, `Opacity=1`, Foreground `#ff1b2929`, no class. The 7 pending NACA rows read "NACA 63-412 · GEN" in the same form as "NACA 0012 · GEN". Selecting NACA 63-412: Replace disabled, detail "Not generated in this build. GEN sections above can be used.", status strip unchanged; a Replace click changes nothing (cursor 0, dialog open). |
| `Probe_PreviewInternalFault_DialogState` | With the fit throwing a non-contract exception (`previewGate`): detail "Fitting…", Replace disabled, `CurrentPreview`, `RefusedPreview` and `PreviewFault` all null, status strip unchanged. No surface says the fit failed. |
| `Probe_SourceSurfaces_AfterEdit` | After Replace (NACA 0012) then one Move: provenance `gen:naca-0012 modified`; chip "Modified from NACA 0012"; station card "Modified from NACA 0012 (GEN)"; `PropertiesView.SourceDisplay` the same; **Browser row "section-a · NACA 0012"** and the Browser pane's rendered station rows "η 0 · section-a · NACA 0012 | η 1 · section-a · NACA 0012". |
| `Probe_EnterDuringFit_ChoosableRow` (`InvEnterDuringFitProbe.cs.txt`, added after the Test Architect's review) | With the fit held open (`previewGate` blocking): NACA 0012 selected, detail "Fitting…", Replace disabled; **Enter in the search box: cursor 0, sheet open, detail and status unchanged** (the intent is dropped, nothing is queued); after the fit lands Replace is enabled and the cursor is still 0; a second Enter applies (cursor 1). Leaf `TextBlock` brushes read on NACA 0012 and NACA 63-412: both `#ff1b2929`, opacity 1, weight Normal. |

The fourth composer of the source text named by the review, the Properties pane's section-group title
(`src/CfdWorkbench.Desktop/PropertiesView.cs:1325`, `{profile} · {SourceName(...)} · degree {n}`), was confirmed by reading:
it calls the same `SourceName` the Browser row calls, which never reads the modified flag.

Large captures stay out of git; the lines above are the whole record.
