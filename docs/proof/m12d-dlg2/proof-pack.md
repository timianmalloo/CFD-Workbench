---
id: proof-m12d-dlg2
title: DLG-2 granted seams and dialog proof
type: proof-pack
status: active
owner: "@track-dlg2"
phase: implementation
tags: [m12d, dlg, catalog, provenance, library, red-first]
links:
  - { to: design-m12d-catalog, rel: depends-on }
  - { to: mockup-m12d-catalog, rel: depends-on }
  - { to: defect-classes, rel: relates-to }
review-by: 2026-11-04
summary: >-
  DLG-2 proof for refused candidate bytes, station provenance, catalog failure cause,
  damaged library rows, and proposed save copy. Records the rendered tests, one full
  ring attempt, the isolated repairs, and the remaining gate failures.
---

# DLG-2 proof

Session `dlg2`, tier T2, 2026-10-04. The visual authority is `docs/mockups/m12d-catalog.html`.
The four seam grants are implemented in commit `65fec70`. The required UI CAD direction
head `d410fcc` was merged by SHA into `e41a3c1` before gates.

## Claims and falsifiers

| Claim | Writer → reader | Falsifying check and result | Confidence |
|---|---|---|---|
| Refused candidate bytes cannot become admissible `Bytes` | `SectionReplace.Refused` → `ReplacePreview.RefusedBytes` and `RefusedResidual` → controller's refused slot → `SectionEditorView.RefreshPreview`; `Patch` reads only `Bytes` after refusal check | `Replace_UniqueRootCambered_RefusedCatSpacingNothingChanged` asserts `Bytes == null`, nonempty `RefusedBytes`, equal residual and no step; PASS in Core full run. | Verified |
| Station card names the profile's provenance | `WorkbenchController.StationSource` parses the profile provenance → `PropertiesContext.StationSource` → `s:source` in the realized Properties model | `Properties_StationSource_UsesProfileProvenance` sees `Catalog original · NACA 0012 (GEN)`; PASS in Desktop full run. | Verified |
| Catalog failure explains its cause | `Catalog.Load` classifies missing file versus failed check → `CatalogSnapshot.FailureCause` → dialog detail | `CatalogDialog_CatalogUnavailable_ShowsCauseAndCancel` failed before the wiring and PASSed after it; exact missing-file cause and Cancel are asserted. | Verified |
| Damaged library entries remain visible and unusable | `SectionLibrary.Scan().Problems` → `CatalogSnapshot.ProblemRows` → My sections disabled row and reason | `CatalogDialog_DamagedLibraryRow_DisabledWithReason` writes a damaged file in a real temp library, then asserts the controller snapshot, rendered row help, detail and disabled Replace; PASS. | Verified |
| Unsupported and uncertain save states use reviewable copy | `ContractError.Code` → `SaveSectionDialog` message | `SaveDialog_UnsupportedPersistence_ExplainsSafety` failed before mapping, then PASSed for COPY-200…203, including the fallback. | Verified |

The first focused dialog run, before the UI wiring, printed `FAIL` for the catalog-unavailable cause,
damaged row, and unsupported save copy. It also exposed two native persistence failures because
that run lacked the repository's non-symlinked `TMPDIR`. The required `tools/run-tests.sh` set
that directory and both Core parts passed.

## Repair after the full ring

The full ring had `FAIL Properties_SectionGroup_OwnTcAndPerStationTcConsequence`: its oracle
used `Rows[0]`, which now denotes Source. The test reads `sec:own` by key; isolated
`--properties-view --part=1/2` passed. The same ring timed out in
`SectionCommands_EveryRow_RunsOrNamesReason`: it awaited Replace and Save modal commands
without a choice. The sweep still checks their availability and reasons, and their dialog
tests exercise the actions. Isolated `--status-strip --part=1/3` passed after the repair.

## Gate record and limit

`tools/run-tests.sh` ran once: Core 334 + 333 PASS, Desktop 659 PASS with the two failures
above, Analysis 109 PASS, CLI 5 PASS; wall 132 s against the 60 s budget. The post-run DLG
named checker found all 23 named DLG PASS lines but failed because five `FAIL` lines from
the old Desktop log are present. `check-docs.py` passed; `check-event-subscribers.py`
reported 27 events, 4 allowed, 0 findings. `tools/ui-craft-gate.py` is absent. The
bundle script scanned the approved mockup and reported one Minor `side-tab` finding.
The repairs were verified in isolated fast partitions; no second full ring was run under
the brief's once-only instruction. Operator review remains pending for COPY-200…203.
